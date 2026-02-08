# Observability Dashboard Guide

> **Language**: [🇯🇵 日本語](../OBSERVABILITY-DASHBOARD.md) | 🇬🇧 English

**Visualize Agent Operations in Real-time** - Monitor agent internals, conversation flows, and performance with a dashboard

---

## 📋 Overview

The Observability Dashboard is a web-based monitoring tool that visualizes the Sales Support Agent's operations in real-time. Using SignalR for real-time communication, it enables instant verification of agent state, conversation timelines, AI inference processes, and performance metrics.

### 💡 Key Features

| Feature | Description |
|---------|-------------|
| 🔴 **Real-time Monitoring** | Instant reflection of agent operations via SignalR |
| 📊 **Agent State Display** | Active/Idle status, last activity time |
| 💬 **Conversation Timeline** | Time-series display of user-agent interactions |
| 🔍 **Detailed Phase Display** | Visualize internal AI inference steps |
| 📈 **Metrics Display** | Response time, API call count, success rate |
| 🎨 **Fluent UI Integration** | Modern UI compliant with Microsoft design system |

### 🎯 Business Value

- **Efficient Troubleshooting**: Instantly verify agent operations and quickly identify issues
- **Performance Optimization**: Visualize bottlenecks and discover improvement points
- **Transparency**: Visualize AI inference process for accountability
- **Development Efficiency**: Reduce debugging time and accelerate development cycles

---

## 🚀 Quick Start

### Access Method

```bash
# Start application
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run

# Access in browser
open https://localhost:5192/observability.html
```

**URL**: `https://localhost:5192/observability.html`

### On First Access

1. Access the above URL in browser
2. If self-signed certificate warning appears, click "Advanced" → "Proceed"
3. Dashboard displays and SignalR connection establishes automatically
4. Connection status displays in upper right (green: connected, red: disconnected)

---

## 🏗️ Architecture

### System Configuration

```
┌─────────────────────────────────────────────┐
│      Browser (observability.html)           │
│  ┌──────────────────────────────────────┐   │
│  │  Vue 3 + Fluent UI System Icons      │   │
│  │  - Real-time UI updates              │   │
│  │  - Conversation timeline display     │   │
│  │  - Metrics visualization             │   │
│  └──────────────┬───────────────────────┘   │
└─────────────────┼───────────────────────────┘
                  │ SignalR (WebSocket)
                  ▼
┌─────────────────────────────────────────────┐
│   Sales Support Agent (.NET 10)             │
│  ┌──────────────────────────────────────┐   │
│  │  ObservabilityHub (SignalR Hub)      │   │
│  │  - Real-time event delivery          │   │
│  │  - Connection management             │   │
│  └──────────────┬───────────────────────┘   │
│                 │                            │
│  ┌──────────────▼───────────────────────┐   │
│  │  Agent Telemetry                     │   │
│  │  - AgentMetrics (OpenTelemetry)     │   │
│  │  - ActivitySource, Meter, Counter   │   │
│  │  - Transcript Logging               │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

### SignalR Hub

**Endpoint**: `/hubs/observability`

```csharp
public class ObservabilityHub : Hub
{
    // Send events to clients
    await Clients.All.SendAsync("AgentStatusUpdated", status);
    await Clients.All.SendAsync("ConversationUpdated", conversation);
    await Clients.All.SendAsync("PhaseUpdated", phase);
}
```

---

## 📊 Dashboard UI

### 1. Agent State Panel

```
┌─────────────────────────────────────────┐
│ 🤖 Agent Status                          │
├─────────────────────────────────────────┤
│ State: ● Active                          │
│ Last Activity: 2026-02-08 14:30:25      │
│ Total Conversations: 15                  │
│ Avg Response Time: 2.3s                 │
└─────────────────────────────────────────┘
```

**Display Items**:
- **State**: Active (green) / Idle (gray) / Error (red)
- **Last Activity**: Last time agent operated
- **Total Conversations**: Total conversation count since startup
- **Avg Response Time**: Average response time across all conversations

### 2. Conversation Timeline

```
┌─────────────────────────────────────────┐
│ 💬 Conversation Timeline                 │
├─────────────────────────────────────────┤
│ [14:30] User                             │
│ └ Show this week's sales summary        │
│                                          │
│ [14:30] Agent (Processing...)            │
│ ├ [Phase 1] Email search started        │
│ ├ [Phase 2] Calendar search started     │
│ ├ [Phase 3] SharePoint search started   │
│ ├ [Phase 4] AI integrated report gen    │
│ └ [Complete] Adaptive Card sent         │
│                                          │
│ [14:31] Agent                            │
│ └ [Sales summary displayed]             │
└─────────────────────────────────────────┘
```

**Display Content**:
- User messages (blue background)
- Agent responses (green background)
- Processing phases (expandable)
- Timestamps (HH:mm:ss)
- Error messages (red background)

### 3. Detailed Phase Display

Click "Show Details" button for each conversation to expand:

```
┌─────────────────────────────────────────┐
│ 🔍 Phase Details: Sales Summary         │
├─────────────────────────────────────────┤
│ Phase 1: Email Search                   │
│ ├ Start: 14:30:25.123                   │
│ ├ End: 14:30:26.456                     │
│ ├ Duration: 1.33s                        │
│ ├ Status: ✅ Success                     │
│ └ Result: Retrieved 15 emails           │
│                                          │
│ Phase 2: Calendar Search                │
│ ├ Start: 14:30:26.500                   │
│ ├ End: 14:30:27.200                     │
│ ├ Duration: 0.70s                        │
│ ├ Status: ✅ Success                     │
│ └ Result: Retrieved 8 events            │
└─────────────────────────────────────────┘
```

### 4. Metrics Panel

```
┌─────────────────────────────────────────┐
│ 📈 Performance Metrics                   │
├─────────────────────────────────────────┤
│ API Call Statistics (Past 1 hour)       │
│ ├ Graph API: 45 (Success: 44, Fail: 1) │
│ ├ LLM API: 15 (Success: 15, Fail: 0)   │
│ └ Avg Response Time: 1.2s               │
│                                          │
│ Token Usage Statistics                   │
│ ├ Total Tokens: 18,750                  │
│ ├ Input Tokens: 12,500 (avg: 833/conv) │
│ └ Output Tokens: 6,250 (avg: 417/conv) │
└─────────────────────────────────────────┘
```

---

## 🔧 SignalR Integration

### Client Side (JavaScript)

```javascript
// Establish SignalR connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/observability")
    .withAutomaticReconnect()
    .build();

// Register event handlers
connection.on("AgentStatusUpdated", (status) => {
    console.log("Agent Status:", status);
    updateAgentStatus(status);
});

connection.on("ConversationUpdated", (conversation) => {
    console.log("Conversation:", conversation);
    addConversationToTimeline(conversation);
});

// Start connection
await connection.start();
console.log("SignalR Connected");
```

### Server Side (C#)

```csharp
// ObservabilityHub.cs
public class ObservabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", 
            new { Message = "Dashboard connected" });
        await base.OnConnectedAsync();
    }
}

// Send events from agent
public class AgentObservabilityService
{
    private readonly IHubContext<ObservabilityHub> _hubContext;

    public async Task NotifyAgentStatus(AgentStatus status)
    {
        await _hubContext.Clients.All.SendAsync(
            "AgentStatusUpdated", 
            status
        );
    }
}
```

---

## ⚙️ Configuration

### appsettings.json

```json
{
  "Observability": {
    "Enabled": true,
    "SignalRHubPath": "/hubs/observability",
    "MaxConversationsInMemory": 100,
    "MetricsRetentionMinutes": 60,
    "EnableDetailedPhases": true
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30"
  }
}
```

---

## ⚠️ Troubleshooting

### Cannot Connect to SignalR

**Symptom**: Dashboard shows "Disconnected"

**Causes and Solutions**:

| Cause | Solution |
|-------|----------|
| Application not running | Start with `dotnet run` |
| Incorrect path | Verify `/hubs/observability` is correct |
| CORS configuration error | Check CORS settings in Program.cs |
| Firewall | Verify port 5192 is open |

**Debug Steps**:
```javascript
// Check errors in browser console
connection.onclose((error) => {
    console.error('SignalR connection closed:', error);
});

connection.onreconnecting((error) => {
    console.warn('SignalR reconnecting:', error);
});
```

### Events Not Received

**Symptom**: Dashboard not updating

**Solutions**:
1. Verify SignalR connection status (green light)
2. Check browser console for error logs
3. Check server logs (`dotnet run` output)
4. Verify event handlers are registered correctly

---

## 📚 Related Documentation

- [Troubleshooting Guide](TROUBLESHOOTING.md) - Common issues and solutions
- [Architecture Document](ARCHITECTURE.md) - System design details
- [Agent Development Guide](AGENT-DEVELOPMENT.md) - Agent implementation patterns

---

## 🔗 External Links

- [SignalR Documentation](https://learn.microsoft.com/aspnet/core/signalr/introduction)
- [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons)
- [Vue 3 Documentation](https://vuejs.org/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

**Fully visualize Sales Support Agent operations with Observability Dashboard!** 📊
