using System.ComponentModel;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Resources;

namespace SalesSupportAgent.Services.MCP.McpTools;

/// <summary>
/// Outlook メール取得ツール
/// </summary>
public class OutlookEmailTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly bool _isConfigured;
    private readonly string _userId;

    public OutlookEmailTool(GraphServiceClient graphClient, M365Settings settings)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        ArgumentNullException.ThrowIfNull(settings);
        _isConfigured = settings.IsConfigured;
        _userId = settings.UserId;
    }

    /// <summary>
    /// 商談関連のメールを検索
    /// </summary>
    /// <param name="startDate">開始日</param>
    /// <param name="endDate">終了日</param>
    /// <param name="keywords">検索キーワード（カンマ区切り）</param>
    /// <returns>メールサマリ</returns>
    [Description("商談関連のメールを検索して取得します")]
    public async Task<string> SearchSalesEmails(
        [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
        [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
        [Description("検索キーワード（例: 商談,提案,見積）")] string keywords = "商談,提案,見積,契約")
    {
        if (!_isConfigured)
        {
            return LocalizedStrings.Current.M365NotConfigured;
        }

        try
        {
            var start = DateTime.SpecifyKind(DateTime.Parse(startDate), DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(DateTime.Parse(endDate).AddDays(1), DateTimeKind.Utc); // 終了日を含めるため+1日

            // Agent Identity を使用して特定ユーザーのメールボックスにアクセス
            var messages = await _graphClient.Users[_userId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"receivedDateTime ge {start:yyyy-MM-ddTHH:mm:ssZ} and receivedDateTime le {end:yyyy-MM-ddTHH:mm:ssZ}";
                    config.QueryParameters.Top = 50;
                    config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime", "bodyPreview", "hasAttachments", "categories" };
                    config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
                });

            if (messages?.Value == null || messages.Value.Count == 0)
            {
                return $"📧 期間 {startDate} ~ {endDate} の商談関連メールは見つかりませんでした。";
            }

            // キーワードでフィルタリング（件名、本文、カテゴリを対象）
            var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
            var filteredMessages = messages.Value
                .Where(m => keywordList.Any(k => 
                    m.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                    m.BodyPreview?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Categories?.Any(c => c.Contains(k, StringComparison.OrdinalIgnoreCase)) == true))
                .ToList();

            if (filteredMessages.Count == 0)
            {
                return $"📧 期間 {startDate} ~ {endDate} でキーワード「{keywords}」に一致するメールは見つかりませんでした。";
            }

            var summary = $"📧 **商談関連メール ({filteredMessages.Count}件)**\n\n";
            foreach (var msg in filteredMessages.Take(10))
            {
                summary += $"- **{msg.Subject}**\n";
                summary += $"  送信者: {msg.From?.EmailAddress?.Name ?? "不明"}\n";
                summary += $"  受信日時: {msg.ReceivedDateTime:yyyy/MM/dd HH:mm}\n";
                summary += $"  添付ファイル: {(msg.HasAttachments == true ? "あり" : "なし")}\n";
                summary += $"  概要: {msg.BodyPreview?.Substring(0, Math.Min(100, msg.BodyPreview.Length))}...\n\n";
            }

            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ メール取得エラー: {ex.Message}";
        }
    }
}
