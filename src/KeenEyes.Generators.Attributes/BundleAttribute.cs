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
/// Bundles are compositions of multiple components that are commonly used together.
/// All fields in a bundle struct must be valid component types (structs implementing IComponent).
/// Bundles enable convenient bulk addition of related components to entities.
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
/// // Usage:
/// var entity = world.Spawn()
///     .WithTransformBundle(
///         position: new Position { X = 0, Y = 0 },
///         rotation: new Rotation { Angle = 0 },
///         scale: new Scale { X = 1, Y = 1 })
///     .Build();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class BundleAttribute : Attribute;
