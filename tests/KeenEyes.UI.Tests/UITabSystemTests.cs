using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UITabSystem tab switching and panel visibility.
/// </summary>
public class UITabSystemTests
{
    #region Tab Switching Tests

    [Fact]
    public void TabClick_SwitchesToNewTab()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 3);

        // Click second tab
        SimulateClick(world, tabButtons[1]);
        system.Update(0);

        ref readonly var state = ref world.Get<UITabViewState>(tabView);
        Assert.Equal(1, state.SelectedIndex);
    }

    [Fact]
    public void TabClick_ShowsCorrectPanel()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 3);

        // Click third tab
        SimulateClick(world, tabButtons[2]);
        system.Update(0);

        // Only third panel should be visible
        Assert.False(world.Get<UIElement>(panels[0]).Visible);
        Assert.False(world.Get<UIElement>(panels[1]).Visible);
        Assert.True(world.Get<UIElement>(panels[2]).Visible);
    }

    [Fact]
    public void TabClick_HidesPreviousPanel()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 3, initialTab: 0);

        // First panel should be visible initially
        Assert.True(world.Get<UIElement>(panels[0]).Visible);

        // Click second tab
        SimulateClick(world, tabButtons[1]);
        system.Update(0);

        // First panel should now be hidden
        Assert.False(world.Get<UIElement>(panels[0]).Visible);
        Assert.True(world.Has<UIHiddenTag>(panels[0]));
    }

    [Fact]
    public void TabClick_RemovesHiddenTagFromNewPanel()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 2, initialTab: 0);

        // Second panel should have hidden tag initially
        Assert.True(world.Has<UIHiddenTag>(panels[1]));

        // Click second tab
        SimulateClick(world, tabButtons[1]);
        system.Update(0);

        // Hidden tag should be removed
        Assert.False(world.Has<UIHiddenTag>(panels[1]));
    }

    [Fact]
    public void TabClick_OnAlreadyActiveTab_DoesNothing()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 2, initialTab: 0);

        // Click already active tab
        SimulateClick(world, tabButtons[0]);
        system.Update(0);

        ref readonly var state = ref world.Get<UITabViewState>(tabView);
        Assert.Equal(0, state.SelectedIndex);
        Assert.True(world.Get<UIElement>(panels[0]).Visible);
    }

    #endregion

    #region Tab Button Style Tests

    [Fact]
    public void TabClick_UpdatesActiveTabStyle()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 2);

        // Click second tab
        SimulateClick(world, tabButtons[1]);
        system.Update(0);

        // Check that second tab has active styling
        ref readonly var style = ref world.Get<UIStyle>(tabButtons[1]);
        ref readonly var text = ref world.Get<UIText>(tabButtons[1]);

        // Active tab should have specific background color
        var expectedActiveColor = new Vector4(0.15f, 0.15f, 0.2f, 1f);
        Assert.Equal(expectedActiveColor, style.BackgroundColor);

        // Active tab text should be white
        var expectedTextColor = new Vector4(1f, 1f, 1f, 1f);
        Assert.Equal(expectedTextColor, text.Color);
    }

    [Fact]
    public void TabClick_UpdatesInactiveTabStyle()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 2, initialTab: 0);

        // Click second tab
        SimulateClick(world, tabButtons[1]);
        system.Update(0);

        // Check that first tab now has inactive styling
        ref readonly var style = ref world.Get<UIStyle>(tabButtons[0]);
        ref readonly var text = ref world.Get<UIText>(tabButtons[0]);

        // Inactive tab should have specific background color
        var expectedInactiveColor = new Vector4(0.18f, 0.18f, 0.22f, 1f);
        Assert.Equal(expectedInactiveColor, style.BackgroundColor);

        // Inactive tab text should be gray
        var expectedTextColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
        Assert.Equal(expectedTextColor, text.Color);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TabClick_WithDeletedTabView_DoesNotCrash()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var (tabView, tabButtons, panels) = CreateTabView(world, 2);

        // Delete tab view
        world.Despawn(tabView);

        // Click should not crash
        SimulateClick(world, tabButtons[0]);
        system.Update(0);
    }

    [Fact]
    public void TabClick_WithNonTabButton_IsIgnored()
    {
        using var world = new World();
        var system = new UITabSystem();
        world.AddSystem(system);
        system.Initialize(world);

        // Create a regular button (not a tab button)
        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .Build();

        // Click should be ignored (no crash)
        SimulateClick(world, button);
        system.Update(0);
    }

    #endregion

    #region Helper Methods

    private static (Entity TabView, Entity[] TabButtons, Entity[] Panels) CreateTabView(
        World world, int tabCount, int initialTab = 0)
    {
        // Create tab view container
        var tabView = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UITabViewState(initialTab))
            .Build();

        var tabButtons = new Entity[tabCount];
        var panels = new Entity[tabCount];

        // Create tab buttons
        for (int i = 0; i < tabCount; i++)
        {
            tabButtons[i] = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed(i * 100, 0, 100, 30))
                .With(new UITabButton(i, tabView))
                .With(UIInteractable.Clickable())
                .With(new UIStyle())
                .With(new UIText { Content = $"Tab {i + 1}" })
                .Build();

            world.SetParent(tabButtons[i], tabView);
        }

        // Create panels
        for (int i = 0; i < tabCount; i++)
        {
            panels[i] = world.Spawn()
                .With(new UIElement { Visible = i == initialTab })
                .With(UIRect.Stretch())
                .With(new UITabPanel(i, tabView))
                .Build();

            world.SetParent(panels[i], tabView);

            // Add hidden tag to non-selected panels
            if (i != initialTab)
            {
                world.Add(panels[i], new UIHiddenTag());
            }
        }

        return (tabView, tabButtons, panels);
    }

    private static void SimulateClick(World world, Entity entity)
    {
        var clickEvent = new UIClickEvent(entity, Vector2.Zero, Input.Abstractions.MouseButton.Left);
        world.Send(clickEvent);
    }

    #endregion
}
