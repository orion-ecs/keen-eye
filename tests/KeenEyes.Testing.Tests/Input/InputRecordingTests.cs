using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class InputRecordingTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesEmptyRecording()
    {
        var recording = new InputRecording();

        Assert.Empty(recording.Events);
        Assert.Equal(0, recording.Count);
        Assert.Equal(0f, recording.Duration);
        Assert.NotNull(recording.Metadata);
    }

    #endregion

    #region Add

    [Fact]
    public void Add_AddsEventToRecording()
    {
        var recording = new InputRecording();
        var evt = new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false);

        recording.Add(evt);

        Assert.Single(recording.Events);
        Assert.Equal(evt, recording.Events[0]);
        Assert.Equal(1, recording.Count);
    }

    [Fact]
    public void Add_WithNullEvent_ThrowsArgumentNullException()
    {
        var recording = new InputRecording();

        Assert.Throws<ArgumentNullException>(() => recording.Add(null!));
    }

    [Fact]
    public void Add_MultipleEvents_MaintainsOrder()
    {
        var recording = new InputRecording();
        var evt1 = new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false);
        var evt2 = new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None);
        var evt3 = new RecordedTextInputEvent(300f, 'a');

        recording.Add(evt1);
        recording.Add(evt2);
        recording.Add(evt3);

        Assert.Equal(3, recording.Count);
        Assert.Equal(evt1, recording.Events[0]);
        Assert.Equal(evt2, recording.Events[1]);
        Assert.Equal(evt3, recording.Events[2]);
    }

    #endregion

    #region Duration

    [Fact]
    public void Duration_WithNoEvents_ReturnsZero()
    {
        var recording = new InputRecording();

        Assert.Equal(0f, recording.Duration);
    }

    [Fact]
    public void Duration_WithEvents_ReturnsLastEventTimestamp()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedTextInputEvent(300f, 'a'));
        recording.Add(new RecordedKeyUpEvent(500f, Key.W, KeyModifiers.None));

        Assert.Equal(500f, recording.Duration);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        recording.Clear();

        Assert.Empty(recording.Events);
        Assert.Equal(0, recording.Count);
        Assert.Equal(0f, recording.Duration);
    }

    #endregion

    #region GetEventsInRange

    [Fact]
    public void GetEventsInRange_ReturnsEventsWithinRange()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));
        recording.Add(new RecordedTextInputEvent(300f, 'a'));
        recording.Add(new RecordedKeyDownEvent(400f, Key.S, KeyModifiers.None, false));

        var events = recording.GetEventsInRange(150f, 350f).ToList();

        Assert.Equal(2, events.Count);
        Assert.IsType<RecordedKeyUpEvent>(events[0]);
        Assert.IsType<RecordedTextInputEvent>(events[1]);
    }

    [Fact]
    public void GetEventsInRange_IncludesStartTime()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));

        var events = recording.GetEventsInRange(100f, 200f).ToList();

        Assert.Single(events);
    }

    [Fact]
    public void GetEventsInRange_ExcludesEndTime()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(200f, Key.W, KeyModifiers.None, false));

        var events = recording.GetEventsInRange(100f, 200f).ToList();

        Assert.Empty(events);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void ToJson_SerializesRecording()
    {
        var recording = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = "Test Recording",
                Description = "A test"
            }
        };
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        var json = recording.ToJson();

        Assert.NotNull(json);
        Assert.Contains("Test Recording", json);
        Assert.Contains("RecordedKeyDownEvent", json);
        Assert.Contains("RecordedKeyUpEvent", json);
    }

    [Fact]
    public void FromJson_DeserializesRecording()
    {
        var original = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = "Test",
                Description = "Desc"
            }
        };
        original.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        original.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        var json = original.ToJson();
        var deserialized = InputRecording.FromJson(json);

        Assert.Equal(2, deserialized.Count);
        Assert.Equal("Test", deserialized.Metadata.Name);
        Assert.Equal("Desc", deserialized.Metadata.Description);
        Assert.IsType<RecordedKeyDownEvent>(deserialized.Events[0]);
        Assert.IsType<RecordedKeyUpEvent>(deserialized.Events[1]);
    }

    [Fact]
    public void FromJson_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => InputRecording.FromJson(null!));
    }

    [Fact]
    public void FromJson_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => InputRecording.FromJson(string.Empty));
    }

    [Fact]
    public void FromJson_WithInvalidJson_Throws()
    {
        // Invalid JSON will throw when deserializing - could be ArgumentException or JsonException
        Assert.ThrowsAny<Exception>(() => InputRecording.FromJson("invalid json"));
    }

    #endregion

    #region Binary Serialization

    [Fact]
    public void ToBinary_SerializesRecording()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.None, false));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.None));

        var binary = recording.ToBinary();

        Assert.NotNull(binary);
        Assert.NotEmpty(binary);
    }

    [Fact]
    public void FromBinary_DeserializesRecording()
    {
        var original = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = "Binary Test",
                Description = "Binary Desc",
                RecordedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }
        };
        original.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.Control, false));
        original.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.Control));
        original.Add(new RecordedMouseMoveEvent(300f, 100f, 200f, 10f, 20f));

        var binary = original.ToBinary();
        var deserialized = InputRecording.FromBinary(binary);

        Assert.Equal(3, deserialized.Count);
        Assert.Equal("Binary Test", deserialized.Metadata.Name);
        Assert.Equal("Binary Desc", deserialized.Metadata.Description);
        Assert.NotNull(deserialized.Metadata.RecordedAt);
    }

    [Fact]
    public void FromBinary_WithNullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => InputRecording.FromBinary(null!));
    }

    [Fact]
    public void FromBinary_WithInvalidMagicBytes_ThrowsArgumentException()
    {
        var invalidData = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<ArgumentException>(() => InputRecording.FromBinary(invalidData));
    }

    [Fact]
    public void FromBinary_WithUnsupportedVersion_ThrowsArgumentException()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("INPREC");
        writer.Write((byte)99); // Unsupported version

        var ex = Assert.Throws<ArgumentException>(() => InputRecording.FromBinary(stream.ToArray()));
        Assert.Contains("Unsupported version", ex.Message);
    }

    #endregion

    #region All Event Types

    [Fact]
    public void BinarySerialization_SupportsAllEventTypes()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.Control, true));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.Control));
        recording.Add(new RecordedTextInputEvent(300f, 'a'));
        recording.Add(new RecordedMouseMoveEvent(400f, 100f, 200f, 10f, 20f));
        recording.Add(new RecordedMouseButtonDownEvent(500f, MouseButton.Left, 150f, 250f, KeyModifiers.Shift));
        recording.Add(new RecordedMouseButtonUpEvent(600f, MouseButton.Left, 150f, 250f, KeyModifiers.Shift));
        recording.Add(new RecordedMouseScrollEvent(700f, 1f, -1f, 150f, 250f));
        recording.Add(new RecordedGamepadButtonDownEvent(800f, 0, GamepadButton.South));
        recording.Add(new RecordedGamepadButtonUpEvent(900f, 0, GamepadButton.South));
        recording.Add(new RecordedGamepadAxisEvent(1000f, 0, GamepadAxis.LeftStickX, 0.5f, 0.0f));

        var binary = recording.ToBinary();
        var deserialized = InputRecording.FromBinary(binary);

        Assert.Equal(10, deserialized.Count);
        Assert.IsType<RecordedKeyDownEvent>(deserialized.Events[0]);
        Assert.IsType<RecordedKeyUpEvent>(deserialized.Events[1]);
        Assert.IsType<RecordedTextInputEvent>(deserialized.Events[2]);
        Assert.IsType<RecordedMouseMoveEvent>(deserialized.Events[3]);
        Assert.IsType<RecordedMouseButtonDownEvent>(deserialized.Events[4]);
        Assert.IsType<RecordedMouseButtonUpEvent>(deserialized.Events[5]);
        Assert.IsType<RecordedMouseScrollEvent>(deserialized.Events[6]);
        Assert.IsType<RecordedGamepadButtonDownEvent>(deserialized.Events[7]);
        Assert.IsType<RecordedGamepadButtonUpEvent>(deserialized.Events[8]);
        Assert.IsType<RecordedGamepadAxisEvent>(deserialized.Events[9]);
    }

    [Fact]
    public void JsonSerialization_SupportsAllEventTypes()
    {
        var recording = new InputRecording();
        recording.Add(new RecordedKeyDownEvent(100f, Key.W, KeyModifiers.Control, true));
        recording.Add(new RecordedKeyUpEvent(200f, Key.W, KeyModifiers.Control));
        recording.Add(new RecordedTextInputEvent(300f, 'a'));
        recording.Add(new RecordedMouseMoveEvent(400f, 100f, 200f, 10f, 20f));
        recording.Add(new RecordedMouseButtonDownEvent(500f, MouseButton.Left, 150f, 250f, KeyModifiers.Shift));
        recording.Add(new RecordedMouseButtonUpEvent(600f, MouseButton.Left, 150f, 250f, KeyModifiers.Shift));
        recording.Add(new RecordedMouseScrollEvent(700f, 1f, -1f, 150f, 250f));
        recording.Add(new RecordedGamepadButtonDownEvent(800f, 0, GamepadButton.South));
        recording.Add(new RecordedGamepadButtonUpEvent(900f, 0, GamepadButton.South));
        recording.Add(new RecordedGamepadAxisEvent(1000f, 0, GamepadAxis.LeftStickX, 0.5f, 0.0f));

        var json = recording.ToJson();
        var deserialized = InputRecording.FromJson(json);

        Assert.Equal(10, deserialized.Count);
        Assert.IsType<RecordedKeyDownEvent>(deserialized.Events[0]);
        Assert.IsType<RecordedKeyUpEvent>(deserialized.Events[1]);
        Assert.IsType<RecordedTextInputEvent>(deserialized.Events[2]);
        Assert.IsType<RecordedMouseMoveEvent>(deserialized.Events[3]);
        Assert.IsType<RecordedMouseButtonDownEvent>(deserialized.Events[4]);
        Assert.IsType<RecordedMouseButtonUpEvent>(deserialized.Events[5]);
        Assert.IsType<RecordedMouseScrollEvent>(deserialized.Events[6]);
        Assert.IsType<RecordedGamepadButtonDownEvent>(deserialized.Events[7]);
        Assert.IsType<RecordedGamepadButtonUpEvent>(deserialized.Events[8]);
        Assert.IsType<RecordedGamepadAxisEvent>(deserialized.Events[9]);
    }

    #endregion

    #region Metadata

    [Fact]
    public void Metadata_CanBeSet()
    {
        var recording = new InputRecording
        {
            Metadata = new InputRecordingMetadata
            {
                Name = "Test",
                Description = "Description",
                RecordedAt = DateTime.UtcNow
            }
        };

        Assert.Equal("Test", recording.Metadata.Name);
        Assert.Equal("Description", recording.Metadata.Description);
        Assert.NotNull(recording.Metadata.RecordedAt);
    }

    #endregion

    #region Recorded Event Property Tests

    [Fact]
    public void RecordedKeyDownEvent_StoresAllProperties()
    {
        var evt = new RecordedKeyDownEvent(150f, Key.Space, KeyModifiers.Shift | KeyModifiers.Control, true);

        Assert.Equal(150f, evt.Timestamp);
        Assert.Equal(Key.Space, evt.Key);
        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Control, evt.Modifiers);
        Assert.True(evt.IsRepeat);
    }

    [Fact]
    public void RecordedKeyUpEvent_StoresAllProperties()
    {
        var evt = new RecordedKeyUpEvent(250f, Key.Enter, KeyModifiers.Alt);

        Assert.Equal(250f, evt.Timestamp);
        Assert.Equal(Key.Enter, evt.Key);
        Assert.Equal(KeyModifiers.Alt, evt.Modifiers);
    }

    [Fact]
    public void RecordedTextInputEvent_StoresCharacter()
    {
        var evt = new RecordedTextInputEvent(100f, 'X');

        Assert.Equal(100f, evt.Timestamp);
        Assert.Equal('X', evt.Character);
    }

    [Fact]
    public void RecordedMouseMoveEvent_StoresAllProperties()
    {
        var evt = new RecordedMouseMoveEvent(200f, 100.5f, 200.5f, 5.0f, -3.0f);

        Assert.Equal(200f, evt.Timestamp);
        Assert.Equal(100.5f, evt.PositionX);
        Assert.Equal(200.5f, evt.PositionY);
        Assert.Equal(5.0f, evt.DeltaX);
        Assert.Equal(-3.0f, evt.DeltaY);
    }

    [Fact]
    public void RecordedMouseButtonDownEvent_StoresAllProperties()
    {
        var evt = new RecordedMouseButtonDownEvent(300f, MouseButton.Right, 150f, 250f, KeyModifiers.Control);

        Assert.Equal(300f, evt.Timestamp);
        Assert.Equal(MouseButton.Right, evt.Button);
        Assert.Equal(150f, evt.PositionX);
        Assert.Equal(250f, evt.PositionY);
        Assert.Equal(KeyModifiers.Control, evt.Modifiers);
    }

    [Fact]
    public void RecordedMouseButtonUpEvent_StoresAllProperties()
    {
        var evt = new RecordedMouseButtonUpEvent(400f, MouseButton.Middle, 175f, 275f, KeyModifiers.Shift);

        Assert.Equal(400f, evt.Timestamp);
        Assert.Equal(MouseButton.Middle, evt.Button);
        Assert.Equal(175f, evt.PositionX);
        Assert.Equal(275f, evt.PositionY);
        Assert.Equal(KeyModifiers.Shift, evt.Modifiers);
    }

    [Fact]
    public void RecordedMouseScrollEvent_StoresAllProperties()
    {
        var evt = new RecordedMouseScrollEvent(500f, 0f, 120f, 300f, 400f);

        Assert.Equal(500f, evt.Timestamp);
        Assert.Equal(0f, evt.DeltaX);
        Assert.Equal(120f, evt.DeltaY);
        Assert.Equal(300f, evt.PositionX);
        Assert.Equal(400f, evt.PositionY);
    }

    [Fact]
    public void RecordedGamepadButtonDownEvent_StoresAllProperties()
    {
        var evt = new RecordedGamepadButtonDownEvent(600f, 1, GamepadButton.East);

        Assert.Equal(600f, evt.Timestamp);
        Assert.Equal(1, evt.GamepadIndex);
        Assert.Equal(GamepadButton.East, evt.Button);
    }

    [Fact]
    public void RecordedGamepadButtonUpEvent_StoresAllProperties()
    {
        var evt = new RecordedGamepadButtonUpEvent(700f, 2, GamepadButton.West);

        Assert.Equal(700f, evt.Timestamp);
        Assert.Equal(2, evt.GamepadIndex);
        Assert.Equal(GamepadButton.West, evt.Button);
    }

    [Fact]
    public void RecordedGamepadAxisEvent_StoresAllProperties()
    {
        var evt = new RecordedGamepadAxisEvent(800f, 0, GamepadAxis.RightStickY, 0.75f, 0.5f);

        Assert.Equal(800f, evt.Timestamp);
        Assert.Equal(0, evt.GamepadIndex);
        Assert.Equal(GamepadAxis.RightStickY, evt.Axis);
        Assert.Equal(0.75f, evt.Value);
        Assert.Equal(0.5f, evt.PreviousValue);
    }

    [Fact]
    public void RecordedInputEvent_EqualityByValue()
    {
        var evt1 = new RecordedKeyDownEvent(100f, Key.A, KeyModifiers.None, false);
        var evt2 = new RecordedKeyDownEvent(100f, Key.A, KeyModifiers.None, false);
        var evt3 = new RecordedKeyDownEvent(100f, Key.B, KeyModifiers.None, false);

        Assert.Equal(evt1, evt2);
        Assert.NotEqual(evt1, evt3);
    }

    [Fact]
    public void RecordedInputEvent_HashCodeConsistent()
    {
        var evt1 = new RecordedMouseScrollEvent(100f, 1f, 2f, 3f, 4f);
        var evt2 = new RecordedMouseScrollEvent(100f, 1f, 2f, 3f, 4f);

        Assert.Equal(evt1.GetHashCode(), evt2.GetHashCode());
    }

    #endregion
}
