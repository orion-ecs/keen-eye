using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Assets;

/// <summary>
/// Data model for a .kescene file.
/// </summary>
public sealed class SceneData
{
    /// <summary>
    /// Gets or sets the JSON schema reference.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; } = "../schemas/kescene.schema.json";

    /// <summary>
    /// Gets or sets the scene name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the scene format version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the entities in the scene.
    /// </summary>
    public List<EntityData> Entities { get; set; } = [];
}

/// <summary>
/// Data model for an entity in a scene file.
/// </summary>
public sealed class EntityData
{
    /// <summary>
    /// Gets or sets the unique entity ID within the scene.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parent entity ID.
    /// </summary>
    public string? Parent { get; set; }

    /// <summary>
    /// Gets or sets the component data.
    /// Key is component type name, value is the serialized component data.
    /// </summary>
    public Dictionary<string, JsonElement> Components { get; set; } = [];
}
