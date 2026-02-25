# Security Best Practices

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/11-SECURITY-BEST-PRACTICES.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](11-SECURITY-BEST-PRACTICES.md)

## 📋 Credential Management

### ❌ DON'T: Hardcoding

```csharp
// BAD - Never do this
var credential = new ClientSecretCredential(
    "tenant-id",
    "client-id",
    "hardcoded-secret"  // Security violation
);
```

### ✅ DO: appsettings.json + Environment Variables

```csharp
// appsettings.json (development environment only)
{
  "M365": {
    "ClientSecret": "development-secret"
  }
}

// Production environment - Environment variables
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

// Automatically retrieved from Key Vault
var clientSecret = builder.Configuration["M365:ClientSecret"];
```

## Managed Identity (Recommended for Production)

### System Assigned Managed Identity

```bash
# Enable on Azure App Service
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

**Benefits**:
- ✅ No secret management required
- ✅ No rotation required
- ✅ Zero leakage risk

## Data Protection

### PII Filtering

```csharp
public static string MaskPII(string text)
{
    // Email address masking
    text = Regex.Replace(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
        "***@***.***");
    
    // Phone number masking
    text = Regex.Replace(text, @"\d{3}-\d{4}-\d{4}", "***-****-****");
    
    return text;
}
```

**Applied when logging**:

```csharp
_logger.LogInformation(
    "Email sent: From={From}, Subject={Subject}",
    MaskPII(email.From),
    email.Subject
);
```

### Exclude Sensitive Information from Telemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (httpContext) =>
            {
                // Do not trace Authorization headers
                return !httpContext.Request.Headers.ContainsKey("Authorization");
            };
        })
    );
```

## API Security

### Authentication Middleware

```csharp
app.UseAuthentication();
app.UseAuthorization();

// Bot endpoints require authentication
app.MapControllers().RequireAuthorization();
```

### CORS Configuration

```csharp
// ❌ BAD - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin());
});

// ✅ GOOD - Specific origins only
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

### Rate Limiting

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

## Graph API Access Control

### Principle of Least Privilege

**Grant only the required permissions**:

```json
{
  "requiredResourceAccess": [
    {
      "resourceAppId": "00000003-0000-0000-c000-000000000000",
      "resourceAccess": [
        {
          "id": "...",
          "type": "Role",
          "value": "Mail.Read"  // Write permission is not needed
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

### UserId Validation

```csharp
public class OutlookEmailTool
{
    private readonly string _allowedUserId;
    
    public async Task<string> SearchSalesEmails(string userId, ...)
    {
        if (userId != _allowedUserId)
        {
            throw new UnauthorizedAccessException("Access to this user's data is not permitted");
        }
        
        // Graph API call
    }
}
```

## Enforcing HTTPS

**Program.cs**:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

## Security Headers

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

## Dependency Scanning

### Dependabot Configuration

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

### Vulnerability Scanning

```bash
# NuGet vulnerability scan
dotnet list package --vulnerable --include-transitive

# OWASP Dependency Check
dotnet tool install -g dotnet-security-scan
security-scan SalesSupportAgent.csproj
```

## Next Steps

- **[AUTHENTICATION.md](../AUTHENTICATION.md)**: Detailed Authentication Guide
- **[DEPLOYMENT-AZURE.md](../DEPLOYMENT-AZURE.md)**: Azure Production Deployment
