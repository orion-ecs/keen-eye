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
    public IKeyboard Keyboard => primaryKeyboard ?? throw new InvalidOperationException("Input not initialized. Wait for window to load.");

    /// <inheritdoc />
    public IMouse Mouse => primaryMouse ?? throw new InvalidOperationException("Input not initialized. Wait for window to load.");

    /// <inheritdoc />
    public IGamepad Gamepad => primaryGamepad ?? throw new InvalidOperationException("Input not initialized. Wait for window to load.");

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

        // Hook into window load to initialize input
        windowProvider.Window.Load += OnWindowLoad;
    }

    private void OnWindowLoad()
    {
        // Get the input context from the shared window provider
        silkInput = windowProvider.InputContext;

        // Wrap Silk.NET devices in our abstractions
        InitializeDevices();

        initialized = true;
    }

    private void InitializeDevices()
    {
        if (silkInput is null)
        {
            return;
        }

        // Initialize keyboards
        var keyboardList = new List<IKeyboard>();
        foreach (var keyboard in silkInput.Keyboards)
        {
            var wrapper = new SilkKeyboard(keyboard);
            keyboardList.Add(wrapper);
            SubscribeKeyboardEvents(wrapper);
        }
        keyboards = [.. keyboardList];
        primaryKeyboard = keyboardList.Count > 0 ? (SilkKeyboard)keyboardList[0] : null;

        // Initialize mice
        var mouseList = new List<IMouse>();
        foreach (var mouse in silkInput.Mice)
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
            foreach (var gamepad in silkInput.Gamepads.Take(config.MaxGamepads))
            {
                var wrapper = new SilkGamepad(gamepad, config.GamepadDeadzone);
                gamepadList.Add(wrapper);
                SubscribeGamepadEvents(wrapper);
            }
            gamepads = [.. gamepadList];
            primaryGamepad = gamepadList.Count > 0 ? (SilkGamepad)gamepadList[0] : null;

            // Subscribe to connection events
            silkInput.ConnectionChanged += OnConnectionChanged;
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

        windowProvider.Window.Load -= OnWindowLoad;

        if (silkInput is not null && config.EnableGamepads)
        {
            silkInput.ConnectionChanged -= OnConnectionChanged;
        }

        // Note: We don't dispose silkInput - it's owned by the window provider
    }
}
