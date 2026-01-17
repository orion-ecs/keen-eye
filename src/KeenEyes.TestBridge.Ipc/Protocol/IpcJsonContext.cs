using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.AI;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Logging;
using KeenEyes.TestBridge.Mutation;
using KeenEyes.TestBridge.Profile;
using KeenEyes.TestBridge.Replay;
using KeenEyes.TestBridge.Snapshot;
using KeenEyes.TestBridge.State;
using KeenEyes.TestBridge.Systems;
using KeenEyes.TestBridge.Time;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Ipc.Protocol;

/// <summary>
/// Source-generated JSON serialization context for IPC protocol types.
/// </summary>
/// <remarks>
/// <para>
/// This context provides AOT-compatible serialization for all types used in the
/// IPC protocol. All serializable types must be registered here with
/// <see cref="JsonSerializableAttribute"/>.
/// </para>
/// <para>
/// Uses camelCase naming and string enum conversion for debuggability.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    UseStringEnumConverter = true,
    Converters = [typeof(TimeSpanMillisecondsConverter)])]
// Protocol types
[JsonSerializable(typeof(IpcRequest))]
[JsonSerializable(typeof(IpcResponse))]
// Input types (enums)
[JsonSerializable(typeof(Key))]
[JsonSerializable(typeof(KeyModifiers))]
[JsonSerializable(typeof(MouseButton))]
[JsonSerializable(typeof(GamepadButton))]
[JsonSerializable(typeof(GamepadAxis))]
// State types
[JsonSerializable(typeof(EntityQuery))]
[JsonSerializable(typeof(EntitySnapshot))]
[JsonSerializable(typeof(WorldStats))]
[JsonSerializable(typeof(SystemInfo))]
[JsonSerializable(typeof(PerformanceMetrics))]
// Capture types
[JsonSerializable(typeof(FrameCapture))]
[JsonSerializable(typeof(ImageFormat))]
// Logging types
[JsonSerializable(typeof(LogQueryDto))]
[JsonSerializable(typeof(LogEntrySnapshot))]
[JsonSerializable(typeof(LogStatsSnapshot))]
[JsonSerializable(typeof(LogEntrySnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<LogEntrySnapshot>))]
[JsonSerializable(typeof(List<LogEntrySnapshot>))]
// Collections
[JsonSerializable(typeof(IReadOnlyList<EntitySnapshot>))]
[JsonSerializable(typeof(IReadOnlyList<SystemInfo>))]
[JsonSerializable(typeof(IReadOnlyList<FrameCapture>))]
[JsonSerializable(typeof(IReadOnlyList<int>))]
[JsonSerializable(typeof(List<EntitySnapshot>))]
[JsonSerializable(typeof(List<SystemInfo>))]
[JsonSerializable(typeof(List<FrameCapture>))]
[JsonSerializable(typeof(List<int>))]
// Arrays (for AOT deserialization)
[JsonSerializable(typeof(EntitySnapshot[]))]
[JsonSerializable(typeof(SystemInfo[]))]
[JsonSerializable(typeof(FrameCapture[]))]
// Dictionaries (for component data)
[JsonSerializable(typeof(IReadOnlyDictionary<string, object?>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, double>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, object?>>))]
[JsonSerializable(typeof(Dictionary<string, double>))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
// Primitives
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(int?))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement?))]
// Result types (serializable alternatives to tuples)
[JsonSerializable(typeof(FrameSizeResult))]
[JsonSerializable(typeof(MousePositionResult))]
[JsonSerializable(typeof(WindowSizeResult))]
// Window types
[JsonSerializable(typeof(WindowStateSnapshot))]
// Time types
[JsonSerializable(typeof(TimeStateSnapshot))]
// System types
[JsonSerializable(typeof(SystemSnapshot))]
[JsonSerializable(typeof(SystemSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<SystemSnapshot>))]
[JsonSerializable(typeof(List<SystemSnapshot>))]
// Mutation types
[JsonSerializable(typeof(EntityResult))]
[JsonSerializable(typeof(ComponentData))]
[JsonSerializable(typeof(ComponentData[]))]
[JsonSerializable(typeof(IReadOnlyList<ComponentData>))]
[JsonSerializable(typeof(List<ComponentData>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<string>))]
// Profile types
[JsonSerializable(typeof(SystemProfileSnapshot))]
[JsonSerializable(typeof(SystemProfileSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<SystemProfileSnapshot>))]
[JsonSerializable(typeof(List<SystemProfileSnapshot>))]
[JsonSerializable(typeof(QueryProfileSnapshot))]
[JsonSerializable(typeof(QueryProfileSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<QueryProfileSnapshot>))]
[JsonSerializable(typeof(List<QueryProfileSnapshot>))]
[JsonSerializable(typeof(QueryCacheStatsSnapshot))]
[JsonSerializable(typeof(AllocationProfileSnapshot))]
[JsonSerializable(typeof(AllocationProfileSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<AllocationProfileSnapshot>))]
[JsonSerializable(typeof(List<AllocationProfileSnapshot>))]
[JsonSerializable(typeof(MemoryStatsSnapshot))]
[JsonSerializable(typeof(ArchetypeStatsSnapshot))]
[JsonSerializable(typeof(ArchetypeStatsSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<ArchetypeStatsSnapshot>))]
[JsonSerializable(typeof(List<ArchetypeStatsSnapshot>))]
[JsonSerializable(typeof(TimelineStatsSnapshot))]
[JsonSerializable(typeof(TimelineEntrySnapshot))]
[JsonSerializable(typeof(TimelineEntrySnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<TimelineEntrySnapshot>))]
[JsonSerializable(typeof(List<TimelineEntrySnapshot>))]
[JsonSerializable(typeof(TimelineSystemStatsSnapshot))]
[JsonSerializable(typeof(TimelineSystemStatsSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<TimelineSystemStatsSnapshot>))]
[JsonSerializable(typeof(List<TimelineSystemStatsSnapshot>))]
// AI types
[JsonSerializable(typeof(AIStatisticsSnapshot))]
[JsonSerializable(typeof(BehaviorTreeSnapshot))]
[JsonSerializable(typeof(StateMachineSnapshot))]
[JsonSerializable(typeof(StateInfoSnapshot))]
[JsonSerializable(typeof(StateInfoSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<StateInfoSnapshot>))]
[JsonSerializable(typeof(List<StateInfoSnapshot>))]
[JsonSerializable(typeof(UtilityAISnapshot))]
[JsonSerializable(typeof(UtilityScoreSnapshot))]
[JsonSerializable(typeof(UtilityScoreSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<UtilityScoreSnapshot>))]
[JsonSerializable(typeof(List<UtilityScoreSnapshot>))]
[JsonSerializable(typeof(BlackboardEntrySnapshot))]
[JsonSerializable(typeof(BlackboardEntrySnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<BlackboardEntrySnapshot>))]
[JsonSerializable(typeof(List<BlackboardEntrySnapshot>))]
// Input command arguments
[JsonSerializable(typeof(KeyActionArgs))]
[JsonSerializable(typeof(KeyPressArgs))]
[JsonSerializable(typeof(TypeTextArgs))]
[JsonSerializable(typeof(SingleKeyArgs))]
[JsonSerializable(typeof(MouseMoveArgs))]
[JsonSerializable(typeof(MouseRelativeArgs))]
[JsonSerializable(typeof(MouseButtonArgs))]
[JsonSerializable(typeof(ClickArgs))]
[JsonSerializable(typeof(DragArgs))]
[JsonSerializable(typeof(ScrollArgs))]
[JsonSerializable(typeof(GamepadButtonArgs))]
[JsonSerializable(typeof(StickArgs))]
[JsonSerializable(typeof(TriggerArgs))]
[JsonSerializable(typeof(GamepadConnectionArgs))]
[JsonSerializable(typeof(ActionNameArgs))]
[JsonSerializable(typeof(ActionValueArgs))]
[JsonSerializable(typeof(ActionVector2Args))]
// State command arguments
[JsonSerializable(typeof(EntityIdArgs))]
[JsonSerializable(typeof(NameArgs))]
[JsonSerializable(typeof(ComponentArgs))]
[JsonSerializable(typeof(TypeNameArgs))]
[JsonSerializable(typeof(FrameCountArgs))]
[JsonSerializable(typeof(TagArgs))]
[JsonSerializable(typeof(ParentIdArgs))]
// Capture command arguments
[JsonSerializable(typeof(SaveScreenshotArgs))]
[JsonSerializable(typeof(GetScreenshotBytesArgs))]
[JsonSerializable(typeof(StartRecordingArgs))]
[JsonSerializable(typeof(CaptureRegionArgs))]
[JsonSerializable(typeof(GetRegionScreenshotBytesArgs))]
[JsonSerializable(typeof(SaveRegionScreenshotArgs))]
// Snapshot types
[JsonSerializable(typeof(SnapshotResult))]
[JsonSerializable(typeof(SnapshotInfo))]
[JsonSerializable(typeof(SnapshotInfo[]))]
[JsonSerializable(typeof(IReadOnlyList<SnapshotInfo>))]
[JsonSerializable(typeof(List<SnapshotInfo>))]
[JsonSerializable(typeof(SnapshotDiff))]
[JsonSerializable(typeof(EntityDiff))]
[JsonSerializable(typeof(EntityDiff[]))]
[JsonSerializable(typeof(IReadOnlyList<EntityDiff>))]
[JsonSerializable(typeof(List<EntityDiff>))]
[JsonSerializable(typeof(ComponentDiff))]
[JsonSerializable(typeof(ComponentDiff[]))]
[JsonSerializable(typeof(IReadOnlyList<ComponentDiff>))]
[JsonSerializable(typeof(List<ComponentDiff>))]
[JsonSerializable(typeof(FieldDiff))]
[JsonSerializable(typeof(FieldDiff[]))]
[JsonSerializable(typeof(IReadOnlyList<FieldDiff>))]
[JsonSerializable(typeof(List<FieldDiff>))]
// Replay types
[JsonSerializable(typeof(ReplayOperationResult))]
[JsonSerializable(typeof(RecordingInfoSnapshot))]
[JsonSerializable(typeof(PlaybackStateSnapshot))]
[JsonSerializable(typeof(ReplayFrameSnapshot))]
[JsonSerializable(typeof(ReplayFrameSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<ReplayFrameSnapshot>))]
[JsonSerializable(typeof(List<ReplayFrameSnapshot>))]
[JsonSerializable(typeof(ReplayMetadataSnapshot))]
[JsonSerializable(typeof(ReplayFileSnapshot))]
[JsonSerializable(typeof(ReplayFileSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<ReplayFileSnapshot>))]
[JsonSerializable(typeof(List<ReplayFileSnapshot>))]
[JsonSerializable(typeof(InputEventSnapshot))]
[JsonSerializable(typeof(InputEventSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<InputEventSnapshot>))]
[JsonSerializable(typeof(List<InputEventSnapshot>))]
[JsonSerializable(typeof(ReplayEventSnapshot))]
[JsonSerializable(typeof(ReplayEventSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<ReplayEventSnapshot>))]
[JsonSerializable(typeof(List<ReplayEventSnapshot>))]
[JsonSerializable(typeof(SnapshotMarkerSnapshot))]
[JsonSerializable(typeof(SnapshotMarkerSnapshot[]))]
[JsonSerializable(typeof(IReadOnlyList<SnapshotMarkerSnapshot>))]
[JsonSerializable(typeof(List<SnapshotMarkerSnapshot>))]
[JsonSerializable(typeof(ValidationResultSnapshot))]
[JsonSerializable(typeof(DeterminismResultSnapshot))]
internal partial class IpcJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Result type for frame size queries (serializable alternative to tuple).
/// </summary>
public sealed record FrameSizeResult
{
    /// <summary>
    /// Gets the frame width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets the frame height in pixels.
    /// </summary>
    public required int Height { get; init; }
}

/// <summary>
/// Result type for mouse position queries (serializable alternative to tuple).
/// </summary>
public sealed record MousePositionResult
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public float Y { get; init; }
}

/// <summary>
/// Result type for window size queries (serializable alternative to tuple).
/// </summary>
public sealed record WindowSizeResult
{
    /// <summary>
    /// Gets the window width in pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets the window height in pixels.
    /// </summary>
    public required int Height { get; init; }
}
