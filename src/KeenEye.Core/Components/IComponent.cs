namespace KeenEye;

/// <summary>
/// Marker interface for ECS components.
/// Components are data containers attached to entities.
/// </summary>
/// <remarks>
/// Implement this interface on structs to define component types.
/// Use the [Component] attribute to enable source generation.
/// </remarks>
public interface IComponent;

/// <summary>
/// Marker interface for tag components (zero-size markers).
/// Tag components have no data and are used purely for filtering queries.
/// </summary>
/// <remarks>
/// Use the [TagComponent] attribute to enable source generation.
/// </remarks>
public interface ITagComponent : IComponent;
