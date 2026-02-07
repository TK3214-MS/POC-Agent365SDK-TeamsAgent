# Agent Identity 設定ガイド

## 📋 概要

**Agent Identity** は、Microsoft 365 において、アプリケーションが独自の ID を持ち、ユーザーの権限を委任することなく、安全に Microsoft 365 データへアクセスできる仕組みです。

このガイドでは、営業支援エージェントで使用する Agent Identity の設定手順を解説します。

---

## 🎯 Agent Identity とは

### 特徴

- ✅ **アプリケーション専用の ID**: ユーザーアカウントに依存しない
- ✅ **最小権限の原則**: 必要な権限のみを付与
- ✅ **監査証跡**: すべてのアクセスが記録される
- ✅ **セキュア**: クライアントシークレットまたは証明書で認証

### 従来の認証との違い

| 項目 | Agent Identity | 委任アクセス (Delegated) |
|-----|---------------|------------------------|
| **認証方法** | アプリケーション ID + シークレット | ユーザーログイン |
| **ユーザーコンテキスト** | なし | あり |
| **適用範囲** | 組織全体 | ログインユーザーのみ |
| **用途** | バックグラウンド処理、Bot | 対話型アプリ |

---

## 🚀 セットアップ手順

### 前提条件

- ✅ Azure サブスクリプション
- ✅ Microsoft Entra ID（旧 Azure AD）の管理者権限
- ✅ Microsoft 365 開発者環境（推奨）

---

### ステップ 1: アプリケーションの登録

#### 1-1. Azure Portal にアクセス

[Azure Portal](https://portal.azure.com) にアクセスし、Microsoft Entra ID に移動します。

```
Azure Portal → Microsoft Entra ID → アプリの登録
```

#### 1-2. 新しいアプリケーションを登録

1. **「+ 新規登録」** をクリック
2. 以下の情報を入力:

| 項目 | 設定値 |
|-----|-------|
| **名前** | `SalesSupportAgent` |
| **サポートされているアカウントの種類** | この組織ディレクトリのみ (シングルテナント) |
| **リダイレクト URI** | 空欄（今回は不要） |

3. **「登録」** をクリック

#### 1-3. アプリケーション情報を記録

登録完了後、以下の情報をコピーして保管します：

```
アプリケーション (クライアント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
ディレクトリ (テナント) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

---

### ステップ 2: クライアントシークレットの作成

#### 2-1. 証明書とシークレット

1. 登録したアプリケーションの詳細画面で、**「証明書とシークレット」** をクリック
2. **「+ 新しいクライアント シークレット」** をクリック

#### 2-2. シークレットの設定

| 項目 | 設定値 |
|-----|-------|
| **説明** | `SalesSupportAgent Secret` |
| **有効期限** | 24 ヶ月（推奨） |

3. **「追加」** をクリック
4. 表示された **「値」** を **すぐにコピー** （この画面でしか表示されません）

```
クライアント シークレット: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

⚠️ **重要**: シークレットは再表示できないため、必ず安全な場所に保管してください。

---

### ステップ 3: API アクセス許可の設定

#### 3-1. 必要な権限の追加

1. **「API のアクセス許可」** をクリック
2. **「+ アクセス許可の追加」** をクリック
3. **「Microsoft Graph」** を選択
4. **「アプリケーションの許可」** を選択

#### 3-2. 権限の選択

以下の権限を検索して追加します：

| 権限 | 用途 | 必須 |
|-----|------|-----|
| **Mail.Read** | Outlook メール検索 | ✅ |
| **Calendars.Read** | カレンダー予定検索 | ✅ |
| **Files.Read.All** | SharePoint ファイルアクセス | ✅ |
| **Sites.Read.All** | SharePoint サイト検索（Search API） | ✅ |
| **ChannelMessage.Read.All** | Teams メッセージ検索 | ✅ |
| **Team.ReadBasic.All** | Teams 基本情報取得 | ✅ |
| **User.Read.All** | ユーザー情報取得（オプション） | ⚪ |

#### 3-3. 管理者の同意を付与

⚠️ **重要**: アプリケーション権限には管理者の同意が必須です。

1. **「{組織名} に管理者の同意を付与する」** をクリック
2. 確認ダイアログで **「はい」** をクリック
3. すべての権限が **「✓ {組織名} に付与済み」** と表示されることを確認

---

### ステップ 4: アプリケーション設定

#### 4-1. appsettings.json の設定

[SalesSupportAgent/appsettings.json](/Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent/appsettings.json) を編集:

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

#### 4-2. 環境変数での設定（推奨）

セキュリティ向上のため、環境変数で設定することを推奨します。

**.env ファイル**:
```bash
M365__TenantId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
M365__ClientId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
M365__ClientSecret=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
M365__UseManagedIdentity=false
```

または直接エクスポート:
```bash
export M365__TenantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientSecret="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

---

## 🔐 セキュリティのベストプラクティス

### 1. シークレットの管理

#### ローカル開発環境

```bash
# .gitignore で除外
appsettings.json
.env
*.secret
```

#### Azure 本番環境

**Azure Key Vault を使用** (推奨):
```json
{
  "M365": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/SalesAgentSecret/)"
  }
}
```

### 2. Managed Identity の使用 (Azure 環境)

Azure App Service / Container Apps にデプロイする場合は、Managed Identity を使用してシークレットレス認証を実現できます。

#### Azure Portal での設定

1. **App Service** → **ID** → **システム割り当て** → **オン**
2. **Microsoft Entra ID** → 登録したアプリ → **認証** → **Managed Identity を許可**

#### appsettings.json

```json
{
  "M365": {
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "UseManagedIdentity": true
  }
}
```

シークレット不要で、Azure が自動的に認証します。

### 3. 権限の最小化

使用しない権限は付与しないでください。例：

- ❌ `Mail.ReadWrite` → ✅ `Mail.Read` で十分
- ❌ `Sites.FullControl.All` → ✅ `Sites.Read.All` で十分

### 4. 定期的なシークレットローテーション

- **推奨**: 3〜6ヶ月ごとにシークレットを再生成
- **手順**:
  1. 新しいシークレットを作成
  2. アプリケーション設定を更新
  3. 古いシークレットを削除

---

## ✅ 動作確認

### 1. ヘルスチェック

アプリケーションを起動して、M365 設定が有効か確認:

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run
```

ブラウザで確認:
```
https://localhost:5001/health
```

**期待される出力**:
```json
{
  "Status": "Healthy",
  "M365Configured": true,
  "LLMProvider": "GitHubModels"
}
```

### 2. Graph API 接続テスト

Outlook メール検索をテスト:

```bash
curl -X POST https://localhost:5001/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{
    "query": "今週のメールを検索"
  }'
```

**成功時**: メール情報が返される  
**失敗時**: エラーメッセージと対処方法を確認

---

## ⚠️ トラブルシューティング

### エラー 1: "Unauthorized" (401)

**原因**: 認証情報が間違っている

**対処**:
- Tenant ID, Client ID, Client Secret が正しいか確認
- シークレットの有効期限を確認
- `.env` ファイルが正しく読み込まれているか確認

### エラー 2: "Forbidden" (403)

**原因**: 権限が不足している

**対処**:
- 必要な権限がすべて追加されているか確認
- **管理者の同意が付与されているか** 確認（最も多い原因）
- Azure Portal で権限の状態を確認

### エラー 3: "TenantId が空です"

**原因**: 設定ファイルが読み込まれていない

**対処**:
```bash
# 環境変数を確認
printenv | grep M365

# appsettings.json を確認
cat appsettings.json | grep -A5 "M365"
```

### エラー 4: "Managed Identity が利用できません"

**原因**: ローカル環境で Managed Identity を使用しようとしている

**対処**:
```json
{
  "M365": {
    "UseManagedIdentity": false  // ローカルでは false に設定
  }
}
```

---

## 📊 権限の詳細説明

### Mail.Read

**用途**: Outlook メールを読み取り専用でアクセス

**取得できるデータ**:
- メールの件名、本文、送信者、受信者
- メールの受信日時
- 添付ファイルの有無

**制限**:
- メールの送信・削除・変更は不可
- ユーザーの代わりにメールを送信できない

### Calendars.Read

**用途**: カレンダー予定を読み取り専用でアクセス

**取得できるデータ**:
- 予定の件名、日時、場所
- 参加者情報
- 主催者情報

**制限**:
- 予定の作成・変更・削除は不可

### Files.Read.All

**用途**: OneDrive および SharePoint のファイルを読み取り

**取得できるデータ**:
- ファイルの内容
- ファイルのメタデータ（名前、サイズ、更新日時）

**制限**:
- ファイルの作成・変更・削除は不可

### Sites.Read.All

**用途**: SharePoint サイトと Microsoft Search API へのアクセス

**取得できるデータ**:
- SharePoint サイトの情報
- Microsoft Search API によるドキュメント検索
- サイトのメタデータ

**制限**:
- サイトの作成・変更・削除は不可

### ChannelMessage.Read.All

**用途**: Teams チャネルメッセージを読み取り

**取得できるデータ**:
- チャネルのメッセージ内容
- メッセージの投稿者、日時
- スレッド情報

**制限**:
- メッセージの投稿・削除は不可
- プライベートチャットは読み取れない

### Team.ReadBasic.All

**用途**: Teams の基本情報を取得

**取得できるデータ**:
- チームの名前、説明
- チャネル一覧
- チームのメンバー情報（基本）

**制限**:
- チームの作成・変更・削除は不可

---

## 🔗 参考リンク

- [Microsoft Graph API ドキュメント](https://learn.microsoft.com/ja-jp/graph/)
- [アプリケーション権限とは](https://learn.microsoft.com/ja-jp/graph/auth-v2-service)
- [Microsoft Graph 権限リファレンス](https://learn.microsoft.com/ja-jp/graph/permissions-reference)
- [Azure Key Vault の使用](https://learn.microsoft.com/ja-jp/azure/key-vault/)
- [Managed Identity とは](https://learn.microsoft.com/ja-jp/azure/active-directory/managed-identities-azure-resources/overview)

---

## 📝 まとめ

Agent Identity を正しく設定することで：

- ✅ セキュアな Microsoft 365 データアクセス
- ✅ ユーザー権限に依存しない安定した動作
- ✅ 監査証跡による可視性
- ✅ 最小権限によるリスク低減

を実現できます。

設定が完了したら、次は [Teams Bot マニフェスト設定](TEAMS-MANIFEST.md) に進んでください。
