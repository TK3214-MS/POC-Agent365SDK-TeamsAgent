using Microsoft.Extensions.AI;

namespace SalesSupportAgent.Services.LLM;

/// <summary>
/// LLM プロバイダーのインターフェイス
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// IChatClient を取得
    /// </summary>
    IChatClient GetChatClient();

    /// <summary>
    /// プロバイダー名
    /// </summary>
    string ProviderName { get; }
}
