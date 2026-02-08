# Performance Optimization - Latency and Throughput Improvements

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/10-PERFORMANCE-OPTIMIZATION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](10-PERFORMANCE-OPTIMIZATION.md)

## 📋 Optimization Areas

- [Graph API Optimization](#graph-api-optimization)
- [LLM Response Time](#llm-response-time)
- [Caching Strategies](#caching-strategies)
- [Parallel Processing](#parallel-processing)

---

## Graph API Optimization

### Use $select to Reduce Payload

```csharp
// ❌ BAD - Fetches all fields
var messages = await _graphClient.Users[userId].Messages.GetAsync();

// ✅ GOOD - Fetches only required fields
var messages = await _graphClient.Users[userId].Messages.GetAsync(config =>
{
    config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };
});
```

**Performance Impact**:
- Without Select: 500ms (200KB response)
- With Select: 200ms (50KB response) - **60% faster**

### Batch Requests

```csharp
// ❌ BAD - 3 sequential requests (1500ms)
var emails = await _graphClient.Users[userId].Messages.GetAsync();  // 500ms
var calendar = await _graphClient.Users[userId].Calendar.GetAsync(); // 500ms
var files = await _graphClient.Users[userId].Drive.Root.Children.GetAsync(); // 500ms

// ✅ GOOD - 1 batch request (600ms)
var batchRequest = new BatchRequestContentCollection(_graphClient);
await batchRequest.AddBatchRequestStepAsync(_graphClient.Users[userId].Messages.ToGetRequestInformation());
await batchRequest.AddBatchRequestStepAsync(_graphClient.Users[userId].Calendar.ToGetRequestInformation());
var batchResponse = await _graphClient.Batch.PostAsync(batchRequest);
```

---

## LLM Response Time

### Stream Responses

```csharp
// ❌ BAD - Wait for complete response (15s)
var response = await _chatClient.CompleteAsync(messages);

// ✅ GOOD - Stream tokens (first token in 1s)
await foreach (var update in _chatClient.CompleteStreamingAsync(messages))
{
    await turnContext.SendActivityAsync(update.Text);
}
```

### Model Selection

| Model | Latency | Quality | Use Case |
|-------|---------|---------|----------|
| GPT-4 | 10-15s | Excellent | Complex analysis |
| GPT-3.5 | 2-5s | Good | Quick responses |
| Ollama (local) | 1-3s | Variable | Development, privacy |

---

## Caching Strategies

### Memory Cache for Graph Data

```csharp
public class OutlookEmailTool
{
    private readonly IMemoryCache _cache;

    public async Task<string> SearchSalesEmails(string startDate, string endDate)
    {
        var cacheKey = $"emails_{startDate}_{endDate}";
        
        if (_cache.TryGetValue(cacheKey, out string cachedResult))
        {
            return cachedResult;
        }
        
        var result = await _graphClient.Users[_userId].Messages.GetAsync();
        
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}
```

**Performance Improvement**:
- Without Cache: 500ms per request
- With Cache: 5ms per request - **99% faster**

---

## Parallel Processing

### Parallel Tool Invocation

```csharp
// ❌ BAD - Sequential (2000ms)
var emails = await _emailTool.SearchSalesEmails(...);    // 500ms
var calendar = await _calendarTool.SearchSalesMeetings(...); // 500ms
var docs = await _sharePointTool.SearchSalesDocuments(...);  // 1000ms

// ✅ GOOD - Parallel (1000ms)
var tasks = new[]
{
    _emailTool.SearchSalesEmails(...),
    _calendarTool.SearchSalesMeetings(...),
    _sharePointTool.SearchSalesDocuments(...)
};

var results = await Task.WhenAll(tasks);
```

---

For complete performance benchmarks, profiling tools, load testing strategies, and production optimization patterns, please refer to the Japanese version at [../developer/10-PERFORMANCE-OPTIMIZATION.md](../../developer/10-PERFORMANCE-OPTIMIZATION.md).
