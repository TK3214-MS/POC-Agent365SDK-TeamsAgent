# Conversation Flow - 会話フロー詳細ウォークスルー

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](CONVERSATION-FLOW.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../../en/developer/13-CODE-WALKTHROUGHS/CONVERSATION-FLOW.md)

## 📋 概要

このドキュメントでは、ユーザーがTeams経由で "今週の商談サマリを教えてください" と送信した際の完全な実行フローをコードレベルで解説します。

---

## エントリーポイント: Bot/TeamsBot.cs

### OnMessageActivityAsync

```csharp
protected override async Task OnMessageActivityAsync(
    ITurnContext<IMessageActivity> turnContext,
    CancellationToken cancellationToken)
{
    // 1. ユーザーメッセージ取得
    var userMessage = turnContext.Activity.Text;
    var userId = turnContext.Activity.From.Id;
    var conversationId = turnContext.Activity.Conversation.Id;
    
    _logger.LogInformation(
        "📨 メッセージ受信: User={UserId}, Message={Message}",
        userId,
        userMessage
    );
    
    // 2. リクエスト構築
    var request = new SalesSummaryRequest
    {
        Query = userMessage,
        StartDate = DateTime.Now.AddDays(-7),  // デフォルト: 今週
        EndDate = DateTime.Now
    };
    
    // 3. タイピングインジケーター送信
    await turnContext.SendActivitiesAsync(
        new Activity[] { new Activity { Type = ActivityTypes.Typing } },
        cancellationToken);
    
    // 4. Sales Agent に処理委譲
    var response = await _salesAgent.GenerateSalesSummaryAsync(request);
    
    // 5. 応答を Adaptive Card で返却
    var card = AdaptiveCardHelper.CreateSalesSummaryCard(response);
    var attachment = new Attachment
    {
        ContentType = AdaptiveCard.ContentType,
        Content = card
    };
    
    await turnContext.SendActivityAsync(
        MessageFactory.Attachment(attachment),
        cancellationToken);
}
```

**実行時のログ出力**:
```
info: SalesSupportAgent.Bot.TeamsBot[0]
      📨 メッセージ受信: User="29:1AbC...", Message="今週の商談サマリを教えてください"
```

---

## Sales Agent: Services/Agent/SalesAgent.cs

### GenerateSalesSummaryAsync

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var operationId = Guid.NewGuid().ToString();
    
    // 1. 詳細トレースセッション開始
    var sessionId = _observabilityService.StartDetailedTrace(
        conversationId: operationId,
        userId: "API-User",
        userQuery: request.Query
    );
    
    _logger.LogInformation("商談サマリ生成開始: {Query}", request.Query);

    try
    {
        // 2. Phase 1: リクエスト受信
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Request Received",
            "商談サマリ生成リクエストを受信しました",
            new { Query = request.Query }
        );
        
        // 3. 通知: 開始
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "🚀 商談サマリ生成を開始しています...",
            0
        );
        
        // 4. 日付範囲設定
        var startDate = request.StartDate ?? GetMondayOfCurrentWeek();
        var endDate = request.EndDate ?? GetSundayOfCurrentWeek();
        var enhancedQuery = $"{request.Query}\n\n期間: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";
        
        // 5. Phase 2: クエリ準備
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Query Preparation",
            "日付範囲を含むクエリを準備しました",
            new { EnhancedQuery = enhancedQuery }
        );
        
        // 6. 通知: データ収集開始
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "📊 データ収集中（メール、カレンダー、ドキュメント）...",
            25
        );
        
        // 7. AI Agent 実行
        var agentStopwatch = Stopwatch.StartNew();
        var agentResponse = await _agent.RunAsync(enhancedQuery);
        agentStopwatch.Stop();
        
        // 8. Phase 3: AI応答取得
        var responseText = ExtractResponseText(agentResponse);
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "AI Response Received",
            $"AIエージェントから応答を取得しました（{agentStopwatch.ElapsedMilliseconds}ms）",
            new { DurationMs = agentStopwatch.ElapsedMilliseconds }
        );
        
        // 9. 通知: AI分析中
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "🤖 AI分析中（サマリ生成処理）...",
            75
        );
        
        stopwatch.Stop();
        
        // 10. 通知: 完了
        await _notificationService.SendSuccessNotificationAsync(
            operationId,
            $"✅ 商談サマリ生成完了！（処理時間: {stopwatch.ElapsedMilliseconds:N0}ms）",
            new { ProcessingTimeMs = stopwatch.ElapsedMilliseconds }
        );
        
        // 11. メトリクス記録
        await _observabilityService.RecordRequestAsync(true, stopwatch.ElapsedMilliseconds);
        
        // 12. セッション完了
        await _observabilityService.CompleteDetailedTraceAsync(
            sessionId,
            responseText,
            success: true
        );
        
        return new SalesSummaryResponse
        {
            Response = responseText,
            DataSources = new List<string> { "Outlook", "Calendar", "SharePoint" },
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            LLMProvider = _llmProvider.ProviderName
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "商談サマリ生成エラー");
        
        // エラー記録
        await _observabilityService.CompleteDetailedTraceAsync(
            sessionId,
            $"エラー: {ex.Message}",
            success: false
        );
        
        return new SalesSummaryResponse
        {
            Response = $"❌ エラーが発生しました: {ex.Message}",
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            LLMProvider = _llmProvider.ProviderName
        };
    }
}
```

**実行時のログ出力**:
```
info: SalesSupportAgent.Services.Agent.SalesAgent[0]
      商談サマリ生成開始: 今週の商談サマリを教えてください

info: SalesSupportAgent.Services.Observability.ObservabilityService[0]
      📊 Phase: Request Received

info: SalesSupportAgent.Services.Notifications.NotificationService[0]
      📢 通知送信: 🚀 商談サマリ生成を開始しています...
```

---

## AI Agent 実行

### CreateAgent → RunAsync

```csharp
private AIAgent CreateAgent()
{
    var chatClient = _llmProvider.GetChatClient();
    
    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments),
    };
    
    return chatClient.AsAIAgent(
        systemPrompt: SystemPrompt,
        name: "営業支援エージェント",
        tools: tools
    );
}
```

**RunAsync 内部フロー**:

1. **LLM にクエリ送信**
```csharp
// Microsoft.Extensions.AI が内部で実行
var messages = new List<ChatMessage>
{
    new(ChatRole.System, SystemPrompt),
    new(ChatRole.User, "今週の商談サマリを教えてください\n\n期間: 2026-02-03 ~ 2026-02-09")
};

var response = await chatClient.CompleteAsync(messages, new ChatOptions
{
    Tools = tools  // SearchSalesEmails, SearchSalesMeetings, ...
});
```

2. **LLM がツール呼び出しを判断**
```json
{
  "role": "assistant",
  "tool_calls": [
    {
      "id": "call_abc123",
      "type": "function",
      "function": {
        "name": "SearchSalesEmails",
        "arguments": "{\"startDate\":\"2026-02-03\",\"endDate\":\"2026-02-09\",\"keywords\":\"商談,提案\"}"
      }
    }
  ]
}
```

3. **ツール実行 → OutlookEmailTool**
```csharp
var result = await _emailTool.SearchSalesEmails(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// 結果: "📧 商談関連メール (5件)..."
```

4. **LLM に結果を返してツール再呼び出し**
```json
{
  "role": "assistant",
  "tool_calls": [
    {
      "function": {
        "name": "SearchSalesMeetings",
        "arguments": "{\"startDate\":\"2026-02-03\",\"endDate\":\"2026-02-09\"}"
      }
    }
  ]
}
```

5. **ツール実行 → OutlookCalendarTool**
```csharp
var result = await _calendarTool.SearchSalesMeetings(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// 結果: "📅 商談予定 (3件)..."
```

6. **最終サマリ生成**
```json
{
  "role": "assistant",
  "content": "## 📊 サマリー\n今週は5件の商談メールと3件の予定があります。\n\n## 📧 商談メール\n- ...\n\n## 📅 商談予定\n- ..."
}
```

---

## Graph API 呼び出し: OutlookEmailTool

### SearchSalesEmails

```csharp
public async Task<string> SearchSalesEmails(
    string startDate,
    string endDate,
    string keywords)
{
    try
    {
        var start = DateTime.Parse(startDate);
        var end = DateTime.Parse(endDate).AddDays(1);
        
        // Graph API 呼び出し
        var messages = await _graphClient.Users[_userId].Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Filter = 
                    $"receivedDateTime ge {start:yyyy-MM-ddTHH:mm:ssZ} " +
                    $"and receivedDateTime le {end:yyyy-MM-ddTHH:mm:ssZ}";
                config.QueryParameters.Top = 50;
                config.QueryParameters.Select = new[]
                {
                    "subject", "from", "receivedDateTime", "bodyPreview"
                };
                config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
            });
        
        // キーワードフィルタリング
        var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
        var filteredMessages = messages.Value
            .Where(m => keywordList.Any(k =>
                m.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
        
        // サマリ生成
        var summary = $"📧 **商談関連メール ({filteredMessages.Count}件)**\n\n";
        foreach (var msg in filteredMessages.Take(10))
        {
            summary += $"- **{msg.Subject}**\n";
            summary += $"  送信者: {msg.From?.EmailAddress?.Name}\n";
            summary += $"  受信日時: {msg.ReceivedDateTime:yyyy/MM/dd HH:mm}\n\n";
        }
        
        return summary;
    }
    catch (ServiceException ex)
    {
        _logger.LogError(ex, "Graph APIエラー: {Code}", ex.ResponseStatusCode);
        return $"❌ メール取得エラー: {ex.Message}";
    }
}
```

**HTTP リクエスト（内部）**:
```http
GET https://graph.microsoft.com/v1.0/users/user@company.com/messages?
  $filter=receivedDateTime ge 2026-02-03T00:00:00Z and receivedDateTime le 2026-02-10T00:00:00Z
  &$top=50
  &$select=subject,from,receivedDateTime,bodyPreview
  &$orderby=receivedDateTime desc
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

---

## 完全なタイムライン

```
[00:00.000] 📨 Bot: メッセージ受信 "今週の商談サマリ"
[00:00.050]    ├─ リクエスト構築
[00:00.100]    ├─ タイピングインジケーター送信
[00:00.150]    └─ SalesAgent.GenerateSalesSummaryAsync() 呼び出し

[00:00.200] 🔍 SalesAgent: トレースセッション開始
[00:00.250]    ├─ Phase 1: Request Received
[00:00.300]    ├─ 通知: 🚀 開始
[00:00.350]    └─ クエリ拡張: "今週の商談サマリ\n期間: 2026-02-03 ~ 2026-02-09"

[00:00.400] 🤖 AI Agent: RunAsync() 実行
[00:00.450]    └─ LLM にクエリ送信

[00:00.600] 🔧 LLM: ツール呼び出し判断
[00:00.650]    └─ SearchSalesEmails("2026-02-03", "2026-02-09", "商談")

[00:00.700] 📧 EmailTool: Graph API 呼び出し
[00:00.750]    ├─ TokenCredential: キャッシュトークン使用
[00:01.300]    ├─ Graph API: 50件取得（550ms）
[00:01.350]    ├─ キーワードフィルタリング: 5件マッチ
[00:01.400]    └─ サマリ生成: "📧 商談関連メール (5件)..."

[00:01.500] 🔧 LLM: 次のツール呼び出し
[00:01.550]    └─ SearchSalesMeetings("2026-02-03", "2026-02-09")

[00:01.600] 📅 CalendarTool: Graph API 呼び出し
[00:02.000]    └─ "📅 商談予定 (3件)..."

[00:02.100] 🤖 LLM: 最終サマリ生成
[00:03.500]    └─ "## 📊 サマリー\n今週は5件の商談メールと..."

[00:03.600] ✅ SalesAgent: 完了
[00:03.650]    ├─ 応答テキスト抽出
[00:03.700]    ├─ 通知: ✅ 完了
[00:03.750]    └─ メトリクス記録

[00:03.800] 💬 Bot: Adaptive Card 送信
[00:03.850]    └─ ユーザーに表示

総処理時間: 3850ms
```

---

## 次のステップ

- **[GRAPH-API-CALLS.md](GRAPH-API-CALLS.md)**: Graph API呼び出しパターン
- **[LLM-INFERENCE.md](LLM-INFERENCE.md)**: LLM推論プロセス
- **[04-DATA-FLOW.md](../04-DATA-FLOW.md)**: データフロー詳細
