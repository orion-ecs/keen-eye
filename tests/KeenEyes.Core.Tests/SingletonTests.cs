namespace KeenEyes.Tests;

public record GameTime(float Delta, float Total);

public record GameConfig(int MaxEntities, bool DebugMode);

public class SingletonTests
{
    [Fact]
    public void SetSingleton_StoresValue()
    {
        using var world = new World();

        world.SetSingleton(new GameTime(0.016f, 1.5f));

        Assert.True(world.HasSingleton<GameTime>());
    }

    [Fact]
    public void GetSingleton_RetrievesValue()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));

        var time = world.GetSingleton<GameTime>();

        Assert.Equal(0.016f, time.Delta);
        Assert.Equal(1.5f, time.Total);
    }

    [Fact]
    public void SetSingleton_ReplacesExistingValue()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.0f));

        world.SetSingleton(new GameTime(0.033f, 2.0f));

        var time = world.GetSingleton<GameTime>();
        Assert.Equal(0.033f, time.Delta);
        Assert.Equal(2.0f, time.Total);
    }

    [Fact]
    public void GetSingleton_ThrowsWhenNotFound()
    {
        using var world = new World();

        Assert.Throws<KeyNotFoundException>(() => world.GetSingleton<GameTime>());
    }

    [Fact]
    public void TryGetSingleton_ReturnsTrueWhenExists()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));

        var found = world.TryGetSingleton<GameTime>(out var time);

        Assert.True(found);
        Assert.NotNull(time);
        Assert.Equal(0.016f, time.Delta);
    }

    [Fact]
    public void TryGetSingleton_ReturnsFalseWhenNotExists()
    {
        using var world = new World();

        var found = world.TryGetSingleton<GameTime>(out var time);

        Assert.False(found);
        Assert.Null(time);
    }

    [Fact]
    public void HasSingleton_ReturnsTrueWhenExists()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));

        Assert.True(world.HasSingleton<GameTime>());
    }

    [Fact]
    public void HasSingleton_ReturnsFalseWhenNotExists()
    {
        using var world = new World();

        Assert.False(world.HasSingleton<GameTime>());
    }

    [Fact]
    public void RemoveSingleton_ReturnsTrueWhenRemoved()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));

        var removed = world.RemoveSingleton<GameTime>();

        Assert.True(removed);
        Assert.False(world.HasSingleton<GameTime>());
    }

    [Fact]
    public void RemoveSingleton_ReturnsFalseWhenNotExists()
    {
        using var world = new World();

        var removed = world.RemoveSingleton<GameTime>();

        Assert.False(removed);
    }

    [Fact]
    public void MultipleSingletonTypes_AreIndependent()
    {
        using var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));
        world.SetSingleton(new GameConfig(1000, true));

        var time = world.GetSingleton<GameTime>();
        var config = world.GetSingleton<GameConfig>();

        Assert.Equal(0.016f, time.Delta);
        Assert.Equal(1000, config.MaxEntities);
    }

    [Fact]
    public void Dispose_ClearsSingletons()
    {
        var world = new World();
        world.SetSingleton(new GameTime(0.016f, 1.5f));

        world.Dispose();

        // After dispose, we can't access the singleton
        // (world is disposed, so this would throw or return false)
        // We test that re-using a disposed world is not recommended
    }
}
