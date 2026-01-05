// Copyright (c) KeenEyes Contributors. Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KeenEyes.Generators.AssetModels;

/// <summary>
/// Provides JSON parsing utilities for asset files.
/// </summary>
internal static class JsonParser
{
    /// <summary>
    /// Parses an asset file (scene or prefab) into a unified SceneModel.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <param name="isPrefab">True if the file is a prefab (.keprefab), false for scene (.kescene).</param>
    /// <returns>The parsed scene model, or null if parsing failed.</returns>
    public static SceneModel? ParseAsset(string json, bool isPrefab)
    {
        if (isPrefab)
        {
            var prefab = ParsePrefab(json);
            return prefab != null ? ConvertPrefabToScene(prefab) : null;
        }
        return ParseScene(json);
    }

    /// <summary>
    /// Converts a PrefabModel to a SceneModel by flattening the hierarchy.
    /// </summary>
    /// <param name="prefab">The prefab model to convert.</param>
    /// <returns>The converted scene model.</returns>
    public static SceneModel ConvertPrefabToScene(PrefabModel prefab)
    {
        var scene = new SceneModel
        {
            Schema = prefab.Schema,
            Name = prefab.Name,
            Version = prefab.Version,
            Base = prefab.Base,
            OverridableFields = [.. prefab.OverridableFields]
        };

        // Flatten hierarchical Root + Children to flat list with parent refs
        if (prefab.Root != null)
        {
            FlattenEntity(prefab.Root, null, scene.Entities);
        }

        foreach (var child in prefab.Children)
        {
            FlattenEntity(child, prefab.Root?.Id, scene.Entities);
        }

        return scene;
    }

    /// <summary>
    /// Recursively flattens a hierarchical prefab entity into a flat entity list.
    /// </summary>
    private static void FlattenEntity(PrefabEntityModel entity, string? parentId, List<EntityModel> entities)
    {
        var flatEntity = new EntityModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Parent = entity.Parent ?? parentId,
            Components = new Dictionary<string, Dictionary<string, object?>>(entity.Components)
        };
        entities.Add(flatEntity);

        // Recursively process nested children
        foreach (var child in entity.Children)
        {
            FlattenEntity(child, entity.Id, entities);
        }
    }

    /// <summary>
    /// Parses a scene from JSON content.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed scene model, or null if parsing failed.</returns>
    public static SceneModel? ParseScene(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var root = document.RootElement;
            var scene = new SceneModel
            {
                Schema = GetStringProperty(root, "$schema"),
                Name = GetStringProperty(root, "name") ?? string.Empty,
                Version = GetIntProperty(root, "version", 1),
                Base = GetStringProperty(root, "base")
            };

            // Parse overridable fields if present
            if (root.TryGetProperty("overridableFields", out var overridesElement) &&
                overridesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var overrideElement in overridesElement.EnumerateArray())
                {
                    if (overrideElement.ValueKind == JsonValueKind.String)
                    {
                        var value = overrideElement.GetString();
                        if (value != null)
                        {
                            scene.OverridableFields.Add(value);
                        }
                    }
                }
            }

            if (root.TryGetProperty("entities", out var entitiesElement) &&
                entitiesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entityElement in entitiesElement.EnumerateArray())
                {
                    scene.Entities.Add(ParseEntity(entityElement));
                }
            }

            return scene;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a prefab from JSON content.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed prefab model, or null if parsing failed.</returns>
    public static PrefabModel? ParsePrefab(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var root = document.RootElement;
            var prefab = new PrefabModel
            {
                Schema = GetStringProperty(root, "$schema"),
                Name = GetStringProperty(root, "name") ?? string.Empty,
                Version = GetIntProperty(root, "version", 1),
                Base = GetStringProperty(root, "base")
            };

            if (root.TryGetProperty("root", out var rootElement))
            {
                prefab.Root = ParsePrefabEntity(rootElement);
            }

            if (root.TryGetProperty("children", out var childrenElement) &&
                childrenElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var childElement in childrenElement.EnumerateArray())
                {
                    prefab.Children.Add(ParsePrefabEntity(childElement));
                }
            }

            if (root.TryGetProperty("overridableFields", out var overridesElement) &&
                overridesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var overrideElement in overridesElement.EnumerateArray())
                {
                    if (overrideElement.ValueKind == JsonValueKind.String)
                    {
                        var value = overrideElement.GetString();
                        if (value != null)
                        {
                            prefab.OverridableFields.Add(value);
                        }
                    }
                }
            }

            return prefab;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a world config from JSON content.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <returns>The parsed world config model, or null if parsing failed.</returns>
    public static WorldConfigModel? ParseWorldConfig(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            var root = document.RootElement;
            var config = new WorldConfigModel
            {
                Schema = GetStringProperty(root, "$schema"),
                Name = GetStringProperty(root, "name") ?? string.Empty,
                Version = GetIntProperty(root, "version", 1)
            };

            if (root.TryGetProperty("settings", out var settingsElement))
            {
                config.Settings = new WorldSettingsModel
                {
                    FixedTimeStep = GetFloatProperty(settingsElement, "fixedTimeStep", 0.02f),
                    MaxDeltaTime = GetFloatProperty(settingsElement, "maxDeltaTime", 0.1f)
                };
            }

            if (root.TryGetProperty("singletons", out var singletonsElement) &&
                singletonsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in singletonsElement.EnumerateObject())
                {
                    config.Singletons[property.Name] = ParseComponentData(property.Value);
                }
            }

            if (root.TryGetProperty("plugins", out var pluginsElement) &&
                pluginsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var pluginElement in pluginsElement.EnumerateArray())
                {
                    if (pluginElement.ValueKind == JsonValueKind.String)
                    {
                        var value = pluginElement.GetString();
                        if (value != null)
                        {
                            config.Plugins.Add(value);
                        }
                    }
                }
            }

            if (root.TryGetProperty("systems", out var systemsElement) &&
                systemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var systemElement in systemsElement.EnumerateArray())
                {
                    config.Systems.Add(new SystemRegistrationModel
                    {
                        Type = GetStringProperty(systemElement, "type") ?? string.Empty,
                        Phase = GetStringProperty(systemElement, "phase") ?? "Update",
                        Order = GetIntProperty(systemElement, "order", 0)
                    });
                }
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private static EntityModel ParseEntity(JsonElement element)
    {
        var entity = new EntityModel
        {
            Id = GetStringProperty(element, "id") ?? string.Empty,
            Name = GetStringProperty(element, "name") ?? string.Empty,
            Parent = GetStringProperty(element, "parent")
        };

        if (element.TryGetProperty("components", out var componentsElement) &&
            componentsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in componentsElement.EnumerateObject())
            {
                entity.Components[property.Name] = ParseComponentData(property.Value);
            }
        }

        // Parse nested children if present (for hierarchical format support)
        if (element.TryGetProperty("children", out var childrenElement) &&
            childrenElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var childElement in childrenElement.EnumerateArray())
            {
                entity.Children.Add(ParseEntity(childElement));
            }
        }

        return entity;
    }

    private static PrefabEntityModel ParsePrefabEntity(JsonElement element)
    {
        var entity = new PrefabEntityModel
        {
            Id = GetStringProperty(element, "id") ?? string.Empty,
            Name = GetStringProperty(element, "name") ?? string.Empty,
            Parent = GetStringProperty(element, "parent")
        };

        if (element.TryGetProperty("components", out var componentsElement) &&
            componentsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in componentsElement.EnumerateObject())
            {
                entity.Components[property.Name] = ParseComponentData(property.Value);
            }
        }

        if (element.TryGetProperty("children", out var childrenElement) &&
            childrenElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var childElement in childrenElement.EnumerateArray())
            {
                entity.Children.Add(ParsePrefabEntity(childElement));
            }
        }

        return entity;
    }

    private static Dictionary<string, object?> ParseComponentData(JsonElement element)
    {
        var data = new Dictionary<string, object?>();

        if (element.ValueKind != JsonValueKind.Object)
        {
            return data;
        }

        foreach (var property in element.EnumerateObject())
        {
            data[property.Name] = ParseValue(property.Value);
        }

        return data;
    }

    private static object? ParseValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
            JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ParseNestedObject(element),
            JsonValueKind.Array => ParseArray(element),
            _ => null
        };
    }

    private static Dictionary<string, object?> ParseNestedObject(JsonElement element)
    {
        var obj = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            obj[property.Name] = ParseValue(property.Value);
        }
        return obj;
    }

    private static List<object?> ParseArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(ParseValue(item));
        }
        return list;
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static int GetIntProperty(JsonElement element, string name, int defaultValue)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }
        return defaultValue;
    }

    private static float GetFloatProperty(JsonElement element, string name, float defaultValue)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return (float)value.GetDouble();
        }
        return defaultValue;
    }
}
