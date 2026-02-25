# Migration Guides - Version Upgrade and Migration Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/15-MIGRATION-GUIDES.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](15-MIGRATION-GUIDES.md)

## 📋 .NET 8 → .NET 10 Migration

### Key Changes

#### 1. Agent 365 SDK Integration

**.NET 8 (Legacy)**:
```csharp
// Manual OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .WithTracing(/* detailed configuration */);
```

**.NET 10 (Agent 365)**:
```csharp
// Simplified with Agent 365 SDK
builder.Services.AddAgent365Observability(options =>
{
    options.ActivitySourceName = "SalesSupportAgent";
    options.EnableDetailedSpans = true;
});
```

#### 2. Introducing Microsoft.Extensions.AI

**.NET 8 (Legacy)**:
```csharp
// Provider-specific client
var openAIClient = new OpenAIClient(apiKey);
var completion = await openAIClient.GetChatCompletionsAsync(/* ... */);
```

**.NET 10 (Microsoft.Extensions.AI)**:
```csharp
// Unified interface
var chatClient = new ChatClientBuilder()
    .Use(new AzureOpenAIClient(endpoint, credential).AsChatClient(deployment))
    .UseOpenTelemetry()
    .Build();

var completion = await chatClient.CompleteAsync(messages);
```

### Migration Steps

#### Step 1: Update Project File

```xml
<!-- SalesSupportAgent.csproj -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>  <!-- Changed from net8.0 -->
</PropertyGroup>

<ItemGroup>
  <!-- Agent 365 SDK -->
  <PackageReference Include="Microsoft.Agents.A365.Observability" Version="1.0.0" />
  <PackageReference Include="Microsoft.Agents.A365.Tooling" Version="1.0.0" />
  
  <!-- Microsoft.Extensions.AI -->
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.0.0" />
  
  <!-- Remove legacy packages -->
  <!-- <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0" /> -->
</ItemGroup>
```

#### Step 2: Update LLM Provider Implementation

**Before (.NET 8)**:
```csharp
public class OpenAIService
{
    private readonly OpenAIClient _client;
    
    public async Task<string> GenerateAsync(string prompt)
    {
        var options = new ChatCompletionsOptions
        {
            Messages = { new ChatMessage(ChatRole.User, prompt) }
        };
        
        var response = await _client.GetChatCompletionsAsync(deployment, options);
        return response.Value.Choices[0].Message.Content;
    }
}
```

**After (.NET 10)**:
```csharp
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly IChatClient _chatClient;
    
    public IChatClient GetChatClient() => _chatClient;
    
    public async Task<string> GenerateAsync(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, prompt)
        };
        
        var response = await _chatClient.CompleteAsync(messages);
        return response.Message.Content;
    }
}
```

#### Step 3: Update Observability Code

**Before (.NET 8)**:
```csharp
using var activity = Activity.Current?.Source.StartActivity("Operation");
activity?.SetTag("key", "value");
```

**After (.NET 10)**:
```csharp
// Using Agent 365 Observability Service
await _observabilityService.RecordTraceAsync("Operation started", "info", 0);
await _observabilityService.AddTracePhaseAsync(sessionId, "Phase1", "Description");
```

---

## Agent Identity → Application-only Authentication

### Terminology Unification

| Legacy | New | Description |
|--------|-----|-------------|
| Agent Identity | Application-only Authentication | Unified terminology |
| Service Principal | Client Secret / Managed Identity | Implementation method |

### Code Update

**Before**:
```csharp
// Using the term "Agent Identity"
builder.Services.AddAgentIdentity(options => { /* ... */ });
```

**After**:
```csharp
// Unified as "Application-only Authentication"
builder.Services.AddSingleton<TokenCredential>(sp =>
{
    return new ClientSecretCredential(
        tenantId, clientId, clientSecret
    );
});
```

---

## GitHub Models Integration

### New Addition (.NET 10)

```csharp
public class GitHubModelsProvider : ILLMProvider
{
    public GitHubModelsProvider(GitHubModelsSettings settings)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://models.inference.ai.azure.com"),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", settings.Token)
            }
        };
        
        _chatClient = new ChatClientBuilder()
            .Use(httpClient.AsChatClient(settings.Model))
            .UseOpenTelemetry()
            .Build();
    }
}
```

**appsettings.json**:
```json
{
  "LLM": {
    "Provider": "GitHubModels",
    "GitHubModels": {
      "Token": "github_pat_...",
      "Model": "gpt-4o"
    }
  }
}
```

---

## Observability Dashboard Implementation

### Adding SignalR Hub

**New file**: `Hubs/ObservabilityHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace SalesSupportAgent.Hubs;

public class ObservabilityHub : Hub
{
    public async Task SendTrace(string message)
    {
        await Clients.All.SendAsync("ReceiveTrace", message);
    }
}
```

**Program.cs update**:
```csharp
// SignalR registration
builder.Services.AddSignalR();

// Endpoint mapping
app.MapHub<ObservabilityHub>("/hubs/observability");
```

---

## Handling Breaking Changes

### 1. IChatClient Interface Changes

**.NET 8 (Azure.AI.OpenAI)**:
```csharp
var response = await client.GetChatCompletionsAsync(deployment, options);
var content = response.Value.Choices[0].Message.Content;
```

**.NET 10 (Microsoft.Extensions.AI)**:
```csharp
var response = await client.CompleteAsync(messages, options);
var content = response.Message.Content;
```

### 2. Tool Invocation Schema

**.NET 8**:
```csharp
var functionDefinition = new FunctionDefinition
{
    Name = "search_emails",
    Description = "Email search",
    Parameters = BinaryData.FromObjectAsJson(parametersSchema)
};
```

**.NET 10**:
```csharp
var tool = AIFunctionFactory.Create(
    _emailTool.SearchSalesEmails  // Auto-generated from method reference
);
```

---

## Test Code Updates

### Moq Version Upgrade

```xml
<!-- .NET 8 -->
<PackageReference Include="Moq" Version="4.18.4" />

<!-- .NET 10 -->
<PackageReference Include="Moq" Version="4.20.0" />
```

### Test Case Updates

```csharp
// .NET 10 compatible
[Fact]
public async Task CompleteAsync_Success_ReturnsResponse()
{
    var mockClient = new Mock<IChatClient>();
    mockClient
        .Setup(x => x.CompleteAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            default))
        .ReturnsAsync(new ChatCompletion
        {
            Message = new ChatMessage(ChatRole.Assistant, "Test response")
        });
    
    var provider = new MockLLMProvider(mockClient.Object);
    var result = await provider.GetChatClient().CompleteAsync(messages);
    
    Assert.Equal("Test response", result.Message.Content);
}
```

---

## Rollback Procedures

### .NET 10 → .NET 8 Downgrade

```bash
# 1. Update project file
# Change TargetFramework to net8.0

# 2. Restore packages
dotnet restore

# 3. Verify build
dotnet build

# 4. Run tests
dotnet test
```

**Tips for maintaining compatibility**:
- Keep the ILLMProvider interface intact
- Retain legacy implementation classes
- Enable switching via Feature Flags

---

## Checklist

### Migration Completion Checklist

- [ ] Confirm .NET 10 SDK installation
- [ ] Update `TargetFramework`
- [ ] Add Agent 365 SDK packages
- [ ] Add Microsoft.Extensions.AI packages
- [ ] Update LLM provider implementation
- [ ] Update observability code
- [ ] Update test code
- [ ] Verify successful build
- [ ] Confirm all tests pass
- [ ] Integration testing before production deployment

---

## Troubleshooting

### Build Errors

**Error**: `The type or namespace name 'ChatCompletionsOptions' could not be found`

**Cause**: Reference to the old Azure.AI.OpenAI package

**Solution**: Update to Microsoft.Extensions.AI

### Runtime Errors

**Error**: `Unable to resolve service for type 'IChatClient'`

**Cause**: Missing DI container registration

**Solution**: Verify ILLMProvider registration in Program.cs

---

## Next Steps

- **[01-SDK-OVERVIEW.md](01-SDK-OVERVIEW.md)**: New SDK overview
- **[DEPLOYMENT-AZURE.md](../DEPLOYMENT-AZURE.md)**: .NET 10 deployment
