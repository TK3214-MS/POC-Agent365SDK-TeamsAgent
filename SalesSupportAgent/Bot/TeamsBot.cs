using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SalesSupportAgent.Models;
using SalesSupportAgent.Services.Agent;

namespace SalesSupportAgent.Bot;

/// <summary>
/// Teams Bot
/// </summary>
public class TeamsBot : ActivityHandler
{
    private readonly SalesAgent _salesAgent;
    private readonly ILogger<TeamsBot> _logger;

    public TeamsBot(SalesAgent salesAgent, ILogger<TeamsBot> logger)
    {
        _salesAgent = salesAgent ?? throw new ArgumentNullException(nameof(salesAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        var userMessage = turnContext.Activity.Text?.Trim();

        if (string.IsNullOrEmpty(userMessage))
        {
            await turnContext.SendActivityAsync("メッセージを入力してください。", cancellationToken: cancellationToken);
            return;
        }

        _logger.LogInformation("Teams メッセージ受信: {Message} (User: {UserId})", 
            userMessage, turnContext.Activity.From?.Id);

        try
        {
            // タイピングインジケーターを表示
            await turnContext.SendActivityAsync(
                new Activity { Type = ActivityTypes.Typing },
                cancellationToken);

            // エージェントに問い合わせ
            var request = new SalesSummaryRequest
            {
                Query = userMessage
            };

            var response = await _salesAgent.GenerateSalesSummaryAsync(request);

            // Adaptive Card で応答
            var cardAttachment = AdaptiveCardHelper.CreateSalesSummaryCard(response.Response);
            
            var reply = MessageFactory.Attachment(cardAttachment);
            reply.Text = $"⚡ 処理時間: {response.ProcessingTimeMs}ms | 🤖 {response.LLMProvider}";
            
            await turnContext.SendActivityAsync(reply, cancellationToken);

            _logger.LogInformation("Teams 応答送信完了 (Adaptive Card): {ProcessingTime}ms", response.ProcessingTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams メッセージ処理エラー");
            
            // エラーも Adaptive Card で表示
            var errorCard = AdaptiveCardHelper.CreateAgentResponseCard(
                "エラーが発生しました",
                $"**エラー内容:**\n{ex.Message}\n\n**対処方法:**\n- appsettings.json の設定を確認してください\n- ログファイルで詳細を確認してください\n- Microsoft 365 の権限設定を確認してください",
                isError: true
            );
            
            var errorReply = MessageFactory.Attachment(errorCard);
            await turnContext.SendActivityAsync(errorReply, cancellationToken);
        }
    }

    protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                var welcomeContent = @"**できること:**
- 📧 Outlook メールから商談関連情報を収集
- 📅 カレンダーから商談予定を確認  
- 📁 SharePoint から提案書・見積書を検索
- 📢 Teams チャネルから商談関連の会話を抽出

**使い方:**
「今週の商談サマリを教えて」と話しかけてください。

**例:**
- 今週の商談サマリを教えて
- 先週の重要な商談を教えて
- 〇〇社に関する情報をまとめて

---
⚠️ 初回利用時は、管理者が Microsoft 365 と Bot の設定を完了している必要があります。";

                var welcomeCard = AdaptiveCardHelper.CreateAgentResponseCard(
                    "👋 こんにちは！営業支援エージェントです",
                    welcomeContent
                );
                
                var welcomeReply = MessageFactory.Attachment(welcomeCard);
                await turnContext.SendActivityAsync(welcomeReply, cancellationToken);
            }
        }
    }
}
