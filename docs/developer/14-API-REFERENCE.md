# API Reference - 主要クラスとインターフェース

## 📋 コア インターフェース

### ILLMProvider

**場所**: `Services/LLM/ILLMProvider.cs`

```csharp
public interface ILLMProvider
{
    string ProviderName { get; }
    IChatClient GetChatClient();
}
```

**実装クラス**:
- `AzureOpenAIProvider`
- `GitHubModelsProvider`
- `OllamaProvider`

**使用例**:
```csharp
var provider = serviceProvider.GetRequiredService<ILLMProvider>();
var chatClient = provider.GetChatClient();
```

---

## Agent クラス

### SalesAgent

**場所**: `Services/Agent/SalesAgent.cs`

#### コンストラクタ

```csharp
public SalesAgent(
    ILLMProvider llmProvider,
    OutlookEmailTool emailTool,
    OutlookCalendarTool calendarTool,
SharePointTool sharePointTool,
    TeamsMessageTool teamsTool,
    ObservabilityService observabilityService,
    NotificationService notificationService,
    ILogger<SalesAgent> logger)
```

#### 主要メソッド

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(
    SalesSummaryRequest request)
```

**パラメータ**:
- `request.Query`: ユーザークエリ
- `request.StartDate`: 検索開始日（省略可）
- `request.EndDate`: 検索終了日（省略可）

**戻り値**:
```csharp
public class SalesSummaryResponse
{
    public string Response { get; set; }
    public List<string> DataSources { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string LLMProvider { get; set; }
}
```

---

## MCP Tools

### OutlookEmailTool

**場所**: `Services/MCP/McpTools/OutlookEmailTool.cs`

#### SearchSalesEmails

```csharp
[Description("商談関連のメールを検索して取得します")]
public async Task<string> SearchSalesEmails(
    [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
    [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
    [Description("検索キーワード")] string keywords = "商談,提案,見積,契約")
```

### OutlookCalendarTool

**場所**: `Services/MCP/McpTools/OutlookCalendarTool.cs`

#### SearchSalesMeetings

```csharp
[Description("商談関連の予定を検索します")]
public async Task<string> SearchSalesMeetings(
    [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
    [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
    [Description("検索キーワード")] string keywords = "商談,提案,ミーティング")
```

### SharePointTool

**場所**: `Services/MCP/McpTools/SharePointTool.cs`

#### SearchSalesDocuments

```csharp
[Description("SharePointから営業資料を検索します")]
public async Task<string> SearchSalesDocuments(
    [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
    [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
    [Description("検索キーワード")] string keywords = "提案書,見積書,契約書")
```

---

## 観測性 サービス

### ObservabilityService

**場所**: `Services/Observability/ObservabilityService.cs`

#### トレース記録

```csharp
public async Task RecordTraceAsync(string message, string level, long timestamp)
```

**パラメータ**:
- `message`: トレースメッセージ
- `level`: `"info"` | `"success"` | `"error"` | `"warning"`
- `timestamp`: 経過時間（ミリ秒）

#### 詳細トレースセッション

```csharp
public string StartDetailedTrace(string conversationId, string userId, string userQuery)

public async Task AddTracePhaseAsync(
    string sessionId,
    string phaseName,
    string description,
    object? metadata = null,
    string status = "Completed")

public async Task CompleteDetailedTraceAsync(
    string sessionId,
    string finalResponse,
    bool success)
```

#### メトリクス

```csharp
public async Task RecordRequestAsync(bool success, long latencyMs)

public async Task UpdateMetricsAsync()

public MetricsSummary GetMetricsSummary()
```

**MetricsSummary**:
```csharp
public class MetricsSummary
{
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatencyMs { get; set; }
    public long TotalProcessingTimeMs { get; set; }
}
```

---

## 通知 サービス

### NotificationService

**場所**: `Services/Notifications/NotificationService.cs`

```csharp
public async Task SendProgressNotificationAsync(
    string operationId,
    string message,
    int progress)  // 0-100

public async Task SendSuccessNotificationAsync(
    string operationId,
    string message,
    object? metadata = null)

public async Task SendErrorNotificationAsync(
    string operationId,
    string message,
    string errorDetails)
```

---

## テレメトリ

### AgentMetrics

**場所**: `Telemetry/AgentMetrics.cs`

#### Activity Sourceと Meter

```csharp
public static readonly string SourceName = "SalesSupportAgent";
private static readonly ActivitySource _activitySource = new ActivitySource(SourceName);
private static readonly Meter _meter = new Meter("SalesSupportAgent.Metrics");
```

#### 観測可能な操作

```csharp
public static async Task<T> InvokeObservedHttpOperation<T>(
    string operationName,
    Func<Task<T>> operation)
```

**使用例**:
```csharp
return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
{
    var response = await salesAgent.GenerateSalesSummaryAsync(request);
    return Results.Ok(response);
});
```

---

## 設定クラス

### M365Settings

**場所**: `Configuration/M365Settings.cs`

```csharp
public class M365Settings
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string UserId { get; set; }
    public bool UseManagedIdentity { get; set; }
    public string[] Scopes { get; set; }
    
    public bool IsConfigured => /* 検証ロジック */;
}
```

### LLMSettings

**場所**: `Configuration/LLMSettings.cs`

```csharp
public class LLMSettings
{
    public string Provider { get; set; }
    public AzureOpenAISettings AzureOpenAI { get; set; }
    public OllamaSettings Ollama { get; set; }
    public GitHubModelsSettings GitHubModels { get; set; }
}
```

### BotSettings

**場所**: `Configuration/BotSettings.cs`

```csharp
public class BotSettings
{
    public string MicrosoftAppType { get; set; }
    public string MicrosoftAppId { get; set; }
    public string MicrosoftAppPassword { get; set; }
    public string MicrosoftAppTenantId { get; set; }
    
    public bool IsConfigured => /* 検証ロジック */;
}
```

---

## Bot クラス

### TeamsBot

**場所**: `Bot/TeamsBot.cs`

```csharp
public class TeamsBot : ActivityHandler
{
    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
        
    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
}
```

---

## データモデル

### SalesSummaryRequest

```csharp
public class SalesSummaryRequest
{
    public string Query { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
```

### SalesSummaryResponse

```csharp
public class SalesSummaryResponse
{
    public string Response { get; set; } = string.Empty;
    public List<string> DataSources { get; set; } = new();
    public long ProcessingTimeMs { get; set; }
    public string LLMProvider { get; set; } = string.Empty;
}
```

---

## 次のステップ

- **[13-CODE-WALKTHROUGHS/](13-CODE-WALKTHROUGHS/)**: コードウォークスルー
- **[02-PROJECT-STRUCTURE.md](02-PROJECT-STRUCTURE.md)**: プロジェクト構造
