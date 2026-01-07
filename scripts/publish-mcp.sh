#!/bin/bash
# Publish MCP TestBridge server
# Usage: ./scripts/publish-mcp.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
MCP_PROJECT="$PROJECT_ROOT/tools/KeenEyes.Mcp.TestBridge/KeenEyes.Mcp.TestBridge.csproj"
OUTPUT_DIR="$PROJECT_ROOT/.mcp"

echo "Publishing MCP TestBridge server..."
echo "  Project: $MCP_PROJECT"
echo "  Output:  $OUTPUT_DIR"
echo ""

dotnet publish "$MCP_PROJECT" -c Release -o "$OUTPUT_DIR" --nologo

echo ""
echo "MCP server published successfully!"
echo "Executable: $OUTPUT_DIR/KeenEyes.Mcp.TestBridge.exe"
