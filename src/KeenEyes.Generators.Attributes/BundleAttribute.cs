using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Marks a struct as a component bundle. Source generators will produce:
/// - IBundle interface implementation
/// - Constructor accepting all bundle components
/// - Fluent builder methods (WithBundleName)
/// </summary>
/// <remarks>
/// <para>
/// Bundles are compositions of multiple components that are commonly used together.
/// All fields in a bundle struct must be valid component types (structs implementing IComponent)
/// or other bundle types (structs implementing IBundle).
/// </para>
/// <para>
/// Bundles can be nested up to 5 levels deep. Nested bundles are automatically flattened
/// when added to entities. Circular bundle references are detected and reported as errors.
/// </para>
/// <para>
/// Fields can be marked with <see cref="OptionalAttribute"/> to make them optional.
/// Optional fields must be nullable types and will only be added if they have a value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Bundle]
/// public partial struct TransformBundle
/// {
///     public Position Position;
///     public Rotation Rotation;
///     public Scale Scale;
/// }
///
/// [Bundle]
/// public partial struct CharacterBundle
/// {
///     public TransformBundle Transform; // Nested bundle
///     public Health Health;
///
///     [Optional]
///     public Shield? Shield; // Optional component
/// }
///
/// // Usage:
/// var entity = world.Spawn()
///     .With(new CharacterBundle
///     {
///         Transform = new(position, rotation, scale),
///         Health = new() { Current = 100, Max = 100 }
///         // Shield omitted (null)
///     })
///     .Build();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class BundleAttribute : Attribute;
