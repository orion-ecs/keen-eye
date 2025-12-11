namespace KeenEyes;

/// <summary>
/// Marker interface for component bundles.
/// Bundles are compositions of multiple components commonly used together.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on structs to define bundle types.
/// Use the [Bundle] attribute to enable source generation.
/// </para>
/// <para>
/// Bundles provide a convenient way to add multiple related components to entities at once.
/// All fields in a bundle must be valid component types (structs implementing <see cref="IComponent"/>).
/// </para>
/// <para>
/// The source generator will validate bundle definitions and produce:
/// - IBundle interface implementation
/// - Constructor accepting all bundle components
/// - Fluent builder methods for easy entity creation
/// </para>
/// <para>
/// Use bundles when you frequently add the same set of components together.
/// For one-off entity creation, individual component methods are simpler.
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
/// // Generated constructor allows initialization:
/// var bundle = new TransformBundle(
///     new Position { X = 0, Y = 0 },
///     new Rotation { Angle = 0 },
///     new Scale { X = 1, Y = 1 });
///
/// // Generated builder method enables fluent API:
/// var entity = world.Spawn()
///     .WithTransformBundle(position, rotation, scale)
///     .Build();
/// </code>
/// </example>
public interface IBundle;
