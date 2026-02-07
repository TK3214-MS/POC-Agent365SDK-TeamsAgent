using Microsoft.AspNetCore.SignalR;

namespace SalesSupportAgent.Hubs;

/// <summary>
/// Agent 365 Observability用SignalR Hub
/// リアルタイムでメトリクス、トレース、ログを配信
/// </summary>
public class ObservabilityHub : Hub
{
    private readonly ILogger<ObservabilityHub> _logger;

    public ObservabilityHub(ILogger<ObservabilityHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("📡 Observabilityクライアント接続: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
        
        // 接続時に現在のステータスを送信
        await Clients.Caller.SendAsync("StatusUpdate", new
        {
            Status = "connected",
            Message = "Agent 365 Observability Platform に接続しました",
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("📡 Observabilityクライアント切断: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// アクティブなエージェント情報を要求
    /// </summary>
    public async Task RequestActiveAgents()
    {
        _logger.LogDebug("アクティブなエージェント情報が要求されました");
        await Clients.Caller.SendAsync("ActiveAgentsUpdate", new
        {
            Agents = new[]
            {
                new
                {
                    Name = "営業支援エージェント",
                    Status = "Active",
                    Uptime = DateTime.UtcNow,
                    LLMProvider = "Ollama qwen2.5:latest"
                }
            }
        });
    }
}
