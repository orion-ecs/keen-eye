using System.Numerics;
using KeenEyes.Common;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UISpinnerSystem animation updates.
/// </summary>
public class UISpinnerSystemTests
{
    #region Spinner Rotation Tests

    [Fact]
    public void Spinner_Update_RotatesBasedOnSpeed()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: MathF.PI);
        var initialAngle = world.Get<UISpinner>(spinner).CurrentAngle;

        // Update with 1 second delta
        system.Update(1.0f);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.True(spinnerData.CurrentAngle.ApproximatelyEquals(initialAngle + MathF.PI));
    }

    [Fact]
    public void Spinner_Update_WrapsAngleAtTwoPi()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        // Start at angle close to 2*PI
        var spinner = CreateSpinner(world, speed: MathF.PI * 2);
        ref var spinnerData = ref world.Get<UISpinner>(spinner);
        spinnerData.CurrentAngle = MathF.PI * 1.9f;

        // Update should wrap around
        system.Update(0.1f);

        ref readonly var updatedSpinner = ref world.Get<UISpinner>(spinner);
        Assert.True(updatedSpinner.CurrentAngle < MathF.PI * 2);
        Assert.True(updatedSpinner.CurrentAngle >= 0);
    }

    [Fact]
    public void Spinner_Hidden_SkipsUpdate()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: MathF.PI * 2);
        world.Add(spinner, new UIHiddenTag());
        var initialAngle = world.Get<UISpinner>(spinner).CurrentAngle;

        system.Update(1.0f);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.True(spinnerData.CurrentAngle.ApproximatelyEquals(initialAngle));
    }

    [Fact]
    public void Spinner_NegativeSpeed_RotatesBackward()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: -MathF.PI);
        ref var spinnerData = ref world.Get<UISpinner>(spinner);
        spinnerData.CurrentAngle = MathF.PI;

        system.Update(0.5f);

        ref readonly var updatedSpinner = ref world.Get<UISpinner>(spinner);
        Assert.True(updatedSpinner.CurrentAngle.ApproximatelyEquals(MathF.PI / 2));
    }

    [Fact]
    public void Spinner_ZeroSpeed_NoRotation()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: 0);
        var initialAngle = world.Get<UISpinner>(spinner).CurrentAngle;

        system.Update(1.0f);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.True(spinnerData.CurrentAngle.ApproximatelyEquals(initialAngle));
    }

    [Fact]
    public void Spinner_MultipleSpinners_AllUpdate()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner1 = CreateSpinner(world, speed: MathF.PI);
        var spinner2 = CreateSpinner(world, speed: MathF.PI * 2);

        system.Update(1.0f);

        ref readonly var spinner1Data = ref world.Get<UISpinner>(spinner1);
        ref readonly var spinner2Data = ref world.Get<UISpinner>(spinner2);
        Assert.True(spinner1Data.CurrentAngle.ApproximatelyEquals(MathF.PI));
        Assert.True(spinner2Data.CurrentAngle.ApproximatelyEquals(MathF.PI * 2));
    }

    #endregion

    #region Spinner Helper Methods Tests

    [Fact]
    public void ResetSpinner_SetsAngleToZero()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: MathF.PI * 2);
        system.Update(1.0f); // Advance angle

        system.ResetSpinner(spinner);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.True(spinnerData.CurrentAngle.IsApproximatelyZero());
    }

    [Fact]
    public void SetSpinnerSpeed_ChangesSpeed()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var spinner = CreateSpinner(world, speed: MathF.PI);
        system.SetSpinnerSpeed(spinner, MathF.PI * 4);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.True(spinnerData.Speed.ApproximatelyEquals(MathF.PI * 4));
    }

    [Fact]
    public void ResetSpinner_InvalidEntity_NoException()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        // Should not throw
        system.ResetSpinner(Entity.Null);
    }

    [Fact]
    public void SetSpinnerSpeed_InvalidEntity_NoException()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        // Should not throw
        system.SetSpinnerSpeed(Entity.Null, MathF.PI);
    }

    #endregion

    #region Progress Bar Tests

    [Fact]
    public void ProgressBar_Update_InterpolatesValue()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0f, animationSpeed: 1f);

        // Set target value
        ref var barData = ref world.Get<UIProgressBar>(progressBar);
        barData.Value = 1f;

        // Update should interpolate
        system.Update(0.5f);

        ref readonly var updatedBar = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(updatedBar.AnimatedValue.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void ProgressBar_Update_ReachesTarget()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0f, animationSpeed: 1f);

        ref var barData = ref world.Get<UIProgressBar>(progressBar);
        barData.Value = 0.5f;

        // Update with enough time to reach target
        system.Update(1.0f);

        ref readonly var updatedBar = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(updatedBar.AnimatedValue.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void ProgressBar_Hidden_SkipsUpdate()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0f, animationSpeed: 1f);
        world.Add(progressBar, new UIHiddenTag());

        ref var barData = ref world.Get<UIProgressBar>(progressBar);
        barData.Value = 1f;

        system.Update(0.5f);

        ref readonly var updatedBar = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(updatedBar.AnimatedValue.IsApproximatelyZero());
    }

    [Fact]
    public void ProgressBar_AlreadyAtTarget_NoChange()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0.5f, animationSpeed: 1f);
        var initialAnimatedValue = world.Get<UIProgressBar>(progressBar).AnimatedValue;

        // Update with value already at target
        system.Update(1.0f);

        ref readonly var updatedBar = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(updatedBar.AnimatedValue.ApproximatelyEquals(initialAnimatedValue));
    }

    [Fact]
    public void ProgressBar_Decrease_AnimatesCorrectly()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 1f, animationSpeed: 2f);

        ref var barData = ref world.Get<UIProgressBar>(progressBar);
        barData.Value = 0f;

        // Update should decrease
        system.Update(0.25f);

        ref readonly var updatedBar = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(updatedBar.AnimatedValue.ApproximatelyEquals(0.5f));
    }

    #endregion

    #region SetProgress Tests

    [Fact]
    public void SetProgress_UpdatesValue()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0f, animationSpeed: 1f);

        system.SetProgress(progressBar, 0.75f);

        ref readonly var barData = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(barData.Value.ApproximatelyEquals(0.75f));
    }

    [Fact]
    public void SetProgress_ClampsToZeroOne()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0.5f, animationSpeed: 1f);

        system.SetProgress(progressBar, 1.5f);

        ref readonly var barData = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(barData.Value.ApproximatelyEquals(1f));
    }

    [Fact]
    public void SetProgress_NegativeValue_ClampsToZero()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0.5f, animationSpeed: 1f);

        system.SetProgress(progressBar, -0.5f);

        ref readonly var barData = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(barData.Value.IsApproximatelyZero());
    }

    [Fact]
    public void SetProgress_InvalidEntity_NoException()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        // Should not throw
        system.SetProgress(Entity.Null, 0.5f);
    }

    #endregion

    #region SetProgressImmediate Tests

    [Fact]
    public void SetProgressImmediate_SetsValueAndAnimatedValue()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0f, animationSpeed: 1f);

        system.SetProgressImmediate(progressBar, 0.75f);

        ref readonly var barData = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(barData.Value.ApproximatelyEquals(0.75f));
        Assert.True(barData.AnimatedValue.ApproximatelyEquals(0.75f));
    }

    [Fact]
    public void SetProgressImmediate_ClampsValue()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        var progressBar = CreateProgressBar(world, value: 0.5f, animationSpeed: 1f);

        system.SetProgressImmediate(progressBar, 2.0f);

        ref readonly var barData = ref world.Get<UIProgressBar>(progressBar);
        Assert.True(barData.Value.ApproximatelyEquals(1f));
        Assert.True(barData.AnimatedValue.ApproximatelyEquals(1f));
    }

    [Fact]
    public void SetProgressImmediate_InvalidEntity_NoException()
    {
        using var world = new World();
        var system = new UISpinnerSystem();
        world.AddSystem(system);

        // Should not throw
        system.SetProgressImmediate(Entity.Null, 0.5f);
    }

    #endregion

    #region Component Default Tests

    [Fact]
    public void UISpinner_WithDefaultSpeed_HasExpectedValues()
    {
        // Note: Primary constructors require explicit invocation for defaults
        var spinner = new UISpinner(MathF.PI * 2);

        Assert.Equal(SpinnerStyle.Circular, spinner.Style);
        Assert.True(spinner.Speed.ApproximatelyEquals(MathF.PI * 2));
        Assert.True(spinner.CurrentAngle.IsApproximatelyZero());
        Assert.True(spinner.Thickness.ApproximatelyEquals(3f));
        Assert.True(spinner.ArcLength.ApproximatelyEquals(0.75f));
        Assert.Equal(8, spinner.ElementCount);
    }

    [Fact]
    public void UISpinner_CustomSpeed_SetsSpeed()
    {
        var spinner = new UISpinner(MathF.PI * 4);

        Assert.True(spinner.Speed.ApproximatelyEquals(MathF.PI * 4));
    }

    [Fact]
    public void UIProgressBar_WithDefaultValue_HasExpectedValues()
    {
        // Note: Primary constructors require explicit invocation for defaults
        var progressBar = new UIProgressBar(0f);

        Assert.True(progressBar.Value.IsApproximatelyZero());
        Assert.True(progressBar.AnimatedValue.IsApproximatelyZero());
        Assert.True(progressBar.AnimationSpeed.ApproximatelyEquals(5f));
        Assert.False(progressBar.ShowText);
    }

    [Fact]
    public void UIProgressBar_WithValue_ClampsAndInitializes()
    {
        var progressBar = new UIProgressBar(0.5f);

        Assert.True(progressBar.Value.ApproximatelyEquals(0.5f));
        Assert.True(progressBar.AnimatedValue.ApproximatelyEquals(0.5f));
    }

    [Fact]
    public void UIProgressBar_ValueOverOne_ClampedToOne()
    {
        var progressBar = new UIProgressBar(1.5f);

        Assert.True(progressBar.Value.ApproximatelyEquals(1f));
        Assert.True(progressBar.AnimatedValue.ApproximatelyEquals(1f));
    }

    [Fact]
    public void UIProgressBar_NegativeValue_ClampedToZero()
    {
        var progressBar = new UIProgressBar(-0.5f);

        Assert.True(progressBar.Value.IsApproximatelyZero());
        Assert.True(progressBar.AnimatedValue.IsApproximatelyZero());
    }

    #endregion

    #region Helper Methods

    private static Entity CreateSpinner(World world, float speed = MathF.PI * 2)
    {
        return world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(40, 40) })
            .With(new UISpinner(speed))
            .Build();
    }

    private static Entity CreateProgressBar(World world, float value = 0f, float animationSpeed = 5f)
    {
        var progressBar = new UIProgressBar(value)
        {
            AnimationSpeed = animationSpeed
        };

        return world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect { Size = new Vector2(200, 20) })
            .With(progressBar)
            .Build();
    }

    #endregion
}
