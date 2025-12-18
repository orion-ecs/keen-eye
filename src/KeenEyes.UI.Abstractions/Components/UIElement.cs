namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Core component for all UI elements. Required for any entity to be part of the UI system.
/// </summary>
/// <remarks>
/// <para>
/// This component serves as the base marker for UI elements and contains visibility
/// and raycast settings that apply to the entire UI hierarchy.
/// </para>
/// <para>
/// For interactive elements, combine with <see cref="UIInteractable"/>.
/// For positioned elements, combine with <see cref="UIRect"/>.
/// </para>
/// </remarks>
public struct UIElement : IComponent
{
    /// <summary>
    /// Whether this element and its children are visible.
    /// </summary>
    /// <remarks>
    /// When false, the element and all descendants are not rendered.
    /// Note: Use <c>UIHiddenTag</c> for temporarily hiding elements while preserving layout space.
    /// </remarks>
    public bool Visible;

    /// <summary>
    /// Whether this element can receive pointer events (clicks, hovers).
    /// </summary>
    /// <remarks>
    /// When false, pointer events pass through to elements behind this one.
    /// This is useful for overlay elements that shouldn't block interaction.
    /// </remarks>
    public bool RaycastTarget;

    /// <summary>
    /// Creates a new UI element with default settings (visible, raycast enabled).
    /// </summary>
    public static UIElement Default => new() { Visible = true, RaycastTarget = true };

    /// <summary>
    /// Creates a UI element that doesn't receive pointer events.
    /// </summary>
    public static UIElement NonInteractive => new() { Visible = true, RaycastTarget = false };
}
