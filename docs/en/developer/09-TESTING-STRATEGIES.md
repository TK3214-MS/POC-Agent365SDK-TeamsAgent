# Testing Strategies - Testing Strategies and Best Practices

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/09-TESTING-STRATEGIES.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](09-TESTING-STRATEGIES.md)

## 📋 Testing Hierarchy

### Testing Pyramid

```
        ┌────────────┐
        │  E2E Tests │  Few (Slow, Fragile, High Cost)
        └────────────┘
      ┌────────────────┐
      │Integration Tests│ Moderate (Medium Speed, Medium Cost)
      └────────────────┘
    ┌────────────────────┐
    │    Unit Tests       │ Many (Fast, Stable, Low Cost)
    └────────────────────┘
```

## Unit Testing

### xUnit + Moq Pattern

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
                    Subject = "Regarding the Deal",
                    From = new Recipient { EmailAddress = new EmailAddress { Name = "Taro Tanaka" } },
                    ReceivedDateTime = DateTimeOffset.UtcNow
                }
            }
        };
        
        mockGraphClient
            .Setup(x => x.Users[It.IsAny<string>()].Messages.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
            .ReturnsAsync(mockMessages);
        
        var tool = new OutlookEmailTool(mockGraphClient.Object, mockSettings);
        
        // Act
        var result = await tool.SearchSalesEmails("2026-02-01", "2026-02-07", "deal");
        
        // Assert
        Assert.Contains("Regarding the Deal", result);
        Assert.Contains("Taro Tanaka", result);
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

## Integration Testing

### WebApplicationFactory Pattern

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
            Query = "This week's deal summary",
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

## E2E Testing

### Playwright Tests

```csharp
[Test]
public async Task Dashboard_RealTimeUpdates_DisplayCorrectly()
{
    await using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync();
    var page = await browser.NewPageAsync();
    
    // Open the dashboard
    await page.GotoAsync("http://localhost:5000");
    
    // Verify SignalR connection
    await page.WaitForSelectorAsync("#connection-status.connected");
    
    // Make API call
    await page.ClickAsync("#test-sales-summary-btn");
    
    // Wait for real-time update
    await page.WaitForSelectorAsync(".notification:has-text('Deal summary generation complete')");
    
    // Verify metrics update
    var requestCount = await page.TextContentAsync("#total-requests");
    Assert.That(requestCount, Is.Not.EqualTo("0"));
}
```

## Mock Patterns

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
                    Content = "Test response"
                }
            });
        
        return mockClient.Object;
    }
}
```

### GraphServiceClient Mock

```csharp
var mockGraphClient = new Mock<GraphServiceClient>();

// Mock for email search
mockGraphClient
    .Setup(x => x.Users[It.IsAny<string>()].Messages.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
    .ReturnsAsync(new MessageCollectionResponse
    {
        Value = CreateMockMessages()
    });

// Mock for calendar search
mockGraphClient
    .Setup(x => x.Users[It.IsAny<string>()].Calendar.Events.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
    .ReturnsAsync(new EventCollectionResponse
    {
        Value = CreateMockEvents()
    });
```

## Test Coverage

### Collecting Coverage

```bash
# Run tests + collect coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Coverage Targets

| Layer | Target Coverage |
|---------|--------------|
| **Services/** | 80% or higher |
| **Bot/** | 70% or higher |
| **Program.cs** | Excluded (covered by integration tests) |

## CI/CD Integration

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

## Next Steps

- **[TESTING.md](../TESTING.md)**: Detailed Testing Guide
- **[13-CODE-WALKTHROUGHS/](13-CODE-WALKTHROUGHS/)**: Code Walkthroughs
