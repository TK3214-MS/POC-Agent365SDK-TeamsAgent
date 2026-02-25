# Teams Bot Manifest Configuration Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../TEAMS-MANIFEST.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](TEAMS-MANIFEST.md)

## 📋 Overview

To run a Bot in Microsoft Teams, you need a **Teams App Manifest**. This guide explains the steps to register the Sales Support Agent in Teams.

---

## 🎯 What is a Teams App Manifest

The Teams App Manifest (`manifest.json`) is a JSON file that defines the configuration of a Teams app.

### Key Elements

- **Basic Information**: App name, description, version
- **Bot Configuration**: Bot ID, scopes, commands
- **Icons**: Color icon, outline icon
- **Permissions**: Permissions used within Teams

### Package Structure

The Teams app package is a ZIP file with the following structure:

```
SalesSupportAgent.zip
├── manifest.json       # App manifest
├── color.png          # Color icon (192x192)
└── outline.png        # Outline icon (32x32)
```

---

## 🚀 Setup Procedure

### Prerequisites

- ✅ Azure Bot Service created (see [README.md](README.md#3-teams-bot-connection))
- ✅ Bot App ID and Password obtained
- ✅ Messaging endpoint configured via Dev Tunnel or ngrok

---

### Step 1: Create Manifest File

Create a `TeamsAppManifest` folder in the project root directory and place the required files.

```bash
cd /path/to/POC-Agent365SDK-TeamsAgent
mkdir -p TeamsAppManifest
cd TeamsAppManifest
```

The following files are already prepared in this project:

- [`manifest.json`](../../TeamsAppManifest/manifest.json) - App manifest
- [`color.png`](../../TeamsAppManifest/color.png) - Color icon (192x192)
- [`outline.png`](../../TeamsAppManifest/outline.png) - Outline icon (32x32)

---

### Step 2: Edit manifest.json

#### 2-1. Update Required Fields

Open [`TeamsAppManifest/manifest.json`](../../TeamsAppManifest/manifest.json) and update the following fields:

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
      "supportsFiles": false,
      "isNotificationOnly": false,
      "commandLists": [
        {
          "scopes": [
            "personal",
            "team",
            "groupchat"
          ],
          "commands": [
            {
              "title": "This week's sales summary",
              "description": "Summarize this week's sales-related information"
            },
            {
              "title": "Help",
              "description": "Display usage instructions"
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
|-------|-------------|---------------|
| `id` | App unique ID | Azure Bot **App ID** |
| `bots[0].botId` | Bot ID | Azure Bot **App ID** (same as `id`) |
| `developer.name` | Developer/Company name | Your choice |
| `developer.websiteUrl` | Company URL | Your choice (valid URL) |
| `developer.privacyUrl` | Privacy policy | Your choice (valid URL) |
| `developer.termsOfUseUrl` | Terms of use | Your choice (valid URL) |

⚠️ **Important**: `id` and `bots[0].botId` must match the **Azure Bot App ID**.

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

#### 3-3. How to Create Icons

**Option 1: Use Default Icons**

This project includes sample icons. You can use them as-is for testing.

**Option 2: Create Custom Icons**

1. Use online tools: [Canva](https://www.canva.com/), [Figma](https://www.figma.com/)
2. Create at 192x192 → Save as `color.png`
3. Create white outline version at 32x32 → Save as `outline.png`

**Sample Commands (using ImageMagick)**:
```bash
# Resize color icon
convert original.png -resize 192x192 color.png

# Create outline icon
convert original.png -resize 32x32 -colorspace Gray -threshold 50% outline.png
```

---

### Step 4: Create App Package

#### 4-1. Create ZIP File

```bash
cd /path/to/POC-Agent365SDK-TeamsAgent/TeamsAppManifest
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

**Output**: `SalesSupportAgent.zip`

#### 4-2. Validate Package

**Online Validation Tool**:
[App Validation Tool](https://dev.teams.microsoft.com/appvalidation.html)

1. Upload ZIP file
2. Check for errors
3. Fix errors if any and re-create ZIP

---

### Step 5: Install to Teams

#### Method A: Use Teams Developer Portal (Recommended)

1. Access [Teams Developer Portal](https://dev.teams.microsoft.com/apps)
2. Click **"Import an existing app"**
3. Upload `SalesSupportAgent.zip`
4. Review and edit app details
5. Click **"Preview in Teams"** to open in Teams
6. Click **"Add"** to install

#### Method B: Direct Upload from Teams

1. Open Teams
2. Click **"Apps"** in the left menu
3. **"Manage your apps"** → **"Upload a custom app"**
4. Select **"Upload a custom app for your organization"**
5. Select `SalesSupportAgent.zip`
6. Click **"Add"**

⚠️ **Note**: Your organization's IT administrator must allow custom app uploads.

---

### Step 6: Verify Operation

#### 6-1. Start Conversation with Bot

1. Search for **"Sales Support Agent"** in Teams
2. Click on the Bot to open personal chat
3. Send a message:

```
Hello
```

**Expected Response**: Welcome message (Adaptive Card) is displayed

#### 6-2. Test Sales Summary

```
Tell me this week's sales summary
```

**Expected Response**: Sales summary (Adaptive Card) is displayed

#### 6-3. Test by Adding to a Team

1. Select a Team in Teams
2. **"..."** → **"Manage apps"**
3. Add **"Sales Support Agent"**
4. Mention **@Sales Support Agent** in a channel

```
@Sales Support Agent Tell me this week's sales summary
```

---

## 🔧 Advanced Settings

### Customize Bot Commands

Edit `commandLists` in `manifest.json` to add frequently used commands:

```json
"commands": [
  {
    "title": "This week's sales summary",
    "description": "Summarize this week's sales-related information"
  },
  {
    "title": "Last week's sales summary",
    "description": "Summarize last week's sales-related information"
  },
  {
    "title": "Search by customer name",
    "description": "Search for information about a specific customer"
  },
  {
    "title": "Help",
    "description": "Display usage instructions"
  }
]
```

### Scope Configuration

You can restrict where the Bot operates:

```json
"scopes": [
  "personal",      // Personal chat
  "team",          // Team channels
  "groupchat"      // Group chat
]
```

Remove unnecessary scopes to improve security.

### Add Permissions

If additional Microsoft Graph API permissions are needed:

```json
"webApplicationInfo": {
  "id": "YOUR-BOT-APP-ID-HERE",
  "resource": "https://graph.microsoft.com"
}
```

---

## 📦 Manifest Update Procedure

When updating the app:

### 1. Update Version

```json
{
  "version": "1.0.1"  // Increment version
}
```

### 2. Re-create ZIP File

```bash
cd TeamsAppManifest
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

### 3. Update in Teams Developer Portal

1. Access [Teams Developer Portal](https://dev.teams.microsoft.com/apps)
2. Select existing app
3. Click **"Update app package"**
4. Upload new ZIP file

**Or update directly in Teams**:
1. Teams → **"Apps"** → **"Manage your apps"**
2. Delete existing app
3. Upload new ZIP file

---

## ⚠️ Troubleshooting

### Error 1: "Manifest parsing has failed"

**Cause**: Invalid JSON format in manifest.json

**Solution**:
- Validate with JSON validator: [JSONLint](https://jsonlint.com/)
- Check comma positions and quotation marks

### Error 2: "Invalid Bot ID"

**Cause**: `botId` doesn't match Azure Bot App ID

**Solution**:
1. Azure Portal → Bot Service → Configuration → Copy Microsoft App ID
2. Paste into `manifest.json` `id` and `bots[0].botId`

### Error 3: "Icon size is invalid"

**Cause**: Icon size doesn't match specifications

**Solution**:
```bash
# Check sizes
file color.png outline.png

# Resize (ImageMagick)
convert color.png -resize 192x192 color.png
convert outline.png -resize 32x32 outline.png
```

### Error 4: "Bot not responding"

**Cause**: Incorrect messaging endpoint

**Solution**:
1. Verify Dev Tunnel / ngrok is running
2. Check Azure Bot Service settings:
   ```
   https://your-tunnel-url/api/messages
   ```
3. Verify local application is running:
   ```bash
   dotnet run
   ```

### Error 5: "Custom app upload is blocked"

**Cause**: Custom app upload is disabled by organization policy

**Solution**:
- Microsoft 365 Admin Center → Teams → Teams Apps → Setup Policies
- Enable **"Allow uploading custom apps"**
- Or contact IT administrator

---

## 🏢 Organization-wide Deployment

### Approval in Microsoft Teams Admin Center

1. Access [Microsoft Teams Admin Center](https://admin.teams.microsoft.com/)
2. **"Teams apps"** → **"Manage apps"**
3. **"+ Upload new app"** → Upload ZIP file
4. Click **"Publish"**

This allows all users in the organization to install the app from the app store.

---

## 📊 Manifest Schema Versions

This project uses **v1.17**, but it can be changed as needed:

| Version | Release Date | Key Features |
|---------|-------------|--------------|
| **1.17** | November 2023 | Latest feature support |
| 1.16 | May 2023 | Adaptive Cards enhancements |
| 1.13 | June 2022 | Bot command extensions |

**Reference**: [Teams Manifest Schema](https://learn.microsoft.com/microsoftteams/platform/resources/schema/manifest-schema)

---

## 🔗 Reference Links

- [Teams App Manifest](https://learn.microsoft.com/microsoftteams/platform/resources/schema/manifest-schema)
- [Teams Developer Portal](https://dev.teams.microsoft.com/)
- [Bot Framework Documentation](https://learn.microsoft.com/azure/bot-service/)
- [Teams App Validation](https://learn.microsoft.com/microsoftteams/platform/concepts/deploy-and-publish/appsource/prepare/teams-store-validation-guidelines)
- [Adaptive Cards Designer](https://adaptivecards.io/designer/)

---

## 📝 Checklist

Verification items before creating the manifest:

- [ ] Azure Bot Service created
- [ ] Bot App ID and Password obtained
- [ ] Messaging endpoint configured via Dev Tunnel / ngrok
- [ ] Changed `id` and `botId` in `manifest.json` to Bot App ID
- [ ] Icons prepared (192x192, 32x32)
- [ ] ZIP file created
- [ ] Verified with manifest validation tool
- [ ] Uploaded to Teams and operation confirmed

Once everything is complete, you can begin full-scale operations combined with [Authentication Setup](AUTHENTICATION.md)!
