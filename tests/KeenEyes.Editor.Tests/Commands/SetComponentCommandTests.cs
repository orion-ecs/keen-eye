using KeenEyes;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class SetComponentCommandTests : IDisposable
{
    private readonly World world;

    public SetComponentCommandTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Test Component

    private struct TestComponent : IComponent
    {
        public int Value;
        public string? Name;
    }

    private struct AnotherComponent : IComponent
    {
        public float X;
        public float Y;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDescriptionWithComponentName()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });

        Assert.Equal("Modify TestComponent", command.Description);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_UpdatesExistingComponent()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10, Name = "Original" });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20, Name = "Updated" });

        command.Execute();

        var component = world.Get<TestComponent>(entity);
        Assert.Equal(20, component.Value);
        Assert.Equal("Updated", component.Name);
    }

    [Fact]
    public void Execute_AddsComponentIfNotExists()
    {
        var entity = world.Spawn("TestEntity").Build();
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 42 });

        command.Execute();

        Assert.True(world.Has<TestComponent>(entity));
        Assert.Equal(42, world.Get<TestComponent>(entity).Value);
    }

    [Fact]
    public void Execute_PreservesOtherComponents()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        world.Add(entity, new AnotherComponent { X = 1.5f, Y = 2.5f });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });

        command.Execute();

        Assert.True(world.Has<AnotherComponent>(entity));
        var other = world.Get<AnotherComponent>(entity);
        Assert.Equal(1.5f, other.X);
        Assert.Equal(2.5f, other.Y);
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_RestoresOriginalValue()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10, Name = "Original" });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20, Name = "Updated" });
        command.Execute();

        command.Undo();

        var component = world.Get<TestComponent>(entity);
        Assert.Equal(10, component.Value);
        Assert.Equal("Original", component.Name);
    }

    [Fact]
    public void Undo_RestoresDefaultWhenComponentDidNotExist()
    {
        var entity = world.Spawn("TestEntity").Build();
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 42 });
        command.Execute();

        command.Undo();

        // Component should still exist but with default values
        Assert.True(world.Has<TestComponent>(entity));
        var component = world.Get<TestComponent>(entity);
        Assert.Equal(0, component.Value);
        Assert.Null(component.Name);
    }

    [Fact]
    public void Undo_DoesNothingIfComponentNoLongerExists()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });
        command.Execute();

        // Remove the component before undo
        world.Remove<TestComponent>(entity);

        // Undo should not throw
        command.Undo();

        Assert.False(world.Has<TestComponent>(entity));
    }

    [Fact]
    public void Undo_IsIdempotent()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });
        command.Execute();

        command.Undo();
        command.Undo(); // Should not throw

        Assert.Equal(10, world.Get<TestComponent>(entity).Value);
    }

    #endregion

    #region TryMerge Tests

    [Fact]
    public void TryMerge_ReturnsFalse_ForDifferentComponentType()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        world.Add(entity, new AnotherComponent { X = 1.0f });

        var command1 = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });
        var command2 = new SetComponentCommand<AnotherComponent>(world, entity, new AnotherComponent { X = 2.0f });

        Assert.False(command1.TryMerge(command2));
    }

    [Fact]
    public void TryMerge_ReturnsFalse_ForDifferentEntity()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();
        world.Add(entity1, new TestComponent { Value = 10 });
        world.Add(entity2, new TestComponent { Value = 10 });

        var command1 = new SetComponentCommand<TestComponent>(world, entity1, new TestComponent { Value = 20 });
        var command2 = new SetComponentCommand<TestComponent>(world, entity2, new TestComponent { Value = 30 });

        Assert.False(command1.TryMerge(command2));
    }

    [Fact]
    public void TryMerge_ReturnsFalse_ForNonSetComponentCommand()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });

        var command1 = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });
        var command2 = new RenameEntityCommand(world, entity, "NewName");

        Assert.False(command1.TryMerge(command2));
    }

    #endregion

    #region Execute/Undo Cycle Tests

    [Fact]
    public void ExecuteUndoExecute_RestoresNewValue()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });
        var command = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });

        command.Execute();
        Assert.Equal(20, world.Get<TestComponent>(entity).Value);

        command.Undo();
        Assert.Equal(10, world.Get<TestComponent>(entity).Value);

        command.Execute();
        Assert.Equal(20, world.Get<TestComponent>(entity).Value);
    }

    [Fact]
    public void MultipleCommands_ChainedUndoRedo()
    {
        var entity = world.Spawn("TestEntity").Build();
        world.Add(entity, new TestComponent { Value = 10 });

        // Important: Create each command AFTER executing the previous one
        // so it captures the correct old value
        var command1 = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 20 });
        command1.Execute();
        Assert.Equal(20, world.Get<TestComponent>(entity).Value);

        var command2 = new SetComponentCommand<TestComponent>(world, entity, new TestComponent { Value = 30 });
        command2.Execute();
        Assert.Equal(30, world.Get<TestComponent>(entity).Value);

        command2.Undo();
        Assert.Equal(20, world.Get<TestComponent>(entity).Value);

        command1.Undo();
        Assert.Equal(10, world.Get<TestComponent>(entity).Value);
    }

    #endregion
}
