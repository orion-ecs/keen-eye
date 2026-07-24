using KeenEyes.Common;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Reads steering input (keyboard and gamepad) and writes it to the ball's
/// <see cref="SteerInput"/> component. NOVAFALL's only inputs are LEFT and RIGHT.
/// </summary>
/// <remarks>
/// This is the only simulation system that touches input devices. When no input
/// context is available (headless <c>--simulate</c> mode), it does nothing and the
/// steering axis stays at zero.
/// </remarks>
public sealed class InputSteerSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (!World.TryGetExtension<IInputContext>(out var input))
        {
            return;
        }

        var axis = 0f;

        var keyboard = input.Keyboard;
        if (keyboard.IsKeyDown(Key.A) || keyboard.IsKeyDown(Key.Left))
        {
            axis -= 1f;
        }

        if (keyboard.IsKeyDown(Key.D) || keyboard.IsKeyDown(Key.Right))
        {
            axis += 1f;
        }

        // Gamepad left stick only contributes when the keyboard is idle, so the
        // two devices never fight over the ball.
        var gamepad = input.Gamepad;
        if (axis.IsApproximatelyZero() && gamepad.IsConnected)
        {
            axis = Math.Clamp(gamepad.LeftStick.X, -1f, 1f);
        }

        foreach (var entity in World.Query<Ball, SteerInput>())
        {
            ref var steer = ref World.Get<SteerInput>(entity);
            steer.Axis = axis;
        }
    }
}
