using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Specifies that a component requires another component to be present on the same entity.
/// </summary>
/// <remarks>
/// <para>
/// When validation is enabled, adding a component with this attribute will verify that
/// all required components are already present on the entity. If a required component
/// is missing, a <c>ComponentValidationException</c> is thrown.
/// </para>
/// <para>
/// Multiple <see cref="RequiresComponentAttribute"/> can be applied to the same component
/// to express multiple dependencies. Each dependency is validated independently when the
/// component is added - required components must already be present on the entity.
/// </para>
/// <para>
/// <b>Note:</b> Dependencies are checked directly, not transitively. If A requires B and
/// B requires C, adding A only validates that B is present. To ensure C is also present,
/// either add C explicitly or add a <c>[RequiresComponent(typeof(C))]</c> to A.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // RigidBody requires Transform to be present
/// [Component]
/// [RequiresComponent(typeof(Transform))]
/// public partial struct RigidBody
/// {
///     public float Mass;
///     public float Drag;
/// }
///
/// // Sprite requires both Transform and Renderable
/// [Component]
/// [RequiresComponent(typeof(Transform))]
/// [RequiresComponent(typeof(Renderable))]
/// public partial struct Sprite
/// {
///     public string TextureId;
/// }
/// </code>
/// </example>
/// <param name="requiredType">The type of component that must be present on the entity.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="requiredType"/> is null.</exception>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class RequiresComponentAttribute(Type requiredType) : Attribute
{
    /// <summary>
    /// Gets the type of the required component.
    /// </summary>
    public Type RequiredType { get; } = requiredType ?? throw new ArgumentNullException(nameof(requiredType));
}

/// <summary>
/// Specifies that a component conflicts with another component and cannot coexist on the same entity.
/// </summary>
/// <remarks>
/// <para>
/// When validation is enabled, adding a component with this attribute will verify that
/// no conflicting components are present on the entity. If a conflicting component
/// is found, a <c>ComponentValidationException</c> is thrown.
/// </para>
/// <para>
/// Multiple <see cref="ConflictsWithAttribute"/> can be applied to the same component
/// to express multiple conflicts. Conflicts are bidirectional - if A conflicts with B,
/// then B implicitly conflicts with A.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // StaticBody and DynamicBody are mutually exclusive
/// [Component]
/// [ConflictsWith(typeof(DynamicBody))]
/// public partial struct StaticBody
/// {
///     public bool IsKinematic;
/// }
///
/// [Component]
/// [ConflictsWith(typeof(StaticBody))]
/// public partial struct DynamicBody
/// {
///     public float Mass;
///     public float Drag;
/// }
/// </code>
/// </example>
/// <param name="conflictingType">The type of component that cannot coexist with this component.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="conflictingType"/> is null.</exception>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class ConflictsWithAttribute(Type conflictingType) : Attribute
{
    /// <summary>
    /// Gets the type of the conflicting component.
    /// </summary>
    public Type ConflictingType { get; } = conflictingType ?? throw new ArgumentNullException(nameof(conflictingType));
}
