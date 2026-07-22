// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Animation;
using KeenEyes.Editor.Navigation;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Installs the editor's built-in gizmo plugins.
/// </summary>
/// <remarks>
/// <para>
/// These plugins register debug gizmo renderers through <c>IViewportCapability</c>:
/// <list type="bullet">
///   <item><description><see cref="NavigationEditorPlugin"/> - navigation mesh visualization.</description></item>
///   <item><description><see cref="AnimationEditorPlugin"/> - skeleton and IK chain visualization.</description></item>
/// </list>
/// </para>
/// <para>
/// The editor installs these unconditionally at startup and relies on
/// <see cref="EditorPluginManager.ShutdownAll"/> for uninstall symmetry, which removes
/// each plugin's renderers from the viewport capability.
/// </para>
/// </remarks>
internal static class BuiltInGizmoPlugins
{
    /// <summary>
    /// Installs every built-in gizmo plugin into the supplied plugin manager.
    /// </summary>
    /// <param name="manager">The plugin manager that hosts the built-in plugins.</param>
    internal static void Install(EditorPluginManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        manager.InstallPlugin(new NavigationEditorPlugin());
        manager.InstallPlugin(new AnimationEditorPlugin());
    }
}
