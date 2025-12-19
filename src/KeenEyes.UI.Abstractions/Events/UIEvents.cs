using System.Numerics;
using KeenEyes;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Event raised when a UI element is clicked.
/// </summary>
/// <param name="Element">The entity that was clicked.</param>
/// <param name="Position">The position where the click occurred (in screen coordinates).</param>
/// <param name="Button">The mouse button that was clicked.</param>
public readonly record struct UIClickEvent(Entity Element, Vector2 Position, MouseButton Button);

/// <summary>
/// Event raised when the pointer enters a UI element's bounds.
/// </summary>
/// <param name="Element">The entity the pointer entered.</param>
/// <param name="Position">The position where the pointer entered (in screen coordinates).</param>
public readonly record struct UIPointerEnterEvent(Entity Element, Vector2 Position);

/// <summary>
/// Event raised when the pointer exits a UI element's bounds.
/// </summary>
/// <param name="Element">The entity the pointer exited.</param>
public readonly record struct UIPointerExitEvent(Entity Element);

/// <summary>
/// Event raised when a UI element gains keyboard focus.
/// </summary>
/// <param name="Element">The entity that gained focus.</param>
/// <param name="Previous">The entity that previously had focus, or null if none.</param>
public readonly record struct UIFocusGainedEvent(Entity Element, Entity? Previous);

/// <summary>
/// Event raised when a UI element loses keyboard focus.
/// </summary>
/// <param name="Element">The entity that lost focus.</param>
/// <param name="Next">The entity that will receive focus next, or null if none.</param>
public readonly record struct UIFocusLostEvent(Entity Element, Entity? Next);

/// <summary>
/// Event raised when a drag operation starts on a UI element.
/// </summary>
/// <param name="Element">The entity being dragged.</param>
/// <param name="StartPosition">The position where the drag started (in screen coordinates).</param>
public readonly record struct UIDragStartEvent(Entity Element, Vector2 StartPosition);

/// <summary>
/// Event raised during a drag operation as the pointer moves.
/// </summary>
/// <param name="Element">The entity being dragged.</param>
/// <param name="Position">The current pointer position (in screen coordinates).</param>
/// <param name="Delta">The movement delta since the last drag event.</param>
public readonly record struct UIDragEvent(Entity Element, Vector2 Position, Vector2 Delta);

/// <summary>
/// Event raised when a drag operation ends.
/// </summary>
/// <param name="Element">The entity that was being dragged.</param>
/// <param name="EndPosition">The position where the drag ended (in screen coordinates).</param>
public readonly record struct UIDragEndEvent(Entity Element, Vector2 EndPosition);

/// <summary>
/// Event raised when a UI element's value changes (for input elements like sliders or text fields).
/// </summary>
/// <param name="Element">The entity whose value changed.</param>
/// <param name="OldValue">The previous value (type depends on the element).</param>
/// <param name="NewValue">The new value (type depends on the element).</param>
public readonly record struct UIValueChangedEvent(Entity Element, object? OldValue, object? NewValue);

/// <summary>
/// Event raised when the submit action is triggered on a focused element (e.g., Enter key).
/// </summary>
/// <param name="Element">The entity that received the submit action.</param>
public readonly record struct UISubmitEvent(Entity Element);

/// <summary>
/// Event raised when a floating window is closed via its close button.
/// </summary>
/// <param name="Window">The window entity that was closed.</param>
public readonly record struct UIWindowClosedEvent(Entity Window);

/// <summary>
/// Event raised when a splitter's ratio changes.
/// </summary>
/// <param name="Splitter">The splitter entity whose ratio changed.</param>
/// <param name="OldRatio">The previous split ratio.</param>
/// <param name="NewRatio">The new split ratio.</param>
public readonly record struct UISplitterChangedEvent(Entity Splitter, float OldRatio, float NewRatio);

/// <summary>
/// Event raised when a tooltip should be shown.
/// </summary>
/// <param name="Element">The element that triggered the tooltip.</param>
/// <param name="Text">The tooltip text to display.</param>
/// <param name="Position">The screen position for the tooltip.</param>
public readonly record struct UITooltipShowEvent(Entity Element, string Text, Vector2 Position);

/// <summary>
/// Event raised when a tooltip should be hidden.
/// </summary>
/// <param name="Element">The element whose tooltip should be hidden.</param>
public readonly record struct UITooltipHideEvent(Entity Element);

/// <summary>
/// Event raised when a popover is opened.
/// </summary>
/// <param name="Popover">The popover entity that was opened.</param>
/// <param name="Trigger">The element that triggered the popover.</param>
public readonly record struct UIPopoverOpenedEvent(Entity Popover, Entity Trigger);

/// <summary>
/// Event raised when a popover is closed.
/// </summary>
/// <param name="Popover">The popover entity that was closed.</param>
public readonly record struct UIPopoverClosedEvent(Entity Popover);

/// <summary>
/// Event raised when a menu item is clicked.
/// </summary>
/// <param name="MenuItem">The menu item entity that was clicked.</param>
/// <param name="Menu">The menu containing the item.</param>
/// <param name="ItemId">The unique identifier of the menu item.</param>
/// <param name="Index">The index of the item within the menu.</param>
public readonly record struct UIMenuItemClickEvent(Entity MenuItem, Entity Menu, string ItemId, int Index);

/// <summary>
/// Event raised when a menu is opened.
/// </summary>
/// <param name="Menu">The menu entity that was opened.</param>
/// <param name="ParentMenu">The parent menu if this is a submenu, or null.</param>
public readonly record struct UIMenuOpenedEvent(Entity Menu, Entity? ParentMenu);

/// <summary>
/// Event raised when a menu is closed.
/// </summary>
/// <param name="Menu">The menu entity that was closed.</param>
public readonly record struct UIMenuClosedEvent(Entity Menu);

/// <summary>
/// Event raised when a toggle menu item changes state.
/// </summary>
/// <param name="MenuItem">The menu item that was toggled.</param>
/// <param name="IsChecked">The new checked state.</param>
public readonly record struct UIMenuToggleChangedEvent(Entity MenuItem, bool IsChecked);

/// <summary>
/// Event to request showing a context menu at a position.
/// </summary>
/// <param name="Menu">The context menu entity to show.</param>
/// <param name="Position">The screen position to show the menu at.</param>
/// <param name="Target">Optional target element that the context menu is for.</param>
public readonly record struct UIContextMenuRequestEvent(Entity Menu, Vector2 Position, Entity Target);

/// <summary>
/// Event raised when a radial menu is opened.
/// </summary>
/// <param name="Menu">The radial menu entity.</param>
/// <param name="Position">The center position of the menu.</param>
public readonly record struct UIRadialMenuOpenedEvent(Entity Menu, Vector2 Position);

/// <summary>
/// Event raised when a radial menu is closed.
/// </summary>
/// <param name="Menu">The radial menu entity.</param>
/// <param name="WasCancelled">Whether the menu was cancelled without selection.</param>
public readonly record struct UIRadialMenuClosedEvent(Entity Menu, bool WasCancelled);

/// <summary>
/// Event raised when the selected slice changes in a radial menu.
/// </summary>
/// <param name="Menu">The radial menu entity.</param>
/// <param name="PreviousIndex">Previously selected index (-1 if none).</param>
/// <param name="NewIndex">Newly selected index (-1 if none).</param>
public readonly record struct UIRadialSliceChangedEvent(Entity Menu, int PreviousIndex, int NewIndex);

/// <summary>
/// Event raised when a radial menu slice is selected (confirmed).
/// </summary>
/// <param name="Slice">The slice entity that was selected.</param>
/// <param name="Menu">The parent radial menu.</param>
/// <param name="ItemId">The unique identifier of the slice.</param>
/// <param name="Index">The index of the selected slice.</param>
public readonly record struct UIRadialSliceSelectedEvent(Entity Slice, Entity Menu, string ItemId, int Index);

/// <summary>
/// Event to request opening a radial menu at a position.
/// </summary>
/// <param name="Menu">The radial menu entity to show.</param>
/// <param name="Position">The center position for the menu.</param>
public readonly record struct UIRadialMenuRequestEvent(Entity Menu, Vector2 Position);

/// <summary>
/// Event raised when a panel is docked to a zone.
/// </summary>
/// <param name="Panel">The panel that was docked.</param>
/// <param name="Zone">The zone it was docked to.</param>
/// <param name="Container">The dock container.</param>
public readonly record struct UIDockPanelDockedEvent(Entity Panel, DockZone Zone, Entity Container);

/// <summary>
/// Event raised when a panel is undocked (floated).
/// </summary>
/// <param name="Panel">The panel that was undocked.</param>
/// <param name="PreviousZone">The zone it was docked in.</param>
public readonly record struct UIDockPanelUndockedEvent(Entity Panel, DockZone PreviousZone);

/// <summary>
/// Event raised when a panel's dock state changes.
/// </summary>
/// <param name="Panel">The panel whose state changed.</param>
/// <param name="OldState">Previous dock state.</param>
/// <param name="NewState">New dock state.</param>
public readonly record struct UIDockStateChangedEvent(Entity Panel, DockState OldState, DockState NewState);

/// <summary>
/// Event raised when a dock zone is resized.
/// </summary>
/// <param name="Zone">The zone that was resized.</param>
/// <param name="OldSize">Previous size in pixels.</param>
/// <param name="NewSize">New size in pixels.</param>
public readonly record struct UIDockZoneResizedEvent(Entity Zone, float OldSize, float NewSize);

/// <summary>
/// Event raised when the selected tab changes in a dock tab group.
/// </summary>
/// <param name="TabGroup">The tab group.</param>
/// <param name="PreviousIndex">Previous selected index.</param>
/// <param name="NewIndex">New selected index.</param>
public readonly record struct UIDockTabChangedEvent(Entity TabGroup, int PreviousIndex, int NewIndex);

/// <summary>
/// Event to request docking a panel to a zone.
/// </summary>
/// <param name="Panel">The panel to dock.</param>
/// <param name="Zone">The target zone.</param>
/// <param name="Container">The dock container.</param>
public readonly record struct UIDockRequestEvent(Entity Panel, DockZone Zone, Entity Container);

/// <summary>
/// Event to request floating (undocking) a panel.
/// </summary>
/// <param name="Panel">The panel to float.</param>
/// <param name="Position">The floating window position.</param>
public readonly record struct UIFloatRequestEvent(Entity Panel, Vector2 Position);

#region TreeView Events

/// <summary>
/// Event raised when a tree node is selected.
/// </summary>
/// <param name="Node">The selected tree node.</param>
/// <param name="TreeView">The tree view containing the node.</param>
public readonly record struct UITreeNodeSelectedEvent(Entity Node, Entity TreeView);

/// <summary>
/// Event raised when a tree node is expanded.
/// </summary>
/// <param name="Node">The expanded tree node.</param>
/// <param name="TreeView">The tree view containing the node.</param>
public readonly record struct UITreeNodeExpandedEvent(Entity Node, Entity TreeView);

/// <summary>
/// Event raised when a tree node is collapsed.
/// </summary>
/// <param name="Node">The collapsed tree node.</param>
/// <param name="TreeView">The tree view containing the node.</param>
public readonly record struct UITreeNodeCollapsedEvent(Entity Node, Entity TreeView);

/// <summary>
/// Event raised when a tree node is double-clicked (e.g., to open a file).
/// </summary>
/// <param name="Node">The double-clicked tree node.</param>
/// <param name="TreeView">The tree view containing the node.</param>
public readonly record struct UITreeNodeDoubleClickedEvent(Entity Node, Entity TreeView);

#endregion

#region PropertyGrid Events

/// <summary>
/// Event raised when a property value changes in a property grid.
/// </summary>
/// <param name="PropertyGrid">The property grid entity.</param>
/// <param name="Row">The property row that changed.</param>
/// <param name="PropertyName">The name of the property.</param>
/// <param name="OldValue">The previous value (type depends on property type).</param>
/// <param name="NewValue">The new value (type depends on property type).</param>
public readonly record struct UIPropertyChangedEvent(
    Entity PropertyGrid,
    Entity Row,
    string PropertyName,
    object? OldValue,
    object? NewValue);

/// <summary>
/// Event raised when a property category is expanded.
/// </summary>
/// <param name="PropertyGrid">The property grid entity.</param>
/// <param name="Category">The category that was expanded.</param>
public readonly record struct UIPropertyCategoryExpandedEvent(Entity PropertyGrid, Entity Category);

/// <summary>
/// Event raised when a property category is collapsed.
/// </summary>
/// <param name="PropertyGrid">The property grid entity.</param>
/// <param name="Category">The category that was collapsed.</param>
public readonly record struct UIPropertyCategoryCollapsedEvent(Entity PropertyGrid, Entity Category);

#endregion

#region Modal Events

/// <summary>
/// Event raised when a modal dialog is opened.
/// </summary>
/// <param name="Modal">The modal entity that was opened.</param>
public readonly record struct UIModalOpenedEvent(Entity Modal);

/// <summary>
/// Event raised when a modal dialog is closed.
/// </summary>
/// <param name="Modal">The modal entity that was closed.</param>
/// <param name="Result">The result of the modal (OK, Cancel, etc.).</param>
public readonly record struct UIModalClosedEvent(Entity Modal, ModalResult Result);

/// <summary>
/// Event raised when a modal action button is clicked.
/// </summary>
/// <param name="Modal">The modal entity.</param>
/// <param name="Button">The button entity that was clicked.</param>
/// <param name="Result">The result value of the clicked button.</param>
public readonly record struct UIModalResultEvent(Entity Modal, Entity Button, ModalResult Result);

#endregion

#region Toast Events

/// <summary>
/// Event raised when a toast notification is shown.
/// </summary>
/// <param name="Toast">The toast entity.</param>
public readonly record struct UIToastShownEvent(Entity Toast);

/// <summary>
/// Event raised when a toast notification is dismissed.
/// </summary>
/// <param name="Toast">The toast entity.</param>
/// <param name="WasManual">True if dismissed by user, false if auto-dismissed.</param>
public readonly record struct UIToastDismissedEvent(Entity Toast, bool WasManual);

#endregion

#region Accordion Events

/// <summary>
/// Event raised when an accordion section is expanded.
/// </summary>
/// <param name="Accordion">The accordion entity.</param>
/// <param name="Section">The section that was expanded.</param>
public readonly record struct UIAccordionSectionExpandedEvent(Entity Accordion, Entity Section);

/// <summary>
/// Event raised when an accordion section is collapsed.
/// </summary>
/// <param name="Accordion">The accordion entity.</param>
/// <param name="Section">The section that was collapsed.</param>
public readonly record struct UIAccordionSectionCollapsedEvent(Entity Accordion, Entity Section);

#endregion

#region Window Events

/// <summary>
/// Event raised when a window is minimized.
/// </summary>
/// <param name="Window">The window entity that was minimized.</param>
public readonly record struct UIWindowMinimizedEvent(Entity Window);

/// <summary>
/// Event raised when a window is maximized.
/// </summary>
/// <param name="Window">The window entity that was maximized.</param>
public readonly record struct UIWindowMaximizedEvent(Entity Window);

/// <summary>
/// Event raised when a window is restored from minimized or maximized state.
/// </summary>
/// <param name="Window">The window entity that was restored.</param>
/// <param name="PreviousState">The state the window was restored from.</param>
public readonly record struct UIWindowRestoredEvent(Entity Window, WindowState PreviousState);

#endregion
