namespace SalesSupportAgent.Resources;

/// <summary>
/// ローカライズされた文字列リソース
/// </summary>
public static class LocalizedStrings
{
    public static class Japanese
    {
        // ウェルカムメッセージ
        public const string WelcomeTitle = "👋 こんにちは！営業支援エージェントです";
        public const string WelcomeContent = @"**できること:**
- 📧 Outlook メールから商談関連情報を収集
- 📅 カレンダーから商談予定を確認  
- 📁 SharePoint から提案書・見積書を検索
- 📢 Teams チャネルから商談関連の会話を抽出

**使い方:**
「今週の商談サマリを教えて」と話しかけてください。

**例:**
- 今週の商談サマリを教えて
- 先週の重要な商談を教えて
- 〇〇社に関する情報をまとめて

---
⚠️ 初回利用時は、管理者が Microsoft 365 と Bot の設定を完了している必要があります。";

        // エラーメッセージ
        public const string ErrorOccurred = "エラーが発生しました";
        public const string ErrorDetails = "**エラー内容:**\n{0}\n\n**対処方法:**\n- appsettings.json の設定を確認してください\n- ログファイルで詳細を確認してください\n- Microsoft 365 の権限設定を確認してください";
        
        // Agent サマリー
        public const string SalesSummaryTitle = "📊 営業支援エージェント - サマリーレポート";
        public const string PoweredBy = "🤖 powered by Agent 365 SDK";
        
        // 設定エラー
        public const string M365NotConfigured = "⚠️ Microsoft 365 が設定されていません。appsettings.json の M365 セクションを設定してください。";
        
        // 処理時間
        public const string ProcessingTime = "⚡ 処理時間: {0}ms";
        public const string LLMProviderInfo = "🤖 {0}";
    }

    public static class English
    {
        // Welcome messages
        public const string WelcomeTitle = "👋 Hello! I'm your Sales Support Agent";
        public const string WelcomeContent = @"**What I can do:**
- 📧 Collect sales-related information from Outlook emails
- 📅 Check sales meetings from Calendar  
- 📁 Search for proposals and quotes from SharePoint
- 📢 Extract sales-related conversations from Teams channels

**How to use:**
Just say ""Show me this week's sales summary""

**Examples:**
- Show me this week's sales summary
- Tell me about last week's important deals
- Summarize information about Company X

---
⚠️ Note: Microsoft 365 and Bot must be configured by administrator before first use.";

        // Error messages
        public const string ErrorOccurred = "An error occurred";
        public const string ErrorDetails = "**Error Details:**\n{0}\n\n**Solution:**\n- Check appsettings.json configuration\n- Review log files for details\n- Verify Microsoft 365 permissions";
        
        // Agent summary
        public const string SalesSummaryTitle = "📊 Sales Support Agent - Summary Report";
        public const string PoweredBy = "🤖 powered by Agent 365 SDK";
        
        // Configuration errors
        public const string M365NotConfigured = "⚠️ Microsoft 365 is not configured. Please configure the M365 section in appsettings.json.";
        
        // Processing time
        public const string ProcessingTime = "⚡ Processing time: {0}ms";
        public const string LLMProviderInfo = "🤖 {0}";
    }

    /// <summary>
    /// 現在の言語設定に基づいて文字列を取得
    /// </summary>
    public static class Current
    {
        private static string _currentLanguage = "ja";

        public static void SetLanguage(string language)
        {
            _currentLanguage = language?.ToLower() ?? "ja";
        }

        public static string WelcomeTitle => _currentLanguage == "en" 
            ? English.WelcomeTitle 
            : Japanese.WelcomeTitle;

        public static string WelcomeContent => _currentLanguage == "en" 
            ? English.WelcomeContent 
            : Japanese.WelcomeContent;

        public static string ErrorOccurred => _currentLanguage == "en" 
            ? English.ErrorOccurred 
            : Japanese.ErrorOccurred;

        public static string ErrorDetails => _currentLanguage == "en" 
            ? English.ErrorDetails 
            : Japanese.ErrorDetails;

        public static string SalesSummaryTitle => _currentLanguage == "en" 
            ? English.SalesSummaryTitle 
            : Japanese.SalesSummaryTitle;

        public static string PoweredBy => _currentLanguage == "en" 
            ? English.PoweredBy 
            : Japanese.PoweredBy;

        public static string M365NotConfigured => _currentLanguage == "en" 
            ? English.M365NotConfigured 
            : Japanese.M365NotConfigured;

        public static string ProcessingTime => _currentLanguage == "en" 
            ? English.ProcessingTime 
            : Japanese.ProcessingTime;

        public static string LLMProviderInfo => _currentLanguage == "en" 
            ? English.LLMProviderInfo 
            : Japanese.LLMProviderInfo;
    }
}
