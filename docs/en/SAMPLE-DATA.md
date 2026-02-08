# Sample Data Creation Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../SAMPLE-DATA.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](SAMPLE-DATA.md)

## 📋 Overview

This guide explains procedures for generating test data in your Microsoft 365 tenant using **API endpoints implemented in this project** to effectively demonstrate the Sales Support Agent.

### ✨ Features

- ✅ **Easy**: Auto-generate emails and events with one API call
- ✅ **Realistic Data**: Create sales data with Japanese company names, contacts, and product names
- ✅ **Customizable**: Adjust generation count and period
- ✅ **Delegated Permissions**: Register as user with Device Code Flow authentication

---

## 🎯 Generated Data

| Data Type | Content | Count (Default) |
|-----------|---------|----------------|
| 📧 **Sales Emails** | Quote requests, proposal confirmations, contract-related emails | 50 |
| 📅 **Sales Events** | Customer visits, online meetings, internal meetings | 30 |

### Sample Data Content

#### Sales Email Example

```
Subject: Re: [Quote Sent] Cloud Infrastructure Service for Sample Tech Inc.
From: noreply@example.com
Category: Sales, Business

Body:
Dear Mr. Tanaka of Sample Tech Inc.,

Thank you for your continued support.

Please find attached the quote for "Cloud Infrastructure Service" as requested.

Deal Amount: ¥3,500,000
Proposed Product: Cloud Infrastructure Service
...
```

#### Sales Event Example

```
Subject: [Sales Meeting] Digital Innovation Inc. - AI Solution Discussion
Date: 2026-02-15 14:00-15:00
Location: Online (Teams Meeting)
Description: AI Solution proposal and quote explanation
```

---

## ⚙️ Prerequisites

### Required

| Item | Description |
|------|-------------|
| ✅ **Application Running** | Start Sales Support Agent with `dotnet run` |
| ✅ **Microsoft 365 Account** | Account in tenant where data will be created |
| ✅ **Graph API Permissions** | `Mail.ReadWrite`, `Calendars.ReadWrite`, `User.Read` |

### Verification

```bash
# Verify application is running
curl https://localhost:5192/health

# Expected response:
# {"Status":"Healthy", "M365Configured":true, ...}
```

---

## 🚀 Test Data Generation Procedure

The Sales Support Agent provides three API endpoints:

### 1️⃣ **Generate All Data at Once (Recommended)**

Generate emails and events simultaneously.

```bash
curl -X POST https://localhost:5192/api/testdata/generate \
  -H "Content-Type: application/json" \
  -d '{
    "emailCount": 50,
    "eventCount": 30
  }'
```

**Authentication Flow on First Execution**:

```
1. API call initiates Device Code Flow
2. Following message appears in console:

   📱 Device Code Flow authentication required
   Access the following URL in browser:
   https://microsoft.com/devicelogin

   Code: ABCD-EFGH

3. Open URL in browser and enter code
4. Sign in with Microsoft 365 account
5. Click "Accept" on permission consent screen
6. Data generation starts automatically after authentication completes
```

**Success Response**:

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

Generate sales emails only.

```bash
curl -X POST https://localhost:5192/api/testdata/generate/emails?count=100
```

**Response Example**:

```json
{
  "Success": true,
  "Message": "Generated 100 sales emails",
  "Created": 100
}
```

---

### 3️⃣ **Generate Events Only**

Generate sales events only.

```bash
curl -X POST https://localhost:5192/api/testdata/generate/events?count=50
```

**Response Example**:

```json
{
  "Success": true,
  "Message": "Generated 50 sales events",
  "Created": 50
}
```

---

## 🎨 Customization

### Adjust Generation Count

Modify count via query parameters:

```bash
# Generate 200 emails and 100 events
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

### Adjust Generation Period

Current code generates data for the following period:

- **Start Date**: 2 months ago (past sales)
- **End Date**: 1 year ahead (future events)

**How to Customize**:

Edit [Services/TestData/TestDataGenerator.cs](../SalesSupportAgent/Services/TestData/TestDataGenerator.cs):

```csharp
// Default
var startDate = DateTime.Now.AddMonths(-2);
var endDate = DateTime.Now.AddYears(1);

// Example: Change to past 6 months to next 6 months
var startDate = DateTime.Now.AddMonths(-6);
var endDate = DateTime.Now.AddMonths(6);
```

---

## 📊 Verify Generated Data

### Check in Outlook

1. Access [Outlook Web App](https://outlook.office.com)
2. Open "Drafts" folder
3. Filter by "Sales" category

### Check in Calendar

1. Access [Outlook Calendar](https://outlook.office.com/calendar)
2. Search "Sales"
3. View event list

---

## 🧪 Demo Scenario Datasets

Recommended datasets for effective demos:

### Scenario 1: Basic Demo (30 min)

```bash
# Basic dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=30&eventCount=20"
```

- Emails: 30 (minimum)
- Events: 20 (few per week)

### Scenario 2: Detailed Demo (1 hour)

```bash
# Rich dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=100&eventCount=60"
```

- Emails: 100 (multiple customer history)
- Events: 60 (past, present, future schedule)

### Scenario 3: Full Demo (Workshop)

```bash
# Large dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=200&eventCount=100"
```

- Emails: 200 (diverse sales patterns)
- Events: 100 (long-term schedule)

---

## ⚠️ Notes and Best Practices

### ⚠️ Important Notes

1. **Mailbox Capacity**: Large data generation consumes mailbox capacity
2. **Test Tenant Recommended**: Use in developer tenant, not production
3. **Generation Time**: 100 data items take 2-5 minutes
4. **Duplicate Execution**: Running same API multiple times duplicates data

### ✅ Best Practices

#### 1. Use Test Tenant

```
Microsoft 365 Developer Program: Get free test tenant
https://developer.microsoft.com/microsoft-365/dev-program
```

#### 2. Clean Up Data

Delete unnecessary test data after demo:

- Bulk delete "Category: Sales" in Outlook
- Bulk delete "Sales" events in Calendar

#### 3. Incremental Data Generation

Test with small amount first:

```bash
# First test with 10 items
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=10&eventCount=5"

# If OK, generate production dataset
curl -X POST "https://localhost:5192/api/testdata/generate?emailCount=50&eventCount=30"
```

---

## 🔧 Troubleshooting

### Error: "Authentication required"

**Cause**: Device Code Flow authentication not yet completed

**Solution**:
1. Access device code URL shown in console
2. Enter code and sign in
3. Accept permissions
4. Run API again

### Error: "Insufficient  permissions"

**Cause**: Required permissions not granted

**Solution**:

Check `TestData` section in `appsettings.json`:

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

**Cause**: Microsoft 365 license or mailbox not activated

**Solution**:
1. Check license in Microsoft 365 admin center
2. Verify Exchange mailbox is created
3. Log in to Outlook once to activate mailbox

---

## 🔍 Using Generated Data

### Test with Agent

Test agent using generated data:

```
@SalesSupportAgent Show this week's sales summary
```

**Expected Behavior**:
1. Extract sales information from generated emails
2. Get meetings from this week's events
3. Generate summary with LLM
4. Reply visually with Adaptive Card

### Check in Observability Dashboard

1. Access `https://localhost:5192/observability.html`
2. Monitor agent execution in real-time
3. Trace data collection phase in detail

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Initial setup
- [Authentication](AUTHENTICATION.md) - Graph API permissions
- [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - Real-time monitoring
- [Agent Development](AGENT-DEVELOPMENT.md) - MCP Tools implementation

---

## 💡 Next Steps

1. ✅ **Test Data Generation Complete**
2. ▶️ [Teams Integration](TEAMS-MANIFEST.md) to add Bot to Teams
3. ▶️ [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) to monitor operations
4. ▶️ Ask agent questions and check responses

---

**Use this project's API to efficiently build your demo environment!** 🚀
