// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Plugins.Security;
using KeenEyes.Logging;

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
internal sealed class EditorPluginManager : IDisposable, IEditorPluginLogger
{
    private readonly Dictionary<string, EditorPluginEntry> plugins = [];
    private readonly Dictionary<Type, IEditorCapability> capabilities = [];
    private readonly Dictionary<Type, object> globalExtensions = [];

    // Event handlers (to be wired up by EditorApplication)
    private readonly List<Action<IWorld>> sceneOpenedHandlers = [];
    private readonly List<Action> sceneClosedHandlers = [];
    private readonly List<Action<IReadOnlyList<Entity>>> selectionChangedHandlers = [];
    private readonly List<Action<EditorPlayState>> playModeChangedHandlers = [];

    // Dynamic loading infrastructure
    private readonly PluginRepository repository;
    private readonly PluginLoader loader;

    // Security infrastructure
    private SecurityConfiguration securityConfig = SecurityConfiguration.Default;
    private PermissionManager? permissionManager;

    // Logging infrastructure
    private readonly ILogProvider? logProvider;
    private const string LogCategory = "Plugin";

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
    /// Gets the log queryable for accessing editor logs.
    /// </summary>
    internal ILogQueryable? Log => logProvider as ILogQueryable;

    /// <summary>
    /// Creates a new plugin manager with the specified services.
    /// </summary>
    /// <param name="worlds">The editor world manager.</param>
    /// <param name="selection">The selection manager.</param>
    /// <param name="undoRedo">The undo/redo manager.</param>
    /// <param name="assets">The asset database.</param>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="logProvider">Optional log provider for plugin diagnostics.</param>
    internal EditorPluginManager(
        IEditorWorldManager worlds,
        ISelectionManager selection,
        IUndoRedoManager undoRedo,
        IAssetDatabase assets,
        IWorld editorWorld,
        ILogProvider? logProvider = null)
    {
        Worlds = worlds;
        Selection = selection;
        UndoRedo = undoRedo;
        Assets = assets;
        EditorWorld = editorWorld;
        this.logProvider = logProvider;

        // Initialize dynamic loading infrastructure
        repository = new PluginRepository(this);
        loader = new PluginLoader(this);
    }

    /// <summary>
    /// Gets the plugin repository for discovering available plugins.
    /// </summary>
    public PluginRepository Repository => repository;

    /// <summary>
    /// Gets the permission manager for checking and granting plugin permissions.
    /// </summary>
    /// <remarks>
    /// Returns null if permissions are not enabled in the security configuration.
    /// </remarks>
    public PermissionManager? Permissions => permissionManager;

    /// <summary>
    /// Gets the current security configuration.
    /// </summary>
    public SecurityConfiguration SecurityConfiguration => securityConfig;

    #region Security Configuration

    /// <summary>
    /// Configures security settings for the plugin manager.
    /// </summary>
    /// <param name="config">The security configuration.</param>
    /// <remarks>
    /// This method should be called before loading any plugins.
    /// If permissions are enabled, the permission manager will be initialized.
    /// </remarks>
    public void ConfigureSecurity(SecurityConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        securityConfig = config;

        if (config.EnablePermissions)
        {
            permissionManager = new PermissionManager(null, this);
            permissionManager.Load();
            LogInfo("Permission system enabled");
        }
        else
        {
            permissionManager = null;
        }
    }

    /// <summary>
    /// Gets the permissions granted to a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The granted permissions, or FullTrust if permissions are disabled.</returns>
    public PluginPermission GetPluginPermissions(string pluginId)
    {
        if (permissionManager == null)
        {
            // Permissions disabled - full trust mode
            return PluginPermission.FullTrust;
        }

        return permissionManager.GetGrantedPermissions(pluginId);
    }

    /// <summary>
    /// Grants permissions to a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permissions">The permissions to grant.</param>
    public void GrantPluginPermissions(string pluginId, PluginPermission permissions)
    {
        if (permissionManager == null)
        {
            LogWarning("Cannot grant permissions - permission system is disabled");
            return;
        }

        permissionManager.GrantPermissions(pluginId, permissions);
    }

    /// <summary>
    /// Revokes permissions from a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="permissions">The permissions to revoke.</param>
    public void RevokePluginPermissions(string pluginId, PluginPermission permissions)
    {
        if (permissionManager == null)
        {
            LogWarning("Cannot revoke permissions - permission system is disabled");
            return;
        }

        permissionManager.RevokePermissions(pluginId, permissions);
    }

    /// <summary>
    /// Saves permission grants to persistent storage.
    /// </summary>
    public void SavePermissions()
    {
        permissionManager?.Save();
    }

    #endregion

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

    #region Dynamic Plugin Loading

    /// <summary>
    /// Scans for available plugins in all configured search paths.
    /// </summary>
    /// <returns>The number of plugins discovered.</returns>
    public int DiscoverPlugins()
    {
        return repository.Scan();
    }

    /// <summary>
    /// Loads and enables a dynamically discovered plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID from the manifest.</param>
    /// <returns>True if the plugin was loaded and enabled; false otherwise.</returns>
    public bool LoadDynamicPlugin(string pluginId)
    {
        var loadedPlugin = repository.GetPlugin(pluginId);
        if (loadedPlugin == null)
        {
            LogWarning($"Plugin '{pluginId}' not found in repository");
            return false;
        }

        return LoadDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Loads and enables a dynamically discovered plugin.
    /// </summary>
    /// <param name="loadedPlugin">The plugin entry to load.</param>
    /// <returns>True if the plugin was loaded and enabled; false otherwise.</returns>
    public bool LoadDynamicPlugin(LoadedPlugin loadedPlugin)
    {
        // Load the assembly if not already loaded
        if ((loadedPlugin.State == PluginState.Discovered ||
            loadedPlugin.State == PluginState.Failed) &&
            !loader.Load(loadedPlugin))
        {
            return false;
        }

        // Enable the plugin
        return EnableDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Enables a loaded dynamic plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if the plugin was enabled; false otherwise.</returns>
    public bool EnableDynamicPlugin(string pluginId)
    {
        var loadedPlugin = repository.GetPlugin(pluginId);
        if (loadedPlugin == null)
        {
            LogWarning($"Plugin '{pluginId}' not found in repository");
            return false;
        }

        return EnableDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Enables a loaded dynamic plugin.
    /// </summary>
    /// <param name="loadedPlugin">The plugin to enable.</param>
    /// <returns>True if the plugin was enabled; false otherwise.</returns>
    public bool EnableDynamicPlugin(LoadedPlugin loadedPlugin)
    {
        if (loadedPlugin.State == PluginState.Enabled)
        {
            return true; // Already enabled
        }

        if (loadedPlugin.State != PluginState.Loaded &&
            loadedPlugin.State != PluginState.Disabled)
        {
            LogWarning($"Plugin '{loadedPlugin.Manifest.Id}' cannot be enabled (state: {loadedPlugin.State})");
            return false;
        }

        if (loadedPlugin.Instance == null)
        {
            LogError($"Plugin '{loadedPlugin.Manifest.Id}' has no instance");
            return false;
        }

        var pluginId = loadedPlugin.Manifest.Id;

        // Register plugin with permission manager and validate permissions
        if (permissionManager != null)
        {
            permissionManager.RegisterPlugin(pluginId, loadedPlugin.Manifest);

            // Grant default permissions if no grants exist
            var granted = permissionManager.GetGrantedPermissions(pluginId);
            if (granted == PluginPermission.None)
            {
                var defaultPerms = ParseDefaultPermissions();
                if (defaultPerms != PluginPermission.None)
                {
                    permissionManager.GrantPermissions(pluginId, defaultPerms);
                    LogInfo($"Granted default permissions to '{pluginId}': {defaultPerms}");
                }
            }

            // Validate required permissions
            var validation = permissionManager.ValidatePlugin(pluginId);
            if (!validation.IsValid)
            {
                loadedPlugin.State = PluginState.Failed;
                loadedPlugin.ErrorMessage = $"Missing required permissions: {validation.MissingPermissions}";
                LogError($"Plugin '{pluginId}' is missing required permissions: {validation.MissingPermissions}");
                return false;
            }
        }

        try
        {
            var context = new EditorPluginContext(this, loadedPlugin.Instance);

            // Wrap in SecurePluginContext if permissions are enabled
            IEditorContext pluginContext = permissionManager != null
                ? new SecurePluginContext(context, permissionManager, pluginId)
                : context;

            loadedPlugin.Instance.Initialize(pluginContext);
            loadedPlugin.Context = context;
            loadedPlugin.State = PluginState.Enabled;

            // Register in main plugins dictionary for compatibility with existing API
            plugins[loadedPlugin.Instance.Name] = new EditorPluginEntry(loadedPlugin.Instance, context);

            LogInfo($"Enabled plugin '{loadedPlugin.Manifest.Id}'");
            return true;
        }
        catch (PermissionDeniedException ex)
        {
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = $"Permission denied: {ex.RequiredPermission.GetDisplayName()}";
            LogError($"Plugin '{pluginId}' was denied permission: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = $"Enable failed: {ex.Message}";
            LogError($"Failed to enable plugin '{loadedPlugin.Manifest.Id}': {ex.Message}");
            return false;
        }
    }

    private PluginPermission ParseDefaultPermissions()
    {
        var result = PluginPermission.None;
        foreach (var name in securityConfig.DefaultPermissions)
        {
            if (PluginPermissionExtensions.TryParse(name, out var perm))
            {
                result |= perm;
            }
        }

        return result;
    }

    /// <summary>
    /// Disables a dynamic plugin without unloading its assembly.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if the plugin was disabled; false otherwise.</returns>
    public bool DisableDynamicPlugin(string pluginId)
    {
        var loadedPlugin = repository.GetPlugin(pluginId);
        if (loadedPlugin == null)
        {
            LogWarning($"Plugin '{pluginId}' not found in repository");
            return false;
        }

        return DisableDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Disables a dynamic plugin without unloading its assembly.
    /// </summary>
    /// <param name="loadedPlugin">The plugin to disable.</param>
    /// <returns>True if the plugin was disabled; false otherwise.</returns>
    public bool DisableDynamicPlugin(LoadedPlugin loadedPlugin)
    {
        if (loadedPlugin.State == PluginState.Disabled)
        {
            return true; // Already disabled
        }

        if (loadedPlugin.State != PluginState.Enabled)
        {
            LogWarning($"Plugin '{loadedPlugin.Manifest.Id}' cannot be disabled (state: {loadedPlugin.State})");
            return false;
        }

        if (!loadedPlugin.SupportsDisable)
        {
            LogWarning($"Plugin '{loadedPlugin.Manifest.Id}' does not support runtime disable");
            return false;
        }

        try
        {
            // Remove from main plugins dictionary
            if (loadedPlugin.Instance != null)
            {
                plugins.Remove(loadedPlugin.Instance.Name);
            }

            // Dispose context (cleans up subscriptions)
            loadedPlugin.Context?.DisposeSubscriptions();

            // Unregister from permission manager
            permissionManager?.UnregisterPlugin(loadedPlugin.Manifest.Id);

            // Shutdown the plugin
            loadedPlugin.Instance?.Shutdown();
            loadedPlugin.Context = null;
            loadedPlugin.State = PluginState.Disabled;

            LogInfo($"Disabled plugin '{loadedPlugin.Manifest.Id}'");
            return true;
        }
        catch (Exception ex)
        {
            loadedPlugin.State = PluginState.Failed;
            loadedPlugin.ErrorMessage = $"Disable failed: {ex.Message}";
            LogError($"Failed to disable plugin '{loadedPlugin.Manifest.Id}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unloads a dynamic plugin and its assembly (if hot-reload is supported).
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if the plugin was unloaded; false if restart is required.</returns>
    public bool UnloadDynamicPlugin(string pluginId)
    {
        var loadedPlugin = repository.GetPlugin(pluginId);
        if (loadedPlugin == null)
        {
            LogWarning($"Plugin '{pluginId}' not found in repository");
            return false;
        }

        return UnloadDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Unloads a dynamic plugin and its assembly (if hot-reload is supported).
    /// </summary>
    /// <param name="loadedPlugin">The plugin to unload.</param>
    /// <returns>True if the plugin was unloaded; false if restart is required.</returns>
    public bool UnloadDynamicPlugin(LoadedPlugin loadedPlugin)
    {
        // First disable if enabled
        if (loadedPlugin.State == PluginState.Enabled)
        {
            DisableDynamicPlugin(loadedPlugin);
        }

        // Attempt to unload the assembly
        return loader.Unload(loadedPlugin);
    }

    /// <summary>
    /// Reloads a dynamic plugin (unload + load).
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if the plugin was reloaded; false otherwise.</returns>
    public bool ReloadDynamicPlugin(string pluginId)
    {
        var loadedPlugin = repository.GetPlugin(pluginId);
        if (loadedPlugin == null)
        {
            LogWarning($"Plugin '{pluginId}' not found in repository");
            return false;
        }

        return ReloadDynamicPlugin(loadedPlugin);
    }

    /// <summary>
    /// Reloads a dynamic plugin (unload + load).
    /// </summary>
    /// <param name="loadedPlugin">The plugin to reload.</param>
    /// <returns>True if the plugin was reloaded; false otherwise.</returns>
    public bool ReloadDynamicPlugin(LoadedPlugin loadedPlugin)
    {
        if (!loadedPlugin.SupportsHotReload)
        {
            LogWarning($"Plugin '{loadedPlugin.Manifest.Id}' does not support hot reload");
            return false;
        }

        var wasEnabled = loadedPlugin.State == PluginState.Enabled;

        if (!UnloadDynamicPlugin(loadedPlugin))
        {
            return false;
        }

        if (!loader.Load(loadedPlugin))
        {
            return false;
        }

        if (wasEnabled)
        {
            return EnableDynamicPlugin(loadedPlugin);
        }

        return true;
    }

    /// <summary>
    /// Gets all dynamically discovered plugins.
    /// </summary>
    /// <returns>An enumerable of all discovered plugins.</returns>
    public IEnumerable<LoadedPlugin> GetDynamicPlugins()
    {
        return repository.Plugins.Values;
    }

    /// <summary>
    /// Gets a dynamically loaded plugin by ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The plugin, or null if not found.</returns>
    public LoadedPlugin? GetDynamicPlugin(string pluginId)
    {
        return repository.GetPlugin(pluginId);
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

        // Save permission grants if configured
        if (securityConfig.RememberPermissionDecisions)
        {
            permissionManager?.Save();
        }
    }

    /// <summary>
    /// Entry for a registered plugin.
    /// </summary>
    private sealed record EditorPluginEntry(IEditorPlugin Plugin, EditorPluginContext Context);

    #region IEditorPluginLogger Implementation

    /// <inheritdoc />
    void IEditorPluginLogger.LogInfo(string message)
    {
        LogInfo(message);
    }

    /// <inheritdoc />
    void IEditorPluginLogger.LogWarning(string message)
    {
        LogWarning(message);
    }

    /// <inheritdoc />
    void IEditorPluginLogger.LogError(string message)
    {
        LogError(message);
    }

    private void LogInfo(string message)
    {
        if (logProvider is not null)
        {
            logProvider.Log(LogLevel.Info, LogCategory, message, null);
        }
        else
        {
            Console.WriteLine($"[Plugin] INFO: {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (logProvider is not null)
        {
            logProvider.Log(LogLevel.Warning, LogCategory, message, null);
        }
        else
        {
            Console.WriteLine($"[Plugin] WARN: {message}");
        }
    }

    private void LogError(string message)
    {
        if (logProvider is not null)
        {
            logProvider.Log(LogLevel.Error, LogCategory, message, null);
        }
        else
        {
            Console.WriteLine($"[Plugin] ERROR: {message}");
        }
    }

    #endregion
}
