# Extensibility - Adding New Tools and Features

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/12-EXTENSIBILITY.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](12-EXTENSIBILITY.md)

## 📋 Extension Points

- [Adding New MCP Tools](#adding-new-mcp-tools)
- [Adding New LLM Providers](#adding-new-llm-providers)
- [Custom Middleware](#custom-middleware)
- [Custom Telemetry](#custom-telemetry)

---

## Adding New MCP Tools

### Step 1: Create Tool Class

```csharp
public class CustomDataTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomDataTool> _logger;

    public CustomDataTool(HttpClient httpClient, ILogger<CustomDataTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    [Description("Fetches custom sales data from external API")]
    public async Task<string> GetCustomData(
        [Description("Data category")] string category,
        [Description("Date range")] string dateRange)
    {
        _logger.LogInformation("Fetching custom data: {Category}, {DateRange}", category, dateRange);
        
        var response = await _httpClient.GetAsync($"/api/data?category={category}&range={dateRange}");
        var data = await response.Content.ReadAsStringAsync();
        
        return FormatData(data);
    }

    private string FormatData(string rawData)
    {
        // Format logic
        return $"📊 Custom Data:\n{rawData}";
    }
}
```

### Step 2: Register in DI

**Program.cs**:

```csharp
builder.Services.AddHttpClient<CustomDataTool>();
builder.Services.AddSingleton<CustomDataTool>();
```

### Step 3: Add to Agent Tools

**SalesAgent.cs**:

```csharp
private AIAgent CreateAgent()
{
    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        AIFunctionFactory.Create(_customDataTool.GetCustomData)  // NEW
    };

    return _chatClient.AsAIAgent(SystemPrompt, "Sales Agent", tools: tools);
}
```

### Step 4: Update System Prompt

```csharp
private const string SystemPrompt = @"
You are a sales support agent with access to:
1. SearchSalesEmails - Outlook emails
2. SearchSalesMeetings - Calendar events
3. GetCustomData - External sales data (NEW)

Use GetCustomData to fetch additional metrics from the CRM system.
";
```

---

## Adding New LLM Providers

### Step 1: Implement ILLMProvider

```csharp
public class AnthropicProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;

    public AnthropicProvider(AnthropicSettings settings)
    {
        _chatClient = new ChatClientBuilder()
            .Use(new AnthropicChatClient(
                apiKey: settings.ApiKey,
                modelId: settings.ModelId))
            .UseOpenTelemetry(sourceName: "SalesSupportAgent")
            .UseFunctionInvocation()
            .Build();
    }

    public IChatClient GetChatClient() => _chatClient;
}
```

### Step 2: Add Configuration

**appsettings.json**:

```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "ModelId": "claude-3-opus"
    }
  }
}
```

### Step 3: Register in DI

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var settings = sp.GetRequiredService<LLMSettings>();
    return settings.Provider switch
    {
        "AzureOpenAI" => new AzureOpenAIProvider(settings.AzureOpenAI),
        "Ollama" => new OllamaProvider(settings.Ollama),
        "Anthropic" => new AnthropicProvider(settings.Anthropic),  // NEW
        _ => throw new NotSupportedException($"Provider {settings.Provider} not supported")
    };
});
```

---

## Custom Middleware

### Custom Logging Middleware

```csharp
public class CustomLoggingMiddleware : DelegatingChatClient
{
    private readonly ILogger _logger;

    public CustomLoggingMiddleware(IChatClient innerClient, ILogger logger) 
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> messages,
        ChatOptions options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("LLM Request: {MessageCount} messages", messages.Count);
        
        var sw = Stopwatch.StartNew();
        var response = await base.CompleteAsync(messages, options, cancellationToken);
        
        _logger.LogInformation("LLM Response: {Latency}ms, {TokenCount} tokens", 
            sw.ElapsedMilliseconds, 
            response.Usage?.TotalTokenCount);
        
        return response;
    }
}
```

### Register Middleware

```csharp
_chatClient = new ChatClientBuilder()
    .Use(CreateBaseClient())
    .Use(client => new CustomLoggingMiddleware(client, logger))  // Add middleware
    .Build();
```

---

For complete extension guides, plugin architecture, event hooks, and custom storage providers, please refer to the Japanese version at [../developer/12-EXTENSIBILITY.md](../../developer/12-EXTENSIBILITY.md).
