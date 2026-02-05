using Microsoft.Extensions.AI;
using OpenAI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Telemetry;

namespace SalesSupportAgent.Services.LLM;

/// <summary>
/// Ollama プロバイダー (OpenAI 互換 API)
/// </summary>
public class OllamaProvider : ILLMProvider
{
    private readonly OllamaSettings _settings;
    private readonly IChatClient _chatClient;

    public string ProviderName => "Ollama";

    public OllamaProvider(OllamaSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // OpenAI 互換クライアントを作成してIChatClientに変換
        var openAIClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential("ollama"),
            new OpenAIClientOptions { Endpoint = new Uri($"{_settings.Endpoint}/v1") }
        );

        // Agent365 パターン: Builder で Function Invocation と OpenTelemetry を追加
        _chatClient = openAIClient
            .GetChatClient(_settings.ModelName)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(sourceName: AgentMetrics.SourceName)
            .Build();
    }

    public IChatClient GetChatClient()
    {
        return _chatClient;
    }
}
