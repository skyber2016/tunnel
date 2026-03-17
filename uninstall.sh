#!/usr/bin/env bash
# ============================================================
# uninstall.sh — SSH Tunnel Manager Uninstaller
#
# Usage:
#   curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/uninstall.sh | bash
#   curl -sSL https://raw.githubusercontent.com/skyber2016/tunnel/main/uninstall.sh | bash -s -- --purge
#
# Options:
#   --purge   Also remove all profile data (~/.tunnel/)
# ============================================================

set -euo pipefail

INSTALL_CLI="/usr/local/bin/tunnel"
INSTALL_DAEMON="/usr/local/bin/tunnel-daemon"
SERVICE_DIR="${HOME}/.config/systemd/user"
SERVICE_NAME="tunnel.service"
CONFIG_DIR="${HOME}/.tunnel"
GITHUB_RAW="https://raw.githubusercontent.com/skyber2016/tunnel/main"

# ── Colors ────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RESET='\033[0m'

info()    { echo -e "${CYAN}[info]${RESET}  $*"; }
success() { echo -e "${GREEN}[ok]${RESET}    $*"; }
warn()    { echo -e "${YELLOW}[warn]${RESET}  $*"; }
error()   { echo -e "${RED}[fail]${RESET}  $*" >&2; exit 1; }
step()    { echo -e "\n${CYAN}▸${RESET} $*"; }

# ── Parse arguments ───────────────────────────────────────────
PURGE=false
for arg in "$@"; do
  [[ "$arg" == "--purge" ]] && PURGE=true
done

# ─────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}SSH Tunnel Manager — Uninstaller${RESET}"
echo "────────────────────────────────────"
echo ""

# ── Check if anything is installed ───────────────────────────
FOUND=false
[[ -f "$INSTALL_CLI" ]]    && FOUND=true || true
[[ -f "$INSTALL_DAEMON" ]] && FOUND=true || true
[[ -f "${SERVICE_DIR}/${SERVICE_NAME}" ]] && FOUND=true || true

if [[ "$FOUND" == "false" ]]; then
  warn "No SSH Tunnel Manager installation found on this system."
  warn "Nothing to uninstall."
  exit 0
fi

# ── Show what will be removed ─────────────────────────────────
echo -e "The following will be removed:"
[[ -f "$INSTALL_CLI" ]]    && echo -e "  ${RED}✗${RESET} $INSTALL_CLI"    || true
[[ -f "$INSTALL_DAEMON" ]] && echo -e "  ${RED}✗${RESET} $INSTALL_DAEMON" || true
[[ -f "${SERVICE_DIR}/${SERVICE_NAME}" ]] && echo -e "  ${RED}✗${RESET} ${SERVICE_DIR}/${SERVICE_NAME}" || true

if [[ "$PURGE" == "true" ]]; then
  echo ""
  echo -e "  ${RED}✗${RESET} ${CONFIG_DIR}/  [--purge: all profile data will be deleted]"
fi

echo ""

# ── Confirm prompt — works both interactively and via curl | bash ──
# When stdin is a pipe (curl), read from /dev/tty directly.
if [ -t 0 ]; then
  read -r -p "Proceed with uninstall? [y/N] " confirm
else
  read -r -p "Proceed with uninstall? [y/N] " confirm < /dev/tty
fi

if [[ ! "$confirm" =~ ^[yY]([eE][sS])?$ ]]; then
  echo "Aborted."
  exit 0
fi

# ── 1. Stop and disable systemd service ───────────────────────
step "Stopping tunnel daemon..."
if systemctl --user is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
  systemctl --user stop "$SERVICE_NAME"
  success "Daemon stopped."
else
  info "Daemon was not running."
fi

if systemctl --user is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
  systemctl --user disable "$SERVICE_NAME"
  success "Service disabled."
fi

# ── 2. Remove service unit file ───────────────────────────────
step "Removing systemd service file..."
if [[ -f "${SERVICE_DIR}/${SERVICE_NAME}" ]]; then
  rm -f "${SERVICE_DIR}/${SERVICE_NAME}"
  systemctl --user daemon-reload
  success "Service file removed."
fi

# ── 3. Remove binary files ────────────────────────────────────
step "Removing binary files..."

for bin in "$INSTALL_CLI" "$INSTALL_DAEMON" \
           "${INSTALL_CLI}.old" "${INSTALL_DAEMON}.old"; do
  if [[ -f "$bin" ]]; then
    sudo rm -f "$bin"
    success "Removed: $bin"
  fi
done

# ── 4. Purge config data (optional) ──────────────────────────
if [[ "$PURGE" == "true" ]]; then
  step "Purging profile data..."
  if [[ -d "$CONFIG_DIR" ]]; then
    rm -rf "$CONFIG_DIR"
    success "Removed: ${CONFIG_DIR}"
  fi
else
  echo ""
  warn "Profile data has NOT been removed: ${CONFIG_DIR}"
  warn "Your SSH profiles are preserved. To remove them:"
  echo -e "  ${CYAN}curl -sSL ${GITHUB_RAW}/uninstall.sh | bash -s -- --purge${RESET}"
  echo -e "  — or manually: ${CYAN}rm -rf ${CONFIG_DIR}${RESET}"
fi

# ── Done ──────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}════════════════════════════════════════${RESET}"
echo -e "${GREEN}  ✔ SSH Tunnel Manager uninstalled.     ${RESET}"
echo -e "${GREEN}════════════════════════════════════════${RESET}"
echo ""
echo "  To reinstall:"
echo -e "  ${CYAN}curl -sSL ${GITHUB_RAW}/install.sh | bash${RESET}"
echo ""
