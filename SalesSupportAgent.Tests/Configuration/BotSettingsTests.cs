using SalesSupportAgent.Configuration;

namespace SalesSupportAgent.Tests.Configuration;

public class BotSettingsTests
{
    [Fact]
    public void IsConfigured_ShouldReturnTrue_WhenAppIdAndPasswordAreProvided()
    {
        // Arrange
        var settings = new BotSettings
        {
            MicrosoftAppId = "test-app-id",
            MicrosoftAppPassword = "test-password"
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenAppIdIsMissing()
    {
        // Arrange
        var settings = new BotSettings
        {
            MicrosoftAppPassword = "test-password"
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenPasswordIsMissing()
    {
        // Arrange
        var settings = new BotSettings
        {
            MicrosoftAppId = "test-app-id"
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("app-id", "")]
    [InlineData("", "password")]
    public void IsConfigured_ShouldReturnFalse_ForVariousIncompleteSettings(
        string appId, string password)
    {
        // Arrange
        var settings = new BotSettings
        {
            MicrosoftAppId = appId,
            MicrosoftAppPassword = password
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var settings = new BotSettings();

        // Assert
        Assert.Equal("MultiTenant", settings.MicrosoftAppType);
        Assert.Equal(string.Empty, settings.MicrosoftAppId);
        Assert.Equal(string.Empty, settings.MicrosoftAppPassword);
        Assert.Equal(string.Empty, settings.MicrosoftAppTenantId);
    }
}
