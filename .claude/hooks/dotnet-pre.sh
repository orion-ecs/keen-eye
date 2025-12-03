#!/bin/bash
# PreToolUse hook: Disable nuget.org before dotnet commands (web only)

# Skip if not in web environment
[[ "$CLAUDE_CODE_REMOTE" != "true" ]] && exit 0

# Read tool input from stdin
input=$(cat)
command=$(echo "$input" | jq -r '.tool_input.command // empty' 2>/dev/null)

# Only process dotnet commands
[[ "$command" != *"dotnet"* ]] && exit 0

# Enable local-feed and disable nuget.org to force offline resolution
if command -v dotnet &> /dev/null; then
    dotnet nuget enable source local-feed 2>/dev/null || true
    dotnet nuget disable source nuget.org 2>/dev/null || true
fi

exit 0
