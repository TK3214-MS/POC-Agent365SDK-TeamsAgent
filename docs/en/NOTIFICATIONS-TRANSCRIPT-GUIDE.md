# 🔔 Notifications & 💬 Transcript Feature Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../NOTIFICATIONS-TRANSCRIPT-GUIDE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](NOTIFICATIONS-TRANSCRIPT-GUIDE.md)

## 📋 Overview

An extended demo integrating the **Notifications** and **Transcript & Storage** features of the Agent 365 SDK.

### New Features

1. **🔔 Real-time Notifications**
   - Real-time delivery of agent processing progress
   - Visualization of progress stages at 0%, 25%, 75%, 100%
   - Automatic sending of success, error, and warning notifications

2. **💬 Conversation History (Transcript & Storage)**
   - Automatic recording of all user and Bot messages
   - Conversation history management and search per conversation
   - Statistics (total conversations, total messages, active conversations)

---

## 🚀 Quick Start

### 1. Start Application

```bash
cd /path/to/SalesSupportAgent
dotnet run
```

### 2. Access Observability Dashboard

```
http://localhost:5192/observability.html
```

**New Panels:**
- 🔔 **Real-time Notifications** - Notifications with progress bar
- 💬 **Conversation History** - Conversation list and message details

---

## 🎬 Demo Scenarios

### Scenario 1: Real-time Notification Experience

**Objective**: Experience instant notification delivery via SignalR

**Steps**:

1. **Prepare Dashboard**
   - Open `http://localhost:5192/observability.html`
   - Verify notification panel shows "Waiting for notification data..."

2. **Bot Operation**
   - Open Web Chat (`http://localhost:5192`)
   - Type "Tell me about this week's sales status"

3. **Verify Notifications**
   - Check the "🔔 Real-time Notifications" panel on Dashboard in real-time:
     ```
     🚀 Starting sales summary generation...          [0%]
     📊 Collecting data (emails, calendar, documents)... [25%]
     🤖 AI analysis in progress (summary generation)... [75%]
     ✅ Sales summary generation complete! (Processing time: 6500ms) [100%]
     ```

4. **Progress Bar**
   - Blue progress bar displayed for each notification
   - Verify animation from 0% → 25% → 75% → 100%

**Focal Points**:
- ✨ **Zero Latency**: Instant notification on Dashboard even while Bot is processing
- ✨ **Staged Progress**: Users can track agent processing status
- ✨ **Color-coded Status**: success (green), error (red), warning (yellow)

---

### Scenario 2: Conversation History Management

**Objective**: Experience conversation recording and history search

**Steps**:

1. **Execute Conversations**
   - Send 3 messages in Web Chat:
     - "Tell me about this week's sales status"
     - "What's the next appointment?"
     - "What's the latest email?"

2. **Verify Conversation List**
   - Check the "💬 Conversation History" panel on Dashboard
   - Conversation ID, message count, and last activity time are displayed

3. **View Conversation Details**
   - Click on a conversation
   - User messages (blue background) and Bot responses (purple background) displayed chronologically

4. **Verify Statistics API**
   ```bash
   curl http://localhost:5192/api/transcript/statistics
   ```
   
   **Expected Response**:
   ```json
   {
     "totalConversations": 1,
     "totalMessages": 6,
     "activeConversations": 1,
     "averageMessagesPerConversation": 6.0
   }
   ```

**Focal Points**:
- ✨ **Complete Recording**: All user-Bot interactions are saved
- ✨ **Searchable**: Instantly search past history by conversation ID
- ✨ **Privacy Compliant**: Conversation deletion API (GDPR compliant)

---

### Scenario 3: Error Notifications

**Objective**: Automatic notification and alert functionality during errors

**Steps**:

1. **Create Error Condition**
   - Temporarily stop Ollama:
     ```bash
     pkill ollama
     ```

2. **Error Request**
   - Type "Tell me about this week's sales status" in Web Chat

3. **Verify Error Notification**
   - Red error notification in Dashboard "🔔 Real-time Notifications" panel:
     ```
     ❌ Sales summary generation failed
     Error: HTTP request failed
     ```

4. **Recovery**
   - Restart Ollama:
     ```bash
     ollama serve
     ```
   - Execute normal request and verify success notification

**Focal Points**:
- ✨ **Instant Error Notification**: Red alert when problems occur
- ✨ **Detailed Error Information**: Display error message and details
- ✨ **Automatic Recovery Tracking**: Confirm state with success notification after recovery

---

## 📊 API Reference

### Notifications API

#### 1. Get Notification History
```bash
GET /api/notifications/history?count=20
```

**Response Example**:
```json
[
  {
    "id": "abc123",
    "operationId": "op-456",
    "type": "success",
    "message": "✅ Sales summary generation complete! (Processing time: 6500ms)",
    "progressPercentage": 100,
    "timestamp": "2026-02-07T10:30:00Z",
    "severity": "success",
    "data": {
      "processingTimeMs": 6500,
      "dataSourceCount": 4
    }
  }
]
```

#### 2. Get Notifications by Operation
```bash
GET /api/notifications/operation/{operationId}
```

---

### Transcript API

#### 1. Get Conversation List
```bash
GET /api/transcript/conversations
```

**Response Example**:
```json
[
  {
    "conversationId": "conv-123-456",
    "messageCount": 6,
    "lastActivity": "2026-02-07T10:35:00Z",
    "participants": ["User", "SalesSupportAgent"]
  }
]
```

#### 2. Get Conversation History
```bash
GET /api/transcript/history/{conversationId}?limit=50
```

**Response Example**:
```json
[
  {
    "id": "msg-1",
    "conversationId": "conv-123-456",
    "type": "message",
    "from": "User",
    "text": "Tell me about this week's sales status",
    "timestamp": "2026-02-07T10:30:00Z",
    "channelId": "webchat"
  },
  {
    "id": "msg-2",
    "conversationId": "conv-123-456",
    "type": "message",
    "from": "SalesSupportAgent",
    "text": "[Adaptive Card Response] ## 📊 Summary...",
    "timestamp": "2026-02-07T10:30:06Z",
    "channelId": "webchat"
  }
]
```

#### 3. Get Statistics
```bash
GET /api/transcript/statistics
```

**Response Example**:
```json
{
  "totalConversations": 5,
  "totalMessages": 24,
  "activeConversations": 2,
  "averageMessagesPerConversation": 4.8
}
```

#### 4. Delete Conversation
```bash
DELETE /api/transcript/history/{conversationId}
```

---

## 🔧 Technical Implementation Details

### NotificationService

**Location**: `Services/Notifications/NotificationService.cs`

**Key Methods**:
- `SendProgressNotificationAsync()` - Progress notification (0-100%)
- `SendSuccessNotificationAsync()` - Success notification
- `SendErrorNotificationAsync()` - Error notification
- `SendWarningNotificationAsync()` - Warning notification
- `GetNotificationHistory()` - Get notification history (retains last 50)

**SignalR Integration**:
```csharp
await _hubContext.Clients.All.SendAsync("NotificationUpdate", notification);
```

---

### TranscriptService

**Location**: `Services/Transcript/TranscriptService.cs`

**Key Methods**:
- `LogActivityAsync()` - Record conversation activity
- `GetConversationHistoryAsync()` - Get conversation history
- `GetAllConversations()` - All conversation list
- `GetStatistics()` - Statistics
- `DeleteConversationHistoryAsync()` - Delete conversation (GDPR compliant)

**Storage Integration**:
```csharp
// Uses Microsoft.Agents.Storage.IStorage
var storageKey = $"transcript:{conversationId}:{entry.Id}";
await _storage.WriteAsync(storeItems);
```

**Cache Strategy**:
- Memory cache with ConcurrentDictionary<string, List<TranscriptEntry>>
- Cache retains up to 100 conversations
- Asynchronous write to storage

---

### SalesAgent Integration

**Changes**:

```csharp
// Add NotificationService to constructor
public SalesAgent(
    ...,
    NotificationService notificationService,
    ...)

// Send notifications within GenerateSalesSummaryAsync
await _notificationService.SendProgressNotificationAsync(operationId, "🚀 Starting sales summary generation...", 0);
await _notificationService.SendProgressNotificationAsync(operationId, "📊 Collecting data...", 25);
await _notificationService.SendProgressNotificationAsync(operationId, "🤖 AI analysis in progress...", 75);
await _notificationService.SendSuccessNotificationAsync(operationId, "✅ Complete!", data);
```

---

### TeamsBot Integration

**Changes**:

```csharp
// Add TranscriptService to constructor
public TeamsBot(
    SalesAgent salesAgent,
    TranscriptService transcriptService,
    ...)

// Record on message send/receive
await _transcriptService.LogActivityAsync(turnContext.Activity, conversationId);
await _transcriptService.LogActivityAsync(botActivity, conversationId);
```

---

## 💡 Best Practices

### 1. Appropriate Use of Notifications

**Recommended**:
- Send progress notifications for long-running processes (5+ seconds)
- Use 4-5 progress steps (0%, 33%, 66%, 100%, etc.)
- Include specific result information on success

**Not Recommended**:
- Excessive notifications for short processes (under 1 second)
- Meaningless progress updates (1% increments, etc.)

### 2. Conversation History Privacy

**GDPR Compliance**:
```csharp
// Delete conversation on user request
await transcriptService.DeleteConversationHistoryAsync(conversationId);
```

**Recommended Settings**:
- Use encrypted storage in production
- PII (Personally Identifiable Information) filtering when personal data is involved
- Set retention period policy (e.g., auto-delete after 30 days)

### 3. Performance Optimization

**Cache Strategy**:
- Memory: Cache latest 100 conversations with ConcurrentDictionary
- Storage: Avoid bottlenecks with asynchronous writes

**SignalR Optimization**:
- Notification frequency limiting (max 10 notifications per second)
- Client-side batch processing

---

## 🎯 Next Steps

### Phase 1 Complete ✅
- ✅ Observability (traces, metrics)
- ✅ Notifications (progress notifications)
- ✅ Transcript & Storage (conversation history)

### Phase 2 (Optional Extensions)

1. **Application Insights Integration** - Enterprise monitoring
2. **Blob Storage Integration** - Persistent storage
3. **PII Filtering** - Automatic personal data masking
4. **Custom Metrics** - Business KPI additions
5. **Alert Rules** - Threshold-based automatic alerts

---

## 📚 References

- [Microsoft Agent 365 SDK - Notifications](https://github.com/microsoft/agent-framework/tree/main/src/packages/Notifications)
- [Microsoft Agent 365 SDK - Storage](https://github.com/microsoft/agent-framework/tree/main/src/packages/Storage)
- [Bot Framework Storage](https://learn.microsoft.com/azure/bot-service/bot-builder-concept-state)
- [SignalR Documentation](https://learn.microsoft.com/aspnet/core/signalr/introduction)

---

**Created**: February 7, 2026  
**Version**: 2.0  
**Target**: Agent 365 SDK Notifications & Transcript Platform Demo
