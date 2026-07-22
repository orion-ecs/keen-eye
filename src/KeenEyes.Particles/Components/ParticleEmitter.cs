using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Components;

/// <summary>
/// Component that marks an entity as a particle emitter.
/// </summary>
/// <remarks>
/// <para>
/// Particle emitters spawn particles based on the configured emission settings.
/// The emitter's position is taken from the entity's Transform2D or Transform3D component.
/// </para>
/// <para>
/// Particles are NOT individual entities - they are pooled data managed by the particle system
/// for optimal performance. A single emitter can manage thousands of particles.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Spawn()
///     .With(new Transform2D(position, 0, Vector2.One))
///     .With(ParticleEmitter.Default with
///     {
///         EmissionRate = 100,
///         StartColor = new Vector4(1, 0.5f, 0, 1), // Orange
///         BlendMode = BlendMode.Additive
///     })
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct ParticleEmitter
{
    #region Emission Settings

    /// <summary>
    /// Number of particles to emit per second (continuous emission).
    /// Set to 0 for burst-only emission.
    /// </summary>
    public float EmissionRate;

    /// <summary>
    /// Number of particles to emit in a burst.
    /// Set to 0 for continuous emission only.
    /// </summary>
    public int BurstCount;

    /// <summary>
    /// Time in seconds between bursts.
    /// Set to 0 for a single one-shot burst.
    /// </summary>
    public float BurstInterval;

    /// <summary>
    /// The shape from which particles are emitted.
    /// </summary>
    public EmissionShape Shape;

    /// <summary>
    /// The coordinate space particles are simulated in.
    /// </summary>
    /// <remarks>
    /// <see cref="ParticleSpace.World"/> (the default) spawns particles at the emitter's
    /// world position and leaves them behind when the emitter moves.
    /// <see cref="ParticleSpace.Local"/> attaches particles to the emitter so they move with it.
    /// </remarks>
    public ParticleSpace Space;

    #endregion

    #region Particle Properties

    /// <summary>
    /// Minimum lifetime of spawned particles in seconds.
    /// </summary>
    public float LifetimeMin;

    /// <summary>
    /// Maximum lifetime of spawned particles in seconds.
    /// </summary>
    public float LifetimeMax;

    /// <summary>
    /// Minimum initial size of spawned particles.
    /// </summary>
    public float StartSizeMin;

    /// <summary>
    /// Maximum initial size of spawned particles.
    /// </summary>
    public float StartSizeMax;

    /// <summary>
    /// Minimum initial speed of spawned particles.
    /// </summary>
    public float StartSpeedMin;

    /// <summary>
    /// Maximum initial speed of spawned particles.
    /// </summary>
    public float StartSpeedMax;

    /// <summary>
    /// Minimum initial rotation of spawned particles in radians.
    /// </summary>
    public float StartRotationMin;

    /// <summary>
    /// Maximum initial rotation of spawned particles in radians.
    /// </summary>
    public float StartRotationMax;

    #endregion

    #region Visual Settings

    /// <summary>
    /// The texture to use for rendering particles.
    /// If invalid, particles are rendered as circles.
    /// </summary>
    public TextureHandle Texture;

    /// <summary>
    /// The initial color of spawned particles (RGBA, 0-1 range).
    /// </summary>
    public Vector4 StartColor;

    /// <summary>
    /// The blend mode for rendering particles.
    /// </summary>
    public BlendMode BlendMode;

    /// <summary>
    /// Number of columns in the particle's texture sheet (sprite sheet).
    /// </summary>
    /// <remarks>
    /// When <see cref="TextureSheetColumns"/> multiplied by <see cref="TextureSheetRows"/>
    /// is greater than 1, the emitter's <see cref="Texture"/> is treated as a grid of
    /// animation frames laid out left-to-right, top-to-bottom. The displayed frame advances
    /// with each particle's normalized age (0 at spawn, the final frame at end of life).
    /// A value of 0 or 1 disables sheet animation and draws the whole texture.
    /// </remarks>
    public int TextureSheetColumns;

    /// <summary>
    /// Number of rows in the particle's texture sheet (sprite sheet).
    /// </summary>
    /// <remarks>
    /// See <see cref="TextureSheetColumns"/> for how the grid is interpreted. A value of
    /// 0 or 1 (combined with the column count) disables sheet animation.
    /// </remarks>
    public int TextureSheetRows;

    #endregion

    #region State (managed by system)

    /// <summary>
    /// Whether the emitter is currently emitting particles.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Accumulated time for continuous emission (managed by system).
    /// </summary>
    [BuilderIgnore]
    public float EmissionAccumulator;

    /// <summary>
    /// Timer for burst emission (managed by system).
    /// </summary>
    [BuilderIgnore]
    public float BurstTimer;

    /// <summary>
    /// Whether the initial burst has been emitted (for one-shot bursts).
    /// </summary>
    [BuilderIgnore]
    public bool InitialBurstEmitted;

    #endregion

    /// <summary>
    /// Creates a default particle emitter with sensible settings.
    /// </summary>
    public static ParticleEmitter Default => new()
    {
        EmissionRate = 10f,
        BurstCount = 0,
        BurstInterval = 0f,
        Shape = EmissionShape.Point,
        Space = ParticleSpace.World,
        LifetimeMin = 1f,
        LifetimeMax = 2f,
        StartSizeMin = 8f,
        StartSizeMax = 16f,
        StartSpeedMin = 50f,
        StartSpeedMax = 100f,
        StartRotationMin = 0f,
        StartRotationMax = 0f,
        Texture = TextureHandle.Invalid,
        StartColor = Vector4.One,
        BlendMode = BlendMode.Additive,
        TextureSheetColumns = 1,
        TextureSheetRows = 1,
        IsPlaying = true,
        EmissionAccumulator = 0f,
        BurstTimer = 0f,
        InitialBurstEmitted = false
    };

    /// <summary>
    /// Creates a burst emitter that emits a specified number of particles once.
    /// </summary>
    /// <param name="count">Number of particles to emit.</param>
    /// <param name="lifetime">Lifetime of the particles.</param>
    /// <returns>A one-shot burst emitter.</returns>
    public static ParticleEmitter Burst(int count, float lifetime) => Default with
    {
        EmissionRate = 0f,
        BurstCount = count,
        BurstInterval = 0f,
        LifetimeMin = lifetime,
        LifetimeMax = lifetime
    };

    /// <summary>
    /// Creates a continuous emitter with the specified rate.
    /// </summary>
    /// <param name="rate">Particles per second.</param>
    /// <param name="lifetime">Lifetime of each particle.</param>
    /// <returns>A continuous emitter.</returns>
    public static ParticleEmitter Continuous(float rate, float lifetime) => Default with
    {
        EmissionRate = rate,
        BurstCount = 0,
        LifetimeMin = lifetime,
        LifetimeMax = lifetime
    };
}
