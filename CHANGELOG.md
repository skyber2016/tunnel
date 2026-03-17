# Changelog

All notable changes to this project are documented in this file.

---

## [1.1.0] — 2026-03-17

### ✨ New Commands

#### `tunnel clean`
- Prompts for confirmation before wiping everything
- Stops the active tunnel (if running)
- Overwrites `~/.tunnel/profiles.json` with an empty config
- Guides user to create a new profile after reset

#### `tunnel remove`
- `--name <alias>` — remove a named port forwarding rule from the active profile (with confirm)
- `--profile-name <name>` — delete an entire profile; auto-stops tunnel if profile is active

#### `tunnel reconnect`
- `--profile-name <name>` — close and reopen the full SSH connection and all port forwarders
- `--name <alias>` — stop and restart a single named port forwarding within the active profile

### 🔄 Changed Commands

#### `tunnel add`
- **Breaking:** removed positional `<profile>` argument
- Added `--name <alias>` (required) — unique identifier for `tunnel remove` / `tunnel reconnect`
- Requires an **active profile** (`tunnel use <name>` first) — adds to current profile automatically
- Validates that the alias is unique within the profile

#### `tunnel list`
- New column layout: **Profile | Host | User | Ports | Status**
- Active profile shown with `● ACTIVE` badge (green)
- Falls back to local-only display if daemon is offline

#### `tunnel status`
- New column layout: **Name | Profile | Local Port | Remote Host | Remote Port | Status**
- Requires an active profile — shows helpful hint if idle
- Per-port `● OPEN` / `○ CLOSED` badges

### 🐛 Bug Fixes

- **CS0104 ambiguous reference** — `ConnectionInfo` was ambiguous between `Renci.SshNet.ConnectionInfo` and `Microsoft.AspNetCore.Http.ConnectionInfo`; resolved with a using alias in `TunnelService.cs`

### 🏗️ Infrastructure

- Dropped **linux-arm64** build target. Only `linux-x64` is released.
- `PortMapping` model: added `Name` field (profile configs must be updated)
- `PortStatus` model: added `Name` and `Profile` fields
- New API endpoints:
  - `POST /api/remove/port`
  - `POST /api/remove/profile`
  - `POST /api/reconnect`
  - `POST /api/clean`
- `TunnelService` now tracks `(PortMapping, ForwardedPortLocal)` tuples to enable named port management

### ⚠️ Breaking Changes

> [!IMPORTANT]
> **`tunnel add` signature changed.** Scripts using `tunnel add <profile> --local ... --remote ...` must be updated to `tunnel use <profile>` first, then `tunnel add --name <alias> --local ... --remote ...`.

> [!IMPORTANT]
> **Existing `profiles.json` files** without a `name` field on port mappings are still loadable (name defaults to empty string), but `tunnel remove --name` and `tunnel reconnect --name` require a non-empty name. Re-add ports with `tunnel add --name` to use these features.

---

## [1.0.0] — 2026-03-17

### 🎉 First Release

Initial public release of **SSH Tunnel Manager** — a Native AOT CLI tool for managing multi-port SSH tunnels on Ubuntu via a Systemd user service.

### ✨ Features

- `tunnel new <name>` — Interactively create a new SSH jump host profile
- `tunnel add` — Add port forwarding mappings to the active profile
- `tunnel list` — List all saved profiles and their connection status
- `tunnel use <name>` — Activate a profile and establish the SSH tunnel
- `tunnel stop` — Stop the active tunnel
- `tunnel status` — Display per-port connection status of the active profile
- `tunnel update [--daemon-only]` — Self-update from GitHub Releases

### 🏗️ Architecture

- **Daemon** (`tunnel-daemon`): Background service on `systemctl --user`, Minimal API on `localhost:6385`
- **CLI** (`tunnel`): HTTP client with Basic Auth + Spectre.Console terminal UI
- **Native AOT**: Single binary, no runtime required, ~3 MB, cold start < 50ms
- **Platform**: `linux-x64`

### 📦 Bundled

- One-line installer: `curl -sSL .../install.sh | bash`
- Docker-style uninstaller with `--purge` flag
- Systemd unit with security hardening
- GitHub Pages product landing page

### 🔧 Dependencies

| Package | Version |
|---------|---------|
| SSH.NET | 2024.2.0 |
| System.CommandLine | 2.0.0-beta4 |
| Spectre.Console | 0.49.1 |
| .NET SDK | 9.0 |

---

[1.1.0]: https://github.com/skyber2016/tunnel/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/skyber2016/tunnel/releases/tag/v1.0.0
