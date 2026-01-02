// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Panels;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the hierarchy panel for viewing entity structure.
/// </summary>
/// <remarks>
/// <para>
/// The hierarchy panel displays a tree view of all entities in the current scene,
/// showing parent-child relationships. Users can select entities by clicking
/// in the hierarchy, and the view syncs with the viewport selection.
/// </para>
/// </remarks>
internal sealed class HierarchyPlugin : EditorPluginBase
{
    private const string PanelId = "hierarchy";

    /// <inheritdoc />
    public override string Name => "Hierarchy";

    /// <inheritdoc />
    public override string? Description => "Entity hierarchy panel for scene structure visualization";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the hierarchy panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Hierarchy",
                Icon = "hierarchy",
                DefaultLocation = PanelDockLocation.Left,
                OpenByDefault = true,
                MinWidth = 200,
                MinHeight = 200,
                DefaultWidth = 280,
                DefaultHeight = 400,
                Category = "Scene",
                ToggleShortcut = "Ctrl+Shift+H"
            },
            () => new HierarchyPanelImpl());

        // Register shortcut for toggling the hierarchy panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "hierarchy.toggle",
                "Toggle Hierarchy",
                ShortcutCategories.View,
                "Ctrl+Shift+H",
                () =>
                {
                    if (context.TryGetCapability<IPanelCapability>(out var p) && p is not null)
                    {
                        if (p.IsPanelOpen(PanelId))
                        {
                            p.ClosePanel(PanelId);
                        }
                        else
                        {
                            p.OpenPanel(PanelId);
                        }
                    }
                });
        }
    }
}

/// <summary>
/// Implementation of the hierarchy panel as an <see cref="IEditorPanel"/>.
/// </summary>
/// <remarks>
/// This is a wrapper around the existing static <see cref="HierarchyPanel"/> functionality,
/// adapted to work with the plugin-based panel system.
/// </remarks>
internal sealed class HierarchyPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private IEditorContext? context;
    private IWorld? editorWorld;
    private EventSubscription? sceneOpenedSubscription;
    private EventSubscription? sceneClosedSubscription;
    private EventSubscription? selectionChangedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext panelContext)
    {
        context = panelContext.EditorContext;
        editorWorld = panelContext.EditorWorld;

        // Create the hierarchy panel UI using the existing static method
        // The existing HierarchyPanel.Create expects specific dependencies
        // For now, we'll create a placeholder and refactor later
        rootEntity = CreatePanelUI(panelContext);

        // Subscribe to editor events
        sceneOpenedSubscription = context.OnSceneOpened(OnSceneOpened);
        sceneClosedSubscription = context.OnSceneClosed(OnSceneClosed);
        selectionChangedSubscription = context.OnSelectionChanged(OnSelectionChanged);
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // The hierarchy panel uses UI events for updates, no per-frame logic needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        sceneOpenedSubscription?.Dispose();
        sceneClosedSubscription?.Dispose();
        selectionChangedSubscription?.Dispose();

        // Cleanup UI
        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }

    private Entity CreatePanelUI(PanelContext panelContext)
    {
        // TODO: Refactor HierarchyPanel.Create to work without static dependencies
        // For now, return a placeholder entity
        // The full refactoring would involve moving the UI creation logic here
        // and getting dependencies from the IEditorContext

        // Create a simple container for now
        return panelContext.Parent;
    }

    private void OnSceneOpened(IWorld sceneWorld)
    {
        // Refresh the hierarchy tree when a scene is opened
        RefreshHierarchy();
    }

    private void OnSceneClosed()
    {
        // Clear the hierarchy tree when the scene is closed
        ClearHierarchy();
    }

    private void OnSelectionChanged(IReadOnlyList<Entity> selectedEntities)
    {
        // Highlight the selected entities in the hierarchy
        if (selectedEntities.Count > 0)
        {
            HighlightEntity(selectedEntities[0]);
        }
    }

    private void RefreshHierarchy()
    {
        // TODO: Implement hierarchy refresh
        // This would rebuild the tree view from the current scene
    }

    private void ClearHierarchy()
    {
        // TODO: Implement hierarchy clear
        // This would remove all tree nodes
    }

    private void HighlightEntity(Entity entity)
    {
        // TODO: Implement entity highlighting
        // This would select the corresponding tree node
    }
}
