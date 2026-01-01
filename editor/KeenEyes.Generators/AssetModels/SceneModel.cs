// Copyright (c) KeenEyes Contributors. Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

namespace KeenEyes.Generators.AssetModels;

/// <summary>
/// Represents a scene definition loaded from a .kescene file.
/// </summary>
internal sealed class SceneModel
{
    /// <summary>
    /// Gets or sets the schema reference for IDE validation.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the scene name (used for method naming).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the entities in the scene.
    /// </summary>
    public List<EntityModel> Entities { get; set; } = [];
}

/// <summary>
/// Represents an entity definition within a scene.
/// </summary>
internal sealed class EntityModel
{
    /// <summary>
    /// Gets or sets the entity's local ID for hierarchy references.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent entity ID (null for root entities).
    /// </summary>
    public string? Parent { get; set; }

    /// <summary>
    /// Gets or sets the components attached to this entity.
    /// Key is the component type name, value is the component data as JSON.
    /// </summary>
    public Dictionary<string, Dictionary<string, object?>> Components { get; set; } = [];
}
