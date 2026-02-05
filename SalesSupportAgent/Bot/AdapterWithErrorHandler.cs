using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;

namespace SalesSupportAgent.Bot;

/// <summary>
/// Bot Framework アダプター（エラーハンドリング付き）
/// </summary>
public class AdapterWithErrorHandler : CloudAdapter
{
    public AdapterWithErrorHandler(
        BotFrameworkAuthentication auth,
        ILogger<CloudAdapter> logger)
        : base(auth, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            // エラーログ
            logger.LogError(exception, "Bot エラー発生");

            // ユーザーにエラーメッセージを送信
            await turnContext.SendActivityAsync($"❌ エラーが発生しました: {exception.Message}");

            // トレース送信
            await turnContext.TraceActivityAsync(
                "OnTurnError Trace",
                exception.Message,
                "https://www.botframework.com/schemas/error",
                "TurnError");
        };
    }
}
