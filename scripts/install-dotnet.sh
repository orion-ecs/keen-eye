#!/bin/bash
# Install .NET 10 SDK for Claude Code on the web
# This script is run by the SessionStart hook

set -e

DOTNET_VERSION="10.0"
DOTNET_INSTALL_DIR="$HOME/.dotnet"

# Only run in remote (web) environments
if [ "$CLAUDE_CODE_REMOTE" != "true" ]; then
    echo "Not running in remote environment, skipping .NET SDK installation"
    exit 0
fi

# Check if .NET 10 is already installed
if command -v dotnet &> /dev/null; then
    INSTALLED_VERSION=$(dotnet --version 2>/dev/null || echo "")
    if [[ "$INSTALLED_VERSION" == 10.* ]]; then
        echo ".NET 10 SDK is already installed: $INSTALLED_VERSION"
        exit 0
    fi
fi

echo "Installing .NET $DOTNET_VERSION SDK..."

# Download and run the official dotnet-install script
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh

# Install .NET 10 SDK (preview/RC channel for .NET 10)
/tmp/dotnet-install.sh --channel "$DOTNET_VERSION" --install-dir "$DOTNET_INSTALL_DIR"

# Clean up
rm -f /tmp/dotnet-install.sh

# Persist environment variables for subsequent bash commands
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "PATH=$DOTNET_INSTALL_DIR:\$PATH" >> "$CLAUDE_ENV_FILE"
    echo "Environment variables persisted to CLAUDE_ENV_FILE"
fi

# Export for current session
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Verify installation
echo "Verifying .NET installation..."
"$DOTNET_INSTALL_DIR/dotnet" --version
"$DOTNET_INSTALL_DIR/dotnet" --list-sdks

echo ".NET $DOTNET_VERSION SDK installed successfully!"
exit 0
