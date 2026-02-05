# 営業支援エージェント (Sales Support Agent)
**Agent 365 SDK デモアプリケーション**

## 📋 概要

Teams チャットで「@営業支援エージェント、今週の商談サマリを教えて」と話しかけると、エージェントが **Agent Identity** を使って安全に Microsoft 365 データへアクセスし、メール・カレンダー・SharePoint から情報を収集してレポートを返すデモアプリケーションです。

**Agent365 SDK ハイブリッド実装**: [microsoft/Agent365-Samples](https://github.com/microsoft/Agent365-Samples) の enterprise 機能（observability、tooling、notifications）を統合しつつ、LM Studio/Ollama などのローカル LLM にも対応した実装です。

### 🎯 デモで見せるポイント

- ✅ **Agent Identity による安全な認証**（専用メールボックスを持つ）
- ✅ **Teams の通知チャネルでエージェントが人間のように返信**
- ✅ **MCP（Model Context Protocol）経由で M365 データを安全に取得**
- ✅ **OpenTelemetry による観測性**（トレース・メトリクス）を管理者が確認
- ✅ **Agent365 SDK パターン**: IChatClient middleware chain (Function Invocation + OpenTelemetry)
- ✅ **エンタープライズグレードの Agent Storage と Transcript ロギング**
- ✅ **Adaptive Cards による視覚的な応答**（見やすく、インタラクティブな UX）
- ✅ **Microsoft Search API による高度な SharePoint 検索**（日付範囲・キーワード対応）

### 💼 顧客価値

- 「Copilot ではなく __自社専用の業務エージェント__」を Teams に自然に組み込める
- データアクセスが **ガバナンス下**であることを強調できる
- LM Studio などの **ローカル LLM** にも対応し、クラウド依存を軽減

---

## 🏗️ アーキテクチャ

```
┌─────────────────────────────────────────────────────────────┐
│                      Microsoft Teams                         │
│               ユーザーが @メンションで問い合わせ                │
└──────────────────────┬──────────────────────────────────────┘
                       │ Bot Framework
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              営業支援エージェント (.NET 10)                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  LLM Provider (切替可能) + Agent365 Middleware       │   │
│  │  - LM Studio (デフォルト)                              │   │
│  │  - Ollama                                            │   │
│  │  - Azure OpenAI                                       │   │
│  │  - OpenAI                                            │   │
│  │  [IChatClient → Builder → UseFunctionInvocation()    │   │
│  │   → UseOpenTelemetry() → Build()]                    │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  MCP Tools (M365 データアクセス)                       │   │
│  │  - Outlook メール検索（Graph API）                     │   │
│  │  - Outlook カレンダー検索（Graph API）                 │   │
│  │  - SharePoint ドキュメント検索（Microsoft Search API） │   │
│  │  - Teams チャット検索（Graph API）                     │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Adaptive Cards レンダリング                          │   │
│  │  - 営業サマリーカード（構造化表示）                     │   │
│  │  - エラーカード（視覚的なエラー通知）                   │   │
│  │  - ウェルカムカード（初回ガイダンス）                   │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Agent365 SDK 統合                                    │   │
│  │  - Observability (AgentMetrics)                      │   │
│  │  - Tooling Extensions                                │   │
│  │  - Notifications (beta)                              │   │
│  │  - Storage + Transcript                              │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  OpenTelemetry                                       │   │
│  │  - トレース（コンソール出力）                           │   │
│  │  - メトリクス                                          │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────┬──────────────────────────────────────┘
                       │ Agent Identity (App-only)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│               Microsoft 365 / Graph API                      │
│     - Outlook   - Calendar   - SharePoint   - Teams         │
└─────────────────────────────────────────────────────────────┘
```

---

## 🚀 クイックスタート

### 必要な環境

- ✅ **.NET 10 SDK**
- ✅ **LM Studio** (またはOllama / Azure OpenAI / OpenAI)
- ✅ **Dev Tunnel CLI** (Microsoft 推奨) または ngrok
- ✅ **Microsoft 365 開発者テナント**（推奨）

### 0. Dev Tunnel のセットアップ（推奨）

**Dev Tunnel は ngrok の代替として Microsoft が提供する開発用トンネルサービスで、固定 URL の利用が可能です。**

#### インストール

```bash
# macOS / Linux
curl -sL https://aka.ms/DevTunnelCliInstall | bash

# または Homebrew (macOS)
brew install devtunnel

# Windows
winget install Microsoft.devtunnel
```

#### 初回セットアップ

```bash
# Microsoft アカウントでログイン
devtunnel user login

# 固定トンネルを作成（初回のみ）
devtunnel create sales-support-agent --allow-anonymous

# トンネル情報を確認
devtunnel list
```

**出力例:**
```
Tunnel ID: abc123xyz
Tunnel Name: sales-support-agent
Access: Anonymous
```

#### トンネル起動

```bash
# ポート 5001 (HTTPS) にトンネルを作成して起動
devtunnel port create sales-support-agent -p 5001

# トンネルをホスト（起動）
devtunnel host sales-support-agent
```

**固定 URL が表示されます:**
```
Connect via browser: https://abc123xyz-5001.euw.devtunnels.ms
Inspect via browser: https://abc123xyz-5001-inspect.euw.devtunnels.ms
```

この URL (`https://abc123xyz-5001.euw.devtunnels.ms`) は **固定** で、トンネルを削除しない限り変わりません。

#### トンネル管理コマンド

```bash
# トンネル一覧
devtunnel list

# トンネル削除
devtunnel delete sales-support-agent

# ヘルプ
devtunnel --help
```

### 1. LM Studio のセットアップ

1. [LM Studio](https://lmstudio.ai/) をダウンロード・インストール
2. 任意のモデルをダウンロード（推奨: Qwen2.5, Llama 3.2, Phi-3.5 など）
3. LM Studio を起動し、**Local Server** を開始
   - デフォルト: `http://localhost:1234`
4. モデルをロードして動作確認

### 2. プロジェクトのセットアップ

```bash
# リポジトリをクローン
cd /path/to/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# 環境変数ファイルを作成
cp .env.sample .env

# .env ファイルを編集
# LLM__Provider=LMStudio
# LLM__LMStudio__ModelName=<LM Studioでロードしたモデル名>
```

###3. ビルド＆実行

```bash
# ビルド
dotnet build

# 実行
dotnet run
```

アプリケーションは `https://localhost:5001` で起動します。

### 4. 動作確認（ローカルテスト）

```bash
# ヘルスチェック
curl https://localhost:5001/health

# 直接エージェント呼び出し
curl -X POST https://localhost:5001/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"今週の商談サマリを教えて"}'
```

---

## 🔧 設定

### LLM プロバイダーの切り替え

[appsettings.json](SalesSupportAgent/appsettings.json) または `.env` ファイルで設定:

```json
{
  "LLM": {
    "Provider": "LMStudio",  // LMStudio, Ollama, AzureOpenAI, OpenAI
    "LMStudio": {
      "Endpoint": "http://localhost:1234/v1",
      "ModelName": "qwen2.5-coder-7b-instruct",
      "ApiKey": "not-needed"
    }
  }
}
```

### Microsoft 365 (Graph API) 接続

**Agent Identity** を設定するには、Azure Portal で以下の手順を実行:

1. **Azure Portal** → **Microsoft Entra ID** → **アプリ登録**
2. **新規登録**
   - 名前: `SalesSupportAgent`
   - サポートされるアカウントの種類: `この組織ディレクトリのみ`
3. **証明書とシークレット** → **新しいクライアントシークレット**
4. **APIのアクセス許可** → **アクセス許可の追加** → **Microsoft Graph** → **アプリケーションの許可**
   - `Mail.Read` - Outlook メール検索
   - `Calendars.Read` - カレンダー予定検索
   - `Files.Read.All` - SharePoint ファイルアクセス
   - `Sites.Read.All` - SharePoint サイト検索（Microsoft Search API）
   - `ChannelMessage.Read.All` - Teams メッセージ検索
   - `Team.ReadBasic.All` - Teams 基本情報取得
5. **管理者の同意を付与**

[appsettings.json](SalesSupportAgent/appsettings.json) に設定:

```json
{
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### Teams Bot 接続

#### 1. Azure Bot Service の作成

1. **Azure Portal** → **Bot Services** → **作成**
2. **Azure Bot** を選択
   - Bot ハンドル: `SalesSupportAgent` (一意の名前)
   - サブスクリプション・リソースグループを選択
   - 価格レベル: **F0 (Free)**
3. **Microsoft App ID**: **マルチテナント** を選択
4. 作成完了後、**構成** → **メッセージング エンドポイント** を設定
   - Dev Tunnel URL を使用: `https://your-tunnel-id-5001.region.devtunnels.ms/api/messages`
   - または ngrok: `https://your-ngrok-url.ngrok.io/api/messages`

#### 2. App ID とシークレットの取得

1. **構成** → **Microsoft App ID** をクリック
2. **証明書とシークレット** → **新しいクライアント シークレット**
   - 説明: `SalesSupportAgent Secret`
   - 有効期限: 任意（推奨: 24ヶ月）
3. **シークレット値** をコピー（1度のみ表示されます）
4. **概要** → **アプリケーション (クライアント) ID** をコピー

#### 3. アプリケーション設定

[appsettings.json](SalesSupportAgent/appsettings.json) に設定:

```json
{
  "Bot": {
    "MicrosoftAppType": "MultiTenant",
    "MicrosoftAppId": "your-app-id-here",
    "MicrosoftAppPassword": "your-app-secret-here",
    "MicrosoftAppTenantId": ""
  }
}
```

#### 4. Dev Tunnel でローカルサーバーを公開

```bash
# 別のターミナルでアプリケーションを起動
cd /Volumes/TK3SSD/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run

# 別のターミナルで Dev Tunnel を起動
devtunnel host sales-support-agent

# 表示された URL を Bot のメッセージング エンドポイントに設定
# 例: https://abc123xyz-5001.euw.devtunnels.ms/api/messages
```

**ngrok を使う場合（非推奨）:**

```bash
ngrok http https://localhost:5001
# 表示された URL を使用
```

#### 5. Teams にボットを追加

**方法 A: Developer Portal 使用（推奨）**

1. [Teams Developer Portal](https://dev.teams.microsoft.com/) にアクセス
2. **Apps** → **New app**
   - App name: `営業支援エージェント`
   - Short description: `商談サマリを自動生成`
3. **App features** → **Bot**
   - Select: **Existing bot**
   - Bot ID: Azure で作成した App ID を入力
4. **Bot endpoint address**: Azure Bot の設定と同じ
5. **Personal** および **Team** スコープを有効化
6. **Publish** → **Download the app package**
7. Teams → **Apps** → **Manage your apps** → **Upload a custom app** → ダウンロードした zip をアップロード

**方法 B: App Studio 使用**

1. Teams で **App Studio** を検索してインストール
2. **Manifest editor** → **Create a new app**
3. 上記と同様の手順でマニフェストを作成
4. **Test and distribute** → **Install** でテスト

#### 6. 動作確認

1. Teams でボットを検索: `営業支援エージェント`
2. チャットを開始
3. メッセージ送信: `こんにちは`
4. ウェルカムメッセージが返ってくることを確認
5. `今週の商談サマリを教えて` と送信してエージェントをテスト

---     # Agent365 middleware 統合
│   │   ├── OllamaProvider.cs       # Agent365 middleware 統合
│   │   └── AzureOpenAIProvider.cs  # Agent365 middleware 統合
│   ├── MCP/                        # M365 ツール (MCP)
│   │   └── McpTools/
│   │       ├── OutlookEmailTool.cs
│   │       ├── OutlookCalendarTool.cs
│   │       ├── SharePointTool.cs
│   │       └── TeamsMessageTool.cs
│   └── Agent/
│       └── SalesAgent.cs           # エージェント本体
├── Bot/
│   ├── TeamsBot.cs                 # Teams Bot 実装
│   ├── BotController.cs            # メッセージングエンドポイント
│   └── AdapterWithErrorHandler.cs  # エラーハンドリング
├── Configuration/                  # 設定クラス
│   ├── LLMSettings.cs
│   ├── M365Settings.cs
│   └── BotSettings.cs
├── Telemetry/                      # Agent365 観測性
│   └── AgentMetrics.cs             # ActivitySource, Meter, Counter, Histogram
│       └── SalesAgent.cs           # エージェント本体
├── Bot/
│   ├── TeamsBot.cs                 # Teams Bot 実装
│   ├── BotController.cs            # メッセージングエンドポイント
│   └── AdapterWithErrorHandler.cs  # エラーハンドリング
├── Configuration/                  # 設定クラス
│   ├── LLMSettings.cs
│   ├── M365Settings.cs
│   └── BotSettings.cs
├── Models/
│   └── SalesSummaryModels.cs       # リクエスト/レスポンスモデル
├── Program.cs                      # アプリケーションエントリポイント
└── appsettings.json                # 設定ファイル
```
- **代替**: Ollama を使用する場合は `LLM__Provider=Ollama` に変更

### Dev Tunnel / ngrok が接続できない

**Dev Tunnel の場合:**
- `devtunnel user login` でログインしているか確認
- `devtunnel host sales-support-agent` が実行中か確認
- トンネル URL が正しく Azure Bot に設定されているか確認
- `--allow-anonymous` フラグが設定されているか確認
Dev Tunnel または ngrok が起動しているか確認
- Bot のメッセージングエンドポイントが正しいか確認
  - Dev Tunnel: `https://your-tunnel-id-5001.region.devtunnels.ms/api/messages`
  - ngrok: `https://your-ngrok-url.ngrok.io/api/messages`
- Bot の App ID と Password が正しいか確認
- Teams にボットが正しくサイドロードされているか確認
- Azure Portal の Bot Service で「Test in Web Chat」を試す
- `dotnet run` でアプリケーションが起動しBot 設定を更新

---

## 🔍 デモシナリオ

### シナリオ 1: 今週の商談サマリ

Teams で以下のようにメンション:

```
@営業支援エージェント 今週の商談サマリを教えて
```

エージェントが以下の情報を収集:
- 📧 Outlook: 商談関連メール
- 📅 Calendar: 商談予定
- 📁 SharePoint: 提案書・見積書（Microsoft Search API で日付範囲検索）
- 📢 Teams: 商談チャネルの会話

**応答は Adaptive Card で視覚的に表示されます:**
- 📊 構造化されたサマリー表示
- 🎨 セクションごとに整理された見やすいレイアウト
- ⏱️ タイムスタンプ表示
- 🤖 LLM プロバイダー情報

そして、統合されたレポートを返します。

### シナリオ 2: 特定の顧客に関する情報

```
@営業支援エージェント 〇〇社に関する情報をまとめて
```

エージェントが顧客名でフィルタリングして情報を収集します。

---

## 🛡️ セキュリティとガバナンス

本デモでは以下のセキュリティベストプラクティスを実装:

- ✅ **Agent Identity**: アプリケーション専用の ID を使用し、ユーザー権限を委任しない
- ✅ **最小権限の原則**: 必要最小限の Graph API 権限のみを付与
- ✅ **シークレット管理**: クライアントシークレットは環境変数で管理
- ✅ **OpenTelemetry**: すべての API 呼び出しを観測可能
- ✅ **認証の一元管理**: GraphServiceClient を DI コンテナでシングルトン管理
- ✅ **Managed Identity 対応**: Azure 環境でのシークレットレス認証に対応
- ✅ **トークンキャッシュ最適化**: 認証トークンの再利用によるパフォーマンス向上

### 認証モード

#### ローカル開発環境
```json
{
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "UseManagedIdentity": false
  }
}
```

#### Azure 本番環境 (Managed Identity)
```json
{
  "M365": {
    "ClientId": "your-client-id",
    "UseManagedIdentity": true
  }
}
```

Managed Identity を使用すると、シークレット管理が不要になり、セキュリティが大幅に向上します。

---

## 📊 観測性 (OpenTelemetry)

アプリケーションは OpenTelemetry を統合し、以下の情報をコンソールに出力:

- **トレース**: エージェント実行フロー
- **メトリクス**: 処理時間、API 呼び出し回数
- **ログ**: エラー、警告、情報ログ

本番環境では、Azure Monitor やその他の observability プラットフォームに送信可能です。

---

## ⚠️ トラブルシューティング

### LM Studio に接続できない

- LM Studio の Local Server が起動しているか確認
- エンドポイント URL が正しいか確認 (`http://localhost:1234/v1`)
- モデルがロードされているか確認

### M365 データにアクセスできない

- Agent Identity の権限が正しく設定されているか確認
- 管理者の同意が付与されているか確認
- `M365__TenantId`, `M365__ClientId`, `M365__ClientSecret` が正しいか確認

### Teams Bot が応答しない

- Dev Tunnel または ngrok が起動しているか確認
- Bot のメッセージングエンドポイントが正しいか確認
  - Dev Tunnel: `https://your-tunnel-id-5001.region.devtunnels.ms/api/messages`
  - ngrok: `https://your-ngrok-url.ngrok.io/api/messages`
- Bot の App ID と Password が正しいか確認
- Teams にボットが正しくサイドロードされているか確認
- Azure Portal の Bot Service で「Test in Web Chat」を試す
- `dotnet run` でアプリケーションが起動していることを確認

---

## 📚 追加ドキュメント

- [Dev Tunnel セットアップガイド](docs/DEV-TUNNEL-SETUP.md) - 固定 URL でのトンネル作成
- [Adaptive Cards 実装ガイド](docs/ADAPTIVE-CARDS-GUIDE.md) - 視覚的な応答カードの作成方法
- [SharePoint Search API ガイド](docs/SHAREPOINT-SEARCH-API.md) - Microsoft Search API による高度な検索
- [Agent Identity 設定ガイド](docs/AGENT-IDENTITY-SETUP.md) - Microsoft 365 認証設定（作成予定）
- [Teams Bot マニフェスト](docs/TEAMS-MANIFEST.md) - Teams アプリ設定（作成予定）

---

## 📝 TODO / 今後の改善

### ✅ 完了

- [x] Dev Tunnel 固定 URL サポート
- [x] ビルドエラーの修正 (Microsoft.Extensions.AI.OpenAI 互換性)
- [x] **Agent365 SDK ハイブリッド実装完了** (2025-01)
  - Microsoft.Agents.A365.Observability.Extensions.AgentFramework (beta)
  - Microsoft.Agents.A365.Tooling.Extensions.AgentFramework (beta)
  - Microsoft.Agents.A365.Notifications (beta)
  - Microsoft.Agents.Storage + Transcript
  - IChatClient middleware chain (UseFunctionInvocation + UseOpenTelemetry)
  - AgentMetrics telemetry (ActivitySource, Meter, Counter, Histogram)
- [x] **認証パターンの改善** (2026-02)
  - GraphServiceClient の DI コンテナ管理
  - TokenCredential の一元化（ClientSecretCredential / DefaultAzureCredential）
  - Managed Identity 対応（Azure 環境でシークレットレス認証）
  - トークンキャッシュ最適化とリトライポリシー
- [x] **SharePoint Search API の実装** (2026-02)
  - Microsoft Search API (`/search/query`) による高度な検索
  - 日付範囲フィルタリング（LastModifiedTime）
  - キーワード OR 検索対応
  - ファイルメタデータ取得（サイズ、拡張子、更新者）
- [x] **Adaptive Cards での応答** (2026-02)
  - 営業サマリーカード（構造化表示、セクション分割）
  - エラーカード（視覚的なエラー通知）
  - ウェルカムカード（初回ガイダンス）
  - タイムスタンプ・LLM プロバイダー情報表示

### 🚧 進行中

なし

### 📋 計画中

- [ ] 多言語対応（英語・日本語切り替え）
- [ ] カスタム MCP サーバーの追加
- [ ] Azure にデプロイするための Dockerfile
- [ ] CI/CD パイプライン設定（GitHub Actions）
- [ ] Application Insights への直接統合
- [ ] ユニットテスト・統合テストの追加

---

## 📄 ライセンス

このプロジェクトは MIT ライセンスのもとで公開されています。

---

## 🙋 サポート

質問や問題がある場合は、Issue を作成してください。

---

**Agent 365 SDK** を使った営業支援エージェントのデモをお楽しみください！ 🚀
