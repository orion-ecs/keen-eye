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

    #region TabView Tests

    [Fact]
    public void CreateTabView_ReturnsContainerAndContentPanels()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        Assert.True(world.IsAlive(tabView));
        Assert.Equal(2, contentPanels.Length);
        Assert.True(world.IsAlive(contentPanels[0]));
        Assert.True(world.IsAlive(contentPanels[1]));
    }

    [Fact]
    public void CreateTabView_ContainerHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        Assert.True(world.Has<UIElement>(tabView));
        Assert.True(world.Has<UIRect>(tabView));
        Assert.True(world.Has<UILayout>(tabView));
        Assert.True(world.Has<UITabViewState>(tabView));
    }

    [Fact]
    public void CreateTabView_SetsParent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        Assert.Equal(parent, world.GetParent(tabView));
    }

    [Fact]
    public void CreateTabView_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, "MyTabs", tabs, testFont);

        Assert.Equal("MyTabs", world.GetName(tabView));
    }

    [Fact]
    public void CreateTabView_WithName_NamesChildEntities()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(world, parent, "MyTabs", tabs, testFont);

        // Find tab bar by name
        var children = world.GetChildren(tabView).ToList();
        var tabBar = children.FirstOrDefault(c => world.GetName(c) == "MyTabs_TabBar");
        Assert.True(world.IsAlive(tabBar));

        // Find content area by name
        var contentArea = children.FirstOrDefault(c => world.GetName(c) == "MyTabs_Content");
        Assert.True(world.IsAlive(contentArea));
    }

    [Fact]
    public void CreateTabView_FirstTabIsSelectedByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        // First panel should be visible, second should be hidden
        ref readonly var panel0Element = ref world.Get<UIElement>(contentPanels[0]);
        ref readonly var panel1Element = ref world.Get<UIElement>(contentPanels[1]);

        Assert.True(panel0Element.Visible);
        Assert.False(panel1Element.Visible);
    }

    [Fact]
    public void CreateTabView_NonSelectedPanels_HaveHiddenTag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2"), new TabConfig("Tab3") };

        var (_, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        // First panel should NOT have hidden tag
        Assert.False(world.Has<UIHiddenTag>(contentPanels[0]));
        // Other panels SHOULD have hidden tag
        Assert.True(world.Has<UIHiddenTag>(contentPanels[1]));
        Assert.True(world.Has<UIHiddenTag>(contentPanels[2]));
    }

    [Fact]
    public void CreateTabView_WithSelectedIndex_SelectsCorrectTab()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2"), new TabConfig("Tab3") };
        var config = new TabViewConfig(SelectedIndex: 1);

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var tabState = ref world.Get<UITabViewState>(tabView);
        Assert.Equal(1, tabState.SelectedIndex);

        // Second panel should be visible
        ref readonly var panel1Element = ref world.Get<UIElement>(contentPanels[1]);
        Assert.True(panel1Element.Visible);
        Assert.False(world.Has<UIHiddenTag>(contentPanels[1]));
    }

    [Fact]
    public void CreateTabView_SelectedIndexClampedToValidRange()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };
        var config = new TabViewConfig(SelectedIndex: 10); // Out of range

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var tabState = ref world.Get<UITabViewState>(tabView);
        Assert.Equal(1, tabState.SelectedIndex); // Clamped to max valid index
    }

    [Fact]
    public void CreateTabView_WithWidthOnly_SetsFixedWidthFillHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };
        var config = new TabViewConfig(Width: 400);

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(tabView);
        Assert.Equal(400f, rect.Size.X);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateTabView_WithHeightOnly_SetsFillWidthFixedHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };
        var config = new TabViewConfig(Height: 300);

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(tabView);
        Assert.Equal(300f, rect.Size.Y);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateTabView_WithWidthAndHeight_SetsFixedBoth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };
        var config = new TabViewConfig(Width: 400, Height: 300);

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(tabView);
        Assert.Equal(400f, rect.Size.X);
        Assert.Equal(300f, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateTabView_WithNoSize_UsesStretch()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        ref readonly var rect = ref world.Get<UIRect>(tabView);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateTabView_TabsHaveUITabButtonComponent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        // Find tab buttons
        var children = world.GetChildren(tabView).ToList();
        var tabBar = children.First(); // Tab bar is first child
        var tabButtons = world.GetChildren(tabBar).ToList();

        Assert.Equal(2, tabButtons.Count);
        foreach (var button in tabButtons)
        {
            Assert.True(world.Has<UITabButton>(button));
            Assert.True(world.Has<UIInteractable>(button));
        }
    }

    [Fact]
    public void CreateTabView_ContentPanelsHaveUITabPanelComponent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };

        var (_, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        foreach (var panel in contentPanels)
        {
            Assert.True(world.Has<UITabPanel>(panel));
        }
    }

    [Fact]
    public void CreateTabView_TabSpacing_IsApplied()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1"), new TabConfig("Tab2") };
        var config = new TabViewConfig(TabSpacing: 10f);

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        // Find tab bar and check layout spacing
        var children = world.GetChildren(tabView).ToList();
        var tabBar = children.First();
        ref readonly var layout = ref world.Get<UILayout>(tabBar);
        Assert.Equal(10f, layout.Spacing);
    }

    [Fact]
    public void CreateTabView_ContentAreaHasClipTag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        // Content area is second child of container
        var children = world.GetChildren(tabView).ToList();
        var contentArea = children[1]; // Second child
        Assert.True(world.Has<UIClipChildrenTag>(contentArea));
    }

    #endregion

    #region ScrollView Tests

    [Fact]
    public void CreateScrollView_ReturnsContainerAndContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, contentPanel) = WidgetFactory.CreateScrollView(world, parent);

        Assert.True(world.IsAlive(scrollView));
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateScrollView_ContainerHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent);

        Assert.True(world.Has<UIElement>(scrollView));
        Assert.True(world.Has<UIRect>(scrollView));
        Assert.True(world.Has<UIStyle>(scrollView));
        Assert.True(world.Has<UIScrollable>(scrollView));
        Assert.True(world.Has<UIClipChildrenTag>(scrollView));
    }

    [Fact]
    public void CreateScrollView_SetsParent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent);

        Assert.Equal(parent, world.GetParent(scrollView));
    }

    [Fact]
    public void CreateScrollView_ContentPanelIsChildOfContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, contentPanel) = WidgetFactory.CreateScrollView(world, parent);

        Assert.Equal(scrollView, world.GetParent(contentPanel));
    }

    [Fact]
    public void CreateScrollView_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, "MyScrollView");

        Assert.Equal("MyScrollView", world.GetName(scrollView));
    }

    [Fact]
    public void CreateScrollView_WithName_NamesContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, contentPanel) = WidgetFactory.CreateScrollView(world, parent, "MyScrollView");

        Assert.Equal("MyScrollView_Content", world.GetName(contentPanel));
    }

    [Fact]
    public void CreateScrollView_WithWidthOnly_SetsFixedWidthFillHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(Width: 300);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(scrollView);
        Assert.Equal(300f, rect.Size.X);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_WithHeightOnly_SetsFillWidthFixedHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(Height: 200);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(scrollView);
        Assert.Equal(200f, rect.Size.Y);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_WithWidthAndHeight_SetsFixedBoth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(Width: 300, Height: 200);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(scrollView);
        Assert.Equal(300f, rect.Size.X);
        Assert.Equal(200f, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_WithNoSize_UsesStretch()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(scrollView);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_WithContentSize_SetsScrollableContentSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ContentWidth: 800, ContentHeight: 1200);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.Equal(800f, scrollable.ContentSize.X);
        Assert.Equal(1200f, scrollable.ContentSize.Y);
    }

    [Fact]
    public void CreateScrollView_WithVerticalScrollbar_CreatesScrollbar()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        // Should have content panel and vertical scrollbar
        var children = world.GetChildren(scrollView).ToList();
        Assert.True(children.Count >= 2);

        // Find scrollbar with thumb
        var hasThumb = children.Any(c =>
        {
            var grandchildren = world.GetChildren(c).ToList();
            return grandchildren.Any(gc => world.Has<UIScrollbarThumb>(gc));
        });
        Assert.True(hasThumb);
    }

    [Fact]
    public void CreateScrollView_WithHorizontalScrollbar_CreatesScrollbar()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowHorizontalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        // Should have content panel and horizontal scrollbar
        var children = world.GetChildren(scrollView).ToList();
        Assert.True(children.Count >= 2);
    }

    [Fact]
    public void CreateScrollView_WithBothScrollbars_CreatesBothScrollbars()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true, ShowHorizontalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        // Should have content panel and two scrollbars
        var children = world.GetChildren(scrollView).ToList();
        Assert.True(children.Count >= 3);
    }

    [Fact]
    public void CreateScrollView_ScrollbarThumb_HasInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        // Find thumb
        var thumbs = world.Query<UIScrollbarThumb>().ToList();
        Assert.Single(thumbs);

        var thumb = thumbs[0];
        Assert.True(world.Has<UIInteractable>(thumb));
        ref readonly var interactable = ref world.Get<UIInteractable>(thumb);
        Assert.True(interactable.CanDrag);
    }

    [Fact]
    public void CreateScrollView_ScrollbarThumb_HasCorrectOrientation()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true, ShowHorizontalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        // Find thumbs
        var thumbs = world.Query<UIScrollbarThumb>().ToList();
        Assert.Equal(2, thumbs.Count);

        var hasVertical = thumbs.Any(t =>
        {
            ref readonly var thumb = ref world.Get<UIScrollbarThumb>(t);
            return thumb.IsVertical;
        });
        var hasHorizontal = thumbs.Any(t =>
        {
            ref readonly var thumb = ref world.Get<UIScrollbarThumb>(t);
            return !thumb.IsVertical;
        });

        Assert.True(hasVertical);
        Assert.True(hasHorizontal);
    }

    [Fact]
    public void CreateScrollView_ContentPanel_HasLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, contentPanel) = WidgetFactory.CreateScrollView(world, parent);

        Assert.True(world.Has<UILayout>(contentPanel));
        ref readonly var layout = ref world.Get<UILayout>(contentPanel);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateScrollView_ContentPanel_SizeModesBasedOnConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ContentWidth: 500, ContentHeight: 1000);

        var (_, contentPanel) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(contentPanel);
        Assert.Equal(500f, rect.Size.X);
        Assert.Equal(1000f, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_ContentPanel_FillsWhenNoContentSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (_, contentPanel) = WidgetFactory.CreateScrollView(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(contentPanel);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateScrollView_WithName_NamesScrollbars()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true, ShowHorizontalScrollbar: true);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, "MyScroll", config);

        var children = world.GetChildren(scrollView).ToList();

        // Find by name
        var vScrollbar = children.FirstOrDefault(c => world.GetName(c) == "MyScroll_VScrollbar");
        var hScrollbar = children.FirstOrDefault(c => world.GetName(c) == "MyScroll_HScrollbar");

        Assert.True(world.IsAlive(vScrollbar));
        Assert.True(world.IsAlive(hScrollbar));
    }

    [Fact]
    public void CreateScrollView_ScrollableFlags_MatchConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig(ShowVerticalScrollbar: true, ShowHorizontalScrollbar: false);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.True(scrollable.VerticalScroll);
        Assert.False(scrollable.HorizontalScroll);
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
