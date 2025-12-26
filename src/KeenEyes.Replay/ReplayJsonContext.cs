using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Source-generated JSON serialization context for replay types.
/// </summary>
/// <remarks>
/// This context enables Native AOT-compatible JSON serialization without
/// reflection. All replay-related types that need JSON serialization should
/// be included here.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ReplayData))]
[JsonSerializable(typeof(ReplayFrame))]
[JsonSerializable(typeof(ReplayEvent))]
[JsonSerializable(typeof(SnapshotMarker))]
[JsonSerializable(typeof(ReplayFileInfo))]
[JsonSerializable(typeof(IReadOnlyList<ReplayFrame>))]
[JsonSerializable(typeof(IReadOnlyList<ReplayEvent>))]
[JsonSerializable(typeof(IReadOnlyList<SnapshotMarker>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
internal partial class ReplayJsonContext : JsonSerializerContext
{
}
