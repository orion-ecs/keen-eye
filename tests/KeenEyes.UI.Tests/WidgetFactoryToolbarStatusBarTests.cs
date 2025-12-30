using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory toolbar and status bar widget creation methods.
/// </summary>
public class WidgetFactoryToolbarStatusBarTests
{
    private static readonly FontHandle testFont = new(1);

    #region Toolbar Tests

    [Fact]
    public void CreateToolbar_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 1"))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        Assert.True(world.Has<UIElement>(toolbar));
        Assert.True(world.Has<UIRect>(toolbar));
        Assert.True(world.Has<UIStyle>(toolbar));
        Assert.True(world.Has<UILayout>(toolbar));
        Assert.True(world.Has<UIToolbar>(toolbar));
    }

    [Fact]
    public void CreateToolbar_HasHorizontalLayout_ByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[] { new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button")) };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        ref readonly var layout = ref world.Get<UILayout>(toolbar);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
    }

    [Fact]
    public void CreateToolbar_WithVerticalOrientation_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[] { new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button")) };
        var config = new ToolbarConfig(Orientation: LayoutDirection.Vertical);

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items, config);

        ref readonly var layout = ref world.Get<UILayout>(toolbar);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateToolbar_CreatesButtons()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 1")),
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 2")),
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 3"))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        ref readonly var toolbarData = ref world.Get<UIToolbar>(toolbar);
        Assert.Equal(3, toolbarData.ButtonCount);
    }

    [Fact]
    public void CreateToolbar_WithSeparator_CreatesSeparator()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 1")),
            new ToolbarItemDef.Separator(),
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button 2"))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        var children = world.GetChildren(toolbar).ToList();
        // Should have 3 children: button, separator, button
        Assert.Equal(3, children.Count);
        Assert.True(world.Has<UIToolbarSeparator>(children[1]));
    }

    [Fact]
    public void CreateToolbar_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[] { new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button")) };

        var toolbar = WidgetFactory.CreateToolbar(world, "MyToolbar", parent, items);

        Assert.Equal("MyToolbar", world.GetName(toolbar));
    }

    [Fact]
    public void CreateToolbar_ButtonsAreInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Button"))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        var children = world.GetChildren(toolbar).ToList();
        var button = children[0];
        Assert.True(world.Has<UIInteractable>(button));
    }

    [Fact]
    public void CreateToolbar_DisabledButton_HasDisabledTag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(IsEnabled: false))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        var children = world.GetChildren(toolbar).ToList();
        var button = children[0];
        Assert.True(world.Has<UIDisabledTag>(button));
    }

    [Fact]
    public void CreateToolbar_ToggleButton_HasToggleProperty()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Toggle", IsToggle: true))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);

        var children = world.GetChildren(toolbar).ToList();
        var button = children[0];
        ref readonly var toolbarButton = ref world.Get<UIToolbarButton>(button);
        Assert.True(toolbarButton.IsToggle);
    }

    [Fact]
    public void SetToolbarButtonPressed_UpdatesPressedState()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new ToolbarItemDef[]
        {
            new ToolbarItemDef.Button(new ToolbarButtonDef(Tooltip: "Toggle", IsToggle: true))
        };

        var toolbar = WidgetFactory.CreateToolbar(world, parent, items);
        var children = world.GetChildren(toolbar).ToList();
        var button = children[0];

        WidgetFactory.SetToolbarButtonPressed(world, button, true);

        ref readonly var toolbarButton = ref world.Get<UIToolbarButton>(button);
        Assert.True(toolbarButton.IsPressed);
    }

    [Fact]
    public void SetToolbarButtonPressed_WithInvalidEntity_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw when trying to set pressed on non-button entity
        WidgetFactory.SetToolbarButtonPressed(world, Entity.Null, true);
    }

    #endregion

    #region StatusBar Tests

    [Fact]
    public void CreateStatusBar_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new StatusBarSectionDef("Ready", 100)
        };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        Assert.True(world.Has<UIElement>(statusBar));
        Assert.True(world.Has<UIRect>(statusBar));
        Assert.True(world.Has<UIStyle>(statusBar));
        Assert.True(world.Has<UILayout>(statusBar));
        Assert.True(world.Has<UIStatusBar>(statusBar));
    }

    [Fact]
    public void CreateStatusBar_HasHorizontalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new StatusBarSectionDef("Ready", 100) };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        ref readonly var layout = ref world.Get<UILayout>(statusBar);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
    }

    [Fact]
    public void CreateStatusBar_CreatesSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new StatusBarSectionDef("Ready", 100),
            new StatusBarSectionDef("Line: 1", 80),
            new StatusBarSectionDef("Col: 1", 60)
        };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        ref readonly var statusBarData = ref world.Get<UIStatusBar>(statusBar);
        Assert.Equal(3, statusBarData.SectionCount);
    }

    [Fact]
    public void CreateStatusBar_CreatesSeparatorsBetweenSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new StatusBarSectionDef("Section 1", 100),
            new StatusBarSectionDef("Section 2", 100)
        };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        var children = world.GetChildren(statusBar).ToList();
        // Should have 3 children: section, separator, section
        Assert.Equal(3, children.Count);
    }

    [Fact]
    public void CreateStatusBar_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new StatusBarSectionDef("Ready", 100) };

        var statusBar = WidgetFactory.CreateStatusBar(world, "MyStatusBar", parent, testFont, sections);

        Assert.Equal("MyStatusBar", world.GetName(statusBar));
    }

    [Fact]
    public void CreateStatusBar_SectionHasText()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new StatusBarSectionDef("Hello World", 150) };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        var statusBarChildren = world.GetChildren(statusBar).ToList();
        var section = statusBarChildren[0];
        var sectionChildren = world.GetChildren(section).ToList();
        var textEntity = sectionChildren.FirstOrDefault(c => world.Has<UIText>(c));
        Assert.NotEqual(Entity.Null, textEntity);
        ref readonly var text = ref world.Get<UIText>(textEntity);
        Assert.Equal("Hello World", text.Content);
    }

    [Fact]
    public void CreateStatusBar_FlexibleSection_UsesFlexibleWidth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new StatusBarSectionDef("Flexible", 100, IsFlexible: true)
        };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        var statusBarChildren = world.GetChildren(statusBar).ToList();
        var section = statusBarChildren[0];
        ref readonly var rect = ref world.Get<UIRect>(section);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
    }

    [Fact]
    public void CreateStatusBar_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new StatusBarSectionDef("Ready", 100) };
        var config = new StatusBarConfig { Height = 30, FontSize = 14 };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections, config);

        ref readonly var rect = ref world.Get<UIRect>(statusBar);
        Assert.Equal(30, rect.Size.Y);
    }

    [Fact]
    public void SetStatusBarSectionText_UpdatesText()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new StatusBarSectionDef("Initial", 100) };

        var statusBar = WidgetFactory.CreateStatusBar(world, parent, testFont, sections);

        WidgetFactory.SetStatusBarSectionText(world, statusBar, 0, "Updated");

        // Find the text entity and verify it was updated
        var statusBarChildren = world.GetChildren(statusBar).ToList();
        var section = statusBarChildren[0];
        var sectionChildren = world.GetChildren(section).ToList();
        var textEntity = sectionChildren.FirstOrDefault(c => world.Has<UIText>(c));
        ref readonly var text = ref world.Get<UIText>(textEntity);
        Assert.Equal("Updated", text.Content);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateRootEntity(World world)
    {
        var root = world.Spawn("Root")
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    #endregion
}
