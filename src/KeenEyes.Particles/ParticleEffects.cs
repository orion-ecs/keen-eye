using System.Numerics;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles;

/// <summary>
/// Factory class for creating common particle effects.
/// </summary>
/// <remarks>
/// <para>
/// This class provides pre-configured particle emitter configurations for common
/// visual effects. Use these as starting points and customize as needed.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var entity = world.Spawn()
///     .With(new Transform2D(position, 0, Vector2.One))
///     .With(ParticleEffects.Fire())
///     .With(ParticleEffects.FireModifiers())
///     .Build();
/// </code>
/// </para>
/// </remarks>
public static class ParticleEffects
{
    #region Fire Effect

    /// <summary>
    /// Creates a fire particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Fire particles emit upward with orange/yellow colors and additive blending
    /// for a bright, glowing effect.
    /// </remarks>
    /// <returns>A fire particle emitter.</returns>
    public static ParticleEmitter Fire()
    {
        return new ParticleEmitter
        {
            EmissionRate = 50f,
            LifetimeMin = 0.5f,
            LifetimeMax = 1.2f,
            StartSizeMin = 8f,
            StartSizeMax = 16f,
            StartSpeedMin = 30f,
            StartSpeedMax = 60f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(1f, 0.8f, 0.3f, 1f), // Orange-yellow
            BlendMode = BlendMode.Additive,
            Shape = EmissionShape.Cone(MathF.PI / 6f, 5f, new Vector2(0, -1)), // Upward cone
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates fire modifiers for color, size, and movement.
    /// </summary>
    /// <returns>Fire particle modifiers.</returns>
    public static ParticleEmitterModifiers FireModifiers()
    {
        // Fire gradient: bright yellow -> orange -> red -> transparent
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(1f, 1f, 0.5f, 1f)),     // Bright yellow
            (0.3f, new Vector4(1f, 0.6f, 0.2f, 1f)), // Orange
            (0.7f, new Vector4(0.8f, 0.2f, 0.1f, 0.7f)), // Red
            (1f, new Vector4(0.3f, 0.1f, 0.1f, 0f))  // Transparent dark red
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = -50f, // Upward drift
            Drag = 0.5f,
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.EaseOut()
        };
    }

    #endregion

    #region Smoke Effect

    /// <summary>
    /// Creates a smoke particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Smoke particles drift upward slowly with gray colors and transparent blending.
    /// </remarks>
    /// <returns>A smoke particle emitter.</returns>
    public static ParticleEmitter Smoke()
    {
        return new ParticleEmitter
        {
            EmissionRate = 20f,
            LifetimeMin = 2f,
            LifetimeMax = 4f,
            StartSizeMin = 10f,
            StartSizeMax = 20f,
            StartSpeedMin = 10f,
            StartSpeedMax = 25f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(0.4f, 0.4f, 0.4f, 0.6f), // Gray
            BlendMode = BlendMode.Transparent,
            Shape = EmissionShape.Cone(MathF.PI / 4f, 8f, new Vector2(0, -1)), // Wide upward cone
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates smoke modifiers for expansion and fading.
    /// </summary>
    /// <returns>Smoke particle modifiers.</returns>
    public static ParticleEmitterModifiers SmokeModifiers()
    {
        // Smoke fades out gradually
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(0.5f, 0.5f, 0.5f, 0.5f)),
            (0.5f, new Vector4(0.4f, 0.4f, 0.4f, 0.4f)),
            (1f, new Vector4(0.3f, 0.3f, 0.3f, 0f))
        ]);

        // Smoke expands as it rises
        var sizeCurve = ParticleCurve.FromPoints([
            (0f, 1f),
            (0.5f, 2f),
            (1f, 3f)
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = -20f, // Slow upward drift
            Drag = 0.8f, // High drag
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasSizeOverLifetime = true,
            SizeCurve = sizeCurve,
            HasRotationOverLifetime = true,
            RotationSpeed = 0.5f, // Slow rotation
            RotationCurve = ParticleCurve.Constant(1f)
        };
    }

    #endregion

    #region Explosion Effect

    /// <summary>
    /// Creates an explosion particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Explosion particles burst outward radially with fast velocity and quick fade.
    /// </remarks>
    /// <returns>An explosion particle emitter.</returns>
    public static ParticleEmitter Explosion()
    {
        return new ParticleEmitter
        {
            EmissionRate = 0f, // No continuous emission
            BurstCount = 100,
            BurstInterval = 0f, // One-shot
            LifetimeMin = 0.3f,
            LifetimeMax = 0.8f,
            StartSizeMin = 6f,
            StartSizeMax = 12f,
            StartSpeedMin = 100f,
            StartSpeedMax = 200f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(1f, 0.8f, 0.4f, 1f), // Bright orange
            BlendMode = BlendMode.Additive,
            Shape = EmissionShape.Sphere(10f), // Radial explosion
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates explosion modifiers for fast fade and gravity.
    /// </summary>
    /// <returns>Explosion particle modifiers.</returns>
    public static ParticleEmitterModifiers ExplosionModifiers()
    {
        // Fast color transition: white -> yellow -> orange -> red -> transparent
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(1f, 1f, 1f, 1f)),      // White flash
            (0.1f, new Vector4(1f, 0.9f, 0.5f, 1f)), // Yellow
            (0.4f, new Vector4(1f, 0.5f, 0.2f, 0.8f)), // Orange
            (0.7f, new Vector4(0.8f, 0.2f, 0.1f, 0.4f)), // Red
            (1f, new Vector4(0.3f, 0.1f, 0.05f, 0f)) // Dark, transparent
        ]);

        // Size shrinks quickly
        var sizeCurve = ParticleCurve.FromPoints([
            (0f, 1.5f),
            (0.3f, 1f),
            (1f, 0.2f)
        ]);

        // Velocity slows down
        var velocityCurve = ParticleCurve.FromPoints([
            (0f, 1f),
            (0.2f, 0.5f),
            (1f, 0.1f)
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = 50f, // Pull down
            Drag = 2f, // High drag
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasSizeOverLifetime = true,
            SizeCurve = sizeCurve,
            HasVelocityOverLifetime = true,
            VelocityCurve = velocityCurve
        };
    }

    #endregion

    #region Magic Sparkles Effect

    /// <summary>
    /// Creates a magic sparkles particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Magic sparkles float and spin with color-shifting effects.
    /// </remarks>
    /// <returns>A magic sparkles particle emitter.</returns>
    public static ParticleEmitter MagicSparkles()
    {
        return new ParticleEmitter
        {
            EmissionRate = 30f,
            LifetimeMin = 1f,
            LifetimeMax = 2f,
            StartSizeMin = 3f,
            StartSizeMax = 8f,
            StartSpeedMin = 5f,
            StartSpeedMax = 20f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(0.8f, 0.6f, 1f, 1f), // Light purple
            BlendMode = BlendMode.Additive,
            Shape = EmissionShape.Sphere(20f), // Spherical emission
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates magic sparkles modifiers for color shifting and rotation.
    /// </summary>
    /// <returns>Magic sparkles particle modifiers.</returns>
    public static ParticleEmitterModifiers MagicSparklesModifiers()
    {
        // Color shifts through magical colors with fade in/out
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(0.5f, 0.3f, 1f, 0f)),   // Start transparent purple
            (0.2f, new Vector4(0.8f, 0.5f, 1f, 1f)), // Fade in bright purple
            (0.5f, new Vector4(0.4f, 0.8f, 1f, 1f)), // Shift to cyan
            (0.8f, new Vector4(1f, 0.6f, 0.8f, 0.8f)), // Shift to pink
            (1f, new Vector4(0.8f, 0.4f, 1f, 0f))   // Fade out purple
        ]);

        // Size pulses
        var sizeCurve = ParticleCurve.FromPoints([
            (0f, 0.5f),
            (0.25f, 1.2f),
            (0.5f, 0.8f),
            (0.75f, 1.1f),
            (1f, 0.3f)
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = -10f, // Gentle upward float
            Drag = 0.2f,
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasSizeOverLifetime = true,
            SizeCurve = sizeCurve,
            HasRotationOverLifetime = true,
            RotationSpeed = 3f, // Fast spin
            RotationCurve = ParticleCurve.Constant(1f)
        };
    }

    #endregion

    #region Blood Splatter Effect

    /// <summary>
    /// Creates a blood splatter particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Blood splatters burst outward and fall with gravity.
    /// </remarks>
    /// <returns>A blood splatter particle emitter.</returns>
    public static ParticleEmitter BloodSplatter()
    {
        return new ParticleEmitter
        {
            EmissionRate = 0f,
            BurstCount = 20,
            BurstInterval = 0f, // One-shot
            LifetimeMin = 0.5f,
            LifetimeMax = 1.5f,
            StartSizeMin = 4f,
            StartSizeMax = 10f,
            StartSpeedMin = 50f,
            StartSpeedMax = 120f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(0.6f, 0f, 0f, 1f), // Dark red
            BlendMode = BlendMode.Transparent,
            Shape = EmissionShape.Cone(MathF.PI / 3f, 5f, new Vector2(0, -1)), // Upward spray
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates blood splatter modifiers.
    /// </summary>
    /// <returns>Blood splatter particle modifiers.</returns>
    public static ParticleEmitterModifiers BloodSplatterModifiers()
    {
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(0.8f, 0f, 0f, 1f)),
            (0.5f, new Vector4(0.5f, 0f, 0f, 0.9f)),
            (1f, new Vector4(0.3f, 0f, 0f, 0f))
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = 300f, // Strong downward gravity
            Drag = 0.5f,
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut()
        };
    }

    #endregion

    #region Rain Effect

    /// <summary>
    /// Creates a rain particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Rain falls straight down with elongated particles.
    /// </remarks>
    /// <returns>A rain particle emitter.</returns>
    public static ParticleEmitter Rain()
    {
        return new ParticleEmitter
        {
            EmissionRate = 200f,
            LifetimeMin = 0.5f,
            LifetimeMax = 1f,
            StartSizeMin = 2f,
            StartSizeMax = 4f,
            StartSpeedMin = 300f,
            StartSpeedMax = 400f,
            StartRotationMin = 0f,
            StartRotationMax = 0f, // No rotation - rain falls straight
            StartColor = new Vector4(0.7f, 0.8f, 0.9f, 0.6f), // Light blue-gray
            BlendMode = BlendMode.Transparent,
            Shape = EmissionShape.Box(400f, 10f), // Wide emission area
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates rain modifiers.
    /// </summary>
    /// <returns>Rain particle modifiers.</returns>
    public static ParticleEmitterModifiers RainModifiers()
    {
        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = 500f // Strong downward gravity
        };
    }

    #endregion

    #region Snow Effect

    /// <summary>
    /// Creates a snow particle emitter configuration.
    /// </summary>
    /// <remarks>
    /// Snow drifts gently downward with swaying motion.
    /// </remarks>
    /// <returns>A snow particle emitter.</returns>
    public static ParticleEmitter Snow()
    {
        return new ParticleEmitter
        {
            EmissionRate = 50f,
            LifetimeMin = 3f,
            LifetimeMax = 6f,
            StartSizeMin = 2f,
            StartSizeMax = 6f,
            StartSpeedMin = 20f,
            StartSpeedMax = 40f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            StartColor = new Vector4(1f, 1f, 1f, 0.9f), // White
            BlendMode = BlendMode.Transparent,
            Shape = EmissionShape.Box(400f, 10f), // Wide emission area
            IsPlaying = true
        };
    }

    /// <summary>
    /// Creates snow modifiers.
    /// </summary>
    /// <returns>Snow particle modifiers.</returns>
    public static ParticleEmitterModifiers SnowModifiers()
    {
        var colorGradient = ParticleGradient.FromPoints([
            (0f, new Vector4(1f, 1f, 1f, 0.8f)),
            (0.8f, new Vector4(1f, 1f, 1f, 0.8f)),
            (1f, new Vector4(1f, 1f, 1f, 0f))
        ]);

        return new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityX = 0f,
            GravityY = 30f, // Gentle downward drift
            Drag = 0.3f,
            HasColorOverLifetime = true,
            ColorGradient = colorGradient,
            HasRotationOverLifetime = true,
            RotationSpeed = 1f, // Slow spin
            RotationCurve = ParticleCurve.Constant(1f)
        };
    }

    #endregion
}
