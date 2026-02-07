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
    private readonly ConcurrentDictionary<string, AgentInfo> _activeAgents = new();
    private readonly ConcurrentDictionary<string, DetailedTraceSession> _traceSessions = new();
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

    // ========== エージェント情報管理 ==========

    /// <summary>
    /// アクティブエージェントを登録・更新
    /// </summary>
    public async Task RegisterAgentAsync(string agentId, string agentName, string agentType, string status = "Active", string? iconUrl = null)
    {
        var agentInfo = new AgentInfo
        {
            AgentId = agentId,
            AgentName = agentName,
            AgentType = agentType,
            Status = status,
            RegisteredAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow,
            Version = "1.0.0",
            TotalInteractions = 0,
            IconUrl = iconUrl
        };

        _activeAgents.AddOrUpdate(agentId, agentInfo, (key, existing) =>
        {
            existing.LastActiveAt = DateTime.UtcNow;
            existing.Status = status;
            return existing;
        });

        _logger.LogInformation("🤖 エージェント登録: {AgentName} ({AgentType})", agentName, agentType);

        await _hubContext.Clients.All.SendAsync("AgentUpdate", agentInfo);
    }

    /// <summary>
    /// エージェントのアクティビティを更新
    /// </summary>
    public async Task UpdateAgentActivityAsync(string agentId, string activity)
    {
        if (_activeAgents.TryGetValue(agentId, out var agent))
        {
            agent.LastActiveAt = DateTime.UtcNow;
            agent.LastActivity = activity;
            agent.TotalInteractions++;

            await _hubContext.Clients.All.SendAsync("AgentUpdate", agent);
        }
    }

    /// <summary>
    /// すべてのアクティブエージェントを取得
    /// </summary>
    public IEnumerable<AgentInfo> GetActiveAgents()
    {
        return _activeAgents.Values.OrderByDescending(a => a.LastActiveAt);
    }

    // ========== 詳細トレース管理 ==========

    /// <summary>
    /// 詳細トレースセッション開始
    /// </summary>
    public string StartDetailedTrace(string conversationId, string userId, string userQuery)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new DetailedTraceSession
        {
            SessionId = sessionId,
            ConversationId = conversationId,
            UserId = userId,
            UserQuery = userQuery,
            StartTime = DateTime.UtcNow,
            Phases = new List<TracePhase>()
        };

        _traceSessions.TryAdd(sessionId, session);

        _logger.LogInformation("🎯 詳細トレース開始: {SessionId} - {UserQuery}", sessionId, userQuery);

        return sessionId;
    }

    /// <summary>
    /// トレースフェーズを追加
    /// </summary>
    public async Task AddTracePhaseAsync(string sessionId, string phaseName, string description, object? data = null, string status = "Completed")
    {
        if (_traceSessions.TryGetValue(sessionId, out var session))
        {
            var phase = new TracePhase
            {
                PhaseName = phaseName,
                Description = description,
                Timestamp = DateTime.UtcNow,
                Data = data,
                Status = status
            };

            session.Phases.Add(phase);

            _logger.LogInformation("📍 トレースフェーズ: {PhaseName} - {Description}", phaseName, description);

            await _hubContext.Clients.All.SendAsync("TracePhaseUpdate", new
            {
                sessionId,
                phase
            });
        }
    }

    /// <summary>
    /// 詳細トレースセッション完了
    /// </summary>
    public async Task CompleteDetailedTraceAsync(string sessionId, string finalResponse, bool success = true)
    {
        if (_traceSessions.TryGetValue(sessionId, out var session))
        {
            session.EndTime = DateTime.UtcNow;
            session.FinalResponse = finalResponse;
            session.Success = success;

            _logger.LogInformation("🏁 詳細トレース完了: {SessionId} - Success: {Success}", sessionId, success);

            await _hubContext.Clients.All.SendAsync("TraceSessionComplete", session);
        }
    }

    /// <summary>
    /// 詳細トレースセッションを取得
    /// </summary>
    public DetailedTraceSession? GetDetailedTrace(string sessionId)
    {
        _traceSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// すべての詳細トレースセッションを取得
    /// </summary>
    public IEnumerable<DetailedTraceSession> GetAllDetailedTraces(int count = 50)
    {
        return _traceSessions.Values
            .OrderByDescending(s => s.StartTime)
            .Take(count);
    }

    /// <summary>
    /// 会話IDで詳細トレースを検索
    /// </summary>
    public IEnumerable<DetailedTraceSession> GetTracesByConversation(string conversationId)
    {
        return _traceSessions.Values
            .Where(s => s.ConversationId == conversationId)
            .OrderByDescending(s => s.StartTime);
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

/// <summary>
/// エージェント情報
/// </summary>
public class AgentInfo
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Idle, Busy, Offline
    public DateTime RegisteredAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public string Version { get; set; } = "1.0.0";
    public int TotalInteractions { get; set; }
    public string? LastActivity { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>
/// 詳細トレースセッション
/// </summary>
public class DetailedTraceSession
{
    public string SessionId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserQuery { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FinalResponse { get; set; }
    public bool Success { get; set; }
    public List<TracePhase> Phases { get; set; } = new();
    
    public double? DurationMs => EndTime.HasValue 
        ? (EndTime.Value - StartTime).TotalMilliseconds 
        : null;
}

/// <summary>
/// トレースフェーズ
/// </summary>
public class TracePhase
{
    public string PhaseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object? Data { get; set; }
    public string Status { get; set; } = "Completed"; // Completed, Running, Failed
}
