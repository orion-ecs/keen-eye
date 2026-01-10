// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Plugins;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for EditorPluginManager logging integration.
/// </summary>
public sealed class EditorPluginManagerLoggingTests : IDisposable
{
    private readonly World world;
    private readonly StubEditorWorldManager worldManager;
    private readonly StubSelectionManager selection;
    private readonly StubUndoRedoManager undoRedo;
    private readonly StubAssetDatabase assets;
    private readonly TestLogProvider logProvider;

    public EditorPluginManagerLoggingTests()
    {
        world = new World();
        worldManager = new StubEditorWorldManager();
        selection = new StubSelectionManager();
        undoRedo = new StubUndoRedoManager();
        assets = new StubAssetDatabase();
        logProvider = new TestLogProvider();
    }

    public void Dispose()
    {
        logProvider.Dispose();
        world.Dispose();
    }

    private EditorPluginManager CreateManager(ILogProvider? logProvider = null)
    {
        return new EditorPluginManager(
            worldManager,
            selection,
            undoRedo,
            assets,
            world,
            logProvider);
    }

    #region LogInfo Tests

    [Fact]
    public void LogInfo_WithLogProvider_CallsLogProviderWithInfoLevel()
    {
        using var manager = CreateManager(logProvider);

        // Access via IEditorPluginLogger interface to trigger logging
        ((IEditorPluginLogger)manager).LogInfo("Test message");

        Assert.Single(logProvider.Entries);
        Assert.Equal(LogLevel.Info, logProvider.Entries[0].Level);
        Assert.Equal("Plugin", logProvider.Entries[0].Category);
        Assert.Equal("Test message", logProvider.Entries[0].Message);
    }

    [Fact]
    public void LogInfo_WithoutLogProvider_DoesNotThrow()
    {
        using var manager = CreateManager(logProvider: null);

        // Should not throw - falls back to Console.WriteLine
        var exception = Record.Exception(() =>
            ((IEditorPluginLogger)manager).LogInfo("Test message"));

        Assert.Null(exception);
    }

    #endregion

    #region LogWarning Tests

    [Fact]
    public void LogWarning_WithLogProvider_CallsLogProviderWithWarningLevel()
    {
        using var manager = CreateManager(logProvider);

        ((IEditorPluginLogger)manager).LogWarning("Warning message");

        Assert.Single(logProvider.Entries);
        Assert.Equal(LogLevel.Warning, logProvider.Entries[0].Level);
        Assert.Equal("Plugin", logProvider.Entries[0].Category);
        Assert.Equal("Warning message", logProvider.Entries[0].Message);
    }

    [Fact]
    public void LogWarning_WithoutLogProvider_DoesNotThrow()
    {
        using var manager = CreateManager(logProvider: null);

        var exception = Record.Exception(() =>
            ((IEditorPluginLogger)manager).LogWarning("Warning message"));

        Assert.Null(exception);
    }

    #endregion

    #region LogError Tests

    [Fact]
    public void LogError_WithLogProvider_CallsLogProviderWithErrorLevel()
    {
        using var manager = CreateManager(logProvider);

        ((IEditorPluginLogger)manager).LogError("Error message");

        Assert.Single(logProvider.Entries);
        Assert.Equal(LogLevel.Error, logProvider.Entries[0].Level);
        Assert.Equal("Plugin", logProvider.Entries[0].Category);
        Assert.Equal("Error message", logProvider.Entries[0].Message);
    }

    [Fact]
    public void LogError_WithoutLogProvider_DoesNotThrow()
    {
        using var manager = CreateManager(logProvider: null);

        var exception = Record.Exception(() =>
            ((IEditorPluginLogger)manager).LogError("Error message"));

        Assert.Null(exception);
    }

    #endregion

    #region Mixed Logging Tests

    [Fact]
    public void MultipleLogs_RecordsAllEntries()
    {
        using var manager = CreateManager(logProvider);
        var logger = (IEditorPluginLogger)manager;

        logger.LogInfo("Info 1");
        logger.LogWarning("Warning 1");
        logger.LogError("Error 1");
        logger.LogInfo("Info 2");

        Assert.Equal(4, logProvider.Entries.Count);
        Assert.Equal(LogLevel.Info, logProvider.Entries[0].Level);
        Assert.Equal(LogLevel.Warning, logProvider.Entries[1].Level);
        Assert.Equal(LogLevel.Error, logProvider.Entries[2].Level);
        Assert.Equal(LogLevel.Info, logProvider.Entries[3].Level);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Simple log provider that captures log entries for testing.
    /// </summary>
    private sealed class TestLogProvider : ILogProvider
    {
        public string Name => "Test";
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
        public List<LogEntry> Entries { get; } = [];

        public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
        {
            Entries.Add(new LogEntry(DateTime.Now, level, category, message, properties));
        }

        public void Flush() { }
        public void Dispose() { }
    }

    /// <summary>
    /// Stub implementation of IEditorWorldManager for testing.
    /// </summary>
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
#pragma warning restore CS0067

    /// <summary>
    /// Stub implementation of ISelectionManager for testing.
    /// </summary>
#pragma warning disable CS0067 // Event is never used
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
#pragma warning restore CS0067

    /// <summary>
    /// Stub implementation of IUndoRedoManager for testing.
    /// </summary>
#pragma warning disable CS0067 // Event is never used
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
#pragma warning restore CS0067

    /// <summary>
    /// Stub implementation of IAssetDatabase for testing.
    /// </summary>
#pragma warning disable CS0067 // Event is never used
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
