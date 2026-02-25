# LLM Inference - Detailed LLM Inference Process

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../../developer/13-CODE-WALKTHROUGHS/LLM-INFERENCE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](LLM-INFERENCE.md)

## 📋 Inference Flow

### 1. Inference with IChatClient

```csharp
var chatClient = _llmProvider.GetChatClient();

var messages = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, SystemPrompt),
    new ChatMessage(ChatRole.User, "今週の商談サマリを教えてください")
};

var options = new ChatOptions
{
    Temperature = 0.7f,
    MaxTokens = 2000,
    Tools = new List<AITool>
    {
        AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
        AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings)
    }
};

var response = await chatClient.CompleteAsync(messages, options);
```

---

## Azure OpenAI Inference Details

### Request Construction

**Internally generated HTTP request**:

```http
POST https://<resource-name>.openai.azure.com/openai/deployments/gpt-4/chat/completions?api-version=2024-02-01
Content-Type: application/json
api-key: <api-key>

{
  "messages": [
    {
      "role": "system",
      "content": "あなたは営業支援エージェントです。以下のツールを使用して..."
    },
    {
      "role": "user",
      "content": "今週の商談サマリを教えてください"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 2000,
  "tools": [
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
              "description": "検索キーワード",
              "default": "商談,提案,見積,契約"
            }
          },
          "required": ["startDate", "endDate"]
        }
      }
    }
  ]
}
```

---

## Tool Calling Flow

### Step 1: Initial LLM Call

**LLM Response** (tool call):

```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1707123456,
  "model": "gpt-4",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": null,
        "tool_calls": [
          {
            "id": "call_email_search_1",
            "type": "function",
            "function": {
              "name": "SearchSalesEmails",
              "arguments": "{\"startDate\":\"2026-02-03\",\"endDate\":\"2026-02-09\",\"keywords\":\"商談,提案\"}"
            }
          }
        ]
      },
      "finish_reason": "tool_calls"
    }
  ]
}
```

### Step 2: Tool Execution

**C# Code**:

```csharp
// Automatically handled by FunctionInvocation Middleware
var toolCall = response.Message.ToolCalls[0];
var functionName = toolCall.Function.Name;        // "SearchSalesEmails"
var arguments = toolCall.Function.Arguments;      // {"startDate":"2026-02-03",...}

// JSON deserialization
var args = JsonSerializer.Deserialize<SearchSalesEmailsArgs>(arguments);

// Tool execution
var result = await _emailTool.SearchSalesEmails(
    args.StartDate,
    args.EndDate,
    args.Keywords
);

// Result: "📧 商談関連メール (5件)..."
```

### Step 3: Return Tool Results to LLM

**Extended message list**:

```csharp
messages.Add(new ChatMessage(ChatRole.Assistant)
{
    ToolCalls = new[] { toolCall }
});

messages.Add(new ChatMessage(ChatRole.Tool)
{
    ToolCallId = "call_email_search_1",
    Content = result  // "📧 商談関連メール (5件)..."
});
```

**Second LLM request**:

```json
{
  "messages": [
    {
      "role": "system",
      "content": "あなたは営業支援エージェントです..."
    },
    {
      "role": "user",
      "content": "今週の商談サマリを教えてください"
    },
    {
      "role": "assistant",
      "tool_calls": [
        {
          "id": "call_email_search_1",
          "type": "function",
          "function": {
            "name": "SearchSalesEmails",
            "arguments": "{\"startDate\":\"2026-02-03\",...}"
          }
        }
      ]
    },
    {
      "role": "tool",
      "tool_call_id": "call_email_search_1",
      "content": "📧 商談関連メール (5件)..."
    }
  ]
}
```

### Step 4: Additional Tool Call (Calendar)

**LLM Response**:

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "tool_calls": [
          {
            "id": "call_calendar_search_1",
            "function": {
              "name": "SearchSalesMeetings",
              "arguments": "{\"startDate\":\"2026-02-03\",\"endDate\":\"2026-02-09\"}"
            }
          }
        ]
      },
      "finish_reason": "tool_calls"
    }
  ]
}
```

**Tool execution**:

```csharp
var result = await _calendarTool.SearchSalesMeetings(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// Result: "📅 商談予定 (3件)..."
```

### Step 5: Final Summary Generation

**Third LLM request** (including 2 tool results):

```json
{
  "messages": [
    /* System prompt */,
    /* User query */,
    /* Email search tool call */,
    /* Email search result */,
    /* Calendar search tool call */,
    /* Calendar search result */
  ]
}
```

**Final LLM response**:

```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "## 📊 サマリー\n今週は5件の商談メールと3件の予定があります。\n\n## 📧 商談メール\n- 株式会社A社からの提案依頼（2/5受信）\n- B社見積もり送付完了（2/6送信）\n\n## 📅 商談予定\n- 2/5 14:00 株式会社A社 商談\n- 2/7 10:00 B社 見積説明\n\n## 💡 推奨アクション\n1. A社提案書を2/4までに準備\n2. B社見積フォローアップ"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 1250,
    "completion_tokens": 350,
    "total_tokens": 1600
  }
}
```

---

## Parameter Tuning

### Temperature

```csharp
// Low temperature (0.0-0.3): Deterministic, consistency-focused
Temperature = 0.2f  // Business reports, summaries

// Medium temperature (0.4-0.7): Balanced
Temperature = 0.7f  // Conversations, summary generation

// High temperature (0.8-1.0): Creative, diverse
Temperature = 0.9f  // Brainstorming, idea generation
```

### MaxTokens

```csharp
// Concise response
MaxTokens = 500  // Approximately 200-300 characters

// Standard summary
MaxTokens = 1500  // 500-700 characters

// Detailed report
MaxTokens = 4000  // 2000+ characters
```

### TopP (Nucleus Sampling)

```csharp
// Alternative to Temperature
TopP = 0.95f  // Select from words in the top 95% probability
```

---

## Streaming Response

### CompleteStreamingAsync

```csharp
await foreach (var update in chatClient.CompleteStreamingAsync(messages, options))
{
    if (update.Text != null)
    {
        // Display to user immediately
        await turnContext.SendActivityAsync(update.Text);
    }
    
    if (update.FinishReason == ChatFinishReason.ToolCalls)
    {
        // Tool call detected
        foreach (var toolCall in update.ToolCalls)
        {
            var result = await ExecuteToolAsync(toolCall);
            messages.Add(new ChatMessage(ChatRole.Tool, result));
        }
        
        // Re-invoke LLM with tool results
        await foreach (var finalUpdate in chatClient.CompleteStreamingAsync(messages, options))
        {
            await turnContext.SendActivityAsync(finalUpdate.Text);
        }
    }
}
```

**User experience improvement**:
```
Non-streaming:
  User input → [3 sec wait] → Full response displayed

Streaming:
  User input → [0.5 sec] → "## 📊" → "サマリー\n" → "今週は..." → ...
                  Real-time display
```

---

## Error Handling
