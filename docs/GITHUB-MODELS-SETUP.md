# GitHub Models セットアップガイド

## 📋 概要

GitHub Models は、**完全無料**で最新の AI モデルを利用できる GitHub 公式のサービスです。OpenAI GPT-4o, Meta Llama, DeepSeek など、様々なモデルを GitHub アカウントだけで利用できます。

## 🌟 主な特徴

- ✅ **完全無料** - 開発・評価用途で利用可能
- ✅ **GitHub アカウントのみ** - 追加のサインアップ不要
- ✅ **最新モデル** - GPT-4o, GPT-4o-mini, Llama 3.2, DeepSeek R1 など
- ✅ **OpenAI 互換 API** - 既存コードの移行が簡単
- ✅ **レート制限が寛容** - 開発用途には十分

## 🚀 セットアップ手順

### 1. Personal Access Token (PAT) の作成

1. [GitHub Settings → Personal access tokens](https://github.com/settings/tokens) にアクセス
2. **Generate new token** → **Generate new token (classic)** をクリック
3. トークンの設定:
   - **Note**: `SalesSupportAgent - GitHub Models`
   - **Expiration**: 90 days（推奨）
   - **Select scopes**: 
     - ✅ `models` にチェック
4. **Generate token** をクリック
5. 生成されたトークンをコピー（**この画面でしか表示されません**）
   - 形式: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### 2. アプリケーション設定

#### 方法 A: appsettings.json で設定

[appsettings.json](../SalesSupportAgent/appsettings.json) を編集:

```json
{
  "LLM": {
    "Provider": "GitHubModels",
    "GitHubModels": {
      "Token": "ghp_your_personal_access_token_here",
      "ModelName": "openai/gpt-4o-mini"
    }
  }
}
```

#### 方法 B: 環境変数で設定（推奨）

```bash
# .env ファイルを作成
cat > .env << EOF
LLM__Provider=GitHubModels
LLM__GitHubModels__Token=ghp_your_token_here
LLM__GitHubModels__ModelName=openai/gpt-4o-mini
EOF
```

または直接エクスポート:

```bash
export LLM__Provider=GitHubModels
export LLM__GitHubModels__Token=ghp_your_token
export LLM__GitHubModels__ModelName=openai/gpt-4o-mini
```

### 3. 動作確認

```bash
# ビルド
dotnet build

# 実行
dotnet run
```

アプリケーションが起動したら、API をテスト:

```bash
curl -X POST https://localhost:5001/api/sales-summary \
  -H "Content-Type: application/json" \
  -d '{"query":"今週の商談サマリを教えて"}'
```

## 🤖 利用可能なモデル

### OpenAI モデル

| モデル名 | 説明 | 推奨用途 |
|---------|------|---------|
| `openai/gpt-4o` | 最新の GPT-4o（高性能） | 複雑なタスク、推論 |
| `openai/gpt-4o-mini` | GPT-4o mini（高速・低コスト） | 一般的なタスク（推奨） |
| `openai/gpt-4.1` | GPT-4.1 | 従来の GPT-4 |

### Meta Llama モデル

| モデル名 | 説明 |
|---------|------|
| `meta-llama/Llama-3.2-90B-Vision-Instruct` | Llama 3.2 90B（ビジョン対応） |
| `meta-llama/Llama-3.2-11B-Vision-Instruct` | Llama 3.2 11B（軽量版） |

### DeepSeek モデル

| モデル名 | 説明 |
|---------|------|
| `deepseek/deepseek-r1` | DeepSeek R1（推論特化） |

**全モデル一覧**: [GitHub Marketplace - Models](https://github.com/marketplace?type=models)

## 📊 モデル選択ガイド

### ユースケース別推奨モデル

#### 営業支援エージェント（本プロジェクト）
```json
"ModelName": "openai/gpt-4o-mini"
```
- **理由**: 十分な性能、高速、コスト効率が良い

#### 高度な推論が必要な場合
```json
"ModelName": "openai/gpt-4o"
```
- **理由**: 最高性能、複雑な質問に対応

#### 大量のコンテキストを扱う場合
```json
"ModelName": "openai/gpt-4o"
```
- **理由**: 128K トークンのコンテキストウィンドウ

#### ビジョン（画像解析）が必要な場合
```json
"ModelName": "meta-llama/Llama-3.2-90B-Vision-Instruct"
```
- **理由**: 画像とテキストの両方に対応

## 🔧 高度な設定

### カスタムエンドポイント（通常は不要）

GitHub Models のデフォルトエンドポイントは `https://models.github.ai/inference/chat/completions` ですが、カスタマイズする場合は `GitHubModelsProvider.cs` を編集:

```csharp
new OpenAIClientOptions 
{ 
    Endpoint = new Uri("https://your-custom-endpoint")
}
```

### トークンの更新

トークンの有効期限が切れた場合:

1. [GitHub Settings → Personal access tokens](https://github.com/settings/tokens) にアクセス
2. 既存のトークンを削除
3. 新しいトークンを生成
4. `appsettings.json` または環境変数を更新

## ⚠️ トラブルシューティング

### エラー: "Unauthorized" (401)

**原因**: トークンが無効または権限不足

**対処**:
- トークンが正しくコピーされているか確認
- トークンに `models` スコープが付与されているか確認
- トークンの有効期限を確認

### エラー: "Model not found"

**原因**: モデル名が間違っている

**対処**:
```json
// ✅ 正しい
"ModelName": "openai/gpt-4o-mini"

// ❌ 間違い
"ModelName": "gpt-4o-mini"  // プレフィックス (openai/) が必要
```

### エラー: "Rate limit exceeded"

**原因**: リクエスト制限を超過

**対処**:
- 少し待ってから再試行
- リクエスト頻度を下げる
- 必要に応じて GitHub サポートに問い合わせ

### 接続タイムアウト

**原因**: ネットワーク問題またはサービス障害

**対処**:
- インターネット接続を確認
- [GitHub Status](https://www.githubstatus.com/) でサービス状態を確認
- VPN を使用している場合は無効化してみる

## 📈 使用制限

### 無料プラン

- **リクエスト数**: 開発用途には十分（具体的な数値は公開されていない）
- **レート制限**: 1分あたり数十リクエスト
- **トークン制限**: モデルごとに異なる

### 制限を超えた場合

エラーメッセージに従ってリトライポリシーを実装するか、リクエスト頻度を調整してください。

## 🔒 セキュリティのベストプラクティス

### 1. トークンの保護

```bash
# ✅ 推奨: 環境変数
export LLM__GitHubModels__Token=ghp_xxx

# ❌ 非推奨: コードに直接埋め込み
public string Token = "ghp_xxx";  // 絶対にやらない
```

### 2. .gitignore でトークンを除外

```.gitignore
# .gitignore
appsettings.json
.env
*.token
```

### 3. トークンのローテーション

- 定期的にトークンを再生成（推奨: 3ヶ月ごと）
- 漏洩の疑いがある場合は即座に削除

## 📚 参考リンク

- [GitHub Models 公式ドキュメント](https://docs.github.com/en/github-models)
- [GitHub Models Quickstart](https://docs.github.com/en/github-models/quickstart)
- [GitHub Marketplace - Models](https://github.com/marketplace?type=models)
- [GitHub Models API Reference](https://docs.github.com/en/rest/models/inference)

## 💡 Tips

### モデルの切り替えをスムーズに

複数のモデルを試す場合は、環境変数で簡単に切り替え:

```bash
# GPT-4o-mini
export LLM__GitHubModels__ModelName=openai/gpt-4o-mini
dotnet run

# GPT-4o
export LLM__GitHubModels__ModelName=openai/gpt-4o
dotnet run

# Llama
export LLM__GitHubModels__ModelName=meta-llama/Llama-3.2-90B-Vision-Instruct
dotnet run
```

### コスト比較

| プロバイダー | 月額コスト（概算） | 備考 |
|-------------|------------------|------|
| GitHub Models | **無料** | 開発・評価用途 |
| Ollama | 無料 | ローカル実行（電気代のみ） |
| Azure OpenAI | $数十〜数百 | 本番運用向け |

---

**GitHub Models で無料で最新の AI モデルを活用しましょう！** 🚀
