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
// Singleton - Single instance across the entire application
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
- One instance created per HTTP request
- Shared within a request
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
        ILogger<OutlookEmailTool> logger)
    {
        // All dependencies are explicit in the constructor
    }
}

// 3. Avoid Service Locator pattern
public class SalesAgent
{
    // ❌ Service Locator (anti-pattern)
    // private readonly IServiceProvider _serviceProvider;
    
    // ✅ Explicit DI
    private readonly ILLMProvider _llmProvider;
}
```

### ❌ DON'T

```csharp
// 1. Depend directly on concrete classes
public class SalesAgent
{
    private readonly AzureOpenAIProvider _provider;  // BAD
}

// 2. Create instances with new inside services
public class SalesAgent
{
    public void Process()
    {
        var provider = new AzureOpenAIProvider();  // BAD
    }
}

// 3. Register Singleton services that depend on Scoped services
// This causes a "Captive Dependency" problem
builder.Services.AddSingleton<MyService>();  // BAD if MyService depends on Scoped services
```

---

> **Next**: [06-SDK-INTEGRATION-PATTERNS.md](06-SDK-INTEGRATION-PATTERNS.md) - SDK Integration Patterns
