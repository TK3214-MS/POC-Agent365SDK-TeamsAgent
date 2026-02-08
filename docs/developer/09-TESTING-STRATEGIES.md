# Testing Strategies - テスト戦略とベストプラクティス

> **Language**: 🇯🇵 日本語 | [🇬🇧 English](../en/developer/09-TESTING-STRATEGIES.md)

## 📋 テスト階層

### テストピラミッド

```
        ┌────────────┐
        │  E2E Tests │  少数（遅い、脆弱、高コスト）
        └────────────┘
      ┌────────────────┐
      │Integration Tests│ 中程度（中速、中コスト）
      └────────────────┘
    ┌────────────────────┐
    │    Unit Tests       │ 多数（高速、安定、低コスト）
    └────────────────────┘
```

## ユニットテスト

### xUnit + Moq パターン

```csharp
public class OutlookEmailToolTests
{
    [Fact]
    public async Task SearchSalesEmails_Success_ReturnsFormattedSummary()
    {
        // Arrange
        var mockGraphClient = new Mock<GraphServiceClient>();
        var mockSettings = new M365Settings
        {
            UserId = "testuser@company.com"
        };
        
        var mockMessages = new MessageCollectionResponse
        {
            Value = new List<Message>
            {
                new Message
                {
                    Subject = "商談の件",
                    From = new Recipient { EmailAddress = new EmailAddress { Name = "田中太郎" } },
                    ReceivedDateTime = DateTimeOffset.UtcNow
                }
            }
        };
        
        mockGraphClient
            .Setup(x => x.Users[It.IsAny<string>()].Messages.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
            .ReturnsAsync(mockMessages);
        
        var tool = new OutlookEmailTool(mockGraphClient.Object, mockSettings);
        
        // Act
        var result = await tool.SearchSalesEmails("2026-02-01", "2026-02-07", "商談");
        
        // Assert
        Assert.Contains("商談の件", result);
        Assert.Contains("田中太郎", result);
    }
}
```

### Test Fixtures

```csharp
public class GraphClientFixture : IDisposable
{
    public Mock<GraphServiceClient> MockGraphClient { get; }
    public M365Settings TestSettings { get; }
    
    public GraphClientFixture()
    {
        MockGraphClient = new Mock<GraphServiceClient>();
        TestSettings = new M365Settings
        {
            UserId = "testuser@example.com",
            TenantId = "test-tenant",
            ClientId = "test-client"
        };
    }
    
    public void Dispose() { }
}

public class EmailToolTests : IClassFixture<GraphClientFixture>
{
    private readonly GraphClientFixture _fixture;
    
    public EmailToolTests(GraphClientFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task TestCase1() { /* ... */ }
}
```

## 統合テスト

### WebApplicationFactory パターン

```csharp
public class SalesAgentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public SalesAgentIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task PostSalesSummary_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new SalesSummaryRequest
        {
            Query = "今週の商談サマリ",
            StartDate = DateTime.Now.AddDays(-7),
            EndDate = DateTime.Now
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/sales-summary", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SalesSummaryResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Response);
    }
}
```

## E2Eテスト

### Playwright テスト

```csharp
[Test]
public async Task Dashboard_RealTimeUpdates_DisplayCorrectly()
{
    await using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync();
    var page = await browser.NewPageAsync();
    
    // ダッシュボードを開く
    await page.GotoAsync("http://localhost:5000");
    
    // SignalR接続確認
    await page.WaitForSelectorAsync("#connection-status.connected");
    
    // API呼び出し
    await page.ClickAsync("#test-sales-summary-btn");
    
    // リアルタイム更新を待機
    await page.WaitForSelectorAsync(".notification:has-text('商談サマリ生成完了')");
    
    // メトリクス更新確認
    var requestCount = await page.TextContentAsync("#total-requests");
    Assert.That(requestCount, Is.Not.EqualTo("0"));
}
```

## モックパターン

### ILLMProvider Mock

```csharp
public class MockLLMProvider : ILLMProvider
{
    public string ProviderName => "Mock";
    
    public IChatClient GetChatClient()
    {
        var mockClient = new Mock<IChatClient>();
        mockClient
            .Setup(x => x.CompleteAsync(It.IsAny<IList<ChatMessage>>(), null, default))
            .ReturnsAsync(new ChatCompletion
            {
                Message = new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    Content = "テストレスポンス"
                }
            });
        
        return mockClient.Object;
    }
}
```

### GraphServiceClient Mock

```csharp
var mockGraphClient = new Mock<GraphServiceClient>();

// メール検索のモック
mockGraphClient
    .Setup(x => x.Users[It.IsAny<string>()].Messages.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
    .ReturnsAsync(new MessageCollectionResponse
    {
        Value = CreateMockMessages()
    });

// カレンダー検索のモック
mockGraphClient
    .Setup(x => x.Users[It.IsAny<string>()].Calendar.Events.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
    .ReturnsAsync(new EventCollectionResponse
    {
        Value = CreateMockEvents()
    });
```

## テストカバレッジ

### カバレッジ収集

```bash
# テスト実行 + カバレッジ収集
dotnet test --collect:"XPlat Code Coverage"

# レポート生成
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### カバレッジ目標

| レイヤー | 目標カバレッジ |
|---------|--------------|
| **Services/** | 80%以上 |
| **Bot/** | 70%以上 |
| **Program.cs** | 除外（統合テストでカバー） |

## CI/CD統合

### GitHub Actions

```yaml
name: Test

on: [push, pull_request]

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
      
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: '**/coverage.cobertura.xml'
```

## 次のステップ

- **[TESTING.md](../TESTING.md)**: テスト詳細ガイド
- **[13-CODE-WALKTHROUGHS/](13-CODE-WALKTHROUGHS/)**: コードウォークスルー
