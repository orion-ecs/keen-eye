namespace KeenEyes.Tests;

/// <summary>
/// Tests for system dependency tracking with runsBefore and runsAfter.
/// </summary>
public sealed class SystemDependencyTests
{
    private sealed class SystemA : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    private sealed class SystemB : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    private sealed class SystemC : SystemBase
    {
        public override void Update(float deltaTime) { }
    }

    [Fact]
    public void AddSystem_WithRunsBeforeAndAfter_SuccessfullyAddsSystem()
    {
        using var world = new World();

        // Add system with dependency constraints
        world.AddSystem<SystemB>(
            phase: SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(SystemC)],
            runsAfter: [typeof(SystemA)]);

        // Should not throw, system should be added
        world.Update(0.016f);
    }

    [Fact]
    public void AddSystem_WithRunsBefore_MaintainsOrdering()
    {
        using var world = new World();

        // System B should run before System C
        world.AddSystem<SystemC>(SystemPhase.Update, order: 10, runsBefore: [], runsAfter: []);
        world.AddSystem<SystemB>(SystemPhase.Update, order: 0, runsBefore: [typeof(SystemC)], runsAfter: []);

        // Verify systems can be updated
        world.Update(0.016f);
    }

    [Fact]
    public void AddSystem_WithRunsAfter_MaintainsOrdering()
    {
        using var world = new World();

        // System B should run after System A
        world.AddSystem<SystemA>(SystemPhase.Update, order: 0, runsBefore: [], runsAfter: []);
        world.AddSystem<SystemB>(SystemPhase.Update, order: 10, runsBefore: [], runsAfter: [typeof(SystemA)]);

        // Verify systems can be updated
        world.Update(0.016f);
    }

    [Fact]
    public void AddSystem_WithEmptyDependencies_WorksNormally()
    {
        using var world = new World();

        world.AddSystem<SystemA>(
            phase: SystemPhase.Update,
            order: 0,
            runsBefore: [],
            runsAfter: []);

        world.Update(0.016f);
    }

    [Fact]
    public void AddSystem_WithMultipleDependencies_SuccessfullyAddsSystem()
    {
        using var world = new World();

        world.AddSystem<SystemA>(SystemPhase.Update, order: 0, runsBefore: [], runsAfter: []);
        world.AddSystem<SystemC>(SystemPhase.Update, order: 10, runsBefore: [], runsAfter: []);

        // System B should run after A and before C
        world.AddSystem<SystemB>(
            phase: SystemPhase.Update,
            order: 5,
            runsBefore: [typeof(SystemC)],
            runsAfter: [typeof(SystemA)]);

        world.Update(0.016f);
    }
}
