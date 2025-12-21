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
FAILED_DIR="$TEMP_DIR/failures"
mkdir -p "$CACHE_DIR" "$FEED_DIR" "$TEMP_DIR" "$FAILED_DIR"
rm -rf "$FAILED_DIR"/*

# Maximum parallel downloads (balance between speed and server load)
MAX_PARALLEL=${MAX_PARALLEL:-8}

# Download a package with retry logic (thread-safe for parallel execution)
# Args: $1 = package id, $2 = version, $3 = is_critical (optional, "critical" to mark as critical)
download_pkg() {
    local id=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    local version="$2"
    local is_critical="${3:-}"
    local pkg_dir="$CACHE_DIR/$id/$version"
    local feed_file="$FEED_DIR/${id}.${version}.nupkg"

    # Skip if already in cache
    if [ -d "$pkg_dir" ] && [ -f "$feed_file" ]; then
        return 0
    fi

    local url="https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.${version}.nupkg"
    # Use unique temp file per download to avoid conflicts in parallel execution
    local nupkg="$TEMP_DIR/${id}.${version}.$$.nupkg"
    local max_retries=3
    local retry_delay=2

    # Retry loop with exponential backoff
    for attempt in $(seq 1 $max_retries); do
        # Clear no_proxy to ensure we use the proxy (DNS requires proxy in this env)
        if no_proxy="" NO_PROXY="" wget -q -O "$nupkg" "$url" 2>/dev/null; then
            # Extract to global cache
            mkdir -p "$pkg_dir"
            unzip -q -o "$nupkg" -d "$pkg_dir" 2>/dev/null
            cp "$nupkg" "$pkg_dir/${id}.${version}.nupkg"
            # Also keep in feed for offline resolution
            mv "$nupkg" "$feed_file"
            return 0
        fi

        if [ $attempt -lt $max_retries ]; then
            sleep $retry_delay
            retry_delay=$((retry_delay * 2))
        fi
    done

    # All retries failed - log the failure (use unique file per package for thread safety)
    rm -f "$nupkg"
    echo "$1 $version${is_critical:+ (CRITICAL)}" > "$FAILED_DIR/${id}.${version}.failed"
    if [ -n "$is_critical" ]; then
        echo "WARNING: Failed to download critical package: $1 $version"
    fi
    return 1
}

# Export function and variables for use in parallel subshells
export -f download_pkg
export CACHE_DIR FEED_DIR TEMP_DIR FAILED_DIR

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

# Sort and dedupe, then download packages in parallel
# Using xargs -P for parallel execution with limited concurrency
echo "Downloading packages (up to $MAX_PARALLEL in parallel)..."
sort -u "$PACKAGES_FILE" | xargs -P "$MAX_PARALLEL" -L 1 bash -c 'download_pkg "$1" "$2"' _

# Download dotnet tools (from .config/dotnet-tools.json)
# These are needed for husky and other tooling
download_pkg "husky" "0.8.0"

# AOT compiler runtime packages (not in packages.lock.json, resolved at restore time)
# These are marked as critical since AOT builds will fail without them
download_pkg "runtime.linux-x64.Microsoft.DotNet.ILCompiler" "10.0.1" "critical"
download_pkg "Microsoft.DotNet.ILCompiler" "10.0.1" "critical"

# Asset loading packages (KeenEyes.Assets dependencies)
download_pkg "StbImageSharp" "2.30.15"
download_pkg "SharpGLTF.Core" "1.0.5"
download_pkg "SharpGLTF.Runtime" "1.0.5"
download_pkg "SharpGLTF.Toolkit" "1.0.5"
download_pkg "NVorbis" "0.10.5"
# SharpGLTF transitive dependencies
download_pkg "System.Numerics.Vectors" "4.5.0"
download_pkg "System.Memory" "4.5.5"
download_pkg "System.Buffers" "4.5.1"
download_pkg "System.Runtime.CompilerServices.Unsafe" "6.0.0"

# Clean up temp files
rm -f "$PACKAGES_FILE"

# Summary and verification
count=$(find "$CACHE_DIR" -maxdepth 2 -mindepth 2 -type d 2>/dev/null | wc -l)
failed_count=$(find "$FAILED_DIR" -name "*.failed" -type f 2>/dev/null | wc -l)

echo ""
echo "Package installation complete!"
echo "  Packages in cache: $count"

if [ "$failed_count" -gt 0 ]; then
    echo "  Failed downloads: $failed_count"
    echo ""
    echo "The following packages failed to download after 3 retries:"
    for failed_file in "$FAILED_DIR"/*.failed; do
        [ -f "$failed_file" ] && echo "  - $(cat "$failed_file")"
    done

    # Check for critical failures
    if grep -q "(CRITICAL)" "$FAILED_DIR"/*.failed 2>/dev/null; then
        echo ""
        echo "ERROR: Critical packages failed to download. Build/restore may fail."
        echo "You can retry by running: CLAUDE_CODE_REMOTE=true .claude/hooks/install-packages.sh"
    fi
fi

# Clean up failure tracking directory
rm -rf "$FAILED_DIR"
