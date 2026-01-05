using KeenEyes.Editor.Application;

namespace KeenEyes.Editor.Tests.Application;

/// <summary>
/// Tests for EditorWorldManager scene serialization functionality.
/// </summary>
public class EditorWorldManagerTests : IDisposable
{
    private readonly EditorWorldManager manager;
    private readonly string tempDir;

    public EditorWorldManagerTests()
    {
        manager = new EditorWorldManager();
        tempDir = Path.Combine(Path.GetTempPath(), $"KeenEyes_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        manager.Dispose();
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #region NewScene Tests

    [Fact]
    public void NewScene_CreatesEmptyScene()
    {
        // Act
        manager.NewScene();

        // Assert - should have world available
        Assert.NotNull(manager.World);
        Assert.True(manager.CurrentSceneRoot.IsValid);
    }

    [Fact]
    public void NewScene_ClearsCurrentPath()
    {
        // Arrange - save a scene first
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("Test");
        manager.SaveSceneAs(path);

        // Act
        manager.NewScene();

        // Assert
        Assert.Null(manager.CurrentScenePath);
    }

    [Fact]
    public void NewScene_ClearsUnsavedChanges()
    {
        // Arrange
        manager.CreateEntity("Test");
        Assert.True(manager.HasUnsavedChanges);

        // Act
        manager.NewScene();

        // Assert
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void NewScene_RaisesSceneOpenedEvent()
    {
        // Arrange
        var eventRaised = false;
        manager.SceneOpened += _ => eventRaised = true;

        // Act
        manager.NewScene();

        // Assert
        Assert.True(eventRaised);
    }

    #endregion

    #region SaveSceneAs Tests

    [Fact]
    public void SaveSceneAs_CreatesFile()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");

        // Act
        var result = manager.SaveSceneAs(path);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SaveSceneAs_UpdatesCurrentPath()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");

        // Act
        manager.SaveSceneAs(path);

        // Assert
        Assert.Equal(path, manager.CurrentScenePath);
    }

    [Fact]
    public void SaveSceneAs_ClearsUnsavedChanges()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");
        Assert.True(manager.HasUnsavedChanges);

        // Act
        manager.SaveSceneAs(path);

        // Assert
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void SaveSceneAs_WithNoScene_ReturnsFalse()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CloseScene();

        // Act
        var result = manager.SaveSceneAs(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SaveSceneAs_WritesValidJson()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");

        // Act
        manager.SaveSceneAs(path);

        // Assert
        var json = File.ReadAllText(path);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    #endregion

    #region SaveScene Tests

    [Fact]
    public void SaveScene_WithNoPath_ReturnsFalse()
    {
        // Arrange - new scene has no path
        manager.CreateEntity("Test");

        // Act
        var result = manager.SaveScene();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SaveScene_WithPath_SavesSuccessfully()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");
        manager.SaveSceneAs(path);

        // Modify and mark dirty
        manager.CreateEntity("AnotherEntity");
        Assert.True(manager.HasUnsavedChanges);

        // Act
        var result = manager.SaveScene();

        // Assert
        Assert.True(result);
        Assert.False(manager.HasUnsavedChanges);
    }

    #endregion

    #region LoadScene Tests

    [Fact]
    public void LoadScene_LoadsEntities()
    {
        // Arrange - create and save a scene
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("Entity1");
        manager.CreateEntity("Entity2");
        manager.SaveSceneAs(path);

        // Create new scene to clear
        manager.NewScene();

        // Act
        manager.LoadScene(path);

        // Assert - should have entities from file plus scene root
        var rootEntities = manager.GetRootEntities().ToList();
        Assert.Equal(2, rootEntities.Count);
    }

    [Fact]
    public void LoadScene_SetsCurrentPath()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");
        manager.SaveSceneAs(path);
        manager.NewScene();

        // Act
        manager.LoadScene(path);

        // Assert
        Assert.Equal(path, manager.CurrentScenePath);
    }

    [Fact]
    public void LoadScene_ClearsUnsavedChanges()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");
        manager.SaveSceneAs(path);
        manager.NewScene();
        manager.CreateEntity("Unsaved");
        Assert.True(manager.HasUnsavedChanges);

        // Act
        manager.LoadScene(path);

        // Assert
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void LoadScene_RaisesSceneOpenedEvent()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("TestEntity");
        manager.SaveSceneAs(path);

        var eventRaised = false;
        manager.SceneOpened += _ => eventRaised = true;

        // Act
        manager.LoadScene(path);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void LoadScene_PreservesEntityNames()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("Alpha");
        manager.CreateEntity("Beta");
        manager.CreateEntity("Gamma");
        manager.SaveSceneAs(path);
        manager.NewScene();

        // Act
        manager.LoadScene(path);

        // Assert
        var names = manager.GetRootEntities()
            .Select(e => manager.GetEntityName(e))
            .ToHashSet();
        Assert.Contains("Alpha", names);
        Assert.Contains("Beta", names);
        Assert.Contains("Gamma", names);
    }

    #endregion

    #region HasUnsavedChanges Tests

    [Fact]
    public void HasUnsavedChanges_InitiallyFalse()
    {
        // Assert - new scene has no changes
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_TrueAfterCreateEntity()
    {
        // Act
        manager.CreateEntity("Test");

        // Assert
        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_TrueAfterMarkModified()
    {
        // Act
        manager.MarkModified();

        // Assert
        Assert.True(manager.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_FalseAfterSave()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("Test");

        // Act
        manager.SaveSceneAs(path);

        // Assert
        Assert.False(manager.HasUnsavedChanges);
    }

    #endregion

    #region CloseScene Tests

    [Fact]
    public void CloseScene_ClearsPath()
    {
        // Arrange
        var path = Path.Combine(tempDir, "test.kescene");
        manager.CreateEntity("Test");
        manager.SaveSceneAs(path);

        // Act
        manager.CloseScene();

        // Assert
        Assert.Null(manager.CurrentScenePath);
    }

    [Fact]
    public void CloseScene_ClearsUnsavedChanges()
    {
        // Arrange
        manager.CreateEntity("Test");

        // Act
        manager.CloseScene();

        // Assert
        Assert.False(manager.HasUnsavedChanges);
    }

    [Fact]
    public void CloseScene_RaisesSceneClosedEvent()
    {
        // Arrange
        var eventRaised = false;
        manager.SceneClosed += () => eventRaised = true;

        // Act
        manager.CloseScene();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void CloseScene_InvalidatesSceneRoot()
    {
        // Act
        manager.CloseScene();

        // Assert
        Assert.False(manager.CurrentSceneRoot.IsValid);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_PreservesMultipleEntities()
    {
        // Arrange
        var path = Path.Combine(tempDir, "roundtrip.kescene");
        manager.CreateEntity("Player");
        manager.CreateEntity("Enemy1");
        manager.CreateEntity("Enemy2");
        manager.CreateEntity("Item");
        manager.SaveSceneAs(path);
        manager.NewScene();

        // Act
        manager.LoadScene(path);

        // Assert
        var rootEntities = manager.GetRootEntities().ToList();
        Assert.Equal(4, rootEntities.Count);
    }

    [Fact]
    public void RoundTrip_MultipleSavesPreservesData()
    {
        // Arrange
        var path = Path.Combine(tempDir, "multi.kescene");
        manager.CreateEntity("Initial");
        manager.SaveSceneAs(path);

        // First load/modify/save cycle
        manager.LoadScene(path);
        manager.CreateEntity("Added1");
        manager.SaveScene();

        // Second load/modify/save cycle
        manager.LoadScene(path);
        manager.CreateEntity("Added2");
        manager.SaveScene();

        // Final load
        manager.NewScene();
        manager.LoadScene(path);

        // Assert
        var rootEntities = manager.GetRootEntities().ToList();
        Assert.Equal(3, rootEntities.Count);

        var names = rootEntities.Select(e => manager.GetEntityName(e)).ToHashSet();
        Assert.Contains("Initial", names);
        Assert.Contains("Added1", names);
        Assert.Contains("Added2", names);
    }

    #endregion

    #region SceneModified Event Tests

    [Fact]
    public void CreateEntity_RaisesSceneModifiedEvent()
    {
        // Arrange
        var eventRaised = false;
        manager.SceneModified += () => eventRaised = true;

        // Act
        manager.CreateEntity("Test");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void MarkModified_RaisesSceneModifiedEvent()
    {
        // Arrange
        var eventRaised = false;
        manager.SceneModified += () => eventRaised = true;

        // Act
        manager.MarkModified();

        // Assert
        Assert.True(eventRaised);
    }

    #endregion
}
