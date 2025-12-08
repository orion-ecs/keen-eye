namespace KeenEyes.Testing.Tests;

public class WorldAssertionsTests
{
    #region ShouldHaveEntityCount Tests

    [Fact]
    public void ShouldHaveEntityCount_CorrectCount_Succeeds()
    {
        using var world = new World();
        world.Spawn().Build();
        world.Spawn().Build();

        Should.NotThrow(() => world.ShouldHaveEntityCount(2));
    }

    [Fact]
    public void ShouldHaveEntityCount_WrongCount_Throws()
    {
        using var world = new World();
        world.Spawn().Build();

        var ex = Should.Throw<AssertionException>(() => world.ShouldHaveEntityCount(5));
        ex.Message.ShouldContain("5 entities");
        ex.Message.ShouldContain("found 1");
    }

    [Fact]
    public void ShouldHaveEntityCount_WithBecause_IncludesReason()
    {
        using var world = new World();

        var ex = Should.Throw<AssertionException>(() =>
            world.ShouldHaveEntityCount(3, "we spawned three"));
        ex.Message.ShouldContain("because we spawned three");
    }

    [Fact]
    public void ShouldHaveEntityCount_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        testWorld.World.Spawn().Build();

        Should.NotThrow(() => testWorld.ShouldHaveEntityCount(1));
    }

    [Fact]
    public void ShouldHaveEntityCount_ReturnsWorld_ForChaining()
    {
        using var world = new World();

        var result = world.ShouldHaveEntityCount(0);

        result.ShouldBe(world);
    }

    #endregion

    #region ShouldBeEmpty Tests

    [Fact]
    public void ShouldBeEmpty_EmptyWorld_Succeeds()
    {
        using var world = new World();

        Should.NotThrow(() => world.ShouldBeEmpty());
    }

    [Fact]
    public void ShouldBeEmpty_NonEmptyWorld_Throws()
    {
        using var world = new World();
        world.Spawn().Build();

        Should.Throw<AssertionException>(() => world.ShouldBeEmpty());
    }

    [Fact]
    public void ShouldBeEmpty_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();

        Should.NotThrow(() => testWorld.ShouldBeEmpty());
    }

    #endregion

    #region ShouldNotBeEmpty Tests

    [Fact]
    public void ShouldNotBeEmpty_NonEmptyWorld_Succeeds()
    {
        using var world = new World();
        world.Spawn().Build();

        Should.NotThrow(() => world.ShouldNotBeEmpty());
    }

    [Fact]
    public void ShouldNotBeEmpty_EmptyWorld_Throws()
    {
        using var world = new World();

        var ex = Should.Throw<AssertionException>(() => world.ShouldNotBeEmpty());
        ex.Message.ShouldContain("at least one entity");
    }

    [Fact]
    public void ShouldNotBeEmpty_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder().Build();
        testWorld.World.Spawn().Build();

        Should.NotThrow(() => testWorld.ShouldNotBeEmpty());
    }

    #endregion

    #region ShouldHavePlugin Tests

    [Fact]
    public void ShouldHavePlugin_Installed_Succeeds()
    {
        using var world = new World();
        world.InstallPlugin<TestPlugin>();

        Should.NotThrow(() => world.ShouldHavePlugin<TestPlugin>());
    }

    [Fact]
    public void ShouldHavePlugin_NotInstalled_Throws()
    {
        using var world = new World();

        var ex = Should.Throw<AssertionException>(() => world.ShouldHavePlugin<TestPlugin>());
        ex.Message.ShouldContain("TestPlugin");
    }

    [Fact]
    public void ShouldHavePlugin_TestWorld_Succeeds()
    {
        using var testWorld = new TestWorldBuilder()
            .WithPlugin<TestPlugin>()
            .Build();

        Should.NotThrow(() => testWorld.ShouldHavePlugin<TestPlugin>());
    }

    #endregion

    #region ShouldNotHavePlugin Tests

    [Fact]
    public void ShouldNotHavePlugin_NotInstalled_Succeeds()
    {
        using var world = new World();

        Should.NotThrow(() => world.ShouldNotHavePlugin<TestPlugin>());
    }

    [Fact]
    public void ShouldNotHavePlugin_Installed_Throws()
    {
        using var world = new World();
        world.InstallPlugin<TestPlugin>();

        var ex = Should.Throw<AssertionException>(() => world.ShouldNotHavePlugin<TestPlugin>());
        ex.Message.ShouldContain("to not have plugin");
    }

    #endregion

    #region ShouldContainEntitiesWith Tests

    [Fact]
    public void ShouldContainEntitiesWith_HasMatching_Succeeds()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();

        Should.NotThrow(() => world.ShouldContainEntitiesWith<TestPosition>());
    }

    [Fact]
    public void ShouldContainEntitiesWith_NoMatching_Throws()
    {
        using var world = new World();
        world.Spawn().With(new TestVelocity()).Build();

        var ex = Should.Throw<AssertionException>(() =>
            world.ShouldContainEntitiesWith<TestPosition>());
        ex.Message.ShouldContain("TestPosition");
    }

    [Fact]
    public void ShouldContainEntitiesWith_TwoComponents_Succeeds()
    {
        using var world = new World();
        world.Spawn()
            .With(new TestPosition())
            .With(new TestVelocity())
            .Build();

        Should.NotThrow(() => world.ShouldContainEntitiesWith<TestPosition, TestVelocity>());
    }

    [Fact]
    public void ShouldContainEntitiesWith_TwoComponents_PartialMatch_Throws()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();

        Should.Throw<AssertionException>(() =>
            world.ShouldContainEntitiesWith<TestPosition, TestVelocity>());
    }

    #endregion

    #region ShouldNotContainEntitiesWith Tests

    [Fact]
    public void ShouldNotContainEntitiesWith_NoMatching_Succeeds()
    {
        using var world = new World();
        world.Spawn().With(new TestVelocity()).Build();

        Should.NotThrow(() => world.ShouldNotContainEntitiesWith<TestPosition>());
    }

    [Fact]
    public void ShouldNotContainEntitiesWith_HasMatching_Throws()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();

        var ex = Should.Throw<AssertionException>(() =>
            world.ShouldNotContainEntitiesWith<TestPosition>());
        ex.Message.ShouldContain("to not contain entities");
    }

    #endregion

    #region ShouldContainExactlyWith Tests

    [Fact]
    public void ShouldContainExactlyWith_CorrectCount_Succeeds()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();
        world.Spawn().With(new TestPosition()).Build();
        world.Spawn().With(new TestVelocity()).Build();

        Should.NotThrow(() => world.ShouldContainExactlyWith<TestPosition>(2));
    }

    [Fact]
    public void ShouldContainExactlyWith_WrongCount_Throws()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();

        var ex = Should.Throw<AssertionException>(() =>
            world.ShouldContainExactlyWith<TestPosition>(5));
        ex.Message.ShouldContain("exactly 5");
        ex.Message.ShouldContain("found 1");
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Assertions_SupportChaining()
    {
        using var world = new World();
        world.Spawn().With(new TestPosition()).Build();
        world.InstallPlugin<TestPlugin>();

        world
            .ShouldHaveEntityCount(1)
            .ShouldNotBeEmpty()
            .ShouldHavePlugin<TestPlugin>()
            .ShouldContainEntitiesWith<TestPosition>()
            .ShouldNotContainEntitiesWith<TestVelocity>();
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void ShouldHaveEntityCount_NullWorld_Throws()
    {
        World? world = null;

        Should.Throw<ArgumentNullException>(() => world!.ShouldHaveEntityCount(0));
    }

    [Fact]
    public void ShouldBeEmpty_NullTestWorld_Throws()
    {
        TestWorld? testWorld = null;

        Should.Throw<ArgumentNullException>(() => testWorld!.ShouldBeEmpty());
    }

    #endregion
}
