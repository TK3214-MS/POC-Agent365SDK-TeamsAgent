# Troubleshooting Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TROUBLESHOOTING.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](TROUBLESHOOTING.md)

## 📋 Overview

This guide describes potential issues and solutions for the Sales Support Agent.

**Issues covered in this guide**:
- Setup errors
- LLM connection errors
- Microsoft 365 authentication errors
- Teams Bot connection errors
- Observability Dashboard errors
- Performance issues

---

## 🔍 Quick Diagnosis

When a problem occurs, start by checking the following:

```bash
# 1. Health check
curl https://localhost:5192/health -k

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# 2. Check logs
# Review the application console output

# 3. Check port
lsof -i :5192  # macOS/Linux
netstat -ano | findstr :5192  # Windows
```

---

## 📚 Table of Contents

1. [Setup Issues](#1-setup-issues)
2. [LLM Connection Errors](#2-llm-connection-errors)
3. [Microsoft 365 Authentication Errors](#3-microsoft-365-authentication-errors)
4. [Teams Bot Errors](#4-teams-bot-errors)
5. [Observability Dashboard Errors](#5-observability-dashboard-errors)
6. [Performance Issues](#6-performance-issues)
7. [Debugging Procedures](#7-debugging-procedures)

---

## 1. Setup Issues

### Error: "SDK version '10.0.xxx' not found"

**Symptoms**:
```
A compatible .NET SDK was not found.
SDK version '10.0.xxx' is required
```

**Cause**: .NET 10 SDK is not installed or not added to PATH

**Solution**:

```bash
# 1. Check installed SDKs
dotnet --list-sdks

# 2. If .NET 10 is not present, install it
# macOS: brew install dotnet@10
# Windows: https://dotnet.microsoft.com/download/dotnet/10.0
# Linux: apt-get install dotnet-sdk-10.0

# 3. Verify again
dotnet --version  # Should display 10.0.x
```

---

### Error: "Port 5192 is already in use"

**Symptoms**:
```
Failed to bind to address https://0.0.0.0:5192: address already in use
```

**Cause**: Another process is using port 5192

**Solution**:

**macOS / Linux**:
```bash
# Find the process using the port
lsof -ti:5192

# Kill the process
lsof -ti:5192 | xargs kill -9

# Or use a different port
dotnet run --urls="https://localhost:5193"
```

**Windows**:
```powershell
# Find the process using the port
netstat -ano | findstr :5192

# Kill the process by PID
taskkill /PID <PID> /F
```

---

### Error: "Build error: Package restore failed"

**Symptoms**:
```
error NU1102: Unable to find package 'Microsoft.Extensions.AI'
```

**Cause**: NuGet package source issues or network error

**Solution**:

```bash
# 1. Clear NuGet cache
dotnet nuget locals all --clear

# 2. Restore packages
dotnet restore

# 3. Build explicitly
dotnet build --no-restore

# 4. If errors persist, check package sources
dotnet nuget list source
```

---

### Error: "appsettings.json not found"

**Symptoms**:
```
Could not find a part of the path '.../appsettings.json'
```

**Cause**: Working directory is incorrect

**Solution**:

```bash
# Navigate to the correct directory
cd /path/to/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# Verify appsettings.json exists
ls -la appsettings.json

# Run application
dotnet run
```

---

## 2. LLM Connection Errors

### Azure OpenAI: "Unauthorized (401)"

**Symptoms**:
```
Azure.RequestFailedException: Unauthorized
Status: 401 (Unauthorized)
```

**Cause**: Incorrect API key, endpoint, or deployment name

**Solution**:

```bash
# 1. Verify in Azure Portal
# Resource → Keys and Endpoint

# 2. Review appsettings.json
cat appsettings.json | grep -A5 "AzureOpenAI"

# Correct settings:
# "Endpoint": "https://your-resource.openai.azure.com" (no trailing slash)
# "DeploymentName": "gpt-4o" (deployment name, not model name)
# "ApiKey": "32-character alphanumeric string"

# 3. Test endpoint connectivity
curl https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01 \
  -H "api-key: your-api-key"
```

---

### Azure OpenAI: "DeploymentNotFound (404)"

**Symptoms**:
```
The API deployment for this resource does not exist
Status: 404 (Not Found)
```

**Cause**: Incorrect deployment name or deployment doesn't exist

**Solution**:

```bash
# 1. Verify deployment in Azure Portal
# Resource → Model deployments → Copy deployment name

# 2. List deployments
curl "https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01" \
  -H "api-key: your-api-key"

# 3. Update DeploymentName in appsettings.json
{
  "LLM": {
    "AzureOpenAI": {
      "DeploymentName": "actual-deployment-name"  # e.g., "gpt-4o-deployment"
    }
  }
}
```

---

### Ollama: "Connection refused"

**Symptoms**:
```
HttpRequestException: Connection refused
Could not connect to http://localhost:11434
```

**Cause**: Ollama server is not running

**Solution**:

```bash
# 1. Start Ollama server
ollama serve

# Verify in a separate terminal
curl http://localhost:11434/api/tags

# Expected output: {"models":[...]}

# 2. Check if model is downloaded
ollama list

# 3. Restart the application
```

---

### Ollama: "Model not found"

**Symptoms**:
```
Error: model 'qwen2.5:latest' not found
```

**Cause**: The specified model has not been downloaded

**Solution**:

```bash
# 1. Download the model
ollama pull qwen2.5:latest

# 2. Check downloaded models
ollama list

# 3. Verify ModelName in appsettings.json
{
  "LLM": {
    "Ollama": {
      "ModelName": "qwen2.5:latest"  # Must match NAME column from ollama list
    }
  }
}
```

---

## 3. Microsoft 365 Authentication Errors

### Error: "Unauthorized - Invalid client secret"

**Symptoms**:
```
AADSTS7000215: Invalid client secret provided
Status: 401 (Unauthorized)
```

**Cause**: ClientSecret is incorrect or expired

**Solution**:

```bash
# 1. Create a new secret in Azure Portal
# Microsoft Entra ID → App registrations → Select app
# → Certificates & secrets → + New client secret

# 2. Copy the displayed "Value" (shown only once)

# 3. Update appsettings.json or environment variables
{
  "M365": {
    "ClientSecret": "new-secret"
  }
}

# Or via environment variable
export M365__ClientSecret="new-secret"
```

---

### Error: "Forbidden - Insufficient privileges"

**Symptoms**:
```
ErrorCode: Authorization_RequestDenied
Message: Insufficient privileges to complete the operation
Status: 403 (Forbidden)
```

**Cause**: Admin consent has not been granted, or insufficient permissions

**Solution**:

```bash
# 1. Check admin consent in Azure Portal
# Microsoft Entra ID → App registrations → Select app
# → API permissions

# 2. Verify all permissions show "✓ Granted for (org name)"

# 3. If not granted:
# Click "Grant admin consent for (org name)" → "Yes"

# 4. Verify required permissions are added:
# - Mail.Read
# - Calendars.Read
# - Files.Read.All
# - Sites.Read.All
# - ChannelMessage.Read.All
# - Team.ReadBasic.All
```

---

### Error: "TenantId is empty"

**Symptoms**:
```
ArgumentException: TenantId cannot be null or empty
```

**Cause**: M365 settings are not properly loaded from appsettings.json or environment variables

**Solution**:

```bash
# 1. Check appsettings.json
cat appsettings.json | grep -A5 "M365"

# 2. Check environment variables
printenv | grep M365

# 3. Verify settings are correct
{
  "M365": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}

# 4. Verify GUID format is correct (with hyphens)
```

---

### Error: "User not found"

**Symptoms**:
```
Request_ResourceNotFound: User 'user-id' does not exist
```

**Cause**: M365Settings.UserId is incorrect or not set

**Solution**:

```bash
# 1. Get your user ID
# Use Microsoft Graph Explorer: https://developer.microsoft.com/graph/graph-explorer
# GET https://graph.microsoft.com/v1.0/me

# 2. Or use PowerShell
Connect-MgGraph
Get-MgUser -UserId "your-email@domain.com" | Select-Object -Property Id

# 3. Update appsettings.json
{
  "M365": {
    "UserId": "obtained-user-id"
  }
}

# Note: Application-only auth requires UserId for mailbox access
```

---

## 4. Teams Bot Errors

### Error: "Bot is not responding"

**Symptoms**: No response when @mentioning the bot in Teams

**Diagnostic steps**:

```bash
# 1. Check if Dev Tunnel / ngrok is running
devtunnel list
# or
ngrok http https://localhost:5192

# 2. Get the tunnel endpoint
# e.g., https://abc123-5192.euw.devtunnels.ms

# 3. Verify Azure Bot messaging endpoint
# Azure Portal → Bot Services → Configuration
# Messaging endpoint:
# https://abc123-5192.euw.devtunnels.ms/api/messages
#                                      ↑ /api/messages is required

# 4. Verify the application is running
curl https://localhost:5192/health -k

# 5. Check logs
# Look for the following in console:
# info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
#       Request starting HTTP/1.1 POST http://localhost:5192/api/messages
```

---

### Error: "Unauthorized - AppId mismatch"

**Symptoms**:
```
BotFrameworkAdapter.ProcessActivity: 401 Unauthorized
```

**Cause**: Bot settings in appsettings.json don't match Azure Bot configuration

**Solution**:

```bash
# 1. Verify the following in Azure Portal → Bot Services → Configuration:
# - Microsoft App ID
# - Microsoft App Tenant ID

# 2. Match with Bot settings in appsettings.json
{
  "Bot": {
    "MicrosoftAppId": "App ID from Azure Portal",
    "MicrosoftAppPassword": "Client secret",
    "MicrosoftAppTenantId": "Tenant ID"
  }
}

# 3. Verify secret is valid (may be expired)
# Microsoft Entra ID → App registrations → Certificates & secrets
```

---

### Error: "Teams Manifest validation failed"

**Symptoms**: Error when installing the app

**Solution**:

```bash
# 1. Validate manifest.json
# Teams Developer Portal: https://dev.teams.microsoft.com/
# Apps → Validate

# 2. Common issues:
# - botId doesn't match the Azure Bot App ID
# - validDomains doesn't include the tunnel URL
# - version format is incorrect (should be e.g., "1.0.0")

# 3. Correct manifest.json example:
{
  "bots": [{
    "botId": "your-app-id-from-azure-bot",
    "scopes": ["personal", "team"]
  }],
  "validDomains": ["*.devtunnels.ms"],
  "version": "1.0.0"
}
```

---

## 5. Observability Dashboard Errors

### Error: "SignalR connection error - 404 Not Found"

**Symptoms**: Dashboard continuously shows "Disconnected" state

**Cause**: SignalR Hub URL path is incorrect

**Solution**:

```bash
# 1. Verify Hub is correctly mapped in Program.cs
# app.MapHub<ObservabilityHub>("/hubs/observability");

# 2. Check SignalR connection URL in observability.html
# const connection = new signalR.HubConnectionBuilder()
#     .withUrl("/hubs/observability")  # ← Must match Program.cs path
#     .build();

# 3. Verify in browser developer tools
# Network tab → observability/negotiate request
# Status should be: 200

# 4. Check for CORS errors
# Look for CORS errors in Console tab
```

---

### Error: "Agent information not displayed"

**Symptoms**: Dashboard stuck at "Fetching agent information..."

**Solution**:

```bash
# 1. Check API directly
curl https://localhost:5192/api/observability/agents -k

# If an empty array [] is returned:
# → Agent is not registered (should be registered automatically at app startup)

# 2. Verify agent registration in Program.cs
# lifetime.ApplicationStarted.Register(async () => { ... });

# 3. Check logs for agent registration messages
# "✅ Agent Identity created successfully" or
# "🤖 Agent registered: Sales Support Agent"

# 4. If errors exist, check ObservabilityService initialization
```

---

## 6. Performance Issues

### Issue: "Response is very slow (30+ seconds)"

**Cause**: LLM timeout, large data retrieval, network latency

**Diagnosis**:

```bash
# 1. Check detailed traces on Observability Dashboard
# https://localhost:5192/observability.html
# → Select the relevant session from Recent Traces
# → Identify which phase is taking long

# 2. Typical bottlenecks:
# - "AI Agent Execution": LLM response is slow
# - "Data Collection": Graph API queries are slow
# - "SharePoint Search": Large document search

# 3. Remediation:
# - LLM: Use a faster model (gpt-4o-mini)
# - Graph API: Tighten filter conditions (TOP 10 → TOP 5)
# - SharePoint: Narrow date range (1 month → 1 week)
```

**Optimization example**:

```csharp
// OutlookEmailTool.cs
var result = await _graphClient.Users[userId]
    .Messages
    .GetAsync(config =>
    {
        config.QueryParameters.Top = 5;  // Reduced from 10 to 5
        config.QueryParameters.Select = new[] { "subject", "from", "receivedDateTime" };  // Only required fields
    });
```

---

### Issue: "High memory usage"

**Cause**: Ollama model memory consumption, large data caching

**Remediation**:

```bash
# 1. Check memory usage
# macOS/Linux
ps aux | grep dotnet
top -pid $(pgrep -f dotnet)

# Windows
tasklist | findstr dotnet

# 2. When using Ollama:
# Use a smaller model
ollama pull qwen2.5:7b  # Use 7B model instead

# 3. .NET GC settings
# Add GC settings to appsettings.json
{
  "System.GC.Concurrent": true,
  "System.GC.Server": true,
  "System.GC.RetainVM": false
}
```

---

## 7. Debugging Procedures

### Enable Verbose Logging

**appsettings.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "SalesSupportAgent": "Debug"  // ← Change to Debug level
    }
  }
}
```

### OpenTelemetry Trace Inspection

```bash
# Console output will show traces like:
# Activity.TraceId:            abc123...
# Activity.SpanId:             def456...
# Activity.TraceFlags:         Recorded
# Activity.ActivitySourceName: SalesSupportAgent
# Activity.DisplayName:        GenerateSalesSummary
# Activity.Kind:               Internal
# Activity.StartTime:          2026-02-08T10:00:00.0000000Z
# Activity.Duration:           00:00:06.4200000
#     SearchOutlookEmails: 850ms
#     SearchCalendarEvents: 620ms
#     SearchSharePointDocuments: 1250ms
#     LLM_Completion: 3200ms
```

### HTTP Request Debugging

```bash
# Use Fiddler / Charles Proxy to capture HTTP traffic

# Or test APIs directly with curl
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"test"}' \
  --verbose \
  -k
```

---

## 📞 Support

If the above solutions don't resolve your issue:

1. **Check log files**: Copy the entire console output
2. **Gather environment information**:
   ```bash
   dotnet --info
   cat appsettings.json | grep -v "Secret\|Key\|Password"
   ```
3. **Create an Issue**: [GitHub Issues](https://github.com/yourusername/POC-Agent365SDK-TeamsAgent/issues)

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Initial setup
- [Authentication](AUTHENTICATION.md) - Graph API authentication details
- [Architecture](ARCHITECTURE.md) - System architecture
- [Agent Development](AGENT-DEVELOPMENT.md) - Customization methods

---

Once your issue is resolved, refer to other guides to continue development! 🚀
