#pragma warning disable CS0067 // Event is never used
#pragma warning disable CA1852 // Type can be sealed
#pragma warning disable IDE0028 // Collection initialization can be simplified
#pragma warning disable IDE0290 // Use primary constructor

using System.Collections.Immutable;
using System.Numerics;

namespace KeenEyes.Input.Abstractions.Tests;

/// <summary>
/// Mock implementation of <see cref="IInputContext"/> for testing.
/// </summary>
internal sealed class MockInputContext : IInputContext
{
    private readonly MockKeyboard keyboard = new();
    private readonly MockMouse mouse = new();
    private readonly MockGamepad[] gamepads = new MockGamepad[4];

    public MockInputContext()
    {
        for (int i = 0; i < gamepads.Length; i++)
        {
            gamepads[i] = new MockGamepad(i);
        }
    }

    public MockKeyboard Keyboard => keyboard;
    public MockMouse Mouse => mouse;

    IKeyboard IInputContext.Keyboard => keyboard;
    IMouse IInputContext.Mouse => mouse;
    IGamepad IInputContext.Gamepad => gamepads[0];
    ImmutableArray<IKeyboard> IInputContext.Keyboards => [keyboard];
    ImmutableArray<IMouse> IInputContext.Mice => [mouse];
    ImmutableArray<IGamepad> IInputContext.Gamepads => [.. gamepads];
    int IInputContext.ConnectedGamepadCount => gamepads.Count(g => g.IsConnected);

    public void Update() { }
    public void Dispose() { }

    public event Action<IKeyboard, KeyEventArgs>? OnKeyDown;
    public event Action<IKeyboard, KeyEventArgs>? OnKeyUp;
    public event Action<IKeyboard, char>? OnTextInput;
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonDown;
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonUp;
    public event Action<IMouse, MouseMoveEventArgs>? OnMouseMove;
    public event Action<IMouse, MouseScrollEventArgs>? OnMouseScroll;
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonDown;
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonUp;
    public event Action<IGamepad>? OnGamepadConnected;
    public event Action<IGamepad>? OnGamepadDisconnected;

    public void ConnectGamepad(int index)
    {
        if (index >= 0 && index < gamepads.Length)
        {
            gamepads[index].IsConnected = true;
        }
    }

    public void DisconnectGamepad(int index)
    {
        if (index >= 0 && index < gamepads.Length)
        {
            gamepads[index].IsConnected = false;
        }
    }

    public MockGamepad GetGamepad(int index)
    {
        return gamepads[index];
    }
}

/// <summary>
/// Mock implementation of <see cref="IKeyboard"/> for testing.
/// </summary>
internal sealed class MockKeyboard : IKeyboard
{
    private readonly HashSet<Key> pressedKeys = new HashSet<Key>();
    private KeyModifiers modifiers = KeyModifiers.None;

    public event Action<KeyEventArgs>? OnKeyDown;
    public event Action<KeyEventArgs>? OnKeyUp;
    public event Action<char>? OnTextInput;

    public KeyModifiers Modifiers => modifiers;

    public void PressKey(Key key)
    {
        pressedKeys.Add(key);
    }

    public void ReleaseKey(Key key)
    {
        pressedKeys.Remove(key);
    }

    public void SetModifiers(KeyModifiers mods)
    {
        modifiers = mods;
    }

    public KeyboardState GetState()
    {
        return new KeyboardState(pressedKeys.ToImmutableHashSet(), modifiers);
    }

    public bool IsKeyDown(Key key)
    {
        return pressedKeys.Contains(key);
    }

    public bool IsKeyUp(Key key)
    {
        return !pressedKeys.Contains(key);
    }

    public void Clear()
    {
        pressedKeys.Clear();
        modifiers = KeyModifiers.None;
    }
}

/// <summary>
/// Mock implementation of <see cref="IMouse"/> for testing.
/// </summary>
internal sealed class MockMouse : IMouse
{
    private Vector2 position = Vector2.Zero;
    private MouseButtons pressedButtons = MouseButtons.None;
    private Vector2 scrollDelta = Vector2.Zero;

    public event Action<MouseButtonEventArgs>? OnButtonDown;
    public event Action<MouseButtonEventArgs>? OnButtonUp;
    public event Action<MouseMoveEventArgs>? OnMove;
    public event Action<MouseScrollEventArgs>? OnScroll;
    public event Action? OnEnter;
    public event Action? OnLeave;

    public Vector2 Position => position;
    public bool IsCursorVisible { get; set; } = true;
    public bool IsCursorCaptured { get; set; }

    public void SetPosition(Vector2 pos)
    {
        position = pos;
    }

    public void PressButton(MouseButton button)
    {
        pressedButtons |= button switch
        {
            MouseButton.Left => MouseButtons.Left,
            MouseButton.Right => MouseButtons.Right,
            MouseButton.Middle => MouseButtons.Middle,
            MouseButton.Button4 => MouseButtons.Button4,
            MouseButton.Button5 => MouseButtons.Button5,
            _ => MouseButtons.None
        };
    }

    public void ReleaseButton(MouseButton button)
    {
        pressedButtons &= ~(button switch
        {
            MouseButton.Left => MouseButtons.Left,
            MouseButton.Right => MouseButtons.Right,
            MouseButton.Middle => MouseButtons.Middle,
            MouseButton.Button4 => MouseButtons.Button4,
            MouseButton.Button5 => MouseButtons.Button5,
            _ => MouseButtons.None
        });
    }

    public void SetScrollDelta(Vector2 delta)
    {
        scrollDelta = delta;
    }

    public MouseState GetState()
    {
        return new MouseState(position, pressedButtons, scrollDelta);
    }

    public bool IsButtonDown(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => (pressedButtons & MouseButtons.Left) != 0,
            MouseButton.Right => (pressedButtons & MouseButtons.Right) != 0,
            MouseButton.Middle => (pressedButtons & MouseButtons.Middle) != 0,
            MouseButton.Button4 => (pressedButtons & MouseButtons.Button4) != 0,
            MouseButton.Button5 => (pressedButtons & MouseButtons.Button5) != 0,
            _ => false
        };
    }

    public bool IsButtonUp(MouseButton button)
    {
        return !IsButtonDown(button);
    }

    public void Clear()
    {
        position = Vector2.Zero;
        pressedButtons = MouseButtons.None;
        scrollDelta = Vector2.Zero;
    }
}

/// <summary>
/// Mock implementation of <see cref="IGamepad"/> for testing.
/// </summary>
internal sealed class MockGamepad : IGamepad
{
    private readonly int index;
    private bool isConnected;
    private GamepadButtons pressedButtons = GamepadButtons.None;
    private Vector2 leftStick = Vector2.Zero;
    private Vector2 rightStick = Vector2.Zero;
    private float leftTrigger = 0f;
    private float rightTrigger = 0f;

    public MockGamepad(int index)
    {
        this.index = index;
    }

    public event Action<GamepadButtonEventArgs>? OnButtonDown;
    public event Action<GamepadButtonEventArgs>? OnButtonUp;
    public event Action<GamepadAxisEventArgs>? OnAxisChanged;
    public event Action<IGamepad>? OnConnected;
    public event Action<IGamepad>? OnDisconnected;

    public int Index => index;
    public bool IsConnected
    {
        get => isConnected;
        set => isConnected = value;
    }
    public string Name => $"MockGamepad{index}";
    public Vector2 LeftStick => leftStick;
    public Vector2 RightStick => rightStick;
    public float LeftTrigger => leftTrigger;
    public float RightTrigger => rightTrigger;

    public void SetVibration(float leftMotor, float rightMotor) { }
    public void StopVibration() { }

    public void PressButton(GamepadButton button)
    {
        pressedButtons |= button switch
        {
            GamepadButton.South => GamepadButtons.South,
            GamepadButton.East => GamepadButtons.East,
            GamepadButton.West => GamepadButtons.West,
            GamepadButton.North => GamepadButtons.North,
            GamepadButton.LeftShoulder => GamepadButtons.LeftShoulder,
            GamepadButton.RightShoulder => GamepadButtons.RightShoulder,
            GamepadButton.LeftTrigger => GamepadButtons.LeftTrigger,
            GamepadButton.RightTrigger => GamepadButtons.RightTrigger,
            GamepadButton.DPadUp => GamepadButtons.DPadUp,
            GamepadButton.DPadDown => GamepadButtons.DPadDown,
            GamepadButton.DPadLeft => GamepadButtons.DPadLeft,
            GamepadButton.DPadRight => GamepadButtons.DPadRight,
            GamepadButton.LeftStick => GamepadButtons.LeftStick,
            GamepadButton.RightStick => GamepadButtons.RightStick,
            GamepadButton.Start => GamepadButtons.Start,
            GamepadButton.Back => GamepadButtons.Back,
            GamepadButton.Guide => GamepadButtons.Guide,
            _ => GamepadButtons.None
        };
    }

    public void ReleaseButton(GamepadButton button)
    {
        pressedButtons &= ~(button switch
        {
            GamepadButton.South => GamepadButtons.South,
            GamepadButton.East => GamepadButtons.East,
            GamepadButton.West => GamepadButtons.West,
            GamepadButton.North => GamepadButtons.North,
            GamepadButton.LeftShoulder => GamepadButtons.LeftShoulder,
            GamepadButton.RightShoulder => GamepadButtons.RightShoulder,
            GamepadButton.LeftTrigger => GamepadButtons.LeftTrigger,
            GamepadButton.RightTrigger => GamepadButtons.RightTrigger,
            GamepadButton.DPadUp => GamepadButtons.DPadUp,
            GamepadButton.DPadDown => GamepadButtons.DPadDown,
            GamepadButton.DPadLeft => GamepadButtons.DPadLeft,
            GamepadButton.DPadRight => GamepadButtons.DPadRight,
            GamepadButton.LeftStick => GamepadButtons.LeftStick,
            GamepadButton.RightStick => GamepadButtons.RightStick,
            GamepadButton.Start => GamepadButtons.Start,
            GamepadButton.Back => GamepadButtons.Back,
            GamepadButton.Guide => GamepadButtons.Guide,
            _ => GamepadButtons.None
        });
    }

    public void SetAxis(GamepadAxis axis, float value)
    {
        switch (axis)
        {
            case GamepadAxis.LeftStickX:
                leftStick = new Vector2(value, leftStick.Y);
                break;
            case GamepadAxis.LeftStickY:
                leftStick = new Vector2(leftStick.X, value);
                break;
            case GamepadAxis.RightStickX:
                rightStick = new Vector2(value, rightStick.Y);
                break;
            case GamepadAxis.RightStickY:
                rightStick = new Vector2(rightStick.X, value);
                break;
            case GamepadAxis.LeftTrigger:
                leftTrigger = value;
                break;
            case GamepadAxis.RightTrigger:
                rightTrigger = value;
                break;
        }
    }

    public GamepadState GetState()
    {
        return new GamepadState(index, isConnected, pressedButtons, leftStick, rightStick, leftTrigger, rightTrigger);
    }

    public bool IsButtonDown(GamepadButton button)
    {
        return button switch
        {
            GamepadButton.South => (pressedButtons & GamepadButtons.South) != 0,
            GamepadButton.East => (pressedButtons & GamepadButtons.East) != 0,
            GamepadButton.West => (pressedButtons & GamepadButtons.West) != 0,
            GamepadButton.North => (pressedButtons & GamepadButtons.North) != 0,
            GamepadButton.LeftShoulder => (pressedButtons & GamepadButtons.LeftShoulder) != 0,
            GamepadButton.RightShoulder => (pressedButtons & GamepadButtons.RightShoulder) != 0,
            GamepadButton.LeftTrigger => (pressedButtons & GamepadButtons.LeftTrigger) != 0,
            GamepadButton.RightTrigger => (pressedButtons & GamepadButtons.RightTrigger) != 0,
            GamepadButton.DPadUp => (pressedButtons & GamepadButtons.DPadUp) != 0,
            GamepadButton.DPadDown => (pressedButtons & GamepadButtons.DPadDown) != 0,
            GamepadButton.DPadLeft => (pressedButtons & GamepadButtons.DPadLeft) != 0,
            GamepadButton.DPadRight => (pressedButtons & GamepadButtons.DPadRight) != 0,
            GamepadButton.LeftStick => (pressedButtons & GamepadButtons.LeftStick) != 0,
            GamepadButton.RightStick => (pressedButtons & GamepadButtons.RightStick) != 0,
            GamepadButton.Start => (pressedButtons & GamepadButtons.Start) != 0,
            GamepadButton.Back => (pressedButtons & GamepadButtons.Back) != 0,
            GamepadButton.Guide => (pressedButtons & GamepadButtons.Guide) != 0,
            _ => false
        };
    }

    public bool IsButtonUp(GamepadButton button)
    {
        return !IsButtonDown(button);
    }

    public float GetAxis(GamepadAxis axis)
    {
        return axis switch
        {
            GamepadAxis.LeftStickX => leftStick.X,
            GamepadAxis.LeftStickY => leftStick.Y,
            GamepadAxis.RightStickX => rightStick.X,
            GamepadAxis.RightStickY => rightStick.Y,
            GamepadAxis.LeftTrigger => leftTrigger,
            GamepadAxis.RightTrigger => rightTrigger,
            _ => 0f
        };
    }

    public void Clear()
    {
        pressedButtons = GamepadButtons.None;
        leftStick = Vector2.Zero;
        rightStick = Vector2.Zero;
        leftTrigger = 0f;
        rightTrigger = 0f;
    }
}
