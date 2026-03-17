# SSH Tunnel Manager

> A blazing-fast, Native AOT CLI tool for managing multi-port SSH tunnels on Ubuntu — powered by .NET 9.

[![License: MIT](https://img.shields.io/badge/License-MIT-cyan.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com)
[![Platform](https://img.shields.io/badge/platform-linux--x64-green.svg)]()

---

## Quick Install

```bash
curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/install.sh | bash
```

> Automatically detects your architecture (`x86_64` → linux-x64, `aarch64` → linux-arm64) and installs the daemon as a systemd user service.

> [!IMPORTANT]
> **If you have a previous version installed, you must uninstall it first.** The installer will block and display an error if conflicting binaries are found.

---

## Uninstall

Before reinstalling (e.g., to upgrade), remove the existing version:

```bash
# Remove binaries and service — keeps your profile data (~/.tunnel/)
curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/uninstall.sh | bash

# Remove everything including all profile data
curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/uninstall.sh | bash -s -- --purge
```

The uninstaller will:
1. Stop and disable the `tunnel.service` systemd unit
2. Remove `/usr/local/bin/tunnel` and `/usr/local/bin/tunnel-daemon`
3. Remove the systemd unit file from `~/.config/systemd/user/`
4. **By default, your profile data (`~/.tunnel/profiles.json`) is preserved** — use `--purge` to delete it

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

### 2. Connect

```bash
tunnel use prod-db
```

---

### 3. Add port mappings (requires active profile)

```bash
# Forward PostgreSQL — named "postgres"
tunnel add --name postgres --local 5432 --remote 5432

# Forward Redis on an internal host
tunnel add --name redis --local 6379 --remote 6379 --remote-host redis.internal

# Forward a web server
tunnel add --name web --local 8080 --remote 80
```

> [!NOTE]
> `tunnel add` requires an active profile. The name (`--name`) is used to identify the rule for `tunnel remove` and `tunnel reconnect`.

---

### 4. View profiles and status

```bash
# List all profiles and their connection status
tunnel list
```

Output:
```
╭──────────┬──────────────────────┬───────┬───────┬──────────╮
│ Profile  │ Host                 │ User  │ Ports │ Status   │
├──────────┼──────────────────────┼───────┼───────┼──────────┤
│ prod-db  │ jump.example.com     │ admin │ 3     │ ● ACTIVE │
│ staging  │ jump-stg.example.com │ admin │ 1     │ ○ idle   │
╰──────────┴──────────────────────┴───────┴───────┴──────────╯
```

```bash
# Show per-port forwarding status of the active profile
tunnel status
```

Output:
```
─────── ● CONNECTED — prod-db ───────

╭──────────┬─────────┬────────────┬─────────────────┬─────────────┬────────╮
│ Name     │ Profile │ Local Port │ Remote Host      │ Remote Port │ Status │
├──────────┼─────────┼────────────┼─────────────────┼─────────────┼────────┤
│ postgres │ prod-db │ :5432      │ 127.0.0.1       │ :5432       │ ● OPEN │
│ redis    │ prod-db │ :6379      │ redis.internal  │ :6379       │ ● OPEN │
│ web      │ prod-db │ :8080      │ 127.0.0.1       │ :80         │ ● OPEN │
╰──────────┴─────────┴────────────┴─────────────────┴─────────────┴────────╯
```

---

### 5. Remove port rules or profiles

```bash
# Remove a port forwarding rule by name (from active profile)
tunnel remove --name redis

# Remove an entire profile (stops tunnel if active)
tunnel remove --profile-name staging
```

---

### 6. Reconnect

```bash
# Reconnect entire profile (close + reopen SSH + all ports)
tunnel reconnect --profile-name prod-db

# Reconnect only one port forwarding (within active profile)
tunnel reconnect --name postgres
```

---

### 7. Stop tunnel

```bash
tunnel stop
```

---

### 8. Self-update

```bash
tunnel update             # Update both CLI and daemon
tunnel update --daemon-only
```

---

## All Commands

| Command | Description |
|---------|-------------|
| `tunnel new <name>` | Create a new SSH profile (interactive) |
| `tunnel list` | List all profiles with connection status |
| `tunnel use <name>` | Connect using a profile |
| `tunnel add --name <n> --local <p> --remote <p> [--remote-host <h>]` | Add port rule to active profile |
| `tunnel stop` | Stop the active tunnel |
| `tunnel status` | Show per-port status of the active profile |
| `tunnel remove --name <n>` | Remove a port rule from active profile |
| `tunnel remove --profile-name <n>` | Remove an entire profile |
| `tunnel reconnect --profile-name <n>` | Reconnect entire profile |
| `tunnel reconnect --name <n>` | Reconnect a single port rule |
| `tunnel clean` | Delete all profiles and reset config (with confirmation) |
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
        { "name": "postgres", "local": 5432, "remote": 5432, "remoteHost": "127.0.0.1" },
        { "name": "redis",    "local": 6379, "remote": 6379, "remoteHost": "redis.internal" }
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

## 🖥️ GUI Desktop (Tauri + Angular)

Besides the CLI, a cross-platform **Desktop GUI** is available built with **Tauri (Rust)** + **Angular 19**.

### Features

- 📋 **Dashboard** — View all profiles, connection status, and per-port statuses at a glance
- ▶️ **One-click actions** — Start/Stop/Reload/Reconnect/Clean profiles
- ➕ **Add/Remove ports** — Manage port forwardings inline
- ⚠️ **Error screen** — Friendly notice when daemon isn't running, with auto-retry
- 🔒 **Secure** — Reads auth token from `~/.tunnel/.auth` automatically via Tauri IPC
- 🖥️ **System Tray** — Right-click context menu: Open GUI, Reload Config, Disconnect, Quit

### Download GUI Installer

Download the latest GUI installer from [GitHub Releases](https://github.com/skyber2016/tunnel/releases/latest):

| Format | Platform | Install Command |
|--------|----------|-----------------|
| `.deb` | Debian / Ubuntu | `sudo dpkg -i SSH_Tunnel_Manager_*_amd64.deb` |
| `.AppImage` | Universal Linux | `chmod +x SSH_Tunnel_Manager_*.AppImage && ./SSH_Tunnel_Manager_*.AppImage` |
| `.rpm` | Fedora / RHEL | `sudo rpm -i SSH_Tunnel_Manager-*.x86_64.rpm` |

> [!NOTE]
> The GUI requires the **tunnel-daemon** to be running. Install the CLI/daemon first using the [Quick Install](#quick-install) command.

### Prerequisites for GUI (Build from Source)

- [Node.js](https://nodejs.org/) 18+
- [Rust](https://rustup.rs/) 1.77+
- Platform build tools (see [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/))

### Run GUI in Dev Mode

```bash
cd src/Tunnel.Gui
npm install
npm run tauri dev
```

### Build GUI Installer

```bash
cd src/Tunnel.Gui
npm install
npm run tauri build
```

> Output installers will be in `src/Tunnel.Gui/src-tauri/target/release/bundle/`:
> - **Linux**: `.deb`, `.AppImage`, `.rpm`
> - **Windows**: `.msi` and `.exe` (NSIS)
> - **macOS**: `.dmg` and `.app`

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
```

---

## Architecture

```
tunnel (CLI)             tunnel-daemon (Background Service)
System.CommandLine  ──►  Minimal API  localhost:6385
Spectre.Console          SSH.NET ForwardedPortLocal
HttpClient               ~/.tunnel/profiles.json
                                 ▲
tunnel-gui (Desktop)             │
Tauri (Rust)  ───────────────────┘
Angular 19 + TailwindCSS
```

- **Daemon**: Runs as a `systemd --user` service, exposes Minimal API on `localhost:6385`
- **CLI**: Connects to daemon via Bearer token HTTP, uses Spectre.Console for rich terminal UI
- **GUI**: Tauri desktop app (Rust + Angular), reads token from `~/.tunnel/.auth`, talks to the same daemon API
- **Config**: Daemon is the single source of truth for `~/.tunnel/profiles.json`

---

## License

MIT © [skyber2016](https://github.com/skyber2016)
