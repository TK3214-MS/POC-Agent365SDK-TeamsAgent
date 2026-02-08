# Observability Dashboard ガイド

**リアルタイムでエージェント動作を可視化** - エージェントの内部動作、会話フロー、パフォーマンスをダッシュボードで監視

---

## 📋 概要

Observability Dashboardは、営業支援エージェントの動作をリアルタイムで可視化するWebベースの監視ツールです。SignalRを利用したリアルタイム通信により、エージェントの状態、会話時系列、AI推論プロセス、パフォーマンスメトリクスを即座に確認できます。

### 💡 主な機能

| 機能 | 説明 |
|-----|------|
| 🔴 **リアルタイム監視** | SignalRでエージェント動作を即座に反映 |
| 📊 **エージェント状態表示** | アクティブ/アイドル状態、最終アクティビティ時刻 |
| 💬 **会話タイムライン** | ユーザーとエージェントのやり取りを時系列表示 |
| 🔍 **詳細フェーズ表示** | AI推論の内部ステップを可視化 |
| 📈 **メトリクス表示** | 応答時間、API呼び出し回数、成功率 |
| 🎨 **Fluent UI統合** | Microsoftデザインシステム準拠のモダンUI |

### 🎯 ビジネス価値

- **トラブルシューティング効率化**: エージェントの動作を即座に確認し、問題を迅速に特定
- **パフォーマンス最適化**: ボトルネックを可視化し、改善ポイントを発見
- **透明性の確保**: AIの推論プロセスを可視化し、説明責任を果たす
- **開発効率向上**: デバッグ時間を短縮し、開発サイクルを加速

---

## 🚀 クイックスタート

### アクセス方法

```bash
# アプリケーションを起動
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run

# ブラウザでアクセス
open https://localhost:5192/observability.html
```

**URL**: `https://localhost:5192/observability.html`

### 初回アクセス時

1. ブラウザで上記URLにアクセス
2. 自己署名証明書の警告が表示される場合は「詳細」→「続行」
3. ダッシュボードが表示され、SignalR接続が自動的に確立されます
4. 接続状態が画面右上に表示されます（緑: 接続中、赤: 切断）

---

## 🏗️ アーキテクチャ

### システム構成

```
┌─────────────────────────────────────────────┐
│         ブラウザ (observability.html)        │
│  ┌──────────────────────────────────────┐   │
│  │  Vue 3 + Fluent UI System Icons      │   │
│  │  - リアルタイムUI更新                 │   │
│  │  - 会話タイムライン表示               │   │
│  │  - メトリクスビジュアライゼーション    │   │
│  └──────────────┬───────────────────────┘   │
└─────────────────┼───────────────────────────┘
                  │ SignalR (WebSocket)
                  ▼
┌─────────────────────────────────────────────┐
│      営業支援エージェント (.NET 10)          │
│  ┌──────────────────────────────────────┐   │
│  │  ObservabilityHub (SignalR Hub)      │   │
│  │  - リアルタイムイベント配信           │   │
│  │  - 接続管理                          │   │
│  └──────────────┬───────────────────────┘   │
│                 │                            │
│  ┌──────────────▼───────────────────────┐   │
│  │  Agent Telemetry                     │   │
│  │  - AgentMetrics (OpenTelemetry)     │   │
│  │  - ActivitySource, Meter, Counter   │   │
│  │  - Transcript Logging               │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

### SignalR Hub

**エンドポイント**: `/hubs/observability`

```csharp
public class ObservabilityHub : Hub
{
    // クライアントへのイベント送信
    await Clients.All.SendAsync("AgentStatusUpdated", status);
    await Clients.All.SendAsync("ConversationUpdated", conversation);
    await Clients.All.SendAsync("PhaseUpdated", phase);
}
```

---

## 📊 ダッシュボードUI

### 1. エージェント状態パネル

```
┌─────────────────────────────────────────┐
│ 🤖 エージェント状態                      │
├─────────────────────────────────────────┤
│ 状態: ● アクティブ                       │
│ 最終アクティビティ: 2026-02-08 14:30:25 │
│ 総会話数: 15                             │
│ 平均応答時間: 2.3秒                      │
└─────────────────────────────────────────┘
```

**表示項目**:
- **状態**: アクティブ（緑）/ アイドル（灰）/ エラー（赤）
- **最終アクティビティ**: 最後にエージェントが動作した時刻
- **総会話数**: 起動後の総会話数
- **平均応答時間**: 全会話の平均応答時間

### 2. 会話タイムライン

```
┌─────────────────────────────────────────┐
│ 💬 会話タイムライン                      │
├─────────────────────────────────────────┤
│ [14:30] ユーザー                         │
│ └ 今週の商談サマリを教えて                │
│                                          │
│ [14:30] エージェント (処理中...)          │
│ ├ [Phase 1] メール検索開始                │
│ ├ [Phase 2] カレンダー検索開始            │
│ ├ [Phase 3] SharePoint検索開始           │
│ ├ [Phase 4] AI統合レポート生成            │
│ └ [完了] Adaptive Card送信               │
│                                          │
│ [14:31] エージェント                      │
│ └ [商談サマリを表示]                      │
└─────────────────────────────────────────┘
```

**表示内容**:
- ユーザーメッセージ（青背景）
- エージェント応答（緑背景）
- 処理フェーズ（展開可能）
- タイムスタンプ（時:分:秒）
- エラーメッセージ（赤背景）

### 3. 詳細フェーズ表示

各会話の「詳細を表示」ボタンをクリックすると展開:

```
┌─────────────────────────────────────────┐
│ 🔍 フェーズ詳細: 今週の商談サマリを教えて │
├─────────────────────────────────────────┤
│ Phase 1: メール検索                      │
│ ├ 開始: 14:30:25.123                     │
│ ├ 終了: 14:30:26.456                     │
│ ├ 所要時間: 1.33秒                        │
│ ├ ステータス: ✅ 成功                     │
│ └ 結果: 15件のメールを取得                │
│                                          │
│ Phase 2: カレンダー検索                  │
│ ├ 開始: 14:30:26.500                     │
│ ├ 終了: 14:30:27.200                     │
│ ├ 所要時間: 0.70秒                        │
│ ├ ステータス: ✅ 成功                     │
│ └ 結果: 8件の予定を取得                   │
│                                          │
│ Phase 3: SharePoint検索                  │
│ ├ 開始: 14:30:27.250                     │
│ ├ 終了: 14:30:28.100                     │
│ ├ 所要時間: 0.85秒                        │
│ ├ ステータス: ✅ 成功                     │
│ └ 結果: 12件のドキュメントを取得          │
│                                          │
│ Phase 4: AI推論                          │
│ ├ 開始: 14:30:28.150                     │
│ ├ 終了: 14:30:30.500                     │
│ ├ 所要時間: 2.35秒                        │
│ ├ ステータス: ✅ 成功                     │
│ ├ LLM: Azure OpenAI (gpt-4o)            │
│ ├ トークン使用: 1,250 (入力) + 450 (出力) │
│ └ 結果: 統合レポート生成完了              │
└─────────────────────────────────────────┘
```

### 4. メトリクスパネル

```
┌─────────────────────────────────────────┐
│ 📈 パフォーマンスメトリクス               │
├─────────────────────────────────────────┤
│ API呼び出し統計 (過去1時間)              │
│ ├ Graph API: 45回 (成功: 44, 失敗: 1)   │
│ ├ LLM API: 15回 (成功: 15, 失敗: 0)     │
│ └ 平均応答時間: 1.2秒                     │
│                                          │
│ トークン使用統計                         │
│ ├ 総トークン: 18,750                     │
│ ├ 入力トークン: 12,500 (平均: 833/会話)  │
│ └ 出力トークン: 6,250 (平均: 417/会話)   │
│                                          │
│ エラー率                                 │
│ ├ 全体: 2.2% (1/45)                      │
│ ├ 認証エラー: 0                          │
│ └ タイムアウト: 1                        │
└─────────────────────────────────────────┘
```

---

## 🔧 SignalR統合

### クライアント側（JavaScript）

```javascript
// SignalR接続確立
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/observability")
    .withAutomaticReconnect()
    .build();

// イベントハンドラ登録
connection.on("AgentStatusUpdated", (status) => {
    console.log("Agent Status:", status);
    updateAgentStatus(status);
});

connection.on("ConversationUpdated", (conversation) => {
    console.log("Conversation:", conversation);
    addConversationToTimeline(conversation);
});

connection.on("PhaseUpdated", (phase) => {
    console.log("Phase:", phase);
    updatePhaseDetails(phase);
});

// 接続開始
await connection.start();
console.log("SignalR Connected");
```

### サーバー側（C#）

```csharp
// ObservabilityHub.cs
public class ObservabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", 
            new { Message = "Dashboard connected" });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // クリーンアップ処理
        await base.OnDisconnectedAsync(exception);
    }
}

// エージェントからのイベント送信
public class AgentObservabilityService
{
    private readonly IHubContext<ObservabilityHub> _hubContext;

    public async Task NotifyAgentStatus(AgentStatus status)
    {
        await _hubContext.Clients.All.SendAsync(
            "AgentStatusUpdated", 
            status
        );
    }

    public async Task NotifyConversation(Conversation conversation)
    {
        await _hubContext.Clients.All.SendAsync(
            "ConversationUpdated", 
            conversation
        );
    }

    public async Task NotifyPhase(PhaseInfo phase)
    {
        await _hubContext.Clients.All.SendAsync(
            "PhaseUpdated", 
            phase
        );
    }
}
```

---

## 🎨 UI実装詳細

### Fluent UI System Icons統合

```html
<!-- Fluent UI System Icons CDN -->
<link rel="stylesheet" 
      href="https://cdn.jsdelivr.net/npm/@fluentui/svg-icons/icons/index.css">

<!-- アイコン使用例 -->
<span class="fluent-icon">
    <svg class="fluent-icon-calendar">
        <use href="#fluent-calendar-24-regular"></use>
    </svg>
</span>
```

**使用アイコン**:
- `fluent-bot-24-regular`: エージェントアイコン
- `fluent-calendar-24-regular`: カレンダー
- `fluent-mail-24-regular`: メール
- `fluent-folder-24-regular`: SharePoint
- `fluent-people-team-24-regular`: Teams
- `fluent-checkmark-circle-24-regular`: 成功
- `fluent-error-circle-24-regular`: エラー
- `fluent-spinner-24-regular`: 処理中

### Vue 3実装

```javascript
const { createApp } = Vue;

createApp({
    data() {
        return {
            agentStatus: {
                state: 'idle',
                lastActivity: null,
                totalConversations: 0,
                averageResponseTime: 0
            },
            conversations: [],
            selectedConversation: null,
            connection: null,
            isConnected: false
        };
    },
    
    async mounted() {
        await this.initializeSignalR();
    },
    
    methods: {
        async initializeSignalR() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/observability")
                .withAutomaticReconnect()
                .build();
            
            this.connection.on("AgentStatusUpdated", this.handleStatusUpdate);
            this.connection.on("ConversationUpdated", this.handleConversationUpdate);
            this.connection.on("PhaseUpdated", this.handlePhaseUpdate);
            
            await this.connection.start();
            this.isConnected = true;
        },
        
        handleStatusUpdate(status) {
            this.agentStatus = status;
        },
        
        handleConversationUpdate(conversation) {
            this.conversations.unshift(conversation);
            if (this.conversations.length > 50) {
                this.conversations.pop();
            }
        },
        
        handlePhaseUpdate(phase) {
            const conversation = this.conversations.find(
                c => c.id === phase.conversationId
            );
            if (conversation) {
                if (!conversation.phases) {
                    conversation.phases = [];
                }
                conversation.phases.push(phase);
            }
        },
        
        selectConversation(conversation) {
            this.selectedConversation = conversation;
        },
        
        formatTimestamp(timestamp) {
            return new Date(timestamp).toLocaleTimeString('ja-JP');
        },
        
        formatDuration(milliseconds) {
            return (milliseconds / 1000).toFixed(2) + '秒';
        }
    }
}).mount('#app');
```

---

## 📈 メトリクス収集

### OpenTelemetry統合

```csharp
// AgentMetrics.cs
public class AgentMetrics
{
    private static readonly ActivitySource ActivitySource = 
        new("SalesSupportAgent");
    
    private static readonly Meter Meter = 
        new("SalesSupportAgent");
    
    private readonly Counter<long> _conversationCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly Counter<long> _errorCounter;
    
    public AgentMetrics()
    {
        _conversationCounter = Meter.CreateCounter<long>(
            "agent.conversations.total",
            description: "Total number of conversations"
        );
        
        _responseTimeHistogram = Meter.CreateHistogram<double>(
            "agent.response.time",
            description: "Agent response time in seconds"
        );
        
        _errorCounter = Meter.CreateCounter<long>(
            "agent.errors.total",
            description: "Total number of errors"
        );
    }
    
    public void RecordConversation(string result)
    {
        _conversationCounter.Add(1, 
            new KeyValuePair<string, object?>("result", result));
    }
    
    public void RecordResponseTime(double seconds, string phase)
    {
        _responseTimeHistogram.Record(seconds,
            new KeyValuePair<string, object?>("phase", phase));
    }
    
    public void RecordError(string errorType)
    {
        _errorCounter.Add(1,
            new KeyValuePair<string, object?>("type", errorType));
    }
}
```

### メトリクス送信

```csharp
// SalesAgent.cs
public async Task<string> ProcessQueryAsync(string query)
{
    var stopwatch = Stopwatch.StartNew();
    
    using var activity = ActivitySource.StartActivity("ProcessQuery");
    activity?.SetTag("query.length", query.Length);
    
    try
    {
        // フェーズ1: メール検索
        var emailStopwatch = Stopwatch.StartNew();
        var emails = await SearchEmailsAsync(query);
        emailStopwatch.Stop();
        
        _metrics.RecordResponseTime(
            emailStopwatch.Elapsed.TotalSeconds, 
            "email_search"
        );
        
        await _observabilityService.NotifyPhase(new PhaseInfo
        {
            Name = "メール検索",
            Duration = emailStopwatch.Elapsed,
            Status = "success",
            ResultCount = emails.Count
        });
        
        // フェーズ2: カレンダー検索
        // ... (同様のパターン)
        
        // フェーズ3: AI推論
        var aiStopwatch = Stopwatch.StartNew();
        var response = await GenerateResponseAsync(emails, events, docs);
        aiStopwatch.Stop();
        
        _metrics.RecordResponseTime(
            aiStopwatch.Elapsed.TotalSeconds,
            "ai_inference"
        );
        
        stopwatch.Stop();
        _metrics.RecordConversation("success");
        
        activity?.SetTag("total.duration", stopwatch.Elapsed.TotalSeconds);
        
        return response;
    }
    catch (Exception ex)
    {
        _metrics.RecordError(ex.GetType().Name);
        _metrics.RecordConversation("error");
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

---

## 🔍 デバッグ機能

### ブラウザ開発者ツール

**Console出力例**:
```javascript
[SignalR] Connected to /hubs/observability
[Agent] Status Updated: { state: 'active', ... }
[Conversation] New message: { id: '123', user: '...' }
[Phase] Email search completed in 1.33s
[Phase] Calendar search completed in 0.70s
[Phase] SharePoint search completed in 0.85s
[Phase] AI inference completed in 2.35s
```

### ネットワーク監視

SignalR接続の確認:
1. 開発者ツール → Network タブ
2. WebSocketフィルタを有効化
3. `observability` 接続を確認
4. メッセージフレームを確認

### パフォーマンスプロファイリング

```javascript
// パフォーマンス測定
performance.mark('conversation-start');

// ... 処理 ...

performance.mark('conversation-end');
performance.measure(
    'conversation-duration',
    'conversation-start',
    'conversation-end'
);

const measure = performance.getEntriesByName('conversation-duration')[0];
console.log(`Duration: ${measure.duration}ms`);
```

---

## ⚙️ 設定

### appsettings.json

```json
{
  "Observability": {
    "Enabled": true,
    "SignalRHubPath": "/hubs/observability",
    "MaxConversationsInMemory": 100,
    "MetricsRetentionMinutes": 60,
    "EnableDetailedPhases": true,
    "EnableTokenCounting": true
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30",
    "HandshakeTimeout": "00:00:15"
  }
}
```

### Program.cs

```csharp
// SignalR追加
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// ObservabilityHub登録
app.MapHub<ObservabilityHub>("/hubs/observability");

// 静的ファイル配信（observability.html）
app.UseStaticFiles();
```

---

## 🛡️ セキュリティ

### 認証・認可

```csharp
// 本番環境では認証を追加
[Authorize]
public class ObservabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }
        
        // 管理者権限チェック
        if (!Context.User.IsInRole("Admin"))
        {
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
    }
}
```

### CORS設定

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ObservabilityPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:5192")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("ObservabilityPolicy");
```

### データフィルタリング

```csharp
// PII（個人情報）をマスク
public class PIIFilter
{
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        
        var parts = email.Split('@');
        if (parts.Length != 2) return "***@***";
        
        return $"{parts[0][0]}***@{parts[1]}";
    }
    
    public static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        
        return new string('*', phone.Length - 4) + phone.Substring(phone.Length - 4);
    }
}

// 使用例
await _observabilityService.NotifyPhase(new PhaseInfo
{
    Name = "メール検索",
    ResultSummary = $"Found {emails.Count} emails from {PIIFilter.MaskEmail(sender)}"
});
```

---

## ⚠️ トラブルシューティング

### SignalR接続できない

**症状**: ダッシュボードに「切断」と表示される

**原因と解決策**:

| 原因 | 解決方法 |
|-----|---------|
| アプリケーションが起動していない | `dotnet run` でアプリケーションを起動 |
| パスが間違っている | `/hubs/observability` が正しいか確認 |
| CORS設定エラー | Program.csでCORS設定を確認 |
| ファイアウォール | ポート5192が開いているか確認 |

**デバッグ手順**:
```javascript
// ブラウザコンソールでエラー確認
connection.onclose((error) => {
    console.error('SignalR connection closed:', error);
});

connection.onreconnecting((error) => {
    console.warn('SignalR reconnecting:', error);
});

connection.onreconnected((connectionId) => {
    console.log('SignalR reconnected:', connectionId);
});
```

### イベントが受信できない

**症状**: ダッシュボードが更新されない

**解決策**:
1. SignalR接続状態を確認（緑ランプ）
2. ブラウザコンソールでエラーログを確認
3. サーバー側のログを確認（`dotnet run`の出力）
4. イベントハンドラが正しく登録されているか確認

```javascript
// イベントハンドラのデバッグ
connection.on("AgentStatusUpdated", (status) => {
    console.log("✅ AgentStatusUpdated received:", status);
});
```

### パフォーマンスが遅い

**症状**: ダッシュボードの更新が遅延する

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| 過剰なログ出力 | `EnableDetailedPhases: false` に設定 |
| メモリ内会話が多すぎる | `MaxConversationsInMemory` を50に削減 |
| ネットワーク遅延 | KeepAliveIntervalを30秒に延長 |

```json
{
  "Observability": {
    "MaxConversationsInMemory": 50,
    "EnableDetailedPhases": false
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:30"
  }
}
```

---

## 📚 関連ドキュメント

- [トラブルシューティングガイド](TROUBLESHOOTING.md) - 一般的な問題と解決方法
- [アーキテクチャドキュメント](ARCHITECTURE.md) - システム設計詳細
- [Agent開発ガイド](AGENT-DEVELOPMENT.md) - エージェント実装パターン

---

## 🔗 外部リンク

- [SignalR ドキュメント](https://learn.microsoft.com/aspnet/core/signalr/introduction)
- [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons)
- [Vue 3 ドキュメント](https://vuejs.org/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

**Observability Dashboard**で営業支援エージェントの動作を完全に可視化しましょう！ 📊
