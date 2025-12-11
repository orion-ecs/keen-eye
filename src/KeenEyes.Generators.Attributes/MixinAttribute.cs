using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a component type to include all fields from one or more mixin types.
/// The source generator will copy all fields from the mixin types into the component at compile-time.
/// </summary>
/// <remarks>
/// <para>
/// Mixins provide compile-time field composition for components, allowing you to reuse common
/// field patterns across multiple component types without inheritance.
/// </para>
/// <para>
/// All mixin types must be structs. Multiple mixins can be applied to a single component.
/// The source generator will validate that there are no circular mixin references.
/// </para>
/// <para>
/// Field names from mixins are copied directly. If multiple mixins define fields with the same name,
/// a compile error will occur. If the component itself defines a field with the same name as a mixin field,
/// a compile error will occur.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Component]
/// public partial struct HealthComponent
/// {
///     public int Current;
///     public int Max;
/// }
///
/// [Component]
/// [Mixin(typeof(HealthComponent))]
/// public partial struct RegeneratingHealth
/// {
///     // Fields from HealthComponent (Current, Max) are copied here
///     public float RegenRate; // Additional field
/// }
///
/// // Usage
/// var entity = world.Spawn()
///     .With(new RegeneratingHealth
///     {
///         Current = 100,
///         Max = 100,
///         RegenRate = 5.0f
///     })
///     .Build();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class MixinAttribute(Type mixinType) : Attribute
{
    /// <summary>
    /// Gets the type to mix in.
    /// </summary>
    public Type MixinType { get; } = mixinType;
}
