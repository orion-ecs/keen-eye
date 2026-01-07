using System.Numerics;
using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Silk.NET implementation of <see cref="IMouse"/>.
/// </summary>
internal sealed class SilkMouse : IMouse
{
    private readonly SilkInput.IMouse mouse;
    private readonly SilkInputConfig config;
    private Vector2 lastPosition;
    private Vector2 scrollDelta;

    /// <inheritdoc />
    public Vector2 Position => new(mouse.Position.X, mouse.Position.Y);

    /// <inheritdoc />
    public bool IsCursorVisible
    {
        get => mouse.Cursor.CursorMode != SilkInput.CursorMode.Hidden &&
               mouse.Cursor.CursorMode != SilkInput.CursorMode.Raw;
        set => mouse.Cursor.CursorMode = value ? SilkInput.CursorMode.Normal : SilkInput.CursorMode.Hidden;
    }

    /// <inheritdoc />
    public bool IsCursorCaptured
    {
        get => mouse.Cursor.CursorMode == SilkInput.CursorMode.Raw;
        set => mouse.Cursor.CursorMode = value ? SilkInput.CursorMode.Raw : SilkInput.CursorMode.Normal;
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
#pragma warning disable CS0067 // Event is never used - Silk.NET doesn't expose enter/leave events
    public event Action? OnEnter;

    /// <inheritdoc />
    public event Action? OnLeave;
#pragma warning restore CS0067

    internal SilkMouse(SilkInput.IMouse mouse, SilkInputConfig config)
    {
        this.mouse = mouse;
        this.config = config;
        lastPosition = new Vector2(mouse.Position.X, mouse.Position.Y);

        mouse.MouseDown += HandleMouseDown;
        mouse.MouseUp += HandleMouseUp;
        mouse.MouseMove += HandleMouseMove;
        mouse.Scroll += HandleScroll;
    }

    /// <inheritdoc />
    public bool IsButtonDown(MouseButton button)
    {
        var silkButton = MouseButtonMapper.ToSilkButton(button);
        return mouse.IsButtonPressed(silkButton);
    }

    /// <inheritdoc />
    public bool IsButtonUp(MouseButton button)
    {
        return !IsButtonDown(button);
    }

    /// <inheritdoc />
    public MouseState GetState()
    {
        var pressedButtons = MouseButtons.None;

        if (mouse.IsButtonPressed(SilkInput.MouseButton.Left))
        {
            pressedButtons |= MouseButtons.Left;
        }

        if (mouse.IsButtonPressed(SilkInput.MouseButton.Right))
        {
            pressedButtons |= MouseButtons.Right;
        }

        if (mouse.IsButtonPressed(SilkInput.MouseButton.Middle))
        {
            pressedButtons |= MouseButtons.Middle;
        }

        if (mouse.IsButtonPressed(SilkInput.MouseButton.Button4))
        {
            pressedButtons |= MouseButtons.Button4;
        }

        if (mouse.IsButtonPressed(SilkInput.MouseButton.Button5))
        {
            pressedButtons |= MouseButtons.Button5;
        }

        return new MouseState(Position, pressedButtons, scrollDelta);
    }

    /// <inheritdoc />
    public void SetPosition(Vector2 position)
    {
        mouse.Position = position;
    }

    private void HandleMouseDown(SilkInput.IMouse _, SilkInput.MouseButton silkButton)
    {
        var button = MouseButtonMapper.FromSilkButton(silkButton);
        if (button.HasValue)
        {
            OnButtonDown?.Invoke(new MouseButtonEventArgs(button.Value, Position, GetCurrentModifiers()));

            // Auto-capture on click if configured
            if (config.CaptureMouseOnClick && !IsCursorCaptured)
            {
                IsCursorCaptured = true;
            }
        }
    }

    private void HandleMouseUp(SilkInput.IMouse _, SilkInput.MouseButton silkButton)
    {
        var button = MouseButtonMapper.FromSilkButton(silkButton);
        if (button.HasValue)
        {
            OnButtonUp?.Invoke(new MouseButtonEventArgs(button.Value, Position, GetCurrentModifiers()));
        }
    }

    private static KeyModifiers GetCurrentModifiers()
    {
        // Mouse events in Silk.NET don't provide modifier info directly,
        // so we return None. Users should check keyboard state if needed.
        return KeyModifiers.None;
    }

    private void HandleMouseMove(SilkInput.IMouse _, System.Numerics.Vector2 position)
    {
        var currentPosition = new Vector2(position.X, position.Y);
        var delta = currentPosition - lastPosition;
        lastPosition = currentPosition;

        OnMove?.Invoke(new MouseMoveEventArgs(currentPosition, delta));
    }

    private void HandleScroll(SilkInput.IMouse _, SilkInput.ScrollWheel wheel)
    {
        scrollDelta = new Vector2(wheel.X, wheel.Y);
        OnScroll?.Invoke(new MouseScrollEventArgs(scrollDelta, Position));
    }
}
