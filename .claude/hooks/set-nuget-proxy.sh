#!/bin/bash
# SessionStart hook: Add api.nuget.org to no_proxy to bypass proxy for NuGet

# Only run in Claude Code web environments
if [[ "$CLAUDE_CODE_REMOTE" != "true" ]]; then
    exit 0
fi

# Add api.nuget.org to no_proxy if not already present
if [[ -n "$no_proxy" && "$no_proxy" != *"api.nuget.org"* ]]; then
    new_no_proxy="$no_proxy,api.nuget.org"
    new_NO_PROXY="$NO_PROXY,api.nuget.org"

    # Persist for subsequent bash commands via CLAUDE_ENV_FILE
    if [[ -n "$CLAUDE_ENV_FILE" ]]; then
        echo "no_proxy=$new_no_proxy" >> "$CLAUDE_ENV_FILE"
        echo "NO_PROXY=$new_NO_PROXY" >> "$CLAUDE_ENV_FILE"
    fi

    echo "Added api.nuget.org to no_proxy"
fi
