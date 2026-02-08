# 🔔 Notifications & 💬 Transcript 機能ガイド

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](en/NOTIFICATIONS-TRANSCRIPT-GUIDE.md)

## 📋 概要

Agent 365 SDK の**Notifications**と**Transcript & Storage**機能を統合した拡張デモです。

### 新機能

1. **🔔 リアルタイム通知（Notifications）**
   - エージェント処理の進捗状況をリアルタイム配信
   - 0%, 25%, 75%, 100%の進捗段階を可視化
   - 成功・エラー・警告通知の自動送信

2. **💬 会話履歴（Transcript & Storage）**
   - ユーザーとBotの全メッセージを自動記録
   - 会話ごとの履歴管理と検索
   - 統計情報（総会話数、総メッセージ数、アクティブ会話数）

---

## 🚀 クイックスタート

### 1. アプリケーション起動

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run
```

### 2. Observability Dashboard アクセス

```
http://localhost:5192/observability.html
```

**新しいパネル：**
- 🔔 **リアルタイム通知** - 進捗バー付き通知
- 💬 **会話履歴** - 会話一覧とメッセージ詳細

---

## 🎬 デモシナリオ

### シナリオ1: リアルタイム通知体験

**目的**: SignalRによる即時通知配信を体験

**手順**:

1. **Dashboard準備**
   - `http://localhost:5192/observability.html` を開く
   - 通知パネルが「通知データを待機中...」と表示されることを確認

2. **Bot操作**
   - Web Chat（`http://localhost:5192`）を開く
   - 「今週の商談状況を教えてください」と入力

3. **通知確認**
   - Dashboard「🔔 リアルタイム通知」パネルで以下をリアルタイム確認：
     ```
     🚀 商談サマリ生成を開始しています...        [0%]
     📊 データ収集中（メール、カレンダー、ドキュメント）... [25%]
     🤖 AI分析中（サマリ生成処理）...           [75%]
     ✅ 商談サマリ生成完了！（処理時間: 6500ms）  [100%]
     ```

4. **進捗バー**
   - 各通知に青色の進捗バーが表示
   - 0% → 25% → 75% → 100% のアニメーション確認

**フォーカルポイント**:
- ✨ **遅延ゼロ**: Botが処理中でもDashboardに即座に通知
- ✨ **段階的進捗**: ユーザーはエージェントの処理状況を把握
- ✨ **色分けステータス**: success（緑）、error（赤）、warning（黄）

---

### シナリオ2: 会話履歴管理

**目的**: 会話記録と履歴検索機能を体験

**手順**:

1. **会話実行**
   - Web Chatで3つのメッセージを送信：
     - 「今週の商談状況を教えてください」
     - 「次の予定は？」
     - 「最新のメールは？」

2. **会話一覧確認**
   - Dashboard「💬 会話履歴」パネルを確認
   - 会話ID、メッセージ数、最終アクティビティ時刻が表示

3. **会話詳細表示**
   - 会話をクリック
   - ユーザーメッセージ（青色背景）とBot応答（紫色背景）が時系列表示

4. **統計API確認**
   ```bash
   curl http://localhost:5192/api/transcript/statistics
   ```
   
   **期待される応答**:
   ```json
   {
     "totalConversations": 1,
     "totalMessages": 6,
     "activeConversations": 1,
     "averageMessagesPerConversation": 6.0
   }
   ```

**フォーカルポイント**:
- ✨ **完全な記録**: すべてのユーザー・Bot間のやり取りを保存
- ✨ **検索可能**: 会話IDで過去の履歴を即座に検索
- ✨ **プライバシー対応**: 会話削除API（GDPR対応）

---

### シナリオ3: エラー時の通知

**目的**: エラー時の自動通知とアラート機能

**手順**:

1. **エラー条件作成**
   - Ollamaを一時停止:
     ```bash
     pkill ollama
     ```

2. **エラーリクエスト**
   - Web Chatで「今週の商談状況を教えてください」と入力

3. **エラー通知確認**
   - Dashboard「🔔 リアルタイム通知」パネルで赤色のエラー通知:
     ```
     ❌ 商談サマリ生成に失敗しました
     Error: HTTP request failed
     ```

4. **復旧**
   - Ollama再起動:
     ```bash
     ollama serve
     ```
   - 正常リクエスト実行で成功通知確認

**フォーカルポイント**:
- ✨ **即時エラー通知**: 問題発生時に赤色アラート
- ✨ **詳細エラー情報**: エラーメッセージと詳細を表示
- ✨ **自動復旧追跡**: 復旧後の成功通知で状態確認

---

## 📊 API リファレンス

### Notifications API

#### 1. 通知履歴取得
```bash
GET /api/notifications/history?count=20
```

**レスポンス例**:
```json
[
  {
    "id": "abc123",
    "operationId": "op-456",
    "type": "success",
    "message": "✅ 商談サマリ生成完了！（処理時間: 6500ms）",
    "progressPercentage": 100,
    "timestamp": "2026-02-07T10:30:00Z",
    "severity": "success",
    "data": {
      "processingTimeMs": 6500,
      "dataSourceCount": 4
    }
  }
]
```

#### 2. 操作別通知取得
```bash
GET /api/notifications/operation/{operationId}
```

---

### Transcript API

#### 1. 会話一覧取得
```bash
GET /api/transcript/conversations
```

**レスポンス例**:
```json
[
  {
    "conversationId": "conv-123-456",
    "messageCount": 6,
    "lastActivity": "2026-02-07T10:35:00Z",
    "participants": ["User", "SalesSupportAgent"]
  }
]
```

#### 2. 会話履歴取得
```bash
GET /api/transcript/history/{conversationId}?limit=50
```

**レスポンス例**:
```json
[
  {
    "id": "msg-1",
    "conversationId": "conv-123-456",
    "type": "message",
    "from": "User",
    "text": "今週の商談状況を教えてください",
    "timestamp": "2026-02-07T10:30:00Z",
    "channelId": "webchat"
  },
  {
    "id": "msg-2",
    "conversationId": "conv-123-456",
    "type": "message",
    "from": "SalesSupportAgent",
    "text": "[Adaptive Card Response] ## 📊 サマリー...",
    "timestamp": "2026-02-07T10:30:06Z",
    "channelId": "webchat"
  }
]
```

#### 3. 統計情報取得
```bash
GET /api/transcript/statistics
```

**レスポンス例**:
```json
{
  "totalConversations": 5,
  "totalMessages": 24,
  "activeConversations": 2,
  "averageMessagesPerConversation": 4.8
}
```

#### 4. 会話削除
```bash
DELETE /api/transcript/history/{conversationId}
```

---

## 🔧 技術実装詳細

### NotificationService

**場所**: `Services/Notifications/NotificationService.cs`

**主要メソッド**:
- `SendProgressNotificationAsync()` - 進捗通知（0-100%）
- `SendSuccessNotificationAsync()` - 成功通知
- `SendErrorNotificationAsync()` - エラー通知
- `SendWarningNotificationAsync()` - 警告通知
- `GetNotificationHistory()` - 通知履歴取得（最新50件保持）

**SignalR統合**:
```csharp
await _hubContext.Clients.All.SendAsync("NotificationUpdate", notification);
```

---

### TranscriptService

**場所**: `Services/Transcript/TranscriptService.cs`

**主要メソッド**:
- `LogActivityAsync()` - 会話アクティビティ記録
- `GetConversationHistoryAsync()` - 会話履歴取得
- `GetAllConversations()` - 全会話一覧
- `GetStatistics()` - 統計情報
- `DeleteConversationHistoryAsync()` - 会話削除（GDPR対応）

**ストレージ統合**:
```csharp
// Microsoft.Agents.Storage.IStorage使用
var storageKey = $"transcript:{conversationId}:{entry.Id}";
await _storage.WriteAsync(storeItems);
```

**キャッシュ戦略**:
- ConcurrentDictionary<string, List<TranscriptEntry>>でメモリキャッシュ
- 最大100件の会話をキャッシュ保持
- ストレージへの非同期書き込み

---

### SalesAgent統合

**変更内容**:

```csharp
// コンストラクタにNotificationService追加
public SalesAgent(
    ...,
    NotificationService notificationService,
    ...)

// GenerateSalesSummaryAsync内で通知送信
await _notificationService.SendProgressNotificationAsync(operationId, "🚀 商談サマリ生成を開始しています...", 0);
await _notificationService.SendProgressNotificationAsync(operationId, "📊 データ収集中...", 25);
await _notificationService.SendProgressNotificationAsync(operationId, "🤖 AI分析中...", 75);
await _notificationService.SendSuccessNotificationAsync(operationId, "✅ 完了！", data);
```

---

### TeamsBot統合

**変更内容**:

```csharp
// コンストラクタにTranscriptService追加
public TeamsBot(
    SalesAgent salesAgent,
    TranscriptService transcriptService,
    ...)

// メッセージ送受信時に記録
await _transcriptService.LogActivityAsync(turnContext.Activity, conversationId);
await _transcriptService.LogActivityAsync(botActivity, conversationId);
```

---

## 💡 ベストプラクティス

### 1. 通知の適切な使用

**推奨**:
- 長時間処理（5秒以上）で進捗通知を送信
- 4-5段階の進捗ステップ（0%, 33%, 66%, 100%など）
- 成功時は具体的な結果情報を含める

**非推奨**:
- 短時間処理（1秒未満）での通知乱発
- 意味のない進捗更新（1%ずつなど）

### 2. 会話履歴のプライバシー

**GDPR対応**:
```csharp
// ユーザー要求による会話削除
await transcriptService.DeleteConversationHistoryAsync(conversationId);
```

**推奨設定**:
- 本番環境では暗号化ストレージ使用
- 個人情報を含む場合はPII（個人識別情報）フィルタリング
- 保持期間ポリシー設定（例: 30日後自動削除）

### 3. パフォーマンス最適化

**キャッシュ戦略**:
- メモリ: 最新100会話をConcurrentDictionaryでキャッシュ
- ストレージ: 非同期書き込みでボトルネック回避

**SignalR最適化**:
- 通知頻度制限（1秒に最大10通知）
- クライアント側でバッチ処理

---

## 🎯 次のステップ

### Phase 1 完了 ✅
- ✅ Observability（トレース、メトリクス）
- ✅ Notifications（進捗通知）
- ✅ Transcript & Storage（会話履歴）

### Phase 2（オプション拡張）

1. **Application Insights統合** - エンタープライズ監視
2. **Blob Storage統合** - 永続化ストレージ
3. **PII Filtering** - 個人情報自動マスキング
4. **カスタムメトリクス** - ビジネスKPI追加
5. **アラートルール** - 閾値ベース自動アラート

---

## 📚 参考資料

- [Microsoft Agent 365 SDK - Notifications](https://github.com/microsoft/agent-framework/tree/main/src/packages/Notifications)
- [Microsoft Agent 365 SDK - Storage](https://github.com/microsoft/agent-framework/tree/main/src/packages/Storage)
- [Bot Framework Storage](https://learn.microsoft.com/en-us/azure/bot-service/bot-builder-concept-state)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)

---

**作成日**: 2026年2月7日  
**バージョン**: 2.0  
**対象**: Agent 365 SDK Notifications & Transcript Platform Demo
