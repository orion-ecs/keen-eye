namespace KeenEyes.Testing.Tests;

public class EntityAssertionsTests
{
    #region ShouldBeAlive Tests

    [Fact]
    public void ShouldBeAlive_AliveEntity_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Should.NotThrow(() => entity.ShouldBeAlive(world));
    }

    [Fact]
    public void ShouldBeAlive_DeadEntity_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var ex = Should.Throw<AssertionException>(() => entity.ShouldBeAlive(world));
        ex.Message.ShouldContain("to be alive");
    }

    [Fact]
    public void ShouldBeAlive_WithBecause_IncludesReason()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldBeAlive(world, "it was just created"));
        ex.Message.ShouldContain("because it was just created");
    }

    [Fact]
    public void ShouldBeAlive_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().Build();

        Should.NotThrow(() => entity.ShouldBeAlive(testWorld));
    }

    [Fact]
    public void ShouldBeAlive_ReturnsEntity_ForChaining()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition()).Build();

        var result = entity.ShouldBeAlive(world);

        result.ShouldBe(entity);
    }

    #endregion

    #region ShouldBeDead Tests

    [Fact]
    public void ShouldBeDead_DeadEntity_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        Should.NotThrow(() => entity.ShouldBeDead(world));
    }

    [Fact]
    public void ShouldBeDead_AliveEntity_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var ex = Should.Throw<AssertionException>(() => entity.ShouldBeDead(world));
        ex.Message.ShouldContain("to be dead");
    }

    [Fact]
    public void ShouldBeDead_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().Build();
        testWorld.World.Despawn(entity);

        Should.NotThrow(() => entity.ShouldBeDead(testWorld));
    }

    #endregion

    #region ShouldHaveComponent Tests

    [Fact]
    public void ShouldHaveComponent_HasComponent_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition { X = 10 }).Build();

        Should.NotThrow(() => entity.ShouldHaveComponent<TestPosition>(world));
    }

    [Fact]
    public void ShouldHaveComponent_MissingComponent_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldHaveComponent<TestPosition>(world));
        ex.Message.ShouldContain("TestPosition");
    }

    [Fact]
    public void ShouldHaveComponent_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().With(new TestPosition()).Build();

        Should.NotThrow(() => entity.ShouldHaveComponent<TestPosition>(testWorld));
    }

    #endregion

    #region ShouldNotHaveComponent Tests

    [Fact]
    public void ShouldNotHaveComponent_MissingComponent_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Should.NotThrow(() => entity.ShouldNotHaveComponent<TestPosition>(world));
    }

    [Fact]
    public void ShouldNotHaveComponent_HasComponent_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition()).Build();

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldNotHaveComponent<TestPosition>(world));
        ex.Message.ShouldContain("to not have component");
    }

    [Fact]
    public void ShouldNotHaveComponent_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().Build();

        Should.NotThrow(() => entity.ShouldNotHaveComponent<TestPosition>(testWorld));
    }

    #endregion

    #region ShouldHaveTag Tests

    [Fact]
    public void ShouldHaveTag_HasTag_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().WithTag<ActiveTag>().Build();

        Should.NotThrow(() => entity.ShouldHaveTag<ActiveTag>(world));
    }

    [Fact]
    public void ShouldHaveTag_MissingTag_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldHaveTag<ActiveTag>(world));
        ex.Message.ShouldContain("ActiveTag");
    }

    [Fact]
    public void ShouldHaveTag_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().WithTag<ActiveTag>().Build();

        Should.NotThrow(() => entity.ShouldHaveTag<ActiveTag>(testWorld));
    }

    #endregion

    #region ShouldNotHaveTag Tests

    [Fact]
    public void ShouldNotHaveTag_MissingTag_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Should.NotThrow(() => entity.ShouldNotHaveTag<ActiveTag>(world));
    }

    [Fact]
    public void ShouldNotHaveTag_HasTag_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().WithTag<ActiveTag>().Build();

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldNotHaveTag<ActiveTag>(world));
        ex.Message.ShouldContain("to not have tag");
    }

    [Fact]
    public void ShouldNotHaveTag_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().Build();

        Should.NotThrow(() => entity.ShouldNotHaveTag<ActiveTag>(testWorld));
    }

    #endregion

    #region ShouldHaveComponentMatching Tests

    [Fact]
    public void ShouldHaveComponentMatching_MatchesPredicate_Succeeds()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition { X = 10, Y = 20 }).Build();

        Should.NotThrow(() =>
            entity.ShouldHaveComponentMatching<TestPosition>(world, p =>
                Math.Abs(p.X - 10f) < 1e-5f && Math.Abs(p.Y - 20f) < 1e-5f));
    }

    [Fact]
    public void ShouldHaveComponentMatching_DoesNotMatch_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition { X = 10, Y = 20 }).Build();

        var ex = Should.Throw<AssertionException>(() =>
            entity.ShouldHaveComponentMatching<TestPosition>(world, p => Math.Abs(p.X - 999f) < 1e-5f));
        ex.Message.ShouldContain("matching predicate");
    }

    [Fact]
    public void ShouldHaveComponentMatching_MissingComponent_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        Should.Throw<AssertionException>(() =>
            entity.ShouldHaveComponentMatching<TestPosition>(world, _ => true));
    }

    [Fact]
    public void ShouldHaveComponentMatching_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        var entity = testWorld.World.Spawn().With(new TestHealth { Current = 50, Max = 100 }).Build();

        Should.NotThrow(() =>
            entity.ShouldHaveComponentMatching<TestHealth>(testWorld, h => h.Current == 50));
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Assertions_SupportChaining()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new TestPosition { X = 10, Y = 20 })
            .WithTag<ActiveTag>()
            .Build();

        entity
            .ShouldBeAlive(world)
            .ShouldHaveComponent<TestPosition>(world)
            .ShouldHaveTag<ActiveTag>(world)
            .ShouldNotHaveComponent<TestVelocity>(world)
            .ShouldNotHaveTag<FrozenTag>(world);
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void ShouldBeAlive_NullWorld_Throws()
    {
        var entity = new Entity(0, 1);

        Should.Throw<ArgumentNullException>(() => entity.ShouldBeAlive((World)null!));
    }

    [Fact]
    public void ShouldHaveComponent_NullWorld_Throws()
    {
        var entity = new Entity(0, 1);

        Should.Throw<ArgumentNullException>(() => entity.ShouldHaveComponent<TestPosition>((World)null!));
    }

    [Fact]
    public void ShouldHaveComponentMatching_NullPredicate_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().With(new TestPosition()).Build();

        Should.Throw<ArgumentNullException>(() =>
            entity.ShouldHaveComponentMatching<TestPosition>(world, null!));
    }

    #endregion
}
