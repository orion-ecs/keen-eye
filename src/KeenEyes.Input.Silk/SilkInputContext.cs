using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;
using KeenEyes.Platform.Silk;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Silk.NET implementation of the input context.
/// </summary>
/// <remarks>
/// <para>
/// This context wraps Silk.NET's input system and provides both polling-based
/// state queries and event-based input notification.
/// </para>
/// <para>
/// The input context uses a shared <see cref="ISilkWindowProvider"/> to access
/// the Silk.NET input context, allowing graphics and input plugins to share
/// the same window.
/// </para>
/// </remarks>
[PluginExtension("SilkInput")]
public sealed class SilkInputContext : IInputContext
{
    private readonly ISilkWindowProvider windowProvider;
    private readonly SilkInputConfig config;
    private SilkInput.IInputContext? silkInput;
    private SilkKeyboard? primaryKeyboard;
    private SilkMouse? primaryMouse;
    private SilkGamepad? primaryGamepad;
    private ImmutableArray<IKeyboard> keyboards = [];
    private ImmutableArray<IMouse> mice = [];
    private ImmutableArray<IGamepad> gamepads = [];
    private bool initialized;
    private bool disposed;

    /// <inheritdoc />
    public IKeyboard Keyboard => primaryKeyboard
        ?? throw DeviceUnavailable(nameof(Keyboard), "keyboard", nameof(Keyboards));

    /// <inheritdoc />
    public IMouse Mouse => primaryMouse
        ?? throw DeviceUnavailable(nameof(Mouse), "mouse", nameof(Mice));

    /// <inheritdoc />
    public IGamepad Gamepad => primaryGamepad ?? throw GamepadUnavailable();

    /// <inheritdoc />
    public ImmutableArray<IKeyboard> Keyboards => keyboards;

    /// <inheritdoc />
    public ImmutableArray<IMouse> Mice => mice;

    /// <inheritdoc />
    public ImmutableArray<IGamepad> Gamepads => gamepads;

    /// <inheritdoc />
    public int ConnectedGamepadCount => gamepads.Count(g => g.IsConnected);

    /// <summary>
    /// Gets whether the input context has been initialized.
    /// </summary>
    /// <remarks>
    /// This becomes <see langword="true"/> only once the window has loaded AND the shared
    /// Silk.NET input context was actually obtained from it. While it is <see langword="false"/>
    /// no devices exist at all; once it is <see langword="true"/>, the device collections
    /// (<see cref="Keyboards"/>, <see cref="Mice"/>, <see cref="Gamepads"/>) report what the
    /// machine really has, which for gamepads is frequently nothing.
    /// </remarks>
    public bool IsInitialized => initialized;

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

    internal SilkInputContext(ISilkWindowProvider windowProvider, SilkInputConfig config)
    {
        this.windowProvider = windowProvider;
        this.config = config;

        // Hook into the provider's load event rather than the window's: the provider
        // creates the Silk.NET input context inside its own window-load handler and
        // then raises this, so InputContext is guaranteed to exist by the time we run.
        windowProvider.OnLoad += OnWindowLoad;
    }

    private void OnWindowLoad()
    {
        // Get the input context from the shared window provider
        var silkInputContext = windowProvider.InputContext;
        if (silkInputContext is null)
        {
            // Nothing to wrap, so stay uninitialized: claiming otherwise would make
            // IsInitialized report success while every device getter fails, which is
            // exactly the lie that makes "no gamepad" look like "window not loaded".
            return;
        }

        silkInput = silkInputContext;

        // Wrap Silk.NET devices in our abstractions
        InitializeDevices(silkInputContext);

        initialized = true;
    }

    /// <summary>
    /// Builds the exception thrown when a primary device is unavailable, distinguishing
    /// "input has not initialized yet" from "this machine has no such device" — two very
    /// different problems that used to share one misleading message.
    /// </summary>
    /// <param name="property">The name of the property being read.</param>
    /// <param name="deviceName">The human-readable device name.</param>
    /// <param name="checkHint">The property (or properties) the caller should test first.</param>
    private InvalidOperationException DeviceUnavailable(string property, string deviceName, string checkHint)
        => initialized
            ? new InvalidOperationException(
                $"No {deviceName} is connected, so {property} has no device to return. " +
                $"Check {checkHint} first and handle the case where none is present.")
            : new InvalidOperationException(
                $"Input is not initialized, so {property} is unavailable. Wait for the window to load " +
                $"(see {nameof(IsInitialized)}) before reading input devices.");

    /// <summary>
    /// Builds the exception for a missing primary gamepad, which has a third case the other
    /// devices do not: gamepad support switched off in configuration. Reporting that as
    /// "nothing is plugged in" would send the caller looking for a hardware problem.
    /// </summary>
    private InvalidOperationException GamepadUnavailable()
        => initialized && !config.EnableGamepads
            ? new InvalidOperationException(
                $"Gamepad support is disabled ({nameof(SilkInputConfig)}." +
                $"{nameof(SilkInputConfig.EnableGamepads)} is false), so {nameof(Gamepad)} has no device " +
                $"to return, whether or not a controller is plugged in.")
            : DeviceUnavailable(
                nameof(Gamepad), "gamepad", $"{nameof(ConnectedGamepadCount)} or {nameof(Gamepads)}");

    private void InitializeDevices(SilkInput.IInputContext silkInputContext)
    {
        // Initialize keyboards
        var keyboardList = new List<IKeyboard>();
        foreach (var keyboard in silkInputContext.Keyboards)
        {
            var wrapper = new SilkKeyboard(keyboard);
            keyboardList.Add(wrapper);
            SubscribeKeyboardEvents(wrapper);
        }
        keyboards = [.. keyboardList];
        primaryKeyboard = keyboardList.Count > 0 ? (SilkKeyboard)keyboardList[0] : null;

        // Initialize mice
        var mouseList = new List<IMouse>();
        foreach (var mouse in silkInputContext.Mice)
        {
            var wrapper = new SilkMouse(mouse, config);
            mouseList.Add(wrapper);
            SubscribeMouseEvents(wrapper);
        }
        mice = [.. mouseList];
        primaryMouse = mouseList.Count > 0 ? (SilkMouse)mouseList[0] : null;

        // Initialize gamepads
        if (config.EnableGamepads)
        {
            var gamepadList = new List<IGamepad>();
            foreach (var gamepad in silkInputContext.Gamepads.Take(config.MaxGamepads))
            {
                var wrapper = new SilkGamepad(gamepad, config.GamepadDeadzone);
                gamepadList.Add(wrapper);
                SubscribeGamepadEvents(wrapper);
            }
            gamepads = [.. gamepadList];
            primaryGamepad = gamepadList.Count > 0 ? (SilkGamepad)gamepadList[0] : null;

            // Subscribe to connection events
            silkInputContext.ConnectionChanged += OnConnectionChanged;
        }
    }

    private void SubscribeKeyboardEvents(SilkKeyboard keyboard)
    {
        keyboard.OnKeyDown += args => OnKeyDown?.Invoke(keyboard, args);
        keyboard.OnKeyUp += args => OnKeyUp?.Invoke(keyboard, args);
        keyboard.OnTextInput += c => OnTextInput?.Invoke(keyboard, c);
    }

    private void SubscribeMouseEvents(SilkMouse mouse)
    {
        mouse.OnButtonDown += args => OnMouseButtonDown?.Invoke(mouse, args);
        mouse.OnButtonUp += args => OnMouseButtonUp?.Invoke(mouse, args);
        mouse.OnMove += args => OnMouseMove?.Invoke(mouse, args);
        mouse.OnScroll += args => OnMouseScroll?.Invoke(mouse, args);
    }

    private void SubscribeGamepadEvents(SilkGamepad gamepad)
    {
        gamepad.OnButtonDown += args => OnGamepadButtonDown?.Invoke(gamepad, args);
        gamepad.OnButtonUp += args => OnGamepadButtonUp?.Invoke(gamepad, args);
    }

    private void OnConnectionChanged(SilkInput.IInputDevice device, bool connected)
    {
        if (device is SilkInput.IGamepad)
        {
            // Find the wrapper for this device
            var wrapper = gamepads
                .OfType<SilkGamepad>()
                .FirstOrDefault(g => g.Index == device.Index);

            if (wrapper is not null)
            {
                if (connected)
                {
                    OnGamepadConnected?.Invoke(wrapper);
                }
                else
                {
                    OnGamepadDisconnected?.Invoke(wrapper);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Update()
    {
        // Silk.NET handles input polling automatically via window events
        // This method is here for consistency with the interface
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        windowProvider.OnLoad -= OnWindowLoad;

        if (silkInput is not null && config.EnableGamepads)
        {
            silkInput.ConnectionChanged -= OnConnectionChanged;
        }

        // Note: We don't dispose silkInput - it's owned by the window provider
    }
}
