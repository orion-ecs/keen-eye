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

    /// <inheritdoc/>
    public bool IsSerializable(Type type) => true;

    /// <inheritdoc/>
    public bool IsSerializable(string typeName) => true;

    /// <inheritdoc/>
    public JsonElement? Serialize(Type type, object value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, type, Options);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
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
        var type = Type.GetType(typeName);
        if (type == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json.GetRawText(), type, Options);
        }
        catch
        {
            // If deserialization fails, return null
            return null;
        }
    }

    /// <inheritdoc/>
    public Type? GetType(string typeName) => Type.GetType(typeName);

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
        var type = Type.GetType(typeName);
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
}
