# 営業支援エージェント (Sales Support Agent)

**Microsoft Agent 365 SDK デモアプリケーション** - AIエージェントでMicrosoft 365データを活用

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![Agent 365](https://img.shields.io/badge/Agent%20365-SDK-blue)](https://github.com/microsoft/Agent365-Samples)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 📋 概要

Teams チャットで「@営業支援エージェント、今週の商談サマリを教えて」と話しかけると、エージェントが **Application-only認証**を使って安全に Microsoft 365 データへアクセスし、メール・カレンダー・SharePoint・Teams から情報を収集してレポートを返すデモアプリケーションです。

### 💡 主な特徴

| 特徴 | 説明 |
|-----|------|
| 🔐 **セキュアな認証** | Application-only認証でユーザー権限を委任せず安全にM365データアクセス |
| 🤖 **LLM切り替え対応** | Azure OpenAI / Ollama（ローカル） / その他のLLMプロバイダーに対応 |
| 💬 **Teams統合** | Bot FrameworkでTeamsに自然に統合、通知チャネルで返信 |
| 📊 **Observability Dashboard** | リアルタイムでエージェント動作を可視化、詳細トレース機能 |
| 🎨 **Adaptive Cards** | 視覚的でインタラクティブな応答 |
| 🔍 **高度な検索** | Microsoft Search API でSharePoint全文検索・日付範囲フィルタ |
| 📈 **Agent 365 SDK統合** | Microsoft公式のエージェントフレームワーク活用 |
| 🌐 **多言語対応** | 日本語・英語完全サポート |

### 🎯 ビジネス価値

- **自社専用の業務エージェント**をTeamsに組み込み、Copilotとは別の専門エージェントを構築
- **ガバナンス下でのデータアクセス**を実現し、セキュリティ要件を満たす
- **コスト最適化**: ローカルLLM（Ollama）対応でクラウドコスト削減可能
- **完全なカスタマイズ性**: 業務フローに合わせた独自エージェント開発

---

## 🏗️ アーキテクチャ概要

```
┌─────────────────────────────────────────────┐
│         Teams ユーザー (@メンション)          │
└──────────────────┬──────────────────────────┘
                   │ Bot Framework
                   ▼
┌──────────────────────────────────────────────────┐
│      営業支援エージェント (.NET 10)                │
│  ┌─────────────────────────────────────────┐    │
│  │  LLM Provider（切り替え可能）             │    │
│  │  - Azure OpenAI / Ollama / その他        │    │
│  └─────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────┐    │
│  │  MCP Tools (M365データアクセス)           │    │
│  │  📧 Outlook  📅 Calendar                │    │
│  │  📁 SharePoint  💬 Teams                │    │
│  └─────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────┐    │
│  │  Agent 365 SDK                          │    │
│  │  - Observability  - Adaptive Cards      │    │
│  └─────────────────────────────────────────┘    │
└──────────────────┬───────────────────────────────┘
                   │ Application-only認証
                   ▼
┌──────────────────────────────────────────────────┐
│         Microsoft 365 / Graph API                │
│   Outlook │ Calendar │ SharePoint │ Teams       │
└──────────────────────────────────────────────────┘
```

**詳細**: [アーキテクチャドキュメント](docs/ARCHITECTURE.md)

---

## 🚀 クイックスタート

### 前提条件

| 必須 | 推奨・環境 |
|------|---------|
| ✅ **.NET 10 SDK** | [ダウンロード](https://dotnet.microsoft.com/download/dotnet/10.0) |
| ✅ **LLM Provider** | Azure OpenAI / Ollama / その他 |
| ✅ **Microsoft 365 テナント** | [開発者プログラム](https://developer.microsoft.com/microsoft-365/dev-program) |
| ✅ **Azure サブスクリプション** | [無料アカウント](https://azure.microsoft.com/free/) |
| ⚪ **Dev Tunnel CLI** | ローカル→Teams接続用（推奨） |

### セットアップ（3ステップ）

#### 1️⃣ プロジェクトのクローン

```bash
git clone https://github.com/yourusername/POC-Agent365SDK-TeamsAgent.git
cd POC-Agent365SDK-TeamsAgent/SalesSupportAgent
```

#### 2️⃣ 設定ファイルの準備

最小限の設定例（`appsettings.json`）:

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key"
    }
  },
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

**詳細手順**: [Getting Startedガイド](docs/GETTING-STARTED.md)

#### 3️⃣ 実行

```bash
dotnet run
```

アプリケーションURL: `https://localhost:5192`

**動作確認**:
```bash
curl https://localhost:5192/health
```

---

## 👥 あなたに合ったドキュメントを選択

このプロジェクトには、異なるペルソナ（役割）に特化したドキュメントが用意されています。あなたの目的に合わせて、最適なドキュメントセットを選択してください。

### 🎓 初めて使う方・運用担当者向け

まずはAgentを動かしてみたい、Teams統合を試したい方はこちら：

- ✅ [スタートガイド（初心者向け）](docs/GETTING-STARTED.md) - 最初に読むべきドキュメント
- ✅ [認証設定](docs/AUTHENTICATION.md) - Azure AD App Registration、権限設定
- ✅ [Dev Tunnel設定](docs/DEV-TUNNEL-SETUP.md) - ローカル→Teams接続
- ✅ [Teams統合](docs/TEAMS-MANIFEST.md) - Botマニフェスト、サイドロード
- ✅ [サンプルデータ作成](docs/SAMPLE-DATA.md) - テストデータ生成
- ✅ [Observability Dashboard](docs/OBSERVABILITY-DASHBOARD.md) - リアルタイム監視
- ✅ [トラブルシューティング](docs/TROUBLESHOOTING.md) - よくある問題と解決方法

### 👨‍💻 開発者向け（コードを理解したい方）

SDK統合方法、コードレベルの理解を深めたい方はこちら：

#### 📚 基礎理解

- ✅ [SDK概要](docs/developer/01-SDK-OVERVIEW.md) - **最初に読む**: Microsoft 365 SDK、Agent 365 SDK、Microsoft.Extensions.AI の全体像
- ✅ [プロジェクト構造](docs/developer/02-PROJECT-STRUCTURE.md) - ディレクトリ構成、ファイル配置
- ✅ [依存性注入](docs/developer/05-DEPENDENCY-INJECTION.md) - DIコンテナの設計と使用

#### 🔐 認証とデータフロー

- ✅ [認証フロー](docs/developer/03-AUTHENTICATION-FLOW.md) - **重要**: Application-only認証の詳細解説、TokenCredential実装
- ✅ [データフロー](docs/developer/04-DATA-FLOW.md) - Graph API → LLM → Response の完全なフロー
- ✅ [SDK統合パターン](docs/developer/06-SDK-INTEGRATION-PATTERNS.md) - ベストプラクティスとデザインパターン

#### 🛠️ 実装詳細

- ✅ [エラーハンドリング](docs/developer/07-ERROR-HANDLING.md) - エラー種別と処理戦略
- ✅ [ロギング・テレメトリ](docs/developer/08-LOGGING-TELEMETRY.md) - 構造化ロギング、OpenTelemetry統合
- ✅ [テスト戦略](docs/developer/09-TESTING-STRATEGIES.md) - ユニット、統合、E2Eテスト
- ✅ [パフォーマンス最適化](docs/developer/10-PERFORMANCE-OPTIMIZATION.md) - Graph API最適化、並列実行
- ✅ [セキュリティ](docs/developer/11-SECURITY-BEST-PRACTICES.md) - Managed Identity、シークレット管理
- ✅ [拡張性](docs/developer/12-EXTENSIBILITY.md) - 新しいツール追加、LLMプロバイダー追加

#### 📖 コードウォークスルー

- ✅ [会話フロー詳細](docs/developer/13-CODE-WALKTHROUGHS/CONVERSATION-FLOW.md) - エンドツーエンドの実行フロー
- ✅ [Graph API呼び出し](docs/developer/13-CODE-WALKTHROUGHS/GRAPH-API-CALLS.md) - パターン別Graph API使用例
- ✅ [LLM推論プロセス](docs/developer/13-CODE-WALKTHROUGHS/LLM-INFERENCE.md) - Tool Calling、ストリーミング応答

#### 📋 リファレンス

- ✅ [API リファレンス](docs/developer/14-API-REFERENCE.md) - 主要クラスとインターフェース
- ✅ [マイグレーションガイド](docs/developer/15-MIGRATION-GUIDES.md) - .NET 8 → .NET 10 移行手順

---

## 📖 ドキュメント（カテゴリ別）

### 🎓 セットアップガイド

| ドキュメント | 内容 |
|------------|------|
| [**Getting Started**](docs/GETTING-STARTED.md) | 完全なセットアップ手順（初心者向け） |
| [**認証設定**](docs/AUTHENTICATION.md) | Azure AD App Registration、権限設定 |
| [**Dev Tunnel**](docs/DEV-TUNNEL-SETUP.md) | ローカル→Teams接続（固定URL） |
| [**Teams統合**](docs/TEAMS-MANIFEST.md) | Botマニフェスト、サイドロード手順 |

### 🔧 開発ガイド

| ドキュメント | 内容 |
|------------|------|
| [**アーキテクチャ**](docs/ARCHITECTURE.md) | システム設計、コンポーネント構成 |
| [**エージェント開発**](docs/AGENT-DEVELOPMENT.md) | エージェント実装パターン、MCP Tools |
| [**Adaptive Cards**](docs/ADAPTIVE-CARDS-GUIDE.md) | 視覚的な応答カード作成 |
| [**多言語対応**](docs/LOCALIZATION.md) | 日本語・英語切り替え |
| [**テスト**](docs/TESTING.md) | ユニットテスト、統合テスト戦略 |

### 🎨 運用ガイド

| ドキュメント | 内容 |
|------------|------|
| [**Observability Dashboard**](docs/OBSERVABILITY-DASHBOARD.md) | リアルタイム監視、詳細トレース |
| [**サンプルデータ作成**](docs/SAMPLE-DATA.md) | テストデータ生成（プロジェクトAPI使用） |
| [**Azure デプロイ**](docs/DEPLOYMENT-AZURE.md) | 本番環境デプロイ手順（App Service/Container Apps/AKS） |
| [**トラブルシューティング**](docs/TROUBLESHOOTING.md) | よくある問題と解決方法 |

---

## 🌟 主要機能

### Microsoft 365 データ統合

| データソース | MCP Tool | 取得内容 |
|------------|----------|---------|
| 📧 **Outlook** | OutlookEmailTool | 商談メール、提案書 |
| 📅 **Calendar** | OutlookCalendarTool | 商談予定、ミーティング |
| 📁 **SharePoint** | SharePointTool | ドキュメント、見積書（日付範囲検索） |
| 💬 **Teams** | TeamsMessageTool | チャネルの会話 |

### Observability Dashboard

リアルタイムでエージェントの動作を可視化：
- **エージェント監視**: アクティブな状態、最終アクティビティ
- **会話タイムライン**: ユーザーとのやり取りをトレース
- **詳細フェーズ表示**: AI実行の内部ロジックを確認
- **SignalR リアルタイム更新**: イベント発生時に即座に反映

**アクセス**: `https://localhost:5192/observability.html`

### LLM プロバイダー切り替え

設定ファイルで簡単に切り替え可能：

```json
// Azure OpenAI
{"LLM": {"Provider": "AzureOpenAI"}}

// Ollama（ローカル）
{"LLM": {"Provider": "Ollama"}}
```

---

## 🧪 デモシナリオ

### シナリオ 1: 今週の商談サマリ

```
@営業支援エージェント 今週の商談サマリを教えて
```

**エージェント動作**:
1. 📧 Outlookから商談メールを検索
2. 📅 Calendarから商談予定を取得
3. 📁 SharePointから提案書・見積書を検索
4. 💬 Teamsチャネルの会話を確認
5. 🤖 LLMで統合レポート生成
6. 🎨 Adaptive Cardで視覚的に返信

### シナリオ 2: 特定顧客の情報収集

```
@営業支援エージェント 株式会社サンプルテックに関する情報をまとめて
```

---

## 🔐 セキュリティ

| 項目 | 実装内容 |
|-----|---------|
| 🔒 **認証方式** | Application-only認証（ユーザー権限を委任しない） |
| 🔑 **シークレット管理** | Azure Key Vault統合（本番環境推奨） |
| 🛡️ **Managed Identity** | Azure環境でシークレットレス認証 |
| 👁️ **監査証跡** | OpenTelemetry、トランスクリプトロギング |

**詳細**: [認証設定ガイド](docs/AUTHENTICATION.md)

---

## ⚠️ トラブルシューティング

| 問題 | 解決方法 |
|-----|---------|
| ❌ **M365データにアクセスできない** | [認証設定](docs/AUTHENTICATION.md)で権限確認 |
| ❌ **Teams Botが応答しない** | [Dev Tunnel設定](docs/DEV-TUNNEL-SETUP.md)でエンドポイント確認 |
| ❌ **Dashboard切断** | SignalR Hub URL確認（/hubs/observability） |

**詳細**: [トラブルシューティングガイド](docs/TROUBLESHOOTING.md)

---

## 📄 ライセンス

このプロジェクトは [MIT License](LICENSE) のもとで公開されています。

---

## 🔗 関連リンク

- [Microsoft Agent 365 SDK](https://github.com/microsoft/Agent365-Samples)
- [Microsoft Graph API](https://learn.microsoft.com/graph/)
- [Bot Framework](https://dev.botframework.com/)
- [Adaptive Cards](https://adaptivecards.io/)

---

**Agent 365 SDK** を使った営業支援エージェントのデモをお楽しみください！ 🚀

