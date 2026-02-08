# Azure Production Environment Deployment Guide

> **Language**: [🇯🇵 日本語](../DEPLOYMENT-AZURE.md) | 🇬🇧 English

## 📋 Overview

This guide explains the procedures for deploying the Sales Support Agent to an Azure production environment.

**Deployment Options**:
- **Azure App Service** - Simple, pay-as-you-go, scalable
- **Azure Container Apps** - Container-based, for microservices
- **Azure Kubernetes Service (AKS)** - Enterprise-grade, advanced control

**Note**: This guide provides procedural instructions only and does not include implementation code.

---

## 🎯 Prerequisites

| Item | Required | Description |
|------|:--------:|-------------|
| **Azure Subscription** | ✅ | Valid subscription |
| **Azure CLI** | ✅ | Verify with `az --version` |
| **Docker** | ⚪ | For Container Apps/AKS |
| **kubectl** | ⚪ | For AKS |
| **Locally Verified** | ✅ | [Getting Started](GETTING-STARTED.md) completed |

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
| **Cost (Minimum)** | $60/month | $40/month | $120/month |
| **Scalability** | 🔼 Medium | 🔼🔼 High | 🔼🔼🔼 Highest |
| **Managed Identity** | ✅ | ✅ | ✅ |
| **Custom Domain** | ✅ | ✅ | ✅ |
| **Let's Encrypt SSL** | ✅ | ✅ | ✅ (Ingress) |
| **Recommended Use Case** | Small to medium | Microservices | Enterprise |

### Recommendations

| Scenario | Recommended Method | Reason |
|----------|-------------------|--------|
| **First deployment** | App Service | Simplest, GUI-complete |
| **Cost-focused** | Container Apps | Low cost, pay-as-you-go |
| **Microservices** | Container Apps | Container-native |
| **Existing K8s env** | AKS | Infrastructure consistency |
| **High availability** | AKS | Enterprise-grade |

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

**Required only for Container Apps / AKS**

```bash
# Create ACR
az acr create \
  --resource-group rg-salesagent-prod \
  --name salesagentacr \
  --sku Basic

# Enable admin (for development)
az acr update \
  --name salesagentacr \
  --admin-enabled true

# Get login credentials
az acr credential show --name salesagentacr
```

---

### 2.4. Build Container Image (For Container Apps/AKS)

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
```

**SKU Comparison**:

| Tier | vCPU | RAM | Monthly (Est.) | Recommended Use |
|------|------|-----|---------------|-----------------|
| **B1** | 1 | 1.75GB | $60 | Dev/Test |
| **S1** | 1 | 1.75GB | $120 | Small production |
| **P1V2** | 1 | 3.5GB | $200 | Medium production |
| **P2V2** | 2 | 7GB | $400 | High load production |

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

# Record the output principalId
# Example: "principalId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

**Grant Graph API permissions**: Refer to [Authentication Guide](AUTHENTICATION.md#42-managed-identity-configuration-on-app-service)

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
    LLM__AzureOpenAI__DeploymentName="gpt-4o"
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

**Method B: GitHub Actions** (described later)

---

#### 3.6. Verification

```bash
# Health check
curl https://salesagent-prod.azurewebsites.net/health

# Expected output:
# {"Status":"Healthy","M365Configured":true,"LLMProvider":"AzureOpenAI"}

# Observability Dashboard
# Browser: https://salesagent-prod.azurewebsites.net/observability.html
```

---

## 4. Option B: Azure Container Apps

### Recommended: Cost Efficiency and Scalability Focus

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
  --cpu 1.0 \
  --memory 2.0Gi \
  --min-replicas 1 \
  --max-replicas 3
```

---

## 5. Option C: Azure Kubernetes Service (AKS)

### Recommended: Enterprise High Availability Environment

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
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
```

---

## 6. Application Insights Integration

### Common for All Deployment Methods

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

## 7. CI/CD Pipeline

### GitHub Actions Workflow Example

**.github/workflows/deploy-azure.yml**:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

env:
  AZURE_WEBAPP_NAME: salesagent-prod
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
    
    - name: Build
      run: dotnet build -c Release
      working-directory: ./SalesSupportAgent
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
      working-directory: ./SalesSupportAgent
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        package: ./SalesSupportAgent/publish
```

---

## 8. Cost Optimization

### Monthly Cost Estimate

| Resource | SKU | Monthly (Est.) |
|----------|-----|---------------|
| **App Service B1** | 1 vCPU, 1.75GB RAM | $60 |
| **Container Apps** | 1 vCPU, 2GB RAM (0.5 avg) | $40 |
| **AKS** | 2 nodes Standard_D2s_v3 | $140 |
| **Application Insights** | 5GB/month | $20 |
| **Azure OpenAI** | GPT-4o (1M tokens) | $15-60 |

---

## 9. Monitoring and Alerts

### Recommended Alert Settings

| Metric | Threshold | Action |
|--------|-----------|--------|
| **HTTP 5xx errors** | > 5 per 5min | Email notification |
| **Response time** | > 5 seconds | Teams notification |
| **CPU usage** | > 80% | Scale out |
| **Memory usage** | > 85% | Email notification |

---

## 📚 Related Documentation

- [Getting Started](GETTING-STARTED.md) - Local environment setup
- [Authentication](AUTHENTICATION.md) - Managed Identity configuration
- [Troubleshooting](TROUBLESHOOTING.md) - Deployment error handling
- [Architecture](ARCHITECTURE.md) - System configuration

---

Once production deployment is complete, start monitoring with [Observability Dashboard](OBSERVABILITY-DASHBOARD.md)! 🚀
