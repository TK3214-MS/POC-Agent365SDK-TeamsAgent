using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Models;
using SalesSupportAgent.Services.LLM;
using SalesSupportAgent.Services.MCP.McpTools;

namespace SalesSupportAgent.Services.Agent;

/// <summary>
/// 営業支援エージェント
/// </summary>
public class SalesAgent
{
    private readonly ILLMProvider _llmProvider;
    private readonly OutlookEmailTool _emailTool;
    private readonly OutlookCalendarTool _calendarTool;
    private readonly SharePointTool _sharePointTool;
    private readonly TeamsMessageTool _teamsTool;
    private readonly AIAgent _agent;
    private readonly ILogger<SalesAgent> _logger;

    private const string SystemPrompt = @"あなたは営業支援エージェントです。
以下のツールを使用して、Microsoft 365 から商談関連情報を収集し、わかりやすくサマリを作成します。

【利用可能なツール】
1. SearchSalesEmails - Outlook メールから商談関連メールを検索
2. SearchSalesMeetings - Outlook カレンダーから商談予定を検索
3. SearchSalesDocuments - SharePoint から提案書・見積書などを検索
4. SearchSalesMessages - Teams チャネルから商談関連メッセージを検索

【重要な指示】
- ユーザーからの質問に基づいて、適切なツールを選択して情報を収集してください
- 複数のツールを組み合わせて、包括的な商談サマリを作成してください
- 日本語で丁寧に回答してください
- 収集した情報を整理して、読みやすい形式で提示してください
- データが見つからない場合は、その旨を明確に伝えてください";

    public SalesAgent(
        ILLMProvider llmProvider,
        OutlookEmailTool emailTool,
        OutlookCalendarTool calendarTool,
        SharePointTool sharePointTool,
        TeamsMessageTool teamsTool,
        ILogger<SalesAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _emailTool = emailTool ?? throw new ArgumentNullException(nameof(emailTool));
        _calendarTool = calendarTool ?? throw new ArgumentNullException(nameof(calendarTool));
        _sharePointTool = sharePointTool ?? throw new ArgumentNullException(nameof(sharePointTool));
        _teamsTool = teamsTool ?? throw new ArgumentNullException(nameof(teamsTool));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // エージェント作成
        _agent = CreateAgent();
    }

    private AIAgent CreateAgent()
    {
        var chatClient = _llmProvider.GetChatClient();

        // ツールを登録
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(_emailTool.SearchSalesEmails),
            AIFunctionFactory.Create(_calendarTool.SearchSalesMeetings),
            AIFunctionFactory.Create(_sharePointTool.SearchSalesDocuments),
            AIFunctionFactory.Create(_teamsTool.SearchSalesMessages)
        };

        return chatClient.AsAIAgent(
            SystemPrompt,
            "営業支援エージェント",
            tools: tools
        );
    }

    /// <summary>
    /// 商談サマリを生成
    /// </summary>
    public async Task<SalesSummaryResponse> GenerateSalesSummaryAsync(SalesSummaryRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var dataSources = new List<string>();

        try
        {
            _logger.LogInformation("商談サマリ生成開始: {Query}", request.Query);

            // デフォルトの日付範囲を設定（今週）
            var startDate = request.StartDate ?? GetMondayOfCurrentWeek();
            var endDate = request.EndDate ?? GetSundayOfCurrentWeek();

            // クエリに日付範囲を追加
            var enhancedQuery = $"{request.Query}\n\n期間: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";

            // エージェント実行
            var agentResponse = await _agent.RunAsync(enhancedQuery);
            var response = agentResponse.Messages.LastOrDefault()?.Contents.OfType<Microsoft.Extensions.AI.TextContent>().FirstOrDefault()?.Text ?? "応答がありませんでした。";

            stopwatch.Stop();

            _logger.LogInformation("商談サマリ生成完了: {ProcessingTime}ms", stopwatch.ElapsedMilliseconds);

            // データソースを特定（実際のツール呼び出しログから）
            dataSources.AddRange(new[] { "Outlook", "Calendar", "SharePoint", "Teams" });

            return new SalesSummaryResponse
            {
                Response = response,
                DataSources = dataSources,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMProvider = _llmProvider.ProviderName
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "商談サマリ生成エラー");

            return new SalesSummaryResponse
            {
                Response = $"❌ エラーが発生しました: {ex.Message}\n\n設定を確認してください。",
                DataSources = dataSources,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMProvider = _llmProvider.ProviderName
            };
        }
    }

    private static DateTime GetMondayOfCurrentWeek()
    {
        var today = DateTime.Today;
        var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        return today.AddDays(-diff);
    }

    private static DateTime GetSundayOfCurrentWeek()
    {
        var monday = GetMondayOfCurrentWeek();
        return monday.AddDays(6);
    }
}
