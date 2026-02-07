using Microsoft.AspNetCore.SignalR;
using SalesSupportAgent.Hubs;
using System.Collections.Concurrent;

namespace SalesSupportAgent.Services.Notifications;

/// <summary>
/// リアルタイム通知サービス（Agent 365 Notifications統合）
/// </summary>
public class NotificationService
{
    private readonly IHubContext<ObservabilityHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;
    private readonly ConcurrentQueue<NotificationEvent> _notificationHistory;
    private const int MaxHistorySize = 50;

    public NotificationService(
        IHubContext<ObservabilityHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationHistory = new ConcurrentQueue<NotificationEvent>();
    }

    /// <summary>
    /// 進捗通知を送信
    /// </summary>
    public async Task SendProgressNotificationAsync(string operationId, string message, int progressPercentage)
    {
        var notification = new NotificationEvent
        {
            Id = Guid.NewGuid().ToString(),
            OperationId = operationId,
            Type = "progress",
            Message = message,
            ProgressPercentage = progressPercentage,
            Timestamp = DateTime.UtcNow,
            Severity = "info"
        };

        await EnqueueAndBroadcastAsync(notification);
        _logger.LogInformation("進捗通知送信: {OperationId} - {Message} ({Progress}%)", 
            operationId, message, progressPercentage);
    }

    /// <summary>
    /// 成功通知を送信
    /// </summary>
    public async Task SendSuccessNotificationAsync(string operationId, string message, object? data = null)
    {
        var notification = new NotificationEvent
        {
            Id = Guid.NewGuid().ToString(),
            OperationId = operationId,
            Type = "success",
            Message = message,
            ProgressPercentage = 100,
            Timestamp = DateTime.UtcNow,
            Severity = "success",
            Data = data
        };

        await EnqueueAndBroadcastAsync(notification);
        _logger.LogInformation("成功通知送信: {OperationId} - {Message}", operationId, message);
    }

    /// <summary>
    /// エラー通知を送信
    /// </summary>
    public async Task SendErrorNotificationAsync(string operationId, string message, string? errorDetails = null)
    {
        var notification = new NotificationEvent
        {
            Id = Guid.NewGuid().ToString(),
            OperationId = operationId,
            Type = "error",
            Message = message,
            ProgressPercentage = 0,
            Timestamp = DateTime.UtcNow,
            Severity = "error",
            ErrorDetails = errorDetails
        };

        await EnqueueAndBroadcastAsync(notification);
        _logger.LogError("エラー通知送信: {OperationId} - {Message}", operationId, message);
    }

    /// <summary>
    /// 警告通知を送信
    /// </summary>
    public async Task SendWarningNotificationAsync(string operationId, string message)
    {
        var notification = new NotificationEvent
        {
            Id = Guid.NewGuid().ToString(),
            OperationId = operationId,
            Type = "warning",
            Message = message,
            Timestamp = DateTime.UtcNow,
            Severity = "warning"
        };

        await EnqueueAndBroadcastAsync(notification);
        _logger.LogWarning("警告通知送信: {OperationId} - {Message}", operationId, message);
    }

    /// <summary>
    /// 情報通知を送信
    /// </summary>
    public async Task SendInfoNotificationAsync(string operationId, string message)
    {
        var notification = new NotificationEvent
        {
            Id = Guid.NewGuid().ToString(),
            OperationId = operationId,
            Type = "info",
            Message = message,
            Timestamp = DateTime.UtcNow,
            Severity = "info"
        };

        await EnqueueAndBroadcastAsync(notification);
        _logger.LogInformation("情報通知送信: {OperationId} - {Message}", operationId, message);
    }

    /// <summary>
    /// 通知履歴取得
    /// </summary>
    public List<NotificationEvent> GetNotificationHistory(int count = 20)
    {
        return _notificationHistory
            .TakeLast(Math.Min(count, MaxHistorySize))
            .OrderByDescending(n => n.Timestamp)
            .ToList();
    }

    /// <summary>
    /// 特定操作の通知履歴取得
    /// </summary>
    public List<NotificationEvent> GetNotificationsByOperation(string operationId)
    {
        return _notificationHistory
            .Where(n => n.OperationId == operationId)
            .OrderBy(n => n.Timestamp)
            .ToList();
    }

    /// <summary>
    /// 通知をキューに追加してSignalRでブロードキャスト
    /// </summary>
    private async Task EnqueueAndBroadcastAsync(NotificationEvent notification)
    {
        // 履歴に追加
        _notificationHistory.Enqueue(notification);

        // 最大サイズ制限
        while (_notificationHistory.Count > MaxHistorySize)
        {
            _notificationHistory.TryDequeue(out _);
        }

        // SignalRでブロードキャスト
        try
        {
            await _hubContext.Clients.All.SendAsync("NotificationUpdate", notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR通知送信エラー: {NotificationId}", notification.Id);
        }
    }
}

/// <summary>
/// 通知イベントモデル
/// </summary>
public class NotificationEvent
{
    /// <summary>通知ID</summary>
    public required string Id { get; set; }

    /// <summary>操作ID（トレース用）</summary>
    public required string OperationId { get; set; }

    /// <summary>通知タイプ（progress, success, error, warning, info）</summary>
    public required string Type { get; set; }

    /// <summary>メッセージ</summary>
    public required string Message { get; set; }

    /// <summary>進捗率（0-100）</summary>
    public int ProgressPercentage { get; set; }

    /// <summary>タイムスタンプ</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>重要度（info, success, warning, error）</summary>
    public required string Severity { get; set; }

    /// <summary>追加データ</summary>
    public object? Data { get; set; }

    /// <summary>エラー詳細</summary>
    public string? ErrorDetails { get; set; }
}
