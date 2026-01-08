using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.InputDebugger;

/// <summary>
/// System that logs input state to the console.
/// </summary>
public class InputLogSystem : SystemBase
{
    private IInputContext? input;
    private IGamepad? cachedGamepad;
    private HashSet<Key> lastPressedKeys = [];
    private HashSet<Key> currentPressedKeys = [];
    private MouseButtons lastMouseButtons;
    private float logTimer;
    private const float LogInterval = 0.1f;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        var keyboard = input.Keyboard;
        var mouse = input.Mouse;

        // Check for key state changes using GetState() which only returns valid keys
        currentPressedKeys.Clear();
        var keyboardState = keyboard.GetState();
        foreach (var key in keyboardState.PressedKeys)
        {
            currentPressedKeys.Add(key);
        }

        // Log newly pressed keys
        foreach (var key in currentPressedKeys)
        {
            if (!lastPressedKeys.Contains(key))
            {
                Console.WriteLine($"[KEY DOWN] {key}");
            }
        }

        // Log released keys
        foreach (var key in lastPressedKeys)
        {
            if (!currentPressedKeys.Contains(key))
            {
                Console.WriteLine($"[KEY UP] {key}");
            }
        }

        // Swap buffers
        (lastPressedKeys, currentPressedKeys) = (currentPressedKeys, lastPressedKeys);

        // Check mouse button changes
        var mouseState = mouse.GetState();
        if (mouseState.PressedButtons != lastMouseButtons)
        {
            var changed = mouseState.PressedButtons ^ lastMouseButtons;
            foreach (MouseButton button in Enum.GetValues<MouseButton>())
            {
                var buttonFlag = (MouseButtons)(1 << (int)button);
                if ((changed & buttonFlag) != 0)
                {
                    bool isDown = (mouseState.PressedButtons & buttonFlag) != 0;
                    Console.WriteLine($"[MOUSE {(isDown ? "DOWN" : "UP")}] {button} at ({mouseState.Position.X:F0}, {mouseState.Position.Y:F0})");
                }
            }
            lastMouseButtons = mouseState.PressedButtons;
        }

        // Periodic status log
        logTimer += deltaTime;
        if (logTimer >= LogInterval)
        {
            logTimer = 0;

            cachedGamepad = GetConnectedGamepad(input, cachedGamepad);

            if (cachedGamepad is not null && cachedGamepad.IsConnected)
            {
                var left = cachedGamepad.LeftStick;
                var right = cachedGamepad.RightStick;

                if (left.LengthSquared() > 0.01f || right.LengthSquared() > 0.01f ||
                    cachedGamepad.LeftTrigger > 0.01f || cachedGamepad.RightTrigger > 0.01f)
                {
                    Console.WriteLine($"[GAMEPAD] L:({left.X:+0.00;-0.00},{left.Y:+0.00;-0.00}) R:({right.X:+0.00;-0.00},{right.Y:+0.00;-0.00}) LT:{cachedGamepad.LeftTrigger:F2} RT:{cachedGamepad.RightTrigger:F2}");
                }
            }
        }
    }

    private static IGamepad? GetConnectedGamepad(IInputContext input, IGamepad? cached)
    {
        if (cached is not null && cached.IsConnected)
        {
            return cached;
        }

        var gamepads = input.Gamepads;
        for (int i = 0; i < gamepads.Length; i++)
        {
            if (gamepads[i].IsConnected)
            {
                return gamepads[i];
            }
        }

        return null;
    }
}

/// <summary>
/// System that updates the mouse marker position and spawns click markers.
/// </summary>
public class MouseVisualizerSystem : SystemBase
{
    private IInputContext? input;
    private IGraphicsContext? graphics;
    private MeshHandle? clickMarkerMesh;
    private const float ClickMarkerDuration = 1.0f;
    private bool wasLeftDown;
    private bool wasRightDown;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        graphics ??= World.TryGetExtension<IGraphicsContext>(out var gfx) ? gfx : null;

        if (input is null || graphics is null)
        {
            return;
        }

        var mouse = input.Mouse;

        // Convert screen position to world position (on XZ plane at Y=0.1)
        var normalizedX = (mouse.Position.X / graphics.Width) * 2f - 1f;
        var normalizedY = -((mouse.Position.Y / graphics.Height) * 2f - 1f);
        var worldPos = new Vector3(normalizedX * 10f, 0.1f, normalizedY * 5f);

        // Update mouse marker position
        foreach (var entity in World.Query<Transform3D, MouseMarkerTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            transform.Position = worldPos;
        }

        // Detect click edges (press, not hold)
        bool isLeftDown = mouse.IsButtonDown(MouseButton.Left);
        bool isRightDown = mouse.IsButtonDown(MouseButton.Right);
        bool leftClicked = isLeftDown && !wasLeftDown;
        bool rightClicked = isRightDown && !wasRightDown;
        wasLeftDown = isLeftDown;
        wasRightDown = isRightDown;

        // Spawn click markers on mouse button press
        if (leftClicked || rightClicked)
        {
            clickMarkerMesh ??= graphics.CreateCube(0.3f);

            var color = leftClicked
                ? new Vector4(1f, 0.2f, 0.2f, 1f)
                : new Vector4(0.2f, 0.2f, 1f, 1f);

            World.Spawn()
                .With(new Transform3D(worldPos + Vector3.UnitY * 0.2f, Quaternion.Identity, Vector3.One * 0.5f))
                .With(new Renderable(clickMarkerMesh.Value.Id, 0))
                .With(new Material
                {
                    ShaderId = graphics.LitShader.Id,
                    TextureId = graphics.WhiteTexture.Id,
                    Color = color,
                    Metallic = 0.8f,
                    Roughness = 0.2f
                })
                .WithTag<ClickMarkerTag>()
                .With(new FadeOut { TimeRemaining = ClickMarkerDuration, TotalDuration = ClickMarkerDuration })
                .Build();
        }
    }
}

/// <summary>
/// System that updates keyboard key visualizers.
/// </summary>
public class KeyboardVisualizerSystem : SystemBase
{
    private IInputContext? input;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        var keyboard = input.Keyboard;

        foreach (var entity in World.Query<KeyBinding, Material, KeyVisualizerTag>())
        {
            ref readonly var binding = ref World.Get<KeyBinding>(entity);
            ref var material = ref World.Get<Material>(entity);

            bool isPressed = keyboard.IsKeyDown(binding.Key);

            material.Color = isPressed
                ? new Vector4(0.2f, 1f, 0.2f, 1f)
                : new Vector4(0.4f, 0.4f, 0.4f, 1f);
        }
    }
}

/// <summary>
/// System that updates gamepad visualizers.
/// </summary>
public class GamepadVisualizerSystem : SystemBase
{
    private IInputContext? input;
    private IGamepad? cachedGamepad;
    private const float StickRange = 2f;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        cachedGamepad = GetConnectedGamepad(input, cachedGamepad);
        if (cachedGamepad is null)
        {
            return;
        }

        var leftStick = cachedGamepad.LeftStick;
        var rightStick = cachedGamepad.RightStick;

        foreach (var entity in World.Query<Transform3D, LeftStickMarkerTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            transform.Position = new Vector3(-6f + leftStick.X * StickRange, 0.5f, 3f + leftStick.Y * StickRange);
        }

        foreach (var entity in World.Query<Transform3D, RightStickMarkerTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            transform.Position = new Vector3(-3f + rightStick.X * StickRange, 0.5f, 3f + rightStick.Y * StickRange);
        }

        foreach (var entity in World.Query<GamepadButtonBinding, Material, GamepadButtonTag>())
        {
            ref readonly var binding = ref World.Get<GamepadButtonBinding>(entity);
            ref var material = ref World.Get<Material>(entity);

            bool isPressed = cachedGamepad.IsButtonDown(binding.Button);

            material.Color = isPressed
                ? new Vector4(1f, 0.8f, 0.2f, 1f)
                : new Vector4(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    private static IGamepad? GetConnectedGamepad(IInputContext input, IGamepad? cached)
    {
        if (cached is not null && cached.IsConnected)
        {
            return cached;
        }

        var gamepads = input.Gamepads;
        for (int i = 0; i < gamepads.Length; i++)
        {
            if (gamepads[i].IsConnected)
            {
                return gamepads[i];
            }
        }

        return null;
    }
}

/// <summary>
/// System that handles fading out and removing click markers.
/// </summary>
public class FadeOutSystem : SystemBase
{
    private readonly List<Entity> toRemove = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        toRemove.Clear();

        foreach (var entity in World.Query<FadeOut, Material>())
        {
            ref var fadeOut = ref World.Get<FadeOut>(entity);
            ref var material = ref World.Get<Material>(entity);

            fadeOut.TimeRemaining -= deltaTime;

            if (fadeOut.TimeRemaining <= 0)
            {
                toRemove.Add(entity);
            }
            else
            {
                float alpha = fadeOut.TimeRemaining / fadeOut.TotalDuration;
                material.Color = new Vector4(material.Color.X, material.Color.Y, material.Color.Z, alpha);
            }
        }

        foreach (var entity in toRemove)
        {
            World.Despawn(entity);
        }
    }
}
