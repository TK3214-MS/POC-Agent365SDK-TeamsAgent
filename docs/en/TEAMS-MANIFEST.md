# Teams Bot Manifest Configuration Guide

> **Language**: [🇯🇵 日本語](../TEAMS-MANIFEST.md) | 🇬🇧 English

## 📋 Overview

To run a Bot in Microsoft Teams, you need a **Teams app manifest**. This guide explains the procedures for registering the Sales Support Agent in Teams.

---

## 🎯 What is Teams App Manifest?

Teams app manifest (`manifest.json`) is a JSON file that defines the configuration of a Teams app.

### Main Elements

- **Basic Info**: App name, description, version
- **Bot Settings**: Bot ID, scopes, commands
- **Icons**: Color icon, outline icon
- **Permissions**: Permissions used within Teams

### Package Structure

Teams app package is a ZIP file with the following structure:

```
SalesSupportAgent.zip
├── manifest.json       # App manifest
├── color.png          # Color icon (192x192)
└── outline.png        # Outline icon (32x32)
```

---

## 🚀 Setup Procedure

### Prerequisites

- ✅ Azure Bot Service created (refer to [README.md](../README.md#3-teams-bot-connection))
- ✅ Bot App ID and Password obtained
- ✅ Messaging endpoint configured with Dev Tunnel or ngrok

---

### Step 1: Create Manifest Files

Create a `TeamsAppManifest` folder in project root and place required files.

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent
mkdir -p TeamsAppManifest
cd TeamsAppManifest
```

This project includes the following files:

- [`manifest.json`](../TeamsAppManifest/manifest.json) - App manifest
- [`color.png`](../TeamsAppManifest/color.png) - Color icon (192x192)
- [`outline.png`](../TeamsAppManifest/outline.png) - Outline icon (32x32)

---

### Step 2: Edit manifest.json

#### 2-1. Update Required Fields

Open [`TeamsAppManifest/manifest.json`](../TeamsAppManifest/manifest.json) and update:

```json
{
  "$schema": "https://developer.microsoft.com/en-us/json-schemas/teams/v1.17/MicrosoftTeams.schema.json",
  "manifestVersion": "1.17",
  "version": "1.0.0",
  "id": "YOUR-BOT-APP-ID-HERE",  // ← Change to Azure Bot App ID
  "developer": {
    "name": "Your Company Name",  // ← Change to company name
    "websiteUrl": "https://www.example.com",  // ← Company URL
    "privacyUrl": "https://www.example.com/privacy",  // ← Privacy policy
    "termsOfUseUrl": "https://www.example.com/terms"  // ← Terms of use
  },
  "name": {
    "short": "Sales Support Agent",
    "full": "Agent 365 SDK Sales Support Agent"
  },
  "description": {
    "short": "Auto-generate sales summaries",
    "full": "Collects sales-related information from Microsoft 365 data and creates easy-to-understand summaries. Integrates information from Outlook, SharePoint, and Teams to support sales activities."
  },
  "bots": [
    {
      "botId": "YOUR-BOT-APP-ID-HERE",  // ← Change to Azure Bot App ID
      "scopes": [
        "personal",
        "team",
        "groupchat"
      ],
      "commandLists": [
        {
          "commands": [
            {
              "title": "This week's sales summary",
              "description": "Summarize this week's sales information"
            },
            {
              "title": "Help",
              "description": "Show usage instructions"
            }
          ]
        }
      ]
    }
  ]
}
```

#### 2-2. Required Changes

| Field | Description | How to Obtain |
|-------|-------------|--------------|
| `id` | Unique app ID | Azure Bot **App ID** |
| `bots[0].botId` | Bot ID | Azure Bot **App ID** (same as `id`) |
| `developer.name` | Developer/Company name | Any |
| `developer.websiteUrl` | Company URL | Any (valid URL) |

⚠️ **Important**: `id` and `bots[0].botId` must match **Azure Bot App ID**.

---

### Step 3: Prepare Icons

#### 3-1. Color Icon (color.png)

- **Size**: 192 x 192 pixels
- **Format**: PNG
- **Use**: Teams app store, chat list

#### 3-2. Outline Icon (outline.png)

- **Size**: 32 x 32 pixels
- **Format**: PNG (transparent background)
- **Color**: White outline only
- **Use**: Teams sidebar

---

### Step 4: Create App Package

#### 4-1. Create ZIP File

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/TeamsAppManifest
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

**Output**: `SalesSupportAgent.zip`

---

### Step 5: Install in Teams

#### Method A: Using Teams Developer Portal (Recommended)

1. Access [Teams Developer Portal](https://dev.teams.microsoft.com/apps)
2. Click **"Import an existing app"**
3. Upload `SalesSupportAgent.zip`
4. Review/edit app details
5. Click **"Preview in Teams"** to open in Teams
6. Click **"Add"** to install

#### Method B: Direct Upload from Teams

1. Open Teams
2. Click **"Apps"** in left menu
3. **"Manage apps"** → **"Upload a custom app"**
4. Select **"Upload for my organization"**
5. Select `SalesSupportAgent.zip`
6. Click **"Add"**

⚠️ **Note**: Organization IT admin must allow custom app uploads.

---

### Step 6: Verification

#### 6-1. Start Bot Conversation

1. Search for **"Sales Support Agent"** in Teams
2. Click Bot to open personal chat
3. Send message:

```
Hello
```

**Expected Response**: Welcome message (Adaptive Card) displayed

#### 6-2. Test Sales Summary

```
Show this week's sales summary
```

**Expected Response**: Sales summary (Adaptive Card) displayed

---

## ⚠️ Troubleshooting

### Error 1: "Manifest parsing has failed"

**Cause**: Invalid JSON format in manifest.json

**Solution**:
- Validate with JSON validator: [JSONLint](https://jsonlint.com/)
- Check comma positions, quotes are correct

### Error 2: "Invalid Bot ID"

**Cause**: `botId` doesn't match Azure Bot App ID

**Solution**:
1. Azure Portal → Bot Service → Configuration → Copy Microsoft App ID
2. Paste into `manifest.json` `id` and `bots[0].botId`

### Error 3: "Icon size is invalid"

**Cause**: Icon size differs from specification

**Solution**:
```bash
# Check size
file color.png outline.png

# Resize (ImageMagick)
convert color.png -resize 192x192 color.png
convert outline.png -resize 32x32 outline.png
```

### Error 4: "Bot not responding"

**Cause**: Messaging endpoint is incorrect

**Solution**:
1. Verify Dev Tunnel / ngrok is running
2. Check Azure Bot Service configuration:
   ```
   https://your-tunnel-url/api/messages
   ```
3. Verify local application is running:
   ```bash
   dotnet run
   ```

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Setup procedures
- [Dev Tunnel Setup](DEV-TUNNEL-SETUP.md) - Tunnel configuration
- [Architecture](ARCHITECTURE.md) - System design

---

**Successfully register the Sales Support Agent in Teams!** 🚀
