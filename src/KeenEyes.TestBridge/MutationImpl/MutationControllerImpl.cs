using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using KeenEyes.TestBridge.Mutation;

namespace KeenEyes.TestBridge.MutationImpl;

/// <summary>
/// In-process implementation of <see cref="IMutationController"/> for runtime world manipulation.
/// </summary>
/// <remarks>
/// <para>
/// This controller uses reflection to deserialize JSON component data to struct types.
/// This is acceptable for debug/tooling infrastructure where AOT compatibility is
/// not required for all operations.
/// </para>
/// </remarks>
[UnconditionalSuppressMessage("AOT", "IL2070:Target parameter of method has annotations that do not match target.",
    Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
[UnconditionalSuppressMessage("AOT", "IL2072:Target parameter of method has annotations that do not match target.",
    Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
[UnconditionalSuppressMessage("AOT", "IL2067:Target parameter of method has annotations that do not match target.",
    Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
internal sealed class MutationControllerImpl(World world) : IMutationController
{
    #region Entity Management

    public Task<EntityResult> SpawnAsync(
        string? name = null,
        IReadOnlyList<ComponentData>? components = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var builder = world.Spawn(name);

            if (components != null)
            {
                foreach (var componentData in components)
                {
                    if (!AddComponentToBuilder(builder, componentData, out var error))
                    {
                        return Task.FromResult(EntityResult.Fail(error));
                    }
                }
            }

            var entity = builder.Build();
            return Task.FromResult(EntityResult.Ok(entity.Id, entity.Version));
        }
        catch (Exception ex)
        {
            return Task.FromResult(EntityResult.Fail(ex.Message));
        }
    }

    public Task<bool> DespawnAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = FindEntityById(entityId);
        if (!entity.IsValid || !world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(world.Despawn(entity));
    }

    public Task<EntityResult> CloneAsync(
        int entityId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source = FindEntityById(entityId);
            if (!source.IsValid || !world.IsAlive(source))
            {
                return Task.FromResult(EntityResult.Fail($"Entity with ID {entityId} not found"));
            }

            // Get all components from source
            var components = world.GetComponents(source).ToList();

            // Create new entity with same components
            var builder = world.Spawn(name);
            foreach (var (type, value) in components)
            {
                var info = world.Components.Get(type);
                if (info != null)
                {
                    builder.WithBoxed(info, value);
                }
            }
            var clone = builder.Build();

            // Copy string tags
            foreach (var tag in world.GetTags(source))
            {
                world.AddTag(clone, tag);
            }

            // Note: Parent-child relationships are NOT copied (intentional)

            return Task.FromResult(EntityResult.Ok(clone.Id, clone.Version));
        }
        catch (Exception ex)
        {
            return Task.FromResult(EntityResult.Fail(ex.Message));
        }
    }

    public Task<bool> SetNameAsync(int entityId, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            world.SetName(entity, name);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> ClearNameAsync(int entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            world.SetName(entity, null);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Hierarchy

    public Task<bool> SetParentAsync(int entityId, int? parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            if (parentId.HasValue)
            {
                var parent = FindEntityById(parentId.Value);
                if (!parent.IsValid || !world.IsAlive(parent))
                {
                    return Task.FromResult(false);
                }
                world.SetParent(entity, parent);
            }
            else
            {
                // Clear parent - make root entity
                world.SetParent(entity, Entity.Null);
            }

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<int>> GetRootEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var roots = new List<int>();

        foreach (var entity in world.GetAllEntities())
        {
            var parent = world.GetParent(entity);
            if (!parent.IsValid)
            {
                roots.Add(entity.Id);
            }
        }

        return Task.FromResult<IReadOnlyList<int>>(roots);
    }

    #endregion

    #region Components

    public Task<bool> AddComponentAsync(
        int entityId,
        string componentType,
        JsonElement? data = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            var info = world.Components.GetByName(componentType);
            if (info == null)
            {
                return Task.FromResult(false);
            }

            // Check if entity already has this component
            if (world.HasComponent(entity, info.Type))
            {
                return Task.FromResult(false);
            }

            // Create component instance
            var component = DeserializeComponent(info.Type, data);
            world.SetComponent(entity, info.Type, component);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> RemoveComponentAsync(
        int entityId,
        string componentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            var info = world.Components.GetByName(componentType);
            if (info == null)
            {
                return Task.FromResult(false);
            }

            // Check if entity has this component
            if (!world.HasComponent(entity, info.Type))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(world.RemoveComponent(entity, info.Type));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> SetComponentAsync(
        int entityId,
        string componentType,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            var info = world.Components.GetByName(componentType);
            if (info == null)
            {
                return Task.FromResult(false);
            }

            // Check if entity has this component
            if (!world.HasComponent(entity, info.Type))
            {
                return Task.FromResult(false);
            }

            // Create component instance from JSON
            var component = DeserializeComponent(info.Type, data);
            world.SetComponent(entity, info.Type, component);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> SetFieldAsync(
        int entityId,
        string componentType,
        string fieldName,
        JsonElement value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            var info = world.Components.GetByName(componentType);
            if (info == null)
            {
                return Task.FromResult(false);
            }

            // Check if entity has this component
            if (!world.HasComponent(entity, info.Type))
            {
                return Task.FromResult(false);
            }

            // Get current component value
            var currentValue = GetComponentValue(entity, info.Type);
            if (currentValue == null)
            {
                return Task.FromResult(false);
            }

            // Set the field
            if (!SetFieldValue(info.Type, currentValue, fieldName, value))
            {
                return Task.FromResult(false);
            }

            // Write back to world
            world.SetComponent(entity, info.Type, currentValue);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Tags

    public Task<bool> AddTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(world.AddTag(entity, tag));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> RemoveTagAsync(int entityId, string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = FindEntityById(entityId);
            if (!entity.IsValid || !world.IsAlive(entity))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(world.RemoveTag(entity, tag));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<string>> GetAllTagsAsync(CancellationToken cancellationToken = default)
    {
        var allTags = new HashSet<string>();

        foreach (var entity in world.GetAllEntities())
        {
            foreach (var tag in world.GetTags(entity))
            {
                allTags.Add(tag);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(allTags.ToList());
    }

    #endregion

    #region Private Helpers

    private Entity FindEntityById(int entityId)
    {
        foreach (var entity in world.GetAllEntities())
        {
            if (entity.Id == entityId)
            {
                return entity;
            }
        }

        return Entity.Null;
    }

    private bool AddComponentToBuilder(EntityBuilder builder, ComponentData componentData, out string error)
    {
        error = string.Empty;

        var info = world.Components.GetByName(componentData.Type);
        if (info == null)
        {
            error = $"Component type '{componentData.Type}' is not registered";
            return false;
        }

        try
        {
            var component = DeserializeComponent(info.Type, componentData.Data);
            builder.WithBoxed(info, component);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to deserialize component '{componentData.Type}': {ex.Message}";
            return false;
        }
    }

    private object? GetComponentValue(Entity entity, Type componentType)
    {
        foreach (var (type, value) in world.GetComponents(entity))
        {
            if (type == componentType)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Deserializes a component from JSON data.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072:Target parameter of method has annotations that do not match target.",
        Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
    [UnconditionalSuppressMessage("AOT", "IL2026:Members with RequiresUnreferencedCodeAttribute are used.",
        Justification = "TestBridge is debug infrastructure - reflection and dynamic serialization are acceptable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Members with RequiresDynamicCodeAttribute are used.",
        Justification = "TestBridge is debug infrastructure - dynamic code generation is acceptable.")]
    private static object DeserializeComponent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type componentType,
        JsonElement? json)
    {
        // Create default instance
        var instance = Activator.CreateInstance(componentType)!;

        if (!json.HasValue || json.Value.ValueKind == JsonValueKind.Null)
        {
            return instance;
        }

        // For each JSON property, find matching field and set it
        foreach (var property in json.Value.EnumerateObject())
        {
            var field = componentType.GetField(
                property.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                var value = ConvertJsonValue(property.Value, field.FieldType);
                field.SetValue(instance, value);
            }
        }

        return instance;
    }

    /// <summary>
    /// Sets a single field on a component instance.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072:Target parameter of method has annotations that do not match target.",
        Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
    private static bool SetFieldValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        Type componentType,
        object instance,
        string fieldName,
        JsonElement value)
    {
        var field = componentType.GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (field == null)
        {
            return false;
        }

        var convertedValue = ConvertJsonValue(value, field.FieldType);
        field.SetValue(instance, convertedValue);
        return true;
    }

    /// <summary>
    /// Converts a JSON value to the specified target type.
    /// </summary>
    private static object? ConvertJsonValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Primitive types
        if (targetType == typeof(int))
        {
            return element.GetInt32();
        }

        if (targetType == typeof(float))
        {
            return element.GetSingle();
        }

        if (targetType == typeof(double))
        {
            return element.GetDouble();
        }

        if (targetType == typeof(bool))
        {
            return element.GetBoolean();
        }

        if (targetType == typeof(string))
        {
            return element.GetString();
        }

        if (targetType == typeof(long))
        {
            return element.GetInt64();
        }

        if (targetType == typeof(short))
        {
            return element.GetInt16();
        }

        if (targetType == typeof(byte))
        {
            return element.GetByte();
        }

        if (targetType == typeof(uint))
        {
            return element.GetUInt32();
        }

        if (targetType == typeof(ulong))
        {
            return element.GetUInt64();
        }

        // Handle enums
        if (targetType.IsEnum)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return Enum.Parse(targetType, element.GetString()!, ignoreCase: true);
            }
            else if (element.ValueKind == JsonValueKind.Number)
            {
                return Enum.ToObject(targetType, element.GetInt32());
            }
        }

        // Handle common vector types by convention (X, Y, Z, W fields)
        // This supports System.Numerics vectors and custom vector types
        if (element.ValueKind == JsonValueKind.Object)
        {
            return DeserializeObjectType(targetType, element);
        }

        throw new NotSupportedException($"Type {targetType.Name} is not supported for JSON deserialization");
    }

    /// <summary>
    /// Deserializes an object type (struct or class) from JSON.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072:Target parameter of method has annotations that do not match target.",
        Justification = "TestBridge is debug infrastructure - reflection is acceptable.")]
    [UnconditionalSuppressMessage("AOT", "IL2026:Members with RequiresUnreferencedCodeAttribute are used.",
        Justification = "TestBridge is debug infrastructure.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Members with RequiresDynamicCodeAttribute are used.",
        Justification = "TestBridge is debug infrastructure.")]
    private static object DeserializeObjectType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type targetType,
        JsonElement element)
    {
        var instance = Activator.CreateInstance(targetType)!;

        foreach (var property in element.EnumerateObject())
        {
            var field = targetType.GetField(
                property.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                var value = ConvertJsonValue(property.Value, field.FieldType);
                field.SetValue(instance, value);
            }
        }

        return instance;
    }

    #endregion
}
