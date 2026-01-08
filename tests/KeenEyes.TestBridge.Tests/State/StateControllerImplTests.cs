using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge.Tests.State;

public class StateControllerImplTests
{
    #region Entity Queries

    [Fact]
    public async Task GetEntityCountAsync_EmptyWorld_ReturnsZero()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var count = await state.GetEntityCountAsync();

        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetEntityCountAsync_WithEntities_ReturnsCorrectCount()
    {
        using var world = new World();
        world.Spawn().Build();
        world.Spawn().Build();
        world.Spawn().Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var count = await state.GetEntityCountAsync();

        count.ShouldBe(3);
    }

    [Fact]
    public async Task GetEntityAsync_ExistingEntity_ReturnsSnapshot()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var snapshot = await state.GetEntityAsync(entity.Id);

        snapshot.ShouldNotBeNull();
        snapshot!.Id.ShouldBe(entity.Id);
    }

    [Fact]
    public async Task GetEntityAsync_NonExistentEntity_ReturnsNull()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var snapshot = await state.GetEntityAsync(999);

        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task GetEntityByNameAsync_ExistingEntity_ReturnsSnapshot()
    {
        using var world = new World();
        var entity = world.Spawn()
            .WithName("Player")
            .Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var snapshot = await state.GetEntityByNameAsync("Player");

        snapshot.ShouldNotBeNull();
        snapshot!.Name.ShouldBe("Player");
    }

    [Fact]
    public async Task GetEntityByNameAsync_NonExistentName_ReturnsNull()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var snapshot = await state.GetEntityByNameAsync("NonExistent");

        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task QueryEntitiesAsync_WithNamePattern_MatchesEntities()
    {
        using var world = new World();
        world.Spawn().WithName("Enemy1").Build();
        world.Spawn().WithName("Enemy2").Build();
        world.Spawn().WithName("Player").Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var query = new EntityQuery { NamePattern = "Enemy*" };
        var results = await state.QueryEntitiesAsync(query);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task QueryEntitiesAsync_WithComponents_MatchesEntities()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition { X = 0, Y = 0 }).Build();
        world.Spawn().With(new TestPosition { X = 1, Y = 1 }).With(new TestVelocity { X = 1, Y = 0 }).Build();
        world.Spawn().With(new TestVelocity { X = 2, Y = 0 }).Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var query = new EntityQuery { WithComponents = ["TestPosition"] };
        var results = await state.QueryEntitiesAsync(query);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task QueryEntitiesAsync_WithoutComponents_ExcludesEntities()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition { X = 0, Y = 0 }).Build();
        world.Spawn().With(new TestPosition { X = 1, Y = 1 }).With(new TestVelocity { X = 1, Y = 0 }).Build();
        world.Spawn().With(new TestVelocity { X = 2, Y = 0 }).Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var query = new EntityQuery { WithComponents = ["TestPosition"], WithoutComponents = ["TestVelocity"] };
        var results = await state.QueryEntitiesAsync(query);

        results.Count.ShouldBe(1);
    }

    [Fact]
    public async Task QueryEntitiesAsync_MaxResults_LimitsResults()
    {
        using var world = new World();
        for (int i = 0; i < 10; i++)
        {
            world.Spawn().Build();
        }
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var query = new EntityQuery { MaxResults = 5 };
        var results = await state.QueryEntitiesAsync(query);

        results.Count.ShouldBe(5);
    }

    [Fact]
    public async Task QueryEntitiesAsync_Skip_SkipsResults()
    {
        using var world = new World();
        for (int i = 0; i < 10; i++)
        {
            world.Spawn().Build();
        }
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var query = new EntityQuery { Skip = 7, MaxResults = 100 };
        var results = await state.QueryEntitiesAsync(query);

        results.Count.ShouldBe(3);
    }

    #endregion

    #region Component Access

    [Fact]
    public async Task GetComponentAsync_ExistingComponent_ReturnsData()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 42, Y = 99 })
            .Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var component = await state.GetComponentAsync(entity.Id, "TestPosition");

        component.ShouldNotBeNull();
        component.Value.GetProperty("X").GetSingle().ShouldBe(42f);
        component.Value.GetProperty("Y").GetSingle().ShouldBe(99f);
    }

    [Fact]
    public async Task GetComponentAsync_NonExistentComponent_ReturnsNull()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var component = await state.GetComponentAsync(entity.Id, "NonExistent");

        component.ShouldBeNull();
    }

    [Fact]
    public async Task GetComponentAsync_NonExistentEntity_ReturnsNull()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var component = await state.GetComponentAsync(999, "TestPosition");

        component.ShouldBeNull();
    }

    #endregion

    #region World Statistics

    [Fact]
    public async Task GetWorldStatsAsync_ReturnsEntityCount()
    {
        using var world = new World();
        world.Spawn().Build();
        world.Spawn().Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var stats = await state.GetWorldStatsAsync();

        stats.EntityCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetWorldStatsAsync_ReturnsFrameNumber()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var stats = await state.GetWorldStatsAsync();

        stats.FrameNumber.ShouldBe(0);
    }

    [Fact]
    public async Task GetWorldStatsAsync_TracksTiming()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        bridge.RecordFrameTime(16.67); // Simulate ~60fps
        var state = bridge.State;

        var stats = await state.GetWorldStatsAsync();

        stats.FrameNumber.ShouldBe(1);
    }

    #endregion

    #region Performance Metrics

    [Fact]
    public async Task GetPerformanceMetricsAsync_NoData_ReturnsZeros()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var metrics = await state.GetPerformanceMetricsAsync();

        metrics.SampleCount.ShouldBe(0);
        metrics.AverageFrameTimeMs.ShouldBe(0);
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_WithData_ReturnsAverages()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        bridge.RecordFrameTime(10.0);
        bridge.RecordFrameTime(20.0);
        bridge.RecordFrameTime(30.0);
        var state = bridge.State;

        var metrics = await state.GetPerformanceMetricsAsync();

        metrics.SampleCount.ShouldBe(3);
        metrics.AverageFrameTimeMs.ShouldBe(20.0, 0.01);
        metrics.MinFrameTimeMs.ShouldBe(10.0);
        metrics.MaxFrameTimeMs.ShouldBe(30.0);
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_CalculatesFps()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        bridge.RecordFrameTime(16.67); // ~60fps
        var state = bridge.State;

        var metrics = await state.GetPerformanceMetricsAsync();

        metrics.AverageFps.ShouldBe(60.0, 1.0);
    }

    [Fact]
    public async Task GetPerformanceMetricsAsync_LimitsFrameCount()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        for (int i = 0; i < 100; i++)
        {
            bridge.RecordFrameTime(16.0);
        }
        var state = bridge.State;

        var metrics = await state.GetPerformanceMetricsAsync(frameCount: 30);

        metrics.SampleCount.ShouldBe(30);
    }

    #endregion

    #region Systems

    [Fact]
    public async Task GetSystemsAsync_NoSystems_ReturnsEmpty()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var systems = await state.GetSystemsAsync();

        systems.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetSystemsAsync_WithTrackedSystems_ReturnsSystems()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        bridge.RecordSystemTime("TestSystem", 5.0);
        bridge.RecordSystemTime("TestSystem", 6.0);
        var state = bridge.State;

        var systems = await state.GetSystemsAsync();

        systems.Count.ShouldBe(1);
        systems[0].TypeName.ShouldBe("TestSystem");
        systems[0].AverageExecutionMs.ShouldBe(5.5, 0.01);
    }

    #endregion

    #region Hierarchy

    [Fact]
    public async Task GetChildrenAsync_WithChildren_ReturnsChildIds()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        world.SetParent(child1, parent);
        world.SetParent(child2, parent);
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var children = await state.GetChildrenAsync(parent.Id);

        children.Count.ShouldBe(2);
        children.ShouldContain(child1.Id);
        children.ShouldContain(child2.Id);
    }

    [Fact]
    public async Task GetChildrenAsync_NoChildren_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var children = await state.GetChildrenAsync(entity.Id);

        children.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetParentAsync_WithParent_ReturnsParentId()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var parentId = await state.GetParentAsync(child.Id);

        parentId.ShouldBe(parent.Id);
    }

    [Fact]
    public async Task GetParentAsync_NoParent_ReturnsNull()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        var parentId = await state.GetParentAsync(entity.Id);

        parentId.ShouldBeNull();
    }

    #endregion

    #region Extensions

    [Fact]
    public async Task GetExtensionAsync_ReturnsNull()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        // Extension lookup by name is not yet implemented
        var extension = await state.GetExtensionAsync("SomeExtension");

        extension.ShouldBeNull();
    }

    [Fact]
    public async Task HasExtensionAsync_ReturnsFalse()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        // Extension lookup by name is not yet implemented
        var hasExtension = await state.HasExtensionAsync("SomeExtension");

        hasExtension.ShouldBeFalse();
    }

    #endregion

    #region Tags

    [Fact]
    public async Task GetEntitiesWithTagAsync_ReturnsEmpty()
    {
        using var world = new World();
        using var bridge = new InProcessBridge(world);
        var state = bridge.State;

        // Tag querying not yet fully implemented
        var entities = await state.GetEntitiesWithTagAsync("SomeTag");

        entities.Count.ShouldBe(0);
    }

    #endregion
}
