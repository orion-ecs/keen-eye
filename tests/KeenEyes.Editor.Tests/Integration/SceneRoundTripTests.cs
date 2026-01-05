using System.Numerics;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Assets;
using KeenEyes.Scenes;

namespace KeenEyes.Editor.Tests.Integration;

#region Test Components for Round-Trip Tests

[Component]
public partial struct PositionComponent
{
    public float X;
    public float Y;
    public float Z;
}

[Component]
public partial struct VelocityComponent
{
    public float VelX;
    public float VelY;
}

[Component]
public partial struct HealthComponent
{
    public int Current;
    public int Max;
}

[Component]
public partial struct Vector3Component
{
    public Vector3 Position;
    public Vector3 Scale;
}

[Component]
public partial struct CollectibleComponent
{
    public int[]? Values;
    public List<string>? Tags;
}

[Component]
public partial struct EntityReferenceComponent
{
    public Entity Target;
}

#endregion

public class SceneRoundTripTests : IDisposable
{
    private readonly string tempDir;

    public SceneRoundTripTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"KeenEyes_RoundTrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #region Entity Hierarchy Round-Trip Tests

    [Fact]
    public void SaveAndLoad_PreservesEntityHierarchy_ThreeLevels()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var grandparent = manager.CreateEntity("Grandparent");
        var parent = manager.CreateEntity("Parent");
        var child = manager.CreateEntity("Child");

        manager.World.SetParent(parent, grandparent);
        manager.World.SetParent(child, parent);

        var filePath = Path.Combine(tempDir, "hierarchy.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var entities = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .ToList();

        Assert.Equal(3, entities.Count);

        var loadedChild = entities.First(e => newManager.World.GetName(e) == "Child");
        var loadedParent = entities.First(e => newManager.World.GetName(e) == "Parent");
        var loadedGrandparent = entities.First(e => newManager.World.GetName(e) == "Grandparent");

        Assert.Equal(loadedParent, newManager.World.GetParent(loadedChild));
        Assert.Equal(loadedGrandparent, newManager.World.GetParent(loadedParent));
        Assert.Equal(Entity.Null, newManager.World.GetParent(loadedGrandparent));
    }

    [Fact]
    public void SaveAndLoad_PreservesMultipleSiblings()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var parent = manager.CreateEntity("Parent");
        var child1 = manager.CreateEntity("Child1");
        var child2 = manager.CreateEntity("Child2");
        var child3 = manager.CreateEntity("Child3");

        manager.World.SetParent(child1, parent);
        manager.World.SetParent(child2, parent);
        manager.World.SetParent(child3, parent);

        var filePath = Path.Combine(tempDir, "siblings.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedParent = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "Parent");

        var children = newManager.World.GetChildren(loadedParent).ToList();
        Assert.Equal(3, children.Count);

        var childNames = children.Select(c => newManager.World.GetName(c)).ToHashSet();
        Assert.Contains("Child1", childNames);
        Assert.Contains("Child2", childNames);
        Assert.Contains("Child3", childNames);
    }

    #endregion

    #region Component Data Round-Trip Tests

    [Fact]
    public void SaveAndLoad_PreservesComponentData_Primitives()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("Player");
        manager.World.Add(entity, new PositionComponent { X = 10.5f, Y = 20.25f, Z = -5.0f });
        manager.World.Add(entity, new HealthComponent { Current = 75, Max = 100 });

        var filePath = Path.Combine(tempDir, "primitives.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedEntity = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "Player");

        Assert.True(newManager.World.Has<PositionComponent>(loadedEntity));
        Assert.True(newManager.World.Has<HealthComponent>(loadedEntity));

        ref readonly var pos = ref newManager.World.Get<PositionComponent>(loadedEntity);
        Assert.Equal(10.5f, pos.X);
        Assert.Equal(20.25f, pos.Y);
        Assert.Equal(-5.0f, pos.Z);

        ref readonly var health = ref newManager.World.Get<HealthComponent>(loadedEntity);
        Assert.Equal(75, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void SaveAndLoad_PreservesComponentData_Vectors()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("Transform");
        manager.World.Add(entity, new Vector3Component
        {
            Position = new Vector3(1.5f, 2.5f, 3.5f),
            Scale = new Vector3(2.0f, 2.0f, 2.0f)
        });

        var filePath = Path.Combine(tempDir, "vectors.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedEntity = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "Transform");

        Assert.True(newManager.World.Has<Vector3Component>(loadedEntity));

        ref readonly var component = ref newManager.World.Get<Vector3Component>(loadedEntity);
        Assert.Equal(new Vector3(1.5f, 2.5f, 3.5f), component.Position);
        Assert.Equal(new Vector3(2.0f, 2.0f, 2.0f), component.Scale);
    }

    [Fact]
    public void SaveAndLoad_PreservesComponentData_Collections()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("Collector");
        manager.World.Add(entity, new CollectibleComponent
        {
            Values = [1, 2, 3, 4, 5],
            Tags = ["tag1", "tag2", "tag3"]
        });

        var filePath = Path.Combine(tempDir, "collections.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedEntity = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "Collector");

        Assert.True(newManager.World.Has<CollectibleComponent>(loadedEntity));

        ref readonly var component = ref newManager.World.Get<CollectibleComponent>(loadedEntity);
        Assert.NotNull(component.Values);
        Assert.Equal([1, 2, 3, 4, 5], component.Values);
        Assert.NotNull(component.Tags);
        Assert.Equal(["tag1", "tag2", "tag3"], component.Tags);
    }

    [Fact]
    public void SaveAndLoad_PreservesMultipleComponentsOnSameEntity()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("MultiComponent");
        manager.World.Add(entity, new PositionComponent { X = 5, Y = 10, Z = 15 });
        manager.World.Add(entity, new VelocityComponent { VelX = 1.5f, VelY = -2.0f });
        manager.World.Add(entity, new HealthComponent { Current = 50, Max = 100 });

        var filePath = Path.Combine(tempDir, "multi.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedEntity = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "MultiComponent");

        Assert.True(newManager.World.Has<PositionComponent>(loadedEntity));
        Assert.True(newManager.World.Has<VelocityComponent>(loadedEntity));
        Assert.True(newManager.World.Has<HealthComponent>(loadedEntity));

        ref readonly var pos = ref newManager.World.Get<PositionComponent>(loadedEntity);
        Assert.Equal(5, pos.X);
        Assert.Equal(10, pos.Y);
        Assert.Equal(15, pos.Z);

        ref readonly var vel = ref newManager.World.Get<VelocityComponent>(loadedEntity);
        Assert.Equal(1.5f, vel.VelX);
        Assert.Equal(-2.0f, vel.VelY);
    }

    #endregion

    #region EditorWorldManager Scene Lifecycle Tests

    [Fact]
    public void NewScene_UnloadsPreviousScene()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        manager.CreateEntity("Entity1");
        manager.CreateEntity("Entity2");
        manager.CreateEntity("Entity3");

        var initialSceneRoot = manager.CurrentSceneRoot;

        // Act
        manager.NewScene();

        // Assert
        Assert.NotEqual(initialSceneRoot, manager.CurrentSceneRoot);
        Assert.True(manager.CurrentSceneRoot.IsValid);

        // Old entities should be gone (scene was unloaded)
        var entities = manager.World.GetAllEntities()
            .Where(e => !manager.World.Has<SceneRootTag>(e))
            .ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void LoadScene_UnloadsPreviousScene()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        manager.CreateEntity("OldEntity1");
        manager.CreateEntity("OldEntity2");

        var filePath = Path.Combine(tempDir, "newscene.kescene");

        // Create and save a different scene
        using var tempManager = new EditorWorldManager();
        tempManager.CreateEntity("NewEntity");
        tempManager.SaveSceneAs(filePath);

        var initialSceneRoot = manager.CurrentSceneRoot;

        // Act
        manager.LoadScene(filePath);

        // Assert
        Assert.NotEqual(initialSceneRoot, manager.CurrentSceneRoot);

        var entities = manager.World.GetAllEntities()
            .Where(e => !manager.World.Has<SceneRootTag>(e))
            .ToList();

        // Only the new entity should exist
        Assert.Single(entities);
        Assert.Equal("NewEntity", manager.World.GetName(entities[0]));
    }

    [Fact]
    public void CloseScene_ClearsCurrentScene()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        manager.CreateEntity("TestEntity");

        // Act
        manager.CloseScene();

        // Assert
        Assert.False(manager.CurrentSceneRoot.IsValid);
        Assert.Null(manager.CurrentScenePath);
    }

    [Fact]
    public void CreateEntity_AddsToCurrentScene()
    {
        // Arrange
        using var manager = new EditorWorldManager();

        // Act
        var entity = manager.CreateEntity("TestEntity");

        // Assert
        Assert.True(entity.IsValid);
        Assert.True(manager.World.Has<SceneMembership>(entity));

        ref readonly var membership = ref manager.World.Get<SceneMembership>(entity);
        Assert.Equal(manager.CurrentSceneRoot.Id, membership.OriginScene.Id);
    }

    #endregion

    #region Multiple Scenes Independence Tests

    [Fact]
    public void MultipleScenes_IndependentEntities()
    {
        // Arrange - Use SceneManager directly
        using var world = new World();

        // Spawn two independent scenes
        var scene1Root = world.Scenes.Spawn("Scene1");
        var scene2Root = world.Scenes.Spawn("Scene2");

        // Create entities in scene 1
        var entity1 = world.Spawn("Entity1").Build();
        world.Scenes.AddToScene(entity1, scene1Root);

        var entity2 = world.Spawn("Entity2").Build();
        world.Scenes.AddToScene(entity2, scene1Root);

        // Create entities in scene 2
        var entity3 = world.Spawn("Entity3").Build();
        world.Scenes.AddToScene(entity3, scene2Root);

        // Act - Unload scene 1
        world.Scenes.Unload(scene1Root);

        // Assert - Only scene 2 entities should remain
        var remainingEntities = world.GetAllEntities()
            .Where(e => !world.Has<SceneRootTag>(e))
            .ToList();

        Assert.Single(remainingEntities);
        Assert.Equal("Entity3", world.GetName(remainingEntities[0]));
    }

    [Fact]
    public void SceneManager_GetScene_ReturnsCorrectRoot()
    {
        // Arrange
        using var world = new World();
        var root = world.Scenes.Spawn("TestScene");

        // Act
        var retrieved = world.Scenes.GetScene("TestScene");

        // Assert
        Assert.Equal(root, retrieved);
    }

    [Fact]
    public void SceneManager_GetLoaded_ReturnsAllScenes()
    {
        // Arrange
        using var world = new World();
        world.Scenes.Spawn("Scene1");
        world.Scenes.Spawn("Scene2");
        world.Scenes.Spawn("Scene3");

        // Act
        var loaded = world.Scenes.GetLoaded().ToList();

        // Assert
        Assert.Equal(3, loaded.Count);
    }

    #endregion

    #region Debug Tests

    [Fact]
    public void Debug_CaptureScene_ProducesCorrectJson()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("Player");
        manager.World.Add(entity, new PositionComponent { X = 10.5f, Y = 20.25f, Z = -5.0f });

        // Verify component was added
        Assert.True(manager.World.Has<PositionComponent>(entity), "Component should be on entity");

        // Get all components
        var components = manager.World.GetComponents(entity).ToList();
        Assert.True(components.Count > 0, $"Entity should have components. Count: {components.Count}");

        // Find our component
        var posComponent = components.FirstOrDefault(c => c.Type == typeof(PositionComponent));
        Assert.True(posComponent.Type is not null, $"PositionComponent should be in GetComponents. Types found: {string.Join(", ", components.Select(c => c.Type.Name))}");

        var filePath = Path.Combine(tempDir, "debug.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        // Assert - check the JSON content has camelCase field names
        var json = File.ReadAllText(filePath);
        Assert.Contains("\"x\"", json); // Field should be serialized in camelCase
        Assert.Contains("10.5", json);  // Value should be present
        Assert.Contains("20.25", json);
        Assert.Contains("-5", json);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SaveAndLoad_EmptyScene_Works()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var filePath = Path.Combine(tempDir, "empty.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var entities = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void SaveAndLoad_NullValuesInComponents_Handled()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        var entity = manager.CreateEntity("NullTest");
        manager.World.Add(entity, new CollectibleComponent
        {
            Values = null,
            Tags = null
        });

        var filePath = Path.Combine(tempDir, "nulls.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);

        // Assert
        var loadedEntity = newManager.World.GetAllEntities()
            .Where(e => !newManager.World.Has<SceneRootTag>(e))
            .First(e => newManager.World.GetName(e) == "NullTest");

        Assert.True(newManager.World.Has<CollectibleComponent>(loadedEntity));

        ref readonly var component = ref newManager.World.Get<CollectibleComponent>(loadedEntity);
        Assert.Null(component.Values);
        Assert.Null(component.Tags);
    }

    [Fact]
    public void SaveAndLoad_PreservesScenePath()
    {
        // Arrange
        using var manager = new EditorWorldManager();
        manager.CreateEntity("TestEntity");
        var filePath = Path.Combine(tempDir, "path_test.kescene");

        // Act
        manager.SaveSceneAs(filePath);

        // Assert
        Assert.Equal(filePath, manager.CurrentScenePath);

        // Load into new manager and verify path
        using var newManager = new EditorWorldManager();
        newManager.LoadScene(filePath);
        Assert.Equal(filePath, newManager.CurrentScenePath);
    }

    [Fact]
    public void HasUnsavedChanges_TracksModifications()
    {
        // Arrange
        using var manager = new EditorWorldManager();

        // Initially no unsaved changes
        Assert.False(manager.HasUnsavedChanges);

        // Act - Create entity
        manager.CreateEntity("TestEntity");

        // Assert - Now has unsaved changes
        Assert.True(manager.HasUnsavedChanges);

        // Save scene
        var filePath = Path.Combine(tempDir, "changes.kescene");
        manager.SaveSceneAs(filePath);

        // Assert - No longer has unsaved changes
        Assert.False(manager.HasUnsavedChanges);
    }

    #endregion
}
