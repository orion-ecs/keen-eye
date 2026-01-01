// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Context provided to editor plugins during initialization, providing access to editor services and APIs.
/// </summary>
/// <remarks>
/// <para>
/// The editor context is passed to <see cref="IEditorPlugin.Initialize"/> and provides
/// access to core editor services, extension storage, capabilities, and events.
/// </para>
/// <para>
/// Plugins access editor functionality through capabilities obtained via
/// <see cref="GetCapability{T}"/> or <see cref="TryGetCapability{T}"/>. This follows
/// the same capability-based pattern as runtime plugins (ADR-007).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Initialize(IEditorContext context)
/// {
///     // Access core services
///     var selection = context.Selection;
///
///     // Register for events
///     context.OnSelectionChanged(entities =>
///     {
///         Console.WriteLine($"Selected: {entities.Count} entities");
///     });
///
///     // Access capabilities
///     if (context.TryGetCapability&lt;IPanelCapability&gt;(out var panels))
///     {
///         panels.RegisterPanel&lt;MyPanel&gt;(new PanelDescriptor("my-panel", "My Panel"));
///     }
///
///     // Store a custom extension
///     context.SetExtension(new MyPluginState());
/// }
/// </code>
/// </example>
/// <seealso cref="IEditorPlugin"/>
/// <seealso cref="IEditorCapability"/>
public interface IEditorContext
{
    #region Core Services

    /// <summary>
    /// Gets the editor world manager for scene management.
    /// </summary>
    IEditorWorldManager Worlds { get; }

    /// <summary>
    /// Gets the selection manager.
    /// </summary>
    ISelectionManager Selection { get; }

    /// <summary>
    /// Gets the undo/redo manager.
    /// </summary>
    IUndoRedoManager UndoRedo { get; }

    /// <summary>
    /// Gets the asset database.
    /// </summary>
    IAssetDatabase Assets { get; }

    /// <summary>
    /// Gets the editor UI world.
    /// </summary>
    /// <remarks>
    /// This is the world used for the editor's own UI, not the scene being edited.
    /// </remarks>
    IWorld EditorWorld { get; }

    #endregion

    #region Extension Storage

    /// <summary>
    /// Sets an extension value that can be retrieved by other plugins.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// Extensions allow plugins to expose custom APIs to other plugins or application code.
    /// </remarks>
    void SetExtension<T>(T extension) where T : class;

    /// <summary>
    /// Gets an extension registered with the editor.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the extension is not registered.</exception>
    T GetExtension<T>() where T : class;

    /// <summary>
    /// Tries to get an extension registered with the editor.
    /// </summary>
    /// <typeparam name="T">The extension type.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found.</param>
    /// <returns>True if the extension is registered; false otherwise.</returns>
    bool TryGetExtension<T>(out T? extension) where T : class;

    /// <summary>
    /// Removes an extension from the editor.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    bool RemoveExtension<T>() where T : class;

    #endregion

    #region Capability Access

    /// <summary>
    /// Gets a capability from the editor context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>The capability implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the capability is not available.</exception>
    /// <remarks>
    /// <para>
    /// Capabilities provide access to editor extension points. Common capabilities include:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>IInspectorCapability</c> - Register property drawers</description></item>
    /// <item><description><c>IMenuCapability</c> - Add menu items</description></item>
    /// <item><description><c>IPanelCapability</c> - Register dockable panels</description></item>
    /// <item><description><c>IViewportCapability</c> - Add gizmos and overlays</description></item>
    /// <item><description><c>IToolCapability</c> - Register viewport tools</description></item>
    /// <item><description><c>IShortcutCapability</c> - Register keyboard shortcuts</description></item>
    /// <item><description><c>IAssetCapability</c> - Add asset importers</description></item>
    /// </list>
    /// <para>
    /// Use <see cref="TryGetCapability{T}"/> if the capability is optional for your plugin.
    /// </para>
    /// </remarks>
    T GetCapability<T>() where T : class, IEditorCapability;

    /// <summary>
    /// Tries to get a capability from the editor context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <param name="capability">When this method returns, contains the capability if available.</param>
    /// <returns>True if the capability is available; false otherwise.</returns>
    bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability;

    /// <summary>
    /// Checks if a capability is available in this context.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>True if the capability is available; false otherwise.</returns>
    bool HasCapability<T>() where T : class, IEditorCapability;

    #endregion

    #region Event Subscriptions

    /// <summary>
    /// Subscribes to scene opened events.
    /// </summary>
    /// <param name="handler">The handler to invoke when a scene is opened.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    EventSubscription OnSceneOpened(Action<IWorld> handler);

    /// <summary>
    /// Subscribes to scene closed events.
    /// </summary>
    /// <param name="handler">The handler to invoke when a scene is closed.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    EventSubscription OnSceneClosed(Action handler);

    /// <summary>
    /// Subscribes to selection changed events.
    /// </summary>
    /// <param name="handler">The handler to invoke when the selection changes.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler);

    /// <summary>
    /// Subscribes to play mode state changed events.
    /// </summary>
    /// <param name="handler">The handler to invoke when the play mode state changes.</param>
    /// <returns>A subscription that can be disposed to unsubscribe.</returns>
    EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler);

    #endregion
}
