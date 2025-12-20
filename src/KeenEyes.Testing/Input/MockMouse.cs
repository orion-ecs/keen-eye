using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// A mock mouse implementation for testing mouse-dependent systems.
/// </summary>
/// <remarks>
/// <para>
/// MockMouse provides two categories of methods:
/// </para>
/// <list type="bullet">
/// <item><b>State methods</b> (SetPosition, SetButtonDown): Change state without firing events.
/// Use these for polling-based input tests.</item>
/// <item><b>Simulate methods</b> (SimulateMove, SimulateButtonDown): Change state AND fire events.
/// Use these for event-driven input tests.</item>
/// </list>
/// <para>
/// Convenience methods like <see cref="SimulateClick"/> and
/// <see cref="SimulateDrag(Vector2, Vector2, MouseButton, KeyModifiers)"/>
/// perform multiple operations in sequence for common interaction patterns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mouse = new MockMouse();
///
/// // For polling-based tests
/// mouse.SetPosition(100, 100);
/// mouse.SetButtonDown(MouseButton.Left);
/// Assert.True(mouse.IsButtonDown(MouseButton.Left));
///
/// // For event-driven tests
/// bool clicked = false;
/// mouse.OnButtonDown += args => clicked = true;
/// mouse.SimulateButtonDown(MouseButton.Left);
/// Assert.True(clicked);
///
/// // Convenience methods
/// mouse.SimulateClick(MouseButton.Left); // Down + Up
/// mouse.SimulateDrag(new Vector2(0, 0), new Vector2(100, 100)); // Full drag sequence
/// </code>
/// </example>
public sealed class MockMouse : IMouse
{
    private Vector2 position;
    private Vector2 scrollDelta;
    private readonly Dictionary<MouseButton, bool> buttons = [];

    #region State Control Methods

    /// <summary>
    /// Sets the mouse position without firing events.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void SetPosition(float x, float y)
    {
        position = new Vector2(x, y);
    }

    /// <summary>
    /// Sets the mouse position without firing events.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vector2 position)
    {
        this.position = position;
    }

    /// <summary>
    /// Sets a mouse button as pressed without firing events.
    /// </summary>
    /// <param name="button">The button to press.</param>
    public void SetButtonDown(MouseButton button)
    {
        buttons[button] = true;
    }

    /// <summary>
    /// Sets a mouse button as released without firing events.
    /// </summary>
    /// <param name="button">The button to release.</param>
    public void SetButtonUp(MouseButton button)
    {
        buttons[button] = false;
    }

    /// <summary>
    /// Sets the scroll delta for the next GetState call.
    /// </summary>
    /// <param name="x">Horizontal scroll delta.</param>
    /// <param name="y">Vertical scroll delta.</param>
    public void SetScrollDelta(float x, float y)
    {
        scrollDelta = new Vector2(x, y);
    }

    /// <summary>
    /// Releases all mouse buttons.
    /// </summary>
    public void ClearAllButtons()
    {
        buttons.Clear();
    }

    /// <summary>
    /// Resets mouse to initial state (position 0,0, no buttons pressed).
    /// </summary>
    public void Reset()
    {
        position = Vector2.Zero;
        scrollDelta = Vector2.Zero;
        buttons.Clear();
        IsCursorVisible = true;
        IsCursorCaptured = false;
    }

    #endregion

    #region Event Simulation Methods

    /// <summary>
    /// Simulates mouse movement, updating position and firing the OnMove event.
    /// </summary>
    /// <param name="newPosition">The new mouse position.</param>
    public void SimulateMove(Vector2 newPosition)
    {
        var delta = newPosition - position;
        position = newPosition;
        OnMove?.Invoke(new MouseMoveEventArgs(position, delta));
    }

    /// <summary>
    /// Simulates mouse movement by delta, updating position and firing the OnMove event.
    /// </summary>
    /// <param name="delta">The movement delta.</param>
    public void SimulateMoveBy(Vector2 delta)
    {
        position += delta;
        OnMove?.Invoke(new MouseMoveEventArgs(position, delta));
    }

    /// <summary>
    /// Simulates pressing a mouse button, updating state and firing the OnButtonDown event.
    /// </summary>
    /// <param name="button">The button to press.</param>
    /// <param name="modifiers">Keyboard modifiers held during the press.</param>
    public void SimulateButtonDown(MouseButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        buttons[button] = true;
        OnButtonDown?.Invoke(new MouseButtonEventArgs(button, position, modifiers));
    }

    /// <summary>
    /// Simulates releasing a mouse button, updating state and firing the OnButtonUp event.
    /// </summary>
    /// <param name="button">The button to release.</param>
    /// <param name="modifiers">Keyboard modifiers held during the release.</param>
    public void SimulateButtonUp(MouseButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        buttons[button] = false;
        OnButtonUp?.Invoke(new MouseButtonEventArgs(button, position, modifiers));
    }

    /// <summary>
    /// Simulates scrolling, firing the OnScroll event.
    /// </summary>
    /// <param name="deltaX">Horizontal scroll amount.</param>
    /// <param name="deltaY">Vertical scroll amount (positive = up).</param>
    public void SimulateScroll(float deltaX, float deltaY)
    {
        scrollDelta = new Vector2(deltaX, deltaY);
        OnScroll?.Invoke(new MouseScrollEventArgs(scrollDelta, position));
    }

    /// <summary>
    /// Simulates vertical scrolling, firing the OnScroll event.
    /// </summary>
    /// <param name="delta">Vertical scroll amount (positive = up).</param>
    public void SimulateScrollVertical(float delta)
    {
        SimulateScroll(0, delta);
    }

    /// <summary>
    /// Simulates the mouse cursor entering the window.
    /// </summary>
    public void SimulateEnter()
    {
        OnEnter?.Invoke();
    }

    /// <summary>
    /// Simulates the mouse cursor leaving the window.
    /// </summary>
    public void SimulateLeave()
    {
        OnLeave?.Invoke();
    }

    #endregion

    #region Convenience Methods

    /// <summary>
    /// Simulates a complete click (button down followed by button up).
    /// </summary>
    /// <param name="button">The button to click.</param>
    /// <param name="modifiers">Keyboard modifiers held during the click.</param>
    public void SimulateClick(MouseButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        SimulateButtonDown(button, modifiers);
        SimulateButtonUp(button, modifiers);
    }

    /// <summary>
    /// Simulates a double-click (two complete clicks in quick succession).
    /// </summary>
    /// <param name="button">The button to double-click.</param>
    /// <param name="modifiers">Keyboard modifiers held during the clicks.</param>
    public void SimulateDoubleClick(MouseButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        SimulateClick(button, modifiers);
        SimulateClick(button, modifiers);
    }

    /// <summary>
    /// Simulates a complete drag operation (move to start, press, move to end, release).
    /// </summary>
    /// <param name="start">The starting position of the drag.</param>
    /// <param name="end">The ending position of the drag.</param>
    /// <param name="button">The button to hold during the drag. Defaults to left button.</param>
    /// <param name="modifiers">Keyboard modifiers held during the drag.</param>
    public void SimulateDrag(Vector2 start, Vector2 end, MouseButton button = MouseButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        SimulateMove(start);
        SimulateButtonDown(button, modifiers);
        SimulateMove(end);
        SimulateButtonUp(button, modifiers);
    }

    /// <summary>
    /// Simulates a drag operation with intermediate points.
    /// </summary>
    /// <param name="points">The sequence of points to drag through.</param>
    /// <param name="button">The button to hold during the drag. Defaults to left button.</param>
    /// <param name="modifiers">Keyboard modifiers held during the drag.</param>
    /// <exception cref="ArgumentException">Thrown when fewer than 2 points are provided.</exception>
    public void SimulateDrag(IReadOnlyList<Vector2> points, MouseButton button = MouseButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("At least 2 points are required for a drag operation.", nameof(points));
        }

        SimulateMove(points[0]);
        SimulateButtonDown(button, modifiers);

        for (int i = 1; i < points.Count; i++)
        {
            SimulateMove(points[i]);
        }

        SimulateButtonUp(button, modifiers);
    }

    #endregion

    #region IMouse Implementation

    /// <inheritdoc />
    public MouseState GetState()
    {
        var result = new MouseState(position, GetButtonFlags(), scrollDelta);
        scrollDelta = Vector2.Zero; // Clear scroll delta after reading
        return result;
    }

    /// <inheritdoc />
    public Vector2 Position => position;

    /// <inheritdoc />
    public bool IsButtonDown(MouseButton button) => buttons.GetValueOrDefault(button, false);

    /// <inheritdoc />
    public bool IsButtonUp(MouseButton button) => !buttons.GetValueOrDefault(button, false);

    /// <inheritdoc />
    public bool IsCursorVisible { get; set; } = true;

    /// <inheritdoc />
    public bool IsCursorCaptured { get; set; }

    /// <inheritdoc />
    void IMouse.SetPosition(Vector2 position)
    {
        this.position = position;
    }

    /// <inheritdoc />
    public event Action<MouseButtonEventArgs>? OnButtonDown;

    /// <inheritdoc />
    public event Action<MouseButtonEventArgs>? OnButtonUp;

    /// <inheritdoc />
    public event Action<MouseMoveEventArgs>? OnMove;

    /// <inheritdoc />
    public event Action<MouseScrollEventArgs>? OnScroll;

    /// <inheritdoc />
    public event Action? OnEnter;

    /// <inheritdoc />
    public event Action? OnLeave;

    #endregion

    #region Private Helpers

    private MouseButtons GetButtonFlags()
    {
        var flags = MouseButtons.None;

        if (buttons.GetValueOrDefault(MouseButton.Left, false))
        {
            flags |= MouseButtons.Left;
        }

        if (buttons.GetValueOrDefault(MouseButton.Right, false))
        {
            flags |= MouseButtons.Right;
        }

        if (buttons.GetValueOrDefault(MouseButton.Middle, false))
        {
            flags |= MouseButtons.Middle;
        }

        if (buttons.GetValueOrDefault(MouseButton.Button4, false))
        {
            flags |= MouseButtons.Button4;
        }

        if (buttons.GetValueOrDefault(MouseButton.Button5, false))
        {
            flags |= MouseButtons.Button5;
        }

        return flags;
    }

    #endregion
}
