// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the project panel for asset browsing.
/// </summary>
/// <remarks>
/// <para>
/// The project panel displays the project's asset folder structure and allows
/// browsing, importing, and managing assets. Assets can be dragged from the
/// project panel into the scene.
/// </para>
/// </remarks>
internal sealed class ProjectPlugin : EditorPluginBase
{
    private const string PanelId = "project";

    /// <inheritdoc />
    public override string Name => "Project";

    /// <inheritdoc />
    public override string? Description => "Project panel for asset browsing and management";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the project panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Project",
                Icon = "folder",
                DefaultLocation = PanelDockLocation.Bottom,
                OpenByDefault = true,
                MinWidth = 300,
                MinHeight = 150,
                DefaultWidth = 600,
                DefaultHeight = 250,
                Category = "Assets",
                ToggleShortcut = "Ctrl+Shift+P"
            },
            () => new ProjectPanelImpl());

        // Register shortcut for toggling the project panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "project.toggle",
                "Toggle Project",
                ShortcutCategories.View,
                "Ctrl+Shift+P",
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

            shortcuts.RegisterShortcut(
                "project.refresh",
                "Refresh Project",
                ShortcutCategories.File,
                "Ctrl+R",
                () =>
                {
                    // Rescan all assets in the project
                    context.Assets.Scan();
                });
        }
    }
}

/// <summary>
/// Implementation of the project panel.
/// </summary>
internal sealed class ProjectPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private Entity panelEntity;
    private Entity treeView;
    private IWorld? editorWorld;
    private IAssetDatabase? assetDatabase;
    private FontHandle font;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        editorWorld = context.EditorWorld;
        rootEntity = context.Parent;
        assetDatabase = context.EditorContext.Assets;
        font = context.Font;

        // Create the main panel container
        panelEntity = WidgetFactory.CreatePanel(editorWorld, rootEntity, "ProjectPanel", new PanelConfig(
            Width: 250,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(panelEntity);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.Fill;

        // Create toolbar
        CreateToolbar(editorWorld, panelEntity, font);

        // Create tree view for assets
        treeView = WidgetFactory.CreateTreeView(editorWorld, panelEntity, new TreeViewConfig(
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f),
            IndentSize: 16,
            RowHeight: 22,
            FontSize: 12,
            TextColor: EditorColors.TextLight
        ));

        ref var treeRect = ref editorWorld.Get<UIRect>(treeView);
        treeRect.WidthMode = UISizeMode.Fill;
        treeRect.HeightMode = UISizeMode.Fill;

        // Subscribe to asset database events
        if (assetDatabase is not null)
        {
            assetDatabase.AssetAdded += OnAssetAdded;
            assetDatabase.AssetRemoved += OnAssetRemoved;
            assetDatabase.AssetModified += OnAssetModified;
        }

        // Populate with existing assets
        RefreshAssetTree();

        // Handle double-click for opening assets
        editorWorld.Subscribe<UITreeNodeDoubleClickedEvent>(OnTreeNodeDoubleClick);
    }

    private static void CreateToolbar(IWorld world, Entity parent, FontHandle toolbarFont)
    {
        var toolbar = WidgetFactory.CreatePanel(world, parent, "ProjectToolbar", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.Symmetric(8, 4),
            Spacing: 8
        ));

        ref var toolbarRect = ref world.Get<UIRect>(toolbar);
        toolbarRect.WidthMode = UISizeMode.Fill;

        // Title
        WidgetFactory.CreateLabel(world, toolbar, "ProjectTitle", "Project", toolbarFont, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));
    }

    private void OnAssetAdded(object? sender, AssetEventArgs args)
    {
        // Refresh the entire tree when assets change
        RefreshAssetTree();
    }

    private void OnAssetRemoved(object? sender, AssetEventArgs args)
    {
        RefreshAssetTree();
    }

    private void OnAssetModified(object? sender, AssetEventArgs args)
    {
        // Asset content changed but path is same, no tree update needed
    }

    private void OnTreeNodeDoubleClick(UITreeNodeDoubleClickedEvent e)
    {
        if (editorWorld is null)
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
    private void RefreshAssetTree()
    {
        if (editorWorld is null || assetDatabase is null || !treeView.IsValid)
        {
            return;
        }

        // Clear existing tree nodes
        ClearTreeNodes();

        // Build folder structure from assets
        var folderTree = BuildFolderTree(assetDatabase.AllAssets.Values);

        // Create tree nodes
        CreateFolderNodes(editorWorld, treeView, Entity.Null, font, folderTree, "");
    }

    private void ClearTreeNodes()
    {
        if (editorWorld is null || !editorWorld.Has<UITreeView>(treeView))
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

    private static FolderNode BuildFolderTree(IEnumerable<AssetEntry> assets)
    {
        var root = new FolderNode("Assets");

        foreach (var asset in assets)
        {
#pragma warning disable S3878 // Arrays should not be created for params parameters
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

    private void CreateFolderNodes(
        IWorld world,
        Entity parentTreeView,
        Entity parentNode,
        FontHandle nodeFont,
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
                world,
                parentTreeView,
                parentNode,
                $"{FolderIcon} {subFolder.Key}",
                nodeFont,
                new TreeNodeConfig(IsExpanded: true));

            // Recursively create child nodes
            CreateFolderNodes(world, parentTreeView, folderNode, nodeFont, subFolder.Value, folderPath);

            // Update has children state
            WidgetFactory.UpdateTreeNodeHasChildren(world, folderNode);
        }

        // Create asset nodes (sorted alphabetically)
        foreach (var asset in folder.Assets.OrderBy(a => a.Name))
        {
            var extension = Path.GetExtension(asset.FullPath);
            var assetNode = WidgetFactory.CreateTreeNode(
                world,
                parentTreeView,
                parentNode,
                $"{GetAssetTypeIcon(asset.Type)} {asset.Name}{extension}",
                nodeFont);

            // Tag node with asset data for double-click handling
            world.Add(assetNode, new ProjectAssetNodeTag
            {
                RelativePath = asset.RelativePath,
                AssetType = asset.Type
            });
        }
    }

    private const string FolderIcon = "\uD83D\uDCC1"; // Folder emoji

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

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Project panel uses UI events, no per-frame logic needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        // Unsubscribe from asset database events
        if (assetDatabase is not null)
        {
            assetDatabase.AssetAdded -= OnAssetAdded;
            assetDatabase.AssetRemoved -= OnAssetRemoved;
            assetDatabase.AssetModified -= OnAssetModified;
        }

        if (editorWorld is not null && panelEntity.IsValid && editorWorld.IsAlive(panelEntity))
        {
            editorWorld.Despawn(panelEntity);
        }
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
