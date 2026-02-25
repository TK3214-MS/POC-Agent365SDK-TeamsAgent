# Azure Production Deployment Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../DEPLOYMENT-AZURE.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](DEPLOYMENT-AZURE.md)

## 📋 Overview

This guide explains how to deploy the Sales Support Agent to an Azure production environment.

**Deployment Options**:
- **Azure App Service** - Simple, pay-as-you-go, scalable
- **Azure Container Apps** - Container-based, for microservices
- **Azure Kubernetes Service (AKS)** - Enterprise-grade, advanced control

**Note**: This guide is a procedural guide only and does not contain implementation code.

---

## 🎯 Prerequisites

| Item | Required | Description |
|------|:--------:|-------------|
| **Azure Subscription** | ✅ | Active subscription |
| **Azure CLI** | ✅ | Verify with `az --version` |
| **Docker** | ⚪ | Required for Container Apps/AKS |
| **kubectl** | ⚪ | Required for AKS |
| **Local verification complete** | ✅ | [Getting Started](GETTING-STARTED.md) completed |

---

## 📚 Table of Contents

1. [Deployment Method Comparison](#1-deployment-method-comparison)
2. [Common: Preparation](#2-common-preparation)
3. [Option A: Azure App Service](#3-option-a-azure-app-service)
4. [Option B: Azure Container Apps](#4-option-b-azure-container-apps)
5. [Option C: Azure Kubernetes Service](#5-option-c-azure-kubernetes-service)
6. [Application Insights Integration](#6-application-insights-integration)
7. [CI/CD Pipeline](#7-cicd-pipeline)
8. [Cost Optimization](#8-cost-optimization)
9. [Monitoring and Alerts](#9-monitoring-and-alerts)

---

## 1. Deployment Method Comparison

### Comparison Table

| Item | App Service | Container Apps | AKS |
|------|------------|---------------|-----|
| **Setup Time** | 15-30 min | 30-45 min | 1-2 hours |
| **Complexity** | ⭐ Low | ⭐⭐ Medium | ⭐⭐⭐ High |
| **Cost (minimum)** | ~¥5,000/mo | ~¥3,000/mo | ~¥10,000/mo |
| **Scalability** | 🔼 Medium | 🔼🔼 High | 🔼🔼🔼 Highest |
| **Managed Identity** | ✅ | ✅ | ✅ |
| **Custom Domain** | ✅ | ✅ | ✅ |
| **Let's Encrypt SSL** | ✅ | ✅ | ✅ (Ingress) |
| **Recommended Use Case** | Small-Medium | Microservices | Enterprise |

### Recommendations

| Scenario | Recommended | Reason |
|----------|-------------|--------|
| **First deployment** | App Service | Simplest, GUI-based |
| **Cost priority** | Container Apps | Low cost, pay-as-you-go |
| **Microservices** | Container Apps | Container native |
| **Existing Kubernetes** | AKS | Infrastructure unification |
| **High availability & scale** | AKS | Enterprise grade |

---

## 2. Common: Preparation

### 2.1. Azure CLI Login

```bash
# Login to Azure
az login

# Verify subscription
az account list --output table

# Set subscription to use
az account set --subscription "your-subscription-id"
```

---

### 2.2. Create Resource Group

```bash
# Create resource group
az group create \
  --name rg-salesagent-prod \
  --location eastus

# Verify
az group show --name rg-salesagent-prod
```

---

### 2.3. Create Azure Container Registry (ACR)

**Required only when using Container Apps / AKS**

```bash
# Create ACR
az acr create \
  --resource-group rg-salesagent-prod \
  --name salesagentacr \
  --sku Basic

# Enable Admin (for development)
az acr update \
  --name salesagentacr \
  --admin-enabled true

# Get login credentials
az acr credential show --name salesagentacr
```

---

### 2.4. Build Container Image (for Container Apps/AKS)

#### Create Dockerfile

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

#### Build & Push Image

```bash
cd /path/to/SalesSupportAgent

# Login to ACR
az acr login --name salesagentacr

# Build image
docker build -t salesagentacr.azurecr.io/salesagent:v1.0.0 .

# Push
docker push salesagentacr.azurecr.io/salesagent:v1.0.0

# Verify
az acr repository list --name salesagentacr --output table
```

---

## 3. Option A: Azure App Service

### Recommended: Simple and Quick Deployment

#### 3.1. Create App Service Plan

```bash
# Create App Service Plan (B1: Basic tier)
az appservice plan create \
  --name plan-salesagent-prod \
  --resource-group rg-salesagent-prod \
  --sku B1 \
  --is-linux

# Scale up (production)
# az appservice plan update --name plan-salesagent-prod --resource-group rg-salesagent-prod --sku P1V2
```

**SKU Comparison**:

| Tier | vCPU | RAM | Monthly (est.) | Recommended Use |
|------|------|-----|----------------|-----------------|
| **B1** | 1 | 1.75GB | ¥5,500 | Development/Testing |
| **S1** | 1 | 1.75GB | ¥11,000 | Small-scale Production |
| **P1V2** | 1 | 3.5GB | ¥18,000 | Medium-scale Production |
| **P2V2** | 2 | 7GB | ¥36,000 | High-load Production |

---

#### 3.2. Create Web App

```bash
# Create .NET 10 Web App
az webapp create \
  --resource-group rg-salesagent-prod \
  --plan plan-salesagent-prod \
  --name salesagent-prod \
  --runtime "DOTNET|10.0"

# Enable HTTPS only
az webapp update \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --https-only true
```

---

#### 3.3. Enable Managed Identity

```bash
# Enable system-assigned Managed Identity
az webapp identity assign \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod

# Note the output principalId
# Example: "principalId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

**Grant Graph API permissions**: Refer to [Authentication Guide](AUTHENTICATION.md#42-managed-identity-setup-on-app-service)

---

#### 3.4. Application Settings

```bash
# Set environment variables
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

#### 3.5. Deploy

**Method A: ZIP Deploy with Azure CLI**

```bash
cd /path/to/SalesSupportAgent

# Publish
dotnet publish -c Release -o ./publish

# Create ZIP
cd publish
zip -r ../salesagent.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --src salesagent.zip
```

**Method B: GitHub Actions** (described below)

**Method C: Visual Studio**

1. Right-click solution → **Publish**
2. **Azure** → **Azure App Service (Linux)**
3. Select subscription and App Service
4. **Publish**

---

#### 3.6. Verify Operation

```bash
# Health check
curl https://salesagent-prod.azurewebsites.net/health

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# Observability Dashboard
# Open https://salesagent-prod.azurewebsites.net/observability.html in browser
```

---

#### 3.7. Custom Domain (Optional)

```bash
# Add custom domain
az webapp config hostname add \
  --resource-group rg-salesagent-prod \
  --webapp-name salesagent-prod \
  --hostname salesagent.yourdomain.com

# SSL certificate binding (Managed Certificate - free)
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

### Recommended: Cost-efficiency & Scalability Focus

#### 4.1. Create Container Apps Environment

```bash
# Install Container Apps extension
az extension add --name containerapp --upgrade

# Create environment
az containerapp env create \
  --name env-salesagent-prod \
  --resource-group rg-salesagent-prod \
  --location eastus
```

---

#### 4.2. Create Container App

```bash
# Deploy from ACR
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

#### 4.3. Enable Managed Identity

```bash
# Enable system-assigned Managed Identity
az containerapp identity assign \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --system-assigned

# Note the principalId and grant Graph API permissions (see Authentication Guide)
```

---

#### 4.4. Scaling Rule Configuration

```bash
# HTTP traffic-based scaling
az containerapp update \
  --name salesagent-prod \
  --resource-group rg-salesagent-prod \
  --scale-rule-name http-rule \
  --scale-rule-type http \
  --scale-rule-http-concurrency 10
```

---

#### 4.5. Custom Domain

```bash
# Add custom domain
az containerapp hostname add \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --hostname salesagent.yourdomain.com

# Managed Certificate (free)
az containerapp hostname bind \
  --resource-group rg-salesagent-prod \
  --name salesagent-prod \
  --hostname salesagent.yourdomain.com \
  --environment env-salesagent-prod \
  --validation-method HTTP
```

---

## 5. Option C: Azure Kubernetes Service (AKS)

### Recommended: Enterprise & High-availability Environment

#### 5.1. Create AKS Cluster

```bash
# Create AKS cluster (2 nodes, Standard_D2s_v3)
az aks create \
  --resource-group rg-salesagent-prod \
  --name aks-salesagent-prod \
  --node-count 2 \
  --node-vm-size Standard_D2s_v3 \
  --enable-managed-identity \
  --generate-ssh-keys \
  --attach-acr salesagentacr

# Get kubectl credentials
az aks get-credentials \
  --resource-group rg-salesagent-prod \
  --name aks-salesagent-prod
```

---

#### 5.2. Create Kubernetes Manifests

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

#### 5.3. Create Secrets

```bash
# Create Kubernetes secrets
kubectl create secret generic salesagent-secrets \
  --from-literal=m365-client-id="your-app-id" \
  --from-literal=openai-api-key="your-api-key"
```

---

#### 5.4. Deploy

```bash
# Apply manifests
kubectl apply -f deployment.yaml

# Verify
kubectl get deployments
kubectl get pods
kubectl get services

# Check logs
kubectl logs -l app=salesagent --tail=100
```

---

#### 5.5. Ingress Configuration (HTTPS)

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
# Install Cert-Manager (Let's Encrypt SSL)
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer
# (Omitted: refer to cert-manager documentation)

# Apply Ingress
kubectl apply -f ingress.yaml
```

---

## 6. Application Insights Integration

### Common Across All Deployment Methods

#### 6.1. Create Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app salesagent-insights \
  --location eastus \
  --resource-group rg-salesagent-prod \
  --application-type web

# Get Instrumentation Key
az monitor app-insights component show \
  --app salesagent-insights \
  --resource-group rg-salesagent-prod \
  --query instrumentationKey -o tsv
```

---

#### 6.2. Application Configuration

**appsettings.json** or **environment variables**:

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

#### 6.3. Add NuGet Package

```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

**Program.cs**:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

---

## 7. CI/CD Pipeline

### GitHub Actions Workflow Example

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

## 8. Cost Optimization

### Monthly Cost Estimates

| Resource | SKU | Monthly (est.) |
|----------|-----|----------------|
| **App Service B1** | 1 vCPU, 1.75GB RAM | ¥5,500 |
| **App Service P1V2** | 1 vCPU, 3.5GB RAM | ¥18,000 |
| **Container Apps** | 1 vCPU, 2GB RAM (0.5 replica avg) | ¥3,000 |
| **AKS** | 2 nodes Standard_D2s_v3 | ¥12,000 |
| **ACR Basic** | - | ¥600 |
| **Application Insights** | 5GB/mo | ¥1,500 |
| **Azure OpenAI** | GPT-4o (1M tokens) | ¥1,000-5,000 |

### Cost Reduction Tips

1. **Azure Hybrid Benefit**: App Service discounts with Windows Server/SQL Server licenses
2. **Reserved Instances**: Up to 72% discount with 1-year/3-year commitments
3. **Container Apps**: Pay-as-you-go with 0 replicas when idle
4. **Dev/Test Pricing**: Discounts for development/test environments
5. **Auto-shutdown**: Automatically stop development environments at night/weekends

---

## 9. Monitoring and Alerts

### Recommended Alert Configuration

| Metric | Threshold | Action |
|--------|-----------|--------|
| **HTTP 5xx Errors** | > 5/5min | Email notification |
| **Response Time** | > 5 seconds | Teams notification |
| **CPU Usage** | > 80% | Scale out |
| **Memory Usage** | > 85% | Email notification |
| **Failed Dependencies** | > 3/5min | Email notification |

### Create Azure Monitor Alerts

```bash
# CPU usage alert
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

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Local environment setup
- [Authentication](AUTHENTICATION.md) - Managed Identity configuration
- [Troubleshooting](TROUBLESHOOTING.md) - Deployment error resolution
- [Architecture](ARCHITECTURE.md) - System architecture

---

## 🔗 External Links

- [Azure App Service](https://learn.microsoft.com/azure/app-service/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure Kubernetes Service](https://learn.microsoft.com/azure/aks/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

Once production deployment is complete, start monitoring with the [Observability Dashboard](OBSERVABILITY-DASHBOARD.md)! 🚀
