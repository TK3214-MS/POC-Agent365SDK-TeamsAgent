# Localization Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../LOCALIZATION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](LOCALIZATION.md)

## 📋 Overview

The Sales Support Agent supports two languages: Japanese and English. This guide explains language settings and extension methods.

---

## 🌍 Supported Languages

- **Japanese (ja)** - Default
- **English (en)**

---

## ⚙️ Language Configuration

### Method 1: Configure in appsettings.json

Edit [appsettings.json](../SalesSupportAgent/appsettings.json):

```json
{
  "Localization": {
    "DefaultLanguage": "ja"  // or "en"
  }
}
```

### Method 2: Configure with Environment Variables

```bash
# Japanese
export Localization__DefaultLanguage=ja

# English
export Localization__DefaultLanguage=en
```

### Method 3: Configure in .env File

```bash
Localization__DefaultLanguage=en
```

---

## 🔤 String Resources

All localized strings are managed in [`LocalizedStrings.cs`](../SalesSupportAgent/Resources/LocalizedStrings.cs).

### Structure

```csharp
public static class LocalizedStrings
{
    public static class Japanese { /* Japanese strings */ }
    public static class English { /* English strings */ }
    public static class Current { /* Current language */ }
}
```

### Usage Example

```csharp
// Get string based on current language setting
var welcomeTitle = LocalizedStrings.Current.WelcomeTitle;
var errorMessage = LocalizedStrings.Current.M365NotConfigured;
```

---

## 📝 Localized Strings List

### Welcome Messages

| Key | Japanese | English |
|-----|----------|---------|
| `WelcomeTitle` | 👋 こんにちは！営業支援エージェントです | 👋 Hello! I'm your Sales Support Agent |
| `WelcomeContent` | What I can do, how to use | What I can do, How to use |

### Error Messages

| Key | Japanese | English |
|-----|----------|---------|
| `ErrorOccurred` | エラーが発生しました | An error occurred |
| `ErrorDetails` | Error details and solution | Error Details and Solution |
| `M365NotConfigured` | Microsoft 365 が設定されていません | Microsoft 365 is not configured |

### Summary Reports

| Key | Japanese | English |
|-----|----------|---------|
| `SalesSummaryTitle` | 📊 営業支援エージェント - サマリーレポート | 📊 Sales Support Agent - Summary Report |
| `PoweredBy` | 🤖 powered by Agent 365 SDK | 🤖 powered by Agent 365 SDK |
| `ProcessingTime` | ⚡ 処理時間: {0}ms | ⚡ Processing time: {0}ms |

---

## 🔧 Adding New Languages

### Step 1: Add Language Class to LocalizedStrings.cs

```csharp
public static class French
{
    public const string WelcomeTitle = "👋 Bonjour! Je suis votre agent de support des ventes";
    public const string WelcomeContent = "...";
    // Add other strings
}
```

### Step 2: Add Language Detection to Current Class

```csharp
public static string WelcomeTitle => _currentLanguage switch
{
    "en" => English.WelcomeTitle,
    "fr" => French.WelcomeTitle,
    _ => Japanese.WelcomeTitle  // Default
};
```

### Step 3: Specify Language in Configuration File

```json
{
  "Localization": {
    "DefaultLanguage": "fr"
  }
}
```

---

## 🎯 Implementation Locations

Localization is implemented in the following components:

### Bot

- [`TeamsBot.cs`](../SalesSupportAgent/Bot/TeamsBot.cs)
  - Welcome messages
  - Error messages
  - Processing time display

- [`AdaptiveCardHelper.cs`](../SalesSupportAgent/Bot/AdaptiveCardHelper.cs)
  - Summary card footer

### MCP Tools

- [`OutlookEmailTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/OutlookEmailTool.cs)
- [`OutlookCalendarTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/OutlookCalendarTool.cs)
- [`SharePointTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/SharePointTool.cs)
- [`TeamsMessageTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/TeamsMessageTool.cs)
  - M365 not configured error messages

---

## 🧪 Testing

### Test in Japanese

```bash
# Set to ja in appsettings.json or environment variable
export Localization__DefaultLanguage=ja

# Start application
dotnet run

# Send message in Teams
こんにちは
```

**Expected Response**: Japanese welcome message

### Test in English

```bash
# Set to en
export Localization__DefaultLanguage=en

# Start application
dotnet run

# Send message in Teams
Hello
```

**Expected Response**: English welcome message

---

## 🔄 Dynamic Language Switching (Future Extension)

Currently language is set at startup, but future implementations could support per-user language settings or dynamic switching:

### Implementation Idea

```csharp
// Get user's Teams locale
var userLocale = turnContext.Activity.Locale; // "ja-JP", "en-US", etc.
var language = userLocale?.StartsWith("ja") == true ? "ja" : "en";

// Set language temporarily
LocalizedStrings.Current.SetLanguage(language);

// Return response
await turnContext.SendActivityAsync(LocalizedStrings.Current.WelcomeTitle);
```

### Configuration in Azure Portal

Language settings can also be retrieved from user profiles.

---

## 📚 Best Practices

### 1. Avoid Hardcoding Strings

❌ **Bad Example**:
```csharp
await turnContext.SendActivityAsync("An error occurred");
```

✅ **Good Example**:
```csharp
await turnContext.SendActivityAsync(LocalizedStrings.Current.ErrorOccurred);
```

### 2. Use String Formatting

❌ **Bad Example**:
```csharp
var message = "Processing time: " + time + "ms";
```

✅ **Good Example**:
```csharp
var message = string.Format(LocalizedStrings.Current.ProcessingTime, time);
```

### 3. Culture-Dependent Date/Time Formatting

```csharp
// Format date/time based on current culture
var formattedDate = DateTime.Now.ToString("d", CultureInfo.CurrentCulture);
```

### 4. Consistent Translation

Unify technical terms:
- Agent → エージェント (use consistently)
- Summary → サマリ (or サマリー, choose one)

---

## ⚠️ Notes

### 1. LLM Response Language

Currently, the agent's system prompt is written in Japanese, so LLM responses are also in Japanese.

**To support English**:

Dynamically change system prompt in [`SalesAgent.cs`](../SalesSupportAgent/Services/Agent/SalesAgent.cs):

```csharp
private string GetSystemPrompt()
{
    if (LocalizedStrings.Current._currentLanguage == "en")
    {
        return @"You are a sales support agent. 
Use the following tools to collect sales-related information from Microsoft 365...";
    }
    else
    {
        return @"あなたは営業支援エージェントです。
以下のツールを使用して、Microsoft 365 から商談関連情報を収集し...";
    }
}
```

### 2. Adaptive Cards Localization

Text in Adaptive Cards must also be dynamically generated using LocalizedStrings.

### 3. Performance

String resources are defined as constants, so there is minimal performance impact.

---

## 🔗 References

- [.NET Localization](https://learn.microsoft.com/dotnet/core/extensions/localization)
- [ASP.NET Core Globalization and Localization](https://learn.microsoft.com/aspnet/core/fundamentals/localization)
- [CultureInfo Class](https://learn.microsoft.com/dotnet/api/system.globalization.cultureinfo)

---

## 📝 Summary

Through localization:

- ✅ Support global user base
- ✅ Easy maintenance
- ✅ Consistent user experience
- ✅ Easy to add future languages

Simply change language setting to switch entire application display language!
