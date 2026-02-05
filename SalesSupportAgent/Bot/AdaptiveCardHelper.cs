using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace SalesSupportAgent.Bot;

/// <summary>
/// Adaptive Card を生成するヘルパークラス
/// </summary>
public static class AdaptiveCardHelper
{
    /// <summary>
    /// エージェントの応答を Adaptive Card として生成
    /// </summary>
    /// <param name="title">カードのタイトル</param>
    /// <param name="content">メインコンテンツ</param>
    /// <param name="isError">エラー表示かどうか</param>
    /// <returns>Attachment として返せる Adaptive Card</returns>
    public static Attachment CreateAgentResponseCard(string title, string content, bool isError = false)
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                // ヘッダー（アイコン + タイトル）
                new AdaptiveColumnSet
                {
                    Columns = new List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Auto,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveImage
                                {
                                    Url = new Uri(isError 
                                        ? "https://adaptivecards.io/content/error.png" 
                                        : "https://adaptivecards.io/content/bot.png"),
                                    Size = AdaptiveImageSize.Small,
                                    Style = AdaptiveImageStyle.Person
                                }
                            }
                        },
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = title,
                                    Weight = AdaptiveTextWeight.Bolder,
                                    Size = AdaptiveTextSize.Large,
                                    Wrap = true
                                }
                            }
                        }
                    }
                },
                
                // 区切り線
                new AdaptiveContainer
                {
                    Separator = true,
                    Spacing = AdaptiveSpacing.Medium
                },

                // メインコンテンツ
                new AdaptiveTextBlock
                {
                    Text = content,
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium,
                    Size = AdaptiveTextSize.Default
                }
            }
        };

        // エラーの場合は色を変更
        if (isError)
        {
            card.Body.Add(new AdaptiveContainer
            {
                Style = AdaptiveContainerStyle.Attention,
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "⚠️ エラーが発生しました。詳細は上記をご確認ください。",
                        Wrap = true,
                        Color = AdaptiveTextColor.Attention
                    }
                }
            });
        }

        // フッター（タイムスタンプ）
        card.Body.Add(new AdaptiveTextBlock
        {
            Text = $"更新日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
            Size = AdaptiveTextSize.Small,
            Color = AdaptiveTextColor.Default,
            IsSubtle = true,
            Spacing = AdaptiveSpacing.Medium
        });

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(card.ToJson())
        };
    }

    /// <summary>
    /// 営業サマリー専用の Adaptive Card を生成
    /// </summary>
    /// <param name="summary">営業サマリーコンテンツ</param>
    /// <returns>Attachment として返せる Adaptive Card</returns>
    public static Attachment CreateSalesSummaryCard(string summary)
    {
        // サマリーをセクション分割（Markdown のヘッダーで分割）
        var sections = ParseSummaryIntoSections(summary);

        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                // ヘッダー
                new AdaptiveColumnSet
                {
                    Columns = new List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Auto,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveImage
                                {
                                    Url = new Uri("https://adaptivecards.io/content/chart.png"),
                                    Size = AdaptiveImageSize.Small
                                }
                            }
                        },
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                {
                                    Text = "📊 営業支援エージェント - サマリーレポート",
                                    Weight = AdaptiveTextWeight.Bolder,
                                    Size = AdaptiveTextSize.Large,
                                    Wrap = true
                                }
                            }
                        }
                    }
                },

                new AdaptiveContainer
                {
                    Separator = true,
                    Spacing = AdaptiveSpacing.Medium
                }
            }
        };

        // 各セクションを Adaptive Card のコンテナとして追加
        foreach (var section in sections)
        {
            // セクションタイトルがある場合
            if (!string.IsNullOrEmpty(section.Title))
            {
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = section.Title,
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                    Wrap = true,
                    Spacing = AdaptiveSpacing.Medium
                });
            }

            // セクション内容
            card.Body.Add(new AdaptiveTextBlock
            {
                Text = section.Content,
                Wrap = true,
                Spacing = AdaptiveSpacing.Small
            });
        }

        // フッター
        card.Body.Add(new AdaptiveTextBlock
        {
            Text = $"🤖 powered by Agent 365 SDK | {DateTime.Now:yyyy/MM/dd HH:mm:ss}",
            Size = AdaptiveTextSize.Small,
            Color = AdaptiveTextColor.Default,
            IsSubtle = true,
            Spacing = AdaptiveSpacing.Medium,
            Separator = true
        });

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(card.ToJson())
        };
    }

    /// <summary>
    /// サマリーをセクションに分割
    /// </summary>
    private static List<SummarySection> ParseSummaryIntoSections(string summary)
    {
        var sections = new List<SummarySection>();
        var lines = summary.Split('\n');
        
        SummarySection? currentSection = null;
        var contentBuilder = new List<string>();

        foreach (var line in lines)
        {
            // Markdown のヘッダー（# や ## や ### または **太字**）
            if (line.StartsWith("##") || line.StartsWith("**") && line.EndsWith("**"))
            {
                // 前のセクションを保存
                if (currentSection != null)
                {
                    currentSection.Content = string.Join("\n", contentBuilder).Trim();
                    sections.Add(currentSection);
                }

                // 新しいセクション開始
                currentSection = new SummarySection
                {
                    Title = line.Replace("##", "").Replace("**", "").Trim()
                };
                contentBuilder.Clear();
            }
            else if (currentSection != null)
            {
                contentBuilder.Add(line);
            }
            else
            {
                // セクションタイトルがない場合は全体として扱う
                contentBuilder.Add(line);
            }
        }

        // 最後のセクションを保存
        if (currentSection != null)
        {
            currentSection.Content = string.Join("\n", contentBuilder).Trim();
            sections.Add(currentSection);
        }
        else if (contentBuilder.Count > 0)
        {
            // セクションがない場合は全体を1つのセクションとして
            sections.Add(new SummarySection
            {
                Title = "",
                Content = string.Join("\n", contentBuilder).Trim()
            });
        }

        return sections;
    }

    private class SummarySection
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
