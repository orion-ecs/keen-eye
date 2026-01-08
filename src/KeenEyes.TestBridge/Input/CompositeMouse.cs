using System.Numerics;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// A mouse implementation that merges input from real and virtual sources.
/// </summary>
/// <remarks>
/// <para>
/// CompositeMouse allows both real hardware input and virtual (TestBridge-injected)
/// input to work together. Button states return true if EITHER source has the button pressed.
/// Position uses virtual if explicitly set, otherwise real.
/// Events are forwarded from BOTH sources.
/// </para>
/// <para>
/// This enables hybrid testing scenarios where real user input and automated test
/// input can coexist.
/// </para>
/// </remarks>
internal sealed class CompositeMouse : IMouse
{
    private readonly IMouse real;
    private readonly IMouse virtual_;
    private bool virtualPositionActive;

    /// <summary>
    /// Creates a new composite mouse merging real and virtual input.
    /// </summary>
    /// <param name="real">The real hardware mouse.</param>
    /// <param name="virtual_">The virtual (mock) mouse for TestBridge injection.</param>
    public CompositeMouse(IMouse real, IMouse virtual_)
    {
        this.real = real;
        this.virtual_ = virtual_;

        // Forward events from both sources
        real.OnButtonDown += args => OnButtonDown?.Invoke(args);
        real.OnButtonUp += args => OnButtonUp?.Invoke(args);
        real.OnMove += args => OnMove?.Invoke(args);
        real.OnScroll += args => OnScroll?.Invoke(args);
        real.OnEnter += () => OnEnter?.Invoke();
        real.OnLeave += () => OnLeave?.Invoke();

        virtual_.OnButtonDown += args => OnButtonDown?.Invoke(args);
        virtual_.OnButtonUp += args => OnButtonUp?.Invoke(args);
        virtual_.OnMove += args =>
        {
            virtualPositionActive = true;
            OnMove?.Invoke(args);
        };
        virtual_.OnScroll += args => OnScroll?.Invoke(args);
        virtual_.OnEnter += () => OnEnter?.Invoke();
        virtual_.OnLeave += () => OnLeave?.Invoke();
    }

    /// <inheritdoc />
    public MouseState GetState()
    {
        var realState = real.GetState();
        var virtualState = virtual_.GetState();

        // Use virtual position if it's been set, otherwise real
        var position = virtualPositionActive ? virtualState.Position : realState.Position;

        // Merge button states (OR them together)
        var mergedButtons = realState.PressedButtons | virtualState.PressedButtons;

        // Merge scroll deltas (add them)
        var mergedScroll = realState.ScrollDelta + virtualState.ScrollDelta;

        return new MouseState(position, mergedButtons, mergedScroll);
    }

    /// <inheritdoc />
    public Vector2 Position => virtualPositionActive ? virtual_.Position : real.Position;

    /// <inheritdoc />
    public bool IsButtonDown(MouseButton button) => real.IsButtonDown(button) || virtual_.IsButtonDown(button);

    /// <inheritdoc />
    public bool IsButtonUp(MouseButton button) => real.IsButtonUp(button) && virtual_.IsButtonUp(button);

    /// <inheritdoc />
    public bool IsCursorVisible
    {
        get => real.IsCursorVisible;
        set => real.IsCursorVisible = value;
    }

    /// <inheritdoc />
    public bool IsCursorCaptured
    {
        get => real.IsCursorCaptured;
        set => real.IsCursorCaptured = value;
    }

    /// <inheritdoc />
    public void SetPosition(Vector2 position)
    {
        // Setting position explicitly goes to real mouse (hardware cursor)
        real.SetPosition(position);
    }

    /// <summary>
    /// Resets the virtual position tracking, allowing real position to be used.
    /// </summary>
    public void ResetVirtualPosition()
    {
        virtualPositionActive = false;
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
}
