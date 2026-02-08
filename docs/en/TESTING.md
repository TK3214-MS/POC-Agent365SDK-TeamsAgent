# Testing Strategy Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TESTING.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](TESTING.md)

**Sales Support Agent Quality Assurance** - Implementation of unit tests, integration tests, and E2E tests

---

## 📋 Overview

This guide explains comprehensive testing strategy for the Sales Support Agent. Covers test creation and execution using xUnit, Moq, and Microsoft.Bot.Builder.Testing.

### 💡 Test Pyramid

```
       ┌────────────┐
       │  E2E (5%)  │  Teams UI, real environment tests
       ├────────────┤
       │ Integration│  Bot + Graph API, LLM integration
       │    (15%)   │
       ├────────────┤
       │   Unit     │  MCP Tools, logic units
       │   (80%)    │
       └────────────┘
```

| Layer | Purpose | Tools |
|-------|---------|-------|
| **Unit Tests** | Validate single functions/classes | xUnit, Moq |
| **Integration Tests** | Verify component interactions | xUnit, TestServer |
| **E2E Tests** | End-to-end operation verification | Playwright, Selenium |

---

## 🚀 Setup

### Create Test Project

```bash
# Navigate to test project directory
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent

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
│   └── Bot/
│       └── TeamsBotTests.cs
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
                Subject = "Sales Discussion: Sample Corporation",
                From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = "customer@example.com"
                    }
                },
                ReceivedDateTime = DateTimeOffset.Now.AddDays(-1),
                BodyPreview = "Sales details..."
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
    
    [Fact]
    public async Task GenerateResponseAsync_ValidPrompt_ReturnsResponse()
    {
        // Arrange
        var prompt = "Show this week's sales summary";
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
    }
}
```

---

## 🔗 Integration Tests

### Graph API Integration Test

```csharp
using Xunit;
using Microsoft.Graph;
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
        var query = "test";
        
        // Act
        var result = await _emailTool!.SearchEmailsAsync(query, maxResults: 5);
        
        // Assert
        result.Should().NotBeNullOrEmpty();
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
}
```

---

## 🎭 E2E Tests

### Teams Bot E2E Test

```csharp
using Microsoft.Playwright;
using Xunit;

public class TeamsE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = false
        });
    }
    
    [Fact]
    [Trait("Category", "E2E")]
    public async Task SendMessage_ToBot_ReceivesResponse()
    {
        // This would require Teams authentication and setup
        // Example placeholder
        var page = await _browser!.NewPageAsync();
        // ... Teams automation
    }
    
    public async Task DisposeAsync()
    {
        await _browser?.CloseAsync();
        _playwright?.Dispose();
    }
}
```

---

## 🏃 Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Category

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests
dotnet test --filter "Category=Integration"

# E2E tests
dotnet test --filter "Category=E2E"
```

### With Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📚 Best Practices

### 1. Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedBehavior]
```

Example:
```csharp
SearchEmailsAsync_ValidQuery_ReturnsEmails()
SearchEmailsAsync_EmptyQuery_ThrowsException()
```

### 2. AAA Pattern

- **Arrange**: Setup test data
- **Act**: Execute method
- **Assert**: Verify results

### 3. Mock External Dependencies

Always mock:
- Graph API calls
- LLM API calls
- Database operations
- File system access

---

## 📚 Related Documentation

- [Architecture](ARCHITECTURE.md) - System design
- [Troubleshooting](TROUBLESHOOTING.md) - Common issues
- [Agent Development](AGENT-DEVELOPMENT.md) - Implementation patterns

---

**Ensure quality with comprehensive testing!** ✅
