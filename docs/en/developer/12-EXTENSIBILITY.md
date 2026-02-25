# Extensibility - Extensibility Patterns and Customization

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/12-EXTENSIBILITY.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](12-EXTENSIBILITY.md)

## 📋 Adding New Tools

### Step 1: Create Tool Class

```csharp
using System.ComponentModel;
using Microsoft.Graph;

namespace SalesSupportAgent.Services.MCP.McpTools;

public class OneDriveTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _userId;

    public OneDriveTool(GraphServiceClient graphClient, M365Settings settings)
    {
        _graphClient = graphClient;
        _userId = settings.UserId;
    }
    
    [Description("Searches for sales documents from OneDrive")]
    public async Task<string> SearchSalesDocuments(
        [Description("Search keyword")] string query,
        [Description("Maximum number of results")] int maxResults = 10)
    {
        try
        {
            var items = await _graphClient.Users[_userId].Drive.Root
                .Search(query)
                .GetAsync(config =>
                {
                    config.QueryParameters.Top = maxResults;
                    config.QueryParameters.Select = new[] { "name", "webUrl", "lastModifiedDateTime" };
                });

            var summary = $"📁 OneDrive Search Results ({items.Value.Count} items)\n\n";
            foreach (var item in items.Value)
            {
                summary += $"- **{item.Name}**\n";
                summary += $"  URL: {item.WebUrl}\n";
                summary += $"  Updated: {item.LastModifiedDateTime:yyyy/MM/dd}\n\n";
            }
            
            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ OneDrive search error: {ex.Message}";
        }
    }
}
```

### Step 2: Register in DI Container

**Program.cs**:

```csharp
// Register MCP tools
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<OneDriveTool>();  // Added
```

### Step 3: Register with Agent

**SalesAgent.cs**:

```csharp
public class SalesAgent
{
    private readonly OneDriveTool _oneDriveTool;  // Added
    
    public SalesAgent(
        ILLMProvider llmProvider,
        OutlookEmailTool emailTool,
        OutlookCalendarTool calendarTool,
        OneDriveTool oneDriveTool,  // Added
        /* ... */)
    {
        _oneDriveTool = oneDriveTool;
        // ...
    }
    
    private AIAgent CreateAgent()
    {
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
            AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
            AIFunctionFactory.Create(_oneDriveTool.SearchSalesDocuments),  // Added
        };
        
        return chatClient.AsAIAgent(SystemPrompt, "Sales Support Agent", tools: tools);
    }
}
```

---

## Adding a New LLM Provider

### Step 1: Create Provider Class

```csharp
using Microsoft.Extensions.AI;

namespace SalesSupportAgent.Services.LLM;

public class AnthropicProvider : ILLMProvider
{
    private readonly AnthropicSettings _settings;
    private readonly IChatClient _chatClient;

    public string ProviderName => "Anthropic Claude";

    public AnthropicProvider(AnthropicSettings settings)
    {
        _settings = settings;
        
        _chatClient = new ChatClientBuilder()
            .Use(new HttpClient
            {
                BaseAddress = new Uri("https://api.anthropic.com"),
                DefaultRequestHeaders =
                {
                    { "x-api-key", settings.ApiKey },
                    { "anthropic-version", "2023-06-01" }
                }
            }.AsChatClient(settings.Model))
            .UseOpenTelemetry()
            .UseLogging()
            .UseFunctionInvocation()
            .Build();
    }

    public IChatClient GetChatClient() => _chatClient;
}
```

### Step 2: Add Settings Class

```csharp
public class AnthropicSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-opus-20240229";
}

public class LLMSettings
{
    public string Provider { get; set; } = "AzureOpenAI";
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
    public AnthropicSettings Anthropic { get; set; } = new();  // Added
}
```

### Step 3: appsettings.json

```json
{
  "LLM": {
    "Provider": "Anthropic",
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-3-opus-20240229"
    }
  }
}
```

### Step 4: Register in Program.cs

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var settings = sp.GetRequiredService<LLMSettings>();
    
    return settings.Provider?.ToLower() switch
    {
        "azureopenai" => new AzureOpenAIProvider(settings.AzureOpenAI),
        "ollama" => new OllamaProvider(settings.Ollama),
        "anthropic" => new AnthropicProvider(settings.Anthropic),  // Added
        _ => throw new NotSupportedException($"Provider: {settings.Provider}")
    };
});
```

---

## Adding Custom Middleware

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
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 LLM request started: {MessageCount} messages", chatMessages.Count);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var response = await base.CompleteAsync(chatMessages, options, cancellationToken);
            _logger.LogInformation("✅ LLM response received: {Duration}ms", sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LLM error: {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

**Add to Builder**:

```csharp
var chatClient = new ChatClientBuilder()
    .Use(baseClient)
    .Use(new CustomLoggingMiddleware(/* ... */))  // Custom middleware
    .UseOpenTelemetry()
    .Build();
```

---

## Plugin Architecture

### Interface Definition

```csharp
public interface IAgentPlugin
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(string input);
}
```

### Plugin Implementation

```csharp
public class SentimentAnalysisPlugin : IAgentPlugin
{
    public string Name => "Sentiment Analysis";
    public string Description => "Analyzes the sentiment of messages";

    public async Task<string> ExecuteAsync(string input)
    {
        // Sentiment analysis using Azure Text Analytics API
        var sentiment = await AnalyzeSentimentAsync(input);
        return $"Sentiment score: {sentiment.Score}, Type: {sentiment.Type}";
    }
}
```

### Plugin Manager

```csharp
public class PluginManager
{
    private readonly List<IAgentPlugin> _plugins = new();

    public void RegisterPlugin(IAgentPlugin plugin)
    {
        _plugins.Add(plugin);
    }

    public async Task<string> ExecutePluginAsync(string pluginName, string input)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Name == pluginName);
        if (plugin == null)
            throw new InvalidOperationException($"Plugin {pluginName} not found");

        return await plugin.ExecuteAsync(input);
    }
}
```

---

## Adaptive Card Customization

### Custom Card Template

```csharp
public static AdaptiveCard CreateSalesSummaryCard(SalesSummaryResponse response)
{
    return new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
    {
        Body = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = "📊 Sales Summary",
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder
            },
            new AdaptiveTextBlock
            {
                Text = response.Response,
                Wrap = true
            },
            new AdaptiveFactSet
            {
                Facts = new List<AdaptiveFact>
                {
                    new AdaptiveFact("Processing Time", $"{response.ProcessingTimeMs}ms"),
                    new AdaptiveFact("Data Sources", string.Join(", ", response.DataSources)),
                    new AdaptiveFact("LLM Provider", response.LLMProvider)
                }
            }
        },
        Actions = new List<AdaptiveAction>
        {
            new AdaptiveOpenUrlAction
            {
                Title = "View Details",
                Url = new Uri("https://example.com/details")
            }
        }
    };
}
```

---

## Feature Toggle via Configuration

### Feature Flags

```csharp
public class FeatureSettings
{
    public bool EnableSharePointSearch { get; set; } = true;
    public bool EnableTeamsMessages { get; set; } = false;
    public bool EnableSentimentAnalysis { get; set; } = false;
}
```

**Conditional Tool Registration**:

```csharp
var tools = new List<AITool>
{
    AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
    AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
};

if (_featureSettings.EnableSharePointSearch)
{
    tools.Add(AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments));
}

if (_featureSettings.EnableTeamsMessages)
{
    tools.Add(AIFunctionFactory.Create(_teamsTool.SearchSalesMessages));
}
```

---

## Next Steps

- **[06-SDK-INTEGRATION-PATTERNS.md](06-SDK-INTEGRATION-PATTERNS.md)**: SDK Integration Patterns
- **[13-CODE-WALKTHROUGHS/](13-CODE-WALKTHROUGHS/)**: Code Walkthroughs
