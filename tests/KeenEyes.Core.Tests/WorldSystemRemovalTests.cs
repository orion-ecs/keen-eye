namespace KeenEyes.Tests;

public class WorldSystemRemovalTests
{
    #region Test Systems

    private sealed class TestSystem : ISystem
    {
        public bool Enabled { get; set; } = true;
        public int UpdateCount { get; private set; }

        public void Initialize(IWorld world) { }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }

        public void Dispose() { }
    }

    private sealed class AnotherTestSystem : ISystem
    {
        public bool Enabled { get; set; } = true;
        public int UpdateCount { get; private set; }

        public void Initialize(IWorld world) { }

        public void Update(float deltaTime)
        {
            UpdateCount++;
        }

        public void Dispose() { }
    }

    #endregion

    #region RemoveSystem(ISystem) Tests

    [Fact]
    public void RemoveSystem_WithExistingSystem_ReturnsTrue()
    {
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        var result = world.RemoveSystem(system);

        Assert.True(result);
    }

    [Fact]
    public void RemoveSystem_WithNonExistingSystem_ReturnsFalse()
    {
        using var world = new World();
        var system = new TestSystem();

        var result = world.RemoveSystem(system);

        Assert.False(result);
    }

    [Fact]
    public void RemoveSystem_RemovedSystemDoesNotReceiveUpdates()
    {
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount);

        world.RemoveSystem(system);

        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount); // Should not have incremented
    }

    [Fact]
    public void RemoveSystem_DoesNotAffectOtherSystems()
    {
        using var world = new World();
        var system1 = new TestSystem();
        var system2 = new AnotherTestSystem();
        world.AddSystem(system1);
        world.AddSystem(system2);

        world.RemoveSystem(system1);

        world.Update(0.016f);
        Assert.Equal(0, system1.UpdateCount);
        Assert.Equal(1, system2.UpdateCount);
    }

    [Fact]
    public void RemoveSystem_CalledTwice_SecondCallReturnsFalse()
    {
        using var world = new World();
        var system = new TestSystem();
        world.AddSystem(system);

        var firstResult = world.RemoveSystem(system);
        var secondResult = world.RemoveSystem(system);

        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    #endregion

    #region RemoveSystem<T>() Tests

    [Fact]
    public void RemoveSystemGeneric_WithExistingSystem_ReturnsTrue()
    {
        using var world = new World();
        world.AddSystem<TestSystem>();

        var result = world.RemoveSystem<TestSystem>();

        Assert.True(result);
    }

    [Fact]
    public void RemoveSystemGeneric_WithNonExistingSystem_ReturnsFalse()
    {
        using var world = new World();

        var result = world.RemoveSystem<TestSystem>();

        Assert.False(result);
    }

    [Fact]
    public void RemoveSystemGeneric_RemovedSystemDoesNotReceiveUpdates()
    {
        using var world = new World();
        world.AddSystem<TestSystem>();

        world.Update(0.016f);
        var systemBefore = world.GetSystem<TestSystem>();
        Assert.NotNull(systemBefore);
        Assert.Equal(1, systemBefore.UpdateCount);

        world.RemoveSystem<TestSystem>();

        world.Update(0.016f);
        var systemAfter = world.GetSystem<TestSystem>();
        Assert.Null(systemAfter);
    }

    [Fact]
    public void RemoveSystemGeneric_DoesNotAffectOtherSystems()
    {
        using var world = new World();
        world.AddSystem<TestSystem>();
        world.AddSystem<AnotherTestSystem>();

        world.RemoveSystem<TestSystem>();

        world.Update(0.016f);
        Assert.Null(world.GetSystem<TestSystem>());
        Assert.NotNull(world.GetSystem<AnotherTestSystem>());
        Assert.Equal(1, world.GetSystem<AnotherTestSystem>()!.UpdateCount);
    }

    #endregion
}
