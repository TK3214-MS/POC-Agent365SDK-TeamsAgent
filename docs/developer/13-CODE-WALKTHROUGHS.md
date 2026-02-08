# コードウォークスルー

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](13-CODE-WALKTHROUGHS.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](../en/developer/13-CODE-WALKTHROUGHS.md)

## 📋 利用可能なウォークスルー

このセクションでは、主要なアプリケーションフローに関する詳細なコードウォークスルーを提供します。

---

## ウォークスルー

1. **[会話フロー](./13-CODE-WALKTHROUGHS/CONVERSATION-FLOW.md)**
   - ユーザーメッセージからボット応答までの完全なフロー
   - Teams Bot統合
   - エージェント実行ライフサイクル
   
2. **[Graph API呼び出し](./13-CODE-WALKTHROUGHS/GRAPH-API-CALLS.md)**
   - メール検索の実装
   - カレンダーイベントの取得
   - SharePointドキュメント検索
   - バッチリクエストの最適化

3. **[LLM推論](./13-CODE-WALKTHROUGHS/LLM-INFERENCE.md)**
   - チャット完了フロー
   - ツール呼び出しメカニズム
   - ストリーミングレスポンス
   - プロバイダー抽象化

---

## クイックリファレンス

### 会話フロー

```
User → TeamsBot → SalesAgent → AIAgent → MCP Tools → Graph API
                                     ↓
                               LLM Provider
                                     ↓
                            Response → User
```

### Graph APIフロー

```
OutlookEmailTool → GraphServiceClient → TokenCredential → Azure AD
                                            ↓
                                    Access Token
                                            ↓
                                    Graph API Request
```

### LLM推論フロー

```
SalesAgent.RunAsync → IChatClient → LLM Provider (Azure OpenAI/Ollama)
                          ↓
                     Tool Calls
                          ↓
                     MCP Tools
                          ↓
                   Final Response
```

---

各フローの詳細な行ごとの説明については、[13-CODE-WALKTHROUGHS](./13-CODE-WALKTHROUGHS)ディレクトリ内の個別のウォークスルードキュメントをご参照ください。英語版は[こちら](../en/developer/13-CODE-WALKTHROUGHS.md)からご覧いただけます。
