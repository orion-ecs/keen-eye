using System.Numerics;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The hierarchy panel displays the entity tree structure of the current scene.
/// </summary>
public static class HierarchyPanel
{
    /// <summary>
    /// Creates the hierarchy panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="worldManager">The world manager for scene access.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        EditorWorldManager worldManager)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "HierarchyPanel", new PanelConfig(
            Width: 280,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        // Create header
        CreateHeader(editorWorld, panel, font);

        // Create tree view container (scrollable area)
        var treeContainer = WidgetFactory.CreatePanel(editorWorld, panel, "HierarchyTreeContainer", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.10f, 0.10f, 0.13f, 1f)
        ));

        ref var treeContainerRect = ref editorWorld.Get<UIRect>(treeContainer);
        treeContainerRect.WidthMode = UISizeMode.Fill;
        treeContainerRect.HeightMode = UISizeMode.Fill;

        // Create the tree view
        var treeView = WidgetFactory.CreateTreeView(editorWorld, "HierarchyTreeView", treeContainer, new TreeViewConfig(
            IndentSize: 16,
            RowHeight: 22,
            BackgroundColor: new Vector4(0.10f, 0.10f, 0.13f, 1f),
            TextColor: EditorColors.TextLight,
            FontSize: 13
        ));

        // Store references for later updates
        editorWorld.Add(panel, new HierarchyPanelState
        {
            TreeView = treeView,
            WorldManager = worldManager,
            Font = font
        });

        // Subscribe to scene events
        worldManager.SceneOpened += scene => RefreshHierarchy(editorWorld, panel);
        worldManager.SceneClosed += () => ClearHierarchy(editorWorld, panel);
        worldManager.EntitySelected += entity => HighlightEntity(editorWorld, panel, entity);

        // Subscribe to tree node selection
        editorWorld.Subscribe<UITreeNodeSelectedEvent>(e =>
        {
            // When a tree node is selected, select the corresponding entity
            if (editorWorld.Has<HierarchyNodeData>(e.Node))
            {
                ref readonly var nodeData = ref editorWorld.Get<HierarchyNodeData>(e.Node);
                worldManager.Select(nodeData.Entity);
            }
        });

        // Initial population if scene is already open
        if (worldManager.CurrentSceneWorld is not null)
        {
            RefreshHierarchy(editorWorld, panel);
        }

        return panel;
    }

    private static void CreateHeader(IWorld world, Entity panel, FontHandle font)
    {
        var header = WidgetFactory.CreatePanel(world, panel, "HierarchyHeader", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, header, "HierarchyTitle", "Hierarchy", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));
    }

    /// <summary>
    /// Refreshes the hierarchy tree to reflect the current scene state.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The hierarchy panel entity.</param>
    public static void RefreshHierarchy(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<HierarchyPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<HierarchyPanelState>(panel);
        var worldManager = state.WorldManager;
        var sceneWorld = worldManager.CurrentSceneWorld;

        if (sceneWorld is null)
        {
            ClearHierarchy(editorWorld, panel);
            return;
        }

        // Clear existing nodes
        ClearTreeNodes(editorWorld, state.TreeView);

        // Add root entities
        foreach (var entity in worldManager.GetRootEntities())
        {
            AddEntityNode(editorWorld, state.TreeView, Entity.Null, entity, worldManager, state.Font);
        }
    }

    private static void AddEntityNode(
        IWorld editorWorld,
        Entity treeView,
        Entity parentNode,
        Entity entity,
        EditorWorldManager worldManager,
        FontHandle font)
    {
        var entityName = worldManager.GetEntityName(entity);

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
        foreach (var child in worldManager.GetChildren(entity))
        {
            AddEntityNode(editorWorld, treeView, node, child, worldManager, font);
        }

        // Update the node to show expand arrow if it has children
        WidgetFactory.UpdateTreeNodeHasChildren(editorWorld, node);
    }

    internal static void ClearHierarchy(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<HierarchyPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<HierarchyPanelState>(panel);
        ClearTreeNodes(editorWorld, state.TreeView);
    }

    private static void ClearTreeNodes(IWorld editorWorld, Entity treeView)
    {
        if (!editorWorld.Has<UITreeView>(treeView))
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
                DespawnRecursive(editorWorld, child);
            }
        }

        // Reset the tree view state
        ref var treeViewDataMut = ref editorWorld.Get<UITreeView>(treeView);
        treeViewDataMut.VisibleNodeCount = 0;
        treeViewDataMut.SelectedItem = Entity.Null;
    }

    private static void DespawnRecursive(IWorld world, Entity entity)
    {
        var children = world.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(world, child);
        }
        world.Despawn(entity);
    }

    internal static void HighlightEntity(IWorld editorWorld, Entity panel, Entity sceneEntity)
    {
        if (!editorWorld.Has<HierarchyPanelState>(panel))
        {
            return;
        }

        // Note: HierarchyPanelState.TreeRoot will be used here when tree scrolling is implemented
        _ = editorWorld.Get<HierarchyPanelState>(panel);

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

/// <summary>
/// Component storing the state of the hierarchy panel.
/// </summary>
internal struct HierarchyPanelState : IComponent
{
    public Entity TreeView;
    public EditorWorldManager WorldManager;
    public FontHandle Font;
}

/// <summary>
/// Component storing the association between a tree node and a scene entity.
/// </summary>
internal struct HierarchyNodeData : IComponent
{
    public Entity Entity;
}
