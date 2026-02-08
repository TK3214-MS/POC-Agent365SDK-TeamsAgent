# Logging & Telemetry - Observability Implementation

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/08-LOGGING-TELEMETRY.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](08-LOGGING-TELEMETRY.md)

## 📋 Key Topics

- [Logging Strategy](#logging-strategy)
- [OpenTelemetry Integration](#opentelemetry-integration)
- [Metrics Collection](#metrics-collection)
- [Distributed Tracing](#distributed-tracing)
- [Production Monitoring](#production-monitoring)

---

## Logging Strategy

### ILogger Usage

```csharp
public class OutlookEmailTool
{
    private readonly ILogger<OutlookEmailTool> _logger;

    public OutlookEmailTool(ILogger<OutlookEmailTool> logger)
    {
        _logger = logger;
    }

    public async Task<string> SearchSalesEmails(...)
    {
        _logger.LogInformation("Searching emails from {StartDate} to {EndDate}", startDate, endDate);
        
        try
        {
            var messages = await _graphClient.Users[_userId].Messages.GetAsync();
            _logger.LogInformation("Found {Count} messages", messages.Value.Count);
            return FormatResults(messages);
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error: {Code}", ex.ResponseStatusCode);
            throw;
        }
    }
}
```

### Log Levels

| Level | Use Case | Example |
|-------|----------|---------|
| **Trace** | Detailed debug info | "Token: abc123..." |
| **Debug** | Development debugging | "Query filter: receivedDateTime ge 2026-02-01" |
| **Information** | Normal flow | "Searching emails from 2026-02-01" |
| **Warning** | Recoverable errors | "Rate limit hit, retrying..." |
| **Error** | Non-recoverable errors | "Graph API authentication failed" |
| **Critical** | System failure | "Database connection lost" |

---

## OpenTelemetry Integration

### Configuration in Program.cs

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("SalesSupportAgent")
            .AddSource("Microsoft.Extensions.AI")
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("SalesSupportAgent.Metrics")
            .AddConsoleExporter();
    });
```

---

## Metrics Collection

### Agent Metrics

**Telemetry/AgentMetrics.cs**:

```csharp
public class AgentMetrics
{
    private static readonly Meter _meter = new Meter("SalesSupportAgent.Metrics");
    
    private static readonly Counter<long> _requestCounter = 
        _meter.CreateCounter<long>("agent.requests", description: "Total requests");
    
    private static readonly Histogram<double> _latencyHistogram = 
        _meter.CreateHistogram<double>("agent.latency", unit: "ms");
    
    private static readonly Counter<long> _errorCounter = 
        _meter.CreateCounter<long>("agent.errors");

    public static void RecordRequest(string operation, double latencyMs, bool success)
    {
        _requestCounter.Add(1, 
            new("operation", operation),
            new("success", success));
        
        _latencyHistogram.Record(latencyMs, 
            new("operation", operation));
        
        if (!success)
        {
            _errorCounter.Add(1, new("operation", operation));
        }
    }
}
```

---

## Distributed Tracing

### Activity Source

```csharp
public class SalesAgent
{
    private static readonly ActivitySource _activitySource = new ActivitySource("SalesSupportAgent");

    public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request)
    {
        using var activity = _activitySource.StartActivity("GenerateSalesSummary");
        activity?.SetTag("request.query", request.Query);
        activity?.SetTag("request.startDate", request.StartDate);
        
        var sw = Stopwatch.StartNew();
        
        try
        {
            var response = await _agent.RunAsync(request.Query);
            
            activity?.SetTag("response.length", response.Text.Length);
            activity?.SetTag("processing.timeMs", sw.ElapsedMilliseconds);
            
            return new SalesSummaryResponse { Response = response.Text };
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            throw;
        }
    }
}
```

### Trace Hierarchy

```
GenerateSalesSummary (SalesAgent)
├── RunAsync (AIAgent)
│   ├── CompleteAsync (IChatClient)
│   │   └── HTTP POST (Azure OpenAI / Ollama)
│   ├── SearchSalesEmails (OutlookEmailTool)
│   │   └── Users[userId].Messages.GetAsync (GraphServiceClient)
│   │       └── HTTP GET (Microsoft Graph API)
│   └── SearchSalesMeetings (OutlookCalendarTool)
│       └── Users[userId].Calendar.GetAsync (GraphServiceClient)
│           └── HTTP GET (Microsoft Graph API)
└── SendSuccessNotificationAsync (NotificationService)
    └── SignalR.SendAsync
```

---

## Production Monitoring

### Application Insights Integration

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        });
    });
```

### Key Metrics to Monitor

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| `agent.requests` | Total requests | N/A |
| `agent.latency` | Average response time | > 10s |
| `agent.errors` | Error count | > 5 in 5min |
| `graph.api.errors` | Graph API failures | > 3 in 5min |
| `llm.timeout` | LLM timeouts | > 1 in 10min |

---

For complete logging configuration, custom telemetry processors, sampling strategies, and production dashboards, please refer to the Japanese version at [../developer/08-LOGGING-TELEMETRY.md](../../developer/08-LOGGING-TELEMETRY.md).
