using SalesSupportAgent.Models;

namespace SalesSupportAgent.Tests.Models;

public class SalesSummaryModelsTests
{
    [Fact]
    public void SalesSummaryRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new SalesSummaryRequest();

        // Assert
        Assert.Equal(string.Empty, request.Query);
        Assert.Null(request.StartDate);
        Assert.Null(request.EndDate);
    }

    [Fact]
    public void SalesSummaryRequest_ShouldAcceptCustomValues()
    {
        // Arrange
        var query = "今週の商談サマリ";
        var startDate = new DateTime(2026, 2, 1);
        var endDate = new DateTime(2026, 2, 7);

        // Act
        var request = new SalesSummaryRequest
        {
            Query = query,
            StartDate = startDate,
            EndDate = endDate
        };

        // Assert
        Assert.Equal(query, request.Query);
        Assert.Equal(startDate, request.StartDate);
        Assert.Equal(endDate, request.EndDate);
    }

    [Fact]
    public void SalesSummaryResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new SalesSummaryResponse();

        // Assert
        Assert.Equal(string.Empty, response.Response);
        Assert.Empty(response.DataSources);
        Assert.Equal(0, response.ProcessingTimeMs);
        Assert.Equal(string.Empty, response.LLMProvider);
    }

    [Fact]
    public void SalesSummaryResponse_ShouldAcceptCustomValues()
    {
        // Arrange
        var responseText = "商談サマリの結果";
        var dataSources = new List<string> { "Outlook", "SharePoint" };
        var processingTime = 1500L;
        var llmProvider = "GitHub Models";

        // Act
        var response = new SalesSummaryResponse
        {
            Response = responseText,
            DataSources = dataSources,
            ProcessingTimeMs = processingTime,
            LLMProvider = llmProvider
        };

        // Assert
        Assert.Equal(responseText, response.Response);
        Assert.Equal(2, response.DataSources.Count);
        Assert.Contains("Outlook", response.DataSources);
        Assert.Contains("SharePoint", response.DataSources);
        Assert.Equal(processingTime, response.ProcessingTimeMs);
        Assert.Equal(llmProvider, response.LLMProvider);
    }

    [Theory]
    [InlineData("今週の商談")]
    [InlineData("先週の進捗")]
    [InlineData("顧客Aに関する情報")]
    public void SalesSummaryRequest_ShouldAcceptVariousQueries(string query)
    {
        // Arrange & Act
        var request = new SalesSummaryRequest { Query = query };

        // Assert
        Assert.Equal(query, request.Query);
    }
}
