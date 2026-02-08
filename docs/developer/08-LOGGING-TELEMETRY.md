# Logging & Telemetry - ロギングとテレメトリの実装

## 📋 ロギング構成

### Program.cs での設定

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "SalesSupportAgent", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddSource(AgentMetrics.SourceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());
```

### ログレベル

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Graph": "Information",
      "SalesSupportAgent": "Debug"
    }
  }
}
```

## 構造化ロギング

### ベストプラクティス

```csharp
// ✅ GOOD - 構造化ロギング
_logger.LogInformation(
    "商談サマリ生成開始: Query={Query}, StartDate={StartDate}, EndDate={EndDate}",
    request.Query,
    request.StartDate,
    request.EndDate
);

// ❌ BAD - 文字列結合
_logger.LogInformation($"商談サマリ生成開始: {request.Query}");
```

### ログ出力例

```
info: SalesSupportAgent.Services.Agent.SalesAgent[0]
      商談サマリ生成開始: Query="今週の商談サマリ", StartDate="2026-02-03", EndDate="2026-02-09"
      
info: SalesSupportAgent.Services.MCP.McpTools.OutlookEmailTool[0]
      📧 メール検索: UserId="user@company.com", Filter="receivedDateTime ge 2026-02-03..."
      
info: SalesSupportAgent.Services.Agent.SalesAgent[0]
      ✅ 商談サマリ生成完了: ProcessingTime=3700ms
```

## テレメトリ

### ActivitySource（分散トレーシング）

```csharp
public class AgentMetrics
{
    public static readonly string SourceName = "SalesSupportAgent";
    private static readonly ActivitySource _activitySource = new ActivitySource(SourceName);

    public static async Task<T> InvokeObservedOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        activity?.SetTag("operation.type", "agent");
        
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Meter（メトリクス）

```csharp
public class AgentMetrics
{
    private static readonly Meter _meter = new Meter("SalesSupportAgent.Metrics");
    private static readonly Counter<long> _requestCounter = 
        _meter.CreateCounter<long>("agent.requests", description: "Total requests");
    private static readonly Histogram<double> _latencyHistogram = 
        _meter.CreateHistogram<double>("agent.latency", unit: "ms");

    public static void RecordRequest(bool success, double latencyMs)
    {
        _requestCounter.Add(1, new("success", success));
        _latencyHistogram.Record(latencyMs);
    }
}
```

## ObservabilityService

### トレース記録

```csharp
public class ObservabilityService
{
    public async Task RecordTraceAsync(string message, string level, long timestamp)
    {
        var trace = new TraceEvent
        {
            Message = message,
            Level = level,
            Timestamp = DateTimeOffset.UtcNow,
            ElapsedMs = timestamp
        };
        
        _traces.Add(trace);
        await _hubContext.Clients.All.SendAsync("ReceiveTrace", trace);
    }
}
```

### 詳細トレースセッション

```csharp
public string StartDetailedTrace(string conversationId, string userId, string userQuery)
{
    var sessionId = Guid.NewGuid().ToString();
    var session = new DetailedTraceSession
    {
        SessionId = sessionId,
        ConversationId = conversationId,
        UserId = userId,
        UserQuery = userQuery,
        StartTime = DateTimeOffset.UtcNow,
        Phases = new List<TracePhase>()
    };
    
    _detailedTraceSessions[sessionId] = session;
    return sessionId;
}

public async Task AddTracePhaseAsync(
    string sessionId,
    string phaseName,
    string description,
    object? metadata = null,
    string status = "Completed")
{
    if (_detailedTraceSessions.TryGetValue(sessionId, out var session))
    {
        session.Phases.Add(new TracePhase
        {
            PhaseName = phaseName,
            Description = description,
            Timestamp = DateTimeOffset.UtcNow,
            Metadata = metadata,
            Status = status
        });
        
        await _hubContext.Clients.All.SendAsync("UpdateDetailedTrace", session);
    }
}
```

## ダッシュボード連携

### SignalR配信

```csharp
// リアルタイムトレース配信
await _hubContext.Clients.All.SendAsync("ReceiveTrace", trace);

// メトリクス更新配信
await _hubContext.Clients.All.SendAsync("UpdateMetrics", metricsSummary);

// 通知配信
await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
```

## 次のステップ

- **[OBSERVABILITY-DASHBOARD.md](../OBSERVABILITY-DASHBOARD.md)**: ダッシュボード詳細
- **[10-PERFORMANCE-OPTIMIZATION.md](10-PERFORMANCE-OPTIMIZATION.md)**: パフォーマンス最適化
