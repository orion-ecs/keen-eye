using System.Text.Json.Serialization;

namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Source-generated JSON serialization context for playtest bundle types.
/// </summary>
/// <remarks>
/// This context enables Native AOT-compatible JSON serialization without reflection for the
/// documents written into a playtest bundle archive. It mirrors the pattern established by
/// <see cref="ReplayJsonContext"/>.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PlaytestManifest))]
[JsonSerializable(typeof(PlaytestCrashInfo))]
[JsonSerializable(typeof(IReadOnlyList<PlaytestFeedback>), TypeInfoPropertyName = "FeedbackList")]
[JsonSerializable(typeof(IReadOnlyList<PlaytestLogEntry>), TypeInfoPropertyName = "LogEntryList")]
internal partial class PlaytestJsonContext : JsonSerializerContext
{
}
