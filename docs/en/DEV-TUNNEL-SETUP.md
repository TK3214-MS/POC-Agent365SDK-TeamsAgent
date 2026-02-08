# Dev Tunnel Setup Guide

[![日本語](https://img.shields.io/badge/lang-日本語-red.svg)](../DEV-TUNNEL-SETUP.md)
[![English](https://img.shields.io/badge/lang-English-blue.svg)](DEV-TUNNEL-SETUP.md)

## What is Dev Tunnel?

A development tunnel service provided by Microsoft that allows you to expose local applications externally.

### Comparison with ngrok

| Feature | Dev Tunnel | ngrok |
|---------|-----------|-------|
| **Fixed URL** | ✅ Yes (persistent) | ❌ No (Free tier) |
| **Microsoft Integration** | ✅ Yes | ❌ No |
| **Authentication** | Microsoft Account | ngrok Account |
| **Pricing** | Free | Free/Paid plans |
| **VS Code Integration** | ✅ Yes | ❌ No |

---

## Installation

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

Verify installation:

```bash
devtunnel --version
```

---

## Setup Procedure

### 1. Login

```bash
devtunnel user login
```

Browser opens and prompts to sign in with Microsoft account.

### 2. Create Tunnel (First time only)

```bash
# Create fixed tunnel
devtunnel create sales-support-agent --allow-anonymous

# Output example:
# Tunnel ID: abc123xyz
# Tunnel Name: sales-support-agent
```

**Options:**
- `--allow-anonymous`: Allow anonymous access (required for Bot Framework)
- `--expiration 30d`: Set expiration (default: unlimited)

### 3. Port Configuration

```bash
# Expose HTTPS port 5001
devtunnel port create sales-support-agent -p 5001 --protocol https
```

### 4. Start Tunnel

```bash
devtunnel host sales-support-agent
```

**Output example:**
```
Hosting port: 5001
Connect via browser: https://abc123xyz-5001.euw.devtunnels.ms
Inspect via browser: https://abc123xyz-5001-inspect.euw.devtunnels.ms

Ready to accept connections for tunnel: sales-support-agent
```

Set this **fixed URL** (`https://abc123xyz-5001.euw.devtunnels.ms`) as Bot Framework messaging endpoint.

---

## Usage

### Daily Use

Tunnel already created, just start it each time:

```bash
# Start tunnel
devtunnel host sales-support-agent
```

### Stop

Press `Ctrl + C` to stop tunnel.

---

## Troubleshooting

### List Tunnels

```bash
devtunnel list
```

### Show Tunnel Details

```bash
devtunnel show sales-support-agent
```

### Check Ports

```bash
devtunnel port list sales-support-agent
```

### Delete Tunnel

```bash
devtunnel delete sales-support-agent
```

### Check Login Status

```bash
devtunnel user show
```

---

## VS Code Integration (Optional)

Manage Dev Tunnel from VS Code GUI:

1. Open VS Code
2. **Command Palette** (`Cmd+Shift+P`)
3. Select `Dev Tunnels: Create Tunnel`
4. Select port (5001)
5. Select access setting (`Public`)

---

## Security

- **Anonymous access** (`--allow-anonymous`) is for development/testing only
- For production, enable authentication:
  ```bash
  devtunnel create sales-support-agent --allow-github
  ```

---

## References

- [Dev Tunnel Official Documentation](https://learn.microsoft.com/azure/developer/dev-tunnels/)
- [CLI Reference](https://learn.microsoft.com/azure/developer/dev-tunnels/cli-commands)
