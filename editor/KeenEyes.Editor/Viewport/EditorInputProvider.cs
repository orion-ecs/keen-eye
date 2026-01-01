using System.Numerics;

using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// Provides polling-based input state for the editor by wrapping <see cref="IInputContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This provider tracks mouse position and scroll wheel changes between frames to compute
/// per-frame deltas. The underlying input context handles event-based input, but viewport
/// camera control works better with polling.
/// </para>
/// <para>
/// Call <see cref="Update"/> once per frame before processing viewport input.
/// </para>
/// </remarks>
public sealed class EditorInputProvider : IInputProvider
{
    private readonly IInputContext _inputContext;
    private Vector2 _lastMousePosition;
    private Vector2 _mouseDelta;
    private Vector2 _scrollDelta;
    private bool _hasLastPosition;

    /// <summary>
    /// Creates a new editor input provider wrapping the specified input context.
    /// </summary>
    /// <param name="inputContext">The input context to wrap.</param>
    public EditorInputProvider(IInputContext inputContext)
    {
        ArgumentNullException.ThrowIfNull(inputContext);
        _inputContext = inputContext;

        // Subscribe to scroll events to accumulate scroll delta
        _inputContext.Mouse.OnScroll += OnMouseScroll;
    }

    /// <inheritdoc/>
    public Vector2 MousePosition => _inputContext.Mouse.Position;

    /// <inheritdoc/>
    public Vector2 MouseDelta => _mouseDelta;

    /// <inheritdoc/>
    public Vector2 MouseScrollDelta => _scrollDelta;

    /// <inheritdoc/>
    public bool IsMouseButtonDown(MouseButton button)
        => _inputContext.Mouse.IsButtonDown(button);

    /// <inheritdoc/>
    public bool IsKeyDown(Key key)
        => _inputContext.Keyboard.IsKeyDown(key);

    /// <inheritdoc/>
    public void Update()
    {
        var currentPosition = _inputContext.Mouse.Position;

        if (_hasLastPosition)
        {
            _mouseDelta = currentPosition - _lastMousePosition;
        }
        else
        {
            _mouseDelta = Vector2.Zero;
            _hasLastPosition = true;
        }

        _lastMousePosition = currentPosition;

        // Scroll delta is reset each frame after being consumed
        // (accumulated via OnMouseScroll event)
    }

    /// <summary>
    /// Resets the accumulated scroll delta. Call after processing input each frame.
    /// </summary>
    public void ResetScrollDelta()
    {
        _scrollDelta = Vector2.Zero;
    }

    private void OnMouseScroll(MouseScrollEventArgs args)
    {
        _scrollDelta += args.Delta;
    }
}
