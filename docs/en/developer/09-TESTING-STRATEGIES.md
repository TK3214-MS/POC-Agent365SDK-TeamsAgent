# Testing Strategies - Unit, Integration, and E2E Testing

> **Language**: [🇯🇵 日本語](../../developer/09-TESTING-STRATEGIES.md) | 🇬🇧 English

## 📋 Testing Pyramid

```
       /\
      /E2E\        <- Few, Slow, Real environments
     /------\
    /  INT   \     <- Medium, Graph API mocks
   /----------\
  /   UNIT     \   <- Many, Fast, Isolated
 /--------------\
```

---

## Unit Testing

### Mock Pattern with Moq

```csharp
[Fact]
public async Task SearchSalesEmails_ShouldReturnFormattedResults()
{
    // Arrange
    var mockGraphClient = new Mock<GraphServiceClient>();
    var mockLogger = new Mock<ILogger<OutlookEmailTool>>();
    
    var messages = new MessageCollectionResponse
    {
        Value = new List<Message>
        {
            new Message { Subject = "Test Email", From = new Recipient { EmailAddress = new EmailAddress { Address = "test@example.com" } } }
        }
    };
    
    mockGraphClient.Setup(x => x.Users[It.IsAny<string>()].Messages.GetAsync(It.IsAny<Action<RequestConfiguration>>(), default))
        .ReturnsAsync(messages);
    
    var tool = new OutlookEmailTool(mockGraphClient.Object, mockSettings, mockLogger.Object);
    
    // Act
    var result = await tool.SearchSalesEmails("2026-02-01", "2026-02-07", "proposal");
    
    // Assert
    Assert.Contains("Test Email", result);
}
```

---

## Integration Testing

### Graph API Integration Test

```csharp
[Fact]
public async Task SearchSalesEmails_WithRealGraphAPI_ShouldReturnResults()
{
    // Arrange - Use real GraphServiceClient with test credentials
    var credential = new ClientSecretCredential(
        TestConfiguration.TenantId,
        TestConfiguration.ClientId,
        TestConfiguration.ClientSecret);
    
    var graphClient = new GraphServiceClient(credential);
    var tool = new OutlookEmailTool(graphClient, settings, logger);
    
    // Act
    var result = await tool.SearchSalesEmails(
        startDate: DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd"),
        endDate: DateTime.Now.ToString("yyyy-MM-dd"),
        keywords: "test");
    
    // Assert
    Assert.NotNull(result);
    Assert.Contains("メール", result); // Japanese response format
}
```

---

## End-to-End Testing

### Full Agent Workflow Test

```csharp
[Fact]
public async Task GenerateSalesSummary_EndToEnd_ShouldReturnCompleteResponse()
{
    // Arrange - Real services
    var salesAgent = CreateRealSalesAgent();
    var request = new SalesSummaryRequest
    {
        Query = "今週の商談サマリを教えてください",
        StartDate = DateTime.Now.AddDays(-7),
        EndDate = DateTime.Now
    };
    
    // Act
    var response = await salesAgent.GenerateSalesSummaryAsync(request);
    
    // Assert
    Assert.NotNull(response.Response);
    Assert.True(response.ProcessingTimeMs > 0);
    Assert.Contains("サマリ", response.Response);
}
```

---

For complete test examples, mocking strategies, CI/CD integration, and coverage reports, please refer to the Japanese version at [../developer/09-TESTING-STRATEGIES.md](../../developer/09-TESTING-STRATEGIES.md).
