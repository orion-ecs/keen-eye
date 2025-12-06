#!/bin/bash
# PreToolUse hook: Configure proxy bypass and sources before dotnet commands (web only)

# Skip if not in web environment
[[ "$CLAUDE_CODE_REMOTE" != "true" ]] && exit 0

# Read tool input from stdin
input=$(cat)
command=$(echo "$input" | jq -r '.tool_input.command // empty' 2>/dev/null)

# Only process dotnet commands
[[ "$command" != *"dotnet"* ]] && exit 0

# Add api.nuget.org to no_proxy list to bypass proxy for NuGet
if [[ -n "$no_proxy" && "$no_proxy" != *"api.nuget.org"* ]]; then
    export no_proxy="$no_proxy,api.nuget.org"
    export NO_PROXY="$NO_PROXY,api.nuget.org"
fi

# Enable local-feed and disable nuget.org to force offline resolution
if command -v dotnet &> /dev/null; then
    dotnet nuget enable source local-feed 2>/dev/null || true
    dotnet nuget disable source nuget.org 2>/dev/null || true
fi

exit 0
