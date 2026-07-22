namespace KeenEyes.Particles.Data;

/// <summary>
/// The coordinate space in which an emitter's particles are simulated.
/// </summary>
/// <remarks>
/// This controls whether particles are anchored to world coordinates at spawn time
/// or remain attached to the emitter and move with it.
/// </remarks>
public enum ParticleSpace
{
    /// <summary>
    /// Particles are spawned at the emitter's world position and stored in world
    /// coordinates. Once spawned they are independent of the emitter, so moving the
    /// emitter afterwards has no effect on existing particles. This is the default.
    /// </summary>
    World,

    /// <summary>
    /// Particles are stored relative to the emitter (local coordinates). Their world
    /// position is resolved each frame by adding the emitter's current position, so
    /// moving the emitter moves all of its live particles with it.
    /// </summary>
    Local
}
