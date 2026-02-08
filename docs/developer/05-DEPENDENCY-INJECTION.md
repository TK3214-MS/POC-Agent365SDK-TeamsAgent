# Dependency Injection - DIコンテナの設計と使用

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](05-DEPENDENCY-INJECTION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../en/developer/05-DEPENDENCY-INJECTION.md)

## 📋 目次

- [DI基本パターン](#di基本パターン)
- [サービスライフタイム](#サービスライフタイム)
- [登録パターン](#登録パターン)
- [ベストプラクティス](#ベストプラクティス)

---

## DI基本パターン

### Program.cs でのサービス登録

```csharp
// Singleton - アプリケーション全体で1つのインスタンス
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

// Transient - リクエストごとに新しいインスタンス
builder.Services.AddTransient<IBot, TeamsBot>();
```

---

## サービスライフタイム

### Singleton

**特徴**:
- アプリケーション起動時に1つだけ作成
- 全リクエストで共有
- スレッドセーフである必要がある

**使用例**:
```csharp
builder.Services.AddSingleton<GraphServiceClient>(/* 実装 */);
```

**適用ケース**:
- `GraphServiceClient`: トークンキャッシュを共有
- `ObservabilityService`: メトリクスを集約
- `NotificationService`: SignalR接続管理

### Scoped

**特徴**:
- HTTPリクエストごとに1つ作成
- リクエスト内で共有
- リクエスト終了時に破棄

**使用例**:
```csharp
builder.Services.AddScoped<MyDbContext>();
```

**適用ケース**:
- データベースコンテキスト
- リクエスト固有の状態管理

### Transient

**特徴**:
- 要求されるたびに新しいインスタンス作成
- 軽量サービス向け

**使用例**:
```csharp
builder.Services.AddTransient<IBot, TeamsBot>();
```

**適用ケース**:
- `TeamsBot`: 会話ごとに新規作成
- ステートレスサービス

---

## 登録パターン

### Pattern 1: Factory Pattern

```csharp
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var settings = sp.GetRequiredService<LLMSettings>();
    
    logger.LogInformation("LLM Provider 初期化: {Provider}", settings.Provider);
    
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
// 設定登録
builder.Services.Configure<M365Settings>(
    builder.Configuration.GetSection("M365"));

// 設定使用
public class OutlookEmailTool
{
    public OutlookEmailTool(IOptions<M365Settings> options)
    {
        var settings = options.Value;
        _userId = settings.UserId;
    }
}
```

### Pattern 3: 条件付き登録

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

## ベストプラクティス

### ✅ DO

```csharp
// 1. インターフェースに依存
public class SalesAgent
{
    private readonly ILLMProvider _llmProvider;  // GOOD
    
    public SalesAgent(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }
}

// 2. 明示的な依存関係
public class OutlookEmailTool
{
    public OutlookEmailTool(
        GraphServiceClient graphClient,
        M365Settings settings,
        ILogger<OutlookEmailTool> logger)
    {
        // すべての依存関係がコンストラクタで明示
    }
}

// 3. サービスロケーターパターンを避ける
public class SalesAgent
{
    // ❌ サービスロケーター（アンチパターン）
    // private readonly IServiceProvider _serviceProvider;
    
    // ✅ 明示的なDI
    private readonly ILLMProvider _llmProvider;
}
```

### ❌ DON'T

```csharp
// 1. 具象クラスに直接依存
public class SalesAgent
{
    private readonly AzureOpenAIProvider _provider;  // BAD
}

// 2. サービスロケーターパターン
public class SalesAgent
{
    public SalesAgent(IServiceProvider serviceProvider)
    {
        _provider = serviceProvider.GetService<ILLMProvider>();  // BAD
    }
}

// 3. new キーワードでインスタンス生成
public class SalesAgent
{
    private readonly ILLMProvider _provider = new AzureOpenAIProvider();  // BAD
}
```

---

## DI検証

### スタートアップ時の検証

```csharp
var app = builder.Build();

// サービス解決テスト
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        var salesAgent = services.GetRequiredService<SalesAgent>();
        var graphClient = services.GetRequiredService<GraphServiceClient>();
        var llmProvider = services.GetRequiredService<ILLMProvider>();
        
        Console.WriteLine("✅ All services resolved successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Service resolution failed: {ex.Message}");
        throw;
    }
}
```

---

## 次のステップ

- **[02-PROJECT-STRUCTURE.md](02-PROJECT-STRUCTURE.md)**: プロジェクト構造
- **[06-SDK-INTEGRATION-PATTERNS.md](06-SDK-INTEGRATION-PATTERNS.md)**: SDK統合パターン
