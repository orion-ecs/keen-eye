#!/bin/bash
# SessionStart hook: Add api.nuget.org to no_proxy to bypass proxy for NuGet

# Only run in Claude Code web environments
if [[ "$CLAUDE_CODE_REMOTE" != "true" ]]; then
    exit 0
fi

# Add api.nuget.org to no_proxy if not already present
if [[ -n "$no_proxy" && "$no_proxy" != *"api.nuget.org"* ]]; then
    export no_proxy="$no_proxy,api.nuget.org"
    export NO_PROXY="$NO_PROXY,api.nuget.org"
    echo "Added api.nuget.org to no_proxy"
fi
