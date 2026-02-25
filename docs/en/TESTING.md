# Testing Strategy Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TESTING.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](TESTING.md)

**Sales Support Agent Quality Assurance** - Implementation of unit tests, integration tests, and E2E tests

---

## 📋 Overview

This guide describes the comprehensive testing strategy for the Sales Support Agent. It covers how to create and run tests using xUnit, Moq, and Microsoft.Bot.Builder.Testing.

### 💡 Test Pyramid

```
       ┌────────────┐
       │   E2E (5%) │  Teams UI, real environment tests
       ├────────────┤
       │ Integ (15%) │  Bot + Graph API, LLM integration
       ├────────────┤
       │ Unit (80%)  │  MCP Tools, isolated logic
       └────────────┘
```

| Layer | Purpose | Tools |
|-------|---------|-------|
| **Unit Tests** | Verify single function/class behavior | xUnit, Moq |
| **Integration Tests** | Confirm inter-component coordination | xUnit, TestServer |
| **E2E Tests** | End-to-end behavior verification | Playwright, Selenium |

---

## 🚀 Setup

### Create Test Project

```bash
# Navigate to test project directory
cd /path/to/POC-Agent365SDK-TeamsAgent

# Create xUnit test project
dotnet new xunit -n SalesSupportAgent.Tests
cd SalesSupportAgent.Tests

# Add required packages
dotnet add package Moq
dotnet add package Microsoft.Bot.Builder.Testing
dotnet add package Microsoft.Extensions.Logging.Abstractions
dotnet add package FluentAssertions
dotnet add package coverlet.collector

# Add project reference
dotnet add reference ../SalesSupportAgent/SalesSupportAgent.csproj
```

### Directory Structure

```
SalesSupportAgent.Tests/
├── SalesSupportAgent.Tests.csproj
├── Unit/                          # Unit tests
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
├── Integration/                   # Integration tests
│   ├── GraphIntegrationTests.cs
│   ├── BotIntegrationTests.cs
│   └── LLMIntegrationTests.cs
└── E2E/                           # E2E tests
    └── TeamsE2ETests.cs
```

---

## 🧪 Unit Tests

### MCP Tool Tests

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
                Subject = "Deal: Sample Corporation",
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = "customer@example.com"
                    }
                },
                ReceivedDateTime = DateTimeOffset.Now.AddDays(-1),
                BodyPreview = "Deal details..."
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
        var result = await _sut.SearchEmailsAsync("Sample", maxResults: 10);
        
        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Sample Corporation");
        
        _mockGraphClient.Verify(
            g => g.Me.Messages.Request()
                .Filter(It.Is<string>(f => f.Contains("Sample")))
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
        var query = "test query";
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

### LLM Provider Tests

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
        var prompt = "Tell me this week's sales summary";
        var expectedResponse = "This week's sales summary:\n1. ...";
        
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

### Adaptive Card Tests

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
        var summaryText = "This week's sales summary";
        var emails = new List<EmailSummary>
        {
            new EmailSummary
            {
                Subject = "Deal email",
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
        
        // Verify summary text is included
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

## 🔗 Integration Tests

### Graph API Integration Tests

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
        // Load settings from appsettings.Test.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();
        
        var tenantId = configuration["M365:TenantId"];
        var clientId = configuration["M365:ClientId"];
        var clientSecret = configuration["M365:ClientSecret"];
        
        // Create TokenCredential (actual authentication)
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
        var query = "test"; // Search for actually existing emails
        
        // Act
        var result = await _emailTool!.SearchEmailsAsync(query, maxResults: 5);
        
        // Assert
        result.Should().NotBeNullOrEmpty();
        // Verify actual email data is returned
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
}
```

### Bot Integration Tests

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
            .Send("Hello")
            .AssertReply(activity =>
            {
                var message = activity.AsMessageActivity();
                message.Text.Should().Contain("Sales Support Agent");
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
            .ReturnsAsync("Sales summary result");
        
        var bot = new TeamsBot(
            mockAgent.Object,
            Mock.Of<ILogger<TeamsBot>>()
        );
        
        var adapter = new TestAdapter();
        
        // Act
        await new TestFlow(adapter, bot.OnTurnAsync)
            .Send("Tell me this week's sales summary")
            .AssertReply(activity =>
            {
                // Verify Adaptive Card is returned
                activity.Attachments.Should().NotBeEmpty();
                activity.Attachments[0].ContentType.Should().Be("application/vnd.microsoft.card.adaptive");
            })
            .StartTestAsync();
        
        // Assert
        mockAgent.Verify(
            a => a.ProcessQueryAsync(It.Is<string>(q => q.Contains("sales summary"))),
            Times.Once
        );
    }
}
```

---

## 🌐 E2E Tests

### Playwright Tests

```bash
# Install Playwright
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
            Headless = false, // Set to false for debugging
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
        
        // Teams login (credentials from environment variables)
        await _page.FillAsync("#i0116", Environment.GetEnvironmentVariable("TEST_USER_EMAIL")!);
        await _page.ClickAsync("#idSIButton9");
        await _page.FillAsync("#i0118", Environment.GetEnvironmentVariable("TEST_USER_PASSWORD")!);
        await _page.ClickAsync("#idSIButton9");
        
        // Search for Bot
        await _page.ClickAsync("[aria-label='Search']");
        await _page.FillAsync("input[type='search']", "Sales Support Agent");
        await _page.ClickAsync("text=Sales Support Agent");
        
        // Send message
        await _page.FillAsync("[contenteditable='true']", "Hello");
        await _page.PressAsync("[contenteditable='true']", "Enter");
        
        // Wait for response (max 30 seconds)
        await _page.WaitForSelectorAsync(
            "text=Sales Support Agent",
            new() { Timeout = 30000 }
        );
        
        // Assert
        var responseText = await _page.TextContentAsync(".message-body");
        responseText.Should().Contain("Sales Support Agent");
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

## 📊 Coverage Measurement

### Running with coverlet

```bash
# Run tests + collect coverage
dotnet test /p:CollectCoverage=true \
             /p:CoverletOutputFormat=opencover \
             /p:Exclude="[xunit.*]*"

# Set coverage threshold
dotnet test /p:CollectCoverage=true \
             /p:Threshold=80 \
             /p:ThresholdType=line
```

### Generate Coverage Report

```bash
# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"coverage.opencover.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open report
open coveragereport/index.html
```

---

## 🚀 CI/CD Integration

### GitHub Actions Workflow

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

## 🐛 Debug Tests

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

### Selective Test Execution

```bash
# Run specific test class
dotnet test --filter "FullyQualifiedName~OutlookEmailToolTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~SearchEmailsAsync_ValidQuery"

# By category
dotnet test --filter "Category=Integration"

# Disable parallel execution (for debugging)
dotnet test --no-parallel
```

---

## 📚 Best Practices

### 1. AAA Pattern

```csharp
[Fact]
public async Task MethodName_Condition_ExpectedBehavior()
{
    // Arrange
    var input = "test";
    var expected = "result";
    
    // Act
    var actual = await _sut.MethodAsync(input);
    
    // Assert
    actual.Should().Be(expected);
}
```

### 2. Theory-based Tests

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

### 3. Using Fixtures

```csharp
public class GraphClientFixture : IDisposable
{
    public GraphServiceClient GraphClient { get; }
    
    public GraphClientFixture()
    {
        // Setup processing
        GraphClient = CreateGraphClient();
    }
    
    public void Dispose()
    {
        // Cleanup processing
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
        // Use _fixture.GraphClient
    }
}
```

---

## 🔗 Related Resources

- [Agent Development Guide](AGENT-DEVELOPMENT.md) - Agent implementation
- [Troubleshooting](TROUBLESHOOTING.md) - Problem resolution
- [xUnit Documentation](https://xunit.net/)
- [Moq Quick Reference](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions](https://fluentassertions.com/)

---

**Ensure the quality of the Sales Support Agent with comprehensive testing!** ✅
