#!/usr/bin/env bash
# ============================================================
# install.sh — SSH Tunnel Manager Installer
# Auto-detects architecture and installs from GitHub Release
# Supports: Ubuntu linux-x64, linux-arm64
# Usage: curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/install.sh | bash
# ============================================================

set -euo pipefail

GITHUB_REPO="skyber2016/tunnel"
INSTALL_CLI="/usr/local/bin/tunnel"
INSTALL_DAEMON="/usr/local/bin/tunnel-daemon"
SERVICE_DIR="${HOME}/.config/systemd/user"
SERVICE_NAME="tunnel.service"
CONFIG_DIR="${HOME}/.tunnel"
RELEASE_BASE="https://github.com/${GITHUB_REPO}/releases/latest/download"

# ── Colors ────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RESET='\033[0m'

info()    { echo -e "${CYAN}[info]${RESET} $*"; }
success() { echo -e "${GREEN}[ok]${RESET}   $*"; }
warn()    { echo -e "${YELLOW}[warn]${RESET} $*"; }
error()   { echo -e "${RED}[fail]${RESET} $*" >&2; exit 1; }

# ── Prerequisites ─────────────────────────────────────────────
info "Checking prerequisites..."
command -v curl      >/dev/null 2>&1 || error "curl not found. Install: sudo apt install curl"
command -v systemctl >/dev/null 2>&1 || error "systemctl not found. Ubuntu with systemd is required."

# ── Detect Architecture ───────────────────────────────────────
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)  ARCH_SUFFIX="x64" ;;
    aarch64) ARCH_SUFFIX="arm64" ;;
    *)       error "Unsupported architecture '$ARCH'. Only x86_64 and aarch64 are supported." ;;
esac
info "Architecture: linux-${ARCH_SUFFIX}"

# ── Download Binaries ─────────────────────────────────────────
TMP_CLI="/tmp/tunnel_new"
TMP_DAEMON="/tmp/tunnel-daemon_new"

info "Downloading CLI binary (tunnel-linux-${ARCH_SUFFIX})..."
curl -fSL --progress-bar \
    "${RELEASE_BASE}/tunnel-linux-${ARCH_SUFFIX}" \
    -o "$TMP_CLI" \
    || error "Failed to download CLI binary from GitHub Releases."

info "Downloading Daemon binary (tunnel-daemon-linux-${ARCH_SUFFIX})..."
curl -fSL --progress-bar \
    "${RELEASE_BASE}/tunnel-daemon-linux-${ARCH_SUFFIX}" \
    -o "$TMP_DAEMON" \
    || error "Failed to download Daemon binary from GitHub Releases."

# ── Install CLI ───────────────────────────────────────────────
info "Installing CLI → ${INSTALL_CLI}"
[[ -f "$INSTALL_CLI" ]] && sudo mv "$INSTALL_CLI" "${INSTALL_CLI}.old"
sudo mv "$TMP_CLI" "$INSTALL_CLI"
sudo chmod +x "$INSTALL_CLI"
success "CLI installed."

# ── Install Daemon ────────────────────────────────────────────
info "Installing Daemon → ${INSTALL_DAEMON}"
[[ -f "$INSTALL_DAEMON" ]] && sudo mv "$INSTALL_DAEMON" "${INSTALL_DAEMON}.old"
sudo mv "$TMP_DAEMON" "$INSTALL_DAEMON"
sudo chmod +x "$INSTALL_DAEMON"
success "Daemon binary installed."

# ── Setup Config Directory ────────────────────────────────────
info "Initializing config directory ${CONFIG_DIR}..."
mkdir -p "$CONFIG_DIR"
if [[ ! -f "${CONFIG_DIR}/profiles.json" ]]; then
    echo '{ "Profiles": [] }' > "${CONFIG_DIR}/profiles.json"
    success "Created default profiles.json."
fi

# ── Setup Systemd User Service ────────────────────────────────
info "Installing systemd user service..."
mkdir -p "$SERVICE_DIR"

cat > "${SERVICE_DIR}/${SERVICE_NAME}" << SERVICEEOF
[Unit]
Description=SSH Tunnel Manager Daemon
Documentation=https://github.com/${GITHUB_REPO}
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
ExecStart=${INSTALL_DAEMON}
Restart=on-failure
RestartSec=5s

PrivateTmp=true
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=read-only
ReadWritePaths=${HOME}/.tunnel

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_NOLOGO=1

[Install]
WantedBy=default.target
SERVICEEOF

systemctl --user daemon-reload
systemctl --user enable "$SERVICE_NAME"
systemctl --user start "$SERVICE_NAME"

sleep 2
if systemctl --user is-active --quiet "$SERVICE_NAME"; then
    success "Daemon is running!"
else
    warn "Daemon not yet active. Check logs: journalctl --user -u tunnel.service -n 20"
fi

# ── Done ──────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}════════════════════════════════════════${RESET}"
echo -e "${GREEN}  ✔ SSH Tunnel Manager installed!       ${RESET}"
echo -e "${GREEN}════════════════════════════════════════${RESET}"
echo ""
echo "  Quick start:"
echo -e "  ${CYAN}tunnel new prod-db${RESET}                        Create an SSH profile"
echo -e "  ${CYAN}tunnel add prod-db --local 5432 --remote 5432${RESET}  Add a port"
echo -e "  ${CYAN}tunnel use prod-db${RESET}                        Connect tunnel"
echo -e "  ${CYAN}tunnel status${RESET}                             View connections"
echo -e "  ${CYAN}tunnel stop${RESET}                               Stop tunnel"
echo ""
echo "  Daemon management:"
echo -e "  ${CYAN}systemctl --user status tunnel${RESET}"
echo -e "  ${CYAN}journalctl --user -u tunnel.service -f${RESET}"
echo ""
