using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Extended tests for WidgetConfig record types to improve coverage.
/// </summary>
public sealed class WidgetConfigExtendedTests
{
    #region UIWindowConfig Tests

    [Fact]
    public void UIWindowConfig_Default_HasExpectedValues()
    {
        var config = UIWindowConfig.Default;

        Assert.Equal(400f, config.Width);
        Assert.Equal(300f, config.Height);
        Assert.True(config.CanClose);
        Assert.False(config.CanResize);
        Assert.True(config.CanDrag);
        Assert.Equal(150f, config.MinWidth);
        Assert.Equal(100f, config.MinHeight);
    }

    [Fact]
    public void UIWindowConfig_GetTitleTextColor_ReturnsWhiteWhenNull()
    {
        var config = new UIWindowConfig();
        var color = config.GetTitleTextColor();
        Assert.Equal(Vector4.One, color);
    }

    [Fact]
    public void UIWindowConfig_GetTitleBarColor_ReturnsDefaultWhenNull()
    {
        var config = new UIWindowConfig();
        var color = config.GetTitleBarColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void UIWindowConfig_GetContentColor_ReturnsDefaultWhenNull()
    {
        var config = new UIWindowConfig();
        var color = config.GetContentColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void UIWindowConfig_GetCloseButtonColor_ReturnsValue()
    {
        var config = new UIWindowConfig();
        _ = config.GetCloseButtonColor();
    }

    [Fact]
    public void UIWindowConfig_GetCloseButtonHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new UIWindowConfig();
        var color = config.GetCloseButtonHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void UIWindowConfig_GetMinimizeButtonColor_ReturnsValue()
    {
        var config = new UIWindowConfig();
        _ = config.GetMinimizeButtonColor();
    }

    [Fact]
    public void UIWindowConfig_GetMaximizeButtonColor_ReturnsValue()
    {
        var config = new UIWindowConfig();
        _ = config.GetMaximizeButtonColor();
    }

    #endregion

    #region SplitterConfig Tests

    [Fact]
    public void SplitterConfig_Default_HasExpectedValues()
    {
        var config = SplitterConfig.Default;

        Assert.Equal(4f, config.HandleSize);
        Assert.Equal(LayoutDirection.Horizontal, config.Orientation);
        Assert.Equal(0.5f, config.InitialRatio);
    }

    [Fact]
    public void SplitterConfig_GetHandleColor_ReturnsDefaultWhenNull()
    {
        var config = new SplitterConfig();
        var color = config.GetHandleColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void SplitterConfig_GetHandleHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new SplitterConfig();
        var color = config.GetHandleHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region MenuBarConfig Tests

    [Fact]
    public void MenuBarConfig_Default_HasExpectedValues()
    {
        var config = MenuBarConfig.Default;

        Assert.Equal(28f, config.Height);
    }

    [Fact]
    public void MenuBarConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuBarConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region MenuConfig Tests

    [Fact]
    public void MenuConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region RadialMenuConfig Tests

    [Fact]
    public void RadialMenuConfig_Default_HasExpectedValues()
    {
        var config = RadialMenuConfig.Default;

        Assert.Equal(40f, config.InnerRadius);
        Assert.Equal(120f, config.OuterRadius);
    }

    #endregion

    #region DockContainerConfig Tests

    [Fact]
    public void DockContainerConfig_Default_HasExpectedValues()
    {
        var config = DockContainerConfig.Default;

        Assert.Equal(4f, config.SplitterSize);
    }

    #endregion

    #region DockPanelConfig Tests

    [Fact]
    public void DockPanelConfig_Default_CanClose()
    {
        var config = new DockPanelConfig();

        Assert.True(config.CanClose);
    }

    #endregion

    #region ToolbarConfig Tests

    [Fact]
    public void ToolbarConfig_Default_HasExpectedValues()
    {
        var config = ToolbarConfig.Default;

        // Just verify it has a default
        Assert.NotNull(config);
    }

    #endregion

    #region ToolbarSeparatorDef Tests

    [Fact]
    public void ToolbarSeparatorDef_CanBeCreated()
    {
        var sep = new ToolbarSeparatorDef();

        Assert.NotNull(sep);
    }

    #endregion

    #region StatusBarConfig Tests

    [Fact]
    public void StatusBarConfig_Default_HasExpectedValues()
    {
        var config = StatusBarConfig.Default;

        Assert.Equal(24f, config.Height);
    }

    #endregion

    #region TreeViewConfig Tests

    [Fact]
    public void TreeViewConfig_Default_HasExpectedValues()
    {
        var config = TreeViewConfig.Default;

        Assert.Equal(20f, config.IndentSize);
    }

    #endregion

    #region PropertyGridConfig Tests

    [Fact]
    public void PropertyGridConfig_Default_HasExpectedValues()
    {
        var config = PropertyGridConfig.Default;

        Assert.NotNull(config);
    }

    #endregion

    #region ModalConfig Tests

    [Fact]
    public void ModalConfig_Default_HasExpectedValues()
    {
        var config = ModalConfig.Default;

        Assert.Equal(400f, config.Width);
    }

    #endregion

    #region AccordionConfig Tests

    [Fact]
    public void AccordionConfig_Default_HasExpectedValues()
    {
        var config = AccordionConfig.Default;

        Assert.NotNull(config);
    }

    #endregion

    #region ToastConfig Tests

    [Fact]
    public void ToastConfig_WithMessage_StoresMessage()
    {
        var config = new ToastConfig(Message: "Operation completed");

        Assert.Equal("Operation completed", config.Message);
    }

    #endregion

    #region ToastContainerConfig Tests

    [Fact]
    public void ToastContainerConfig_Default_HasExpectedPosition()
    {
        var config = ToastContainerConfig.Default;

        Assert.Equal(ToastPosition.TopRight, config.Position);
    }

    #endregion

    #region SpinnerConfig Tests

    [Fact]
    public void SpinnerConfig_Default_HasExpectedValues()
    {
        var config = SpinnerConfig.Default;

        Assert.Equal(40f, config.Size);
    }

    #endregion

    #region ColorPickerConfig Tests

    [Fact]
    public void ColorPickerConfig_Default_HasExpectedValues()
    {
        var config = ColorPickerConfig.Default;

        Assert.Equal(250f, config.Width);
        Assert.True(config.ShowAlpha);
    }

    #endregion

    #region TooltipConfig Tests

    [Fact]
    public void TooltipConfig_Default_HasExpectedDelay()
    {
        var config = TooltipConfig.Default;

        Assert.Equal(0.5f, config.Delay);
    }

    [Fact]
    public void TooltipConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new TooltipConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TooltipConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new TooltipConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region CheckboxConfig Tests

    [Fact]
    public void CheckboxConfig_Default_HasExpectedValues()
    {
        var config = CheckboxConfig.Default;

        Assert.Equal(20f, config.Size);
        Assert.Equal(0, config.TabIndex);
    }

    #endregion

    #region DropdownConfig Tests

    [Fact]
    public void DropdownConfig_Default_HasExpectedValues()
    {
        var config = DropdownConfig.Default;

        Assert.Equal(200f, config.Width);
    }

    #endregion

    #region DatePickerConfig Tests

    [Fact]
    public void DatePickerConfig_Default_HasExpectedValues()
    {
        var config = DatePickerConfig.Default;

        Assert.Equal(280f, config.Width);
        Assert.Equal(320f, config.Height);
    }

    #endregion

    #region MenuItemDef Tests

    [Fact]
    public void MenuItemDef_WithLabel_StoresLabel()
    {
        var def = new MenuItemDef("File");
        Assert.Equal("File", def.Label);
    }

    [Fact]
    public void MenuItemDef_WithAllParameters_StoresAllValues()
    {
        var def = new MenuItemDef(
            Label: "Save",
            ItemId: "file-save",
            Shortcut: "Ctrl+S",
            IsEnabled: false,
            IsSeparator: false,
            IsToggle: true,
            IsChecked: true,
            SubmenuItems: null);

        Assert.Equal("Save", def.Label);
        Assert.Equal("file-save", def.ItemId);
        Assert.Equal("Ctrl+S", def.Shortcut);
        Assert.False(def.IsEnabled);
        Assert.False(def.IsSeparator);
        Assert.True(def.IsToggle);
        Assert.True(def.IsChecked);
        Assert.Null(def.SubmenuItems);
    }

    [Fact]
    public void MenuItemDef_Separator_IsSeparatorIsTrue()
    {
        var sep = MenuItemDef.Separator();
        Assert.True(sep.IsSeparator);
    }

    [Fact]
    public void MenuItemDef_WithSubmenu_StoresSubmenuItems()
    {
        var subItems = new[] { new MenuItemDef("Sub1"), new MenuItemDef("Sub2") };
        var def = new MenuItemDef("Parent", SubmenuItems: subItems);

        Assert.NotNull(def.SubmenuItems);
        Assert.Equal(2, def.SubmenuItems.Count());
    }

    #endregion

    #region RadialSliceDef Tests

    [Fact]
    public void RadialSliceDef_WithLabel_StoresLabel()
    {
        var def = new RadialSliceDef("Attack");
        Assert.Equal("Attack", def.Label);
    }

    [Fact]
    public void RadialSliceDef_WithAllParameters_StoresAllValues()
    {
        var subSlices = new[] { new RadialSliceDef("SubAction") };
        var def = new RadialSliceDef(
            Label: "Action",
            ItemId: "action-1",
            IsEnabled: false,
            SubSlices: subSlices);

        Assert.Equal("Action", def.Label);
        Assert.Equal("action-1", def.ItemId);
        Assert.False(def.IsEnabled);
        Assert.NotNull(def.SubSlices);
    }

    #endregion

    #region ToolbarButtonDef Tests

    [Fact]
    public void ToolbarButtonDef_Default_HasExpectedDefaults()
    {
        var def = new ToolbarButtonDef();

        Assert.Equal("", def.Tooltip);
        Assert.False(def.IsToggle);
        Assert.True(def.IsEnabled);
        Assert.Null(def.GroupId);
    }

    [Fact]
    public void ToolbarButtonDef_WithAllParameters_StoresAllValues()
    {
        var def = new ToolbarButtonDef(
            Tooltip: "Save file",
            IsToggle: true,
            IsEnabled: false,
            GroupId: "group1");

        Assert.Equal("Save file", def.Tooltip);
        Assert.True(def.IsToggle);
        Assert.False(def.IsEnabled);
        Assert.Equal("group1", def.GroupId);
    }

    #endregion

    #region StatusBarSectionDef Tests

    [Fact]
    public void StatusBarSectionDef_Default_HasExpectedDefaults()
    {
        var def = new StatusBarSectionDef();

        Assert.Equal("", def.InitialText);
        Assert.Equal(0f, def.Width);
        Assert.False(def.IsFlexible);
        Assert.Equal(50f, def.MinWidth);
        Assert.Equal(TextAlignH.Left, def.TextAlign);
    }

    [Fact]
    public void StatusBarSectionDef_WithAllParameters_StoresAllValues()
    {
        var def = new StatusBarSectionDef(
            InitialText: "Ready",
            Width: 100f,
            IsFlexible: true,
            MinWidth: 80f,
            TextAlign: TextAlignH.Center);

        Assert.Equal("Ready", def.InitialText);
        Assert.Equal(100f, def.Width);
        Assert.True(def.IsFlexible);
        Assert.Equal(80f, def.MinWidth);
        Assert.Equal(TextAlignH.Center, def.TextAlign);
    }

    #endregion

    #region ToolbarItemDef Tests

    [Fact]
    public void ToolbarItemDef_Button_WrapsDefinition()
    {
        var buttonDef = new ToolbarButtonDef(Tooltip: "Test");
        var item = new ToolbarItemDef.Button(buttonDef);

        Assert.Equal(buttonDef, item.Definition);
    }

    [Fact]
    public void ToolbarItemDef_Separator_CanBeCreated()
    {
        var sep = new ToolbarItemDef.Separator();
        Assert.NotNull(sep);
    }

    #endregion

    #region RadialMenuConfig Extended Tests

    [Fact]
    public void RadialMenuConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new RadialMenuConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void RadialMenuConfig_GetSelectedColor_ReturnsDefaultWhenNull()
    {
        var config = new RadialMenuConfig();
        var color = config.GetSelectedColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void RadialMenuConfig_GetDisabledColor_ReturnsDefaultWhenNull()
    {
        var config = new RadialMenuConfig();
        var color = config.GetDisabledColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void RadialMenuConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new RadialMenuConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region TreeNodeDef Tests

    [Fact]
    public void TreeNodeDef_WithLabel_StoresLabel()
    {
        var def = new TreeNodeDef("Root");
        Assert.Equal("Root", def.Label);
    }

    [Fact]
    public void TreeNodeDef_WithChildren_StoresChildren()
    {
        var children = new[] { new TreeNodeDef("Child1"), new TreeNodeDef("Child2") };
        var def = new TreeNodeDef("Parent", Children: children, IsExpanded: true);

        Assert.Equal("Parent", def.Label);
        Assert.True(def.IsExpanded);
        Assert.NotNull(def.Children);
        Assert.Equal(2, def.Children.Count());
    }

    [Fact]
    public void TreeNodeDef_WithUserData_StoresUserData()
    {
        var userData = new { Id = 42 };
        var def = new TreeNodeDef("Node", UserData: userData);

        Assert.Equal(userData, def.UserData);
    }

    #endregion

    #region PropertyDef Tests

    [Fact]
    public void PropertyDef_WithName_StoresName()
    {
        var def = new PropertyDef("Speed");
        Assert.Equal("Speed", def.Name);
    }

    [Fact]
    public void PropertyDef_WithAllParameters_StoresAllValues()
    {
        var enumValues = new[] { "Low", "Medium", "High" };
        var def = new PropertyDef(
            Name: "Priority",
            Label: "Priority Level",
            Type: PropertyType.Enum,
            Category: "Settings",
            InitialValue: "Medium",
            IsReadOnly: true,
            EnumValues: enumValues,
            MinValue: 0f,
            MaxValue: 100f);

        Assert.Equal("Priority", def.Name);
        Assert.Equal("Priority Level", def.Label);
        Assert.Equal(PropertyType.Enum, def.Type);
        Assert.Equal("Settings", def.Category);
        Assert.Equal("Medium", def.InitialValue);
        Assert.True(def.IsReadOnly);
        Assert.NotNull(def.EnumValues);
        Assert.Equal(0f, def.MinValue);
        Assert.Equal(100f, def.MaxValue);
    }

    #endregion

    #region PropertyCategoryDef Tests

    [Fact]
    public void PropertyCategoryDef_WithName_StoresName()
    {
        var def = new PropertyCategoryDef("General");
        Assert.Equal("General", def.Name);
        Assert.True(def.IsExpanded);
        Assert.Null(def.Properties);
    }

    [Fact]
    public void PropertyCategoryDef_WithProperties_StoresProperties()
    {
        var props = new[] { new PropertyDef("Prop1"), new PropertyDef("Prop2") };
        var def = new PropertyCategoryDef("Category", IsExpanded: false, Properties: props);

        Assert.Equal("Category", def.Name);
        Assert.False(def.IsExpanded);
        Assert.NotNull(def.Properties);
        Assert.Equal(2, def.Properties.Count());
    }

    #endregion

    #region AlertConfig Tests

    [Fact]
    public void AlertConfig_Default_HasExpectedDefaults()
    {
        var config = new AlertConfig();

        Assert.Equal(350f, config.Width);
        Assert.Equal("Alert", config.Title);
        Assert.Equal("OK", config.OkButtonText);
        Assert.True(config.CloseOnBackdropClick);
        Assert.True(config.CloseOnEscape);
    }

    [Fact]
    public void AlertConfig_WithCustomValues_StoresValues()
    {
        var config = new AlertConfig(
            Width: 500f,
            Title: "Warning",
            OkButtonText: "Understood",
            CloseOnBackdropClick: false,
            CloseOnEscape: false);

        Assert.Equal(500f, config.Width);
        Assert.Equal("Warning", config.Title);
        Assert.Equal("Understood", config.OkButtonText);
        Assert.False(config.CloseOnBackdropClick);
        Assert.False(config.CloseOnEscape);
    }

    #endregion

    #region ConfirmConfig Tests

    [Fact]
    public void ConfirmConfig_Default_HasExpectedDefaults()
    {
        var config = new ConfirmConfig();

        Assert.Equal(350f, config.Width);
        Assert.Equal("Confirm", config.Title);
        Assert.Equal("OK", config.OkButtonText);
        Assert.Equal("Cancel", config.CancelButtonText);
        Assert.False(config.CloseOnBackdropClick);
        Assert.True(config.CloseOnEscape);
    }

    [Fact]
    public void ConfirmConfig_WithCustomValues_StoresValues()
    {
        var config = new ConfirmConfig(
            Width: 400f,
            Title: "Delete?",
            OkButtonText: "Yes",
            CancelButtonText: "No",
            CloseOnBackdropClick: true,
            CloseOnEscape: false);

        Assert.Equal(400f, config.Width);
        Assert.Equal("Delete?", config.Title);
        Assert.Equal("Yes", config.OkButtonText);
        Assert.Equal("No", config.CancelButtonText);
        Assert.True(config.CloseOnBackdropClick);
        Assert.False(config.CloseOnEscape);
    }

    #endregion

    #region PromptConfig Tests

    [Fact]
    public void PromptConfig_Default_HasExpectedDefaults()
    {
        var config = new PromptConfig();

        Assert.Equal(400f, config.Width);
        Assert.Equal("Input", config.Title);
        Assert.Equal("", config.Placeholder);
        Assert.Equal("", config.InitialValue);
        Assert.Equal("OK", config.OkButtonText);
        Assert.Equal("Cancel", config.CancelButtonText);
    }

    [Fact]
    public void PromptConfig_WithCustomValues_StoresValues()
    {
        var config = new PromptConfig(
            Width: 500f,
            Title: "Enter Name",
            Placeholder: "Your name here...",
            InitialValue: "John",
            OkButtonText: "Submit",
            CancelButtonText: "Skip");

        Assert.Equal(500f, config.Width);
        Assert.Equal("Enter Name", config.Title);
        Assert.Equal("Your name here...", config.Placeholder);
        Assert.Equal("John", config.InitialValue);
        Assert.Equal("Submit", config.OkButtonText);
        Assert.Equal("Skip", config.CancelButtonText);
    }

    #endregion

    #region ModalButtonDef Tests

    [Fact]
    public void ModalButtonDef_WithText_StoresText()
    {
        var def = new ModalButtonDef("Save");
        Assert.Equal("Save", def.Text);
        Assert.Equal(ModalResult.OK, def.Result);
        Assert.False(def.IsPrimary);
        Assert.Null(def.Width);
    }

    [Fact]
    public void ModalButtonDef_WithAllParameters_StoresAllValues()
    {
        var def = new ModalButtonDef(
            Text: "Delete",
            Result: ModalResult.Cancel,
            IsPrimary: true,
            Width: 100f);

        Assert.Equal("Delete", def.Text);
        Assert.Equal(ModalResult.Cancel, def.Result);
        Assert.True(def.IsPrimary);
        Assert.Equal(100f, def.Width);
    }

    #endregion

    #region AccordionSectionDef Tests

    [Fact]
    public void AccordionSectionDef_WithTitle_StoresTitle()
    {
        var def = new AccordionSectionDef("Section 1");
        Assert.Equal("Section 1", def.Title);
        Assert.False(def.IsExpanded);
    }

    [Fact]
    public void AccordionSectionDef_WithExpanded_StoresExpanded()
    {
        var def = new AccordionSectionDef("Section 2", IsExpanded: true);
        Assert.Equal("Section 2", def.Title);
        Assert.True(def.IsExpanded);
    }

    #endregion

    #region AccordionConfig Extended Tests

    [Fact]
    public void AccordionConfig_GetBackgroundColor_ReturnsValue()
    {
        var config = new AccordionConfig();
        var color = config.GetBackgroundColor();
        // Should return Vector4.Zero as default
        Assert.Equal(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetHeaderColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetHeaderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetHeaderHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetHeaderHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetContentColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetContentColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetHeaderTextColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetHeaderTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetArrowColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetArrowColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void AccordionConfig_GetBorderColor_ReturnsDefaultWhenNull()
    {
        var config = new AccordionConfig();
        var color = config.GetBorderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region TreeNodeConfig Tests

    [Fact]
    public void TreeNodeConfig_Default_HasExpectedDefaults()
    {
        var config = new TreeNodeConfig();
        Assert.False(config.IsExpanded);
        Assert.Equal(16f, config.IconSize);
    }

    [Fact]
    public void TreeNodeConfig_WithCustomValues_StoresValues()
    {
        var config = new TreeNodeConfig(IsExpanded: true, IconSize: 24f);
        Assert.True(config.IsExpanded);
        Assert.Equal(24f, config.IconSize);
    }

    #endregion

    #region PropertyGridConfig Extended Tests

    [Fact]
    public void PropertyGridConfig_GetBackgroundColor_ReturnsValue()
    {
        var config = new PropertyGridConfig();
        var color = config.GetBackgroundColor();
        // Default is transparent
        Assert.Equal(Vector4.Zero, color);
    }

    [Fact]
    public void PropertyGridConfig_GetRowAlternateColor_ReturnsDefaultWhenNull()
    {
        var config = new PropertyGridConfig();
        var color = config.GetRowAlternateColor();
        // Just verify it returns something
        Assert.True(color.W >= 0);
    }

    [Fact]
    public void PropertyGridConfig_GetCategoryColor_ReturnsDefaultWhenNull()
    {
        var config = new PropertyGridConfig();
        var color = config.GetCategoryColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void PropertyGridConfig_GetLabelColor_ReturnsDefaultWhenNull()
    {
        var config = new PropertyGridConfig();
        var color = config.GetLabelColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void PropertyGridConfig_GetValueColor_ReturnsDefaultWhenNull()
    {
        var config = new PropertyGridConfig();
        var color = config.GetValueColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void PropertyGridConfig_GetSeparatorColor_ReturnsDefaultWhenNull()
    {
        var config = new PropertyGridConfig();
        var color = config.GetSeparatorColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region ToastConfig Extended Tests

    [Fact]
    public void ToastConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new ToastConfig(Message: "Test");
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void ToastConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new ToastConfig(Message: "Test");
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void ToastConfig_WithAllParameters_StoresAllValues()
    {
        var config = new ToastConfig(
            Message: "Success!",
            Title: "Saved",
            Type: ToastType.Success,
            Duration: 5f,
            CanDismiss: false,
            ShowCloseButton: true,
            Width: 300f);

        Assert.Equal("Success!", config.Message);
        Assert.Equal("Saved", config.Title);
        Assert.Equal(ToastType.Success, config.Type);
        Assert.Equal(5f, config.Duration);
        Assert.False(config.CanDismiss);
        Assert.True(config.ShowCloseButton);
        Assert.Equal(300f, config.Width);
    }

    #endregion

    #region ToastContainerConfig Extended Tests

    [Fact]
    public void ToastContainerConfig_Default_StoresPosition()
    {
        var config = ToastContainerConfig.Default;
        Assert.Equal(ToastPosition.TopRight, config.Position);
    }

    [Fact]
    public void ToastContainerConfig_WithPosition_StoresPosition()
    {
        var config = new ToastContainerConfig(Position: ToastPosition.BottomLeft);
        Assert.Equal(ToastPosition.BottomLeft, config.Position);
    }

    #endregion

    #region TreeViewConfig Extended Tests

    [Fact]
    public void TreeViewConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetBackgroundColor();
        // Default is transparent
        Assert.Equal(Vector4.Zero, color);
    }

    [Fact]
    public void TreeViewConfig_GetSelectedColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetSelectedColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TreeViewConfig_GetHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TreeViewConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TreeViewConfig_GetLineColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetLineColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TreeViewConfig_GetExpandArrowColor_ReturnsDefaultWhenNull()
    {
        var config = new TreeViewConfig();
        var color = config.GetExpandArrowColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region DockContainerConfig Extended Tests

    [Fact]
    public void DockContainerConfig_GetSplitterColor_ReturnsDefaultWhenNull()
    {
        var config = new DockContainerConfig();
        var color = config.GetSplitterColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region ToolbarConfig Extended Tests

    [Fact]
    public void ToolbarConfig_GetPadding_ReturnsDefaultWhenNull()
    {
        var config = new ToolbarConfig();
        var padding = config.GetPadding();
        Assert.NotEqual(default, padding);
    }

    [Fact]
    public void ToolbarConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new ToolbarConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void ToolbarConfig_GetButtonColor_ReturnsValue()
    {
        var config = new ToolbarConfig();
        var color = config.GetButtonColor();
        // May or may not be zero
        Assert.True(color.W >= 0);
    }

    [Fact]
    public void ToolbarConfig_GetButtonHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new ToolbarConfig();
        var color = config.GetButtonHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void ToolbarConfig_GetButtonPressedColor_ReturnsDefaultWhenNull()
    {
        var config = new ToolbarConfig();
        var color = config.GetButtonPressedColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void ToolbarConfig_GetSeparatorColor_ReturnsDefaultWhenNull()
    {
        var config = new ToolbarConfig();
        var color = config.GetSeparatorColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region StatusBarConfig Extended Tests

    [Fact]
    public void StatusBarConfig_GetPadding_ReturnsDefaultWhenNull()
    {
        var config = new StatusBarConfig();
        var padding = config.GetPadding();
        Assert.NotEqual(default, padding);
    }

    [Fact]
    public void StatusBarConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new StatusBarConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void StatusBarConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new StatusBarConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void StatusBarConfig_GetSeparatorColor_ReturnsDefaultWhenNull()
    {
        var config = new StatusBarConfig();
        var color = config.GetSeparatorColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region TextFieldConfig Tests

    [Fact]
    public void TextFieldConfig_Default_HasExpectedValues()
    {
        var config = TextFieldConfig.Default;

        Assert.Equal(200f, config.Width);
        Assert.Equal(32f, config.Height);
        Assert.Equal("", config.PlaceholderText);
        Assert.Equal(0, config.MaxLength);
        Assert.Equal(1f, config.BorderWidth);
        Assert.Equal(14f, config.FontSize);
        Assert.Equal(0, config.TabIndex);
    }

    [Fact]
    public void TextFieldConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new TextFieldConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TextFieldConfig_GetBackgroundColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.2f, 0.2f, 0.2f, 1f);
        var config = new TextFieldConfig(BackgroundColor: customColor);
        Assert.Equal(customColor, config.GetBackgroundColor());
    }

    [Fact]
    public void TextFieldConfig_GetBorderColor_ReturnsDefaultWhenNull()
    {
        var config = new TextFieldConfig();
        var color = config.GetBorderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TextFieldConfig_GetBorderColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var config = new TextFieldConfig(BorderColor: customColor);
        Assert.Equal(customColor, config.GetBorderColor());
    }

    [Fact]
    public void TextFieldConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new TextFieldConfig();
        var color = config.GetTextColor();
        Assert.Equal(Vector4.One, color);
    }

    [Fact]
    public void TextFieldConfig_GetTextColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.8f, 0.8f, 0.8f, 1f);
        var config = new TextFieldConfig(TextColor: customColor);
        Assert.Equal(customColor, config.GetTextColor());
    }

    [Fact]
    public void TextFieldConfig_GetPlaceholderColor_ReturnsDefaultWhenNull()
    {
        var config = new TextFieldConfig();
        var color = config.GetPlaceholderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void TextFieldConfig_GetPlaceholderColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.3f, 0.3f, 0.3f, 1f);
        var config = new TextFieldConfig(PlaceholderColor: customColor);
        Assert.Equal(customColor, config.GetPlaceholderColor());
    }

    #endregion

    #region CheckboxConfig Extended Tests

    [Fact]
    public void CheckboxConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new CheckboxConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void CheckboxConfig_GetBackgroundColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.1f, 0.1f, 0.1f, 1f);
        var config = new CheckboxConfig(BackgroundColor: customColor);
        Assert.Equal(customColor, config.GetBackgroundColor());
    }

    [Fact]
    public void CheckboxConfig_GetCheckColor_ReturnsDefaultWhenNull()
    {
        var config = new CheckboxConfig();
        var color = config.GetCheckColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void CheckboxConfig_GetCheckColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0f, 1f, 0f, 1f);
        var config = new CheckboxConfig(CheckColor: customColor);
        Assert.Equal(customColor, config.GetCheckColor());
    }

    [Fact]
    public void CheckboxConfig_GetBorderColor_ReturnsDefaultWhenNull()
    {
        var config = new CheckboxConfig();
        var color = config.GetBorderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void CheckboxConfig_GetBorderColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.6f, 0.6f, 0.6f, 1f);
        var config = new CheckboxConfig(BorderColor: customColor);
        Assert.Equal(customColor, config.GetBorderColor());
    }

    [Fact]
    public void CheckboxConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new CheckboxConfig();
        var color = config.GetTextColor();
        Assert.Equal(Vector4.One, color);
    }

    [Fact]
    public void CheckboxConfig_GetTextColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0.9f, 0.9f, 0.9f, 1f);
        var config = new CheckboxConfig(TextColor: customColor);
        Assert.Equal(customColor, config.GetTextColor());
    }

    [Fact]
    public void CheckboxConfig_WithAllParameters_StoresAllValues()
    {
        var config = new CheckboxConfig(
            IsChecked: true,
            Size: 24f,
            Spacing: 12f,
            BorderWidth: 2f,
            FontSize: 16f,
            TabIndex: 5);

        Assert.True(config.IsChecked);
        Assert.Equal(24f, config.Size);
        Assert.Equal(12f, config.Spacing);
        Assert.Equal(2f, config.BorderWidth);
        Assert.Equal(16f, config.FontSize);
        Assert.Equal(5, config.TabIndex);
    }

    #endregion

    #region DropdownConfig Extended Tests

    [Fact]
    public void DropdownConfig_GetBackgroundColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetBackgroundColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DropdownConfig_GetDropdownColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetDropdownColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DropdownConfig_GetSelectedColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetSelectedColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DropdownConfig_GetHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DropdownConfig_GetBorderColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetBorderColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DropdownConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new DropdownConfig();
        var color = config.GetTextColor();
        Assert.Equal(Vector4.One, color);
    }

    [Fact]
    public void DropdownConfig_WithAllParameters_StoresAllValues()
    {
        var config = new DropdownConfig(
            Width: 250f,
            Height: 36f,
            MaxDropdownHeight: 250f,
            SelectedIndex: 2,
            BorderWidth: 2f,
            FontSize: 16f,
            TabIndex: 3);

        Assert.Equal(250f, config.Width);
        Assert.Equal(36f, config.Height);
        Assert.Equal(250f, config.MaxDropdownHeight);
        Assert.Equal(2, config.SelectedIndex);
        Assert.Equal(2f, config.BorderWidth);
        Assert.Equal(16f, config.FontSize);
        Assert.Equal(3, config.TabIndex);
    }

    #endregion

    #region MenuBarConfig Extended Tests

    [Fact]
    public void MenuBarConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuBarConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuBarConfig_GetItemHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuBarConfig();
        var color = config.GetItemHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuBarConfig_WithAllParameters_StoresAllValues()
    {
        var customColor = new Vector4(0.2f, 0.2f, 0.25f, 1f);
        var config = new MenuBarConfig(
            Height: 32f,
            BackgroundColor: customColor,
            FontSize: 16f,
            ItemPadding: 16f);

        Assert.Equal(32f, config.Height);
        Assert.Equal(customColor, config.GetBackgroundColor());
        Assert.Equal(16f, config.FontSize);
        Assert.Equal(16f, config.ItemPadding);
    }

    #endregion

    #region MenuConfig Extended Tests

    [Fact]
    public void MenuConfig_Default_HasExpectedValues()
    {
        var config = new MenuConfig();

        Assert.Equal(180f, config.MinWidth);
        Assert.Equal(0f, config.MaxWidth);
        Assert.Equal(26f, config.ItemHeight);
    }

    [Fact]
    public void MenuConfig_GetItemHoverColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetItemHoverColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuConfig_GetTextColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuConfig_GetSeparatorColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetSeparatorColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuConfig_GetShortcutColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetShortcutColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void MenuConfig_GetDisabledTextColor_ReturnsDefaultWhenNull()
    {
        var config = new MenuConfig();
        var color = config.GetDisabledTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    #endregion

    #region DockPanelConfig Extended Tests

    [Fact]
    public void DockPanelConfig_Default_HasExpectedValues()
    {
        var config = new DockPanelConfig();

        Assert.True(config.CanClose);
        Assert.True(config.CanFloat);
        Assert.True(config.CanDock);
        Assert.Equal(300f, config.Width);
        Assert.Equal(200f, config.Height);
        Assert.Equal(28f, config.TitleBarHeight);
    }

    [Fact]
    public void DockPanelConfig_GetTitleBarColor_ReturnsDefaultWhenNull()
    {
        var config = new DockPanelConfig();
        var color = config.GetTitleBarColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DockPanelConfig_GetContentColor_ReturnsDefaultWhenNull()
    {
        var config = new DockPanelConfig();
        var color = config.GetContentColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DockPanelConfig_GetTitleTextColor_ReturnsDefaultWhenNull()
    {
        var config = new DockPanelConfig();
        var color = config.GetTitleTextColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DockPanelConfig_GetTabColor_ReturnsDefaultWhenNull()
    {
        var config = new DockPanelConfig();
        var color = config.GetTabColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DockPanelConfig_GetActiveTabColor_ReturnsDefaultWhenNull()
    {
        var config = new DockPanelConfig();
        var color = config.GetActiveTabColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void DockPanelConfig_WithAllParameters_StoresAllValues()
    {
        var config = new DockPanelConfig(
            Width: 400f,
            Height: 300f,
            CanClose: false,
            CanFloat: false,
            CanDock: false,
            AllowedZones: DockZone.Left | DockZone.Right,
            TitleBarHeight: 32f,
            FontSize: 14f);

        Assert.Equal(400f, config.Width);
        Assert.Equal(300f, config.Height);
        Assert.False(config.CanClose);
        Assert.False(config.CanFloat);
        Assert.False(config.CanDock);
        Assert.Equal(DockZone.Left | DockZone.Right, config.AllowedZones);
        Assert.Equal(32f, config.TitleBarHeight);
        Assert.Equal(14f, config.FontSize);
    }

    #endregion

    #region SpinnerConfig Extended Tests

    [Fact]
    public void SpinnerConfig_Small_CreatesSmallSpinner()
    {
        var config = SpinnerConfig.Small();

        Assert.Equal(24f, config.Size);
        Assert.Equal(2f, config.Thickness);
    }

    [Fact]
    public void SpinnerConfig_Small_WithCustomColor_StoresColor()
    {
        var customColor = new Vector4(1f, 0f, 0f, 1f);
        var config = SpinnerConfig.Small(color: customColor);

        Assert.Equal(customColor, config.Color);
    }

    [Fact]
    public void SpinnerConfig_Medium_CreatesMediumSpinner()
    {
        var config = SpinnerConfig.Medium();

        Assert.Equal(40f, config.Size);
    }

    [Fact]
    public void SpinnerConfig_Large_CreatesLargeSpinner()
    {
        var config = SpinnerConfig.Large();

        Assert.Equal(64f, config.Size);
        Assert.Equal(4f, config.Thickness);
    }

    [Fact]
    public void SpinnerConfig_Dots_CreatesDotSpinner()
    {
        var config = SpinnerConfig.Dots();

        Assert.Equal(SpinnerStyle.Dots, config.Style);
        Assert.Equal(40f, config.Size);
        Assert.Equal(8, config.ElementCount);
    }

    [Fact]
    public void SpinnerConfig_Dots_WithCustomParameters_StoresValues()
    {
        var config = SpinnerConfig.Dots(size: 60f, dotCount: 12);

        Assert.Equal(60f, config.Size);
        Assert.Equal(12, config.ElementCount);
    }

    [Fact]
    public void SpinnerConfig_Bar_CreatesBarSpinner()
    {
        var config = SpinnerConfig.Bar();

        Assert.Equal(SpinnerStyle.Bar, config.Style);
        Assert.Equal(200f, config.Size);
    }

    [Fact]
    public void SpinnerConfig_Bar_WithCustomWidth_StoresWidth()
    {
        var config = SpinnerConfig.Bar(width: 300f);

        Assert.Equal(300f, config.Size);
    }

    [Fact]
    public void SpinnerConfig_GetColor_ReturnsDefaultWhenNull()
    {
        var config = new SpinnerConfig();
        var color = config.GetColor();
        Assert.NotEqual(Vector4.Zero, color);
    }

    [Fact]
    public void SpinnerConfig_GetColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0f, 1f, 0f, 1f);
        var config = new SpinnerConfig(Color: customColor);
        Assert.Equal(customColor, config.GetColor());
    }

    [Fact]
    public void SpinnerConfig_WithAllParameters_StoresAllValues()
    {
        var config = new SpinnerConfig(
            Style: SpinnerStyle.Circular,
            Size: 50f,
            Speed: 5f,
            Thickness: 4f,
            ArcLength: 0.5f,
            ElementCount: 10);

        Assert.Equal(SpinnerStyle.Circular, config.Style);
        Assert.Equal(50f, config.Size);
        Assert.Equal(5f, config.Speed);
        Assert.Equal(4f, config.Thickness);
        Assert.Equal(0.5f, config.ArcLength);
        Assert.Equal(10, config.ElementCount);
    }

    #endregion

    #region ColorPickerConfig Extended Tests

    [Fact]
    public void ColorPickerConfig_HSV_CreatesHsvPicker()
    {
        var config = ColorPickerConfig.HSV();

        Assert.Equal(ColorPickerMode.HSV, config.Mode);
        Assert.True(config.ShowAlpha);
    }

    [Fact]
    public void ColorPickerConfig_HSV_WithInitialColor_StoresColor()
    {
        var initialColor = new Vector4(1f, 0.5f, 0f, 1f);
        var config = ColorPickerConfig.HSV(initialColor: initialColor);

        Assert.Equal(initialColor, config.InitialColor);
    }

    [Fact]
    public void ColorPickerConfig_RGB_CreatesRgbPicker()
    {
        var config = ColorPickerConfig.RGB();

        Assert.Equal(ColorPickerMode.RGB, config.Mode);
    }

    [Fact]
    public void ColorPickerConfig_Compact_CreatesCompactPicker()
    {
        var config = ColorPickerConfig.Compact();

        Assert.False(config.ShowHexInput);
        Assert.Equal(200f, config.Width);
        Assert.Equal(220f, config.Height);
    }

    [Fact]
    public void ColorPickerConfig_Opaque_HidesAlpha()
    {
        var config = ColorPickerConfig.Opaque();

        Assert.False(config.ShowAlpha);
    }

    [Fact]
    public void ColorPickerConfig_GetInitialColor_ReturnsDefaultWhenNull()
    {
        var config = new ColorPickerConfig();
        var color = config.GetInitialColor();
        // Default is red
        Assert.Equal(1f, color.X);
        Assert.Equal(0f, color.Y);
        Assert.Equal(0f, color.Z);
        Assert.Equal(1f, color.W);
    }

    [Fact]
    public void ColorPickerConfig_GetInitialColor_ReturnsCustomWhenSet()
    {
        var customColor = new Vector4(0f, 1f, 0f, 0.5f);
        var config = new ColorPickerConfig(InitialColor: customColor);
        Assert.Equal(customColor, config.GetInitialColor());
    }

    [Fact]
    public void ColorPickerConfig_WithAllParameters_StoresAllValues()
    {
        var config = new ColorPickerConfig(
            Mode: ColorPickerMode.Both,
            ShowAlpha: false,
            ShowHexInput: false,
            ShowPreview: false,
            Width: 300f,
            Height: 400f,
            CornerRadius: 8f);

        Assert.Equal(ColorPickerMode.Both, config.Mode);
        Assert.False(config.ShowAlpha);
        Assert.False(config.ShowHexInput);
        Assert.False(config.ShowPreview);
        Assert.Equal(300f, config.Width);
        Assert.Equal(400f, config.Height);
        Assert.Equal(8f, config.CornerRadius);
    }

    #endregion

    #region ToastContainerConfig Extended Tests

    [Fact]
    public void ToastContainerConfig_WithMaxVisible_StoresMaxVisible()
    {
        var config = new ToastContainerConfig(MaxVisible: 5);
        Assert.Equal(5, config.MaxVisible);
    }

    [Fact]
    public void ToastContainerConfig_WithSpacing_StoresSpacing()
    {
        var config = new ToastContainerConfig(Spacing: 16f);
        Assert.Equal(16f, config.Spacing);
    }

    [Fact]
    public void ToastContainerConfig_WithMargin_StoresMargin()
    {
        var config = new ToastContainerConfig(Margin: 20f);
        Assert.Equal(20f, config.Margin);
    }

    [Fact]
    public void ToastContainerConfig_AllPositions_StoreCorrectly()
    {
        var positions = new[]
        {
            ToastPosition.TopLeft, ToastPosition.TopCenter, ToastPosition.TopRight,
            ToastPosition.BottomLeft, ToastPosition.BottomCenter, ToastPosition.BottomRight
        };

        foreach (var position in positions)
        {
            var config = new ToastContainerConfig(Position: position);
            Assert.Equal(position, config.Position);
        }
    }

    [Fact]
    public void ToastContainerConfig_TopRight_Factory()
    {
        var config = ToastContainerConfig.TopRight(maxVisible: 10);
        Assert.Equal(ToastPosition.TopRight, config.Position);
        Assert.Equal(10, config.MaxVisible);
    }

    [Fact]
    public void ToastContainerConfig_BottomRight_Factory()
    {
        var config = ToastContainerConfig.BottomRight();
        Assert.Equal(ToastPosition.BottomRight, config.Position);
    }

    [Fact]
    public void ToastContainerConfig_TopCenter_Factory()
    {
        var config = ToastContainerConfig.TopCenter();
        Assert.Equal(ToastPosition.TopCenter, config.Position);
    }

    [Fact]
    public void ToastContainerConfig_BottomCenter_Factory()
    {
        var config = ToastContainerConfig.BottomCenter();
        Assert.Equal(ToastPosition.BottomCenter, config.Position);
    }

    #endregion

    #region TooltipConfig Extended Tests

    [Fact]
    public void TooltipConfig_AllPositions_StoreCorrectly()
    {
        var positions = new[]
        {
            TooltipPosition.Auto, TooltipPosition.Above, TooltipPosition.Below,
            TooltipPosition.Left, TooltipPosition.Right
        };

        foreach (var position in positions)
        {
            var config = new TooltipConfig(Position: position);
            Assert.Equal(position, config.Position);
        }
    }

    [Fact]
    public void TooltipConfig_WithAllParameters_StoresAllValues()
    {
        var bgColor = new Vector4(0.1f, 0.1f, 0.1f, 0.9f);
        var textColor = new Vector4(1f, 1f, 1f, 1f);
        var config = new TooltipConfig(
            Delay: 1f,
            MaxWidth: 400f,
            Position: TooltipPosition.Below,
            FollowMouse: true,
            BackgroundColor: bgColor,
            TextColor: textColor,
            FontSize: 14f,
            CornerRadius: 6f);

        Assert.Equal(1f, config.Delay);
        Assert.Equal(400f, config.MaxWidth);
        Assert.Equal(TooltipPosition.Below, config.Position);
        Assert.True(config.FollowMouse);
        Assert.Equal(bgColor, config.GetBackgroundColor());
        Assert.Equal(textColor, config.GetTextColor());
        Assert.Equal(14f, config.FontSize);
        Assert.Equal(6f, config.CornerRadius);
    }

    #endregion
}
