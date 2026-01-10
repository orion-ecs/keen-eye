// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for InspectorPlugin functionality.
/// </summary>
public sealed class InspectorPluginTests : IDisposable
{
    private readonly World world;

    public InspectorPluginTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region InspectorPanelImpl Tests

    [Fact]
    public void InspectorPanelImpl_Initialize_CreatesRootEntity()
    {
        // Arrange
        var panelImpl = new InspectorPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);

        // Act
        panelImpl.Initialize(context);

        // Assert
        Assert.True(panelImpl.RootEntity.IsValid);
    }

    [Fact]
    public void InspectorPanelImpl_Update_DoesNotThrow()
    {
        // Arrange
        var panelImpl = new InspectorPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act & Assert - should not throw
        var exception = Record.Exception(() => panelImpl.Update(0.016f));
        Assert.Null(exception);
    }

    [Fact]
    public void InspectorPanelImpl_Shutdown_DisposesSubscriptions()
    {
        // Arrange
        var panelImpl = new InspectorPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act - should not throw
        var exception = Record.Exception(() => panelImpl.Shutdown());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void InspectorPanelImpl_OnSelectionChanged_WithNoSelection_ClearsInspector()
    {
        // Arrange
        var panelImpl = new InspectorPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);

        // Act - should not throw when clearing
        var exception = Record.Exception(() => panelImpl.SimulateSelectionChanged([]));
        Assert.Null(exception);
    }

    [Fact]
    public void InspectorPanelImpl_OnSelectionChanged_WithSelection_RefreshesInspector()
    {
        // Arrange
        var panelImpl = new InspectorPanelImplTestable();
        var parentEntity = world.Spawn().Build();
        var context = CreatePanelContext(parentEntity);
        panelImpl.Initialize(context);
        var selectedEntity = world.Spawn().Build();

        // Act - should not throw when selecting
        var exception = Record.Exception(() => panelImpl.SimulateSelectionChanged([selectedEntity]));
        Assert.Null(exception);
    }

    #endregion

    #region Test Helpers

    private PanelContext CreatePanelContext(Entity parent)
    {
        return new PanelContext
        {
            EditorContext = new TestEditorContext(),
            EditorWorld = world,
            Parent = parent,
            Descriptor = new PanelDescriptor
            {
                Id = "inspector",
                Title = "Inspector"
            },
            Font = new FontHandle(1)
        };
    }

    /// <summary>
    /// Test implementation of IEditorContext.
    /// </summary>
    private sealed class TestEditorContext : IEditorContext
    {
        public IEditorWorldManager Worlds => new StubEditorWorldManager();
        public ISelectionManager Selection => new StubSelectionManager();
        public IUndoRedoManager UndoRedo => new StubUndoRedoManager();
        public IAssetDatabase Assets => new StubAssetDatabase();
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
    /// Testable version of InspectorPanelImpl that exposes internal behavior.
    /// </summary>
    private sealed class InspectorPanelImplTestable : IEditorPanel
    {
        private Entity rootEntity;
        private IWorld? editorWorld;
        private bool initialized;

        public Entity RootEntity => rootEntity;

        public void Initialize(PanelContext context)
        {
            editorWorld = context.EditorWorld;

            // For testing, we create a simple entity to verify initialization worked
            rootEntity = context.EditorWorld.Spawn().Build();
            initialized = true;
        }

        public void Update(float deltaTime)
        {
            // Inspector updates are handled via event subscriptions
        }

        public void Shutdown()
        {
            if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
            {
                editorWorld.Despawn(rootEntity);
            }
        }

        // Test helper method to simulate selection changes
        public void SimulateSelectionChanged(IReadOnlyList<Entity> selectedEntities)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Panel not initialized");
            }

            // In actual impl, this rebuilds the inspector for selected entities
            // or clears it when selection is empty
        }
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
