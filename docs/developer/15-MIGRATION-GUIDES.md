# Migration Guides - バージョンアップと移行ガイド

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](../en/developer/15-MIGRATION-GUIDES.md)

## 📋 .NET 8 → .NET 10 移行

### 主な変更点

#### 1. Agent 365 SDK 統合

**.NET 8（従来）**:
```csharp
// 手動でOpenTelemetry設定
builder.Services.AddOpenTelemetry()
    .WithTracing(/* 詳細設定 */);
```

**.NET 10（Agent 365）**:
```csharp
// Agent 365 SDK による簡素化
builder.Services.AddAgent365Observability(options =>
{
    options.ActivitySourceName = "SalesSupportAgent";
    options.EnableDetailedSpans = true;
});
```

#### 2. Microsoft.Extensions.AI 導入

**.NET 8（従来）**:
```csharp
// プロバイダー固有のクライアント
var openAIClient = new OpenAIClient(apiKey);
var completion = await openAIClient.GetChatCompletionsAsync(/* ... */);
```

**.NET 10（Microsoft.Extensions.AI）**:
```csharp
// 統一インターフェース
var chatClient = new ChatClientBuilder()
    .Use(new AzureOpenAIClient(endpoint, credential).AsChatClient(deployment))
    .UseOpenTelemetry()
    .Build();

var completion = await chatClient.CompleteAsync(messages);
```

### 移行手順

#### ステップ1: プロジェクトファイル更新

```xml
<!-- SalesSupportAgent.csproj -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>  <!-- net8.0 から変更 -->
</PropertyGroup>

<ItemGroup>
  <!-- Agent 365 SDK -->
  <PackageReference Include="Microsoft.Agents.A365.Observability" Version="1.0.0" />
  <PackageReference Include="Microsoft.Agents.A365.Tooling" Version="1.0.0" />
  
  <!-- Microsoft.Extensions.AI -->
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.0.0" />
  
  <!-- 従来パッケージの削除 -->
  <!-- <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0" /> -->
</ItemGroup>
```

#### ステップ2: LLMプロバイダー実装更新

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

#### ステップ3: 観測性コード更新

**Before (.NET 8)**:
```csharp
using var activity = Activity.Current?.Source.StartActivity("Operation");
activity?.SetTag("key", "value");
```

**After (.NET 10)**:
```csharp
// Agent 365 Observability Service 使用
await _observabilityService.RecordTraceAsync("Operation started", "info", 0);
await _observabilityService.AddTracePhaseAsync(sessionId, "Phase1", "Description");
```

---

## Agent Identity → Application-only認証

### 用語統一

| 従来 | 新規 | 説明 |
|------|------|------|
| Agent Identity | Application-only認証 | 統一用語 |
| Service Principal | Client Secret / Managed Identity | 実装方式 |

### コード更新

**Before**:
```csharp
// Agent Identity という用語を使用
builder.Services.AddAgentIdentity(options => { /* ... */ });
```

**After**:
```csharp
// Application-only認証 に統一
builder.Services.AddSingleton<TokenCredential>(sp =>
{
    return new ClientSecretCredential(
        tenantId, clientId, clientSecret
    );
});
```

---

## GitHub Models 統合

### 新規追加（.NET 10）

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

## Observability Dashboard 実装

### SignalR Hub追加

**新規ファイル**: `Hubs/ObservabilityHub.cs`

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

**Program.cs更新**:
```csharp
// SignalR 登録
builder.Services.AddSignalR();

// エンドポイントマッピング
app.MapHub<ObservabilityHub>("/hubs/observability");
```

---

## 破壊的変更への対応

### 1. IChatClient インターフェース変更

**.NET 8（Azure.AI.OpenAI）**:
```csharp
var response = await client.GetChatCompletionsAsync(deployment, options);
var content = response.Value.Choices[0].Message.Content;
```

**.NET 10（Microsoft.Extensions.AI）**:
```csharp
var response = await client.CompleteAsync(messages, options);
var content = response.Message.Content;
```

### 2. ツール呼び出しスキーマ

**.NET 8**:
```csharp
var functionDefinition = new FunctionDefinition
{
    Name = "search_emails",
    Description = "メール検索",
    Parameters = BinaryData.FromObjectAsJson(parametersSchema)
};
```

**.NET 10**:
```csharp
var tool = AIFunctionFactory.Create(
    _emailTool.SearchSalesEmails  // メソッド参照から自動生成
);
```

---

## テストコード更新

### Moq バージョンアップ

```xml
<!-- .NET 8 -->
<PackageReference Include="Moq" Version="4.18.4" />

<!-- .NET 10 -->
<PackageReference Include="Moq" Version="4.20.0" />
```

### テストケース更新

```csharp
// .NET 10 対応
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
            Message = new ChatMessage(ChatRole.Assistant, "テスト応答")
        });
    
    var provider = new MockLLMProvider(mockClient.Object);
    var result = await provider.GetChatClient().CompleteAsync(messages);
    
    Assert.Equal("テスト応答", result.Message.Content);
}
```

---

## ロールバック手順

### .NET 10 → .NET 8 ダウングレード

```bash
# 1. プロジェクトファイル更新
# TargetFramework を net8.0 に変更

# 2. パッケージ復元
dotnet restore

# 3. ビルド確認
dotnet build

# 4. テスト実行
dotnet test
```

**互換性維持のヒント**:
- ILLMProvider インターフェースを維持
- 従来の実装クラスを残す
- Feature Flag で切り替え可能に

---

## チェックリスト

### 移行完了チェック

- [ ] .NET 10 SDK インストール確認
- [ ] `TargetFramework` 更新
- [ ] Agent 365 SDK パッケージ追加
- [ ] Microsoft.Extensions.AI パッケージ追加
- [ ] LLMプロバイダー実装更新
- [ ] 観測性コード更新
- [ ] テストコード更新
- [ ] ビルド成功確認
- [ ] 全テスト通過確認
- [ ] 本番デプロイ前の統合テスト

---

## トラブルシューティング

### ビルドエラー

**エラー**: `The type or namespace name 'ChatCompletionsOptions' could not be found`

**原因**: 古い Azure.AI.OpenAI パッケージへの参照

**解決**: Microsoft.Extensions.AI に更新

### ランタイムエラー

**エラー**: `Unable to resolve service for type 'IChatClient'`

**原因**: DIコンテナ登録漏れ

**解決**: Program.cs で ILLMProvider 登録確認

---

## 次のステップ

- **[01-SDK-OVERVIEW.md](01-SDK-OVERVIEW.md)**: 新SDK概要
- **[DEPLOYMENT-AZURE.md](../DEPLOYMENT-AZURE.md)**: .NET 10 デプロイ
