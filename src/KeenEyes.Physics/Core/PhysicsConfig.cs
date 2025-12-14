using System.Numerics;

namespace KeenEyes.Physics.Core;

/// <summary>
/// Configuration options for the physics simulation.
/// </summary>
public sealed class PhysicsConfig
{
    /// <summary>
    /// The target fixed timestep for physics simulation in seconds.
    /// </summary>
    /// <remarks>
    /// Default is 1/60 second (60 Hz physics). Lower values increase accuracy
    /// but require more CPU time. Higher values are faster but less accurate.
    /// </remarks>
    public float FixedTimestep { get; init; } = 1f / 60f;

    /// <summary>
    /// Maximum number of physics steps per frame.
    /// </summary>
    /// <remarks>
    /// Limits how many physics steps can run in a single frame to prevent
    /// spiral of death when the game can't keep up with physics simulation.
    /// Default is 3 steps.
    /// </remarks>
    public int MaxStepsPerFrame { get; init; } = 3;

    /// <summary>
    /// The gravity vector applied to all dynamic bodies.
    /// </summary>
    /// <remarks>
    /// Default is (0, -9.81, 0) for Earth-like gravity.
    /// Set to Vector3.Zero for space simulations.
    /// </remarks>
    public Vector3 Gravity { get; init; } = new(0, -9.81f, 0);

    /// <summary>
    /// Number of velocity iterations for the constraint solver.
    /// </summary>
    /// <remarks>
    /// Higher values improve constraint accuracy but require more CPU time.
    /// Default is 8 iterations.
    /// </remarks>
    public int VelocityIterations { get; init; } = 8;

    /// <summary>
    /// Number of substeps for position correction.
    /// </summary>
    /// <remarks>
    /// Higher values improve penetration recovery but require more CPU time.
    /// Default is 1 substep.
    /// </remarks>
    public int SubstepCount { get; init; } = 1;

    /// <summary>
    /// Whether to enable rendering interpolation.
    /// </summary>
    /// <remarks>
    /// When enabled, the <see cref="KeenEyes.Physics.Systems.PhysicsSyncSystem"/>
    /// interpolates between physics states for smooth rendering even when
    /// physics runs at a lower rate than rendering.
    /// </remarks>
    public bool EnableInterpolation { get; init; } = true;

    /// <summary>
    /// Initial capacity for body storage.
    /// </summary>
    /// <remarks>
    /// Setting this to an expected number of bodies avoids reallocations.
    /// Default is 1024 bodies.
    /// </remarks>
    public int InitialBodyCapacity { get; init; } = 1024;

    /// <summary>
    /// Initial capacity for static body storage.
    /// </summary>
    /// <remarks>
    /// Setting this to an expected number of statics avoids reallocations.
    /// Default is 1024 statics.
    /// </remarks>
    public int InitialStaticCapacity { get; init; } = 1024;

    /// <summary>
    /// Initial capacity for constraint storage.
    /// </summary>
    /// <remarks>
    /// Setting this to an expected number of constraints avoids reallocations.
    /// Default is 2048 constraints.
    /// </remarks>
    public int InitialConstraintCapacity { get; init; } = 2048;

    /// <summary>
    /// Default physics configuration for general use.
    /// </summary>
    public static PhysicsConfig Default => new();

    /// <summary>
    /// Validates the configuration and returns any error message.
    /// </summary>
    /// <returns>An error message if invalid, null otherwise.</returns>
    public string? Validate()
    {
        if (FixedTimestep <= 0)
        {
            return "FixedTimestep must be positive";
        }

        if (FixedTimestep > 0.1f)
        {
            return "FixedTimestep is too large (max 0.1 seconds)";
        }

        if (MaxStepsPerFrame < 1)
        {
            return "MaxStepsPerFrame must be at least 1";
        }

        if (VelocityIterations < 1)
        {
            return "VelocityIterations must be at least 1";
        }

        if (SubstepCount < 1)
        {
            return "SubstepCount must be at least 1";
        }

        if (InitialBodyCapacity < 1)
        {
            return "InitialBodyCapacity must be at least 1";
        }

        if (InitialStaticCapacity < 1)
        {
            return "InitialStaticCapacity must be at least 1";
        }

        if (InitialConstraintCapacity < 1)
        {
            return "InitialConstraintCapacity must be at least 1";
        }

        return null;
    }
}
