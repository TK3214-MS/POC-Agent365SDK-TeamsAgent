# Getting Started - 営業支援エージェント

## 📋 はじめに

このガイドでは、**営業支援エージェント**を初めてセットアップする方向けに、ゼロから動作確認まで完全な手順を説明します。

**所要時間**: 約60-90分

**目標**: ローカル環境で営業支援エージェントを起動し、APIで動作確認

---

## 🎯 完了時の状態

このガイドを完了すると、以下が実現できます：

- ✅ .NET 10環境のセットアップ完了
- ✅ LLMプロバイダー（Azure OpenAI / Ollama）の設定完了
- ✅ Microsoft 365との認証設定完了
- ✅ ローカルで営業支援エージェントが起動
- ✅ APIで商談サマリ生成が可能
- ✅ Observability Dashboardで動作監視が可能

---

## 📚 目次

1. [前提条件の確認](#1-前提条件の確認)
2. [開発環境のセットアップ](#2-開発環境のセットアップ)
3. [プロジェクトのクローン](#3-プロジェクトのクローン)
4. [LLMプロバイダーの選択と設定](#4-llmプロバイダーの選択と設定)
5. [Microsoft 365認証の設定](#5-microsoft-365認証の設定)
6. [アプリケーション設定](#6-アプリケーション設定)
7. [ビルドと起動](#7-ビルドと起動)
8. [動作確認](#8-動作確認)
9. [次のステップ](#9-次のステップ)

---

## 1. 前提条件の確認

### 必須アカウント・環境

| 項目 | 必須 | 説明 | 取得方法 |
|-----|:----:|------|---------|
| **Microsoft 365テナント** | ✅ | メール・カレンダーなどのデータアクセス用 | [開発者プログラム](https://developer.microsoft.com/microsoft-365/dev-program) |
| **Azureサブスクリプション** | ✅ | App Registration作成用 | [無料アカウント](https://azure.microsoft.com/free/) |
| **LLMプロバイダー** | ✅ | Azure OpenAI または Ollama | 下記参照 |

### LLMプロバイダーの選択

以下のいずれかを選択してください：

#### オプション A: Azure OpenAI（推奨）

- **メリット**: 高性能、安定、エンタープライズ対応
- **コスト**: 従量課金（GPT-4o: $5-15/1Mトークン）
- **必要なもの**: Azureサブスクリプション、リソース作成
- **セットアップ時間**: 15-20分

#### オプション B: Ollama（ローカルLLM）

- **メリット**: 完全無料、オフライン動作、プライバシー保護
- **コスト**: 無料（ハードウェアのみ）
- **必要なもの**: 十分なメモリ（16GB以上推奨）
- **セットアップ時間**: 10-15分
- **ダウンロード**: [Ollama公式サイト](https://ollama.ai/)

### ハードウェア要件

| 項目 | 最小要件 | 推奨要件 |
|-----|---------|---------|
| **CPU** | 2コア | 4コア以上 |
| **メモリ** | 8GB | 16GB以上（Ollama使用時は必須） |
| **ストレージ** | 10GB | 20GB以上 |
| **OS** | Windows 10/11, macOS 12+, Linux | - |

---

## 2. 開発環境のセットアップ

### 2.1. .NET 10 SDKのインストール

#### Windows

```powershell
# Microsoft公式サイトからダウンロード
# https://dotnet.microsoft.com/download/dotnet/10.0

# インストール後、確認
dotnet --version
# 出力例: 10.0.0
```

#### macOS

```bash
# Homebrewを使用
brew install dotnet@10

# または公式インストーラー
# https://dotnet.microsoft.com/download/dotnet/10.0

# 確認
dotnet --version
```

#### Linux (Ubuntu/Debian)

```bash
# Microsoft パッケージリポジトリを追加
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# .NET 10 SDKをインストール
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# 確認
dotnet --version
```

### 2.2. Gitのインストール

```bash
# Windows: https://git-scm.com/download/win
# macOS: brew install git
# Linux: sudo apt-get install git

# 確認
git --version
```

### 2.3. エディタ/IDE（推奨）

以下のいずれかをインストール：

- **Visual Studio Code** (推奨): [ダウンロード](https://code.visualstudio.com/)
  - 拡張機能: C# Dev Kit, .NET Extension Pack
- **Visual Studio 2022**: [ダウンロード](https://visualstudio.microsoft.com/)
- **JetBrains Rider**: [ダウンロード](https://www.jetbrains.com/rider/)

### 2.4. curlまたはHTTPクライアント（動作確認用）

```bash
# curl（通常はプリインストール済み）
curl --version

# または Postman / Insomnia などのGUIツール
```

---

## 3. プロジェクトのクローン

### 3.1. リポジトリをクローン

```bash
# クローン
git clone https://github.com/yourusername/POC-Agent365SDK-TeamsAgent.git

# ディレクトリに移動
cd POC-Agent365SDK-TeamsAgent/SalesSupportAgent
```

### 3.2. プロジェクト構造の確認

```bash
ls -la

# 以下のようなファイル・ディレクトリが表示されます:
# Program.cs
# SalesSupportAgent.csproj
# appsettings.json
# Bot/
# Services/
# ...
```

---

## 4. LLMプロバイダーの選択と設定

選択したLLMプロバイダーに応じて、以下のいずれかを実施してください。

### オプション A: Azure OpenAI のセットアップ

#### 4.A.1. Azure OpenAI リソースの作成

1. **Azure Portal にアクセス**: [portal.azure.com](https://portal.azure.com)

2. **リソースの作成**:
   ```
   検索バーで「Azure OpenAI」を検索 → 作成
   ```

3. **基本設定**:
   - **サブスクリプション**: 任意
   - **リソースグループ**: 新規作成（例: `rg-salesagent-dev`）
   - **リージョン**: East US / Japan East など
   - **名前**: 一意の名前（例: `openai-salesagent-dev`）
   - **価格レベル**: Standard S0

4. **作成** → デプロイ完了まで待機（2-3分）

#### 4.A.2. モデルのデプロイ

1. 作成したリソースに移動
2. **「モデルのデプロイ」** → **「+ デプロイの作成」**
3. **モデル選択**:
   - **モデル**: gpt-4o（推奨） または gpt-4o-mini
   - **デプロイ名**: `gpt-4o`（この名前を記録）
   - **バージョン**: 最新
4. **デプロイを作成**

#### 4.A.3. エンドポイントとAPIキーの取得

1. **「キーとエンドポイント」** をクリック
2. 以下をコピーして保存：
   ```
   エンドポイント: https://your-resource.openai.azure.com
   キー 1: xxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```

#### 4.A.4. appsettings.json の設定

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

---

### オプション B: Ollama のセットアップ

#### 4.B.1. Ollamaのインストール

**macOS / Linux:**

```bash
# インストール
curl -fsSL https://ollama.ai/install.sh | sh

# 確認
ollama --version
```

**Windows:**

1. [Ollama Windows版](https://ollama.ai/download/windows)をダウンロード
2. インストーラーを実行
3. コマンドプロンプトで確認:
   ```cmd
   ollama --version
   ```

#### 4.B.2. モデルのダウンロード

推奨モデル: **Qwen2.5** (高性能・日本語対応)

```bash
# モデルをダウンロード（初回は10-15分程度）
ollama pull qwen2.5:latest

# 他のオプション:
# ollama pull llama3.2:latest       # Meta Llama 3.2 (8B)
# ollama pull mistral:latest        # Mistral (7B)
# ollama pull gemma2:latest         # Google Gemma 2 (9B)
```

#### 4.B.3. Ollamaサーバーの起動

```bash
# バックグラウンドでサーバーを起動
ollama serve

# 別のターミナルで動作確認
ollama list

# 出力例:
# NAME             ID              SIZE      MODIFIED
# qwen2.5:latest   abc123...       4.7GB     2 minutes ago
```

#### 4.B.4. appsettings.json の設定

```json
{
  "LLM": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelName": "qwen2.5:latest"
    }
  }
}
```

---

## 5. Microsoft 365認証の設定

### 5.1. Azure AD App Registrationの作成

#### ステップ 1: Azure Portal にアクセス

[Azure Portal](https://portal.azure.com) → **Microsoft Entra ID**

#### ステップ 2: アプリの登録

1. **「アプリ登録」** → **「+ 新規登録」**

2. **基本情報**:
   - **名前**: `SalesSupportAgent`
   - **サポートされているアカウントの種類**: `この組織ディレクトリのみ（シングルテナント）`
   - **リダイレクト URI**: 空欄

3. **「登録」** をクリック

#### ステップ 3: アプリケーション情報を記録

登録後、**「概要」** ページで以下をコピー：

```
アプリケーション (クライアント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
ディレクトリ (テナント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

#### ステップ 4: クライアントシークレットの作成

1. **「証明書とシークレット」** → **「+ 新しいクライアント シークレット」**

2. **設定**:
   - **説明**: `SalesSupportAgent Secret`
   - **有効期限**: 24ヶ月（推奨）

3. **「追加」** → 表示された**「値」**をコピー（1度しか表示されません）

```
クライアント シークレット: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

#### ステップ 5: API アクセス許可の設定

1. **「APIのアクセス許可」** → **「+ アクセス許可の追加」**

2. **「Microsoft Graph」** → **「アプリケーションの許可」**

3. 以下の権限を検索して追加：

| 権限 | 用途 |
|-----|------|
| `Mail.Read` | Outlookメール検索 |
| `Calendars.Read` | カレンダー予定検索 |
| `Files.Read.All` | SharePointファイルアクセス |
| `Sites.Read.All` | SharePointサイト・Search API |
| `ChannelMessage.Read.All` | Teamsメッセージ検索 |
| `Team.ReadBasic.All` | Teams基本情報取得 |

4. **「アクセス許可の追加」** をクリック

#### ステップ 6: 管理者の同意を付与 ⚠️

**重要**: この手順を忘れると動作しません

1. **「{組織名} に管理者の同意を付与します」** をクリック
2. 確認ダイアログで **「はい」** をクリック
3. すべての権限が **「✓ {組織名} に付与済み」** と表示されることを確認

---

## 6. アプリケーション設定

### 6.1. appsettings.json の編集

`SalesSupportAgent/appsettings.json` を開き、以下のセクションを更新：

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",  // または "Ollama"
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key-here"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelName": "qwen2.5:latest"
    }
  },
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "UseManagedIdentity": false
  },
  "Bot": {
    "MicrosoftAppType": "MultiTenant",
    "MicrosoftAppId": "",
    "MicrosoftAppPassword": "",
    "MicrosoftAppTenantId": ""
  },
  "Localization": {
    "DefaultLanguage": "ja"
  }
}
```

### 6.2. 環境変数での設定（推奨・オプション）

セキュリティ向上のため、シークレットは環境変数で設定することを推奨：

```bash
# macOS / Linux
export M365__TenantId="your-tenant-id"
export M365__ClientId="your-client-id"
export M365__ClientSecret="your-client-secret"
export LLM__AzureOpenAI__ApiKey="your-api-key"

# Windows PowerShell
$env:M365__TenantId="your-tenant-id"
$env:M365__ClientId="your-client-id"
$env:M365__ClientSecret="your-client-secret"
$env:LLM__AzureOpenAI__ApiKey="your-api-key"
```

---

## 7. ビルドと起動

### 7.1. 依存関係の復元

```bash
cd /path/to/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# NuGetパッケージを復元
dotnet restore
```

### 7.2. ビルド

```bash
dotnet build

# 成功時の出力例:
# ビルドに成功しました。
#     0 個の警告
#     0 エラー
```

### 7.3. 起動

```bash
dotnet run

# 起動ログが表示されます:
# ========================================
# 営業支援エージェント起動
# LLM Provider: AzureOpenAI
# M365 設定: ✅ 有効
# Bot 設定: ❌ 未設定
# ========================================
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:5192
```

### 7.4. 起動確認

別のターミナルで確認：

```bash
curl https://localhost:5192/health -k

# 期待される出力:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}
```

---

## 8. 動作確認

### 8.1. ヘルスチェック

```bash
curl https://localhost:5192/health -k
```

### 8.2. 商談サマリ生成テスト

```bash
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"今週の商談サマリを教えて"}' \
  -k
```

**初回実行時の注意**:
- メール・予定などのデータがない場合、空の結果が返ることがあります
- 先に[サンプルデータ作成](SAMPLE-DATA.md)を実施することを推奨

### 8.3. Observability Dashboard

ブラウザで以下にアクセス：

```
https://localhost:5192/observability.html
```

**確認事項**:
- ✅ 「接続済み」ステータス表示
- ✅ エージェント情報が表示される
- ✅ メトリクスが更新される

### 8.4. OpenAPI / Swagger UI

```
https://localhost:5192/swagger
```

利用可能なAPIエンドポイント一覧を確認できます。

---

## 9. 次のステップ

### ✅ 完了したこと

- [x] ローカル環境でエージェントが起動
- [x] LLMプロバイダー設定完了
- [x] Microsoft 365認証設定完了
- [x] 基本的な動作確認完了

### ▶️ 次に進むべきガイド

1. **テストデータ作成**
   - [サンプルデータ作成ガイド](SAMPLE-DATA.md)
   - プロジェクトのAPIでメール・予定を自動生成

2. **Teams統合**
   - [Dev Tunnelセットアップ](DEV-TUNNEL-SETUP.md)
   - [Teams Botマニフェスト設定](TEAMS-MANIFEST.md)
   - Teamsで「@営業支援エージェント」として動作

3. **Observability活用**
   - [Observability Dashboardガイド](OBSERVABILITY-DASHBOARD.md)
   - エージェント動作のリアルタイム監視

4. **カスタマイズ**
   - [エージェント開発ガイド](AGENT-DEVELOPMENT.md)
   - 独自のエージェント・MCP Toolsを追加

---

## ⚠️ トラブルシューティング

### ビルドエラー: "SDK not found"

```bash
# .NET SDKを確認
dotnet --list-sdks

# .NET 10がない場合は再インストール
```

### 起動エラー: "Port 5192 already in use"

```bash
# macOS / Linux
lsof -ti:5192 | xargs kill -9

# Windows
netstat -ano | findstr :5192
taskkill /PID <PID> /F
```

### M365データアクセスエラー: "Unauthorized (401)"

**原因**: 認証情報が間違っている、または管理者の同意が未付与

**対処**:
1. `appsettings.json` の `TenantId`, `ClientId`, `ClientSecret` を再確認
2. Azure Portal で管理者の同意が付与されているか確認
3. シークレットの有効期限を確認

### LLM接続エラー

**Azure OpenAI**:
```bash
# エンドポイント・キー・デプロイ名を再確認
# Azure Portal → リソース → キーとエンドポイント
```

**Ollama**:
```bash
# Ollamaサーバーが起動しているか確認
ollama list

# 起動していない場合
ollama serve
```

詳細は [トラブルシューティングガイド](TROUBLESHOOTING.md) を参照してください。

---

## 📚 関連ドキュメント

- [README](../README.md) - プロジェクト概要
- [アーキテクチャ](ARCHITECTURE.md) - システム設計詳細
- [認証設定](AUTHENTICATION.md) - App Registration詳細
- [サンプルデータ](SAMPLE-DATA.md) - テストデータ生成
- [Teams統合](TEAMS-MANIFEST.md) - Teamsへの組み込み
- [Azure デプロイ](DEPLOYMENT-AZURE.md) - 本番環境構築

---

**おめでとうございます！ 🎉**

営業支援エージェントのローカル環境セットアップが完了しました。次は実際のデータでエージェントを試してみましょう！
