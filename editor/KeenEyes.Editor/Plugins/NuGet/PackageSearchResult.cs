// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.NuGet;

/// <summary>
/// Represents a package search result from NuGet.
/// </summary>
public sealed class PackageSearchResult
{
    /// <summary>
    /// Gets or sets the package ID.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the latest available version.
    /// </summary>
    public required string LatestVersion { get; init; }

    /// <summary>
    /// Gets or sets the package description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the package authors.
    /// </summary>
    public string? Authors { get; init; }

    /// <summary>
    /// Gets or sets the total download count.
    /// </summary>
    public long? DownloadCount { get; init; }

    /// <summary>
    /// Gets or sets the package icon URL.
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Gets or sets the project URL.
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// Gets or sets the package tags.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets or sets whether the package is verified.
    /// </summary>
    public bool IsVerified { get; init; }
}
