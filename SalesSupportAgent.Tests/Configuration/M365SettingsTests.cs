using SalesSupportAgent.Configuration;

namespace SalesSupportAgent.Tests.Configuration;

public class M365SettingsTests
{
    [Fact]
    public void IsConfigured_ShouldReturnTrue_WhenAllCredentialsAreProvided()
    {
        // Arrange
        var settings = new M365Settings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            UseManagedIdentity = false
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnTrue_WhenManagedIdentityIsEnabled()
    {
        // Arrange
        var settings = new M365Settings
        {
            ClientId = "test-client-id",
            UseManagedIdentity = true
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenTenantIdIsMissing()
    {
        // Arrange
        var settings = new M365Settings
        {
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            UseManagedIdentity = false
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenClientIdIsMissing()
    {
        // Arrange
        var settings = new M365Settings
        {
            TenantId = "test-tenant-id",
            ClientSecret = "test-secret",
            UseManagedIdentity = false
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenClientSecretIsMissing()
    {
        // Arrange
        var settings = new M365Settings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            UseManagedIdentity = false
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("", "", "", false)]
    [InlineData("tenant", "", "", false)]
    [InlineData("", "client", "", false)]
    [InlineData("", "", "secret", false)]
    public void IsConfigured_ShouldReturnFalse_ForVariousIncompleteSettings(
        string tenantId, string clientId, string clientSecret, bool useManagedIdentity)
    {
        // Arrange
        var settings = new M365Settings
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret,
            UseManagedIdentity = useManagedIdentity
        };

        // Act
        var result = settings.IsConfigured;

        // Assert
        Assert.False(result);
    }
}
