// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Installation;

/// <summary>
/// Represents a package to be installed as part of an installation plan.
/// </summary>
public sealed class PackageToInstall
{
    /// <summary>
    /// Gets or sets the package ID.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the version to install.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the source URL.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Gets or sets whether this is the primary package (not a dependency).
    /// </summary>
    public bool IsPrimary { get; init; }
}
