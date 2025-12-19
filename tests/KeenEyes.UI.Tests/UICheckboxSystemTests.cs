using System.Numerics;
using KeenEyes.Common;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UICheckboxSystem checkbox and toggle interactions.
/// </summary>
public class UICheckboxSystemTests
{
    #region Checkbox Tests

    [Fact]
    public void Checkbox_Click_TogglesCheckedState()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var checkbox = CreateCheckbox(world, false);

        // Simulate click
        SimulateClick(world, checkbox);
        system.Update(0);

        ref readonly var checkboxData = ref world.Get<UICheckbox>(checkbox);
        Assert.True(checkboxData.IsChecked);
    }

    [Fact]
    public void Checkbox_ClickWhenChecked_UnchecksCheckbox()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var checkbox = CreateCheckbox(world, true);

        // Simulate click
        SimulateClick(world, checkbox);
        system.Update(0);

        ref readonly var checkboxData = ref world.Get<UICheckbox>(checkbox);
        Assert.False(checkboxData.IsChecked);
    }

    [Fact]
    public void Checkbox_Click_UpdatesVisualStyle()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var checkbox = CreateCheckbox(world, false);

        // Get initial box color
        var boxEntity = world.Get<UICheckbox>(checkbox).BoxEntity;
        var initialColor = world.Get<UIStyle>(boxEntity).BackgroundColor;

        // Simulate click
        SimulateClick(world, checkbox);
        system.Update(0);

        // Color should change after checking
        var newColor = world.Get<UIStyle>(boxEntity).BackgroundColor;
        Assert.NotEqual(initialColor, newColor);
    }

    [Fact]
    public void Checkbox_UncheckedColor_MatchesExpected()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var checkbox = CreateCheckbox(world, false);

        // Trigger update
        SimulateClick(world, checkbox);
        system.Update(0);

        // Click again to uncheck
        SimulateClick(world, checkbox);
        system.Update(0);

        var boxEntity = world.Get<UICheckbox>(checkbox).BoxEntity;
        var color = world.Get<UIStyle>(boxEntity).BackgroundColor;

        var expectedUnchecked = new Vector4(0.2f, 0.2f, 0.2f, 1f);
        Assert.True(color.X.ApproximatelyEquals(expectedUnchecked.X));
        Assert.True(color.Y.ApproximatelyEquals(expectedUnchecked.Y));
        Assert.True(color.Z.ApproximatelyEquals(expectedUnchecked.Z));
    }

    [Fact]
    public void Checkbox_CheckedColor_MatchesExpected()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var checkbox = CreateCheckbox(world, false);

        // Simulate click to check
        SimulateClick(world, checkbox);
        system.Update(0);

        var boxEntity = world.Get<UICheckbox>(checkbox).BoxEntity;
        var color = world.Get<UIStyle>(boxEntity).BackgroundColor;

        var expectedChecked = new Vector4(0.2f, 0.6f, 1f, 1f);
        Assert.True(color.X.ApproximatelyEquals(expectedChecked.X));
        Assert.True(color.Y.ApproximatelyEquals(expectedChecked.Y));
        Assert.True(color.Z.ApproximatelyEquals(expectedChecked.Z));
    }

    #endregion

    #region Toggle Tests

    [Fact]
    public void Toggle_Click_TogglesOnState()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var toggle = CreateToggle(world, false);

        // Simulate click
        SimulateClick(world, toggle);
        system.Update(0);

        ref readonly var toggleData = ref world.Get<UIToggle>(toggle);
        Assert.True(toggleData.IsOn);
    }

    [Fact]
    public void Toggle_ClickWhenOn_TurnsOff()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var toggle = CreateToggle(world, true);

        // Simulate click
        SimulateClick(world, toggle);
        system.Update(0);

        ref readonly var toggleData = ref world.Get<UIToggle>(toggle);
        Assert.False(toggleData.IsOn);
    }

    [Fact]
    public void Toggle_Click_UpdatesTrackColor()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var toggle = CreateToggle(world, false);

        // Get initial track color
        var trackEntity = world.Get<UIToggle>(toggle).TrackEntity;
        var initialColor = world.Get<UIStyle>(trackEntity).BackgroundColor;

        // Simulate click
        SimulateClick(world, toggle);
        system.Update(0);

        // Color should change after turning on
        var newColor = world.Get<UIStyle>(trackEntity).BackgroundColor;
        Assert.NotEqual(initialColor, newColor);
    }

    [Fact]
    public void Toggle_Click_UpdatesThumbPosition()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var toggle = CreateToggle(world, false);

        // Get initial thumb position
        var thumbEntity = world.Get<UIToggle>(toggle).ThumbEntity;
        var initialOffset = world.Get<UIRect>(thumbEntity).Offset.Left;

        // Simulate click
        SimulateClick(world, toggle);
        system.Update(0);

        // Thumb position should change
        var newOffset = world.Get<UIRect>(thumbEntity).Offset.Left;
        Assert.NotEqual(initialOffset, newOffset);
    }

    [Fact]
    public void Toggle_Click_MarksLayoutDirty()
    {
        using var world = new World();
        var system = new UICheckboxSystem();
        world.AddSystem(system);
        system.Initialize(world);

        var toggle = CreateToggle(world, false);
        var thumbEntity = world.Get<UIToggle>(toggle).ThumbEntity;

        // Simulate click
        SimulateClick(world, toggle);
        system.Update(0);

        // Thumb should be marked dirty for layout recalculation
        Assert.True(world.Has<UILayoutDirtyTag>(thumbEntity));
    }

    #endregion

    #region Helper Methods

    private static Entity CreateCheckbox(World world, bool isChecked)
    {
        // Create box entity
        var box = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 20))
            .With(new UIStyle())
            .Build();

        // Create checkbox container
        var checkbox = world.Spawn()
            .With(UIElement.Default)
            .With(new UICheckbox(isChecked) { BoxEntity = box })
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(box, checkbox);

        return checkbox;
    }

    private static Entity CreateToggle(World world, bool isOn)
    {
        // Create track entity
        var track = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 50, 24))
            .With(new UIStyle())
            .Build();

        // Create thumb entity
        var thumb = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 20, 20))
            .With(new UIStyle())
            .Build();

        world.SetParent(thumb, track);

        // Create toggle container
        var toggle = world.Spawn()
            .With(UIElement.Default)
            .With(new UIToggle(isOn) { TrackEntity = track, ThumbEntity = thumb })
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(track, toggle);

        return toggle;
    }

    private static void SimulateClick(World world, Entity entity)
    {
        ref var interactable = ref world.Get<UIInteractable>(entity);
        interactable.PendingEvents |= UIEventFlags.Click;
    }

    #endregion
}
