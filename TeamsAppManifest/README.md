# Teams App Manifest

このディレクトリには、営業支援エージェントを Microsoft Teams にインストールするためのアプリマニフェストファイルが含まれています。

## 📦 ファイル構成

- `manifest.json` - Teams アプリマニフェスト（要編集）
- `color.png` - カラーアイコン (192x192)
- `outline.png` - アウトラインアイコン (32x32)

## 🚀 セットアップ手順

### 1. manifest.json の編集

以下の項目を必ず変更してください：

```json
{
  "id": "YOUR-BOT-APP-ID-HERE",  // ← Azure Bot の App ID
  "bots": [
    {
      "botId": "YOUR-BOT-APP-ID-HERE"  // ← Azure Bot の App ID（同じ）
    }
  ],
  "webApplicationInfo": {
    "id": "YOUR-BOT-APP-ID-HERE"  // ← Azure Bot の App ID（同じ）
  },
  "developer": {
    "name": "Your Company Name",  // ← 会社名
    "websiteUrl": "https://www.example.com",  // ← URL
    "privacyUrl": "https://www.example.com/privacy",
    "termsOfUseUrl": "https://www.example.com/terms"
  }
}
```

### 2. アイコンの準備

**オプション A: デフォルトアイコンを使用**

サンプルアイコンを生成します：

```bash
# カラーアイコン (192x192) - 青い背景にロゴ
convert -size 192x192 xc:#0078D4 -gravity center -pointsize 72 -fill white -annotate +0+0 "営業\nBot" color.png

# アウトラインアイコン (32x32) - 白いアウトライン
convert -size 32x32 xc:transparent -gravity center -pointsize 20 -fill white -annotate +0+0 "営" outline.png
```

**ImageMagick がない場合はインストール:**
```bash
brew install imagemagick
```

**オプション B: カスタムアイコンを作成**

- `color.png`: 192x192 ピクセル、PNG、フルカラー
- `outline.png`: 32x32 ピクセル、PNG、透過背景、白いアウトライン

### 3. ZIP パッケージの作成

```bash
zip -r ../SalesSupportAgent.zip manifest.json color.png outline.png
```

出力: `SalesSupportAgent.zip`

### 4. Teams へのインストール

**方法 A: Teams Developer Portal（推奨）**

1. https://dev.teams.microsoft.com/apps にアクセス
2. "Import an existing app" をクリック
3. `SalesSupportAgent.zip` をアップロード
4. "Preview in Teams" でインストール

**方法 B: Teams から直接アップロード**

1. Teams → アプリ → アプリを管理
2. カスタム アプリをアップロード
3. `SalesSupportAgent.zip` を選択
4. 追加

## ✅ 検証

マニフェストが正しいか検証:
https://dev.teams.microsoft.com/appvalidation.html

## 📚 詳細ガイド

詳しい手順は [docs/TEAMS-MANIFEST.md](../docs/TEAMS-MANIFEST.md) を参照してください。
