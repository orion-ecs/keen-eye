// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.NuGet;

/// <summary>
/// Detailed package information for a specific version.
/// </summary>
public sealed class PluginPackageInfo
{
    /// <summary>
    /// Gets or sets the package ID.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the package description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the package authors.
    /// </summary>
    public string? Authors { get; init; }

    /// <summary>
    /// Gets or sets the license URL or expression.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Gets or sets the project URL.
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// Gets or sets the package dependencies.
    /// </summary>
    public IReadOnlyList<PackageDependency> Dependencies { get; init; } = [];

    /// <summary>
    /// Gets or sets the source URL this package was found on.
    /// </summary>
    public string? SourceUrl { get; init; }
}

/// <summary>
/// Represents a package dependency.
/// </summary>
public sealed class PackageDependency
{
    /// <summary>
    /// Gets or sets the dependency package ID.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the version range (e.g., "[1.0.0, 2.0.0)").
    /// </summary>
    public required string VersionRange { get; init; }

    /// <summary>
    /// Gets or sets the target framework (e.g., "net10.0").
    /// </summary>
    public string? TargetFramework { get; init; }
}
