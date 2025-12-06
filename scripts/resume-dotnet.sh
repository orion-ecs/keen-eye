#!/bin/bash
# Restore .NET SDK PATH for resumed Claude Code web sessions
# This script is run by the SessionResume hook

DOTNET_INSTALL_DIR="$HOME/.dotnet"

# Only run in remote (web) environments
if [ "$CLAUDE_CODE_REMOTE" != "true" ]; then
    exit 0
fi

# Check if .NET SDK is installed
if [ ! -d "$DOTNET_INSTALL_DIR" ] || [ ! -x "$DOTNET_INSTALL_DIR/dotnet" ]; then
    echo "Warning: .NET SDK not found at $DOTNET_INSTALL_DIR"
    echo "Run the SessionStart hooks to install it."
    exit 0
fi

# Persist environment variables to CLAUDE_ENV_FILE so subsequent bash commands have dotnet in PATH
if [ -n "$CLAUDE_ENV_FILE" ]; then
    echo "DOTNET_ROOT=$DOTNET_INSTALL_DIR" >> "$CLAUDE_ENV_FILE"
    echo "PATH=$DOTNET_INSTALL_DIR:\$PATH" >> "$CLAUDE_ENV_FILE"
    echo "Restored .NET SDK environment variables for resumed session"
fi

# Export for current shell
export DOTNET_ROOT="$DOTNET_INSTALL_DIR"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

# Verify dotnet is accessible
"$DOTNET_INSTALL_DIR/dotnet" --version
