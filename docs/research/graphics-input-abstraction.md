# Graphics & Input Abstraction Layer

This document outlines the architecture for abstracting graphics and input systems in KeenEyes, enabling multiple backend implementations while maintaining a consistent API.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State](#current-state)
3. [Goals](#goals)
4. [Graphics Abstraction](#graphics-abstraction)
5. [Input Abstraction](#input-abstraction)
6. [Project Structure](#project-structure)
7. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes currently has a working graphics implementation using Silk.NET/OpenGL, but it's tightly coupled to that specific backend. To support future backends (Vulkan, DirectX, WebGPU) and enable testing without a GPU, we need abstraction layers for both graphics and input.

**Recommendation:** Create separate abstraction projects (`KeenEyes.Graphics.Abstractions`, `KeenEyes.Input.Abstractions`) that define contracts, then refactor the existing Silk.NET implementation to implement these contracts.

---

## Current State

### Graphics

```
KeenEyes.Graphics/
├── GraphicsPlugin.cs          # IWorldPlugin implementation
├── GraphicsContext.cs         # Manages window, device, rendering
├── SilkNetWindow.cs          # Silk.NET window wrapper
├── IGraphicsWindow.cs        # Internal window abstraction
├── IGraphicsDevice.cs        # Internal device abstraction
└── Components/
    ├── Sprite.cs             # 2D sprite component
    ├── Transform2D.cs        # 2D transform component
    └── Camera2D.cs           # 2D camera component
```

**Issues:**
- Internal abstractions exist but aren't exposed for other backends
- Components are defined in the implementation project
- No way to swap backends without changing user code

### Input

- `Silk.NET.Input` is referenced but not integrated
- No input components or systems exist
- No abstraction layer

---

## Goals

1. **Backend Independence** - User code works with any graphics/input backend
2. **Testability** - Mock implementations for unit testing without GPU
3. **AOT Compatibility** - No reflection, source-generator friendly
4. **Gradual Migration** - Existing code continues to work during transition
5. **Clear Boundaries** - Abstractions define WHAT, implementations define HOW

---

## Graphics Abstraction

### Core Interfaces

```csharp
// KeenEyes.Graphics.Abstractions/IRenderer.cs
public interface IRenderer
{
    void Begin(Camera2D camera);
    void Draw(in Sprite sprite, in Transform2D transform);
    void DrawBatch(ReadOnlySpan<SpriteInstance> sprites);
    void End();
}

// KeenEyes.Graphics.Abstractions/IRenderPipeline.cs
public interface IRenderPipeline
{
    void AddPass<T>(T pass) where T : IRenderPass;
    void Execute(IRenderer renderer);
}

// KeenEyes.Graphics.Abstractions/IRenderPass.cs
public interface IRenderPass
{
    string Name { get; }
    int Order { get; }
    void Execute(IRenderContext context);
}

// KeenEyes.Graphics.Abstractions/IGraphicsBackend.cs
public interface IGraphicsBackend
{
    string Name { get; }  // "OpenGL", "Vulkan", "DirectX", etc.
    IRenderer CreateRenderer();
    ITexture LoadTexture(ReadOnlySpan<byte> data, TextureFormat format);
    IShader LoadShader(string vertexSource, string fragmentSource);
    void Present();
}
```

### Texture & Resource Abstractions

```csharp
// KeenEyes.Graphics.Abstractions/ITexture.cs
public interface ITexture : IDisposable
{
    int Width { get; }
    int Height { get; }
    TextureFormat Format { get; }
    nint Handle { get; }  // Backend-specific handle for advanced use
}

// KeenEyes.Graphics.Abstractions/IShader.cs
public interface IShader : IDisposable
{
    void SetUniform<T>(string name, T value) where T : unmanaged;
    void Bind();
    void Unbind();
}
```

### Common Components (in Abstractions)

```csharp
// These move FROM KeenEyes.Graphics TO KeenEyes.Graphics.Abstractions

[Component]
public partial struct Transform2D
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}

[Component]
public partial struct Sprite
{
    public ITexture? Texture;
    public Rectangle SourceRect;
    public Color Tint;
    public Vector2 Origin;
}

[Component]
public partial struct Camera2D
{
    public Vector2 Position;
    public float Zoom;
    public float Rotation;
    public Rectangle Viewport;
}
```

### Extension Pattern for World Access

```csharp
// KeenEyes.Graphics.Abstractions/IGraphicsContext.cs
public interface IGraphicsContext
{
    IGraphicsBackend Backend { get; }
    IRenderer Renderer { get; }
    IRenderPipeline Pipeline { get; }

    ITexture LoadTexture(string path);
    IShader LoadShader(string name);
}

// Generated extension for easy access
public static class WorldGraphicsExtensions
{
    extension(IWorld world)
    {
        public IGraphicsContext Graphics => world.GetExtension<IGraphicsContext>();
    }
}
```

---

## Input Abstraction

### Design Philosophy

Input uses a **hybrid model**:
- **Polling** for continuous state (is key held?)
- **Events** for discrete actions (key just pressed)

Both are captured each frame and exposed through components and the extension API.

### Core Interfaces

```csharp
// KeenEyes.Input.Abstractions/IInputSource.cs
public interface IInputSource
{
    void Update();  // Called each frame to capture state

    // Keyboard
    bool IsKeyDown(Key key);
    bool IsKeyPressed(Key key);   // Just this frame
    bool IsKeyReleased(Key key);  // Just this frame

    // Mouse
    Vector2 MousePosition { get; }
    Vector2 MouseDelta { get; }
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    bool IsMouseButtonReleased(MouseButton button);
    float ScrollDelta { get; }

    // Gamepad
    bool IsGamepadConnected(int index);
    float GetAxis(int gamepadIndex, GamepadAxis axis);
    bool IsButtonDown(int gamepadIndex, GamepadButton button);
}

// KeenEyes.Input.Abstractions/IInputManager.cs
public interface IInputManager
{
    IInputSource Source { get; }

    // Action mapping
    void MapAction(string action, params InputBinding[] bindings);
    bool IsActionActive(string action);
    float GetActionValue(string action);  // For analog inputs

    // Events
    event Action<Key>? OnKeyPressed;
    event Action<Key>? OnKeyReleased;
    event Action<MouseButton>? OnMouseButtonPressed;
    event Action<Vector2>? OnMouseMoved;
}
```

### Input Enums

```csharp
// KeenEyes.Input.Abstractions/Key.cs
public enum Key
{
    Unknown = 0,

    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Numbers
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    // Modifiers
    LeftShift, RightShift, LeftControl, RightControl,
    LeftAlt, RightAlt, LeftSuper, RightSuper,

    // Navigation
    Up, Down, Left, Right,
    Home, End, PageUp, PageDown,
    Insert, Delete,

    // Common
    Space, Enter, Escape, Tab, Backspace,
    // ... etc
}

// KeenEyes.Input.Abstractions/MouseButton.cs
public enum MouseButton
{
    Left,
    Right,
    Middle,
    Button4,
    Button5
}

// KeenEyes.Input.Abstractions/GamepadButton.cs
public enum GamepadButton
{
    A, B, X, Y,
    LeftBumper, RightBumper,
    Back, Start, Guide,
    LeftStick, RightStick,
    DPadUp, DPadDown, DPadLeft, DPadRight
}

// KeenEyes.Input.Abstractions/GamepadAxis.cs
public enum GamepadAxis
{
    LeftX, LeftY,
    RightX, RightY,
    LeftTrigger, RightTrigger
}
```

### Input Components

```csharp
// KeenEyes.Input.Abstractions/Components/InputReceiver.cs
[Component]
public partial struct InputReceiver
{
    public bool Enabled;
    public int Priority;  // Higher priority receives input first
}

// KeenEyes.Input.Abstractions/Components/InputState.cs
[Component]
public partial struct InputState
{
    public Vector2 MovementAxis;     // WASD/Left stick normalized
    public Vector2 LookAxis;         // Mouse delta/Right stick
    public InputFlags CurrentFrame;  // Bitfield of actions this frame
    public InputFlags PreviousFrame; // For detecting changes
}

[Flags]
public enum InputFlags : uint
{
    None = 0,
    Jump = 1 << 0,
    Attack = 1 << 1,
    Interact = 1 << 2,
    Pause = 1 << 3,
    // ... game-specific actions
}
```

### Action Binding System

```csharp
// KeenEyes.Input.Abstractions/InputBinding.cs
public readonly record struct InputBinding
{
    public InputBindingType Type { get; init; }
    public int Code { get; init; }  // Key, MouseButton, or GamepadButton
    public int GamepadIndex { get; init; }
    public float DeadZone { get; init; }

    public static InputBinding Key(Key key) => new()
    {
        Type = InputBindingType.Keyboard,
        Code = (int)key
    };

    public static InputBinding Mouse(MouseButton button) => new()
    {
        Type = InputBindingType.Mouse,
        Code = (int)button
    };

    public static InputBinding Gamepad(GamepadButton button, int index = 0) => new()
    {
        Type = InputBindingType.Gamepad,
        Code = (int)button,
        GamepadIndex = index
    };
}

public enum InputBindingType
{
    Keyboard,
    Mouse,
    Gamepad,
    GamepadAxis
}
```

---

## Project Structure

```
src/
├── KeenEyes.Graphics.Abstractions/
│   ├── KeenEyes.Graphics.Abstractions.csproj
│   ├── IGraphicsBackend.cs
│   ├── IGraphicsContext.cs
│   ├── IRenderer.cs
│   ├── IRenderPipeline.cs
│   ├── IRenderPass.cs
│   ├── ITexture.cs
│   ├── IShader.cs
│   ├── Components/
│   │   ├── Transform2D.cs
│   │   ├── Sprite.cs
│   │   └── Camera2D.cs
│   └── WorldGraphicsExtensions.cs
│
├── KeenEyes.Input.Abstractions/
│   ├── KeenEyes.Input.Abstractions.csproj
│   ├── IInputSource.cs
│   ├── IInputManager.cs
│   ├── Key.cs
│   ├── MouseButton.cs
│   ├── GamepadButton.cs
│   ├── GamepadAxis.cs
│   ├── InputBinding.cs
│   ├── Components/
│   │   ├── InputReceiver.cs
│   │   └── InputState.cs
│   └── WorldInputExtensions.cs
│
├── KeenEyes.Graphics/                    # Silk.NET OpenGL implementation
│   ├── KeenEyes.Graphics.csproj
│   ├── GraphicsPlugin.cs
│   ├── SilkNetBackend.cs                 # Implements IGraphicsBackend
│   ├── SilkNetRenderer.cs                # Implements IRenderer
│   ├── SilkNetTexture.cs                 # Implements ITexture
│   └── SilkNetShader.cs                  # Implements IShader
│
└── KeenEyes.Input/                       # Silk.NET input implementation
    ├── KeenEyes.Input.csproj
    ├── InputPlugin.cs
    ├── SilkNetInputSource.cs             # Implements IInputSource
    └── SilkNetInputManager.cs            # Implements IInputManager
```

### Dependency Graph

```
KeenEyes.Abstractions (ECS contracts)
         ↑
    ┌────┴────┐
    ↓         ↓
Graphics.Abstractions    Input.Abstractions
    ↑                         ↑
    │                         │
KeenEyes.Graphics       KeenEyes.Input
(Silk.NET impl)         (Silk.NET impl)
```

---

## Implementation Plan

### Phase 1: Create Abstraction Projects

1. Create `KeenEyes.Graphics.Abstractions` project
2. Define core interfaces (IGraphicsBackend, IRenderer, ITexture, IShader)
3. Move components from `KeenEyes.Graphics` to abstractions
4. Create `KeenEyes.Input.Abstractions` project
5. Define input interfaces and enums

### Phase 2: Refactor Existing Graphics

1. Have `KeenEyes.Graphics` reference `KeenEyes.Graphics.Abstractions`
2. Implement interfaces in existing classes
3. Update GraphicsPlugin to register IGraphicsContext extension
4. Update samples to use abstraction types

### Phase 3: Implement Input

1. Create `KeenEyes.Input` project
2. Implement `SilkNetInputSource` using Silk.NET.Input
3. Create `InputPlugin` with systems for updating input state
4. Add input to samples

### Phase 4: Documentation & Testing

1. Create mock implementations for testing
2. Write integration tests
3. Update documentation
4. Create migration guide for existing users

---

## Open Questions

1. **3D Support** - Should abstractions include 3D primitives now, or add later?
2. **Render Targets** - How to abstract framebuffers for post-processing?
3. **Shader Language** - GLSL only, or abstract shader representation?
4. **Input Rebinding** - Runtime rebinding UI, or leave to user?
5. **Touch Input** - Mobile support scope?

---

## Related Issues

- Milestone #14: Graphics & Input Abstraction Layer
- Issue #411: Create KeenEyes.Graphics.Abstractions project
- Issue #412: Extract graphics components to abstractions
- Issue #413: Create KeenEyes.Input.Abstractions project
- Issue #414: Implement Silk.NET input backend
- Issue #415: Update GraphicsPlugin for abstraction layer
