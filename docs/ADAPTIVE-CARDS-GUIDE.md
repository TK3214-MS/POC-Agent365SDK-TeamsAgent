# Adaptive Cards 実装ガイド

## 📋 概要

本プロジェクトでは、Teams Bot の応答に **Adaptive Cards** を使用しています。Adaptive Cards は、構造化された視覚的に魅力的なカードを作成するための JSON ベースの形式で、Microsoft Teams をはじめとする様々なプラットフォームで動作します。

## 🎨 実装済みの Adaptive Card タイプ

### 1. 営業サマリーカード (`CreateSalesSummaryCard`)

**用途**: エージェントが生成した営業サマリーを視覚的に表示

**特徴**:
- 📊 ヘッダーにアイコンとタイトル
- 📝 セクションごとに分割された読みやすいレイアウト
- ⏱️ タイムスタンプ表示
- 🤖 LLM プロバイダー情報

**サンプル出力**:
```
┌─────────────────────────────────────────┐
│ 📊 営業支援エージェント - サマリーレポート  │
├─────────────────────────────────────────┤
│ ## 📧 商談関連メール (5件)               │
│ - 件名1                                 │
│ - 件名2                                 │
│                                         │
│ ## 📅 商談関連予定 (3件)                │
│ - 予定1                                 │
│ - 予定2                                 │
│                                         │
│ 🤖 powered by Agent 365 SDK | 2026/02/05│
└─────────────────────────────────────────┘
```

**コード例**:
```csharp
var cardAttachment = AdaptiveCardHelper.CreateSalesSummaryCard(response.Response);
var reply = MessageFactory.Attachment(cardAttachment);
await turnContext.SendActivityAsync(reply, cancellationToken);
```

### 2. エラーカード (`CreateAgentResponseCard` with `isError: true`)

**用途**: エラー発生時に視覚的な通知を表示

**特徴**:
- ⚠️ 目立つエラー表示
- 📝 エラー内容の詳細
- 💡 対処方法のガイダンス
- 🎨 警告色のスタイル

**サンプル出力**:
```
┌─────────────────────────────────────────┐
│ ⚠️ エラーが発生しました                  │
├─────────────────────────────────────────┤
│ **エラー内容:**                          │
│ Microsoft 365 が設定されていません       │
│                                         │
│ **対処方法:**                            │
│ - appsettings.json を確認してください   │
│ - M365 の権限設定を確認してください     │
│                                         │
│ ⚠️ エラーが発生しました。                │
│ 更新日時: 2026/02/05 18:30:00           │
└─────────────────────────────────────────┘
```

### 3. ウェルカムカード

**用途**: 初回接続時のガイダンス表示

**特徴**:
- 👋 フレンドリーな挨拶
- 📖 機能説明
- 💬 使い方の具体例
- ⚠️ 初期設定の注意事項

## 🛠️ Adaptive Card の構造

### 基本構成

```csharp
var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
{
    Body = new List<AdaptiveElement>
    {
        // ヘッダー
        new AdaptiveColumnSet { /* ... */ },
        
        // 区切り線
        new AdaptiveContainer { Separator = true },
        
        // メインコンテンツ
        new AdaptiveTextBlock { /* ... */ },
        
        // フッター
        new AdaptiveTextBlock { /* ... */ }
    }
};
```

### 主要な要素

#### AdaptiveColumnSet
2カラムレイアウトを作成（アイコン + テキスト）

```csharp
new AdaptiveColumnSet
{
    Columns = new List<AdaptiveColumn>
    {
        new AdaptiveColumn
        {
            Width = AdaptiveColumnWidth.Auto,
            Items = new List<AdaptiveElement>
            {
                new AdaptiveImage { /* アイコン */ }
            }
        },
        new AdaptiveColumn
        {
            Width = AdaptiveColumnWidth.Stretch,
            Items = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock { /* タイトル */ }
            }
        }
    }
}
```

#### AdaptiveTextBlock
テキストコンテンツの表示

```csharp
new AdaptiveTextBlock
{
    Text = "表示するテキスト",
    Weight = AdaptiveTextWeight.Bolder,  // 太字
    Size = AdaptiveTextSize.Large,       // サイズ
    Wrap = true,                         // 折り返し
    Color = AdaptiveTextColor.Default    // 色
}
```

#### AdaptiveContainer
セクションのグループ化と区切り

```csharp
new AdaptiveContainer
{
    Separator = true,                    // 区切り線
    Spacing = AdaptiveSpacing.Medium,    // スペース
    Style = AdaptiveContainerStyle.Attention  // 警告スタイル
}
```

## 📝 カスタマイズ方法

### 新しいカードタイプを追加

1. **AdaptiveCardHelper.cs** に新しいメソッドを追加

```csharp
public static Attachment CreateCustomCard(string title, string content)
{
    var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
    {
        Body = new List<AdaptiveElement>
        {
            // カスタム要素を追加
        }
    };

    return new Attachment
    {
        ContentType = AdaptiveCard.ContentType,
        Content = JsonConvert.DeserializeObject(card.ToJson())
    };
}
```

2. **TeamsBot.cs** で使用

```csharp
var customCard = AdaptiveCardHelper.CreateCustomCard("タイトル", "内容");
var reply = MessageFactory.Attachment(customCard);
await turnContext.SendActivityAsync(reply, cancellationToken);
```

### スタイルの変更

#### 色の変更
```csharp
Color = AdaptiveTextColor.Accent      // アクセント色
Color = AdaptiveTextColor.Good        // 成功（緑）
Color = AdaptiveTextColor.Warning     // 警告（黄）
Color = AdaptiveTextColor.Attention   // エラー（赤）
```

#### サイズの変更
```csharp
Size = AdaptiveTextSize.Small
Size = AdaptiveTextSize.Default
Size = AdaptiveTextSize.Medium
Size = AdaptiveTextSize.Large
Size = AdaptiveTextSize.ExtraLarge
```

#### 太さの変更
```csharp
Weight = AdaptiveTextWeight.Lighter
Weight = AdaptiveTextWeight.Default
Weight = AdaptiveTextWeight.Bolder
```

## 🧪 テスト方法

### Bot Framework Emulator でテスト

1. Bot Framework Emulator をダウンロード
2. `http://localhost:5001/api/messages` に接続
3. メッセージを送信して Adaptive Card を確認

### Teams でテスト

1. Dev Tunnel または ngrok でトンネルを作成
2. Teams に Bot をインストール
3. メッセージを送信して Adaptive Card を確認

### デザイナーでプレビュー

[Adaptive Cards Designer](https://adaptivecards.io/designer/) で JSON を確認・編集

## 📚 参考リンク

- [Adaptive Cards 公式ドキュメント](https://adaptivecards.io/)
- [Adaptive Cards スキーマ](https://adaptivecards.io/explorer/)
- [Bot Framework - Adaptive Cards](https://learn.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/cards-reference#adaptive-card)
- [Teams での Adaptive Cards](https://learn.microsoft.com/en-us/microsoftteams/platform/task-modules-and-cards/cards/design-effective-cards)

## 💡 ベストプラクティス

### 1. レスポンシブデザイン
- `Wrap = true` を使用してテキストを折り返す
- `AdaptiveColumnWidth.Stretch` でレイアウトを柔軟に

### 2. アクセシビリティ
- 色だけに頼らず、アイコンやテキストで情報を補完
- `IsSubtle = true` でフッター情報を控えめに表示

### 3. パフォーマンス
- カードのサイズは適切に（最大 28KB 推奨）
- 画像は URL 参照を使用（埋め込みを避ける）

### 4. ユーザー体験
- タイムスタンプを表示して情報の鮮度を示す
- セクションごとに分割して読みやすく
- エラー時は具体的な対処方法を提示

## 🔧 トラブルシューティング

### Adaptive Card が表示されない

**原因**: JSON 形式が無効
**対処**: Adaptive Cards Designer で JSON を検証

### スタイルが適用されない

**原因**: Teams が対応していないスタイル
**対処**: Teams 対応の要素のみ使用（公式ドキュメント参照）

### カードが長すぎる

**原因**: コンテンツが多すぎる
**対処**: セクションを分割して複数のメッセージに分ける

---

**Agent 365 SDK + Adaptive Cards** で美しく機能的な Teams Bot を構築しましょう！
