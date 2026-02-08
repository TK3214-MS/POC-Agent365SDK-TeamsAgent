# SharePoint Search API Implementation Guide

> **Language**: [🇯🇵 日本語](../SHAREPOINT-SEARCH-API.md) | 🇬🇧 English

## 📋 Overview

This project uses the **Microsoft Search API** to search SharePoint documents. By using the `/search/query` endpoint instead of traditional Graph API's `/sites/{site-id}/drive/items`, we achieve more advanced search capabilities.

## 🔍 What is Microsoft Search API

Microsoft Search API is a unified API that can search across all of Microsoft 365 (SharePoint, OneDrive, Teams, Outlook, etc.).

### Key Features

- ✅ **Date Range Filtering**: Filter by LastModifiedTime
- ✅ **Keyword Search**: Support OR/AND operators
- ✅ **Full-text Search**: Search document contents
- ✅ **File Metadata**: Get size, extension, modifier, etc.
- ✅ **Paging**: Efficiently retrieve large result sets

## 🛠️ Implementation Details

### SharePointTool.cs Implementation

```csharp
public async Task<string> SearchSalesDocuments(
    string startDate,
    string endDate,
    string keywords = "提案書,見積,見積もり,契約書,RFP")
{
    var start = DateTime.Parse(startDate);
    var end = DateTime.Parse(endDate);

    // Convert keywords to OR search
    var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
    var searchQuery = string.Join(" OR ", keywordList);

    // Build Microsoft Search API request
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

    // Execute Microsoft Search API
    var searchResults = await _graphClient.Search.Query.PostAsync(requestBody);
    
    // Process results...
}
```

### Query Syntax

#### Basic Query
```
提案書 OR 見積 OR 契約書
```

#### Date Range Filtering
```
LastModifiedTime>=2026-02-01 AND LastModifiedTime<=2026-02-05
```

#### Compound Query
```
(提案書 OR 見積) AND LastModifiedTime>=2026-02-01
```

#### File Type Specification
```
FileExtension:docx OR FileExtension:pdf
```

## 📊 Available Fields

### File Basic Information

| Field | Description | Example |
|-------|-------------|---------|
| `title` | Document title | "Sales Proposal_2026" |
| `name` | File name | "proposal.docx" |
| `webUrl` | SharePoint URL | "https://..." |

### File Metadata

| Field | Description | Example |
|-------|-------------|---------|
| `size` | File size (bytes) | 1048576 |
| `fileExtension` | Extension | "docx" |
| `lastModifiedDateTime` | Last modified date/time | "2026-02-05T10:30:00Z" |
| `lastModifiedBy` | Last modifier | "John Doe" |
| `createdDateTime` | Creation date/time | "2026-01-15T09:00:00Z" |

### Custom Metadata

SharePoint custom columns are also retrievable (configuration required):
- `customField1`
- `customField2`

## 🔧 Required Permissions

### Microsoft Graph API Application Permissions

```json
{
  "permissions": [
    "Files.Read.All",       // Read files
    "Sites.Read.All"        // Read SharePoint sites (Search API)
  ]
}
```

### Azure Portal Configuration Steps

1. **Azure Portal** → **Microsoft Entra ID** → **App registrations**
2. Select target application
3. **API permissions** → **Add a permission**
4. **Microsoft Graph** → **Application permissions**
5. Check `Files.Read.All`
6. Check `Sites.Read.All`
7. Click **Grant admin consent**

## 📝 Usage Examples

### Basic Search

```csharp
var result = await sharePointTool.SearchSalesDocuments(
    startDate: "2026-02-01",
    endDate: "2026-02-05",
    keywords: "提案書,見積"
);
```

**Output Example**:
```
📁 **Sales-related Documents (15 items)**

Period: 2026-02-01 ~ 2026-02-05
Search keywords: 提案書,見積

- **Sales Proposal_ABC Corp.docx**
  Modified: 2026-02-03T14:30:00Z
  File size: 2.5 MB
  Extension: .docx
  URL: https://contoso.sharepoint.com/sites/sales/documents/proposal.docx

- **Quote_XYZ Corp.xlsx**
  Modified: 2026-02-04T09:15:00Z
  File size: 512 KB
  Extension: .xlsx
  URL: https://contoso.sharepoint.com/sites/sales/documents/quote.xlsx

💡 13 more documents available.
```

### Search Specific File Types Only

```csharp
// PDF only
keywords: "提案書 AND FileExtension:pdf"

// Word or Excel
keywords: "(FileExtension:docx OR FileExtension:xlsx) AND 提案書"
```

### Documents from Last Week

```csharp
var endDate = DateTime.Now.ToString("yyyy-MM-dd");
var startDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");

var result = await sharePointTool.SearchSalesDocuments(
    startDate: startDate,
    endDate: endDate,
    keywords: "商談,提案"
);
```

## 🧪 Testing Methods

### Local Testing

1. **Complete Microsoft 365 Configuration**
   ```json
   {
     "M365": {
       "TenantId": "your-tenant-id",
       "ClientId": "your-client-id",
       "ClientSecret": "your-client-secret"
     }
   }
   ```

2. **Start Application**
   ```bash
   dotnet run
   ```

3. **Call API Endpoint**
   ```bash
   curl -X POST https://localhost:5001/api/sales-summary \
     -H "Content-Type: application/json" \
     -d '{"query":"今週の提案書を教えて"}'
   ```

### Teams Testing

1. Create tunnel with Dev Tunnel
2. Talk to Bot in Teams
   ```
   @営業支援エージェント 今週の商談関連ドキュメントを教えて
   ```
3. Agent searches SharePoint and returns results

## 🔍 Advanced Usage Examples

### 1. Search Within Specific Folder

```csharp
QueryString = $"(Path:'/sites/sales/documents/proposals') AND {searchQuery}"
```

### 2. Files Updated by Specific User

```csharp
QueryString = $"{searchQuery} AND Author:'john@contoso.com'"
```

### 3. Filter by Size

```csharp
QueryString = $"{searchQuery} AND Size>1048576"  // 1MB or larger
```

### 4. Pagination Implementation

```csharp
// Page 1
From = 0,
Size = 25

// Page 2
From = 25,
Size = 25
```

## 📊 Performance Optimization

### 1. Retrieve Only Required Fields

```csharp
Fields = new List<string> 
{ 
    "title", "webUrl", "lastModifiedDateTime"  // Minimal
}
```

### 2. Size Limiting

```csharp
Size = 10  // Set lower to improve response time
```

### 3. Cache Utilization

```csharp
// Enable memory cache in Program.cs
builder.Services.AddMemoryCache();

// Use cache in SharePointTool
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

## ⚠️ Troubleshooting

### 1. Zero Search Results

**Cause**: 
- Insufficient permissions (Sites.Read.All not granted)
- No access to SharePoint site
- Query syntax error

**Solution**:
- Check permissions in Azure Portal
- Verify SharePoint site sharing settings
- Output query string to log and validate

### 2. "Forbidden" Error

**Cause**: Admin consent required

**Solution**:
- Azure Portal → App registrations → API permissions
- Click "Grant admin consent for {organization}"

### 3. Date Filtering Not Working

**Cause**: Incorrect date format

**Solution**:
```csharp
// ✅ Correct format
start.ToString("yyyy-MM-dd")

// ❌ Incorrect format
start.ToString("MM/dd/yyyy")
```

### 4. AdditionalData is null

**Cause**: Incorrect field name

**Solution**:
```csharp
// ✅ Correct field name
"lastModifiedDateTime"

// ❌ Incorrect field name
"LastModifiedDateTime"  // Wrong case
```

## 📚 References

- [Microsoft Search API Documentation](https://learn.microsoft.com/en-us/graph/api/resources/search-api-overview)
- [Query Syntax Reference](https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference)
- [Graph API Permissions](https://learn.microsoft.com/en-us/graph/permissions-reference)

## 💡 Future Enhancement Ideas

### 1. Faceted Search
```csharp
Aggregations = new List<AggregationOption>
{
    new AggregationOption { Field = "fileExtension" },
    new AggregationOption { Field = "lastModifiedBy" }
}
```

### 2. Sorting
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

### 3. Highlighting
```csharp
EnableTopResults = true
```

---

**Achieve powerful SharePoint search capabilities with Microsoft Search API!**
