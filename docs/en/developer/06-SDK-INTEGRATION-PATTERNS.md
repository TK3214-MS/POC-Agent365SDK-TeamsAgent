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

This document explains major SDK integration patterns and best practices used in Sales Support Agent.

---

## Microsoft.Extensions.AI Patterns

### Pattern 1: IChatClient Builder Pattern

**Purpose**: Abstract LLM provider and extend features with middleware chain

**Implementation**:

```csharp
public class GitHubModelsProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;

    public GitHubModelsProvider(GitHubModelsSettings settings)
    {
        // Build middleware with Builder pattern
        _chatClient = new ChatClientBuilder()
            // Base client
            .Use(CreateGitHubModelsClient(settings))
            // Telemetry
            .UseOpenTelemetry(sourceName: "SalesSupportAgent", configure: options =>
            {
                options.EnableSensitiveData = false;
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
- ✅ Separate cross-cutting concerns with middleware (telemetry, logging, tools)
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
        AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments)
    };

    return chatClient.AsAIAgent(
        systemPrompt: SystemPrompt,
        name: "Sales Support Agent",
        tools: tools
    );
}
```

**Points**:
- `AIFunctionFactory.Create`: Auto-generate tool schema from methods
- `AsAIAgent`: Convert IChatClient → AIAgent
- Tools: Integrated tool management

---

## Agent 365 SDK Patterns

### Pattern 3: Observability Pattern

**Purpose**: Distributed tracing and metrics collection

```csharp
public class AgentMetrics
{
    private static readonly ActivitySource _activitySource = 
        new ActivitySource("SalesSupportAgent");
    private static readonly Counter<long> _requestCounter = 
        _meter.CreateCounter<long>("agent.requests");

    public static async Task<T> InvokeObservedHttpOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            _requestCounter.Add(1, new("success", true));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _requestCounter.Add(1, new("success", false));
            throw;
        }
    }
}
```

### Pattern 4: Notification Pattern

**Purpose**: Real-time notifications and progress tracking

```csharp
// Start notification
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "🚀 Starting...", 
    progress: 0);

// Progress update
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "📊 Collecting data...", 
    progress: 25);

// Completion
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

    public async Task<string> SearchSalesEmails(
        string startDate,
        string endDate)
    {
        try
        {
            var messages = await _graphClient.Users[_userId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"receivedDateTime ge {startDate}";
                    config.QueryParameters.Top = 50;
                    config.QueryParameters.Select = new[] { "subject", "from" };
                });

            return FormatResults(messages);
        }
        catch (ServiceException ex)
        {
            return HandleGraphError(ex);
        }
    }

    private string HandleGraphError(ServiceException ex)
    {
        return ex.ResponseStatusCode switch
        {
            401 => "❌ Authentication error",
            403 => "❌ Insufficient permissions",
            429 => "❌ Rate limit",
            _ => $"❌ Error: {ex.Message}"
        };
    }
}
```

### Pattern 6: Batch Request Pattern

**Purpose**: Consolidate multiple Graph API calls into one HTTP request

```csharp
public async Task<CombinedDataResponse> GetCombinedDataAsync(string userId)
{
    var batchRequestContent = new BatchRequestContentCollection(_graphClient);
    
    // Request 1: Email
    var messageRequest = _graphClient.Users[userId].Messages.ToGetRequestInformation();
    var messageStepId = await batchRequestContent.AddBatchRequestStepAsync(messageRequest);
    
    // Request 2: Calendar
    var calendarRequest = _graphClient.Users[userId].Calendar.ToGetRequestInformation();
    var calendarStepId = await batchRequestContent.AddBatchRequestStepAsync(calendarRequest);
    
    // Execute batch
    var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);
    
    // Get results
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
  Total: 900ms

Batch:
  POST /batch { requests: [messages, calendar] } (600ms)
  Total: 600ms (33% faster)
```

---

For complete pattern catalog including error handling strategies, telemetry patterns, retry policies, and advanced integration scenarios, please refer to the Japanese version at [../developer/06-SDK-INTEGRATION-PATTERNS.md](../../developer/06-SDK-INTEGRATION-PATTERNS.md).
