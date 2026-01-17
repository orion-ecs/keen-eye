using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using KeenEyes.TestBridge.Snapshot;

namespace KeenEyes.TestBridge.SnapshotImpl;

/// <summary>
/// In-process implementation of <see cref="ISnapshotController"/> for world state snapshots.
/// </summary>
/// <remarks>
/// <para>
/// Uses reflection to capture and restore component data. This is acceptable for
/// debugging/testing scenarios but is not AOT-compatible.
/// </para>
/// <para>
/// Snapshots are stored in memory and can be persisted to files as JSON.
/// </para>
/// </remarks>
internal sealed class SnapshotControllerImpl(World world) : ISnapshotController
{
    private const string QuickSaveName = "__quicksave__";
    private const int MaxSnapshots = 100;

    private readonly ConcurrentDictionary<string, InMemorySnapshot> snapshots = new();

    public Task<SnapshotResult> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(SnapshotResult.Fail("Snapshot name cannot be empty."));
        }

        try
        {
            // Check snapshot limit
            if (snapshots.Count >= MaxSnapshots && !snapshots.ContainsKey(name))
            {
                return Task.FromResult(SnapshotResult.Fail($"Maximum snapshot limit ({MaxSnapshots}) reached. Delete some snapshots first."));
            }

            var snapshot = CaptureSnapshot(name);
            snapshots[name] = snapshot;

            var info = CreateSnapshotInfo(name, snapshot);
            return Task.FromResult(SnapshotResult.Ok(name, info));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapshotResult.Fail($"Failed to create snapshot: {ex.Message}"));
        }
    }

    public Task<SnapshotResult> RestoreAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(SnapshotResult.Fail("Snapshot name cannot be empty."));
        }

        if (!snapshots.TryGetValue(name, out var snapshot))
        {
            return Task.FromResult(SnapshotResult.Fail($"Snapshot '{name}' not found."));
        }

        try
        {
            RestoreSnapshot(snapshot);
            var info = CreateSnapshotInfo(name, snapshot);
            return Task.FromResult(SnapshotResult.Ok(name, info));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapshotResult.Fail($"Failed to restore snapshot: {ex.Message}"));
        }
    }

    public Task<bool> DeleteAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(snapshots.TryRemove(name, out _));
    }

    public Task<IReadOnlyList<SnapshotInfo>> ListAsync()
    {
        var infos = snapshots
            .Select(kvp => CreateSnapshotInfo(kvp.Key, kvp.Value))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<SnapshotInfo>>(infos);
    }

    public Task<SnapshotInfo?> GetInfoAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult<SnapshotInfo?>(null);
        }

        if (!snapshots.TryGetValue(name, out var snapshot))
        {
            return Task.FromResult<SnapshotInfo?>(null);
        }

        return Task.FromResult<SnapshotInfo?>(CreateSnapshotInfo(name, snapshot));
    }

    public Task<SnapshotDiff> DiffAsync(string name1, string name2)
    {
        if (!snapshots.TryGetValue(name1, out var snapshot1))
        {
            return Task.FromResult(CreateErrorDiff(name1, name2, $"Snapshot '{name1}' not found."));
        }

        if (!snapshots.TryGetValue(name2, out var snapshot2))
        {
            return Task.FromResult(CreateErrorDiff(name1, name2, $"Snapshot '{name2}' not found."));
        }

        var diff = CompareSnapshots(name1, snapshot1, name2, snapshot2);
        return Task.FromResult(diff);
    }

    public Task<SnapshotDiff> DiffCurrentAsync(string name)
    {
        if (!snapshots.TryGetValue(name, out var snapshot))
        {
            return Task.FromResult(CreateErrorDiff(name, "(current)", $"Snapshot '{name}' not found."));
        }

        var currentSnapshot = CaptureSnapshot("(current)");
        var diff = CompareSnapshots(name, snapshot, "(current)", currentSnapshot);
        return Task.FromResult(diff);
    }

    public Task<SnapshotResult> SaveToFileAsync(string name, string path)
    {
        if (!snapshots.TryGetValue(name, out var snapshot))
        {
            return Task.FromResult(SnapshotResult.Fail($"Snapshot '{name}' not found."));
        }

        try
        {
            var json = SerializeSnapshot(snapshot);
            File.WriteAllText(path, json);
            var info = CreateSnapshotInfo(name, snapshot);
            return Task.FromResult(SnapshotResult.Ok(name, info));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapshotResult.Fail($"Failed to save snapshot to file: {ex.Message}"));
        }
    }

    public Task<SnapshotResult> LoadFromFileAsync(string path, string? name = null)
    {
        if (!File.Exists(path))
        {
            return Task.FromResult(SnapshotResult.Fail($"File not found: {path}"));
        }

        try
        {
            var json = File.ReadAllText(path);
            var snapshot = DeserializeSnapshot(json);
            if (snapshot == null)
            {
                return Task.FromResult(SnapshotResult.Fail("Failed to deserialize snapshot."));
            }

            var snapshotName = name ?? Path.GetFileNameWithoutExtension(path);
            snapshots[snapshotName] = snapshot;

            var info = CreateSnapshotInfo(snapshotName, snapshot);
            return Task.FromResult(SnapshotResult.Ok(snapshotName, info));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapshotResult.Fail($"Failed to load snapshot from file: {ex.Message}"));
        }
    }

    public Task<string> ExportJsonAsync(string name)
    {
        if (!snapshots.TryGetValue(name, out var snapshot))
        {
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(SerializeSnapshot(snapshot));
    }

    public Task<SnapshotResult> ImportJsonAsync(string json, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(SnapshotResult.Fail("Snapshot name cannot be empty."));
        }

        try
        {
            var snapshot = DeserializeSnapshot(json);
            if (snapshot == null)
            {
                return Task.FromResult(SnapshotResult.Fail("Failed to deserialize snapshot."));
            }

            snapshots[name] = snapshot;
            var info = CreateSnapshotInfo(name, snapshot);
            return Task.FromResult(SnapshotResult.Ok(name, info));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SnapshotResult.Fail($"Failed to import snapshot: {ex.Message}"));
        }
    }

    public Task<SnapshotResult> QuickSaveAsync()
    {
        return CreateAsync(QuickSaveName);
    }

    public Task<SnapshotResult> QuickLoadAsync()
    {
        return RestoreAsync(QuickSaveName);
    }

    private InMemorySnapshot CaptureSnapshot(string name)
    {
        var entities = new List<SnapshotEntity>();
        var totalComponents = 0;

        foreach (var entity in world.GetAllEntities())
        {
            var snapshotEntity = new SnapshotEntity
            {
                EntityId = entity.Id,
                Version = entity.Version,
                Name = world.GetName(entity),
                ParentId = world.GetParent(entity) is { IsValid: true } parent ? parent.Id : null,
                Components = []
            };

            foreach (var (componentType, componentValue) in world.GetComponents(entity))
            {
                var componentData = CaptureComponentData(componentType, componentValue);
                snapshotEntity.Components[componentType.FullName ?? componentType.Name] = componentData;
                totalComponents++;
            }

            entities.Add(snapshotEntity);
        }

        return new InMemorySnapshot
        {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            Entities = entities,
            TotalComponents = totalComponents
        };
    }

    private static Dictionary<string, object?> CaptureComponentData(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type componentType,
        object componentValue)
    {
        var data = new Dictionary<string, object?>();

        // Use reflection to capture field values
        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var value = field.GetValue(componentValue);
            // Convert to serializable form
            data[field.Name] = ConvertToSerializable(value);
        }

        return data;
    }

    private static object? ConvertToSerializable(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();

        // Primitives and strings are directly serializable
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
        {
            return value;
        }

        // Enums -> string
        if (type.IsEnum)
        {
            return value.ToString();
        }

        // Arrays -> List
        if (type.IsArray)
        {
            var array = (Array)value;
            var list = new List<object?>();
            foreach (var item in array)
            {
                list.Add(ConvertToSerializable(item));
            }
            return list;
        }

        // Value types (structs) -> Dictionary of fields
        if (type.IsValueType)
        {
            return ConvertStructToDict(type, value);
        }

        // Fall back to string representation
        return value.ToString();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "TestBridge uses reflection for debugging purposes")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "TestBridge uses reflection for debugging purposes")]
    private static Dictionary<string, object?> ConvertStructToDict(Type type, object value)
    {
        var dict = new Dictionary<string, object?>();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            dict[field.Name] = ConvertToSerializable(field.GetValue(value));
        }
        return dict;
    }

    private void RestoreSnapshot(InMemorySnapshot snapshot)
    {
        // Clear current world state
        world.Clear();

        // Map from original entity ID to new entity
        var entityMap = new Dictionary<int, Entity>();

        // First pass: Create all entities with their components
        foreach (var snapshotEntity in snapshot.Entities)
        {
            var builder = world.Spawn(snapshotEntity.Name);

            foreach (var (componentTypeName, componentData) in snapshotEntity.Components)
            {
                // Try to find the component type
                var componentType = FindComponentType(componentTypeName);
                if (componentType == null)
                {
                    continue; // Skip unknown component types
                }

                // Create component instance and populate fields
                var componentValue = CreateComponentFromData(componentType, componentData);
                if (componentValue != null)
                {
                    // Get ComponentInfo and use WithBoxed
                    // Note: Component must be pre-registered by the application
                    // We cannot dynamically register types at runtime without generics
                    var info = world.Components.Get(componentType);
                    if (info != null && builder is EntityBuilder entityBuilder)
                    {
                        entityBuilder.WithBoxed(info, componentValue);
                    }
                }
            }

            var entity = builder.Build();
            entityMap[snapshotEntity.EntityId] = entity;
        }

        // Second pass: Restore hierarchy relationships
        foreach (var snapshotEntity in snapshot.Entities.Where(e => e.ParentId.HasValue))
        {
            if (entityMap.TryGetValue(snapshotEntity.EntityId, out var child) &&
                entityMap.TryGetValue(snapshotEntity.ParentId!.Value, out var parent))
            {
                world.SetParent(child, parent);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TestBridge uses reflection for debugging purposes")]
    [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "TestBridge uses reflection for debugging purposes")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    private static Type? FindComponentType(string typeName)
    {
        // Try to find the type in all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            catch
            {
                // Ignore assembly load errors
            }
        }

        return null;
    }

    private static object? CreateComponentFromData(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type componentType,
        Dictionary<string, object?> data)
    {
        try
        {
            // Create default instance
            var instance = Activator.CreateInstance(componentType);
            if (instance == null)
            {
                return null;
            }

            // Set field values
            var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (data.TryGetValue(field.Name, out var value) && value != null)
                {
                    var convertedValue = ConvertFromSerializable(value, field.FieldType);
                    if (convertedValue != null)
                    {
                        field.SetValue(instance, convertedValue);
                    }
                }
            }

            return instance;
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertFromSerializable(object value, Type targetType)
    {
        if (value == null)
        {
            return null;
        }

        // Handle JsonElement from deserialization
        if (value is JsonElement jsonElement)
        {
            return ConvertJsonElement(jsonElement, targetType);
        }

        // Direct assignment if types match
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        // Numeric conversions
        if (IsNumericType(targetType) && IsNumericType(value.GetType()))
        {
            return Convert.ChangeType(value, targetType);
        }

        // String to enum
        if (targetType.IsEnum && value is string enumString)
        {
            return Enum.Parse(targetType, enumString);
        }

        // Dictionary to struct
        if (targetType.IsValueType && value is Dictionary<string, object?> dict)
        {
            return ConvertDictToStruct(targetType, dict);
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "TestBridge uses reflection for debugging purposes")]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "TestBridge uses reflection for debugging purposes")]
    private static object? ConvertDictToStruct(Type targetType, Dictionary<string, object?> dict)
    {
        var instance = Activator.CreateInstance(targetType);
        var fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (dict.TryGetValue(field.Name, out var fieldValue) && fieldValue != null)
            {
                var convertedValue = ConvertFromSerializable(fieldValue, field.FieldType);
                if (convertedValue != null)
                {
                    field.SetValue(instance, convertedValue);
                }
            }
        }
        return instance;
    }

    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when targetType == typeof(int) => element.GetInt32(),
            JsonValueKind.Number when targetType == typeof(long) => element.GetInt64(),
            JsonValueKind.Number when targetType == typeof(float) => element.GetSingle(),
            JsonValueKind.Number when targetType == typeof(double) => element.GetDouble(),
            JsonValueKind.Number when targetType == typeof(decimal) => element.GetDecimal(),
            JsonValueKind.String when targetType == typeof(string) => element.GetString(),
            JsonValueKind.String when targetType.IsEnum => Enum.Parse(targetType, element.GetString()!),
            JsonValueKind.True or JsonValueKind.False when targetType == typeof(bool) => element.GetBoolean(),
            _ => null
        };
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private static SnapshotInfo CreateSnapshotInfo(string name, InMemorySnapshot snapshot)
    {
        // Estimate size (rough approximation)
        var estimatedSize = snapshot.Entities.Count * 100L + snapshot.TotalComponents * 50L;

        return new SnapshotInfo
        {
            Name = name,
            CreatedAt = snapshot.CreatedAt,
            EntityCount = snapshot.Entities.Count,
            ComponentCount = snapshot.TotalComponents,
            SizeBytes = estimatedSize,
            IsQuickSave = name == QuickSaveName
        };
    }

    private static SnapshotDiff CreateErrorDiff(string name1, string name2, string error)
    {
        return new SnapshotDiff
        {
            Snapshot1 = name1,
            Snapshot2 = name2,
            AddedEntities = [],
            RemovedEntities = [],
            ModifiedEntities = [new EntityDiff
            {
                EntityId = -1,
                ChangeType = "Error",
                Name = error
            }],
            TotalChanges = 0
        };
    }

    private static SnapshotDiff CompareSnapshots(string name1, InMemorySnapshot snapshot1, string name2, InMemorySnapshot snapshot2)
    {
        var addedEntities = new List<EntityDiff>();
        var removedEntities = new List<EntityDiff>();
        var modifiedEntities = new List<EntityDiff>();

        var entities1 = snapshot1.Entities.ToDictionary(e => e.EntityId);
        var entities2 = snapshot2.Entities.ToDictionary(e => e.EntityId);

        // Find added entities (in 2 but not in 1)
        foreach (var entity2 in snapshot2.Entities)
        {
            if (!entities1.ContainsKey(entity2.EntityId))
            {
                addedEntities.Add(new EntityDiff
                {
                    EntityId = entity2.EntityId,
                    Name = entity2.Name,
                    ChangeType = "Added",
                    AddedComponents = [.. entity2.Components.Keys]
                });
            }
        }

        // Find removed entities (in 1 but not in 2)
        foreach (var entity1 in snapshot1.Entities)
        {
            if (!entities2.ContainsKey(entity1.EntityId))
            {
                removedEntities.Add(new EntityDiff
                {
                    EntityId = entity1.EntityId,
                    Name = entity1.Name,
                    ChangeType = "Removed",
                    RemovedComponents = [.. entity1.Components.Keys]
                });
            }
        }

        // Find modified entities
        foreach (var entity1 in snapshot1.Entities)
        {
            if (entities2.TryGetValue(entity1.EntityId, out var entity2))
            {
                var entityDiff = CompareEntities(entity1, entity2);
                if (entityDiff != null)
                {
                    modifiedEntities.Add(entityDiff);
                }
            }
        }

        var totalChanges = addedEntities.Count + removedEntities.Count + modifiedEntities.Count;

        return new SnapshotDiff
        {
            Snapshot1 = name1,
            Snapshot2 = name2,
            AddedEntities = addedEntities,
            RemovedEntities = removedEntities,
            ModifiedEntities = modifiedEntities,
            TotalChanges = totalChanges
        };
    }

    private static EntityDiff? CompareEntities(SnapshotEntity entity1, SnapshotEntity entity2)
    {
        var addedComponents = new List<string>();
        var removedComponents = new List<string>();
        var modifiedComponents = new List<ComponentDiff>();

        // Find added components
        foreach (var componentType in entity2.Components.Keys)
        {
            if (!entity1.Components.ContainsKey(componentType))
            {
                addedComponents.Add(componentType);
            }
        }

        // Find removed components
        foreach (var componentType in entity1.Components.Keys)
        {
            if (!entity2.Components.ContainsKey(componentType))
            {
                removedComponents.Add(componentType);
            }
        }

        // Compare common components
        foreach (var (componentType, data1) in entity1.Components)
        {
            if (entity2.Components.TryGetValue(componentType, out var data2))
            {
                var componentDiff = CompareComponents(componentType, data1, data2);
                if (componentDiff != null)
                {
                    modifiedComponents.Add(componentDiff);
                }
            }
        }

        // If nothing changed, return null
        if (addedComponents.Count == 0 && removedComponents.Count == 0 && modifiedComponents.Count == 0)
        {
            return null;
        }

        return new EntityDiff
        {
            EntityId = entity1.EntityId,
            Name = entity1.Name ?? entity2.Name,
            ChangeType = "Modified",
            AddedComponents = addedComponents.Count > 0 ? addedComponents : null,
            RemovedComponents = removedComponents.Count > 0 ? removedComponents : null,
            ModifiedComponents = modifiedComponents.Count > 0 ? modifiedComponents : null
        };
    }

    private static ComponentDiff? CompareComponents(string componentType, Dictionary<string, object?> data1, Dictionary<string, object?> data2)
    {
        var fieldDiffs = new List<FieldDiff>();

        var allFields = data1.Keys.Union(data2.Keys).ToHashSet();

        foreach (var fieldName in allFields)
        {
            var has1 = data1.TryGetValue(fieldName, out var value1);
            var has2 = data2.TryGetValue(fieldName, out var value2);

            if (!has1 && has2)
            {
                fieldDiffs.Add(new FieldDiff
                {
                    FieldName = fieldName,
                    OldValue = "(none)",
                    NewValue = FormatValue(value2)
                });
            }
            else if (has1 && !has2)
            {
                fieldDiffs.Add(new FieldDiff
                {
                    FieldName = fieldName,
                    OldValue = FormatValue(value1),
                    NewValue = "(none)"
                });
            }
            else if (!ValuesEqual(value1, value2))
            {
                fieldDiffs.Add(new FieldDiff
                {
                    FieldName = fieldName,
                    OldValue = FormatValue(value1),
                    NewValue = FormatValue(value2)
                });
            }
        }

        if (fieldDiffs.Count == 0)
        {
            return null;
        }

        return new ComponentDiff
        {
            ComponentType = componentType,
            Fields = fieldDiffs
        };
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        // Handle numeric comparisons with tolerance
        if (IsNumericType(a.GetType()) && IsNumericType(b.GetType()))
        {
            var da = Convert.ToDouble(a);
            var db = Convert.ToDouble(b);
            return Math.Abs(da - db) < 1e-9;
        }

        // Handle collections
        if (a is IEnumerable<object?> enumA && b is IEnumerable<object?> enumB)
        {
            var listA = enumA.ToList();
            var listB = enumB.ToList();
            if (listA.Count != listB.Count)
            {
                return false;
            }

            for (var i = 0; i < listA.Count; i++)
            {
                if (!ValuesEqual(listA[i], listB[i]))
                {
                    return false;
                }
            }

            return true;
        }

        return a.Equals(b);
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string str)
        {
            return $"\"{str}\"";
        }

        if (value is float f)
        {
            return f.ToString("G9");
        }

        if (value is double d)
        {
            return d.ToString("G17");
        }

        return value.ToString() ?? "null";
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TestBridge uses reflection for debugging purposes")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TestBridge uses reflection for debugging purposes")]
    private static string SerializeSnapshot(InMemorySnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TestBridge uses reflection for debugging purposes")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TestBridge uses reflection for debugging purposes")]
    private static InMemorySnapshot? DeserializeSnapshot(string json)
    {
        return JsonSerializer.Deserialize<InMemorySnapshot>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Internal representation of a snapshot stored in memory.
    /// </summary>
    private sealed class InMemorySnapshot
    {
        public required string Name { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required List<SnapshotEntity> Entities { get; init; }
        public required int TotalComponents { get; init; }
    }

    /// <summary>
    /// Internal representation of an entity in a snapshot.
    /// </summary>
    private sealed class SnapshotEntity
    {
        public required int EntityId { get; init; }
        public required int Version { get; init; }
        public string? Name { get; init; }
        public int? ParentId { get; init; }
        public required Dictionary<string, Dictionary<string, object?>> Components { get; init; }
    }
}
