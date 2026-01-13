namespace KeenEyes.TestBridge.Systems;

/// <summary>
/// IPC-transportable snapshot of a system's state and metadata.
/// </summary>
/// <remarks>
/// <para>
/// This record represents a point-in-time snapshot of a system registered
/// with the world. It includes both the system's current state (enabled/disabled)
/// and its execution metadata (phase, order, dependencies).
/// </para>
/// </remarks>
public sealed record SystemSnapshot
{
    /// <summary>
    /// Gets the simple name of the system type.
    /// </summary>
    /// <remarks>
    /// This is the type name without namespace, e.g., "MovementSystem".
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the fully qualified type name of the system.
    /// </summary>
    /// <remarks>
    /// This includes the namespace, e.g., "MyGame.Systems.MovementSystem".
    /// </remarks>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets whether the system is currently enabled.
    /// </summary>
    /// <remarks>
    /// Disabled systems are skipped during world updates.
    /// </remarks>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Gets the execution phase of this system.
    /// </summary>
    /// <remarks>
    /// Common phases include Update, FixedUpdate, LateUpdate, etc.
    /// Systems in earlier phases run before systems in later phases.
    /// </remarks>
    public required string Phase { get; init; }

    /// <summary>
    /// Gets the execution order within the phase.
    /// </summary>
    /// <remarks>
    /// Lower order values run before higher values within the same phase.
    /// </remarks>
    public required int Order { get; init; }

    /// <summary>
    /// Gets the names of systems that this system runs before.
    /// </summary>
    /// <remarks>
    /// These are type names of systems that depend on this system
    /// and must run after it completes.
    /// </remarks>
    public IReadOnlyList<string>? RunsBefore { get; init; }

    /// <summary>
    /// Gets the names of systems that this system runs after.
    /// </summary>
    /// <remarks>
    /// These are type names of systems that this system depends on
    /// and must wait for before running.
    /// </remarks>
    public IReadOnlyList<string>? RunsAfter { get; init; }

    /// <summary>
    /// Gets whether this system is a system group containing other systems.
    /// </summary>
    public bool IsGroup { get; init; }

    /// <summary>
    /// Gets the names of child systems if this is a group.
    /// </summary>
    /// <remarks>
    /// Only populated when <see cref="IsGroup"/> is true.
    /// </remarks>
    public IReadOnlyList<string>? ChildSystems { get; init; }
}
