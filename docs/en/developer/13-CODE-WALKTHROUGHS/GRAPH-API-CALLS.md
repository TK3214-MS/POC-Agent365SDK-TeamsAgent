# Graph API Calls - Detailed Call Patterns

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../../developer/13-CODE-WALKTHROUGHS/GRAPH-API-CALLS.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](GRAPH-API-CALLS.md)

## 📋 Call Patterns by Category

### Mail Search

#### Basic Pattern

```csharp
var messages = await _graphClient.Users[_userId].Messages
    .GetAsync(config =>
    {
        config.QueryParameters.Filter = "receivedDateTime ge 2026-02-01T00:00:00Z";
        config.QueryParameters.Top = 10;
        config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };
        config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
    });
```

**Generated HTTP Request**:
```http
GET /v1.0/users/user@company.com/messages?
  $filter=receivedDateTime ge 2026-02-01T00:00:00Z
  &$top=10
  &$select=subject,from,receivedDateTime
  &$orderby=receivedDateTime desc
Authorization: Bearer eyJ0eXAiOiJKV1Qi...
```

#### Advanced Filtering

```csharp
// AND conditions
config.QueryParameters.Filter = 
    "receivedDateTime ge 2026-02-01T00:00:00Z " +
    "and receivedDateTime le 2026-02-07T23:59:59Z " +
    "and hasAttachments eq true";

// OR conditions (categories)
config.QueryParameters.Filter = 
    "categories/any(c: c eq '商談' or c eq '提案')";

// NOT conditions
config.QueryParameters.Filter = 
    "not(isDraft eq true)";
```

### Calendar Search

```csharp
var events = await _graphClient.Users[_userId].Calendar.Events
    .GetAsync(config =>
    {
        config.QueryParameters.Filter = 
            "start/dateTime ge '2026-02-03T00:00:00' " +
            "and end/dateTime le '2026-02-09T23:59:59'";
        config.QueryParameters.Select = new[] 
        {
            "subject", "start", "end", "attendees", "location"
        };
        config.QueryParameters.Orderby = new[] { "start/dateTime" };
    });
```

**JSON Response**:
```json
{
  "value": [
    {
      "subject": "株式会社A社 商談",
      "start": {
        "dateTime": "2026-02-05T14:00:00",
        "timeZone": "Tokyo Standard Time"
      },
      "end": {
        "dateTime": "2026-02-05T15:00:00",
        "timeZone": "Tokyo Standard Time"
      },
      "attendees": [
        {
          "emailAddress": {
            "name": "田中太郎",
            "address": "tanaka@company.com"
          },
          "type": "required"
        }
      ],
      "location": {
        "displayName": "会議室A"
      }
    }
  ]
}
```

### SharePoint Search

```csharp
var items = await _graphClient.Users[_userId].Drive.Root
    .Search("提案書")
    .GetAsync(config =>
    {
        config.QueryParameters.Top = 20;
        config.QueryParameters.Select = new[] 
        {
            "name", "webUrl", "lastModifiedDateTime", "size"
        };
    });
```

**Response Processing**:
```csharp
foreach (var item in items.Value)
{
    Console.WriteLine($"📄 {item.Name}");
    Console.WriteLine($"   URL: {item.WebUrl}");
    Console.WriteLine($"   Updated: {item.LastModifiedDateTime:yyyy/MM/dd}");
    Console.WriteLine($"   Size: {item.Size / 1024}KB");
}
```

---

## Performance Optimization

### 1. Minimize Select Fields

```csharp
// ❌ BAD - All fields (response ~10KB)
var messages = await _graphClient.Users[_userId].Messages.GetAsync();

// ✅ GOOD - Required fields only (response ~2KB)
config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };
```

**Effect**: 80% reduction in response size, 75% reduction in transfer time

### 2. Limit with Top

```csharp
// When the first 10 items are sufficient
config.QueryParameters.Top = 10;
```

**Effect**: 60% reduction in API processing time

### 3. Server-Side Filtering

```csharp
// ❌ BAD - Retrieve all then filter client-side
var allMessages = await _graphClient.Users[_userId].Messages.GetAsync();
var filtered = allMessages.Value.Where(m => m.Subject.Contains("商談"));

// ✅ GOOD - Server-side filtering
config.QueryParameters.Filter = "contains(subject, '商談')";
```

**Effect**: 90% reduction in data transfer volume

---

## Batch Requests

### Aggregating Multiple API Calls

```csharp
var batchRequestContent = new BatchRequestContentCollection(_graphClient);

// Request 1: Mail
var messagesRequest = _graphClient.Users[_userId].Messages
    .ToGetRequestInformation(config =>
    {
        config.QueryParameters.Top = 10;
        config.QueryParameters.Select = new[] { "subject", "from" };
    });
var messagesStepId = await batchRequestContent.AddBatchRequestStepAsync(messagesRequest);

// Request 2: Calendar
var eventsRequest = _graphClient.Users[_userId].Calendar.Events
    .ToGetRequestInformation(config =>
    {
        config.QueryParameters.Top = 5;
    });
var eventsStepId = await batchRequestContent.AddBatchRequestStepAsync(eventsRequest);

// Execute batch (single HTTP request)
var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);

// Retrieve results
var messages = await batchResponse.GetResponseByIdAsync<MessageCollectionResponse>(messagesStepId);
var events = await batchResponse.GetResponseByIdAsync<EventCollectionResponse>(eventsStepId);
```

**Performance Comparison**:
```
Sequential (2 HTTP requests):
  Request 1: Messages (500ms)
  Request 2: Events   (400ms)
  Total time: 900ms

Batch (1 HTTP request):
  Batch Request: (600ms)
  Total time: 600ms (33% faster)
```

---

## Error Handling

### ServiceException Handling

```csharp
try
{
    var messages = await _graphClient.Users[_userId].Messages.GetAsync();
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 401)
{
    _logger.LogError("Authentication error: Token is invalid or expired");
    // Normally does not occur as TokenCredential auto-refreshes
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 403)
{
    _logger.LogError("Insufficient permissions: Mail.Read permission is missing");
    // Add permissions in Azure AD app registration and obtain admin consent
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
{
    _logger.LogWarning("User not found: {UserId}", _userId);
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 429)
{
    var retryAfter = ex.ResponseHeaders?.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
    _logger.LogWarning("Rate limit: Retrying after {Seconds} seconds", retryAfter.TotalSeconds);
    await Task.Delay(retryAfter);
    // Retry
}
catch (ServiceException ex)
{
    _logger.LogError(ex, "Graph API error: {Code}", ex.ResponseStatusCode);
}
```

### Retry Policy (Polly)

```csharp
var retryPolicy = Policy
    .Handle<ServiceException>(ex => 
        ex.ResponseStatusCode == 429 ||  // Rate limit
        ex.ResponseStatusCode >= 500)    // Server error
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning(
                "Retry {RetryCount}/3: Retrying after {Delay} seconds",
                retryCount,
                timeSpan.TotalSeconds
            );
        }
    );

var messages = await retryPolicy.ExecuteAsync(async () =>
{
    return await _graphClient.Users[_userId].Messages.GetAsync();
});
```

---

## Rate Limiting Countermeasures

### Throttling Detection

```csharp
var response = await _graphClient.Users[_userId].Messages.GetAsync();

// Retrieve rate limit information from response headers
if (response.OdataNextLink != null)
{
    // Pagination is required
    _logger.LogInformation("NextLink present: More data exists");
}

// Retry-After header check (on exception only)
catch (ServiceException ex) when (ex.ResponseStatusCode == 429)
{
    var retryAfter = ex.ResponseHeaders?.RetryAfter;
    if (retryAfter?.Delta.HasValue == true)
    {
        await Task.Delay(retryAfter.Delta.Value);
    }
}
```

### Request Interval Adjustment

```csharp
private readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(5, 5);  // Up to 5 concurrent requests

public async Task<MessageCollectionResponse> GetMessagesWithRateLimitAsync()
{
    await _rateLimiter.WaitAsync();
    try
    {
        return await _graphClient.Users[_userId].Messages.GetAsync();
    }
    finally
    {
        await Task.Delay(200);  // Wait 200ms
        _rateLimiter.Release();
    }
}
```

---

## Pagination

### OData NextLink Handling

```csharp
var allMessages = new List<Message>();
var response = await _graphClient.Users[_userId].Messages.GetAsync(config =>
{
    config.QueryParameters.Top = 50;
});

allMessages.AddRange(response.Value);

// Paginate while NextLink exists
while (response.OdataNextLink != null)
{
    var nextPageRequest = new HttpRequestMessage(HttpMethod.Get, response.OdataNextLink);
    response = await _graphClient.RequestAdapter.SendAsync(
        nextPageRequest,
        MessageCollectionResponse.CreateFromDiscriminatorValue
    );
    
    allMessages.AddRange(response.Value);
    
    _logger.LogInformation("Page retrieved: Cumulative {Count} items", allMessages.Count);
}

_logger.LogInformation("All items retrieved: {TotalCount} items", allMessages.Count);
```

---

## Next Steps

- **[CONVERSATION-FLOW.md](CONVERSATION-FLOW.md)**: Conversation Flow Details
- **[03-AUTHENTICATION-FLOW.md](../03-AUTHENTICATION-FLOW.md)**: Authentication Flow
- **[04-DATA-FLOW.md](../04-DATA-FLOW.md)**: Data Flow
