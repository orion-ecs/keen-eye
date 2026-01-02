// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Interface for plugins that can preserve state across hot reloads.
/// </summary>
/// <remarks>
/// <para>
/// When a plugin implementing this interface is hot-reloaded:
/// </para>
/// <list type="number">
/// <item><see cref="SaveState"/> is called before the plugin is unloaded</item>
/// <item>The plugin's assembly is unloaded and reloaded</item>
/// <item><see cref="IEditorPlugin.Initialize"/> is called on the new instance</item>
/// <item><see cref="RestoreState"/> is called with the previously saved state</item>
/// </list>
/// <para>
/// The state object returned by <see cref="SaveState"/> should contain only serializable
/// data types that do not reference types from the plugin's assembly. Using types from
/// the plugin will prevent proper restoration after reload.
/// </para>
/// <para>
/// Recommended state object types:
/// </para>
/// <list type="bullet">
/// <item>Primitive types (int, float, string, etc.)</item>
/// <item>Collections of primitives</item>
/// <item>Simple record types defined in the host (KeenEyes.Editor.Abstractions)</item>
/// <item>JSON-serializable DTOs</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class MyPlugin : EditorPluginBase, IStatefulPlugin
/// {
///     private MySettings settings = new();
///
///     public object? SaveState()
///     {
///         // Return a simple dictionary, not a plugin-defined type
///         return new Dictionary&lt;string, object&gt;
///         {
///             ["ShowOverlay"] = settings.ShowOverlay,
///             ["SelectedTab"] = settings.SelectedTabIndex
///         };
///     }
///
///     public void RestoreState(object? state)
///     {
///         if (state is Dictionary&lt;string, object&gt; dict)
///         {
///             settings.ShowOverlay = dict.GetValueOrDefault("ShowOverlay") as bool? ?? true;
///             settings.SelectedTabIndex = dict.GetValueOrDefault("SelectedTab") as int? ?? 0;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IStatefulPlugin : IEditorPlugin
{
    /// <summary>
    /// Saves plugin state before hot reload.
    /// </summary>
    /// <returns>
    /// A serializable state object that can be passed to <see cref="RestoreState"/>
    /// after reload, or <c>null</c> if there is no state to preserve.
    /// </returns>
    /// <remarks>
    /// This method is called before <see cref="IEditorPlugin.Shutdown"/> during hot reload.
    /// The returned object should not reference any types from the plugin's assembly.
    /// </remarks>
    object? SaveState();

    /// <summary>
    /// Restores plugin state after hot reload.
    /// </summary>
    /// <param name="state">
    /// The state object previously returned by <see cref="SaveState"/>,
    /// or <c>null</c> if no state was saved.
    /// </param>
    /// <remarks>
    /// This method is called after <see cref="IEditorPlugin.Initialize"/> during hot reload.
    /// </remarks>
    void RestoreState(object? state);
}
