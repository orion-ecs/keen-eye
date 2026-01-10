namespace KeenEyes.Capabilities;

/// <summary>
/// Interface for component type metadata used in serialization operations.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides read-only access to component metadata needed
/// for serialization, without exposing internal implementation details.
/// </para>
/// </remarks>
public interface IComponentInfo
{
    /// <summary>
    /// Gets the CLR type of this component.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the size of the component in bytes. Zero for tag components.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets whether this is a tag component (zero-size marker).
    /// </summary>
    bool IsTag { get; }

    /// <summary>
    /// Gets the schema version of this component for migration support.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Gets the name of the component type.
    /// </summary>
    string Name { get; }
}
