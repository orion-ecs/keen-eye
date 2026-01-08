using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Logging;
using KeenEyes.TestBridge.State;

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
