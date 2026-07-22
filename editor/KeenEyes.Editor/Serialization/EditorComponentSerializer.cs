using System.Collections.Concurrent;
using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Editor.Serialization;

/// <summary>
/// Reflection-based component serializer for editor use.
/// Uses System.Text.Json with reflection for dynamic serialization.
/// </summary>
/// <remarks>
/// <para>
/// This serializer is designed for editor-only use where Native AOT compatibility
/// is not required. It uses reflection-based JSON serialization to handle any
/// component type dynamically.
/// </para>
/// <para>
/// This enables features like play mode snapshot/restore without requiring
/// source-generated serializers for every component type.
/// </para>
/// </remarks>
public sealed class EditorComponentSerializer : IComponentSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        WriteIndented = false
    };

    /// <summary>
    /// Caches <see cref="Type.GetType(string)"/> lookups. Snapshot restore resolves the
    /// type once per component instance, so at editor scale (thousands of entities) an
    /// uncached lookup dominates the restore path.
    /// </summary>
    private readonly ConcurrentDictionary<string, Type?> typeCache = new();

    /// <inheritdoc/>
    public bool IsSerializable(Type type) => true;

    /// <inheritdoc/>
    public bool IsSerializable(string typeName) => true;

    /// <inheritdoc/>
    public JsonElement? Serialize(Type type, object value)
    {
        try
        {
            // SerializeToElement writes to a pooled UTF-8 buffer and parses it in place,
            // avoiding the intermediate string + JsonDocument + Clone of the naive path.
            return JsonSerializer.SerializeToElement(value, type, Options);
        }
        catch
        {
            // If serialization fails, return null (component won't be preserved)
            return null;
        }
    }

    /// <inheritdoc/>
    public object? Deserialize(string typeName, JsonElement json)
    {
        var type = ResolveType(typeName);
        if (type == null)
        {
            return null;
        }

        try
        {
            // Deserializing from the element directly avoids materializing the raw JSON text.
            return json.Deserialize(type, Options);
        }
        catch
        {
            // If deserialization fails, return null
            return null;
        }
    }

    /// <inheritdoc/>
    public Type? GetType(string typeName) => ResolveType(typeName);

    /// <inheritdoc/>
    public ComponentInfo? RegisterComponent(
        ISerializationCapability serialization,
        string typeName,
        bool isTag) => null;

    /// <inheritdoc/>
    public bool SetSingleton(
        ISerializationCapability serialization,
        string typeName,
        object value) => false;

    /// <inheritdoc/>
    public object? CreateDefault(string typeName)
    {
        var type = ResolveType(typeName);
        if (type == null)
        {
            return null;
        }

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public int GetVersion(string typeName) => 1;

    /// <inheritdoc/>
    public int GetVersion(Type type) => 1;

    private Type? ResolveType(string typeName)
        => typeCache.GetOrAdd(typeName, static name => Type.GetType(name));
}
