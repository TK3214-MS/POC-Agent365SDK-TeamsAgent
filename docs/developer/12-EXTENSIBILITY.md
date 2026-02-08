# Extensibility - 拡張性パターンとカスタマイズ

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](../en/developer/12-EXTENSIBILITY.md)

## 📋 新しいツール追加

### ステップ1: ツールクラス作成

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
    
    [Description("OneDriveから営業資料を検索します")]
    public async Task<string> SearchSalesDocuments(
        [Description("検索キーワード")] string query,
        [Description("最大件数")] int maxResults = 10)
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

            var summary = $"📁 OneDrive検索結果 ({items.Value.Count}件)\n\n";
            foreach (var item in items.Value)
            {
                summary += $"- **{item.Name}**\n";
                summary += $"  URL: {item.WebUrl}\n";
                summary += $"  更新日: {item.LastModifiedDateTime:yyyy/MM/dd}\n\n";
            }
            
            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ OneDrive検索エラー: {ex.Message}";
        }
    }
}
```

### ステップ2: DIコンテナに登録

**Program.cs**:

```csharp
// MCP ツールの登録
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<OneDriveTool>();  // 追加
```

### ステップ3: エージェントに登録

**SalesAgent.cs**:

```csharp
public class SalesAgent
{
    private readonly OneDriveTool _oneDriveTool;  // 追加
    
    public SalesAgent(
        ILLMProvider llmProvider,
        OutlookEmailTool emailTool,
        OutlookCalendarTool calendarTool,
        OneDriveTool oneDriveTool,  // 追加
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
            AIFunctionFactory.Create(_oneDriveTool.SearchSalesDocuments),  // 追加
        };
        
        return chatClient.AsAIAgent(SystemPrompt, "営業支援エージェント", tools: tools);
    }
}
```

---

## 新しいLLMプロバイダー追加

### ステップ1: プロバイダークラス作成

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

### ステップ2: 設定クラス追加

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
    public AnthropicSettings Anthropic { get; set; } = new();  // 追加
}
```

### ステップ3: appsettings.json

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

### ステップ4: Program.csで登録

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var settings = sp.GetRequiredService<LLMSettings>();
    
    return settings.Provider?.ToLower() switch
    {
        "azureopenai" => new AzureOpenAIProvider(settings.AzureOpenAI),
        "ollama" => new OllamaProvider(settings.Ollama),
        "anthropic" => new AnthropicProvider(settings.Anthropic),  // 追加
        _ => throw new NotSupportedException($"Provider: {settings.Provider}")
    };
});
```

---

## カスタムミドルウェア追加

### カスタムロギングミドルウェア

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
        _logger.LogInformation("🚀 LLMリクエスト開始: {MessageCount}件", chatMessages.Count);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var response = await base.CompleteAsync(chatMessages, options, cancellationToken);
            _logger.LogInformation("✅ LLMレスポンス受信: {Duration}ms", sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ LLMエラー: {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

**Builderに追加**:

```csharp
var chatClient = new ChatClientBuilder()
    .Use(baseClient)
    .Use(new CustomLoggingMiddleware(/* ... */))  // カスタムミドルウェア
    .UseOpenTelemetry()
    .Build();
```

---

## プラグインアーキテクチャ

### インターフェース定義

```csharp
public interface IAgentPlugin
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(string input);
}
```

### プラグイン実装

```csharp
public class SentimentAnalysisPlugin : IAgentPlugin
{
    public string Name => "感情分析";
    public string Description => "メッセージの感情を分析します";

    public async Task<string> ExecuteAsync(string input)
    {
        // Azure Text Analytics APIで感情分析
        var sentiment = await AnalyzeSentimentAsync(input);
        return $"感情スコア: {sentiment.Score}, 種別: {sentiment.Type}";
    }
}
```

### プラグインマネージャー

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

## Adaptive Card カスタマイズ

### カスタムカードテンプレート

```csharp
public static AdaptiveCard CreateSalesSummaryCard(SalesSummaryResponse response)
{
    return new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
    {
        Body = new List<AdaptiveElement>
        {
            new AdaptiveTextBlock
            {
                Text = "📊 営業サマリ",
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
                    new AdaptiveFact("処理時間", $"{response.ProcessingTimeMs}ms"),
                    new AdaptiveFact("データソース", string.Join(", ", response.DataSources)),
                    new AdaptiveFact("LLMプロバイダー", response.LLMProvider)
                }
            }
        },
        Actions = new List<AdaptiveAction>
        {
            new AdaptiveOpenUrlAction
            {
                Title = "詳細を見る",
                Url = new Uri("https://example.com/details")
            }
        }
    };
}
```

---

## 設定による機能切り替え

### Feature Flags

```csharp
public class FeatureSettings
{
    public bool EnableSharePointSearch { get; set; } = true;
    public bool EnableTeamsMessages { get; set; } = false;
    public bool EnableSentimentAnalysis { get; set; } = false;
}
```

**条件付きツール登録**:

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

## 次のステップ

- **[06-SDK-INTEGRATION-PATTERNS.md](06-SDK-INTEGRATION-PATTERNS.md)**: SDK統合パターン
- **[13-CODE-WALKTHROUGHS/](13-CODE-WALKTHROUGHS/)**: コードウォークスルー
