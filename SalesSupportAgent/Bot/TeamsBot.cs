using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SalesSupportAgent.Models;
using SalesSupportAgent.Resources;
using SalesSupportAgent.Services.Agent;
using SalesSupportAgent.Services.Transcript;

namespace SalesSupportAgent.Bot;

/// <summary>
/// Teams Bot
/// </summary>
public class TeamsBot : ActivityHandler
{
    private readonly SalesAgent _salesAgent;
    private readonly ILogger<TeamsBot> _logger;
    private readonly TranscriptService _transcriptService;

    public TeamsBot(
        SalesAgent salesAgent, 
        TranscriptService transcriptService,
        ILogger<TeamsBot> logger)
    {
        _salesAgent = salesAgent ?? throw new ArgumentNullException(nameof(salesAgent));
        _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
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

        // 会話記録: ユーザーメッセージ
        var conversationId = turnContext.Activity.Conversation?.Id ?? "unknown";
        await _transcriptService.LogActivityAsync(turnContext.Activity, conversationId);

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

            // Adaptive Card で応答（LLMプロバイダーと処理時間を含む）
            var cardAttachment = AdaptiveCardHelper.CreateSalesSummaryCard(
                response.Response,
                llmProvider: response.LLMProvider,
                processingTime: response.ProcessingTimeMs
            );
            
            var reply = MessageFactory.Attachment(cardAttachment);
            
            await turnContext.SendActivityAsync(reply, cancellationToken);

            // 会話記録: Bot応答
            var botActivity = reply as Activity;
            if (botActivity != null)
            {
                botActivity.From = new ChannelAccount { Name = "SalesSupportAgent" };
                botActivity.Text = $"[Adaptive Card Response] {response.Response.Substring(0, Math.Min(100, response.Response.Length))}...";
                await _transcriptService.LogActivityAsync(botActivity, conversationId);
            }

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
