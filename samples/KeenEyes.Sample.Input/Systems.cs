using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.Input;

/// <summary>
/// System that handles player movement from keyboard/gamepad input.
/// Demonstrates polling-based input in an ECS system.
/// </summary>
public class PlayerMovementSystem : SystemBase
{
    private IInputContext? input;
    private const float MoveSpeed = 5f;
    private const float JumpForce = 8f;
    private const float Gravity = 20f;
    private const float GroundY = 0.5f;

    /// <summary>
    /// Updates player position based on input.
    /// </summary>
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        var keyboard = input.Keyboard;
        var gamepad = input.Gamepad;

        // Calculate movement direction from keyboard
        var moveDir = Vector2.Zero;

        if (keyboard.IsKeyDown(Key.W) || keyboard.IsKeyDown(Key.Up))
        {
            moveDir.Y -= 1;
        }

        if (keyboard.IsKeyDown(Key.S) || keyboard.IsKeyDown(Key.Down))
        {
            moveDir.Y += 1;
        }

        if (keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.Left))
        {
            moveDir.X -= 1;
        }

        if (keyboard.IsKeyDown(Key.D) || keyboard.IsKeyDown(Key.Right))
        {
            moveDir.X += 1;
        }

        // Override with gamepad if connected and has input
        if (gamepad.IsConnected)
        {
            var stick = gamepad.LeftStick;
            if (stick.LengthSquared() > 0.01f)
            {
                moveDir = new Vector2(stick.X, stick.Y);
            }
        }

        // Normalize if needed
        if (moveDir.LengthSquared() > 1f)
        {
            moveDir = Vector2.Normalize(moveDir);
        }

        // Check for jump
        bool wantsJump = keyboard.IsKeyDown(Key.Space) ||
                         (gamepad.IsConnected && gamepad.IsButtonDown(GamepadButton.South));

        // Apply movement to player entities
        foreach (var entity in World.Query<Transform3D, PlayerVelocity, PlayerTag>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var velocity = ref World.Get<PlayerVelocity>(entity);

            // Horizontal movement
            velocity.Velocity = new Vector3(moveDir.X * MoveSpeed, 0, moveDir.Y * MoveSpeed);

            // Jump logic
            bool isGrounded = transform.Position.Y <= GroundY + 0.01f;

            if (wantsJump && isGrounded)
            {
                velocity.VerticalVelocity = JumpForce;
            }

            // Apply gravity
            if (!isGrounded)
            {
                velocity.VerticalVelocity -= Gravity * deltaTime;
            }

            // Update position
            var newPos = transform.Position;
            newPos.X += velocity.Velocity.X * deltaTime;
            newPos.Z += velocity.Velocity.Z * deltaTime;
            newPos.Y += velocity.VerticalVelocity * deltaTime;

            // Clamp to ground
            if (newPos.Y < GroundY)
            {
                newPos.Y = GroundY;
                velocity.VerticalVelocity = 0;
            }

            transform.Position = newPos;
        }
    }
}

/// <summary>
/// System that displays current input state (for debugging/demo).
/// </summary>
public class InputDisplaySystem : SystemBase
{
    private IInputContext? input;
    private float displayTimer;
    private const float DisplayInterval = 1f; // Print status every second

    /// <summary>
    /// Periodically displays input status to console.
    /// </summary>
    public override void Update(float deltaTime)
    {
        input ??= World.TryGetExtension<IInputContext>(out var ctx) ? ctx : null;
        if (input is null)
        {
            return;
        }

        displayTimer += deltaTime;
        if (displayTimer < DisplayInterval)
        {
            return;
        }

        displayTimer = 0;

        var mouse = input.Mouse;
        var gamepad = input.Gamepad;

        // Show mouse position
        var pos = mouse.Position;
        var captured = mouse.IsCursorCaptured ? " [CAPTURED]" : "";

        Console.WriteLine($"Mouse: ({pos.X:F0}, {pos.Y:F0}){captured}");

        // Show gamepad state if connected
        if (gamepad.IsConnected)
        {
            var left = gamepad.LeftStick;
            var right = gamepad.RightStick;
            Console.WriteLine($"Gamepad: L({left.X:F2},{left.Y:F2}) R({right.X:F2},{right.Y:F2}) LT:{gamepad.LeftTrigger:F2} RT:{gamepad.RightTrigger:F2}");
        }

        // Show player position
        foreach (var entity in World.Query<Transform3D, PlayerTag>())
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            Console.WriteLine($"Player: ({transform.Position.X:F2}, {transform.Position.Y:F2}, {transform.Position.Z:F2})");
        }

        Console.WriteLine();
    }
}
