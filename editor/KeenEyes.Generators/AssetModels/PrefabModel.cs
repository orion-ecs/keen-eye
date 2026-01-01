// Copyright (c) KeenEyes Contributors. Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

namespace KeenEyes.Generators.AssetModels;

/// <summary>
/// Represents a prefab definition loaded from a .keprefab file.
/// </summary>
internal sealed class PrefabModel
{
    /// <summary>
    /// Gets or sets the schema reference for IDE validation.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the prefab name (used for method naming).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the format version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the base prefab name for inheritance (null if no base).
    /// </summary>
    public string? Base { get; set; }

    /// <summary>
    /// Gets or sets the root entity definition.
    /// </summary>
    public PrefabEntityModel? Root { get; set; }

    /// <summary>
    /// Gets or sets the child entity definitions.
    /// </summary>
    public List<PrefabEntityModel> Children { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of overridable field paths (e.g., "Transform.Position").
    /// These become optional parameters in the generated spawn method.
    /// </summary>
    public List<string> OverridableFields { get; set; } = [];
}

/// <summary>
/// Represents an entity definition within a prefab.
/// </summary>
internal sealed class PrefabEntityModel
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
    /// Gets or sets the parent entity ID (null for root or direct children of root).
    /// </summary>
    public string? Parent { get; set; }

    /// <summary>
    /// Gets or sets the components attached to this entity.
    /// Key is the component type name, value is the component data.
    /// </summary>
    public Dictionary<string, Dictionary<string, object?>> Components { get; set; } = [];

    /// <summary>
    /// Gets or sets nested children of this entity.
    /// </summary>
    public List<PrefabEntityModel> Children { get; set; } = [];
}
