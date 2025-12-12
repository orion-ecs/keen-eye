using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a class as an ECS system for auto-discovery and registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
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
/// Specifies that this system must run before another system.
/// </summary>
/// <remarks>
/// <para>
/// This attribute creates an ordering constraint between systems within the same phase.
/// The system with this attribute will be scheduled to execute before the specified target system.
/// </para>
/// <para>
/// Multiple <see cref="RunBeforeAttribute"/> can be applied to the same system to express
/// multiple dependencies. All constraints are resolved using topological sorting.
/// </para>
/// <para>
/// If the constraints create a cycle (e.g., A runs before B, B runs before A),
/// an <see cref="InvalidOperationException"/> is thrown during system sorting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [System(Phase = SystemPhase.Update)]
/// [RunBefore(typeof(RenderSystem))]
/// public partial class MovementSystem : SystemBase
/// {
///     public override void Update(float deltaTime) { }
/// }
/// </code>
/// </example>
/// <param name="targetSystem">The type of system that this system must run before.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="targetSystem"/> is null.</exception>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class RunBeforeAttribute(Type targetSystem) : Attribute
{
    /// <summary>
    /// Gets the type of the system that this system must run before.
    /// </summary>
    public Type TargetSystem { get; } = targetSystem ?? throw new ArgumentNullException(nameof(targetSystem));
}

/// <summary>
/// Specifies that this system must run after another system.
/// </summary>
/// <remarks>
/// <para>
/// This attribute creates an ordering constraint between systems within the same phase.
/// The system with this attribute will be scheduled to execute after the specified target system.
/// </para>
/// <para>
/// Multiple <see cref="RunAfterAttribute"/> can be applied to the same system to express
/// multiple dependencies. All constraints are resolved using topological sorting.
/// </para>
/// <para>
/// If the constraints create a cycle (e.g., A runs after B, B runs after A),
/// an <see cref="InvalidOperationException"/> is thrown during system sorting.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [System(Phase = SystemPhase.Update)]
/// [RunAfter(typeof(InputSystem))]
/// public partial class MovementSystem : SystemBase
/// {
///     public override void Update(float deltaTime) { }
/// }
/// </code>
/// </example>
/// <param name="targetSystem">The type of system that this system must run after.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="targetSystem"/> is null.</exception>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class RunAfterAttribute(Type targetSystem) : Attribute
{
    /// <summary>
    /// Gets the type of the system that this system must run after.
    /// </summary>
    public Type TargetSystem { get; } = targetSystem ?? throw new ArgumentNullException(nameof(targetSystem));
}
