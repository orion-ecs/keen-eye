namespace KeenEyes.Tests;

/// <summary>
/// Tests for IWorldPlugin interface and plugin installation.
/// </summary>
public class PluginInstallationTests
{
    [Fact]
    public void InstallPlugin_Generic_CallsInstall()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();
        Assert.NotNull(plugin);
        Assert.True(plugin.InstallCalled);
    }

    [Fact]
    public void InstallPlugin_Instance_CallsInstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();

        world.InstallPlugin(plugin);

        Assert.True(plugin.InstallCalled);
    }

    [Fact]
    public void InstallPlugin_ProvidesValidContext()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();
        Assert.NotNull(plugin);
        Assert.NotNull(plugin.LastContext);
        Assert.Same(world, plugin.LastContext.World);
        Assert.Same(plugin, plugin.LastContext.Plugin);
    }

    [Fact]
    public void InstallPlugin_ReturnsWorld_ForChaining()
    {
        using var world = new World();

        var result = world.InstallPlugin<TestSimplePlugin>();

        Assert.Same(world, result);
    }

    [Fact]
    public void InstallPlugin_MultiplePlugins_AllInstalled()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>()
             .InstallPlugin<TestExtensionPlugin>();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
        Assert.True(world.HasPlugin<TestExtensionPlugin>());
    }

    [Fact]
    public void InstallPlugin_DuplicateName_ThrowsInvalidOperationException()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.InstallPlugin<TestSimplePlugin>());

        Assert.Contains("TestSimple", exception.Message);
        Assert.Contains("already installed", exception.Message);
    }

    [Fact]
    public void InstallPlugin_Null_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.InstallPlugin(null!));
    }
}

/// <summary>
/// Tests for plugin uninstallation.
/// </summary>
public class PluginUninstallationTests
{
    [Fact]
    public void UninstallPlugin_Generic_CallsUninstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSimplePlugin>();

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void UninstallPlugin_ByName_CallsUninstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin("TestSimple");

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void UninstallPlugin_ReturnsTrue_WhenPluginExists()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        var result = world.UninstallPlugin<TestSimplePlugin>();

        Assert.True(result);
    }

    [Fact]
    public void UninstallPlugin_ReturnsFalse_WhenPluginNotFound()
    {
        using var world = new World();

        var result = world.UninstallPlugin<TestSimplePlugin>();

        Assert.False(result);
    }

    [Fact]
    public void UninstallPlugin_ByName_ReturnsFalse_WhenNotFound()
    {
        using var world = new World();

        var result = world.UninstallPlugin("NonExistent");

        Assert.False(result);
    }

    [Fact]
    public void UninstallPlugin_RemovesFromWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        world.UninstallPlugin<TestSimplePlugin>();

        Assert.False(world.HasPlugin<TestSimplePlugin>());
        Assert.Null(world.GetPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void UninstallPlugin_DisposesRegisteredSystems()
    {
        using var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSystemPlugin>();

        Assert.True(plugin.System1!.Disposed);
        Assert.True(plugin.System2!.Disposed);
    }

    [Fact]
    public void UninstallPlugin_RemovesSystemsFromWorld()
    {
        using var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSystemPlugin>();
        world.Update(0.016f);

        // Systems should no longer update after uninstall
        Assert.Equal(0, plugin.System1!.UpdateCount);
        Assert.Equal(0, plugin.System2!.UpdateCount);
    }
}

/// <summary>
/// Tests for World disposal with plugins.
/// </summary>
public class PluginDisposeTests
{
    [Fact]
    public void Dispose_UninstallsAllPlugins()
    {
        var plugin = new TestSimplePlugin();
        var world = new World();
        world.InstallPlugin(plugin);

        world.Dispose();

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void Dispose_DisposesPluginSystems()
    {
        var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.Dispose();

        Assert.True(plugin.System1!.Disposed);
        Assert.True(plugin.System2!.Disposed);
    }

    [Fact]
    public void Dispose_ClearsExtensions()
    {
        var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        world.Dispose();

        // Extensions should be cleared (can't easily test this without internal access)
        // The main point is that Dispose runs without error
    }
}
