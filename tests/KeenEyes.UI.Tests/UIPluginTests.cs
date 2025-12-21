using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for UIPlugin installation and uninstallation.
/// </summary>
public sealed class UIPluginTests
{
    [Fact]
    public void Install_CreatesUIContext()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        Assert.True(world.TryGetExtension<UIContext>(out var context));
        Assert.NotNull(context);
    }

    [Fact]
    public void Install_CanCreateCanvas()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);
        var ui = world.GetExtension<UIContext>();

        var canvas = ui.CreateCanvas("TestCanvas");

        Assert.True(canvas.IsValid);
        Assert.True(world.Has<UIElement>(canvas));
        Assert.True(world.Has<UIRect>(canvas));
        Assert.True(world.Has<UIRootTag>(canvas));
    }

    [Fact]
    public void Uninstall_RemovesUIContext()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<UIContext>(out _));

        world.UninstallPlugin("UI");

        Assert.False(world.TryGetExtension<UIContext>(out _));
    }

    [Fact]
    public void Name_ReturnsUI()
    {
        var plugin = new UIPlugin();

        Assert.Equal("UI", plugin.Name);
    }

    [Fact]
    public void Install_RegistersCoreComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Test that we can create entities with core components
        var entity = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect())
            .With(new UIStyle())
            .With(UIInteractable.Button())
            .Build();

        Assert.True(world.Has<UIElement>(entity));
        Assert.True(world.Has<UIRect>(entity));
        Assert.True(world.Has<UIStyle>(entity));
        Assert.True(world.Has<UIInteractable>(entity));
    }

    [Fact]
    public void Install_RegistersTagComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Test that we can add tag components
        var entity = world.Spawn()
            .With(UIElement.Default)
            .WithTag<UIRootTag>()
            .WithTag<UIHiddenTag>()
            .WithTag<UIFocusedTag>()
            .Build();

        Assert.True(world.Has<UIRootTag>(entity));
        Assert.True(world.Has<UIHiddenTag>(entity));
        Assert.True(world.Has<UIFocusedTag>(entity));
    }

    [Fact]
    public void Install_RegistersTabComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify tab components can be created
        var entity = world.Spawn()
            .With(new UITabButton { TabIndex = 0 })
            .Build();

        Assert.True(world.Has<UITabButton>(entity));
    }

    [Fact]
    public void Install_RegistersWindowComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify window components can be created
        var entity = world.Spawn()
            .With(new UIWindow { Title = "Test Window" })
            .Build();

        Assert.True(world.Has<UIWindow>(entity));
    }

    [Fact]
    public void Install_RegistersMenuComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify menu components can be created
        var menu = world.Spawn()
            .With(new UIMenu { IsOpen = false })
            .Build();

        var menuItem = world.Spawn()
            .With(new UIMenuItem { Label = "Open" })
            .Build();

        Assert.True(world.Has<UIMenu>(menu));
        Assert.True(world.Has<UIMenuItem>(menuItem));
    }

    [Fact]
    public void Install_RegistersDockComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify dock components can be created
        var container = world.Spawn()
            .With(new UIDockContainer())
            .Build();

        var panel = world.Spawn()
            .With(new UIDockPanel { Title = "Test Panel", CurrentZone = DockZone.Left })
            .Build();

        Assert.True(world.Has<UIDockContainer>(container));
        Assert.True(world.Has<UIDockPanel>(panel));
    }

    [Fact]
    public void Install_RegistersInputWidgetComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify input widget components can be created
        var checkbox = world.Spawn()
            .With(new UICheckbox { IsChecked = true })
            .Build();

        var slider = world.Spawn()
            .With(new UISlider(minValue: 0f, maxValue: 1f, value: 0.5f))
            .Build();

        var textInput = world.Spawn()
            .With(UITextInput.SingleLine())
            .Build();

        Assert.True(world.Has<UICheckbox>(checkbox));
        Assert.True(world.Has<UISlider>(slider));
        Assert.True(world.Has<UITextInput>(textInput));
    }

    [Fact]
    public void Install_RegistersDataGridComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify data grid components can be created
        var dataGrid = world.Spawn()
            .With(new UIDataGrid())
            .Build();

        var column = world.Spawn()
            .With(new UIDataGridColumn { Header = "Column1", Width = 100f })
            .Build();

        Assert.True(world.Has<UIDataGrid>(dataGrid));
        Assert.True(world.Has<UIDataGridColumn>(column));
    }

    [Fact]
    public void Install_RegistersModalComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify modal components can be created
        var modal = world.Spawn()
            .With(new UIModal { Title = "Modal Title" })
            .Build();

        Assert.True(world.Has<UIModal>(modal));
    }

    [Fact]
    public void Install_RegistersToastComponents()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        world.InstallPlugin(plugin);

        // Verify toast components can be created
        var toast = world.Spawn()
            .With(new UIToast { Message = "Toast message", Duration = 3f })
            .Build();

        Assert.True(world.Has<UIToast>(toast));
    }

    [Fact]
    public void Install_CanBeCalledOnce()
    {
        using var world = new World();
        var plugin = new UIPlugin();

        // Installing the plugin should work without throwing
        var exception = Record.Exception(() => world.InstallPlugin(plugin));

        Assert.Null(exception);
        Assert.True(world.TryGetExtension<UIContext>(out _));
    }
}
