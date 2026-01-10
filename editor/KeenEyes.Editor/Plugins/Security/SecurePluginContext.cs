// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// A permission-aware wrapper around <see cref="EditorPluginContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This context checks permissions before allowing access to capabilities and services.
/// It is used when the permission system is enabled in the security configuration.
/// </para>
/// <para>
/// Access to services and capabilities requires appropriate permissions to be granted.
/// If a plugin attempts to access a capability without the required permission,
/// a <see cref="PermissionDeniedException"/> is thrown.
/// </para>
/// </remarks>
internal sealed class SecurePluginContext : IEditorContext
{
    private readonly EditorPluginContext inner;
    private readonly PermissionManager permissionManager;
    private readonly string pluginId;

    // Maps capability types to required permissions
    private static readonly Dictionary<Type, PluginPermission> CapabilityPermissions = new()
    {
        // Menu capability
        [typeof(IMenuCapability)] = PluginPermission.MenuAccess,

        // Shortcut capability
        [typeof(IShortcutCapability)] = PluginPermission.ShortcutAccess,

        // Panel capability
        [typeof(IPanelCapability)] = PluginPermission.PanelAccess,
        [typeof(IExtendedPanelCapability)] = PluginPermission.PanelAccess,

        // Viewport capability
        [typeof(IViewportCapability)] = PluginPermission.ViewportAccess,

        // Tool capability
        [typeof(IToolCapability)] = PluginPermission.ViewportAccess,

        // Inspector capability
        [typeof(IInspectorCapability)] = PluginPermission.InspectorAccess,

        // Asset capability
        [typeof(IAssetCapability)] = PluginPermission.AssetDatabaseAccess
    };

    /// <summary>
    /// Creates a new secure plugin context.
    /// </summary>
    /// <param name="inner">The wrapped context.</param>
    /// <param name="permissionManager">The permission manager.</param>
    /// <param name="pluginId">The plugin ID.</param>
    internal SecurePluginContext(
        EditorPluginContext inner,
        PermissionManager permissionManager,
        string pluginId)
    {
        this.inner = inner;
        this.permissionManager = permissionManager;
        this.pluginId = pluginId;
    }

    /// <summary>
    /// Gets the inner context for internal use.
    /// </summary>
    internal EditorPluginContext Inner => inner;

    #region Core Services

    /// <inheritdoc />
    public IEditorWorldManager Worlds
    {
        get
        {
            // World manager doesn't require special permissions
            return inner.Worlds;
        }
    }

    /// <inheritdoc />
    public ISelectionManager Selection
    {
        get
        {
            permissionManager.DemandPermission(pluginId, PluginPermission.SelectionAccess, typeof(ISelectionManager));
            return inner.Selection;
        }
    }

    /// <inheritdoc />
    public IUndoRedoManager UndoRedo
    {
        get
        {
            permissionManager.DemandPermission(pluginId, PluginPermission.UndoAccess, typeof(IUndoRedoManager));
            return inner.UndoRedo;
        }
    }

    /// <inheritdoc />
    public IAssetDatabase Assets
    {
        get
        {
            permissionManager.DemandPermission(pluginId, PluginPermission.AssetDatabaseAccess, typeof(IAssetDatabase));
            return inner.Assets;
        }
    }

    /// <inheritdoc />
    public IWorld EditorWorld
    {
        get
        {
            // Editor world access doesn't require special permissions
            return inner.EditorWorld;
        }
    }

    /// <inheritdoc />
    public ILogQueryable? Log
    {
        get
        {
            // Log access doesn't require special permissions
            return inner.Log;
        }
    }

    #endregion

    #region Extension Storage

    /// <inheritdoc />
    public void SetExtension<T>(T extension) where T : class
    {
        // Extension storage doesn't require permissions
        inner.SetExtension(extension);
    }

    /// <inheritdoc />
    public T GetExtension<T>() where T : class
    {
        return inner.GetExtension<T>();
    }

    /// <inheritdoc />
    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        return inner.TryGetExtension(out extension);
    }

    /// <inheritdoc />
    public bool RemoveExtension<T>() where T : class
    {
        return inner.RemoveExtension<T>();
    }

    #endregion

    #region Capability Access

    /// <inheritdoc />
    public T GetCapability<T>() where T : class, IEditorCapability
    {
        DemandCapabilityPermission<T>();
        return inner.GetCapability<T>();
    }

    /// <inheritdoc />
    public bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability
    {
        // Check permission first
        if (!HasCapabilityPermission<T>())
        {
            capability = default;
            return false;
        }

        return inner.TryGetCapability(out capability);
    }

    /// <inheritdoc />
    public bool HasCapability<T>() where T : class, IEditorCapability
    {
        // Return false if permission not granted
        if (!HasCapabilityPermission<T>())
        {
            return false;
        }

        return inner.HasCapability<T>();
    }

    #endregion

    #region Event Subscriptions

    /// <inheritdoc />
    public EventSubscription OnSceneOpened(Action<IWorld> handler)
    {
        // Scene events don't require special permissions
        return inner.OnSceneOpened(handler);
    }

    /// <inheritdoc />
    public EventSubscription OnSceneClosed(Action handler)
    {
        return inner.OnSceneClosed(handler);
    }

    /// <inheritdoc />
    public EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler)
    {
        permissionManager.DemandPermission(pluginId, PluginPermission.SelectionAccess);
        return inner.OnSelectionChanged(handler);
    }

    /// <inheritdoc />
    public EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler)
    {
        // Play mode events don't require special permissions
        return inner.OnPlayModeChanged(handler);
    }

    #endregion

    #region Permission Helpers

    private void DemandCapabilityPermission<T>() where T : class, IEditorCapability
    {
        var requiredPermission = GetRequiredPermission<T>();

        if (requiredPermission != PluginPermission.None)
        {
            permissionManager.DemandPermission(pluginId, requiredPermission, typeof(T));
        }
    }

    private bool HasCapabilityPermission<T>() where T : class, IEditorCapability
    {
        var requiredPermission = GetRequiredPermission<T>();

        if (requiredPermission == PluginPermission.None)
        {
            return true;
        }

        return permissionManager.HasPermission(pluginId, requiredPermission);
    }

    private static PluginPermission GetRequiredPermission<T>() where T : class, IEditorCapability
    {
        // Check direct type first
        if (CapabilityPermissions.TryGetValue(typeof(T), out var permission))
        {
            return permission;
        }

        // Check implemented interfaces
        foreach (var iface in typeof(T).GetInterfaces())
        {
            if (CapabilityPermissions.TryGetValue(iface, out permission))
            {
                return permission;
            }
        }

        // Unknown capability - no special permission required
        return PluginPermission.None;
    }

    #endregion

    #region Resource Tracking Passthrough

    /// <summary>
    /// Disposes all tracked subscriptions.
    /// </summary>
    internal void DisposeSubscriptions()
    {
        inner.DisposeSubscriptions();
    }

    /// <summary>
    /// Gets counts of tracked resources.
    /// </summary>
    internal IReadOnlyDictionary<string, int> GetTrackedResourceCounts()
    {
        return inner.GetTrackedResourceCounts();
    }

    /// <summary>
    /// Gets descriptions of live resources.
    /// </summary>
    internal IReadOnlyList<string> GetLiveResourceDescriptions()
    {
        return inner.GetLiveResourceDescriptions();
    }

    #endregion
}
