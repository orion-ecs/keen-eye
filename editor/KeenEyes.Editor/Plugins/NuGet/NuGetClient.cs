// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.IO.Compression;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace KeenEyes.Editor.Plugins.NuGet;

/// <summary>
/// NuGet client implementation for package operations.
/// </summary>
public sealed class NuGetClient : INuGetClient
{
    private const string DefaultSource = "https://api.nuget.org/v3/index.json";

    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NuGetClient"/> class.
    /// </summary>
    public NuGetClient()
    {
        logger = NullLogger.Instance;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PackageSearchResult>> SearchAsync(
        string query,
        string? source,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = source ?? DefaultSource;
        var repository = Repository.Factory.GetCoreV3(sourceUrl);
        var searchResource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);

        var searchFilter = new SearchFilter(includePrerelease: false);
        var results = await searchResource.SearchAsync(
            query,
            searchFilter,
            skip: 0,
            take: take,
            logger,
            cancellationToken);

        var packages = new List<PackageSearchResult>();

        foreach (var result in results)
        {
            packages.Add(new PackageSearchResult
            {
                PackageId = result.Identity.Id,
                LatestVersion = result.Identity.Version.ToNormalizedString(),
                Description = result.Description,
                Authors = result.Authors,
                DownloadCount = result.DownloadCount,
                IconUrl = result.IconUrl?.ToString(),
                ProjectUrl = result.ProjectUrl?.ToString(),
                Tags = result.Tags?.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries) ?? [],
                IsVerified = result.PrefixReserved
            });
        }

        return packages;
    }

    /// <inheritdoc />
    public async Task<PluginPackageInfo?> GetPackageMetadataAsync(
        string packageId,
        string? version,
        string? source,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = source ?? DefaultSource;
        var repository = Repository.Factory.GetCoreV3(sourceUrl);
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        using var cache = new SourceCacheContext();

        IPackageSearchMetadata? metadata;

        if (version != null)
        {
            metadata = await metadataResource.GetMetadataAsync(
                new global::NuGet.Packaging.Core.PackageIdentity(packageId, NuGetVersion.Parse(version)),
                cache,
                logger,
                cancellationToken);
        }
        else
        {
            var allMetadata = await metadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: false,
                includeUnlisted: false,
                cache,
                logger,
                cancellationToken);

            metadata = allMetadata
                .OrderByDescending(m => m.Identity.Version)
                .FirstOrDefault();
        }

        if (metadata == null)
        {
            return null;
        }

        var dependencies = new List<PackageDependency>();

        foreach (var dependencyGroup in metadata.DependencySets)
        {
            foreach (var dep in dependencyGroup.Packages)
            {
                dependencies.Add(new PackageDependency
                {
                    PackageId = dep.Id,
                    VersionRange = dep.VersionRange.ToString(),
                    TargetFramework = dependencyGroup.TargetFramework?.GetShortFolderName()
                });
            }
        }

        return new PluginPackageInfo
        {
            PackageId = metadata.Identity.Id,
            Version = metadata.Identity.Version.ToNormalizedString(),
            Description = metadata.Description,
            Authors = metadata.Authors,
            License = metadata.LicenseMetadata?.License ?? metadata.LicenseUrl?.ToString(),
            ProjectUrl = metadata.ProjectUrl?.ToString(),
            Dependencies = dependencies,
            SourceUrl = sourceUrl
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetLatestVersionAsync(
        string packageId,
        string? source,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = source ?? DefaultSource;
        var repository = Repository.Factory.GetCoreV3(sourceUrl);
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

        using var cache = new SourceCacheContext();

        var allMetadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: false,
            includeUnlisted: false,
            cache,
            logger,
            cancellationToken);

        var latest = allMetadata
            .OrderByDescending(m => m.Identity.Version)
            .FirstOrDefault();

        return latest?.Identity.Version.ToNormalizedString();
    }

    /// <inheritdoc />
    public async Task<string> DownloadPackageAsync(
        string packageId,
        string version,
        string? source,
        CancellationToken cancellationToken = default)
    {
        var sourceUrl = source ?? DefaultSource;
        var repository = Repository.Factory.GetCoreV3(sourceUrl);

        var downloadResource = await repository.GetResourceAsync<DownloadResource>(cancellationToken);
        var packageIdentity = new global::NuGet.Packaging.Core.PackageIdentity(
            packageId,
            NuGetVersion.Parse(version));

        using var cache = new SourceCacheContext();

        var tempPath = Path.Combine(Path.GetTempPath(), "keeneyes-plugins");
        Directory.CreateDirectory(tempPath);

        var nupkgPath = Path.Combine(tempPath, $"{packageId}.{version}.nupkg");

        var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
            packageIdentity,
            new PackageDownloadContext(cache),
            globalPackagesFolder: SettingsUtility.GetGlobalPackagesFolder(global::NuGet.Configuration.Settings.LoadDefaultSettings(null)),
            logger,
            cancellationToken);

        if (downloadResult.Status != DownloadResourceResultStatus.Available &&
            downloadResult.Status != DownloadResourceResultStatus.AvailableWithoutStream)
        {
            throw new InvalidOperationException(
                $"Package {packageId} v{version} not available. Status: {downloadResult.Status}");
        }

        // If already in global packages folder, return that path
        if (downloadResult.Status == DownloadResourceResultStatus.AvailableWithoutStream &&
            downloadResult.PackageReader != null)
        {
            return GetPackageCachePath(packageId, version);
        }

        // Otherwise, copy to temp location
        if (downloadResult.PackageStream != null)
        {
            using var fileStream = File.Create(nupkgPath);
            await downloadResult.PackageStream.CopyToAsync(fileStream, cancellationToken);
        }

        return nupkgPath;
    }

    /// <inheritdoc />
    public Task<string> ExtractToNuGetCacheAsync(
        string nupkgPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var archive = ZipFile.OpenRead(nupkgPath);
        using var packageReader = new PackageArchiveReader(archive);

        var identity = packageReader.GetIdentity();
        var cachePath = GetPackageCachePath(identity.Id, identity.Version.ToNormalizedString());

        if (Directory.Exists(cachePath))
        {
            // Already extracted
            return Task.FromResult(cachePath);
        }

        Directory.CreateDirectory(cachePath);

        // Extract files
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destinationPath = Path.Combine(cachePath, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destinationPath);

            if (destinationDir != null)
            {
                Directory.CreateDirectory(destinationDir);
            }

            if (!string.IsNullOrEmpty(entry.Name))
            {
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        // Create the .nupkg.metadata file that NuGet expects
        var metadataPath = Path.Combine(cachePath, ".nupkg.metadata");
        File.WriteAllText(metadataPath, $$"""
            {
              "version": 2,
              "contentHash": "",
              "source": "{{nupkgPath}}"
            }
            """);

        return Task.FromResult(cachePath);
    }

    private static string GetPackageCachePath(string packageId, string version)
    {
        var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
            global::NuGet.Configuration.Settings.LoadDefaultSettings(null));
        return Path.Combine(globalPackagesFolder, packageId.ToLowerInvariant(), version);
    }
}
