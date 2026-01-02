// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.NuGet;

/// <summary>
/// Interface for NuGet operations.
/// </summary>
public interface INuGetClient
{
    /// <summary>
    /// Searches for packages matching a query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="source">Optional source URL. Uses default sources if null.</param>
    /// <param name="take">Maximum results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching packages.</returns>
    Task<IReadOnlyList<PackageSearchResult>> SearchAsync(
        string query,
        string? source,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed metadata for a specific package version.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="version">Optional version. Gets latest if null.</param>
    /// <param name="source">Optional source URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Package info or null if not found.</returns>
    Task<PluginPackageInfo?> GetPackageMetadataAsync(
        string packageId,
        string? version,
        string? source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest version of a package.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="source">Optional source URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest version string or null if not found.</returns>
    Task<string?> GetLatestVersionAsync(
        string packageId,
        string? source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a package to a temporary location.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="version">The package version.</param>
    /// <param name="source">Optional source URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the downloaded .nupkg file.</returns>
    Task<string> DownloadPackageAsync(
        string packageId,
        string version,
        string? source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a package to the NuGet cache.
    /// </summary>
    /// <param name="nupkgPath">Path to the .nupkg file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the extracted package in the cache.</returns>
    Task<string> ExtractToNuGetCacheAsync(
        string nupkgPath,
        CancellationToken cancellationToken = default);
}
