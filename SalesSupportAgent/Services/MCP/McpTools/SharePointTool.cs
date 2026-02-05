using System.ComponentModel;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.Search;
using Microsoft.Graph.Search.Query;
using SalesSupportAgent.Configuration;

namespace SalesSupportAgent.Services.MCP.McpTools;

/// <summary>
/// SharePoint ドキュメント取得ツール
/// </summary>
public class SharePointTool
{
    private readonly GraphServiceClient _graphClient;
    private readonly bool _isConfigured;

    public SharePointTool(GraphServiceClient graphClient, M365Settings settings)
    {
        _graphClient = graphClient;
        _isConfigured = settings.IsConfigured;
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
        if (!_isConfigured)
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

            // Microsoft Search API リクエストを構築
            var searchRequest = new SearchRequest
            {
                EntityTypes = new List<EntityType?> { EntityType.DriveItem },
                Query = new SearchQuery
                {
                    QueryString = $"{searchQuery} AND LastModifiedTime>={start:yyyy-MM-dd} AND LastModifiedTime<={end:yyyy-MM-dd}"
                },
                From = 0,
                Size = 25,
                Fields = new List<string> 
                { 
                    "title", "name", "lastModifiedDateTime", "lastModifiedBy", 
                    "webUrl", "size", "fileExtension", "createdDateTime" 
                }
            };

            var requestBody = new QueryPostRequestBody
            {
                Requests = new List<SearchRequest> { searchRequest }
            };

            // Microsoft Search API を実行
            var searchResults = await _graphClient.Search.Query.PostAsQueryPostResponseAsync(requestBody);

            if (searchResults?.Value == null || searchResults.Value.Count == 0)
            {
                return $"📁 期間 {startDate} ~ {endDate} の商談関連ドキュメントは見つかりませんでした。";
            }

            var hitsContainers = searchResults.Value.FirstOrDefault()?.HitsContainers;
            if (hitsContainers == null || hitsContainers.Count == 0)
            {
                return $"📁 期間 {startDate} ~ {endDate} でキーワード「{keywords}」に一致するドキュメントは見つかりませんでした。";
            }

            var totalHits = hitsContainers.First().Total ?? 0;
            var hits = hitsContainers.First().Hits;

            if (hits == null || hits.Count == 0)
            {
                return $"📁 検索結果は 0 件でした。";
            }

            var summary = $"📁 **商談関連ドキュメント ({totalHits}件)**\n\n";
            summary += $"期間: {startDate} ~ {endDate}\n";
            summary += $"検索キーワード: {keywords}\n\n";

            foreach (var hit in hits.Take(10))
            {
                var resource = hit.Resource;
                if (resource?.AdditionalData == null) continue;

                // ドキュメント情報を抽出
                var title = GetAdditionalDataValue(resource.AdditionalData, "title") 
                           ?? GetAdditionalDataValue(resource.AdditionalData, "name") 
                           ?? "無題";
                var lastModified = GetAdditionalDataValue(resource.AdditionalData, "lastModifiedDateTime");
                var webUrl = GetAdditionalDataValue(resource.AdditionalData, "webUrl");
                var sizeStr = GetAdditionalDataValue(resource.AdditionalData, "size");
                var extension = GetAdditionalDataValue(resource.AdditionalData, "fileExtension") ?? "不明";

                long.TryParse(sizeStr, out long size);

                summary += $"- **{title}**\n";
                summary += $"  更新日時: {lastModified ?? "不明"}\n";
                summary += $"  ファイルサイズ: {FormatFileSize(size)}\n";
                summary += $"  拡張子: .{extension}\n";
                
                if (!string.IsNullOrEmpty(webUrl))
                {
                    summary += $"  URL: {webUrl}\n";
                }
                
                summary += "\n";
            }

            if (totalHits > 10)
            {
                summary += $"\n💡 他に {totalHits - 10} 件のドキュメントがあります。\n";
            }

            return summary;
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

    /// <summary>
    /// AdditionalData から値を安全に取得
    /// </summary>
    private static string? GetAdditionalDataValue(IDictionary<string, object> additionalData, string key)
    {
        if (additionalData.TryGetValue(key, out var value))
        {
            // JsonElement の場合は文字列に変換
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetInt64().ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => jsonElement.ToString()
                };
            }
            return value?.ToString();
        }
        return null;
    }
}

