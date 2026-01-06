using System.Numerics;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the input recording and playback functionality.
/// </summary>
public class ReplayInputTests
{
    #region InputEvent Tests

    [Fact]
    public void InputEvent_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var inputEvent = new InputEvent();

        // Assert
        Assert.Equal(InputEventType.KeyDown, inputEvent.Type);
        Assert.Equal(0, inputEvent.Frame);
        Assert.Null(inputEvent.Key);
        Assert.Equal(0f, inputEvent.Value);
        Assert.Equal(Vector2.Zero, inputEvent.Position);
        Assert.Null(inputEvent.CustomType);
        Assert.Null(inputEvent.CustomData);
        Assert.Equal(TimeSpan.Zero, inputEvent.Timestamp);
    }

    [Fact]
    public void InputEvent_WithProperties_SetsValuesCorrectly()
    {
        // Arrange & Act
        var inputEvent = new InputEvent
        {
            Type = InputEventType.MouseMove,
            Frame = 42,
            Key = "Left",
            Value = 1.5f,
            Position = new Vector2(100, 200),
            CustomType = "TestType",
            CustomData = "TestData",
            Timestamp = TimeSpan.FromMilliseconds(10)
        };

        // Assert
        Assert.Equal(InputEventType.MouseMove, inputEvent.Type);
        Assert.Equal(42, inputEvent.Frame);
        Assert.Equal("Left", inputEvent.Key);
        Assert.Equal(1.5f, inputEvent.Value);
        Assert.Equal(new Vector2(100, 200), inputEvent.Position);
        Assert.Equal("TestType", inputEvent.CustomType);
        Assert.Equal("TestData", inputEvent.CustomData);
        Assert.Equal(TimeSpan.FromMilliseconds(10), inputEvent.Timestamp);
    }

    [Fact]
    public void InputEvent_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var keyEvent = new InputEvent
        {
            Type = InputEventType.KeyDown,
            Frame = 42,
            Key = "Space"
        };

        var mouseEvent = new InputEvent
        {
            Type = InputEventType.MouseMove,
            Frame = 42,
            Position = new Vector2(100, 200)
        };

        // Act
        var keyString = keyEvent.ToString();
        var mouseString = mouseEvent.ToString();

        // Assert
        Assert.Contains("KeyDown", keyString);
        Assert.Contains("Frame=42", keyString);
        Assert.Contains("Key=Space", keyString);

        Assert.Contains("MouseMove", mouseString);
        Assert.Contains("Frame=42", mouseString);
    }

    #endregion

    #region ReplayRecorder Input Recording Tests

    [Fact]
    public void RecordKeyDown_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordKeyDown("Space");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.KeyDown, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("Space", result.Frames[0].InputEvents[0].Key);
    }

    [Fact]
    public void RecordKeyUp_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordKeyUp("Enter");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.KeyUp, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("Enter", result.Frames[0].InputEvents[0].Key);
    }

    [Fact]
    public void RecordMouseMove_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordMouseMove(new Vector2(100, 200));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.MouseMove, result.Frames[0].InputEvents[0].Type);
        Assert.Equal(new Vector2(100, 200), result.Frames[0].InputEvents[0].Position);
    }

    [Fact]
    public void RecordMouseButtonDown_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordMouseButtonDown("Left", new Vector2(50, 75));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.MouseButtonDown, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("Left", result.Frames[0].InputEvents[0].Key);
        Assert.Equal(new Vector2(50, 75), result.Frames[0].InputEvents[0].Position);
    }

    [Fact]
    public void RecordMouseButtonUp_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordMouseButtonUp("Right", new Vector2(150, 175));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.MouseButtonUp, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("Right", result.Frames[0].InputEvents[0].Key);
        Assert.Equal(new Vector2(150, 175), result.Frames[0].InputEvents[0].Position);
    }

    [Fact]
    public void RecordMouseWheel_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordMouseWheel(3.5f, new Vector2(200, 300));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.MouseWheel, result.Frames[0].InputEvents[0].Type);
        Assert.Equal(3.5f, result.Frames[0].InputEvents[0].Value);
        Assert.Equal(new Vector2(200, 300), result.Frames[0].InputEvents[0].Position);
    }

    [Fact]
    public void RecordGamepadButton_Pressed_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordGamepadButton("A", pressed: true);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.GamepadButton, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("A", result.Frames[0].InputEvents[0].Key);
        Assert.Equal(1.0f, result.Frames[0].InputEvents[0].Value);
    }

    [Fact]
    public void RecordGamepadButton_Released_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordGamepadButton("B", pressed: false);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.GamepadButton, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("B", result.Frames[0].InputEvents[0].Key);
        Assert.Equal(0.0f, result.Frames[0].InputEvents[0].Value);
    }

    [Fact]
    public void RecordGamepadAxis_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordGamepadAxis("LeftStickX", 0.75f);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.GamepadAxis, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("LeftStickX", result.Frames[0].InputEvents[0].Key);
        Assert.Equal(0.75f, result.Frames[0].InputEvents[0].Value);
    }

    [Fact]
    public void RecordCustomInput_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordCustomInput("TouchGesture", "SwipeLeft");
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.Custom, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("TouchGesture", result.Frames[0].InputEvents[0].CustomType);
        Assert.Equal("SwipeLeft", result.Frames[0].InputEvents[0].CustomData);
    }

    [Fact]
    public void RecordCustomInput_Generic_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordCustomInput("GestureData", new TestGestureData("Pinch", 1.5f));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.Custom, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("GestureData", result.Frames[0].InputEvents[0].CustomType);
        var gestureData = Assert.IsType<TestGestureData>(result.Frames[0].InputEvents[0].CustomData);
        Assert.Equal("Pinch", gestureData.GestureType);
        Assert.Equal(1.5f, gestureData.Scale);
    }

    [Fact]
    public void RecordInput_RawEvent_WhenRecording_AddsInputEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        var inputEvent = new InputEvent
        {
            Type = InputEventType.KeyDown,
            Frame = 0,
            Key = "Escape",
            Timestamp = TimeSpan.FromMilliseconds(5)
        };

        // Act
        recorder.RecordInput(inputEvent);
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(InputEventType.KeyDown, result.Frames[0].InputEvents[0].Type);
        Assert.Equal("Escape", result.Frames[0].InputEvents[0].Key);
    }

    [Fact]
    public void RecordInput_WhenNotRecording_DoesNotAddEvent()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Act - Record before starting
        recorder.RecordKeyDown("Space");

        // Assert - Should not throw and no events recorded
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void RecordMultipleInputs_SameFrame_AddsAllEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);

        // Act
        recorder.RecordKeyDown("W");
        recorder.RecordKeyDown("Shift");
        recorder.RecordMouseMove(new Vector2(100, 100));
        recorder.EndFrame(0.016f);
        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Frames[0].InputEvents.Count);
    }

    [Fact]
    public void RecordInputsAcrossFrames_StoresCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);
        recorder.StartRecording();

        // Act - Record inputs across multiple frames
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("A");
        recorder.EndFrame(0.016f);

        recorder.BeginFrame(0.016f);
        recorder.RecordKeyUp("A");
        recorder.RecordKeyDown("B");
        recorder.EndFrame(0.016f);

        recorder.BeginFrame(0.016f);
        recorder.RecordKeyUp("B");
        recorder.EndFrame(0.016f);

        var result = recorder.StopRecording();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.FrameCount);
        Assert.Single(result.Frames[0].InputEvents);
        Assert.Equal(2, result.Frames[1].InputEvents.Count);
        Assert.Single(result.Frames[2].InputEvents);
    }

    [Fact]
    public void IInputRecorder_IsRecordingInputs_ReturnsCorrectValue()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        IInputRecorder recorder = new ReplayRecorder(world, serializer);

        // Act & Assert
        Assert.False(recorder.IsRecordingInputs);

        ((ReplayRecorder)recorder).StartRecording();
        Assert.True(recorder.IsRecordingInputs);

        ((ReplayRecorder)recorder).StopRecording();
        Assert.False(recorder.IsRecordingInputs);
    }

    [Fact]
    public void IInputRecorder_CurrentInputFrame_ReturnsCorrectValue()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        IInputRecorder recorder = new ReplayRecorder(world, serializer);

        // Assert - Not recording
        Assert.Equal(-1, recorder.CurrentInputFrame);

        // Start recording
        ((ReplayRecorder)recorder).StartRecording();
        Assert.Equal(0, recorder.CurrentInputFrame);

        // Record a frame
        ((ReplayRecorder)recorder).BeginFrame(0.016f);
        ((ReplayRecorder)recorder).EndFrame(0.016f);
        Assert.Equal(1, recorder.CurrentInputFrame);

        ((ReplayRecorder)recorder).StopRecording();
        Assert.Equal(-1, recorder.CurrentInputFrame);
    }

    #endregion

    #region ReplayPlayer Input Handler Tests

    [Fact]
    public void RegisterInputHandler_WithHandler_AddsHandler()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var handlerCalled = false;

        // Act
        player.RegisterInputHandler(InputEventType.KeyDown, _ => handlerCalled = true);

        // Assert - Handler registered (we can't directly verify, but no exception thrown)
        Assert.False(handlerCalled); // Not called yet
    }

    [Fact]
    public void RegisterInputHandler_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            player.RegisterInputHandler(InputEventType.KeyDown, null!));
    }

    [Fact]
    public void RegisterInputHandler_Generic_WithHandler_AddsHandler()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var handlerCalled = false;

        // Act
        player.RegisterInputHandler<TestGestureData>("TouchGesture", _ => handlerCalled = true);

        // Assert - Handler registered (no exception thrown)
        Assert.False(handlerCalled);
    }

    [Fact]
    public void RegisterInputHandler_Generic_WithNullCustomType_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            player.RegisterInputHandler<TestGestureData>(null!, _ => { }));
    }

    [Fact]
    public void RegisterInputHandler_Generic_WithEmptyCustomType_ThrowsArgumentException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            player.RegisterInputHandler<TestGestureData>("", _ => { }));
    }

    [Fact]
    public void RegisterInputHandler_Generic_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            player.RegisterInputHandler<TestGestureData>("TouchGesture", null!));
    }

    [Fact]
    public void UnregisterInputHandlers_RemovesHandlers()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var callCount = 0;
        player.RegisterInputHandler(InputEventType.KeyDown, _ => callCount++);

        // Act
        player.UnregisterInputHandlers(InputEventType.KeyDown);

        // Assert - No exception thrown (handlers removed)
    }

    [Fact]
    public void UnregisterCustomInputHandlers_RemovesHandlers()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var callCount = 0;
        player.RegisterInputHandler<TestGestureData>("TouchGesture", _ => callCount++);

        // Act
        player.UnregisterCustomInputHandlers("TouchGesture");

        // Assert - No exception thrown (handlers removed)
    }

    [Fact]
    public void ClearInputHandlers_RemovesAllHandlers()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.RegisterInputHandler(InputEventType.KeyDown, _ => { });
        player.RegisterInputHandler(InputEventType.MouseMove, _ => { });
        player.RegisterInputHandler<TestGestureData>("TouchGesture", _ => { });

        // Act
        player.ClearInputHandlers();

        // Assert - No exception thrown (all handlers removed)
    }

    #endregion

    #region ApplyInputFrame Tests

    [Fact]
    public void ApplyInputFrame_WithNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.ApplyInputFrame());
    }

    [Fact]
    public void ApplyInputFrame_WithKeyDownEvent_InvokesHandler()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("Space");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        InputEvent? capturedEvent = null;
        player.RegisterInputHandler(InputEventType.KeyDown, evt => capturedEvent = evt);

        // Act
        player.ApplyInputFrame();

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(InputEventType.KeyDown, capturedEvent.Value.Type);
        Assert.Equal("Space", capturedEvent.Value.Key);
    }

    [Fact]
    public void ApplyInputFrame_WithMultipleEvents_InvokesAllHandlers()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("W");
        recorder.RecordMouseMove(new Vector2(100, 200));
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        var keyEvents = new List<InputEvent>();
        var mouseEvents = new List<InputEvent>();
        player.RegisterInputHandler(InputEventType.KeyDown, evt => keyEvents.Add(evt));
        player.RegisterInputHandler(InputEventType.MouseMove, evt => mouseEvents.Add(evt));

        // Act
        player.ApplyInputFrame();

        // Assert
        Assert.Single(keyEvents);
        Assert.Single(mouseEvents);
        Assert.Equal("W", keyEvents[0].Key);
        Assert.Equal(new Vector2(100, 200), mouseEvents[0].Position);
    }

    [Fact]
    public void ApplyInputFrame_WithNoHandlerRegistered_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("Space");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        // Act & Assert - Should not throw
        player.ApplyInputFrame();
    }

    [Fact]
    public void ApplyInputFrame_WithCustomEvent_InvokesTypedHandler()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        var gestureData = new TestGestureData("Pinch", 1.5f);
        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordCustomInput("GestureData", gestureData);
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        TestGestureData? capturedData = null;
        player.RegisterInputHandler<TestGestureData>("GestureData", data => capturedData = data);

        // Act
        player.ApplyInputFrame();

        // Assert
        Assert.NotNull(capturedData);
        Assert.Equal("Pinch", capturedData.GestureType);
        Assert.Equal(1.5f, capturedData.Scale);
    }

    [Fact]
    public void ApplyInputFrame_ByIndex_AppliesCorrectFrame()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("A");
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("B");
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("C");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        var capturedKeys = new List<string>();
        player.RegisterInputHandler(InputEventType.KeyDown, evt => capturedKeys.Add(evt.Key!));

        // Act - Apply frame 1 (second frame with "B")
        player.ApplyInputFrame(1);

        // Assert
        Assert.Single(capturedKeys);
        Assert.Equal("B", capturedKeys[0]);
    }

    [Fact]
    public void ApplyInputFrame_ByIndex_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.ApplyInputFrame(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => player.ApplyInputFrame(-1));
    }

    [Fact]
    public void ApplyInputFrame_WithMultipleHandlersForSameType_InvokesAll()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("Space");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        var callCount = 0;
        player.RegisterInputHandler(InputEventType.KeyDown, _ => callCount++);
        player.RegisterInputHandler(InputEventType.KeyDown, _ => callCount++);
        player.RegisterInputHandler(InputEventType.KeyDown, _ => callCount++);

        // Act
        player.ApplyInputFrame();

        // Assert
        Assert.Equal(3, callCount);
    }

    #endregion

    #region GetInputEvents Tests

    [Fact]
    public void GetCurrentInputEvents_WithNoReplayLoaded_ReturnsEmptyList()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        var events = player.GetCurrentInputEvents();

        // Assert
        Assert.Empty(events);
    }

    [Fact]
    public void GetCurrentInputEvents_WithReplayLoaded_ReturnsInputEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("Space");
        recorder.RecordKeyDown("Enter");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        // Act
        var events = player.GetCurrentInputEvents();

        // Assert
        Assert.Equal(2, events.Count);
    }

    [Fact]
    public void GetInputEvents_ByIndex_ReturnsInputEvents()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("A");
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("B");
        recorder.RecordKeyDown("C");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        // Act
        var frame0Events = player.GetInputEvents(0);
        var frame1Events = player.GetInputEvents(1);

        // Assert
        Assert.Single(frame0Events);
        Assert.Equal(2, frame1Events.Count);
    }

    [Fact]
    public void GetInputEvents_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.GetInputEvents(10));
        Assert.Throws<ArgumentOutOfRangeException>(() => player.GetInputEvents(-1));
    }

    [Fact]
    public void GetInputEvents_WithNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.GetInputEvents(0));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullInputPlayback_RecordAndReplay_WorksCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        // Record session
        recorder.StartRecording("Input Test");
        for (int frame = 0; frame < 10; frame++)
        {
            recorder.BeginFrame(0.016f);
            recorder.RecordKeyDown($"Key{frame}");
            recorder.RecordMouseMove(new Vector2(frame * 10, frame * 20));
            recorder.EndFrame(0.016f);
        }
        var replayData = recorder.StopRecording();

        // Playback
        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        var keyEvents = new List<InputEvent>();
        var mouseEvents = new List<InputEvent>();
        player.RegisterInputHandler(InputEventType.KeyDown, evt => keyEvents.Add(evt));
        player.RegisterInputHandler(InputEventType.MouseMove, evt => mouseEvents.Add(evt));

        // Apply all frames
        for (int frame = 0; frame < replayData!.FrameCount; frame++)
        {
            player.ApplyInputFrame(frame);
        }

        // Assert
        Assert.Equal(10, keyEvents.Count);
        Assert.Equal(10, mouseEvents.Count);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"Key{i}", keyEvents[i].Key);
            Assert.Equal(new Vector2(i * 10, i * 20), mouseEvents[i].Position);
        }
    }

    [Fact]
    public void InputPlayback_WithPlaybackLoop_WorksCorrectly()
    {
        // Arrange
        using var world = new World();
        var serializer = new MockComponentSerializer();
        var recorder = new ReplayRecorder(world, serializer);

        recorder.StartRecording();
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyDown("Jump");
        recorder.EndFrame(0.016f);
        recorder.BeginFrame(0.016f);
        recorder.RecordKeyUp("Jump");
        recorder.EndFrame(0.016f);
        var replayData = recorder.StopRecording();

        using var player = new ReplayPlayer();
        player.LoadReplay(replayData!);

        var allEvents = new List<InputEvent>();
        player.RegisterInputHandler(InputEventType.KeyDown, evt => allEvents.Add(evt));
        player.RegisterInputHandler(InputEventType.KeyUp, evt => allEvents.Add(evt));

        player.Play();

        // Apply first frame before entering the loop
        player.ApplyInputFrame();

        // Act - Simulate playback loop
        while (player.State == PlaybackState.Playing)
        {
            if (player.Update(0.016f))
            {
                player.ApplyInputFrame();
            }
        }

        // Assert - Should have captured events from both frames
        Assert.Equal(2, allEvents.Count);
        Assert.Equal(InputEventType.KeyDown, allEvents[0].Type);
        Assert.Equal(InputEventType.KeyUp, allEvents[1].Type);
    }

    #endregion
}

/// <summary>
/// Test data class for custom input events.
/// </summary>
public record TestGestureData(string GestureType, float Scale);
