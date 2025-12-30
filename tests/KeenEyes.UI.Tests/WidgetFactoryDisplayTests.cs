using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory display widgets: Spinner, LoadingOverlay, ProgressBar.
/// </summary>
public class WidgetFactoryDisplayTests
{
    private static readonly FontHandle testFont = new(1);

    #region Spinner Tests

    [Fact]
    public void CreateSpinner_HasRequiredComponents()
    {
        using var world = new World();

        var spinner = WidgetFactory.CreateSpinner(world);

        Assert.True(world.Has<UIElement>(spinner));
        Assert.True(world.Has<UIRect>(spinner));
        Assert.True(world.Has<UISpinner>(spinner));
    }

    [Fact]
    public void CreateSpinner_AppliesDefaultSize()
    {
        using var world = new World();

        var spinner = WidgetFactory.CreateSpinner(world);

        ref readonly var rect = ref world.Get<UIRect>(spinner);
        Assert.Equal(40f, rect.Size.X);
        Assert.Equal(40f, rect.Size.Y);
    }

    [Fact]
    public void CreateSpinner_AppliesConfig()
    {
        using var world = new World();
        var config = new SpinnerConfig(Size: 64, Speed: 2.0f, Thickness: 4f);

        var spinner = WidgetFactory.CreateSpinner(world, config);

        ref readonly var rect = ref world.Get<UIRect>(spinner);
        Assert.Equal(64f, rect.Size.X);
        Assert.Equal(64f, rect.Size.Y);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(2.0f, spinnerData.Speed);
        Assert.Equal(4f, spinnerData.Thickness);
    }

    [Fact]
    public void CreateSpinner_WithParent_SetsParent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var spinner = WidgetFactory.CreateSpinner(world, parent);

        Assert.Equal(parent, world.GetParent(spinner));
    }

    [Fact]
    public void CreateSpinner_WithName_SetsEntityName()
    {
        using var world = new World();

        var spinner = WidgetFactory.CreateSpinner(world, "LoadingSpinner");

        Assert.Equal("LoadingSpinner", world.GetName(spinner));
    }

    [Fact]
    public void CreateSpinner_DefaultStyle_IsCircular()
    {
        using var world = new World();

        var spinner = WidgetFactory.CreateSpinner(world);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(SpinnerStyle.Circular, spinnerData.Style);
    }

    [Fact]
    public void CreateSpinner_DotsStyle_IsApplied()
    {
        using var world = new World();
        var config = new SpinnerConfig(Style: SpinnerStyle.Dots);

        var spinner = WidgetFactory.CreateSpinner(world, config);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(SpinnerStyle.Dots, spinnerData.Style);
    }

    [Fact]
    public void CreateSpinner_BarStyle_IsApplied()
    {
        using var world = new World();
        var config = new SpinnerConfig(Style: SpinnerStyle.Bar);

        var spinner = WidgetFactory.CreateSpinner(world, config);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(SpinnerStyle.Bar, spinnerData.Style);
    }

    [Fact]
    public void CreateSpinner_StoresArcLength()
    {
        using var world = new World();
        var config = new SpinnerConfig(ArcLength: 180f);

        var spinner = WidgetFactory.CreateSpinner(world, config);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(180f, spinnerData.ArcLength);
    }

    [Fact]
    public void CreateSpinner_StoresElementCount()
    {
        using var world = new World();
        var config = new SpinnerConfig(ElementCount: 12);

        var spinner = WidgetFactory.CreateSpinner(world, config);

        ref readonly var spinnerData = ref world.Get<UISpinner>(spinner);
        Assert.Equal(12, spinnerData.ElementCount);
    }

    [Fact]
    public void CreateSpinner_IsNotRaycastTarget()
    {
        using var world = new World();

        var spinner = WidgetFactory.CreateSpinner(world);

        ref readonly var element = ref world.Get<UIElement>(spinner);
        Assert.False(element.RaycastTarget);
    }

    #endregion

    #region SpinnerConfig Factory Methods Tests

    [Fact]
    public void SpinnerConfig_Small_SetsReducedSize()
    {
        var config = SpinnerConfig.Small();

        Assert.Equal(24f, config.Size);
    }

    [Fact]
    public void SpinnerConfig_Large_SetsIncreasedSize()
    {
        var config = SpinnerConfig.Large();

        Assert.Equal(64f, config.Size);
    }

    [Fact]
    public void SpinnerConfig_Dots_SetsDotsStyle()
    {
        var config = SpinnerConfig.Dots();

        Assert.Equal(SpinnerStyle.Dots, config.Style);
    }

    #endregion

    #region LoadingOverlay Tests

    [Fact]
    public void CreateLoadingOverlay_ReturnsOverlayAndSpinner()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, spinner) = WidgetFactory.CreateLoadingOverlay(world, parent);

        Assert.True(world.IsAlive(overlay));
        Assert.True(world.IsAlive(spinner));
    }

    [Fact]
    public void CreateLoadingOverlay_OverlayHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, _) = WidgetFactory.CreateLoadingOverlay(world, parent);

        Assert.True(world.Has<UIElement>(overlay));
        Assert.True(world.Has<UIRect>(overlay));
        Assert.True(world.Has<UIStyle>(overlay));
        Assert.True(world.Has<UILayout>(overlay));
    }

    [Fact]
    public void CreateLoadingOverlay_OverlayIsRaycastTarget()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, _) = WidgetFactory.CreateLoadingOverlay(world, parent);

        ref readonly var element = ref world.Get<UIElement>(overlay);
        Assert.True(element.RaycastTarget);
    }

    [Fact]
    public void CreateLoadingOverlay_OverlayHasBackdropColor()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var customBackdrop = new Vector4(0.2f, 0.2f, 0.2f, 0.8f);

        var (overlay, _) = WidgetFactory.CreateLoadingOverlay(world, parent, backdropColor: customBackdrop);

        ref readonly var style = ref world.Get<UIStyle>(overlay);
        Assert.Equal(customBackdrop, style.BackgroundColor);
    }

    [Fact]
    public void CreateLoadingOverlay_SpinnerIsChildOfOverlay()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, spinner) = WidgetFactory.CreateLoadingOverlay(world, parent);

        Assert.Equal(overlay, world.GetParent(spinner));
    }

    [Fact]
    public void CreateLoadingOverlay_DefaultBackdrop_IsSemiTransparentBlack()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, _) = WidgetFactory.CreateLoadingOverlay(world, parent);

        ref readonly var style = ref world.Get<UIStyle>(overlay);
        Assert.Equal(0f, style.BackgroundColor.X); // Black
        Assert.Equal(0f, style.BackgroundColor.Y);
        Assert.Equal(0f, style.BackgroundColor.Z);
        Assert.Equal(0.5f, style.BackgroundColor.W); // 50% alpha
    }

    [Fact]
    public void CreateLoadingOverlay_OverlayHasCenteredLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, _) = WidgetFactory.CreateLoadingOverlay(world, parent);

        ref readonly var layout = ref world.Get<UILayout>(overlay);
        Assert.Equal(LayoutAlign.Center, layout.MainAxisAlign);
        Assert.Equal(LayoutAlign.Center, layout.CrossAxisAlign);
    }

    [Fact]
    public void CreateLoadingOverlay_WithMessage_ReturnsThreeEntities()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, spinner, message) = WidgetFactory.CreateLoadingOverlay(
            world, parent, "Loading...", testFont);

        Assert.True(world.IsAlive(overlay));
        Assert.True(world.IsAlive(spinner));
        Assert.True(world.IsAlive(message));
    }

    [Fact]
    public void CreateLoadingOverlay_WithMessage_MessageHasText()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, _, message) = WidgetFactory.CreateLoadingOverlay(
            world, parent, "Please wait...", testFont);

        Assert.True(world.Has<UIText>(message));
        ref readonly var text = ref world.Get<UIText>(message);
        Assert.Equal("Please wait...", text.Content);
    }

    [Fact]
    public void CreateLoadingOverlay_WithMessage_MessageIsChildOfOverlay()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (overlay, _, message) = WidgetFactory.CreateLoadingOverlay(
            world, parent, "Loading...", testFont);

        Assert.Equal(overlay, world.GetParent(message));
    }

    [Fact]
    public void CreateLoadingOverlay_UsesLargeSpinnerByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, spinner) = WidgetFactory.CreateLoadingOverlay(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(spinner);
        Assert.Equal(64f, rect.Size.X); // Large spinner size
    }

    #endregion

    #region ProgressBar Tests

    [Fact]
    public void CreateProgressBar_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont);

        Assert.True(world.Has<UIElement>(progressBar));
        Assert.True(world.Has<UIRect>(progressBar));
        Assert.True(world.Has<UIStyle>(progressBar));
    }

    [Fact]
    public void CreateProgressBar_AppliesDefaultSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont);

        ref readonly var rect = ref world.Get<UIRect>(progressBar);
        Assert.Equal(200f, rect.Size.X);
        Assert.Equal(20f, rect.Size.Y);
    }

    [Fact]
    public void CreateProgressBar_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Width: 400, Height: 30);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(progressBar);
        Assert.Equal(400f, rect.Size.X);
        Assert.Equal(30f, rect.Size.Y);
    }

    [Fact]
    public void CreateProgressBar_HasFillChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont);

        var children = world.GetChildren(progressBar).ToList();
        Assert.True(children.Count >= 1); // At least the fill element
    }

    [Fact]
    public void CreateProgressBar_WithValue_SetsFillWidth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Value: 50f, MinValue: 0f, MaxValue: 100f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(0.5f, fillRect.AnchorMax.X, 2); // 50% fill
    }

    [Fact]
    public void CreateProgressBar_WithValue0_HasEmptyFill()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Value: 0f, MinValue: 0f, MaxValue: 100f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(0f, fillRect.AnchorMax.X);
    }

    [Fact]
    public void CreateProgressBar_WithValue100_HasFullFill()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Value: 100f, MinValue: 0f, MaxValue: 100f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(1f, fillRect.AnchorMax.X);
    }

    [Fact]
    public void CreateProgressBar_WithShowLabel_HasLabelChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(ShowLabel: true, Value: 75f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        // Should have fill and label children
        var children = world.GetChildren(progressBar).ToList();
        Assert.True(children.Count >= 2);

        // Find label (has UIText)
        var hasLabel = children.Any(c => world.Has<UIText>(c));
        Assert.True(hasLabel);
    }

    [Fact]
    public void CreateProgressBar_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, "DownloadProgress", testFont);

        Assert.Equal("DownloadProgress", world.GetName(progressBar));
    }

    [Fact]
    public void CreateProgressBar_AppliesCornerRadius()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(CornerRadius: 8f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        ref readonly var style = ref world.Get<UIStyle>(progressBar);
        Assert.Equal(8f, style.CornerRadius);
    }

    [Fact]
    public void CreateProgressBar_ValueClampsAboveMax()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Value: 150f, MinValue: 0f, MaxValue: 100f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(1f, fillRect.AnchorMax.X); // Clamped to 100%
    }

    [Fact]
    public void CreateProgressBar_ValueClampsBelowMin()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig(Value: -50f, MinValue: 0f, MaxValue: 100f);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(0f, fillRect.AnchorMax.X); // Clamped to 0%
    }

    #endregion

    #region ProgressBarConfig Factory Methods Tests

    [Fact]
    public void ProgressBarConfig_Default_HasDefaultValues()
    {
        var config = ProgressBarConfig.Default;

        Assert.Equal(200f, config.Width);
        Assert.Equal(20f, config.Height);
        Assert.Equal(0f, config.Value);
        Assert.Equal(0f, config.MinValue);
        Assert.Equal(100f, config.MaxValue);
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
