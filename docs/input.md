# Input

The KeenEyes input system provides backend-agnostic input handling with support for keyboard, mouse, and gamepad devices.

## Architecture Overview

The input system follows the same abstraction pattern as graphics:

| Package | Purpose |
|---------|---------|
| `KeenEyes.Input.Abstractions` | Backend-agnostic interfaces and types |
| `KeenEyes.Input` | Backend-agnostic systems (InputCaptureSystem) |
| `KeenEyes.Input.Silk` | Silk.NET implementation |

This separation allows:
- Swapping input backends without changing game code
- Writing backend-agnostic input systems
- Testing input logic without hardware dependencies

## Quick Start

```csharp
using KeenEyes;
using KeenEyes.Input;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Graphics.Silk;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;

var windowConfig = new WindowConfig { Title = "My Game" };
var graphicsConfig = new SilkGraphicsConfig();
var inputConfig = new SilkInputConfig { GamepadDeadzone = 0.15f };

using var world = new World();

// Install plugins (window first, then graphics/input in any order)
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));

// Add systems
world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate);
world.AddSystem<RenderSystem>(SystemPhase.Render);

world.CreateRunner()
    .OnReady(() =>
    {
        var input = world.GetExtension<IInputContext>();

        // Event-based input
        input.OnKeyDown += (kb, args) =>
        {
            if (args.Key == Key.Escape)
                Console.WriteLine("Escape pressed!");
        };
    })
    .Run();
```

## IInputContext

The main entry point for all input handling. Access it via `world.GetExtension<IInputContext>()`.

### Device Access

```csharp
var input = world.GetExtension<IInputContext>();

// Primary devices (most common use case)
IKeyboard keyboard = input.Keyboard;
IMouse mouse = input.Mouse;
IGamepad gamepad = input.Gamepad;

// All connected devices (multi-device support)
ImmutableArray<IKeyboard> keyboards = input.Keyboards;
ImmutableArray<IMouse> mice = input.Mice;
ImmutableArray<IGamepad> gamepads = input.Gamepads;

// Gamepad connection count
int connectedGamepads = input.ConnectedGamepadCount;
```

### Global Events

Events that fire for any device:

```csharp
// Keyboard
input.OnKeyDown += (keyboard, args) => { };
input.OnKeyUp += (keyboard, args) => { };
input.OnTextInput += (keyboard, character) => { };

// Mouse
input.OnMouseButtonDown += (mouse, args) => { };
input.OnMouseButtonUp += (mouse, args) => { };
input.OnMouseMove += (mouse, args) => { };
input.OnMouseScroll += (mouse, args) => { };

// Gamepad
input.OnGamepadButtonDown += (gamepad, args) => { };
input.OnGamepadButtonUp += (gamepad, args) => { };
input.OnGamepadConnected += (gamepad) => { };
input.OnGamepadDisconnected += (gamepad) => { };
```

## Keyboard

### Polling

Check key state each frame (good for continuous input like movement):

```csharp
var keyboard = input.Keyboard;

// Check specific key
if (keyboard.IsKeyDown(Key.W))
    MoveForward(deltaTime);

if (keyboard.IsKeyUp(Key.Space))
    StopJumping();

// Check modifiers
KeyModifiers mods = keyboard.Modifiers;
if ((mods & KeyModifiers.Shift) != 0)
    Sprint();

// Get full state snapshot
KeyboardState state = keyboard.GetState();
foreach (var key in state.PressedKeys)
    Console.WriteLine($"{key} is pressed");
```

### Events

Respond to discrete key presses (good for actions like jumping, menu selection):

```csharp
keyboard.OnKeyDown += (args) =>
{
    Console.WriteLine($"Key pressed: {args.Key}");
    Console.WriteLine($"Is repeat: {args.IsRepeat}");
    Console.WriteLine($"Shift held: {args.IsShiftDown}");
};

keyboard.OnKeyUp += (args) =>
{
    Console.WriteLine($"Key released: {args.Key}");
};

// Text input (for text fields, chat)
keyboard.OnTextInput += (character) =>
{
    textBuffer.Append(character);
};
```

### Key Modifiers

```csharp
// Via keyboard
if (keyboard.Modifiers.HasFlag(KeyModifiers.Control))
    HandleCtrl();

// Via event args
keyboard.OnKeyDown += (args) =>
{
    if (args.IsControlDown && args.Key == Key.S)
        SaveGame();
};
```

## Mouse

### Polling

```csharp
var mouse = input.Mouse;

// Position
Vector2 position = mouse.Position;
float x = position.X;
float y = position.Y;

// Button state
if (mouse.IsButtonDown(MouseButton.Left))
    Fire();

if (mouse.IsButtonUp(MouseButton.Right))
    StopAiming();

// Full state snapshot
MouseState state = mouse.GetState();
Vector2 scrollDelta = state.ScrollDelta;
```

### Events

```csharp
mouse.OnButtonDown += (args) =>
{
    Console.WriteLine($"Button: {args.Button} at {args.Position}");
};

mouse.OnMove += (args) =>
{
    Console.WriteLine($"Position: {args.Position}, Delta: {args.Delta}");
    RotateCamera(args.DeltaX, args.DeltaY);
};

mouse.OnScroll += (args) =>
{
    ZoomCamera(args.DeltaY);
};
```

### Cursor Control

```csharp
// Visibility
mouse.IsCursorVisible = false;  // Hide cursor

// Capture (lock to window, raw input)
mouse.IsCursorCaptured = true;  // For FPS-style camera

// Position
mouse.SetPosition(new Vector2(400, 300));  // Center cursor
```

## Gamepad

### Connection Status

```csharp
var gamepad = input.Gamepad;

if (!gamepad.IsConnected)
{
    Console.WriteLine("No gamepad connected");
    return;
}

Console.WriteLine($"Gamepad: {gamepad.Name} (index {gamepad.Index})");
```

### Polling

```csharp
// Buttons
if (gamepad.IsButtonDown(GamepadButton.South))  // A on Xbox
    Jump();

if (gamepad.IsButtonDown(GamepadButton.RightShoulder))
    Aim();

// Analog sticks (with deadzone applied)
Vector2 leftStick = gamepad.LeftStick;
Vector2 rightStick = gamepad.RightStick;

MovePlayer(leftStick.X, leftStick.Y);
RotateCamera(rightStick.X, rightStick.Y);

// Triggers (0.0 to 1.0)
float leftTrigger = gamepad.LeftTrigger;
float rightTrigger = gamepad.RightTrigger;

Brake(leftTrigger);
Accelerate(rightTrigger);

// Generic axis access
float axis = gamepad.GetAxis(GamepadAxis.LeftStickX);
```

### Events

```csharp
gamepad.OnButtonDown += (args) =>
{
    if (args.Button == GamepadButton.Start)
        PauseGame();
};

gamepad.OnAxisChanged += (args) =>
{
    Console.WriteLine($"Axis {args.Axis}: {args.Value} (delta: {args.Delta})");
};

gamepad.OnConnected += () => Console.WriteLine("Gamepad connected!");
gamepad.OnDisconnected += () => Console.WriteLine("Gamepad disconnected!");
```

### Vibration

```csharp
// Set vibration (0.0 to 1.0 for each motor)
gamepad.SetVibration(leftMotor: 0.5f, rightMotor: 0.8f);

// Stop vibration
gamepad.StopVibration();
```

## Configuration

```csharp
var config = new SilkInputConfig
{
    EnableGamepads = true,       // Enable gamepad support
    MaxGamepads = 4,             // Maximum gamepad slots
    GamepadDeadzone = 0.15f,     // Analog stick deadzone
    CaptureMouseOnClick = false  // Auto-capture mouse on click
};

world.InstallPlugin(new SilkInputPlugin(config));
```

## Input in Systems

For game logic, use polling-based input in systems:

```csharp
public class PlayerMovementSystem : ISystem
{
    private IWorld? world;
    private IInputContext? input;

    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    public void Update(float deltaTime)
    {
        // Lazy initialization
        input ??= world?.TryGetExtension<IInputContext>(out var ctx) == true ? ctx : null;
        if (input is null) return;

        var keyboard = input.Keyboard;
        var gamepad = input.Gamepad;

        // Calculate movement from keyboard
        var moveDir = Vector2.Zero;
        if (keyboard.IsKeyDown(Key.W)) moveDir.Y -= 1;
        if (keyboard.IsKeyDown(Key.S)) moveDir.Y += 1;
        if (keyboard.IsKeyDown(Key.A)) moveDir.X -= 1;
        if (keyboard.IsKeyDown(Key.D)) moveDir.X += 1;

        // Or from gamepad
        if (gamepad.IsConnected)
            moveDir = gamepad.LeftStick;

        // Apply to player entities
        foreach (var entity in world!.Query<Transform3D, PlayerTag>())
        {
            ref var transform = ref world.Get<Transform3D>(entity);
            transform.Position += new Vector3(moveDir.X, 0, moveDir.Y) * deltaTime * 5f;
        }
    }

    public void Dispose() { }
}
```

## Polling vs Events

| Use Case | Pattern | Example |
|----------|---------|---------|
| Continuous input | Polling | WASD movement, camera rotation |
| Discrete actions | Events | Jump, attack, menu selection |
| Text input | Events | Chat messages, text fields |
| Connection status | Events | Gamepad connect/disconnect |

**Rule of thumb:** If you check it every frame, use polling. If it's a one-time action, use events.

## Event Handler Patterns

### Registering Global Handlers

```csharp
// In OnReady callback
world.CreateRunner()
    .OnReady(() =>
    {
        var input = world.GetExtension<IInputContext>();

        // Global key handler
        input.OnKeyDown += (kb, args) =>
        {
            if (args.Key == Key.F1) ShowHelp();
            if (args.Key == Key.F11) ToggleFullscreen();
        };

        // Global mouse handler
        input.OnMouseButtonDown += (mouse, args) =>
        {
            if (args.Button == MouseButton.Middle)
                StartPanning();
        };
    })
    .Run();
```

### Device-Specific Handlers

```csharp
// Per-keyboard handler (multi-keyboard setups)
foreach (var keyboard in input.Keyboards)
{
    keyboard.OnKeyDown += (args) =>
    {
        Console.WriteLine($"Keyboard {keyboard.Index}: {args.Key}");
    };
}

// Per-gamepad handler (local multiplayer)
foreach (var gamepad in input.Gamepads)
{
    gamepad.OnButtonDown += (args) =>
    {
        HandlePlayerInput(gamepad.Index, args.Button);
    };
}
```

### Cleanup Pattern

When using event handlers, store references for proper cleanup:

```csharp
private Action<IKeyboard, KeyEventArgs>? keyHandler;

public void Initialize()
{
    var input = world.GetExtension<IInputContext>();
    keyHandler = (kb, args) => HandleKey(args);
    input.OnKeyDown += keyHandler;
}

public void Cleanup()
{
    var input = world.GetExtension<IInputContext>();
    if (keyHandler != null)
        input.OnKeyDown -= keyHandler;
}
```

## Action Mapping

Action mapping provides an abstraction layer between game logic and physical inputs. Instead of checking specific keys or buttons, you define named actions that can be bound to multiple input sources.

### Why Use Action Mapping?

- **Multiple bindings per action** - Same action works with keyboard AND gamepad
- **Rebindable controls** - Let players customize their bindings at runtime
- **Context switching** - Easily enable/disable groups of actions (gameplay vs menu)
- **Cleaner code** - Check `jump.IsPressed(input)` instead of checking each key/button

### Basic Usage

```csharp
// Define an action with multiple bindings
var jump = new InputAction("Jump",
    InputBinding.FromKey(Key.Space),
    InputBinding.FromGamepadButton(GamepadButton.South));

// In your update loop
if (jump.IsPressed(input))
    player.Jump();
```

### InputBinding

An `InputBinding` represents a single input source:

```csharp
// Keyboard key
var spaceBinding = InputBinding.FromKey(Key.Space);

// Mouse button
var clickBinding = InputBinding.FromMouseButton(MouseButton.Left);

// Gamepad button
var buttonBinding = InputBinding.FromGamepadButton(GamepadButton.South);

// Gamepad axis (for axis-as-button, e.g., trigger as "accelerate")
var triggerBinding = InputBinding.FromGamepadAxis(GamepadAxis.RightTrigger, threshold: 0.5f);

// Gamepad axis (negative direction)
var leftStickLeft = InputBinding.FromGamepadAxis(GamepadAxis.LeftStickX, threshold: 0.3f, isPositive: false);
```

### InputAction

An `InputAction` groups bindings together and provides state queries:

```csharp
// Create with multiple bindings
var fire = new InputAction("Fire",
    InputBinding.FromMouseButton(MouseButton.Left),
    InputBinding.FromGamepadButton(GamepadButton.RightShoulder));

// State queries
bool pressed = fire.IsPressed(input);   // Any binding active
bool released = fire.IsReleased(input); // No binding active
float value = fire.GetValue(input);     // Analog value (1.0 if digital)

// Disable/enable
fire.Enabled = false;

// Rebind at runtime
fire.ClearBindings();
fire.AddBinding(InputBinding.FromKey(Key.F));
```

### InputActionMap

Group related actions into maps for context switching:

```csharp
// Create action maps for different contexts
var gameplayMap = new InputActionMap("Gameplay");
gameplayMap.AddAction("Jump", InputBinding.FromKey(Key.Space));
gameplayMap.AddAction("Fire", InputBinding.FromMouseButton(MouseButton.Left));
gameplayMap.AddAction("Interact", InputBinding.FromKey(Key.E));

var menuMap = new InputActionMap("Menu");
menuMap.AddAction("Select", InputBinding.FromKey(Key.Enter));
menuMap.AddAction("Back", InputBinding.FromKey(Key.Escape));

// Access actions by name
var jump = gameplayMap.GetAction("Jump");

// Switch contexts
gameplayMap.Enabled = false;  // Disables all gameplay actions
menuMap.Enabled = true;       // Enables all menu actions
```

### ActionMapProvider

For managing multiple action maps:

```csharp
var provider = new ActionMapProvider();

// Register maps
provider.AddActionMap(gameplayMap);
provider.AddActionMap(menuMap);

// Switch active context
provider.SetActiveMap("Menu");  // Enables Menu, disables others

// Access
var activeMap = provider.ActiveMap;
var menuActions = provider.GetActionMap("Menu");
```

### Multiplayer (Per-Player Input)

Bind actions to specific gamepads for local multiplayer:

```csharp
// Player 1's actions bound to gamepad index 0
var player1Jump = new InputAction("Jump",
    InputBinding.FromGamepadButton(GamepadButton.South))
{
    GamepadIndex = 0  // Only check gamepad 0
};

// Player 2's actions bound to gamepad index 1
var player2Jump = new InputAction("Jump",
    InputBinding.FromGamepadButton(GamepadButton.South))
{
    GamepadIndex = 1  // Only check gamepad 1
};
```

### In ECS Systems

```csharp
public class PlayerMovementSystem : SystemBase
{
    // Define actions (can also be passed in via constructor)
    private static readonly InputAction jumpAction = new("Jump",
        InputBinding.FromKey(Key.Space),
        InputBinding.FromGamepadButton(GamepadButton.South));

    private IInputContext? input;

    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null) return;

        // Clean, readable input checks
        if (jumpAction.IsPressed(input))
        {
            // Apply jump to player entities
        }
    }
}
```

## Button Naming

Gamepad buttons use standardized names based on position:

| KeenEyes | Xbox | PlayStation | Nintendo |
|----------|------|-------------|----------|
| South | A | Cross | B |
| East | B | Circle | A |
| West | X | Square | Y |
| North | Y | Triangle | X |
| LeftShoulder | LB | L1 | L |
| RightShoulder | RB | R1 | R |
| LeftTrigger | LT | L2 | ZL |
| RightTrigger | RT | R2 | ZR |

## Plugin Architecture

All platform plugins share the window through `ISilkWindowProvider` from `KeenEyes.Platform.Silk`:

```
SilkWindowPlugin (must be installed first)
├── Creates native window
├── Provides ISilkWindowProvider (shared window access)
├── Provides ILoopProvider (main loop)
└── Creates input context

SilkGraphicsPlugin               SilkInputPlugin
├── Uses ISilkWindowProvider     ├── Uses ISilkWindowProvider
├── Creates OpenGL context       └── Wraps input context
└── Provides IGraphicsContext    └── Provides IInputContext
```

**Required installation order:**

```csharp
// Window plugin MUST be installed first
var windowConfig = new WindowConfig
{
    Title = "My Game",
    Width = 1920,
    Height = 1080,
    VSync = true
};
world.InstallPlugin(new SilkWindowPlugin(windowConfig));

// Then install graphics and/or input (any order after window)
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));
```

This architecture means:
- `SilkWindowPlugin` owns the window and main loop
- Both `SilkGraphicsPlugin` and `SilkInputPlugin` require `SilkWindowPlugin`
- Graphics and input can be installed in any order after window
- All plugins share the same native window and input context

## Input + UI Integration

When using the KeenEyes UI system, input handling requires coordination between raw input and UI focus.

### UI Focus and Input Context

```csharp
var input = world.GetExtension<IInputContext>();
var ui = world.TryGetExtension<UIContext>(out var ctx) ? ctx : null;

// Check if UI has focus before processing gameplay input
if (ui?.HasFocus != true)
{
    // Process gameplay input normally
    ProcessGameplayInput(input);
}
else
{
    // UI has focus - skip gameplay input
    // UI systems handle input automatically
}
```

### Input Blocking Patterns

```csharp
// Option 1: Let UI consume input first
public class GameInputSystem : ISystem
{
    private IWorld? world;
    private IInputContext? input;
    private UIContext? ui;

    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world) => this.world = world;

    public void Update(float dt)
    {
        input ??= world?.TryGetExtension<IInputContext>(out var i) == true ? i : null;
        ui ??= world?.TryGetExtension<UIContext>(out var u) == true ? u : null;

        // Skip if UI is capturing input
        if (ui?.HasFocus == true) return;

        // Process gameplay input
        var keyboard = input?.Keyboard;
        // ...
    }

    public void Dispose() { }
}
```

### Context-Sensitive Input

```csharp
public class ContextualInputSystem : ISystem
{
    public void Update(float dt)
    {
        var input = world.GetExtension<IInputContext>();
        var ui = world.TryGetExtension<UIContext>(out var ctx) ? ctx : null;

        // Escape always available for pause menu
        if (input.Keyboard.IsKeyDown(Key.Escape))
        {
            TogglePause();
            return;
        }

        // UI has priority for focused elements
        if (ui?.HasFocus == true)
        {
            // Let UI handle input
            return;
        }

        // Process gameplay input
        HandleMovement(input);
        HandleActions(input);
    }
}
```

### Mouse Position to UI Coordinates

Mouse position from the input system is in screen coordinates, which matches the UI coordinate system:

```csharp
var mousePos = input.Mouse.Position;  // Screen coordinates (0,0 = top-left)

// UI elements use the same coordinate system
// ComputedBounds are in screen space
foreach (var entity in world.Query<UIRect, UIInteractable>())
{
    ref readonly var rect = ref world.Get<UIRect>(entity);
    if (rect.ComputedBounds.Contains(mousePos))
    {
        // Mouse is over this element
    }
}
```

## Advanced Multimodal Input

### Unified Input Abstraction

When supporting keyboard, mouse, AND gamepad simultaneously:

```csharp
public class UnifiedInputSystem : ISystem
{
    public void Update(float dt)
    {
        var input = World.GetExtension<IInputContext>();
        var kb = input.Keyboard;
        var mouse = input.Mouse;
        var gamepad = input.Gamepad;

        // Movement: keyboard OR gamepad
        var moveDir = Vector2.Zero;

        // Keyboard WASD
        if (kb.IsKeyDown(Key.W)) moveDir.Y -= 1;
        if (kb.IsKeyDown(Key.S)) moveDir.Y += 1;
        if (kb.IsKeyDown(Key.A)) moveDir.X -= 1;
        if (kb.IsKeyDown(Key.D)) moveDir.X += 1;

        // Gamepad overrides if connected and active
        if (gamepad.IsConnected)
        {
            var stick = gamepad.LeftStick;
            if (stick.LengthSquared() > 0.01f)
                moveDir = stick;
        }

        // Normalize if needed
        if (moveDir.LengthSquared() > 1)
            moveDir = Vector2.Normalize(moveDir);

        // Aim: mouse OR right stick
        var aimDir = Vector2.Zero;

        if (gamepad.IsConnected &&
            gamepad.RightStick.LengthSquared() > 0.01f)
        {
            aimDir = Vector2.Normalize(gamepad.RightStick);
        }
        else
        {
            // Aim toward mouse from player position
            var playerPos = GetPlayerScreenPosition();
            var toMouse = mouse.Position - playerPos;
            if (toMouse.LengthSquared() > 1)
                aimDir = Vector2.Normalize(toMouse);
        }

        // Apply to player
        ApplyMovement(moveDir, aimDir);
    }
}
```

### Input Device Detection

Track which input device was used most recently:

```csharp
public enum ActiveInputDevice { Keyboard, Mouse, Gamepad }

public class InputDeviceTracker : ISystem
{
    private ActiveInputDevice lastDevice = ActiveInputDevice.Keyboard;
    private Vector2 lastMousePos;

    public ActiveInputDevice ActiveDevice => lastDevice;

    public void Update(float dt)
    {
        var input = World.GetExtension<IInputContext>();

        // Check for keyboard activity
        if (input.Keyboard.GetState().PressedKeys.Length > 0)
            lastDevice = ActiveInputDevice.Keyboard;

        // Check for mouse movement or clicks
        if (input.Mouse.Position != lastMousePos ||
            input.Mouse.IsButtonDown(MouseButton.Left))
        {
            lastDevice = ActiveInputDevice.Mouse;
            lastMousePos = input.Mouse.Position;
        }

        // Check for gamepad activity
        if (input.Gamepad.IsConnected)
        {
            var gp = input.Gamepad;
            if (gp.LeftStick.LengthSquared() > 0.1f ||
                gp.RightStick.LengthSquared() > 0.1f ||
                gp.IsButtonDown(GamepadButton.South))
            {
                lastDevice = ActiveInputDevice.Gamepad;
            }
        }
    }
}
```

### Cursor Visibility Based on Device

Show/hide cursor based on active input device:

```csharp
// In your input system
if (deviceTracker.ActiveDevice == ActiveInputDevice.Gamepad)
{
    input.Mouse.IsCursorVisible = false;
}
else
{
    input.Mouse.IsCursorVisible = true;
}
```

### UI Prompts Based on Device

Display controller-appropriate button prompts:

```csharp
public string GetPrompt(string action)
{
    return deviceTracker.ActiveDevice switch
    {
        ActiveInputDevice.Gamepad => action switch
        {
            "Jump" => "[A]",
            "Attack" => "[X]",
            "Interact" => "[Y]",
            _ => "[?]"
        },
        _ => action switch
        {
            "Jump" => "[Space]",
            "Attack" => "[Left Click]",
            "Interact" => "[E]",
            _ => "[?]"
        }
    };
}
```

## Dependencies

- **KeenEyes.Input.Abstractions** - Backend-agnostic interfaces
- **KeenEyes.Input** - Backend-agnostic systems
- **KeenEyes.Input.Silk** - Silk.NET implementation
- **KeenEyes.Platform.Silk** - Shared window provider
