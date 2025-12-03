#!/bin/bash
# NuGet Package Installer for Proxy-Restricted Environments (Claude Code Web)
# This script downloads NuGet packages via wget (which works through the proxy)
# and places them in a local feed directory.
#
# Usage: Runs automatically as a SessionStart hook on Claude Code web.
# The nuget.config in this repo is configured to use /tmp/nuget-feed as a package source.

# Only run in Claude Code web environments (skip for local CLI)
if [[ "$CLAUDE_CODE_REMOTE" != "true" ]]; then
    exit 0
fi

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
        return 0
    else
        rm -f "$file"
        return 1
    fi
}

echo "Installing NuGet packages for KeenEyes (proxy workaround)..."

# Core Roslyn packages
download_pkg "Microsoft.CodeAnalysis.Analyzers" "3.11.0"
download_pkg "Microsoft.CodeAnalysis.CSharp" "5.0.0"
download_pkg "Microsoft.CodeAnalysis.Common" "5.0.0"
download_pkg "Microsoft.CodeAnalysis.CSharp.Workspaces" "5.0.0"
download_pkg "Microsoft.CodeAnalysis.Workspaces.Common" "5.0.0"
download_pkg "Microsoft.CodeAnalysis.NetAnalyzers" "9.0.0"

# Roslyn testing packages
download_pkg "Microsoft.CodeAnalysis.Analyzer.Testing" "1.1.2"
download_pkg "Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing" "1.1.2"
download_pkg "Microsoft.CodeAnalysis.SourceGenerators.Testing" "1.1.2"

# Testing framework packages
download_pkg "GitHubActionsTestLogger" "3.0.1"
download_pkg "Microsoft.NET.Test.Sdk" "18.0.1"
download_pkg "Microsoft.Testing.Platform" "2.0.2"
download_pkg "Microsoft.Testing.Platform.MSBuild" "2.0.2"
download_pkg "Microsoft.Testing.Extensions.CodeCoverage" "18.1.0"
download_pkg "Microsoft.Testing.Extensions.TrxReport" "2.0.2"
download_pkg "Microsoft.Testing.Extensions.TrxReport.Abstractions" "2.0.2"
download_pkg "Microsoft.Testing.Extensions.Telemetry" "2.0.2"

# xUnit v3 packages
download_pkg "xunit.v3" "3.2.1"
download_pkg "xunit.v3.assert" "3.2.1"
download_pkg "xunit.v3.core" "3.2.1"
download_pkg "xunit.v3.common" "3.2.1"
download_pkg "xunit.v3.extensibility.core" "3.2.1"
download_pkg "xunit.v3.runner.common" "3.2.1"
download_pkg "xunit.v3.runner.inproc.console" "3.2.1"
download_pkg "xunit.v3.mtp-v2" "3.2.1"
download_pkg "xunit.v3.core.mtp-v2" "3.2.1"
download_pkg "xunit.runner.visualstudio" "3.1.5"
download_pkg "xunit.analyzers" "1.26.0"

# Test utilities
download_pkg "Shouldly" "4.3.0"
download_pkg "Moq" "4.20.72"
download_pkg "DiffEngine" "15.5.3"
download_pkg "DiffPlex" "1.7.2"
download_pkg "EmptyFiles" "8.5.0"

# Analyzers
download_pkg "SonarAnalyzer.CSharp" "10.4.0.108396"

# VS Composition (for Roslyn testing)
download_pkg "Microsoft.VisualStudio.Composition" "17.0.46"
download_pkg "Microsoft.VisualStudio.Composition.Analyzers" "17.0.46"
download_pkg "Microsoft.VisualStudio.Validation" "17.8.8"

# NuGet packages (for Roslyn testing)
download_pkg "NuGet.Common" "6.11.0"
download_pkg "NuGet.Configuration" "6.11.0"
download_pkg "NuGet.Frameworks" "6.11.0"
download_pkg "NuGet.Packaging" "6.11.0"
download_pkg "NuGet.Protocol" "6.11.0"
download_pkg "NuGet.Resolver" "6.11.0"
download_pkg "NuGet.Versioning" "6.11.0"

# Common transitive dependencies
download_pkg "NETStandard.Library" "2.0.3"
download_pkg "Microsoft.NETCore.Platforms" "1.1.0"
download_pkg "Microsoft.DiaSymReader" "2.0.0"
download_pkg "Microsoft.Extensions.DependencyModel" "9.0.0"
download_pkg "Microsoft.ApplicationInsights" "2.23.0"
download_pkg "Microsoft.Bcl.AsyncInterfaces" "9.0.0"
download_pkg "Microsoft.Win32.Registry" "5.0.0"
download_pkg "Newtonsoft.Json" "13.0.3"
download_pkg "Castle.Core" "5.1.1"
download_pkg "Humanizer.Core" "2.14.1"

# System.* packages
download_pkg "System.Buffers" "4.6.0"
download_pkg "System.Memory" "4.6.0"
download_pkg "System.Numerics.Vectors" "4.6.0"
download_pkg "System.Threading.Tasks.Extensions" "4.6.0"
download_pkg "System.Runtime.CompilerServices.Unsafe" "6.1.0"
download_pkg "System.Collections.Immutable" "9.0.0"
download_pkg "System.Reflection.Metadata" "9.0.0"
download_pkg "System.Reflection.TypeExtensions" "4.7.0"
download_pkg "System.Text.Encoding.CodePages" "8.0.0"
download_pkg "System.Diagnostics.EventLog" "8.0.0"
download_pkg "System.Management" "9.0.0"
download_pkg "System.CodeDom" "9.0.0"
download_pkg "System.ComponentModel.Composition" "9.0.0"
download_pkg "System.Security.Cryptography.Pkcs" "9.0.0"
download_pkg "System.Security.Cryptography.ProtectedData" "9.0.0"

# System.Composition packages
download_pkg "System.Composition" "9.0.0"
download_pkg "System.Composition.AttributedModel" "9.0.0"
download_pkg "System.Composition.Convention" "9.0.0"
download_pkg "System.Composition.Hosting" "9.0.0"
download_pkg "System.Composition.Runtime" "9.0.0"
download_pkg "System.Composition.TypedParts" "9.0.0"

count=$(ls "$FEED_DIR"/*.nupkg 2>/dev/null | wc -l)
echo "Package installation complete! ($count packages in $FEED_DIR)"
