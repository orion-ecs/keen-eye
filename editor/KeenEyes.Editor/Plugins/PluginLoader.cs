// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Loader;
using KeenEyes.Editor.Abstractions;

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

    /// <summary>
    /// Creates a new plugin loader.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public PluginLoader(IEditorPluginLogger? logger = null)
    {
        this.logger = logger;
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
        if (plugin.State == PluginState.Discovered)
        {
            // Nothing to unload
            return true;
        }

        if (!plugin.SupportsHotReload)
        {
            logger?.LogWarning(
                $"Plugin '{plugin.Manifest.Id}' does not support hot reload. " +
                "Restart the editor to fully unload.");
            return false;
        }

        if (plugin.LoadContext == null)
        {
            logger?.LogWarning($"Plugin '{plugin.Manifest.Id}' has no load context to unload");
            return false;
        }

        plugin.State = PluginState.Unloading;

        try
        {
            // Clear references to the plugin instance
            plugin.Instance = null;
            plugin.Context = null;

            // Unload the context
            var weakRef = TryUnloadContext(plugin.LoadContext);
            plugin.LoadContext = null;

            // Wait for GC to collect the assembly (with timeout)
            if (weakRef != null)
            {
                WaitForUnload(weakRef, plugin.Manifest.Id);
            }

            plugin.State = PluginState.Discovered;
            logger?.LogInfo($"Unloaded plugin '{plugin.Manifest.Id}'");
            return true;
        }
        catch (Exception ex)
        {
            plugin.State = PluginState.Failed;
            plugin.ErrorMessage = $"Unload failed: {ex.Message}";
            logger?.LogError($"Failed to unload plugin '{plugin.Manifest.Id}': {ex.Message}");
            return false;
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

    private WeakReference? TryUnloadContext(PluginLoadContext context)
    {
        if (!context.IsCollectible)
        {
            return null;
        }

        var weakRef = new WeakReference(context);
        context.Unload();
        return weakRef;
    }

    private void WaitForUnload(WeakReference weakRef, string pluginId, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts && weakRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (weakRef.IsAlive)
        {
            logger?.LogWarning(
                $"Plugin '{pluginId}' assembly may not have fully unloaded. " +
                "There may be lingering references.");
        }
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
