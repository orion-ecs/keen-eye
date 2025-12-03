#!/bin/bash
# PostToolUse hook: Re-enable nuget.org after dotnet commands (web only)

# Skip if not in web environment
[[ "$CLAUDE_CODE_REMOTE" != "true" ]] && exit 0

# Read tool input from stdin
input=$(cat)
command=$(echo "$input" | jq -r '.tool_input.command // empty' 2>/dev/null)

# Only process dotnet commands
[[ "$command" != *"dotnet"* ]] && exit 0

# Restore source states (keeps nuget.config clean for commits)
if command -v dotnet &> /dev/null; then
    dotnet nuget disable source local-feed 2>/dev/null || true
    dotnet nuget enable source nuget.org 2>/dev/null || true
fi

exit 0
