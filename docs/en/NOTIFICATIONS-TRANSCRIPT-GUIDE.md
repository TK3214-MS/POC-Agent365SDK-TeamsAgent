# 🔔 Notifications & 💬 Transcript Features Guide

> **Language**: [🇯🇵 日本語](../NOTIFICATIONS-TRANSCRIPT-GUIDE.md) | 🇬🇧 English

## 📋 Overview

Extended demo integrating Agent 365 SDK's **Notifications** and **Transcript & Storage** features.

### New Features

1. **🔔 Real-time Notifications**
   - Real-time delivery of agent processing progress
   - Visualize progress stages: 0%, 25%, 75%, 100%
   - Automatic sending of success/error/warning notifications

2. **💬 Conversation History (Transcript & Storage)**
   - Automatic recording of all user and Bot messages
   - History management and search by conversation
   - Statistics (total conversations, total messages, active conversations)

---

## 🚀 Quick Start

### 1. Start Application

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent
dotnet run
```

### 2. Access Observability Dashboard

```
http://localhost:5192/observability.html
```

**New Panels:**
- 🔔 **Real-time Notifications** - Notifications with progress bars
- 💬 **Conversation History** - Conversation list and message details

---

## 🎬 Demo Scenarios

### Scenario 1: Real-time Notification Experience

**Purpose**: Experience instant notification delivery via SignalR

**Steps**:

1. **Prepare Dashboard**
   - Open `http://localhost:5192/observability.html`
   - Verify notification panel shows "Waiting for notification data..."

2. **Operate Bot**
   - Open Web Chat (`http://localhost:5192`)
   - Enter "Show this week's sales status"

3. **Verify Notifications**
   - Verify in Dashboard "🔔 Real-time Notifications" panel real-time:
     ```
     🚀 Starting sales summary generation...        [0%]
     📊 Collecting data (emails, calendar, documents)... [25%]
     🤖 AI analyzing (summary generation processing)...  [75%]
     ✅ Sales summary generation complete! (Processing time: 6500ms) [100%]
     ```

4. **Progress Bar**
   - Blue progress bar displayed for each notification
   - Verify 0% → 25% → 75% → 100% animation

**Focal Points**:
- ✨ **Zero Delay**: Dashboard receives instant notifications even while Bot processes
- ✨ **Staged Progress**: Users understand agent processing status
- ✨ **Color-coded Status**: success (green), error (red), warning (yellow)

---

### Scenario 2: Conversation History Management

**Purpose**: Experience conversation recording and history search features

**Steps**:

1. **Execute Conversations**
   - Send 3 messages in Web Chat:
     - "Show this week's sales status"
     - "What's my next appointment?"
     - "What's the latest email?"

2. **Verify Conversation List**
   - Check Dashboard "💬 Conversation History" panel
   - Display conversation ID, message count, last activity time

3. **Display Conversation Details**
   - Click conversation
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
- ✨ **Complete Recording**: Save all user-Bot interactions
- ✨ **Searchable**: Instantly search past history by conversation ID
- ✨ **Privacy Support**: Conversation deletion API (GDPR compliant)

---

### Scenario 3: Error Notifications

**Purpose**: Automatic notification and alert features during errors

**Steps**:

1. **Create Error Condition**
   - Temporarily stop Ollama:
     ```bash
     pkill ollama
     ```

2. **Error Request**
   - In Web Chat, enter "Show this week's sales status"

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
   - Verify success notification with normal request

**Focal Points**:
- ✨ **Instant Error Notification**: Red alert when issues occur
- ✨ **Detailed Error Information**: Display error message and details
- ✨ **Automatic Recovery Tracking**: Verify status with success notification after recovery

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
    "text": "Show this week's sales status",
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
- `GetNotificationHistory()` - Get notification history (retain latest 50)

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
- `GetAllConversations()` - Get all conversations list
- `GetStatistics()` - Get statistics
- `DeleteConversationHistoryAsync()` - Delete conversation (GDPR compliant)

**Storage Integration**:
```csharp
// Use Microsoft.Agents.Storage.IStorage
var storageKey = $"transcript:{conversationId}:{entry.Id}";
await _storage.WriteAsync(storeItems);
```

**Cache Strategy**:
- Memory cache with ConcurrentDictionary<string, List<TranscriptEntry>>
- Retain maximum 100 conversations in cache

---

## 📚 Related Documentation

- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - Dashboard features
- [Agent Development](AGENT-DEVELOPMENT.md) - Custom implementation

---

**Experience advanced features of Agent 365 SDK!** 🚀
