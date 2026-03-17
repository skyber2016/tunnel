# SSH Tunnel Manager

> A blazing-fast, Native AOT CLI tool for managing multi-port SSH tunnels on Ubuntu — powered by .NET 9.

[![License: MIT](https://img.shields.io/badge/License-MIT-cyan.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/platform-linux--x64%20%7C%20linux--arm64-green.svg)]()

---

## Quick Install

```bash
curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/install.sh | bash
```

> Automatically detects your architecture (`x86_64` → linux-x64, `aarch64` → linux-arm64) and installs the daemon as a systemd user service.

---

## Requirements

- Ubuntu 20.04 or later (or any systemd-based Linux distro)
- `curl` installed
- SSH private key for your jump host

---

## Usage

### 1. Create a profile

```bash
tunnel new prod-db
```

You'll be prompted for:
- Jump host address (e.g., `jump.example.com`)
- SSH user  
- SSH port (default: `22`)
- SSH private key path (default: `~/.ssh/id_rsa`)

---

### 2. Add port mappings

```bash
# Forward PostgreSQL
tunnel add prod-db --local 5432 --remote 5432

# Forward Redis on an internal host
tunnel add prod-db --local 6379 --remote 6379 --remote-host redis.internal

# Forward a web server
tunnel add prod-db --local 8080 --remote 80
```

---

### 3. Connect

```bash
tunnel use prod-db
```

---

### 4. View active connections

```bash
tunnel status
```

Output:

```
─────────────── ● CONNECTED — prod-db ───────────────
Jump Host: admin@jump.example.com:22

╭───┬────────────┬───────────┬────────────────────┬────────╮
│ # │ Local Port │ Direction │ Remote             │ Status │
├───┼────────────┼───────────┼────────────────────┼────────┤
│ 1 │ :5432      │     →     │ 127.0.0.1:5432     │ ● OPEN │
│ 2 │ :6379      │     →     │ redis.internal:6379│ ● OPEN │
╰───┴────────────┴───────────┴────────────────────┴────────╯
2 port(s) forwarded.
```

---

### 5. Stop tunnel

```bash
tunnel stop
```

---

### 6. Self-update

```bash
tunnel update             # Update both CLI and daemon
tunnel update --daemon-only
```

---

## All Commands

| Command | Description |
|---------|-------------|
| `tunnel new <name>` | Create a new SSH profile (interactive) |
| `tunnel add <profile> --local N --remote N [--remote-host H]` | Add port mapping |
| `tunnel list` | List all profiles |
| `tunnel use <name>` | Connect using a profile |
| `tunnel stop` | Stop the active tunnel |
| `tunnel status` | Show per-port connection status |
| `tunnel update [--daemon-only]` | Self-update from GitHub Releases |

---

## Profile Config (`~/.tunnel/profiles.json`)

Managed automatically by the daemon. Manual edit example:

```json
{
  "profiles": [
    {
      "name": "prod-db",
      "jumpHost": {
        "host": "jump.example.com",
        "user": "admin",
        "port": 22,
        "keyPath": "~/.ssh/id_rsa"
      },
      "ports": [
        { "local": 5432, "remote": 5432, "remoteHost": "127.0.0.1" },
        { "local": 6379, "remote": 6379, "remoteHost": "redis.internal" }
      ]
    }
  ]
}
```

---

## Daemon Management

```bash
# Check daemon status
systemctl --user status tunnel

# View live logs
journalctl --user -u tunnel.service -f

# Restart daemon
systemctl --user restart tunnel

# Stop daemon
systemctl --user stop tunnel
```

---

## Build from Source

Requires **.NET 9 SDK** on **Linux** (Native AOT cross-compile from Windows is not supported).

```bash
git clone https://github.com/skyber2016/tunnel.git
cd tunnel

# Build CLI
cd src/Tunnel.Cli
dotnet publish -r linux-x64 -c Release -o ./bin/linux-x64

# Build Daemon
cd ../Tunnel.Daemon
dotnet publish -r linux-x64 -c Release -o ./bin/linux-x64

# Build for ARM64
dotnet publish -r linux-arm64 -c Release -o ./bin/linux-arm64
```

---

## Architecture

```
tunnel (CLI)             tunnel-daemon (Background Service)
System.CommandLine  ──►  Minimal API  localhost:6385
Spectre.Console          SSH.NET ForwardedPortLocal
HttpClient               ~/.tunnel/profiles.json
```

- **Daemon**: Runs as a `systemd --user` service, exposes Minimal API on `localhost:6385`
- **CLI**: Connects to daemon via Basic Auth HTTP, uses Spectre.Console for rich terminal UI
- **Config**: Daemon is the single source of truth for `~/.tunnel/profiles.json`

---

## License

MIT © [skyber2016](https://github.com/skyber2016)
