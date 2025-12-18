using System.Numerics;
using KeenEyes;
using KeenEyes.Input.Abstractions;

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
