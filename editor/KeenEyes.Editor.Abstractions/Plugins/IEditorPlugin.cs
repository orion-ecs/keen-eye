// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Interface for editor plugins that provide modular functionality.
/// </summary>
/// <remarks>
/// <para>
/// Editor plugins are separate from world plugins (<see cref="IWorldPlugin"/>) and operate on
/// the editor itself rather than the ECS world. They can register panels, property drawers,
/// gizmos, menu items, shortcuts, and other editor extensions.
/// </para>
/// <para>
/// Plugin lifecycle:
/// <list type="number">
/// <item><description>Plugin is instantiated by the editor</description></item>
/// <item><description><see cref="Initialize"/> is called with the editor context</description></item>
/// <item><description>Plugin registers its extensions via capabilities</description></item>
/// <item><description>Plugin remains active until editor shutdown</description></item>
/// <item><description><see cref="Shutdown"/> is called to clean up resources</description></item>
/// </list>
/// </para>
/// <para>
/// Plugins access editor functionality through capabilities obtained from <see cref="IEditorContext"/>.
/// This follows the same capability-based pattern as runtime plugins (ADR-007).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class MyPlugin : IEditorPlugin
/// {
///     public string Name => "My Plugin";
///     public string Version => "1.0.0";
///     public string? Description => "Adds custom functionality to the editor";
///
///     public void Initialize(IEditorContext context)
///     {
///         if (context.TryGetCapability&lt;IPanelCapability&gt;(out var panels))
///         {
///             panels.RegisterPanel&lt;MyPanel&gt;(new PanelDescriptor(
///                 Id: "my-panel",
///                 Title: "My Panel",
///                 DefaultPosition: DockPosition.Right));
///         }
///     }
///
///     public void Shutdown()
///     {
///         // Cleanup handled by capability system
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IEditorContext"/>
/// <seealso cref="IEditorCapability"/>
/// <seealso cref="EditorPluginBase"/>
public interface IEditorPlugin
{
    /// <summary>
    /// Gets the unique name of this plugin.
    /// </summary>
    /// <remarks>
    /// The name should be unique among all installed plugins and is used
    /// for identification and logging purposes.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the version of this plugin.
    /// </summary>
    /// <remarks>
    /// Recommended format is semantic versioning (e.g., "1.0.0").
    /// </remarks>
    string Version { get; }

    /// <summary>
    /// Gets an optional description of this plugin.
    /// </summary>
    /// <remarks>
    /// The description may be displayed in plugin management UI.
    /// </remarks>
    string? Description { get; }

    /// <summary>
    /// Called when the plugin is being initialized.
    /// </summary>
    /// <param name="context">The editor context providing access to editor APIs.</param>
    /// <remarks>
    /// <para>
    /// Use this method to register panels, property drawers, gizmos, menu items,
    /// and other editor extensions. Access capabilities through the context:
    /// </para>
    /// <code>
    /// var inspector = context.GetCapability&lt;IInspectorCapability&gt;();
    /// inspector.RegisterPropertyDrawer&lt;Color&gt;(new ColorDrawer());
    /// </code>
    /// <para>
    /// Resources registered during initialization are tracked and will be
    /// automatically cleaned up when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    void Initialize(IEditorContext context);

    /// <summary>
    /// Called when the plugin is being shut down.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Perform any custom cleanup here. Note that resources registered through
    /// capabilities are automatically cleaned up by the editor plugin system.
    /// </para>
    /// <para>
    /// This method is called when the editor is closing or when the plugin
    /// is explicitly uninstalled.
    /// </para>
    /// </remarks>
    void Shutdown();
}
