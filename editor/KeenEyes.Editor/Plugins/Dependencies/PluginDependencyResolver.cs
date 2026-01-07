// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using NuGet.Versioning;

namespace KeenEyes.Editor.Plugins.Dependencies;

/// <summary>
/// Resolves plugin dependencies and determines load order.
/// </summary>
/// <remarks>
/// <para>
/// This class validates plugin dependencies, checks version constraints,
/// detects circular dependencies, and determines the correct load order
/// for a set of plugins.
/// </para>
/// <para>
/// The resolver uses Kahn's algorithm for topological sorting to determine
/// load order, ensuring dependencies are loaded before dependents.
/// </para>
/// </remarks>
internal sealed class PluginDependencyResolver
{
    private readonly NuGetVersion editorVersion;

    /// <summary>
    /// Creates a new plugin dependency resolver.
    /// </summary>
    /// <param name="editorVersion">The current editor version.</param>
    public PluginDependencyResolver(string editorVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editorVersion);

        if (!NuGetVersion.TryParse(editorVersion, out var version))
        {
            throw new ArgumentException(
                $"Invalid editor version: '{editorVersion}'",
                nameof(editorVersion));
        }

        this.editorVersion = version;
    }

    /// <summary>
    /// Resolves dependencies for a set of plugins and returns the load order.
    /// </summary>
    /// <param name="plugins">The plugins to resolve, keyed by plugin ID.</param>
    /// <returns>The validation result containing load order or errors.</returns>
    public DependencyGraphValidationResult Resolve(IReadOnlyDictionary<string, LoadedPlugin> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        if (plugins.Count == 0)
        {
            return DependencyGraphValidationResult.Success([]);
        }

        var errors = new List<DependencyError>();

        // Step 1: Validate editor version compatibility for all plugins
        foreach (var (pluginId, plugin) in plugins)
        {
            var compatibilityError = CheckEditorCompatibility(pluginId, plugin.Manifest);
            if (compatibilityError is not null)
            {
                errors.Add(compatibilityError);
            }
        }

        // If there are editor compatibility errors, return them before checking dependencies
        if (errors.Count > 0)
        {
            return DependencyGraphValidationResult.Failure(errors);
        }

        // Step 2: Build dependency graph and validate dependencies
        var graph = new DependencyGraph();

        foreach (var (pluginId, plugin) in plugins)
        {
            graph.AddPlugin(pluginId);

            foreach (var (dependencyId, constraintStr) in plugin.Manifest.Dependencies)
            {
                // Check if dependency is installed
                if (!plugins.TryGetValue(dependencyId, out var dependency))
                {
                    errors.Add(new MissingDependencyError(pluginId, dependencyId, constraintStr));
                    continue;
                }

                // Check if dependency version satisfies constraint
                if (!VersionConstraint.TryParse(constraintStr, out var constraint))
                {
                    errors.Add(new MissingDependencyError(
                        pluginId,
                        dependencyId,
                        $"{constraintStr} (invalid constraint)"));
                    continue;
                }

                if (!constraint.IsSatisfiedBy(dependency.Manifest.Version))
                {
                    errors.Add(new VersionMismatchError(
                        pluginId,
                        dependencyId,
                        constraintStr,
                        dependency.Manifest.Version));
                    continue;
                }

                // Add edge: dependency → dependent (dependency loaded first)
                graph.AddDependency(dependencyId, pluginId);
            }
        }

        // If there are missing/version errors, return them before checking cycles
        if (errors.Count > 0)
        {
            return DependencyGraphValidationResult.Failure(errors);
        }

        // Step 3: Perform topological sort and detect cycles
        var (loadOrder, cycleParticipants) = graph.TopologicalSort();

        if (cycleParticipants.Count > 0)
        {
            // Find the actual cycle path for a better error message
            var cyclePath = graph.FindCyclePath(cycleParticipants[0]);
            if (cyclePath.Count == 0)
            {
                // Fallback if we can't find the path
                cyclePath = [.. cycleParticipants, cycleParticipants[0]];
            }

            errors.Add(new CyclicDependencyError(cycleParticipants[0], cyclePath));
            return DependencyGraphValidationResult.Failure(errors);
        }

        return DependencyGraphValidationResult.Success(loadOrder);
    }

    /// <summary>
    /// Checks if a single plugin can be loaded given currently loaded plugins.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to check.</param>
    /// <param name="manifest">The plugin's manifest.</param>
    /// <param name="loadedPlugins">The currently loaded plugins.</param>
    /// <returns>The validation result.</returns>
    public DependencyGraphValidationResult CanLoad(
        string pluginId,
        PluginManifest manifest,
        IReadOnlyDictionary<string, LoadedPlugin> loadedPlugins)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(loadedPlugins);

        var errors = new List<DependencyError>();

        // Check editor compatibility
        var compatibilityError = CheckEditorCompatibility(pluginId, manifest);
        if (compatibilityError is not null)
        {
            return DependencyGraphValidationResult.Failure(compatibilityError);
        }

        // Check all dependencies
        foreach (var (dependencyId, constraintStr) in manifest.Dependencies)
        {
            if (!loadedPlugins.TryGetValue(dependencyId, out var dependency))
            {
                errors.Add(new MissingDependencyError(pluginId, dependencyId, constraintStr));
                continue;
            }

            if (!VersionConstraint.TryParse(constraintStr, out var constraint))
            {
                errors.Add(new MissingDependencyError(
                    pluginId,
                    dependencyId,
                    $"{constraintStr} (invalid constraint)"));
                continue;
            }

            if (!constraint.IsSatisfiedBy(dependency.Manifest.Version))
            {
                errors.Add(new VersionMismatchError(
                    pluginId,
                    dependencyId,
                    constraintStr,
                    dependency.Manifest.Version));
            }
        }

        if (errors.Count > 0)
        {
            return DependencyGraphValidationResult.Failure(errors);
        }

        return DependencyGraphValidationResult.Success([pluginId]);
    }

    /// <summary>
    /// Checks if a plugin can be unloaded without breaking dependents.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to unload.</param>
    /// <param name="loadedPlugins">The currently loaded plugins.</param>
    /// <returns>
    /// A tuple indicating whether unload is possible and the list of
    /// blocking dependents if not.
    /// </returns>
    public static (bool CanUnload, IReadOnlyList<string> BlockingDependents) CanUnload(
        string pluginId,
        IReadOnlyDictionary<string, LoadedPlugin> loadedPlugins)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(loadedPlugins);

        var blockingDependents = new List<string>();

        foreach (var (id, plugin) in loadedPlugins)
        {
            if (id == pluginId)
            {
                continue;
            }

            // Check if this plugin depends on the one being unloaded
            // Only count enabled plugins as blocking
            if (plugin.Manifest.Dependencies.ContainsKey(pluginId) &&
                plugin.State == PluginState.Enabled)
            {
                blockingDependents.Add(id);
            }
        }

        return (blockingDependents.Count == 0, blockingDependents);
    }

    /// <summary>
    /// Gets the unload order for a set of plugins (reverse of load order).
    /// </summary>
    /// <param name="plugins">The plugins to order.</param>
    /// <returns>The plugin IDs in unload order (dependents first).</returns>
    public IReadOnlyList<string> GetUnloadOrder(IReadOnlyDictionary<string, LoadedPlugin> plugins)
    {
        var result = Resolve(plugins);
        if (!result.IsValid)
        {
            // If there are errors, just return plugins in arbitrary order
            return [.. plugins.Keys];
        }

        // Reverse the load order for unloading
        var unloadOrder = result.LoadOrder.ToList();
        unloadOrder.Reverse();
        return unloadOrder;
    }

    private EditorVersionIncompatibleError? CheckEditorCompatibility(
        string pluginId,
        PluginManifest manifest)
    {
        var compatibility = manifest.Compatibility;
        if (compatibility is null)
        {
            return null;
        }

        // Check minimum version
        if (compatibility.MinEditorVersion is not null &&
            NuGetVersion.TryParse(compatibility.MinEditorVersion, out var minVersion) &&
            editorVersion < minVersion)
        {
            return new EditorVersionIncompatibleError(
                pluginId,
                editorVersion.ToString(),
                compatibility.MinEditorVersion,
                compatibility.MaxEditorVersion);
        }

        // Check maximum version
        if (compatibility.MaxEditorVersion is not null &&
            NuGetVersion.TryParse(compatibility.MaxEditorVersion, out var maxVersion) &&
            editorVersion > maxVersion)
        {
            return new EditorVersionIncompatibleError(
                pluginId,
                editorVersion.ToString(),
                compatibility.MinEditorVersion,
                compatibility.MaxEditorVersion);
        }

        return null;
    }
}
