using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace SalesSupportAgent.Services.TestData;

/// <summary>
/// テストデータ生成サービス（委任された権限を使用）
/// </summary>
public class TestDataGenerator
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<TestDataGenerator> _logger;
    private string _currentUserId = string.Empty;

    // サンプル企業名
    private readonly string[] _companies = new[]
    {
        "株式会社サンプルテック", "合同会社クラウドソリューションズ", "株式会社デジタルイノベーション",
        "株式会社エンタープライズシステムズ", "合同会社ビジネスパートナーズ", "株式会社グローバルトレーディング",
        "株式会社アドバンスドテクノロジー", "合同会社プロフェッショナルサービス", "株式会社フューチャービジョン",
        "株式会社スマートソリューション"
    };

    // サンプル担当者名
    private readonly string[] _contacts = new[]
    {
        "田中太郎", "佐藤花子", "鈴木一郎", "高橋美咲", "渡辺健太",
        "伊藤由美", "山本大輔", "中村さくら", "小林誠", "加藤麻衣"
    };

    // サンプル製品名
    private readonly string[] _products = new[]
    {
        "クラウド基盤サービス", "AIソリューション", "データ分析プラットフォーム",
        "セキュリティ対策パッケージ", "業務効率化ツール", "コラボレーションシステム",
        "モバイルアプリケーション", "IoTプラットフォーム", "ビジネスインテリジェンス",
        "カスタマーサポートシステム"
    };

    public TestDataGenerator(GraphServiceClient graphClient, ILogger<TestDataGenerator> logger)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 現在のユーザーIDを初期化
    /// </summary>
    private async Task<string> GetCurrentUserIdAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            var me = await _graphClient.Me.GetAsync();
            _currentUserId = me?.Id ?? throw new InvalidOperationException("ユーザーIDを取得できませんでした");
            _logger.LogInformation("認証ユーザー: {DisplayName} ({Id})", me?.DisplayName, _currentUserId);
        }
        return _currentUserId;
    }

    /// <summary>
    /// 商談メールを生成
    /// </summary>
    public async Task<int> GenerateSalesEmailsAsync(DateTime startDate, DateTime endDate, int count)
    {
        var created = 0;
        var random = new Random();

        try
        {
            var userId = await GetCurrentUserIdAsync();

            for (int i = 0; i < count; i++)
            {
                var company = _companies[random.Next(_companies.Length)];
                var contact = _contacts[random.Next(_contacts.Length)];
                var product = _products[random.Next(_products.Length)];
                var amount = random.Next(100, 5000) * 10000;
                var date = GetRandomDate(startDate, endDate, random);

                var subject = GetRandomEmailSubject(company, contact, product, random);
                var body = GenerateEmailBody(company, contact, product, amount, date);

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = "noreply@example.com"
                            }
                        }
                    },
                    Categories = new List<string> { "商談", "営業" },
                    ReceivedDateTime = date,
                    SentDateTime = date
                };

                await _graphClient.Me.MailFolders["drafts"].Messages.PostAsync(message);

                created++;
                _logger.LogInformation("メール作成: {Subject}", subject);

                if (i % 10 == 9)
                {
                    await Task.Delay(1000);
                }
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "メール生成エラー（作成済み: {Count}/{Total}）", created, count);
            return created;
        }
    }

    /// <summary>
    /// 商談予定を生成
    /// </summary>
    public async Task<int> GenerateCalendarEventsAsync(DateTime startDate, DateTime endDate, int count)
    {
        var created = 0;
        var random = new Random();

        try
        {
            var userId = await GetCurrentUserIdAsync();

            for (int i = 0; i < count; i++)
            {
                var company = _companies[random.Next(_companies.Length)];
                var contact = _contacts[random.Next(_contacts.Length)];
                var product = _products[random.Next(_products.Length)];
                var eventDate = GetRandomDate(startDate, endDate, random);
                var startTime = eventDate.AddHours(random.Next(9, 17));
                var endTime = startTime.AddHours(random.Next(1, 3));

                var subject = GetRandomMeetingSubject(company, contact, random);
                var body = GenerateMeetingBody(company, contact, product);

                var calendarEvent = new Event
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = body
                    },
                    Start = new DateTimeTimeZone
                    {
                        DateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Tokyo Standard Time"
                    },
                    End = new DateTimeTimeZone
                    {
                        DateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Tokyo Standard Time"
                    },
                    Location = new Location
                    {
                        DisplayName = random.Next(2) == 0 ? "会議室A" : "オンライン（Teams）"
                    },
                    Categories = new List<string> { "商談", "営業", "ミーティング" }
                };

                await _graphClient.Me.Events.PostAsync(calendarEvent);

                created++;
                _logger.LogInformation("予定作成: {Subject} ({Date})", subject, startTime.ToString("yyyy-MM-dd HH:mm"));

                if (i % 10 == 9)
                {
                    await Task.Delay(1000);
                }
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "予定生成エラー（作成済み: {Count}/{Total}）", created, count);
            return created;
        }
    }

    private string GetRandomEmailSubject(string company, string contact, string product, Random random)
    {
        var subjects = new[]
        {
            $"【商談】{company} - {product}のご提案",
            $"Re: {contact}様との打ち合わせについて",
            $"{company}様向け　{product}　見積書送付",
            $"【重要】{company} - 契約更新のご案内",
            $"{product}のデモンストレーション日程調整",
            $"{contact}様　{product}の導入事例のご紹介",
            $"【至急】{company}様　提案書修正版",
            $"{product}に関するお問い合わせへの回答",
            // 追加: より多様なシナリオ
            $"✅ {company}様　{product}導入成功事例のご報告",
            $"📊 {contact}様　ROI分析レポート送付",
            $"🎯 {company} - 今期目標達成に向けた戦略提案",
            $"⚠️ {contact}様　課題解決に関するフォローアップ",
            $"🔄 {company}様　プロジェクト進捗確認",
            $"💼 {product} - 競合比較資料のご提供",
            $"📈 {company}様　売上予測と提案",
            $"🤝 {contact}様　パートナーシップ契約について"
        };
        return subjects[random.Next(subjects.Length)];
    }

    private string GetRandomMeetingSubject(string company, string contact, Random random)
    {
        var subjects = new[]
        {
            $"{company}様　商談ミーティング",
            $"{contact}様　打ち合わせ",
            $"{company} - 提案プレゼンテーション",
            $"【営業】{company}様　定例会",
            $"{contact}様　ヒアリング",
            $"{company}　契約締結前確認",
            $"【商談】{company}様　要件定義",
            $"{contact}様　デモンストレーション",
            // 追加: より具体的なミーティング
            $"🎤 {company}様　製品デモ&Q&Aセッション",
            $"💡 {contact}様　課題分析ワークショップ",
            $"📊 {company} - 四半期レビューミーティング",
            $"🔍 {contact}様　技術要件ヒアリング",
            $"✅ {company}様　最終提案プレゼンテーション",
            $"🎯 {contact}様　キックオフミーティング",
            $"📝 {company} - 契約内容詳細確認",
            $"🤝 {contact}様　エグゼクティブプレゼン"
        };
        return subjects[random.Next(subjects.Length)];
    }

    private string GenerateEmailBody(string company, string contact, string product, int amount, DateTime date)
    {
        return $@"
<html>
<body>
<p>{contact}様</p>
<p>いつもお世話になっております。</p>
<p>{company}様向けの<strong>{product}</strong>についてご提案させていただきます。</p>
<h3>提案概要</h3>
<ul>
<li>製品名: {product}</li>
<li>想定金額: ¥{amount:N0}</li>
<li>提案日: {date:yyyy年MM月dd日}</li>
</ul>
<p>詳細は添付の提案書をご確認ください。</p>
<p>ご不明点がございましたら、お気軽にお問い合わせください。</p>
<br>
<p>よろしくお願いいたします。</p>
</body>
</html>";
    }

    private string GenerateMeetingBody(string company, string contact, string product)
    {
        return $@"
<html>
<body>
<h3>議題</h3>
<ul>
<li>{product}の詳細説明</li>
<li>導入スケジュールの確認</li>
<li>見積もり内容の協議</li>
<li>次回アクション確認</li>
</ul>
<p><strong>参加者:</strong> {contact}様、営業担当</p>
<p><strong>会社:</strong> {company}</p>
</body>
</html>";
    }

    private DateTime GetRandomDate(DateTime start, DateTime end, Random random)
    {
        var range = (end - start).Days;
        return start.AddDays(random.Next(range));
    }
}
