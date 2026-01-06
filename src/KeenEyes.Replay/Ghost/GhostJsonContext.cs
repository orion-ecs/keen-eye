using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Source-generated JSON serialization context for ghost types.
/// </summary>
/// <remarks>
/// This context enables Native AOT-compatible JSON serialization without
/// reflection. All ghost-related types that need JSON serialization should
/// be included here.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(GhostData))]
[JsonSerializable(typeof(GhostFrame))]
[JsonSerializable(typeof(GhostFileInfo))]
[JsonSerializable(typeof(IReadOnlyList<GhostFrame>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class GhostJsonContext : JsonSerializerContext
{
}
