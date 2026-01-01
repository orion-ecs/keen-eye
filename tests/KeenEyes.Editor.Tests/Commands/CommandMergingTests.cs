using KeenEyes;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

/// <summary>
/// Tests for command merging behavior in the UndoRedoManager.
/// Commands like SetComponentCommand merge rapid changes within a time window.
/// </summary>
public class CommandMergingTests : IDisposable
{
    private readonly World world;
    private readonly UndoRedoManager manager;

    public CommandMergingTests()
    {
        world = new World();
        manager = new UndoRedoManager();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Test Components

    private struct TestValue : IComponent
    {
        public int Value;
    }

    private struct OtherValue : IComponent
    {
        public float Amount;
    }

    #endregion

    #region SetComponentCommand Merging

    [Fact]
    public void SetComponent_RapidChanges_MergesIntoSingleUndo()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });

        // Execute rapid changes (simulating slider drag)
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 20 }));
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 30 }));

        // Current value should be 30
        Assert.Equal(30, world.Get<TestValue>(entity).Value);

        // Single undo should restore to original value (0)
        manager.Undo();
        Assert.Equal(0, world.Get<TestValue>(entity).Value);

        // Should not be able to undo further (all merged into one)
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void SetComponent_DifferentEntities_DoNotMerge()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();
        world.Add(entity1, new TestValue { Value = 0 });
        world.Add(entity2, new TestValue { Value = 0 });

        manager.Execute(new SetComponentCommand<TestValue>(world, entity1, new TestValue { Value = 10 }));
        manager.Execute(new SetComponentCommand<TestValue>(world, entity2, new TestValue { Value = 20 }));

        // Both should be at their new values
        Assert.Equal(10, world.Get<TestValue>(entity1).Value);
        Assert.Equal(20, world.Get<TestValue>(entity2).Value);

        // First undo affects entity2
        manager.Undo();
        Assert.Equal(10, world.Get<TestValue>(entity1).Value);
        Assert.Equal(0, world.Get<TestValue>(entity2).Value);

        // Second undo affects entity1
        manager.Undo();
        Assert.Equal(0, world.Get<TestValue>(entity1).Value);
    }

    [Fact]
    public void SetComponent_DifferentComponentTypes_DoNotMerge()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });
        world.Add(entity, new OtherValue { Amount = 0f });

        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Execute(new SetComponentCommand<OtherValue>(world, entity, new OtherValue { Amount = 5.0f }));

        // First undo affects OtherValue
        manager.Undo();
        Assert.Equal(10, world.Get<TestValue>(entity).Value);
        Assert.Equal(0f, world.Get<OtherValue>(entity).Amount);

        // Second undo affects TestValue
        manager.Undo();
        Assert.Equal(0, world.Get<TestValue>(entity).Value);
    }

    #endregion

    #region Non-Mergeable Commands

    [Fact]
    public void CreateEntityCommand_DoesNotMerge()
    {
        manager.Execute(new CreateEntityCommand(world, "Entity1"));
        manager.Execute(new CreateEntityCommand(world, "Entity2"));

        // Two separate entities, two separate undos
        manager.Undo();
        Assert.Equal(1, world.EntityCount);

        manager.Undo();
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void DeleteEntityCommand_DoesNotMerge()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();

        manager.Execute(new DeleteEntityCommand(world, entity1));
        manager.Execute(new DeleteEntityCommand(world, entity2));

        // Two separate deletes, two separate undos
        manager.Undo();
        Assert.Equal(1, world.EntityCount);

        manager.Undo();
        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void RenameEntityCommand_RapidChanges_MergesIntoSingleUndo()
    {
        var entity = world.Spawn("Original").Build();

        // Rapid rename commands merge within 500ms (similar to SetComponent)
        manager.Execute(new RenameEntityCommand(world, entity, "Name1"));
        manager.Execute(new RenameEntityCommand(world, entity, "Name2"));
        manager.Execute(new RenameEntityCommand(world, entity, "Name3"));

        Assert.Equal("Name3", world.GetName(entity));

        // Single undo should restore to original since all renames merged
        manager.Undo();
        Assert.Equal("Original", world.GetName(entity));

        // No more undos available
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void RenameEntityCommand_DifferentEntities_DoNotMerge()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();

        manager.Execute(new RenameEntityCommand(world, entity1, "NewName1"));
        manager.Execute(new RenameEntityCommand(world, entity2, "NewName2"));

        // First undo affects entity2
        manager.Undo();
        Assert.Equal("NewName1", world.GetName(entity1));
        Assert.Equal("Entity2", world.GetName(entity2));

        // Second undo affects entity1
        manager.Undo();
        Assert.Equal("Entity1", world.GetName(entity1));
    }

    [Fact]
    public void ReparentEntityCommand_DoesNotMerge()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var child = world.Spawn("Child").Build();

        manager.Execute(new ReparentEntityCommand(world, child, parent1));
        manager.Execute(new ReparentEntityCommand(world, child, parent2));

        Assert.Equal(parent2, world.GetParent(child));

        manager.Undo();
        Assert.Equal(parent1, world.GetParent(child));

        manager.Undo();
        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    #endregion

    #region Mixed Command Types

    [Fact]
    public void MixedCommands_DoNotMerge()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });

        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Execute(new RenameEntityCommand(world, entity, "NewName"));
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 20 }));

        Assert.Equal("NewName", world.GetName(entity));
        Assert.Equal(20, world.Get<TestValue>(entity).Value);

        // Undo SetComponent(20)
        manager.Undo();
        Assert.Equal(10, world.Get<TestValue>(entity).Value);

        // Undo Rename
        manager.Undo();
        Assert.Equal("Entity", world.GetName(entity));

        // Undo SetComponent(10)
        manager.Undo();
        Assert.Equal(0, world.Get<TestValue>(entity).Value);
    }

    #endregion

    #region Batch Commands

    [Fact]
    public void BatchCommands_UndoAsUnit()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });

        manager.BeginBatch("Move and resize");
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Execute(new RenameEntityCommand(world, entity, "NewName"));
        manager.EndBatch();

        Assert.Equal("NewName", world.GetName(entity));
        Assert.Equal(10, world.Get<TestValue>(entity).Value);

        // Single undo reverts entire batch
        manager.Undo();
        Assert.Equal("Entity", world.GetName(entity));
        Assert.Equal(0, world.Get<TestValue>(entity).Value);

        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void BatchCommands_RedoAsUnit()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });

        manager.BeginBatch("Move and resize");
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Execute(new RenameEntityCommand(world, entity, "NewName"));
        manager.EndBatch();

        manager.Undo();

        // Single redo restores entire batch
        manager.Redo();
        Assert.Equal("NewName", world.GetName(entity));
        Assert.Equal(10, world.Get<TestValue>(entity).Value);
    }

    [Fact]
    public void BatchCommands_NestedBatchThrows()
    {
        manager.BeginBatch("Outer");

        Assert.Throws<InvalidOperationException>(() => manager.BeginBatch("Inner"));

        manager.CancelBatch();
    }

    [Fact]
    public void EmptyBatch_NotAddedToStack()
    {
        manager.BeginBatch("Empty batch");
        manager.EndBatch();

        Assert.False(manager.CanUndo);
    }

    #endregion

    #region Command Merging With Redo

    [Fact]
    public void NewCommand_ClearsRedoStack()
    {
        var entity = world.Spawn("Entity").Build();
        world.Add(entity, new TestValue { Value = 0 });

        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 10 }));
        manager.Undo();

        Assert.True(manager.CanRedo);

        // New command should clear redo stack
        manager.Execute(new SetComponentCommand<TestValue>(world, entity, new TestValue { Value = 20 }));

        Assert.False(manager.CanRedo);
    }

    #endregion
}
