# Agent開発ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../AGENT-DEVELOPMENT.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/AGENT-DEVELOPMENT.md)

**営業支援エージェントのカスタマイズと拡張** - MCP Tools、LLMプロバイダー、Adaptive Cardsの実装パターン

---

## 📋 概要

このガイドでは、営業支援エージェントのコア機能（MCP Tools、LLM統合、Adaptive Cards、Observability）をカスタマイズ・拡張する方法を説明します。Microsoft Agent 365 SDKのベストプラクティスに準拠した実装パターンを提供します。

### 💡 カバー範囲

| トピック | 内容 |
|---------|------|
| 🔧 **MCP Tools** | M365データアクセスツールの作成・拡張 |
| 🤖 **LLMプロバイダー** | 新しいLLMの統合方法 |
| 🎨 **Adaptive Cards** | 視覚的な応答カードの設計 |
| 📊 **Observability** | トレース・メトリクスの実装 |
| 🔄 **ワークフロー** | エージェント処理フローのカスタマイズ |

---

## 🔧 MCP Tools開発

### MCP（Model Context Protocol）とは

MCPは、LLMがツール（関数）を呼び出してタスクを実行するための標準プロトコルです。営業支援エージェントでは、Microsoft 365データへのアクセスをMCP Toolsとして実装しています。

### 基本構造

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

### 新しいMCP Toolの作成

#### 例: OneDrive検索ツール

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
                Error = "OneDrive検索に失敗しました",
                Message = ex.Message
            });
        }
    }
}
```

### ツール登録

```csharp
// Program.cs
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<SharePointTool>();
builder.Services.AddSingleton<TeamsMessageTool>();
builder.Services.AddSingleton<OneDriveSearchTool>(); // 新規ツール

// エージェントにツールを登録
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

### パラメータ検証

```csharp
public async Task<string> SearchEmailsAsync(
    [Description("Search query")] string query,
    [Description("Start date (yyyy-MM-dd)")] string? startDate = null,
    [Description("End date (yyyy-MM-dd)")] string? endDate = null)
{
    // パラメータ検証
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
    
    // フィルタ構築
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
    
    // Graph API呼び出し
    var messages = await _graphClient.Me.Messages
        .Request()
        .Filter(filterString)
        .OrderBy("receivedDateTime desc")
        .GetAsync();
    
    // ...
}
```

---

## 🤖 LLMプロバイダー統合

### ILLMProvider インターフェース

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

### Azure OpenAI実装

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
    
    public async Task<string> GenerateResponseWithToolsAsync(
        string prompt,
        IEnumerable<IAgentTool> tools,
        CancellationToken cancellationToken = default)
    {
        var chatOptions = new ChatOptions
        {
            Tools = tools.Select(t => AIFunctionFactory.Create(t)).ToList()
        };
        
        var response = await _chatClient.CompleteAsync(
            prompt,
            chatOptions,
            cancellationToken
        );
        
        return response.Message.Text ?? string.Empty;
    }
}
```

### カスタムLLM実装例（Anthropic Claude）

```csharp
using Anthropic.SDK;

public class AnthropicProvider : ILLMProvider
{
    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly ILogger<AnthropicProvider> _logger;
    
    public string ProviderName => "Anthropic";
    
    public AnthropicProvider(
        LLMSettings settings,
        ILogger<AnthropicProvider> logger)
    {
        _logger = logger;
        _client = new AnthropicClient(settings.Anthropic.ApiKey);
        _model = settings.Anthropic.ModelName ?? "claude-3-5-sonnet-20241022";
    }
    
    public async Task<string> GenerateResponseAsync(
        string prompt,
        List<Message> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = conversationHistory
                .Select(m => new Anthropic.SDK.Messaging.Message
                {
                    Role = m.Role == "user" ? "user" : "assistant",
                    Content = m.Content
                })
                .ToList();
            
            messages.Add(new Anthropic.SDK.Messaging.Message
            {
                Role = "user",
                Content = prompt
            });
            
            var response = await _client.Messages.CreateAsync(
                new MessageRequest
                {
                    Model = _model,
                    Messages = messages,
                    MaxTokens = 4096
                },
                cancellationToken
            );
            
            return response.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic generation failed");
            throw;
        }
    }
    
    public async Task<string> GenerateResponseWithToolsAsync(
        string prompt,
        IEnumerable<IAgentTool> tools,
        CancellationToken cancellationToken = default)
    {
        // Anthropic Tool Useの実装
        var anthropicTools = tools.Select(t => new Tool
        {
            Name = t.GetType().Name,
            Description = t.GetType().GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
            InputSchema = GenerateInputSchema(t)
        }).ToList();
        
        var response = await _client.Messages.CreateAsync(
            new MessageRequest
            {
                Model = _model,
                Messages = new[]
                {
                    new Anthropic.SDK.Messaging.Message
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                Tools = anthropicTools,
                MaxTokens = 4096
            },
            cancellationToken
        );
        
        // Tool呼び出し処理
        // ...
        
        return response.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
    
    private object GenerateInputSchema(IAgentTool tool)
    {
        // ツールのメソッドシグネチャからJSON Schemaを生成
        // ...
    }
}
```

### LLMプロバイダー切り替え

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

## 🎨 Adaptive Cards設計

### 基本構造

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
                // ヘッダー
                new AdaptiveContainer
                {
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                            {
                                new AdaptiveColumn
                                {
                                    Width = "auto",
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveImage
                                        {
                                            Url = new Uri("https://example.com/bot-icon.png"),
                                            Size = AdaptiveImageSize.Small,
                                            Style = AdaptiveImageStyle.Person
                                        }
                                    }
                                },
                                new AdaptiveColumn
                                {
                                    Width = "stretch",
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveTextBlock
                                        {
                                            Text = "📊 商談サマリレポート",
                                            Weight = AdaptiveTextWeight.Bolder,
                                            Size = AdaptiveTextSize.Large
                                        },
                                        new AdaptiveTextBlock
                                        {
                                            Text = $"生成日時: {DateTime.Now:yyyy/MM/dd HH:mm}",
                                            Spacing = AdaptiveSpacing.None,
                                            IsSubtle = true
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                
                // サマリテキスト
                new AdaptiveContainer
                {
                    Separator = true,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = "📝 サマリ",
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
                
                // メールセクション
                new AdaptiveContainer
                {
                    Separator = true,
                    Items = CreateEmailSection(emails)
                },
                
                // イベントセクション
                new AdaptiveContainer
                {
                    Separator = true,
                    Items = CreateEventSection(events)
                },
                
                // フッター
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
                    Title = "詳細をOutlookで確認",
                    Url = new Uri("https://outlook.office.com")
                },
                new AdaptiveSubmitAction
                {
                    Title = "フィードバック送信",
                    Data = new { action = "feedback" }
                }
            }
        };
        
        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };
    }
    
    private static List<AdaptiveElement> CreateEmailSection(List<EmailSummary> emails)
    {
        var items = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = $"📧 メール ({emails.Count}件)",
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium
            }
        };
        
        items.AddRange(emails.Take(5).Select(email => new AdaptiveFactSet
        {
            Facts = new List<AdaptiveFact>
            {
                new AdaptiveFact("件名", email.Subject),
                new AdaptiveFact("差出人", email.From),
                new AdaptiveFact("日時", email.ReceivedDateTime.ToString("yyyy/MM/dd HH:mm"))
            }
        }));
        
        if (emails.Count > 5)
        {
            items.Add(new AdaptiveTextBlock
            {
                Text = $"...他 {emails.Count - 5} 件",
                IsSubtle = true
            });
        }
        
        return items;
    }
    
    private static List<AdaptiveElement> CreateEventSection(List<EventSummary> events)
    {
        var items = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = $"📅 予定 ({events.Count}件)",
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium
            }
        };
        
        items.AddRange(events.Take(5).Select(evt => new AdaptiveFactSet
        {
            Facts = new List<AdaptiveFact>
            {
                new AdaptiveFact("件名", evt.Subject),
                new AdaptiveFact("開始", evt.StartTime.ToString("yyyy/MM/dd HH:mm")),
                new AdaptiveFact("終了", evt.EndTime.ToString("yyyy/MM/dd HH:mm"))
            }
        }));
        
        return items;
    }
}
```

### インタラクティブカード

```csharp
public static Attachment CreateInteractiveCard()
{
    var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
    {
        Body = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = "検索オプションを選択してください",
                Weight = AdaptiveTextWeight.Bolder
            },
            new AdaptiveChoiceSetInput
            {
                Id = "searchType",
                Label = "検索対象",
                IsMultiSelect = true,
                Choices = new List<AdaptiveChoice>
                {
                    new AdaptiveChoice { Title = "メール", Value = "email" },
                    new AdaptiveChoice { Title = "カレンダー", Value = "calendar" },
                    new AdaptiveChoice { Title = "SharePoint", Value = "sharepoint" },
                    new AdaptiveChoice { Title = "Teams", Value = "teams" }
                },
                Value = "email,calendar" // デフォルト選択
            },
            new AdaptiveTextInput
            {
                Id = "keyword",
                Label = "検索キーワード",
                Placeholder = "例: 株式会社サンプル"
            },
            new AdaptiveDateInput
            {
                Id = "startDate",
                Label = "開始日"
            },
            new AdaptiveDateInput
            {
                Id = "endDate",
                Label = "終了日"
            }
        },
        Actions = new List<AdaptiveAction>
        {
            new AdaptiveSubmitAction
            {
                Title = "🔍 検索実行",
                Data = new { action = "search" }
            },
            new AdaptiveSubmitAction
            {
                Title = "❌ キャンセル",
                Data = new { action = "cancel" }
            }
        }
    };
    
    return new Attachment
    {
        ContentType = AdaptiveCard.ContentType,
        Content = card
    };
}
```

### ユーザー入力の処理

```csharp
// TeamsBot.cs
protected override async Task OnTeamsMessagingExtensionSubmitActionAsync(
    ITurnContext<IInvokeActivity> turnContext,
    MessagingExtensionAction action,
    CancellationToken cancellationToken)
{
    var data = JObject.FromObject(action.Data);
    var actionType = data["action"]?.ToString();
    
    if (actionType == "search")
    {
        var searchType = data["searchType"]?.ToString()?.Split(',') ?? Array.Empty<string>();
        var keyword = data["keyword"]?.ToString();
        var startDate = data["startDate"]?.ToString();
        var endDate = data["endDate"]?.ToString();
        
        // エージェント呼び出し
        var result = await _salesAgent.ProcessCustomSearchAsync(
            searchType,
            keyword,
            startDate,
            endDate
        );
        
        var card = AdaptiveCardHelper.CreateSearchResultCard(result);
        
        await turnContext.SendActivityAsync(
            MessageFactory.Attachment(card),
            cancellationToken
        );
    }
}
```

---

## 📊 Observability実装

### OpenTelemetry統合

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
    private readonly Counter<long> _toolCallCounter;
    private readonly Counter<long> _errorCounter;
    
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
        
        _toolCallCounter = Meter.CreateCounter<long>(
            "agent.tool.calls.total",
            description: "Total number of tool calls"
        );
        
        _errorCounter = Meter.CreateCounter<long>(
            "agent.errors.total",
            description: "Total number of errors"
        );
    }
    
    public Activity? StartConversationActivity(string query)
    {
        var activity = ActivitySource.StartActivity(
            "ProcessConversation",
            ActivityKind.Internal
        );
        
        activity?.SetTag("query.length", query.Length);
        activity?.SetTag("timestamp", DateTime.UtcNow);
        
        return activity;
    }
    
    public void RecordConversation(string result, double durationSeconds)
    {
        _conversationCounter.Add(1,
            new KeyValuePair<string, object?>("result", result));
        
        _responseTimeHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("result", result));
    }
    
    public void RecordToolCall(string toolName, bool success, double durationSeconds)
    {
        _toolCallCounter.Add(1,
            new KeyValuePair<string, object?>("tool", toolName),
            new KeyValuePair<string, object?>("success", success));
        
        _responseTimeHistogram.Record(durationSeconds,
            new KeyValuePair<string, object?>("phase", $"tool_{toolName}"));
    }
    
    public void RecordError(string errorType, string? phase = null)
    {
        _errorCounter.Add(1,
            new KeyValuePair<string, object?>("type", errorType),
            new KeyValuePair<string, object?>("phase", phase ?? "unknown"));
    }
}
```

### エージェントでの使用

```csharp
public class SalesAgent
{
    private readonly IChatClient _chatClient;
    private readonly IEnumerable<IAgentTool> _tools;
    private readonly AgentMetrics _metrics;
    private readonly ILogger<SalesAgent> _logger;
    
    public async Task<string> ProcessQueryAsync(string query)
    {
        var stopwatch = Stopwatch.StartNew();
        
        using var activity = _metrics.StartConversationActivity(query);
        activity?.SetTag("query", query);
        
        try
        {
            // ツール呼び出し
            var results = new List<string>();
            
            foreach (var tool in _tools)
            {
                var toolStopwatch = Stopwatch.StartNew();
                
                try
                {
                    var result = await tool.ExecuteAsync(query);
                    results.Add(result);
                    
                    toolStopwatch.Stop();
                    _metrics.RecordToolCall(
                        tool.GetType().Name,
                        success: true,
                        toolStopwatch.Elapsed.TotalSeconds
                    );
                    
                    activity?.AddEvent(new ActivityEvent(
                        $"Tool {tool.GetType().Name} completed",
                        tags: new ActivityTagsCollection
                        {
                            { "duration_ms", toolStopwatch.ElapsedMilliseconds },
                            { "result_length", result.Length }
                        }
                    ));
                }
                catch (Exception ex)
                {
                    _metrics.RecordToolCall(
                        tool.GetType().Name,
                        success: false,
                        toolStopwatch.Elapsed.TotalSeconds
                    );
                    
                    _metrics.RecordError(
                        ex.GetType().Name,
                        phase: tool.GetType().Name
                    );
                    
                    _logger.LogError(ex, "Tool {Tool} failed", tool.GetType().Name);
                }
            }
            
            // LLM推論
            var llmStopwatch = Stopwatch.StartNew();
            var response = await _chatClient.CompleteAsync(
                BuildPrompt(query, results)
            );
            llmStopwatch.Stop();
            
            _metrics.RecordToolCall(
                "LLM",
                success: true,
                llmStopwatch.Elapsed.TotalSeconds
            );
            
            activity?.SetTag("llm.duration_ms", llmStopwatch.ElapsedMilliseconds);
            activity?.SetTag("llm.tokens", response.Usage?.TotalTokenCount);
            
            stopwatch.Stop();
            _metrics.RecordConversation(
                "success",
                stopwatch.Elapsed.TotalSeconds
            );
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            return response.Message.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordConversation(
                "error",
                stopwatch.Elapsed.TotalSeconds
            );
            
            _metrics.RecordError(ex.GetType().Name);
            
            activity?.SetStatus(
                ActivityStatusCode.Error,
                ex.Message
            );
            
            _logger.LogError(ex, "Conversation processing failed");
            throw;
        }
    }
}
```

---

## 🔄 ワークフローカスタマイズ

### カスタム処理フロー

```csdarp
public class CustomSalesAgent
{
    public async Task<string> ProcessComplexQueryAsync(string query)
    {
        // ステップ1: クエリ分類
        var queryType = await ClassifyQueryAsync(query);
        
        // ステップ2: クエリタイプに応じた処理
        return queryType switch
        {
            "sales_summary" => await ProcessSalesSummaryAsync(query),
            "customer_info" => await ProcessCustomerInfoAsync(query),
            "forecast" => await ProcessForecastAsync(query),
            _ => await ProcessGeneralQueryAsync(query)
        };
    }
    
    private async Task<string> ClassifyQueryAsync(string query)
    {
        var classificationPrompt = $@"
以下のクエリを分類してください。
分類: sales_summary, customer_info, forecast, general

クエリ: {query}

分類結果（単語のみ）:";
        
        var response = await _chatClient.CompleteAsync(
            classificationPrompt
        );
        
        return response.Message.Text?.Trim().ToLower() ?? "general";
    }
    
    private async Task<string> ProcessSalesSummaryAsync(string query)
    {
        // 商談サマリ特化処理
        var emails = await _outlookTool.SearchEmailsAsync(query);
        var events = await _calendarTool.SearchEventsAsync(query);
        var docs = await _sharePointTool.SearchDocumentsAsync(query);
        
        var summaryPrompt = $@"
以下の情報を基に、商談サマリレポートを作成してください。

メール:
{emails}

カレンダー:
{events}

ドキュメント:
{docs}

レポート:";
        
        var response = await _chatClient.CompleteAsync(summaryPrompt);
        return response.Message.Text ?? string.Empty;
    }
}
```

### 並列処理最適化

```csharp
public async Task<string> ProcessQueryParallelAsync(string query)
{
    // 並列でツール実行
    var tasks = new[]
    {
        _outlookTool.SearchEmailsAsync(query),
        _calendarTool.SearchEventsAsync(query),
        _sharePointTool.SearchDocumentsAsync(query),
        _teamsTool.SearchMessagesAsync(query)
    };
    
    var results = await Task.WhenAll(tasks);
    
    var emails = results[0];
    var events = results[1];
    var docs = results[2];
    var messages = results[3];
    
    // 統合レポート生成
    var response = await _chatClient.CompleteAsync(
        BuildIntegratedPrompt(query, emails, events, docs, messages)
    );
    
    return response.Message.Text ?? string.Empty;
}
```

---

## 📚 ベストプラクティス

### 1. エラーハンドリング

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed on attempt {Attempt}", attempt);
            if (attempt == maxRetries) throw;
            
            await Task.Delay(retryDelay);
        }
    }
    
    throw new InvalidOperationException("All retry attempts failed");
}
```

### 2. キャッシング

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

### 3. ロギング

```csharp
public async Task<string> WellLoggedToolCallAsync(string query)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["Query"] = query,
        ["Tool"] = GetType().Name,
        ["Timestamp"] = DateTime.UtcNow
    });
    
    _logger.LogInformation("Starting tool execution");
    
    try
    {
        var result = await ExecuteAsync(query);
        
        _logger.LogInformation(
            "Tool execution succeeded. Result length: {Length}",
            result.Length
        );
        
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Tool execution failed for query: {Query}",
            query
        );
        throw;
    }
}
```

---

## 🔗 関連リソース

- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - リアルタイム監視
- [アーキテクチャ](ARCHITECTURE.md) - システム設計詳細
- [認証設定](AUTHENTICATION.md) - Graph API認証
- [Microsoft Agent Framework](https://github.com/microsoft/Agent365-Samples) - 公式SDK

---

**営業支援エージェントをカスタマイズして、独自のビジネスロジックを実装しましょう！** 🚀
