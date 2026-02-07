using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SalesSupportAgent.Models;
using SalesSupportAgent.Resources;
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
            reply.Text = $"{string.Format(LocalizedStrings.Current.ProcessingTime, response.ProcessingTimeMs)} | {string.Format(LocalizedStrings.Current.LLMProviderInfo, response.LLMProvider)}";
            
            await turnContext.SendActivityAsync(reply, cancellationToken);

            _logger.LogInformation("Teams 応答送信完了 (Adaptive Card): {ProcessingTime}ms", response.ProcessingTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams メッセージ処理エラー");
            
            // エラーも Adaptive Card で表示
            var errorCard = AdaptiveCardHelper.CreateAgentResponseCard(
                LocalizedStrings.Current.ErrorOccurred,
                string.Format(LocalizedStrings.Current.ErrorDetails, ex.Message),
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
                var welcomeCard = AdaptiveCardHelper.CreateAgentResponseCard(
                    LocalizedStrings.Current.WelcomeTitle,
                    LocalizedStrings.Current.WelcomeContent
                );
                
                var welcomeReply = MessageFactory.Attachment(welcomeCard);
                await turnContext.SendActivityAsync(welcomeReply, cancellationToken);
            }
        }
    }
}
