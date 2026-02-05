using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Telemetry;

namespace SalesSupportAgent.Services.LLM;

/// <summary>
/// Azure OpenAI プロバイダー
/// </summary>
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly AzureOpenAISettings _settings;
    private readonly IChatClient _chatClient;

    public string ProviderName => "Azure OpenAI";

    public AzureOpenAIProvider(AzureOpenAISettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        if (string.IsNullOrEmpty(_settings.Endpoint) || 
            string.IsNullOrEmpty(_settings.ApiKey) || 
            string.IsNullOrEmpty(_settings.DeploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI settings are not configured properly.");
        }

        // Azure OpenAI クライアントを作成してIChatClientに変換
        var azureClient = new AzureOpenAIClient(
            new Uri(_settings.Endpoint),
            new AzureKeyCredential(_settings.ApiKey)
        );

        // Agent365 パターン: Builder で Function Invocation と OpenTelemetry を追加
        _chatClient = azureClient
            .GetChatClient(_settings.DeploymentName)
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
