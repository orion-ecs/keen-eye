// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.NuGet;
using KeenEyes.Editor.Plugins.Registry;
using NuGet.Versioning;

namespace KeenEyes.Editor.Plugins.Installation;

/// <summary>
/// Orchestrates plugin installation from NuGet.
/// </summary>
public sealed class PluginInstaller
{
    private readonly INuGetClient nugetClient;
    private readonly PluginRegistry registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstaller"/> class.
    /// </summary>
    /// <param name="nugetClient">The NuGet client.</param>
    /// <param name="registry">The plugin registry.</param>
    public PluginInstaller(INuGetClient nugetClient, PluginRegistry registry)
    {
        this.nugetClient = nugetClient;
        this.registry = registry;
    }

    /// <summary>
    /// Creates an installation plan for a package.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <param name="version">Optional version. Latest if null.</param>
    /// <param name="source">Optional source URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installation plan.</returns>
    public async Task<InstallationPlan> CreatePlanAsync(
        string packageId,
        string? version,
        string? source,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get package metadata
            var packageInfo = await nugetClient.GetPackageMetadataAsync(
                packageId,
                version,
                source,
                cancellationToken);

            if (packageInfo == null)
            {
                return InstallationPlan.Invalid($"Package '{packageId}' not found.");
            }

            var packagesToInstall = new List<PackageToInstall>();

            // Resolve dependencies (depth-first)
            var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await ResolveDependenciesAsync(
                packageInfo,
                source,
                packagesToInstall,
                resolved,
                cancellationToken);

            // Add the primary package last (after dependencies)
            packagesToInstall.Add(new PackageToInstall
            {
                PackageId = packageInfo.PackageId,
                Version = packageInfo.Version,
                Source = source,
                IsPrimary = true
            });

            return InstallationPlan.Valid(packagesToInstall);
        }
        catch (Exception ex)
        {
            return InstallationPlan.Invalid($"Failed to create plan: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes an installation plan.
    /// </summary>
    /// <param name="plan">The plan to execute.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The installation result.</returns>
    public async Task<InstallationResult> ExecuteAsync(
        InstallationPlan plan,
        IProgress<string>? progress,
        CancellationToken cancellationToken = default)
    {
        if (!plan.IsValid)
        {
            return InstallationResult.Failed(plan.ErrorMessage ?? "Invalid plan.");
        }

        var installedPackages = new List<string>();
        string? primaryInstallPath = null;
        string? primaryVersion = null;

        try
        {
            foreach (var package in plan.PackagesToInstall)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report($"Installing {package.PackageId} v{package.Version}...");

                // Download package
                var nupkgPath = await nugetClient.DownloadPackageAsync(
                    package.PackageId,
                    package.Version,
                    package.Source,
                    cancellationToken);

                // Extract to cache
                var installPath = await nugetClient.ExtractToNuGetCacheAsync(
                    nupkgPath,
                    cancellationToken);

                installedPackages.Add(package.PackageId);

                if (package.IsPrimary)
                {
                    primaryInstallPath = installPath;
                    primaryVersion = package.Version;
                }

                // Clean up temp file if it exists
                if (File.Exists(nupkgPath) && nupkgPath.Contains("keeneyes-plugins"))
                {
                    try
                    {
                        File.Delete(nupkgPath);
                    }
                    catch
                    {
                        // Ignore cleanup failures
                    }
                }
            }

            // Register in the plugin registry
            var primary = plan.PackagesToInstall.FirstOrDefault(p => p.IsPrimary);
            if (primary != null)
            {
                var entry = new InstalledPluginEntry
                {
                    PackageId = primary.PackageId,
                    Version = primary.Version,
                    Source = primary.Source,
                    InstallPath = primaryInstallPath,
                    InstalledAt = DateTimeOffset.UtcNow,
                    Enabled = true,
                    Dependencies = plan.PackagesToInstall
                        .Where(p => !p.IsPrimary)
                        .Select(p => p.PackageId)
                        .ToList()
                };

                registry.RegisterPlugin(entry);
                registry.Save();
            }

            return InstallationResult.Succeeded(
                primaryVersion ?? "unknown",
                primaryInstallPath ?? "unknown",
                installedPackages);
        }
        catch (OperationCanceledException)
        {
            return InstallationResult.Failed("Installation cancelled.");
        }
        catch (Exception ex)
        {
            return InstallationResult.Failed($"Installation failed: {ex.Message}");
        }
    }

    private async Task ResolveDependenciesAsync(
        PluginPackageInfo packageInfo,
        string? source,
        List<PackageToInstall> packagesToInstall,
        HashSet<string> resolved,
        CancellationToken cancellationToken)
    {
        // Filter to .NET 10 compatible dependencies
        var targetFrameworks = new[] { "net10.0", "net9.0", "net8.0", "netstandard2.1", "netstandard2.0" };

        var dependencies = packageInfo.Dependencies
            .Where(d => d.TargetFramework == null ||
                        targetFrameworks.Any(tf =>
                            d.TargetFramework.Equals(tf, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var dep in dependencies)
        {
            if (resolved.Contains(dep.PackageId))
            {
                continue;
            }

            resolved.Add(dep.PackageId);

            // Check if already installed at compatible version
            // Check if installed version satisfies the dependency
            var installed = registry.GetInstalledPlugin(dep.PackageId);
            if (installed != null &&
                IsVersionSatisfied(installed.Version, dep.VersionRange))
            {
                continue;
            }

            // Parse version range to get minimum version
            var versionToInstall = GetMinimumVersion(dep.VersionRange);

            // Get dependency metadata (to resolve its dependencies)
            var depInfo = await nugetClient.GetPackageMetadataAsync(
                dep.PackageId,
                versionToInstall,
                source,
                cancellationToken);

            if (depInfo != null)
            {
                // Recursively resolve
                await ResolveDependenciesAsync(
                    depInfo,
                    source,
                    packagesToInstall,
                    resolved,
                    cancellationToken);

                packagesToInstall.Add(new PackageToInstall
                {
                    PackageId = depInfo.PackageId,
                    Version = depInfo.Version,
                    Source = source,
                    IsPrimary = false
                });
            }
        }
    }

    private static bool IsVersionSatisfied(string installedVersion, string versionRange)
    {
        try
        {
            var installed = NuGetVersion.Parse(installedVersion);
            var range = VersionRange.Parse(versionRange);
            return range.Satisfies(installed);
        }
        catch
        {
            return false;
        }
    }

    private static string? GetMinimumVersion(string versionRange)
    {
        try
        {
            var range = VersionRange.Parse(versionRange);
            return range.MinVersion?.ToNormalizedString();
        }
        catch
        {
            return null;
        }
    }
}
