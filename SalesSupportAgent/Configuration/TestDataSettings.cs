namespace SalesSupportAgent.Configuration;

/// <summary>
/// テストデータ生成用設定
/// </summary>
public class TestDataSettings
{
    /// <summary>
    /// テストデータ生成用アプリのクライアントID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// テナントID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 設定が有効かどうか
    /// </summary>
    public bool IsConfigured => 
        !string.IsNullOrEmpty(ClientId) && 
        !string.IsNullOrEmpty(TenantId);
}
