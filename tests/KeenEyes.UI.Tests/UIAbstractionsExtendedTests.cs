using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Extended tests for UI.Abstractions components including checkboxes, tooltips, toasts, and enums.
/// </summary>
public class UIAbstractionsExtendedTests
{
    #region UICheckbox Tests

    [Fact]
    public void UICheckbox_DefaultConstructor_IsUnchecked()
    {
        var checkbox = new UICheckbox();

        Assert.False(checkbox.IsChecked);
    }

    [Fact]
    public void UICheckbox_ConstructorWithTrue_IsChecked()
    {
        var checkbox = new UICheckbox(true);

        Assert.True(checkbox.IsChecked);
    }

    [Fact]
    public void UICheckbox_ConstructorWithFalse_IsUnchecked()
    {
        var checkbox = new UICheckbox(false);

        Assert.False(checkbox.IsChecked);
    }

    #endregion

    #region UIToggle Tests

    [Fact]
    public void UIToggle_DefaultConstructor_IsOff()
    {
        var toggle = new UIToggle();

        Assert.False(toggle.IsOn);
    }

    [Fact]
    public void UIToggle_ConstructorWithTrue_IsOn()
    {
        var toggle = new UIToggle(true);

        Assert.True(toggle.IsOn);
    }

    [Fact]
    public void UIToggle_ConstructorWithFalse_IsOff()
    {
        var toggle = new UIToggle(false);

        Assert.False(toggle.IsOn);
    }

    #endregion

    #region UISlider Tests

    [Fact]
    public void UISlider_DefaultConstructor_HasDefaultValues()
    {
        var slider = new UISlider();

        Assert.Equal(0f, slider.MinValue);
        Assert.Equal(0f, slider.MaxValue);
        Assert.Equal(0f, slider.Value);
    }

    [Fact]
    public void UISlider_ConstructorWithParameters_SetsValues()
    {
        var slider = new UISlider(minValue: 10f, maxValue: 100f, value: 50f);

        Assert.Equal(10f, slider.MinValue);
        Assert.Equal(100f, slider.MaxValue);
        Assert.Equal(50f, slider.Value);
    }

    [Fact]
    public void UISlider_ConstructorWithPartialParams_SetsSpecifiedValues()
    {
        var slider = new UISlider(minValue: 0f, maxValue: 10f, value: 5f);

        Assert.Equal(0f, slider.MinValue);
        Assert.Equal(10f, slider.MaxValue);
        Assert.Equal(5f, slider.Value);
    }

    #endregion

    #region UITooltip Tests

    [Fact]
    public void UITooltip_Constructor_SetsText()
    {
        var tooltip = new UITooltip("Hover text");

        Assert.Equal("Hover text", tooltip.Text);
        Assert.Equal(0.5f, tooltip.Delay);
        Assert.Equal(300f, tooltip.MaxWidth);
        Assert.Equal(TooltipPosition.Auto, tooltip.Position);
        Assert.False(tooltip.FollowMouse);
    }

    [Fact]
    public void UITooltip_DefaultValues_AreCorrect()
    {
        var tooltip = new UITooltip("Test");

        Assert.Equal(0.5f, tooltip.Delay);
        Assert.Equal(300f, tooltip.MaxWidth);
        Assert.Equal(TooltipPosition.Auto, tooltip.Position);
        Assert.False(tooltip.FollowMouse);
    }

    #endregion

    #region UIPopover Tests

    [Fact]
    public void UIPopover_DefaultConstructor_HasCorrectDefaults()
    {
        var popover = new UIPopover();

        Assert.False(popover.IsOpen);
        Assert.Equal(PopoverTrigger.Click, popover.Trigger);
        Assert.Equal(TooltipPosition.Auto, popover.Position);
        Assert.False(popover.CloseOnClickOutside);
        Assert.Equal(Vector2.Zero, popover.Offset);
    }

    #endregion

    #region UIToast Tests

    [Fact]
    public void UIToast_Constructor_SetsMessageAndDuration()
    {
        var toast = new UIToast("Notification", 5f);

        Assert.Equal("Notification", toast.Message);
        Assert.Equal(5f, toast.Duration);
        Assert.Equal(ToastType.Info, toast.Type);
        Assert.True(toast.CanDismiss);
        Assert.Null(toast.Title);
        Assert.Equal(5f, toast.TimeRemaining);
        Assert.False(toast.IsClosing);
    }

    [Fact]
    public void UIToast_ConstructorWithDefaultDuration_Uses3Seconds()
    {
        var toast = new UIToast("Test");

        Assert.Equal("Test", toast.Message);
        Assert.Equal(3f, toast.Duration);
        Assert.Equal(3f, toast.TimeRemaining);
    }

    [Fact]
    public void UIToast_ConstructorWithZeroDuration_IsIndefinite()
    {
        var toast = new UIToast("Persistent", 0f);

        Assert.Equal("Persistent", toast.Message);
        Assert.Equal(0f, toast.Duration);
        Assert.Equal(0f, toast.TimeRemaining);
    }

    #endregion

    #region UIToastContainer Tests

    [Fact]
    public void UIToastContainer_DefaultConstructor_HasDefaultValues()
    {
        var container = new UIToastContainer();

        Assert.Equal(ToastPosition.TopRight, container.Position);
        Assert.Equal(5, container.MaxVisible);
        Assert.Equal(10f, container.Spacing);
        Assert.Equal(20f, container.Margin);
    }

    #endregion

    #region UIToastCloseButton Tests

    [Fact]
    public void UIToastCloseButton_Constructor_SetsToast()
    {
        var toast = new Entity(300, 1);
        var closeButton = new UIToastCloseButton(toast);

        Assert.Equal(toast, closeButton.Toast);
    }

    #endregion

    #region UITooltipHoverState Tests

    [Fact]
    public void UITooltipHoverState_DefaultConstructor_HasDefaultValues()
    {
        var state = new UITooltipHoverState();

        Assert.Equal(0f, state.HoverTime);
        Assert.Equal(Vector2.Zero, state.HoverPosition);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void TooltipPosition_Values_AreCorrect()
    {
        Assert.Equal(0, (int)TooltipPosition.Auto);
        Assert.Equal(1, (int)TooltipPosition.Above);
        Assert.Equal(2, (int)TooltipPosition.Below);
        Assert.Equal(3, (int)TooltipPosition.Left);
        Assert.Equal(4, (int)TooltipPosition.Right);
    }

    [Fact]
    public void PopoverTrigger_Values_AreCorrect()
    {
        Assert.Equal(0, (int)PopoverTrigger.Click);
        Assert.Equal(1, (int)PopoverTrigger.Hover);
        Assert.Equal(2, (int)PopoverTrigger.Manual);
    }

    [Fact]
    public void ToastType_Values_AreCorrect()
    {
        Assert.Equal(0, (int)ToastType.Info);
        Assert.Equal(1, (int)ToastType.Success);
        Assert.Equal(2, (int)ToastType.Warning);
        Assert.Equal(3, (int)ToastType.Error);
    }

    [Fact]
    public void ToastPosition_Values_AreCorrect()
    {
        Assert.Equal(0, (int)ToastPosition.TopLeft);
        Assert.Equal(1, (int)ToastPosition.TopCenter);
        Assert.Equal(2, (int)ToastPosition.TopRight);
        Assert.Equal(3, (int)ToastPosition.BottomLeft);
        Assert.Equal(4, (int)ToastPosition.BottomCenter);
        Assert.Equal(5, (int)ToastPosition.BottomRight);
    }

    #endregion

    #region Additional Event Tests

    [Fact]
    public void UIModalOpenedEvent_Constructor_SetsModal()
    {
        var modal = new Entity(400, 1);
        var evt = new UIModalOpenedEvent(modal);

        Assert.Equal(modal, evt.Modal);
    }

    [Fact]
    public void UIModalResultEvent_Constructor_SetsProperties()
    {
        var modal = new Entity(401, 1);
        var button = new Entity(402, 1);
        var evt = new UIModalResultEvent(modal, button, ModalResult.Yes);

        Assert.Equal(modal, evt.Modal);
        Assert.Equal(button, evt.Button);
        Assert.Equal(ModalResult.Yes, evt.Result);
    }

    [Fact]
    public void UIToastShownEvent_Constructor_SetsToast()
    {
        var toast = new Entity(403, 1);
        var evt = new UIToastShownEvent(toast);

        Assert.Equal(toast, evt.Toast);
    }

    [Fact]
    public void UIAccordionSectionExpandedEvent_Constructor_SetsProperties()
    {
        var accordion = new Entity(404, 1);
        var section = new Entity(405, 1);
        var evt = new UIAccordionSectionExpandedEvent(accordion, section);

        Assert.Equal(accordion, evt.Accordion);
        Assert.Equal(section, evt.Section);
    }

    [Fact]
    public void UIAccordionSectionCollapsedEvent_Constructor_SetsProperties()
    {
        var accordion = new Entity(406, 1);
        var section = new Entity(407, 1);
        var evt = new UIAccordionSectionCollapsedEvent(accordion, section);

        Assert.Equal(accordion, evt.Accordion);
        Assert.Equal(section, evt.Section);
    }

    [Fact]
    public void UIWindowMinimizedEvent_Constructor_SetsWindow()
    {
        var window = new Entity(408, 1);
        var evt = new UIWindowMinimizedEvent(window);

        Assert.Equal(window, evt.Window);
    }

    [Fact]
    public void UIWindowMaximizedEvent_Constructor_SetsWindow()
    {
        var window = new Entity(409, 1);
        var evt = new UIWindowMaximizedEvent(window);

        Assert.Equal(window, evt.Window);
    }

    [Fact]
    public void UIWindowRestoredEvent_Constructor_SetsProperties()
    {
        var window = new Entity(410, 1);
        var evt = new UIWindowRestoredEvent(window, WindowState.Maximized);

        Assert.Equal(window, evt.Window);
        Assert.Equal(WindowState.Maximized, evt.PreviousState);
    }

    [Fact]
    public void UIPopoverOpenedEvent_Constructor_SetsProperties()
    {
        var popover = new Entity(411, 1);
        var trigger = new Entity(412, 1);
        var evt = new UIPopoverOpenedEvent(popover, trigger);

        Assert.Equal(popover, evt.Popover);
        Assert.Equal(trigger, evt.Trigger);
    }

    [Fact]
    public void UIPopoverClosedEvent_Constructor_SetsPopover()
    {
        var popover = new Entity(413, 1);
        var evt = new UIPopoverClosedEvent(popover);

        Assert.Equal(popover, evt.Popover);
    }

    [Fact]
    public void UIMenuOpenedEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(414, 1);
        var parentMenu = new Entity(415, 1);
        var evt = new UIMenuOpenedEvent(menu, parentMenu);

        Assert.Equal(menu, evt.Menu);
        Assert.Equal(parentMenu, evt.ParentMenu);
    }

    [Fact]
    public void UIMenuOpenedEvent_WithNullParent_AllowsNull()
    {
        var menu = new Entity(414, 1);
        var evt = new UIMenuOpenedEvent(menu, null);

        Assert.Equal(menu, evt.Menu);
        Assert.Null(evt.ParentMenu);
    }

    [Fact]
    public void UIMenuClosedEvent_Constructor_SetsMenu()
    {
        var menu = new Entity(416, 1);
        var evt = new UIMenuClosedEvent(menu);

        Assert.Equal(menu, evt.Menu);
    }

    [Fact]
    public void UIMenuToggleChangedEvent_Constructor_SetsProperties()
    {
        var menuItem = new Entity(417, 1);
        var evt = new UIMenuToggleChangedEvent(menuItem, true);

        Assert.Equal(menuItem, evt.MenuItem);
        Assert.True(evt.IsChecked);
    }

    [Fact]
    public void UIContextMenuRequestEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(418, 1);
        var target = new Entity(419, 1);
        var position = new Vector2(250, 350);
        var evt = new UIContextMenuRequestEvent(menu, position, target);

        Assert.Equal(menu, evt.Menu);
        Assert.Equal(position, evt.Position);
        Assert.Equal(target, evt.Target);
    }

    [Fact]
    public void UIRadialMenuOpenedEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(420, 1);
        var position = new Vector2(400, 300);
        var evt = new UIRadialMenuOpenedEvent(menu, position);

        Assert.Equal(menu, evt.Menu);
        Assert.Equal(position, evt.Position);
    }

    [Fact]
    public void UIRadialMenuClosedEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(421, 1);
        var evt = new UIRadialMenuClosedEvent(menu, true);

        Assert.Equal(menu, evt.Menu);
        Assert.True(evt.WasCancelled);
    }

    [Fact]
    public void UIRadialSliceChangedEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(422, 1);
        var evt = new UIRadialSliceChangedEvent(menu, 2, 5);

        Assert.Equal(menu, evt.Menu);
        Assert.Equal(2, evt.PreviousIndex);
        Assert.Equal(5, evt.NewIndex);
    }

    [Fact]
    public void UIRadialMenuRequestEvent_Constructor_SetsProperties()
    {
        var menu = new Entity(423, 1);
        var position = new Vector2(100, 200);
        var evt = new UIRadialMenuRequestEvent(menu, position);

        Assert.Equal(menu, evt.Menu);
        Assert.Equal(position, evt.Position);
    }

    [Fact]
    public void UITreeNodeExpandedEvent_Constructor_SetsProperties()
    {
        var node = new Entity(424, 1);
        var treeView = new Entity(425, 1);
        var evt = new UITreeNodeExpandedEvent(node, treeView);

        Assert.Equal(node, evt.Node);
        Assert.Equal(treeView, evt.TreeView);
    }

    [Fact]
    public void UITreeNodeCollapsedEvent_Constructor_SetsProperties()
    {
        var node = new Entity(426, 1);
        var treeView = new Entity(427, 1);
        var evt = new UITreeNodeCollapsedEvent(node, treeView);

        Assert.Equal(node, evt.Node);
        Assert.Equal(treeView, evt.TreeView);
    }

    [Fact]
    public void UITreeNodeDoubleClickedEvent_Constructor_SetsProperties()
    {
        var node = new Entity(428, 1);
        var treeView = new Entity(429, 1);
        var evt = new UITreeNodeDoubleClickedEvent(node, treeView);

        Assert.Equal(node, evt.Node);
        Assert.Equal(treeView, evt.TreeView);
    }

    [Fact]
    public void UIPropertyCategoryExpandedEvent_Constructor_SetsProperties()
    {
        var grid = new Entity(430, 1);
        var category = new Entity(431, 1);
        var evt = new UIPropertyCategoryExpandedEvent(grid, category);

        Assert.Equal(grid, evt.PropertyGrid);
        Assert.Equal(category, evt.Category);
    }

    [Fact]
    public void UIPropertyCategoryCollapsedEvent_Constructor_SetsProperties()
    {
        var grid = new Entity(432, 1);
        var category = new Entity(433, 1);
        var evt = new UIPropertyCategoryCollapsedEvent(grid, category);

        Assert.Equal(grid, evt.PropertyGrid);
        Assert.Equal(category, evt.Category);
    }

    #endregion
}
