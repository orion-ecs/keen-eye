// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Panels;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

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
    private Entity treeView;
    private IEditorContext? editorContext;
    private IWorld? editorWorld;
    private FontHandle font;
    private EventSubscription? sceneOpenedSubscription;
    private EventSubscription? sceneClosedSubscription;
    private EventSubscription? selectionChangedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        editorContext = context.EditorContext;
        editorWorld = context.EditorWorld;
        font = context.Font;

        // Create the hierarchy panel UI
        rootEntity = CreatePanelUI(context);

        // Subscribe to editor events
        sceneOpenedSubscription = editorContext.OnSceneOpened(OnSceneOpened);
        sceneClosedSubscription = editorContext.OnSceneClosed(OnSceneClosed);
        selectionChangedSubscription = editorContext.OnSelectionChanged(OnSelectionChanged);

        // Initial population if scene is already open
        if (editorContext.Worlds.CurrentSceneWorld is not null)
        {
            RefreshHierarchy();
        }
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

    private Entity CreatePanelUI(PanelContext context)
    {
        var world = context.EditorWorld;

        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(world, context.Parent, "HierarchyPluginPanel", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref world.Get<UIRect>(panel);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.Fill;

        // Create header
        var header = WidgetFactory.CreatePanel(world, panel, "HierarchyPluginHeader", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, header, "HierarchyPluginTitle", "Hierarchy", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Create tree view container
        var treeContainer = WidgetFactory.CreatePanel(world, panel, "HierarchyPluginTreeContainer", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.10f, 0.10f, 0.13f, 1f)
        ));

        ref var treeContainerRect = ref world.Get<UIRect>(treeContainer);
        treeContainerRect.WidthMode = UISizeMode.Fill;
        treeContainerRect.HeightMode = UISizeMode.Fill;

        // Create the tree view
        treeView = WidgetFactory.CreateTreeView(world, "HierarchyPluginTreeView", treeContainer, new TreeViewConfig(
            IndentSize: 16,
            RowHeight: 22,
            BackgroundColor: new Vector4(0.10f, 0.10f, 0.13f, 1f),
            TextColor: EditorColors.TextLight,
            FontSize: 13
        ));

        // Subscribe to tree node selection
        world.Subscribe<UITreeNodeSelectedEvent>(e =>
        {
            // When a tree node is selected, select the corresponding entity
            if (world.Has<HierarchyNodeData>(e.Node))
            {
                ref readonly var nodeData = ref world.Get<HierarchyNodeData>(e.Node);
                editorContext?.Selection.Select(nodeData.Entity);
            }
        });

        return panel;
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
        if (editorWorld is null || editorContext is null || !treeView.IsValid)
        {
            return;
        }

        var worlds = editorContext.Worlds;
        if (worlds.CurrentSceneWorld is null)
        {
            ClearHierarchy();
            return;
        }

        // Clear existing nodes
        ClearTreeNodes();

        // Add root entities
        foreach (var entity in worlds.GetRootEntities())
        {
            AddEntityNode(Entity.Null, entity);
        }
    }

    private void ClearHierarchy()
    {
        ClearTreeNodes();
    }

    private void ClearTreeNodes()
    {
        if (editorWorld is null || !treeView.IsValid || !editorWorld.Has<UITreeView>(treeView))
        {
            return;
        }

        ref readonly var treeViewData = ref editorWorld.Get<UITreeView>(treeView);

        // Remove all child nodes from the node container
        if (treeViewData.NodeContainer.IsValid)
        {
            var children = editorWorld.GetChildren(treeViewData.NodeContainer).ToList();
            foreach (var child in children)
            {
                DespawnRecursive(child);
            }
        }

        // Reset the tree view state
        ref var treeViewDataMut = ref editorWorld.Get<UITreeView>(treeView);
        treeViewDataMut.VisibleNodeCount = 0;
        treeViewDataMut.SelectedItem = Entity.Null;
    }

    private void AddEntityNode(Entity parentNode, Entity entity)
    {
        if (editorWorld is null || editorContext is null)
        {
            return;
        }

        var entityName = editorContext.Worlds.GetEntityName(entity);

        var node = WidgetFactory.CreateTreeNode(
            editorWorld,
            treeView,
            parentNode,
            entityName,
            font,
            new TreeNodeConfig(IsExpanded: true));

        // Store reference to the scene entity
        editorWorld.Add(node, new HierarchyNodeData { Entity = entity });

        // Add children recursively
        foreach (var child in editorContext.Worlds.GetChildren(entity))
        {
            AddEntityNode(node, child);
        }

        // Update the node to show expand arrow if it has children
        WidgetFactory.UpdateTreeNodeHasChildren(editorWorld, node);
    }

    private void DespawnRecursive(Entity entity)
    {
        if (editorWorld is null)
        {
            return;
        }

        var children = editorWorld.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(child);
        }
        editorWorld.Despawn(entity);
    }

    private void HighlightEntity(Entity sceneEntity)
    {
        if (editorWorld is null || !treeView.IsValid)
        {
            return;
        }

        // Find the tree node corresponding to this entity
        foreach (var nodeEntity in editorWorld.Query<HierarchyNodeData>())
        {
            ref readonly var nodeData = ref editorWorld.Get<HierarchyNodeData>(nodeEntity);
            if (nodeData.Entity == sceneEntity)
            {
                WidgetFactory.SelectTreeNode(editorWorld, nodeEntity);
                break;
            }
        }
    }
}
