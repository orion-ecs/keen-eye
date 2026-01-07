// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Editor plugin for navigation mesh baking and visualization.
/// </summary>
/// <remarks>
/// <para>
/// This plugin adds navigation-related tooling to the editor:
/// - Navigation settings panel for agent configuration and baking
/// - NavMesh visualizer for debug rendering in the viewport
/// - Menu items for navigation commands
/// - Keyboard shortcuts for common operations
/// </para>
/// <para>
/// The plugin integrates with the DotRecast navigation system to provide
/// a complete workflow for creating and visualizing navigation meshes.
/// </para>
/// </remarks>
public sealed class NavigationEditorPlugin : EditorPluginBase
{
    private NavMeshVisualizer? visualizer;

    /// <inheritdoc/>
    public override string Name => "Navigation";

    /// <inheritdoc/>
    public override string Description => "Navigation mesh baking and visualization tools";

    /// <inheritdoc/>
    protected override void OnInitialize(IEditorContext context)
    {
        // Register the navigation panel
        if (context.TryGetCapability<IPanelCapability>(out var panels) && panels != null)
        {
            panels.RegisterPanel<NavigationPanel>(new PanelDescriptor
            {
                Id = "navigation",
                Title = "Navigation",
                Icon = "navigation",
                DefaultLocation = PanelDockLocation.Right,
                OpenByDefault = false,
                MinWidth = 280,
                DefaultWidth = 320,
                DefaultHeight = 500,
                Category = "AI",
                ToggleShortcut = "Ctrl+Shift+N"
            });
        }

        // Register the navmesh visualizer
        if (context.TryGetCapability<IViewportCapability>(out var viewport) && viewport != null)
        {
            visualizer = new NavMeshVisualizer();
            viewport.AddGizmoRenderer(visualizer);
        }

        // Register menu items
        if (context.TryGetCapability<IMenuCapability>(out var menu) && menu != null)
        {
            RegisterMenuItems(menu);
        }

        // Register keyboard shortcuts
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts != null)
        {
            RegisterShortcuts(shortcuts);
        }
    }

    /// <inheritdoc/>
    protected override void OnShutdown()
    {
        // Remove the visualizer
        if (Context != null && Context.TryGetCapability<IViewportCapability>(out var viewport) && viewport != null && visualizer != null)
        {
            viewport.RemoveGizmoRenderer(visualizer);
        }
    }

    private void RegisterMenuItems(IMenuCapability menu)
    {
        // Window menu item to open navigation panel
        menu.AddMenuItem(
            "Window/AI/Navigation",
            new EditorCommand
            {
                Id = "window.navigation",
                DisplayName = "Navigation",
                Execute = OpenNavigationPanel
            });

        // Navigation menu for baking
        menu.AddMenuItem(
            "Navigation/Bake NavMesh",
            new EditorCommand
            {
                Id = "navigation.bake",
                DisplayName = "Bake NavMesh",
                Execute = BakeNavMesh,
                CanExecute = CanBakeNavMesh,
                Shortcut = "Ctrl+Shift+B"
            });

        menu.AddMenuItem(
            "Navigation/Clear NavMesh",
            new EditorCommand
            {
                Id = "navigation.clear",
                DisplayName = "Clear NavMesh",
                Execute = ClearNavMesh
            });

        menu.AddMenuItem(
            "Navigation/Show NavMesh",
            new EditorCommand
            {
                Id = "navigation.show",
                DisplayName = "Show NavMesh",
                Execute = ToggleVisualization
            });
    }

    private void RegisterShortcuts(IShortcutCapability shortcuts)
    {
        shortcuts.RegisterShortcut(
            "navigation.bake",
            "Bake NavMesh",
            ShortcutCategories.Navigation,
            "Ctrl+Shift+B",
            BakeNavMesh,
            CanBakeNavMesh);

        shortcuts.RegisterShortcut(
            "navigation.toggle-visualization",
            "Toggle NavMesh Visualization",
            ShortcutCategories.Navigation,
            "Ctrl+Shift+V",
            ToggleVisualization,
            () => visualizer != null);
    }

    private void OpenNavigationPanel()
    {
        if (Context?.TryGetCapability<IPanelCapability>(out var panels) == true && panels != null)
        {
            panels.OpenPanel("navigation");
        }
    }

    private void BakeNavMesh()
    {
        // Get the scene world and trigger a bake
        // In actual implementation, this would get the current scene world
        // and execute through the panel
        OpenNavigationPanel();
    }

    // Navigation mesh baking is always enabled - actual scene validation happens during bake
    private const bool CanBakeNavMeshResult = true;
    private static bool CanBakeNavMesh() => CanBakeNavMeshResult;

    private void ClearNavMesh()
    {
        visualizer?.SetNavMesh(null);
    }

    private void ToggleVisualization()
    {
        if (visualizer != null)
        {
            visualizer.IsEnabled = !visualizer.IsEnabled;
        }
    }

    /// <summary>
    /// Gets the navmesh visualizer.
    /// </summary>
    internal NavMeshVisualizer? Visualizer => visualizer;
}
