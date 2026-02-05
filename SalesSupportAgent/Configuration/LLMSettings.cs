namespace SalesSupportAgent.Configuration;

/// <summary>
/// LLM プロバイダーの設定
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// 使用する LLM プロバイダー (Ollama, AzureOpenAI, OpenAI, GitHubModels)
    /// </summary>
    public string Provider { get; set; } = "GitHubModels";

    /// <summary>
    /// Ollama 設定
    /// </summary>
    public OllamaSettings Ollama { get; set; } = new();

    /// <summary>
    /// Azure OpenAI 設定
    /// </summary>
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();

    /// <summary>
    /// OpenAI 設定
    /// </summary>
    public OpenAISettings OpenAI { get; set; } = new();

    /// <summary>
    /// GitHub Models 設定
    /// </summary>
    public GitHubModelsSettings GitHubModels { get; set; } = new();
}

public class OllamaSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "qwen2.5:latest";
}

public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}


public class GitHubModelsSettings
{
    /// <summary>
    /// GitHub Personal Access Token (PAT) with 'models' scope
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// モデル名 (例: openai/gpt-4o, openai/gpt-4o-mini, meta-llama/Llama-3.2-90B-Vision-Instruct)
    /// </summary>
    public string ModelName { get; set; } = "openai/gpt-4o-mini";
}
public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o";
}
