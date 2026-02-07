# 🚀 Teams + Dashboard 同時動作ガイド

**実施日**: 2026年2月7日  
**目的**: Teamsでエージェントを動かしながら、リアルタイムでダッシュボードを確認

---

## 📋 現在の状態

✅ **アプリケーション**: 起動中（http://localhost:5192）  
✅ **Dev Tunnel**: 稼働中（https://7hfqnlhn-5192.asse.devtunnels.ms）  
✅ **Observability Dashboard**: 利用可能（http://localhost:5192/observability.html）  

---

## 🎬 デモ実行手順（5ステップ）

### Step 1: ダッシュボードを開く（ローカル）

**ブラウザで以下を開く**:
```
http://localhost:5192/observability.html
```

**確認ポイント**:
- ✅ 右上「✓ 接続済み」（緑色）
- ✅ メトリクスカード（4枚）
- ✅ トレースリスト
- ✅ 🔔 リアルタイム通知パネル
- ✅ 💬 会話履歴パネル

**画面位置**: 
- Macなら **Split View**（左半分にDashboard）
- または **別モニター** / **別デスクトップ**

---

### Step 2: Teams アプリを開く

**方法A: Teams Web版（推奨・簡単）**
```
別タブで開く: https://teams.microsoft.com
```

**方法B: Teams デスクトップアプリ**
```
アプリケーション → Microsoft Teams
```

**画面位置**:
- Macなら **Split View**（右半分にTeams）

---

### Step 3: Teamsでカスタムアプリをアップロード

#### 3-1. App Manifestの確認

まず、appPackageフォルダの存在を確認：
```bash
ls -la /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/appPackage/
```

**必要なファイル**:
- `manifest.json` - アプリ定義
- `color.png` - カラーアイコン
- `outline.png` - アウトラインアイコン

#### 3-2. manifest.jsonのBot エンドポイント確認

`manifest.json`を開いて以下を確認：
```json
{
  "bots": [
    {
      "botId": "e9b14ac6-2820-446e-827c-e1ba086b5389",
      "scopes": ["personal", "team", "groupchat"]
    }
  ],
  "validDomains": [
    "7hfqnlhn-5192.asse.devtunnels.ms"
  ]
}
```

**⚠️ 重要**: Bot エンドポイントはAzure Portal（Bot Channels Registration）で設定します：
```
https://7hfqnlhn-5192.asse.devtunnels.ms/api/messages
```

#### 3-3. App Packageのzip化

**方法1: コマンドライン**
```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/appPackage
zip -r SalesSupportAgent.zip manifest.json color.png outline.png
```

**方法2: Finder**
1. appPackageフォルダを開く
2. manifest.json, color.png, outline.pngを選択
3. 右クリック → "3項目を圧縮"

#### 3-4. Teamsにアップロード

**Teams Web/Desktop**:
1. 左サイドバー「アプリ」をクリック
2. 下部「アプリの管理」
3. 右上「カスタムアプリをアップロード」
4. 作成したSalesSupportAgent.zipを選択
5. 「追加」ボタンをクリック

**エラーが出た場合の対処**:
```
❌ "カスタムアプリのアップロードが許可されていません"
→ Teams Admin Center で許可が必要（IT管理者に依頼）

❌ "Botエンドポイントに接続できません"
→ Step 4のAzure Portal設定を確認
```

---

### Step 4: Azure Portal でBot エンドポイント設定

**URL**: https://portal.azure.com

**手順**:
1. Azure Portal にサインイン
2. 「Bot Services」を検索
3. Bot名を選択（e9b14ac6-2820-446e-827c-e1ba086b5389）
4. 左メニュー「設定」→「構成」
5. 「メッセージング エンドポイント」を以下に変更:
   ```
   https://7hfqnlhn-5192.asse.devtunnels.ms/api/messages
   ```
6. 「適用」をクリック

**確認**:
```bash
# Dev Tunnel経由でBot エンドポイントにアクセス
curl -X POST https://7hfqnlhn-5192.asse.devtunnels.ms/api/messages \
  -H "Content-Type: application/json" \
  -d '{"type":"message","text":"test"}'
```

---

### Step 5: 🎉 同時デモ実行！

#### 画面配置

```
┌─────────────────────────────┬─────────────────────────────┐
│  Dashboard (左半分)          │  Teams (右半分)              │
│  http://localhost:5192/      │  https://teams.microsoft.com│
│  observability.html          │                             │
│                              │                             │
│  📊 メトリクス               │  💬 チャット                │
│  📝 トレース                 │  👤 Sales Support Agent     │
│  🔔 通知 (リアルタイム更新)  │                             │
│  💬 会話履歴                 │  "今週の商談状況を           │
│                              │   教えてください"           │
└─────────────────────────────┴─────────────────────────────┘
```

#### デモシナリオ

**👁️ Dashboardを見ながら、Teamsでメッセージ送信**:

1️⃣ **Teamsで入力**: 「今週の商談状況を教えてください」

2️⃣ **Dashboardでリアルタイム確認**（0.5秒遅延）:
```
🔔 リアルタイム通知パネル（進捗バー付き）:
  🚀 商談サマリ生成を開始しています... [0%] ▁▁▁▁▁▁▁▁▁▁
  📊 データ収集中（メール、カレンダー、ドキュメント）... [25%] ██▁▁▁▁▁▁▁▁
  🤖 AI分析中（サマリ生成処理）... [75%] ███████▁▁▁
  ✅ 商談サマリ生成完了！（処理時間: 6,500ms） [100%] ██████████

📝 リアルタイムトレース:
  1. 🎉 商談サマリ生成完了 [success] 6500ms
  2. ✅ AIエージェント実行完了 [success] 6200ms
  3. ⚙️ AIエージェント実行中 [info] 150ms
  4. 🚀 商談サマリ生成開始 [info] 0ms

📊 メトリクスカード（自動更新）:
  - 総リクエスト: 0 → 1
  - 平均応答時間: 6500ms
  - 成功率: 100%
  - 稼働時間: 増加中

💬 会話履歴:
  新しい会話が記録される
  - ユーザー: "今週の商談状況を教えてください"
  - Bot: "[Adaptive Card Response] ..."
```

3️⃣ **Teamsで応答確認**（約5-10秒後）:
```
💼 Sales Support Agent が応答:

[Adaptive Cardが表示]
📊 今週の商談サマリー
━━━━━━━━━━━━━━━━━━━━

📧 商談メール (50件)
・件名1...
・件名2...

📅 商談予定 (4件)
・2026/02/10 14:00 製品デモ
・2026/02/12 10:00 契約交渉

💡 推奨アクション
1. 緊急対応が必要なメールを確認
2. ...

━━━━━━━━━━━━━━━━━━━━
⚡ 処理時間: 6,500ms | 🤖 Ollama
```

---

## 🎯 フォーカルポイント（デモで強調）

### 1. リアルタイム性の証明

**説明**:
> "Teamsでメッセージを送信すると、0.5秒以内にDashboardに通知が表示されます。SignalRによる双方向通信で、エージェントの内部処理がリアルタイムで可視化されます。"

**実演**:
- Teamsでメッセージ送信
- **即座に**Dashboard通知パネルに「0%」が表示
- Teamsの応答を待つ前にDashboardで全ステップ確認

### 2. 透明性（Transparency）

**説明**:
> "従来のAIエージェントはブラックボックスでしたが、Agent 365ではすべての処理ステップが可視化されます。データ収集、AI分析、応答生成の各段階を追跡できます。"

**実演**:
- Dashboard「📝 トレース」で4段階確認:
  1. 🚀 開始
  2. 📊 データ収集
  3. 🤖 AI分析
  4. 🎉 完了

### 3. メトリクス駆動の品質保証

**説明**:
> "各リクエストの処理時間、成功率、稼働時間をリアルタイムで監視できます。SLAの設定と監視が可能です。"

**実演**:
- メトリクスカードの自動更新
- 平均応答時間の計算
- 成功率100%の維持確認

### 4. 会話履歴とトレーサビリティ

**説明**:
> "すべてのユーザー・Bot間のやり取りが記録され、いつでも振り返りができます。監査証跡として利用可能です。"

**実演**:
- Dashboard「💬 会話履歴」パネル
- 会話一覧をクリック→詳細表示
- ユーザーメッセージ（青）とBot応答（紫）の色分け

---

## 🔧 トラブルシューティング

### Issue 1: Teamsでカスタムアプリが表示されない

**症状**: アップロードしたアプリが見つからない

**原因**: カスタムアプリのアップロードが組織で許可されていない

**解決策**:
1. Teams Admin Center（https://admin.teams.microsoft.com）にアクセス
2. 「Teams アプリ」→「アプリの管理」→「組織全体のアプリ設定」
3. 「カスタムアプリのアップロードを許可する」を有効化
4. 変更反映まで最大24時間待機

**代替案**: IT管理者に依頼

---

### Issue 2: Teamsで「応答がありません」

**症状**: Teamsでメッセージを送ってもBotが応答しない

**原因**: Bot エンドポイントの設定ミス

**確認手順**:
```bash
# 1. ローカルアプリケーション起動確認
curl http://localhost:5192/health

# 2. Dev Tunnel経由アクセス確認
curl https://7hfqnlhn-5192.asse.devtunnels.ms/health

# 3. Azure Portal Bot設定確認
# メッセージング エンドポイント: 
# https://7hfqnlhn-5192.asse.devtunnels.ms/api/messages
```

**解決策**:
- Azure PortalでBot エンドポイントを正しく設定
- Dev Tunnelが起動していることを確認
- ローカルアプリケーションが5192ポートで起動していることを確認

---

### Issue 3: Dashboardで通知が表示されない

**症状**: Teamsでメッセージ送信しても、Dashboard通知が更新されない

**原因**: SignalR接続エラーまたはBot経由ではないリクエスト

**確認手順**:
1. Dashboard右上のステータス確認
   - ✅ 「✓ 接続済み」（緑色）→ OK
   - ❌ 「✗ 切断」（赤色）→ 再接続必要

2. ブラウザコンソール確認（F12）
   ```
   SignalR Connected
   Notification received: {...}
   ```

3. Teamsからのメッセージであることを確認
   - `/api/sales-summary`エンドポイント（直接API）は会話履歴に記録されない
   - Teams経由のメッセージのみTranscriptServiceが動作

**解決策**:
- ページをリロード（Cmd+R）
- SignalR接続を再確立
- Teamsアプリ経由でメッセージ送信を確認

---

### Issue 4: Dev Tunnelが「Connection refused」

**症状**: Dev Tunnel URLにアクセスできない

**原因**: Dev Tunnelが停止している

**解決策**:
```bash
# Dev Tunnelを再起動
devtunnel host -p 5192 --allow-anonymous
```

---

## 📊 デモ成功の確認ポイント

### ✅ チェックリスト

- [ ] Dashboardが開いている（http://localhost:5192/observability.html）
- [ ] Dashboard右上「✓ 接続済み」（緑色）
- [ ] Teamsアプリが開いている
- [ ] TeamsでSales Support Agentとのチャット開始
- [ ] Teamsでメッセージ送信
- [ ] **0.5秒以内に**Dashboard通知パネルに「🚀 0%」表示
- [ ] Dashboard通知が段階的に更新（0% → 25% → 75% → 100%）
- [ ] Dashboardトレースリストに4件追加
- [ ] Dashboardメトリクスカード更新
- [ ] TeamsでAdaptive Card応答表示
- [ ] Dashboard会話履歴に新しい会話記録

---

## 🎥 デモシナリオ例

### シナリオA: 初回体験

**オーディエンス**: 初めてAgent 365を見る人

**流れ**:
1. **Dashboard紹介**（30秒）
   - "これがObservability Dashboardです"
   - メトリクス、トレース、通知、会話履歴を説明

2. **Teamsでメッセージ送信**（5秒）
   - "今週の商談状況を教えてください"

3. **リアルタイム通知の強調**（10秒）
   - "ご覧ください、Teamsでメッセージを送った瞬間にDashboardに通知が表示されました"
   - "0% → 25% → 75% → 100%と進捗が可視化されています"

4. **トレース詳細の説明**（20秒）
   - "4つのステップが時系列で記録されています"
   - "開始、データ収集、AI分析、完了"

5. **Teams応答確認**（10秒）
   - "約6秒後、TeamsでAdaptive Cardが表示されました"
   - "処理時間6500ms、成功率100%"

6. **会話履歴確認**（15秒）
   - "すべての会話が自動記録されています"
   - "いつでも過去のやり取りを振り返れます"

**合計時間**: 約90秒

**強調ポイント**:
- リアルタイム性（0.5秒）
- 透明性（4段階のトレース）
- 自動記録（会話履歴）

---

### シナリオB: 複数リクエストでのメトリクス更新

**オーディエンス**: 技術者、運用チーム

**流れ**:
1. **初期メトリクス確認**
   - 総リクエスト: 0
   - 平均応答時間: 0ms

2. **1回目のリクエスト**
   - Teams: "今週の商談状況を教えてください"
   - Dashboard: 総リクエスト: 1, 平均: 6500ms

3. **2回目のリクエスト**
   - Teams: "次の予定を教えてください"
   - Dashboard: 総リクエスト: 2, 平均: 4000ms（高速化）

4. **3回目のリクエスト**
   - Teams: "最新のメールは？"
   - Dashboard: 総リクエスト: 3, 平均: 3500ms

5. **メトリクス分析**
   - "平均応答時間が6500ms → 3500msに改善"
   - "成功率100%を維持"
   - "稼働時間3分"

**強調ポイント**:
- メトリクス精度（平均値計算）
- パフォーマンス改善（キャッシュ効果）
- SLA監視（成功率100%）

---

## 🚀 次のステップ

### 完了後の拡張

1. **Application Insights統合**
   - Azure Application Insightsでエンタープライズ監視
   - ダッシュボードのテンプレート化

2. **カスタムメトリクス追加**
   - ビジネスKPI（商談件数、売上予測）
   - ユーザー別使用統計

3. **アラート設定**
   - 応答時間が10秒超えたら通知
   - 成功率が95%未満なら警告

4. **本番環境デプロイ**
   - Azure App Service
   - Azure Bot Service Production設定
   - HTTPS/認証設定

---

**作成日**: 2026年2月7日  
**対象**: Teams + Dashboard 同時デモ  
**想定時間**: 準備10分、デモ実行10分
