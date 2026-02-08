# LLM Inference - LLM推論プロセス詳細

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](LLM-INFERENCE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../../en/developer/13-CODE-WALKTHROUGHS/LLM-INFERENCE.md)

## 📋 推論フロー

### 1. IChatClient による推論

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

## Azure OpenAI 推論詳細

### リクエスト構築

**内部的に生成されるHTTPリクエスト**:

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

## Tool Calling フロー

### ステップ1: 初回LLM呼び出し

**LLMレスポンス**（ツール呼び出し）:

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

### ステップ2: ツール実行

**C# コード**:

```csharp
// FunctionInvocation Middleware が自動処理
var toolCall = response.Message.ToolCalls[0];
var functionName = toolCall.Function.Name;        // "SearchSalesEmails"
var arguments = toolCall.Function.Arguments;      // {"startDate":"2026-02-03",...}

// JSON デシリアライズ
var args = JsonSerializer.Deserialize<SearchSalesEmailsArgs>(arguments);

// ツール実行
var result = await _emailTool.SearchSalesEmails(
    args.StartDate,
    args.EndDate,
    args.Keywords
);

// 結果: "📧 商談関連メール (5件)..."
```

### ステップ3: ツール結果をLLMに返却

**拡張されたメッセージリスト**:

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

**2回目のLLMリクエスト**:

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

### ステップ4: 追加ツール呼び出し（カレンダー）

**LLMレスポンス**:

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

**ツール実行**:

```csharp
var result = await _calendarTool.SearchSalesMeetings(
    "2026-02-03",
    "2026-02-09",
    "商談,提案"
);
// 結果: "📅 商談予定 (3件)..."
```

### ステップ5: 最終サマリ生成

**3回目のLLMリクエスト**（ツール結果2つを含む）:

```json
{
  "messages": [
    /* システムプロンプト */,
    /* ユーザークエリ */,
    /* メール検索ツール呼び出し */,
    /* メール検索結果 */,
    /* カレンダー検索ツール呼び出し */,
    /* カレンダー検索結果 */
  ]
}
```

**最終LLMレスポンス**:

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

## パラメータ調整

### Temperature

```csharp
// 低温度（0.0-0.3）: 決定的、一貫性重視
Temperature = 0.2f  // ビジネスレポート、要約

// 中温度（0.4-0.7）: バランス
Temperature = 0.7f  // 会話、サマリ生成

// 高温度（0.8-1.0）: 創造的、多様性
Temperature = 0.9f  // ブレインストーミング、アイデア生成
```

### MaxTokens

```csharp
// 簡潔な応答
MaxTokens = 500  // 200-300文字程度

// 標準サマリ
MaxTokens = 1500  // 500-700文字

// 詳細レポート
MaxTokens = 4000  // 2000文字以上
```

### TopP（Nucleus Sampling）

```csharp
// Temperature の代替
TopP = 0.95f  // 上位95%の確率の単語から選択
```

---

## ストリーミング応答

### CompleteStreamingAsync

```csharp
await foreach (var update in chatClient.CompleteStreamingAsync(messages, options))
{
    if (update.Text != null)
    {
        // 即座にユーザーに表示
        await turnContext.SendActivityAsync(update.Text);
    }
    
    if (update.FinishReason == ChatFinishReason.ToolCalls)
    {
        // ツール呼び出し検出
        foreach (var toolCall in update.ToolCalls)
        {
            var result = await ExecuteToolAsync(toolCall);
            messages.Add(new ChatMessage(ChatRole.Tool, result));
        }
        
        // ツール結果でLLM再呼び出し
        await foreach (var finalUpdate in chatClient.CompleteStreamingAsync(messages, options))
        {
            await turnContext.SendActivityAsync(finalUpdate.Text);
        }
    }
}
```

**ユーザー体験改善**:
```
非ストリーミング:
  ユーザー入力 → [3秒待機] → 完全な応答表示

ストリーミング:
  ユーザー入力 → [0.5秒] → "## 📊" → "サマリー\n" → "今週は..." → ...
                  リアルタイム表示
```

---

## エラーハンドリング

### HTTP エラー

```csharp
try
{
    var response = await chatClient.CompleteAsync(messages, options);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "LLMリクエストエラー: ネットワーク接続失敗");
    // リトライロジック
}
catch (TaskCanceledException ex)
{
    _logger.LogWarning(ex, "LLMリクエストタイムアウト");
    // タイムアウト延長またはエラー通知
}
```

### コンテンツフィルター

```csharp
catch (ServiceException ex) when (ex.Message.Contains("content_filter"))
{
    _logger.LogWarning("コンテンツフィルターによりブロック: {Message}", ex.Message);
    return "❌ 不適切な内容が検出されました。別の表現で試してください。";
}
```

---

## プロンプトエンジニアリング

### System Prompt 設計

```csharp
private const string SystemPrompt = @"あなたは営業支援エージェントです。

【役割】
Microsoft 365から商談関連情報を収集し、わかりやすくサマリを作成します。

【利用可能なツール】
1. SearchSalesEmails - Outlookメール検索
2. SearchSalesMeetings - カレンダー予定検索

【重要な指示】
- 複数のツールを組み合わせて包括的なサマリを作成
- 日本語で丁寧に回答
- 日付は yyyy/MM/dd 形式で表示
- 緊急度の高い情報を優先

【出力フォーマット】
## 📊 サマリー
全体概要を2-3文で

## 📧 商談メール
重要なメールを箇条書き

## 📅 商談予定
今後の予定を日付順に

## 💡 推奨アクション
次に取るべき具体的なアクション3-5個";
```

**ポイント**:
- ✅ 明確な役割定義
- ✅ 利用可能なツールの列挙
- ✅ 具体的な指示
- ✅ 出力フォーマット指定

---

## 次のステップ

- **[CONVERSATION-FLOW.md](CONVERSATION-FLOW.md)**: 会話フロー詳細
- **[06-SDK-INTEGRATION-PATTERNS.md](../06-SDK-INTEGRATION-PATTERNS.md)**: SDK統合パターン
- **[12-EXTENSIBILITY.md](../12-EXTENSIBILITY.md)**: 新しいLLMプロバイダー追加
