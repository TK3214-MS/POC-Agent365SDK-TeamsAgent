# Sample Data Creation Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../SAMPLE-DATA.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](SAMPLE-DATA.md)

## 📋 Overview

This guide explains how to generate test data in your Microsoft 365 tenant using **the API endpoints implemented in this project** for effective Sales Support Agent demos.

### ✨ Features

- ✅ **Simple**: Auto-generate emails and events with a single API call
- ✅ **Realistic data**: Creates deal data with Japanese company names, contacts, and products
- ✅ **Customizable**: Adjust generation count and time period
- ✅ **Delegated permissions**: Registers as user via Device Code Flow authentication

---

## 🎯 Generated Data

| Data Type | Contents | Count (Default) |
|-----------|----------|----------------|
| 📧 **Deal Emails** | Quote requests, proposal confirmations, contract-related emails | 50 |
| 📅 **Deal Events** | Customer visits, online meetings, internal meetings | 30 |

### Sample Data Contents

#### Deal Email Example

```
Subject: Re: 【見積書送付】株式会社サンプルテック 様向け クラウド基盤サービス
From: noreply@example.com
Category: Deal, Sales

Body:
株式会社サンプルテック 田中太郎様

いつもお世話になっております。

先日ご依頼頂きました「クラウド基盤サービス」の見積書を送付いたします。

Deal amount: ¥3,500,000
Proposed product: Cloud Infrastructure Service
...
```

#### Deal Event Example

```
Subject: 【商談】株式会社デジタルイノベーション - AIソリューション 打ち合わせ
Date/Time: 2026-02-15  14:00-15:00
Location: Online (Teams meeting)
Description: AI Solution proposal & quote explanation
```

---

## ⚙️ Prerequisites

### Required

| Item | Description |
|------|-------------|
| ✅ **Application running** | Start the Sales Support Agent with `dotnet run` |
| ✅ **Microsoft 365 account** | Account for the tenant where data will be created |
| ✅ **Graph API permissions** | `Mail.ReadWrite`, `Calendars.ReadWrite`, `User.Read` |

### Verification

```bash
# Verify the application is running
curl https://localhost:5192/health

# Expected response:
# {"Status":"Healthy", "M365Configured":true, ...}
```

---

## 🚀 Test Data Generation Steps

The Sales Support Agent provides three API endpoints:

### 1️⃣ **Generate All Data at Once (Recommended)**

Generates emails and events simultaneously.

```bash
curl -X POST https://localhost:5192/api/testdata/generate \
  -H "Content-Type: application/json" \
  -d '{
    "emailCount": 50,
    "eventCount": 30
  }'
```

**Authentication flow on first run**:

```
1. Calling the API initiates the Device Code Flow
2. A message like the following appears in the console:

   📱 Device Code Flow authentication required
   Please visit the following URL in your browser:
   https://microsoft.com/devicelogin

   Code: ABCD-EFGH

3. Open the URL in your browser and enter the code
4. Sign in with your Microsoft 365 account
5. Click "Accept" on the permissions consent screen
6. After authentication completes, data generation begins automatically
```

**Success response**:

```json
{
  "Success": true,
  "Message": "Test data generation complete",
  "EmailsCreated": 50,
  "EventsCreated": 30,
  "Period": {
    "StartDate": "2025-12-08",
    "EndDate": "2027-02-08"
  }
}
```

---

### 2️⃣ **Generate Emails Only**

Generates deal emails only.

```bash
curl -X POST https://localhost:5192/api/testdata/generate/emails?count=100
```

**Response example**:

```json
{
  "Success": true,
  "Message": "Generated 100 deal emails",
  "Created": 100
}
```

---

### 3️⃣ **Generate Events Only**

Generates deal events only.

```bash
curl -X POST https://localhost:5192/api/testdata/generate/events?count=50
```

**Response example**:

```json
{
  "Success": true,
  "Message": "Generated 50 deal events",
  "Created": 50
}
```

---

## 🎨 Customization

### Adjusting Generation Count

You can change the generation count via query parameters:

```bash
# Generate 200 emails and 100 events
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

### Adjusting Generation Period

The current code generates data within the following period:

- **Start date**: 2 months ago (past deals)
- **End date**: 1 year ahead (future events)

**Customization method**:

Edit the following in [Services/TestData/TestDataGenerator.cs](../SalesSupportAgent/Services/TestData/TestDataGenerator.cs):

```csharp
// Default
var startDate = DateTime.Now.AddMonths(-2);
var endDate = DateTime.Now.AddYears(1);

// Example: Change to 6 months ago through 6 months ahead
var startDate = DateTime.Now.AddMonths(-6);
var endDate = DateTime.Now.AddMonths(6);
```

---

## 📊 Verifying Generated Data

### Check in Outlook

1. Go to [Outlook Web App](https://outlook.office.com)
2. Open the "Drafts" folder
3. Filter by category "商談" (Deal)

### Check in Calendar

1. Go to [Outlook Calendar](https://outlook.office.com/calendar)
2. Search for "商談" (Deal)
3. Review the event list

---

## 🧪 Demo Scenario Datasets

Recommended datasets for effective demos:

### Scenario 1: Basic Demo (30 minutes)

```bash
# Basic dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=30&eventCount=20"
```

- Emails: 30 (minimal)
- Events: 20 (a few per week)

### Scenario 2: Detailed Demo (1 hour)

```bash
# Rich dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=100&eventCount=60"
```

- Emails: 100 (history with multiple customers)
- Events: 60 (past, present, and future schedules)

### Scenario 3: Full Demo (Workshop)

```bash
# Large-scale dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

- Emails: 200 (diverse deal patterns)
- Events: 100 (long-term schedule)

---

## ⚠️ Notes and Best Practices

### ⚠️ Important Notes

1. **Mailbox capacity**: Large data generation consumes mailbox capacity
2. **Test tenant recommended**: Use a developer tenant, not a production environment
3. **Generation time**: Generating 100 items takes approximately 2-5 minutes
4. **Duplicate runs**: Running the same API multiple times creates duplicate data

### ✅ Best Practices

#### 1. Use a Test Tenant

```
Microsoft 365 Developer Program: Get a free test tenant
https://developer.microsoft.com/microsoft-365/dev-program
```

#### 2. Data Cleanup

Delete unnecessary test data after demos:

- Bulk delete emails with category "商談" (Deal) in Outlook
- Bulk delete "商談" (Deal) events in Calendar

#### 3. Incremental Data Generation

Start with a small amount for testing:

```bash
# First test with 10 items
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=10&eventCount=5"

# If everything works, generate the main dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=50&eventCount=30"
```

---

## 🔧 Troubleshooting

### Error: "Authentication required"

**Cause**: Device Code Flow authentication has not been completed

**Solution**:
1. Visit the device code URL displayed in the console
2. Enter the code and sign in
3. Accept the permissions
4. Run the API again

### Error: "Insufficient permissions"

**Cause**: Required permissions have not been granted

**Solution**:

Check the `TestData` section in `appsettings.json`:

```json
{
  "TestData": {
    "Scopes": [
      "User.Read",
      "Mail.ReadWrite",        // ✅ Required
      "Calendars.ReadWrite"    // ✅ Required
    ]
  }
}
```

### Error: "Cannot access mailbox"

**Cause**: Microsoft 365 license or mailbox is not enabled

**Solution**:
1. Check the license in Microsoft 365 admin center
2. Verify that the Exchange mailbox has been created
3. Log into Outlook once to activate the mailbox

### Data is not being generated

**Diagnostic steps**:

```bash
# 1. Health check
curl https://localhost:5192/health

# 2. Check logs
# Review the application console output

# 3. Test with a small amount
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=1&eventCount=1"
```

---

## 🔍 Using Generated Data

### Test with the Agent

Test the agent using generated data:

```
@Sales Support Agent Tell me this week's deal summary
```

**Expected behavior**:
1. Extracts deal information from generated emails
2. Retrieves this week's meetings from events
3. Generates summary with LLM
4. Replies with visual Adaptive Card

### Verify on Observability Dashboard

1. Access `https://localhost:5192/observability.html`
2. Monitor agent execution in real-time
3. View detailed traces for the data collection phase

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Initial setup
- [Authentication](AUTHENTICATION.md) - Graph API permission settings
- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - Real-time monitoring
- [Agent Development](AGENT-DEVELOPMENT.md) - MCP Tools implementation

---

## 💡 Next Steps

1. ✅ **Test data generation complete**
2. ▶️ Add the Bot to Teams via [Teams Integration](TEAMS-MANIFEST.md)
3. ▶️ Monitor behavior with [Observability Dashboard](OBSERVABILITY-DASHBOARD.md)
4. ▶️ Ask questions to the agent and verify responses

---

**Build an efficient demo environment using the project's APIs!** 🚀
