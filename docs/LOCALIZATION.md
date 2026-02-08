# 多言語対応ガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../LOCALIZATION.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/LOCALIZATION.md)

## 📋 概要

営業支援エージェントは日本語と英語の2言語に対応しています。このガイドでは、言語設定と拡張方法について説明します。

---

## 🌍 サポート言語

- **日本語 (ja)** - デフォルト
- **English (en)**

---

## ⚙️ 言語設定

### 方法 1: appsettings.json で設定

[appsettings.json](../SalesSupportAgent/appsettings.json) を編集:

```json
{
  "Localization": {
    "DefaultLanguage": "ja"  // または "en"
  }
}
```

### 方法 2: 環境変数で設定

```bash
# 日本語
export Localization__DefaultLanguage=ja

# English
export Localization__DefaultLanguage=en
```

### 方法 3: .env ファイルで設定

```bash
Localization__DefaultLanguage=en
```

---

## 🔤 文字列リソース

すべてのローカライズされた文字列は [`LocalizedStrings.cs`](../SalesSupportAgent/Resources/LocalizedStrings.cs) で管理されています。

### 構造

```csharp
public static class LocalizedStrings
{
    public static class Japanese { /* 日本語文字列 */ }
    public static class English { /* English strings */ }
    public static class Current { /* 現在の言語 */ }
}
```

### 使用例

```csharp
// 現在の言語設定に基づいて文字列を取得
var welcomeTitle = LocalizedStrings.Current.WelcomeTitle;
var errorMessage = LocalizedStrings.Current.M365NotConfigured;
```

---

## 📝 ローカライズされた文字列一覧

### ウェルカムメッセージ

| キー | 日本語 | English |
|-----|-------|---------|
| `WelcomeTitle` | 👋 こんにちは！営業支援エージェントです | 👋 Hello! I'm your Sales Support Agent |
| `WelcomeContent` | できること、使い方の説明 | What I can do, How to use |

### エラーメッセージ

| キー | 日本語 | English |
|-----|-------|---------|
| `ErrorOccurred` | エラーが発生しました | An error occurred |
| `ErrorDetails` | エラー内容と対処方法 | Error Details and Solution |
| `M365NotConfigured` | Microsoft 365 が設定されていません | Microsoft 365 is not configured |

### サマリーレポート

| キー | 日本語 | English |
|-----|-------|---------|
| `SalesSummaryTitle` | 📊 営業支援エージェント - サマリーレポート | 📊 Sales Support Agent - Summary Report |
| `PoweredBy` | 🤖 powered by Agent 365 SDK | 🤖 powered by Agent 365 SDK |
| `ProcessingTime` | ⚡ 処理時間: {0}ms | ⚡ Processing time: {0}ms |

---

## 🔧 新しい言語の追加

### ステップ 1: LocalizedStrings.cs に言語クラスを追加

```csharp
public static class French
{
    public const string WelcomeTitle = "👋 Bonjour! Je suis votre agent de support des ventes";
    public const string WelcomeContent = "...";
    // 他の文字列を追加
}
```

### ステップ 2: Current クラスに言語判定を追加

```csharp
public static string WelcomeTitle => _currentLanguage switch
{
    "en" => English.WelcomeTitle,
    "fr" => French.WelcomeTitle,
    _ => Japanese.WelcomeTitle  // デフォルト
};
```

### ステップ 3: 設定ファイルで言語を指定

```json
{
  "Localization": {
    "DefaultLanguage": "fr"
  }
}
```

---

## 🎯 実装箇所

多言語対応は以下のコンポーネントで実装されています：

### Bot

- [`TeamsBot.cs`](../SalesSupportAgent/Bot/TeamsBot.cs)
  - ウェルカムメッセージ
  - エラーメッセージ
  - 処理時間表示

- [`AdaptiveCardHelper.cs`](../SalesSupportAgent/Bot/AdaptiveCardHelper.cs)
  - サマリーカードのフッター

### MCP Tools

- [`OutlookEmailTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/OutlookEmailTool.cs)
- [`OutlookCalendarTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/OutlookCalendarTool.cs)
- [`SharePointTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/SharePointTool.cs)
- [`TeamsMessageTool.cs`](../SalesSupportAgent/Services/MCP/McpTools/TeamsMessageTool.cs)
  - M365 未設定エラーメッセージ

---

## 🧪 テスト

### 日本語でテスト

```bash
# appsettings.json または環境変数で ja に設定
export Localization__DefaultLanguage=ja

# アプリケーションを起動
dotnet run

# Teams でメッセージを送信
こんにちは
```

**期待される応答**: 日本語のウェルカムメッセージ

### 英語でテスト

```bash
# en に設定
export Localization__DefaultLanguage=en

# アプリケーションを起動
dotnet run

# Teams でメッセージを送信
Hello
```

**期待される応答**: English welcome message

---

## 🔄 動的な言語切り替え（将来の拡張）

現在は起動時に言語を設定しますが、将来的にはユーザーごとの言語設定や動的な切り替えを実装できます：

### 実装案

```csharp
// ユーザーの Teams ロケールを取得
var userLocale = turnContext.Activity.Locale; // "ja-JP", "en-US" など
var language = userLocale?.StartsWith("ja") == true ? "ja" : "en";

// 一時的に言語を設定
LocalizedStrings.Current.SetLanguage(language);

// 応答を返す
await turnContext.SendActivityAsync(LocalizedStrings.Current.WelcomeTitle);
```

### Azure Portal での設定

ユーザープロファイルから言語設定を取得することも可能です。

---

## 📚 ベストプラクティス

### 1. 文字列のハードコーディングを避ける

❌ **悪い例**:
```csharp
await turnContext.SendActivityAsync("エラーが発生しました");
```

✅ **良い例**:
```csharp
await turnContext.SendActivityAsync(LocalizedStrings.Current.ErrorOccurred);
```

### 2. 文字列フォーマットを使用

❌ **悪い例**:
```csharp
var message = "処理時間: " + time + "ms";
```

✅ **良い例**:
```csharp
var message = string.Format(LocalizedStrings.Current.ProcessingTime, time);
```

### 3. 文化依存の日時フォーマット

```csharp
// 現在の文化に基づいて日時をフォーマット
var formattedDate = DateTime.Now.ToString("d", CultureInfo.CurrentCulture);
```

### 4. 一貫性のある翻訳

専門用語は統一します：
- Agent → エージェント（一貫して使用）
- Summary → サマリ（または サマリー、どちらかに統一）

---

## ⚠️ 注意事項

### 1. LLM の応答言語

現在、エージェントのシステムプロンプトは日本語で記述されているため、LLM の応答も日本語になります。

**英語対応するには**:

[`SalesAgent.cs`](../SalesSupportAgent/Services/Agent/SalesAgent.cs) のシステムプロンプトを動的に変更:

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

### 2. Adaptive Cards の多言語対応

Adaptive Cards 内のテキストも LocalizedStrings を使用して動的に生成する必要があります。

### 3. パフォーマンス

文字列リソースは定数として定義されているため、パフォーマンスへの影響はほとんどありません。

---

## 🔗 参考リンク

- [.NET のローカライゼーション](https://learn.microsoft.com/ja-jp/dotnet/core/extensions/localization)
- [ASP.NET Core のグローバリゼーションとローカライゼーション](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/localization)
- [CultureInfo クラス](https://learn.microsoft.com/ja-jp/dotnet/api/system.globalization.cultureinfo)

---

## 📝 まとめ

多言語対応により：

- ✅ グローバルなユーザーベースに対応
- ✅ メンテナンスが容易
- ✅ 一貫したユーザー体験
- ✅ 将来の言語追加が簡単

言語設定を変更するだけで、アプリケーション全体の表示言語が切り替わります！
