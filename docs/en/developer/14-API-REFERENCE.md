# API Reference - Core Interfaces and Classes

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/14-API-REFERENCE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](14-API-REFERENCE.md)

## 📋 Core APIs

- [ILLMProvider](#illmprovider)
- [SalesAgent](#salesagent)
- [MCP Tools](#mcp-tools)
- [ObservabilityService](#observabilityservice)
- [NotificationService](#notificationservice)

---

## ILLMProvider

### Interface Definition

```csharp
public interface ILLMProvider
{
    IChatClient GetChatClient();
}
```

### Implementations

- `AzureOpenAIProvider`
- `OllamaProvider`
- `GitHubModelsProvider`

### Usage Example

```csharp
var chatClient = _llmProvider.GetChatClient();
var response = await chatClient.CompleteAsync(messages);
```

---

## SalesAgent

### Class Definition

```csharp
public class SalesAgent
{
    public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request);
}
```

### SalesSummaryRequest

```csharp
public class SalesSummaryRequest
{
    public string Query { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
```

### SalesSummaryResponse

```csharp
public class SalesSummaryResponse
{
    public string Response { get; set; }
    public long ProcessingTimeMs { get; set; }
}
```

---

## MCP Tools

### OutlookEmailTool

```csharp
public class OutlookEmailTool
{
    [Description("Searches sales-related emails from Outlook")]
    public async Task<string> SearchSalesEmails(
        [Description("Start date (YYYY-MM-DD)")] string startDate,
        [Description("End date (YYYY-MM-DD)")] string endDate,
        [Description("Comma-separated keywords")] string keywords = "提案,見積");
}
```

### OutlookCalendarTool

```csharp
public class OutlookCalendarTool
{
    [Description("Searches sales meetings from Outlook Calendar")]
    public async Task<string> SearchSalesMeetings(
        [Description("Start date")] string startDate,
        [Description("End date")] string endDate);
}
```

### SharePointTool

```csharp
public class SharePointTool
{
    [Description("Searches sales documents from SharePoint")]
    public async Task<string> SearchSalesDocuments(
        [Description("Start date")] string startDate,
        [Description("End date")] string endDate,
        [Description("Keywords")] string keywords = "提案書,見積,契約書");
}
```

---

## ObservabilityService

### Methods

```csharp
public class ObservabilityService
{
    public string StartDetailedTrace(string conversationId, string userId, string userQuery);
    public Task AddTracePhaseAsync(string sessionId, string phaseName, string description, object metadata);
    public ObservabilityMetrics GetMetrics();
    public List<ObservabilityTrace> GetTraces(int count = 10);
}
```

### ObservabilityMetrics

```csharp
public class ObservabilityMetrics
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan Uptime { get; set; }
}
```

---

## NotificationService

### Methods

```csharp
public class NotificationService
{
    public Task SendProgressNotificationAsync(string operationId, string message, int progressPercentage);
    public Task SendSuccessNotificationAsync(string operationId, string message, object metadata = null);
    public Task SendErrorNotificationAsync(string operationId, string message, string errorDetails);
    public List<AgentNotification> GetNotificationHistory(int count = 20);
}
```

### AgentNotification

```csharp
public class AgentNotification
{
    public string Id { get; set; }
    public string OperationId { get; set; }
    public string Type { get; set; }  // "progress", "success", "error"
    public string Message { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public object Data { get; set; }
}
```

---

For complete API reference including all methods, parameters, return types, and usage examples, please refer to the Japanese version at [../developer/14-API-REFERENCE.md](../../developer/14-API-REFERENCE.md).
