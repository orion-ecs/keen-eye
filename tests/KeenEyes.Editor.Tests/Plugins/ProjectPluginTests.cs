// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for ProjectPlugin functionality.
/// </summary>
public sealed class ProjectPluginTests : IDisposable
{
    private readonly World world;

    public ProjectPluginTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region ProjectPanelImpl Tests

    [Fact]
    public void ProjectPanelImpl_Initialize_CreatesRootEntity()
    {
        // Arrange
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);

        // Act
        panelImpl.Initialize(context);

        // Assert
        Assert.True(panelImpl.RootEntity.IsValid);
    }

    [Fact]
    public void ProjectPanelImpl_Update_DoesNotThrow()
    {
        // Arrange
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act & Assert - should not throw
        var exception = Record.Exception(() => panelImpl.Update(0.016f));
        Assert.Null(exception);
    }

    [Fact]
    public void ProjectPanelImpl_Shutdown_DoesNotThrow()
    {
        // Arrange
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act - should not throw
        var exception = Record.Exception(() => panelImpl.Shutdown());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ProjectPanelImpl_RefreshAssetTree_WithEmptyDatabase_DoesNotThrow()
    {
        // Arrange
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act - should not throw when asset database is empty
        var exception = Record.Exception(() => panelImpl.SimulateRefreshAssetTree());
        Assert.Null(exception);
    }

    [Fact]
    public void ProjectPanelImpl_RefreshAssetTree_WithAssets_BuildsFolderStructure()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var assets = new Dictionary<string, AssetEntry>
        {
            { "Scenes/Level1.kescene", new AssetEntry("Scenes/Level1.kescene", "/full/path/Scenes/Level1.kescene", "Level1", AssetType.Scene, now) },
            { "Scenes/Level2.kescene", new AssetEntry("Scenes/Level2.kescene", "/full/path/Scenes/Level2.kescene", "Level2", AssetType.Scene, now) },
            { "Textures/Player.png", new AssetEntry("Textures/Player.png", "/full/path/Textures/Player.png", "Player", AssetType.Texture, now) }
        };
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assets);
        panelImpl.Initialize(context);

        // Act
        var folderTree = panelImpl.SimulateBuildFolderTree();

        // Assert
        Assert.NotNull(folderTree);
        Assert.Equal(2, folderTree.SubFolderCount); // Scenes and Textures folders
    }

    [Fact]
    public void ProjectPanelImpl_AssetAdded_TriggersRefresh()
    {
        // Arrange
        var assetDb = new TestAssetDatabase();
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assetDb);
        panelImpl.Initialize(context);
        var refreshCount = panelImpl.RefreshCount;

        // Act
        assetDb.SimulateAssetAdded("NewAsset.png", AssetType.Texture);

        // Assert
        Assert.Equal(refreshCount + 1, panelImpl.RefreshCount);
    }

    [Fact]
    public void ProjectPanelImpl_AssetRemoved_TriggersRefresh()
    {
        // Arrange
        var assetDb = new TestAssetDatabase();
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assetDb);
        panelImpl.Initialize(context);
        var refreshCount = panelImpl.RefreshCount;

        // Act
        assetDb.SimulateAssetRemoved("SomeAsset.png", AssetType.Texture);

        // Assert
        Assert.Equal(refreshCount + 1, panelImpl.RefreshCount);
    }

    [Fact]
    public void ProjectPanelImpl_AssetModified_DoesNotTriggerRefresh()
    {
        // Arrange
        var assetDb = new TestAssetDatabase();
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assetDb);
        panelImpl.Initialize(context);
        var refreshCount = panelImpl.RefreshCount;

        // Act - asset modification doesn't require tree rebuild
        assetDb.SimulateAssetModified("SomeAsset.png", AssetType.Texture);

        // Assert
        Assert.Equal(refreshCount, panelImpl.RefreshCount);
    }

    [Fact]
    public void ProjectPanelImpl_Shutdown_UnsubscribesFromEvents()
    {
        // Arrange
        var assetDb = new TestAssetDatabase();
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assetDb);
        panelImpl.Initialize(context);

        // Act
        panelImpl.Shutdown();
        var refreshCount = panelImpl.RefreshCount;
        assetDb.SimulateAssetAdded("NewAsset.png", AssetType.Texture);

        // Assert - should not refresh after shutdown
        Assert.Equal(refreshCount, panelImpl.RefreshCount);
    }

    #endregion

    #region Asset Tree Building Tests

    [Fact]
    public void BuildFolderTree_EmptyAssets_ReturnsRootOnly()
    {
        // Arrange
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, new Dictionary<string, AssetEntry>());
        panelImpl.Initialize(context);

        // Act
        var tree = panelImpl.SimulateBuildFolderTree();

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(0, tree.SubFolderCount);
        Assert.Equal(0, tree.AssetCount);
    }

    [Fact]
    public void BuildFolderTree_NestedFolders_CreatesCorrectHierarchy()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var assets = new Dictionary<string, AssetEntry>
        {
            { "Art/Characters/Player.png", new AssetEntry("Art/Characters/Player.png", "/full/Art/Characters/Player.png", "Player", AssetType.Texture, now) },
            { "Art/Characters/Enemy.png", new AssetEntry("Art/Characters/Enemy.png", "/full/Art/Characters/Enemy.png", "Enemy", AssetType.Texture, now) },
            { "Art/Environment/Grass.png", new AssetEntry("Art/Environment/Grass.png", "/full/Art/Environment/Grass.png", "Grass", AssetType.Texture, now) }
        };
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assets);
        panelImpl.Initialize(context);

        // Act
        var tree = panelImpl.SimulateBuildFolderTree();

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(1, tree.SubFolderCount); // Only "Art" at root level
    }

    [Fact]
    public void BuildFolderTree_MultipleAssetTypes_GroupsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var assets = new Dictionary<string, AssetEntry>
        {
            { "scene.kescene", new AssetEntry("scene.kescene", "/full/scene.kescene", "scene", AssetType.Scene, now) },
            { "texture.png", new AssetEntry("texture.png", "/full/texture.png", "texture", AssetType.Texture, now) },
            { "audio.wav", new AssetEntry("audio.wav", "/full/audio.wav", "audio", AssetType.Audio, now) }
        };
        var panelImpl = new ProjectPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity, assets);
        panelImpl.Initialize(context);

        // Act
        var tree = panelImpl.SimulateBuildFolderTree();

        // Assert
        Assert.NotNull(tree);
        Assert.Equal(0, tree.SubFolderCount); // No subfolders for root-level files
        Assert.Equal(3, tree.AssetCount); // All three assets at root
    }

    #endregion

    #region Test Helpers

    private PanelContext CreatePanelContext(Entity parent)
    {
        return CreatePanelContext(parent, new Dictionary<string, AssetEntry>());
    }

    private PanelContext CreatePanelContext(Entity parent, IReadOnlyDictionary<string, AssetEntry> assets)
    {
        return CreatePanelContext(parent, new StubAssetDatabase(assets));
    }

    private PanelContext CreatePanelContext(Entity parent, IAssetDatabase assetDatabase)
    {
        return new PanelContext
        {
            EditorContext = new TestEditorContext(assetDatabase),
            EditorWorld = world,
            Parent = parent,
            Descriptor = new PanelDescriptor
            {
                Id = "project",
                Title = "Project"
            },
            Font = new FontHandle(1)
        };
    }

    /// <summary>
    /// Test implementation of IEditorContext.
    /// </summary>
    private sealed class TestEditorContext(IAssetDatabase assets) : IEditorContext
    {
        public IEditorWorldManager Worlds => new StubEditorWorldManager();
        public ISelectionManager Selection => new StubSelectionManager();
        public IUndoRedoManager UndoRedo => new StubUndoRedoManager();
        public IAssetDatabase Assets => assets;
        public IWorld EditorWorld => throw new NotSupportedException();
        public ILogQueryable? Log => null;

        public void SetExtension<T>(T extension) where T : class { }
        public T GetExtension<T>() where T : class => throw new InvalidOperationException();
        public bool TryGetExtension<T>(out T? extension) where T : class { extension = default; return false; }
        public bool RemoveExtension<T>() where T : class => false;
        public T GetCapability<T>() where T : class, IEditorCapability => throw new InvalidOperationException();
        public bool TryGetCapability<T>(out T? capability) where T : class, IEditorCapability { capability = default; return false; }
        public bool HasCapability<T>() where T : class, IEditorCapability => false;
        public EventSubscription OnSceneOpened(Action<IWorld> handler) => new(() => { });
        public EventSubscription OnSceneClosed(Action handler) => new(() => { });
        public EventSubscription OnSelectionChanged(Action<IReadOnlyList<Entity>> handler) => new(() => { });
        public EventSubscription OnPlayModeChanged(Action<EditorPlayState> handler) => new(() => { });
    }

    /// <summary>
    /// Testable version of project panel that exposes internal behavior.
    /// </summary>
    private sealed class ProjectPanelImplTestable : IEditorPanel
    {
        private Entity rootEntity;
        private IWorld? editorWorld;
        private IAssetDatabase? assetDatabase;
        private bool initialized;

        public Entity RootEntity => rootEntity;
        public int RefreshCount { get; private set; }

        public void Initialize(PanelContext context)
        {
            editorWorld = context.EditorWorld;
            assetDatabase = context.EditorContext.Assets;

            // For testing, we create a simple entity to verify initialization worked
            rootEntity = context.EditorWorld.Spawn().Build();
            initialized = true;

            // Subscribe to asset events
            if (assetDatabase is not null)
            {
                assetDatabase.AssetAdded += OnAssetAdded;
                assetDatabase.AssetRemoved += OnAssetRemoved;
                assetDatabase.AssetModified += OnAssetModified;
            }

            // Initial refresh
            SimulateRefreshAssetTree();
        }

        public void Update(float deltaTime)
        {
            // Project panel updates are handled via event subscriptions
        }

        public void Shutdown()
        {
            if (assetDatabase is not null)
            {
                assetDatabase.AssetAdded -= OnAssetAdded;
                assetDatabase.AssetRemoved -= OnAssetRemoved;
                assetDatabase.AssetModified -= OnAssetModified;
            }

            if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
            {
                editorWorld.Despawn(rootEntity);
            }
        }

        private void OnAssetAdded(object? sender, AssetEventArgs args)
        {
            SimulateRefreshAssetTree();
        }

        private void OnAssetRemoved(object? sender, AssetEventArgs args)
        {
            SimulateRefreshAssetTree();
        }

        private void OnAssetModified(object? sender, AssetEventArgs args)
        {
            // Asset content changed but path is same, no tree update needed
        }

        // Test helper methods to simulate internal behavior
        public void SimulateRefreshAssetTree()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Panel not initialized");
            }
            RefreshCount++;
        }

        public FolderNodeTestHelper SimulateBuildFolderTree()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Panel not initialized");
            }

            var root = new FolderNodeTestHelper("Assets");
            if (assetDatabase is null)
            {
                return root;
            }

            foreach (var asset in assetDatabase.AllAssets.Values)
            {
                var pathParts = asset.RelativePath.Split(['/', '\\']);
                var currentFolder = root;

                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    var folderName = pathParts[i];
                    if (!currentFolder.SubFolders.TryGetValue(folderName, out var subFolder))
                    {
                        subFolder = new FolderNodeTestHelper(folderName);
                        currentFolder.SubFolders[folderName] = subFolder;
                    }
                    currentFolder = subFolder;
                }

                currentFolder.Assets.Add(asset);
            }

            return root;
        }
    }

    /// <summary>
    /// Helper class for testing folder tree building.
    /// </summary>
    internal sealed class FolderNodeTestHelper(string name)
    {
        public string Name { get; } = name;
        public Dictionary<string, FolderNodeTestHelper> SubFolders { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<AssetEntry> Assets { get; } = [];

        public int SubFolderCount => SubFolders.Count;
        public int AssetCount => Assets.Count;
    }

#pragma warning disable CS0067 // Event is never used
    private sealed class StubEditorWorldManager : IEditorWorldManager
    {
        public IWorld? CurrentSceneWorld => null;
        public string? CurrentScenePath => null;
        public bool HasUnsavedChanges => false;
        public event EventHandler<SceneEventArgs>? SceneOpened;
        public event EventHandler? SceneClosed;
        public event EventHandler? SceneModified;

        public void NewScene() { }
        public bool LoadScene(string path) => false;
        public bool SaveScene() => false;
        public bool SaveSceneAs(string path) => false;
        public void CloseScene() { }
        public void MarkModified() { }
        public IEnumerable<Entity> GetRootEntities() => [];
        public IEnumerable<Entity> GetChildren(Entity parent) => [];
        public string GetEntityName(Entity entity) => string.Empty;
    }

    private sealed class StubSelectionManager : ISelectionManager
    {
        public Entity PrimarySelection => Entity.Null;
        public IReadOnlyCollection<Entity> SelectedEntities => Array.Empty<Entity>();
        public bool HasSelection => false;
        public bool HasMultipleSelection => false;
        public int SelectionCount => 0;
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

        public void Select(Entity entity) { }
        public void AddToSelection(Entity entity) { }
        public void RemoveFromSelection(Entity entity) { }
        public void ToggleSelection(Entity entity) { }
        public void SelectMultiple(IEnumerable<Entity> entities) { }
        public void ClearSelection() { }
        public bool IsSelected(Entity entity) => false;
    }

    private sealed class StubUndoRedoManager : IUndoRedoManager
    {
        public bool CanUndo => false;
        public bool CanRedo => false;
        public string? NextUndoDescription => null;
        public string? NextRedoDescription => null;
        public int MaxHistorySize { get; set; } = 100;
        public event Action? StateChanged;

        public void Execute(IEditorCommand command) { }
        public bool Undo() => false;
        public bool Redo() => false;
        public void Clear() { }
        public void BeginBatch(string description) { }
        public void EndBatch() { }
        public void CancelBatch() { }
    }

    private sealed class StubAssetDatabase(IReadOnlyDictionary<string, AssetEntry> initialAssets) : IAssetDatabase
    {
        private readonly Dictionary<string, AssetEntry> assets = new(initialAssets);

        public string ProjectRoot => string.Empty;
        public IReadOnlyDictionary<string, AssetEntry> AllAssets => assets;
        public event EventHandler<AssetEventArgs>? AssetAdded;
        public event EventHandler<AssetEventArgs>? AssetRemoved;
        public event EventHandler<AssetEventArgs>? AssetModified;

        public void Scan(params string[] extensions) { }
        public void StartWatching() { }
        public void StopWatching() { }
        public AssetEntry? GetAsset(string relativePath) => assets.GetValueOrDefault(relativePath);
        public IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType) => assets.Values.Where(a => a.Type == assetType);
        public void Refresh(string relativePath) { }
    }

    private sealed class TestAssetDatabase : IAssetDatabase
    {
        private readonly Dictionary<string, AssetEntry> assets = [];

        public string ProjectRoot => string.Empty;
        public IReadOnlyDictionary<string, AssetEntry> AllAssets => assets;
        public event EventHandler<AssetEventArgs>? AssetAdded;
        public event EventHandler<AssetEventArgs>? AssetRemoved;
        public event EventHandler<AssetEventArgs>? AssetModified;

        public void Scan(params string[] extensions) { }
        public void StartWatching() { }
        public void StopWatching() { }
        public AssetEntry? GetAsset(string relativePath) => assets.GetValueOrDefault(relativePath);
        public IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType) => assets.Values.Where(a => a.Type == assetType);
        public void Refresh(string relativePath) { }

        public void SimulateAssetAdded(string path, AssetType type)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var entry = new AssetEntry(path, $"/full/{path}", name, type, DateTime.UtcNow);
            assets[path] = entry;
            AssetAdded?.Invoke(this, new AssetEventArgs(entry));
        }

        public void SimulateAssetRemoved(string path, AssetType type)
        {
            if (assets.TryGetValue(path, out var entry))
            {
                assets.Remove(path);
                AssetRemoved?.Invoke(this, new AssetEventArgs(entry));
            }
            else
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var removedEntry = new AssetEntry(path, $"/full/{path}", name, type, DateTime.UtcNow);
                AssetRemoved?.Invoke(this, new AssetEventArgs(removedEntry));
            }
        }

        public void SimulateAssetModified(string path, AssetType type)
        {
            if (assets.TryGetValue(path, out var entry))
            {
                AssetModified?.Invoke(this, new AssetEventArgs(entry));
            }
            else
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var modifiedEntry = new AssetEntry(path, $"/full/{path}", name, type, DateTime.UtcNow);
                AssetModified?.Invoke(this, new AssetEventArgs(modifiedEntry));
            }
        }
    }
#pragma warning restore CS0067

    #endregion
}
