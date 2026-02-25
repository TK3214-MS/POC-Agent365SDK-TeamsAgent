# Getting Started - Sales Support Agent

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../GETTING-STARTED.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](GETTING-STARTED.md)

## 📋 Introduction

This guide provides complete step-by-step instructions for first-time setup of the **Sales Support Agent**, from zero to operational state.

**Estimated Time**: 60-90 minutes

**Goal**: Launch the Sales Support Agent locally and verify operation via API

---

## 🎯 Completion State

Upon completing this guide, you will achieve:

- ✅ .NET 10 environment setup complete
- ✅ LLM provider (Azure OpenAI / Ollama) configured
- ✅ Microsoft 365 authentication configured
- ✅ Sales Support Agent running locally
- ✅ API-based sales summary generation functional
- ✅ Observability Dashboard monitoring operations

---

## 📚 Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Development Environment Setup](#2-development-environment-setup)
3. [Clone the Project](#3-clone-the-project)
4. [LLM Provider Selection and Configuration](#4-llm-provider-selection-and-configuration)
5. [Microsoft 365 Authentication Configuration](#5-microsoft-365-authentication-configuration)
6. [Application Configuration](#6-application-configuration)
7. [Build and Launch](#7-build-and-launch)
8. [Verification](#8-verification)
9. [Next Steps](#9-next-steps)

---

## 1. Prerequisites

### Required Accounts & Environment

| Item | Required | Description | How to Obtain |
|------|:--------:|-------------|---------------|
| **Microsoft 365 Tenant** | ✅ | For email, calendar, and other data access | [Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) |
| **Azure Subscription** | ✅ | For App Registration creation | [Free Account](https://azure.microsoft.com/free/) |
| **LLM Provider** | ✅ | Azure OpenAI or Ollama | See below |

### LLM Provider Selection

Choose one of the following options:

#### Option A: Azure OpenAI (Recommended)

- **Benefits**: High performance, stable, enterprise-ready
- **Cost**: Pay-as-you-go (GPT-4o: $5-15/1M tokens)
- **Requirements**: Azure subscription, resource creation
- **Setup Time**: 15-20 minutes

#### Option B: Ollama (Local LLM)

- **Benefits**: Completely free, offline operation, privacy protection
- **Cost**: Free (hardware only)
- **Requirements**: Sufficient memory (16GB+ recommended)
- **Setup Time**: 10-15 minutes
- **Download**: [Ollama Official Site](https://ollama.ai/)

### Hardware Requirements

| Item | Minimum | Recommended |
|------|---------|-------------|
| **CPU** | 2 cores | 4+ cores |
| **Memory** | 8GB | 16GB+ (required for Ollama) |
| **Storage** | 10GB | 20GB+ |
| **OS** | Windows 10/11, macOS 12+, Linux | - |

---

## 2. Development Environment Setup

### 2.1. Install .NET 10 SDK

#### Windows

```powershell
# Download from Microsoft official site
# https://dotnet.microsoft.com/download/dotnet/10.0

# After installation, verify
dotnet --version
# Expected output: 10.0.0
```

#### macOS

```bash
# Using Homebrew
brew install dotnet@10

# Or official installer
# https://dotnet.microsoft.com/download/dotnet/10.0

# Verify
dotnet --version
```

#### Linux (Ubuntu/Debian)

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 10 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Verify
dotnet --version
```

### 2.2. Install Git

```bash
# Windows: https://git-scm.com/download/win
# macOS: brew install git
# Linux: sudo apt-get install git

# Verify
git --version
```

### 2.3. Editor/IDE (Recommended)

Install one of the following:

- **Visual Studio Code** (Recommended): [Download](https://code.visualstudio.com/)
  - Extensions: C# Dev Kit, .NET Extension Pack
- **Visual Studio 2022**: [Download](https://visualstudio.microsoft.com/)
- **JetBrains Rider**: [Download](https://www.jetbrains.com/rider/)

### 2.4. curl or HTTP Client (For Verification)

```bash
# curl (usually pre-installed)
curl --version

# Or GUI tools like Postman / Insomnia
```

---

## 3. Clone the Project

### 3.1. Clone Repository

```bash
# Clone
git clone https://github.com/yourusername/POC-Agent365SDK-TeamsAgent.git

# Navigate to directory
cd POC-Agent365SDK-TeamsAgent/SalesSupportAgent
```

### 3.2. Verify Project Structure

```bash
ls -la

# You should see files and directories like:
# Program.cs
# SalesSupportAgent.csproj
# appsettings.json
# Bot/
# Services/
# ...
```

---

## 4. LLM Provider Selection and Configuration

Follow one of the procedures below based on your chosen LLM provider.

### Option A: Azure OpenAI Setup

#### 4.A.1. Create Azure OpenAI Resource

1. **Access Azure Portal**: [portal.azure.com](https://portal.azure.com)

2. **Create Resource**:
   ```
   Search for "Azure OpenAI" in the search bar → Create
   ```

3. **Basic Settings**:
   - **Subscription**: Any
   - **Resource Group**: Create new (e.g., `rg-salesagent-dev`)
   - **Region**: East US / Japan East, etc.
   - **Name**: Unique name (e.g., `openai-salesagent-dev`)
   - **Pricing Tier**: Standard S0

4. **Create** → Wait for deployment completion (2-3 minutes)

#### 4.A.2. Deploy Model

1. Navigate to the created resource
2. **"Model deployments"** → **"+ Create deployment"**
3. **Model Selection**:
   - **Model**: gpt-4o (recommended) or gpt-4o-mini
   - **Deployment name**: `gpt-4o` (record this name)
   - **Version**: Latest
4. **Create deployment**

#### 4.A.3. Get Endpoint and API Key

1. Click **"Keys and Endpoint"**
2. Copy and save the following:
   ```
   Endpoint: https://your-resource.openai.azure.com
   Key 1: xxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```

#### 4.A.4. Configure appsettings.json

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

---

### Option B: Ollama Setup

#### 4.B.1. Install Ollama

**macOS / Linux:**

```bash
# Install
curl -fsSL https://ollama.ai/install.sh | sh

# Verify
ollama --version
```

**Windows:**

1. Download [Ollama for Windows](https://ollama.ai/download/windows)
2. Run installer
3. Verify in command prompt:
   ```cmd
   ollama --version
   ```

#### 4.B.2. Download Model

Recommended model: **Qwen2.5** (high performance, Japanese support)

```bash
# Download model (10-15 minutes for first time)
ollama pull qwen2.5:latest

# Other options:
# ollama pull llama3.2:latest       # Meta Llama 3.2 (8B)
# ollama pull mistral:latest        # Mistral (7B)
# ollama pull gemma2:latest         # Google Gemma 2 (9B)
```

#### 4.B.3. Start Ollama Server

```bash
# Start server in background
ollama serve

# Verify in another terminal
ollama list

# Expected output:
# NAME             ID              SIZE      MODIFIED
# qwen2.5:latest   abc123...       4.7GB     2 minutes ago
```

#### 4.B.4. Configure appsettings.json

```json
{
  "LLM": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelName": "qwen2.5:latest"
    }
  }
}
```

---

## 5. Microsoft 365 Authentication Configuration

### 5.1. Create Azure AD App Registration

#### Step 1: Access Azure Portal

[Azure Portal](https://portal.azure.com) → **Microsoft Entra ID**

#### Step 2: Register App

1. **"App registrations"** → **"+ New registration"**

2. **Basic Information**:
   - **Name**: `SalesSupportAgent`
   - **Supported account types**: `Accounts in this organizational directory only (Single tenant)`
   - **Redirect URI**: Leave blank

3. Click **"Register"**

#### Step 3: Record Application Information

After registration, on the **"Overview"** page, copy:

```
Application (client) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Directory (tenant) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

#### Step 4: Create Client Secret

1. **"Certificates & secrets"** → **"+ New client secret"**

2. **Settings**:
   - **Description**: `SalesSupportAgent Secret`
   - **Expires**: 24 months (recommended)

3. **"Add"** → Copy the displayed **"Value"** (shown only once)

```
Client Secret: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

#### Step 5: Configure API Permissions

1. **"API permissions"** → **"+ Add a permission"**

2. **"Microsoft Graph"** → **"Application permissions"**

3. Search and add the following permissions:

| Permission | Purpose |
|-----------|---------|
| `Mail.Read` | Outlook email search |
| `Calendars.Read` | Calendar event search |
| `Files.Read.All` | SharePoint file access |
| `Sites.Read.All` | SharePoint sites & Search API |
| `ChannelMessage.Read.All` | Teams message search |
| `Team.ReadBasic.All` | Teams basic information |

4. Click **"Add permissions"**

#### Step 6: Grant Admin Consent ⚠️

**Important**: The application won't work without this step

1. Click **"Grant admin consent for {organization}"**
2. Click **"Yes"** in confirmation dialog
3. Verify all permissions show **"✓ Granted for {organization}"**

---

## 6. Application Configuration

### 6.1. Edit appsettings.json

Open `SalesSupportAgent/appsettings.json` and update the following sections:

```json
{
  "LLM": {
    "Provider": "AzureOpenAI",  // or "Ollama"
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-api-key-here"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelName": "qwen2.5:latest"
    }
  },
  "M365": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "UseManagedIdentity": false
  },
  "Bot": {
    "MicrosoftAppType": "MultiTenant",
    "MicrosoftAppId": "",
    "MicrosoftAppPassword": "",
    "MicrosoftAppTenantId": ""
  },
  "Localization": {
    "DefaultLanguage": "ja"
  }
}
```

### 6.2. Configuration via Environment Variables (Recommended・Optional)

For improved security, configure secrets via environment variables:

```bash
# macOS / Linux
export M365__TenantId="your-tenant-id"
export M365__ClientId="your-client-id"
export M365__ClientSecret="your-client-secret"
export LLM__AzureOpenAI__ApiKey="your-api-key"

# Windows PowerShell
$env:M365__TenantId="your-tenant-id"
$env:M365__ClientId="your-client-id"
$env:M365__ClientSecret="your-client-secret"
$env:LLM__AzureOpenAI__ApiKey="your-api-key"
```

---

## 7. Build and Launch

### 7.1. Restore Dependencies

```bash
cd /path/to/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# Restore NuGet packages
dotnet restore
```

### 7.2. Build

```bash
dotnet build

# Expected output on success:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

### 7.3. Launch

```bash
dotnet run

# Startup logs will be displayed:
# ========================================
# Sales Support Agent Starting
# LLM Provider: AzureOpenAI
# M365 Configuration: ✅ Enabled
# Bot Configuration: ❌ Not configured
# ========================================
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:5192
```

### 7.4. Verify Startup

In another terminal:

```bash
curl https://localhost:5192/health -k

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}
```

---

## 8. Verification

### 8.1. Health Check

```bash
curl https://localhost:5192/health -k
```

### 8.2. Sales Summary Generation Test

```bash
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"Show me this week'\''s sales summary"}' \
  -k
```

**Note for First Execution**:
- If there is no email or event data, empty results may be returned
- Recommended to create [sample data](SAMPLE-DATA.md) first

### 8.3. Observability Dashboard

Access in browser:

```
https://localhost:5192/observability.html
```

**Verification Points**:
- ✅ "Connected" status displayed
- ✅ Agent information displayed
- ✅ Metrics are updating

### 8.4. OpenAPI / Swagger UI

```
https://localhost:5192/swagger
```

View list of available API endpoints.

---

## 9. Next Steps

### ✅ What You've Accomplished

- [x] Agent running in local environment
- [x] LLM provider configured
- [x] Microsoft 365 authentication configured
- [x] Basic operation verified

### ▶️ Guides to Follow Next

1. **Create Test Data**
   - [Sample Data Creation Guide](SAMPLE-DATA.md)
   - Auto-generate emails and events via project API

2. **Teams Integration**
   - [Dev Tunnel Setup](DEV-TUNNEL-SETUP.md)
   - [Teams Bot Manifest Configuration](TEAMS-MANIFEST.md)
   - Operate as "@SalesSupportAgent" in Teams

3. **Utilize Observability**
   - [Observability Dashboard Guide](OBSERVABILITY-DASHBOARD.md)
   - Real-time monitoring of agent operations

4. **Customization**
   - [Agent Development Guide](AGENT-DEVELOPMENT.md)
   - Add custom agents and MCP Tools

---

## ⚠️ Troubleshooting

### Build Error: "SDK not found"

```bash
# Check .NET SDK
dotnet --list-sdks

# Reinstall if .NET 10 is missing
```

### Startup Error: "Port 5192 already in use"

```bash
# macOS / Linux
lsof -ti:5192 | xargs kill -9

# Windows
netstat -ano | findstr :5192
taskkill /PID <PID> /F
```

### M365 Data Access Error: "Unauthorized (401)"

**Cause**: Authentication credentials incorrect or admin consent not granted

**Resolution**:
1. Verify `TenantId`, `ClientId`, `ClientSecret` in `appsettings.json`
2. Verify admin consent is granted in Azure Portal
3. Check secret expiration date

### LLM Connection Error

**Azure OpenAI**:
```bash
# Verify endpoint, key, and deployment name
# Azure Portal → Resource → Keys and Endpoint
```

**Ollama**:
```bash
# Verify Ollama server is running
ollama list

# If not running
ollama serve
```

For details, refer to the [Troubleshooting Guide](TROUBLESHOOTING.md).

---

## 📚 Related Documentation

- [README](README.md) - Project overview
- [Architecture](ARCHITECTURE.md) - System design details
- [Authentication](AUTHENTICATION.md) - App Registration details
- [Sample Data](SAMPLE-DATA.md) - Test data generation
- [Teams Integration](TEAMS-MANIFEST.md) - Teams integration
- [Azure Deployment](DEPLOYMENT-AZURE.md) - Production environment setup

---

**Congratulations! 🎉**

Your local Sales Support Agent environment setup is complete. Next, try the agent with actual data!
