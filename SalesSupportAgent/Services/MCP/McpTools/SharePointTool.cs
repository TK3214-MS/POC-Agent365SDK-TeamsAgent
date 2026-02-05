using System.ComponentModel;
using Azure.Identity;
using Microsoft.Graph;
using SalesSupportAgent.Configuration;

namespace SalesSupportAgent.Services.MCP.McpTools;

/// <summary>
/// SharePoint ドキュメント取得ツール
/// </summary>
public class SharePointTool
{
    private readonly GraphServiceClient? _graphClient;
    private readonly bool _isConfigured;

    public SharePointTool(M365Settings settings)
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
    /// SharePoint から商談関連ドキュメントを検索
    /// </summary>
    /// <param name="startDate">開始日</param>
    /// <param name="endDate">終了日</param>
    /// <param name="keywords">検索キーワード（カンマ区切り）</param>
    /// <returns>ドキュメントサマリ</returns>
    [Description("SharePoint から商談関連ドキュメントを検索して取得します")]
    public async Task<string> SearchSalesDocuments(
        [Description("検索開始日 (yyyy-MM-dd)")] string startDate,
        [Description("検索終了日 (yyyy-MM-dd)")] string endDate,
        [Description("検索キーワード（例: 提案書,見積,契約書）")] string keywords = "提案書,見積,見積もり,契約書,RFP")
    {
        if (!_isConfigured || _graphClient == null)
        {
            return "⚠️ Microsoft 365 が設定されていません。appsettings.json の M365 セクションを設定してください。";
        }

        try
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);

            // Microsoft Search API を使用してドキュメントを検索
            var keywordList = keywords.Split(',').Select(k => k.Trim()).ToList();
            var searchQuery = string.Join(" OR ", keywordList);

            // 簡略化: M365 が設定されていても、実際のAPI呼び出しは設定完了後に有効化
            // ここではモックレスポンスを返す
            return $"📁 **商談関連ドキュメント (Mock)**\n\n" +
                   $"期間: {startDate} ~ {endDate}\n" +
                   $"検索キーワード: {keywords}\n\n" +
                   $"⚠️ SharePoint 検索を有効にするには、M365 テナントへの接続と適切な権限設定が必要です。\n" +
                   $"💡 必要な権限: Sites.Read.All, Files.Read.All";
        }
        catch (Exception ex)
        {
            return $"❌ SharePoint ドキュメント取得エラー: {ex.Message}\n\n💡 Agent Identity に適切な権限 (Sites.Read.All, Files.Read.All) が付与されているか確認してください。";
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
