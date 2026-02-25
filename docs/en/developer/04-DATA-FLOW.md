# Data Flow - Graph API → LLM → Response Detailed Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/04-DATA-FLOW.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](04-DATA-FLOW.md)

## 📋 Table of Contents

- [Overview](#overview)
- [End-to-End Data Flow](#end-to-end-data-flow)
- [Phase 1: Receiving User Requests](#phase-1-receiving-user-requests)
- [Phase 2: Graph API Data Collection](#phase-2-graph-api-data-collection)
- [Phase 3: LLM Inference and Response Generation](#phase-3-llm-inference-and-response-generation)
- [Phase 4: Response Delivery and Real-Time Notifications](#phase-4-response-delivery-and-real-time-notifications)
- [Detailed Sequence Diagram](#detailed-sequence-diagram)
- [Code Walkthrough](#code-walkthrough)
- [Performance Optimization](#performance-optimization)

---

## Overview

The Sales Support Agent data flow consists of the following four phases:

```
User Request → Graph API Data Collection → LLM Inference → Response Delivery
     ↓                    ↓                      ↓                  ↓
  Teams Bot          MCP Tools              AI Agent          SignalR Hub
                   (Email/Calendar)    (Microsoft.Extensions.AI)  (Dashboard)
```

---

## End-to-End Data Flow

### Overall Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        User[👤 User<br/>Teams / API]
        Dashboard[📊 Dashboard<br/>Observability UI]
    end
    
    subgraph "Bot Framework Layer"
        TeamsBot[🤖 Teams Bot<br/>ActivityHandler]
        BotAdapter[Bot Adapter]
    end
    
    subgraph "Application Layer"
        SalesAgent[💼 Sales Agent<br/>Business Logic]
        MessageProcessor[📨 Message Processor]
    end
    
    subgraph "AI Layer"
        AIAgent[🧠 AI Agent<br/>Agent365 SDK]
        IChatClient[💬 IChatClient<br/>Microsoft.Extensions.AI]
        LLMProvider[☁️ LLM Provider<br/>Azure OpenAI/Ollama]
    end
    
    subgraph "Data Layer"
        EmailTool[📧 OutlookEmailTool]
        CalendarTool[📅 OutlookCalendarTool]
        SharePointTool[📁 SharePointTool]
        TeamsTool[💬 TeamsMessageTool]
        GraphClient[📊 GraphServiceClient]
    end
    
    subgraph "Microsoft Graph API"
        Mail[✉️ Mail API]
        Calendar[📆 Calendar API]
        SharePoint[🗂️ SharePoint API]
        Teams[💬 Teams API]
    end
    
    subgraph "Observability Layer"
        ObsService[🔍 ObservabilityService]
        NotificationService[📢 NotificationService]
        SignalR[⚡ SignalR Hub]
    end
    
    User -->|1. Request| TeamsBot
    User -->|1. Request| SalesAgent
    TeamsBot -->|2. Delegate processing| SalesAgent
    SalesAgent -->|3. Execute| AIAgent
    AIAgent -->|4. LLM call| IChatClient
    IChatClient -->|5. Inference request| LLMProvider
    
    AIAgent -->|6. Tool call| EmailTool
    AIAgent -->|6. Tool call| CalendarTool
    AIAgent -->|6. Tool call| SharePointTool
    AIAgent -->|6. Tool call| TeamsTool
    
    EmailTool -->|7. Fetch data| GraphClient
    CalendarTool -->|7. Fetch data| GraphClient
    SharePointTool -->|7. Fetch data| GraphClient
    TeamsTool -->|7. Fetch data| GraphClient
    
    GraphClient -->|8. API call| Mail
    GraphClient -->|8. API call| Calendar
    GraphClient -->|8. API call| SharePoint
    GraphClient -->|8. API call| Teams
    
    Mail -->|9. Return data| GraphClient
    Calendar -->|9. Return data| GraphClient
    SharePoint -->|9. Return data| GraphClient
    Teams -->|9. Return data| GraphClient
    
    GraphClient -->|10. Result| EmailTool
    EmailTool -->|11. Summary| AIAgent
    AIAgent -->|12. Tool result| LLMProvider
    LLMProvider -->|13. Final response| AIAgent
    AIAgent -->|14. Result| SalesAgent
    SalesAgent -->|15. Response| TeamsBot
    TeamsBot -->|16. Message| User
    
    SalesAgent -.->|Record trace| ObsService
    SalesAgent -.->|Send notification| NotificationService
    ObsService -.->|Real-time delivery| SignalR
    NotificationService -.->|Real-time delivery| SignalR
    SignalR -.->|Update| Dashboard
    
    style User fill:#e1f5ff
    style SalesAgent fill:#fff4e1
    style AIAgent fill:#f0e1ff
    style GraphClient fill:#e1ffe1
    style ObsService fill:#fff3e0
    style Dashboard fill:#e8f5e9
```

---

## Phase 1: Receiving User Requests

### 1.1 Teams Bot Entry Point

**Bot/TeamsBot.cs**:

```csharp
protected override async Task OnMessageActivityAsync(
    ITurnContext<IMessageActivity> turnContext,
    CancellationToken cancellationToken)
{
    var userMessage = turnContext.Activity.Text;
    
    // Delegate processing to Sales Agent
    var request = new SalesSummaryRequest
    {
        Query = userMessage,
       StartDate = DateTime.Now.AddDays(-7),
        EndDate = DateTime.Now
    };
    
    var response = await _salesAgent.GenerateSalesSummaryAsync(request);
    
    // Return response to user
    await turnContext.SendActivityAsync(
        MessageFactory.Text(response.Response),
        cancellationToken);
}
```

### 1.2 API Endpoint (Direct Call)

**Program.cs**:

```csharp
app.MapPost("/api/sales-summary", async (
    SalesSummaryRequest request,
    SalesAgent salesAgent) =>
{
    return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
    {
        var response = await salesAgent.GenerateSalesSummaryAsync(request);
        return Results.Ok(response);
    });
});
```

**Request Example**:

```bash
curl -X POST http://localhost:5000/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{
    "query": "今週の商談サマリを教えてください",
    "startDate": "2026-02-03",
    "endDate": "2026-02-09"
  }'
```

---

## Phase 2: Graph API Data Collection

### 2.1 Sales Agent Execution Start

**Services/Agent/SalesAgent.cs - GenerateSalesSummaryAsync**:

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var operationId = Guid.NewGuid().ToString();
    
    // Start detailed trace session
    var sessionId = _observabilityService.StartDetailedTrace(
        conversationId: operationId,
        userId: "API-User",
        userQuery: request.Query
    );

    try
    {
        // Phase 1: Request received
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Request Received",
            "商談サマリ生成リクエストを受信しました",
            new { Query = request.Query, StartDate = request.StartDate, EndDate = request.EndDate }
        );
        
        // Notification: Start notification
        await _notificationService.SendProgressNotificationAsync(
            operationId, 
            "🚀 商談サマリ生成を開始しています...", 
            0);
        
        // Set default date range
        var startDate = request.StartDate ?? GetMondayOfCurrentWeek();
        var endDate = request.EndDate ?? GetSundayOfCurrentWeek();

        // Add date range to query
        var enhancedQuery = $"{request.Query}\n\n期間: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";
        
        // Phase 2: Query preparation
        await _observabilityService.AddTracePhaseAsync(
            sessionId,
            "Query Preparation",
            "日付範囲を含むクエリを準備しました",
            new { EnhancedQuery = enhancedQuery, StartDate = startDate, EndDate = endDate }
        );
        
        // Agent execution (detailed in next section)
        var agentResponse = await _agent.RunAsync(enhancedQuery);
        // ...
    }
    catch (Exception ex)
    {
        // Error handling (described later)
    }
}
```

### 2.2 AI Agent Tool Calling

**Agent Configuration (CreateAgent Method)**:

```csharp
private AIAgent CreateAgent()
{
    var chatClient = _llmProvider.GetChatClient();

    // Register tools
    var tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments),
        AIFunctionFactory.Create(_teamsTool.SearchSalesMessages)
    };

    return chatClient.AsAIAgent(
        SystemPrompt,  // Sales support agent system prompt
        "営業支援エージェント",
        tools: tools
    );
}
```

**System Prompt (Important)**:

```csharp
private const string SystemPrompt = @"あなたは営業支援エージェントです。
以下のツールを使用して、Microsoft 365 から商談関連情報を収集し、わかりやすくサマリを作成します。

【利用可能なツール】
1. SearchSalesEmails - Outlook メールから商談関連メールを検索
2. SearchSalesMeetings - Outlook カレンダーから商談予定を検索
3. SearchSalesDocuments - SharePoint から提案書・見積書などを検索
4. SearchSalesMessages - Teams チャネルから商談関連メッセージを検索

【重要な指示】
- ユーザーからの質問に基づいて、適切なツールを選択して情報を収集してください
- 複数のツールを組み合わせて、包括的な商談サマリを作成してください
- 日本語で丁寧に回答してください

【出力フォーマット】
## 📊 サマリー
- 全体概要

## 📧 商談メール
- 重要なメール

## 📅 商談予定
- 今後の予定

## 📁 関連ドキュメント
- 提案書、見積書

## 💡 推奨アクション
- 次のアクション";
```

### 2.3 Graph API Data Retrieval via MCP Tools

#### OutlookEmailTool Implementation

**Services/MCP/McpTools/OutlookEmailTool.cs**:

```csharp
public class OutlookEmailTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _userId;

    [Description("商談関連のメールを検索して取得します")]
    public async Task<string> SearchSalesEmails(
        [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
        [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
        [Description("検索キーワード（例: 商談,提案,見積）")] string keywords = "商談,提案,見積,契約")
    {
        try
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate).AddDays(1); // Include end date

            // Access specific user's mailbox using Agent Identity
            var messages = await _graphClient.Users[_userId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = 
                        $"receivedDateTime ge {start:yyyy-MM-ddTHH:mm:ssZ} " +
                        $"and receivedDateTime le {end:yyyy-MM-ddTHH:mm:ssZ}";
                    config.QueryParameters.Top = 50;
                    config.QueryParameters.Select = new[] 
                    { 
                        "subject", "from", "receivedDateTime", 
                        "bodyPreview", "hasAttachments", "categories" 
                    };
                    config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                });

            if (messages?.Value == null || messages.Value.Count == 0)
            {
                return $"📧 期間 {startDate} ~ {endDate} の商談関連メールは見つかりませんでした。";
            }

            // Filter by keywords
            var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
            var filteredMessages = messages.Value
                .Where(m => keywordList.Any(k => 
                    m.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                    m.BodyPreview?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Categories?.Any(c => c.Contains(k, StringComparison.OrdinalIgnoreCase)) == true))
                .ToList();

            // Generate summary
            var summary = $"📧 **商談関連メール ({filteredMessages.Count}件)**\n\n";
            foreach (var msg in filteredMessages.Take(10))
            {
                summary += $"- **{msg.Subject}**\n";
                summary += $"  送信者: {msg.From?.EmailAddress?.Name ?? "不明"}\n";
                summary += $"  受信日時: {msg.ReceivedDateTime:yyyy/MM/dd HH:mm}\n";
                summary += $"  添付ファイル: {(msg.HasAttachments == true ? "あり" : "なし")}\n";
                summary += $"  概要: {msg.BodyPreview?.Substring(0, Math.Min(100, msg.BodyPreview.Length))}...\n\n";
            }

            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ メール取得エラー: {ex.Message}";
        }
    }
}
```

**Graph API Call Internal Flow**:

```mermaid
sequenceDiagram
    participant Tool as OutlookEmailTool
    participant Graph as GraphServiceClient
    participant Cred as TokenCredential
    participant AAD as Azure AD
    participant API as Graph Mail API
    
    Tool->>Graph: Users[userId].Messages.GetAsync(config)
    Graph->>Graph: Build query parameters<br/>Filter, Top, Select, Orderby
    Graph->>Cred: Request token
    
    alt Token cache hit
        Cred->>Graph: Cached token
    else No token / Expired
        Cred->>AAD: Client Credentials Flow
        AAD->>Cred: Access Token (valid for 1 hour)
        Cred->>Graph: New token
    end
    
    Graph->>API: GET /v1.0/users/{userId}/messages<br/>?$filter=receivedDateTime ge ...<br/>&$top=50<br/>&$select=subject,from,...<br/>&$orderby=receivedDateTime desc<br/>Authorization: Bearer {token}
    
    API->>API: Apply filter, sort, projection
    API->>Graph: Message[] (JSON)
    Graph->>Graph: Deserialize
    Graph->>Tool: MessageCollectionResponse
    
    Tool->>Tool: Keyword filtering<br/>(subject, bodyPreview, categories)
    Tool->>Tool: Generate summary text
    Tool-->>AIAgent: "📧商談関連メール (5件)..."
```

#### OutlookCalendarTool Implementation

**Same pattern**:

```csharp
public async Task<string> SearchSalesMeetings(
    string startDate,
    string endDate,
    string keywords = "商談,提案,ミーティング")
{
    var events = await _graphClient.Users[_userId].Calendar.Events
        .GetAsync(config =>
        {
            config.QueryParameters.Filter = 
                $"start/dateTime ge '{start:yyyy-MM-ddTHH:mm:ss}' " +
                $"and end/dateTime le '{end:yyyy-MM-ddTHH:mm:ss}'";
            config.QueryParameters.Select = new[] 
            { 
                "subject", "start", "end", "attendees", "location", "bodyPreview" 
            };
            config.QueryParameters.Orderby = new[] { "start/dateTime" };
        });
    
    // Filtering and summary generation (same pattern as email)
}
```

---

## Phase 3: LLM Inference and Response Generation

### 3.1 AI Agent Execution Flow

```mermaid
sequenceDiagram
    participant SA as SalesAgent
    participant Agent as AIAgent
    participant Chat as IChatClient
    participant LLM as LLM Provider<br/>(Azure OpenAI)
    participant Tools as MCP Tools
    
    SA->>Agent: RunAsync("今週の商談サマリ\n期間: 2026-02-03~2026-02-09")
    Agent->>Chat: CompleteAsync(messages, options)
    
    Note over Chat: Build messages<br/>[SystemPrompt, UserQuery]
    
    Chat->>LLM: POST /chat/completions<br/>model: gpt-4<br/>messages: [...]<br/>tools: [SearchSalesEmails, ...]
    
    LLM->>LLM: Determine if tool call is needed
    LLM->>Chat: ToolCall:<br/>SearchSalesEmails<br/>{startDate: "2026-02-03", ...}
    
    Chat->>Agent: ToolCall detected
    Agent->>Tools: SearchSalesEmails("2026-02-03", "2026-02-09", "商談,提案")
    
    Tools->>Tools: Graph API call<br/>(see previous section)
    Tools-->>Agent: "📧商談関連メール (5件)..."
    
    Agent->>Chat: Add tool result to messages
    Chat->>LLM: POST /chat/completions<br/>messages: [System, User, ToolCall, ToolResult]
    
    LLM->>LLM: Determine if more tools are needed
    LLM->>Chat: ToolCall:<br/>SearchSalesMeetings<br/>{startDate: "2026-02-03", ...}
    
    Chat->>Agent: ToolCall detected
    Agent->>Tools: SearchSalesMeetings(...)
    Tools-->>Agent: "📅商談予定 (3件)..."
    
    Agent->>Chat: Add tool result
    Chat->>LLM: POST /chat/completions<br/>messages: [System, User, Tool1, Result1, Tool2, Result2]
    
    LLM->>LLM: Generate final summary
    LLM->>Chat: TextContent:<br/>"## 📊 サマリー\n今週は5件の商談メールと..."
    
    Chat->>Agent: ChatCompletion
    Agent->>SA: AgentResponse {Messages: [...], Contents: [TextContent]}
```

### 3.2 LLM Tool Calling Details

**IChatClient Options Configuration**:

```csharp
var options = new ChatOptions
{
    Temperature = 0.7f,
    MaxTokens = 2000,
    Tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
        // ...
    }
};
```

**Automatic Schema Generation by AIFunctionFactory**:

```csharp
// Method definition
[Description("商談関連のメールを検索して取得します")]
public async Task<string> SearchSalesEmails(
    [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
    [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
    [Description("検索キーワード（例: 商談,提案,見積）")] string keywords = "商談,提案,見積,契約")

// ↓ Automatically generated by AIFunctionFactory.Create ↓

{
  "type": "function",
  "function": {
    "name": "SearchSalesEmails",
    "description": "商談関連のメールを検索して取得します",
    "parameters": {
      "type": "object",
      "properties": {
        "startDate": {
          "type": "string",
          "description": "検索開始日 (yyyy-MM-dd)"
        },
        "endDate": {
          "type": "string",
          "description": "検索終了日 (yyyy-MM-dd)"
        },
        "keywords": {
          "type": "string",
          "description": "検索キーワード（例: 商談,提案,見積）",
          "default": "商談,提案,見積,契約"
        }
      },
      "required": ["startDate", "endDate"]
    }
  }
}
```

### 3.3 Response Text Extraction

**SalesAgent.cs - ExtractResponseText Method**:

```csharp
private string ExtractResponseText(object agentResponse)
{
    try
    {
        dynamic response = agentResponse;
        
        // Agent 365 SDK response structure
        if (agentResponse.GetType().GetProperty("Messages") != null)
        {
            var messages = response.Messages as IEnumerable<object>;
            if (messages != null && messages.Any())
            {
                var lastMessage = messages.LastOrDefault();
                if (lastMessage != null)
                {
                    dynamic message = lastMessage;
                    
                    // Check Contents property
                    if (lastMessage.GetType().GetProperty("Contents") != null)
                    {
                        var contents = message.Contents as IEnumerable<object>;
                        if (contents != null)
                        {
                            var textContents = contents
                                .Where(c => c.GetType().Name.Contains("TextContent"))
                                .ToList();
                            
                            if (textContents.Any())
                            {
                                var texts = textContents.Select(tc => 
                                {
                                    dynamic textContent = tc;
                                    return textContent.Text as string ?? "";
                                }).Where(t => !string.IsNullOrWhiteSpace(t));
                                
                                return string.Join("\n\n", texts).Trim();
                            }
                        }
                    }
                }
            }
        }

        return "応答がありませんでした。";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "応答テキスト抽出エラー");
        return $"応答の処理中にエラーが発生しました: {ex.Message}";
    }
}
```

---

## Phase 4: Response Delivery and Real-Time Notifications

### 4.1 Observability Trace Recording

```csharp
// Phase 5: Completion
await _observabilityService.AddTracePhaseAsync(
    sessionId,
    "Summary Generation Completed",
    "商談サマリの生成が完了しました",
    new 
    { 
        TotalDurationMs = stopwatch.ElapsedMilliseconds,
        DataSources = dataSources,
        ResponseLength = responseText?.Length ?? 0
    }
);
```

### 4.2 SignalR Real-Time Notifications

```csharp
// Notification: Completion notification (including data source information)
await _notificationService.SendSuccessNotificationAsync(
    operationId, 
    $"✅ 商談サマリ生成完了！（処理時間: {stopwatch.ElapsedMilliseconds:N0}ms）",
    new 
    { 
        ProcessingTimeMs = stopwatch.ElapsedMilliseconds, 
        DataSourceCount = dataSources.Count,
        DataSources = string.Join(", ", dataSources),
        ResponseLength = responseText?.Length ?? 0
    }
);
```

**SignalR Hub Delivery Flow**:

```mermaid
sequenceDiagram
    participant SA as SalesAgent
    participant NS as NotificationService
    participant Hub as SignalR Hub
    participant Client as Dashboard Client
    
    SA->>NS: SendProgressNotificationAsync("🚀開始", 0%)
    NS->>Hub: SendNotificationAsync(notification)
    Hub->>Client: notification: {message: "🚀開始", progress: 0}
    Client->>Client: UI update (progress bar)
    
    SA->>NS: SendProgressNotificationAsync("📊データ収集中", 25%)
    NS->>Hub: SendNotificationAsync(notification)
    Hub->>Client: notification: {message: "📊データ収集中", progress: 25}
    
    SA->>NS: SendProgressNotificationAsync("🤖AI分析中", 75%)
    NS->>Hub: SendNotificationAsync(notification)
    Hub->>Client: notification: {message: "🤖AI分析中", progress: 75}
    
    SA->>NS: SendSuccessNotificationAsync("✅完了", metadata)
    NS->>Hub: SendNotificationAsync(notification)
    Hub->>Client: notification: {type: "success", message: "✅完了"}
    Client->>Client: UI update (completion display)
```

### 4.3 Final Response Return

```csharp
return new SalesSummaryResponse
{
    Response = responseText ?? "応答がありませんでした。",
    DataSources = dataSources,  // ["Outlook", "Calendar", "SharePoint"]
    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
    LLMProvider = _llmProvider.ProviderName  // "AzureOpenAI"
};
```

---

## Detailed Sequence Diagram

### Complete End-to-End Sequence

```mermaid
sequenceDiagram
    participant U as User
    participant Bot as Teams Bot
    participant SA as SalesAgent
    participant Obs as ObservabilityService
    participant Notif as NotificationService
    participant Agent as AIAgent
    participant Chat as IChatClient
    participant LLM as LLM Provider
    participant Email as EmailTool
    participant Graph as GraphServiceClient
    participant API as Graph API
    participant Hub as SignalR Hub
    participant Dash as Dashboard
    
    U->>Bot: "今週の商談サマリ"
    Bot->>SA: GenerateSalesSummaryAsync(request)
    
    SA->>Obs: StartDetailedTrace(sessionId)
    SA->>Obs: AddTracePhaseAsync("Request Received")
    SA->>Notif: SendProgressNotificationAsync("🚀開始", 0%)
    Notif->>Hub: SendNotificationAsync
    Hub->>Dash: Real-time update
    
    SA->>Agent: RunAsync("今週の商談サマリ\n期間: 2026-02-03~09")
    Agent->>Chat: CompleteAsync(messages, options)
    
    Chat->>LLM: POST /chat/completions<br/>tools: [SearchSalesEmails, ...]
    LLM-->>Chat: ToolCall: SearchSalesEmails
    
    Chat->>Agent: ToolCall detected
    Agent->>Email: SearchSalesEmails("2026-02-03", "2026-02-09", "商談")
    
    SA->>Notif: SendProgressNotificationAsync("📊データ収集", 25%)
    Notif->>Hub: SendNotificationAsync
    Hub->>Dash: Progress update
    
    Email->>Graph: Users[userId].Messages.GetAsync(filter)
    Graph->>API: GET /v1.0/users/{id}/messages?$filter=...
    API-->>Graph: Message[] (JSON)
    Graph-->>Email: MessageCollectionResponse
    
    Email->>Email: Keyword filtering
    Email->>Email: Summary text generation
    Email-->>Agent: "📧商談関連メール (5件)..."
    
    Agent->>Chat: Add tool result
    Chat->>LLM: POST /chat/completions<br/>messages: [User, ToolResult]
    
    LLM-->>Chat: ToolCall: SearchSalesMeetings
    Agent->>Email: SearchSalesMeetings(...)
    Email->>Graph: Calendar.Events.GetAsync
    Graph->>API: GET /v1.0/users/{id}/calendar/events
    API-->>Graph: Event[]
    Graph-->>Email: EventCollectionResponse
    Email-->>Agent: "📅商談予定 (3件)..."
    
    Agent->>Chat: Add tool result
    Chat->>LLM: POST /chat/completions
    
    SA->>Notif: SendProgressNotificationAsync("🤖AI分析中", 75%)
    Notif->>Hub: SendNotificationAsync
    
    LLM->>LLM: Generate final summary
    LLM-->>Chat: TextContent: "## 📊サマリー\n..."
    Chat-->>Agent: ChatCompletion
    Agent-->>SA: AgentResponse
    
    SA->>SA: ExtractResponseText(response)
    SA->>Obs: AddTracePhaseAsync("Completed")
    SA->>Notif: SendSuccessNotificationAsync("✅完了", metadata)
    Notif->>Hub: SendNotificationAsync
    Hub->>Dash: Completion notification
    
    SA-->>Bot: SalesSummaryResponse
    Bot->>U: Adaptive Card display<br/>"## 📊サマリー\n..."
```

---

## Code Walkthrough

### Typical Execution Trace

```
[00:00.000] ℹ️ Sales summary generation started: 今週の商談サマリを教えてください
[00:00.050] 📝 Detailed trace session started: session-abc123
[00:00.100] 📊 Phase: Request Received
[00:00.150] 📢 Notification sent: 🚀 商談サマリ生成を開始しています... (0%)
[00:00.200] 📝 Query expansion: 今週の商談サマリ\n期間: 2026-02-03 ~ 2026-02-09

[00:00.300] 📊 Phase: Query Preparation
[00:00.350] 🤖 AI Agent execution started
[00:00.400] 📊 Phase: AI Agent Execution Started

[00:00.500] 🔧 LLM: Tool call - SearchSalesEmails
[00:00.550] 📢 Notification sent: 📊 データ収集中（メール、カレンダー）... (25%)
[00:00.600] 🔐 TokenCredential: Using cached token
[00:00.650] 📊 Graph API: GET /users/{id}/messages?$filter=...
[00:01.200] ✅ Graph API: Retrieved 50 messages
[00:01.250] 🔍 Keyword filtering: 5 matches
[00:01.300] 📧 Summary generated: Sales-related emails (5 items)

[00:01.400] 🔧 LLM: Tool call - SearchSalesMeetings
[00:01.450] 📊 Graph API: GET /users/{id}/calendar/events?$filter=...
[00:01.900] ✅ Graph API: Retrieved 10 events
[00:01.950] 🔍 Keyword filtering: 3 matches
[00:02.000] 📅 Summary generated: Sales meetings (3 items)

[00:02.100] 📢 Notification sent: 🤖 AI分析中（サマリ生成処理）... (75%)
[00:02.200] 🤖 LLM: Final summary generation started
[00:03.500] ✅ LLM: Summary generation completed (1200 characters)

[00:03.600] 📊 Phase: AI Response Received (3000ms)
[00:03.650] 📝 Response text extraction completed
[00:03.700] 📊 Phase: Summary Generation Completed
[00:03.750] 📢 Notification sent: ✅ 商談サマリ生成完了！（処理時間: 3750ms）
[00:03.800] ✅ Sales summary generation completed (total: 3800ms)
```

### Data Transformation at Each Phase

| Phase | Input | Processing | Output |
|-------|-------|------------|--------|
| 1. Request | User text message | Parse query, set date range | SalesSummaryRequest |
| 2. Data Collection | Date range + keywords | Graph API call, keyword filtering | Summary text per tool |
| 3. LLM Inference | System prompt + tool results | Multi-turn tool calling | Final summary text |
| 4. Response | Summary text | Trace recording, notification | SalesSummaryResponse |

---

## Performance Optimization

### Data Collection Optimization

```mermaid
graph LR
    subgraph "Current Implementation"
        A[Tool 1] -->|Sequential| B[Tool 2]
        B -->|Sequential| C[Tool 3]
    end
    
    subgraph "Optimized (LLM Control)"
        D[Tool 1] -->|Parallel possible| E[Tool 2]
        D -->|Parallel possible| F[Tool 3]
    end
```

### Key Optimization Points

| Item | Current | Optimization |
|------|---------|-------------|
| Graph API Calls | Top=50 per tool | Reduce Top, add Select for projection |
| Token Caching | SDK standard caching | Token pre-acquisition and caching |
| LLM Calls | Multiple round-trips per tool | Batch tool calling |
| Response Size | Full body text | bodyPreview only (reducing token count) |
| Timeout | No setting | Set timeout per tool call |

### Recommended Improvements

1. **Graph API Query Optimization**: Use `$select` to retrieve only required fields
2. **Parallel Execution**: Execute independent tool calls in parallel when possible via the LLM
3. **Response Caching**: Cache frequently executed queries for a certain period
4. **Streaming Response**: Send partial results using SignalR streaming
5. **Token Management**: Pre-acquire tokens and implement smart caching

---

> **Next**: [Dependency Injection](05-DEPENDENCY-INJECTION.md) - Learn how DI container configuration works
