using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using KeenEyes.Editor.Common.Inspector;
using KeenEyes.Scenes;

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
    /// Scene component types that should be excluded from serialization.
    /// These are runtime-only components managed by SceneManager.
    /// </summary>
    private static readonly HashSet<Type> ExcludedComponentTypes =
    [
        typeof(SceneMembership),
        typeof(SceneRootTag),
        typeof(SceneMetadata),
        typeof(PersistentTag)
    ];

    /// <summary>
    /// Saves a world to a .kescene file.
    /// </summary>
    /// <param name="world">The world to save.</param>
    /// <param name="sceneName">The scene name.</param>
    /// <param name="filePath">The file path to save to.</param>
    public void Save(World world, string sceneName, string filePath)
    {
        var sceneData = CaptureScene(world, sceneName);

        // Serialize straight to the file stream; a 5k-entity scene produces a multi-megabyte
        // document, and materializing it as a string first doubles the transient allocations.
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, sceneData, JsonOptions);
    }

    /// <summary>
    /// Loads a .kescene file into a world.
    /// </summary>
    /// <param name="world">The world to load into.</param>
    /// <param name="filePath">The file path to load from.</param>
    /// <returns>The scene name.</returns>
    public static string Load(World world, string filePath)
    {
        var sceneData = ReadSceneFile(filePath);
        RestoreScene(world, sceneData);
        return sceneData.Name;
    }

    /// <summary>
    /// Loads a .kescene file into a world, associating entities with a scene root.
    /// </summary>
    /// <param name="world">The world to load into.</param>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="sceneRoot">The scene root entity to associate restored entities with.</param>
    /// <returns>The scene name.</returns>
    public static string Load(World world, string filePath, Entity sceneRoot)
    {
        var sceneData = ReadSceneFile(filePath);
        RestoreScene(world, sceneData, sceneRoot);
        return sceneData.Name;
    }

    /// <summary>
    /// Reads and parses a scene file directly from its stream, avoiding a transient
    /// whole-file string allocation.
    /// </summary>
    private static SceneData ReadSceneFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<SceneData>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse scene file: {filePath}");
    }

    /// <summary>
    /// Captures the current world state as scene data.
    /// </summary>
    /// <param name="world">The world to capture.</param>
    /// <param name="sceneName">The scene name.</param>
    /// <returns>The captured scene data.</returns>
    public SceneData CaptureScene(World world, string sceneName)
    {
        return CaptureSceneCore(world, sceneName, Entity.Null);
    }

    /// <summary>
    /// Captures entities belonging to a specific scene root.
    /// </summary>
    /// <param name="world">The world to capture from.</param>
    /// <param name="sceneRoot">The scene root entity. Only entities with SceneMembership
    /// referencing this scene root will be captured.</param>
    /// <returns>The captured scene data.</returns>
    public SceneData CaptureScene(World world, Entity sceneRoot)
    {
        if (!world.IsAlive(sceneRoot))
        {
            throw new ArgumentException("Scene root entity is not alive.", nameof(sceneRoot));
        }

        // Get scene name from SceneMetadata if available
        string sceneName = "Scene";
        if (world.Has<SceneMetadata>(sceneRoot))
        {
            ref readonly var metadata = ref world.Get<SceneMetadata>(sceneRoot);
            sceneName = metadata.Name;
        }

        return CaptureSceneCore(world, sceneName, sceneRoot);
    }

    private SceneData CaptureSceneCore(World world, string sceneName, Entity sceneRoot)
    {
        var entities = new List<EntityData>();
        var entityIdMap = new Dictionary<Entity, string>();
        var captureFieldCache = new Dictionary<Type, (FieldInfo Field, string JsonName)[]>();

        // Determine which entities to capture
        var entitiesToCapture = GetEntitiesToCapture(world, sceneRoot);

        // First pass: assign IDs to all entities
        var index = 0;
        foreach (var entity in entitiesToCapture)
        {
            var name = world.GetName(entity);
            var id = name ?? $"entity_{index}";
            entityIdMap[entity] = id;
            index++;
        }

        // Second pass: capture entity data
        foreach (var entity in entitiesToCapture)
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
                Components = CaptureComponents(world, entity, captureFieldCache)
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
    /// Gets the entities to capture based on scene root filter.
    /// </summary>
    private static IEnumerable<Entity> GetEntitiesToCapture(World world, Entity sceneRoot)
    {
        if (!sceneRoot.IsValid)
        {
            // No scene root specified - capture all entities except scene infrastructure
            foreach (var entity in world.GetAllEntities())
            {
                // Skip scene root entities
                if (world.Has<SceneRootTag>(entity))
                {
                    continue;
                }

                yield return entity;
            }
        }
        else
        {
            // Capture only entities belonging to this scene
            foreach (var entity in world.GetAllEntities())
            {
                // Skip scene root entities
                if (world.Has<SceneRootTag>(entity))
                {
                    continue;
                }

                // Check if entity belongs to this scene
                if (world.Has<SceneMembership>(entity))
                {
                    ref readonly var membership = ref world.Get<SceneMembership>(entity);
                    if (membership.OriginScene.Id == sceneRoot.Id)
                    {
                        yield return entity;
                    }
                }
                else
                {
                    // Entities without SceneMembership are included (backward compatibility)
                    yield return entity;
                }
            }
        }
    }

    /// <summary>
    /// Restores scene data into a world.
    /// </summary>
    /// <param name="world">The world to restore into.</param>
    /// <param name="sceneData">The scene data to restore.</param>
    public static void RestoreScene(World world, SceneData sceneData)
    {
        RestoreSceneCore(world, sceneData, Entity.Null);
    }

    /// <summary>
    /// Restores scene data into a world, associating entities with a scene root.
    /// </summary>
    /// <param name="world">The world to restore into.</param>
    /// <param name="sceneData">The scene data to restore.</param>
    /// <param name="sceneRoot">The scene root entity to associate restored entities with.</param>
    public static void RestoreScene(World world, SceneData sceneData, Entity sceneRoot)
    {
        RestoreSceneCore(world, sceneData, sceneRoot);
    }

    private static void RestoreSceneCore(World world, SceneData sceneData, Entity sceneRoot)
    {
        // If no scene root, clear existing entities (legacy behavior)
        if (!sceneRoot.IsValid)
        {
            foreach (var entity in world.GetAllEntities().ToList())
            {
                world.Despawn(entity);
            }
        }

        var entityMap = new Dictionary<string, Entity>();

        // Component type resolution and field metadata are identical for every entity in the
        // scene, so resolve each component type once per restore rather than once per entity.
        var restoreCache = new Dictionary<string, ComponentRestoreInfo?>();

        // First pass: create all entities with their components
        foreach (var entityData in sceneData.Entities)
        {
            var builder = world.Spawn(entityData.Name);

            // Restore components using reflection-based deserialization
            RestoreComponents(builder, entityData.Components, world, restoreCache);

            var entity = builder.Build();
            entityMap[entityData.Id] = entity;

            // Associate with scene if scene root is provided
            if (sceneRoot.IsValid && world.IsAlive(sceneRoot))
            {
                world.Scenes.AddToScene(entity, sceneRoot);
            }
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
    }

    private static Dictionary<string, JsonElement> CaptureComponents(
        World world,
        Entity entity,
        Dictionary<Type, (FieldInfo Field, string JsonName)[]> fieldCache)
    {
        var result = new Dictionary<string, JsonElement>();

        foreach (var (type, value) in world.GetComponents(entity))
        {
            // Skip scene-related runtime components
            if (ExcludedComponentTypes.Contains(type))
            {
                continue;
            }

            var typeName = type.FullName ?? type.Name;
            var componentData = new Dictionary<string, object?>();

            // Use ComponentIntrospector to get all editable fields
            // Use camelCase for field names to match JSON deserialization expectations
            foreach (var (field, jsonFieldName) in GetCaptureFields(type, fieldCache))
            {
                var fieldValue = ComponentIntrospector.GetFieldValue(value, field);
                componentData[jsonFieldName] = SerializeFieldValue(fieldValue);
            }

            // Serialize into a pooled UTF-8 buffer and parse in place; this skips the
            // intermediate JSON string + reparse the previous implementation performed
            // for every component instance.
            result[typeName] = JsonSerializer.SerializeToElement(componentData, JsonOptions);
        }

        return result;
    }

    /// <summary>
    /// Gets the editable fields of a component type paired with their camelCase JSON names,
    /// computing them once per type per capture.
    /// </summary>
    private static (FieldInfo Field, string JsonName)[] GetCaptureFields(
        Type type,
        Dictionary<Type, (FieldInfo Field, string JsonName)[]> fieldCache)
    {
        if (!fieldCache.TryGetValue(type, out var fields))
        {
            fields = ComponentIntrospector.GetEditableFields(type)
                .Select(field => (field, GetJsonPropertyName(field.Name)))
                .ToArray();
            fieldCache[type] = fields;
        }

        return fields;
    }

    /// <summary>
    /// Serializes a field value to a JSON-compatible representation.
    /// </summary>
    private static object? SerializeFieldValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();

        // Handle primitives and strings directly
        if (type.IsPrimitive || value is string || value is decimal)
        {
            return value;
        }

        // Handle enums as their string name
        if (type.IsEnum)
        {
            return value.ToString();
        }

        // Handle common vector types from System.Numerics
        if (value is System.Numerics.Vector2 v2)
        {
            return new { X = v2.X, Y = v2.Y };
        }
        if (value is System.Numerics.Vector3 v3)
        {
            return new { X = v3.X, Y = v3.Y, Z = v3.Z };
        }
        if (value is System.Numerics.Vector4 v4)
        {
            return new { X = v4.X, Y = v4.Y, Z = v4.Z, W = v4.W };
        }
        if (value is System.Numerics.Quaternion q)
        {
            return new { X = q.X, Y = q.Y, Z = q.Z, W = q.W };
        }

        // Handle Entity references
        if (value is Entity entity)
        {
            return new { Id = entity.Id, Version = entity.Version };
        }

        // Handle arrays and collections
        if (type.IsArray || ComponentIntrospector.IsCollectionType(type))
        {
            var list = new List<object?>();
            if (value is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    list.Add(SerializeFieldValue(item));
                }
            }
            return list;
        }

        // Handle nested structs/objects
        if (type.IsValueType || type.IsClass)
        {
            // Use camelCase keys to match the deserialization lookup, which resolves nested
            // fields via GetJsonPropertyName(field.Name). Writing raw PascalCase keys here
            // would make every nested-struct field miss on load and silently reset to default.
            var nested = new Dictionary<string, object?>();
            foreach (var field in ComponentIntrospector.GetEditableFields(type))
            {
                var fieldValue = ComponentIntrospector.GetFieldValue(value, field);
                nested[GetJsonPropertyName(field.Name)] = SerializeFieldValue(fieldValue);
            }
            return nested;
        }

        return value.ToString();
    }

    /// <summary>
    /// Cached per-restore metadata for a component type: its registry info and the editable
    /// fields paired with their camelCase JSON property names.
    /// </summary>
    private sealed record ComponentRestoreInfo(
        Type Type,
        ComponentInfo Info,
        (FieldInfo Field, string JsonName)[] Fields);

    /// <summary>
    /// Restores components on an entity from serialized data.
    /// </summary>
    private static void RestoreComponents(
        EntityBuilder builder,
        Dictionary<string, JsonElement> components,
        World world,
        Dictionary<string, ComponentRestoreInfo?> restoreCache)
    {
        foreach (var (typeName, jsonElement) in components)
        {
            if (!restoreCache.TryGetValue(typeName, out var restoreInfo))
            {
                restoreInfo = BuildRestoreInfo(world, typeName);
                restoreCache[typeName] = restoreInfo;
            }

            if (restoreInfo is null)
            {
                // Type not found or failed to register - skip this component
                continue;
            }

            // Skip tag components (no data to restore)
            if (restoreInfo.Info.IsTag)
            {
                // For tags, just add without data
                builder.WithBoxed(restoreInfo.Info, Activator.CreateInstance(restoreInfo.Type)!);
                continue;
            }

            // Create a new instance of the component
            var component = Activator.CreateInstance(restoreInfo.Type)!;

            // Restore field values
            foreach (var (field, jsonName) in restoreInfo.Fields)
            {
                if (jsonElement.TryGetProperty(jsonName, out var fieldElement))
                {
                    var fieldValue = DeserializeFieldValue(fieldElement, field.FieldType);
                    if (fieldValue is not null)
                    {
                        ComponentIntrospector.SetFieldValue(ref component, field, fieldValue);
                    }
                }
            }

            // Add the component to the builder
            builder.WithBoxed(restoreInfo.Info, component);
        }
    }

    /// <summary>
    /// Resolves a serialized component type name to its type, registry info, and editable
    /// field metadata. Returns null when the type cannot be found or registered.
    /// </summary>
    private static ComponentRestoreInfo? BuildRestoreInfo(World world, string typeName)
    {
        // Try to find the component type
        var componentType = FindComponentType(world, typeName);
        if (componentType is null)
        {
            return null;
        }

        // Get component info from registry, registering if needed (editor-only reflection)
        var info = world.Components.Get(componentType) ?? RegisterComponentType(world, componentType);
        if (info is null)
        {
            return null;
        }

        (FieldInfo Field, string JsonName)[] fields = info.IsTag
            ? []
            : ComponentIntrospector.GetEditableFields(componentType)
                .Select(field => (field, GetJsonPropertyName(field.Name)))
                .ToArray();

        return new ComponentRestoreInfo(componentType, info, fields);
    }

    /// <summary>
    /// Registers a component type using reflection.
    /// This is editor-only code that allows dynamic component registration.
    /// </summary>
    private static ComponentInfo? RegisterComponentType(World world, Type componentType)
    {
        try
        {
            // Get the generic Register<T> method
            var registerMethod = typeof(ComponentRegistry).GetMethod("Register")
                ?? throw new InvalidOperationException("Register method not found");

            // Make it generic with our component type
            var genericMethod = registerMethod.MakeGenericMethod(componentType);

            // Call it with isTag = false (auto-detected from ITagComponent)
            return genericMethod.Invoke(world.Components, [false]) as ComponentInfo;
        }
        catch (Exception ex) when (
            ex is ArgumentException or InvalidOperationException or MemberAccessException or TargetInvocationException)
        {
            // Registration failed (type may not be a valid component)
            return null;
        }
    }

    /// <summary>
    /// Finds a component type by its full name.
    /// </summary>
    private static Type? FindComponentType(World world, string typeName)
    {
        // First, check registered components
        foreach (var info in world.Components.All)
        {
            var fullName = info.Type.FullName ?? info.Type.Name;
            if (fullName == typeName || info.Type.Name == typeName)
            {
                return info.Type;
            }
        }

        // Try to resolve by Type.GetType (works for assembly-qualified names)
        var type = Type.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        // Search all loaded assemblies for the type
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip dynamic assemblies
            if (assembly.IsDynamic)
            {
                continue;
            }

            type = assembly.GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a property name to its JSON camelCase equivalent.
    /// </summary>
    private static string GetJsonPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return propertyName;
        }
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    /// <summary>
    /// Deserializes a field value from a JsonElement.
    /// </summary>
    private static object? DeserializeFieldValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Handle primitives
        if (targetType == typeof(bool))
        {
            return element.GetBoolean();
        }
        if (targetType == typeof(int))
        {
            return element.GetInt32();
        }
        if (targetType == typeof(long))
        {
            return element.GetInt64();
        }
        if (targetType == typeof(float))
        {
            return element.GetSingle();
        }
        if (targetType == typeof(double))
        {
            return element.GetDouble();
        }
        if (targetType == typeof(string))
        {
            return element.GetString();
        }
        if (targetType == typeof(decimal))
        {
            return element.GetDecimal();
        }
        if (targetType == typeof(byte))
        {
            return (byte)element.GetInt32();
        }
        if (targetType == typeof(short))
        {
            return (short)element.GetInt32();
        }

        // Handle enums
        if (targetType.IsEnum)
        {
            var enumString = element.GetString();
            if (enumString is not null && Enum.TryParse(targetType, enumString, out var enumValue))
            {
                return enumValue;
            }
            return Activator.CreateInstance(targetType);
        }

        // Handle common vector types
        if (targetType == typeof(System.Numerics.Vector2))
        {
            var x = element.GetProperty("x").GetSingle();
            var y = element.GetProperty("y").GetSingle();
            return new System.Numerics.Vector2(x, y);
        }
        if (targetType == typeof(System.Numerics.Vector3))
        {
            var x = element.GetProperty("x").GetSingle();
            var y = element.GetProperty("y").GetSingle();
            var z = element.GetProperty("z").GetSingle();
            return new System.Numerics.Vector3(x, y, z);
        }
        if (targetType == typeof(System.Numerics.Vector4))
        {
            var x = element.GetProperty("x").GetSingle();
            var y = element.GetProperty("y").GetSingle();
            var z = element.GetProperty("z").GetSingle();
            var w = element.GetProperty("w").GetSingle();
            return new System.Numerics.Vector4(x, y, z, w);
        }
        if (targetType == typeof(System.Numerics.Quaternion))
        {
            var x = element.GetProperty("x").GetSingle();
            var y = element.GetProperty("y").GetSingle();
            var z = element.GetProperty("z").GetSingle();
            var w = element.GetProperty("w").GetSingle();
            return new System.Numerics.Quaternion(x, y, z, w);
        }

        // Handle Entity references
        if (targetType == typeof(Entity))
        {
            var id = element.GetProperty("id").GetInt32();
            var version = element.GetProperty("version").GetInt32();
            return new Entity(id, version);
        }

        // Handle arrays
        if (targetType.IsArray && element.ValueKind == JsonValueKind.Array)
        {
            var elementType = targetType.GetElementType()!;
            var list = new List<object?>();
            foreach (var item in element.EnumerateArray())
            {
                list.Add(DeserializeFieldValue(item, elementType));
            }
            var array = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                array.SetValue(list[i], i);
            }
            return array;
        }

        // Handle generic List<T>
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>) &&
            element.ValueKind == JsonValueKind.Array)
        {
            var elementType = targetType.GetGenericArguments()[0];
            var list = (System.Collections.IList)Activator.CreateInstance(targetType)!;
            foreach (var item in element.EnumerateArray())
            {
                list.Add(DeserializeFieldValue(item, elementType));
            }
            return list;
        }

        // Handle nested structs/objects
        if ((targetType.IsValueType || targetType.IsClass) && element.ValueKind == JsonValueKind.Object)
        {
            var instance = Activator.CreateInstance(targetType)!;
            foreach (var field in ComponentIntrospector.GetEditableFields(targetType))
            {
                if (element.TryGetProperty(GetJsonPropertyName(field.Name), out var fieldElement))
                {
                    var fieldValue = DeserializeFieldValue(fieldElement, field.FieldType);
                    if (fieldValue is not null)
                    {
                        var boxed = instance;
                        ComponentIntrospector.SetFieldValue(ref boxed, field, fieldValue);
                        instance = boxed;
                    }
                }
            }
            return instance;
        }

        return null;
    }
}
