using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace SalesSupportAgent.Bot;

/// <summary>
/// Bot Framework メッセージングエンドポイント
/// </summary>
[ApiController]
[Route("api/messages")]
public class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;
    private readonly ILogger<BotController> _logger;

    public BotController(
        IBotFrameworkHttpAdapter adapter,
        IBot bot,
        ILogger<BotController> logger)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Teams からのメッセージを受信
    /// </summary>
    [HttpPost]
    [HttpGet] // Bot Framework Emulator 対応
    public async Task PostAsync()
    {
        _logger.LogInformation("Bot メッセージ受信: {Method} {Path}", Request.Method, Request.Path);

        try
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bot メッセージ処理エラー");
            throw;
        }
    }
}
