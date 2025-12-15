# Input Handling

## Problem

You want to process player input cleanly, supporting keyboard, mouse, gamepad, and action mapping.

## Solution

### Input State Singleton

```csharp
public sealed class InputState
{
    // Keyboard
    public HashSet<Key> KeysDown { get; } = new();
    public HashSet<Key> KeysPressed { get; } = new();  // Just this frame
    public HashSet<Key> KeysReleased { get; } = new(); // Just this frame

    // Mouse
    public Vector2 MousePosition { get; set; }
    public Vector2 MouseDelta { get; set; }
    public HashSet<MouseButton> MouseDown { get; } = new();
    public HashSet<MouseButton> MousePressed { get; } = new();
    public HashSet<MouseButton> MouseReleased { get; } = new();
    public float ScrollDelta { get; set; }

    // Gamepad
    public Vector2 LeftStick { get; set; }
    public Vector2 RightStick { get; set; }
    public float LeftTrigger { get; set; }
    public float RightTrigger { get; set; }
    public HashSet<GamepadButton> ButtonsDown { get; } = new();
    public HashSet<GamepadButton> ButtonsPressed { get; } = new();
    public HashSet<GamepadButton> ButtonsReleased { get; } = new();

    public void Clear()
    {
        KeysPressed.Clear();
        KeysReleased.Clear();
        MousePressed.Clear();
        MouseReleased.Clear();
        MouseDelta = Vector2.Zero;
        ScrollDelta = 0;
        ButtonsPressed.Clear();
        ButtonsReleased.Clear();
    }
}
```

### Input Collection System

```csharp
public class InputCollectionSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.EarlyUpdate;
    public override int Order => -1000;  // Run first

    private Vector2 lastMousePosition;

    public override void Update(float deltaTime)
    {
        var input = World.GetSingleton<InputState>();
        input.Clear();

        // Keyboard events (from window/platform layer)
        foreach (var key in PlatformInput.GetPressedKeys())
        {
            if (!input.KeysDown.Contains(key))
            {
                input.KeysPressed.Add(key);
            }
            input.KeysDown.Add(key);
        }

        foreach (var key in PlatformInput.GetReleasedKeys())
        {
            input.KeysDown.Remove(key);
            input.KeysReleased.Add(key);
        }

        // Mouse
        input.MousePosition = PlatformInput.GetMousePosition();
        input.MouseDelta = input.MousePosition - lastMousePosition;
        lastMousePosition = input.MousePosition;
        input.ScrollDelta = PlatformInput.GetScrollDelta();

        // Similar for mouse buttons and gamepad...
    }
}
```

### Action Mapping

```csharp
public enum GameAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Jump,
    Attack,
    Interact,
    Pause,
    Inventory
}

public sealed class InputMapping
{
    private readonly Dictionary<GameAction, List<InputBinding>> bindings = new();

    public void Bind(GameAction action, Key key)
    {
        GetOrCreateBindings(action).Add(new KeyBinding(key));
    }

    public void Bind(GameAction action, MouseButton button)
    {
        GetOrCreateBindings(action).Add(new MouseBinding(button));
    }

    public void Bind(GameAction action, GamepadButton button)
    {
        GetOrCreateBindings(action).Add(new GamepadBinding(button));
    }

    public bool IsPressed(GameAction action, InputState input)
    {
        if (!bindings.TryGetValue(action, out var actionBindings))
            return false;

        return actionBindings.Any(b => b.IsPressed(input));
    }

    public bool IsDown(GameAction action, InputState input)
    {
        if (!bindings.TryGetValue(action, out var actionBindings))
            return false;

        return actionBindings.Any(b => b.IsDown(input));
    }

    public bool IsReleased(GameAction action, InputState input)
    {
        if (!bindings.TryGetValue(action, out var actionBindings))
            return false;

        return actionBindings.Any(b => b.IsReleased(input));
    }

    private List<InputBinding> GetOrCreateBindings(GameAction action)
    {
        if (!bindings.TryGetValue(action, out var list))
        {
            list = new List<InputBinding>();
            bindings[action] = list;
        }
        return list;
    }
}

public abstract record InputBinding
{
    public abstract bool IsPressed(InputState input);
    public abstract bool IsDown(InputState input);
    public abstract bool IsReleased(InputState input);
}

public record KeyBinding(Key Key) : InputBinding
{
    public override bool IsPressed(InputState input) => input.KeysPressed.Contains(Key);
    public override bool IsDown(InputState input) => input.KeysDown.Contains(Key);
    public override bool IsReleased(InputState input) => input.KeysReleased.Contains(Key);
}

public record MouseBinding(MouseButton Button) : InputBinding
{
    public override bool IsPressed(InputState input) => input.MousePressed.Contains(Button);
    public override bool IsDown(InputState input) => input.MouseDown.Contains(Button);
    public override bool IsReleased(InputState input) => input.MouseReleased.Contains(Button);
}
```

### Player Input Component

```csharp
[Component]
public partial struct PlayerInput : IComponent
{
    public Vector2 MoveDirection;
    public Vector2 AimDirection;
    public bool JumpPressed;
    public bool AttackPressed;
    public bool InteractPressed;
}
```

### Input Processing System

```csharp
public class PlayerInputSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.EarlyUpdate;

    public override void Update(float deltaTime)
    {
        var input = World.GetSingleton<InputState>();
        var mapping = World.GetSingleton<InputMapping>();

        foreach (var entity in World.Query<PlayerInput>().With<Player>())
        {
            ref var playerInput = ref World.Get<PlayerInput>(entity);

            // Movement (supports both keyboard and gamepad)
            playerInput.MoveDirection = Vector2.Zero;

            if (mapping.IsDown(GameAction.MoveUp, input))
                playerInput.MoveDirection.Y += 1;
            if (mapping.IsDown(GameAction.MoveDown, input))
                playerInput.MoveDirection.Y -= 1;
            if (mapping.IsDown(GameAction.MoveLeft, input))
                playerInput.MoveDirection.X -= 1;
            if (mapping.IsDown(GameAction.MoveRight, input))
                playerInput.MoveDirection.X += 1;

            // Normalize diagonal movement
            if (playerInput.MoveDirection.LengthSquared() > 1)
                playerInput.MoveDirection = Vector2.Normalize(playerInput.MoveDirection);

            // Or use gamepad stick directly if available
            if (input.LeftStick.LengthSquared() > 0.1f)
                playerInput.MoveDirection = input.LeftStick;

            // Aim direction (mouse or right stick)
            if (input.RightStick.LengthSquared() > 0.1f)
            {
                playerInput.AimDirection = Vector2.Normalize(input.RightStick);
            }
            else
            {
                // Aim toward mouse
                ref readonly var pos = ref World.Get<Position>(entity);
                var toMouse = input.MousePosition - new Vector2(pos.X, pos.Y);
                if (toMouse.LengthSquared() > 1)
                    playerInput.AimDirection = Vector2.Normalize(toMouse);
            }

            // Actions (pressed this frame only)
            playerInput.JumpPressed = mapping.IsPressed(GameAction.Jump, input);
            playerInput.AttackPressed = mapping.IsPressed(GameAction.Attack, input);
            playerInput.InteractPressed = mapping.IsPressed(GameAction.Interact, input);
        }
    }
}
```

### Default Bindings

```csharp
public static class DefaultInputBindings
{
    public static InputMapping Create()
    {
        var mapping = new InputMapping();

        // Keyboard (WASD)
        mapping.Bind(GameAction.MoveUp, Key.W);
        mapping.Bind(GameAction.MoveDown, Key.S);
        mapping.Bind(GameAction.MoveLeft, Key.A);
        mapping.Bind(GameAction.MoveRight, Key.D);

        // Keyboard (Arrows) - alternative
        mapping.Bind(GameAction.MoveUp, Key.Up);
        mapping.Bind(GameAction.MoveDown, Key.Down);
        mapping.Bind(GameAction.MoveLeft, Key.Left);
        mapping.Bind(GameAction.MoveRight, Key.Right);

        // Actions
        mapping.Bind(GameAction.Jump, Key.Space);
        mapping.Bind(GameAction.Attack, MouseButton.Left);
        mapping.Bind(GameAction.Interact, Key.E);
        mapping.Bind(GameAction.Pause, Key.Escape);
        mapping.Bind(GameAction.Inventory, Key.I);

        // Gamepad
        mapping.Bind(GameAction.Jump, GamepadButton.A);
        mapping.Bind(GameAction.Attack, GamepadButton.RightTrigger);
        mapping.Bind(GameAction.Interact, GamepadButton.X);
        mapping.Bind(GameAction.Pause, GamepadButton.Start);

        return mapping;
    }
}
```

## Why This Works

### Separation of Raw Input and Actions

- **InputState**: Raw hardware state (keys, buttons)
- **InputMapping**: Configuration (what keys do what)
- **PlayerInput component**: Game-meaningful intentions (move, attack)

This allows:
- Rebindable controls
- Multiple input devices
- Input playback/recording
- AI can set PlayerInput directly

### Frame-Based Events

`KeysPressed` vs `KeysDown`:
- `Down`: Is the key held? (continuous)
- `Pressed`: Was the key just pressed this frame? (one-shot)
- `Released`: Was the key just released this frame? (one-shot)

Prevents:
- Repeated jumps from holding spacebar
- Missed inputs from polling timing

### Component-Based Player Input

Making player intentions a component means:
- Any system can react to input
- Input can be serialized (replays, netcode)
- AI can "fake" player input
- Multiple players supported naturally

## Variations

### Input Buffering

```csharp
[Component]
public partial struct InputBuffer : IComponent
{
    public GameAction[] BufferedActions;
    public float[] BufferTimestamps;
    public int BufferIndex;
    public const int BufferSize = 10;
    public const float BufferWindow = 0.15f;  // 150ms
}

public class InputBufferSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var input = World.GetSingleton<InputState>();
        var mapping = World.GetSingleton<InputMapping>();
        float currentTime = Time.TotalTime;

        foreach (var entity in World.Query<InputBuffer>().With<Player>())
        {
            ref var buffer = ref World.Get<InputBuffer>(entity);

            // Buffer pressed actions
            foreach (GameAction action in Enum.GetValues<GameAction>())
            {
                if (mapping.IsPressed(action, input))
                {
                    buffer.BufferedActions[buffer.BufferIndex] = action;
                    buffer.BufferTimestamps[buffer.BufferIndex] = currentTime;
                    buffer.BufferIndex = (buffer.BufferIndex + 1) % InputBuffer.BufferSize;
                }
            }
        }
    }

    public static bool ConsumeBufferedAction(ref InputBuffer buffer, GameAction action, float currentTime)
    {
        for (int i = 0; i < InputBuffer.BufferSize; i++)
        {
            if (buffer.BufferedActions[i] == action &&
                currentTime - buffer.BufferTimestamps[i] < InputBuffer.BufferWindow)
            {
                buffer.BufferedActions[i] = default;  // Consume
                return true;
            }
        }
        return false;
    }
}
```

### Combo Detection

```csharp
public record ComboDefinition(GameAction[] Sequence, float MaxInterval, Action OnCombo);

public class ComboSystem : SystemBase
{
    private readonly List<ComboDefinition> combos = new()
    {
        new([GameAction.Attack, GameAction.Attack, GameAction.Attack], 0.3f,
            () => Console.WriteLine("Triple Attack!")),
        new([GameAction.MoveDown, GameAction.MoveRight, GameAction.Attack], 0.5f,
            () => Console.WriteLine("Hadouken!"))
    };

    private readonly List<GameAction> recentActions = new();
    private readonly List<float> actionTimes = new();

    public override void Update(float deltaTime)
    {
        var input = World.GetSingleton<InputState>();
        var mapping = World.GetSingleton<InputMapping>();
        float currentTime = Time.TotalTime;

        // Record actions
        foreach (GameAction action in Enum.GetValues<GameAction>())
        {
            if (mapping.IsPressed(action, input))
            {
                recentActions.Add(action);
                actionTimes.Add(currentTime);
            }
        }

        // Check combos
        foreach (var combo in combos)
        {
            if (MatchesCombo(combo, currentTime))
            {
                combo.OnCombo();
                recentActions.Clear();
                actionTimes.Clear();
            }
        }

        // Trim old actions
        while (actionTimes.Count > 0 && currentTime - actionTimes[0] > 1f)
        {
            recentActions.RemoveAt(0);
            actionTimes.RemoveAt(0);
        }
    }

    private bool MatchesCombo(ComboDefinition combo, float currentTime)
    {
        if (recentActions.Count < combo.Sequence.Length)
            return false;

        int startIndex = recentActions.Count - combo.Sequence.Length;

        // Check time window
        if (currentTime - actionTimes[startIndex] > combo.MaxInterval * combo.Sequence.Length)
            return false;

        // Check sequence
        for (int i = 0; i < combo.Sequence.Length; i++)
        {
            if (recentActions[startIndex + i] != combo.Sequence[i])
                return false;
        }

        return true;
    }
}
```

### Context-Sensitive Input

```csharp
[Component]
public partial struct InputContext : IComponent
{
    public InputContextType Context;
}

public enum InputContextType
{
    Gameplay,
    Menu,
    Dialogue,
    Inventory
}

public class ContextualInputSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        ref var context = ref World.GetSingleton<InputContext>();
        var input = World.GetSingleton<InputState>();
        var mapping = World.GetSingleton<InputMapping>();

        switch (context.Context)
        {
            case InputContextType.Gameplay:
                ProcessGameplayInput(input, mapping);
                break;

            case InputContextType.Menu:
                ProcessMenuInput(input, mapping);
                break;

            case InputContextType.Dialogue:
                ProcessDialogueInput(input, mapping);
                break;
        }

        // Context switching
        if (mapping.IsPressed(GameAction.Pause, input))
        {
            context.Context = context.Context == InputContextType.Gameplay
                ? InputContextType.Menu
                : InputContextType.Gameplay;
        }
    }
}
```

### Touch Input

```csharp
public sealed class TouchState
{
    public List<Touch> ActiveTouches { get; } = new();
    public List<Touch> TouchesStarted { get; } = new();
    public List<Touch> TouchesEnded { get; } = new();
}

public record struct Touch(int Id, Vector2 Position, Vector2 Delta, TouchPhase Phase);

public enum TouchPhase { Began, Moved, Stationary, Ended, Cancelled }

// Virtual joystick from touch
public class VirtualJoystickSystem : SystemBase
{
    private Vector2 joystickCenter;
    private bool isActive;
    private const float JoystickRadius = 100f;

    public override void Update(float deltaTime)
    {
        var touch = World.GetSingleton<TouchState>();
        var input = World.GetSingleton<InputState>();

        foreach (var t in touch.TouchesStarted)
        {
            if (t.Position.X < Screen.Width / 2)  // Left side = joystick
            {
                joystickCenter = t.Position;
                isActive = true;
            }
        }

        if (isActive)
        {
            var activeTouch = touch.ActiveTouches.FirstOrDefault(t => t.Position.X < Screen.Width / 2);
            if (activeTouch.Id != 0)
            {
                var delta = activeTouch.Position - joystickCenter;
                var distance = delta.Length();

                if (distance > 0)
                {
                    var normalized = delta / MathF.Max(distance, JoystickRadius);
                    // Apply to input as if it were a gamepad stick
                    // (Or directly to PlayerInput component)
                }
            }
        }

        foreach (var t in touch.TouchesEnded)
        {
            if (t.Position.X < Screen.Width / 2)
            {
                isActive = false;
            }
        }
    }
}
```

## See Also

- [Windowing & Input Research](../research/windowing-input.md) - Platform input handling
- [State Machines](state-machines.md) - Input-driven state changes
- [Timers & Cooldowns](timers-cooldowns.md) - Input rate limiting
