# 🚀 Agent 365 Observability Platform デモガイド

## 📋 目次

- [概要](#概要)
- [デモの価値提案](#デモの価値提案)
- [セットアップ手順](#セットアップ手順)
- [デモシナリオ](#デモシナリオ)
- [フォーカルポイント](#フォーカルポイント)
- [トラブルシューティング](#トラブルシューティング)

---

## 概要

このデモは、**Microsoft Agent 365 SDK** の **Observability** 機能を視覚的に示すエンタープライズプラットフォームです。
リアルタイムダッシュボードを通じて、AIエージェントの透明性、トレーサビリティ、パフォーマンス監視を実現します。

### 主要コンポーネント

1. **Sales Support Agent** - Teams Bot統合営業支援エージェント
2. **Observability Dashboard** - リアルタイムメトリクス & トレース可視化
3. **SignalR Hub** - リアルタイム双方向通信
4. **Agent 365 SDK** - Observability, Tooling, Notifications統合

### アーキテクチャ

```
┌─────────────┐       ┌──────────────────┐       ┌─────────────────┐
│ Teams Bot   │──────▶│ SalesAgent       │──────▶│ Observability   │
│ (Web Chat)  │       │ + Agent365 SDK   │       │ Service         │
└─────────────┘       └──────────────────┘       └─────────────────┘
                                                          │
                                                          ▼
                                                   ┌─────────────────┐
                                                   │ SignalR Hub     │
                                                   └─────────────────┘
                                                          │
                                                          ▼
                                                   ┌─────────────────┐
                                                   │ HTML Dashboard  │
                                                   │ (Real-time UI)  │
                                                   └─────────────────┘
```

---

## デモの価値提案

### 🎯 ビジネス価値

1. **透明性 (Transparency)**
   - エージェントの内部動作が可視化され、意思決定プロセスが明確
   - ツール実行状況をリアルタイムで追跡

2. **トレーサビリティ (Traceability)**
   - すべての操作が時系列で記録され、監査証跡として利用可能
   - パフォーマンス問題の根本原因分析が容易

3. **信頼性 (Reliability)**
   - 成功率、応答時間などのメトリクスで品質を保証
   - エラー検知と自動アラート機能

4. **エンタープライズ対応 (Enterprise-Ready)**
   - OpenTelemetry標準に準拠
   - 既存の監視システム（Application Insights, Grafana）と統合可能

### 🔥 技術的差別化要因

- **Agent 365 SDK統合** - Microsoft公式SDKのエンタープライズ機能を活用
- **リアルタイム可視化** - SignalRによる即座のフィードバック
- **ゼロ設定監視** - エージェント実行と同時に自動トレース
- **スケーラブル設計** - 複数エージェント、大規模運用に対応

---

## セットアップ手順

### 前提条件

- ✅ .NET 10 SDK インストール済み
- ✅ Dev Tunnel 設定済み（`https://7hfqnlhn-5192.asse.devtunnels.ms`）
- ✅ Ollama実行中（`http://localhost:11434` - qwen2.5:latest）
- ✅ Microsoft Graph API権限設定済み（Mail.ReadWrite, Calendars.ReadWrite）

### 1. アプリケーション起動

```bash
cd /Users/tk3214/GitHub/POC-Agent365SDK-TeamsAgent/SalesSupportAgent

# Dev Tunnel起動（別ターミナル）
devtunnel host -p 5192 --allow-anonymous

# アプリケーション実行
dotnet run
```

### 2. Observability Dashboardアクセス

ブラウザで以下のURLを開く：

```
http://localhost:5192/observability.html
```

**期待される表示：**
- ✅ 右上に「✓ 接続済み」ステータス（緑色）
- ✅ メトリクスカード（総リクエスト、平均応答時間、成功率、稼働時間）
- ✅ アクティブエージェント情報
- ✅ リアルタイムトレースリスト

### 3. Teams Bot Web Chatアクセス

別のブラウザタブで以下を開く：

```
https://7hfqnlhn-5192.asse.devtunnels.ms
```

または、ローカル：

```
http://localhost:5192
```

**期待される表示：**
- ✅ Teams Bot Web Chatウィジェット
- ✅ ウェルカムメッセージ

---

## デモシナリオ

### 🎬 シナリオ1: 基本的なObservability体験

**目的**: Agent 365のリアルタイムトレース機能を体感

**手順**:

1. **Dashboardを表示**
   - `http://localhost:5192/observability.html`を開く
   - 初期状態のメトリクスを確認（総リクエスト: 0, 成功率: 100%）

2. **Bot操作**
   - Web Chatで「今週の商談状況を教えてください」と入力
   - Adaptive Card応答を待つ（約5-10秒）

3. **Dashboard確認**
   - リアルタイムトレース更新を確認：
     - 🚀 商談サマリ生成開始
     - ⚙️ AIエージェント実行中
     - ✅ AIエージェント実行完了
     - 🎉 商談サマリ生成完了
   - メトリクス更新を確認：
     - 総リクエスト: 1
     - 平均応答時間: 5000-8000ms
     - 成功率: 100%

**フォーカルポイント**:
- ✨ **ゼロ遅延表示**: Botが応答する前にDashboardでトレースが表示される
- ✨ **透明性**: エージェントの内部処理が可視化
- ✨ **メトリクス自動計算**: 平均応答時間、成功率が自動更新

---

### 🎬 シナリオ2: 複数リクエストでのメトリクス変化

**目的**: メトリクス集計機能とパフォーマンス傾向の可視化

**手順**:

1. **連続リクエスト実行**
   - Web Chatで以下を順番に入力（各5-10秒間隔）：
     - 「今週の商談状況を教えてください」
     - 「今月のメールサマリをください」
     - 「次の予定を教えてください」

2. **Dashboard観察**
   - トレースリストに新しいエントリが追加されるタイミングを確認
   - メトリクスの変化を観察：
     - 総リクエスト: 3
     - 平均応答時間: 各リクエストの平均値
     - 成功率: 100%（エラーがない場合）

3. **トレース履歴確認**
   - 最新20件のトレースが時系列で表示
   - 色分けステータス：
     - 🔵 青 = info（進行中）
     - 🟢 緑 = success（成功）
     - 🔴 赤 = error（エラー）
     - 🟡 黄 = warning（警告）

**フォーカルポイント**:
- ✨ **メトリクス精度**: 複数リクエストでの平均値計算
- ✨ **履歴管理**: 最新100件を保持、画面は20件表示
- ✨ **自動リフレッシュ**: 30秒ごとにメトリクス取得

---

### 🎬 シナリオ3: エラーハンドリングと監視

**目的**: エラー検知とアラート機能のデモ

**手順**:

1. **エラー発生条件作成**
   - Ollamaを一時停止（意図的なエラー発生）:
     ```bash
     # 別ターミナルで
     pkill ollama
     ```

2. **エラーリクエスト実行**
   - Web Chatで「今週の商談状況を教えてください」と入力
   - エラーレスポンスを確認

3. **Dashboard確認**
   - エラートレースが赤色で表示：
     - ❌ エラー: HTTP request failed
   - メトリクス更新：
     - 成功率が低下（例: 75%）
     - 総リクエスト増加

4. **復旧確認**
   - Ollama再起動:
     ```bash
     ollama serve
     ```
   - 正常リクエスト実行
   - 成功率が回復することを確認

**フォーカルポイント**:
- ✨ **エラー可視化**: 赤色トレースで即座に問題を認識
- ✨ **成功率計算**: 総リクエストvs成功リクエストの比率
- ✨ **自動復旧追跡**: 復旧後の成功率上昇を確認

---

### 🎬 シナリオ4: エンタープライズ統合（Application Insights）

**目的**: 既存監視システムとの統合可能性を示す

**手順**:

1. **OpenTelemetry設定確認**
   - `Program.cs`の`AddOpenTelemetry()`設定を表示
   - AgentMetrics.SourceName統合を説明

2. **Application Insights統合（オプション）**
   - `appsettings.json`にApplication Insights接続文字列を追加:
     ```json
     {
       "ApplicationInsights": {
         "ConnectionString": "InstrumentationKey=YOUR_KEY"
       }
     }
     ```
   - NuGetパッケージ追加:
     ```bash
     dotnet add package Microsoft.ApplicationInsights.AspNetCore
     ```

3. **トレース確認**
   - Application Insightsポータルでトレースを確認
   - カスタムメトリクスとして`agent.sales_summary`が表示

**フォーカルポイント**:
- ✨ **標準準拠**: OpenTelemetry標準で任意の監視ツールと統合
- ✨ **エンタープライズ対応**: Application Insights, Grafana, Prometheusと連携可能
- ✨ **ゼロ設定**: コード変更なしで監視システム切り替え

---

## フォーカルポイント

### 🎯 デモ中に強調すべきポイント

#### 1. Agent 365の透明性（Transparency）

**説明**:
> 「従来のAIエージェントはブラックボックスでしたが、Agent 365ではすべての処理が可視化されます。このダッシュボードで、エージェントが今何をしているか、どのツールを実行したか、どのくらい時間がかかったかが一目瞭然です。」

**実演**:
- Botにメッセージ送信
- Dashboardでリアルタイムトレース表示
- 「🚀 商談サマリ生成開始」→「⚙️ AIエージェント実行中」→「✅ 完了」の流れを追跡

#### 2. リアルタイム性（Real-Time Observability）

**説明**:
> 「SignalRによる双方向通信で、エージェントの処理状況が即座にダッシュボードに反映されます。運用チームはエージェントの健全性をリアルタイムで監視できます。」

**実演**:
- Botリクエスト送信と同時にDashboardを表示
- トレースが遅延なく追加されることを確認
- 「0.5秒以内にトレースが表示される」ことを強調

#### 3. メトリクス駆動の品質保証（Metrics-Driven QA）

**説明**:
> 「成功率、平均応答時間、稼働時間などのメトリクスで、エージェントのパフォーマンスを数値で管理できます。SLAの設定と監視が可能です。」

**実演**:
- メトリクスカードを表示
- 「成功率95%以上」などのSLA基準を説明
- 平均応答時間の推移を複数リクエストで示す

#### 4. エンタープライズ統合（Enterprise Integration）

**説明**:
> 「OpenTelemetry標準に準拠しているため、Application Insights、Grafana、Prometheusなど既存の監視システムとシームレスに統合できます。ゼロからの監視インフラ構築は不要です。」

**実演**:
- Program.csのAddOpenTelemetry()コードを表示
- Application Insights統合例を紹介（スクリーンショット準備）
- 「コード変更なしで監視システム切り替え可能」を強調

#### 5. DevOps & MLOps対応（DevOps/MLOps Ready）

**説明**:
> 「CI/CDパイプラインから自動テスト、本番監視まで、DevOpsベストプラクティスを適用できます。AIエージェントも従来のアプリケーションと同様に運用できます。」

**実演**:
- トレース履歴をスクロール表示
- 「過去のトレースから問題を特定」するシナリオ
- メトリクスAPIをPostmanで呼び出し（CI/CD統合例）

---

## トラブルシューティング

### Issue 1: Dashboard接続エラー「✗ 切断」

**症状**:
- Dashboardの右上に「✗ 切断」と表示
- トレースが表示されない

**原因**:
- アプリケーションが起動していない
- SignalR Hubがマップされていない

**解決策**:
```bash
# アプリケーション再起動
dotnet run

# ブラウザコンソールでエラー確認
# F12 → Console → SignalR関連エラーを確認
```

---

### Issue 2: メトリクスが更新されない

**症状**:
- Botにメッセージを送信してもメトリクスが0のまま

**原因**:
- ObservabilityServiceが正しく注入されていない
- RecordRequestAsync()が呼び出されていない

**解決策**:
```csharp
// SalesAgent.cs で確認
await _observabilityService.RecordRequestAsync(success: true, stopwatch.ElapsedMilliseconds);
await _observabilityService.UpdateMetricsAsync();
```

---

### Issue 3: トレースが表示されるが色分けされない

**症状**:
- トレースは表示されるが、すべて同じ色

**原因**:
- TraceEventのstatusプロパティが正しく設定されていない

**解決策**:
```csharp
// ObservabilityService.cs 確認
await RecordTraceAsync("🚀 商談サマリ生成開始", "info", 0);  // ✅ "info"
await RecordTraceAsync("✅ AIエージェント実行完了", "success", ms);  // ✅ "success"
await RecordTraceAsync($"❌ エラー: {ex.Message}", "error", ms);  // ✅ "error"
```

---

### Issue 4: Botが応答しない

**症状**:
- Web Chatでメッセージ送信後、応答がない

**原因**:
- Ollamaが停止している
- Graph API権限が不足

**解決策**:
```bash
# Ollama確認
ollama list
ollama run qwen2.5:latest
curl http://localhost:11434/api/tags

# Graph API権限確認
# Azure Portal → Enterprise Applications → アプリ検索 → API permissions
```

---

## まとめ

### ✅ デモ完了チェックリスト

- [ ] Dashboardが正常に表示され、「✓ 接続済み」ステータス
- [ ] Botメッセージ送信でトレースがリアルタイム表示
- [ ] メトリクス（総リクエスト、平均応答時間、成功率）が正しく更新
- [ ] エラーシナリオでエラートレース（赤色）が表示
- [ ] 複数リクエストでメトリクスが正しく集計

### 🚀 次のステップ

1. **Notifications機能追加** - リアルタイム進捗通知
2. **Transcript & Storage** - 会話履歴の永続化とクエリ
3. **Application Insights統合** - エンタープライズ監視システム連携
4. **カスタムメトリクス** - ビジネスKPI（商談件数、売上予測など）の追加

### 📚 参考資料

- [Microsoft Agent 365 SDK Documentation](https://github.com/microsoft/agent-framework)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Application Insights OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable)

---

**作成日**: 2025年1月  
**バージョン**: 1.0  
**対象**: Agent 365 SDK Observability Platform Demo
