using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using SalesSupportAgent.Hubs;

namespace SalesSupportAgent.Services.Observability;

/// <summary>
/// Agent 365 Observabilityメトリクス収集・配信サービス
/// </summary>
public class ObservabilityService
{
    private readonly IHubContext<ObservabilityHub> _hubContext;
    private readonly ILogger<ObservabilityService> _logger;
    private readonly ConcurrentQueue<TraceEvent> _recentTraces = new();
    private readonly ConcurrentDictionary<string, MetricData> _metrics = new();
    private readonly object _lock = new();
    private DateTime _startTime = DateTime.UtcNow;
    
    private int _totalRequests;
    private long _totalProcessingTimeMs;
    private int _successfulRequests;
    private int _failedRequests;

    public ObservabilityService(
        IHubContext<ObservabilityHub> hubContext,
        ILogger<ObservabilityService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        
        // 初期メトリクスを設定
        _metrics["app.start_time"] = new MetricData { Name = "App Start Time", Value = _startTime.ToString("o") };
    }

    /// <summary>
    /// トレースイベントを記録してリアルタイム配信
    /// </summary>
    public async Task RecordTraceAsync(string operation, string status, long durationMs, Dictionary<string, string>? additionalData = null)
    {
        var trace = new TraceEvent
        {
            Timestamp = DateTime.UtcNow,
            Operation = operation,
            Status = status,
            DurationMs = durationMs,
            AdditionalData = additionalData ?? new Dictionary<string, string>()
        };

        _recentTraces.Enqueue(trace);
        
        // 最新100件のみ保持
        while (_recentTraces.Count > 100)
        {
            _recentTraces.TryDequeue(out _);
        }

        _logger.LogInformation("📊 トレース記録: {Operation} - {Status} ({DurationMs}ms)", operation, status, durationMs);

        // SignalRでリアルタイム配信
        await _hubContext.Clients.All.SendAsync("TraceUpdate", new
        {
            trace.Timestamp,
            trace.Operation,
            trace.Status,
            trace.DurationMs,
            Icon = GetStatusIcon(status),
            trace.AdditionalData
        });
    }

    /// <summary>
    /// リクエストメトリクスを記録
    /// </summary>
    public async Task RecordRequestAsync(bool success, long durationMs)
    {
        lock (_lock)
        {
            _totalRequests++;
            _totalProcessingTimeMs += durationMs;
            
            if (success)
                _successfulRequests++;
            else
                _failedRequests++;
        }

        await UpdateMetricsAsync();
    }

    /// <summary>
    /// メトリクスを更新してリアルタイム配信
    /// </summary>
    public async Task UpdateMetricsAsync()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var averageResponseTime = _totalRequests > 0 
            ? (double)_totalProcessingTimeMs / _totalRequests 
            : 0;
        var successRate = _totalRequests > 0 
            ? (double)_successfulRequests / _totalRequests * 100 
            : 0;

        var metrics = new
        {
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            AverageResponseTimeMs = Math.Round(averageResponseTime, 2),
            SuccessRate = Math.Round(successRate, 2),
            UptimeHours = Math.Round(uptime.TotalHours, 2),
            LastUpdated = DateTime.UtcNow
        };

        await _hubContext.Clients.All.SendAsync("MetricsUpdate", metrics);
    }

    /// <summary>
    /// 最近のトレースを取得
    /// </summary>
    public IEnumerable<TraceEvent> GetRecentTraces(int count = 20)
    {
        return _recentTraces.TakeLast(count).Reverse();
    }

    /// <summary>
    /// 現在のメトリクスサマリーを取得
    /// </summary>
    public object GetMetricsSummary()
    {
        var uptime = DateTime.UtcNow - _startTime;
        var averageResponseTime = _totalRequests > 0 
            ? (double)_totalProcessingTimeMs / _totalRequests 
            : 0;
        var successRate = _totalRequests > 0 
            ? (double)_successfulRequests / _totalRequests * 100 
            : 0;

        return new
        {
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            AverageResponseTimeMs = Math.Round(averageResponseTime, 2),
            SuccessRate = Math.Round(successRate, 2),
            Uptime = uptime.ToString(@"hh\:mm\:ss"),
            UptimeHours = Math.Round(uptime.TotalHours, 2),
            StartTime = _startTime,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static string GetStatusIcon(string status)
    {
        return status.ToLower() switch
        {
            "success" or "completed" or "✅" => "✅",
            "running" or "in-progress" or "🔵" => "🔵",
            "failed" or "error" or "❌" => "❌",
            "warning" or "⚠️" => "⚠️",
            _ => "ℹ️"
        };
    }
}

/// <summary>
/// パラメータとして渡されるトレースイベント
/// </summary>
public class TraceEvent
{
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

/// <summary>
/// メトリクスデータ
/// </summary>
public class MetricData
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
