namespace KeenEyes.TestBridge.Tests;

public class TestBridgePluginTests
{
    #region Plugin Name

    [Fact]
    public void Name_ReturnsTestBridge()
    {
        var plugin = new TestBridgePlugin();

        plugin.Name.ShouldBe("TestBridge");
    }

    #endregion

    #region Install

    [Fact]
    public void Install_ExposesITestBridgeExtension()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        world.TryGetExtension<ITestBridge>(out var bridge).ShouldBeTrue();
        bridge.ShouldNotBeNull();
    }

    [Fact]
    public void Install_ExposesInProcessBridgeExtension()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        world.TryGetExtension<InProcessBridge>(out var bridge).ShouldBeTrue();
        bridge.ShouldNotBeNull();
    }

    [Fact]
    public void Install_BridgeIsConnected()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<ITestBridge>();
        bridge.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void Install_BridgeHasInputController()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<ITestBridge>();
        bridge.Input.ShouldNotBeNull();
    }

    [Fact]
    public void Install_BridgeHasStateController()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<ITestBridge>();
        bridge.State.ShouldNotBeNull();
    }

    [Fact]
    public void Install_BridgeHasCaptureController()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<ITestBridge>();
        bridge.Capture.ShouldNotBeNull();
    }

    #endregion

    #region Options

    [Fact]
    public void Install_WithOptions_UsesGamepadCount()
    {
        using var world = new World();
        var options = new TestBridgeOptions { GamepadCount = 2 };
        var plugin = new TestBridgePlugin(options);

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<InProcessBridge>();
        bridge.InputContext.Gamepads.Length.ShouldBe(2);
    }

    [Fact]
    public void Install_WithNullOptions_UsesDefaults()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin(null);

        world.InstallPlugin(plugin);

        var bridge = world.GetExtension<InProcessBridge>();
        bridge.InputContext.Gamepads.Length.ShouldBe(4); // Default gamepad count
    }

    #endregion

    #region Uninstall

    [Fact]
    public void Uninstall_RemovesITestBridgeExtension()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin(plugin.Name);

        world.TryGetExtension<ITestBridge>(out _).ShouldBeFalse();
    }

    [Fact]
    public void Uninstall_RemovesInProcessBridgeExtension()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin(plugin.Name);

        world.TryGetExtension<InProcessBridge>(out _).ShouldBeFalse();
    }

    [Fact]
    public void Uninstall_BridgeIsNoLongerConnected()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();
        world.InstallPlugin(plugin);
        var bridge = world.GetExtension<ITestBridge>();

        world.UninstallPlugin(plugin.Name);

        bridge.IsConnected.ShouldBeFalse();
    }

    #endregion

    #region OnFrameComplete

    [Fact]
    public void OnFrameComplete_DoesNotThrowWithValidBridge()
    {
        using var world = new World();
        var plugin = new TestBridgePlugin();
        world.InstallPlugin(plugin);

        Should.NotThrow(() => plugin.OnFrameComplete());
    }

    #endregion
}
