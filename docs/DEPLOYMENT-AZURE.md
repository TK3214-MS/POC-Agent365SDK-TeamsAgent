# Azure 本番環境デプロイガイド

## 📋 概要

営業支援エージェントをAzure本番環境にデプロイする手順を説明します。

**デプロイオプション**:
- **Azure App Service** - シンプル、従量課金、スケーラブル
- **Azure Container Apps** - コンテナベース、マイクロサービス向け
- **Azure Kubernetes Service (AKS)** - エンタープライズグレード、高度な制御

**注意**: このガイドは手順説明のみで、実装コードは含まれません。

---

## 🎯 前提条件

| 項目 | 必須 | 説明 |
|-----|:----:|------|
| **Azureサブスクリプション** | ✅ | 有効なサブスクリプション |
| **Azure CLI** | ✅ | `az --version` で確認 |
| **Docker** | ⚪ | Container Apps/AKS使用時 |
| **kubectl** | ⚪ | AKS使用時 |
| **ローカルで動作確認済み** | ✅ | [Getting Started](GETTING-STARTED.md) 完了 |

---

## 📚 目次

1. [デプロイ方式の比較](#1-デプロイ方式の比較)
2. [共通: 事前準備](#2-共通-事前準備)
3. [Option A: Azure App Service](#3-option-a-azure-app-service)
4. [Option B: Azure Container Apps](#4-option-b-azure-container-apps)
5. [Option C: Azure Kubernetes Service](#5-option-c-azure-kubernetes-service)
6. [Application Insights 統合](#6-application-insights-統合)
7. [CI/CD パイプライン](#7-cicd-パイプライン)
8. [コスト最適化](#8-コスト最適化)
9. [監視とアラート](#9-監視とアラート)

---

## 1. デプロイ方式の比較

### 比較表

| 項目 | App Service | Container Apps | AKS |
|-----|------------|---------------|-----|
| **セットアップ時間** | 15-30分 | 30-45分 | 1-2時間 |
| **複雑性** | ⭐ 低 | ⭐⭐ 中 | ⭐⭐⭐ 高 |
| **コスト（最小）** | ¥5,000~/月 | ¥3,000~/月 | ¥10,000~/月 |
| **スケーラビリティ** | 🔼 中 | 🔼🔼 高 | 🔼🔼🔼 最高 |
| **Managed Identity** | ✅ | ✅ | ✅ |
| **カスタムドメイン** | ✅ | ✅ | ✅ |
| **Let's Encrypt SSL** | ✅ | ✅ | ✅（Ingress） |
| **推奨ユースケース** | 中小規模 | マイクロサービス | エンタープライズ |

### 推奨

| シナリオ | 推奨方式 | 理由 |
|---------|---------|------|
| **初めてのデプロイ** | App Service | 最もシンプル、GUIで完結 |
| **コスト重視** | Container Apps | 低コスト、従量課金 |
| **マイクロサービス化** | Container Apps | コンテナネイティブ |
| **既存Kubernetes環境** | AKS | インフラ統一 |
| **高可用性・スケール** | AKS | エンタープライズグレード |

---

## 2. 共通: 事前準備

### 2.1. Azure CLI ログイン

```bash
# Azureにログイン
az login

# サブスクリプション確認
az account list --output table

# 使用するサブスクリプションを設定
az account set --subscription "your-subscription-id"
```

---

### 2.2. リソースグループ作成

```bash
# リソースグループ作成
az group create \
  --name rg-salesagent-prod \
  --location eastus

# 確認
az group show --name rg-salesagent-prod
```

---

### 2.3. Azure Container Registry (ACR) 作成

**Container Apps / AKS を使用する場合のみ必要**

```bash
# ACR作成
az acr create \
  --resource-group rg-salesagent-prod \
  --name salesagentacr \
  --sku Basic

# Admin有効化（開発用）
az acr update \
  --name salesagentacr \
  --admin-enabled true

# ログイン情報取得
az acr credential show --name salesagentacr
```

---

### 2.4. コンテナイメージビルド（Container Apps/AKS用）

#### Dockerfile作成

`SalesSupportAgent/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SalesSupportAgent.csproj", "./"]
RUN dotnet restore "SalesSupportAgent.csproj"
COPY . .
RUN dotnet build "SalesSupportAgent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SalesSupportAgent.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SalesSupportAgent.dll"]
```

#### イメージビルド＆プッシュ

```bash
cd /path/to/SalesSupportAgent

# ACRにログイン
az acr login --name salesagentacr

# イメージビルド
docker build -t salesagentacr.azurecr.io/salesagent:v1.0.0 .

# プッシュ
docker push salesagentacr.azurecr.io/salesagent:v1.0.0

# 確認
az acr repository list --name salesagentacr --output table
```

---

## 3. Option A: Azure App Service

### 推奨: シンプルで迅速なデプロイ

#### 3.1. App Service Plan 作成

```bash
# App Service Plan作成（B1: Basic tier）
az appservice plan create \
  --name plan-salesagent-prod \
  --resource-group rg-salesagent-prod \
  --sku B1 \
  --is-linux

# スケールアップ（本番環境）
# az appservice plan update --name plan-salesagent-prod --resource-group rg-salesagent-prod --sku P1V2
```

**SKU比較**:

| Tier | vCPU | RAM | 月額（目安） | 推奨用途 |
|------|------|-----|------------|---------|
| **B1** | 1 | 1.75GB | ¥5,500 | 開発・テスト |
| **S1** | 1 | 1.75GB | ¥11,000 | 小規模本番 |
| **P1V2** | 1 | 3.5GB | ¥18,000 | 中規模本番 |
| **P2V2** | 2 | 7GB | ¥36,000 | 高負荷本番 |

---

#### 3.2. Web App 作成

```bash
# .NET 10 Web App作成
az webapp create \
  --resource-group rg-salesagent-prod \
  --plan plan-salesagent-prod \
  --name salesagent-prod \
  --runtime "DOTNET|10.0"

# HTTPS のみ有効化
az webapp update \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --https-only true
```

---

#### 3.3. Managed Identity 有効化

```bash
# システム割り当てManaged Identity有効化
az webapp identity assign \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod

# 出力されたprincipalIdを記録
# 例: "principalId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

**Graph API権限付与**: [認証設定ガイド](AUTHENTICATION.md#42-app-service-での-managed-identity-設定) を参照

---

#### 3.4. アプリケーション設定

```bash
# 環境変数設定
az webapp config appsettings set \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --settings \
    M365__ClientId="your-app-id" \
    M365__UseManagedIdentity=true \
    LLM__Provider="AzureOpenAI" \
    LLM__AzureOpenAI__Endpoint="https://your-openai.openai.azure.com" \
    LLM__AzureOpenAI__DeploymentName="gpt-4o" \
    LLM__AzureOpenAI__ApiKey="@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/OpenAIApiKey/)" \
    Bot__MicrosoftAppId="your-bot-app-id" \
    Bot__MicrosoftAppPassword="@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/BotPassword/)"
```

---

#### 3.5. デプロイ

**方法 A: Azure CLI でZIPデプロイ**

```bash
cd /path/to/SalesSupportAgent

# 発行
dotnet publish -c Release -o ./publish

# ZIP作成
cd publish
zip -r ../salesagent.zip .
cd ..

# デプロイ
az webapp deployment source config-zip \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --src salesagent.zip
```

**方法 B: GitHub Actions** (後述)

**方法 C: Visual Studio**

1. ソリューションを右クリック → **発行**
2. **Azure** → **Azure App Service (Linux)**
3. サブスクリプション・App Service選択
4. **発行**

---

#### 3.6. 動作確認

```bash
# ヘルスチェック
curl https://salesagent-prod.azurewebsites.net/health

# 期待される出力:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# Observability Dashboard
# ブラウザで https://salesagent-prod.azurewebsites.net/observability.html
```

---

#### 3.7. カスタムドメイン（オプション）

```bash
# カスタムドメイン追加
az webapp config hostname add \
  --resource-group rg-salesagent-prod \
  --webapp-name salesagent-prod \
  --hostname salesagent.yourdomain.com

# SSL証明書バインディング（Managed Certificate - 無料）
az webapp config ssl create \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --hostname salesagent.yourdomain.com

az webapp config ssl bind \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

---

## 4. Option B: Azure Container Apps

### 推奨: コスト効率・スケーラビリティ重視

#### 4.1. Container Apps 環境作成

```bash
# Container Apps拡張機能インストール
az extension add --name containerapp --upgrade

# 環境作成
az containerapp env create \
  --name env-salesagent-prod \
  --resource-group rg-salesagent-prod \
  --location eastus
```

---

#### 4.2. Container App 作成

```bash
# ACRからデプロイ
az containerapp create \
  --name salesagent-prod \
  --resource-group rg-salesagent-prod \
  --environment env-salesagent-prod \
  --image salesagentacr.azurecr.io/salesagent:v1.0.0 \
  --target-port 8080 \
  --ingress external \
  --registry-server salesagentacr.azurecr.io \
  --registry-username salesagentacr \
  --registry-password <acr-password> \
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 3 \
  --env-vars \
    M365__ClientId="your-app-id" \
    M365__UseManagedIdentity=true \
    LLM__Provider="AzureOpenAI" \
    LLM__AzureOpenAI__Endpoint="https://your-openai.openai.azure.com" \
    LLM__AzureOpenAI__DeploymentName="gpt-4o"
```

---

#### 4.3. Managed Identity 有効化

```bash
# システム割り当てManaged Identity有効化
az containerapp identity assign \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --system-assigned

# principalIdを記録してGraph API権限を付与（認証設定ガイド参照）
```

---

#### 4.4. スケーリングルール設定

```bash
# HTTPトラフィックベースのスケーリング
az containerapp update \
  --name salesagent-prod \
  --resource-group rg- salesagent-prod \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 10
```

---

#### 4.5. カスタムドメイン

```bash
# カスタムドメイン追加
az containerapp hostname add \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --hostname salesagent.yourdomain.com

# Managed Certificate（無料）
az containerapp hostname bind \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --hostname salesagent.yourdomain.com \
  --environment env-salesagent-prod \
  --validation-method HTTP
```

---

## 5. Option C: Azure Kubernetes Service (AKS)

### 推奨: エンタープライズ・高可用性環境

#### 5.1. AKS クラスター作成

```bash
# AKSクラスター作成（2 nodes, Standard_D2s_v3）
az aks create \
  --resource-group rg-salesagent-prod \
  --name aks-salesagent-prod \
  --node-count 2 \
  --node-vm-size Standard_D2s_v3 \
  --enable-managed-identity \
  --generate-ssh-keys \
  --attach-acr salesagentacr

# kubectl認証情報取得
az aks get-credentials \
  --resource-group rg-salesagent-prod \
  --name aks-salesagent-prod
```

---

#### 5.2. Kubernetes マニフェスト作成

**deployment.yaml**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: salesagent
  namespace: default
spec:
  replicas: 3
  selector:
    matchLabels:
      app: salesagent
  template:
    metadata:
      labels:
        app: salesagent
    spec:
      containers:
      - name: salesagent
        image: salesagentacr.azurecr.io/salesagent:v1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: M365__ClientId
          valueFrom:
            secretKeyRef:
              name: salesagent-secrets
              key: m365-client-id
        - name: M365__UseManagedIdentity
          value: "true"
        - name: LLM__Provider
          value: "AzureOpenAI"
        - name: LLM__AzureOpenAI__Endpoint
          value: "https://your-openai.openai.azure.com"
        - name: LLM__AzureOpenAI__DeploymentName
          value: "gpt-4o"
        - name: LLM__AzureOpenAI__ApiKey
          valueFrom:
            secretKeyRef:
              name: salesagent-secrets
              key: openai-api-key
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: salesagent-service
spec:
  selector:
    app: salesagent
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  type: LoadBalancer
```

---

#### 5.3. シークレット作成

```bash
# Kubernetesシークレット作成
kubectl create secret generic salesagent-secrets \
  --from-literal=m365-client-id="your-app-id" \
  --from-literal=openai-api-key="your-api-key"
```

---

#### 5.4. デプロイ

```bash
# マニフェスト適用
kubectl apply -f deployment.yaml

# 確認
kubectl get deployments
kubectl get pods
kubectl get services

# ログ確認
kubectl logs -l app=salesagent --tail=100
```

---

#### 5.5. Ingress 設定（HTTPS対応）

**ingress.yaml**:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: salesagent-ingress
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - salesagent.yourdomain.com
    secretName: salesagent-tls
  rules:
  - host: salesagent.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: salesagent-service
            port:
              number: 80
```

```bash
# Cert-Manager インストール（Let's Encrypt SSL）
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# ClusterIssuer作成
# （省略: cert-managerドキュメント参照）

# Ingress適用
kubectl apply -f ingress.yaml
```

---

## 6. Application Insights 統合

### すべてのデプロイ方式で共通

#### 6.1. Application Insights 作成

```bash
# Application Insights作成
az monitor app-insights component create \
  --app salesagent-insights \
  --location eastus \
  --resource-group rg-salesagent-prod \
  --application-type web

# Instrumentation Key取得
az monitor app-insights component show \
  --app salesagent-insights \
  --resource-group rg-salesagent-prod \
  --query instrumentationKey -o tsv
```

---

#### 6.2. アプリケーション設定

**appsettings.json** または **環境変数**:

```json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key",
    "EnableAdaptiveSampling": true,
    "EnableDependencyTracking": true
  }
}
```

---

#### 6.3. NuGetパッケージ追加

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**Program.cs**:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

---

## 7. CI/CD パイプライン

### GitHub Actions ワークフロー例

**.github/workflows/deploy-azure.yml**:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: salesagent-prod
  AZURE_RESOURCE_GROUP: rg-salesagent-prod
  DOTNET_VERSION: '10.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
      working-directory: ./SalesSupportAgent
    
    - name: Build
      run: dotnet build --no-restore -c Release
      working-directory: ./SalesSupportAgent
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
      working-directory: ./SalesSupportAgent
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        package: ./SalesSupportAgent/publish
```

---

## 8. コスト最適化

### 月額コスト見積もり

| リソース | SKU | 月額（目安） |
|---------|-----|------------|
| **App Service B1** | 1 vCPU, 1.75GB RAM | ¥5,500 |
| **App Service P1V2** | 1 vCPU, 3.5GB RAM | ¥18,000 |
| **Container Apps** | 1 vCPU, 2GB RAM (0.5 replica平均) | ¥3,000 |
| **AKS** | 2 nodes Standard_D2s_v3 | ¥12,000 |
| **ACR Basic** | - | ¥600 |
| **Application Insights** | 5GB/月 | ¥1,500 |
| **Azure OpenAI** | GPT-4o (1M tokens) | ¥1,000-5,000 |

### コスト削減のヒント

1. **Azure Hybrid Benefit**: Windows Server/SQL ServerライセンスでApp Service割引
2. **Reserved Instances**: 1年/3年契約で最大72%割引
3. **Container Apps**: アイドル時のレプリカ数0で従量課金
4. **Dev/Test Pricing**: 開発・テスト環境で割引適用
5. **Auto-shutdown**: 開発環境を夜間・週末自動停止

---

## 9. 監視とアラート

### 推奨アラート設定

| メトリック | しきい値 | アクション |
|----------|---------|----------|
| **HTTP 5xx エラー** | > 5件/5分 | メール通知 |
| **応答時間** | > 5秒 | Teams通知 |
| **CPU使用率** | > 80% | スケールアウト |
| **メモリ使用率** | > 85% | メール通知 |
| **失敗した依存関係** | > 3件/5分 | メール通知 |

### Azure Monitor アラート作成

```bash
# CPU使用率アラート
az monitor metrics alert create \
  --name high-cpu-alert \
  --resource-group rg-salesagent-prod \
  --scopes /subscriptions/{sub-id}/resourceGroups/rg-salesagent-prod/providers/Microsoft.Web/sites/salesagent-prod \
  --condition "avg Percentage CPU > 80" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action-group <action-group-id>
```

---

## 📚 関連ドキュメント

- [Getting Started](GETTING-STARTED.md) - ローカル環境セットアップ
- [Authentication](AUTHENTICATION.md) - Managed Identity設定
- [Troubleshooting](TROUBLESHOOTING.md) - デプロイエラー対処
- [Architecture](ARCHITECTURE.md) - システム構成

---

## 🔗 外部リンク

- [Azure App Service](https://learn.microsoft.com/azure/app-service/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure Kubernetes Service](https://learn.microsoft.com/azure/aks/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

本番環境デプロイが完了したら、[Observability Dashboard](OBSERVABILITY-DASHBOARD.md) で監視を開始しましょう！ 🚀
