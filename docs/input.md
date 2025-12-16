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

## Dependencies

- **KeenEyes.Input.Abstractions** - Backend-agnostic interfaces
- **KeenEyes.Input** - Backend-agnostic systems
- **KeenEyes.Input.Silk** - Silk.NET implementation
- **KeenEyes.Platform.Silk** - Shared window provider
