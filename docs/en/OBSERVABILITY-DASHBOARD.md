# Observability Dashboard Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../OBSERVABILITY-DASHBOARD.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](OBSERVABILITY-DASHBOARD.md)

**Real-time Agent Behavior Visualization** - Monitor agent internals, conversation flow, and performance through a dashboard

---

## 📋 Overview

The Observability Dashboard is a web-based monitoring tool that visualizes the Sales Support Agent's behavior in real-time. Using real-time communication via SignalR, you can instantly check agent state, conversation timeline, AI inference process, and performance metrics.

### 💡 Key Features

| Feature | Description |
|---------|-------------|
| 🔴 **Real-time Monitoring** | Instantly reflect agent behavior via SignalR |
| 📊 **Agent State Display** | Active/idle state, last activity time |
| 💬 **Conversation Timeline** | Chronological display of user-agent interactions |
| 🔍 **Detailed Phase Display** | Visualize internal steps of AI inference |
| 📈 **Metrics Display** | Response time, API call count, success rate |
| 🎨 **Fluent UI Integration** | Modern UI conforming to Microsoft Design System |

### 🎯 Business Value

- **Efficient Troubleshooting**: Instantly verify agent behavior and quickly identify issues
- **Performance Optimization**: Visualize bottlenecks and discover improvement points
- **Ensure Transparency**: Visualize AI reasoning process for accountability
- **Development Efficiency**: Reduce debug time and accelerate development cycles

---

## 🚀 Quick Start

### Access Method

```bash
# Start the application
cd /path/to/SalesSupportAgent
dotnet run

# Access in browser
open https://localhost:5192/observability.html
```

**URL**: `https://localhost:5192/observability.html`

### First Access

1. Access the above URL in your browser
2. If a self-signed certificate warning appears, click "Advanced" → "Continue"
3. The dashboard will appear and SignalR connection will be automatically established
4. Connection status is displayed in the upper right corner (green: connected, red: disconnected)

---

## 🏗️ Architecture

### System Configuration

```
┌─────────────────────────────────────────────┐
│         Browser (observability.html)        │
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
│      Sales Support Agent (.NET 10)          │
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
│ 🤖 Agent State                          │
├─────────────────────────────────────────┤
│ State: ● Active                         │
│ Last Activity: 2026-02-08 14:30:25      │
│ Total Conversations: 15                 │
│ Average Response Time: 2.3s             │
└─────────────────────────────────────────┘
```

**Display Items**:
- **State**: Active (green) / Idle (gray) / Error (red)
- **Last Activity**: Time of last agent activity
- **Total Conversations**: Total conversations since startup
- **Average Response Time**: Average response time across all conversations

### 2. Conversation Timeline

```
┌─────────────────────────────────────────┐
│ 💬 Conversation Timeline                │
├─────────────────────────────────────────┤
│ [14:30] User                            │
│ └ Tell me about this week's sales       │
│                                          │
│ [14:30] Agent (processing...)            │
│ ├ [Phase 1] Email search started         │
│ ├ [Phase 2] Calendar search started      │
│ ├ [Phase 3] SharePoint search started    │
│ ├ [Phase 4] AI integrated report gen     │
│ └ [Complete] Adaptive Card sent          │
│                                          │
│ [14:31] Agent                            │
│ └ [Sales summary displayed]              │
└─────────────────────────────────────────┘
```

**Display Content**:
- User messages (blue background)
- Agent responses (green background)
- Processing phases (expandable)
- Timestamps (hour:minute:second)
- Error messages (red background)

### 3. Detailed Phase Display

Click "Show Details" button for each conversation to expand:

```
┌─────────────────────────────────────────┐
│ 🔍 Phase Details: This week's sales     │
├─────────────────────────────────────────┤
│ Phase 1: Email Search                    │
│ ├ Start: 14:30:25.123                    │
│ ├ End: 14:30:26.456                      │
│ ├ Duration: 1.33s                        │
│ ├ Status: ✅ Success                     │
│ └ Result: Retrieved 15 emails            │
│                                          │
│ Phase 2: Calendar Search                 │
│ ├ Start: 14:30:26.500                    │
│ ├ End: 14:30:27.200                      │
│ ├ Duration: 0.70s                        │
│ ├ Status: ✅ Success                     │
│ └ Result: Retrieved 8 events             │
│                                          │
│ Phase 3: SharePoint Search               │
│ ├ Start: 14:30:27.250                    │
│ ├ End: 14:30:28.100                      │
│ ├ Duration: 0.85s                        │
│ ├ Status: ✅ Success                     │
│ └ Result: Retrieved 12 documents         │
│                                          │
│ Phase 4: AI Inference                    │
│ ├ Start: 14:30:28.150                    │
│ ├ End: 14:30:30.500                      │
│ ├ Duration: 2.35s                        │
│ ├ Status: ✅ Success                     │
│ ├ LLM: Azure OpenAI (gpt-4o)            │
│ ├ Token Usage: 1,250 (input) + 450 (out) │
│ └ Result: Integrated report generated    │
└─────────────────────────────────────────┘
```

### 4. Metrics Panel

```
┌─────────────────────────────────────────┐
│ 📈 Performance Metrics                   │
├─────────────────────────────────────────┤
│ API Call Statistics (past 1 hour)        │
│ ├ Graph API: 45 calls (success: 44, fail: 1) │
│ ├ LLM API: 15 calls (success: 15, fail: 0)  │
│ └ Average Response Time: 1.2s            │
│                                          │
│ Token Usage Statistics                   │
│ ├ Total Tokens: 18,750                   │
│ ├ Input Tokens: 12,500 (avg: 833/conv)   │
│ └ Output Tokens: 6,250 (avg: 417/conv)   │
│                                          │
│ Error Rate                               │
│ ├ Overall: 2.2% (1/45)                   │
│ ├ Auth Errors: 0                         │
│ └ Timeouts: 1                            │
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

connection.on("PhaseUpdated", (phase) => {
    console.log("Phase:", phase);
    updatePhaseDetails(phase);
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Cleanup processing
        await base.OnDisconnectedAsync(exception);
    }
}

// Event sending from agent
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

    public async Task NotifyConversation(Conversation conversation)
    {
        await _hubContext.Clients.All.SendAsync(
            "ConversationUpdated", 
            conversation
        );
    }

    public async Task NotifyPhase(PhaseInfo phase)
    {
        await _hubContext.Clients.All.SendAsync(
            "PhaseUpdated", 
            phase
        );
    }
}
```

---

## 🎨 UI Implementation Details

### Fluent UI System Icons Integration

```html
<!-- Fluent UI System Icons CDN -->
<link rel="stylesheet" 
      href="https://cdn.jsdelivr.net/npm/@fluentui/svg-icons/icons/index.css">

<!-- Icon usage example -->
<span class="fluent-icon">
    <svg class="fluent-icon-calendar">
        <use href="#fluent-calendar-24-regular"></use>
    </svg>
</span>
```

**Icons Used**:
- `fluent-bot-24-regular`: Agent icon
- `fluent-calendar-24-regular`: Calendar
- `fluent-mail-24-regular`: Email
- `fluent-folder-24-regular`: SharePoint
- `fluent-people-team-24-regular`: Teams
- `fluent-checkmark-circle-24-regular`: Success
- `fluent-error-circle-24-regular`: Error
- `fluent-spinner-24-regular`: Processing

### Vue 3 Implementation

```javascript
const { createApp } = Vue;

createApp({
    data() {
        return {
            agentStatus: {
                state: 'idle',
                lastActivity: null,
                totalConversations: 0,
                averageResponseTime: 0
            },
            conversations: [],
            selectedConversation: null,
            connection: null,
            isConnected: false
        };
    },
    
    async mounted() {
        await this.initializeSignalR();
    },
    
    methods: {
        async initializeSignalR() {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/observability")
                .withAutomaticReconnect()
                .build();
            
            this.connection.on("AgentStatusUpdated", this.handleStatusUpdate);
            this.connection.on("ConversationUpdated", this.handleConversationUpdate);
            this.connection.on("PhaseUpdated", this.handlePhaseUpdate);
            
            await this.connection.start();
            this.isConnected = true;
        },
        
        handleStatusUpdate(status) {
            this.agentStatus = status;
        },
        
        handleConversationUpdate(conversation) {
            this.conversations.unshift(conversation);
            if (this.conversations.length > 50) {
                this.conversations.pop();
            }
        },
        
        handlePhaseUpdate(phase) {
            const conversation = this.conversations.find(
                c => c.id === phase.conversationId
            );
            if (conversation) {
                if (!conversation.phases) {
                    conversation.phases = [];
                }
                conversation.phases.push(phase);
            }
        },
        
        selectConversation(conversation) {
            this.selectedConversation = conversation;
        },
        
        formatTimestamp(timestamp) {
            return new Date(timestamp).toLocaleTimeString();
        },
        
        formatDuration(milliseconds) {
            return (milliseconds / 1000).toFixed(2) + 's';
        }
    }
}).mount('#app');
```

---

## 📈 Metrics Collection

### OpenTelemetry Integration

```csharp
// AgentMetrics.cs
public class AgentMetrics
{
    private static readonly ActivitySource ActivitySource = 
        new("SalesSupportAgent");
    
    private static readonly Meter Meter = 
        new("SalesSupportAgent");
    
    private readonly Counter<long> _conversationCounter;
    private readonly Histogram<double> _responseTimeHistogram;
    private readonly Counter<long> _errorCounter;
    
    public AgentMetrics()
    {
        _conversationCounter = Meter.CreateCounter<long>(
            "agent.conversations.total",
            description: "Total number of conversations"
        );
        
        _responseTimeHistogram = Meter.CreateHistogram<double>(
            "agent.response.time",
            description: "Agent response time in seconds"
        );
        
        _errorCounter = Meter.CreateCounter<long>(
            "agent.errors.total",
            description: "Total number of errors"
        );
    }
    
    public void RecordConversation(string result)
    {
        _conversationCounter.Add(1, 
            new KeyValuePair<string, object?>("result", result));
    }
    
    public void RecordResponseTime(double seconds, string phase)
    {
        _responseTimeHistogram.Record(seconds,
            new KeyValuePair<string, object?>("phase", phase));
    }
    
    public void RecordError(string errorType)
    {
        _errorCounter.Add(1,
            new KeyValuePair<string, object?>("type", errorType));
    }
}
```

### Metrics Sending

```csharp
// SalesAgent.cs
public async Task<string> ProcessQueryAsync(string query)
{
    var stopwatch = Stopwatch.StartNew();
    
    using var activity = ActivitySource.StartActivity("ProcessQuery");
    activity?.SetTag("query.length", query.Length);
    
    try
    {
        // Phase 1: Email search
        var emailStopwatch = Stopwatch.StartNew();
        var emails = await SearchEmailsAsync(query);
        emailStopwatch.Stop();
        
        _metrics.RecordResponseTime(
            emailStopwatch.Elapsed.TotalSeconds, 
            "email_search"
        );
        
        await _observabilityService.NotifyPhase(new PhaseInfo
        {
            Name = "Email Search",
            Duration = emailStopwatch.Elapsed,
            Status = "success",
            ResultCount = emails.Count
        });
        
        // Phase 2: Calendar search
        // ... (same pattern)
        
        // Phase 3: AI inference
        var aiStopwatch = Stopwatch.StartNew();
        var response = await GenerateResponseAsync(emails, events, docs);
        aiStopwatch.Stop();
        
        _metrics.RecordResponseTime(
            aiStopwatch.Elapsed.TotalSeconds,
            "ai_inference"
        );
        
        stopwatch.Stop();
        _metrics.RecordConversation("success");
        
        activity?.SetTag("total.duration", stopwatch.Elapsed.TotalSeconds);
        
        return response;
    }
    catch (Exception ex)
    {
        _metrics.RecordError(ex.GetType().Name);
        _metrics.RecordConversation("error");
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

---

## 🔍 Debugging Features

### Browser Developer Tools

**Console Output Example**:
```javascript
[SignalR] Connected to /hubs/observability
[Agent] Status Updated: { state: 'active', ... }
[Conversation] New message: { id: '123', user: '...' }
[Phase] Email search completed in 1.33s
[Phase] Calendar search completed in 0.70s
[Phase] SharePoint search completed in 0.85s
[Phase] AI inference completed in 2.35s
```

### Network Monitoring

Verify SignalR connection:
1. Developer Tools → Network tab
2. Enable WebSocket filter
3. Verify `observability` connection
4. Check message frames

### Performance Profiling

```javascript
// Performance measurement
performance.mark('conversation-start');

// ... processing ...

performance.mark('conversation-end');
performance.measure(
    'conversation-duration',
    'conversation-start',
    'conversation-end'
);

const measure = performance.getEntriesByName('conversation-duration')[0];
console.log(`Duration: ${measure.duration}ms`);
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
    "EnableDetailedPhases": true,
    "EnableTokenCounting": true
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30",
    "HandshakeTimeout": "00:00:15"
  }
}
```

### Program.cs

```csharp
// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Register ObservabilityHub
app.MapHub<ObservabilityHub>("/hubs/observability");

// Static file serving (observability.html)
app.UseStaticFiles();
```

---

## 🛡️ Security

### Authentication & Authorization

```csharp
// Add authentication in production
[Authorize]
public class ObservabilityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            Context.Abort();
            return;
        }
        
        // Admin role check
        if (!Context.User.IsInRole("Admin"))
        {
            Context.Abort();
            return;
        }
        
        await base.OnConnectedAsync();
    }
}
```

### CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ObservabilityPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:5192")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

app.UseCors("ObservabilityPolicy");
```

### Data Filtering

```csharp
// Mask PII (Personally Identifiable Information)
public class PIIFilter
{
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        
        var parts = email.Split('@');
        if (parts.Length != 2) return "***@***";
        
        return $"{parts[0][0]}***@{parts[1]}";
    }
    
    public static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        
        return new string('*', phone.Length - 4) + phone.Substring(phone.Length - 4);
    }
}

// Usage example
await _observabilityService.NotifyPhase(new PhaseInfo
{
    Name = "Email Search",
    ResultSummary = $"Found {emails.Count} emails from {PIIFilter.MaskEmail(sender)}"
});
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

connection.onreconnected((connectionId) => {
    console.log('SignalR reconnected:', connectionId);
});
```

### Events Not Received

**Symptom**: Dashboard not updating

**Solutions**:
1. Verify SignalR connection status (green indicator)
2. Check browser console for error logs
3. Check server-side logs (`dotnet run` output)
4. Verify event handlers are properly registered

```javascript
// Debug event handlers
connection.on("AgentStatusUpdated", (status) => {
    console.log("✅ AgentStatusUpdated received:", status);
});
```

### Slow Performance

**Symptom**: Dashboard updates are delayed

**Causes and Solutions**:

| Cause | Solution |
|-------|----------|
| Excessive log output | Set `EnableDetailedPhases: false` |
| Too many in-memory conversations | Reduce `MaxConversationsInMemory` to 50 |
| Network latency | Extend KeepAliveInterval to 30 seconds |

```json
{
  "Observability": {
    "MaxConversationsInMemory": 50,
    "EnableDetailedPhases": false
  },
  "SignalR": {
    "KeepAliveInterval": "00:00:30"
  }
}
```

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

Fully visualize your Sales Support Agent's behavior with the **Observability Dashboard**! 📊
