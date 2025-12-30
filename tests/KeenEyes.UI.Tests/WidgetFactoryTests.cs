using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the WidgetFactory widget creation methods.
/// </summary>
public class WidgetFactoryTests
{
    // Default font handle for tests
    private static readonly FontHandle testFont = new(1);

    #region Button Tests

    [Fact]
    public void CreateButton_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var button = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        Assert.True(world.Has<UIElement>(button));
        Assert.True(world.Has<UIRect>(button));
        Assert.True(world.Has<UIStyle>(button));
        Assert.True(world.Has<UIText>(button));
        Assert.True(world.Has<UIInteractable>(button));
    }

    [Fact]
    public void CreateButton_SetsTextContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var button = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        ref readonly var text = ref world.Get<UIText>(button);
        Assert.Equal("Click Me", text.Content);
    }

    [Fact]
    public void CreateButton_IsInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var button = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.True(interactable.CanClick);
        Assert.True(interactable.CanFocus);
    }

    [Fact]
    public void CreateButton_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ButtonConfig { Width = 200, Height = 50, FontSize = 18 };

        var button = WidgetFactory.CreateButton(world, parent, "Click Me", testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(button);
        Assert.Equal(new Vector2(200, 50), rect.Size);
        ref readonly var text = ref world.Get<UIText>(button);
        Assert.Equal(18, text.FontSize);
    }

    [Fact]
    public void CreateButton_SetsParentRelationship()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var button = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        Assert.Equal(parent, world.GetParent(button));
    }

    [Fact]
    public void CreateButton_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var button = WidgetFactory.CreateButton(world, parent, "SubmitButton", "Submit", testFont);

        Assert.Equal("SubmitButton", world.GetName(button));
    }

    [Fact]
    public void CreateButton_WithNoParent_DoesNotSetParent()
    {
        using var world = new World();

        var button = WidgetFactory.CreateButton(world, Entity.Null, "Click Me", testFont);

        Assert.Equal(Entity.Null, world.GetParent(button));
    }

    #endregion

    #region Panel Tests

    [Fact]
    public void CreatePanel_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var panel = WidgetFactory.CreatePanel(world, parent);

        Assert.True(world.Has<UIElement>(panel));
        Assert.True(world.Has<UIRect>(panel));
        Assert.True(world.Has<UIStyle>(panel));
        Assert.True(world.Has<UILayout>(panel));
    }

    [Fact]
    public void CreatePanel_IsNotRaycastTarget()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var panel = WidgetFactory.CreatePanel(world, parent);

        ref readonly var element = ref world.Get<UIElement>(panel);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreatePanel_StretchesWhenNoSizeSpecified()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var panel = WidgetFactory.CreatePanel(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreatePanel_AppliesFixedSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PanelConfig { Width = 300, Height = 200 };

        var panel = WidgetFactory.CreatePanel(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(new Vector2(300, 200), rect.Size);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreatePanel_AppliesLayoutConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PanelConfig
        {
            Direction = LayoutDirection.Horizontal,
            Spacing = 15f
        };

        var panel = WidgetFactory.CreatePanel(world, parent, config);

        ref readonly var layout = ref world.Get<UILayout>(panel);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
        Assert.Equal(15f, layout.Spacing);
    }

    [Fact]
    public void CreatePanel_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var panel = WidgetFactory.CreatePanel(world, parent, "MainPanel");

        Assert.Equal("MainPanel", world.GetName(panel));
    }

    [Fact]
    public void CreatePanel_WithWidthOnly_SetsFixedWidthFillHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PanelConfig { Width = 400 };

        var panel = WidgetFactory.CreatePanel(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(400, rect.Size.X);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreatePanel_WithHeightOnly_SetsFillWidthFixedHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PanelConfig { Height = 300 };

        var panel = WidgetFactory.CreatePanel(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(300, rect.Size.Y);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreatePanel_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PanelConfig { Width = 500, Spacing = 20f };

        var panel = WidgetFactory.CreatePanel(world, parent, "ConfiguredPanel", config);

        Assert.Equal("ConfiguredPanel", world.GetName(panel));
        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(500, rect.Size.X);
        ref readonly var layout = ref world.Get<UILayout>(panel);
        Assert.Equal(20f, layout.Spacing);
    }

    #endregion

    #region Label Tests

    [Fact]
    public void CreateLabel_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var label = WidgetFactory.CreateLabel(world, parent, "Hello", testFont);

        Assert.True(world.Has<UIElement>(label));
        Assert.True(world.Has<UIRect>(label));
        Assert.True(world.Has<UIText>(label));
    }

    [Fact]
    public void CreateLabel_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var label = WidgetFactory.CreateLabel(world, parent, "Hello", testFont);

        Assert.False(world.Has<UIInteractable>(label));
        ref readonly var element = ref world.Get<UIElement>(label);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateLabel_SetsTextContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var label = WidgetFactory.CreateLabel(world, parent, "Hello World", testFont);

        ref readonly var text = ref world.Get<UIText>(label);
        Assert.Equal("Hello World", text.Content);
    }

    [Fact]
    public void CreateLabel_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new LabelConfig
        {
            FontSize = 24,
            HorizontalAlign = TextAlignH.Right
        };

        var label = WidgetFactory.CreateLabel(world, parent, "Test", testFont, config);

        ref readonly var text = ref world.Get<UIText>(label);
        Assert.Equal(24, text.FontSize);
        Assert.Equal(TextAlignH.Right, text.HorizontalAlign);
    }

    #endregion

    #region TextField Tests

    [Fact]
    public void CreateTextField_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var textField = WidgetFactory.CreateTextField(world, parent, testFont);

        Assert.True(world.Has<UIElement>(textField));
        Assert.True(world.Has<UIRect>(textField));
        Assert.True(world.Has<UIStyle>(textField));
        Assert.True(world.Has<UIText>(textField));
        Assert.True(world.Has<UIInteractable>(textField));
    }

    [Fact]
    public void CreateTextField_IsInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var textField = WidgetFactory.CreateTextField(world, parent, testFont);

        ref readonly var interactable = ref world.Get<UIInteractable>(textField);
        Assert.True(interactable.CanClick);
        Assert.True(interactable.CanFocus);
    }

    [Fact]
    public void CreateTextField_AppliesPlaceholder()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new TextFieldConfig { PlaceholderText = "Enter name..." };

        var textField = WidgetFactory.CreateTextField(world, parent, testFont, config);

        ref readonly var text = ref world.Get<UIText>(textField);
        Assert.Equal("Enter name...", text.Content);
    }

    [Fact]
    public void CreateTextField_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var textField = WidgetFactory.CreateTextField(world, parent, "EmailField", testFont);

        Assert.Equal("EmailField", world.GetName(textField));
    }

    [Fact]
    public void CreateTextField_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new TextFieldConfig { Width = 400, PlaceholderText = "Search..." };

        var textField = WidgetFactory.CreateTextField(world, parent, "SearchBox", testFont, config);

        Assert.Equal("SearchBox", world.GetName(textField));
        ref readonly var rect = ref world.Get<UIRect>(textField);
        Assert.Equal(400, rect.Size.X);
        ref readonly var input = ref world.Get<UITextInput>(textField);
        Assert.Equal("Search...", input.PlaceholderText);
    }

    #endregion

    #region Checkbox Tests

    [Fact]
    public void CreateCheckbox_HasContainerWithLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var checkbox = WidgetFactory.CreateCheckbox(world, parent, "Accept Terms", testFont);

        Assert.True(world.Has<UILayout>(checkbox));
        ref readonly var layout = ref world.Get<UILayout>(checkbox);
        Assert.Equal(LayoutDirection.Horizontal, layout.Direction);
    }

    [Fact]
    public void CreateCheckbox_HasChildEntities()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var checkbox = WidgetFactory.CreateCheckbox(world, parent, "Accept Terms", testFont);

        var children = world.GetChildren(checkbox).ToList();
        Assert.Equal(2, children.Count); // Box and Label
    }

    [Fact]
    public void CreateCheckbox_ContainerIsInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var checkbox = WidgetFactory.CreateCheckbox(world, parent, "Accept Terms", testFont);

        ref readonly var interactable = ref world.Get<UIInteractable>(checkbox);
        Assert.True(interactable.CanClick);
        Assert.True(interactable.CanFocus);
    }

    [Fact]
    public void CreateCheckbox_WithName_SetsChildNames()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var checkbox = WidgetFactory.CreateCheckbox(world, parent, "TermsCheckbox", "Accept Terms", testFont);

        var children = world.GetChildren(checkbox).ToList();
        Assert.Equal("TermsCheckbox_Box", world.GetName(children[0]));
        Assert.Equal("TermsCheckbox_Label", world.GetName(children[1]));
    }

    #endregion

    #region Slider Tests

    [Fact]
    public void CreateSlider_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var slider = WidgetFactory.CreateSlider(world, parent);

        Assert.True(world.Has<UIElement>(slider));
        Assert.True(world.Has<UIRect>(slider));
        Assert.True(world.Has<UIInteractable>(slider));
        Assert.True(world.Has<UIScrollable>(slider));
    }

    [Fact]
    public void CreateSlider_HasTrackFillAndThumb()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var slider = WidgetFactory.CreateSlider(world, parent);

        var children = world.GetChildren(slider).ToList();
        Assert.Equal(3, children.Count); // Track, Fill, Thumb
    }

    [Fact]
    public void CreateSlider_StoresNormalizedValue()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new SliderConfig { MinValue = 0, MaxValue = 100, Value = 50 };

        var slider = WidgetFactory.CreateSlider(world, parent, config);

        ref readonly var scrollable = ref world.Get<UIScrollable>(slider);
        Assert.Equal(0.5f, scrollable.ScrollPosition.X, 3);
    }

    [Fact]
    public void CreateSlider_CanDrag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var slider = WidgetFactory.CreateSlider(world, parent);

        ref readonly var interactable = ref world.Get<UIInteractable>(slider);
        Assert.True(interactable.CanDrag);
    }

    [Fact]
    public void CreateSlider_WithName_SetsChildNames()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var slider = WidgetFactory.CreateSlider(world, parent, "VolumeSlider");

        var children = world.GetChildren(slider).ToList();
        Assert.Equal("VolumeSlider_Track", world.GetName(children[0]));
        Assert.Equal("VolumeSlider_Fill", world.GetName(children[1]));
        Assert.Equal("VolumeSlider_Thumb", world.GetName(children[2]));
    }

    [Fact]
    public void CreateSlider_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new SliderConfig { MinValue = 0, MaxValue = 100, Value = 50, Width = 250 };

        var slider = WidgetFactory.CreateSlider(world, parent, "BrightnessSlider", config);

        Assert.Equal("BrightnessSlider", world.GetName(slider));
        ref readonly var sliderData = ref world.Get<UISlider>(slider);
        Assert.Equal(50f, sliderData.Value, 2);
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
    public void CreateProgressBar_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont);

        ref readonly var element = ref world.Get<UIElement>(progressBar);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateProgressBar_HasFillChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont);

        var children = world.GetChildren(progressBar).ToList();
        Assert.True(children.Count >= 1); // At least fill
    }

    [Fact]
    public void CreateProgressBar_WithLabel_HasLabelChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig { ShowLabel = true, Value = 75 };

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        Assert.Equal(2, children.Count); // Fill and Label
    }

    [Fact]
    public void CreateProgressBar_FillReflectsValue()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ProgressBarConfig { MinValue = 0, MaxValue = 100, Value = 25 };

        var progressBar = WidgetFactory.CreateProgressBar(world, parent, testFont, config);

        var children = world.GetChildren(progressBar).ToList();
        var fill = children[0];
        ref readonly var fillRect = ref world.Get<UIRect>(fill);
        Assert.Equal(0.25f, fillRect.AnchorMax.X, 3);
    }

    #endregion

    #region Toggle Tests

    [Fact]
    public void CreateToggle_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var toggle = WidgetFactory.CreateToggle(world, parent, "Enable Feature", testFont);

        Assert.True(world.Has<UIElement>(toggle));
        Assert.True(world.Has<UILayout>(toggle));
        Assert.True(world.Has<UIInteractable>(toggle));
    }

    [Fact]
    public void CreateToggle_HasTrackThumbAndLabel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var toggle = WidgetFactory.CreateToggle(world, parent, "Enable Feature", testFont);

        var children = world.GetChildren(toggle).ToList();
        Assert.Equal(2, children.Count); // Track and Label (thumb is child of track)
    }

    [Fact]
    public void CreateToggle_ReflectsInitialState()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ToggleConfig { IsOn = true };

        var toggle = WidgetFactory.CreateToggle(world, parent, "Enable Feature", testFont, config);

        ref readonly var interactable = ref world.Get<UIInteractable>(toggle);
        Assert.True(interactable.IsPressed); // Pressed state indicates "on"
    }

    [Fact]
    public void CreateToggle_WithName_SetsChildNames()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var toggle = WidgetFactory.CreateToggle(world, parent, "DarkMode", "Dark Mode", testFont);

        var children = world.GetChildren(toggle).ToList();
        Assert.Equal("DarkMode_Track", world.GetName(children[0]));
        Assert.Equal("DarkMode_Label", world.GetName(children[1]));
    }

    [Fact]
    public void CreateToggle_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ToggleConfig { IsOn = true };

        var toggle = WidgetFactory.CreateToggle(world, parent, "Feature", "Feature", testFont, config);

        Assert.Equal("Feature", world.GetName(toggle));
        ref readonly var interactable = ref world.Get<UIInteractable>(toggle);
        Assert.True(interactable.IsPressed);
    }

    #endregion

    #region Dropdown Tests

    [Fact]
    public void CreateDropdown_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2", "Option 3" };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, items, testFont);

        Assert.True(world.Has<UIElement>(dropdown));
        Assert.True(world.Has<UILayout>(dropdown));
        Assert.True(world.Has<UIInteractable>(dropdown));
        Assert.True(world.Has<UIScrollable>(dropdown));
    }

    [Fact]
    public void CreateDropdown_HasHeaderChildren()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2" };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, items, testFont);

        var children = world.GetChildren(dropdown).ToList();
        Assert.Equal(3, children.Count); // Selected label, arrow, dropdown list
    }

    [Fact]
    public void CreateDropdown_ListIsInitiallyHidden()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2" };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, items, testFont);

        var children = world.GetChildren(dropdown).ToList();
        var list = children[2]; // The dropdown list
        ref readonly var element = ref world.Get<UIElement>(list);
        Assert.False(element.Visible);
    }

    [Fact]
    public void CreateDropdown_StoresSelectedIndex()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2", "Option 3" };
        var config = new DropdownConfig { SelectedIndex = 1 };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, items, testFont, config);

        ref readonly var scrollable = ref world.Get<UIScrollable>(dropdown);
        Assert.Equal(1, (int)scrollable.ScrollPosition.X);
    }

    [Fact]
    public void CreateDropdown_ListHasCorrectItemCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "A", "B", "C", "D" };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, items, testFont);

        var headerChildren = world.GetChildren(dropdown).ToList();
        var list = headerChildren[2];
        var listItems = world.GetChildren(list).ToList();
        Assert.Equal(4, listItems.Count);
    }

    #endregion

    #region TabView Tests

    [Fact]
    public void CreateTabView_ReturnsContainerAndContentPanels()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab 1"), new TabConfig("Tab 2") };

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        Assert.True(world.IsAlive(tabView));
        Assert.Equal(2, contentPanels.Length);
        Assert.All(contentPanels, p => Assert.True(world.IsAlive(p)));
    }

    [Fact]
    public void CreateTabView_HasTabBarAndContentArea()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("Tab 1") };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont);

        var children = world.GetChildren(tabView).ToList();
        Assert.Equal(2, children.Count); // Tab bar and content area
    }

    [Fact]
    public void CreateTabView_OnlySelectedTabIsVisible()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[]
        {
            new TabConfig("Tab 1"),
            new TabConfig("Tab 2"),
            new TabConfig("Tab 3")
        };
        var config = new TabViewConfig { SelectedIndex = 1 };

        var (_, contentPanels) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        Assert.False(world.Get<UIElement>(contentPanels[0]).Visible);
        Assert.True(world.Get<UIElement>(contentPanels[1]).Visible);
        Assert.False(world.Get<UIElement>(contentPanels[2]).Visible);
    }

    [Fact]
    public void CreateTabView_StoresSelectedIndex()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var tabs = new[] { new TabConfig("A"), new TabConfig("B") };
        var config = new TabViewConfig { SelectedIndex = 1 };

        var (tabView, _) = WidgetFactory.CreateTabView(world, parent, tabs, testFont, config);

        ref readonly var tabViewState = ref world.Get<UITabViewState>(tabView);
        Assert.Equal(1, tabViewState.SelectedIndex);
    }

    #endregion

    #region Divider Tests

    [Fact]
    public void CreateDivider_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var divider = WidgetFactory.CreateDivider(world, parent);

        Assert.True(world.Has<UIElement>(divider));
        Assert.True(world.Has<UIRect>(divider));
        Assert.True(world.Has<UIStyle>(divider));
    }

    [Fact]
    public void CreateDivider_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var divider = WidgetFactory.CreateDivider(world, parent);

        ref readonly var element = ref world.Get<UIElement>(divider);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateDivider_HorizontalFillsWidth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DividerConfig { Orientation = LayoutDirection.Horizontal };

        var divider = WidgetFactory.CreateDivider(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(divider);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateDivider_VerticalFillsHeight()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DividerConfig { Orientation = LayoutDirection.Vertical };

        var divider = WidgetFactory.CreateDivider(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(divider);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    [Fact]
    public void CreateDivider_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var divider = WidgetFactory.CreateDivider(world, parent, "Separator");

        Assert.Equal("Separator", world.GetName(divider));
    }

    [Fact]
    public void CreateDivider_WithName_AppliesVerticalConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DividerConfig { Orientation = LayoutDirection.Vertical, Thickness = 3 };

        var divider = WidgetFactory.CreateDivider(world, parent, "VerticalDivider", config);

        Assert.Equal("VerticalDivider", world.GetName(divider));
        ref readonly var rect = ref world.Get<UIRect>(divider);
        Assert.Equal(3, rect.Size.X);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
    }

    [Fact]
    public void CreateDivider_HorizontalWithFixedLength_SetsSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DividerConfig { Orientation = LayoutDirection.Horizontal, Length = 200, Thickness = 2 };

        var divider = WidgetFactory.CreateDivider(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(divider);
        Assert.Equal(200, rect.Size.X);
        Assert.Equal(2, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    [Fact]
    public void CreateDivider_VerticalWithFixedLength_SetsSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DividerConfig { Orientation = LayoutDirection.Vertical, Length = 150, Thickness = 2 };

        var divider = WidgetFactory.CreateDivider(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(divider);
        Assert.Equal(2, rect.Size.X);
        Assert.Equal(150, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
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
    public void CreateScrollView_HasScrollableComponent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent);

        Assert.True(world.Has<UIScrollable>(scrollView));
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
    public void CreateScrollView_WithVerticalScrollbar_HasScrollbarChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig { ShowVerticalScrollbar = true, ShowHorizontalScrollbar = false };

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        var children = world.GetChildren(scrollView).ToList();
        Assert.Equal(2, children.Count); // Content panel and vertical scrollbar
    }

    [Fact]
    public void CreateScrollView_WithBothScrollbars_HasTwoScrollbarChildren()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig { ShowVerticalScrollbar = true, ShowHorizontalScrollbar = true };

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        var children = world.GetChildren(scrollView).ToList();
        Assert.Equal(3, children.Count); // Content panel and both scrollbars
    }

    [Fact]
    public void CreateScrollView_WithName_SetsChildNames()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig { ShowVerticalScrollbar = true };

        var (scrollView, contentPanel) = WidgetFactory.CreateScrollView(world, parent, "MyScrollView", config);

        Assert.Equal("MyScrollView", world.GetName(scrollView));
        Assert.Equal("MyScrollView_Content", world.GetName(contentPanel));
    }

    [Fact]
    public void CreateScrollView_AppliesContentSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ScrollViewConfig { ContentWidth = 800, ContentHeight = 600 };

        var (scrollView, _) = WidgetFactory.CreateScrollView(world, parent, config);

        ref readonly var scrollable = ref world.Get<UIScrollable>(scrollView);
        Assert.Equal(new Vector2(800, 600), scrollable.ContentSize);
    }

    #endregion

    #region Image Tests

    [Fact]
    public void CreateImage_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(1);

        var image = WidgetFactory.CreateImage(world, parent, texture);

        Assert.True(world.Has<UIElement>(image));
        Assert.True(world.Has<UIRect>(image));
        Assert.True(world.Has<UIImage>(image));
    }

    [Fact]
    public void CreateImage_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(1);

        var image = WidgetFactory.CreateImage(world, parent, texture);

        ref readonly var element = ref world.Get<UIElement>(image);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateImage_SetsTexture()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(42);

        var image = WidgetFactory.CreateImage(world, parent, texture);

        ref readonly var uiImage = ref world.Get<UIImage>(image);
        Assert.Equal(texture, uiImage.Texture);
    }

    [Fact]
    public void CreateImage_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(1);
        var config = new ImageConfig
        {
            Width = 128,
            Height = 96,
            ScaleMode = ImageScaleMode.Stretch,
            PreserveAspect = false
        };

        var image = WidgetFactory.CreateImage(world, parent, texture, config);

        ref readonly var rect = ref world.Get<UIRect>(image);
        Assert.Equal(new Vector2(128, 96), rect.Size);
        ref readonly var uiImage = ref world.Get<UIImage>(image);
        Assert.Equal(ImageScaleMode.Stretch, uiImage.ScaleMode);
        Assert.False(uiImage.PreserveAspect);
    }

    [Fact]
    public void CreateImage_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(1);

        var image = WidgetFactory.CreateImage(world, parent, "Icon", texture);

        Assert.Equal("Icon", world.GetName(image));
    }

    #endregion

    #region Card Tests

    [Fact]
    public void CreateCard_ReturnsCardAndContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (card, content) = WidgetFactory.CreateCard(world, parent, "Title", testFont);

        Assert.True(world.IsAlive(card));
        Assert.True(world.IsAlive(content));
    }

    [Fact]
    public void CreateCard_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (card, _) = WidgetFactory.CreateCard(world, parent, "Title", testFont);

        Assert.True(world.Has<UIElement>(card));
        Assert.True(world.Has<UIRect>(card));
        Assert.True(world.Has<UIStyle>(card));
        Assert.True(world.Has<UILayout>(card));
    }

    [Fact]
    public void CreateCard_HasTitleBarAndContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (card, content) = WidgetFactory.CreateCard(world, parent, "Title", testFont);

        var children = world.GetChildren(card).ToList();
        Assert.Equal(2, children.Count); // Title bar and content area
        Assert.Contains(content, children);
    }

    [Fact]
    public void CreateCard_TitleBarHasText()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (card, _) = WidgetFactory.CreateCard(world, parent, "My Card", testFont);

        var children = world.GetChildren(card).ToList();
        var titleBar = children[0];
        ref readonly var text = ref world.Get<UIText>(titleBar);
        Assert.Equal("My Card", text.Content);
    }

    [Fact]
    public void CreateCard_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new CardConfig { Width = 400, TitleHeight = 50, CornerRadius = 12 };

        var (card, _) = WidgetFactory.CreateCard(world, parent, "Title", testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(card);
        Assert.Equal(400, rect.Size.X);
        ref readonly var style = ref world.Get<UIStyle>(card);
        Assert.Equal(12, style.CornerRadius);
    }

    #endregion

    #region Badge Tests

    [Fact]
    public void CreateBadge_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var badge = WidgetFactory.CreateBadge(world, parent, 5, testFont);

        Assert.True(world.Has<UIElement>(badge));
        Assert.True(world.Has<UIRect>(badge));
        Assert.True(world.Has<UIStyle>(badge));
        Assert.True(world.Has<UIText>(badge));
    }

    [Fact]
    public void CreateBadge_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var badge = WidgetFactory.CreateBadge(world, parent, 5, testFont);

        ref readonly var element = ref world.Get<UIElement>(badge);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateBadge_DisplaysValue()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var badge = WidgetFactory.CreateBadge(world, parent, 7, testFont);

        ref readonly var text = ref world.Get<UIText>(badge);
        Assert.Equal("7", text.Content);
    }

    [Fact]
    public void CreateBadge_CapsValueAtMax()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new BadgeConfig { MaxValue = 99 };

        var badge = WidgetFactory.CreateBadge(world, parent, 150, testFont, config);

        ref readonly var text = ref world.Get<UIText>(badge);
        Assert.Equal("99+", text.Content);
    }

    [Fact]
    public void CreateBadge_IsCircular()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new BadgeConfig { Size = 30 };

        var badge = WidgetFactory.CreateBadge(world, parent, 1, testFont, config);

        ref readonly var style = ref world.Get<UIStyle>(badge);
        Assert.Equal(15, style.CornerRadius); // Half of size for circular
    }

    #endregion

    #region Avatar Tests

    [Fact]
    public void CreateAvatar_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var avatar = WidgetFactory.CreateAvatar(world, parent, testFont);

        Assert.True(world.Has<UIElement>(avatar));
        Assert.True(world.Has<UIRect>(avatar));
        Assert.True(world.Has<UIStyle>(avatar));
    }

    [Fact]
    public void CreateAvatar_IsNotInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var avatar = WidgetFactory.CreateAvatar(world, parent, testFont);

        ref readonly var element = ref world.Get<UIElement>(avatar);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateAvatar_WithImage_HasUIImage()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var texture = new TextureHandle(1);
        var config = new AvatarConfig { Image = texture };

        var avatar = WidgetFactory.CreateAvatar(world, parent, testFont, config);

        Assert.True(world.Has<UIImage>(avatar));
        ref readonly var image = ref world.Get<UIImage>(avatar);
        Assert.Equal(texture, image.Texture);
    }

    [Fact]
    public void CreateAvatar_WithFallbackText_HasUIText()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AvatarConfig { FallbackText = "JD" };

        var avatar = WidgetFactory.CreateAvatar(world, parent, testFont, config);

        Assert.True(world.Has<UIText>(avatar));
        ref readonly var text = ref world.Get<UIText>(avatar);
        Assert.Equal("JD", text.Content);
    }

    [Fact]
    public void CreateAvatar_AppliesSize()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AvatarConfig { Size = 80, CornerRadius = 40 };

        var avatar = WidgetFactory.CreateAvatar(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(avatar);
        Assert.Equal(new Vector2(80, 80), rect.Size);
        ref readonly var style = ref world.Get<UIStyle>(avatar);
        Assert.Equal(40, style.CornerRadius);
    }

    #endregion

    #region Tooltip Tests

    [Fact]
    public void AddTooltip_AddsTooltipComponent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var element = WidgetFactory.CreateButton(world, parent, "Hover Me", testFont);

        WidgetFactory.AddTooltip(world, element, "This is a tooltip");

        Assert.True(world.Has<UITooltip>(element));
        ref readonly var tooltip = ref world.Get<UITooltip>(element);
        Assert.Equal("This is a tooltip", tooltip.Text);
    }

    [Fact]
    public void AddTooltip_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var element = WidgetFactory.CreateButton(world, parent, "Hover Me", testFont);
        var config = new TooltipConfig
        {
            Delay = 1.5f,
            MaxWidth = 250,
            Position = TooltipPosition.Below,
            FollowMouse = true
        };

        WidgetFactory.AddTooltip(world, element, "Tooltip Text", config);

        ref readonly var tooltip = ref world.Get<UITooltip>(element);
        Assert.Equal(1.5f, tooltip.Delay);
        Assert.Equal(250, tooltip.MaxWidth);
        Assert.Equal(TooltipPosition.Below, tooltip.Position);
        Assert.True(tooltip.FollowMouse);
    }

    [Fact]
    public void RemoveTooltip_RemovesTooltipComponent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var element = WidgetFactory.CreateButton(world, parent, "Hover Me", testFont);
        WidgetFactory.AddTooltip(world, element, "Tooltip");

        WidgetFactory.RemoveTooltip(world, element);

        Assert.False(world.Has<UITooltip>(element));
    }

    [Fact]
    public void RemoveTooltip_WhenNoTooltip_DoesNotThrow()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var element = WidgetFactory.CreateButton(world, parent, "Hover Me", testFont);

        // Should not throw
        WidgetFactory.RemoveTooltip(world, element);
    }

    #endregion

    #region Popover Tests

    [Fact]
    public void CreatePopover_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        var popover = WidgetFactory.CreatePopover(world, trigger);

        Assert.True(world.Has<UIElement>(popover));
        Assert.True(world.Has<UIRect>(popover));
        Assert.True(world.Has<UIStyle>(popover));
        Assert.True(world.Has<UILayout>(popover));
        Assert.True(world.Has<UIPopover>(popover));
    }

    [Fact]
    public void CreatePopover_InitiallyHidden()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        var popover = WidgetFactory.CreatePopover(world, trigger);

        ref readonly var element = ref world.Get<UIElement>(popover);
        Assert.False(element.Visible);
        ref readonly var popoverData = ref world.Get<UIPopover>(popover);
        Assert.False(popoverData.IsOpen);
    }

    [Fact]
    public void CreatePopover_SetsTriggerElement()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        var popover = WidgetFactory.CreatePopover(world, trigger);

        ref readonly var popoverData = ref world.Get<UIPopover>(popover);
        Assert.Equal(trigger, popoverData.TriggerElement);
    }

    [Fact]
    public void CreatePopover_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);
        var config = new PopoverConfig
        {
            Width = 300,
            Height = 200,
            CornerRadius = 8,
            Trigger = PopoverTrigger.Hover,
            Position = TooltipPosition.Left,
            CloseOnClickOutside = false
        };

        var popover = WidgetFactory.CreatePopover(world, trigger, config);

        ref readonly var rect = ref world.Get<UIRect>(popover);
        Assert.Equal(new Vector2(300, 200), rect.Size);
        ref readonly var style = ref world.Get<UIStyle>(popover);
        Assert.Equal(8, style.CornerRadius);
        ref readonly var popoverData = ref world.Get<UIPopover>(popover);
        Assert.Equal(PopoverTrigger.Hover, popoverData.Trigger);
        Assert.Equal(TooltipPosition.Left, popoverData.Position);
        Assert.False(popoverData.CloseOnClickOutside);
    }

    [Fact]
    public void CreatePopover_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);

        var popover = WidgetFactory.CreatePopover(world, "HelpPopover", trigger);

        Assert.Equal("HelpPopover", world.GetName(popover));
    }

    [Fact]
    public void OpenPopover_MakesPopoverVisible()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);
        var popover = WidgetFactory.CreatePopover(world, trigger);

        WidgetFactory.OpenPopover(world, popover);

        ref readonly var element = ref world.Get<UIElement>(popover);
        Assert.True(element.Visible);
        ref readonly var popoverData = ref world.Get<UIPopover>(popover);
        Assert.True(popoverData.IsOpen);
    }

    [Fact]
    public void ClosePopover_HidesPopover()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var trigger = WidgetFactory.CreateButton(world, parent, "Click Me", testFont);
        var popover = WidgetFactory.CreatePopover(world, trigger);
        WidgetFactory.OpenPopover(world, popover);

        WidgetFactory.ClosePopover(world, popover);

        ref readonly var element = ref world.Get<UIElement>(popover);
        Assert.False(element.Visible);
        ref readonly var popoverData = ref world.Get<UIPopover>(popover);
        Assert.False(popoverData.IsOpen);
    }

    #endregion

    #region Dropdown Named Overload Tests

    [Fact]
    public void CreateDropdown_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2" };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, "CountryDropdown", items, testFont);

        Assert.Equal("CountryDropdown", world.GetName(dropdown));
    }

    [Fact]
    public void CreateDropdown_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var items = new[] { "Option 1", "Option 2", "Option 3" };
        var config = new DropdownConfig { Width = 250, SelectedIndex = 1 };

        var dropdown = WidgetFactory.CreateDropdown(world, parent, "LanguageDropdown", items, testFont, config);

        Assert.Equal("LanguageDropdown", world.GetName(dropdown));
        ref readonly var rect = ref world.Get<UIRect>(dropdown);
        Assert.Equal(250f, rect.Size.X);
        ref readonly var scrollable = ref world.Get<UIScrollable>(dropdown);
        Assert.Equal(1, (int)scrollable.ScrollPosition.X);
    }

    #endregion

    #region ColorPicker Tests

    [Fact]
    public void CreateColorPicker_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent);

        Assert.True(world.Has<UIElement>(colorPicker));
        Assert.True(world.Has<UIRect>(colorPicker));
        Assert.True(world.Has<UIStyle>(colorPicker));
        Assert.True(world.Has<UILayout>(colorPicker));
        Assert.True(world.Has<UIColorPicker>(colorPicker));
    }

    [Fact]
    public void CreateColorPicker_HasChildComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent);

        var children = world.GetChildren(colorPicker);
        Assert.True(children.Count() >= 2); // At least satval area and hue slider
    }

    [Fact]
    public void CreateColorPicker_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig { Width = 300, Height = 280, ShowAlpha = true, ShowPreview = true };

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(colorPicker);
        Assert.Equal(new Vector2(300, 280), rect.Size);
        ref readonly var picker = ref world.Get<UIColorPicker>(colorPicker);
        Assert.True(picker.ShowAlpha);
    }

    [Fact]
    public void CreateColorPicker_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent, "BackgroundColorPicker");

        Assert.Equal("BackgroundColorPicker", world.GetName(colorPicker));
    }

    [Fact]
    public void CreateColorPicker_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new ColorPickerConfig { ShowAlpha = true, Mode = ColorPickerMode.HSV };

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent, "ThemeColorPicker", config);

        Assert.Equal("ThemeColorPicker", world.GetName(colorPicker));
        ref readonly var picker = ref world.Get<UIColorPicker>(colorPicker);
        Assert.True(picker.ShowAlpha);
        Assert.Equal(ColorPickerMode.HSV, picker.Mode);
    }

    [Fact]
    public void CreateColorPicker_SetsParent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var colorPicker = WidgetFactory.CreateColorPicker(world, parent);

        Assert.Equal(parent, world.GetParent(colorPicker));
    }

    #endregion

    #region DatePicker Tests

    [Fact]
    public void CreateDatePicker_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont);

        Assert.True(world.Has<UIElement>(datePicker));
        Assert.True(world.Has<UIRect>(datePicker));
        Assert.True(world.Has<UILayout>(datePicker));
        Assert.True(world.Has<UIStyle>(datePicker));
        Assert.True(world.Has<UIDatePicker>(datePicker));
    }

    [Fact]
    public void CreateDatePicker_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig { Width = 320, Height = 350, Mode = DatePickerMode.DateTime };

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(datePicker);
        Assert.Equal(new Vector2(320, 350), rect.Size);
        ref readonly var picker = ref world.Get<UIDatePicker>(datePicker);
        Assert.Equal(DatePickerMode.DateTime, picker.Mode);
    }

    [Fact]
    public void CreateDatePicker_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, "BirthdayPicker");

        Assert.Equal("BirthdayPicker", world.GetName(datePicker));
    }

    [Fact]
    public void CreateDatePicker_WithName_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig { Mode = DatePickerMode.Time, ShowSeconds = true };

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, "AppointmentPicker", config);

        Assert.Equal("AppointmentPicker", world.GetName(datePicker));
        ref readonly var picker = ref world.Get<UIDatePicker>(datePicker);
        Assert.Equal(DatePickerMode.Time, picker.Mode);
        Assert.True(picker.ShowSeconds);
    }

    [Fact]
    public void CreateDatePicker_SetsParent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont);

        Assert.Equal(parent, world.GetParent(datePicker));
    }

    [Fact]
    public void CreateDatePicker_TimeModeCreatesTimeSpinners()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig { Mode = DatePickerMode.Time };

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var picker = ref world.Get<UIDatePicker>(datePicker);
        Assert.NotEqual(Entity.Null, picker.HourEntity);
        Assert.NotEqual(Entity.Null, picker.MinuteEntity);
    }

    [Fact]
    public void CreateDatePicker_DateModeCreatesCalendarGrid()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig { Mode = DatePickerMode.Date };

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var picker = ref world.Get<UIDatePicker>(datePicker);
        Assert.NotEqual(Entity.Null, picker.CalendarGridEntity);
        Assert.NotEqual(Entity.Null, picker.HeaderEntity);
    }

    [Fact]
    public void CreateDatePicker_12HourFormat_HasAmPmSpinner()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DatePickerConfig { Mode = DatePickerMode.Time, TimeFormat = TimeFormat.Hour12 };

        var datePicker = WidgetFactory.CreateDatePicker(world, parent, testFont, config);

        ref readonly var picker = ref world.Get<UIDatePicker>(datePicker);
        Assert.NotEqual(Entity.Null, picker.AmPmEntity);
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

        // Create and run layout system to compute bounds
        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    #endregion
}
