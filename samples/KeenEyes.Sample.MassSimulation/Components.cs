namespace KeenEyes.Sample.MassSimulation;

// =============================================================================
// COMPONENT DEFINITIONS - Mass Entity Simulation
// =============================================================================
// Demonstrates KeenEyes ECS performance with 100,000+ entities using:
// - Archetype-based chunked storage (128 entities per 16KB chunk)
// - Chunk pooling for reduced GC pressure during spawn/despawn cycles
// - Cache-friendly iteration via contiguous component arrays
// =============================================================================

/// <summary>
/// Position in 2D simulation space.
/// </summary>
[Component]
public partial struct Position
{
    /// <summary>X coordinate.</summary>
    public float X;
    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>
/// Velocity for movement.
/// </summary>
[Component]
public partial struct Velocity
{
    /// <summary>X velocity component.</summary>
    public float X;
    /// <summary>Y velocity component.</summary>
    public float Y;
}

/// <summary>
/// Particle lifetime before despawn.
/// </summary>
[Component]
public partial struct Lifetime
{
    /// <summary>Seconds remaining.</summary>
    public float Remaining;
}

/// <summary>
/// Particle color for rendering.
/// </summary>
[Component]
public partial struct ParticleColor
{
    /// <summary>Hue value (0-360).</summary>
    public float Hue;
}

/// <summary>
/// Gravity influence on particles.
/// </summary>
[Component]
public partial struct Gravity
{
    /// <summary>Gravity strength.</summary>
    public float Strength;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================

/// <summary>Marks a particle as active.</summary>
[TagComponent]
public partial struct Active;

/// <summary>Marks a particle for removal.</summary>
[TagComponent]
public partial struct Dead;
