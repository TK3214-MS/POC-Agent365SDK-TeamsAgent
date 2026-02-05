using Microsoft.Agents.A365.Observability;
using Microsoft.Agents.A365.Observability.Extensions.AgentFramework;
using Microsoft.Agents.A365.Tooling.Extensions.AgentFramework.Services;
using Microsoft.Agents.A365.Tooling.Services;
using Microsoft.Agents.Storage;
using Microsoft.Agents.Storage.Transcript;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesSupportAgent.Bot;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Services.Agent;
using SalesSupportAgent.Services.LLM;
using SalesSupportAgent.Services.MCP.McpTools;
using SalesSupportAgent.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 設定の読み込み
// ========================================
builder.Services.Configure<LLMSettings>(builder.Configuration.GetSection("LLM"));
builder.Services.Configure<M365Settings>(builder.Configuration.GetSection("M365"));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));

var llmSettings = builder.Configuration.GetSection("LLM").Get<LLMSettings>() ?? new LLMSettings();
var m365Settings = builder.Configuration.GetSection("M365").Get<M365Settings>() ?? new M365Settings();
var botSettings = builder.Configuration.GetSection("Bot").Get<BotSettings>() ?? new BotSettings();

// ========================================
// OpenTelemetry 設定（Agent365 対応）
// ========================================
var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "SalesSupportAgent";
var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";

// 注: AddAgenticTracingExporter と AddA365Tracing は .NET 8 向けのため、
// .NET 10 では基本的な OpenTelemetry を使用し、Agent365 メトリクスは手動実装
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddSource(AgentMetrics.SourceName)  // Agent365 カスタムメトリクスソース追加
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// ========================================
// LLM プロバイダーの登録
// ========================================
builder.Services.AddSingleton<ILLMProvider>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("LLM Provider 初期化: {Provider}", llmSettings.Provider);

    return llmSettings.Provider?.ToLower() switch
    {
        "lmstudio" => new LMStudioProvider(llmSettings.LMStudio),
        "ollama" => new OllamaProvider(llmSettings.Ollama),
        "azureopenai" => new AzureOpenAIProvider(llmSettings.AzureOpenAI),
        _ => throw new InvalidOperationException($"未サポートの LLM プロバイダー: {llmSettings.Provider}")
    };
});

// ========================================
// MCP ツールの登録（Agent365 パターン）
// ========================================
builder.Services.AddSingleton(m365Settings);
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<SharePointTool>();
builder.Services.AddSingleton<TeamsMessageTool>();

// Agent365 MCP Tool Services
builder.Services.AddSingleton<IMcpToolRegistrationService, McpToolRegistrationService>();
builder.Services.AddSingleton<IMcpToolServerConfigurationService, McpToolServerConfigurationService>();

// ========================================
// ストレージ設定
// ========================================
// 開発環境では MemoryStorage を使用（本番環境では永続化ストレージを推奨）
builder.Services.AddSingleton<Microsoft.Agents.Storage.IStorage, Microsoft.Agents.Storage.MemoryStorage>();

// トランスクリプトロギング（オプション：会話を自動記録）
// 注: 本番環境ではプライバシーポリシーに従って有効化
// builder.Services.AddSingleton<Microsoft.Agents.Builder.IMiddleware[]>([
//     new TranscriptLoggerMiddleware(new FileTranscriptLogger())
// ]);

// ========================================
// エージェントの登録
// ========================================
builder.Services.AddSingleton<SalesAgent>();

// ========================================
// Bot Framework の登録
// ========================================
if (botSettings.IsConfigured)
{
    builder.Services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();
    builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
    builder.Services.AddTransient<IBot, TeamsBot>();
    
    builder.Services.AddControllers();
}

// ========================================
// OpenAPI / Swagger
// ========================================
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ========================================
// HTTP リクエストパイプライン
// ========================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Bot Framework エンドポイント
if (botSettings.IsConfigured)
{
    app.MapControllers();
}

// ========================================
// ヘルスチェックエンドポイント
// ========================================
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    LLMProvider = llmSettings.Provider,
    M365Configured = m365Settings.IsConfigured,
    BotConfigured = botSettings.IsConfigured
}))
.WithName("HealthCheck");

// ========================================
// テスト用エンドポイント（直接エージェント呼び出し）
// ========================================
app.MapPost("/api/sales-summary", async (
    SalesSupportAgent.Models.SalesSummaryRequest request,
    SalesAgent salesAgent) =>
{
    return await AgentMetrics.InvokeObservedHttpOperation("agent.sales_summary", async () =>
    {
        var response = await salesAgent.GenerateSalesSummaryAsync(request);
        return Results.Ok(response);
    });
})
.WithName("GenerateSalesSummary");

// ========================================
// 起動ログ
// ========================================
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("========================================");
startupLogger.LogInformation("営業支援エージェント起動");
startupLogger.LogInformation("LLM Provider: {Provider}", llmSettings.Provider);
startupLogger.LogInformation("M365 設定: {Status}", m365Settings.IsConfigured ? "✅ 有効" : "❌ 未設定");
startupLogger.LogInformation("Bot 設定: {Status}", botSettings.IsConfigured ? "✅ 有効" : "❌ 未設定");
startupLogger.LogInformation("========================================");

if (!botSettings.IsConfigured)
{
    startupLogger.LogWarning("Teams Bot が設定されていません。appsettings.json の Bot セクションを設定してください。");
}

app.Run();
