# Sales Support Agent

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../README.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](README.md)

**Microsoft Agent 365 SDK Demo Application** - AI Agent Leveraging Microsoft 365 Data

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![Agent 365](https://img.shields.io/badge/Agent%20365-SDK-blue)](https://github.com/microsoft/Agent365-Samples)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../LICENSE)

---

## 📋 Overview

A demo application where you can ask in Teams chat "@Sales Support Agent, tell me about this week's sales summary" and the agent will securely access Microsoft 365 data using **Application-only authentication**, collect information from Email, Calendar, SharePoint, and Teams, and return a report.

### 💡 Key Features

| Feature | Description |
|---------|-------------|
| 🔐 **Secure Authentication** | Application-only authentication for safe M365 data access without delegating user permissions |
| 🤖 **LLM Switching** | Support for Azure OpenAI / Ollama (local) / other LLM providers |
| 💬 **Teams Integration** | Seamless integration with Teams via Bot Framework, responses in notification channel |
| 📊 **Observability Dashboard** | Real-time visualization of agent behavior with detailed trace functionality |
| 🎨 **Adaptive Cards** | Visual and interactive responses |
| 🔍 **Advanced Search** | SharePoint full-text search and date range filtering via Microsoft Search API |
| 📈 **Agent 365 SDK Integration** | Leveraging Microsoft's official agent framework |
| 🌐 **Multi-language Support** | Full support for Japanese and English |

### 🎯 Business Value

- Build **your own business agent** in Teams, creating a specialized agent separate from Copilot
- Achieve **data access under governance**, meeting security requirements
- **Cost optimization**: Support for local LLM (Ollama) to reduce cloud costs
- **Full customizability**: Develop custom agents tailored to business workflows

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────┐
│         Teams User (@mention)                │
└──────────────────┬──────────────────────────┘
                   │ Bot Framework
                   ▼
┌──────────────────────────────────────────────────┐
│      Sales Support Agent (.NET 10)               │
│  ┌─────────────────────────────────────────┐    │
│  │  LLM Provider (switchable)               │    │
│  │  - Azure OpenAI / Ollama / Others        │    │
│  └─────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────┐    │
│  │  MCP Tools (M365 Data Access)            │    │
│  │  📧 Outlook  📅 Calendar                │    │
│  │  📁 SharePoint  💬 Teams                │    │
│  └─────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────┐    │
│  │  Agent 365 SDK                          │    │
│  │  - Observability  - Adaptive Cards      │    │
│  └─────────────────────────────────────────┘    │
└──────────────────┬───────────────────────────────┘
                   │ Application-only Authentication
                   ▼
┌──────────────────────────────────────────────────┐
│         Microsoft 365 / Graph API                │
│   Outlook │ Calendar │ SharePoint │ Teams       │
└──────────────────────────────────────────────────┘
```

**Details**: [Architecture Documentation](ARCHITECTURE.md)

---

## 🚀 Quick Start

### Prerequisites

| Required | Recommended/Environment |
|----------|------------------------|
| ✅ **.NET 10 SDK** | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| ✅ **LLM Provider** | Azure OpenAI / Ollama / Others |
| ✅ **Microsoft 365 Tenant** | [Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) |
| ✅ **Azure Subscription** | [Free Account](https://azure.microsoft.com/free/) |
| ⚪ **Dev Tunnel CLI** | For local→Teams connection (recommended) |

### Setup (3 Steps)

#### 1️⃣ Clone the Project

```bash
git clone https://github.com/yourusername/POC-Agent365SDK-TeamsAgent.git
cd POC-Agent365SDK-TeamsAgent/SalesSupportAgent
```

#### 2️⃣ Configure Settings

Minimum configuration example (`appsettings.json`):

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "ApiKey": "your-api-key",
      "DeploymentName": "gpt-4"
    }
  },
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "UserId": "user@company.com"
  }
}
```

**Details**: [Getting Started Guide](GETTING-STARTED.md)

#### 3️⃣ Run

```bash
dotnet run
```

Access to `https://localhost:5192` to verify the Observability Dashboard.

---

## 👥 Choose Documentation for Your Role

This project provides documentation specialized for different personas (roles). Select the optimal documentation set for your purpose.

### 🎓 For First-Time Users & Operations

If you want to try running the Agent first or test Teams integration:

- ✅ [Getting Started Guide (Beginners)](GETTING-STARTED.md) - Read this first
- ✅ [Authentication Setup](AUTHENTICATION.md) - Azure AD App Registration, permissions
- ✅ [Dev Tunnel Setup](DEV-TUNNEL-SETUP.md) - Local→Teams connection
- ✅ [Teams Integration](TEAMS-MANIFEST.md) - Bot manifest, sideload
- ✅ [Sample Data Creation](SAMPLE-DATA.md) - Test data generation
- ✅ [Observability Dashboard](OBSERVABILITY-DASHBOARD.md) - Real-time monitoring
- ✅ [Troubleshooting](TROUBLESHOOTING.md) - Common issues and solutions

### 👨‍💻 For Developers (Code-level Understanding)

If you want to understand SDK integration methods and deepen code-level understanding:

#### 📚 Foundation

- ✅ [SDK Overview](developer/01-SDK-OVERVIEW.md) - **Read this first**: Overview of Microsoft 365 SDK, Agent 365 SDK, Microsoft.Extensions.AI
- ✅ [Project Structure](developer/02-PROJECT-STRUCTURE.md) - Directory structure, file placement
- ✅ [Dependency Injection](developer/05-DEPENDENCY-INJECTION.md) - DI container design and usage

#### 🔐 Authentication & Data Flow

- ✅ [Authentication Flow](developer/03-AUTHENTICATION-FLOW.md) - **Important**: Detailed explanation of Application-only authentication, TokenCredential implementation
- ✅ [Data Flow](developer/04-DATA-FLOW.md) - Complete flow from Graph API → LLM → Response
- ✅ [SDK Integration Patterns](developer/06-SDK-INTEGRATION-PATTERNS.md) - Best practices and design patterns

#### 🛠️ Implementation Details

- ✅ [Error Handling](developer/07-ERROR-HANDLING.md) - Error types and handling strategies
- ✅ [Logging & Telemetry](developer/08-LOGGING-TELEMETRY.md) - Structured logging, OpenTelemetry integration
- ✅ [Testing Strategies](developer/09-TESTING-STRATEGIES.md) - Unit, Integration, E2E testing
- ✅ [Performance Optimization](developer/10-PERFORMANCE-OPTIMIZATION.md) - Graph API optimization, parallel execution
- ✅ [Security Best Practices](developer/11-SECURITY-BEST-PRACTICES.md) - Managed Identity, secret management
- ✅ [Extensibility](developer/12-EXTENSIBILITY.md) - Adding new tools, LLM providers

#### 📖 Code Walkthroughs

- ✅ [Conversation Flow Details](developer/13-CODE-WALKTHROUGHS/CONVERSATION-FLOW.md) - End-to-end execution flow
- ✅ [Graph API Calls](developer/13-CODE-WALKTHROUGHS/GRAPH-API-CALLS.md) - Pattern-based Graph API usage examples
- ✅ [LLM Inference Process](developer/13-CODE-WALKTHROUGHS/LLM-INFERENCE.md) - Tool Calling, streaming responses

#### 📋 Reference

- ✅ [API Reference](developer/14-API-REFERENCE.md) - Main classes and interfaces
- ✅ [Migration Guides](developer/15-MIGRATION-GUIDES.md) - .NET 8 → .NET 10 migration steps

---

## 📖 Documentation (By Category)

### 🎓 Setup Guides

| Document | Content |
|----------|---------|
| [**Getting Started**](GETTING-STARTED.md) | Complete setup instructions (for beginners) |
| [**Authentication**](AUTHENTICATION.md) | Azure AD App Registration, permissions |
| [**Dev Tunnel**](DEV-TUNNEL-SETUP.md) | Local→Teams connection (fixed URL) |
| [**Teams Integration**](TEAMS-MANIFEST.md) | Bot manifest, sideload instructions |

### 🔧 Development Guides

| Document | Content |
|----------|---------|
| [**Architecture**](ARCHITECTURE.md) | System design, component structure |
| [**Agent Development**](AGENT-DEVELOPMENT.md) | Agent implementation patterns, MCP Tools |
| [**Adaptive Cards**](ADAPTIVE-CARDS-GUIDE.md) | Creating visual response cards |
| [**Localization**](LOCALIZATION.md) | Japanese/English switching |
| [**Testing**](TESTING.md) | Unit test, integration test strategies |

### 🎨 Operations Guides

| Document | Content |
|----------|---------|
| [**Observability Dashboard**](OBSERVABILITY-DASHBOARD.md) | Real-time monitoring, detailed traces |
| [**Sample Data**](SAMPLE-DATA.md) | Test data generation (using Project API) |
| [**Azure Deployment**](DEPLOYMENT-AZURE.md) | Production deployment (App Service/Container Apps/AKS) |
| [**Troubleshooting**](TROUBLESHOOTING.md) | Common issues and solutions |

---

## 🌟 Key Features

### Microsoft 365 Data Integration

| Data Source | MCP Tool | Retrieved Content |
|------------|----------|-------------------|
| 📧 **Outlook** | OutlookEmailTool | Sales emails, proposals |
| 📅 **Calendar** | OutlookCalendarTool | Sales appointments, meetings |
| 📁 **SharePoint** | SharePointTool | Documents, quotes (date range search) |
| 💬 **Teams** | TeamsMessageTool | Channel conversations |

### Observability Dashboard

Real-time visualization of agent behavior:
- **Agent Monitoring**: Active status, last activity
- **Conversation Timeline**: Trace user interactions
- **Detailed Phase Display**: Check AI execution internal logic
- **SignalR Real-time Updates**: Immediate reflection on event occurrence

**Access**: `https://localhost:5192/observability.html`

### LLM Provider Switching

Easy switching via configuration file:

```json
// Azure OpenAI
{"LLM": {"Provider": "AzureOpenAI"}}

// Ollama (local)
{"LLM": {"Provider": "Ollama"}}
```

---

## 🧪 Demo Scenarios

### Scenario 1: This Week's Sales Summary

```
@Sales Support Agent Tell me about this week's sales summary
```

**Agent Actions**:
1. 📧 Search sales emails from Outlook
2. 📅 Retrieve sales appointments from Calendar
3. 📁 Search proposals and quotes from SharePoint
4. 💬 Check Teams channel conversations
5. 🤖 Generate integrated report with LLM
6. 🎨 Reply visually with Adaptive Card

### Scenario 2: Specific Customer Information Gathering

```
@Sales Support Agent Compile information about Sample Tech Inc.
```

---

## 🔐 Security

| Item | Implementation |
|------|----------------|
| 🔒 **Authentication Method** | Application-only authentication (no user permission delegation) |
| 🔑 **Secret Management** | Azure Key Vault integration (recommended for production) |
| 🛡️ **Managed Identity** | Secretless authentication in Azure environment |
| 👁️ **Audit Trail** | OpenTelemetry, transcript logging |

**Details**: [Authentication Setup Guide](AUTHENTICATION.md)

---

## ⚠️ Troubleshooting

| Issue | Solution |
|-------|----------|
| ❌ **Cannot access M365 data** | Check permissions in [Authentication Setup](AUTHENTICATION.md) |
| ❌ **Teams Bot not responding** | Verify endpoint in [Dev Tunnel Setup](DEV-TUNNEL-SETUP.md) |
| ❌ **Dashboard disconnection** | Check SignalR Hub URL (/hubs/observability) |

**Details**: [Troubleshooting Guide](TROUBLESHOOTING.md)

---

## 📄 License

This project is released under the [MIT License](../LICENSE).

---

## 🔗 Related Links

- [Microsoft Agent 365 SDK](https://github.com/microsoft/Agent365-Samples)
- [Microsoft Graph API](https://learn.microsoft.com/graph/)
- [Bot Framework](https://dev.botframework.com/)
- [Adaptive Cards](https://adaptivecards.io/)

---

**Enjoy the Sales Support Agent demo using Agent 365 SDK!** 🚀
