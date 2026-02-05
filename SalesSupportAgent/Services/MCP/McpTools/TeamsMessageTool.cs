using System.ComponentModel;
using Azure.Identity;
using Microsoft.Graph;
using SalesSupportAgent.Configuration;

namespace SalesSupportAgent.Services.MCP.McpTools;

/// <summary>
/// Teams メッセージ取得ツール
/// </summary>
public class TeamsMessageTool
{
    private readonly GraphServiceClient? _graphClient;
    private readonly bool _isConfigured;

    public TeamsMessageTool(M365Settings settings)
    {
        if (settings.IsConfigured)
        {
            var credential = new ClientSecretCredential(
                settings.TenantId,
                settings.ClientId,
                settings.ClientSecret
            );

            _graphClient = new GraphServiceClient(credential);
            _isConfigured = true;
        }
        else
        {
            _isConfigured = false;
        }
    }

    /// <summary>
    /// Teams チャネルから商談関連メッセージを検索
    /// </summary>
    /// <param name="teamId">Teams の ID（省略可）</param>
    /// <param name="channelId">チャネルの ID（省略可）</param>
    /// <param name="keywords">検索キーワード（カンマ区切り）</param>
    /// <returns>メッセージサマリ</returns>
    [Description("Teams チャネルから商談関連メッセージを検索して取得します")]
    public async Task<string> SearchSalesMessages(
        [Description("Teams ID（省略可）")] string teamId = "",
        [Description("チャネル ID（省略可）")] string channelId = "",
        [Description("検索キーワード（例: 商談,進捗,提案）")] string keywords = "商談,進捗,提案,クライアント")
    {
        if (!_isConfigured || _graphClient == null)
        {
            return "⚠️ Microsoft 365 が設定されていません。appsettings.json の M365 セクションを設定してください。";
        }

        try
        {
            // Team ID が指定されていない場合は、ユーザーが参加している Teams を取得
            if (string.IsNullOrEmpty(teamId))
            {
                var teams = await _graphClient.Me.JoinedTeams.GetAsync();
                
                if (teams?.Value == null || teams.Value.Count == 0)
                {
                    return "📢 参加している Teams が見つかりませんでした。";
                }

                // 最初の Team を使用（デモ用）
                teamId = teams.Value.First().Id ?? "";
                
                if (string.IsNullOrEmpty(teamId))
                {
                    return "📢 Teams ID を取得できませんでした。";
                }
            }

            // Channel ID が指定されていない場合は、一般チャネルを取得
            if (string.IsNullOrEmpty(channelId))
            {
                var channels = await _graphClient.Teams[teamId].Channels.GetAsync();
                
                if (channels?.Value == null || channels.Value.Count == 0)
                {
                    return $"📢 Teams (ID: {teamId}) にチャネルが見つかりませんでした。";
                }

                // 一般チャネルを優先
                var generalChannel = channels.Value.FirstOrDefault(c => 
                    c.DisplayName?.Equals("General", StringComparison.OrdinalIgnoreCase) == true ||
                    c.DisplayName?.Equals("一般", StringComparison.OrdinalIgnoreCase) == true);
                
                channelId = (generalChannel ?? channels.Value.First()).Id ?? "";
            }

            // チャネルメッセージを取得
            var messages = await _graphClient.Teams[teamId].Channels[channelId].Messages
                .GetAsync(config =>
                {
                    config.QueryParameters.Top = 50;
                });

            if (messages?.Value == null || messages.Value.Count == 0)
            {
                return $"📢 チャネルにメッセージが見つかりませんでした。";
            }

            // キーワードでフィルタリング
            var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
            var filteredMessages = messages.Value
                .Where(m => m.Body?.Content != null && 
                    keywordList.Any(k => m.Body.Content.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (filteredMessages.Count == 0)
            {
                return $"📢 キーワード「{keywords}」に一致するメッセージは見つかりませんでした。";
            }

            var summary = $"📢 **Teams メッセージ ({filteredMessages.Count}件)**\n\n";
            foreach (var msg in filteredMessages.Take(10))
            {
                var content = msg.Body?.Content ?? "";
                // HTML タグを簡易的に除去
                content = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", "");
                
                summary += $"- **{msg.From?.User?.DisplayName ?? "不明"}** ({msg.CreatedDateTime:yyyy/MM/dd HH:mm})\n";
                summary += $"  {content.Substring(0, Math.Min(150, content.Length))}...\n\n";
            }

            return summary;
        }
        catch (Exception ex)
        {
            return $"❌ Teams メッセージ取得エラー: {ex.Message}\n\n💡 Agent Identity に適切な権限 (ChannelMessage.Read.All, Team.ReadBasic.All) が付与されているか確認してください。";
        }
    }
}
