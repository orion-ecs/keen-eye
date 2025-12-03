#!/bin/bash
# Auto-download missing NuGet packages based on restore errors
# This script runs dotnet restore, parses errors, and downloads missing packages

FEED_DIR="/tmp/nuget-feed"
mkdir -p "$FEED_DIR"

download_pkg() {
    local id=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    local version="$2"
    local file="$FEED_DIR/${id}.${version}.nupkg"

    if [ -f "$file" ]; then
        return 0
    fi

    local url="https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.${version}.nupkg"

    if wget -q -O "$file" "$url" 2>/dev/null; then
        echo "  Downloaded: $id@$version"
        return 0
    else
        rm -f "$file"
        return 1
    fi
}

get_latest_version() {
    local id=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    wget -q -O - "https://api.nuget.org/v3-flatcontainer/${id}/index.json" 2>/dev/null | \
        grep -oP '"[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9.]+)?"' | tail -1 | tr -d '"'
}

echo "Auto-downloading missing NuGet packages..."

max_iterations=10
iteration=0

while [ $iteration -lt $max_iterations ]; do
    iteration=$((iteration + 1))
    echo "Iteration $iteration: Running restore..."

    # Run restore and capture errors
    errors=$(dotnet restore /home/user/keen-eye/KeenEyes.sln --verbosity minimal 2>&1)

    # Check if restore succeeded
    if echo "$errors" | grep -q "All projects are up-to-date"; then
        echo "All packages restored successfully!"
        exit 0
    fi

    # Parse missing packages (NU1101 errors)
    missing=$(echo "$errors" | grep -oP "Unable to find package \K[^.]+[^ ]+" | sort -u)

    # Parse version mismatches (NU1102 errors)
    version_errors=$(echo "$errors" | grep -oP "Unable to find package \K[^ ]+ with version \(.*?= ([0-9.]+)" | sort -u)

    if [ -z "$missing" ] && [ -z "$version_errors" ]; then
        echo "No more missing packages found, but restore still failing."
        echo "$errors" | grep -E "error NU"
        exit 1
    fi

    downloaded=0

    # Download missing packages (get latest version)
    for pkg in $missing; do
        version=$(get_latest_version "$pkg")
        if [ -n "$version" ]; then
            if download_pkg "$pkg" "$version"; then
                downloaded=$((downloaded + 1))
            fi
        fi
    done

    # Download specific versions
    echo "$errors" | grep -oP "Unable to find package ([^ ]+) with version \([><=]*\s*([0-9.]+)" | while read line; do
        pkg=$(echo "$line" | grep -oP "package \K[^ ]+")
        ver=$(echo "$line" | grep -oP "[0-9]+\.[0-9]+\.[0-9]+")
        if [ -n "$pkg" ] && [ -n "$ver" ]; then
            download_pkg "$pkg" "$ver"
        fi
    done

    if [ $downloaded -eq 0 ]; then
        echo "Could not download any new packages. Remaining errors:"
        echo "$errors" | grep -E "error NU"
        exit 1
    fi

    echo "Downloaded $downloaded packages, retrying restore..."
done

echo "Max iterations reached. Some packages may still be missing."
exit 1
