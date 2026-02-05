using Microsoft.Extensions.AI;
using OpenAI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Telemetry;

namespace SalesSupportAgent.Services.LLM;

/// <summary>
/// LM Studio プロバイダー (OpenAI 互換 API)
/// </summary>
public class LMStudioProvider : ILLMProvider
{
    private readonly LMStudioSettings _settings;
    private readonly IChatClient _chatClient;

    public string ProviderName => "LM Studio";

    public LMStudioProvider(LMStudioSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // OpenAI 互換クライアントを作成してIChatClientに変換
        var openAIClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(_settings.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(_settings.Endpoint) }
        );

        // Agent365 パターン: ミドルウェアで FunctionInvocation と OpenTelemetry を追加
        _chatClient = openAIClient.GetChatClient(_settings.ModelName)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(
                sourceName: AgentMetrics.SourceName,
                configure: (cfg) => cfg.EnableSensitiveData = false)
            .Build();
    }

    public IChatClient GetChatClient()
    {
        return _chatClient;
    }
}
