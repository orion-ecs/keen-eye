namespace KeenEyes.Testing.Tests;

public class TestWorldTests
{
    #region Step Tests

    [Fact]
    public void Step_WithManualTime_AdvancesClockAndUpdatesWorld()
    {
        var system = new TestCountingSystem();
        using var testWorld = new TestWorldBuilder()
            .WithManualTime(fps: 60f)
            .WithSystem(system)
            .Build();

        testWorld.Step();

        system.UpdateCount.ShouldBe(1);
        testWorld.Clock!.FrameCount.ShouldBe(1);
    }

    [Fact]
    public void Step_MultipleFrames_AdvancesCorrectly()
    {
        var system = new TestCountingSystem();
        using var testWorld = new TestWorldBuilder()
            .WithManualTime(fps: 60f)
            .WithSystem(system)
            .Build();

        testWorld.Step(3);

        system.UpdateCount.ShouldBe(1); // World.Update called once with combined delta
        testWorld.Clock!.FrameCount.ShouldBe(3);
    }

    [Fact]
    public void Step_WithoutManualTime_Throws()
    {
        using var testWorld = new TestWorldBuilder().Build();

        Should.Throw<InvalidOperationException>(() => testWorld.Step());
    }

    [Fact]
    public void Step_ReturnsDeltaSeconds()
    {
        using var testWorld = new TestWorldBuilder()
            .WithManualTime(fps: 60f)
            .Build();

        var result = testWorld.Step();

        result.ShouldBe(1f / 60f, tolerance: 0.0001f);
    }

    #endregion

    #region StepByTime Tests

    [Fact]
    public void StepByTime_AdvancesCorrectly()
    {
        var system = new TestCountingSystem();
        using var testWorld = new TestWorldBuilder()
            .WithManualTime()
            .WithSystem(system)
            .Build();

        testWorld.StepByTime(100f);

        system.UpdateCount.ShouldBe(1);
        system.TotalDeltaTime.ShouldBe(0.1f, tolerance: 0.0001f);
    }

    [Fact]
    public void StepByTime_WithoutManualTime_Throws()
    {
        using var testWorld = new TestWorldBuilder().Build();

        Should.Throw<InvalidOperationException>(() => testWorld.StepByTime(100f));
    }

    #endregion

    #region GetEntityCount Tests

    [Fact]
    public void GetEntityCount_ReturnsCorrectCount()
    {
        using var testWorld = new TestWorldBuilder().Build();
        testWorld.World.Spawn().Build();
        testWorld.World.Spawn().Build();
        testWorld.World.Spawn().Build();

        testWorld.GetEntityCount().ShouldBe(3);
    }

    [Fact]
    public void GetEntityCount_EmptyWorld_ReturnsZero()
    {
        using var testWorld = new TestWorldBuilder().Build();

        testWorld.GetEntityCount().ShouldBe(0);
    }

    #endregion

    #region CreateEntity Tests

    [Fact]
    public void CreateEntity_SingleComponent_CreatesEntity()
    {
        using var testWorld = new TestWorldBuilder().Build();

        var entity = testWorld.CreateEntity(new TestPosition { X = 10, Y = 20 });

        entity.ShouldBeAlive(testWorld);
        entity.ShouldHaveComponent<TestPosition>(testWorld);
    }

    [Fact]
    public void CreateEntity_TwoComponents_CreatesEntity()
    {
        using var testWorld = new TestWorldBuilder().Build();

        var entity = testWorld.CreateEntity(
            new TestPosition { X = 10, Y = 20 },
            new TestVelocity { X = 1, Y = 2 });

        entity.ShouldBeAlive(testWorld);
        entity.ShouldHaveComponent<TestPosition>(testWorld);
        entity.ShouldHaveComponent<TestVelocity>(testWorld);
    }

    [Fact]
    public void CreateEntity_ThreeComponents_CreatesEntity()
    {
        using var testWorld = new TestWorldBuilder().Build();

        var entity = testWorld.CreateEntity(
            new TestPosition { X = 10, Y = 20 },
            new TestVelocity { X = 1, Y = 2 },
            new TestHealth { Current = 100, Max = 100 });

        entity.ShouldBeAlive(testWorld);
        entity.ShouldHaveComponent<TestPosition>(testWorld);
        entity.ShouldHaveComponent<TestVelocity>(testWorld);
        entity.ShouldHaveComponent<TestHealth>(testWorld);
    }

    #endregion

    #region CreateEntities Tests

    [Fact]
    public void CreateEntities_CreatesMultipleEntities()
    {
        using var testWorld = new TestWorldBuilder().Build();

        var entities = testWorld.CreateEntities(5, i => new TestPosition { X = i, Y = i * 2 });

        entities.Length.ShouldBe(5);
        testWorld.GetEntityCount().ShouldBe(5);
    }

    [Fact]
    public void CreateEntities_FactoryReceivesCorrectIndex()
    {
        using var testWorld = new TestWorldBuilder().Build();

        var entities = testWorld.CreateEntities(3, i => new TestHealth { Current = i * 10, Max = 100 });

        testWorld.World.Get<TestHealth>(entities[0]).Current.ShouldBe(0);
        testWorld.World.Get<TestHealth>(entities[1]).Current.ShouldBe(10);
        testWorld.World.Get<TestHealth>(entities[2]).Current.ShouldBe(20);
    }

    #endregion

    #region AssertClean Tests

    [Fact]
    public void AssertClean_EmptyWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();

        Should.NotThrow(() => testWorld.AssertClean());
    }

    [Fact]
    public void AssertClean_WithEntities_Throws()
    {
        using var testWorld = new TestWorldBuilder().Build();
        testWorld.World.Spawn().Build();

        Should.Throw<InvalidOperationException>(() => testWorld.AssertClean());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesUnderlyingWorld()
    {
        var testWorld = new TestWorldBuilder().Build();

        testWorld.Dispose();

        // Disposing again should not throw
        Should.NotThrow(() => testWorld.Dispose());
    }

    #endregion
}
