# Dev Tunnel セットアップガイド

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../DEV-TUNNEL-SETUP.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](en/DEV-TUNNEL-SETUP.md)

## Dev Tunnel とは？

Microsoft が提供する開発用トンネルサービスで、ローカルアプリケーションを外部に公開できます。

### ngrok との比較

| 機能 | Dev Tunnel | ngrok |
|-----|-----------|-------|
| **固定 URL** | ✅ Yes (persistent) | ❌ Free版はNo |
| **Microsoft 統合** | ✅ Yes | ❌ No |
| **認証** | Microsoft アカウント | ngrok アカウント |
| **価格** | 無料 | Free/有料プラン |
| **VS Code 統合** | ✅ Yes | ❌ No |

---

## インストール

### macOS / Linux

```bash
# cURL
curl -sL https://aka.ms/DevTunnelCliInstall | bash

# Homebrew (macOS)
brew install devtunnel
```

### Windows

```powershell
# winget
winget install Microsoft.devtunnel
```

インストール確認:

```bash
devtunnel --version
```

---

## セットアップ手順

### 1. ログイン

```bash
devtunnel user login
```

ブラウザが開き、Microsoft アカウントでサインインを求められます。

### 2. トンネル作成（初回のみ）

```bash
# 固定トンネルを作成
devtunnel create sales-support-agent --allow-anonymous

# 出力例:
# Tunnel ID: abc123xyz
# Tunnel Name: sales-support-agent
```

**オプション:**
- `--allow-anonymous`: 匿名アクセスを許可（Bot Framework 用に必要）
- `--expiration 30d`: 有効期限を設定（デフォルト: 無期限）

### 3. ポート設定

```bash
# HTTPS ポート 5001 を公開
devtunnel port create sales-support-agent -p 5001 --protocol https
```

### 4. トンネル起動

```bash
devtunnel host sales-support-agent
```

**出力例:**
```
Hosting port: 5001
Connect via browser: https://abc123xyz-5001.euw.devtunnels.ms
Inspect via browser: https://abc123xyz-5001-inspect.euw.devtunnels.ms

Ready to accept connections for tunnel: sales-support-agent
```

この **固定 URL** (`https://abc123xyz-5001.euw.devtunnels.ms`) を Bot Framework のメッセージング エンドポイントに設定します。

---

## 使い方

### 日常的な使用

トンネルは作成済みなので、毎回以下のコマンドだけで起動:

```bash
# トンネル起動
devtunnel host sales-support-agent
```

### 停止

`Ctrl + C` でトンネルを停止します。

---

## トラブルシューティング

### トンネル一覧を確認

```bash
devtunnel list
```

### トンネル詳細を確認

```bash
devtunnel show sales-support-agent
```

### ポート確認

```bash
devtunnel port list sales-support-agent
```

### トンネル削除

```bash
devtunnel delete sales-support-agent
```

### ログイン状態確認

```bash
devtunnel user show
```

---

## VS Code 統合（オプション）

VS Code で Dev Tunnel を GUI から管理できます:

1. VS Code を開く
2. **Command Palette** (`Cmd+Shift+P`)
3. `Dev Tunnels: Create Tunnel` を選択
4. ポートを選択 (5001)
5. アクセス設定を選択 (`Public`)

---

## セキュリティ

- **匿名アクセス** (`--allow-anonymous`) は開発・テスト専用
- 本番環境では認証を有効化:
  ```bash
  devtunnel create sales-support-agent --allow-github
  ```

---

## 参考資料

- [Dev Tunnel 公式ドキュメント](https://learn.microsoft.com/azure/developer/dev-tunnels/)
- [CLI リファレンス](https://learn.microsoft.com/azure/developer/dev-tunnels/cli-commands)
