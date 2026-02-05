using Microsoft.Extensions.AI;
using OpenAI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Telemetry;

namespace SalesSupportAgent.Services.LLM;

/// <summary>
/// GitHub Models プロバイダー (OpenAI 互換 API)
/// </summary>
public class GitHubModelsProvider : ILLMProvider
{
    private readonly GitHubModelsSettings _settings;
    private readonly IChatClient _chatClient;

    public string ProviderName => "GitHub Models";

    public GitHubModelsProvider(GitHubModelsSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrEmpty(_settings.Token))
        {
            throw new InvalidOperationException("GitHub Personal Access Token (PAT) が設定されていません。");
        }

        // OpenAI 互換クライアントを作成してIChatClientに変換
        // GitHub Models は https://models.github.ai/inference/chat/completions をエンドポイントとして使用
        var openAIClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(_settings.Token),
            new OpenAIClientOptions 
            { 
                Endpoint = new Uri("https://models.github.ai/inference/chat/completions")
            }
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
