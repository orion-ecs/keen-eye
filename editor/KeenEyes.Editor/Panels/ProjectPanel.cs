using System.Numerics;

using KeenEyes.Editor.Application;
using KeenEyes.Editor.Assets;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The project panel displays the project's asset files in a tree view.
/// </summary>
public static class ProjectPanel
{
    /// <summary>
    /// Creates the project panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="assetDatabase">The asset database to display.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        AssetDatabase assetDatabase)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "ProjectPanel", new PanelConfig(
            Width: 250,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(panel);
        panelRect.HeightMode = UISizeMode.Fill;

        // Create toolbar
        var toolbar = CreateToolbar(editorWorld, panel, font);

        // Create tree view for assets
        var treeView = WidgetFactory.CreateTreeView(editorWorld, panel, new TreeViewConfig(
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f),
            IndentSize: 16,
            RowHeight: 22,
            FontSize: 12,
            TextColor: EditorColors.TextLight
        ));

        ref var treeRect = ref editorWorld.Get<UIRect>(treeView);
        treeRect.WidthMode = UISizeMode.Fill;
        treeRect.HeightMode = UISizeMode.Fill;

        // Store state
        var state = new ProjectPanelState
        {
            TreeView = treeView,
            Toolbar = toolbar,
            AssetDatabase = assetDatabase,
            Font = font,
            SearchText = null
        };

        editorWorld.Add(panel, state);

        // Subscribe to asset database events
        assetDatabase.AssetAdded += (_, args) => OnAssetAdded(editorWorld, panel, args.Asset);
        assetDatabase.AssetRemoved += (_, args) => OnAssetRemoved(editorWorld, panel, args.Asset);
        assetDatabase.AssetModified += (_, args) => OnAssetModified(editorWorld, panel, args.Asset);

        // Populate with existing assets
        RefreshAssetTree(editorWorld, panel);

        // Handle double-click for opening assets
        editorWorld.Subscribe<UITreeNodeDoubleClickedEvent>(e => OnTreeNodeDoubleClick(editorWorld, panel, e));

        return panel;
    }

    private static Entity CreateToolbar(
        IWorld editorWorld,
        Entity panel,
        FontHandle font)
    {
        var toolbar = WidgetFactory.CreatePanel(editorWorld, panel, "ProjectToolbar", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.Symmetric(8, 4),
            Spacing: 8
        ));

        ref var toolbarRect = ref editorWorld.Get<UIRect>(toolbar);
        toolbarRect.WidthMode = UISizeMode.Fill;

        // Title
        WidgetFactory.CreateLabel(editorWorld, toolbar, "ProjectTitle", "Project", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Store toolbar reference
        editorWorld.Add(toolbar, new ProjectToolbarState());

        return toolbar;
    }

    private static void OnAssetAdded(IWorld editorWorld, Entity panel, AssetEntry asset)
    {
        // Refresh the entire tree for now (could be optimized to add single node)
        RefreshAssetTree(editorWorld, panel);
    }

    private static void OnAssetRemoved(IWorld editorWorld, Entity panel, AssetEntry asset)
    {
        // Refresh the entire tree for now (could be optimized to remove single node)
        RefreshAssetTree(editorWorld, panel);
    }

    private static void OnAssetModified(IWorld editorWorld, Entity panel, AssetEntry asset)
    {
        // Asset content changed but path is same, no tree update needed
    }

    private static void OnTreeNodeDoubleClick(IWorld editorWorld, Entity panel, UITreeNodeDoubleClickedEvent e)
    {
        if (!editorWorld.Has<ProjectPanelState>(panel))
        {
            return;
        }

        // Check if clicked node has asset data
        if (editorWorld.Has<ProjectAssetNodeTag>(e.Node))
        {
            ref readonly var nodeTag = ref editorWorld.Get<ProjectAssetNodeTag>(e.Node);

            // Raise event for asset opening
            editorWorld.Send(new ProjectAssetOpenRequestedEvent(nodeTag.RelativePath, nodeTag.AssetType));
        }
    }

    /// <summary>
    /// Refreshes the asset tree display.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The project panel entity.</param>
    public static void RefreshAssetTree(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<ProjectPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ProjectPanelState>(panel);

        // Clear existing tree nodes
        ClearTreeNodes(editorWorld, state.TreeView);

        // Build folder structure
        var folderTree = BuildFolderTree(state.AssetDatabase.AllAssets, state.SearchText);

        // Create tree nodes
        CreateFolderNodes(editorWorld, state.TreeView, Entity.Null, state.Font, folderTree, "");
    }

    private static void ClearTreeNodes(IWorld editorWorld, Entity treeView)
    {
        if (!editorWorld.Has<UITreeView>(treeView))
        {
            return;
        }

        ref readonly var treeViewData = ref editorWorld.Get<UITreeView>(treeView);

        if (treeViewData.NodeContainer.IsValid)
        {
            var children = editorWorld.GetChildren(treeViewData.NodeContainer).ToList();
            foreach (var child in children)
            {
                DespawnRecursive(editorWorld, child);
            }
        }

        // Reset visible node count
        ref var treeViewMut = ref editorWorld.Get<UITreeView>(treeView);
        treeViewMut.VisibleNodeCount = 0;
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

    private static FolderNode BuildFolderTree(IEnumerable<AssetEntry> assets, string? searchText)
    {
        var root = new FolderNode("Assets");

        foreach (var asset in assets)
        {
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchText) &&
                !asset.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                !asset.RelativePath.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

#pragma warning disable S3878 // Arrays should not be created for params parameters - S3220 requires explicit array to disambiguate overload
            var pathParts = asset.RelativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
#pragma warning restore S3878

            var currentFolder = root;
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                var folderName = pathParts[i];
                if (!currentFolder.SubFolders.TryGetValue(folderName, out var subFolder))
                {
                    subFolder = new FolderNode(folderName);
                    currentFolder.SubFolders[folderName] = subFolder;
                }
                currentFolder = subFolder;
            }

            currentFolder.Assets.Add(asset);
        }

        return root;
    }

    private static void CreateFolderNodes(
        IWorld editorWorld,
        Entity treeView,
        Entity parentNode,
        FontHandle font,
        FolderNode folder,
        string pathPrefix)
    {
        // Create folder nodes first (sorted alphabetically)
        foreach (var subFolder in folder.SubFolders.OrderBy(kv => kv.Key))
        {
            var folderPath = string.IsNullOrEmpty(pathPrefix)
                ? subFolder.Key
                : Path.Combine(pathPrefix, subFolder.Key);

            var folderNode = WidgetFactory.CreateTreeNode(
                editorWorld,
                treeView,
                parentNode,
                subFolder.Key,
                font,
                new TreeNodeConfig(IsExpanded: true));

            // Add folder icon (text-based)
            AddIconToNode(editorWorld, folderNode, GetFolderIcon());

            // Recursively create child nodes
            CreateFolderNodes(editorWorld, treeView, folderNode, font, subFolder.Value, folderPath);

            // Update has children state
            WidgetFactory.UpdateTreeNodeHasChildren(editorWorld, folderNode);
        }

        // Create asset nodes (sorted alphabetically)
        foreach (var asset in folder.Assets.OrderBy(a => a.Name))
        {
            var assetNode = WidgetFactory.CreateTreeNode(
                editorWorld,
                treeView,
                parentNode,
                $"{asset.Name}{asset.Extension}",
                font);

            // Add asset icon (text-based)
            AddIconToNode(editorWorld, assetNode, GetAssetTypeIcon(asset.Type));

            // Tag node with asset data for double-click handling
            editorWorld.Add(assetNode, new ProjectAssetNodeTag
            {
                RelativePath = asset.RelativePath,
                AssetType = asset.Type
            });
        }
    }

    private static void AddIconToNode(IWorld editorWorld, Entity node, string icon)
    {
        // Find the label and prepend icon
        var children = editorWorld.GetChildren(node);
        foreach (var child in children)
        {
            if (editorWorld.Has<UIText>(child))
            {
                ref var text = ref editorWorld.Get<UIText>(child);
                if (!text.Content.StartsWith(icon))
                {
                    text.Content = $"{icon} {text.Content}";
                }
                break;
            }
        }
    }

    private const string FolderIcon = "\uD83D\uDCC1"; // Folder emoji
    private static string GetFolderIcon() => FolderIcon;

    private static string GetAssetTypeIcon(AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Scene => "\uD83C\uDFAC",      // Clapper board (scene)
            AssetType.Prefab => "\uD83D\uDCE6",     // Package (prefab)
            AssetType.WorldConfig => "\uD83C\uDF0D", // Globe (world)
            AssetType.Texture => "\uD83D\uDDBC",    // Frame/picture
            AssetType.Shader => "\u2728",           // Sparkles (shader)
            AssetType.Audio => "\uD83D\uDD0A",      // Speaker (audio)
            AssetType.Script => "\uD83D\uDCDC",     // Scroll (script)
            AssetType.Data => "\uD83D\uDCC4",       // Document (data)
            _ => "\uD83D\uDCC4"                     // Document (unknown)
        };
    }

    /// <summary>
    /// Sets the search text filter for the project panel.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The project panel entity.</param>
    /// <param name="searchText">The text to search for, or null to clear search.</param>
    public static void SetSearchText(IWorld editorWorld, Entity panel, string? searchText)
    {
        if (!editorWorld.Has<ProjectPanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<ProjectPanelState>(panel);
        state.SearchText = searchText;

        RefreshAssetTree(editorWorld, panel);
    }

    /// <summary>
    /// Helper class for building folder hierarchy.
    /// </summary>
    private sealed class FolderNode
    {
        public string Name { get; }
        public Dictionary<string, FolderNode> SubFolders { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<AssetEntry> Assets { get; } = [];

        public FolderNode(string name)
        {
            Name = name;
        }
    }
}

/// <summary>
/// Component storing the state of the project panel.
/// </summary>
internal struct ProjectPanelState : IComponent
{
    public Entity TreeView;
    public Entity Toolbar;
    public AssetDatabase AssetDatabase;
    public FontHandle Font;
    public string? SearchText;
}

/// <summary>
/// Component storing toolbar element references.
/// </summary>
internal struct ProjectToolbarState : IComponent;

/// <summary>
/// Tag component to identify asset tree nodes with their asset data.
/// </summary>
internal struct ProjectAssetNodeTag : IComponent
{
    public string RelativePath;
    public AssetType AssetType;
}

/// <summary>
/// Event raised when an asset is double-clicked for opening.
/// </summary>
/// <param name="RelativePath">The relative path of the asset.</param>
/// <param name="AssetType">The type of asset.</param>
public readonly record struct ProjectAssetOpenRequestedEvent(string RelativePath, AssetType AssetType);
