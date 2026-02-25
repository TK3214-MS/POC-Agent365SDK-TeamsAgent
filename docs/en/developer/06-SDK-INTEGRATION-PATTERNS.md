# SDK Integration Patterns - Best Practices and Design Patterns

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/06-SDK-INTEGRATION-PATTERNS.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](06-SDK-INTEGRATION-PATTERNS.md)

## 📋 Table of Contents

- [Overview](#overview)
- [Microsoft.Extensions.AI Patterns](#microsoftextensionsai-patterns)
- [Agent 365 SDK Patterns](#agent-365-sdk-patterns)
- [Microsoft 365 SDK Patterns](#microsoft-365-sdk-patterns)
- [Dependency Injection Patterns](#dependency-injection-patterns)
- [Error Handling Patterns](#error-handling-patterns)
- [Telemetry Patterns](#telemetry-patterns)

---

## Overview

This document explains the key SDK integration patterns and best practices used in the Sales Support Agent.

---

## Microsoft.Extensions.AI Patterns

### Pattern 1: IChatClient Builder Pattern

**Purpose**: Abstract LLM providers and extend functionality with a middleware chain

**Implementation**:

```csharp
public class GitHubModelsProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;

    public GitHubModelsProvider(GitHubModelsSettings settings)
    {
        // Configure middleware using the Builder pattern
        _chatClient = new ChatClientBuilder()
            // Base client
            .Use(CreateGitHubModelsClient(settings))
            // Telemetry
            .UseOpenTelemetry(sourceName: "SalesSupportAgent", configure: options =>
            {
                options.EnableSensitiveData = false;
                options.JsonSerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
            })
            // Logging
            .UseLogging(loggerFactory: LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            }))
            // Function invocation
            .UseFunctionInvocation()
            // Build
            .Build();
    }
}
```

**Benefits**:
- ✅ Easy provider switching (Azure OpenAI ↔ Ollama ↔ GitHub Models)
- ✅ Separation of cross-cutting concerns via middleware (telemetry, logging, tool invocation)
- ✅ Testability (Mock IChatClient)

### Pattern 2: AIAgent Pattern

**Purpose**: Standardize tool integration and system prompts

```csharp
private AIAgent CreateAgent()
{
    var chatClient = _llmProvider.GetChatClient();

    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments),
        AIFunctionFactory.Create(_teamsTool.SearchSalesMessages)
    };

    return chatClient.AsAIAgent(
        systemPrompt: SystemPrompt,
        name: "Sales Support Agent",
        tools: tools
    );
}
```

**Key Points**:
- `AIFunctionFactory.Create`: Automatically generates tool schemas from methods
- `AsAIAgent`: Converts IChatClient → AIAgent
- Tools: Unified management of multiple tools

---

## Agent 365 SDK Patterns

### Pattern 3: Observability Pattern

**Purpose**: Distributed tracing and metrics collection

```csharp
public class AgentMetrics
{
    private static readonly ActivitySource _activitySource = 
        new ActivitySource("SalesSupportAgent");
    private static readonly Meter _meter = 
        new Meter("SalesSupportAgent.Metrics");
    private static readonly Counter<long> _requestCounter = 
        _meter.CreateCounter<long>("agent.requests");
    private static readonly Histogram<double> _latencyHistogram = 
        _meter.CreateHistogram<double>("agent.latency", unit: "ms");

    public static async Task<T> InvokeObservedHttpOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            _requestCounter.Add(1, new("operation", operationName), new("success", true));
            _latencyHistogram.Record(sw.ElapsedMilliseconds, new("operation", operationName));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _requestCounter.Add(1, new("operation", operationName), new("success", false));
            throw;
        }
    }
}
```

**Usage Example**:

```csharp
app.MapPost("/api/sales-summary", async (request, salesAgent) =>
{
    return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
    {
        var response = await salesAgent.GenerateSalesSummaryAsync(request);
        return Results.Ok(response);
    });
});
```

### Pattern 4: Notification Pattern

**Purpose**: Real-time notifications and progress tracking

```csharp
// Start notification
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "🚀 Starting sales summary generation...", 
    progress: 0);

// Progress notification
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "📊 Collecting data...", 
    progress: 25);

// Completion notification
await _notificationService.SendSuccessNotificationAsync(
    operationId, 
    "✅ Complete!",
    metadata: new { ProcessingTimeMs = 3500 });
```

---

## Microsoft 365 SDK Patterns

### Pattern 5: Repository Pattern with Graph API

**Purpose**: Encapsulate Graph API calls

```csharp
public class OutlookEmailTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _userId;

    public async Task<string> SearchSalesEmails(
        string startDate,
        string endDate,
        string keywords)
    {
        try
        {
            var messages = await _graphClient.Users[_userId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = BuildFilter(startDate, endDate);
                    config.QueryParameters.Top = 50;
                    config.QueryParameters.Select = SelectFields();
                    config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                });

            return FormatResults(messages, keywords);
        }
        catch (ServiceException ex)
        {
            return HandleGraphError(ex);
        }
    }

    private string BuildFilter(string start, string end) =>
        $"receivedDateTime ge {start} and receivedDateTime le {end}";

    private string[] SelectFields() =>
        new[] { "subject", "from", "receivedDateTime", "bodyPreview" };

    private string FormatResults(MessageCollectionResponse messages, string keywords)
    {
        // Filtering and summary generation
    }

    private string HandleGraphError(ServiceException ex)
    {
        return ex.ResponseStatusCode switch
        {
            401 => "❌ Authentication error",
            403 => "❌ Insufficient permissions",
            429 => "❌ Rate limit exceeded",
            _ => $"❌ Error: {ex.Message}"
        };
    }
}
```

### Pattern 6: Batch Request Pattern

**Purpose**: Consolidate multiple Graph API calls into a single HTTP request

```csharp
public async Task<CombinedDataResponse> GetCombinedDataAsync(string userId)
{
    var batchRequestContent = new BatchRequestContentCollection(_graphClient);
    
    // Request 1: Emails
    var messageRequest = _graphClient.Users[userId].Messages.ToGetRequestInformation();
    var messageStepId = await batchRequestContent.AddBatchRequestStepAsync(messageRequest);
    
    // Request 2: Calendar
    var calendarRequest = _graphClient.Users[userId].Calendar.ToGetRequestInformation();
    var calendarStepId = await batchRequestContent.AddBatchRequestStepAsync(calendarRequest);
    
    // Execute batch
    var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);
    
    // Retrieve results
    var messages = await batchResponse.GetResponseByIdAsync<MessageCollectionResponse>(messageStepId);
    var calendar = await batchResponse.GetResponseByIdAsync<Calendar>(calendarStepId);
    
    return new CombinedDataResponse(messages, calendar);
}
```

**Performance Improvement**:
```
Sequential:
  GET /messages (500ms)
  GET /calendar (400ms)
  Total time: 900ms

Batch:
  POST /batch { requests: [messages, calendar] } (600ms)
  Total time: 600ms (33% faster)
```

---

## Dependency Injection Patterns

### Pattern 7: Service Registration Pattern

**Program.cs**:

```csharp
// Singleton services (stateful, shared across the entire application)
builder.Services.AddSingleton<TokenCredential>(/* implementation */);
builder.Services.AddSingleton<GraphServiceClient>(/* implementation */);
builder.Services.AddSingleton<ILLMProvider>(/* implementation */);
builder.Services.AddSingleton<ObservabilityService>();

// Singleton MCP Tools
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();

// Transient services (new instance every time)
builder.Services.AddTransient<IBot, TeamsBot>();
```

**Lifetime Selection Criteria**:

| Lifetime | Use Case | Example |
|----------|----------|---------|
| **Singleton** | Shared state, expensive initialization | GraphServiceClient, ObservabilityService |
| **Scoped** | HTTP request scope | Database context |
| **Transient** | Lightweight, stateless | Bot (new for each conversation) |

### Pattern 8: Options Pattern

```csharp
// appsettings.json
{
  "M365": {
    "TenantId": "...",
    "ClientId": "...",
    "UseManagedIdentity": false
  }
}

// Configuration class
public class M365Settings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; }
}

// Registration
builder.Services.Configure<M365Settings>(
    builder.Configuration.GetSection("M365"));

// Injection
public class OutlookEmailTool
{
    public OutlookEmailTool(IOptions<M365Settings> options)
    {
        var settings = options.Value;
    }
}
```

---

## Error Handling Patterns

### Pattern 9: Graceful Degradation Pattern

**Purpose**: Prevent partial failures from affecting the entire system

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(request)
{
    var emailData = await TryGetEmailData(request);  // Returns null on failure
    var calendarData = await TryGetCalendarData(request);  // Returns null on failure
    
    if (emailData == null && calendarData == null)
    {
        return new SalesSummaryResponse
        {
            Response = "Failed to retrieve data. Please check the configuration."
        };
    }
    
    // Perform LLM inference using only the data that was successfully retrieved
    var availableData = new List<string>();
    if (emailData != null) availableData.Add(emailData);
    if (calendarData != null) availableData.Add(calendarData);
    
    var summary = await _llm.GenerateSummaryAsync(string.Join("\n", availableData));
    return new SalesSummaryResponse { Response = summary };
}

private async Task<string?> TryGetEmailData(request)
{
    try
    {
        return await _emailTool.SearchSalesEmails(...);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to retrieve email data");
        return null;
    }
}
```

### Pattern 10: Circuit Breaker Pattern (Polly)

**Purpose**: Prevent unnecessary calls to a repeatedly failing service

```csharp
// NuGet: Polly
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<ServiceException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromMinutes(1)
    );

public async Task<string> SearchEmailsWithCircuitBreaker(query)
{
    return await circuitBreakerPolicy.ExecuteAsync(async () =>
    {
        return await _emailTool.SearchSalesEmails(query);
    });
}
```

**Behavior**:
```
Request 1 → Failure (1/3)
Request 2 → Failure (2/3)
Request 3 → Failure (3/3) → Circuit OPEN

Request 4 → Circuit OPEN → Immediate error response (no API call)
Request 5 → Circuit OPEN → Immediate error response

[1 minute elapsed]

Request 6 → Circuit HALF-OPEN → Attempt
  → Success → Circuit CLOSED (normal operation restored)
```

---

## Telemetry Patterns

### Pattern 11: Distributed Tracing Pattern

**Purpose**: Track processing flows across microservices

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(request)
{
    using var activity = Activity.Current?.Source.StartActivity("GenerateSalesSummary");
    activity?.SetTag("user.query", request.Query);
    activity?.SetTag("date.range", $"{request.StartDate} - {request.EndDate}");
    
    try
    {
        // Phase 1: Data collection
        using var dataCollectionActivity = Activity.Current?.Source.StartActivity("DataCollection");
        var emailData = await CollectEmailData(request);
        dataCollectionActivity?.SetTag("email.count", emailData.Count);
        
        // Phase 2: LLM inference
        using var llmActivity = Activity.Current?.Source.StartActivity("LLMInference");
        llmActivity?.SetTag("llm.provider", _llmProvider.ProviderName);
        var response = await _agent.RunAsync(request.Query);
        llmActivity?.SetTag("response.length", response.Length);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        return new SalesSummaryResponse { Response = response };
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

**Trace Output Example**:
```
GenerateSalesSummary (3700ms)
├── DataCollection (1200ms)
│   ├── SearchSalesEmails (800ms)
│   │   └── GraphAPICall /messages (600ms)
│   └── SearchSalesMeetings (400ms)
│       └── GraphAPICall /events (300ms)
└── LLMInference (2500ms)
    ├── LLMRequest /chat/completions (2000ms)
    └── ResponseExtraction (500ms)
```

### Pattern 12: Metrics Pattern

```csharp
public class AgentMetrics
{
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _latencyHistogram;
    private readonly Gauge<int> _activeAgentsGauge;

    public void RecordRequest(string operation, bool success, double latencyMs)
    {
        _requestCounter.Add(1, 
            new("operation", operation),
            new("success", success)
        );
        
        _latencyHistogram.Record(latencyMs,
            new("operation", operation)
        );
    }

    public void UpdateActiveAgents(int count)
    {
        _activeAgentsGauge.Record(count);
    }
}
```

**Dashboard Queries**:
```promql
# Request success rate
rate(agent_requests_total{success="true"}[5m]) / 
rate(agent_requests_total[5m]) * 100

# P95 latency
histogram_quantile(0.95, sum(rate(agent_latency_bucket[5m])) by (le))

# Active agent count
agent_active_agents
```

---

## Summary

Key patterns used in the Sales Support Agent:

### Microsoft.Extensions.AI
- ✅ **Builder Pattern**: Middleware chain
- ✅ **AIAgent Pattern**: Tool integration

### Agent 365 SDK
- ✅ **Observability Pattern**: Distributed tracing
- ✅ **Notification Pattern**: Real-time notifications

### Microsoft 365 SDK
- ✅ **Repository Pattern**: Graph API encapsulation
- ✅ **Batch Request Pattern**: Performance optimization

### Cross-Cutting Patterns
- ✅ **DI Pattern**: Service lifetime management
- ✅ **Graceful Degradation**: Partial failure tolerance
- ✅ **Circuit Breaker**: Prevention of cascading failures
- ✅ **Distributed Tracing**: End-to-end observability

### Next Steps

- **[07-ERROR-HANDLING.md](07-ERROR-HANDLING.md)**: Error handling details
- **[08-LOGGING-TELEMETRY.md](08-LOGGING-TELEMETRY.md)**: Logging and telemetry
- **[10-PERFORMANCE-OPTIMIZATION.md](10-PERFORMANCE-OPTIMIZATION.md)**: Performance optimization
