using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph.Rendering;

/// <summary>
/// Context passed to node widgets for rendering and input handling.
/// </summary>
/// <remarks>
/// <para>
/// Provides access to rendering, input devices, and the current world state.
/// Widgets use this context to draw themselves and process user interactions.
/// </para>
/// <para>
/// Created fresh each frame by the GraphWidgetSystem and passed to RenderBody methods.
/// </para>
/// </remarks>
public readonly struct WidgetContext
{
    /// <summary>
    /// The 2D renderer for drawing widget content.
    /// </summary>
    public required I2DRenderer Renderer { get; init; }

    /// <summary>
    /// The ECS world containing the graph entities.
    /// </summary>
    public required IWorld World { get; init; }

    /// <summary>
    /// The canvas entity containing the node.
    /// </summary>
    public required Entity Canvas { get; init; }

    /// <summary>
    /// The node entity containing the widget.
    /// </summary>
    public required Entity Node { get; init; }

    /// <summary>
    /// The mouse input device for click/drag detection.
    /// </summary>
    public required IMouse Mouse { get; init; }

    /// <summary>
    /// The keyboard input device for text input and hotkeys.
    /// </summary>
    public required IKeyboard Keyboard { get; init; }

    /// <summary>
    /// The currently focused widget, if any.
    /// </summary>
    /// <remarks>
    /// Null if no widget is focused. Widgets should check if they have focus
    /// by comparing their WidgetId against Focus.WidgetId when Focus is not null.
    /// </remarks>
    public WidgetFocus? Focus { get; init; }

    /// <summary>
    /// The current zoom level of the canvas.
    /// </summary>
    /// <remarks>
    /// Used to scale widget rendering appropriately. A zoom of 1.0 is normal size.
    /// </remarks>
    public float Zoom { get; init; }

    /// <summary>
    /// The canvas offset (pan position) in world coordinates.
    /// </summary>
    public Vector2 Offset { get; init; }

    /// <summary>
    /// The current frame's delta time in seconds.
    /// </summary>
    public float DeltaTime { get; init; }

    /// <summary>
    /// Gets whether the specified widget currently has focus.
    /// </summary>
    /// <param name="widgetId">The widget ID to check.</param>
    /// <returns><c>true</c> if this widget has focus; otherwise, <c>false</c>.</returns>
    public bool HasFocus(int widgetId)
    {
        return Focus.HasValue && Focus.Value.Node == Node && Focus.Value.WidgetId == widgetId;
    }

    /// <summary>
    /// Gets whether the mouse position is within the specified rectangle.
    /// </summary>
    /// <param name="area">The area to test in screen coordinates.</param>
    /// <returns><c>true</c> if the mouse is within the area; otherwise, <c>false</c>.</returns>
    public bool IsMouseOver(Rectangle area)
    {
        var pos = Mouse.Position;
        return pos.X >= area.X && pos.X < area.Right &&
               pos.Y >= area.Y && pos.Y < area.Bottom;
    }

    /// <summary>
    /// Gets whether the left mouse button was just clicked within the specified area.
    /// </summary>
    /// <param name="area">The area to test in screen coordinates.</param>
    /// <returns><c>true</c> if a click occurred within the area; otherwise, <c>false</c>.</returns>
    public bool WasClicked(Rectangle area)
    {
        return Mouse.IsButtonDown(MouseButton.Left) && IsMouseOver(area);
    }
}
