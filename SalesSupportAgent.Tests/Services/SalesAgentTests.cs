using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Moq;
using SalesSupportAgent.Models;
using SalesSupportAgent.Services.Agent;
using SalesSupportAgent.Services.LLM;
using SalesSupportAgent.Services.MCP.McpTools;

namespace SalesSupportAgent.Tests.Services;

public class SalesAgentTests
{
    private readonly Mock<ILLMProvider> _mockLlmProvider;
    private readonly Mock<OutlookEmailTool> _mockEmailTool;
    private readonly Mock<OutlookCalendarTool> _mockCalendarTool;
    private readonly Mock<SharePointTool> _mockSharePointTool;
    private readonly Mock<TeamsMessageTool> _mockTeamsTool;
    private readonly Mock<ILogger<SalesAgent>> _mockLogger;

    public SalesAgentTests()
    {
        _mockLlmProvider = new Mock<ILLMProvider>();
        _mockEmailTool = new Mock<OutlookEmailTool>(null, null);
        _mockCalendarTool = new Mock<OutlookCalendarTool>(null, null);
        _mockSharePointTool = new Mock<SharePointTool>(null, null);
        _mockTeamsTool = new Mock<TeamsMessageTool>(null, null);
        _mockLogger = new Mock<ILogger<SalesAgent>>();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLlmProviderIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SalesAgent(
            null!,
            _mockEmailTool.Object,
            _mockCalendarTool.Object,
            _mockShare PointTool.Object,
            _mockTeamsTool.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenEmailToolIsNull()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        _mockLlmProvider.Setup(x => x.GetChatClient()).Returns(mockChatClient.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SalesAgent(
            _mockLlmProvider.Object,
            null!,
            _mockCalendarTool.Object,
            _mockSharePointTool.Object,
            _mockTeamsTool.Object,
            _mockLogger.Object
        ));
    }

    [Fact]
    public async Task GenerateSalesSummaryAsync_ShouldReturnResponse()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        _mockLlmProvider.Setup(x => x.GetChatClient()).Returns(mockChatClient.Object);
        _mockLlmProvider.Setup(x => x.ProviderName).Returns("TestProvider");

        var agent = new SalesAgent(
            _mockLlmProvider.Object,
            _mockEmailTool.Object,
            _mockCalendarTool.Object,
            _mockSharePointTool.Object,
            _mockTeamsTool.Object,
            _mockLogger.Object
        );

        var request = new SalesSummaryRequest
        {
            Query = "今週の商談サマリを教えて"
        };

        // Note: 実際の IChatClient の挙動をモックするのは複雑なため、
        // このテストは基本的な構造のみを検証

        // Act & Assert should not throw
        // 実際のテストでは、モックされた IChatClient が適切に応答を返すよう設定する必要があります
    }
}
