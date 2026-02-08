# サンプルデータ作成ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](SAMPLE-DATA.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/SAMPLE-DATA.md)

## 📋 概要

営業支援エージェントのデモを効果的に実施するため、**このプロジェクトに実装されているAPIエンドポイント**を使用してMicrosoft 365テナントにテストデータを生成する手順を説明します。

### ✨ 特徴

- ✅ **簡単**: 1つのAPIコールでメール・予定を自動生成
- ✅ **リアルなデータ**: 日本企業名・担当者名・製品名で商談データ作成
- ✅ **カスタマイズ可能**: 生成数・期間を調整可能
- ✅ **委任された権限**: Device Code Flow認証でユーザーとして登録

---

## 🎯 生成されるデータ

| データ種類 | 内容 | 件数（デフォルト） |
|----------|------|----------------|
| 📧 **商談メール** | 見積依頼、提案確認、契約関連メール | 50件 |
| 📅 **商談予定** | 顧客訪問、オンラインミーティング、社内ミーティング | 30件 |

### サンプルデータの内容

#### 商談メール例

```
件名: Re: 【見積書送付】株式会社サンプルテック 様向け クラウド基盤サービス
差出人: noreply@example.com
カテゴリ: 商談, 営業

本文:
株式会社サンプルテック 田中太郎様

いつもお世話になっております。

先日ご依頼頂きました「クラウド基盤サービス」の見積書を送付いたします。

商談金額: ¥3,500,000
提案製品: クラウド基盤サービス
...
```

#### 商談予定例

```
件名: 【商談】株式会社デジタルイノベーション - AIソリューション 打ち合わせ
日時: 2026-02-15  14:00-15:00
場所: オンライン（Teams会議）
説明: AIソリューション 提案・見積説明
```

---

## ⚙️ 前提条件

### 必須

| 項目 | 説明 |
|-----|------|
| ✅ **アプリケーション起動** | `dotnet run` で営業支援エージェントを起動 |
| ✅ **Microsoft 365アカウント** | データを作成するテナントのアカウント |
| ✅ **Graph API権限** | `Mail.ReadWrite`, `Calendars.ReadWrite`, `User.Read` |

### 確認事項

```bash
# アプリケーションが起動しているか確認
curl https://localhost:5192/health

# 期待される応答:
# {"Status":"Healthy", "M365Configured":true, ...}
```

---

## 🚀 テストデータ生成手順

営業支援エージェントには、3つのAPIエンドポイントが用意されています：

### 1️⃣ **すべてのデータを一括生成（推奨）**

メールと予定を一度に生成します。

```bash
curl -X POST https://localhost:5192/api/testdata/generate \
  -H "Content-Type: application/json" \
  -d '{
    "emailCount": 50,
    "eventCount": 30
  }'
```

**初回実行時の認証フロー**:

```
1. APIを呼び出すと、Device Code Flowが開始されます
2. コンソールに以下のようなメッセージが表示されます:

   📱 デバイスコードフロー認証が必要です
   ブラウザで以下のURLにアクセスしてください:
   https://microsoft.com/devicelogin

   コード: ABCD-EFGH

3. ブラウザでURLを開き、コードを入力
4. Microsoft 365アカウントでサインイン
5. 権限の同意画面で「承諾」をクリック
6. 認証完了後、データ生成が自動的に開始されます
```

**成功時の応答**:

```json
{
  "Success": true,
  "Message": "テストデータ生成完了",
  "EmailsCreated": 50,
  "EventsCreated": 30,
  "Period": {
    "StartDate": "2025-12-08",
    "EndDate": "2027-02-08"
  }
}
```

---

### 2️⃣ **メールのみ生成**

商談メールのみを生成します。

```bash
curl -X POST https://localhost:5192/api/testdata/generate/emails?count=100
```

**応答例**:

```json
{
  "Success": true,
  "Message": "100件の商談メールを生成しました",
  "Created": 100
}
```

---

### 3️⃣ **予定のみ生成**

商談予定のみを生成します。

```bash
curl -X POST https://localhost:5192/api/testdata/generate/events?count=50
```

**応答例**:

```json
{
  "Success": true,
  "Message": "50件の商談予定を生成しました",
  "Created": 50
}
```

---

## 🎨 カスタマイズ

### 生成数の調整

クエリパラメーターで生成数を変更できます：

```bash
# 200件のメールと100件の予定を生成
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

### 生成期間の調整

現在のコードでは以下の期間でデータが生成されます：

- **開始日**: 2ヶ月前（過去の商談）
- **終了日**: 1年後（未来の予定）

**カスタマイズ方法**:

[Services/TestData/TestDataGenerator.cs](../SalesSupportAgent/Services/TestData/TestDataGenerator.cs) の以下の箇所を編集：

```csharp
// デフォルト
var startDate = DateTime.Now.AddMonths(-2);
var endDate = DateTime.Now.AddYears(1);

// 例: 過去6ヶ月から今後6ヶ月に変更
var startDate = DateTime.Now.AddMonths(-6);
var endDate = DateTime.Now.AddMonths(6);
```

---

## 📊 生成データの確認

### Outlook で確認

1. [Outlook Web App](https://outlook.office.com) にアクセス
2. 「下書き」フォルダを開く
3. カテゴリ「商談」でフィルター

### Calendar で確認

1. [Outlook Calendar](https://outlook.office.com/calendar) にアクセス
2. "商談"で検索
3. イベント一覧を確認

---

## 🧪 デモシナリオ用データセット

デモを効果的に実施するための推奨データセット：

### シナリオ 1: 基本デモ（30分）

```bash
# 基本的なデータセット
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=30&eventCount=20"
```

- メール: 30件（最小限）
- 予定: 20件（1週間に数件程度）

### シナリオ 2: 詳細デモ（1時間）

```bash
# 充実したデータセット
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=100&eventCount=60"
```

- メール: 100件（複数顧客の履歴）
- 予定: 60件（過去・現在・未来のスケジュール）

### シナリオ 3: フルデモ（ワークショップ）

```bash
# 大規模データセット
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

- メール: 200件（多様な商談パターン）
- 予定: 100件（長期スケジュール）

---

## ⚠️ 注意事項とベストプラクティス

### ⚠️ 重要な注意事項

1. **メールボックス容量**: 大量のデータ生成はメールボックス容量を消費します
2. **テストテナント推奨**: 本番環境ではなく、開発者テナントで実施してください
3. **生成時間**: 100件のデータ生成には2-5分程度かかります
4. **重複実行**: 同じAPIを複数回実行すると、データが重複します

### ✅ ベストプラクティス

#### 1. テストテナントを使用

```
Microsoft 365 開発者プログラム: 無料のテストテナント取得
https://developer.microsoft.com/microsoft-365/dev-program
```

#### 2. データのクリーンアップ

デモ後は不要なテストデータを削除：

- Outlook で「カテゴリ: 商談」を一括削除
- Calendar で「商談」予定を一括削除

#### 3. 段階的なデータ生成

最初は少量でテスト：

```bash
# まず10件で動作確認
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=10&eventCount=5"

# 問題なければ本番データセット生成
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=50&eventCount=30"
```

---

## 🔧 トラブルシューティング

### エラー: "認証が必要です"

**原因**: Device Code Flow認証がまだ完了していない

**解決方法**:
1. コンソールに表示されたデバイスコードURLにアクセス
2. コードを入力してサインイン
3. 権限を承諾
4. 再度APIを実行

### エラー: "権限が不足しています"

**原因**: 必要な権限が付与されていない

**解決方法**:

`appsettings.json` の `TestData` セクションを確認：

```json
{
  "TestData": {
    "Scopes": [
      "User.Read",
      "Mail.ReadWrite",        // ✅ 必須
      "Calendars.ReadWrite"    // ✅ 必須
    ]
  }
}
```

### エラー: "メールボックスにアクセスできません"

**原因**: Microsoft 365ライセンスまたはメールボックスが有効化されていない

**解決方法**:
1. Microsoft 365管理センターでライセンスを確認
2. Exchangeメールボックスが作成されているか確認
3. Outlookに一度ログインしてメールボックスをアクティブ化

### データが生成されない

`**診断手順**:

```bash
# 1. ヘルスチェック
curl https://localhost:5192/health

# 2. ログ確認
# アプリケーションのコンソール出力を確認

# 3. 少量データでテスト
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=1&eventCount=1"
```

---

## 🔍 生成されたデータの活用

### エージェントでテスト

生成したデータを使ってエージェントをテスト：

```
@営業支援エージェント 今週の商談サマリを教えて
```

**期待される動作**:
1. 生成したメールから商談情報を抽出
2. 予定から今週のミーティングを取得
3. LLMでサマリー生成
4. Adaptive Cardで視覚的に返信

### Observability Dashboardで確認

1. `https://localhost:5192/observability.html` にアクセス
2. エージェント実行をリアルタイム監視
3. データ収集フェーズを詳細トレース

---

## 📚 関連ドキュメント

- [Getting Started](GETTING-STARTED.md) - 初期セットアップ
- [認証設定](AUTHENTICATION.md) - Graph API権限設定
- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - リアルタイム監視
- [エージェント開発](AGENT-DEVELOPMENT.md) - MCP Tools実装

---

## 💡 次のステップ

1. ✅ **テストデータ生成完了**
2. ▶️ [Teams統合](TEAMS-MANIFEST.md)でBotをTeamsに追加
3. ▶️ [Observability Dashboard](OBSERVABILITY-DASHBOARD.md)で動作を監視
4. ▶️ エージェントに質問してレスポンスを確認

---

**プロジェクトのAPIを活用して、効率的にデモ環境を構築しましょう！** 🚀
