# Security Best Practices - Authentication, Authorization, and Data Protection

> **Language**: [🇯🇵 日本語](../../developer/11-SECURITY-BEST-PRACTICES.md) | 🇬🇧 English

## 📋 Security Checklist

- [x] Managed Identity in production
- [x] Secrets in Azure Key Vault
- [x] Least-privilege API permissions
- [x] HTTPS enforcement
- [x] Input validation
- [x] PII filtering in logs

---

## Credential Management

### Use Managed Identity

```csharp
// ❌ NEVER - Hard-coded secrets
var credential = new ClientSecretCredential(
    "12345678-...",
    "abcd1234-...",
    "MySecretPassword123!"  // SECURITY RISK
);

// ✅ ALWAYS - Managed Identity
var credential = new DefaultAzureCredential();
```

### Azure Key Vault Integration

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:VaultUri"]),
    new DefaultAzureCredential());

// Access secrets
var clientSecret = builder.Configuration["M365--ClientSecret"]; // From Key Vault
```

---

## API Permissions (Least Privilege)

### Required Graph API Permissions

| Permission | Type | Justification |
|------------|------|---------------|
| `Mail.Read` | Application | Read emails |
| `Calendars.Read` | Application | Read calendar |
| `Sites.Read.All` | Application | SharePoint search |
| `Files.Read.All` | Application | Document access |

### ❌ Avoid Over-Permissioning

```
DON'T grant: Mail.ReadWrite (only need Read)
DON'T grant: Files.ReadWrite.All (only need Read)
```

---

## Input Validation

### Sanitize User Input

```csharp
public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        throw new ArgumentException("Query cannot be empty");
    }
    
    if (request.Query.Length > 1000)
    {
        throw new ArgumentException("Query too long");
    }
    
    // Sanitize for log injection
    var sanitizedQuery = request.Query.Replace("\n", " ").Replace("\r", " ");
    
    _logger.LogInformation("Processing query: {Query}", sanitizedQuery);
    
    // Proceed with processing
}
```

---

## PII Filtering

### Filter Sensitive Data from Logs

```csharp
public class PiiFilter : ILoggerProvider
{
    private static readonly Regex EmailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
    private static readonly Regex PhoneRegex = new Regex(@"\b\d{3}-\d{3}-\d{4}\b");

    public string FilterPii(string message)
    {
        message = EmailRegex.Replace(message, "[EMAIL]");
        message = PhoneRegex.Replace(message, "[PHONE]");
        return message;
    }
}
```

---

## HTTPS Enforcement

```csharp
// Enforce HTTPS in production
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

---

For complete security guidelines, OAuth flows, token management, and compliance considerations, please refer to the Japanese version at [../developer/11-SECURITY-BEST-PRACTICES.md](../../developer/11-SECURITY-BEST-PRACTICES.md).
