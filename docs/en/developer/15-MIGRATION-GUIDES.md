# Migration Guides - Upgrade Paths and Breaking Changes

> **Language**: [🇯🇵 日本語](../../developer/15-MIGRATION-GUIDES.md) | 🇬🇧 English

## 📋 Migration Paths

- [From Bot Framework v3 to v4](#from-bot-framework-v3-to-v4)
- [From Semantic Kernel to Microsoft.Extensions.AI](#from-semantic-kernel-to-microsoftextensionsai)
- [From Manual Observability to Agent 365 SDK](#from-manual-observability-to-agent-365-sdk)

---

## From Bot Framework v3 to v4

### Key Changes

| v3 | v4 |
|--- |---|
| `IDialogContext` | `ITurnContext` |
| `IMessageActivity` | `Activity.Type == "message"` |
| `context.PostAsync()` | `turnContext.SendActivityAsync()` |

### Code Migration

**v3 (Old)**:

```csharp
public async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> argument)
{
    var message = await argument;
    await context.PostAsync($"You said: {message.Text}");
}
```

**v4 (New)**:

```csharp
protected override async Task OnMessageActivityAsync(
    ITurnContext<IMessageActivity> turnContext,
    CancellationToken cancellationToken)
{
    await turnContext.SendActivityAsync(
        MessageFactory.Text($"You said: {turnContext.Activity.Text}"),
        cancellationToken);
}
```

---

## From Semantic Kernel to Microsoft.Extensions.AI

### Motivation

- ✅ **Unified API**: Microsoft.Extensions.AI provides standard chat interface
- ✅ **Middleware Support**: Easily add telemetry, logging, caching
- ✅ **Provider Agnostic**: Switch between Azure OpenAI, Ollama, Anthropic

### Code Migration

**Semantic Kernel (Old)**:

```csharp
var kernel = new KernelBuilder()
    .WithAzureChatCompletionService(deploymentName, endpoint, apiKey)
    .Build();

var result = await kernel.InvokeAsync("Tell me about sales");
```

**Microsoft.Extensions.AI (New)**:

```csharp
var chatClient = new ChatClientBuilder()
    .Use(new AzureOpenAIChatClient(endpoint, credential, deploymentName))
    .UseOpenTelemetry()
    .UseFunctionInvocation()
    .Build();

var response = await chatClient.CompleteAsync(new List<ChatMessage>
{
    new ChatMessage(ChatRole.User, "Tell me about sales")
});
```

### Tool/Function Migration

**Semantic Kernel (Old)**:

```csharp
[SKFunction, Description("Search emails")]
public async Task<string> SearchEmails(string query)
{
    // Implementation
}
```

**Microsoft.Extensions.AI (New)**:

```csharp
[Description("Search emails")]
public async Task<string> SearchEmails(
    [Description("Search query")] string query)
{
    // Implementation
}

// Register
var tools = new List<AITool>
{
    AIFunctionFactory.Create(SearchEmails)
};
```

---

## From Manual Observability to Agent 365 SDK

### Before: Manual ActivitySource

```csharp
private static readonly ActivitySource _activitySource = new ActivitySource("MyAgent");

public async Task ProcessAsync()
{
    using var activity = _activitySource.StartActivity("Process");
    activity?.SetTag(...);
    
    // Manual metrics
    _requestCounter.Add(1);
    _latencyHistogram.Record(...);
}
```

### After: Agent 365 SDK

```csharp
builder.Services.AddAgent365Observability(options =>
{
    options.ActivitySourceName = "SalesSupportAgent";
    options.MeterName = "SalesSupportAgent.Metrics";
});

// Automatic tracing and metrics for all AI operations
```

---

## Breaking Changes in Agent 365 SDK v1.0 → v2.0

### 1. Notification API Changes

**v1.0 (Old)**:

```csharp
await _notificationService.SendNotificationAsync(new Notification
{
    Message = "Processing...",
    Level = NotificationLevel.Info
});
```

**v2.0 (New)**:

```csharp
await _notificationService.SendProgressNotificationAsync(
    operationId: "op-123",
    message: "Processing...",
    progressPercentage: 50);
```

### 2. ObservabilityService Constructor

**v1.0 (Old)**:

```csharp
var obsService = new ObservabilityService(signalRHub);
```

**v2.0 (New)**:

```csharp
builder.Services.AddAgent365Observability();  // DI registration
```

---

For complete migration guides, version compatibility matrix, and upgrade checklists, please refer to the Japanese version at [../developer/15-MIGRATION-GUIDES.md](../../developer/15-MIGRATION-GUIDES.md).
