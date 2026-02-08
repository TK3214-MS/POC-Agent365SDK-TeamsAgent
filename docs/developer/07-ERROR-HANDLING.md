# Error Handling - エラーハンドリング戦略

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](07-ERROR-HANDLING.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../en/developer/07-ERROR-HANDLING.md)

## 📋 エラー種別とハンドリング

### Graph API エラー

```csharp
try
{
    var messages = await _graphClient.Users[_userId].Messages.GetAsync();
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 401)
{
    _logger.LogError("認証エラー: {Message}", ex.Message);
    return "❌ 認証エラー。設定を確認してください。";
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 403)
{
    _logger.LogError("権限不足: {Message}", ex.Message);
    return "❌ 権限不足。Azure ADで権限を付与してください。";
}
catch (ServiceException ex) when (ex.ResponseStatusCode == 429)
{
    var retryAfter = ex.ResponseHeaders?.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
    _logger.LogWarning("レート制限: {Seconds}秒後に再試行", retryAfter.TotalSeconds);
    await Task.Delay(retryAfter);
    // リトライロジック
}
catch (ServiceException ex)
{
    _logger.LogError(ex, "Graph APIエラー: {Code}", ex.ResponseStatusCode);
    return $"❌ データ取得エラー: {ex.Message}";
}
```

### LLM エラー

```csharp
try
{
    var response = await _agent.RunAsync(query);
    return response;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "LLMリクエストエラー");
    return "❌ LLMサービスに接続できません。ネットワークを確認してください。";
}
catch (TaskCanceledException ex)
{
    _logger.LogWarning(ex, "LLMリクエストタイムアウト");
    return "❌ リクエストがタイムアウトしました。もう一度お試しください。";
}
catch (Exception ex)
{
    _logger.LogError(ex, "LLM推論エラー");
    return "❌ AI処理中にエラーが発生しました。";
}
```

### グローバルエラーハンドラー

**Program.cs**:

```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = 
            context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "未処理の例外: {Path}", context.Request.Path);

        await context.Response.WriteAsJsonAsync(new
        {
            Error = "Internal Server Error",
            Message = exception?.Message ?? "予期しないエラーが発生しました",
            Path = context.Request.Path.ToString()
        });
    });
});
```

## リトライパターン

### 指数バックオフ

```csharp
public async Task<string> SearchEmailsWithRetry(string query)
{
    const int maxRetries = 3;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await _graphClient.Users[_userId].Messages.GetAsync(/* config */);
        }
        catch (ServiceException ex) when (ex.ResponseStatusCode == 429 && attempt < maxRetries)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            _logger.LogWarning("レート制限（試行 {Attempt}/{Max}）: {Delay}秒待機", 
                attempt, maxRetries, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
    
    throw new InvalidOperationException("最大リトライ回数に達しました");
}
```

## エラーメッセージの多言語化

**Resources/LocalizedStrings.cs**:

```csharp
public class LocalizedStrings
{
    private Dictionary<string, string> Strings { get; set; }

    public string M365NotConfigured => 
        GetString("M365NotConfigured", "Microsoft 365が設定されていません");
    
    public string AuthenticationError =>
        GetString("AuthenticationError", "認証エラーが発生しました");
}
```

## 次のステップ

- **[08-LOGGING-TELEMETRY.md](08-LOGGING-TELEMETRY.md)**: ロギング詳細
- **[11-SECURITY-BEST-PRACTICES.md](11-SECURITY-BEST-PRACTICES.md)**: セキュリティ
