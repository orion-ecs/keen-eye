using System.Text.Json.Serialization;

namespace KeenEyes.Serialization;

/// <summary>
/// JSON source generation context for AOT-compatible WorldSnapshot serialization.
/// </summary>
/// <remarks>
/// This context enables Native AOT compilation by generating serialization code at compile-time
/// instead of using reflection at runtime. It includes all types needed to serialize/deserialize
/// the WorldSnapshot envelope (entity metadata, component metadata, singletons).
/// Component data itself is handled separately by <see cref="IComponentSerializer"/>.
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    IncludeFields = true)]
[JsonSerializable(typeof(WorldSnapshot))]
[JsonSerializable(typeof(SerializedEntity))]
[JsonSerializable(typeof(SerializedComponent))]
[JsonSerializable(typeof(SerializedSingleton))]
internal partial class SnapshotJsonContext : JsonSerializerContext
{
}
