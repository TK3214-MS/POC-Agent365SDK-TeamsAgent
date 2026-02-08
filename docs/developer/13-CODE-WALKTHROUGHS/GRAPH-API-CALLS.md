# Graph API Calls - Graph API呼び出しパターン詳細

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](GRAPH-API-CALLS.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../../en/developer/13-CODE-WALKTHROUGHS/GRAPH-API-CALLS.md)

## 📋 パターン別呼び出し

### メール検索

#### 基本パターン

```csharp
var messages = await _graphClient.Users[_userId].Messages
    .GetAsync(config =>
    {
        config.QueryParameters.Filter = "receivedDateTime ge 2026-02-01T00:00:00Z";
        config.QueryParameters.Top = 10;
        config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };
        config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
    });
```

**生成されるHTTPリクエスト**:
```http
GET /v1.0/users/user@company.com/messages?
  $filter=receivedDateTime ge 2026-02-01T00:00:00Z
  &$top=10
  &$select=subject,from,receivedDateTime
  &$orderby=receivedDateTime desc
Authorization: Bearer eyJ0eXAiOiJKV1Qi...
```

#### 高度なフィルタリング

```csharp
// AND条件
config.QueryParameters.Filter = 
    "receivedDateTime ge 2026-02-01T00:00:00Z " +
    "and receivedDateTime le 2026-02-07T23:59:59Z " +
    "and hasAttachments eq true";

// OR条件（カテゴリ）
config.QueryParameters.Filter = 
    "categories/any(c: c eq '商談' or c eq '提案')";

// NOT条件
config.QueryParameters.Filter = 
    "not(isDraft eq true)";
```

### カレンダー検索

```csharp
var events = await _graphClient.Users[_userId].Calendar.Events
    .GetAsync(config =>
    {
        config.QueryParameters.Filter = 
            "start/dateTime ge '2026-02-03T00:00:00' " +
            "and end/dateTime le '2026-02-09T23:59:59'";
        config.QueryParameters.Select = new[] 
        {
            "subject", "start", "end", "attendees", "location"
        };
        config.QueryParameters.Orderby = new[] { "start/dateTime" };
    });
```

**JSONレスポンス**:
```json
{
  "value": [
    {
      "subject": "株式会社A社 商談",
      "start": {
        "dateTime": "2026-02-05T14:00:00",
        "timeZone": "Tokyo Standard Time"
      },
      "end": {
        "dateTime": "2026-02-05T15:00:00",
        "timeZone": "Tokyo Standard Time"
      },
      "attendees": [
        {
          "emailAddress": {
            "name": "田中太郎",
            "address": "tanaka@company.com"
          },
          "type": "required"
        }
      ],
      "location": {
        "displayName": "会議室A"
      }
    }
  ]
}
```

### SharePoint 検索

```csharp
var items = await _graphClient.Users[_userId].Drive.Root
    .Search("提案書")
    .GetAsync(config =>
    {
        config.QueryParameters.Top = 20;
        config.QueryParameters.Select = new[] 
        {
            "name", "webUrl", "lastModifiedDateTime", "size"
        };
    });
```

**レスポンス処理**:
```csharp
foreach (var item in items.Value)
{
    Console.WriteLine($"📄 {item.Name}");
    Console.WriteLine($"   URL: {item.WebUrl}");
    Console.WriteLine($"   更新: {item.LastModifiedDateTime:yyyy/MM/dd}");
    Console.WriteLine($"   サイズ: {item.Size / 1024}KB");
}
```

---

## パフォーマンス最適化

### 1. Select 最小化

```csharp
// ❌ BAD - 全フィールド（レスポンス10KB）
var messages = await _graphClient.Users[_userId].Messages.GetAsync();

// ✅ GOOD - 必要フィールドのみ（レスポンス2KB）
config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };
```

**効果**: レスポンスサイズ 80%削減、転送時間 75%短縮

### 2. Top 制限

```csharp
// 最初の10件で十分な場合
config.QueryParameters.Top = 10;
```

**効果**: API処理時間 60%短縮

### 3. Filter サーバー側実行

```csharp
// ❌ BAD - 全件取得後にクライアント側フィルタリング
var allMessages = await _graphClient.Users[_userId].Messages.GetAsync();
var filtered = allMessages.Value.Where(m => m.Subject.Contains("商談"));

// ✅ GOOD - サーバー側フィルタリング
config.QueryParameters.Filter = "contains(subject, '商談')";
```

**効果**: データ転送量 90%削減

---

## バッチリクエスト

### 複数API呼び出しの集約

```csharp
var batchRequestContent = new BatchRequestContentCollection(_graphClient);

// リクエスト1: メール
var messagesRequest = _graphClient.Users[_userId].Messages
    .ToGetRequestInformation(config =>
    {
        config.QueryParameters.Top = 10;
        config.QueryParameters.Select = new[] { "subject", "from" };
    });
var messagesStepId = await batchRequestContent.AddBatchRequestStepAsync(messagesRequest);

// リクエスト2: カレンダー
var eventsRequest = _graphClient.Users[_userId].Calendar.Events
    .ToGetRequestInformation(config =>
    {
        config.QueryParameters.Top = 5;
    });
var eventsStepId = await batchRequestContent.AddBatchRequestStepAsync(eventsRequest);

// バッチ実行（1回のHTTPリクエスト）
var batchResponse = await _graphClient.Batch.PostAsync(batchRequestContent);

// 結果取得
var messages = await batchResponse.GetResponseByIdAsync<MessageCollectionResponse>(messagesStepId);
var events = await batchResponse.GetResponseByIdAsync<EventCollectionResponse>(eventsStepId);
```

**パフォーマンス比較**:
```
シーケンシャル（2回のHTTPリクエスト）:
  Request 1: Messages (500ms)
  Request 2: Events   (400ms)
  総時間: 900ms

バッチ（1回のHTTPリクエスト）:
  Batch Request: (600ms)
  総時間: 600ms（33%高速化）
```

---

## エラーハンドリング

### ServiceException 処理

```csharp
try
{
    var messages = await _graphClient.Users[_userId].Messages.GetAsync();
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 401)
{
    _logger.LogError("認証エラー: トークンが無効または期限切れ");
    // TokenCredential が自動リフレッシュするため、通常は発生しない
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 403)
{
    _logger.LogError("権限不足: Mail.Read 権限がありません");
    // Azure ADアプリ登録で権限を追加し、管理者同意を取得
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 404)
{
    _logger.LogWarning("ユーザーが見つかりません: {UserId}", _userId);
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 429)
{
    var retryAfter = ex.ResponseHeaders?.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
    _logger.LogWarning("レート制限: {Seconds}秒後に再試行", retryAfter.TotalSeconds);
    await Task.Delay(retryAfter);
    // リトライ
}
catch (ServiceException ex)
{
    _logger.LogError(ex, "Graph APIエラー: {Code}", ex.ResponseStatusCode);
}
```

### リトライポリシー（Polly）

```csharp
var retryPolicy = Policy
    .Handle<ServiceException>(ex => 
        ex.ResponseStatusCode == 429 ||  // レート制限
        ex.ResponseStatusCode >= 500)    // サーバーエラー
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning(
                "リトライ {RetryCount}/3: {Delay}秒後に再試行",
                retryCount,
                timeSpan.TotalSeconds
            );
        }
    );

var messages = await retryPolicy.ExecuteAsync(async () =>
{
    return await _graphClient.Users[_userId].Messages.GetAsync();
});
```

---

## レート制限対策

### スロットリング検出

```csharp
var response = await _graphClient.Users[_userId].Messages.GetAsync();

// レスポンスヘッダーからレート制限情報を取得
if (response.OdataNextLink != null)
{
    // ページネーションが必要
    _logger.LogInformation("NextLink あり: さらにデータが存在");
}

// Retry-After ヘッダー確認（例外時のみ）
catch (ServiceException ex) when (ex.ResponseStatusCode == 429)
{
    var retryAfter = ex.ResponseHeaders?.RetryAfter;
    if (retryAfter?.Delta.HasValue == true)
    {
        await Task.Delay(retryAfter.Delta.Value);
    }
}
```

### リクエスト間隔調整

```csharp
private readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(5, 5);  // 同時5リクエストまで

public async Task<MessageCollectionResponse> GetMessagesWithRateLimitAsync()
{
    await _rateLimiter.WaitAsync();
    try
    {
        return await _graphClient.Users[_userId].Messages.GetAsync();
    }
    finally
    {
        await Task.Delay(200);  // 200ms待機
        _rateLimiter.Release();
    }
}
```

---

## ページネーション

### OData NextLink処理

```csharp
var allMessages = new List<Message>();
var response = await _graphClient.Users[_userId].Messages.GetAsync(config =>
{
    config.QueryParameters.Top = 50;
});

allMessages.AddRange(response.Value);

// NextLinkが存在する限りページネーション
while (response.OdataNextLink != null)
{
    var nextPageRequest = new HttpRequestMessage(HttpMethod.Get, response.OdataNextLink);
    response = await _graphClient.RequestAdapter.SendAsync(
        nextPageRequest,
        MessageCollectionResponse.CreateFromDiscriminatorValue
    );
    
    allMessages.AddRange(response.Value);
    
    _logger.LogInformation("ページ取得: 累計 {Count}件", allMessages.Count);
}

_logger.LogInformation("全件取得完了: {TotalCount}件", allMessages.Count);
```

---

## 次のステップ

- **[CONVERSATION-FLOW.md](CONVERSATION-FLOW.md)**: 会話フロー詳細
- **[03-AUTHENTICATION-FLOW.md](../03-AUTHENTICATION-FLOW.md)**: 認証フロー
- **[04-DATA-FLOW.md](../04-DATA-FLOW.md)**: データフロー
