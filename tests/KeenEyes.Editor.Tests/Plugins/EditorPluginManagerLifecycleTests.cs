// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Plugins;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for <see cref="EditorPluginManager"/> plugin lifecycle failure handling (#1177).
/// </summary>
public sealed class EditorPluginManagerLifecycleTests : IDisposable
{
    private readonly World world;
    private readonly StubEditorWorldManager worldManager = new();
    private readonly StubSelectionManager selection = new();
    private readonly StubUndoRedoManager undoRedo = new();
    private readonly StubAssetDatabase assets = new();

    public EditorPluginManagerLifecycleTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    private EditorPluginManager CreateManager()
    {
        return new EditorPluginManager(worldManager, selection, undoRedo, assets, world);
    }

    #region Failed Initialize Cleanup Tests

    [Fact]
    public void InstallPlugin_WhenInitializeThrows_DisposesSubscriptionsSoDeadHandlerNeverFires()
    {
        using var manager = CreateManager();
        var badPlugin = new ThrowingInitPlugin();

        // Initialize throws after subscribing to OnSceneOpened; the failure propagates.
        Assert.Throws<InvalidOperationException>(() => manager.InstallPlugin(badPlugin));

        // The subscription registered before the throw must have been disposed, so
        // raising the scene-opened event must not invoke the dead plugin's handler.
        manager.RaiseSceneOpened(world);

        Assert.False(badPlugin.HandlerFired);
    }

    [Fact]
    public void RaiseSceneOpened_WhenOneHandlerThrows_StillInvokesOtherHandlersAndDoesNotEscape()
    {
        using var manager = CreateManager();
        var faulting = new SceneOpenedPlugin("Faulting", throwOnScene: true);
        var healthy = new SceneOpenedPlugin("Healthy", throwOnScene: false);

        manager.InstallPlugin(faulting);
        manager.InstallPlugin(healthy);

        // A throwing handler must not escape the raise loop nor prevent later handlers.
        var exception = Record.Exception(() => manager.RaiseSceneOpened(world));

        Assert.Null(exception);
        Assert.True(healthy.HandlerFired);
    }

    #endregion

    #region Test Plugins

    /// <summary>
    /// Plugin that subscribes to scene-opened then fails during initialization.
    /// </summary>
    private sealed class ThrowingInitPlugin : IEditorPlugin
    {
        public string Name => "ThrowingInitPlugin";
        public string Version => "1.0.0";
        public string? Description => null;

        public bool HandlerFired { get; private set; }

        public void Initialize(IEditorContext context)
        {
            context.OnSceneOpened(_ => HandlerFired = true);
            throw new InvalidOperationException("Initialization failed after subscribing.");
        }

        public void Shutdown()
        {
        }
    }

    /// <summary>
    /// Plugin that subscribes to scene-opened; its handler optionally throws.
    /// </summary>
    private sealed class SceneOpenedPlugin(string name, bool throwOnScene) : IEditorPlugin
    {
        public string Name => name;
        public string Version => "1.0.0";
        public string? Description => null;

        public bool HandlerFired { get; private set; }

        public void Initialize(IEditorContext context)
        {
            context.OnSceneOpened(_ =>
            {
                HandlerFired = true;
                if (throwOnScene)
                {
                    throw new InvalidOperationException("Handler failure.");
                }
            });
        }

        public void Shutdown()
        {
        }
    }

    #endregion

    #region Service Stubs

#pragma warning disable CS0067 // Events are never used by these test stubs.
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

    private sealed class StubAssetDatabase : IAssetDatabase
    {
        public string ProjectRoot => string.Empty;
        public IReadOnlyDictionary<string, AssetEntry> AllAssets => new Dictionary<string, AssetEntry>();
        public event EventHandler<AssetEventArgs>? AssetAdded;
        public event EventHandler<AssetEventArgs>? AssetRemoved;
        public event EventHandler<AssetEventArgs>? AssetModified;

        public void Scan(params string[] extensions) { }
        public void StartWatching() { }
        public void StopWatching() { }
        public AssetEntry? GetAsset(string relativePath) => null;
        public IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType) => [];
        public void Refresh(string relativePath) { }
    }
#pragma warning restore CS0067

    #endregion
}
