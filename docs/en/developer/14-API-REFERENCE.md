# API Reference - Core Classes and Interfaces

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/14-API-REFERENCE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](14-API-REFERENCE.md)

## 📋 Core Interfaces

### ILLMProvider

**Location**: `Services/LLM/ILLMProvider.cs`

```csharp
public interface ILLMProvider
{
    string ProviderName { get; }
    IChatClient GetChatClient();
}
```

**Implementation Classes**:
- `AzureOpenAIProvider`
- `GitHubModelsProvider`
- `OllamaProvider`

**Usage Example**:
```csharp
var provider = serviceProvider.GetRequiredService<ILLMProvider>();
var chatClient = provider.GetChatClient();
```

---

## Agent Classes

### SalesAgent

**Location**: `Services/Agent/SalesAgent.cs`

#### Constructor

```csharp
public SalesAgent(
    ILLMProvider llmProvider,
    OutlookEmailTool emailTool,
    OutlookCalendarTool calendarTool,
SharePointTool sharePointTool,
    TeamsMessageTool teamsTool,
    ObservabilityService observabilityService,
    NotificationService notificationService,
    ILogger<SalesAgent> logger)
```

#### Key Methods

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(
    SalesSummaryRequest request)
```

**Parameters**:
- `request.Query`: User query
- `request.StartDate`: Search start date (optional)
- `request.EndDate`: Search end date (optional)

**Return Value**:
```csharp
public class SalesSummaryResponse
{
    public string Response { get; set; }
    public List<string> DataSources { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string LLMProvider { get; set; }
}
```

---

## MCP Tools

### OutlookEmailTool

**Location**: `Services/MCP/McpTools/OutlookEmailTool.cs`

#### SearchSalesEmails

```csharp
[Description("Searches and retrieves sales-related emails")]
public async Task<string> SearchSalesEmails(
    [Description("Search start date (yyyy-MM-dd)")] string startDate,
    [Description("Search end date (yyyy-MM-dd)")] string endDate,
    [Description("Search keywords")] string keywords = "商談,提案,見積,契約")
```

### OutlookCalendarTool

**Location**: `Services/MCP/McpTools/OutlookCalendarTool.cs`

#### SearchSalesMeetings

```csharp
[Description("Searches sales-related calendar events")]
public async Task<string> SearchSalesMeetings(
    [Description("Search start date (yyyy-MM-dd)")] string startDate,
    [Description("Search end date (yyyy-MM-dd)")] string endDate,
    [Description("Search keywords")] string keywords = "商談,提案,ミーティング")
```

### SharePointTool

**Location**: `Services/MCP/McpTools/SharePointTool.cs`

#### SearchSalesDocuments

```csharp
[Description("Searches sales documents from SharePoint")]
public async Task<string> SearchSalesDocuments(
    [Description("Search start date (yyyy-MM-dd)")] string startDate,
    [Description("Search end date (yyyy-MM-dd)")] string endDate,
    [Description("Search keywords")] string keywords = "提案書,見積書,契約書")
```

---

## Observability Service

### ObservabilityService

**Location**: `Services/Observability/ObservabilityService.cs`

#### Trace Recording

```csharp
public async Task RecordTraceAsync(string message, string level, long timestamp)
```

**Parameters**:
- `message`: Trace message
- `level`: `"info"` | `"success"` | `"error"` | `"warning"`
- `timestamp`: Elapsed time (milliseconds)

#### Detailed Trace Session

```csharp
public string StartDetailedTrace(string conversationId, string userId, string userQuery)

public async Task AddTracePhaseAsync(
    string sessionId,
    string phaseName,
    string description,
    object? metadata = null,
    string status = "Completed")

public async Task CompleteDetailedTraceAsync(
    string sessionId,
    string finalResponse,
    bool success)
```

#### Metrics

```csharp
public async Task RecordRequestAsync(bool success, long latencyMs)

public async Task UpdateMetricsAsync()

public MetricsSummary GetMetricsSummary()
```

**MetricsSummary**:
```csharp
public class MetricsSummary
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public long TotalProcessingTimeMs { get; set; }
}
```

---

## Notification Service

### NotificationService

**Location**: `Services/Notifications/NotificationService.cs`

```csharp
public async Task SendProgressNotificationAsync(
    string operationId,
    string message,
    int progress)  // 0-100

public async Task SendSuccessNotificationAsync(
    string operationId,
    string message,
    object? metadata = null)

public async Task SendErrorNotificationAsync(
    string operationId,
    string message,
    string errorDetails)
```

---

## Telemetry

### AgentMetrics

**Location**: `Telemetry/AgentMetrics.cs`

#### Activity Source and Meter

```csharp
public static readonly string SourceName = "SalesSupportAgent";
private static readonly ActivitySource _activitySource = new ActivitySource(SourceName);
private static readonly Meter _meter = new Meter("SalesSupportAgent.Metrics");
```

#### Observable Operations

```csharp
public static async Task<T> InvokeObservedHttpOperation<T>(
    string operationName,
    Func<Task<T>> operation)
```

**Usage Example**:
```csharp
return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
{
    var response = await salesAgent.GenerateSalesSummaryAsync(request);
    return Results.Ok(response);
});
```

---

## Configuration Classes

### M365Settings

**Location**: `Configuration/M365Settings.cs`

```csharp
public class M365Settings
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string UserId { get; set; }
    public bool UseManagedIdentity { get; set; }
    public string[] Scopes { get; set; }
    
    public bool IsConfigured => /* Validation logic */;
}
```

### LLMSettings

**Location**: `Configuration/LLMSettings.cs`

```csharp
public class LLMSettings
{
    public string Provider { get; set; }        // "AzureOpenAI" | "GitHubModels" | "Ollama"
    public string DeploymentName { get; set; }
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string ModelId { get; set; }
}
```

### BotSettings

**Location**: `Configuration/BotSettings.cs`

```csharp
public class BotSettings
{
    public string MicrosoftAppId { get; set; }
    public string MicrosoftAppPassword { get; set; }
    public string MicrosoftAppTenantId { get; set; }
    public string MicrosoftAppType { get; set; }
}
```

---

## Related Documentation

- [SDK Overview](01-SDK-OVERVIEW.md) - SDK architecture overview
- [Project Structure](02-PROJECT-STRUCTURE.md) - Project file structure
- [Dependency Injection](05-DEPENDENCY-INJECTION.md) - Service registration patterns
- [Error Handling](07-ERROR-HANDLING.md) - Exception handling
- [Code Walkthroughs](13-CODE-WALKTHROUGHS.md) - Detailed code explanations
