namespace KeenEyes.Particles;

/// <summary>
/// Configuration options for the particle system.
/// </summary>
public sealed class ParticlesConfig
{
    /// <summary>
    /// Maximum number of particles per emitter.
    /// </summary>
    /// <remarks>
    /// When an emitter's pool reaches this limit, new particles are not spawned
    /// until existing particles expire. Default is 1000 particles.
    /// </remarks>
    public int MaxParticlesPerEmitter { get; init; } = 1000;

    /// <summary>
    /// Maximum total number of emitters allowed in the world.
    /// </summary>
    /// <remarks>
    /// Additional emitters beyond this limit are ignored.
    /// Default is 100 emitters.
    /// </remarks>
    public int MaxEmitters { get; init; } = 100;

    /// <summary>
    /// Initial pool capacity for each emitter.
    /// </summary>
    /// <remarks>
    /// Pools grow dynamically up to <see cref="MaxParticlesPerEmitter"/>.
    /// Setting this higher reduces reallocations but uses more memory upfront.
    /// Default is 256 particles.
    /// </remarks>
    public int InitialPoolCapacity { get; init; } = 256;

    /// <summary>
    /// Default particle configuration with sensible values.
    /// </summary>
    public static ParticlesConfig Default => new();

    /// <summary>
    /// High-performance configuration for many particles.
    /// </summary>
    public static ParticlesConfig HighPerformance => new()
    {
        MaxParticlesPerEmitter = 5000,
        MaxEmitters = 50,
        InitialPoolCapacity = 1024
    };

    /// <summary>
    /// Low-memory configuration for constrained environments.
    /// </summary>
    public static ParticlesConfig LowMemory => new()
    {
        MaxParticlesPerEmitter = 200,
        MaxEmitters = 20,
        InitialPoolCapacity = 64
    };

    /// <summary>
    /// Validates the configuration and returns any error message.
    /// </summary>
    /// <returns>An error message if invalid, null otherwise.</returns>
    public string? Validate()
    {
        if (MaxParticlesPerEmitter < 1)
        {
            return "MaxParticlesPerEmitter must be at least 1";
        }

        if (MaxEmitters < 1)
        {
            return "MaxEmitters must be at least 1";
        }

        if (InitialPoolCapacity < 1)
        {
            return "InitialPoolCapacity must be at least 1";
        }

        if (InitialPoolCapacity > MaxParticlesPerEmitter)
        {
            return "InitialPoolCapacity cannot exceed MaxParticlesPerEmitter";
        }

        return null;
    }
}
