using KeenEyes.Testing.Plugins;

namespace KeenEyes.Testing.Tests.Plugins;

public class MockPluginTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidName_CreatesPlugin()
    {
        var plugin = new MockPlugin("TestPlugin");

        Assert.Equal("TestPlugin", plugin.Name);
        Assert.False(plugin.WasInstalled);
        Assert.False(plugin.WasUninstalled);
        Assert.Equal(0, plugin.InstallCount);
        Assert.Equal(0, plugin.UninstallCount);
        Assert.False(plugin.IsInstalled);
        Assert.Null(plugin.LastInstallContext);
        Assert.Null(plugin.LastUninstallContext);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // ArgumentException.ThrowIfNullOrEmpty throws ArgumentNullException for null
        Assert.ThrowsAny<ArgumentException>(() => new MockPlugin(null!));
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new MockPlugin(string.Empty));
    }

    [Fact]
    public void Constructor_WithInstallAction_StoresAction()
    {
        var actionCalled = false;
        var plugin = new MockPlugin("TestPlugin", ctx => actionCalled = true);

        var mockContext = new MockPluginContext(plugin);
        plugin.Install(mockContext);

        Assert.True(actionCalled);
    }

    [Fact]
    public void Constructor_WithUninstallAction_StoresAction()
    {
        var actionCalled = false;
        var plugin = new MockPlugin("TestPlugin", null, ctx => actionCalled = true);

        var mockContext = new MockPluginContext(plugin);
        plugin.Install(mockContext);
        plugin.Uninstall(mockContext);

        Assert.True(actionCalled);
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_WithValidContext_IncrementsCount()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(plugin.WasInstalled);
        Assert.Equal(1, plugin.InstallCount);
        Assert.Same(context, plugin.LastInstallContext);
    }

    [Fact]
    public void Install_MultipleTime_IncrementsCountEachTime()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Install(context);
        plugin.Install(context);

        Assert.Equal(3, plugin.InstallCount);
        Assert.True(plugin.WasInstalled);
    }

    [Fact]
    public void Install_WithNullContext_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");

        Assert.Throws<ArgumentNullException>(() => plugin.Install(null!));
    }

    [Fact]
    public void Install_WithAction_ExecutesAction()
    {
        IPluginContext? capturedContext = null;
        var plugin = new MockPlugin("TestPlugin", ctx => capturedContext = ctx);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.Same(context, capturedContext);
    }

    [Fact]
    public void Install_UpdatesIsInstalled()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.False(plugin.IsInstalled);

        plugin.Install(context);

        Assert.True(plugin.IsInstalled);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_WithValidContext_IncrementsCount()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Uninstall(context);

        Assert.True(plugin.WasUninstalled);
        Assert.Equal(1, plugin.UninstallCount);
        Assert.Same(context, plugin.LastUninstallContext);
    }

    [Fact]
    public void Uninstall_MultipleTimes_IncrementsCountEachTime()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Uninstall(context);
        plugin.Uninstall(context);

        Assert.Equal(2, plugin.UninstallCount);
        Assert.True(plugin.WasUninstalled);
    }

    [Fact]
    public void Uninstall_WithNullContext_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");

        Assert.Throws<ArgumentNullException>(() => plugin.Uninstall(null!));
    }

    [Fact]
    public void Uninstall_WithAction_ExecutesAction()
    {
        IPluginContext? capturedContext = null;
        var plugin = new MockPlugin("TestPlugin", null, ctx => capturedContext = ctx);
        var context = new MockPluginContext(plugin);

        plugin.Uninstall(context);

        Assert.Same(context, capturedContext);
    }

    [Fact]
    public void Uninstall_UpdatesIsInstalled()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        Assert.True(plugin.IsInstalled);

        plugin.Uninstall(context);
        Assert.False(plugin.IsInstalled);
    }

    #endregion

    #region IsInstalled Tests

    [Fact]
    public void IsInstalled_WhenNeverInstalled_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");

        Assert.False(plugin.IsInstalled);
    }

    [Fact]
    public void IsInstalled_AfterInstall_ReturnsTrue()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(plugin.IsInstalled);
    }

    [Fact]
    public void IsInstalled_AfterInstallAndUninstall_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Uninstall(context);

        Assert.False(plugin.IsInstalled);
    }

    [Fact]
    public void IsInstalled_WithMultipleInstalls_ReturnsTrue()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Install(context);
        plugin.Uninstall(context);

        Assert.True(plugin.IsInstalled);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Uninstall(context);
        plugin.Reset();

        Assert.False(plugin.WasInstalled);
        Assert.False(plugin.WasUninstalled);
        Assert.Equal(0, plugin.InstallCount);
        Assert.Equal(0, plugin.UninstallCount);
        Assert.False(plugin.IsInstalled);
        Assert.Null(plugin.LastInstallContext);
        Assert.Null(plugin.LastUninstallContext);
    }

    [Fact]
    public void Reset_DoesNotChangeName()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Reset();

        Assert.Equal("TestPlugin", plugin.Name);
    }

    #endregion
}

public class MockPluginGenericTests
{
    private sealed class TestExtension
    {
        public string Value { get; set; } = "Test";
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesPlugin()
    {
        var extension = new TestExtension();
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension);

        Assert.Equal("TestPlugin", plugin.Name);
        Assert.Same(extension, plugin.Extension);
        Assert.Null(plugin.AdditionalInstallAction);
    }

    [Fact]
    public void Constructor_WithNullExtension_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MockPlugin<TestExtension>("TestPlugin", null!));
    }

    [Fact]
    public void Constructor_WithAdditionalAction_StoresAction()
    {
        var extension = new TestExtension();
        var actionCalled = false;
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension, ctx => actionCalled = true);

        var context = new MockPluginContext(plugin);
        plugin.Install(context);

        Assert.True(actionCalled);
    }

    [Fact]
    public void Constructor_WithUninstallAction_StoresAction()
    {
        var extension = new TestExtension();
        var actionCalled = false;
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension, null, ctx => actionCalled = true);

        var context = new MockPluginContext(plugin);
        plugin.Install(context);
        plugin.Uninstall(context);

        Assert.True(actionCalled);
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_RegistersExtension()
    {
        var extension = new TestExtension();
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(context.WasExtensionSet<TestExtension>());
        Assert.Same(extension, context.GetSetExtension<TestExtension>());
    }

    [Fact]
    public void Install_ExecutesAdditionalAction()
    {
        var extension = new TestExtension();
        var actionCalled = false;
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension, ctx => actionCalled = true);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(actionCalled);
    }

    [Fact]
    public void Install_CallsBaseInstall()
    {
        var extension = new TestExtension();
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(plugin.WasInstalled);
        Assert.Equal(1, plugin.InstallCount);
    }

    [Fact]
    public void Install_WithModifiableAdditionalAction_CanChangeAction()
    {
        var extension = new TestExtension();
        var plugin = new MockPlugin<TestExtension>("TestPlugin", extension);
        var context = new MockPluginContext(plugin);

        var actionCalled = false;
        plugin.AdditionalInstallAction = ctx => actionCalled = true;

        plugin.Install(context);

        Assert.True(actionCalled);
    }

    #endregion
}

public class FailingMockPluginTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidName_CreatesPlugin()
    {
        var plugin = new FailingMockPlugin("TestPlugin");

        Assert.Equal("TestPlugin", plugin.Name);
    }

    [Fact]
    public void Constructor_WithInstallException_StoresException()
    {
        var exception = new InvalidOperationException("Test error");
        var plugin = new FailingMockPlugin("TestPlugin", installException: exception);
        var context = new MockPluginContext(plugin);

        var thrown = Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
        Assert.Same(exception, thrown);
    }

    [Fact]
    public void Constructor_WithUninstallException_StoresException()
    {
        var exception = new InvalidOperationException("Test error");
        var plugin = new FailingMockPlugin("TestPlugin", uninstallException: exception);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        var thrown = Assert.Throws<InvalidOperationException>(() => plugin.Uninstall(context));
        Assert.Same(exception, thrown);
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_WithNoException_Succeeds()
    {
        var plugin = new FailingMockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);

        Assert.True(plugin.WasInstalled);
    }

    [Fact]
    public void Install_WithException_ThrowsException()
    {
        var exception = new InvalidOperationException("Install failed");
        var plugin = new FailingMockPlugin("TestPlugin", installException: exception);
        var context = new MockPluginContext(plugin);

        var thrown = Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
        Assert.Equal("Install failed", thrown.Message);
    }

    [Fact]
    public void Install_WithException_StillIncrementsCount()
    {
        var exception = new InvalidOperationException("Install failed");
        var plugin = new FailingMockPlugin("TestPlugin", installException: exception);
        var context = new MockPluginContext(plugin);

        Assert.Throws<InvalidOperationException>(() => plugin.Install(context));

        Assert.Equal(1, plugin.InstallCount);
        Assert.True(plugin.WasInstalled);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_WithNoException_Succeeds()
    {
        var plugin = new FailingMockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        plugin.Uninstall(context);

        Assert.True(plugin.WasUninstalled);
    }

    [Fact]
    public void Uninstall_WithException_ThrowsException()
    {
        var exception = new InvalidOperationException("Uninstall failed");
        var plugin = new FailingMockPlugin("TestPlugin", uninstallException: exception);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        var thrown = Assert.Throws<InvalidOperationException>(() => plugin.Uninstall(context));
        Assert.Equal("Uninstall failed", thrown.Message);
    }

    [Fact]
    public void Uninstall_WithException_StillIncrementsCount()
    {
        var exception = new InvalidOperationException("Uninstall failed");
        var plugin = new FailingMockPlugin("TestPlugin", uninstallException: exception);
        var context = new MockPluginContext(plugin);

        plugin.Install(context);
        Assert.Throws<InvalidOperationException>(() => plugin.Uninstall(context));

        Assert.Equal(1, plugin.UninstallCount);
        Assert.True(plugin.WasUninstalled);
    }

    #endregion

    #region Both Exceptions Tests

    [Fact]
    public void BothMethods_WithDifferentExceptions_ThrowCorrectException()
    {
        var installEx = new InvalidOperationException("Install failed");
        var uninstallEx = new ArgumentException("Uninstall failed");
        var plugin = new FailingMockPlugin("TestPlugin", installEx, uninstallEx);
        var context = new MockPluginContext(plugin);

        Assert.Throws<InvalidOperationException>(() => plugin.Install(context));
        Assert.Throws<ArgumentException>(() => plugin.Uninstall(context));
    }

    #endregion
}
