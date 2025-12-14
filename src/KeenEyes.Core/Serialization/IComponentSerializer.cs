using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Interface for AOT-compatible component serialization.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by generated code when components are marked with
/// <c>[Component(Serializable = true)]</c>. The source generator creates a strongly-typed
/// implementation that avoids runtime reflection.
/// </para>
/// <para>
/// For applications requiring AOT compatibility, pass the generated serializer to
/// <see cref="SnapshotManager.RestoreSnapshot"/> instead of relying on reflection.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use generated serializer for AOT compatibility
/// var serializer = new ComponentSerializationRegistry();
/// SnapshotManager.RestoreSnapshot(world, snapshot, serializer: serializer);
/// </code>
/// </example>
public interface IComponentSerializer
{
    /// <summary>
    /// Checks if a component type is registered for serialization.
    /// </summary>
    /// <param name="type">The component type to check.</param>
    /// <returns>True if the type can be serialized; false otherwise.</returns>
    bool IsSerializable(Type type);

    /// <summary>
    /// Checks if a component type name is registered for serialization.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name to check.</param>
    /// <returns>True if the type can be serialized; false otherwise.</returns>
    bool IsSerializable(string typeName);

    /// <summary>
    /// Deserializes a component from JSON.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <param name="json">The JSON element containing the component data.</param>
    /// <returns>The deserialized component, or null if the type is not registered.</returns>
    object? Deserialize(string typeName, JsonElement json);

    /// <summary>
    /// Serializes a component to JSON.
    /// </summary>
    /// <param name="type">The type of the component.</param>
    /// <param name="value">The component value to serialize.</param>
    /// <returns>The serialized JSON element, or null if the type is not registered.</returns>
    JsonElement? Serialize(Type type, object value);

    /// <summary>
    /// Gets the CLR type for a type name.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name.</param>
    /// <returns>The CLR type, or null if not registered.</returns>
    Type? GetType(string typeName);

    /// <summary>
    /// Registers a component type in the world's component registry.
    /// </summary>
    /// <param name="world">The world to register the component in.</param>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <param name="isTag">Whether the component is a tag component.</param>
    /// <returns>The component info, or null if the type is not registered in this serializer.</returns>
    /// <remarks>
    /// This method enables AOT-compatible component registration without reflection.
    /// The implementation should call <c>world.Components.Register&lt;T&gt;(isTag)</c>
    /// for the appropriate type.
    /// </remarks>
    ComponentInfo? RegisterComponent(World world, string typeName, bool isTag);

    /// <summary>
    /// Sets a singleton value in the world.
    /// </summary>
    /// <param name="world">The world to set the singleton in.</param>
    /// <param name="typeName">The fully-qualified type name of the singleton.</param>
    /// <param name="value">The singleton value.</param>
    /// <returns>True if the singleton was set; false if the type is not registered.</returns>
    /// <remarks>
    /// This method enables AOT-compatible singleton setting without reflection.
    /// The implementation should call <c>world.SetSingleton&lt;T&gt;((T)value)</c>
    /// for the appropriate type.
    /// </remarks>
    bool SetSingleton(World world, string typeName, object value);

    /// <summary>
    /// Creates a default instance of a component type.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name of the component.</param>
    /// <returns>A default instance of the component, or null if the type is not registered.</returns>
    /// <remarks>
    /// This method enables AOT-compatible default value creation without reflection.
    /// The implementation should return <c>default(T)</c> for the appropriate type.
    /// This is primarily used for tag components during delta restoration.
    /// </remarks>
    object? CreateDefault(string typeName);
}
