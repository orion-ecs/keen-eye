using KeenEyes;
using KeenEyes.Editor.Assets;

namespace KeenEyes.Editor.Tests.Assets;

public class SceneSerializerTests : IDisposable
{
    private readonly World world;
    private readonly SceneSerializer serializer;
    private readonly string tempDir;

    public SceneSerializerTests()
    {
        world = new World();
        serializer = new SceneSerializer();
        tempDir = Path.Combine(Path.GetTempPath(), $"KeenEyes_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        world.Dispose();
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #region CaptureScene Tests

    [Fact]
    public void CaptureScene_EmptyWorld_ReturnsEmptyEntityList()
    {
        var sceneData = serializer.CaptureScene(world, "TestScene");

        Assert.Equal("TestScene", sceneData.Name);
        Assert.Equal(1, sceneData.Version);
        Assert.Empty(sceneData.Entities);
    }

    [Fact]
    public void CaptureScene_SingleEntity_CapturesName()
    {
        world.Spawn("Player").Build();

        var sceneData = serializer.CaptureScene(world, "TestScene");

        Assert.Single(sceneData.Entities);
        Assert.Equal("Player", sceneData.Entities[0].Name);
        Assert.Equal("Player", sceneData.Entities[0].Id);
    }

    [Fact]
    public void CaptureScene_UnnamedEntity_GeneratesId()
    {
        world.Spawn().Build();

        var sceneData = serializer.CaptureScene(world, "TestScene");

        Assert.Single(sceneData.Entities);
        Assert.Null(sceneData.Entities[0].Name);
        Assert.StartsWith("entity_", sceneData.Entities[0].Id);
    }

    [Fact]
    public void CaptureScene_MultipleEntities_CapturesAll()
    {
        world.Spawn("Entity1").Build();
        world.Spawn("Entity2").Build();
        world.Spawn("Entity3").Build();

        var sceneData = serializer.CaptureScene(world, "TestScene");

        Assert.Equal(3, sceneData.Entities.Count);
    }

    [Fact]
    public void CaptureScene_ParentChild_CapturesHierarchy()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        var sceneData = serializer.CaptureScene(world, "TestScene");

        var childData = sceneData.Entities.First(e => e.Name == "Child");
        Assert.Equal("Parent", childData.Parent);
    }

    [Fact]
    public void CaptureScene_RootEntity_HasNullParent()
    {
        world.Spawn("Root").Build();

        var sceneData = serializer.CaptureScene(world, "TestScene");

        Assert.Null(sceneData.Entities[0].Parent);
    }

    [Fact]
    public void CaptureScene_DeepHierarchy_CapturesAllLevels()
    {
        var grandparent = world.Spawn("Grandparent").Build();
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var sceneData = serializer.CaptureScene(world, "TestScene");

        var parentData = sceneData.Entities.First(e => e.Name == "Parent");
        var childData = sceneData.Entities.First(e => e.Name == "Child");

        Assert.Equal("Grandparent", parentData.Parent);
        Assert.Equal("Parent", childData.Parent);
    }

    #endregion

    #region RestoreScene Tests

    [Fact]
    public void RestoreScene_EmptyScene_ClearsWorld()
    {
        world.Spawn("Existing").Build();
        var sceneData = new SceneData { Name = "Empty", Entities = [] };

        SceneSerializer.RestoreScene(world, sceneData);

        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void RestoreScene_SingleEntity_CreatesEntity()
    {
        var sceneData = new SceneData
        {
            Name = "Test",
            Entities =
            [
                new EntityData { Id = "player", Name = "Player" }
            ]
        };

        SceneSerializer.RestoreScene(world, sceneData);

        Assert.Equal(1, world.EntityCount);
        var entities = world.GetAllEntities().ToList();
        Assert.Equal("Player", world.GetName(entities[0]));
    }

    [Fact]
    public void RestoreScene_MultipleEntities_CreatesAll()
    {
        var sceneData = new SceneData
        {
            Name = "Test",
            Entities =
            [
                new EntityData { Id = "e1", Name = "Entity1" },
                new EntityData { Id = "e2", Name = "Entity2" },
                new EntityData { Id = "e3", Name = "Entity3" }
            ]
        };

        SceneSerializer.RestoreScene(world, sceneData);

        Assert.Equal(3, world.EntityCount);
    }

    [Fact]
    public void RestoreScene_Hierarchy_RestoresParentChild()
    {
        var sceneData = new SceneData
        {
            Name = "Test",
            Entities =
            [
                new EntityData { Id = "parent", Name = "Parent" },
                new EntityData { Id = "child", Name = "Child", Parent = "parent" }
            ]
        };

        SceneSerializer.RestoreScene(world, sceneData);

        var entities = world.GetAllEntities().ToList();
        var parent = entities.First(e => world.GetName(e) == "Parent");
        var child = entities.First(e => world.GetName(e) == "Child");

        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void RestoreScene_MissingParent_SkipsParenting()
    {
        var sceneData = new SceneData
        {
            Name = "Test",
            Entities =
            [
                new EntityData { Id = "child", Name = "Child", Parent = "nonexistent" }
            ]
        };

        SceneSerializer.RestoreScene(world, sceneData);

        var child = world.GetAllEntities().First();
        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    [Fact]
    public void RestoreScene_ClearsExistingEntities()
    {
        world.Spawn("Old1").Build();
        world.Spawn("Old2").Build();

        var sceneData = new SceneData
        {
            Name = "Test",
            Entities =
            [
                new EntityData { Id = "new", Name = "New" }
            ]
        };

        SceneSerializer.RestoreScene(world, sceneData);

        Assert.Equal(1, world.EntityCount);
        var entity = world.GetAllEntities().First();
        Assert.Equal("New", world.GetName(entity));
    }

    #endregion

    #region Save/Load Tests

    [Fact]
    public void Save_CreatesFile()
    {
        world.Spawn("TestEntity").Build();
        var filePath = Path.Combine(tempDir, "test.kescene");

        serializer.Save(world, "TestScene", filePath);

        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void Save_WritesValidJson()
    {
        world.Spawn("TestEntity").Build();
        var filePath = Path.Combine(tempDir, "test.kescene");

        serializer.Save(world, "TestScene", filePath);

        var json = File.ReadAllText(filePath);
        var parsed = System.Text.Json.JsonDocument.Parse(json);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void Load_ReturnsSceneName()
    {
        var sourceWorld = new World();
        sourceWorld.Spawn("Entity").Build();
        var filePath = Path.Combine(tempDir, "test.kescene");
        serializer.Save(sourceWorld, "MyScene", filePath);
        sourceWorld.Dispose();

        var loadedName = SceneSerializer.Load(world, filePath);

        Assert.Equal("MyScene", loadedName);
    }

    [Fact]
    public void Load_RestoresEntities()
    {
        var sourceWorld = new World();
        sourceWorld.Spawn("Entity1").Build();
        sourceWorld.Spawn("Entity2").Build();
        var filePath = Path.Combine(tempDir, "test.kescene");
        serializer.Save(sourceWorld, "TestScene", filePath);
        sourceWorld.Dispose();

        SceneSerializer.Load(world, filePath);

        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void Load_ThrowsOnInvalidFile()
    {
        var filePath = Path.Combine(tempDir, "invalid.kescene");
        File.WriteAllText(filePath, "not valid json {{{");

        Assert.ThrowsAny<Exception>(() => SceneSerializer.Load(world, filePath));
    }

    [Fact]
    public void Load_ThrowsOnMissingFile()
    {
        var filePath = Path.Combine(tempDir, "nonexistent.kescene");

        Assert.Throws<FileNotFoundException>(() => SceneSerializer.Load(world, filePath));
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_PreservesEntityNames()
    {
        world.Spawn("Alpha").Build();
        world.Spawn("Beta").Build();
        world.Spawn("Gamma").Build();
        var filePath = Path.Combine(tempDir, "test.kescene");

        serializer.Save(world, "Test", filePath);

        var newWorld = new World();
        SceneSerializer.Load(newWorld, filePath);

        var names = newWorld.GetAllEntities()
            .Select(e => newWorld.GetName(e))
            .Where(n => n is not null)
            .ToHashSet();

        Assert.Contains("Alpha", names);
        Assert.Contains("Beta", names);
        Assert.Contains("Gamma", names);

        newWorld.Dispose();
    }

    [Fact]
    public void RoundTrip_PreservesHierarchy()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);
        var filePath = Path.Combine(tempDir, "test.kescene");

        serializer.Save(world, "Test", filePath);

        var newWorld = new World();
        SceneSerializer.Load(newWorld, filePath);

        var loadedChild = newWorld.GetAllEntities()
            .First(e => newWorld.GetName(e) == "Child");
        var loadedParent = newWorld.GetParent(loadedChild);

        Assert.True(loadedParent.IsValid);
        Assert.Equal("Parent", newWorld.GetName(loadedParent));

        newWorld.Dispose();
    }

    [Fact]
    public void RoundTrip_ViaMemory_PreservesData()
    {
        world.Spawn("Entity1").Build();
        world.Spawn("Entity2").Build();

        var sceneData = serializer.CaptureScene(world, "TestScene");

        var newWorld = new World();
        SceneSerializer.RestoreScene(newWorld, sceneData);

        Assert.Equal(world.EntityCount, newWorld.EntityCount);

        newWorld.Dispose();
    }

    #endregion
}
