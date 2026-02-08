# SDK Integration Patterns - ベストプラクティスとデザインパターン

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](../en/developer/06-SDK-INTEGRATION-PATTERNS.md)

## 📋 目次

- [概要](#概要)
- [Microsoft.Extensions.AI パターン](#microsoftextensionsai-パターン)
- [Agent 365 SDK パターン](#agent-365-sdk-パターン)
- [Microsoft 365 SDK パターン](#microsoft-365-sdk-パターン)
- [依存性注入パターン](#依存性注入パターン)
- [エラーハンドリングパターン](#エラーハンドリングパターン)
- [テレメトリパターン](#テレメトリパターン)

---

## 概要

このドキュメントでは、Sales Support Agentで使用している主要なSDK統合パターンとベストプラクティスを解説します。

---

## Microsoft.Extensions.AI パターン

### Pattern 1: IChatClient Builder Pattern

**目的**: LLMプロバイダーを抽象化し、ミドルウェアチェーンで機能拡張

**実装**:

```csharp
public class GitHubModelsProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;

    public GitHubModelsProvider(GitHubModelsSettings settings)
    {
        // Builder パターンでミドルウェアを構成
        _chatClient = new ChatClientBuilder()
            // ベースクライアント
            .Use(CreateGitHubModelsClient(settings))
            // テレメトリ
            .UseOpenTelemetry(sourceName: "SalesSupportAgent", configure: options =>
            {
                options.EnableSensitiveData = false;
                options.JsonSerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
            })
            // ロギング
            .UseLogging(loggerFactory: LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            }))
            // 関数呼び出し
            .UseFunctionInvocation()
            // ビルド
            .Build();
    }
}
```

**メリット**:
- ✅ プロバイダー切り替え容易（Azure OpenAI ↔ Ollama ↔ GitHub Models）
- ✅ ミドルウェアで横断的関心事を分離（テレメトリ、ロギング、ツール呼び出し）
- ✅ テスト容易性（Mock IChatClient）

### Pattern 2: AIAgent Pattern

**目的**: ツール統合とシステムプロンプトの標準化

```csharp
private AIAgent CreateAgent()
{
    var chatClient = _llmProvider.GetChatClient();

    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments),
        AIFunctionFactory.Create(_teamsTool.SearchSalesMessages)
    };

    return chatClient.AsAIAgent(
        systemPrompt: SystemPrompt,
        name: "営業支援エージェント",
        tools: tools
    );
}
```

**ポイント**:
- `AIFunctionFactory.Create`: メソッドから自動的にツールスキーマ生成
- `AsAIAgent`: IChatClient → AIAgent 変換
- Tools: 複数ツールを統合管理

---

## Agent 365 SDK パターン

### Pattern 3: Observability Pattern

**目的**: 分散トレーシングとメトリクス収集

```csharp
public class AgentMetrics
{
    private static readonly ActivitySource _activitySource = 
        new ActivitySource("SalesSupportAgent");
    private static readonly Meter _meter = 
        new Meter("SalesSupportAgent.Metrics");
    private static readonly Counter<long> _requestCounter = 
        _meter.CreateCounter<long>("agent.requests");
    private static readonly Histogram<double> _latencyHistogram = 
        _meter.CreateHistogram<double>("agent.latency", unit: "ms");

    public static async Task<T> InvokeObservedHttpOperation<T>(
        string operationName,
        Func<Task<T>> operation)
    {
        using var activity = _activitySource.StartActivity(operationName);
        var sw = Stopwatch.StartNew();
        
        try
        {
            var result = await operation();
            _requestCounter.Add(1, new("operation", operationName), new("success", true));
            _latencyHistogram.Record(sw.ElapsedMilliseconds, new("operation", operationName));
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _requestCounter.Add(1, new("operation", operationName), new("success", false));
            throw;
        }
    }
}
```

**使用例**:

```csharp
app.MapPost("/api/sales-summary", async (request, salesAgent) =>
{
    return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
    {
        var response = await salesAgent.GenerateSalesSummaryAsync(request);
        return Results.Ok(response);
    });
});
```

### Pattern 4: Notification Pattern

**目的**: リアルタイム通知とプログレス追跡

```csharp
// 開始通知
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "🚀 商談サマリ生成を開始しています...", 
    progress: 0);

// 進行状況通知
await _notificationService.SendProgressNotificationAsync(
    operationId, 
    "📊 データ収集中...", 
    progress: 25);

// 完了通知
await _notificationService.SendSuccessNotificationAsync(
    operationId, 
    "✅ 完了！",
    metadata: new { ProcessingTimeMs = 3500 });
```

---

## Microsoft 365 SDK パターン

### Pattern 5: Repository Pattern with Graph API

**目的**: Graph API呼び出しをカプセル化

```csharp
public class OutlookEmailTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _userId;

    public async Task<string> SearchSalesEmails(
        string startDate,
        string endDate,
        string keywords)
    {
        try
        {
            var messages = await _graphClient.Users[_userId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = BuildFilter(startDate, endDate);
                    config.QueryParameters.Top = 50;
                    config.QueryParameters.Select = SelectFields();
                    config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                });

            return FormatResults(messages, keywords);
        }
        catch (ServiceException ex)
        {
            return HandleGraphError(ex);
        }
    }

    private string BuildFilter(string start, string end) =>
        $"receivedDateTime ge {start} and receivedDateTime le {end}";

    private string[] SelectFields() =>
        new[] { "subject", "from", "receivedDateTime", "bodyPreview" };

    private string FormatResults(MessageCollectionResponse messages, string keywords)
    {
        // フィルタリングとサマリ生成
    }

    private string HandleGraphError(ServiceException ex)
    {
        return ex.ResponseStatusCode switch
        {
            401 => "❌ 認証エラー",
            403 => "❌ 権限不足",
            429 => "❌ レート制限",
            _ => $"❌ エラー: {ex.Message}"
        };
    }
}
```

### Pattern 6: Batch Request Pattern

**目的**: 複数Graph API呼び出しを1つのHTTPリクエストに集約

```csharp
public async Task<CombinedDataResponse> GetCombinedDataAsync(string userId)
{
    var batchRequestContent = new BatchRequestContentCollection(_graphClient);
    
    // リクエスト1: メール
    var messageRequest = _graphClient.Users[userId].Messages.ToGetRequestInformation();
    var messageStepId = await batchRequestContent.AddBatchRequestStepAsync(messageRequest);
    
    // リクエスト2: カレンダー
    var calendarRequest = _graphClient.Users[userId].Calendar.ToGetRequestInformation();
    var calendarStepId = await batchRequestContent.AddBatchRequestStepAsync(calendarRequest);
    
    // バッチ実行
    var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);
    
    // 結果取得
    var messages = await batchResponse.GetResponseByIdAsync<MessageCollectionResponse>(messageStepId);
    var calendar = await batchResponse.GetResponseByIdAsync<Calendar>(calendarStepId);
    
    return new CombinedDataResponse(messages, calendar);
}
```

**パフォーマンス改善**:
```
シーケンシャル:
  GET /messages (500ms)
  GET /calendar (400ms)
  総時間: 900ms

バッチ:
  POST /batch { requests: [messages, calendar] } (600ms)
  総時間: 600ms（33%高速化）
```

---

## 依存性注入パターン

### Pattern 7: Service Registration Pattern

**Program.cs**:

```csharp
// シングルトンサービス（状態を持つ、アプリケーション全体で共有）
builder.Services.AddSingleton<TokenCredential>(/* 実装 */);
builder.Services.AddSingleton<GraphServiceClient>(/* 実装 */);
builder.Services.AddSingleton<ILLMProvider>(/* 実装 */);
builder.Services.AddSingleton<ObservabilityService>();

// シングルトン MCP Tools
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();

// Transient サービス（毎回新しいインスタンス）
builder.Services.AddTransient<IBot, TeamsBot>();
```

**ライフタイム選択基準**:

| ライフタイム | 使用ケース | 例 |
|-------------|-----------|-----|
| **Singleton** | 状態共有、高コスト初期化 | GraphServiceClient, ObservabilityService |
| **Scoped** | HTTPリクエストスコープ | データベースコンテキスト |
| **Transient** | 軽量、状態なし | Bot（会話ごとに新規） |

### Pattern 8: Options Pattern

```csharp
// appsettings.json
{
  "M365": {
    "TenantId": "...",
    "ClientId": "...",
    "UseManagedIdentity": false
  }
}

// 設定クラス
public class M365Settings
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; }
}

// 登録
builder.Services.Configure<M365Settings>(
    builder.Configuration.GetSection("M365"));

// 注入
public class OutlookEmailTool
{
    public OutlookEmailTool(IOptions<M365Settings> options)
    {
        var settings = options.Value;
    }
}
```

---

## エラーハンドリングパターン

### Pattern 9: Graceful Degradation Pattern

**目的**: 一部機能の失敗が全体に影響しない

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(request)
{
    var emailData = await TryGetEmailData(request);  // 失敗してもnull
    var calendarData = await TryGetCalendarData(request);  // 失敗してもnull
    
    if (emailData == null && calendarData == null)
    {
        return new SalesSummaryResponse
        {
            Response = "データ取得に失敗しました。設定を確認してください。"
        };
    }
    
    // 取得できたデータのみでLLM推論
    var availableData = new List<string>();
    if (emailData != null) availableData.Add(emailData);
    if (calendarData != null) availableData.Add(calendarData);
    
    var summary = await _llm.GenerateSummaryAsync(string.Join("\n", availableData));
    return new SalesSummaryResponse { Response = summary };
}

private async Task<string?> TryGetEmailData(request)
{
    try
    {
        return await _emailTool.SearchSalesEmails(...);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "メールデータ取得失敗");
        return null;
    }
}
```

### Pattern 10: Circuit Breaker Pattern (Polly)

**目的**: 繰り返し失敗するサービスへの無駄な呼び出しを防ぐ

```csharp
// NuGet: Polly
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<ServiceException>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromMinutes(1)
    );

public async Task<string> SearchEmailsWithCircuitBreaker(query)
{
    return await circuitBreakerPolicy.ExecuteAsync(async () =>
    {
        return await _emailTool.SearchSalesEmails(query);
    });
}
```

**動作**:
```
Request 1 → Failure (1/3)
Request 2 → Failure (2/3)
Request 3 → Failure (3/3) → Circuit OPEN

Request 4 → Circuit OPEN → 即座にエラー返却（API呼び出しなし）
Request 5 → Circuit OPEN → 即座にエラー返却

[1分経過]

Request 6 → Circuit HALF-OPEN → 試行
  → Success → Circuit CLOSED（正常復帰）
```

---

## テレメトリパターン

### Pattern 11: Distributed Tracing Pattern

**目的**: マイクロサービス間の処理フローを追跡

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(request)
{
    using var activity = Activity.Current?.Source.StartActivity("GenerateSalesSummary");
    activity?.SetTag("user.query", request.Query);
    activity?.SetTag("date.range", $"{request.StartDate} - {request.EndDate}");
    
    try
    {
        // Phase 1: データ収集
        using var dataCollectionActivity = Activity.Current?.Source.StartActivity("DataCollection");
        var emailData = await CollectEmailData(request);
        dataCollectionActivity?.SetTag("email.count", emailData.Count);
        
        // Phase 2: LLM推論
        using var llmActivity = Activity.Current?.Source.StartActivity("LLMInference");
        llmActivity?.SetTag("llm.provider", _llmProvider.ProviderName);
        var response = await _agent.RunAsync(request.Query);
        llmActivity?.SetTag("response.length", response.Length);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        return new SalesSummaryResponse { Response = response };
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

**トレース出力例**:
```
GenerateSalesSummary (3700ms)
├── DataCollection (1200ms)
│   ├── SearchSalesEmails (800ms)
│   │   └── GraphAPICall /messages (600ms)
│   └── SearchSalesMeetings (400ms)
│       └── GraphAPICall /events (300ms)
└── LLMInference (2500ms)
    ├── LLMRequest /chat/completions (2000ms)
    └── ResponseExtraction (500ms)
```

### Pattern 12: Metrics Pattern

```csharp
public class AgentMetrics
{
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _latencyHistogram;
    private readonly Gauge<int> _activeAgentsGauge;

    public void RecordRequest(string operation, bool success, double latencyMs)
    {
        _requestCounter.Add(1, 
            new("operation", operation),
            new("success", success)
        );
        
        _latencyHistogram.Record(latencyMs,
            new("operation", operation)
        );
    }

    public void UpdateActiveAgents(int count)
    {
        _activeAgentsGauge.Record(count);
    }
}
```

**ダッシュボードクエリ**:
```promql
# リクエスト成功率
rate(agent_requests_total{success="true"}[5m]) / 
rate(agent_requests_total[5m]) * 100

# P95レイテンシ
histogram_quantile(0.95, sum(rate(agent_latency_bucket[5m])) by (le))

# アクティブエージェント数
agent_active_agents
```

---

## まとめ

Sales Support Agentで使用している主要パターン:

### Microsoft.Extensions.AI
- ✅ **Builder Pattern**: ミドルウェアチェーン
- ✅ **AIAgent Pattern**: ツール統合

### Agent 365 SDK
- ✅ **Observability Pattern**: 分散トレーシング
- ✅ **Notification Pattern**: リアルタイム通知

### Microsoft 365 SDK
- ✅ **Repository Pattern**: Graph API カプセル化
- ✅ **Batch Request Pattern**: パフォーマンス最適化

### 横断的パターン
- ✅ **DI Pattern**: サービスライフタイム管理
- ✅ **Graceful Degradation**: 部分的障害許容
- ✅ **Circuit Breaker**: 障害の連鎖防止
- ✅ **Distributed Tracing**: エンドツーエンド可観測性

### 次のステップ

- **[07-ERROR-HANDLING.md](07-ERROR-HANDLING.md)**: エラーハンドリング詳細
- **[08-LOGGING-TELEMETRY.md](08-LOGGING-TELEMETRY.md)**: ロギングとテレメトリ
- **[10-PERFORMANCE-OPTIMIZATION.md](10-PERFORMANCE-OPTIMIZATION.md)**: パフォーマンス最適化
