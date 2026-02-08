# テスト戦略ガイド

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](en/TESTING.md)

**営業支援エージェントの品質保証** - ユニットテスト、統合テスト、E2Eテストの実装

---

## 📋 概要

このガイドでは、営業支援エージェントの包括的なテスト戦略を説明します。xUnit、Moq、Microsoft.Bot.Builder.Testing を使用したテストの作成・実行方法をカバーします。

### 💡 テストピラミッド

```
       ┌────────────┐
       │   E2E (5%) │  Teams UI、実環境テスト
       ├────────────┤
       │ 統合 (15%)  │  Bot + Graph API、LLM統合
       ├────────────┤
       │ ユニット (80%) │  MCP Tools、ロジック単体
       └────────────┘
```

| レイヤー | 目的 | ツール |
|---------|-----|-------|
| **ユニットテスト** | 単一関数・クラスの動作検証 | xUnit, Moq |
| **統合テスト** | コンポーネント間の連携確認 | xUnit, TestServer |
| **E2Eテスト** | エンドツーエンドの動作確認 | Playwright, Selenium |

---

## 🚀 セットアップ

### テストプロジェクト作成

```bash
# テストプロジェクトディレクトリに移動
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent

# xUnitテストプロジェクト作成
dotnet new xunit -n SalesSupportAgent.Tests
cd SalesSupportAgent.Tests

# 必要なパッケージ追加
dotnet add package Moq
dotnet add package Microsoft.Bot.Builder.Testing
dotnet add package Microsoft.Extensions.Logging.Abstractions
dotnet add package FluentAssertions
dotnet add package coverlet.collector

# プロジェクト参照追加
dotnet add reference ../SalesSupportAgent/SalesSupportAgent.csproj
```

### ディレクトリ構造

```
SalesSupportAgent.Tests/
├── SalesSupportAgent.Tests.csproj
├── Unit/                          # ユニットテスト
│   ├── Services/
│   │   ├── Mcp/
│   │   │   ├── OutlookEmailToolTests.cs
│   │   │   ├── OutlookCalendarToolTests.cs
│   │   │   ├── SharePointToolTests.cs
│   │   │   └── TeamsMessageToolTests.cs
│   │   ├── LLM/
│   │   │   ├── AzureOpenAIProviderTests.cs
│   │   │   └── OllamaProviderTests.cs
│   │   └── Agent/
│   │       └── SalesAgentTests.cs
│   ├── Bot/
│   │   └── TeamsBotTests.cs
│   └── Helpers/
│       └── AdaptiveCardHelperTests.cs
├── Integration/                   # 統合テスト
│   ├── GraphIntegrationTests.cs
│   ├── BotIntegrationTests.cs
│   └── LLMIntegrationTests.cs
└── E2E/                           # E2Eテスト
    └── TeamsE2ETests.cs
```

---

## 🧪 ユニットテスト

### MCP Tool テスト

#### OutlookEmailToolTests.cs

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Graph;
using Microsoft.Extensions.Logging;
using SalesSupportAgent.Services.MCP.McpTools;

public class OutlookEmailToolTests
{
    private readonly Mock<GraphServiceClient> _mockGraphClient;
    private readonly Mock<ILogger<OutlookEmailTool>> _mockLogger;
    private readonly OutlookEmailTool _sut; // System Under Test
    
    public OutlookEmailToolTests()
    {
        _mockGraphClient = new Mock<GraphServiceClient>();
        _mockLogger = new Mock<ILogger<OutlookEmailTool>>();
        _sut = new OutlookEmailTool(
            _mockGraphClient.Object,
            _mockLogger.Object
        );
    }
    
    [Fact]
    public async Task SearchEmailsAsync_ValidQuery_ReturnsEmails()
    {
        // Arrange
        var expectedEmails = new List<Message>
        {
            new Message
            {
                Subject = "商談: 株式会社サンプル",
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = "customer@example.com"
                    }
                },
                ReceivedDateTime = DateTimeOffset.Now.AddDays(-1),
                BodyPreview = "商談詳細..."
            }
        };
        
        var mockMessageCollectionPage = new Mock<IUserMessagesCollectionPage>();
        mockMessageCollectionPage.Setup(p => p.GetEnumerator())
            .Returns(expectedEmails.GetEnumerator());
        
        _mockGraphClient
            .Setup(g => g.Me.Messages.Request()
                .Filter(It.IsAny<string>())
                .Top(It.IsAny<int>())
                .OrderBy(It.IsAny<string>())
                .GetAsync())
            .ReturnsAsync(mockMessageCollectionPage.Object);
        
        // Act
        var result = await _sut.SearchEmailsAsync("サンプル", maxResults: 10);
        
        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("株式会社サンプル");
        
        _mockGraphClient.Verify(
            g => g.Me.Messages.Request()
                .Filter(It.Is<string>(f => f.Contains("サンプル")))
                .Top(10)
                .OrderBy("receivedDateTime desc")
                .GetAsync(),
            Times.Once
        );
    }
    
    [Fact]
    public async Task SearchEmailsAsync_EmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var emptyQuery = "";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SearchEmailsAsync(emptyQuery)
        );
    }
    
    [Fact]
    public async Task SearchEmailsAsync_GraphApiError_LogsAndThrows()
    {
        // Arrange
        var query = "テストクエリ";
        _mockGraphClient
            .Setup(g => g.Me.Messages.Request()
                .Filter(It.IsAny<string>())
                .Top(It.IsAny<int>())
                .OrderBy(It.IsAny<string>())
                .GetAsync())
            .ThrowsAsync(new ServiceException(
                new Error { Code = "ErrorAccessDenied" }
            ));
        
        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(
            () => _sut.SearchEmailsAsync(query)
        );
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}
```

### LLM Provider テスト

#### AzureOpenAIProviderTests.cs

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.AI;
using SalesSupportAgent.Services.LLM;

public class AzureOpenAIProviderTests
{
    private readonly Mock<IChatClient> _mockChatClient;
    private readonly AzureOpenAIProvider _sut;
    
    public AzureOpenAIProviderTests()
    {
        _mockChatClient = new Mock<IChatClient>();
        _sut = new AzureOpenAIProvider(_mockChatClient.Object);
    }
    
    [Fact]
    public async Task GenerateResponseAsync_ValidPrompt_ReturnsResponse()
    {
        // Arrange
        var prompt = "今週の商談サマリを教えて";
        var expectedResponse = "今週の商談サマリ:\n1. ...";
        
        var mockResponse = new ChatCompletion(new[]
        {
            new ChatMessage(ChatRole.Assistant, expectedResponse)
        });
        
        _mockChatClient
            .Setup(c => c.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(mockResponse);
        
        // Act
        var result = await _sut.GenerateResponseAsync(
            prompt,
            new List<Message>(),
            CancellationToken.None
        );
        
        // Assert
        result.Should().Be(expectedResponse);
        
        _mockChatClient.Verify(
            c => c.CompleteAsync(
                It.Is<IList<ChatMessage>>(
                    m => m.Any(msg => msg.Text == prompt)
                ),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GenerateResponseAsync_EmptyPrompt_ThrowsException(string? prompt)
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GenerateResponseAsync(
                prompt!,
                new List<Message>(),
                CancellationToken.None
            )
        );
    }
}
```

### Adaptive Card テスト

#### AdaptiveCardHelperTests.cs

```csharp
using Xunit;
using FluentAssertions;
using SalesSupportAgent.Bot;
using AdaptiveCards;

public class AdaptiveCardHelperTests
{
    [Fact]
    public void CreateSalesSummaryCard_ValidData_ReturnsCard()
    {
        // Arrange
        var summaryText = "今週の商談サマリ";
        var emails = new List<EmailSummary>
        {
            new EmailSummary
            {
                Subject = "商談メール",
                From = "customer@example.com",
                ReceivedDateTime = DateTime.Now
            }
        };
        var events = new List<EventSummary>();
        var llmProvider = "AzureOpenAI";
        
        // Act
        var attachment = AdaptiveCardHelper.CreateSalesSummaryCard(
            summaryText,
            emails,
            events,
            llmProvider
        );
        
        // Assert
        attachment.Should().NotBeNull();
        attachment.ContentType.Should().Be(AdaptiveCard.ContentType);
        
        var card = attachment.Content as AdaptiveCard;
        card.Should().NotBeNull();
        card!.Body.Should().NotBeEmpty();
        
        // サマリテキストが含まれているか
        var textBlocks = card.Body
            .OfType<AdaptiveContainer>()
            .SelectMany(c => c.Items)
            .OfType<AdaptiveTextBlock>();
        
        textBlocks.Should().Contain(
            t => t.Text.Contains(summaryText)
        );
    }
}
```

---

## 🔗 統合テスト

### Graph API 統合テスト

```csharp
using Xunit;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using SalesSupportAgent.Services.MCP.McpTools;

[Collection("Graph Integration")]
public class GraphIntegrationTests : IAsyncLifetime
{
    private GraphServiceClient? _graphClient;
    private OutlookEmailTool? _emailTool;
    
    public async Task InitializeAsync()
    {
        // appsettings.Test.json から設定読み込み
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        var tenantId = configuration["M365:TenantId"];
        var clientId = configuration["M365:ClientId"];
        var clientSecret = configuration["M365:ClientSecret"];
        
        // TokenCredential作成（実際の認証）
        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret
        );
        
        _graphClient = new GraphServiceClient(credential);
        _emailTool = new OutlookEmailTool(
            _graphClient,
            NullLogger<OutlookEmailTool>.Instance
        );
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchEmails_RealGraphAPI_ReturnsData()
    {
        // Arrange
        var query = "test"; // 実際に存在するメールを検索
        
        // Act
        var result = await _emailTool!.SearchEmailsAsync(query, maxResults: 5);
        
        // Assert
        result.Should().NotBeNullOrEmpty();
        // 実際のメールデータが返されることを確認
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
}
```

### Bot 統合テスト

```csharp
using Xunit;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Schema;
using SalesSupportAgent.Bot;

public class BotIntegrationTests
{
    [Fact]
    public async Task OnMessageActivity_HelloMessage_ReturnsWelcome()
    {
        // Arrange
        var bot = new TeamsBot(
            Mock.Of<SalesAgent>(),
            Mock.Of<ILogger<TeamsBot>>()
        );
        
        var adapter = new TestAdapter();
        
        // Act
        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("こんにちは")
            .AssertReply(activity =>
            {
                var message = activity.AsMessageActivity();
                message.Text.Should().Contain("営業支援エージェント");
            })
            .StartTestAsync();
    }
    
    [Fact]
    public async Task OnMessageActivity_SummaryRequest_InvokesAgent()
    {
        // Arrange
        var mockAgent = new Mock<SalesAgent>();
        mockAgent
            .Setup(a => a.ProcessQueryAsync(It.IsAny<string>()))
            .ReturnsAsync("商談サマリ結果");
        
        var bot = new TeamsBot(
            mockAgent.Object,
            Mock.Of<ILogger<TeamsBot>>()
        );
        
        var adapter = new TestAdapter();
        
        // Act
        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("今週の商談サマリを教えて")
            .AssertReply(activity =>
            {
                // Adaptive Card が返されることを確認
                activity.Attachments.Should().NotBeEmpty();
                activity.Attachments[0].ContentType.Should().Be("application/vnd.microsoft.card.adaptive");
            })
            .StartTestAsync();
        
        // Assert
        mockAgent.Verify(
            a => a.ProcessQueryAsync(It.Is<string>(q => q.Contains("商談サマリ"))),
            Times.Once
        );
    }
}
```

---

## 🌐 E2Eテスト

### Playwright テスト

```bash
# Playwright インストール
dotnet add package Microsoft.Playwright
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

#### TeamsE2ETests.cs

```csharp
using Xunit;
using Microsoft.Playwright;

public class TeamsE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = false, // デバッグ時はfalse
            SlowMo = 100
        });
        
        _page = await _browser.NewPageAsync();
    }
    
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Teams_SendMessageToBot_ReceivesResponse()
    {
        // Arrange
        await _page!.GotoAsync("https://teams.microsoft.com");
        
        // Teams ログイン（認証情報は環境変数から取得）
        await _page.FillAsync("#i0116", Environment.GetEnvironmentVariable("TEST_USER_EMAIL")!);
        await _page.ClickAsync("#idSIButton9");
        await _page.FillAsync("#i0118", Environment.GetEnvironmentVariable("TEST_USER_PASSWORD")!);
        await _page.ClickAsync("#idSIButton9");
        
        // Botを検索
        await _page.ClickAsync("[aria-label='Search']");
        await _page.FillAsync("input[type='search']", "営業支援エージェント");
        await _page.ClickAsync("text=営業支援エージェント");
        
        // メッセージ送信
        await _page.FillAsync("[contenteditable='true']", "こんにちは");
        await _page.PressAsync("[contenteditable='true']", "Enter");
        
        // 応答待機（最大30秒）
        await _page.WaitForSelectorAsync(
            "text=営業支援エージェントです",
            new() { Timeout = 30000 }
        );
        
        // Assert
        var responseText = await _page.TextContentAsync(".message-body");
        responseText.Should().Contain("営業支援エージェント");
    }
    
    public async Task DisposeAsync()
    {
        await _page?.CloseAsync()!;
        await _browser?.CloseAsync()!;
        _playwright?.Dispose();
    }
}
```

---

## 📊 カバレッジ測定

### coverlet での実行

```bash
# テスト実行 + カバレッジ収集
dotnet test /p:CollectCoverage=true \
             /p:CoverletOutputFormat=opencover \
             /p:Exclude="[xunit.*]*"

# カバレッジ閾値設定
dotnet test /p:CollectCoverage=true \
             /p:Threshold=80 \
             /p:ThresholdType=line
```

### カバレッジレポート生成

```bash
# ReportGenerator インストール
dotnet tool install -g dotnet-reportgenerator-globaltool

# HTMLレポート生成
reportgenerator \
  -reports:"coverage.opencover.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# レポート表示
open coveragereport/index.html
```

---

## 🚀 CI/CD 統合

### GitHub Actions ワークフロー

```.github/workflows/test.yml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: |
        dotnet test --no-build --verbosity normal \
          --filter "Category!=Integration&Category!=E2E" \
          /p:CollectCoverage=true \
          /p:CoverletOutputFormat=opencover
    
    - name: Run Integration Tests
      run: |
        dotnet test --no-build --verbosity normal \
          --filter "Category=Integration"
      env:
        M365__TenantId: ${{ secrets.M365_TENANT_ID }}
        M365__ClientId: ${{ secrets.M365_CLIENT_ID }}
        M365__ClientSecret: ${{ secrets.M365_CLIENT_SECRET }}
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage.opencover.xml
```

---

## 🐛 デバッグテスト

### Visual Studio Code

```json
// .vscode/launch.json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Tests",
      "type": "coreclr",
      "request": "launch",
      "program": "dotnet",
      "args": [
        "test",
        "--filter",
        "FullyQualifiedName~OutlookEmailToolTests"
      ],
      "cwd": "${workspaceFolder}/SalesSupportAgent.Tests",
      "console": "internalConsole",
      "stopAtEntry": false
    }
  ]
}
```

### テスト選択実行

```bash
# 特定のテストクラス実行
dotnet test --filter "FullyQualifiedName~OutlookEmailToolTests"

# 特定のテストメソッド実行
dotnet test --filter "FullyQualifiedName~SearchEmailsAsync_ValidQuery"

# カテゴリ指定
dotnet test --filter "Category=Integration"

# 並列実行無効化（デバッグ用）
dotnet test --no-parallel
```

---

## 📚 ベストプラクティス

### 1. AAA パターン

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedBehavior()
{
    // Arrange (準備)
    var input = "test";
    var expected = "result";
    
    // Act (実行)
    var actual = await _sut.MethodAsync(input);
    
    // Assert (検証)
    actual.Should().Be(expected);
}
```

### 2. 理論ベーステスト

```csharp
[Theory]
[InlineData("test", 10)]
[InlineData("example", 20)]
[InlineData("sample", 5)]
public async Task SearchAsync_VariousQueries_ReturnsResults(
    string query,
    int expectedCount)
{
    var result = await _sut.SearchAsync(query);
    result.Count.Should().Be(expectedCount);
}
```

### 3. Fixtures 使用

```csharp
public class GraphClientFixture : IDisposable
{
    public GraphServiceClient GraphClient { get; }
    
    public GraphClientFixture()
    {
        // セットアップ処理
        GraphClient = CreateGraphClient();
    }
    
    public void Dispose()
    {
        // クリーンアップ処理
    }
}

[Collection("Graph Collection")]
public class MyTests : IClassFixture<GraphClientFixture>
{
    private readonly GraphClientFixture _fixture;
    
    public MyTests(GraphClientFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task Test()
    {
        // _fixture.GraphClient を使用
    }
}
```

---

## 🔗 関連リソース

- [Agent開発ガイド](AGENT-DEVELOPMENT.md) - エージェント実装
- [トラブルシューティング](TROUBLESHOOTING.md) - 問題解決
- [xUnit Documentation](https://xunit.net/)
- [Moq Quick Reference](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions](https://fluentassertions.com/)

---

**包括的なテストで営業支援エージェントの品質を保証しましょう！** ✅
