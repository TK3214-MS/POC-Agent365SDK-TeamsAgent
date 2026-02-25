# Conversation Flow - Detailed Walkthrough

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../../developer/13-CODE-WALKTHROUGHS/CONVERSATION-FLOW.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](CONVERSATION-FLOW.md)

## 📋 Overview

This document explains the complete execution flow at the code level when a user sends "今週の商談サマリを教えてください" (Show me this week's sales summary) via Teams.

---

## Entry Point: Bot/TeamsBot.cs

### OnMessageActivityAsync

```csharp
protected override async Task OnMessageActivityAsync(
    ITurnContext<IMessageActivity> turnContext,
    CancellationToken cancellationToken)
{
    // 1. Get user message
    var userMessage = turnContext.Activity.Text;
    var userId = turnContext.Activity.From.Id;
    var conversationId = turnContext.Activity.Conversation.Id;
    
    _logger.LogInformation(
        "📨 メッセージ受信: User={UserId}, Message={Message}",
        userId,
        userMessage
    );
    
    // 2. Build request
    var request = new SalesSummaryRequest
    {
        Query = userMessage,
        StartDate = DateTime.Now.AddDays(-7),  // Default: this week
        EndDate = DateTime.Now
    };
    
    // 3. Send typing indicator
    await turnContext.SendActivitiesAsync(
        new Activity[] { new Activity { Type = ActivityTypes.Typing } },
        cancellationToken);
    
    // 4. Delegate processing to Sales Agent
    var response = await _salesAgent.GenerateSalesSummaryAsync(request);
    
    // 5. Return response as Adaptive Card
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

**Runtime log output**:
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
    
    // 1. Start detailed trace session
    var sessionId = _observabilityService.StartDetailedTrace(
        conversationId: operationId,
        userId: "API-User",
        userQuery: request.Query
    );
    
    _logger.LogInformation("商談サマリ生成開始: {Query}", request.Query);

    try
    {
        // 2. Phase 1: Request received
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Request Received",
            "商談サマリ生成リクエストを受信しました",
            new { Query = request.Query }
        );
        
        // 3. Notification: Start
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "🚀 商談サマリ生成を開始しています...",
            0
        );
        
        // 4. Set date range
        var startDate = request.StartDate ?? GetMondayOfCurrentWeek();
        var endDate = request.EndDate ?? GetSundayOfCurrentWeek();
        var enhancedQuery = $"{request.Query}\n\n期間: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";
        
        // 5. Phase 2: Query preparation
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Query Preparation",
            "日付範囲を含むクエリを準備しました",
            new { EnhancedQuery = enhancedQuery }
        );
        
        // 6. Notification: Data collection started
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "📊 データ収集中（メール、カレンダー、ドキュメント）...",
            25
        );
        
        // 7. Execute AI Agent
        var agentStopwatch = Stopwatch.StartNew();
        var agentResponse = await _agent.RunAsync(enhancedQuery);
        agentStopwatch.Stop();
        
        // 8. Phase 3: AI response received
        var responseText = ExtractResponseText(agentResponse);
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "AI Response Received",
            $"AIエージェントから応答を取得しました（{agentStopwatch.ElapsedMilliseconds}ms）",
            new { DurationMs = agentStopwatch.ElapsedMilliseconds }
        );
        
        // 9. Notification: AI analysis in progress
        await _notificationService.SendProgressNotificationAsync(
            operationId,
            "🤖 AI分析中（サマリ生成処理）...",
            75
        );
        
        stopwatch.Stop();
        
        // 10. Notification: Complete
        await _notificationService.SendSuccessNotificationAsync(
            operationId,
            $"✅ 商談サマリ生成完了！（処理時間: {stopwatch.ElapsedMilliseconds:N0}ms）",
            new { ProcessingTimeMs = stopwatch.ElapsedMilliseconds }
        );
        
        // 11. Record metrics
        await _observabilityService.RecordRequestAsync(true, stopwatch.ElapsedMilliseconds);
        
        // 12. Complete session
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
        
        // Record error
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

**Runtime log output**:
```
info: SalesSupportAgent.Services.Agent.SalesAgent[0]
      商談サマリ生成開始: 今週の商談サマリを教えてください

info: SalesSupportAgent.Services.Observability.ObservabilityService[0]
      📊 Phase: Request Received

info: SalesSupportAgent.Services.Notifications.NotificationService[0]
      📢 通知送信: 🚀 商談サマリ生成を開始しています...
```

---

## AI Agent Execution

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

**Internal flow of RunAsync**:

1. **Send query to LLM**
```csharp
// Executed internally by Microsoft.Extensions.AI
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

2. **LLM decides on tool calls**
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

3. **Tool execution → OutlookEmailTool**
```csharp
var result = await _emailTool.SearchSalesEmails(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// Result: "📧 商談関連メール (5件)..."
```

4. **Return results to LLM and trigger next tool call**
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

5. **Tool execution → OutlookCalendarTool**
```csharp
var result = await _calendarTool.SearchSalesMeetings(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// Result: "📅 商談予定 (3件)..."
```

6. **Final summary generation**
```json
{
  "role": "assistant",
  "content": "## 📊 サマリー\n今週は5件の商談メールと3件の予定があります。\n\n## 📧 商談メール\n- ...\n\n## 📅 商談予定\n- ..."
}
```

---

## Graph API Call: OutlookEmailTool

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
        
        // Graph API call
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
        
        // Keyword filtering
        var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
        var filteredMessages = messages.Value
            .Where(m => keywordList.Any(k =>
                m.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();
        
        // Generate summary
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

**HTTP Request (internal)**:
```http
GET https://graph.microsoft.com/v1.0/users/user@company.com/messages?
  $filter=receivedDateTime ge 2026-02-03T00:00:00Z and receivedDateTime le 2026-02-10T00:00:00Z
  &$top=50
  &$select=subject,from,receivedDateTime,bodyPreview
  &$orderby=receivedDateTime desc
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
```

---

## Complete Timeline

```
[00:00.000] 📨 Bot: Message received "今週の商談サマリ"
[00:00.050]    ├─ Build request
[00:00.100]    ├─ Send typing indicator
[00:00.150]    └─ Call SalesAgent.GenerateSalesSummaryAsync()

[00:00.200] 🔍 SalesAgent: Start trace session
[00:00.250]    ├─ Phase 1: Request Received
[00:00.300]    ├─ Notification: 🚀 Start
[00:00.350]    └─ Query enhancement: "今週の商談サマリ\n期間: 2026-02-03 ~ 2026-02-09"

[00:00.400] 🤖 AI Agent: Execute RunAsync()
[00:00.450]    └─ Send query to LLM

[00:00.600] 🔧 LLM: Decide on tool calls
[00:00.650]    └─ SearchSalesEmails("2026-02-03", "2026-02-09", "商談")

[00:00.700] 📧 EmailTool: Graph API call
[00:00.750]    ├─ TokenCredential: Use cached token
[00:01.300]    ├─ Graph API: Retrieved 50 items (550ms)
[00:01.350]    ├─ Keyword filtering: 5 matches
[00:01.400]    └─ Generate summary: "📧 商談関連メール (5件)..."

[00:01.500] 🔧 LLM: Next tool call
[00:01.550]    └─ SearchSalesMeetings("2026-02-03", "2026-02-09")

[00:01.600] 📅 CalendarTool: Graph API call
[00:02.000]    └─ "📅 商談予定 (3件)..."

[00:02.100] 🤖 LLM: Generate final summary
[00:03.500]    └─ "## 📊 サマリー\n今週は5件の商談メールと..."

[00:03.600] ✅ SalesAgent: Complete
[00:03.650]    ├─ Extract response text
[00:03.700]    ├─ Notification: ✅ Complete
[00:03.750]    └─ Record metrics

[00:03.800] 💬 Bot: Send Adaptive Card
[00:03.850]    └─ Display to user

Total processing time: 3850ms
```

---

## Next Steps

- **[GRAPH-API-CALLS.md](GRAPH-API-CALLS.md)**: Graph API call patterns
- **[LLM-INFERENCE.md](LLM-INFERENCE.md)**: LLM inference process
- **[04-DATA-FLOW.md](../04-DATA-FLOW.md)**: Data flow details
