using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using SalesSupportAgent.Resources;

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
    /// 営業サマリー専用の Adaptive Card を生成（強化版）
    /// </summary>
    /// <param name="summary">営業サマリーコンテンツ</param>
    /// <param name="llmProvider">LLMプロバイダー名</param>
    /// <param name="processingTime">処理時間（ミリ秒）</param>
    /// <returns>Attachment として返せる Adaptive Card</returns>
    public static Attachment CreateSalesSummaryCard(string summary, string? llmProvider = null, long? processingTime = null)
    {
        // サマリーをセクション分割（Markdown のヘッダーで分割）
        var sections = ParseSummaryIntoSections(summary);

        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = new List<AdaptiveElement>
            {
                // ヘッダー（グラデーション背景風）
                new AdaptiveContainer
                {
                    Style = AdaptiveContainerStyle.Emphasis,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                            {
                                new AdaptiveColumn
                                {
                                    Width = AdaptiveColumnWidth.Auto,
                                    Items = new List<AdaptiveElement>
                                    {
                                        new AdaptiveTextBlock
                                        {
                                            Text = "🤖",
                                            Size = AdaptiveTextSize.ExtraLarge
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
                                            Text = "営業支援エージェント",
                                            Weight = AdaptiveTextWeight.Bolder,
                                            Size = AdaptiveTextSize.Large,
                                            Wrap = true
                                        },
                                        new AdaptiveTextBlock
                                        {
                                            Text = "Agent 365 SDK | サマリーレポート",
                                            Size = AdaptiveTextSize.Small,
                                            IsSubtle = true,
                                            Spacing = AdaptiveSpacing.None
                                        }
                                    }
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

        // 各セクションを色分けして追加
        var sectionIndex = 0;
        foreach (var section in sections)
        {
            var sectionIcon = GetSectionIcon(section.Title);
            var containerStyle = GetSectionStyle(sectionIndex);

            var sectionContainer = new AdaptiveContainer
            {
                Style = containerStyle,
                Spacing = AdaptiveSpacing.Medium,
                Items = new List<AdaptiveElement>()
            };

            // セクションタイトル（アイコン付き）
            if (!string.IsNullOrEmpty(section.Title))
            {
                sectionContainer.Items.Add(new AdaptiveTextBlock
                {
                    Text = $"{sectionIcon} {section.Title}",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                    Wrap = true
                });
            }

            // セクション内容
            sectionContainer.Items.Add(new AdaptiveTextBlock
            {
                Text = section.Content,
                Wrap = true,
                Spacing = AdaptiveSpacing.Small
            });

            card.Body.Add(sectionContainer);
            sectionIndex++;
        }

        // 統計情報（Fact Set）
        var facts = new List<AdaptiveFact>
        {
            new AdaptiveFact
            {
                Title = "データソース",
                Value = $"{sections.Count} セクション"
            },
            new AdaptiveFact
            {
                Title = "生成日時",
                Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")
            }
        };

        if (!string.IsNullOrEmpty(llmProvider))
        {
            facts.Insert(0, new AdaptiveFact
            {
                Title = "LLM",
                Value = llmProvider
            });
        }

        if (processingTime.HasValue)
        {
            facts.Add(new AdaptiveFact
            {
                Title = "処理時間",
                Value = $"{processingTime.Value:N0}ms ({processingTime.Value / 1000.0:F2}秒)"
            });
        }

        card.Body.Add(new AdaptiveFactSet
        {
            Facts = facts,
            Spacing = AdaptiveSpacing.Medium,
            Separator = true
        });

        // アクションボタン
        card.Actions = new List<AdaptiveAction>
        {
            new AdaptiveOpenUrlAction
            {
                Title = "📧 Outlookを開く",
                Url = new Uri("https://outlook.office.com")
            },
            new AdaptiveOpenUrlAction
            {
                Title = "📅 カレンダーを開く",
                Url = new Uri("https://outlook.office.com/calendar")
            },
            new AdaptiveOpenUrlAction
            {
                Title = "📁 SharePointを開く",
                Url = new Uri("https://www.office.com/launch/sharepoint")
            }
        };

        return new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = JsonConvert.DeserializeObject(card.ToJson())
        };
    }

    /// <summary>
    /// セクションタイトルに基づいてアイコンを取得
    /// </summary>
    private static string GetSectionIcon(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return "📄";

        var lowerTitle = title.ToLower();
        if (lowerTitle.Contains("メール") || lowerTitle.Contains("email"))
            return "📧";
        if (lowerTitle.Contains("カレンダー") || lowerTitle.Contains("予定") || lowerTitle.Contains("calendar"))
            return "📅";
        if (lowerTitle.Contains("sharepoint") || lowerTitle.Contains("ドキュメント") || lowerTitle.Contains("文書"))
            return "📁";
        if (lowerTitle.Contains("teams") || lowerTitle.Contains("メッセージ") || lowerTitle.Contains("チャット"))
            return "💬";
        if (lowerTitle.Contains("サマリー") || lowerTitle.Contains("まとめ") || lowerTitle.Contains("summary"))
            return "📊";

        return "📄";
    }

    /// <summary>
    /// セクションインデックスに基づいてスタイルを取得
    /// </summary>
    private static AdaptiveContainerStyle GetSectionStyle(int index)
    {
        // 交互に色を変える
        return index % 2 == 0 ? AdaptiveContainerStyle.Default : AdaptiveContainerStyle.Emphasis;
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
