# Troubleshooting Guide

> **Language**: [🇯🇵 日本語](../TROUBLESHOOTING.md) | 🇬🇧 English

## 📋 Overview

This guide explains possible issues with the Sales Support Agent and their solutions.

**What this guide can resolve**:
- Setup errors
- LLM connection errors
- Microsoft 365 authentication errors
- Teams Bot connection errors
- Observability Dashboard errors
- Performance issues

---

## 🔍 Quick Diagnosis

When issues occur, first check the following:

```bash
# 1. Health check
curl https://localhost:5192/health -k

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# 2. Check logs
# Check application console output

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
7. [Debug Procedures](#7-debug-procedures)

---

## 1. Setup Issues

### Error: "SDK version '10.0.xxx' not found"

**Symptom**:
```
A compatible .NET SDK was not found.
SDK version '10.0.xxx' is required
```

**Cause**: .NET 10 SDK not installed or not in PATH

**Solution**:

```bash
# 1. Check installed SDKs
dotnet --list-sdks

# 2. Install .NET 10 if missing
# macOS: brew install dotnet@10
# Windows: https://dotnet.microsoft.com/download/dotnet/10.0
# Linux: apt-get install dotnet-sdk-10.0

# 3. Verify again
dotnet --version  # Should show 10.0.x
```

---

### Error: "Port  5192 is already in use"

**Symptom**:
```
Failed to bind to address https://0.0.0.0:5192: address already in use
```

**Cause**: Another process is using port 5192

**Solution**:

**macOS / Linux**:
```bash
# Check process using port
lsof -ti:5192

# Kill process
lsof -ti:5192 | xargs kill -9

# Or use different port
dotnet run --urls="https://localhost:5193"
```

**Windows**:
```powershell
# Check process
netstat -ano | findstr :5192

# Kill process (use PID from above)
taskkill /PID <PID> /F
```

---

### Error: "Build error: Package restore failed"

**Symptom**:
```
error NU1102: Unable to find package 'Microsoft.Extensions.AI'
```

**Cause**: NuGet package source issue or network error

**Solution**:

```bash
# 1. Clear NuGet cache
dotnet nuget locals all --clear

# 2. Restore packages
dotnet restore

# 3. Build explicitly
dotnet build --no-restore

# 4. If error persists, check package sources
dotnet nuget list source
```

---

## 2. LLM Connection Errors

### Azure OpenAI: "Unauthorized (401)"

**Symptom**:
```
Azure.RequestFailedException: Unauthorized
Status: 401 (Unauthorized)
```

**Cause**: Incorrect API key, endpoint, or deployment name

**Solution**:

```bash
# 1. Verify in Azure Portal
# Resource → Keys and Endpoint

# 2. Re-check appsettings.json
cat appsettings.json | grep -A5 "AzureOpenAI"

# Correct format:
# "Endpoint": "https://your-resource.openai.azure.com" (no trailing slash)
# "DeploymentName": "gpt-4o" (deployment name, not model name)
# "ApiKey": "32-character alphanumeric"

# 3. Test endpoint connection
curl https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01 \
  -H "api-key: your-api-key"
```

---

### Azure OpenAI: "DeploymentNotFound (404)"

**Symptom**:
```
The API deployment for this resource does not exist
Status: 404 (Not Found)
```

**Cause**: Deployment name is incorrect or deployment doesn't exist

**Solution**:

```bash
# 1. Check deployment in Azure Portal
# Resource → Model deployments → Copy deployment name

# 2. Get deployment list
curl "https://your-resource.openai.azure.com/openai/deployments?api-version=2024-02-01" \
  -H "api-key: your-api-key"

# 3. Fix DeploymentName in appsettings.json
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

**Symptom**:
```
HttpRequestException: Connection refused
Could not connect to http://localhost:11434
```

**Cause**: Ollama server not running

**Solution**:

```bash
# 1. Start Ollama server
ollama serve

# Verify in another terminal
curl http://localhost:11434/api/tags

# Expected output: {"models":[...]}

# 2. Check downloaded models
ollama list

# 3. Restart application
```

---

### Ollama: "Model not found"

**Symptom**:
```
Error: model 'qwen2.5:latest' not found
```

**Cause**: Specified model not downloaded

**Solution**:

```bash
# 1. Download model
ollama pull qwen2.5:latest

# 2. Verify downloaded models
ollama list

# 3. Check ModelName in appsettings.json
{
  "LLM": {
    "Ollama": {
      "ModelName": "qwen2.5:latest"  # Must match NAME column in ollama list
    }
  }
}
```

---

## 3. Microsoft 365 Authentication Errors

### Error: "Unauthorized - Invalid client secret"

**Symptom**:
```
AADSTS7000215: Invalid client secret provided
Status: 401 (Unauthorized)
```

**Cause**: ClientSecret is incorrect or expired

**Solution**:

```bash
# 1. Create new secret in Azure Portal
# Microsoft Entra ID → App registrations → Select app
# → Certificates & secrets → + New client secret

# 2. Copy displayed "Value" (shown only once)

# 3. Update appsettings.json or environment variable
{
  "M365": {
    "ClientSecret": "new-secret"
  }
}

# Or environment variable
export M365__ClientSecret="new-secret"
```

---

### Error: "Forbidden - Insufficient privileges"

**Symptom**:
```
ErrorCode: Authorization_RequestDenied
Message: Insufficient privileges to complete the operation
Status: 403 (Forbidden)
```

**Cause**: Admin consent not granted or insufficient permissions

**Solution**:

```bash
# 1. Verify admin consent in Azure Portal
# Microsoft Entra ID → App registrations → Select app
# → API permissions

# 2. Check all permissions show "✓ Granted for (organization)"

# 3. If not granted:
# Click "Grant admin consent for (organization)" → "Yes"

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

**Symptom**:
```
ArgumentException: TenantId cannot be null or empty
```

**Cause**: M365 settings not correctly loaded from appsettings.json or environment variables

**Solution**:

```bash
# 1. Check appsettings.json
cat appsettings.json | grep -A5 "M365"

# 2. Check environment variables
printenv | grep M365

# 3. Verify correct configuration
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

## 4. Teams Bot Errors

### Error: "Bot not responding in Teams"

**Symptom**: No response when messaging bot in Teams

**Solution**:

```bash
# 1. Verify local application is running
dotnet run

# 2. Verify Dev Tunnel / ngrok is running
devtunnel host sales-support-agent
# or
ngrok http 5192

# 3. Verify messaging endpoint in Azure Portal
# Bot Service → Configuration → Messaging endpoint
# Should be: https://your-tunnel-url/api/messages

# 4. Test endpoint locally
curl -X POST https://localhost:5192/api/messages \
  -H "Content-Type: application/json" \
  -d '{"type":"message","text":"test"}' \
  -k
```

---

## 5. Observability Dashboard Errors

### Error: "Cannot connect to SignalR"

**Symptom**: Dashboard shows "Disconnected"

**Cause**: Application not running, incorrect path, or CORS configuration error

**Solution**:

```bash
# 1. Verify application is running
dotnet run

# 2. Verify correct path: /hubs/observability

# 3. Check browser console for errors
# Open browser DevTools → Console

# 4. Verify port 5192 is accessible
curl https://localhost:5192/health -k
```

---

## 6. Performance Issues

### Issue: "Slow response time"

**Symptoms**: Agent takes >10 seconds to respond

**Solutions**:

1. **Check LLM Response Time**:
```bash
# Monitor logs for LLM call duration
# Look for: "LLM_Completion: XXXXms"
```

2. **Check Graph API Performance**:
```bash
# Monitor logs for Graph API call duration
# Look for: "SearchEmails: XXXms"
```

3. **Enable Caching**:
```csharp
// Add caching for frequently accessed data
services.AddMemoryCache();
```

4. **Optimize Queries**:
```csharp
// Reduce maxResults
await _emailTool.SearchEmailsAsync(query, maxResults: 10); // Instead of 50
```

---

## 7. Debug Procedures

### Enable Detailed Logging

**appsettings.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.Graph": "Debug"
    }
  }
}
```

### Use Debug Mode

```bash
# Run in debug mode
dotnet run --configuration Debug

# Or attach debugger in VS Code
# F5 → .NET Core Launch
```

### Check Application Insights (Azure)

If deployed to Azure:

```bash
# View logs in Azure Portal
# Application Insights → Logs → Query logs
```

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Setup procedures
- [Authentication](AUTHENTICATION.md) - Authentication configuration
- [Architecture](ARCHITECTURE.md) - System design
- [Testing](TESTING.md) - Testing strategy

---

**Resolve issues quickly and keep the agent running smoothly!** 🔧
