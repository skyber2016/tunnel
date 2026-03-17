# Changelog

All notable changes to this project will be documented in this file.

---

## [1.0.0] — 2026-03-17

### 🎉 First Release

Initial public release of **SSH Tunnel Manager** — a Native AOT CLI tool for managing multi-port SSH tunnels on Ubuntu via a Systemd user service.

---

### ✨ Features

- **`tunnel new <name>`** — Interactively create a new SSH jump host profile
- **`tunnel add <profile> --local N --remote N [--remote-host H]`** — Add port forwarding mappings to a profile
- **`tunnel list`** — List all saved profiles in a table view (falls back to local file if daemon is offline)
- **`tunnel use <name>`** — Activate a profile and establish the SSH tunnel with live spinner UI
- **`tunnel stop`** — Stop the active tunnel
- **`tunnel status`** — Display a live per-port connection status table
- **`tunnel update [--daemon-only]`** — Self-update CLI and/or daemon binary from GitHub Releases with progress bar and atomic file swap

### 🏗️ Architecture

- **Daemon** (`tunnel-daemon`): Background service running as `systemctl --user`, exposes Minimal API on `localhost:6385`
- **CLI** (`tunnel`): Thin client communicating with daemon over Basic Auth HTTP
- **Native AOT**: Both binaries compiled with `.NET 9 PublishAot` — no runtime required, ~3 MB, cold start < 50ms
- **Platforms**: `linux-x64` (x86_64) and `linux-arm64` (aarch64)

### 📦 Bundled

- Profile config at `~/.tunnel/profiles.json` — managed by daemon
- One-line installer: `curl -sSL .../install.sh | bash`
- Systemd unit with security hardening: `NoNewPrivileges`, `PrivateTmp`, `ProtectSystem`
- GitHub Pages product landing page (`/docs`)

### 🔧 Dependencies

| Package | Version |
|---------|---------|
| SSH.NET | 2024.2.0 |
| System.CommandLine | 2.0.0-beta4 |
| Spectre.Console | 0.49.1 |
| .NET SDK | 9.0 |

---

[1.0.0]: https://github.com/skyber2016/tunnel/releases/tag/v1.0.0
