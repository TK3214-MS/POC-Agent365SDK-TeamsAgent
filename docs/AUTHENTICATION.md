# Microsoft 365 認証設定ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../AUTHENTICATION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/AUTHENTICATION.md)

## 📋 概要

営業支援エージェントは**Application-only認証**（アプリケーション専用認証）を使用してMicrosoft 365データにアクセスします。

このガイドでは、Azure AD App Registrationの作成から権限設定、本番環境でのManaged Identity利用まで、完全な認証設定手順を説明します。

---

## 🎯 Application-only認証とは

### 特徴

| 特徴 | 説明 |
|-----|------|
| 🔐 **ユーザー権限を委任しない** | アプリケーション独自の権限でアクセス |
| 🤖 **バックグラウンド処理に最適** | Bot、スケジュールタスクなど |
| 🔑 **App ID + Secret/証明書** | ClientSecretCredential または Managed Identity |
| 📊 **組織全体のデータアクセス** | 特定ユーザーに依存しない |
| 🛡️ **監査証跡完備** | すべてのアクセスログが記録 |

### 委任認証との違い

| 項目 | Application-only | 委任認証 (Delegated) |
|-----|-----------------|---------------------|
| **認証方法** | App ID + Secret/証明書 | ユーザーログイン（OAuth） |
| **ユーザーコンテキスト** | なし | あり（サインインユーザー） |
| **アクセス範囲** | 組織全体（権限に応じて） | サインインユーザーのデータのみ |
| **用途** | Bot、自動化、サーバーアプリ | 対話型Web/Mobileアプリ |
| **Graph API権限** | Application Permissions | Delegated Permissions |

---

## 📚 目次

1. [Azure AD App Registrationの作成](#1-azure-ad-app-registrationの作成)
2. [API権限の設定](#2-api権限の設定)
3. [ローカル開発環境の設定](#3-ローカル開発環境の設定)
4. [Azure本番環境の設定](#4-azure本番環境の設定)
5. [セキュリティベストプラクティス](#5-セキュリティベストプラクティス)
6. [動作確認](#6-動作確認)
7. [トラブルシューティング](#7-トラブルシューティング)

---

## 1. Azure AD App Registrationの作成

### ステップ 1-1: Azure Portal にアクセス

1. [Azure Portal](https://portal.azure.com) を開く
2. **Microsoft Entra ID** に移動

### ステップ 1-2: アプリケーションを登録

1. **「アプリの登録」** → **「+ 新規登録」** をクリック

2. **基本情報を入力**:

| 項目 | 設定値 |
|-----|-------|
| **名前** | `SalesSupportAgent` |
| **サポートされているアカウントの種類** | `この組織ディレクトリのみ（シングルテナント）` |
| **リダイレクト URI** | 空欄（Application-only認証では不要） |

3. **「登録」** をクリック

### ステップ 1-3: アプリケーション情報を記録

登録完了後、**「概要」** ページで以下をコピーして保存：

```
アプリケーション (クライアント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
ディレクトリ (テナント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

**重要**: これらの値は後で必要になります。

---

## 2. API権限の設定

### ステップ 2-1: クライアントシークレットの作成

1. **「証明書とシークレット」** → **「+ 新しいクライアント シークレット」** をクリック

2. **設定**:
   - **説明**: `SalesSupportAgent Secret`
   - **有効期限**: **24ヶ月**（推奨） または **カスタム**

3. **「追加」** をクリック

4. **「値」** をコピー（⚠️ 1度しか表示されません）:
   ```
   クライアント シークレット: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```

**セキュリティ注意**:
- シークレットは安全な場所に保管
- `.gitignore` に追加してGitにコミットしない
- 本番環境では Azure Key Vault または Managed Identity を使用

---

### ステップ 2-2: Microsoft Graph API 権限の追加

1. **「APIのアクセス許可」** → **「+ アクセス許可の追加」** をクリック

2. **「Microsoft Graph」** → **「アプリケーションの許可」** を選択

3. 以下の権限を検索して追加：

#### 必須権限

| 権限 | 用途 | 重要度 |
|-----|------|:------:|
| **Mail.Read** | Outlookメール検索 | ✅ 必須 |
| **Calendars.Read** | カレンダー予定検索 | ✅ 必須 |
| **Files.Read.All** | SharePointファイルアクセス | ✅ 必須 |
| **Sites.Read.All** | SharePointサイト・Search API | ✅ 必須 |
| **ChannelMessage.Read.All** | Teamsメッセージ検索 | ✅ 必須 |
| **Team.ReadBasic.All** | Teams基本情報取得 | ✅ 必須 |

#### オプション権限

| 権限 | 用途 | 重要度 |
|-----|------|:------:|
| **User.Read.All** | ユーザー情報取得 | ⚪ オプション |
| **Group.Read.All** | グループ情報取得 | ⚪ オプション |

4. **「アクセス許可の追加」** をクリック

---

### ステップ 2-3: 管理者の同意を付与 ⚠️

**最重要ステップ**: この手順を忘れると動作しません

1. **「{組織名} に管理者の同意を付与します」** ボタンをクリック
2. 確認ダイアログで **「はい」** をクリック
3. すべての権限が **「✓ {組織名} に付与済み」** と表示されることを確認

**確認方法**:
- 「状態」列がすべて緑色のチェックマーク
- 「{組織名} に付与済み」と表示

---

## 3. ローカル開発環境の設定

### 方法 A: appsettings.json（シンプル）

`SalesSupportAgent/appsettings.json` を編集：

```json
{
  "M365": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "UseManagedIdentity": false
  }
}
```

**注意**: シークレットをGitにコミットしないこと

---

### 方法 B: 環境変数（推奨）

シークレットを環境変数で管理：

**macOS / Linux**:
```bash
export M365__TenantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientSecret="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export M365__UseManagedIdentity=false

# .zshrc または .bashrc に追加して永続化
echo 'export M365__TenantId="your-tenant-id"' >> ~/.zshrc
```

**Windows PowerShell**:
```powershell
$env:M365__TenantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
$env:M365__ClientId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
$env:M365__ClientSecret="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
$env:M365__UseManagedIdentity="false"

# 永続化（ユーザー環境変数）
[System.Environment]::SetEnvironmentVariable('M365__TenantId', 'your-tenant-id', 'User')
```

---

### 方法 C: User Secrets（.NET推奨）

```bash
cd /path/to/SalesSupportAgent

# User Secretsを初期化
dotnet user-secrets init

# シークレットを設定
dotnet user-secrets set "M365:TenantId" "your-tenant-id"
dotnet user-secrets set "M365:ClientId" "your-client-id"
dotnet user-secrets set "M365:ClientSecret" "your-client-secret"

# 確認
dotnet user-secrets list
```

**メリット**:
- Gitにコミットされない（`%APPDATA%\Microsoft\UserSecrets`に保存）
- プロジェクトごとに管理
- チーム開発で安全

---

## 4. Azure本番環境の設定

### 4.1. Managed Identity の概要

**Managed Identity**は、Azure環境で**シークレット不要**で認証できる仕組みです。

| メリット | 説明 |
|---------|------|
| 🔐 **シークレット管理不要** | Azureが自動的に資格情報を管理 |
| 🔄 **自動ローテーション** | 定期的にクレデンシャルが更新される |
| 🛡️ **漏洩リスクゼロ** | シークレットが設定ファイルに存在しない |
| ✅ **推奨方式** | Microsoft公式推奨のセキュリティベストプラクティス |

---

### 4.2. App Service での Managed Identity 設定

#### ステップ 1: Managed Identity を有効化

1. **Azure Portal** → **App Service** を選択
2. **「ID」** → **「システム割り当て済み」** タブ
3. **「状態」** を **「オン」** に変更
4. **「保存」** をクリック
5. **オブジェクト (プリンシパル) ID** が表示される（コピーして保存）

#### ステップ 2: App Registration に権限付与

1. **Microsoft Entra ID** → **アプリの登録** → 作成したアプリを選択
2. **「APIのアクセス許可」** → 権限が設定済みであることを確認
3. **注意**: Managed IdentityはApp Registrationとは別のサービスプリンシパル

#### ステップ 3: Graph API 権限を付与（PowerShell）

```powershell
# Microsoft Graph PowerShell モジュールをインストール
Install-Module Microsoft.Graph -Scope CurrentUser

# 接続
Connect-MgGraph -Scopes "Application.ReadWrite.All", "AppRoleAssignment.ReadWrite.All"

# Managed Identity のオブジェクトIDを取得（App Serviceで確認したID）
$managedIdentityId = "your-managed-identity-object-id"

# Microsoft Graph のサービスプリンシパルIDを取得
$graphServicePrincipal = Get-MgServicePrincipal -Filter "displayName eq 'Microsoft Graph'"

# 必要なApp Roleを取得
$mailReadRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Mail.Read"}
$calendarsReadRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Calendars.Read"}
$filesReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Files.Read.All"}
$sitesReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Sites.Read.All"}
$channelMessageReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "ChannelMessage.Read.All"}
$teamReadBasicAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Team.ReadBasic.All"}

# App Role割り当て
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $mailReadRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $calendarsReadRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $filesReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $sitesReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $channelMessageReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $teamReadBasicAllRole.Id
```

#### ステップ 4: アプリケーション設定

**App Service の設定**:
```json
{
  "M365": {
    "ClientId": "app-registration-client-id",
    "UseManagedIdentity": true
  }
}
```

**注意**: `TenantId` と `ClientSecret` は不要

---

### 4.3. Container Apps での Managed Identity 設定

Container AppsでもManaged Identityが利用可能：

```bash
# Container Apps で Managed Identity を有効化
az containerapp identity assign \
  --name your-container-app \
  --resource-group your-resource-group \
  --system-assigned

# 出力されたprincipalIdを使用してGraph API権限を付与（上記PowerShellスクリプト参照）
```

---

### 4.4. Azure Key Vault 統合（オプション）

シークレットをKey Vaultで管理する高度な方法：

#### ステップ 1: Key Vault にシークレットを保存

```bash
# Key Vaultを作成
az keyvault create \
  --name salesagent-vault \
  --resource-group your-resource-group \
  --location eastus

# シークレットを保存
az keyvault secret set \
  --vault-name salesagent-vault \
  --name M365ClientSecret \
  --value "your-client-secret"
```

#### ステップ 2: App Service にアクセス許可

```bash
# App Service の Managed Identity に Key Vault アクセス許可
az keyvault set-policy \
  --name salesagent-vault \
  --object-id <app-service-managed-identity-id> \
  --secret-permissions get list
```

#### ステップ 3: appsettings.json で参照

```json
{
  "M365": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://salesagent-vault.vault.azure.net/secrets/M365ClientSecret/)"
  }
}
```

---

## 5. セキュリティベストプラクティス

### ✅ 推奨事項

| 項目 | ローカル開発 | Azure本番環境 |
|-----|------------|-------------|
| **認証方式** | ClientSecretCredential | Managed Identity |
| **シークレット管理** | User Secrets / 環境変数 | Key Vault / Managed Identity |
| **権限** | 最小限（Read系のみ） | 最小限（Read系のみ） |
| **ローテーション** | 6ヶ月ごと | 自動（Managed Identity） |
| **監査** | ローカルログ | Application Insights + Audit Logs |

### 🔐 シークレット管理のDo's and Don'ts

#### ✅ Do（推奨）

- ✅ User Secrets, 環境変数, Key Vault を使用
- ✅ `.gitignore` に `appsettings.json` を追加
- ✅ 定期的にシークレットをローテーション（3-6ヶ月）
- ✅ 本番環境では Managed Identity を使用
- ✅ 期限付きシークレット（24ヶ月以下）

#### ❌ Don't（禁止）

- ❌ appsettings.json にシークレットを直接記載してGitにコミット
- ❌ シークレットをコードにハードコーディング
- ❌ 無期限のシークレットを使用
- ❌ 本番環境でClientSecretを使用（Managed Identityを優先）
- ❌ 同じシークレットを複数環境で使い回し

---

### 🛡️ 権限の最小化

**原則**: 必要な権限のみを付与

| ❌ 過剰な権限 | ✅ 適切な権限 |
|------------|------------|
| `Mail.ReadWrite` | `Mail.Read` |
| `Files.ReadWrite.All` | `Files.Read.All` |
| `Sites.FullControl.All` | `Sites.Read.All` |

**理由**:
- セキュリティリスクの最小化
- コンプライアンス要件への対応
- 監査時の説明が容易

---

## 6. 動作確認

### 6.1. ローカル環境での確認

```bash
# アプリケーションを起動
cd /path/to/SalesSupportAgent
dotnet run

# 別のターミナルでヘルスチェック
curl https://localhost:5192/health -k

# 期待される出力:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"..."}
```

### 6.2. Graph API 接続テスト

```bash
# 商談サマリAPIを実行（Graph API を内部で呼び出す）
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"今週のメールを検索"}' \
  -k

# 成功時: メール情報が返される
# 失敗時: エラーメッセージを確認（下記トラブルシューティング参照）
```

### 6.3. 診断エンドポイント

```bash
# ユーザープロフィール取得テスト
curl https://localhost:5192/api/test/graph/profile -k

# メール取得テスト
curl "https://localhost:5192/api/test/graph/emails/raw?top=5" -k
```

---

## 7. トラブルシューティング

### エラー: "Unauthorized (401)"

**症状**:
```json
{
  "error": {
    "code": "InvalidAuthenticationToken",
    "message": "Access token validation failure"
  }
}
```

**原因と対処**:

| 原因 | 確認方法 | 対処 |
|-----|---------|------|
| TenantId が間違っている | Azure Portal で確認 | 正しい TenantId に修正 |
| ClientId が間違っている | Azure Portal で確認 | 正しい ClientId に修正 |
| ClientSecret が間違っている/期限切れ | 新しいシークレット作成 | 新しいシークレットに更新 |

---

### エラー: "Forbidden (403)"

**症状**:
```json
{
  "error": {
    "code": "Authorization_RequestDenied",
    "message": "Insufficient privileges to complete the operation"
  }
}
```

**原因と対処**:

1. **管理者の同意が未付与**:
   ```
   Azure Portal → アプリの登録 → APIのアクセス許可
   → 「管理者の同意を付与」をクリック
   ```

2. **必要な権限が不足**:
   ```
   必要な権限を追加 → 管理者の同意を再付与
   ```

3. **UserIdが間違っている**:
   ```json
   {
     "M365": {
       "UserId": "正しいユーザーID"  // Graph Explorerで確認
     }
   }
   ```

---

### エラー: "Managed Identity が機能しない"

**症状**:
```
ManagedIdentityCredential authentication failed: 
No managed identity endpoint found
```

**原因**: ローカル環境でManaged Identityを使用しようとしている

**対処**:
```json
{
  "M365": {
    "UseManagedIdentity": false  // ローカルでは false
  }
}
```

---

## 📚 関連ドキュメント

- [Getting Started](GETTING-STARTED.md) - 初期セットアップ
- [Troubleshooting](TROUBLESHOOTING.md) - 詳細なエラー対処
- [Architecture](ARCHITECTURE.md) - 認証フロー詳細
- [Deployment Azure](DEPLOYMENT-AZURE.md) - 本番環境構築

---

## 🔗 外部リンク

- [Microsoft Graph API ドキュメント](https://learn.microsoft.com/graph/)
- [Application-only認証](https://learn.microsoft.com/graph/auth-v2-service)
- [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
- [Graph 権限リファレンス](https://learn.microsoft.com/graph/permissions-reference)

---

認証設定が完了したら、次は [サンプルデータ作成](SAMPLE-DATA.md) でテストデータを生成しましょう！ 🚀
