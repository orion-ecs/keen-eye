// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Handles loading and unloading of plugin assemblies.
/// </summary>
/// <remarks>
/// <para>
/// The plugin loader is responsible for:
/// </para>
/// <list type="bullet">
/// <item>Creating isolated <see cref="PluginLoadContext"/> for each plugin</item>
/// <item>Loading plugin assemblies and instantiating plugin types</item>
/// <item>Unloading plugin assemblies (for hot-reloadable plugins)</item>
/// <item>Managing shared assembly references</item>
/// </list>
/// </remarks>
internal sealed class PluginLoader
{
    private readonly IEditorPluginLogger? logger;
    private readonly TypeCacheManager? typeCacheManager;
    private readonly PluginSecurityManager? securityManager;

    /// <summary>
    /// Event raised when unload diagnostics are available.
    /// </summary>
    public event Action<UnloadDiagnostics>? OnUnloadDiagnostics;

    /// <summary>
    /// Creates a new plugin loader.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="typeCacheManager">Optional type cache manager for clearing caches on unload.</param>
    /// <param name="securityManager">Optional security manager for pre-load security checks.</param>
    public PluginLoader(
        IEditorPluginLogger? logger = null,
        TypeCacheManager? typeCacheManager = null,
        PluginSecurityManager? securityManager = null)
    {
        this.logger = logger;
        this.typeCacheManager = typeCacheManager;
        this.securityManager = securityManager;
    }

    /// <summary>
    /// Loads a plugin assembly into an isolated context.
    /// </summary>
    /// <param name="plugin">The plugin entry to load.</param>
    /// <returns>True if loading succeeded; false otherwise.</returns>
    public bool Load(LoadedPlugin plugin)
    {
        if (plugin.State != PluginState.Discovered && plugin.State != PluginState.Failed)
        {
            logger?.LogWarning($"Plugin '{plugin.Manifest.Id}' is already loaded (state: {plugin.State})");
            return plugin.State == PluginState.Loaded;
        }

        var assemblyPath = plugin.GetAssemblyPath();
        if (!File.Exists(assemblyPath))
        {
            plugin.State = PluginState.Failed;
            plugin.ErrorMessage = $"Assembly not found: {assemblyPath}";
            logger?.LogError($"Failed to load plugin '{plugin.Manifest.Id}': {plugin.ErrorMessage}");
            return false;
        }

        // Perform security checks before loading
        if (securityManager != null)
        {
            var securityResult = securityManager.CheckPlugin(plugin);
            plugin.SecurityResult = securityResult;

            if (!securityResult.CanLoad)
            {
                plugin.State = PluginState.Failed;
                plugin.ErrorMessage = $"Security check failed: {string.Join("; ", securityResult.BlockingReasons)}";
                logger?.LogError($"Plugin '{plugin.Manifest.Id}' blocked by security: {plugin.ErrorMessage}");
                return false;
            }

            foreach (var warning in securityResult.Warnings)
            {
                logger?.LogWarning($"Plugin '{plugin.Manifest.Id}': {warning}");
            }
        }

        try
        {
            // Create an isolated load context for this plugin
            var isCollectible = plugin.SupportsHotReload;
            var loadContext = new PluginLoadContext(
                plugin.Manifest.Id,
                assemblyPath,
                isCollectible);

            plugin.LoadContext = loadContext;

            // Load the assembly
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            // Find the plugin type
            var pluginType = assembly.GetType(plugin.Manifest.EntryPoint.Type);
            if (pluginType == null)
            {
                throw new TypeLoadException(
                    $"Type '{plugin.Manifest.EntryPoint.Type}' not found in assembly");
            }

            // Verify it implements IEditorPlugin
            if (!typeof(IEditorPlugin).IsAssignableFrom(pluginType))
            {
                throw new TypeLoadException(
                    $"Type '{pluginType.FullName}' does not implement IEditorPlugin");
            }

            // Create the plugin instance
            var instance = Activator.CreateInstance(pluginType) as IEditorPlugin;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create instance of '{pluginType.FullName}'");
            }

            plugin.Instance = instance;
            plugin.State = PluginState.Loaded;
            plugin.ErrorMessage = null;

            logger?.LogInfo($"Loaded plugin '{plugin.Manifest.Id}' v{plugin.Manifest.Version}");
            return true;
        }
        catch (Exception ex)
        {
            plugin.State = PluginState.Failed;
            plugin.ErrorMessage = ex.Message;
            plugin.Instance = null;

            // Clean up the load context if it was created
            if (plugin.LoadContext != null)
            {
                TryUnloadContext(plugin.LoadContext);
                plugin.LoadContext = null;
            }

            logger?.LogError($"Failed to load plugin '{plugin.Manifest.Id}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unloads a plugin assembly.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <returns>True if unloading succeeded; false if the plugin doesn't support unloading.</returns>
    /// <remarks>
    /// Only plugins with <see cref="PluginManifest.Capabilities"/> set to support
    /// hot reload can be unloaded. Other plugins require an editor restart.
    /// </remarks>
    public bool Unload(LoadedPlugin plugin)
    {
        var diagnostics = UnloadWithDiagnostics(plugin);
        return diagnostics?.FullyUnloaded ?? false;
    }

    /// <summary>
    /// Unloads a plugin assembly and returns detailed diagnostics.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <returns>Diagnostics about the unload operation, or null if unload wasn't attempted.</returns>
    public UnloadDiagnostics? UnloadWithDiagnostics(LoadedPlugin plugin)
    {
        if (plugin.State == PluginState.Discovered)
        {
            // Nothing to unload - return success
            return new UnloadDiagnostics
            {
                PluginId = plugin.Manifest.Id,
                UnloadDuration = TimeSpan.Zero,
                FullyUnloaded = true,
                GcAttempts = 0,
                PotentialHolders = []
            };
        }

        if (!plugin.SupportsHotReload)
        {
            logger?.LogWarning(
                $"Plugin '{plugin.Manifest.Id}' does not support hot reload. " +
                "Restart the editor to fully unload.");
            return null;
        }

        if (plugin.LoadContext == null)
        {
            logger?.LogWarning($"Plugin '{plugin.Manifest.Id}' has no load context to unload");
            return null;
        }

        var stopwatch = Stopwatch.StartNew();
        plugin.State = PluginState.Unloading;

        // Capture resource info before clearing references
        IReadOnlyDictionary<string, int>? resourceCounts = null;
        IReadOnlyList<string> potentialHolders = [];

        if (plugin.Context != null)
        {
            resourceCounts = plugin.Context.GetTrackedResourceCounts();
            potentialHolders = plugin.Context.GetLiveResourceDescriptions();
        }

        try
        {
            // Notify type caches before clearing references
            typeCacheManager?.NotifyPluginUnloading(plugin.Manifest.Id);

            // Clear references to the plugin instance
            plugin.Instance = null;
            plugin.Context = null;

            // Unload the context
            var weakRef = TryUnloadContext(plugin.LoadContext);
            plugin.LoadContext = null;

            // Wait for GC to collect the assembly
            int gcAttempts = 0;
            bool fullyUnloaded = true;

            if (weakRef != null)
            {
                (gcAttempts, fullyUnloaded) = WaitForUnloadWithDiagnostics(weakRef, plugin.Manifest.Id);
            }

            stopwatch.Stop();

            plugin.State = PluginState.Discovered;
            logger?.LogInfo($"Unloaded plugin '{plugin.Manifest.Id}'");

            var diagnostics = new UnloadDiagnostics
            {
                PluginId = plugin.Manifest.Id,
                UnloadDuration = stopwatch.Elapsed,
                FullyUnloaded = fullyUnloaded,
                GcAttempts = gcAttempts,
                PotentialHolders = potentialHolders,
                ResourceCounts = resourceCounts
            };

            // Raise event for UI
            OnUnloadDiagnostics?.Invoke(diagnostics);

            return diagnostics;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            plugin.State = PluginState.Failed;
            plugin.ErrorMessage = $"Unload failed: {ex.Message}";
            logger?.LogError($"Failed to unload plugin '{plugin.Manifest.Id}': {ex.Message}");

            return new UnloadDiagnostics
            {
                PluginId = plugin.Manifest.Id,
                UnloadDuration = stopwatch.Elapsed,
                FullyUnloaded = false,
                GcAttempts = 0,
                PotentialHolders = [$"Exception: {ex.Message}"],
                ResourceCounts = resourceCounts
            };
        }
    }

    /// <summary>
    /// Reloads a plugin by unloading and loading it again.
    /// </summary>
    /// <param name="plugin">The plugin to reload.</param>
    /// <returns>True if reloading succeeded; false otherwise.</returns>
    public bool Reload(LoadedPlugin plugin)
    {
        if (!plugin.SupportsHotReload)
        {
            logger?.LogWarning($"Plugin '{plugin.Manifest.Id}' does not support hot reload");
            return false;
        }

        if (!Unload(plugin))
        {
            return false;
        }

        if (!Load(plugin))
        {
            return false;
        }

        // Note: The caller is responsible for re-enabling the plugin if it was enabled
        return true;
    }

    private static WeakReference? TryUnloadContext(PluginLoadContext context)
    {
        if (!context.IsCollectible)
        {
            return null;
        }

        var weakRef = new WeakReference(context);
        context.Unload();
        return weakRef;
    }

    private (int attempts, bool fullyUnloaded) WaitForUnloadWithDiagnostics(
        WeakReference weakRef,
        string pluginId,
        int maxAttempts = 10)
    {
        // GC.Collect is required for proper AssemblyLoadContext unloading - this is the recommended pattern
        // See: https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
#pragma warning disable S1215 // GC.Collect should not be called
        int attempts = 0;
        for (; attempts < maxAttempts && weakRef.IsAlive; attempts++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
#pragma warning restore S1215

        bool fullyUnloaded = !weakRef.IsAlive;

        if (!fullyUnloaded)
        {
            logger?.LogWarning(
                $"Plugin '{pluginId}' assembly may not have fully unloaded. " +
                "There may be lingering references.");
        }

        return (attempts, fullyUnloaded);
    }
}

/// <summary>
/// Interface for plugin loader diagnostic logging.
/// </summary>
internal interface IEditorPluginLogger
{
    /// <summary>Logs an informational message.</summary>
    void LogInfo(string message);

    /// <summary>Logs a warning message.</summary>
    void LogWarning(string message);

    /// <summary>Logs an error message.</summary>
    void LogError(string message);
}
