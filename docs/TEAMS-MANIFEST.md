# Teams Bot マニフェスト設定ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TEAMS-MANIFEST.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/TEAMS-MANIFEST.md)

## 📋 概要

Microsoft Teams で Bot を動作させるには、**Teams アプリマニフェスト** が必要です。このガイドでは、営業支援エージェントを Teams に登録するための手順を解説します。

---

## 🎯 Teams アプリマニフェストとは

Teams アプリマニフェスト (`manifest.json`) は、Teams アプリの構成を定義する JSON ファイルです。

### 主な要素

- **基本情報**: アプリ名、説明、バージョン
- **Bot 設定**: Bot ID、スコープ、コマンド
- **アイコン**: カラーアイコン、アウトラインアイコン
- **権限**: Teams 内で使用する権限

### パッケージ構成

Teams アプリパッケージは ZIP ファイルで、以下の構成になります：

```
SalesSupportAgent.zip
├── manifest.json       # アプリマニフェスト
├── color.png          # カラーアイコン (192x192)
└── outline.png        # アウトラインアイコン (32x32)
```

---

## 🚀 セットアップ手順

### 前提条件

- ✅ Azure Bot Service の作成完了（[README.md](../README.md#3-teams-bot-接続) 参照）
- ✅ Bot の App ID と Password の取得完了
- ✅ Dev Tunnel または ngrok でメッセージングエンドポイント設定完了

---

### ステップ 1: マニフェストファイルの作成

プロジェクトのルートディレクトリに `TeamsAppManifest` フォルダを作成し、必要なファイルを配置します。

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent
mkdir -p TeamsAppManifest
cd TeamsAppManifest
```

本プロジェクトでは、以下のファイルが既に用意されています：

- [`manifest.json`](../TeamsAppManifest/manifest.json) - アプリマニフェスト
- [`color.png`](../TeamsAppManifest/color.png) - カラーアイコン (192x192)
- [`outline.png`](../TeamsAppManifest/outline.png) - アウトラインアイコン (32x32)

---

### ステップ 2: manifest.json の編集

#### 2-1. 必須項目の更新

[`TeamsAppManifest/manifest.json`](../TeamsAppManifest/manifest.json) を開き、以下の項目を更新します：

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/v1.17/MicrosoftTeams.schema.json",
  "manifestVersion": "1.17",
  "version": "1.0.0",
  "id": "YOUR-BOT-APP-ID-HERE",  // ← Azure Bot の App ID に変更
  "developer": {
    "name": "Your Company Name",  // ← 会社名に変更
    "websiteUrl": "https://www.example.com",  // ← 会社URL
    "privacyUrl": "https://www.example.com/privacy",  // ← プライバシーポリシー
    "termsOfUseUrl": "https://www.example.com/terms"  // ← 利用規約
  },
  "name": {
    "short": "営業支援エージェント",
    "full": "Agent 365 SDK 営業支援エージェント"
  },
  "description": {
    "short": "商談サマリを自動生成",
    "full": "Microsoft 365 データから商談関連情報を収集し、わかりやすいサマリを作成します。Outlook、SharePoint、Teams の情報を統合して営業活動を支援します。"
  },
  "bots": [
    {
      "botId": "YOUR-BOT-APP-ID-HERE",  // ← Azure Bot の App ID に変更
      "scopes": [
        "personal",
        "team",
        "groupchat"
      ],
      "supportsFiles": false,
      "isNotificationOnly": false,
      "commandLists": [
        {
          "scopes": [
            "personal",
            "team",
            "groupchat"
          ],
          "commands": [
            {
              "title": "今週の商談サマリ",
              "description": "今週の商談関連情報をまとめます"
            },
            {
              "title": "ヘルプ",
              "description": "使い方を表示します"
            }
          ]
        }
      ]
    }
  ]
}
```

#### 2-2. 必須変更項目

| 項目 | 説明 | 取得方法 |
|-----|------|---------|
| `id` | アプリの一意な ID | Azure Bot の **App ID** |
| `bots[0].botId` | Bot の ID | Azure Bot の **App ID**（`id` と同じ） |
| `developer.name` | 開発者/会社名 | 任意 |
| `developer.websiteUrl` | 会社のURL | 任意（有効なURL） |
| `developer.privacyUrl` | プライバシーポリシー | 任意（有効なURL） |
| `developer.termsOfUseUrl` | 利用規約 | 任意（有効なURL） |

⚠️ **重要**: `id` と `bots[0].botId` は **Azure Bot の App ID** と一致させる必要があります。

---

### ステップ 3: アイコンの準備

#### 3-1. カラーアイコン (color.png)

- **サイズ**: 192 x 192 ピクセル
- **フォーマット**: PNG
- **用途**: Teams アプリストア、チャット一覧

#### 3-2. アウトラインアイコン (outline.png)

- **サイズ**: 32 x 32 ピクセル
- **フォーマット**: PNG（透過背景）
- **色**: 白いアウトラインのみ
- **用途**: Teams サイドバー

#### 3-3. アイコンの作成方法

**オプション 1: デフォルトアイコンを使用**

本プロジェクトには、サンプルアイコンが含まれています。そのまま使用してテストできます。

**オプション 2: カスタムアイコンを作成**

1. オンラインツールを使用: [Canva](https://www.canva.com/), [Figma](https://www.figma.com/)
2. 192x192 で作成 → `color.png` として保存
3. 32x32 で白いアウトラインバージョンを作成 → `outline.png` として保存

**サンプルコマンド（ImageMagick 使用）**:
```bash
# カラーアイコンをリサイズ
convert original.png -resize 192x192 color.png

# アウトラインアイコンを作成
convert original.png -resize 32x32 -colorspace Gray -threshold 50% outline.png
```

---

### ステップ 4: アプリパッケージの作成

#### 4-1. ZIP ファイルの作成

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/TeamsAppManifest
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

**出力**: `SalesSupportAgent.zip`

#### 4-2. パッケージの検証

**オンライン検証ツール**:
[App Validation Tool](https://dev.teams.microsoft.com/appvalidation.html)

1. ZIP ファイルをアップロード
2. エラーがないか確認
3. エラーがあれば修正して再度 ZIP 化

---

### ステップ 5: Teams へのインストール

#### 方法 A: Teams Developer Portal を使用（推奨）

1. [Teams Developer Portal](https://dev.teams.microsoft.com/apps) にアクセス
2. **「Import an existing app」** をクリック
3. `SalesSupportAgent.zip` をアップロード
4. アプリの詳細を確認・編集
5. **「Preview in Teams」** をクリックして Teams で開く
6. **「追加」** をクリックしてインストール

#### 方法 B: Teams から直接アップロード

1. Teams を開く
2. 左メニューの **「アプリ」** をクリック
3. **「アプリを管理」** → **「カスタム アプリをアップロード」**
4. **「組織向けにカスタマイズしたアプリをアップロードする」** を選択
5. `SalesSupportAgent.zip` を選択
6. **「追加」** をクリック

⚠️ **注意**: 組織の IT 管理者がカスタムアプリのアップロードを許可している必要があります。

---

### ステップ 6: 動作確認

#### 6-1. Bot との会話を開始

1. Teams で **「営業支援エージェント」** を検索
2. Bot をクリックして個人チャットを開く
3. メッセージを送信:

```
こんにちは
```

**期待される応答**: ウェルカムメッセージ（Adaptive Card）が表示

#### 6-2. 商談サマリをテスト

```
今週の商談サマリを教えて
```

**期待される応答**: 営業サマリー（Adaptive Card）が表示

#### 6-3. チームに追加してテスト

1. Teams のチームを選択
2. **「...」** → **「アプリを管理」**
3. **「営業支援エージェント」** を追加
4. チャネルで **@営業支援エージェント** とメンション

```
@営業支援エージェント 今週の商談サマリを教えて
```

---

## 🔧 高度な設定

### Bot コマンドのカスタマイズ

`manifest.json` の `commandLists` を編集して、よく使うコマンドを追加できます：

```json
"commands": [
  {
    "title": "今週の商談サマリ",
    "description": "今週の商談関連情報をまとめます"
  },
  {
    "title": "先週の商談サマリ",
    "description": "先週の商談関連情報をまとめます"
  },
  {
    "title": "顧客名で検索",
    "description": "特定の顧客に関する情報を検索します"
  },
  {
    "title": "ヘルプ",
    "description": "使い方を表示します"
  }
]
```

### スコープの設定

Bot が動作する場所を制限できます：

```json
"scopes": [
  "personal",      // 個人チャット
  "team",          // チームチャネル
  "groupchat"      // グループチャット
]
```

不要なスコープを削除することで、セキュリティを向上できます。

### 権限の追加

Microsoft Graph API の追加権限が必要な場合:

```json
"webApplicationInfo": {
  "id": "YOUR-BOT-APP-ID-HERE",
  "resource": "https://graph.microsoft.com"
}
```

---

## 📦 マニフェスト更新手順

アプリを更新する場合:

### 1. バージョンの更新

```json
{
  "version": "1.0.1"  // バージョンを上げる
}
```

### 2. ZIP ファイルの再作成

```bash
cd TeamsAppManifest
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

### 3. Teams Developer Portal で更新

1. [Teams Developer Portal](https://dev.teams.microsoft.com/apps) にアクセス
2. 既存のアプリを選択
3. **「Update app package」** をクリック
4. 新しい ZIP ファイルをアップロード

**または Teams で直接更新**:
1. Teams → **「アプリ」** → **「アプリを管理」**
2. 既存のアプリを削除
3. 新しい ZIP ファイルをアップロード

---

## ⚠️ トラブルシューティング

### エラー 1: "Manifest parsing has failed"

**原因**: manifest.json の JSON 形式が不正

**対処**:
- JSON バリデーターで検証: [JSONLint](https://jsonlint.com/)
- カンマの位置、引用符が正しいか確認

### エラー 2: "Invalid Bot ID"

**原因**: `botId` が Azure Bot の App ID と一致していない

**対処**:
1. Azure Portal → Bot Service → 構成 → Microsoft App ID をコピー
2. `manifest.json` の `id` と `bots[0].botId` に貼り付け

### エラー 3: "Icon size is invalid"

**原因**: アイコンのサイズが規定と異なる

**対処**:
```bash
# サイズを確認
file color.png outline.png

# リサイズ（ImageMagick）
convert color.png -resize 192x192 color.png
convert outline.png -resize 32x32 outline.png
```

### エラー 4: "Bot not responding"

**原因**: メッセージングエンドポイントが間違っている

**対処**:
1. Dev Tunnel / ngrok が起動しているか確認
2. Azure Bot Service の設定を確認:
   ```
   https://your-tunnel-url/api/messages
   ```
3. ローカルアプリケーションが起動しているか確認:
   ```bash
   dotnet run
   ```

### エラー 5: "Custom app upload is blocked"

**原因**: 組織のポリシーでカスタムアプリアップロードが無効

**対処**:
- Microsoft 365 管理センター → Teams → Teams アプリ → セットアップ ポリシー
- **「カスタム アプリのアップロードを許可する」** を有効化
- または IT 管理者に依頼

---

## 🏢 組織全体への展開

### Microsoft Teams 管理センターでの承認

1. [Microsoft Teams 管理センター](https://admin.teams.microsoft.com/) にアクセス
2. **「Teams アプリ」** → **「アプリを管理」**
3. **「+ アプリをアップロード」** → ZIP ファイルをアップロード
4. **「公開」** をクリック

これにより、組織内のすべてのユーザーがアプリストアからインストールできるようになります。

---

## 📊 マニフェスト スキーマバージョン

本プロジェクトでは **v1.17** を使用していますが、必要に応じて変更できます：

| バージョン | リリース日 | 主な機能 |
|----------|----------|---------|
| **1.17** | 2023年11月 | 最新機能サポート |
| 1.16 | 2023年5月 | Adaptive Cards 強化 |
| 1.13 | 2022年6月 | Bot コマンド拡張 |

**参考**: [Teams マニフェスト スキーマ](https://learn.microsoft.com/ja-jp/microsoftteams/platform/resources/schema/manifest-schema)

---

## 🔗 参考リンク

- [Teams アプリマニフェスト](https://learn.microsoft.com/ja-jp/microsoftteams/platform/resources/schema/manifest-schema)
- [Teams Developer Portal](https://dev.teams.microsoft.com/)
- [Bot Framework ドキュメント](https://learn.microsoft.com/ja-jp/azure/bot-service/)
- [Teams アプリの検証](https://learn.microsoft.com/ja-jp/microsoftteams/platform/concepts/deploy-and-publish/appsource/prepare/teams-store-validation-guidelines)
- [Adaptive Cards デザイナー](https://adaptivecards.io/designer/)

---

## 📝 チェックリスト

マニフェスト作成前の確認項目:

- [ ] Azure Bot Service 作成済み
- [ ] Bot App ID と Password 取得済み
- [ ] Dev Tunnel / ngrok でメッセージングエンドポイント設定済み
- [ ] `manifest.json` の `id` と `botId` を Bot App ID に変更
- [ ] アイコン (192x192, 32x32) を準備
- [ ] ZIP ファイル作成
- [ ] マニフェスト検証ツールで確認
- [ ] Teams にアップロードして動作確認

すべて完了したら、[認証設定](AUTHENTICATION.md) と組み合わせて本格的な運用が可能になります！
