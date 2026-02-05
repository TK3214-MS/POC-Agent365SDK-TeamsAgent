namespace SalesSupportAgent.Configuration;

/// <summary>
/// Microsoft 365 / Graph API 設定
/// </summary>
public class M365Settings
{
    /// <summary>
    /// Microsoft Entra ID テナント ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// アプリケーション (クライアント) ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// クライアントシークレット (Agent Identity 用)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 設定が有効かどうかを確認
    /// </summary>
    public bool IsConfigured => 
        !string.IsNullOrEmpty(TenantId) && 
        !string.IsNullOrEmpty(ClientId) && 
        !string.IsNullOrEmpty(ClientSecret);
}
