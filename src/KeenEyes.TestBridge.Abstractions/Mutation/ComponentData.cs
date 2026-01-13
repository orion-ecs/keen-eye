using System.Text.Json;

namespace KeenEyes.TestBridge.Mutation;

/// <summary>
/// Data transfer object for component information in mutation operations.
/// </summary>
/// <remarks>
/// <para>
/// This record is used to specify component type and initial data when spawning
/// entities or adding components. The type is specified by name (short or full name)
/// and the data is provided as a JSON element for runtime deserialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var components = new[]
/// {
///     new ComponentData { Type = "Position", Data = JsonSerializer.SerializeToElement(new { X = 10, Y = 20 }) },
///     new ComponentData { Type = "Velocity", Data = JsonSerializer.SerializeToElement(new { X = 1, Y = 0 }) }
/// };
/// </code>
/// </example>
public sealed record ComponentData
{
    /// <summary>
    /// Gets the component type name (short name like "Position" or full name like "MyGame.Components.Position").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the component field data as a JSON element, or null for default values.
    /// </summary>
    /// <remarks>
    /// When null, the component will be initialized with default field values.
    /// When provided, JSON properties are mapped to component fields by name.
    /// </remarks>
    public JsonElement? Data { get; init; }
}
