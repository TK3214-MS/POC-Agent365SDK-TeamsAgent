# Code Walkthroughs

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../../developer/13-CODE-WALKTHROUGHS.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](13-CODE-WALKTHROUGHS.md)

## 📋 Available Walkthroughs

This section provides detailed code walkthroughs for key application flows.

---

## Walkthroughs

1. **[Conversation Flow](./CONVERSATION-FLOW.md)**
   - Complete user message to bot response flow
   - Teams Bot integration
   - Agent execution lifecycle
   
2. **[Graph API Calls](./GRAPH-API-CALLS.md)**
   - Email search implementation
   - Calendar event retrieval
   - SharePoint document search
   - Batch request optimization

3. **[LLM Inference](./LLM-INFERENCE.md)**
   - Chat completion flow
   - Tool calling mechanism
   - Streaming responses
   - Provider abstraction

---

## Quick Reference

### Conversation Flow

```
User → TeamsBot → SalesAgent → AIAgent → MCP Tools → Graph API
                                     ↓
                               LLM Provider
                                     ↓
                            Response → User
```

### Graph API Flow

```
OutlookEmailTool → GraphServiceClient → TokenCredential → Azure AD
                                            ↓
                                    Access Token
                                            ↓
                                    Graph API Request
```

### LLM Inference Flow

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

For detailed step-by-step code walkthroughs with line-by-line explanations, please refer to the individual walkthrough documents in the [13-CODE-WALKTHROUGHS](./13-CODE-WALKTHROUGHS) directory. For the Japanese version, see [こちら](../../developer/13-CODE-WALKTHROUGHS.md).
