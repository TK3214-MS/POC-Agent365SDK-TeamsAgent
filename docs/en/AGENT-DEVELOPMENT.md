# Agent Development Guide

> **Language**: [🇯🇵 日本語](../AGENT-DEVELOPMENT.md) | 🇬🇧 English

**Customizing and Extending the Sales Support Agent** - Implementation patterns for MCP Tools, LLM Providers, and Adaptive Cards

---

## 📋 Overview

This guide explains how to customize and extend the core features of the Sales Support Agent (MCP Tools, LLM integration, Adaptive Cards, Observability). It provides implementation patterns that comply with Microsoft Agent 365 SDK best practices.

### 💡 Coverage

| Topic | Content |
|-------|---------|
| 🔧 **MCP Tools** | Creating and extending M365 data access tools |
| 🤖 **LLM Providers** | How to integrate new LLMs |
| 🎨 **Adaptive Cards** | Designing visual response cards |
| 📊 **Observability** | Implementing traces and metrics |
| 🔄 **Workflows** | Customizing agent processing flows |

---

## 🔧 MCP Tools Development

### What is MCP (Model Context Protocol)?

MCP is a standard protocol for LLMs to invoke tools (functions) to execute tasks. In the Sales Support Agent, access to Microsoft 365 data is implemented as MCP Tools.

### Basic Structure

```csharp
using Microsoft.AI.Agents.Abstractions;
using Microsoft.Graph;

public class OutlookEmailTool : IAgentTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<OutlookEmailTool> _logger;
    
    public OutlookEmailTool(
        GraphServiceClient graphClient,
        ILogger<OutlookEmailTool> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }
    
    [Description("Searches emails from Outlook")]
    public async Task<string> SearchEmailsAsync(
        [Description("Search query (e.g., customer name)")] string query,
        [Description("Maximum results to return")] int maxResults = 10)
    {
        try
        {
            var messages = await _graphClient.Me.Messages
                .Request()
                .Filter($"contains(subject, '{query}') or contains(body/content, '{query}')")
                .Top(maxResults)
                .OrderBy("receivedDateTime desc")
                .GetAsync();
            
            var results = messages.Select(m => new
            {
                Subject = m.Subject,
                From = m.From?.EmailAddress?.Address,
                ReceivedDateTime = m.ReceivedDateTime,
                BodyPreview = m.BodyPreview
            }).ToList();
            
            return JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search emails for query: {Query}", query);
            throw;
        }
    }
}
```

### Creating a New MCP Tool

#### Example: OneDrive Search Tool

```csharp
using Microsoft.AI.Agents.Abstractions;
using Microsoft.Graph;
using System.Text.Json;

public class OneDriveSearchTool : IAgentTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<OneDriveSearchTool> _logger;
    
    public OneDriveSearchTool(
        GraphServiceClient graphClient,
        ILogger<OneDriveSearchTool> logger)
    {
        _graphClient = graphClient;
        _logger = logger;
    }
    
    [Description("Searches files in OneDrive")]
    public async Task<string> SearchOneDriveAsync(
        [Description("File name or content to search")] string query,
        [Description("File type filter (e.g., 'pdf', 'xlsx')")] string? fileType = null,
        [Description("Maximum results to return")] int maxResults = 10)
    {
        try
        {
            var searchQuery = string.IsNullOrEmpty(fileType)
                ? query
                : $"{query} AND fileExtension:{fileType}";
            
            var driveItems = await _graphClient.Me.Drive.Root
                .Search(searchQuery)
                .Request()
                .Top(maxResults)
                .GetAsync();
            
            var results = driveItems.Select(item => new
            {
                Name = item.Name,
                WebUrl = item.WebUrl,
                Size = item.Size,
                CreatedDateTime = item.CreatedDateTime,
                LastModifiedDateTime = item.LastModifiedDateTime,
                FileType = item.File?.MimeType
            }).ToList();
            
            _logger.LogInformation(
                "OneDrive search completed: {Query}, Found: {Count} files",
                query, results.Count
            );
            
            return JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search OneDrive for query: {Query}", query);
            return JsonSerializer.Serialize(new
            {
                Error = "OneDrive search failed",
                Message = ex.Message
            });
        }
    }
}
```

### Tool Registration

```csharp
// Program.cs
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<SharePointTool>();
builder.Services.AddSingleton<TeamsMessageTool>();
builder.Services.AddSingleton<OneDriveSearchTool>(); // New tool

// Register tools with agent
builder.Services.AddSingleton<SalesAgent>(sp =>
{
    var tools = new IAgentTool[]
    {
        sp.GetRequiredService<OutlookEmailTool>(),
        sp.GetRequiredService<OutlookCalendarTool>(),
        sp.GetRequiredService<SharePointTool>(),
        sp.GetRequiredService<TeamsMessageTool>(),
        sp.GetRequiredService<OneDriveSearchTool>()
    };
    
    return new SalesAgent(
        sp.GetRequiredService<IChatClient>(),
        tools,
        sp.GetRequiredService<ILogger<SalesAgent>>()
    );
});
```

### Parameter Validation

```csharp
public async Task<string> SearchEmailsAsync(
    [Description("Search query")] string query,
    [Description("Start date (yyyy-MM-dd)")] string? startDate = null,
    [Description("End date (yyyy-MM-dd)")] string? endDate = null)
{
    // Parameter validation
    if (string.IsNullOrWhiteSpace(query))
    {
        throw new ArgumentException("Query cannot be empty", nameof(query));
    }
    
    DateTime? start = null;
    DateTime? end = null;
    
    if (!string.IsNullOrEmpty(startDate))
    {
        if (!DateTime.TryParse(startDate, out var parsedStart))
        {
            throw new ArgumentException(
                $"Invalid start date format: {startDate}. Expected: yyyy-MM-dd",
                nameof(startDate)
            );
        }
        start = parsedStart;
    }
    
    if (!string.IsNullOrEmpty(endDate))
    {
        if (!DateTime.TryParse(endDate, out var parsedEnd))
        {
            throw new ArgumentException(
                $"Invalid end date format: {endDate}. Expected: yyyy-MM-dd",
                nameof(endDate)
            );
        }
        end = parsedEnd;
    }
    
    // Build filters
    var filters = new List<string>
    {
        $"contains(subject, '{query}') or contains(body/content, '{query}')"
    };
    
    if (start.HasValue)
    {
        filters.Add($"receivedDateTime ge {start.Value:yyyy-MM-ddTHH:mm:ssZ}");
    }
    
    if (end.HasValue)
    {
        filters.Add($"receivedDateTime le {end.Value:yyyy-MM-ddTHH:mm:ssZ}");
    }
    
    var filterString = string.Join(" and ", filters);
    
    // Graph API call
    var messages = await _graphClient.Me.Messages
        .Request()
        .Filter(filterString)
        .OrderBy("receivedDateTime desc")
        .GetAsync();
    
    // ...
}
```

---

## 🤖 LLM Provider Integration

### ILLMProvider Interface

```csharp
public interface ILLMProvider
{
    string ProviderName { get; }
    Task<string> GenerateResponseAsync(
        string prompt,
        List<Message> conversationHistory,
        CancellationToken cancellationToken = default
    );
    Task<string> GenerateResponseWithToolsAsync(
        string prompt,
        IEnumerable<IAgentTool> tools,
        CancellationToken cancellationToken = default
    );
}

public class Message
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
}
```

### Azure OpenAI Implementation

```csharp
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

public class AzureOpenAIProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<AzureOpenAIProvider> _logger;
    
    public string ProviderName => "AzureOpenAI";
    
    public AzureOpenAIProvider(
        LLMSettings settings,
        ILogger<AzureOpenAIProvider> logger)
    {
        _logger = logger;
        
        var openAIClient = new AzureOpenAIClient(
            new Uri(settings.AzureOpenAI.Endpoint),
            new AzureKeyCredential(settings.AzureOpenAI.ApiKey)
        );
        
        _chatClient = openAIClient
            .AsChatClient(settings.AzureOpenAI.DeploymentName)
            .AsBuilder()
            .UseFunctionInvocation() // Agent365 SDK
            .UseOpenTelemetry()      // Agent365 SDK
            .Build();
    }
    
    public async Task<string> GenerateResponseAsync(
        string prompt,
        List<Message> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = conversationHistory
                .Select(m => new ChatMessage(
                    m.Role == "user" ? ChatRole.User : ChatRole.Assistant,
                    m.Content
                ))
                .ToList();
            
            messages.Add(new ChatMessage(ChatRole.User, prompt));
            
            var response = await _chatClient.CompleteAsync(
                messages,
                cancellationToken: cancellationToken
            );
            
            return response.Message.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI generation failed");
            throw;
        }
    }
}
```

### LLM Provider Switching

```csharp
// Program.cs
builder.Services.AddSingleton<IChatClient>(sp =>
{
    var settings = sp.GetRequiredService<LLMSettings>();
    var logger = sp.GetRequiredService<ILogger<IChatClient>>();
    
    return settings.Provider switch
    {
        "AzureOpenAI" => new AzureOpenAIProvider(settings, logger),
        "Ollama" => new OllamaProvider(settings, logger),
        "Anthropic" => new AnthropicProvider(settings, logger),
        "GitHubModels" => new GitHubModelsProvider(settings, logger),
        _ => throw new InvalidOperationException(
            $"Unknown LLM provider: {settings.Provider}"
        )
    };
});
```

---

## 🎨 Adaptive Cards Design

### Basic Structure

```csharp
using AdaptiveCards;
using AdaptiveCards.Templating;

public class AdaptiveCardHelper
{
    public static Attachment CreateSalesSummaryCard(
        string summaryText,
        List<EmailSummary> emails,
        List<EventSummary> events,
        string llmProvider)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                // Header
                new AdaptiveContainer
                {
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = "📊 Sales Summary Report",
                            Weight = AdaptiveTextWeight.Bolder,
                            Size = AdaptiveTextSize.Large
                        },
                        new AdaptiveTextBlock
                        {
                            Text = $"Generated: {DateTime.Now:yyyy/MM/dd HH:mm}",
                            Spacing = AdaptiveSpacing.None,
                            IsSubtle = true
                        }
                    }
                },
                
                // Summary text
                new AdaptiveContainer
                {
                    Separator = true,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = "📝 Summary",
                            Weight = AdaptiveTextWeight.Bolder,
                            Size = AdaptiveTextSize.Medium
                        },
                        new AdaptiveTextBlock
                        {
                            Text = summaryText,
                            Wrap = true
                        }
                    }
                },
                
                // Footer
                new AdaptiveContainer
                {
                    Separator = true,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = $"🤖 LLM: {llmProvider}",
                            Size = AdaptiveTextSize.Small,
                            IsSubtle = true,
                            HorizontalAlignment = AdaptiveHorizontalAlignment.Right
                        }
                    }
                }
            },
            Actions = new List<AdaptiveAction>
            {
                new AdaptiveOpenUrlAction
                {
                    Title = "View Details in Outlook",
                    Url = new Uri("https://outlook.office.com")
                }
            }
        };
        
        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }
}
```

---

## 📊 Observability Implementation

### OpenTelemetry Integration

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class AgentMetrics
{
    private static readonly ActivitySource ActivitySource = 
        new("SalesSupportAgent", "1.0.0");
    
    private static readonly Meter Meter = 
        new("SalesSupportAgent", "1.0.0");
    
    private readonly Counter<long> _conversationCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    
    public AgentMetrics()
    {
        _conversationCounter = Meter.CreateCounter<long>(
            "agent.conversations.total",
            description: "Total number of conversations"
        );
        
        _responseTimeHistogram = Meter.CreateHistogram<double>(
            "agent.response.time.seconds",
            description: "Agent response time distribution"
        );
    }
    
    public void RecordConversation(string result, double durationSeconds)
    {
        _conversationCounter.Add(1,
            new KeyValuePair<string, object?>("result", result));
        
        _responseTimeHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("result", result));
    }
}
```

---

## 🔄 Workflow Customization

### Custom Processing Flow

```csharp
public class CustomSalesAgent
{
    public async Task<string> ProcessComplexQueryAsync(string query)
    {
        // Step 1: Query classification
        var queryType = await ClassifyQueryAsync(query);
        
        // Step 2: Process based on query type
        return queryType switch
        {
            "sales_summary" => await ProcessSalesSummaryAsync(query),
            "customer_info" => await ProcessCustomerInfoAsync(query),
            "forecast" => await ProcessForecastAsync(query),
            _ => await ProcessGeneralQueryAsync(query)
        };
    }
}
```

### Parallel Processing Optimization

```csharp
public async Task<string> ProcessQueryParallelAsync(string query)
{
    // Execute tools in parallel
    var tasks = new[]
    {
        _outlookTool.SearchEmailsAsync(query),
        _calendarTool.SearchEventsAsync(query),
        _sharePointTool.SearchDocumentsAsync(query),
        _teamsTool.SearchMessagesAsync(query)
    };
    
    var results = await Task.WhenAll(tasks);
    
    // Generate integrated report
    var response = await _chatClient.CompleteAsync(
        BuildIntegratedPrompt(query, results[0], results[1], results[2], results[3])
    );
    
    return response.Message.Text ?? string.Empty;
}
```

---

## 📚 Best Practices

### 1. Error Handling

```csharp
public async Task<string> RobustToolCallAsync(string query)
{
    const int maxRetries = 3;
    var retryDelay = TimeSpan.FromSeconds(1);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await _tool.ExecuteAsync(query);
        }
        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            if (attempt == maxRetries) throw;
            
            _logger.LogWarning(
                "Rate limit exceeded. Retrying in {Delay}s (Attempt {Attempt}/{Max})",
                retryDelay.TotalSeconds, attempt, maxRetries
            );
            
            await Task.Delay(retryDelay);
            retryDelay *= 2; // Exponential backoff
        }
    }
    
    throw new InvalidOperationException("All retry attempts failed");
}
```

### 2. Caching

```csharp
public class CachedGraphService
{
    private readonly IMemoryCache _cache;
    private readonly GraphServiceClient _graphClient;
    
    public async Task<string> GetUserProfileAsync(string userId)
    {
        var cacheKey = $"user_profile_{userId}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedProfile))
        {
            return cachedProfile!;
        }
        
        var user = await _graphClient.Users[userId]
            .Request()
            .GetAsync();
        
        var profile = JsonSerializer.Serialize(user);
        
        _cache.Set(cacheKey, profile, TimeSpan.FromMinutes(15));
        
        return profile;
    }
}
```

---

## 🔗 Related Resources

- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - Real-time monitoring
- [Architecture](ARCHITECTURE.md) - System design details
- [Authentication](AUTHENTICATION.md) - Graph API authentication
- [Microsoft Agent Framework](https://github.com/microsoft/Agent365-Samples) - Official SDK

---

**Customize the Sales Support Agent and implement your own business logic!** 🚀
