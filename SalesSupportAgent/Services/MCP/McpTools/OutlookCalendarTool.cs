using System.ComponentModel;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Resources;

namespace SalesSupportAgent.Services.MCP.McpTools;

/// <summary>
/// Outlook カレンダー取得ツール
/// </summary>
public class OutlookCalendarTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly bool _isConfigured;
    private readonly string _userId;

    public OutlookCalendarTool(GraphServiceClient graphClient, M365Settings settings)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        ArgumentNullException.ThrowIfNull(settings);
        _isConfigured = settings.IsConfigured;
        _userId = settings.UserId;
    }

    /// <summary>
    /// 商談関連のカレンダー予定を検索
    /// </summary>
    /// <param name="startDate">開始日</param>
    /// <param name="endDate">終了日</param>
    /// <param name="keywords">検索キーワード（カンマ区切り）</param>
    /// <returns>予定サマリ</returns>
    [Description("商談関連のカレンダー予定を検索して取得します")]
    public async Task<string> SearchSalesMeetings(
        [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
        [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
        [Description("検索キーワード（例: 商談,打ち合わせ,ミーティング）")] string keywords = "商談,打ち合わせ,ミーティング,面談")
    {
        if (!_isConfigured)
        {
            return LocalizedStrings.Current.M365NotConfigured;
        }

        try
        {
            var start = DateTime.SpecifyKind(DateTime.Parse(startDate), DateTimeKind.Utc);
            var end = DateTime.SpecifyKind(DateTime.Parse(endDate).AddDays(1), DateTimeKind.Utc); // 終了日を含めるため+1日

            // Agent Identity を使用して特定ユーザーのカレンダーにアクセス
            var events = await _graphClient.Users[_userId].CalendarView
                .GetAsync(config =>
                {
                    config.QueryParameters.StartDateTime = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    config.QueryParameters.EndDateTime = end.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    config.QueryParameters.Select = new[] { "subject", "start", "end", "location", "attendees", "organizer", "categories" };
                    config.QueryParameters.Orderby = new[] { "start/dateTime" };
                });

            if (events?.Value == null || events.Value.Count == 0)
            {
                return $"📅 期間 {startDate} ~ {endDate} のカレンダー予定は見つかりませんでした。";
            }

            // キーワードでフィルタリング（件名、カテゴリを対象）
            var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
            var filteredEvents = events.Value
                .Where(e => keywordList.Any(k => 
                    e.Subject?.Contains(k, StringComparison.OrdinalIgnoreCase) == true ||
                    e.Categories?.Any(c => c.Contains(k, StringComparison.OrdinalIgnoreCase)) == true))
                .ToList();

            if (filteredEvents.Count == 0)
            {
                return $"📅 期間 {startDate} ~ {endDate} でキーワード「{keywords}」に一致する予定は見つかりませんでした。";
            }

            var summary = $"📅 **商談関連予定 ({filteredEvents.Count}件)**\n\n";
            foreach (var evt in filteredEvents)
            {
                summary += $"- **{evt.Subject}**\n";
                summary += $"  日時: {evt.Start?.DateTime} ~ {evt.End?.DateTime}\n";
                summary += $"  場所: {evt.Location?.DisplayName ?? "オンライン/未設定"}\n";
                summary += $"  主催者: {evt.Organizer?.EmailAddress?.Name ?? "不明"}\n";
                summary += $"  参加者: {evt.Attendees?.Count ?? 0}名\n\n";
            }

            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ カレンダー取得エラー: {ex.Message}";
        }
    }
}
