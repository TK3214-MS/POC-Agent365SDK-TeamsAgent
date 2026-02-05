namespace SalesSupportAgent.Configuration;

/// <summary>
/// LLM プロバイダーの設定
/// </summary>
public class LLMSettings
{
    /// <summary>
    /// 使用する LLM プロバイダー (LMStudio, Ollama, AzureOpenAI, OpenAI)
    /// </summary>
    public string Provider { get; set; } = "LMStudio";

    /// <summary>
    /// LM Studio 設定
    /// </summary>
    public LMStudioSettings LMStudio { get; set; } = new();

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
}

public class LMStudioSettings
{
    public string Endpoint { get; set; } = "http://localhost:1234/v1";
    public string ModelName { get; set; } = "local-model";
    public string ApiKey { get; set; } = "not-needed";
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

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o";
}
