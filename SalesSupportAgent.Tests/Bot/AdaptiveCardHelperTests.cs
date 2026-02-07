using Microsoft.Extensions.Logging;
using Moq;
using SalesSupportAgent.Bot;

namespace SalesSupportAgent.Tests.Bot;

public class AdaptiveCardHelperTests
{
    [Fact]
    public void CreateAgentResponseCard_ShouldReturnValidAttachment()
    {
        // Arrange
        var title = "テストタイトル";
        var content = "テストコンテンツ";

        // Act
        var result = AdaptiveCardHelper.CreateAgentResponseCard(title, content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/vnd.microsoft.card.adaptive", result.ContentType);
        Assert.NotNull(result.Content);
    }

    [Fact]
    public void CreateAgentResponseCard_WithError_ShouldIncludeErrorStyling()
    {
        // Arrange
        var title = "エラータイトル";
        var content = "エラーコンテンツ";

        // Act
        var result = AdaptiveCardHelper.CreateAgentResponseCard(title, content, isError: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/vnd.microsoft.card.adaptive", result.ContentType);
    }

    [Fact]
    public void CreateSalesSummaryCard_ShouldReturnValidAttachment()
    {
        // Arrange
        var summary = @"## 商談関連メール (5件)
- メール1
- メール2

## 商談関連予定 (3件)
- 予定1
- 予定2";

        // Act
        var result = AdaptiveCardHelper.CreateSalesSummaryCard(summary);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/vnd.microsoft.card.adaptive", result.ContentType);
        Assert.NotNull(result.Content);
    }

    [Fact]
    public void CreateSalesSummaryCard_WithEmptySummary_ShouldReturnValidCard()
    {
        // Arrange
        var summary = "";

        // Act
        var result = AdaptiveCardHelper.CreateSalesSummaryCard(summary);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("application/vnd.microsoft.card.adaptive", result.ContentType);
    }

    [Theory]
    [InlineData("短いサマリ")]
    [InlineData("## セクション1\n内容1\n\n## セクション2\n内容2")]
    [InlineData("**太字**テスト")]
    public void CreateSalesSummaryCard_WithVariousSummaries_ShouldReturnValidCard(string summary)
    {
        // Act
        var result = AdaptiveCardHelper.CreateSalesSummaryCard(summary);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
    }
}
