#!/bin/bash
# NuGet Package Installer for Proxy-Restricted Environments (Claude Code Web)
# This script parses packages.lock.json files and downloads NuGet packages via wget
# (which works through the proxy) and extracts them directly to the global packages cache.
#
# Usage: Runs automatically as a SessionStart hook on Claude Code web.

# Only run in Claude Code web environments (skip for local CLI)
if [[ "$CLAUDE_CODE_REMOTE" != "true" ]]; then
    exit 0
fi

# Get the script's directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$(dirname "$SCRIPT_DIR")")"
PARENT_DIR="$(dirname "$PROJECT_ROOT")"

# Copy web nuget.config to parent directory to override project config
# NuGet searches parent directories, so this applies to all dotnet commands
if [[ -f "$SCRIPT_DIR/nuget.config.web" ]]; then
    cp "$SCRIPT_DIR/nuget.config.web" "$PARENT_DIR/nuget.config"
    echo "Installed nuget.config to $PARENT_DIR (local-feed enabled, nuget.org disabled)"
fi

CACHE_DIR="${NUGET_PACKAGES:-$HOME/.nuget/packages}"
FEED_DIR="/tmp/nuget-feed"
TEMP_DIR="/tmp/nuget-download"
mkdir -p "$CACHE_DIR" "$FEED_DIR" "$TEMP_DIR"

download_pkg() {
    local id=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    local version="$2"
    local pkg_dir="$CACHE_DIR/$id/$version"
    local feed_file="$FEED_DIR/${id}.${version}.nupkg"

    # Skip if already in cache
    if [ -d "$pkg_dir" ] && [ -f "$feed_file" ]; then
        return 0
    fi

    local url="https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.${version}.nupkg"
    local nupkg="$TEMP_DIR/${id}.${version}.nupkg"

    # Clear no_proxy to ensure we use the proxy (DNS requires proxy in this env)
    if no_proxy="" NO_PROXY="" wget -q -O "$nupkg" "$url" 2>/dev/null; then
        # Extract to global cache
        mkdir -p "$pkg_dir"
        unzip -q -o "$nupkg" -d "$pkg_dir" 2>/dev/null
        cp "$nupkg" "$pkg_dir/${id}.${version}.nupkg"
        # Also keep in feed for offline resolution
        mv "$nupkg" "$feed_file"
        return 0
    else
        rm -f "$nupkg"
        return 1
    fi
}

echo "Installing NuGet packages for KeenEyes (proxy workaround)..."

# Parse all packages.lock.json files and extract unique package/version pairs
# Skip entries with type "Project" (project references, not NuGet packages)
packages=$(find "$PROJECT_ROOT" -name "packages.lock.json" -type f -exec cat {} \; | \
    grep -E '"resolved"|"type"' | \
    paste - - | \
    grep -v '"Project"' | \
    sed -n 's/.*"resolved": "\([^"]*\)".*/\1/p' | \
    sort -u)

# We need both name and version, so let's use a different approach with a temp file
PACKAGES_FILE="$TEMP_DIR/packages.txt"
> "$PACKAGES_FILE"

for lockfile in $(find "$PROJECT_ROOT" -name "packages.lock.json" -type f); do
    # Use a simple state machine to extract package name, type, and version
    # jq would be cleaner but may not be available
    python3 - "$lockfile" >> "$PACKAGES_FILE" 2>/dev/null << 'PYTHON_SCRIPT'
import json
import sys

with open(sys.argv[1]) as f:
    data = json.load(f)

for framework, deps in data.get("dependencies", {}).items():
    for pkg_name, pkg_info in deps.items():
        pkg_type = pkg_info.get("type", "")
        resolved = pkg_info.get("resolved", "")
        # Skip project references
        if pkg_type == "Project" or not resolved:
            continue
        print(f"{pkg_name} {resolved}")
PYTHON_SCRIPT
done

# Sort and dedupe, then download each package
sort -u "$PACKAGES_FILE" | while read -r pkg version; do
    if [[ -n "$pkg" && -n "$version" ]]; then
        download_pkg "$pkg" "$version"
    fi
done

# Download dotnet tools (from .config/dotnet-tools.json)
# These are needed for husky and other tooling
download_pkg "husky" "0.8.0"

# Clean up
rm -f "$PACKAGES_FILE"

count=$(find "$CACHE_DIR" -maxdepth 2 -mindepth 2 -type d 2>/dev/null | wc -l)
echo "Package installation complete! ($count packages in $CACHE_DIR)"
