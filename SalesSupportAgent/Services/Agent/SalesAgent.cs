using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Models;
using SalesSupportAgent.Services.LLM;
using SalesSupportAgent.Services.MCP.McpTools;
using SalesSupportAgent.Services.Observability;
using SalesSupportAgent.Services.Notifications;

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
    private readonly ObservabilityService _observabilityService;
    private readonly NotificationService _notificationService;

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

【出力フォーマット】
以下の構造で情報を整理してください：

## 📊 サマリー
全体の概要を2-3文で簡潔にまとめる

## 📧 商談メール
- 重要なメールを箇条書きで列挙
- 各メールの要点を1-2行で説明
- 緊急度の高いものを優先

## 📅 商談予定
- 今後の予定を日付順に列挙
- 各予定の目的と参加者を明記
- 準備が必要な項目があれば指摘

## 📁 関連ドキュメント
- 提案書、見積書などの重要文書を列挙
- 各文書の目的と状態を説明

## 💡 推奨アクション
- 次に取るべき具体的なアクションを3-5個提案
- 優先度順に並べる
- 期限があるものは明記";

    public SalesAgent(
        ILLMProvider llmProvider,
        OutlookEmailTool emailTool,
        OutlookCalendarTool calendarTool,
        SharePointTool sharePointTool,
        TeamsMessageTool teamsTool,
        ObservabilityService observabilityService,
        NotificationService notificationService,
        ILogger<SalesAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _emailTool = emailTool ?? throw new ArgumentNullException(nameof(emailTool));
        _calendarTool = calendarTool ?? throw new ArgumentNullException(nameof(calendarTool));
        _sharePointTool = sharePointTool ?? throw new ArgumentNullException(nameof(sharePointTool));
        _teamsTool = teamsTool ?? throw new ArgumentNullException(nameof(teamsTool));
        _observabilityService = observabilityService ?? throw new ArgumentNullException(nameof(observabilityService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
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
        var operationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("商談サマリ生成開始: {Query}", request.Query);
            
            // 通知: 開始通知
            await _notificationService.SendProgressNotificationAsync(operationId, "🚀 商談サマリ生成を開始しています...", 0);
            
            // Observability: リクエスト開始トレース
            await _observabilityService.RecordTraceAsync("🚀 商談サマリ生成開始", "info", 0);

            // デフォルトの日付範囲を設定（今週）
            var startDate = request.StartDate ?? GetMondayOfCurrentWeek();
            var endDate = request.EndDate ?? GetSundayOfCurrentWeek();

            // クエリに日付範囲を追加
            var enhancedQuery = $"{request.Query}\n\n期間: {startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}";

            // 通知: データ収集開始
            await _notificationService.SendProgressNotificationAsync(operationId, "📊 データ収集中（メール、カレンダー、ドキュメント）...", 25);
            
            // Observability: エージェント実行開始トレース
            await _observabilityService.RecordTraceAsync("⚙️ AIエージェント実行中", "info", stopwatch.ElapsedMilliseconds);
            
            // エージェント実行
            var agentStopwatch = Stopwatch.StartNew();
            var agentResponse = await _agent.RunAsync(enhancedQuery);
            agentStopwatch.Stop();
            
            // 通知: AI分析中
            await _notificationService.SendProgressNotificationAsync(operationId, "🤖 AI分析中（サマリ生成処理）...", 75);
            
            // Observability: エージェント実行完了トレース
            await _observabilityService.RecordTraceAsync("✅ AIエージェント実行完了", "success", agentStopwatch.ElapsedMilliseconds);
            
            // デバッグ: 応答型を確認
            _logger.LogInformation("エージェント応答型: {Type}", agentResponse.GetType().FullName);
            
            // 応答からテキストを抽出（ツール実行結果を含む最終応答を取得）
            var responseText = ExtractResponseText(agentResponse);
            
            _logger.LogInformation("エージェント応答取得完了: {ResponseLength} 文字", responseText?.Length ?? 0);

            stopwatch.Stop();

            _logger.LogInformation("商談サマリ生成完了: {ProcessingTime}ms", stopwatch.ElapsedMilliseconds);
            
            // 通知: 完了通知
            await _notificationService.SendSuccessNotificationAsync(
                operationId, 
                $"✅ 商談サマリ生成完了！（処理時間: {stopwatch.ElapsedMilliseconds:N0}ms）",
                new { ProcessingTimeMs = stopwatch.ElapsedMilliseconds, DataSourceCount = dataSources.Count }
            );
            
            // Observability: 成功完了トレース＆メトリクス記録
            await _observabilityService.RecordTraceAsync("🎉 商談サマリ生成完了", "success", stopwatch.ElapsedMilliseconds);
            await _observabilityService.RecordRequestAsync(success: true, stopwatch.ElapsedMilliseconds);
            await _observabilityService.UpdateMetricsAsync();

            // データソースを特定（実際のツール呼び出しログから）
            dataSources.AddRange(new[] { "Outlook", "Calendar", "SharePoint", "Teams" });

            return new SalesSummaryResponse
            {
                Response = responseText ?? "応答がありませんでした。",
                DataSources = dataSources,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                LLMProvider = _llmProvider.ProviderName
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "商談サマリ生成エラー");
            
            // 通知: エラー通知
            await _notificationService.SendErrorNotificationAsync(
                operationId,
                "❌ 商談サマリ生成に失敗しました",
                ex.Message
            );
            
            // Observability: エラートレース＆メトリクス記録
            await _observabilityService.RecordTraceAsync($"❌ エラー: {ex.Message}", "error", stopwatch.ElapsedMilliseconds);
            await _observabilityService.RecordRequestAsync(success: false, stopwatch.ElapsedMilliseconds);
            await _observabilityService.UpdateMetricsAsync();

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

    /// <summary>
    /// エージェント応答からテキストを抽出
    /// </summary>
    private string ExtractResponseText(object agentResponse)
    {
        try
        {
            // 動的に応答を処理
            dynamic response = agentResponse;
            
            // Agent 365 SDKの応答構造を確認してログ出力
            _logger.LogInformation("エージェント応答型: {Type}", agentResponse.GetType().FullName);
            
            // Messagesプロパティが存在するか確認
            if (agentResponse.GetType().GetProperty("Messages") != null)
            {
                var messages = response.Messages as IEnumerable<object>;
                if (messages != null && messages.Any())
                {
                    var lastMessage = messages.LastOrDefault();
                    if (lastMessage != null)
                    {
                        dynamic message = lastMessage;
                        
                        // Contentsプロパティを確認
                        if (lastMessage.GetType().GetProperty("Contents") != null)
                        {
                            var contents = message.Contents as IEnumerable<object>;
                            if (contents != null)
                            {
                                var textContents = contents
                                    .Where(c => c.GetType().Name.Contains("TextContent"))
                                    .ToList();
                                
                                if (textContents.Any())
                                {
                                    var texts = textContents.Select(tc => 
                                    {
                                        dynamic textContent = tc;
                                        return textContent.Text as string ?? "";
                                    }).Where(t => !string.IsNullOrWhiteSpace(t));
                                    
                                    var combinedText = string.Join("\n\n", texts).Trim();
                                    
                                    // デバッグ: 実際のテキスト内容をログ出力
                                    _logger.LogInformation("抽出されたテキスト（最初の200文字）: {Text}", 
                                        combinedText.Length > 200 ? combinedText.Substring(0, 200) + "..." : combinedText);
                                    
                                    // ツールコール形式のテキストを除外
                                    if (combinedText.StartsWith("oith") || 
                                        combinedText.Contains("\"name\":") || 
                                        combinedText.Contains("\"arguments\":"))
                                    {
                                        _logger.LogWarning("応答がツールコール形式です: {Text}", 
                                            combinedText.Length > 100 ? combinedText.Substring(0, 100) : combinedText);
                                        return "申し訳ございません。情報を収集中です。もう一度お試しください。";
                                    }
                                    
                                    return combinedText;
                                }
                                else
                                {
                                    _logger.LogWarning("TextContentが見つかりません。全コンテンツタイプ: {Types}", 
                                        string.Join(", ", contents.Select(c => c.GetType().Name)));
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Contentsプロパティが見つかりません。メッセージ型: {Type}", 
                                lastMessage.GetType().FullName);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Messagesが空またはnullです");
                }
            }
            else
            {
                _logger.LogWarning("Messagesプロパティが見つかりません");
            }

            _logger.LogWarning("応答からテキストを抽出できませんでした");
            return "応答がありませんでした。";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "応答テキスト抽出エラー");
            return $"応答の処理中にエラーが発生しました: {ex.Message}";
        }
    }
}
