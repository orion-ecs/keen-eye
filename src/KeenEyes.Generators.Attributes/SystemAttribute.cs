using System;

namespace KeenEyes;

/// <summary>
/// Marks a class as an ECS system for auto-discovery and registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SystemAttribute : Attribute
{
    /// <summary>
    /// The phase in which this system runs (e.g., Update, FixedUpdate, Render).
    /// </summary>
    public SystemPhase Phase { get; set; } = SystemPhase.Update;

    /// <summary>
    /// Execution order within the phase. Lower values run first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Optional group name for organizing related systems.
    /// </summary>
    public string? Group { get; set; }
}

/// <summary>
/// Defines when a system executes in the game loop.
/// </summary>
public enum SystemPhase
{
    /// <summary>Runs at the start of each frame before other updates.</summary>
    EarlyUpdate,

    /// <summary>Runs at a fixed timestep, ideal for physics.</summary>
    FixedUpdate,

    /// <summary>Main update phase, runs every frame.</summary>
    Update,

    /// <summary>Runs after Update, before rendering.</summary>
    LateUpdate,

    /// <summary>Runs during the render phase.</summary>
    Render,

    /// <summary>Runs after rendering completes.</summary>
    PostRender
}
