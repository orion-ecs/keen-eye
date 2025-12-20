namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for entity inspection and debugging.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to debugging and inspection APIs that allow
/// examining entity state, component metadata, and entity naming. These features
/// are useful for debugging tools, editors, and runtime introspection.
/// </para>
/// <para>
/// Plugins that need entity inspection should request this capability via
/// <see cref="IPluginContext.GetCapability{T}"/> rather than casting to
/// the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IInspectionCapability&gt;(out var inspector))
///     {
///         var name = inspector.GetName(entity);
///         var components = inspector.GetComponentTypes(entity);
///     }
/// }
/// </code>
/// </example>
public interface IInspectionCapability
{
    /// <summary>
    /// Gets the name assigned to an entity.
    /// </summary>
    /// <param name="entity">The entity to get the name for.</param>
    /// <returns>The entity's name, or null if no name is assigned.</returns>
    string? GetName(Entity entity);

    /// <summary>
    /// Checks if an entity has a component of the specified type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentType">The component type to check for.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    /// <remarks>
    /// This method allows checking for components by runtime Type, useful for
    /// debugging and inspection scenarios where the component type isn't known
    /// at compile time.
    /// </remarks>
    bool HasComponent(Entity entity, Type componentType);

    /// <summary>
    /// Gets metadata about all registered component types.
    /// </summary>
    /// <returns>An enumerable of component metadata.</returns>
    /// <remarks>
    /// This includes all components that have been registered with the world,
    /// whether explicitly or implicitly through entity creation.
    /// </remarks>
    IEnumerable<RegisteredComponentInfo> GetRegisteredComponents();
}

/// <summary>
/// Contains metadata about a registered component type.
/// </summary>
/// <param name="Type">The CLR type of the component.</param>
/// <param name="Name">The name of the component type.</param>
/// <param name="Size">The size of the component in bytes.</param>
/// <param name="IsTag">Whether this is a tag component (zero-size).</param>
public readonly record struct RegisteredComponentInfo(
    Type Type,
    string Name,
    int Size,
    bool IsTag);
