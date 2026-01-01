using KeenEyes;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Integration.Tests;

/// <summary>
/// Integration tests for complex undo/redo scenarios across
/// multiple command types and world operations.
/// </summary>
public class UndoRedoIntegrationTests : IDisposable
{
    private readonly World world;
    private readonly UndoRedoManager manager;

    public UndoRedoIntegrationTests()
    {
        world = new World();
        manager = new UndoRedoManager();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Test Components

    private struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Health : IComponent
    {
        public int Current;
        public int Max;
    }

    #endregion

    #region Mixed Command Types

    [Fact]
    public void MixedCommands_CreateRenameModify_UndoAll_RestoresOriginalState()
    {
        // Start empty
        Assert.Equal(0, world.EntityCount);

        // Create entity
        manager.Execute(new CreateEntityCommand(world, "Player"));
        var player = world.GetAllEntities().First();
        Assert.Equal(1, world.EntityCount);
        Assert.Equal("Player", world.GetName(player));

        // Add component
        world.Add(player, new Position { X = 0, Y = 0 });

        // Rename entity
        manager.Execute(new RenameEntityCommand(world, player, "Hero"));
        Assert.Equal("Hero", world.GetName(player));

        // Modify component
        manager.Execute(new SetComponentCommand<Position>(world, player, new Position { X = 10, Y = 20 }));
        Assert.Equal(10, world.Get<Position>(player).X);
        Assert.Equal(20, world.Get<Position>(player).Y);

        // Undo all (in reverse order)
        manager.Undo(); // Undo position change
        Assert.Equal(0, world.Get<Position>(player).X);
        Assert.Equal(0, world.Get<Position>(player).Y);

        manager.Undo(); // Undo rename
        Assert.Equal("Player", world.GetName(player));

        manager.Undo(); // Undo create
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void CreateMultipleEntities_ModifyEach_UndoInOrder()
    {
        // Create 3 entities
        manager.Execute(new CreateEntityCommand(world, "Entity1"));
        var entity1 = world.GetAllEntities().Last();

        manager.Execute(new CreateEntityCommand(world, "Entity2"));
        var entity2 = world.GetAllEntities().Last();

        manager.Execute(new CreateEntityCommand(world, "Entity3"));
        var entity3 = world.GetAllEntities().Last();

        // Rename each
        manager.Execute(new RenameEntityCommand(world, entity1, "A"));
        manager.Execute(new RenameEntityCommand(world, entity2, "B"));
        manager.Execute(new RenameEntityCommand(world, entity3, "C"));

        Assert.Equal("A", world.GetName(entity1));
        Assert.Equal("B", world.GetName(entity2));
        Assert.Equal("C", world.GetName(entity3));

        // Undo renames
        manager.Undo(); // Entity3 rename
        Assert.Equal("Entity3", world.GetName(entity3));

        manager.Undo(); // Entity2 rename
        Assert.Equal("Entity2", world.GetName(entity2));

        manager.Undo(); // Entity1 rename
        Assert.Equal("Entity1", world.GetName(entity1));

        // Undo creates
        manager.Undo();
        Assert.Equal(2, world.EntityCount);

        manager.Undo();
        Assert.Equal(1, world.EntityCount);

        manager.Undo();
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void CreateHierarchy_ModifyComponents_UndoAll_RestoresEmpty()
    {
        // Create hierarchy
        manager.Execute(new CreateEntityCommand(world, "Parent"));
        var parent = world.GetAllEntities().Last();

        manager.Execute(new CreateEntityCommand(world, "Child"));
        var child = world.GetAllEntities().Last();

        manager.Execute(new ReparentEntityCommand(world, child, parent));

        // Add and modify components
        world.Add(parent, new Position { X = 0, Y = 0 });
        manager.Execute(new SetComponentCommand<Position>(world, parent, new Position { X = 5, Y = 5 }));

        Assert.Equal(2, world.EntityCount);
        Assert.Equal(parent, world.GetParent(child));
        Assert.Equal(5, world.Get<Position>(parent).X);

        // Undo everything
        manager.Undo(); // Position change
        Assert.Equal(0, world.Get<Position>(parent).X);

        manager.Undo(); // Reparent
        Assert.Equal(Entity.Null, world.GetParent(child));

        manager.Undo(); // Create child
        Assert.Equal(1, world.EntityCount);

        manager.Undo(); // Create parent
        Assert.Equal(0, world.EntityCount);
    }

    #endregion

    #region Redo After Partial Undo

    [Fact]
    public void PartialUndo_ThenRedo_RestoresIntermediateState()
    {
        // Create entity directly (not via command) so we can focus on component changes
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new Position { X = 0, Y = 0 });

        // Use separate component types to prevent merging
        world.Add(entity, new Velocity { X = 0, Y = 0 });
        world.Add(entity, new Health { Current = 100, Max = 100 });

        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 10, Y = 0 }));
        manager.Execute(new SetComponentCommand<Velocity>(world, entity, new Velocity { X = 5, Y = 0 }));
        manager.Execute(new SetComponentCommand<Health>(world, entity, new Health { Current = 50, Max = 100 }));

        Assert.Equal(10, world.Get<Position>(entity).X);
        Assert.Equal(5, world.Get<Velocity>(entity).X);
        Assert.Equal(50, world.Get<Health>(entity).Current);

        // Undo twice (Health, then Velocity)
        manager.Undo(); // Undo Health change
        manager.Undo(); // Undo Velocity change

        Assert.Equal(10, world.Get<Position>(entity).X);
        Assert.Equal(0, world.Get<Velocity>(entity).X);
        Assert.Equal(100, world.Get<Health>(entity).Current);

        // Redo once (Velocity)
        manager.Redo();
        Assert.Equal(5, world.Get<Velocity>(entity).X);

        // Redo again (Health)
        manager.Redo();
        Assert.Equal(50, world.Get<Health>(entity).Current);
    }

    [Fact]
    public void UndoThenNewCommand_ClearsRedoStack()
    {
        manager.Execute(new CreateEntityCommand(world, "Entity1"));
        manager.Execute(new CreateEntityCommand(world, "Entity2"));

        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);

        manager.Undo(); // Undo Entity2

        Assert.True(manager.CanUndo);
        Assert.True(manager.CanRedo);

        // New command should clear redo
        manager.Execute(new CreateEntityCommand(world, "Entity3"));

        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void MultipleUndoRedo_NavigatesHistory()
    {
        manager.Execute(new CreateEntityCommand(world, "A"));
        manager.Execute(new CreateEntityCommand(world, "B"));
        manager.Execute(new CreateEntityCommand(world, "C"));

        Assert.Equal(3, world.EntityCount);

        // Undo all
        manager.Undo();
        manager.Undo();
        manager.Undo();
        Assert.Equal(0, world.EntityCount);
        Assert.False(manager.CanUndo);

        // Redo all
        manager.Redo();
        manager.Redo();
        manager.Redo();
        Assert.Equal(3, world.EntityCount);
        Assert.False(manager.CanRedo);
    }

    #endregion

    #region Batch Commands Integration

    [Fact]
    public void BatchWithMixedCommands_UndoAsUnit()
    {
        manager.Execute(new CreateEntityCommand(world, "Existing"));
        var existing = world.GetAllEntities().First();
        world.Add(existing, new Position { X = 0, Y = 0 });

        Assert.Equal(1, world.EntityCount);

        manager.BeginBatch("Complex operation");
        manager.Execute(new CreateEntityCommand(world, "New"));
        manager.Execute(new RenameEntityCommand(world, existing, "Modified"));
        manager.Execute(new SetComponentCommand<Position>(world, existing, new Position { X = 100, Y = 100 }));
        manager.EndBatch();

        Assert.Equal(2, world.EntityCount);
        Assert.Equal("Modified", world.GetName(existing));
        Assert.Equal(100, world.Get<Position>(existing).X);

        // Single undo should revert entire batch
        manager.Undo();

        Assert.Equal(1, world.EntityCount);
        Assert.Equal("Existing", world.GetName(existing));
        Assert.Equal(0, world.Get<Position>(existing).X);
    }

    [Fact]
    public void NestedOperationsInBatch_UndoRestoresAll()
    {
        var parent = world.Spawn("Parent").Build();
        world.Add(parent, new Position { X = 0, Y = 0 });
        world.Add(parent, new Velocity { X = 1, Y = 1 });

        manager.BeginBatch("Update all components");
        manager.Execute(new SetComponentCommand<Position>(world, parent, new Position { X = 10, Y = 10 }));
        manager.Execute(new SetComponentCommand<Velocity>(world, parent, new Velocity { X = 5, Y = 5 }));
        manager.Execute(new RenameEntityCommand(world, parent, "UpdatedParent"));
        manager.EndBatch();

        Assert.Equal(10, world.Get<Position>(parent).X);
        Assert.Equal(5, world.Get<Velocity>(parent).X);
        Assert.Equal("UpdatedParent", world.GetName(parent));

        manager.Undo();

        Assert.Equal(0, world.Get<Position>(parent).X);
        Assert.Equal(1, world.Get<Velocity>(parent).X);
        Assert.Equal("Parent", world.GetName(parent));
    }

    [Fact]
    public void MultipleBatches_IndependentUndo()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new Position { X = 0, Y = 0 });

        // Batch 1
        manager.BeginBatch("Batch 1");
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 10, Y = 10 }));
        manager.Execute(new RenameEntityCommand(world, entity, "Batch1Result"));
        manager.EndBatch();

        // Batch 2
        manager.BeginBatch("Batch 2");
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 20, Y = 20 }));
        manager.Execute(new RenameEntityCommand(world, entity, "Batch2Result"));
        manager.EndBatch();

        Assert.Equal("Batch2Result", world.GetName(entity));
        Assert.Equal(20, world.Get<Position>(entity).X);

        // Undo batch 2
        manager.Undo();
        Assert.Equal("Batch1Result", world.GetName(entity));
        Assert.Equal(10, world.Get<Position>(entity).X);

        // Undo batch 1
        manager.Undo();
        Assert.Equal("Entity", world.GetName(entity));
        Assert.Equal(0, world.Get<Position>(entity).X);
    }

    #endregion

    #region Entity Lifecycle with Undo

    [Fact]
    public void DeleteEntity_Undo_RestoresEntity()
    {
        manager.Execute(new CreateEntityCommand(world, "ToDelete"));
        var entity = world.GetAllEntities().First();
        world.Add(entity, new Position { X = 42, Y = 24 });

        manager.Execute(new DeleteEntityCommand(world, entity));
        Assert.Equal(0, world.EntityCount);

        manager.Undo();
        Assert.Equal(1, world.EntityCount);

        // Entity should be restored with its name
        var restored = world.GetAllEntities().First();
        Assert.Equal("ToDelete", world.GetName(restored));
    }

    [Fact]
    public void DeleteEntity_Undo_RestoresEntityWithName()
    {
        // Create entity directly
        var first = world.Spawn("First").Build();
        Assert.Equal(1, world.EntityCount);

        // Delete entity via command
        manager.Execute(new DeleteEntityCommand(world, first));
        Assert.Equal(0, world.EntityCount);

        // Undo delete - should restore the entity with same name
        manager.Undo();
        Assert.Equal(1, world.EntityCount);
        Assert.Equal("First", world.GetName(world.GetAllEntities().First()));
    }

    [Fact]
    public void MultipleCreates_UndoAll_RestoresEmpty()
    {
        manager.Execute(new CreateEntityCommand(world, "First"));
        manager.Execute(new CreateEntityCommand(world, "Second"));
        manager.Execute(new CreateEntityCommand(world, "Third"));

        Assert.Equal(3, world.EntityCount);

        // Undo all creates
        manager.Undo(); // Remove Third
        Assert.Equal(2, world.EntityCount);

        manager.Undo(); // Remove Second
        Assert.Equal(1, world.EntityCount);

        manager.Undo(); // Remove First
        Assert.Equal(0, world.EntityCount);

        // Redo all creates
        manager.Redo();
        Assert.Equal(1, world.EntityCount);

        manager.Redo();
        Assert.Equal(2, world.EntityCount);

        manager.Redo();
        Assert.Equal(3, world.EntityCount);
    }

    #endregion

    #region Component Modification Chains

    [Fact]
    public void ModifyMultipleComponents_SameEntity_IndependentUndo()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new Position { X = 0, Y = 0 });
        world.Add(entity, new Velocity { X = 0, Y = 0 });
        world.Add(entity, new Health { Current = 100, Max = 100 });

        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 10, Y = 10 }));
        manager.Execute(new SetComponentCommand<Velocity>(world, entity, new Velocity { X = 5, Y = 5 }));
        manager.Execute(new SetComponentCommand<Health>(world, entity, new Health { Current = 50, Max = 100 }));

        // Undo health change only
        manager.Undo();
        Assert.Equal(100, world.Get<Health>(entity).Current);
        Assert.Equal(5, world.Get<Velocity>(entity).X);
        Assert.Equal(10, world.Get<Position>(entity).X);

        // Undo velocity change
        manager.Undo();
        Assert.Equal(0, world.Get<Velocity>(entity).X);
        Assert.Equal(10, world.Get<Position>(entity).X);

        // Undo position change
        manager.Undo();
        Assert.Equal(0, world.Get<Position>(entity).X);
    }

    [Fact]
    public void RapidComponentChanges_MergeIntoSingleUndo()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new Position { X = 0, Y = 0 });

        // Rapid changes (simulating slider drag)
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 1, Y = 0 }));
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 2, Y = 0 }));
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 3, Y = 0 }));
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 4, Y = 0 }));
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 5, Y = 0 }));

        Assert.Equal(5, world.Get<Position>(entity).X);

        // Single undo should restore original (merged)
        manager.Undo();
        Assert.Equal(0, world.Get<Position>(entity).X);
    }

    #endregion

    #region State Consistency

    [Fact]
    public void ClearHistory_RemovesAllCommands()
    {
        manager.Execute(new CreateEntityCommand(world, "Entity1"));
        manager.Execute(new CreateEntityCommand(world, "Entity2"));
        manager.Execute(new CreateEntityCommand(world, "Entity3"));

        Assert.True(manager.CanUndo);

        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);

        // World state should be unchanged
        Assert.Equal(3, world.EntityCount);
    }

    [Fact]
    public void MaxHistorySize_OldCommandsDropped()
    {
        var smallManager = new UndoRedoManager(maxHistorySize: 3);

        smallManager.Execute(new CreateEntityCommand(world, "Entity1"));
        smallManager.Execute(new CreateEntityCommand(world, "Entity2"));
        smallManager.Execute(new CreateEntityCommand(world, "Entity3"));
        smallManager.Execute(new CreateEntityCommand(world, "Entity4")); // Entity1 command dropped

        Assert.Equal(4, world.EntityCount);

        // Can only undo 3 times
        smallManager.Undo();
        smallManager.Undo();
        smallManager.Undo();

        Assert.False(smallManager.CanUndo); // Entity1 command was dropped
        Assert.Equal(1, world.EntityCount); // Entity1 still exists but can't undo its creation
    }

    [Fact]
    public void UndoDescriptions_MatchCommands()
    {
        manager.Execute(new CreateEntityCommand(world, "MyEntity"));
        Assert.Contains("Create", manager.NextUndoDescription);

        var entity = world.GetAllEntities().First();
        manager.Execute(new RenameEntityCommand(world, entity, "NewName"));
        Assert.Contains("Rename", manager.NextUndoDescription);

        world.Add(entity, new Position { X = 0, Y = 0 });
        manager.Execute(new SetComponentCommand<Position>(world, entity, new Position { X = 10, Y = 10 }));
        Assert.Contains("Modify", manager.NextUndoDescription);
    }

    #endregion
}
