using KeenEyes;
using KeenEyes.Editor.Assets;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Integration.Tests;

/// <summary>
/// Integration tests for complete scene workflows including
/// creation, modification, saving, and loading.
/// </summary>
public class SceneWorkflowTests : IDisposable
{
    private readonly string tempDir;

    public SceneWorkflowTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"KeenEyes_Integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }


    #region Scene Creation and Save/Load

    [Fact]
    public void CreateScene_AddEntities_SaveReload_PreservesEntityCount()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "scene.kescene");

        // Create and populate scene
        using (var world = new World())
        {
            world.Spawn("Player").Build();
            world.Spawn("Enemy1").Build();
            world.Spawn("Enemy2").Build();
            world.Spawn("Item").Build();

            serializer.Save(world, "GameLevel", filePath);
        }

        // Load into new world
        using var loadedWorld = new World();
        var sceneName = SceneSerializer.Load(loadedWorld, filePath);

        Assert.Equal("GameLevel", sceneName);
        Assert.Equal(4, loadedWorld.EntityCount);
    }

    [Fact]
    public void CreateScene_AddEntities_SaveReload_PreservesEntityNames()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "scene.kescene");

        // Create and populate scene
        using (var world = new World())
        {
            world.Spawn("Hero").Build();
            world.Spawn("Villain").Build();
            world.Spawn("NPC").Build();

            serializer.Save(world, "TestScene", filePath);
        }

        // Load into new world
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        var names = loadedWorld.GetAllEntities()
            .Select(e => loadedWorld.GetName(e))
            .Where(n => n is not null)
            .ToHashSet();

        Assert.Contains("Hero", names);
        Assert.Contains("Villain", names);
        Assert.Contains("NPC", names);
    }

    [Fact]
    public void CreateScene_WithHierarchy_SaveReload_PreservesParentChild()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "hierarchy.kescene");

        // Create hierarchy
        using (var world = new World())
        {
            var parent = world.Spawn("Parent").Build();
            var child1 = world.Spawn("Child1").Build();
            var child2 = world.Spawn("Child2").Build();
            world.SetParent(child1, parent);
            world.SetParent(child2, parent);

            serializer.Save(world, "HierarchyScene", filePath);
        }

        // Load and verify
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        var loadedParent = loadedWorld.GetAllEntities()
            .First(e => loadedWorld.GetName(e) == "Parent");
        var loadedChild1 = loadedWorld.GetAllEntities()
            .First(e => loadedWorld.GetName(e) == "Child1");
        var loadedChild2 = loadedWorld.GetAllEntities()
            .First(e => loadedWorld.GetName(e) == "Child2");

        Assert.Equal(loadedParent, loadedWorld.GetParent(loadedChild1));
        Assert.Equal(loadedParent, loadedWorld.GetParent(loadedChild2));
    }

    [Fact]
    public void CreateScene_DeepHierarchy_SaveReload_PreservesAllLevels()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "deep.kescene");

        // Create 4-level hierarchy
        using (var world = new World())
        {
            var root = world.Spawn("Root").Build();
            var level1 = world.Spawn("Level1").Build();
            var level2 = world.Spawn("Level2").Build();
            var level3 = world.Spawn("Level3").Build();

            world.SetParent(level1, root);
            world.SetParent(level2, level1);
            world.SetParent(level3, level2);

            serializer.Save(world, "DeepScene", filePath);
        }

        // Load and verify chain
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        var entities = loadedWorld.GetAllEntities().ToList();
        var loadedRoot = entities.First(e => loadedWorld.GetName(e) == "Root");
        var loadedLevel1 = entities.First(e => loadedWorld.GetName(e) == "Level1");
        var loadedLevel2 = entities.First(e => loadedWorld.GetName(e) == "Level2");
        var loadedLevel3 = entities.First(e => loadedWorld.GetName(e) == "Level3");

        Assert.Equal(Entity.Null, loadedWorld.GetParent(loadedRoot));
        Assert.Equal(loadedRoot, loadedWorld.GetParent(loadedLevel1));
        Assert.Equal(loadedLevel1, loadedWorld.GetParent(loadedLevel2));
        Assert.Equal(loadedLevel2, loadedWorld.GetParent(loadedLevel3));
    }

    #endregion

    #region Scene Modification Workflows

    [Fact]
    public void ModifyScene_AddEntities_SaveReload_IncludesNewEntities()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "modified.kescene");

        // Create initial scene
        using (var world = new World())
        {
            world.Spawn("Initial").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load, modify, and save
        using (var world = new World())
        {
            SceneSerializer.Load(world, filePath);
            world.Spawn("Added").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load final state
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        Assert.Equal(2, loadedWorld.EntityCount);
        var names = loadedWorld.GetAllEntities()
            .Select(e => loadedWorld.GetName(e))
            .ToHashSet();
        Assert.Contains("Initial", names);
        Assert.Contains("Added", names);
    }

    [Fact]
    public void ModifyScene_RenameEntity_SaveReload_PreservesNewName()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "renamed.kescene");

        // Create and save
        using (var world = new World())
        {
            world.Spawn("OriginalName").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load, rename, and save
        using (var world = new World())
        {
            SceneSerializer.Load(world, filePath);
            var entity = world.GetAllEntities().First();
            var renameCommand = new RenameEntityCommand(world, entity, "NewName");
            renameCommand.Execute();
            serializer.Save(world, "Scene", filePath);
        }

        // Load final state
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        var name = loadedWorld.GetName(loadedWorld.GetAllEntities().First());
        Assert.Equal("NewName", name);
    }

    [Fact]
    public void ModifyScene_DeleteEntity_SaveReload_EntityGone()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "deleted.kescene");

        // Create scene with multiple entities
        using (var world = new World())
        {
            world.Spawn("Keep").Build();
            world.Spawn("Delete").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load, delete one, and save
        using (var world = new World())
        {
            SceneSerializer.Load(world, filePath);
            var toDelete = world.GetAllEntities()
                .First(e => world.GetName(e) == "Delete");
            world.Despawn(toDelete);
            serializer.Save(world, "Scene", filePath);
        }

        // Load final state
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        Assert.Equal(1, loadedWorld.EntityCount);
        var name = loadedWorld.GetName(loadedWorld.GetAllEntities().First());
        Assert.Equal("Keep", name);
    }

    [Fact]
    public void ModifyScene_ReparentEntity_SaveReload_PreservesNewParent()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "reparented.kescene");

        // Create initial hierarchy
        using (var world = new World())
        {
            var oldParent = world.Spawn("OldParent").Build();
            var newParent = world.Spawn("NewParent").Build();
            var child = world.Spawn("Child").Build();
            world.SetParent(child, oldParent);
            serializer.Save(world, "Scene", filePath);
        }

        // Load, reparent, and save
        using (var world = new World())
        {
            SceneSerializer.Load(world, filePath);
            var entities = world.GetAllEntities().ToList();
            var newParent = entities.First(e => world.GetName(e) == "NewParent");
            var child = entities.First(e => world.GetName(e) == "Child");
            world.SetParent(child, newParent);
            serializer.Save(world, "Scene", filePath);
        }

        // Load final state
        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        var loadedEntities = loadedWorld.GetAllEntities().ToList();
        var loadedNewParent = loadedEntities.First(e => loadedWorld.GetName(e) == "NewParent");
        var loadedChild = loadedEntities.First(e => loadedWorld.GetName(e) == "Child");

        Assert.Equal(loadedNewParent, loadedWorld.GetParent(loadedChild));
    }

    #endregion

    #region Command Integration with Scene Save/Load

    [Fact]
    public void ExecuteCommands_SaveScene_UndoAll_SaveAgain_DifferentState()
    {
        var serializer = new SceneSerializer();
        var afterCommandsPath = Path.Combine(tempDir, "after_commands.kescene");
        var afterUndoPath = Path.Combine(tempDir, "after_undo.kescene");

        using var world = new World();
        var manager = new UndoRedoManager();

        // Initial state
        manager.Execute(new CreateEntityCommand(world, "Entity1"));
        manager.Execute(new CreateEntityCommand(world, "Entity2"));
        manager.Execute(new CreateEntityCommand(world, "Entity3"));

        // Save after commands
        serializer.Save(world, "AfterCommands", afterCommandsPath);
        var afterCommandsCount = world.EntityCount;

        // Undo all
        manager.Undo();
        manager.Undo();
        manager.Undo();

        // Save after undo
        serializer.Save(world, "AfterUndo", afterUndoPath);

        // Verify different states were saved
        using var loadedAfterCommands = new World();
        using var loadedAfterUndo = new World();

        SceneSerializer.Load(loadedAfterCommands, afterCommandsPath);
        SceneSerializer.Load(loadedAfterUndo, afterUndoPath);

        Assert.Equal(3, loadedAfterCommands.EntityCount);
        Assert.Equal(0, loadedAfterUndo.EntityCount);
    }

    [Fact]
    public void SaveScene_LoadIntoNewWorld_ContinueEditing_WorksCorrectly()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "continue.kescene");

        // Create initial scene
        using (var world = new World())
        {
            world.Spawn("Existing").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load and continue editing with commands
        using var loadedWorld = new World();
        var manager = new UndoRedoManager();

        SceneSerializer.Load(loadedWorld, filePath);

        // Add more entities via commands
        manager.Execute(new CreateEntityCommand(loadedWorld, "New1"));
        manager.Execute(new CreateEntityCommand(loadedWorld, "New2"));

        Assert.Equal(3, loadedWorld.EntityCount);

        // Undo should work on the new commands
        manager.Undo();
        Assert.Equal(2, loadedWorld.EntityCount);

        manager.Undo();
        Assert.Equal(1, loadedWorld.EntityCount);

        // Original entity should still exist
        var name = loadedWorld.GetName(loadedWorld.GetAllEntities().First());
        Assert.Equal("Existing", name);
    }

    #endregion

    #region Multiple Save/Load Cycles

    [Fact]
    public void MultipleSaveLoadCycles_PreservesIntegrity()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "cycles.kescene");

        // Initial creation
        using (var world = new World())
        {
            var parent = world.Spawn("Parent").Build();
            var child = world.Spawn("Child").Build();
            world.SetParent(child, parent);
            serializer.Save(world, "CycleTest", filePath);
        }

        // Multiple load/modify/save cycles
        for (int i = 0; i < 5; i++)
        {
            using var world = new World();
            SceneSerializer.Load(world, filePath);

            // Verify structure is intact
            var entities = world.GetAllEntities().ToList();
            var parent = entities.First(e => world.GetName(e) == "Parent");
            var child = entities.First(e => world.GetName(e) == "Child");
            Assert.Equal(parent, world.GetParent(child));

            // Save again
            serializer.Save(world, "CycleTest", filePath);
        }

        // Final verification
        using var finalWorld = new World();
        SceneSerializer.Load(finalWorld, filePath);
        Assert.Equal(2, finalWorld.EntityCount);
    }

    [Fact]
    public void SaveScene_LoadMultipleTimes_IndependentWorlds()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "multiload.kescene");

        // Create and save
        using (var world = new World())
        {
            world.Spawn("Shared").Build();
            serializer.Save(world, "Shared", filePath);
        }

        // Load into two independent worlds
        using var world1 = new World();
        using var world2 = new World();

        SceneSerializer.Load(world1, filePath);
        SceneSerializer.Load(world2, filePath);

        // Modify world1 only
        world1.Spawn("OnlyInWorld1").Build();

        Assert.Equal(2, world1.EntityCount);
        Assert.Equal(1, world2.EntityCount);

        // Modify world2 differently
        world2.Despawn(world2.GetAllEntities().First());

        Assert.Equal(2, world1.EntityCount);
        Assert.Equal(0, world2.EntityCount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void SaveEmptyScene_LoadScene_WorldIsEmpty()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "empty.kescene");

        using (var world = new World())
        {
            serializer.Save(world, "Empty", filePath);
        }

        using var loadedWorld = new World();
        var sceneName = SceneSerializer.Load(loadedWorld, filePath);

        Assert.Equal("Empty", sceneName);
        Assert.Equal(0, loadedWorld.EntityCount);
    }

    [Fact]
    public void LoadScene_IntoPopulatedWorld_ClearsExisting()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "clear.kescene");

        // Create scene with 1 entity
        using (var world = new World())
        {
            world.Spawn("FromFile").Build();
            serializer.Save(world, "Scene", filePath);
        }

        // Load into world with existing entities
        using var loadedWorld = new World();
        loadedWorld.Spawn("Existing1").Build();
        loadedWorld.Spawn("Existing2").Build();
        loadedWorld.Spawn("Existing3").Build();

        Assert.Equal(3, loadedWorld.EntityCount);

        SceneSerializer.Load(loadedWorld, filePath);

        // Should only have entity from file
        Assert.Equal(1, loadedWorld.EntityCount);
        var name = loadedWorld.GetName(loadedWorld.GetAllEntities().First());
        Assert.Equal("FromFile", name);
    }

    [Fact]
    public void SaveScene_EntityWithNoName_PreservesEntity()
    {
        var serializer = new SceneSerializer();
        var filePath = Path.Combine(tempDir, "unnamed.kescene");

        using (var world = new World())
        {
            world.Spawn("Named").Build();
            world.Spawn().Build(); // Unnamed entity
            serializer.Save(world, "Scene", filePath);
        }

        using var loadedWorld = new World();
        SceneSerializer.Load(loadedWorld, filePath);

        Assert.Equal(2, loadedWorld.EntityCount);

        var names = loadedWorld.GetAllEntities()
            .Select(e => loadedWorld.GetName(e))
            .ToList();

        Assert.Contains("Named", names);
        Assert.Contains(null, names);
    }

    #endregion
}
