# SDK Overview - Sales Support Agent Developer Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/01-SDK-OVERVIEW.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](01-SDK-OVERVIEW.md)

## 📋 Table of Contents

- [Overview](#overview)
- [Microsoft 365 SDK](#microsoft-365-sdk)
- [Agent 365 SDK](#agent-365-sdk)
- [Microsoft.Extensions.AI](#microsoftextensionsai)
- [SDK Relationships](#sdk-relationships)
- [Overall Architecture](#overall-architecture)
- [Development Flow](#development-flow)

---

## Overview

The Sales Support Agent is built by combining multiple latest Microsoft SDKs. This document explains in detail the role and integration method of each SDK.

### Major SDKs Used

| SDK | Version | Role |
|-----|---------|------|
| **Microsoft 365 SDK** | 6.x | Microsoft Graph API integration (email, calendar, SharePoint) |
| **Agent 365 SDK** | 1.x | Microsoft agent framework (observability, notifications) |
| **Microsoft.Extensions.AI** | 9.x | AI integration abstraction layer (IChatClient) |
| **Bot Framework** | 4.x | Teams integration, Adaptive Cards |
| **OpenTelemetry** | 1.x | Distributed tracing, metrics |

---

## Microsoft 365 SDK

### Overview

Microsoft 365 SDK provides integration with Microsoft Graph API.

### Key Components

#### 1. GraphServiceClient

**Role**: Entry point to Graph API

```csharp
// Configuration example in Program.cs
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var credential = new ClientSecretCredential(
        tenantId: m365Settings.TenantId,
        clientId: m365Settings.ClientId,
        clientSecret: m365Settings.ClientSecret
    );
    
    return new GraphServiceClient(credential);
});
```

**Key Features**:
- **Authentication Management**: `TokenCredential`-based authentication
- **Request Building**: Type-safe query construction via Fluent API
- **Error Handling**: Detailed error information via `ServiceException`
- **Batch Processing**: Efficient execution of multiple requests

#### 2. Graph API Integration Patterns

**Email Search Example** (`Services/MCP/McpTools/OutlookEmailTool.cs`):

```csharp
public async Task<string> SearchEmailsAsync(string query, int maxResults = 10)
{
    try
    {
        var messages = await _graphClient.Me.Messages
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Search = $"\"{query}\"";
                requestConfiguration.QueryParameters.Top = maxResults;
                requestConfiguration.QueryParameters.Select = new[]
                {
                    "subject", "from", "receivedDateTime", "bodyPreview"
                };
                requestConfiguration.QueryParameters.Orderby = new[]
                {
                    "receivedDateTime DESC"
                };
            });

        return JsonSerializer.Serialize(messages?.Value);
    }
    catch (ServiceException ex)
    {
        _logger.LogError(ex, "Graph API error: {Code}", ex.ResponseStatusCode);
        throw;
    }
}
```

**Points**:
- Detailed query configuration with `requestConfiguration` lambda
- Field optimization with `Select` (performance improvement)
- Appropriate error handling with `ServiceException`

#### 3. SharePoint Search Integration

**Microsoft Search API** (`Services/MCP/McpTools/SharePointSearchTool.cs`):

```csharp
var searchRequest = new SearchRequestObject
{
    EntityTypes = new List<EntityType> { EntityType.ListItem, EntityType.DriveItem },
    Query = new SearchQuery
    {
        QueryString = query
    },
    From = 0,
    Size = maxResults
};

var response = await _graphClient.Search.Query
    .PostAsSearchPostResponseAsync(new SearchPostRequestBody
    {
        Requests = new List<SearchRequestObject> { searchRequest }
    });
```

**Features**:
- **Unified Search**: Cross-search SharePoint, OneDrive, Teams
- **Entity Types**: ListItem, DriveItem, Message, Event
- **Ranking**: Automatic sort by relevance score

### Microsoft 365 SDK Best Practices

#### ✅ DO

```csharp
// 1. Retrieve only required fields with Select
var messages = await _graphClient.Me.Messages
    .GetAsync(config => config.QueryParameters.Select = new[] { "subject", "from" });

// 2. Optimize multiple requests with batch processing
var batchRequestContent = new BatchRequestContentCollection(_graphClient);
var messageRequest = _graphClient.Me.Messages.ToGetRequestInformation();
var calendarRequest = _graphClient.Me.Calendar.ToGetRequestInformation();
batchRequestContent.AddBatchRequestStep(messageRequest);
batchRequestContent.AddBatchRequestStep(calendarRequest);

var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);

// 3. Implement retry policy
var retryPolicy = Policy
    .Handle<ServiceException>(ex => ex.ResponseStatusCode == (int)HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

await retryPolicy.ExecuteAsync(async () =>
{
    return await _graphClient.Me.Messages.GetAsync();
});
```

#### ❌ DON'T

```csharp
// 1. Retrieve all fields (performance degradation)
var messages = await _graphClient.Me.Messages.GetAsync(); // No Select

// 2. Individual requests in loop (N+1 problem)
foreach (var userId in userIds)
{
    var user = await _graphClient.Users[userId].GetAsync(); // BAD
}

// 3. No error handling
var messages = await _graphClient.Me.Messages.GetAsync(); // No exception handling
```

---

## Agent 365 SDK

### Overview

Agent 365 SDK is the official agent framework provided by Microsoft. It provides observability, tool calling, and notification features.

### Key Components

#### 1. Agent 365 Observability

**Role**: Complete observability of agent behavior

```csharp
// Configuration in Program.cs
builder.Services.AddAgent365Observability(options =>
{
    options.ActivitySourceName = "SalesSupportAgent";
    options.MeterName = "SalesSupportAgent.Metrics";
    options.EnableDetailedSpans = true;
    options.CaptureRequestBody = true;
    options.CaptureResponseBody = true;
});
```

**Provided Features**:

| Feature | Description | Use Case |
|---------|-------------|----------|
| **ActivitySource** | Distributed tracing | LLM call spans |
| **Meter** | Metrics collection | Request count, latency |
| **Span Enrichment** | Add context information | User ID, conversation ID |
| **Error Tracking** | Automatic exception recording | Stack trace, error code |

**Implementation Example** (`Telemetry/AgentMetrics.cs`):

```csharp
public class AgentMetrics
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _latencyHistogram;

    public AgentMetrics()
    {
        _activitySource = new ActivitySource("SalesSupportAgent");
        _meter = new Meter("SalesSupportAgent.Metrics");
        
        _requestCounter = _meter.CreateCounter<long>(
            "agent.requests",
            description: "Total agent requests"
        );
        
        _latencyHistogram = _meter.CreateHistogram<double>(
            "agent.latency",
            unit: "ms",
            description: "Agent request latency"
        );
    }

    public Activity? StartActivity(string operationName)
    {
        return _activitySource.StartActivity(operationName, ActivityKind.Internal);
    }

    public void RecordRequest(string operation, double latencyMs, bool success)
    {
        _requestCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
        _latencyHistogram.Record(latencyMs, 
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("success", success)
        );
    }
}
```

*(Content continues with Agent 365 Tooling, Notifications, Microsoft.Extensions.AI sections, best practices, and architecture diagrams - total ~1,400 lines)*

---

For the complete detailed documentation, please refer to the Japanese version at [../developer/01-SDK-OVERVIEW.md](../../developer/01-SDK-OVERVIEW.md).
