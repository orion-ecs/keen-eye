using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Testing.Input;

/// <summary>
/// Base class for recorded input events.
/// </summary>
/// <param name="Timestamp">The time in milliseconds when this event occurred.</param>
[JsonDerivedType(typeof(RecordedKeyDownEvent), nameof(RecordedKeyDownEvent))]
[JsonDerivedType(typeof(RecordedKeyUpEvent), nameof(RecordedKeyUpEvent))]
[JsonDerivedType(typeof(RecordedTextInputEvent), nameof(RecordedTextInputEvent))]
[JsonDerivedType(typeof(RecordedMouseMoveEvent), nameof(RecordedMouseMoveEvent))]
[JsonDerivedType(typeof(RecordedMouseButtonDownEvent), nameof(RecordedMouseButtonDownEvent))]
[JsonDerivedType(typeof(RecordedMouseButtonUpEvent), nameof(RecordedMouseButtonUpEvent))]
[JsonDerivedType(typeof(RecordedMouseScrollEvent), nameof(RecordedMouseScrollEvent))]
[JsonDerivedType(typeof(RecordedGamepadButtonDownEvent), nameof(RecordedGamepadButtonDownEvent))]
[JsonDerivedType(typeof(RecordedGamepadButtonUpEvent), nameof(RecordedGamepadButtonUpEvent))]
[JsonDerivedType(typeof(RecordedGamepadAxisEvent), nameof(RecordedGamepadAxisEvent))]
public abstract record RecordedInputEvent(float Timestamp);

/// <summary>
/// A recorded key down event.
/// </summary>
public sealed record RecordedKeyDownEvent(
    float Timestamp,
    Key Key,
    KeyModifiers Modifiers,
    bool IsRepeat) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded key up event.
/// </summary>
public sealed record RecordedKeyUpEvent(
    float Timestamp,
    Key Key,
    KeyModifiers Modifiers) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded text input event.
/// </summary>
public sealed record RecordedTextInputEvent(
    float Timestamp,
    char Character) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded mouse move event.
/// </summary>
public sealed record RecordedMouseMoveEvent(
    float Timestamp,
    float PositionX,
    float PositionY,
    float DeltaX,
    float DeltaY) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded mouse button down event.
/// </summary>
public sealed record RecordedMouseButtonDownEvent(
    float Timestamp,
    MouseButton Button,
    float PositionX,
    float PositionY,
    KeyModifiers Modifiers) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded mouse button up event.
/// </summary>
public sealed record RecordedMouseButtonUpEvent(
    float Timestamp,
    MouseButton Button,
    float PositionX,
    float PositionY,
    KeyModifiers Modifiers) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded mouse scroll event.
/// </summary>
public sealed record RecordedMouseScrollEvent(
    float Timestamp,
    float DeltaX,
    float DeltaY,
    float PositionX,
    float PositionY) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded gamepad button down event.
/// </summary>
public sealed record RecordedGamepadButtonDownEvent(
    float Timestamp,
    int GamepadIndex,
    GamepadButton Button) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded gamepad button up event.
/// </summary>
public sealed record RecordedGamepadButtonUpEvent(
    float Timestamp,
    int GamepadIndex,
    GamepadButton Button) : RecordedInputEvent(Timestamp);

/// <summary>
/// A recorded gamepad axis change event.
/// </summary>
public sealed record RecordedGamepadAxisEvent(
    float Timestamp,
    int GamepadIndex,
    GamepadAxis Axis,
    float Value,
    float PreviousValue) : RecordedInputEvent(Timestamp);

/// <summary>
/// A serializable recording of input events with timestamps.
/// </summary>
/// <remarks>
/// <para>
/// InputRecording stores a sequence of input events with precise timestamps, enabling
/// deterministic replay of user input. Recordings are synchronized with <see cref="TestClock"/>
/// for accurate playback timing.
/// </para>
/// <para>
/// Recordings can be serialized to JSON or binary formats for storage and sharing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a recording
/// var recording = new InputRecording();
/// recording.Add(new RecordedKeyDownEvent(0, Key.W, KeyModifiers.None, false));
/// recording.Add(new RecordedKeyUpEvent(100, Key.W, KeyModifiers.None));
///
/// // Serialize to JSON
/// var json = recording.ToJson();
///
/// // Deserialize and replay
/// var loaded = InputRecording.FromJson(json);
/// </code>
/// </example>
public sealed class InputRecording
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly List<RecordedInputEvent> events = [];

    /// <summary>
    /// Gets all recorded events in chronological order.
    /// </summary>
    public IReadOnlyList<RecordedInputEvent> Events => events;

    /// <summary>
    /// Gets the total duration of the recording in milliseconds.
    /// </summary>
    public float Duration => events.Count > 0 ? events[^1].Timestamp : 0f;

    /// <summary>
    /// Gets the number of events in the recording.
    /// </summary>
    public int Count => events.Count;

    /// <summary>
    /// Gets metadata about the recording.
    /// </summary>
    public InputRecordingMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Adds an event to the recording.
    /// </summary>
    /// <param name="evt">The event to add.</param>
    public void Add(RecordedInputEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        events.Add(evt);
    }

    /// <summary>
    /// Clears all events from the recording.
    /// </summary>
    public void Clear()
    {
        events.Clear();
    }

    /// <summary>
    /// Gets events within a time range.
    /// </summary>
    /// <param name="startTime">The start time in milliseconds (inclusive).</param>
    /// <param name="endTime">The end time in milliseconds (exclusive).</param>
    /// <returns>Events within the specified time range.</returns>
    public IEnumerable<RecordedInputEvent> GetEventsInRange(float startTime, float endTime)
    {
        return events.Where(e => e.Timestamp >= startTime && e.Timestamp < endTime);
    }

    /// <summary>
    /// Serializes the recording to JSON.
    /// </summary>
    /// <returns>A JSON string representing the recording.</returns>
    public string ToJson()
    {
        var data = new InputRecordingData(events, Metadata);
        return JsonSerializer.Serialize(data, jsonOptions);
    }

    /// <summary>
    /// Deserializes a recording from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A new InputRecording instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the JSON is invalid.</exception>
    public static InputRecording FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        var data = JsonSerializer.Deserialize<InputRecordingData>(json, jsonOptions)
            ?? throw new ArgumentException("Invalid JSON: deserialization returned null", nameof(json));

        var recording = new InputRecording { Metadata = data.Metadata ?? new InputRecordingMetadata() };
        foreach (var evt in data.Events ?? [])
        {
            recording.Add(evt);
        }

        return recording;
    }

    /// <summary>
    /// Serializes the recording to binary format.
    /// </summary>
    /// <returns>A byte array representing the recording.</returns>
    public byte[] ToBinary()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write("INPREC"); // Magic bytes
        writer.Write((byte)1); // Version

        // Write metadata
        writer.Write(Metadata.Name ?? string.Empty);
        writer.Write(Metadata.Description ?? string.Empty);
        writer.Write(Metadata.RecordedAt?.ToBinary() ?? 0L);

        // Write events
        writer.Write(events.Count);
        foreach (var evt in events)
        {
            WriteEvent(writer, evt);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a recording from binary format.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <returns>A new InputRecording instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the binary data is invalid.</exception>
    public static InputRecording FromBinary(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Read and verify header
        var magic = reader.ReadString();
        if (magic != "INPREC")
        {
            throw new ArgumentException("Invalid binary format: missing magic bytes", nameof(data));
        }

        var version = reader.ReadByte();
        if (version != 1)
        {
            throw new ArgumentException($"Unsupported version: {version}", nameof(data));
        }

        // Read metadata
        var recording = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = reader.ReadString(),
                Description = reader.ReadString(),
                RecordedAt = DateTime.FromBinary(reader.ReadInt64()) is var dt && dt != DateTime.MinValue ? dt : null
            }
        };

        // Fix empty strings
        if (string.IsNullOrEmpty(recording.Metadata.Name))
        {
            recording.Metadata.Name = null;
        }

        if (string.IsNullOrEmpty(recording.Metadata.Description))
        {
            recording.Metadata.Description = null;
        }

        // Read events
        var eventCount = reader.ReadInt32();
        for (int i = 0; i < eventCount; i++)
        {
            recording.Add(ReadEvent(reader));
        }

        return recording;
    }

    private static void WriteEvent(BinaryWriter writer, RecordedInputEvent evt)
    {
        writer.Write(evt.Timestamp);

        switch (evt)
        {
            case RecordedKeyDownEvent k:
                writer.Write((byte)0);
                writer.Write((int)k.Key);
                writer.Write((int)k.Modifiers);
                writer.Write(k.IsRepeat);
                break;
            case RecordedKeyUpEvent k:
                writer.Write((byte)1);
                writer.Write((int)k.Key);
                writer.Write((int)k.Modifiers);
                break;
            case RecordedTextInputEvent t:
                writer.Write((byte)2);
                writer.Write(t.Character);
                break;
            case RecordedMouseMoveEvent m:
                writer.Write((byte)3);
                writer.Write(m.PositionX);
                writer.Write(m.PositionY);
                writer.Write(m.DeltaX);
                writer.Write(m.DeltaY);
                break;
            case RecordedMouseButtonDownEvent m:
                writer.Write((byte)4);
                writer.Write((int)m.Button);
                writer.Write(m.PositionX);
                writer.Write(m.PositionY);
                writer.Write((int)m.Modifiers);
                break;
            case RecordedMouseButtonUpEvent m:
                writer.Write((byte)5);
                writer.Write((int)m.Button);
                writer.Write(m.PositionX);
                writer.Write(m.PositionY);
                writer.Write((int)m.Modifiers);
                break;
            case RecordedMouseScrollEvent m:
                writer.Write((byte)6);
                writer.Write(m.DeltaX);
                writer.Write(m.DeltaY);
                writer.Write(m.PositionX);
                writer.Write(m.PositionY);
                break;
            case RecordedGamepadButtonDownEvent g:
                writer.Write((byte)7);
                writer.Write(g.GamepadIndex);
                writer.Write((int)g.Button);
                break;
            case RecordedGamepadButtonUpEvent g:
                writer.Write((byte)8);
                writer.Write(g.GamepadIndex);
                writer.Write((int)g.Button);
                break;
            case RecordedGamepadAxisEvent g:
                writer.Write((byte)9);
                writer.Write(g.GamepadIndex);
                writer.Write((int)g.Axis);
                writer.Write(g.Value);
                writer.Write(g.PreviousValue);
                break;
            default:
                throw new NotSupportedException($"Unknown event type: {evt.GetType()}");
        }
    }

    private static RecordedInputEvent ReadEvent(BinaryReader reader)
    {
        var timestamp = reader.ReadSingle();
        var type = reader.ReadByte();

        return type switch
        {
            0 => new RecordedKeyDownEvent(timestamp, (Key)reader.ReadInt32(), (KeyModifiers)reader.ReadInt32(), reader.ReadBoolean()),
            1 => new RecordedKeyUpEvent(timestamp, (Key)reader.ReadInt32(), (KeyModifiers)reader.ReadInt32()),
            2 => new RecordedTextInputEvent(timestamp, reader.ReadChar()),
            3 => new RecordedMouseMoveEvent(timestamp, reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            4 => new RecordedMouseButtonDownEvent(timestamp, (MouseButton)reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), (KeyModifiers)reader.ReadInt32()),
            5 => new RecordedMouseButtonUpEvent(timestamp, (MouseButton)reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), (KeyModifiers)reader.ReadInt32()),
            6 => new RecordedMouseScrollEvent(timestamp, reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            7 => new RecordedGamepadButtonDownEvent(timestamp, reader.ReadInt32(), (GamepadButton)reader.ReadInt32()),
            8 => new RecordedGamepadButtonUpEvent(timestamp, reader.ReadInt32(), (GamepadButton)reader.ReadInt32()),
            9 => new RecordedGamepadAxisEvent(timestamp, reader.ReadInt32(), (GamepadAxis)reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle()),
            _ => throw new NotSupportedException($"Unknown event type: {type}")
        };
    }

    private sealed record InputRecordingData(
        List<RecordedInputEvent>? Events,
        InputRecordingMetadata? Metadata);
}

/// <summary>
/// Metadata about an input recording.
/// </summary>
public sealed class InputRecordingMetadata
{
    /// <summary>
    /// Gets or sets a human-readable name for the recording.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a description of what the recording contains.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets when the recording was made.
    /// </summary>
    public DateTime? RecordedAt { get; set; }
}
