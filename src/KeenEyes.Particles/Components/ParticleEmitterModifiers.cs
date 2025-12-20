using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Components;

/// <summary>
/// Component that holds modifier configurations for a particle emitter.
/// </summary>
/// <remarks>
/// <para>
/// Modifiers change particle properties over their lifetime. Each modifier type
/// is optional - set to null to disable that modifier.
/// </para>
/// <para>
/// Modifiers are applied in order during the particle update phase:
/// Gravity → Velocity → Size → Color → Rotation
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Spawn()
///     .With(new Transform2D(...))
///     .With(ParticleEmitter.Default)
///     .With(new ParticleEmitterModifiers
///     {
///         GravityX = 0,
///         GravityY = 98f, // Downward gravity
///         ColorGradient = ParticleGradient.FadeOut(new Vector4(1, 0.5f, 0, 1))
///     })
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct ParticleEmitterModifiers
{
    #region Gravity Modifier

    /// <summary>
    /// Whether gravity is applied to particles.
    /// </summary>
    public bool HasGravity;

    /// <summary>
    /// X component of gravity acceleration (units per second squared).
    /// </summary>
    public float GravityX;

    /// <summary>
    /// Y component of gravity acceleration (units per second squared).
    /// Positive values typically point downward in screen coordinates.
    /// </summary>
    public float GravityY;

    /// <summary>
    /// Air resistance factor (0 = no drag, 1 = full stop over 1 second).
    /// </summary>
    public float Drag;

    #endregion

    #region Velocity Over Lifetime

    /// <summary>
    /// Whether velocity scaling over lifetime is enabled.
    /// </summary>
    public bool HasVelocityOverLifetime;

    /// <summary>
    /// Curve that scales particle speed over lifetime.
    /// Value of 1.0 = original speed, 0.5 = half speed, etc.
    /// </summary>
    public ParticleCurve VelocityCurve;

    #endregion

    #region Size Over Lifetime

    /// <summary>
    /// Whether size scaling over lifetime is enabled.
    /// </summary>
    public bool HasSizeOverLifetime;

    /// <summary>
    /// Curve that scales particle size over lifetime.
    /// Value of 1.0 = original size, 2.0 = double size, etc.
    /// </summary>
    public ParticleCurve SizeCurve;

    #endregion

    #region Color Over Lifetime

    /// <summary>
    /// Whether color changes over lifetime are enabled.
    /// </summary>
    public bool HasColorOverLifetime;

    /// <summary>
    /// Gradient that defines particle color over lifetime.
    /// </summary>
    public ParticleGradient ColorGradient;

    #endregion

    #region Rotation Over Lifetime

    /// <summary>
    /// Whether rotation over lifetime is enabled.
    /// </summary>
    public bool HasRotationOverLifetime;

    /// <summary>
    /// Rotation speed in radians per second.
    /// </summary>
    public float RotationSpeed;

    /// <summary>
    /// Curve that scales rotation speed over lifetime.
    /// </summary>
    public ParticleCurve RotationCurve;

    #endregion

    /// <summary>
    /// Creates default modifiers with no effects enabled.
    /// </summary>
    public static ParticleEmitterModifiers None => new()
    {
        HasGravity = false,
        GravityX = 0,
        GravityY = 0,
        Drag = 0,
        HasVelocityOverLifetime = false,
        VelocityCurve = ParticleCurve.Constant(1f),
        HasSizeOverLifetime = false,
        SizeCurve = ParticleCurve.Constant(1f),
        HasColorOverLifetime = false,
        ColorGradient = ParticleGradient.Constant(System.Numerics.Vector4.One),
        HasRotationOverLifetime = false,
        RotationSpeed = 0,
        RotationCurve = ParticleCurve.Constant(1f)
    };

    /// <summary>
    /// Creates modifiers with gravity enabled.
    /// </summary>
    /// <param name="gravityY">Gravity strength (positive = down).</param>
    /// <param name="drag">Optional drag coefficient.</param>
    /// <returns>Modifiers with gravity enabled.</returns>
    public static ParticleEmitterModifiers WithGravity(float gravityY, float drag = 0) => None with
    {
        HasGravity = true,
        GravityY = gravityY,
        Drag = drag
    };

    /// <summary>
    /// Creates modifiers with a fade-out color effect.
    /// </summary>
    /// <param name="startColor">The starting color.</param>
    /// <returns>Modifiers with color fade-out enabled.</returns>
    public static ParticleEmitterModifiers WithFadeOut(System.Numerics.Vector4 startColor) => None with
    {
        HasColorOverLifetime = true,
        ColorGradient = ParticleGradient.FadeOut(startColor)
    };
}
