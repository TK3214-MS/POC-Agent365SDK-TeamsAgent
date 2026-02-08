# SharePoint Search API 実装ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../SHAREPOINT-SEARCH-API.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/SHAREPOINT-SEARCH-API.md)

## 📋 概要

本プロジェクトでは、**Microsoft Search API** を使用して SharePoint ドキュメントを検索しています。従来の Graph API の `/sites/{site-id}/drive/items` ではなく、`/search/query` エンドポイントを使用することで、より高度な検索機能を実現しています。

## 🔍 Microsoft Search API とは

Microsoft Search API は、Microsoft 365 全体（SharePoint, OneDrive, Teams, Outlook など）を横断的に検索できる統一された API です。

### 主な特徴

- ✅ **日付範囲フィルタリング**: LastModifiedTime でフィルタリング可能
- ✅ **キーワード検索**: OR/AND 演算子対応
- ✅ **全文検索**: ドキュメント内容も検索対象
- ✅ **ファイルメタデータ**: サイズ、拡張子、更新者などを取得
- ✅ **ページング**: 大量の結果を効率的に取得

## 🛠️ 実装詳細

### SharePointTool.cs の実装

```csharp
public async Task<string> SearchSalesDocuments(
    string startDate,
    string endDate,
    string keywords = "提案書,見積,見積もり,契約書,RFP")
{
    var start = DateTime.Parse(startDate);
    var end = DateTime.Parse(endDate);

    // キーワードをOR検索に変換
    var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
    var searchQuery = string.Join(" OR ", keywordList);

    // Microsoft Search API リクエストを構築
    var searchRequest = new SearchRequestObject
    {
        EntityTypes = new List<EntityType?> { EntityType.DriveItem },
        Query = new SearchQuery
        {
            QueryString = $"{searchQuery} AND LastModifiedTime>={start:yyyy-MM-dd} AND LastModifiedTime<={end:yyyy-MM-dd}"
        },
        From = 0,
        Size = 25,
        Fields = new List<string> 
        { 
            "title", "name", "lastModifiedDateTime", "lastModifiedBy", 
            "webUrl", "size", "fileExtension", "createdDateTime" 
        }
    };

    var requestBody = new QueryPostRequestBody
    {
        Requests = new List<SearchRequestObject> { searchRequest }
    };

    // Microsoft Search API を実行
    var searchResults = await _graphClient.Search.Query.PostAsync(requestBody);
    
    // 結果の処理...
}
```

### クエリ構文

#### 基本的なクエリ
```
提案書 OR 見積 OR 契約書
```

#### 日付範囲フィルタリング
```
LastModifiedTime>=2026-02-01 AND LastModifiedTime<=2026-02-05
```

#### 複合クエリ
```
(提案書 OR 見積) AND LastModifiedTime>=2026-02-01
```

#### ファイルタイプ指定
```
FileExtension:docx OR FileExtension:pdf
```

## 📊 取得できるフィールド

### ファイル基本情報

| フィールド | 説明 | 例 |
|-----------|------|-----|
| `title` | ドキュメントタイトル | "営業提案書_2026" |
| `name` | ファイル名 | "proposal.docx" |
| `webUrl` | SharePoint URL | "https://..." |

### ファイルメタデータ

| フィールド | 説明 | 例 |
|-----------|------|-----|
| `size` | ファイルサイズ（バイト） | 1048576 |
| `fileExtension` | 拡張子 | "docx" |
| `lastModifiedDateTime` | 最終更新日時 | "2026-02-05T10:30:00Z" |
| `lastModifiedBy` | 最終更新者 | "John Doe" |
| `createdDateTime` | 作成日時 | "2026-01-15T09:00:00Z" |

### カスタムメタデータ

SharePoint のカスタム列も取得可能（設定が必要）:
- `customField1`
- `customField2`

## 🔧 必要な権限

### Microsoft Graph API アプリケーション権限

```json
{
  "permissions": [
    "Files.Read.All",       // ファイルの読み取り
    "Sites.Read.All"        // SharePoint サイトの読み取り（Search API）
  ]
}
```

### Azure Portal での設定手順

1. **Azure Portal** → **Microsoft Entra ID** → **アプリ登録**
2. 対象のアプリケーションを選択
3. **API のアクセス許可** → **アクセス許可の追加**
4. **Microsoft Graph** → **アプリケーションの許可**
5. `Files.Read.All` にチェック
6. `Sites.Read.All` にチェック
7. **管理者の同意を付与** をクリック

## 📝 使用例

### 基本的な検索

```csharp
var result = await sharePointTool.SearchSalesDocuments(
    startDate: "2026-02-01",
    endDate: "2026-02-05",
    keywords: "提案書,見積"
);
```

**出力例**:
```
📁 **商談関連ドキュメント (15件)**

期間: 2026-02-01 ~ 2026-02-05
検索キーワード: 提案書,見積

- **営業提案書_ABC社.docx**
  更新日時: 2026-02-03T14:30:00Z
  ファイルサイズ: 2.5 MB
  拡張子: .docx
  URL: https://contoso.sharepoint.com/sites/sales/documents/proposal.docx

- **見積書_XYZ社.xlsx**
  更新日時: 2026-02-04T09:15:00Z
  ファイルサイズ: 512 KB
  拡張子: .xlsx
  URL: https://contoso.sharepoint.com/sites/sales/documents/quote.xlsx

💡 他に 13 件のドキュメントがあります。
```

### 特定のファイルタイプのみ検索

```csharp
// PDF のみ
keywords: "提案書 AND FileExtension:pdf"

// Word または Excel
keywords: "(FileExtension:docx OR FileExtension:xlsx) AND 提案書"
```

### 直近1週間のドキュメント

```csharp
var endDate = DateTime.Now.ToString("yyyy-MM-dd");
var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");

var result = await sharePointTool.SearchSalesDocuments(
    startDate: startDate,
    endDate: endDate,
    keywords: "商談,提案"
);
```

## 🧪 テスト方法

### ローカルでのテスト

1. **Microsoft 365 設定を完了**
   ```json
   {
     "M365": {
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret"
     }
   }
   ```

2. **アプリケーションを起動**
   ```bash
   dotnet run
   ```

3. **API エンドポイントを呼び出し**
   ```bash
   curl -X POST https://localhost:5001/api/sales-summary \
     -H "Content-Type: application/json" \
     -d '{"query":"今週の提案書を教えて"}'
   ```

### Teams でのテスト

1. Dev Tunnel でトンネルを作成
2. Teams で Bot に話しかける
   ```
   @営業支援エージェント 今週の商談関連ドキュメントを教えて
   ```
3. エージェントが SharePoint を検索して結果を返す

## 🔍 高度な使用例

### 1. 特定のフォルダ内を検索

```csharp
QueryString = $"(Path:'/sites/sales/documents/proposals') AND {searchQuery}"
```

### 2. 特定のユーザーが更新したファイル

```csharp
QueryString = $"{searchQuery} AND Author:'john@contoso.com'"
```

### 3. サイズでフィルタリング

```csharp
QueryString = $"{searchQuery} AND Size>1048576"  // 1MB以上
```

### 4. ページングの実装

```csharp
// 1ページ目
From = 0,
Size = 25

// 2ページ目
From = 25,
Size = 25
```

## 📊 パフォーマンス最適化

### 1. 必要なフィールドのみ取得

```csharp
Fields = new List<string> 
{ 
    "title", "webUrl", "lastModifiedDateTime"  // 最小限
}
```

### 2. サイズ制限

```csharp
Size = 10  // 少なめに設定してレスポンスタイムを改善
```

### 3. キャッシュの活用

```csharp
// Program.cs でメモリキャッシュを有効化
builder.Services.AddMemoryCache();

// SharePointTool でキャッシュを使用
private readonly IMemoryCache _cache;

public async Task<string> SearchSalesDocuments(...)
{
    var cacheKey = $"sharepoint_{startDate}_{endDate}_{keywords}";
    
    if (_cache.TryGetValue(cacheKey, out string cachedResult))
    {
        return cachedResult;
    }
    
    var result = await SearchInternal(...);
    
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
    return result;
}
```

## ⚠️ トラブルシューティング

### 1. 検索結果が0件

**原因**: 
- 権限不足（Sites.Read.All が付与されていない）
- SharePoint サイトへのアクセス権がない
- クエリ構文エラー

**対処**:
- Azure Portal で権限を確認
- SharePoint サイトの共有設定を確認
- クエリ文字列をログに出力して検証

### 2. "Forbidden" エラー

**原因**: 管理者の同意が必要

**対処**:
- Azure Portal → アプリ登録 → API のアクセス許可
- 「{組織名} に管理者の同意を付与する」をクリック

### 3. 日付フィルタリングが効かない

**原因**: 日付形式が間違っている

**対処**:
```csharp
// ✅ 正しい形式
start.ToString("yyyy-MM-dd")

// ❌ 間違った形式
start.ToString("MM/dd/yyyy")
```

### 4. AdditionalData が null

**原因**: フィールド名が間違っている

**対処**:
```csharp
// ✅ 正しいフィールド名
"lastModifiedDateTime"

// ❌ 間違ったフィールド名
"LastModifiedDateTime"  // 大文字小文字が違う
```

## 📚 参考リンク

- [Microsoft Search API ドキュメント](https://learn.microsoft.com/en-us/graph/api/resources/search-api-overview)
- [クエリ構文リファレンス](https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference)
- [Graph API Permissions](https://learn.microsoft.com/en-us/graph/permissions-reference)

## 💡 今後の拡張案

### 1. ファセット検索
```csharp
Aggregations = new List<AggregationOption>
{
    new AggregationOption { Field = "fileExtension" },
    new AggregationOption { Field = "lastModifiedBy" }
}
```

### 2. 並び替え
```csharp
SortProperties = new List<SortProperty>
{
    new SortProperty 
    { 
        Name = "lastModifiedDateTime", 
        IsDescending = true 
    }
}
```

### 3. ハイライト
```csharp
EnableTopResults = true
```

---

**Microsoft Search API** で強力な SharePoint 検索機能を実現しましょう！
