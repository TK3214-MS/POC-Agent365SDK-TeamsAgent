# Security Best Practices - セキュリティベストプラクティス

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](11-SECURITY-BEST-PRACTICES.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../en/developer/11-SECURITY-BEST-PRACTICES.md)

## 📋 認証情報管理

### ❌ DON'T: ハードコーディング

```csharp
// BAD - 絶対にしない
var credential = new ClientSecretCredential(
    "tenant-id",
    "client-id",
    "hardcoded-secret"  // セキュリティ違反
);
```

### ✅ DO: appsettings.json + 環境変数

```csharp
// appsettings.json（開発環境のみ）
{
  "M365": {
    "ClientSecret": "development-secret"
  }
}

// 本番環境 - 環境変数
export M365__ClientSecret="production-secret"
```

### ✅ DO: Azure Key Vault

```csharp
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
var credential = new DefaultAzureCredential();

builder.Configuration.AddAzureKeyVault(
    new Uri(keyVaultUrl),
    credential
);

// Key Vaultから自動的に取得
var clientSecret = builder.Configuration["M365:ClientSecret"];
```

##Managed Identity（本番推奨）

### System Assigned Managed Identity

```bash
# Azure App Service で有効化
az webapp identity assign \
  --name <app-name> \
  --resource-group <resource-group>
```

**Program.cs**:

```csharp
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = m365Settings.ClientId,
    ExcludeVisualStudioCredential = true,
    ExcludeVisualStudioCodeCredential = true
});
```

**メリット**:
- ✅ シークレット管理不要
- ✅ ローテーション不要
- ✅ 漏洩リスクゼロ

## データ保護

### PIIフィルタリング

```csharp
public static string MaskPII(string text)
{
    // メールアドレスマスク
    text = Regex.Replace(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
        "***@***.***");
    
    // 電話番号マスク
    text = Regex.Replace(text, @"\d{3}-\d{4}-\d{4}", "***-****-****");
    
    return text;
}
```

**ログ記録時に適用**:

```csharp
_logger.LogInformation(
    "メール送信: From={From}, Subject={Subject}",
    MaskPII(email.From),
    email.Subject
);
```

### テレメトリからセンシティブ情報を除外

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) =>
            {
                // Authorizationヘッダーをトレースしない
                return !httpContext.Request.Headers.ContainsKey("Authorization");
            };
        })
    );
```

## APIセキュリティ

### 認証ミドルウェア

```csharp
app.UseAuthentication();
app.UseAuthorization();

// Bot エンドポイントは認証済み
app.MapControllers().RequireAuthorization();
```

### CORS設定

```csharp
// ❌ BAD - すべて許可
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin());
});

// ✅ GOOD - 特定オリジンのみ
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://teams.microsoft.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### レート制限

```csharp
// NuGet: AspNetCoreRateLimit
builder.Services.AddInMemoryRateLimiting();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});
```

## Graph APIアクセス制御

### 最小権限の原則

**必要な権限のみ付与**:

```json
{
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "...",
          "type": "Role",
          "value": "Mail.Read"  // Write権限は不要
        },
        {
          "id": "...",
          "type": "Role",
          "value": "Calendars.Read"
        }
      ]
    }
  ]
}
```

### UserId検証

```csharp
public class OutlookEmailTool
{
    private readonly string _allowedUserId;
    
    public async Task<string> SearchSalesEmails(string userId, ...)
    {
        if (userId != _allowedUserId)
        {
            throw new UnauthorizedAccessException("このユーザーのデータにはアクセスできません");
        }
        
        // Graph API呼び出し
    }
}
```

## HTTPS強制

**Program.cs**:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

## セキュリティヘッダー

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
    
    await next();
});
```

## 依存関係スキャン

### Dependabot設定

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
```

### 脆弱性スキャン

```bash
# NuGet脆弱性スキャン
dotnet list package --vulnerable --include-transitive

# OWASP Dependency Check
dotnet tool install -g dotnet-security-scan
security-scan SalesSupportAgent.csproj
```

## 次のステップ

- **[AUTHENTICATION.md](../AUTHENTICATION.md)**: 認証詳細ガイド
- **[DEPLOYMENT-AZURE.md](../DEPLOYMENT-AZURE.md)**: Azure本番デプロイ
