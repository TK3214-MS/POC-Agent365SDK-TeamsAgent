# トラブルシューティングガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TROUBLESHOOTING.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/TROUBLESHOOTING.md)

## 📋 概要

営業支援エージェントで発生する可能性のある問題と解決方法を説明します。

**このガイドで解決できること**:
- セットアップ時のエラー
- LLM接続エラー
- Microsoft 365認証エラー
- Teams Bot接続エラー
- Observability Dashboard エラー
- パフォーマンス問題

---

## 🔍 クイック診断

問題が発生したら、まず以下を確認してください：

```bash
# 1. ヘルスチェック
curl https://localhost:5192/health -k

# 期待される出力:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# 2. ログ確認
# アプリケーションのコンソール出力を確認

# 3. ポート確認
lsof -i :5192  # macOS/Linux
netstat -ano | findstr :5192  # Windows
```

---

## 📚 目次

1. [セットアップ関連](#1-セットアップ関連)
2. [LLM接続エラー](#2-llm接続エラー)
3. [Microsoft 365認証エラー](#3-microsoft-365認証エラー)
4. [Teams Bot エラー](#4-teams-bot-エラー)
5. [Observability Dashboard エラー](#5-observability-dashboard-エラー)
6. [パフォーマンス問題](#6-パフォーマンス問題)
7. [デバッグ手順](#7-デバッグ手順)

---

## 1. セットアップ関連

### エラー: "SDK version '10.0.xxx' not found"

**症状**:
```
A compatible .NET SDK was not found.
SDK version '10.0.xxx' is required
```

**原因**: .NET 10 SDKがインストールされていない、またはパスが通っていない

**解決方法**:

```bash
# 1. インストール済みSDKを確認
dotnet --list-sdks

# 2. .NET 10がない場合はインストール
# macOS: brew install dotnet@10
# Windows: https://dotnet.microsoft.com/download/dotnet/10.0
# Linux: apt-get install dotnet-sdk-10.0

# 3. 再度確認
dotnet --version  # 10.0.x が表示されることを確認
```

---

### エラー: "Port 5192 is already in use"

**症状**:
```
Failed to bind to address https://0.0.0.0:5192: address already in use
```

**原因**: 別のプロセスがポート5192を使用中

**解決方法**:

**macOS / Linux**:
```bash
# 使用中のプロセスを確認
lsof -ti:5192

# プロセスを終了
lsof -ti:5192 | xargs kill -9

# または別のポートを使用
dotnet run --urls="https://localhost:5193"
```

**Windows**:
```powershell
# 使用中のプロセスを確認
netstat -ano | findstr :5192

# プロセスIDを確認して終了
taskkill /PID <PID> /F
```

---

### エラー: "ビルドエラー: パッケージの復元に失敗"

**症状**:
```
error NU1102: Unable to find package 'Microsoft.Extensions.AI'
```

**原因**: NuGetパッケージソースの問題、またはネットワークエラー

**解決方法**:

```bash
# 1. NuGetキャッシュをクリア
dotnet nuget locals all --clear

# 2. パッケージを再復元
dotnet restore

# 3. 明示的にビルド
dotnet build --no-restore

# 4. エラーが続く場合はパッケージソースを確認
dotnet nuget list source
```

---

### エラー: "appsettings.json が見つからない"

**症状**:
```
Could not find a part of the path '.../appsettings.json'
```

**原因**: 作業ディレクトリが間違っている

**解決方法**:

```bash
# 正しいディレクトリに移動
cd /path/to/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# appsettings.jsonが存在することを確認
ls -la appsettings.json

# 実行
dotnet run
```

---

## 2. LLM接続エラー

### Azure OpenAI: "Unauthorized (401)"

**症状**:
```
Azure.RequestFailedException: Unauthorized
Status: 401 (Unauthorized)
```

**原因**: APIキー、エンドポイント、またはデプロイ名が間違っている

**解決方法**:

```bash
# 1. Azure Portalで確認
# リソース → キーとエンドポイント

# 2. appsettings.jsonを再確認
cat appsettings.json | grep -A5 "AzureOpenAI"

# 正しい設定:
# "Endpoint": "https://your-resource.openai.azure.com" (末尾にスラッシュなし)
# "DeploymentName": "gpt-4o" (モデル名ではなくデプロイ名)
# "ApiKey": "32文字の英数字"

# 3. エンドポイント接続テスト
curl https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01 \
  -H "api-key: your-api-key"
```

---

### Azure OpenAI: "DeploymentNotFound (404)"

**症状**:
```
The API deployment for this resource does not exist
Status: 404 (Not Found)
```

**原因**: デプロイ名が間違っている、またはデプロイが存在しない

**解決方法**:

```bash
# 1. Azure Portalでデプロイを確認
# リソース → モデルのデプロイ → デプロイ名をコピー

# 2. デプロイ一覧を取得
curl "https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01" \
  -H "api-key: your-api-key"

# 3. appsettings.jsonのDeploymentNameを修正
{
  "LLM": {
    "AzureOpenAI": {
      "DeploymentName": "実際のデプロイ名"  # 例: "gpt-4o-deployment"
    }
  }
}
```

---

### Ollama: "Connection refused"

**症状**:
```
HttpRequestException: Connection refused
Could not connect to http://localhost:11434
```

**原因**: Ollamaサーバーが起動していない

**解決方法**:

```bash
# 1. Ollamaサーバーを起動
ollama serve

# 別のターミナルで確認
curl http://localhost:11434/api/tags

# 期待される出力: {"models":[...]}

# 2. モデルがダウンロードされているか確認
ollama list

# 3. アプリケーションを再起動
```

---

### Ollama: "Model not found"

**症状**:
```
Error: model 'qwen2.5:latest' not found
```

**原因**: 指定したモデルがダウンロードされていない

**解決方法**:

```bash
# 1. モデルをダウンロード
ollama pull qwen2.5:latest

# 2. ダウンロード済みモデルを確認
ollama list

# 3. appsettings.jsonのModelNameを確認
{
  "LLM": {
    "Ollama": {
      "ModelName": "qwen2.5:latest"  # ollama listの NAME列と一致
    }
  }
}
```

---

## 3. Microsoft 365認証エラー

### エラー: "Unauthorized - Invalid client secret"

**症状**:
```
AADSTS7000215: Invalid client secret provided
Status: 401 (Unauthorized)
```

**原因**: ClientSecretが間違っているまたは期限切れ

**解決方法**:

```bash
# 1. Azure Portal で新しいシークレットを作成
# Microsoft Entra ID → アプリの登録 → アプリ選択
# → 証明書とシークレット → + 新しいクライアント シークレット

# 2. 表示された「値」をコピー（1度しか表示されない）

# 3. appsettings.jsonまたは環境変数を更新
{
  "M365": {
    "ClientSecret": "新しいシークレット"
  }
}

# または環境変数
export M365__ClientSecret="新しいシークレット"
```

---

### エラー: "Forbidden - Insufficient privileges"

**症状**:
```
ErrorCode: Authorization_RequestDenied
Message: Insufficient privileges to complete the operation
Status: 403 (Forbidden)
```

**原因**: 管理者の同意が付与されていない、または権限不足

**解決方法**:

```bash
# 1. Azure Portalで管理者の同意を確認
# Microsoft Entra ID → アプリの登録 → アプリ選択
# → APIのアクセス許可

# 2. すべての権限が「✓ (組織名) に付与済み」になっているか確認

# 3. 付与されていない場合:
# 「(組織名) に管理者の同意を付与します」をクリック → 「はい」

# 4. 必要な権限が追加されているか確認:
# - Mail.Read
# - Calendars.Read
# - Files.Read.All
# - Sites.Read.All
# - ChannelMessage.Read.All
# - Team.ReadBasic.All
```

---

### エラー: "TenantId が空です"

**症状**:
```
ArgumentException: TenantId cannot be null or empty
```

**原因**: appsettings.jsonまたは環境変数でM365設定が正しく読み込まれていない

**解決方法**:

```bash
# 1. appsettings.jsonを確認
cat appsettings.json | grep -A5 "M365"

# 2. 環境変数を確認
printenv | grep M365

# 3. 設定が正しいか確認
{
  "M365": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}

# 4. GUIDフォーマットが正しいか確認（ハイフン付き）
```

---

### エラー: "User not found"

**症状**:
```
Request_ResourceNotFound: User 'user-id' does not exist
```

**原因**: M365Settings.UserIdが間違っているまたは未設定

**解決方法**:

```bash
# 1. 自分のユーザーIDを取得
# Microsoft Graph Explorerを使用: https://developer.microsoft.com/graph/graph-explorer
# GET https://graph.microsoft.com/v1.0/me

# 2. または PowerShell
Connect-MgGraph
Get-MgUser -UserId "your-email@domain.com" | Select-Object -Property Id

# 3. appsettings.jsonを更新
{
  "M365": {
    "UserId": "取得したユーザーID"
  }
}

# 注: Application-only認証では、メールボックスアクセスにUserIdが必要
```

---

## 4. Teams Bot エラー

### エラー: "Bot is not responding"

**症状**: Teamsで@メンションしても応答がない

**診断手順**:

```bash
# 1. Dev Tunnel / ngrok が起動しているか確認
devtunnel list
# または
ngrok http https://localhost:5192

# 2. Tunnelエンドポイントを取得
# 例: https://abc123-5192.euw.devtunnels.ms

# 3. Azure Botのメッセージング エンドポイントを確認
# Azure Portal → Bot Services → 構成
# メッセージング エンドポイント:
# https://abc123-5192.euw.devtunnels.ms/api/messages
#                                      ↑ /api/messages 必須

# 4. アプリケーションが起動しているか確認
curl https://localhost:5192/health -k

# 5. ログを確認
# コンソールに以下が表示されるか:
# info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
#       Request starting HTTP/1.1 POST http://localhost:5192/api/messages
```

---

### エラー: "Unauthorized - AppId mismatch"

**症状**:
```
BotFrameworkAdapter.ProcessActivity: 401 Unauthorized
```

**原因**: appsettings.jsonのBot設定とAzure Botの設定が一致していない

**解決方法**:

```bash
# 1. Azure Portal → Bot Services → 構成 で以下を確認:
# - Microsoft アプリ ID
# - Microsoft アプリ テナント ID

# 2. appsettings.jsonのBot設定と照合
{
  "Bot": {
    "MicrosoftAppId": "Azure Portalのアプリ ID",
    "MicrosoftAppPassword": "クライアント シークレット",
    "MicrosoftAppTenantId": "テナント ID"
  }
}

# 3. シークレットが正しいか確認（期限切れの可能性）
# Microsoft Entra ID → アプリの登録 → 証明書とシークレット
```

---

### エラー: "Teams Manifest validation failed"

**症状**: アプリのインストール時にエラー

**解決方法**:

```bash
# 1. manifest.jsonを検証
# Teams Developer Portal: https://dev.teams.microsoft.com/
# Apps → Validate

# 2. よくある問題:
# - botId が Azure Botのアプリ IDと一致しない
# - validDomains にトンネルURLが含まれていない
# - version フォーマットが間違っている（例: "1.0.0"）

# 3. 正しいmanifest.json例:
{
  "bots": [{
    "botId": "your-app-id-from-azure-bot",
    "scopes": ["personal", "team"]
  }],
  "validDomains": ["*.devtunnels.ms"],
  "version": "1.0.0"
}
```

---

## 5. Observability Dashboard エラー

### エラー: "SignalR接続エラー - 404 Not Found"

**症状**: Dashboard上で「切断状態」が続く

**原因**: SignalR HubのURLパスが間違っている

**解決方法**:

```bash
# 1. Program.csでHubが正しくマッピングされているか確認
# app.MapHub<ObservabilityHub>("/hubs/observability");

# 2. observability.html のSignalR接続URLを確認
# const connection = new signalR.HubConnectionBuilder()
#     .withUrl("/hubs/observability")  # ← このパスがProgram.csと一致
#     .build();

# 3. ブラウザの開発者ツールで確認
# Network タブ → observability/negotiate リクエスト
# Status: 200 であることを確認

# 4. CORSエラーの確認
# Console タブにCORSエラーがないか確認
```

---

### エラー: "エージェント情報が表示されない"

**症状**: Dashboard上で「エージェント情報を取得中...」から進まない

**解決方法**:

```bash
# 1. API直接確認
curl https://localhost:5192/api/observability/agents -k

# 空の配列 [] が返る場合:
# → エージェントが未登録（アプリケーション起動時に自動登録されるはず）

# 2. Program.csでエージェント登録を確認
# lifetime.ApplicationStarted.Register(async () => { ... });

# 3. ログにエージェント登録メッセージがあるか確認
# "✅ Agent Identity作成成功" または
# "🤖 エージェント登録: 営業支援エージェント"

# 4. エラーがある場合、ObservabilityServiceの初期化を確認
```

---

## 6. パフォーマンス問題

### 問題: "レスポンスが非常に遅い (30秒以上)"

**原因**: LLMタイムアウト、大量データ取得、ネットワーク遅延

**診断**:

```bash
# 1. Observability Dashboardで詳細トレースを確認
# https://localhost:5192/observability.html
# → Recent Tracesから該当セッションを選択
# → どのフェーズで時間がかかっているか確認

# 2. 典型的なボトルネック:
# - "AI Agent Execution": LLMレスポンスが遅い
# - "Data Collection": Graph APIクエリが遅い
# - "SharePoint Search": 大量ドキュメント検索

# 3. 対処方法:
# - LLM: より高速なモデル使用（gpt-4o-mini）
# - Graph API: フィルタ条件を厳しく（TOP 10 → TOP 5）
# - SharePoint: 日付範囲を狭める（1ヶ月 → 1週間）
```

**最適化例**:

```csharp
// OutlookEmailTool.cs
var result = await _graphClient.Users[userId]
    .Messages
    .GetAsync(config =>
    {
        config.QueryParameters.Top = 5;  // 10 → 5 に削減
        config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };  // 必要なフィールドのみ
    });
```

---

### 問題: "メモリ使用量が高い"

**原因**: Ollamaモデルのメモリ消費、大量データキャッシュ

**対処**:

```bash
# 1. メモリ使用量確認
# macOS/Linux
ps aux | grep dotnet
top -pid $(pgrep -f dotnet)

# Windows
tasklist | findstr dotnet

# 2. Ollama使用時の対策:
# より小さいモデルを使用
ollama pull qwen2.5:7b  # 代わりに7Bモデル

# 3. .NET GC設定
# appsettings.jsonにGC設定追加
{
  "System.GC.Concurrent": true,
  "System.GC.Server": true,
  "System.GC.RetainVM": false
}
```

---

## 7. デバッグ手順

### 詳細ログの有効化

**appsettings.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "SalesSupportAgent": "Debug"  // ← Debugレベルに変更
    }
  }
}
```

### OpenTelemetry トレース確認

```bash
# コンソール出力で以下のようなトレースが表示されます:
# Activity.TraceId:            abc123...
# Activity.SpanId:             def456...
# Activity.TraceFlags:         Recorded
# Activity.ActivitySourceName: SalesSupportAgent
# Activity.DisplayName:        GenerateSalesSummary
# Activity.Kind:               Internal
# Activity.StartTime:          2026-02-08T10:00:00.0000000Z
# Activity.Duration:           00:00:06.4200000
#     SearchOutlookEmails: 850ms
#     SearchCalendarEvents: 620ms
#     SearchSharePointDocuments: 1250ms
#     LLM_Completion: 3200ms
```

### HTTP リクエストのデバッグ

```bash
# Fiddler / Charles Proxy などを使用してHTTP traffic をキャプチャ

# または curl でAPIを直接テスト
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"test"}' \
  --verbose \
  -k
```

---

## 📞 サポート

上記で解決しない場合：

1. **ログファイルを確認**: コンソール出力全体をコピー
2. **環境情報を収集**:
   ```bash
   dotnet --info
   cat appsettings.json | grep -v "Secret\|Key\|Password"
   ```
3. **Issue作成**: [GitHub Issues](https://github.com/yourusername/POC-Agent365SDK-TeamsAgent/issues)

---

## 📚 関連ドキュメント

- [Getting Started](GETTING-STARTED.md) - 初期セットアップ
- [認証設定](AUTHENTICATION.md) - Graph API認証詳細
- [アーキテクチャ](ARCHITECTURE.md) - システム構成
- [エージェント開発](AGENT-DEVELOPMENT.md) - カスタマイズ方法

---

問題が解決しましたら、他のガイドを参照して開発を続けてください！ 🚀
