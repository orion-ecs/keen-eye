namespace KeenEyes.Capabilities;

/// <summary>
/// Extended snapshot capability for AOT-compatible serialization operations.
/// </summary>
/// <remarks>
/// <para>
/// This capability extends <see cref="ISnapshotCapability"/> with access to the
/// component registry, which is needed for AOT-compatible component registration
/// and singleton setting during deserialization.
/// </para>
/// <para>
/// The World class implements this capability. Serialization code
/// should use this interface instead of the base <see cref="ISnapshotCapability"/>
/// when component registration is needed.
/// </para>
/// </remarks>
public interface ISerializationCapability : ISnapshotCapability
{
    /// <summary>
    /// Gets the component registry for component type registration.
    /// </summary>
    IComponentRegistry Components { get; }
}

/// <summary>
/// Abstraction over the component registry for AOT-compatible component registration.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows snapshot and serialization code to work with component registries
/// without depending on the concrete ComponentRegistry type.
/// </para>
/// </remarks>
public interface IComponentRegistry
{
    /// <summary>
    /// Gets all registered component types.
    /// </summary>
    IReadOnlyList<IComponentInfo> All { get; }

    /// <summary>
    /// Gets the number of registered component types.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets component info for a type.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <returns>The component info, or null if not registered.</returns>
    IComponentInfo? Get(Type type);

    /// <summary>
    /// Registers a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="isTag">Whether the component is a tag component.</param>
    /// <returns>The component info.</returns>
    IComponentInfo Register<T>(bool isTag = false) where T : struct, IComponent;
}
