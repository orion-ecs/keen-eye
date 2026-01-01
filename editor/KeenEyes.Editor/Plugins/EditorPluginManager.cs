// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Manages the lifecycle of editor plugins.
/// </summary>
/// <remarks>
/// <para>
/// The plugin manager handles plugin installation, initialization, and shutdown.
/// It tracks resources registered by plugins and ensures proper cleanup when
/// plugins are uninstalled.
/// </para>
/// <para>
/// Plugins are typically installed during editor startup and remain active
/// until the editor is closed.
/// </para>
/// </remarks>
internal sealed class EditorPluginManager : IDisposable
{
    private readonly Dictionary<string, EditorPluginEntry> plugins = [];
    private readonly Dictionary<Type, IEditorCapability> capabilities = [];
    private readonly Dictionary<Type, object> globalExtensions = [];

    // Event handlers (to be wired up by EditorApplication)
    private readonly List<Action<IWorld>> sceneOpenedHandlers = [];
    private readonly List<Action> sceneClosedHandlers = [];
    private readonly List<Action<IReadOnlyList<Entity>>> selectionChangedHandlers = [];
    private readonly List<Action<EditorPlayState>> playModeChangedHandlers = [];

    private bool disposed;

    /// <summary>
    /// Gets the editor world manager.
    /// </summary>
    internal IEditorWorldManager Worlds { get; }

    /// <summary>
    /// Gets the selection manager.
    /// </summary>
    internal ISelectionManager Selection { get; }

    /// <summary>
    /// Gets the undo/redo manager.
    /// </summary>
    internal IUndoRedoManager UndoRedo { get; }

    /// <summary>
    /// Gets the asset database.
    /// </summary>
    internal IAssetDatabase Assets { get; }

    /// <summary>
    /// Gets the editor UI world.
    /// </summary>
    internal IWorld EditorWorld { get; }

    /// <summary>
    /// Creates a new plugin manager with the specified services.
    /// </summary>
    /// <param name="worlds">The editor world manager.</param>
    /// <param name="selection">The selection manager.</param>
    /// <param name="undoRedo">The undo/redo manager.</param>
    /// <param name="assets">The asset database.</param>
    /// <param name="editorWorld">The editor UI world.</param>
    internal EditorPluginManager(
        IEditorWorldManager worlds,
        ISelectionManager selection,
        IUndoRedoManager undoRedo,
        IAssetDatabase assets,
        IWorld editorWorld)
    {
        Worlds = worlds;
        Selection = selection;
        UndoRedo = undoRedo;
        Assets = assets;
        EditorWorld = editorWorld;
    }

    #region Plugin Lifecycle

    /// <summary>
    /// Installs a plugin of the specified type.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>The installed plugin instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a plugin with the same name is already installed.</exception>
    public T InstallPlugin<T>() where T : IEditorPlugin, new()
    {
        var plugin = new T();
        InstallPlugin(plugin);
        return plugin;
    }

    /// <summary>
    /// Installs a plugin instance.
    /// </summary>
    /// <param name="plugin">The plugin to install.</param>
    /// <exception cref="InvalidOperationException">Thrown if a plugin with the same name is already installed.</exception>
    public void InstallPlugin(IEditorPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugins.ContainsKey(plugin.Name))
        {
            throw new InvalidOperationException(
                $"A plugin named '{plugin.Name}' is already installed.");
        }

        var context = new EditorPluginContext(this, plugin);
        plugin.Initialize(context);
        plugins[plugin.Name] = new EditorPluginEntry(plugin, context);
    }

    /// <summary>
    /// Uninstalls a plugin by type.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    public bool UninstallPlugin<T>() where T : IEditorPlugin
    {
        var entry = plugins.Values.FirstOrDefault(e => e.Plugin is T);
        if (entry is null)
        {
            return false;
        }

        return UninstallPlugin(entry.Plugin.Name);
    }

    /// <summary>
    /// Uninstalls a plugin by name.
    /// </summary>
    /// <param name="name">The plugin name.</param>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    public bool UninstallPlugin(string name)
    {
        if (!plugins.TryGetValue(name, out var entry))
        {
            return false;
        }

        // Dispose tracked resources
        entry.Context.DisposeSubscriptions();

        // Shutdown the plugin
        entry.Plugin.Shutdown();

        plugins.Remove(name);
        return true;
    }

    /// <summary>
    /// Gets a plugin by type.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>The plugin instance, or null if not found.</returns>
    public T? GetPlugin<T>() where T : class, IEditorPlugin
    {
        return plugins.Values.FirstOrDefault(e => e.Plugin is T)?.Plugin as T;
    }

    /// <summary>
    /// Gets a plugin by name.
    /// </summary>
    /// <param name="name">The plugin name.</param>
    /// <returns>The plugin instance, or null if not found.</returns>
    public IEditorPlugin? GetPlugin(string name)
    {
        return plugins.TryGetValue(name, out var entry) ? entry.Plugin : null;
    }

    /// <summary>
    /// Checks if a plugin is installed.
    /// </summary>
    /// <typeparam name="T">The plugin type.</typeparam>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin<T>() where T : IEditorPlugin
    {
        return plugins.Values.Any(e => e.Plugin is T);
    }

    /// <summary>
    /// Checks if a plugin is installed.
    /// </summary>
    /// <param name="name">The plugin name.</param>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin(string name)
    {
        return plugins.ContainsKey(name);
    }

    /// <summary>
    /// Gets all installed plugins.
    /// </summary>
    /// <returns>An enumerable of installed plugins.</returns>
    public IEnumerable<IEditorPlugin> GetPlugins()
    {
        return plugins.Values.Select(e => e.Plugin);
    }

    /// <summary>
    /// Shuts down all installed plugins.
    /// </summary>
    public void ShutdownAll()
    {
        foreach (var name in plugins.Keys.ToList())
        {
            UninstallPlugin(name);
        }
    }

    #endregion

    #region Capability Management

    /// <summary>
    /// Registers a capability with the manager.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <param name="capability">The capability implementation.</param>
    public void RegisterCapability<T>(T capability) where T : class, IEditorCapability
    {
        ArgumentNullException.ThrowIfNull(capability);
        capabilities[typeof(T)] = capability;
    }

    /// <summary>
    /// Tries to get a capability.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <param name="capability">The capability, if found.</param>
    /// <returns>True if the capability is available; false otherwise.</returns>
    public bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability
    {
        if (capabilities.TryGetValue(typeof(T), out var obj))
        {
            capability = (T)obj;
            return true;
        }

        capability = null;
        return false;
    }

    /// <summary>
    /// Checks if a capability is available.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>True if the capability is available; false otherwise.</returns>
    public bool HasCapability<T>() where T : class, IEditorCapability
    {
        return capabilities.ContainsKey(typeof(T));
    }

    #endregion

    #region Global Extensions

    /// <summary>
    /// Sets a global extension that can be accessed by all plugins.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension instance.</param>
    public void SetGlobalExtension<T>(T extension) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        globalExtensions[typeof(T)] = extension;
    }

    /// <summary>
    /// Tries to get a global extension.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension, if found.</param>
    /// <returns>True if the extension exists; false otherwise.</returns>
    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        if (globalExtensions.TryGetValue(typeof(T), out var obj))
        {
            extension = (T)obj;
            return true;
        }

        extension = null;
        return false;
    }

    #endregion

    #region Event Subscriptions

    /// <summary>
    /// Subscribes to scene opened events.
    /// </summary>
    internal EventSubscription SubscribeSceneOpened(Action<IWorld> handler)
    {
        sceneOpenedHandlers.Add(handler);
        return new EventSubscription(() => sceneOpenedHandlers.Remove(handler));
    }

    /// <summary>
    /// Subscribes to scene closed events.
    /// </summary>
    internal EventSubscription SubscribeSceneClosed(Action handler)
    {
        sceneClosedHandlers.Add(handler);
        return new EventSubscription(() => sceneClosedHandlers.Remove(handler));
    }

    /// <summary>
    /// Subscribes to selection changed events.
    /// </summary>
    internal EventSubscription SubscribeSelectionChanged(Action<IReadOnlyList<Entity>> handler)
    {
        selectionChangedHandlers.Add(handler);
        return new EventSubscription(() => selectionChangedHandlers.Remove(handler));
    }

    /// <summary>
    /// Subscribes to play mode changed events.
    /// </summary>
    internal EventSubscription SubscribePlayModeChanged(Action<EditorPlayState> handler)
    {
        playModeChangedHandlers.Add(handler);
        return new EventSubscription(() => playModeChangedHandlers.Remove(handler));
    }

    /// <summary>
    /// Raises the scene opened event.
    /// </summary>
    internal void RaiseSceneOpened(IWorld world)
    {
        foreach (var handler in sceneOpenedHandlers)
        {
            handler(world);
        }
    }

    /// <summary>
    /// Raises the scene closed event.
    /// </summary>
    internal void RaiseSceneClosed()
    {
        foreach (var handler in sceneClosedHandlers)
        {
            handler();
        }
    }

    /// <summary>
    /// Raises the selection changed event.
    /// </summary>
    internal void RaiseSelectionChanged(IReadOnlyList<Entity> entities)
    {
        foreach (var handler in selectionChangedHandlers)
        {
            handler(entities);
        }
    }

    /// <summary>
    /// Raises the play mode changed event.
    /// </summary>
    internal void RaisePlayModeChanged(EditorPlayState state)
    {
        foreach (var handler in playModeChangedHandlers)
        {
            handler(state);
        }
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ShutdownAll();
    }

    /// <summary>
    /// Entry for a registered plugin.
    /// </summary>
    private sealed record EditorPluginEntry(IEditorPlugin Plugin, EditorPluginContext Context);
}
