using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Assets;

/// <summary>
/// Serializes and deserializes scene files (.kescene).
/// </summary>
public sealed class SceneSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Saves a world to a .kescene file.
    /// </summary>
    /// <param name="world">The world to save.</param>
    /// <param name="sceneName">The scene name.</param>
    /// <param name="filePath">The file path to save to.</param>
    public void Save(World world, string sceneName, string filePath)
    {
        var sceneData = CaptureScene(world, sceneName);
        var json = JsonSerializer.Serialize(sceneData, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a .kescene file into a world.
    /// </summary>
    /// <param name="world">The world to load into.</param>
    /// <param name="filePath">The file path to load from.</param>
    /// <returns>The scene name.</returns>
    public string Load(World world, string filePath)
    {
        var json = File.ReadAllText(filePath);
        var sceneData = JsonSerializer.Deserialize<SceneData>(json, JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse scene file: {filePath}");

        RestoreScene(world, sceneData);
        return sceneData.Name;
    }

    /// <summary>
    /// Captures the current world state as scene data.
    /// </summary>
    /// <param name="world">The world to capture.</param>
    /// <param name="sceneName">The scene name.</param>
    /// <returns>The captured scene data.</returns>
    public SceneData CaptureScene(World world, string sceneName)
    {
        var entities = new List<EntityData>();
        var entityIdMap = new Dictionary<Entity, string>();

        // First pass: assign IDs to all entities
        var index = 0;
        foreach (var entity in world.GetAllEntities())
        {
            var name = world.GetName(entity);
            var id = name ?? $"entity_{index}";
            entityIdMap[entity] = id;
            index++;
        }

        // Second pass: capture entity data
        foreach (var entity in world.GetAllEntities())
        {
            var name = world.GetName(entity);
            var parent = world.GetParent(entity);
            string? parentId = null;

            if (parent.IsValid && entityIdMap.TryGetValue(parent, out var pid))
            {
                parentId = pid;
            }

            var entityData = new EntityData
            {
                Id = entityIdMap[entity],
                Name = name,
                Parent = parentId,
                Components = CaptureComponents(world, entity)
            };

            entities.Add(entityData);
        }

        return new SceneData
        {
            Name = sceneName,
            Version = 1,
            Entities = entities
        };
    }

    /// <summary>
    /// Restores scene data into a world.
    /// </summary>
    /// <param name="world">The world to restore into.</param>
    /// <param name="sceneData">The scene data to restore.</param>
    public void RestoreScene(World world, SceneData sceneData)
    {
        // Clear existing entities
        foreach (var entity in world.GetAllEntities().ToList())
        {
            world.Despawn(entity);
        }

        var entityMap = new Dictionary<string, Entity>();

        // First pass: create all entities
        foreach (var entityData in sceneData.Entities)
        {
            var builder = world.Spawn(entityData.Name);
            // Note: Component restoration would go here with reflection or a factory
            var entity = builder.Build();
            entityMap[entityData.Id] = entity;
        }

        // Second pass: set up hierarchy
        foreach (var entityData in sceneData.Entities)
        {
            if (entityData.Parent is not null && entityMap.TryGetValue(entityData.Parent, out var parent))
            {
                var entity = entityMap[entityData.Id];
                world.SetParent(entity, parent);
            }
        }

        // Note: Component data restoration would require a component factory
        // that can deserialize JSON to the appropriate component types.
        // This is a placeholder that just creates entities with names and hierarchy.
    }

    private static Dictionary<string, JsonElement> CaptureComponents(World world, Entity entity)
    {
        // Note: Full component serialization requires reflection or a registry
        // of component types with serializers. For now, return empty.
        // In a complete implementation, this would:
        // 1. Get all component types on the entity
        // 2. Serialize each component's data to JSON
        _ = world;
        _ = entity;
        return [];
    }
}
