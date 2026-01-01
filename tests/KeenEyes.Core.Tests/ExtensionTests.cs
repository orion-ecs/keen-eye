namespace KeenEyes.Tests;

/// <summary>
/// Tests for World extension API.
/// </summary>
public class WorldExtensionTests
{
    [Fact]
    public void SetExtension_StoresValue()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void SetExtension_ReplacesExisting()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestPhysicsWorld { Gravity = -10.0f });

        var physics = world.GetExtension<TestPhysicsWorld>();
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void SetExtension_Null_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.SetExtension<TestPhysicsWorld>(null!));
    }

    [Fact]
    public void GetExtension_ReturnsStoredValue()
    {
        using var world = new World();
        var original = new TestPhysicsWorld { Gravity = -15.0f };

        world.SetExtension(original);
        var retrieved = world.GetExtension<TestPhysicsWorld>();

        Assert.Same(original, retrieved);
        Assert.Equal(-15.0f, retrieved.Gravity);
    }

    [Fact]
    public void GetExtension_ThrowsInvalidOperationException_WhenNotSet()
    {
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetExtension<TestPhysicsWorld>());

        Assert.Contains("TestPhysicsWorld", exception.Message);
    }

    [Fact]
    public void TryGetExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.True(result);
        Assert.NotNull(physics);
        Assert.Equal(-9.81f, physics.Gravity);
    }

    [Fact]
    public void TryGetExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.False(result);
        Assert.Null(physics);
    }

    [Fact]
    public void HasExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void HasExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.True(result);
        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.False(result);
    }

    [Fact]
    public void Extensions_MultipleTypes_Independent()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestRenderer { DrawCalls = 100 });

        var physics = world.GetExtension<TestPhysicsWorld>();
        var renderer = world.GetExtension<TestRenderer>();

        Assert.Equal(-9.81f, physics.Gravity);
        Assert.Equal(100, renderer.DrawCalls);
    }
}

/// <summary>
/// Tests for plugin extensions via PluginContext.
/// </summary>
public class PluginExtensionTests
{
    [Fact]
    public void Plugin_SetExtension_AvailableOnWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        var physics = world.GetExtension<TestPhysicsWorld>();

        Assert.NotNull(physics);
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void Plugin_Uninstall_RemovesExtension()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        world.UninstallPlugin<TestExtensionPlugin>();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }
}
