using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Concurrent;

namespace SalesSupportAgent.Services.Transcript;

/// <summary>
/// 会話履歴管理サービス（Transcript & Storage統合）
/// </summary>
public class TranscriptService
{
    private readonly Microsoft.Agents.Storage.IStorage _storage;
    private readonly ILogger<TranscriptService> _logger;
    private readonly ConcurrentDictionary<string, List<TranscriptEntry>> _conversationCache;
    private const int MaxCacheSize = 100;

    public TranscriptService(
        Microsoft.Agents.Storage.IStorage storage,
        ILogger<TranscriptService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conversationCache = new ConcurrentDictionary<string, List<TranscriptEntry>>();
    }

    /// <summary>
    /// 会話アクティビティを記録
    /// </summary>
    public async Task LogActivityAsync(IActivity activity, string conversationId)
    {
        try
        {
            var messageActivity = activity as Activity;
            
            var entry = new TranscriptEntry
            {
                Id = activity.Id ?? Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Type = activity.Type,
                From = activity.From?.Name ?? activity.From?.Id ?? "Unknown",
                Text = messageActivity?.Text,
                Timestamp = activity.Timestamp ?? DateTimeOffset.UtcNow,
                ChannelId = activity.ChannelId
            };

            // キャッシュに追加
            var entries = _conversationCache.GetOrAdd(conversationId, _ => new List<TranscriptEntry>());
            entries.Add(entry);

            // ストレージに保存
            var storageKey = $"transcript:{conversationId}:{entry.Id}";
            var storeItems = new Dictionary<string, object>
            {
                { storageKey, entry }
            };

            await _storage.WriteAsync(storeItems);

            _logger.LogInformation("会話記録保存: {ConversationId} - {From}: {Text}", 
                conversationId, entry.From, entry.Text?.Substring(0, Math.Min(50, entry.Text?.Length ?? 0)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会話記録保存エラー: {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// 会話履歴取得
    /// </summary>
    public async Task<List<TranscriptEntry>> GetConversationHistoryAsync(string conversationId, int limit = 50)
    {
        try
        {
            // キャッシュから取得
            if (_conversationCache.TryGetValue(conversationId, out var cachedEntries))
            {
                return cachedEntries
                    .OrderByDescending(e => e.Timestamp)
                    .Take(limit)
                    .OrderBy(e => e.Timestamp)
                    .ToList();
            }

            // ストレージから取得（キャッシュにない場合）
            var keys = new[] { $"transcript:{conversationId}:*" };
            var items = await _storage.ReadAsync(keys);

            var entries = items.Values
                .OfType<TranscriptEntry>()
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .OrderBy(e => e.Timestamp)
                .ToList();

            // キャッシュに追加
            _conversationCache.TryAdd(conversationId, entries);

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会話履歴取得エラー: {ConversationId}", conversationId);
            return new List<TranscriptEntry>();
        }
    }

    /// <summary>
    /// すべての会話履歴取得
    /// </summary>
    public List<ConversationSummary> GetAllConversations()
    {
        return _conversationCache
            .Select(kvp => new ConversationSummary
            {
                ConversationId = kvp.Key,
                MessageCount = kvp.Value.Count,
                LastActivity = kvp.Value.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp ?? DateTimeOffset.UtcNow,
                Participants = kvp.Value.Select(e => e.From).Where(f => !string.IsNullOrEmpty(f)).Cast<string>().Distinct().ToList()
            })
            .OrderByDescending(c => c.LastActivity)
            .Take(MaxCacheSize)
            .ToList();
    }

    /// <summary>
    /// 会話履歴削除
    /// </summary>
    public async Task DeleteConversationHistoryAsync(string conversationId)
    {
        try
        {
            // キャッシュから削除
            _conversationCache.TryRemove(conversationId, out _);

            // ストレージから削除
            var keys = new[] { $"transcript:{conversationId}:*" };
            await _storage.DeleteAsync(keys);

            _logger.LogInformation("会話履歴削除: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "会話履歴削除エラー: {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// 会話統計取得
    /// </summary>
    public TranscriptStatistics GetStatistics()
    {
        var totalConversations = _conversationCache.Count;
        var totalMessages = _conversationCache.Sum(kvp => kvp.Value.Count);
        var activeConversations = _conversationCache
            .Count(kvp => kvp.Value.Any(e => e.Timestamp > DateTimeOffset.UtcNow.AddHours(-24)));

        return new TranscriptStatistics
        {
            TotalConversations = totalConversations,
            TotalMessages = totalMessages,
            ActiveConversations = activeConversations,
            AverageMessagesPerConversation = totalConversations > 0 ? (double)totalMessages / totalConversations : 0
        };
    }
}

/// <summary>
/// 会話記録エントリ
/// </summary>
public class TranscriptEntry
{
    public required string Id { get; set; }
    public required string ConversationId { get; set; }
    public string? Type { get; set; }
    public string? From { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? ChannelId { get; set; }
}

/// <summary>
/// 会話サマリー
/// </summary>
public class ConversationSummary
{
    public required string ConversationId { get; set; }
    public int MessageCount { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public List<string> Participants { get; set; } = new();
}

/// <summary>
/// 会話統計
/// </summary>
public class TranscriptStatistics
{
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public int ActiveConversations { get; set; }
    public double AverageMessagesPerConversation { get; set; }
}
