using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// A mock input context that provides access to all mock input devices.
/// </summary>
/// <remarks>
/// <para>
/// MockInputContext integrates <see cref="MockKeyboard"/>, <see cref="MockMouse"/>,
/// and multiple <see cref="MockGamepad"/> instances into a single testable input context.
/// </para>
/// <para>
/// Use the <see cref="MockKeyboard"/>, <see cref="MockMouse"/>, and <see cref="GetMockGamepad"/>
/// properties to access the underlying mock devices for state manipulation and event simulation.
/// </para>
/// <para>
/// Convenience methods are provided for common operations:
/// </para>
/// <list type="bullet">
/// <item><see cref="SetKeyDown"/> / <see cref="SetKeyUp"/> - Quick keyboard state changes</item>
/// <item><see cref="SetMousePosition(float, float)"/> / <see cref="SetMouseButton"/> - Quick mouse state changes</item>
/// <item><see cref="SetGamepadButton"/> / <see cref="SetGamepadStick"/> - Quick gamepad state changes</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// using var input = new MockInputContext(gamepadCount: 2);
///
/// // Quick state setup
/// input.SetKeyDown(Key.W);
/// input.SetMousePosition(100, 100);
/// input.SetMouseButton(MouseButton.Left, true);
/// input.SetGamepadStick(0, isLeft: true, 1.0f, 0.0f);
///
/// // Or access mock devices directly for more control
/// input.MockKeyboard.SimulateKeyDown(Key.Space);
/// input.MockMouse.SimulateDrag(Vector2.Zero, new Vector2(200, 200));
/// input.GetMockGamepad(1).SimulateButtonDown(GamepadButton.South);
/// </code>
/// </example>
public sealed class MockInputContext : IInputContext
{
    private readonly MockKeyboard keyboard;
    private readonly MockMouse mouse;
    private readonly MockGamepad[] gamepads;
    private bool disposed;

    /// <summary>
    /// Creates a new mock input context with the specified number of gamepads.
    /// </summary>
    /// <param name="gamepadCount">The number of gamepad slots to create. Defaults to 4.</param>
    public MockInputContext(int gamepadCount = 4)
    {
        keyboard = new MockKeyboard();
        mouse = new MockMouse();
        gamepads = new MockGamepad[gamepadCount];

        for (int i = 0; i < gamepadCount; i++)
        {
            gamepads[i] = new MockGamepad(i, $"Mock Gamepad {i}", connected: i == 0);
        }

        // Wire up device events to global events
        WireKeyboardEvents();
        WireMouseEvents();
        WireGamepadEvents();
    }

    #region Mock Device Access

    /// <summary>
    /// Gets direct access to the mock keyboard for state manipulation and event simulation.
    /// </summary>
    public MockKeyboard MockKeyboard => keyboard;

    /// <summary>
    /// Gets direct access to the mock mouse for state manipulation and event simulation.
    /// </summary>
    public MockMouse MockMouse => mouse;

    /// <summary>
    /// Gets direct access to a mock gamepad by index.
    /// </summary>
    /// <param name="index">The gamepad index (0-based).</param>
    /// <returns>The mock gamepad at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public MockGamepad GetMockGamepad(int index)
    {
        if (index < 0 || index >= gamepads.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Gamepad index must be between 0 and {gamepads.Length - 1}.");
        }

        return gamepads[index];
    }

    #endregion

    #region Convenience Methods - Keyboard

    /// <summary>
    /// Sets a key as pressed without firing events.
    /// </summary>
    /// <param name="key">The key to press.</param>
    public void SetKeyDown(Key key) => keyboard.SetKeyDown(key);

    /// <summary>
    /// Sets a key as released without firing events.
    /// </summary>
    /// <param name="key">The key to release.</param>
    public void SetKeyUp(Key key) => keyboard.SetKeyUp(key);

    /// <summary>
    /// Simulates pressing a key, firing events.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">Modifier keys held during the press.</param>
    public void SimulateKeyDown(Key key, KeyModifiers modifiers = KeyModifiers.None)
        => keyboard.SimulateKeyDown(key, modifiers);

    /// <summary>
    /// Simulates releasing a key, firing events.
    /// </summary>
    /// <param name="key">The key to release.</param>
    /// <param name="modifiers">Modifier keys held during the release.</param>
    public void SimulateKeyUp(Key key, KeyModifiers modifiers = KeyModifiers.None)
        => keyboard.SimulateKeyUp(key, modifiers);

    #endregion

    #region Convenience Methods - Mouse

    /// <summary>
    /// Sets the mouse position without firing events.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void SetMousePosition(float x, float y) => mouse.SetPosition(x, y);

    /// <summary>
    /// Sets the mouse position without firing events.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetMousePosition(System.Numerics.Vector2 position) => mouse.SetPosition(position);

    /// <summary>
    /// Sets a mouse button state without firing events.
    /// </summary>
    /// <param name="button">The button to set.</param>
    /// <param name="isDown">Whether the button should be pressed.</param>
    public void SetMouseButton(MouseButton button, bool isDown)
    {
        if (isDown)
        {
            mouse.SetButtonDown(button);
        }
        else
        {
            mouse.SetButtonUp(button);
        }
    }

    /// <summary>
    /// Simulates mouse movement, firing the OnMouseMove event.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SimulateMouseMove(System.Numerics.Vector2 position) => mouse.SimulateMove(position);

    /// <summary>
    /// Simulates a mouse click at the current position.
    /// </summary>
    /// <param name="button">The button to click.</param>
    public void SimulateMouseClick(MouseButton button = MouseButton.Left) => mouse.SimulateClick(button);

    #endregion

    #region Convenience Methods - Gamepad

    /// <summary>
    /// Sets a gamepad button state without firing events.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index.</param>
    /// <param name="button">The button to set.</param>
    /// <param name="isDown">Whether the button should be pressed.</param>
    public void SetGamepadButton(int gamepadIndex, GamepadButton button, bool isDown)
    {
        var gamepad = GetMockGamepad(gamepadIndex);
        if (isDown)
        {
            gamepad.SetButtonDown(button);
        }
        else
        {
            gamepad.SetButtonUp(button);
        }
    }

    /// <summary>
    /// Sets a gamepad analog stick position without firing events.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index.</param>
    /// <param name="isLeft">True for left stick, false for right stick.</param>
    /// <param name="x">X position (-1 to 1).</param>
    /// <param name="y">Y position (-1 to 1).</param>
    public void SetGamepadStick(int gamepadIndex, bool isLeft, float x, float y)
    {
        var gamepad = GetMockGamepad(gamepadIndex);
        if (isLeft)
        {
            gamepad.SetLeftStick(x, y);
        }
        else
        {
            gamepad.SetRightStick(x, y);
        }
    }

    /// <summary>
    /// Sets a gamepad trigger value without firing events.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index.</param>
    /// <param name="isLeft">True for left trigger, false for right trigger.</param>
    /// <param name="value">Trigger value (0 to 1).</param>
    public void SetGamepadTrigger(int gamepadIndex, bool isLeft, float value)
    {
        var gamepad = GetMockGamepad(gamepadIndex);
        if (isLeft)
        {
            gamepad.SetLeftTrigger(value);
        }
        else
        {
            gamepad.SetRightTrigger(value);
        }
    }

    /// <summary>
    /// Sets the connection state of a gamepad.
    /// </summary>
    /// <param name="gamepadIndex">The gamepad index.</param>
    /// <param name="connected">Whether the gamepad should be connected.</param>
    public void SetGamepadConnected(int gamepadIndex, bool connected)
    {
        GetMockGamepad(gamepadIndex).SetConnected(connected);
    }

    #endregion

    #region IInputContext Implementation

    /// <inheritdoc />
    public IKeyboard Keyboard => keyboard;

    /// <inheritdoc />
    public IMouse Mouse => mouse;

    /// <inheritdoc />
    public IGamepad Gamepad => gamepads.Length > 0 ? gamepads[0] : throw new InvalidOperationException("No gamepads available.");

    /// <inheritdoc />
    public ImmutableArray<IKeyboard> Keyboards => [keyboard];

    /// <inheritdoc />
    public ImmutableArray<IMouse> Mice => [mouse];

    /// <inheritdoc />
    public ImmutableArray<IGamepad> Gamepads => [.. gamepads];

    /// <inheritdoc />
    public int ConnectedGamepadCount => gamepads.Count(g => g.IsConnected);

    /// <inheritdoc />
    public void Update()
    {
        // Mock input context doesn't need to poll hardware
        // State is already set directly via Set* methods
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Unhook events to prevent memory leaks
        UnwireKeyboardEvents();
        UnwireMouseEvents();
        UnwireGamepadEvents();
    }

    #endregion

    #region Global Events

    /// <inheritdoc />
    public event Action<IKeyboard, KeyEventArgs>? OnKeyDown;

    /// <inheritdoc />
    public event Action<IKeyboard, KeyEventArgs>? OnKeyUp;

    /// <inheritdoc />
    public event Action<IKeyboard, char>? OnTextInput;

    /// <inheritdoc />
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonDown;

    /// <inheritdoc />
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonUp;

    /// <inheritdoc />
    public event Action<IMouse, MouseMoveEventArgs>? OnMouseMove;

    /// <inheritdoc />
    public event Action<IMouse, MouseScrollEventArgs>? OnMouseScroll;

    /// <inheritdoc />
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonDown;

    /// <inheritdoc />
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonUp;

    /// <inheritdoc />
    public event Action<IGamepad>? OnGamepadConnected;

    /// <inheritdoc />
    public event Action<IGamepad>? OnGamepadDisconnected;

    #endregion

    #region Event Wiring

    private void WireKeyboardEvents()
    {
        keyboard.OnKeyDown += args => OnKeyDown?.Invoke(keyboard, args);
        keyboard.OnKeyUp += args => OnKeyUp?.Invoke(keyboard, args);
        keyboard.OnTextInput += c => OnTextInput?.Invoke(keyboard, c);
    }

    private void UnwireKeyboardEvents()
    {
        // Events on MockKeyboard are cleared when it's no longer referenced
    }

    private void WireMouseEvents()
    {
        mouse.OnButtonDown += args => OnMouseButtonDown?.Invoke(mouse, args);
        mouse.OnButtonUp += args => OnMouseButtonUp?.Invoke(mouse, args);
        mouse.OnMove += args => OnMouseMove?.Invoke(mouse, args);
        mouse.OnScroll += args => OnMouseScroll?.Invoke(mouse, args);
    }

    private void UnwireMouseEvents()
    {
        // Events on MockMouse are cleared when it's no longer referenced
    }

    private void WireGamepadEvents()
    {
        foreach (var gamepad in gamepads)
        {
            gamepad.OnButtonDown += args => OnGamepadButtonDown?.Invoke(gamepad, args);
            gamepad.OnButtonUp += args => OnGamepadButtonUp?.Invoke(gamepad, args);
            gamepad.OnConnected += g => OnGamepadConnected?.Invoke(g);
            gamepad.OnDisconnected += g => OnGamepadDisconnected?.Invoke(g);
        }
    }

    private void UnwireGamepadEvents()
    {
        // Events on MockGamepad are cleared when it's no longer referenced
    }

    #endregion
}
