using KeenEyes.Testing.Plugins;

namespace KeenEyes.Testing.Tests.Plugins;

public class PluginAssertionsTests
{
    #region MockPlugin Assertions

    [Fact]
    public void ShouldBeInstalled_WhenInstalled_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);

        var result = plugin.ShouldBeInstalled();

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldBeInstalled_WhenNotInstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldBeInstalled());
        Assert.Contains("TestPlugin", ex.Message);
        // Message is: "Expected plugin 'TestPlugin' to have been installed, but it was not."
        Assert.Contains("was not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldBeInstalled_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldBeInstalled());
    }

    [Fact]
    public void ShouldNotBeInstalled_WhenNotInstalled_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");

        var result = plugin.ShouldNotBeInstalled();

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldNotBeInstalled_WhenInstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldNotBeInstalled());
        Assert.Contains("TestPlugin", ex.Message);
        Assert.Contains("not have been installed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 time(s)", ex.Message);
    }

    [Fact]
    public void ShouldNotBeInstalled_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldNotBeInstalled());
    }

    [Fact]
    public void ShouldBeUninstalled_WhenUninstalled_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);

        var result = plugin.ShouldBeUninstalled();

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldBeUninstalled_WhenNotUninstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldBeUninstalled());
        Assert.Contains("TestPlugin", ex.Message);
        // Message is: "Expected plugin 'TestPlugin' to have been uninstalled, but it was not."
        Assert.Contains("was not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldBeUninstalled_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldBeUninstalled());
    }

    [Fact]
    public void ShouldNotBeUninstalled_WhenNotUninstalled_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");

        var result = plugin.ShouldNotBeUninstalled();

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldNotBeUninstalled_WhenUninstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);
        plugin.Uninstall(context);

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldNotBeUninstalled());
        Assert.Contains("TestPlugin", ex.Message);
        Assert.Contains("not have been uninstalled", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2 time(s)", ex.Message);
    }

    [Fact]
    public void ShouldNotBeUninstalled_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldNotBeUninstalled());
    }

    [Fact]
    public void ShouldHaveBeenInstalledTimes_WhenCountMatches_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Install(context);
        plugin.Install(context);

        var result = plugin.ShouldHaveBeenInstalledTimes(3);

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldHaveBeenInstalledTimes_WhenCountDoesNotMatch_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldHaveBeenInstalledTimes(2));
        Assert.Contains("TestPlugin", ex.Message);
        Assert.Contains("2 time(s)", ex.Message);
        Assert.Contains("1 time(s)", ex.Message);
    }

    [Fact]
    public void ShouldHaveBeenInstalledTimes_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldHaveBeenInstalledTimes(1));
    }

    [Fact]
    public void ShouldHaveBeenUninstalledTimes_WhenCountMatches_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);
        plugin.Uninstall(context);

        var result = plugin.ShouldHaveBeenUninstalledTimes(2);

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldHaveBeenUninstalledTimes_WhenCountDoesNotMatch_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldHaveBeenUninstalledTimes(3));
        Assert.Contains("TestPlugin", ex.Message);
        Assert.Contains("3 time(s)", ex.Message);
        Assert.Contains("1 time(s)", ex.Message);
    }

    [Fact]
    public void ShouldHaveBeenUninstalledTimes_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldHaveBeenUninstalledTimes(1));
    }

    [Fact]
    public void ShouldBeCurrentlyInstalled_WhenInstalled_ReturnsPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);

        var result = plugin.ShouldBeCurrentlyInstalled();

        Assert.Same(plugin, result);
    }

    [Fact]
    public void ShouldBeCurrentlyInstalled_WhenNotInstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldBeCurrentlyInstalled());
        Assert.Contains("TestPlugin", ex.Message);
        Assert.Contains("currently installed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldBeCurrentlyInstalled_WhenInstalledAndUninstalled_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);

        var ex = Assert.Throws<PluginAssertionException>(() => plugin.ShouldBeCurrentlyInstalled());
        Assert.Contains("installed: 1", ex.Message);
        Assert.Contains("uninstalled: 1", ex.Message);
    }

    [Fact]
    public void ShouldBeCurrentlyInstalled_WithNullPlugin_ThrowsArgumentNullException()
    {
        MockPlugin plugin = null!;

        Assert.Throws<ArgumentNullException>(() => plugin.ShouldBeCurrentlyInstalled());
    }

    [Fact]
    public void MockPluginAssertions_CanChain()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);

        plugin
            .ShouldBeInstalled()
            .ShouldBeUninstalled()
            .ShouldHaveBeenInstalledTimes(1)
            .ShouldHaveBeenUninstalledTimes(1);
    }

    #endregion

    #region MockPluginContext Assertions

    [Fact]
    public void ShouldHaveRegisteredSystem_WhenRegistered_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>();

        var result = context.ShouldHaveRegisteredSystem<TestCountingSystem>();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveRegisteredSystem_WhenNotRegistered_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldHaveRegisteredSystem<TestCountingSystem>());
        Assert.Contains("TestCountingSystem", ex.Message);
        // Message is: "Expected system 'TestCountingSystem' to have been registered, but it was not."
        Assert.Contains("was not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldHaveRegisteredSystem_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldHaveRegisteredSystem<TestCountingSystem>());
    }

    [Fact]
    public void ShouldNotHaveRegisteredSystem_WhenNotRegistered_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var result = context.ShouldNotHaveRegisteredSystem<TestCountingSystem>();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldNotHaveRegisteredSystem_WhenRegistered_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>();

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldNotHaveRegisteredSystem<TestCountingSystem>());
        Assert.Contains("TestCountingSystem", ex.Message);
        Assert.Contains("not have been registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldNotHaveRegisteredSystem_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldNotHaveRegisteredSystem<TestCountingSystem>());
    }

    [Fact]
    public void ShouldHaveRegisteredSystemAtPhase_WhenRegisteredAtPhase_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>(SystemPhase.FixedUpdate);

        var result = context.ShouldHaveRegisteredSystemAtPhase<TestCountingSystem>(SystemPhase.FixedUpdate);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveRegisteredSystemAtPhase_WhenRegisteredAtDifferentPhase_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>(SystemPhase.Update);

        var ex = Assert.Throws<PluginAssertionException>(() =>
            context.ShouldHaveRegisteredSystemAtPhase<TestCountingSystem>(SystemPhase.FixedUpdate));
        Assert.Contains("TestCountingSystem", ex.Message);
        Assert.Contains("FixedUpdate", ex.Message);
        Assert.Contains("Update", ex.Message);
    }

    [Fact]
    public void ShouldHaveRegisteredSystemAtPhase_WhenNotRegistered_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<PluginAssertionException>(() =>
            context.ShouldHaveRegisteredSystemAtPhase<TestCountingSystem>(SystemPhase.Update));
        Assert.Contains("TestCountingSystem", ex.Message);
        // Message is: "Expected system 'TestCountingSystem' to have been registered at phase Update, but it was not registered at all."
        Assert.Contains("not registered at all", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldHaveRegisteredSystemAtPhase_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() =>
            context.ShouldHaveRegisteredSystemAtPhase<TestCountingSystem>(SystemPhase.Update));
    }

    [Fact]
    public void ShouldHaveSetExtension_WhenSet_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.SetExtension(new TestExtension());

        var result = context.ShouldHaveSetExtension<TestExtension>();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveSetExtension_WhenNotSet_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldHaveSetExtension<TestExtension>());
        Assert.Contains("TestExtension", ex.Message);
        // Message is: "Expected extension 'TestExtension' to have been set, but it was not."
        Assert.Contains("was not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldHaveSetExtension_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldHaveSetExtension<TestExtension>());
    }

    [Fact]
    public void ShouldNotHaveSetExtension_WhenNotSet_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var result = context.ShouldNotHaveSetExtension<TestExtension>();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldNotHaveSetExtension_WhenSet_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.SetExtension(new TestExtension());

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldNotHaveSetExtension<TestExtension>());
        Assert.Contains("TestExtension", ex.Message);
        Assert.Contains("not have been set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldNotHaveSetExtension_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldNotHaveSetExtension<TestExtension>());
    }

    [Fact]
    public void ShouldHaveRegisteredComponent_WhenRegistered_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.RegisterComponent<TestPosition>();

        var result = context.ShouldHaveRegisteredComponent<TestPosition>();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveRegisteredComponent_WhenNotRegistered_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldHaveRegisteredComponent<TestPosition>());
        Assert.Contains("TestPosition", ex.Message);
        // Message is: "Expected component 'TestPosition' to have been registered, but it was not."
        Assert.Contains("was not", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldHaveRegisteredComponent_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldHaveRegisteredComponent<TestPosition>());
    }

    [Fact]
    public void ShouldHaveRegisteredSystemCount_WhenCountMatches_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>();

        var result = context.ShouldHaveRegisteredSystemCount(1);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveRegisteredSystemCount_WhenCountDoesNotMatch_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>();

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldHaveRegisteredSystemCount(2));
        Assert.Contains("2 system(s)", ex.Message);
        Assert.Contains("1 were", ex.Message);
    }

    [Fact]
    public void ShouldHaveRegisteredSystemCount_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldHaveRegisteredSystemCount(0));
    }

    [Fact]
    public void ShouldHaveSetExtensionCount_WhenCountMatches_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.SetExtension(new TestExtension());

        var result = context.ShouldHaveSetExtensionCount(1);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveSetExtensionCount_WhenCountDoesNotMatch_ThrowsPluginAssertionException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<PluginAssertionException>(() => context.ShouldHaveSetExtensionCount(1));
        Assert.Contains("1 extension(s)", ex.Message);
        Assert.Contains("0 were", ex.Message);
    }

    [Fact]
    public void ShouldHaveSetExtensionCount_WithNullContext_ThrowsArgumentNullException()
    {
        MockPluginContext context = null!;

        Assert.Throws<ArgumentNullException>(() => context.ShouldHaveSetExtensionCount(0));
    }

    [Fact]
    public void MockPluginContextAssertions_CanChain()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        context.AddSystem<TestCountingSystem>(SystemPhase.Update);
        context.SetExtension(new TestExtension());
        context.RegisterComponent<TestPosition>();

        context
            .ShouldHaveRegisteredSystem<TestCountingSystem>()
            .ShouldHaveRegisteredSystemAtPhase<TestCountingSystem>(SystemPhase.Update)
            .ShouldHaveSetExtension<TestExtension>()
            .ShouldHaveRegisteredComponent<TestPosition>()
            .ShouldHaveRegisteredSystemCount(1)
            .ShouldHaveSetExtensionCount(1);
    }

    #endregion

    private sealed class TestExtension
    {
        public string Value { get; set; } = "Test";
    }
}

public class PluginAssertionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesException()
    {
        var ex = new PluginAssertionException("Test message");

        Assert.Equal("Test message", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new PluginAssertionException("Test message", inner);

        Assert.Equal("Test message", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void InheritsFromException()
    {
        var ex = new PluginAssertionException("Test");

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
