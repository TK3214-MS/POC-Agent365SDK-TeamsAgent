namespace SalesSupportAgent.Models;

/// <summary>
/// 商談サマリリクエスト
/// </summary>
public class SalesSummaryRequest
{
    /// <summary>
    /// クエリ（例: "今週の商談サマリを教えて"）
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 対象期間の開始日（省略可能、デフォルトは今週の月曜日）
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 対象期間の終了日（省略可能、デフォルトは今週の日曜日）
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// 商談サマリレスポンス
/// </summary>
public class SalesSummaryResponse
{
    /// <summary>
    /// エージェントからの応答テキスト
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// 収集したデータのソース
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// 処理時間（ミリ秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// 使用した LLM プロバイダー
    /// </summary>
    public string LLMProvider { get; set; } = string.Empty;
}
