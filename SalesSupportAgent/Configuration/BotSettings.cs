namespace SalesSupportAgent.Configuration;

/// <summary>
/// Bot Framework 設定
/// </summary>
public class BotSettings
{
    /// <summary>
    /// Bot の種類 (MultiTenant, SingleTenant, UserAssignedMSI)
    /// </summary>
    public string MicrosoftAppType { get; set; } = "MultiTenant";

    /// <summary>
    /// Microsoft App ID (Bot ID)
    /// </summary>
    public string MicrosoftAppId { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft App Password (Bot Secret)
    /// </summary>
    public string MicrosoftAppPassword { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft App Tenant ID
    /// </summary>
    public string MicrosoftAppTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 設定が有効かどうかを確認
    /// </summary>
    public bool IsConfigured => 
        !string.IsNullOrEmpty(MicrosoftAppId) && 
        !string.IsNullOrEmpty(MicrosoftAppPassword);
}
