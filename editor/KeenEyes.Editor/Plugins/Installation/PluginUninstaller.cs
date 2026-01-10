// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Editor.Plugins.Installation;

/// <summary>
/// Result of an uninstall operation.
/// </summary>
public sealed class UninstallResult
{
    /// <summary>
    /// Gets whether the uninstall succeeded.
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Gets the error message if the uninstall failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Gets the list of plugins that were uninstalled.
    /// </summary>
    public IReadOnlyList<string> UninstalledPlugins { get; private init; } = [];

    /// <summary>
    /// Gets the list of plugins that depend on the uninstalled plugin.
    /// </summary>
    public IReadOnlyList<string> DependentPlugins { get; private init; } = [];

    /// <summary>
    /// Creates a successful uninstall result.
    /// </summary>
    /// <param name="uninstalledPlugins">The plugins that were uninstalled.</param>
    /// <returns>A successful result.</returns>
    public static UninstallResult Succeeded(IReadOnlyList<string> uninstalledPlugins)
    {
        return new UninstallResult
        {
            Success = true,
            UninstalledPlugins = uninstalledPlugins
        };
    }

    /// <summary>
    /// Creates a failed uninstall result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UninstallResult Failed(string errorMessage)
    {
        return new UninstallResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a result indicating the plugin has dependents.
    /// </summary>
    /// <param name="dependentPlugins">The plugins that depend on the target.</param>
    /// <returns>A result indicating dependencies exist.</returns>
    public static UninstallResult HasDependents(IReadOnlyList<string> dependentPlugins)
    {
        return new UninstallResult
        {
            Success = false,
            ErrorMessage = $"Cannot uninstall: {dependentPlugins.Count} plugin(s) depend on this package.",
            DependentPlugins = dependentPlugins
        };
    }
}

/// <summary>
/// Orchestrates plugin uninstallation with dependency checking.
/// </summary>
public sealed class PluginUninstaller
{
    private readonly PluginRegistry registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginUninstaller"/> class.
    /// </summary>
    /// <param name="registry">The plugin registry.</param>
    public PluginUninstaller(PluginRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>
    /// Checks if a plugin can be uninstalled.
    /// </summary>
    /// <param name="packageId">The package ID to check.</param>
    /// <returns>A tuple indicating if uninstall is possible and any dependent plugins.</returns>
    public (bool CanUninstall, IReadOnlyList<string> Dependents) CanUninstall(string packageId)
    {
        // Check if the plugin is installed
        if (!registry.IsInstalled(packageId))
        {
            return (false, []);
        }

        // Check for dependents
        var dependents = registry.GetDependentPlugins(packageId);
        var dependentIds = dependents.Select(d => d.PackageId).ToList();

        return (dependentIds.Count == 0, dependentIds);
    }

    /// <summary>
    /// Uninstalls a plugin if possible.
    /// </summary>
    /// <param name="packageId">The package ID to uninstall.</param>
    /// <param name="force">If true, uninstall even if there are dependents.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>The uninstall result.</returns>
    public UninstallResult Uninstall(
        string packageId,
        bool force = false,
        IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report($"Checking plugin: {packageId}...");

            // Check if installed
            var plugin = registry.GetInstalledPlugin(packageId);
            if (plugin == null)
            {
                return UninstallResult.Failed($"Plugin '{packageId}' is not installed.");
            }

            // Check for dependents
            var (canUninstall, dependents) = CanUninstall(packageId);
            if (!canUninstall && !force)
            {
                return UninstallResult.HasDependents(dependents);
            }

            var uninstalled = new List<string>();

            // If force uninstall, we still warn about dependents but proceed
            if (!canUninstall && force)
            {
                progress?.Report($"Warning: {dependents.Count} plugin(s) depend on this package.");
            }

            // Remove the plugin from registry
            progress?.Report($"Unregistering {packageId}...");
            registry.UnregisterPlugin(packageId);
            uninstalled.Add(packageId);

            // Optionally clean up unused dependencies
            var orphanedDeps = FindOrphanedDependencies(plugin.Dependencies, packageId);
            foreach (var dep in orphanedDeps)
            {
                progress?.Report($"Removing orphaned dependency: {dep}...");
                registry.UnregisterPlugin(dep);
                uninstalled.Add(dep);
            }

            // Save changes
            registry.Save();

            progress?.Report("Uninstall complete.");
            return UninstallResult.Succeeded(uninstalled);
        }
        catch (Exception ex)
        {
            return UninstallResult.Failed($"Uninstall failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Uninstalls a plugin and all of its dependents (cascade uninstall).
    /// </summary>
    /// <param name="packageId">The package ID to uninstall.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>The uninstall result.</returns>
    public UninstallResult UninstallCascade(
        string packageId,
        IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report($"Planning cascade uninstall for: {packageId}...");

            // Check if installed
            var plugin = registry.GetInstalledPlugin(packageId);
            if (plugin == null)
            {
                return UninstallResult.Failed($"Plugin '{packageId}' is not installed.");
            }

            // Build uninstall order (dependents first, then target)
            var uninstallOrder = new List<string>();
            BuildUninstallOrder(packageId, uninstallOrder, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            // Uninstall in order
            foreach (var id in uninstallOrder)
            {
                progress?.Report($"Unregistering {id}...");
                registry.UnregisterPlugin(id);
            }

            // Save changes
            registry.Save();

            progress?.Report($"Cascade uninstall complete. Removed {uninstallOrder.Count} plugin(s).");
            return UninstallResult.Succeeded(uninstallOrder);
        }
        catch (Exception ex)
        {
            return UninstallResult.Failed($"Cascade uninstall failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a preview of what would be uninstalled in a cascade uninstall.
    /// </summary>
    /// <param name="packageId">The package ID.</param>
    /// <returns>List of plugins that would be uninstalled.</returns>
    public IReadOnlyList<string> PreviewCascadeUninstall(string packageId)
    {
        var uninstallOrder = new List<string>();
        BuildUninstallOrder(packageId, uninstallOrder, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return uninstallOrder;
    }

    private void BuildUninstallOrder(
        string packageId,
        List<string> order,
        HashSet<string> visited)
    {
        if (visited.Contains(packageId))
        {
            return;
        }

        visited.Add(packageId);

        // First, add all dependents (recursively)
        var dependents = registry.GetDependentPlugins(packageId);
        foreach (var dep in dependents)
        {
            BuildUninstallOrder(dep.PackageId, order, visited);
        }

        // Then add this package
        order.Add(packageId);
    }

    /// <summary>
    /// Finds dependencies that are no longer needed by any other installed plugins.
    /// </summary>
    /// <param name="dependencies">The dependencies of the plugin being uninstalled.</param>
    /// <param name="excludePackageId">The package ID being uninstalled (to exclude from dependency checks).</param>
    /// <returns>List of orphaned dependency package IDs that can be safely removed.</returns>
    private List<string> FindOrphanedDependencies(List<string> dependencies, string excludePackageId)
    {
        var orphaned = new List<string>();

        foreach (var dependency in dependencies)
        {
            // Skip if the dependency isn't installed (might have been manually removed)
            if (!registry.IsInstalled(dependency))
            {
                continue;
            }

            // Check if any other installed plugin still needs this dependency
            var dependents = registry.GetDependentPlugins(dependency);
            var otherDependents = dependents
                .Where(d => !d.PackageId.Equals(excludePackageId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // If no other plugins depend on this, it's orphaned
            if (otherDependents.Count == 0)
            {
                orphaned.Add(dependency);
            }
        }

        return orphaned;
    }
}
