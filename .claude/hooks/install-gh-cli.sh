#!/bin/bash
# GitHub CLI Installer for Claude Code Web Sessions
# Downloads and installs the gh CLI which is needed for GitHub operations
# (creating PRs, managing issues, etc.)
#
# Usage: Runs automatically as a SessionStart hook on Claude Code web.

# Only run in Claude Code web environments (skip for local CLI)
if [[ "$CLAUDE_CODE_REMOTE" != "true" ]]; then
    exit 0
fi

# Check if gh is already installed and working
if command -v gh &>/dev/null; then
    echo "GitHub CLI already installed: $(gh --version | head -n1)"
    exit 0
fi

echo "Installing GitHub CLI..."

GH_VERSION="2.63.2"
INSTALL_DIR="$HOME/.local/bin"
TEMP_DIR="/tmp/gh-install"

mkdir -p "$INSTALL_DIR" "$TEMP_DIR"

# Download with retry logic
download_with_retry() {
    local url="$1"
    local output="$2"
    local max_retries=4
    local retry_delay=2

    for attempt in $(seq 1 $max_retries); do
        # Clear no_proxy to ensure we use the proxy (DNS requires proxy in this env)
        if no_proxy="" NO_PROXY="" wget -q -O "$output" "$url" 2>/dev/null; then
            return 0
        fi

        if [ $attempt -lt $max_retries ]; then
            echo "  Retry $attempt/$max_retries after ${retry_delay}s..."
            sleep $retry_delay
            retry_delay=$((retry_delay * 2))
        fi
    done

    return 1
}

# Download gh CLI tarball
TARBALL="$TEMP_DIR/gh.tar.gz"
URL="https://github.com/cli/cli/releases/download/v${GH_VERSION}/gh_${GH_VERSION}_linux_amd64.tar.gz"

if ! download_with_retry "$URL" "$TARBALL"; then
    echo "ERROR: Failed to download GitHub CLI after multiple retries"
    exit 1
fi

# Extract and install
cd "$TEMP_DIR"
if ! tar -xzf "$TARBALL"; then
    echo "ERROR: Failed to extract GitHub CLI"
    exit 1
fi

# Move binary to install directory
cp "gh_${GH_VERSION}_linux_amd64/bin/gh" "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/gh"

# Add to PATH if not already there
if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
    export PATH="$INSTALL_DIR:$PATH"

    # Persist to CLAUDE_ENV_FILE if available
    if [[ -n "$CLAUDE_ENV_FILE" ]]; then
        echo "PATH=$INSTALL_DIR:\$PATH" >> "$CLAUDE_ENV_FILE"
        echo "Environment variables persisted to CLAUDE_ENV_FILE"
    fi
fi

# Cleanup
rm -rf "$TEMP_DIR"

# Verify installation
if command -v gh &>/dev/null; then
    echo "GitHub CLI installed successfully!"
    gh --version | head -n1
else
    echo "ERROR: GitHub CLI installation failed - binary not found in PATH"
    exit 1
fi
