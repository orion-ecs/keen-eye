// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// Provides context for editor plugin initialization.
/// </summary>
/// <remarks>
/// <para>
/// The plugin context is passed to <see cref="IEditorPlugin.Initialize"/> and provides
/// access to editor services, extension storage, capabilities, and events.
/// </para>
/// <para>
/// Resources registered through the context (event subscriptions, etc.) are tracked
/// and automatically cleaned up when the plugin is uninstalled.
/// </para>
/// </remarks>
internal sealed class EditorPluginContext : IEditorContext
{
    private readonly EditorPluginManager manager;
    private readonly List<EventSubscription> subscriptions = [];
    private readonly Dictionary<Type, object> extensions = [];

    /// <summary>
    /// Gets the plugin this context belongs to.
    /// </summary>
    internal IEditorPlugin Plugin { get; }

    /// <summary>
    /// Gets the event subscriptions registered by this plugin.
    /// </summary>
    internal IReadOnlyList<EventSubscription> Subscriptions => subscriptions;

    /// <summary>
    /// Creates a new plugin context.
    /// </summary>
    /// <param name="manager">The plugin manager.</param>
    /// <param name="plugin">The plugin this context is for.</param>
    internal EditorPluginContext(EditorPluginManager manager, IEditorPlugin plugin)
    {
        this.manager = manager;
        Plugin = plugin;
    }

    #region Core Services

    /// <inheritdoc />
    public IEditorWorldManager Worlds => manager.Worlds;

    /// <inheritdoc />
    public ISelectionManager Selection => manager.Selection;

    /// <inheritdoc />
    public IUndoRedoManager UndoRedo => manager.UndoRedo;

    /// <inheritdoc />
    public IAssetDatabase Assets => manager.Assets;

    /// <inheritdoc />
    public IWorld EditorWorld => manager.EditorWorld;

    #endregion

    #region Extension Storage

    /// <inheritdoc />
    public void SetExtension<T>(T extension) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        extensions[typeof(T)] = extension;
    }

    /// <inheritdoc />
    public T GetExtension<T>() where T : class
    {
        if (TryGetExtension<T>(out var extension))
        {
            return extension!;
        }

        throw new InvalidOperationException(
            $"Extension of type {typeof(T).Name} is not registered.");
    }

    /// <inheritdoc />
    public bool TryGetExtension<T>(out T? extension) where T : class
    {
        // Check local extensions first
        if (extensions.TryGetValue(typeof(T), out var obj))
        {
            extension = (T)obj;
            return true;
        }

        // Check global extensions from manager
        return manager.TryGetExtension(out extension);
    }

    /// <inheritdoc />
    public bool RemoveExtension<T>() where T : class
    {
        return extensions.Remove(typeof(T));
    }

    #endregion

    #region Capability Access

    /// <inheritdoc />
    public T GetCapability<T>() where T : class, IEditorCapability
    {
        if (TryGetCapability<T>(out var capability))
        {
            return capability!;
        }

        throw new InvalidOperationException(
            $"Capability of type {typeof(T).Name} is not available.");
    }

    /// <inheritdoc />
    public bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability
    {
        return manager.TryGetCapability(out capability);
    }

    /// <inheritdoc />
    public bool HasCapability<T>() where T : class, IEditorCapability
    {
        return manager.HasCapability<T>();
    }

    #endregion

    #region Event Subscriptions

    /// <inheritdoc />
    public EventSubscription OnSceneOpened(Action<IWorld> handler)
    {
        var subscription = manager.SubscribeSceneOpened(handler);
        subscriptions.Add(subscription);
        return subscription;
    }

    /// <inheritdoc />
    public EventSubscription OnSceneClosed(Action handler)
    {
        var subscription = manager.SubscribeSceneClosed(handler);
        subscriptions.Add(subscription);
        return subscription;
    }

    /// <inheritdoc />
    public EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler)
    {
        var subscription = manager.SubscribeSelectionChanged(handler);
        subscriptions.Add(subscription);
        return subscription;
    }

    /// <inheritdoc />
    public EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler)
    {
        var subscription = manager.SubscribePlayModeChanged(handler);
        subscriptions.Add(subscription);
        return subscription;
    }

    #endregion

    /// <summary>
    /// Disposes all tracked subscriptions.
    /// </summary>
    internal void DisposeSubscriptions()
    {
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
        subscriptions.Clear();
    }
}
