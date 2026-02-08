# Dependency Injection - DI Container Design and Usage

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/05-DEPENDENCY-INJECTION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](05-DEPENDENCY-INJECTION.md)

## 📋 Table of Contents

- [DI Basic Patterns](#di-basic-patterns)
- [Service Lifetimes](#service-lifetimes)
- [Registration Patterns](#registration-patterns)
- [Best Practices](#best-practices)

---

## DI Basic Patterns

### Service Registration in Program.cs

```csharp
// Singleton - Single instance across application
builder.Services.AddSingleton<TokenCredential>(sp =>
{
    var settings = sp.GetRequiredService<M365Settings>();
    return new ClientSecretCredential(
        settings.TenantId,
        settings.ClientId,
        settings.ClientSecret
    );
});

builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    return new GraphServiceClient(credential, m365Settings.Scopes);
});

// MCP Tools
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();

// LLM Provider
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var settings = sp.GetRequiredService<LLMSettings>();
    return settings.Provider switch
    {
        "AzureOpenAI" => new AzureOpenAIProvider(settings.AzureOpenAI),
        "Ollama" => new OllamaProvider(settings.Ollama),
        _ => throw new NotSupportedException($"Provider {settings.Provider} not supported")
    };
});

// Agent
builder.Services.AddSingleton<SalesAgent>();

// Transient - New instance per request
builder.Services.AddTransient<IBot, TeamsBot>();
```

---

## Service Lifetimes

### Singleton

**Characteristics**:
- Created once at application startup
- Shared across all requests
- Must be thread-safe

**Usage Example**:
```csharp
builder.Services.AddSingleton<GraphServiceClient>(/* implementation */);
```

**Use Cases**:
- `GraphServiceClient`: Share token cache
- `ObservabilityService`: Aggregate metrics
- `NotificationService`: Manage SignalR connections

### Scoped

**Characteristics**:
- One instance per HTTP request
- Shared within request
- Disposed when request ends

**Usage Example**:
```csharp
builder.Services.AddScoped<MyDbContext>();
```

**Use Cases**:
- Database contexts
- Request-specific state management

### Transient

**Characteristics**:
- New instance created each time requested
- For lightweight services

**Usage Example**:
```csharp
builder.Services.AddTransient<IBot, TeamsBot>();
```

**Use Cases**:
- `TeamsBot`: Create new per conversation
- Stateless services

---

## Registration Patterns

### Pattern 1: Factory Pattern

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var settings = sp.GetRequiredService<LLMSettings>();
    
    logger.LogInformation("LLM Provider initialization: {Provider}", settings.Provider);
    
    return settings.Provider switch
    {
        "AzureOpenAI" => new AzureOpenAIProvider(settings.AzureOpenAI),
        "Ollama" => new OllamaProvider(settings.Ollama),
        _ => throw new InvalidOperationException($"Unsupported: {settings.Provider}")
    };
});
```

### Pattern 2: Options Pattern

```csharp
// Register configuration
builder.Services.Configure<M365Settings>(
    builder.Configuration.GetSection("M365"));

// Use configuration
public class OutlookEmailTool
{
    public OutlookEmailTool(IOptions<M365Settings> options)
    {
        var settings = options.Value;
        _userId = settings.UserId;
    }
}
```

### Pattern 3: Conditional Registration

```csharp
if (botSettings.IsConfigured)
{
    builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
    builder.Services.AddTransient<IBot, TeamsBot>();
}
else
{
    builder.Services.AddSingleton<IBotFrameworkHttpAdapter, NullBotAdapter>();
}
```

---

## Best Practices

### ✅ DO

```csharp
// 1. Depend on interfaces
public class SalesAgent
{
    private readonly ILLMProvider _llmProvider;  // GOOD
    
    public SalesAgent(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }
}

// 2. Explicit dependencies
public class OutlookEmailTool
{
    public OutlookEmailTool(
        GraphServiceClient graphClient,
        M365Settings settings,
        I Logger<OutlookEmailTool> logger)
    {
        // All dependencies explicit in constructor
    }
}
```

### ❌ DON'T

```csharp
// 1. Depend on concrete classes
public class SalesAgent
{
    private readonly AzureOpenAIProvider _provider;  // BAD
}

// 2. Service locator pattern
public class SalesAgent
{
    public SalesAgent(IServiceProvider serviceProvider)
    {
        _provider = serviceProvider.GetService<ILLMProvider>();  // BAD
    }
}

// 3. Use 'new' keyword
public class SalesAgent
{
    private readonly ILLMProvider _provider = new AzureOpenAIProvider();  // BAD
}
```

---

For complete DI validation strategies, service resolution testing, and advanced dependency patterns, please refer to the Japanese version at [../developer/05-DEPENDENCY-INJECTION.md](../../developer/05-DEPENDENCY-INJECTION.md).
