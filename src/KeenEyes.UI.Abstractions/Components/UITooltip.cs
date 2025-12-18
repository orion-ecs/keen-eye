using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the preferred position for a tooltip relative to its trigger element.
/// </summary>
public enum TooltipPosition
{
    /// <summary>
    /// Automatically determine the best position based on available screen space.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Position the tooltip above the trigger element.
    /// </summary>
    Above = 1,

    /// <summary>
    /// Position the tooltip below the trigger element.
    /// </summary>
    Below = 2,

    /// <summary>
    /// Position the tooltip to the left of the trigger element.
    /// </summary>
    Left = 3,

    /// <summary>
    /// Position the tooltip to the right of the trigger element.
    /// </summary>
    Right = 4
}

/// <summary>
/// Specifies what triggers a popover to open.
/// </summary>
public enum PopoverTrigger
{
    /// <summary>
    /// Open on click, close on click outside or another click.
    /// </summary>
    Click = 0,

    /// <summary>
    /// Open on hover, close when mouse leaves.
    /// </summary>
    Hover = 1,

    /// <summary>
    /// Manually controlled via code.
    /// </summary>
    Manual = 2
}

/// <summary>
/// Component for a simple text tooltip shown when hovering over an element.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to any UI element to show a tooltip when the user hovers
/// over it. The tooltip appears after a configurable delay and follows the mouse
/// or stays near the element based on the <see cref="Position"/> setting.
/// </para>
/// </remarks>
/// <param name="text">The tooltip text to display.</param>
public struct UITooltip(string text) : IComponent
{
    /// <summary>
    /// The tooltip text to display.
    /// </summary>
    public string Text = text;

    /// <summary>
    /// Delay in seconds before the tooltip appears (default 0.5s).
    /// </summary>
    public float Delay = 0.5f;

    /// <summary>
    /// Maximum width for text wrapping (0 for no limit).
    /// </summary>
    public float MaxWidth = 300f;

    /// <summary>
    /// Preferred position for the tooltip.
    /// </summary>
    public TooltipPosition Position = TooltipPosition.Auto;

    /// <summary>
    /// Whether to follow the mouse cursor or stay anchored to the element.
    /// </summary>
    public bool FollowMouse = false;
}

/// <summary>
/// Component for a rich content popover that can contain any UI elements.
/// </summary>
/// <remarks>
/// <para>
/// Unlike tooltips, popovers can contain arbitrary UI content and stay open
/// until explicitly closed. They're useful for additional information panels,
/// mini-forms, or contextual actions.
/// </para>
/// </remarks>
public struct UIPopover : IComponent
{
    /// <summary>
    /// Whether the popover is currently visible.
    /// </summary>
    public bool IsOpen;

    /// <summary>
    /// The element that triggered this popover.
    /// </summary>
    public Entity TriggerElement;

    /// <summary>
    /// What action triggers the popover to open.
    /// </summary>
    public PopoverTrigger Trigger;

    /// <summary>
    /// Preferred position relative to the trigger element.
    /// </summary>
    public TooltipPosition Position;

    /// <summary>
    /// Whether clicking outside the popover closes it.
    /// </summary>
    public bool CloseOnClickOutside;

    /// <summary>
    /// Offset from the calculated position in pixels.
    /// </summary>
    public Vector2 Offset;
}

/// <summary>
/// Tag component marking the currently visible tooltip entity.
/// </summary>
/// <remarks>
/// Only one tooltip should be visible at a time. The tooltip system uses this
/// tag to track which tooltip entity is currently shown.
/// </remarks>
public struct UITooltipVisibleTag : ITagComponent;

/// <summary>
/// Internal component tracking tooltip hover state for timing.
/// </summary>
/// <remarks>
/// Added by UITooltipSystem to elements with UITooltip when hover begins.
/// Tracks the accumulated hover time to determine when to show the tooltip.
/// </remarks>
public struct UITooltipHoverState : IComponent
{
    /// <summary>
    /// The element being hovered that has the tooltip.
    /// </summary>
    public Entity HoveredElement;

    /// <summary>
    /// Time in seconds the element has been hovered.
    /// </summary>
    public float HoverTime;

    /// <summary>
    /// Position where the hover started (for positioning).
    /// </summary>
    public Vector2 HoverPosition;
}
