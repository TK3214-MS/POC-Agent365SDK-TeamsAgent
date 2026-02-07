# 📊 Observability Dashboard アップデート完了ガイド

## 🎯 アップデート内容

### ✅ 1. **ObservabilityService 拡張**
   - **エージェント情報管理機能**
     - アクティブエージェントの登録・更新
     - エージェント状態管理（Active, Idle, Busy, Offline）
     - 最終アクティビティ追跡
     - 総インタラクション数カウント

   - **詳細トレース機能（Verboseレベル）**
     - トレースセッション管理
     - フェーズ単位のトレース記録
     - ユーザークエリ → AI内部処理 → 最終応答の完全トレース
     - データ収集情報の記録

   **新規データモデル:**
   - `AgentInfo` - エージェント情報
   - `DetailedTraceSession` - 詳細トレースセッション
   - `TracePhase` - トレースフェーズ

### ✅ 2. **Program.cs API エンドポイント追加**

   ```http
   GET /api/observability/agents
   # アクティブエージェント一覧取得

   GET /api/observability/detailed-traces?count=50
   # 詳細トレースセッション一覧取得

   GET /api/observability/detailed-trace/{sessionId}
   # 特定の詳細トレースセッション取得

   GET /api/observability/traces-by-conversation/{conversationId}
   # 会話IDで詳細トレースを検索
   ```

   **起動時自動処理:**
   - エージェント情報自動登録（"営業支援エージェント"）

### ✅ 3. **SalesAgent 詳細トレース統合**

   **トレースフェーズ:**
   1. **Request Received** - リクエスト受信
   2. **Query Preparation** - クエリ準備（日付範囲追加）
   3. **AI Agent Execution Started** - AI実行開始
   4. **AI Response Received** - AI応答取得
   5. **Summary Generation Completed** - サマリ生成完了
   6. **Error Occurred** - エラー発生（エラー時のみ）

   **記録データ:**
   - ユーザークエリ
   - 日付範囲
   - AI実行時間
   - データソース（Email, Calendar, SharePoint, Teams）
   - 応答長
   - エラー情報（エラー時）

### ✅ 4. **Observability Dashboard 完全リニューアル**

   **デザイン:**
   - ✨ 白を基軸にしたクリーンなデザイン
   - 🎨 Fluent UI スタイルガイド準拠
   - 📱 レスポンシブデザイン

   **主要機能:**

   #### 📈 **メトリクスダッシュボード**
   - Total Requests
   - Average Response Time
   - Success Rate
   - Uptime

   #### 🤖 **Active Agents パネル**
   - エージェント名、タイプ、バージョン表示
   - 状態表示（Active, Idle, Busy, Offline）
   - 最終アクティビティ表示
   - **✅ 修正**: "エージェント情報を取得中"エラー解消

   #### 💬 **Conversation Timeline**
   - ユーザーとエージェントのやり取りをタイムライン表示
   - クリックで詳細トレースモーダルを表示
   - 各会話のフェーズ概要表示
   - **✅ 修正**: 会話履歴が正しく表示されるように

   #### 🔍 **Detailed Trace Modal**
   - ユーザークエリ
   - 実行フェーズ詳細（Verboseレベル）
   - 各フェーズのデータ（JSON形式）
   - 最終応答
   - メタデータ（セッションID、会話ID、実行時間、ステータス）

   #### 📊 **Recent Traces テーブル**
   - 最新20件のトレース
   - タイムスタンプ、操作、ステータス、実行時間表示

   **リアルタイム更新（SignalR）:**
   - メトリクス自動更新
   - エージェント情報更新
   - トレース追加時自動更新
   - フェーズ更新通知

### ✅ 5. **通知機能の強化**

   **データ収集情報表示:**
   - 成功通知にデータソース情報を含む
   - 処理時間、データソース数、応答長を表示
   - データソース一覧（"Outlook, Calendar, SharePoint, Teams"）

## 🖼️ Agent 365 ロゴ配置ディレクトリ

### 📁 配置場所
```
POC-Agent365SDK-TeamsAgent/
└── SalesSupportAgent/
    └── wwwroot/
        └── images/
            └── agent365-logo.png  ← ここに配置
```

### 📏 推奨仕様
- **ファイル形式**: PNG（透過背景推奨）
- **サイズ**: 400x400px 以上（正方形）
- **最大ファイルサイズ**: 100KB以下
- **用途**: ヘッダーロゴとして表示（40x40pxにリサイズされます）

### 🎨 フォールバック
ロゴ画像が配置されていない場合、グラデーション背景（青系）に "A" の文字が表示されます。

## 🚀 アプリケーション起動方法

### 1. ビルド
```bash
cd POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet build
```

### 2. 起動
```bash
dotnet run
```

### 3. ダッシュボードアクセス
```
http://localhost:5192/observability.html
```

## 🧪 テスト手順

### 1. **ダッシュボード初期表示確認**
   - ブラウザで http://localhost:5192/observability.html を開く
   - 接続ステータスが「接続済み」になることを確認
   - メトリクスが表示されることを確認

### 2. **Active Agents 確認**
   - "営業支援エージェント" が表示されることを確認
   - エージェントタイプ: "Sales Support"
   - ステータス: "Active"

### 3. **商談サマリリクエスト**
   ```bash
   curl -X POST http://localhost:5192/api/sales-summary \
     -H "Content-Type: application/json" \
     -d '{
       "query": "今週の商談状況を教えてください",
       "topN": 10
     }'
   ```

### 4. **Conversation Timeline 確認**
   - ダッシュボードで新しい会話が表示されることを確認
   - タイムラインアイテムをクリック

### 5. **Detailed Trace Modal 確認**
   - モーダルが開くことを確認
   - 以下のフェーズが表示されることを確認:
     - Request Received
     - Query Preparation
     - AI Agent Execution Started
     - AI Response Received
     - Summary Generation Completed
   - 各フェーズのデータ（JSON）が表示されることを確認

### 6. **データ収集情報確認**
   - 通知パネル（もしあれば）でデータソース情報を確認
   - データソース: "Outlook, Calendar, SharePoint, Teams"

### 7. **Recent Traces 確認**
   - Recent Tracesテーブルに新しいトレースが追加されることを確認
   - 各トレースの実行時間が表示されることを確認

## 📝 ページタイトル

```html
Agent Dashboard powered by Agent 365
```

- "Agent Dashboard" - 通常フォント
- "powered by" - 軽いグレー、軽量フォント
- "Agent 365" - ロゴ画像（または "A" フォールバック）

## 🎨 UIデザイン詳細

### カラーパレット
- **Primary**: `#0078d4` (Microsoft Blue)
- **Success**: `#107c10`
- **Error**: `#d13438`
- **Warning**: `#ffb900`
- **Background**: `#f5f5f5` (Light Gray)
- **Surface**: `#ffffff` (White)
- **Text Primary**: `#242424`
- **Text Secondary**: `#605e5c`
- **Border**: `#edebe9`

### Fluent UIアイコン
- CDN: `fluent-icons-all.css` （SharePoint）
- フォールバック: 絵文字アイコン（🤖, 💬, 🔍, 📊）

### レスポンシブブレークポイント
- メトリクスグリッド: `minmax(250px, 1fr)`
- エージェントカード: `minmax(300px, 1fr)`
- モーダル: 最大幅 900px、画面の90%

## 🔧 トラブルシューティング

### 問題: "エージェント情報を取得中"で止まる
**解決策:**
- Program.csでエージェント登録処理が正しく実行されているか確認
- `/api/observability/agents` エンドポイントが正常に応答するか確認
- ブラウザのコンソールでエラーメッセージを確認

### 問題: 会話履歴が表示されない
**解決策:**
- `/api/observability/detailed-traces` エンドポイントが正常に応答するか確認
- 商談サマリリクエストを実行して、詳細トレースセッションが作成されているか確認
- ブラウザをリロードしてSignalR接続を再確立

### 問題: モーダルが開かない
**解決策:**
- ブラウザのJavaScriptコンソールでエラーを確認
- `/api/observability/detailed-trace/{sessionId}` エンドポイントが正常に応答するか確認

## 📚 API リファレンス

### GET /api/observability/agents
**レスポンス:**
```json
[
  {
    "agentId": "sales-support-agent-1",
    "agentName": "営業支援エージェント",
    "agentType": "Sales Support",
    "status": "Active",
    "registeredAt": "2026-02-07T12:00:00Z",
    "lastActiveAt": "2026-02-07T12:30:00Z",
    "version": "1.0.0",
    "totalInteractions": 5,
    "lastActivity": "商談サマリ生成"
  }
]
```

### GET /api/observability/detailed-traces?count=50
**レスポンス:**
```json
[
  {
    "sessionId": "abc-123",
    "conversationId": "conv-456",
    "userId": "API-User",
    "userQuery": "今週の商談状況を教えてください",
    "startTime": "2026-02-07T12:00:00Z",
    "endTime": "2026-02-07T12:00:15Z",
    "finalResponse": "## 📊 サマリー\n...",
    "success": true,
    "durationMs": 15000,
    "phases": [
      {
        "phaseName": "Request Received",
        "description": "商談サマリ生成リクエストを受信しました",
        "timestamp": "2026-02-07T12:00:00Z",
        "data": { "query": "今週の商談状況を教えてください" },
        "status": "Completed"
      }
    ]
  }
]
```

### GET /api/observability/detailed-trace/{sessionId}
**レスポンス:**
詳細トレースセッション単体（上記と同じ構造）

## 🎉 まとめ

すべてのアップデート要件が実装されました：

✅ アクティブエージェント情報の正しい表示  
✅ ユーザーとエージェントのやり取りをタイムライン表示  
✅ 詳細トレースモーダル（Verboseレベル内部ロジック）  
✅ データ収集情報の表示  
✅ 会話履歴の表示修正  
✅ 白ベースのクリーンなUIデザイン  
✅ Fluent UIベースのアイコン  
✅ ページタイトル更新 + Agent 365ロゴプレースホルダー  

**次のステップ:**
1. アプリケーションを再起動
2. http://localhost:5192/observability.html でダッシュボードを確認
3. Agent 365ロゴを `wwwroot/images/agent365-logo.png` に配置
4. Teamsで動作確認
