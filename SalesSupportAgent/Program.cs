using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.A365.Observability;
using Microsoft.Agents.A365.Observability.Extensions.AgentFramework;
using Microsoft.Agents.A365.Tooling.Extensions.AgentFramework.Services;
using Microsoft.Agents.A365.Tooling.Services;
using Microsoft.Agents.Storage;
using Microsoft.Agents.Storage.Transcript;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesSupportAgent.Bot;
using SalesSupportAgent.Configuration;
using SalesSupportAgent.Resources;
using SalesSupportAgent.Services.Agent;
using SalesSupportAgent.Services.LLM;
using SalesSupportAgent.Services.MCP.McpTools;
using SalesSupportAgent.Services.Observability;
using SalesSupportAgent.Services.Notifications;
using SalesSupportAgent.Services.Transcript;
using SalesSupportAgent.Telemetry;
using SalesSupportAgent.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 設定の読み込み
// ========================================
builder.Services.Configure<LLMSettings>(builder.Configuration.GetSection("LLM"));
builder.Services.Configure<M365Settings>(builder.Configuration.GetSection("M365"));
builder.Services.Configure<BotSettings>(builder.Configuration.GetSection("Bot"));
builder.Services.Configure<TestDataSettings>(builder.Configuration.GetSection("TestData"));

// ========================================
// HttpClient の登録（Agent365 MCP Tools に必要）
// ========================================
builder.Services.AddHttpClient();

var llmSettings = builder.Configuration.GetSection("LLM").Get<LLMSettings>() ?? new LLMSettings();
var m365Settings = builder.Configuration.GetSection("M365").Get<M365Settings>() ?? new M365Settings();
var botSettings = builder.Configuration.GetSection("Bot").Get<BotSettings>() ?? new BotSettings();
var testDataSettings = builder.Configuration.GetSection("TestData").Get<TestDataSettings>() ?? new TestDataSettings();

// ========================================
// 多言語対応の初期化
// ========================================
var defaultLanguage = builder.Configuration["Localization:DefaultLanguage"] ?? "ja";
LocalizedStrings.Current.SetLanguage(defaultLanguage);

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
        "ollama" => new OllamaProvider(llmSettings.Ollama),
        "azureopenai" => new AzureOpenAIProvider(llmSettings.AzureOpenAI),
        "githubmodels" => new GitHubModelsProvider(llmSettings.GitHubModels),
        _ => throw new InvalidOperationException($"未サポートの LLM プロバイダー: {llmSettings.Provider}")
    };
});

// ========================================
// Microsoft Graph API 認証設定（Agent365 パターン）
// ========================================
builder.Services.AddSingleton(m365Settings);

// TokenCredential の作成（Managed Identity または ClientSecretCredential）
builder.Services.AddSingleton<TokenCredential>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    if (!m365Settings.IsConfigured)
    {
        logger.LogWarning("⚠️ Microsoft 365 が設定されていません。Graph API 機能は無効です。");
        // ダミー実装を返す（認証情報なしでも起動できるように）
        return new ClientSecretCredential("dummy-tenant", "dummy-client", "dummy-secret");
    }

    if (m365Settings.UseManagedIdentity)
    {
        logger.LogInformation("🔐 Managed Identity を使用して Graph API に接続します");
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = m365Settings.ClientId,
            ExcludeVisualStudioCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeAzurePowerShellCredential = true,
            Retry =
            {
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(2),
                NetworkTimeout = TimeSpan.FromSeconds(30)
            }
        });
    }
    else
    {
        logger.LogInformation("🔐 ClientSecretCredential を使用して Graph API に接続します");
        return new ClientSecretCredential(
            m365Settings.TenantId,
            m365Settings.ClientId,
            m365Settings.ClientSecret,
            new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                Retry =
                {
                    MaxRetries = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    NetworkTimeout = TimeSpan.FromSeconds(30)
                }
            });
    }
});

// GraphServiceClient をシングルトンで登録（トークンキャッシュ最適化）
builder.Services.AddSingleton<GraphServiceClient>(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("📊 GraphServiceClient を初期化しています...");
    
    return new GraphServiceClient(credential, m365Settings.Scopes);
});

// ========================================
// MCP ツールの登録（Agent365 パターン）
// ========================================
builder.Services.AddSingleton<OutlookEmailTool>();
builder.Services.AddSingleton<OutlookCalendarTool>();
builder.Services.AddSingleton<SharePointTool>();
builder.Services.AddSingleton<TeamsMessageTool>();

// テストデータ生成サービス（委任された権限用の別GraphServiceClientを使用）
builder.Services.AddSingleton<SalesSupportAgent.Services.TestData.TestDataGenerator>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SalesSupportAgent.Services.TestData.TestDataGenerator>>();
    
    if (!testDataSettings.IsConfigured)
    {
        logger.LogWarning("テストデータ生成設定が未設定です");
        // ダミーのGraphServiceClientを返す（使用時にエラーになる）
        return new SalesSupportAgent.Services.TestData.TestDataGenerator(
            new GraphServiceClient(new Azure.Identity.ChainedTokenCredential()),
            logger);
    }
    
    logger.LogInformation("🔐 デバイスコードフロー認証でテストデータ生成用 GraphServiceClient を初期化");
    
    // デバイスコードフロー認証（委任された権限）
    var deviceCodeCredential = new Azure.Identity.DeviceCodeCredential(
        new Azure.Identity.DeviceCodeCredentialOptions
        {
            TenantId = testDataSettings.TenantId,
            ClientId = testDataSettings.ClientId,
            DeviceCodeCallback = (code, cancellation) =>
            {
                Console.WriteLine();
                Console.WriteLine("=".PadRight(70, '='));
                Console.WriteLine("📱 デバイスコード認証");
                Console.WriteLine("=".PadRight(70, '='));
                Console.WriteLine($"ブラウザで以下のURLを開いてください: {code.VerificationUri}");
                Console.WriteLine($"コード: {code.UserCode}");
                Console.WriteLine("=".PadRight(70, '='));
                Console.WriteLine();
                return Task.CompletedTask;
            },
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        });
    
    // 委任された権限では具体的なスコープを指定
    var scopes = new[] 
    { 
        "User.Read",
        "Mail.ReadWrite", 
        "Calendars.ReadWrite"
    };
    
    var testDataGraphClient = new GraphServiceClient(deviceCodeCredential, scopes);
    
    return new SalesSupportAgent.Services.TestData.TestDataGenerator(testDataGraphClient, logger);
});

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
// CORS設定（Web Chat対応）
// ========================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========================================
// Bot Framework の登録
// ========================================
if (botSettings.IsConfigured)
{
    // Bot Framework 認証設定 - IConfiguration の Bot セクションを使用
    builder.Services.AddSingleton<BotFrameworkAuthentication>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var botConfiguration = configuration.GetSection("Bot");
        return new ConfigurationBotFrameworkAuthentication(botConfiguration);
    });
    
    builder.Services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
    builder.Services.AddTransient<IBot, TeamsBot>();
    
    builder.Services.AddControllers();
}

// ========================================
// SignalR の登録（Observability リアルタイム配信）
// ========================================
builder.Services.AddSignalR();
builder.Services.AddSingleton<ObservabilityService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<TranscriptService>();

// ========================================
// OpenAPI / Swagger
// ========================================
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ========================================
// HTTP リクエストパイプライン
// ========================================
// グローバル例外ハンドラー
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "未処理の例外が発生しました: {Path}", context.Request.Path);

        await context.Response.WriteAsJsonAsync(new
        {
            Error = "Internal Server Error",
            Message = exception?.Message ?? "予期しないエラーが発生しました",
            Path = context.Request.Path.ToString()
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ========================================
// 静的ファイル配信（wwwroot）
// ========================================
app.UseDefaultFiles(); // index.htmlをデフォルトファイルとして提供
app.UseStaticFiles();

// CORS を有効化
app.UseCors();

// Bot Framework エンドポイント
if (botSettings.IsConfigured)
{
    app.MapControllers();
}

// ========================================
// SignalR Hub マッピング
// ========================================
app.MapHub<ObservabilityHub>("/hubs/observability");

// ========================================
// Observability API エンドポイント
// ========================================
app.MapGet("/api/observability/metrics", (ObservabilityService observabilityService) =>
{
    var metrics = observabilityService.GetMetricsSummary();
    return Results.Json(metrics);
})
.WithName("GetObservabilityMetrics");

app.MapGet("/api/observability/traces", (ObservabilityService observabilityService, int count = 20) =>
{
    var traces = observabilityService.GetRecentTraces(count);
    return Results.Ok(traces);
})
.WithName("GetRecentTraces");

// ========================================
// Transcript & Conversation History API
// ========================================
app.MapGet("/api/transcript/conversations", (TranscriptService transcriptService) =>
{
    var conversations = transcriptService.GetAllConversations();
    return Results.Ok(conversations);
})
.WithName("GetAllConversations");

app.MapGet("/api/transcript/history/{conversationId}", async (
    string conversationId,
    TranscriptService transcriptService,
    int limit = 50) =>
{
    var history = await transcriptService.GetConversationHistoryAsync(conversationId, limit);
    return Results.Ok(history);
})
.WithName("GetConversationHistory");

app.MapGet("/api/transcript/statistics", (TranscriptService transcriptService) =>
{
    var stats = transcriptService.GetStatistics();
    return Results.Ok(stats);
})
.WithName("GetTranscriptStatistics");

app.MapDelete("/api/transcript/history/{conversationId}", async (
    string conversationId,
    TranscriptService transcriptService) =>
{
    await transcriptService.DeleteConversationHistoryAsync(conversationId);
    return Results.Ok(new { Message = $"Conversation {conversationId} deleted" });
})
.WithName("DeleteConversationHistory");

// ========================================
// Notification History API
// ========================================
app.MapGet("/api/notifications/history", (NotificationService notificationService, int count = 20) =>
{
    var notifications = notificationService.GetNotificationHistory(count);
    return Results.Ok(notifications);
})
.WithName("GetNotificationHistory");

app.MapGet("/api/notifications/operation/{operationId}", (
    string operationId,
    NotificationService notificationService) =>
{
    var notifications = notificationService.GetNotificationsByOperation(operationId);
    return Results.Ok(notifications);
})
.WithName("GetNotificationsByOperation");

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
// Graph API テストエンドポイント
// ========================================
app.MapGet("/api/test/graph/profile", async (GraphServiceClient graphClient, M365Settings m365Settings) =>
{
    try
    {
        var user = await graphClient.Users[m365Settings.UserId].GetAsync();
        return Results.Ok(new { 
            Success = true,
            DisplayName = user?.DisplayName,
            Email = user?.Mail ?? user?.UserPrincipalName,
            Id = user?.Id
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Graph API エラー: {ex.Message}");
    }
})
.WithName("TestGraphProfile");

app.MapGet("/api/test/graph/emails", async (OutlookEmailTool emailTool, int days = 7) =>
{
    try
    {
        var result = await emailTool.SearchSalesEmails(
            DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "商談,営業"
        );
        return Results.Ok(new { Success = true, Result = result });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Email Tool エラー: {ex.Message}");
    }
})
.WithName("TestGraphEmails");

app.MapGet("/api/test/graph/calendar", async (OutlookCalendarTool calendarTool, int days = 7) =>
{
    try
    {
        var result = await calendarTool.SearchSalesMeetings(
            DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "商談,営業,ミーティング"
        );
        return Results.Ok(new { Success = true, Result = result });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Calendar Tool エラー: {ex.Message}");
    }
})
.WithName("TestGraphCalendar");

// 診断用：フィルターなしで受信トレイメールを取得
app.MapGet("/api/test/graph/emails/raw", async (GraphServiceClient graphClient, M365Settings settings, int top = 10) =>
{
    try
    {
        var messages = await graphClient.Users[settings.UserId].Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Top = top;
                config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime", "sentDateTime", "categories", "isDraft" };
                config.QueryParameters.Orderby = new[] { "receivedDateTime desc" };
            });

        var result = messages?.Value?.Select(m => new
        {
            Subject = m.Subject,
            From = m.From?.EmailAddress?.Address,
            ReceivedDateTime = m.ReceivedDateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            SentDateTime = m.SentDateTime?.ToString("yyyy-MM-dd HH:mm:ss"),
            Categories = m.Categories,
            IsDraft = m.IsDraft
        });

        return Results.Ok(new { Success = true, Count = result?.Count() ?? 0, Messages = result });
    }
    catch (Exception ex)
    {
        return Results.Problem($"診断 API エラー: {ex.Message}");
    }
})
.WithName("DiagnosticEmails");

// ========================================
// テストデータ生成エンドポイント
// ========================================
app.MapPost("/api/testdata/generate", async (
    SalesSupportAgent.Services.TestData.TestDataGenerator generator,
    int emailCount = 50,
    int eventCount = 30) =>
{
    try
    {
        var startDate = DateTime.Now.AddMonths(-2);
        var endDate = DateTime.Now.AddYears(1);

        var emailsCreated = await generator.GenerateSalesEmailsAsync(startDate, endDate, emailCount);
        var eventsCreated = await generator.GenerateCalendarEventsAsync(startDate, endDate, eventCount);

        return Results.Ok(new
        {
            Success = true,
            Message = "テストデータ生成完了",
            EmailsCreated = emailsCreated,
            EventsCreated = eventsCreated,
            Period = new
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"テストデータ生成エラー: {ex.Message}\n\nスタックトレース: {ex.StackTrace}");
    }
})
.WithName("GenerateTestData")
.WithDescription("商談関連のテストデータを生成します（メール・予定）");

app.MapPost("/api/testdata/generate/emails", async (
    SalesSupportAgent.Services.TestData.TestDataGenerator generator,
    int count = 50) =>
{
    try
    {
        var startDate = DateTime.Now.AddMonths(-2);
        var endDate = DateTime.Now.AddYears(1);
        var created = await generator.GenerateSalesEmailsAsync(startDate, endDate, count);

        return Results.Ok(new
        {
            Success = true,
            Message = $"{created}件の商談メールを生成しました",
            Created = created
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"メール生成エラー: {ex.Message}");
    }
})
.WithName("GenerateTestEmails");

app.MapPost("/api/testdata/generate/events", async (
    SalesSupportAgent.Services.TestData.TestDataGenerator generator,
    int count = 30) =>
{
    try
    {
        var startDate = DateTime.Now.AddMonths(-2);
        var endDate = DateTime.Now.AddYears(1);
        var created = await generator.GenerateCalendarEventsAsync(startDate, endDate, count);

        return Results.Ok(new
        {
            Success = true,
            Message = $"{created}件の商談予定を生成しました",
            Created = created
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"予定生成エラー: {ex.Message}");
    }
})
.WithName("GenerateTestEvents");

// ========================================
// 起動ログ
// ========================================
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("========================================");
startupLogger.LogInformation("営業支援エージェント起動");
startupLogger.LogInformation("LLM Provider: {Provider}", llmSettings.Provider);
startupLogger.LogInformation("M365 設定: {Status}", m365Settings.IsConfigured ? "✅ 有効" : "❌ 未設定");
startupLogger.LogInformation("Bot 設定: {Status}", botSettings.IsConfigured ? "✅ 有効" : "❌ 未設定");
if (botSettings.IsConfigured)
{
    startupLogger.LogInformation("  - MicrosoftAppType: {AppType}", botSettings.MicrosoftAppType);
    startupLogger.LogInformation("  - MicrosoftAppId: {AppId}", botSettings.MicrosoftAppId);
    startupLogger.LogInformation("  - MicrosoftAppTenantId: {TenantId}", botSettings.MicrosoftAppTenantId);
    startupLogger.LogInformation("  - MicrosoftAppPassword: {HasPassword}", string.IsNullOrEmpty(botSettings.MicrosoftAppPassword) ? "未設定" : "設定済み");
}
startupLogger.LogInformation("========================================");

if (!botSettings.IsConfigured)
{
    startupLogger.LogWarning("Teams Bot が設定されていません。appsettings.json の Bot セクションを設定してください。");
}

app.Run();
