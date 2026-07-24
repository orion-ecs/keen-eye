using System.Numerics;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// The camera as an actor: trauma-decay shake, a downward kick on Floor Smash,
/// speed-scaled zoom-out at high fall speed, and a crush-proximity zoom-in.
/// Writes only the <see cref="CameraState"/> singleton; the render system
/// composes it into the orthographic projection matrix.
/// </summary>
/// <remarks>
/// <para>
/// Runs in EarlyUpdate so it reacts to the previous frame's
/// <see cref="FrameEvents"/> (before <see cref="FrameEventsClearSystem"/> wipes
/// them). All motion uses REAL delta time — the camera must keep breathing
/// through hitstop and slow motion, or the freeze reads as a hang.
/// </para>
/// <para>
/// Shake follows the trauma pattern: events add trauma, trauma decays linearly,
/// and the offset amplitude is trauma SQUARED — so big hits shake hard while
/// small ones only shiver. The offset itself is smooth pseudo-noise, clamped
/// under <see cref="Tuning.MaxShakeOffset"/> design units per the readability
/// contract. The noise is a pure function of accumulated phase — no wall clock,
/// no <c>System.Random</c> — and it never feeds back into the simulation.
/// </para>
/// </remarks>
public sealed class CameraSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        ref var camera = ref World.GetSingleton<CameraState>();

        if (!juice.PresentationAvailable || !juice.Enabled)
        {
            // Juice off: a perfectly still, neutral camera — the A side of the
            // A/B readability demo.
            camera.Trauma = 0f;
            camera.ShakeOffset = Vector2.Zero;
            camera.KickY = 0f;
            camera.Zoom = 1f;
            return;
        }

        ref readonly var events = ref World.GetSingleton<FrameEvents>();

        // --- Trauma in ---
        if (events.Smashed)
        {
            camera.Trauma += Tuning.SmashTrauma;
            camera.KickY += Tuning.SmashKick;
        }

        if (events.Grazes > 0)
        {
            camera.Trauma += Tuning.GrazeTrauma;
        }

        if (events.TierChanged && events.TierTo > events.TierFrom)
        {
            camera.Trauma += Tuning.TierUpTrauma;
        }

        camera.Trauma = Math.Clamp(camera.Trauma, 0f, 1f);

        // --- Trauma out, kick recovery ---
        camera.Trauma = Math.Max(camera.Trauma - Tuning.TraumaDecayPerSecond * deltaTime, 0f);
        camera.KickY -= camera.KickY * Math.Min(Tuning.KickDecayPerSecond * deltaTime, 1f);

        // --- Shake offset: amplitude = trauma², direction = smooth noise ---
        camera.NoisePhase += deltaTime * Tuning.ShakeFrequency;
        var amplitude = camera.Trauma * camera.Trauma * Tuning.MaxShakeOffset;
        camera.ShakeOffset = new Vector2(
            amplitude * Noise(camera.NoisePhase, 0.0f),
            amplitude * Noise(camera.NoisePhase, 7.31f));

        // --- Zoom: out with speed, in with crush danger ---
        var targetZoom = 1f;
        if (World.GetSingleton<GameState>().Phase == GamePhase.Playing)
        {
            foreach (var entity in World.Query<Ball, Position2D, Velocity2D>())
            {
                ref readonly var position = ref World.Get<Position2D>(entity);
                ref readonly var velocity = ref World.Get<Velocity2D>(entity);

                var speedFraction = Math.Clamp(velocity.Y / Tuning.MaxFallSpeed, 0f, 1f);
                targetZoom = 1f + (Tuning.ZoomOutAtMaxFall - 1f) * speedFraction;

                // Crush proximity overrides speed: lean IN on the danger.
                var ceilingDistance = position.Y - Tuning.CeilingY;
                if (ceilingDistance < Tuning.CrushProximityRange)
                {
                    var danger = 1f - Math.Clamp(ceilingDistance / Tuning.CrushProximityRange, 0f, 1f);
                    targetZoom = float.Lerp(targetZoom, Tuning.CrushZoomIn, danger);
                }

                break;
            }
        }

        camera.Zoom += (targetZoom - camera.Zoom) * Math.Min(Tuning.ZoomLerpPerSecond * deltaTime, 1f);
    }

    /// <summary>
    /// Smooth periodic pseudo-noise in [-1, 1]: two incommensurate sine waves.
    /// Cheap, allocation-free, and continuous — exactly enough for shake.
    /// </summary>
    private static float Noise(float phase, float lane)
        => 0.6f * MathF.Sin(phase * 1.093f + lane) + 0.4f * MathF.Sin(phase * 2.357f + lane * 3.7f);
}
