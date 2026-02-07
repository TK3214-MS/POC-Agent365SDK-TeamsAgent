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
    /// Managed Identity を使用するかどうか (Azure 環境)
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// アクセス対象のユーザー ID (Agent Identity 使用時に必要)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Graph API のスコープ (デフォルト: .default)
    /// </summary>
    public string[] Scopes { get; set; } = new[] { "https://graph.microsoft.com/.default" };

    /// <summary>
    /// 設定が有効かどうかを確認
    /// </summary>
    public bool IsConfigured => 
        UseManagedIdentity || // Managed Identity の場合は ClientId のみ必要
        (!string.IsNullOrEmpty(TenantId) && 
         !string.IsNullOrEmpty(ClientId) && 
         !string.IsNullOrEmpty(ClientSecret));
}
