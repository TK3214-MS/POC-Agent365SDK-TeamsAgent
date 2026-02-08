# Microsoft 365 Authentication Configuration Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../AUTHENTICATION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](AUTHENTICATION.md)

## 📋 Overview

The Sales Support Agent uses **Application-only authentication** to access Microsoft 365 data.

This guide provides complete authentication configuration procedures, from creating Azure AD App Registration and configuring permissions to using Managed Identity in production environments.

---

## 🎯 What is Application-only Authentication?

### Characteristics

| Feature | Description |
|---------|-------------|
| 🔐 **No user permission delegation** | Access with application's own permissions |
| 🤖 **Optimal for background processing** | Bots, scheduled tasks, etc. |
| 🔑 **App ID + Secret/Certificate** | ClientSecretCredential or Managed Identity |
| 📊 **Organization-wide data access** | Not dependent on specific users |
| 🛡️ **Complete audit trail** | All access logs are recorded |

### Difference from Delegated Authentication

| Item | Application-only | Delegated |
|------|-----------------|-----------|
| **Auth Method** | App ID + Secret/Certificate | User login (OAuth) |
| **User Context** | None | Yes (signed-in user) |
| **Access Scope** | Organization-wide (per permissions) | Signed-in user's data only |
| **Use Case** | Bots, automation, server apps | Interactive Web/Mobile apps |
| **Graph API Permissions** | Application Permissions | Delegated Permissions |

---

## 📚 Table of Contents

1. [Create Azure AD App Registration](#1-create-azure-ad-app-registration)
2. [Configure API Permissions](#2-configure-api-permissions)
3. [Local Development Environment Configuration](#3-local-development-environment-configuration)
4. [Azure Production Environment Configuration](#4-azure-production-environment-configuration)
5. [Security Best Practices](#5-security-best-practices)
6. [Verification](#6-verification)
7. [Troubleshooting](#7-troubleshooting)

---

## 1. Create Azure AD App Registration

### Step 1-1: Access Azure Portal

1. Open [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra ID**

### Step 1-2: Register Application

1. Click **"App registrations"** → **"+ New registration"**

2. **Enter basic information**:

| Item | Value |
|------|-------|
| **Name** | `SalesSupportAgent` |
| **Supported account types** | `Accounts in this organizational directory only (Single tenant)` |
| **Redirect URI** | Leave blank (not required for Application-only auth) |

3. Click **"Register"**

### Step 1-3: Record Application Information

After registration completes, on the **"Overview"** page, copy and save:

```
Application (client) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Directory (tenant) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

**Important**: You will need these values later.

---

## 2. Configure API Permissions

### Step 2-1: Create Client Secret

1. Click **"Certificates & secrets"** → **"+ New client secret"**

2. **Settings**:
   - **Description**: `SalesSupportAgent Secret`
   - **Expires**: **24 months** (recommended) or **Custom**

3. Click **"Add"**

4. **Copy the "Value"** (⚠️ shown only once):
   ```
   Client Secret: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```

**Security Note**:
- Store secret in a secure location
- Add to `.gitignore` to prevent Git commit
- Use Azure Key Vault or Managed Identity in production

---

### Step 2-2: Add Microsoft Graph API Permissions

1. Click **"API permissions"** → **"+ Add a permission"**

2. Select **"Microsoft Graph"** → **"Application permissions"**

3. Search and add the following permissions:

#### Required Permissions

| Permission | Purpose | Priority |
|-----------|---------|:--------:|
| **Mail.Read** | Outlook email search | ✅ Required |
| **Calendars.Read** | Calendar event search | ✅ Required |
| **Files.Read.All** | SharePoint file access | ✅ Required |
| **Sites.Read.All** | SharePoint sites & Search API | ✅ Required |
| **ChannelMessage.Read.All** | Teams message search | ✅ Required |
| **Team.ReadBasic.All** | Teams basic information | ✅ Required |

#### Optional Permissions

| Permission | Purpose | Priority |
|-----------|---------|:--------:|
| **User.Read.All** | Get user information | ⚪ Optional |
| **Group.Read.All** | Get group information | ⚪ Optional |

4. Click **"Add permissions"**

---

### Step 2-3: Grant Admin Consent ⚠️

**Most Critical Step**: The application won't work without this

1. Click **"Grant admin consent for {organization}"** button
2. Click **"Yes"** in confirmation dialog
3. Verify all permissions show **"✓ Granted for {organization}"**

**Verification Method**:
- All items in "Status" column show green checkmark
- Displays "Granted for {organization}"

---

## 3. Local Development Environment Configuration

### Method A: appsettings.json (Simple)

Edit `SalesSupportAgent/appsettings.json`:

```json
{
  "M365": {
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "UseManagedIdentity": false
  }
}
```

**Note**: Do not commit secrets to Git

---

### Method B: Environment Variables (Recommended)

Manage secrets via environment variables:

**macOS / Linux**:
```bash
export M365__TenantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
export M365__ClientSecret="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
export M365__UseManagedIdentity=false

# Persist by adding to .zshrc or .bashrc
echo 'export M365__TenantId="your-tenant-id"' >> ~/.zshrc
```

**Windows PowerShell**:
```powershell
$env:M365__TenantId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
$env:M365__ClientId="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
$env:M365__ClientSecret="xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
$env:M365__UseManagedIdentity="false"

# Persist (user environment variable)
[System.Environment]::SetEnvironmentVariable('M365__TenantId', 'your-tenant-id', 'User')
```

---

### Method C: User Secrets (.NET Recommended)

```bash
cd /path/to/SalesSupportAgent

# Initialize User Secrets
dotnet user-secrets init

# Configure secrets
dotnet user-secrets set "M365:TenantId" "your-tenant-id"
dotnet user-secrets set "M365:ClientId" "your-client-id"
dotnet user-secrets set "M365:ClientSecret" "your-client-secret"

# Verify
dotnet user-secrets list
```

**Benefits**:
- Not committed to Git (stored in `%APPDATA%\Microsoft\UserSecrets`)
- Managed per project
- Secure for team development

---

## 4. Azure Production Environment Configuration

### 4.1. Managed Identity Overview

**Managed Identity** allows authentication in Azure environments **without secrets**.

| Benefit | Description |
|---------|-------------|
| 🔐 **No secret management** | Azure automatically manages credentials |
| 🔄 **Automatic rotation** | Credentials are periodically updated |
| 🛡️ **Zero leakage risk** | No secrets in configuration files |
| ✅ **Recommended approach** | Microsoft official security best practice |

---

### 4.2. Managed Identity Configuration on App Service

#### Step 1: Enable Managed Identity

1. **Azure Portal** → Select **App Service**
2. **"Identity"** → **"System assigned"** tab
3. Change **"Status"** to **"On"**
4. Click **"Save"**
5. **Object (principal) ID** will be displayed (copy and save)

#### Step 2: Grant Permissions to App Registration

1. **Microsoft Entra ID** → **App registrations** → Select created app
2. **"API permissions"** → Verify permissions are configured
3. **Note**: Managed Identity is a separate service principal from App Registration

#### Step 3: Grant Graph API Permissions (PowerShell)

```powershell
# Install Microsoft Graph PowerShell module
Install-Module Microsoft.Graph -Scope CurrentUser

# Connect
Connect-MgGraph -Scopes "Application.ReadWrite.All", "AppRoleAssignment.ReadWrite.All"

# Get Managed Identity object ID (ID confirmed in App Service)
$managedIdentityId = "your-managed-identity-object-id"

# Get Microsoft Graph service principal ID
$graphServicePrincipal = Get-MgServicePrincipal -Filter "displayName eq 'Microsoft Graph'"

# Get required App Roles
$mailReadRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Mail.Read"}
$calendarsReadRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Calendars.Read"}
$filesReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Files.Read.All"}
$sitesReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Sites.Read.All"}
$channelMessageReadAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "ChannelMessage.Read.All"}
$teamReadBasicAllRole = $graphServicePrincipal.AppRoles | Where-Object {$_.Value -eq "Team.ReadBasic.All"}

# Assign App Roles
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $mailReadRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $calendarsReadRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $filesReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $sitesReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $channelMessageReadAllRole.Id
New-MgServicePrincipalAppRoleAssignment -ServicePrincipalId $managedIdentityId -PrincipalId $managedIdentityId -ResourceId $graphServicePrincipal.Id -AppRoleId $teamReadBasicAllRole.Id
```

#### Step 4: Application Configuration

**App Service Settings**:
```json
{
  "M365": {
    "ClientId": "app-registration-client-id",
    "UseManagedIdentity": true
  }
}
```

**Note**: `TenantId` and `ClientSecret` are not required

---

### 4.3. Managed Identity Configuration on Container Apps

Managed Identity is also available on Container Apps:

```bash
# Enable Managed Identity on Container Apps
az containerapp identity assign \
  --name your-container-app \
  --resource-group your-resource-group \
  --system-assigned

# Grant Graph API permissions using the output principalId (see PowerShell script above)
```

---

### 4.4. Azure Key Vault Integration (Optional)

Advanced method for managing secrets with Key Vault:

#### Step 1: Save Secret to Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name salesagent-vault \
  --resource-group your-resource-group \
  --location eastus

# Save secret
az keyvault secret set \
  --vault-name salesagent-vault \
  --name M365ClientSecret \
  --value "your-client-secret"
```

#### Step 2: Grant Access Permission to App Service

```bash
# Grant Key Vault access permission to App Service Managed Identity
az keyvault set-policy \
  --name salesagent-vault \
  --object-id <app-service-managed-identity-id> \
  --secret-permissions get list
```

#### Step 3: Reference in appsettings.json

```json
{
  "M365": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://salesagent-vault.vault.azure.net/secrets/M365ClientSecret/)"
  }
}
```

---

## 5. Security Best Practices

### ✅ Recommendations

| Item | Local Development | Azure Production |
|------|------------------|------------------|
| **Auth Method** | ClientSecretCredential | Managed Identity |
| **Secret Management** | User Secrets / Environment Variables | Key Vault / Managed Identity |
| **Permissions** | Minimal (Read-only) | Minimal (Read-only) |
| **Rotation** | Every 6 months | Automatic (Managed Identity) |
| **Auditing** | Local logs | Application Insights + Audit Logs |

### 🔐 Secret Management Do's and Don'ts

#### ✅ Do (Recommended)

- ✅ Use User Secrets, environment variables, Key Vault
- ✅ Add `appsettings.json` to `.gitignore`
- ✅ Rotate secrets regularly (3-6 months)
- ✅ Use Managed Identity in production
- ✅ Time-limited secrets (24 months or less)

#### ❌ Don't (Prohibited)

- ❌ Write secrets directly in appsettings.json and commit to Git
- ❌ Hardcode secrets in code
- ❌ Use unlimited-duration secrets
- ❌ Use ClientSecret in production (prefer Managed Identity)
- ❌ Reuse same secret across multiple environments

---

### 🛡️ Minimizing Permissions

**Principle**: Grant only necessary permissions

| ❌ Excessive Permissions | ✅ Appropriate Permissions |
|------------------------|--------------------------|
| `Mail.ReadWrite` | `Mail.Read` |
| `Files.ReadWrite.All` | `Files.Read.All` |
| `Sites.FullControl.All` | `Sites.Read.All` |

**Reasons**:
- Minimize security risks
- Comply with compliance requirements
- Easy to explain during audits

---

## 6. Verification

### 6.1. Local Environment Verification

```bash
# Start application
cd /path/to/SalesSupportAgent
dotnet run

# Health check in another terminal
curl https://localhost:5192/health -k

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"..."}
```

### 6.2. Graph API Connection Test

```bash
# Execute sales summary API (internally calls Graph API)
curl -X POST https://localhost:5192/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"Search this week'\''s emails"}' \
  -k

# On success: Email information is returned
# On failure: Check error message (see Troubleshooting below)
```

### 6.3. Diagnostic Endpoints

```bash
# User profile retrieval test
curl https://localhost:5192/api/test/graph/profile -k

# Email retrieval test
curl "https://localhost:5192/api/test/graph/emails/raw?top=5" -k
```

---

## 7. Troubleshooting

### Error: "Unauthorized (401)"

**Symptom**:
```json
{
  "error": {
    "code": "InvalidAuthenticationToken",
    "message": "Access token validation failure"
  }
}
```

**Causes and Solutions**:

| Cause | Verification Method | Solution |
|-------|-------------------|----------|
| TenantId is incorrect | Verify in Azure Portal | Fix TenantId |
| ClientId is incorrect | Verify in Azure Portal | Fix ClientId |
| ClientSecret is incorrect/expired | Create new secret | Update to new secret |

---

### Error: "Forbidden (403)"

**Symptom**:
```json
{
  "error": {
    "code": "Authorization_RequestDenied",
    "message": "Insufficient privileges to complete the operation"
  }
}
```

**Causes and Solutions**:

1. **Admin consent not granted**:
   ```
   Azure Portal → App registrations → API permissions
   → Click "Grant admin consent"
   ```

2. **Required permissions missing**:
   ```
   Add required permissions → Re-grant admin consent
   ```

3. **UserId is incorrect**:
   ```json
   {
     "M365": {
       "UserId": "correct-user-id"  // Verify in Graph Explorer
     }
   }
   ```

---

### Error: "Managed Identity not working"

**Symptom**:
```
ManagedIdentityCredential authentication failed: 
No managed identity endpoint found
```

**Cause**: Attempting to use Managed Identity in local environment

**Solution**:
```json
{
  "M365": {
    "UseManagedIdentity": false  // Set to false for local
  }
}
```

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Initial setup
- [Troubleshooting](TROUBLESHOOTING.md) - Detailed error handling
- [Architecture](ARCHITECTURE.md) - Authentication flow details
- [Deployment Azure](DEPLOYMENT-AZURE.md) - Production environment setup

---

## 🔗 External Links

- [Microsoft Graph API Documentation](https://learn.microsoft.com/graph/)
- [Application-only Authentication](https://learn.microsoft.com/graph/auth-v2-service)
- [Managed Identity](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/overview)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
- [Graph Permissions Reference](https://learn.microsoft.com/graph/permissions-reference)

---

Once authentication configuration is complete, move on to [Sample Data Creation](SAMPLE-DATA.md) to generate test data! 🚀
