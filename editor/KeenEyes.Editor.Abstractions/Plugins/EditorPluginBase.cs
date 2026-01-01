// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Base class for editor plugins providing common functionality.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides a convenient way to implement <see cref="IEditorPlugin"/>
/// with sensible defaults. Override <see cref="OnInitialize"/> and optionally
/// <see cref="OnShutdown"/> to add plugin functionality.
/// </para>
/// <para>
/// The <see cref="Context"/> property provides access to the editor context
/// after initialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class MyPlugin : EditorPluginBase
/// {
///     public override string Name => "My Plugin";
///     public override string? Description => "Adds custom panels and drawers";
///
///     protected override void OnInitialize(IEditorContext context)
///     {
///         // Access capabilities and register extensions
///         if (context.TryGetCapability&lt;IPanelCapability&gt;(out var panels))
///         {
///             panels.RegisterPanel&lt;MyPanel&gt;(new PanelDescriptor("my-panel", "My Panel"));
///         }
///     }
///
///     protected override void OnShutdown()
///     {
///         // Optional cleanup
///     }
/// }
/// </code>
/// </example>
public abstract class EditorPluginBase : IEditorPlugin
{
    /// <summary>
    /// Gets the unique name of this plugin.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the version of this plugin.
    /// </summary>
    /// <remarks>
    /// Defaults to "1.0.0". Override to specify a different version.
    /// </remarks>
    public virtual string Version => "1.0.0";

    /// <summary>
    /// Gets an optional description of this plugin.
    /// </summary>
    /// <remarks>
    /// Defaults to null. Override to provide a description.
    /// </remarks>
    public virtual string? Description => null;

    /// <summary>
    /// Gets the editor context after initialization.
    /// </summary>
    /// <remarks>
    /// This property is set during <see cref="Initialize"/> and cleared during
    /// <see cref="Shutdown"/>. Accessing it before initialization or after shutdown
    /// will return null.
    /// </remarks>
    protected IEditorContext? Context { get; private set; }

    /// <summary>
    /// Initializes the plugin with the provided editor context.
    /// </summary>
    /// <param name="context">The editor context.</param>
    public void Initialize(IEditorContext context)
    {
        Context = context;
        OnInitialize(context);
    }

    /// <summary>
    /// Shuts down the plugin.
    /// </summary>
    public void Shutdown()
    {
        OnShutdown();
        Context = null;
    }

    /// <summary>
    /// Called when the plugin is being initialized.
    /// </summary>
    /// <param name="context">The editor context providing access to editor APIs.</param>
    /// <remarks>
    /// Override this method to register panels, property drawers, gizmos,
    /// menu items, and other editor extensions.
    /// </remarks>
    protected abstract void OnInitialize(IEditorContext context);

    /// <summary>
    /// Called when the plugin is being shut down.
    /// </summary>
    /// <remarks>
    /// Override this method to perform custom cleanup. Note that resources
    /// registered through capabilities are automatically cleaned up by the
    /// editor plugin system.
    /// </remarks>
    protected virtual void OnShutdown()
    {
    }
}
