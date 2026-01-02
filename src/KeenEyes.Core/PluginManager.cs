namespace KeenEyes;

/// <summary>
/// Manages plugin installation, uninstallation, and lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all plugin operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Plugins are tracked by name and can register systems that are automatically
/// cleaned up when the plugin is uninstalled.
/// </para>
/// <para>
/// This class is thread-safe: all plugin operations can be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
internal sealed class PluginManager
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<string, PluginEntry> plugins = [];
    private readonly World world;
    private readonly SystemManager systemManager;

    /// <summary>
    /// Creates a new plugin manager for the specified world.
    /// </summary>
    /// <param name="world">The world that owns this plugin manager.</param>
    /// <param name="systemManager">The system manager for removing plugin systems.</param>
    internal PluginManager(World world, SystemManager systemManager)
    {
        this.world = world;
        this.systemManager = systemManager;
    }

    /// <summary>
    /// Installs a plugin of the specified type into this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to install.</typeparam>
    internal void InstallPlugin<T>() where T : IWorldPlugin, new()
    {
        var plugin = new T();
        InstallPlugin(plugin);
    }

    /// <summary>
    /// Installs a plugin instance into this world.
    /// </summary>
    /// <param name="plugin">The plugin instance to install.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin with the same name is already installed.
    /// </exception>
    internal void InstallPlugin(IWorldPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        PluginContext context;
        lock (syncRoot)
        {
            if (plugins.ContainsKey(plugin.Name))
            {
                throw new InvalidOperationException(
                    $"A plugin with name '{plugin.Name}' is already installed in this world.");
            }

            context = new PluginContext(world, plugin);
            plugins[plugin.Name] = new PluginEntry(plugin, context);
        }

        // Call Install outside lock to allow plugins to use world APIs
        try
        {
            plugin.Install(context);
        }
        catch
        {
            // If Install throws, remove the plugin from the registry
            lock (syncRoot)
            {
                plugins.Remove(plugin.Name);
            }
            throw;
        }
    }

    /// <summary>
    /// Uninstalls a plugin of the specified type from this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to uninstall.</typeparam>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    internal bool UninstallPlugin<T>() where T : IWorldPlugin
    {
        string? pluginName;
        lock (syncRoot)
        {
            var entry = plugins.Values.FirstOrDefault(e => e.Plugin is T);
            if (entry is null)
            {
                return false;
            }
            pluginName = entry.Plugin.Name;
        }
        return UninstallPluginInternal(pluginName);
    }

    /// <summary>
    /// Uninstalls a plugin by name from this world.
    /// </summary>
    /// <param name="name">The name of the plugin to uninstall.</param>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    internal bool UninstallPlugin(string name)
    {
        return UninstallPluginInternal(name);
    }

    /// <summary>
    /// Gets a plugin of the specified type.
    /// </summary>
    /// <typeparam name="T">The plugin type to retrieve.</typeparam>
    /// <returns>The plugin instance, or null if not found.</returns>
    internal T? GetPlugin<T>() where T : class, IWorldPlugin
    {
        lock (syncRoot)
        {
            foreach (var entry in plugins.Values)
            {
                if (entry.Plugin is T typedPlugin)
                {
                    return typedPlugin;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Gets a plugin by name.
    /// </summary>
    /// <param name="name">The name of the plugin to retrieve.</param>
    /// <returns>The plugin instance, or null if not found.</returns>
    internal IWorldPlugin? GetPlugin(string name)
    {
        lock (syncRoot)
        {
            return plugins.TryGetValue(name, out var entry) ? entry.Plugin : null;
        }
    }

    /// <summary>
    /// Checks if a plugin of the specified type is installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    internal bool HasPlugin<T>() where T : IWorldPlugin
    {
        lock (syncRoot)
        {
            return plugins.Values.Any(e => e.Plugin is T);
        }
    }

    /// <summary>
    /// Checks if a plugin with the specified name is installed.
    /// </summary>
    /// <param name="name">The name of the plugin to check for.</param>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    internal bool HasPlugin(string name)
    {
        lock (syncRoot)
        {
            return plugins.ContainsKey(name);
        }
    }

    /// <summary>
    /// Gets all installed plugins.
    /// </summary>
    /// <returns>An enumerable of all installed plugins.</returns>
    internal IEnumerable<IWorldPlugin> GetPlugins()
    {
        lock (syncRoot)
        {
            // Return snapshot to avoid collection modification during enumeration
            return plugins.Values.Select(e => e.Plugin).ToArray();
        }
    }

    /// <summary>
    /// Uninstalls all plugins, disposing their registered systems.
    /// </summary>
    internal void UninstallAll()
    {
        string[] names;
        lock (syncRoot)
        {
            names = [.. plugins.Keys];
        }

        foreach (var name in names)
        {
            UninstallPluginInternal(name);
        }
    }

    /// <summary>
    /// Clears all plugin references without uninstalling.
    /// Used during world disposal after systems are already disposed.
    /// </summary>
    internal void Clear()
    {
        lock (syncRoot)
        {
            plugins.Clear();
        }
    }

    /// <summary>
    /// Uninstalls a plugin by name, handling cleanup of registered systems.
    /// </summary>
    private bool UninstallPluginInternal(string name)
    {
        PluginEntry? entry;
        lock (syncRoot)
        {
            if (!plugins.TryGetValue(name, out entry))
            {
                return false;
            }
            plugins.Remove(name);
        }

        // Call the plugin's uninstall hook (outside lock to allow plugins to use world APIs)
        entry.Plugin.Uninstall(entry.Context);

        // Remove and dispose all systems registered by the plugin
        foreach (var system in entry.Context.RegisteredSystems)
        {
            systemManager.RemoveSystem(system);
            system.Dispose();
        }

        return true;
    }

    /// <summary>
    /// Internal record for tracking plugin state.
    /// </summary>
    private sealed record PluginEntry(IWorldPlugin Plugin, PluginContext Context);
}
