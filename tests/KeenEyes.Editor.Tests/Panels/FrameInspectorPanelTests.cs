// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Panels;
using KeenEyes.Replay;

namespace KeenEyes.Editor.Tests.Panels;

/// <summary>
/// Tests for <see cref="FrameInspectorPanel"/> helper functions and state components.
/// </summary>
public sealed class FrameInspectorPanelTests
{
    #region State Component Tests

    [Fact]
    public void FrameInspectorPanelState_DefaultValues()
    {
        // Arrange
        var state = new FrameInspectorPanelState();

        // Assert - Default values for filter flags
        Assert.False(state.ShowEntityEvents);
        Assert.False(state.ShowComponentEvents);
        Assert.False(state.ShowSystemEvents);
        Assert.False(state.ShowCustomEvents);
        Assert.Null(state.FilterEntityId);
        Assert.Equal(0, state.ExpandedEventIndex);
    }

    [Fact]
    public void FrameInspectorHeaderState_CanStoreEntities()
    {
        // Arrange
        var state = new FrameInspectorHeaderState
        {
            FrameLabel = new Entity(1, 0),
            DeltaLabel = new Entity(2, 0)
        };

        // Assert - Entity handles store values
        Assert.Equal(1, state.FrameLabel.Id);
        Assert.Equal(2, state.DeltaLabel.Id);
    }

    [Fact]
    public void FrameInspectorToolbarState_CanStoreEntities()
    {
        // Arrange
        var state = new FrameInspectorToolbarState
        {
            EntityFilterButton = new Entity(1, 0),
            ComponentFilterButton = new Entity(2, 0),
            SystemFilterButton = new Entity(3, 0),
            CustomFilterButton = new Entity(4, 0),
            EventCountLabel = new Entity(5, 0)
        };

        // Assert - Entity handles store values
        Assert.Equal(1, state.EntityFilterButton.Id);
        Assert.Equal(2, state.ComponentFilterButton.Id);
        Assert.Equal(3, state.SystemFilterButton.Id);
        Assert.Equal(4, state.CustomFilterButton.Id);
        Assert.Equal(5, state.EventCountLabel.Id);
    }

    [Fact]
    public void FrameInspectorEventTag_StoresEventIndex()
    {
        // Arrange
        var tag = new FrameInspectorEventTag { EventIndex = 42 };

        // Assert
        Assert.Equal(42, tag.EventIndex);
    }

    [Fact]
    public void FrameInspectorFilterButtonTag_StoresFilterType()
    {
        // Arrange
        var tag = new FrameInspectorFilterButtonTag { FilterType = "EntityFilter" };

        // Assert
        Assert.Equal("EntityFilter", tag.FilterType);
    }

    #endregion

    #region ReplayEvent Type Tests

    [Fact]
    public void ReplayEventType_EntityCreated_HasCorrectValue()
    {
        // Assert
        Assert.Equal(5, (int)ReplayEventType.EntityCreated);
    }

    [Fact]
    public void ReplayEventType_EntityDestroyed_HasCorrectValue()
    {
        // Assert
        Assert.Equal(6, (int)ReplayEventType.EntityDestroyed);
    }

    [Fact]
    public void ReplayEventType_ComponentAdded_HasCorrectValue()
    {
        // Assert
        Assert.Equal(7, (int)ReplayEventType.ComponentAdded);
    }

    [Fact]
    public void ReplayEventType_ComponentRemoved_HasCorrectValue()
    {
        // Assert
        Assert.Equal(8, (int)ReplayEventType.ComponentRemoved);
    }

    [Fact]
    public void ReplayEventType_ComponentChanged_HasCorrectValue()
    {
        // Assert
        Assert.Equal(9, (int)ReplayEventType.ComponentChanged);
    }

    [Fact]
    public void ReplayEventType_SystemStart_HasCorrectValue()
    {
        // Assert
        Assert.Equal(3, (int)ReplayEventType.SystemStart);
    }

    [Fact]
    public void ReplayEventType_SystemEnd_HasCorrectValue()
    {
        // Assert
        Assert.Equal(4, (int)ReplayEventType.SystemEnd);
    }

    [Fact]
    public void ReplayEventType_Custom_HasCorrectValue()
    {
        // Assert
        Assert.Equal(0, (int)ReplayEventType.Custom);
    }

    [Fact]
    public void ReplayEventType_Snapshot_HasCorrectValue()
    {
        // Assert
        Assert.Equal(10, (int)ReplayEventType.Snapshot);
    }

    #endregion

    #region ReplayEvent Construction Tests

    [Fact]
    public void ReplayEvent_EntityCreated_CanBeConstructed()
    {
        // Arrange
        var evt = new ReplayEvent
        {
            Type = ReplayEventType.EntityCreated,
            EntityId = 42,
            Timestamp = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        Assert.Equal(ReplayEventType.EntityCreated, evt.Type);
        Assert.Equal(42, evt.EntityId);
        Assert.Equal(TimeSpan.FromMilliseconds(100), evt.Timestamp);
    }

    [Fact]
    public void ReplayEvent_ComponentChanged_WithData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["before"] = 100,
            ["after"] = 85
        };

        var evt = new ReplayEvent
        {
            Type = ReplayEventType.ComponentChanged,
            EntityId = 1,
            ComponentTypeName = "Health",
            Data = data
        };

        // Assert
        Assert.Equal(ReplayEventType.ComponentChanged, evt.Type);
        Assert.Equal(1, evt.EntityId);
        Assert.Equal("Health", evt.ComponentTypeName);
        Assert.NotNull(evt.Data);
        Assert.Equal(100, evt.Data["before"]);
        Assert.Equal(85, evt.Data["after"]);
    }

    [Fact]
    public void ReplayEvent_SystemStart_WithTypeName()
    {
        // Arrange
        var evt = new ReplayEvent
        {
            Type = ReplayEventType.SystemStart,
            SystemTypeName = "KeenEyes.Game.MovementSystem"
        };

        // Assert
        Assert.Equal(ReplayEventType.SystemStart, evt.Type);
        Assert.Equal("KeenEyes.Game.MovementSystem", evt.SystemTypeName);
    }

    [Fact]
    public void ReplayEvent_Custom_WithCustomType()
    {
        // Arrange
        var evt = new ReplayEvent
        {
            Type = ReplayEventType.Custom,
            CustomType = "PlayerJumped",
            Data = new Dictionary<string, object> { ["height"] = 2.5f }
        };

        // Assert
        Assert.Equal(ReplayEventType.Custom, evt.Type);
        Assert.Equal("PlayerJumped", evt.CustomType);
        Assert.NotNull(evt.Data);
        Assert.Equal(2.5f, evt.Data["height"]);
    }

    #endregion

    #region ReplayFrame Tests

    [Fact]
    public void ReplayFrame_CanBeConstructed()
    {
        // Arrange
        var events = new List<ReplayEvent>
        {
            new() { Type = ReplayEventType.FrameStart },
            new() { Type = ReplayEventType.EntityCreated, EntityId = 1 },
            new() { Type = ReplayEventType.FrameEnd }
        };

        var frame = new ReplayFrame
        {
            FrameNumber = 0,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.Zero,
            Events = events
        };

        // Assert
        Assert.Equal(0, frame.FrameNumber);
        Assert.Equal(TimeSpan.FromMilliseconds(16.67), frame.DeltaTime);
        Assert.Equal(TimeSpan.Zero, frame.ElapsedTime);
        Assert.Equal(3, frame.Events.Count);
    }

    [Fact]
    public void ReplayFrame_WithPrecedingSnapshotIndex()
    {
        // Arrange
        var frame = new ReplayFrame
        {
            FrameNumber = 100,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.FromSeconds(1.667),
            Events = [],
            PrecedingSnapshotIndex = 0
        };

        // Assert
        Assert.Equal(100, frame.FrameNumber);
        Assert.Equal(0, frame.PrecedingSnapshotIndex);
    }

    [Fact]
    public void ReplayFrame_WithoutSnapshot()
    {
        // Arrange
        var frame = new ReplayFrame
        {
            FrameNumber = 50,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.FromMilliseconds(833.5),
            Events = []
        };

        // Assert
        Assert.Null(frame.PrecedingSnapshotIndex);
    }

    #endregion

    #region ReplayPlayer State Tests

    [Fact]
    public void ReplayPlayer_InitialState_IsStopped()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Assert
        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.False(player.IsLoaded);
        Assert.Equal(-1, player.CurrentFrame);
        Assert.Equal(0, player.TotalFrames);
    }

    [Fact]
    public void ReplayPlayer_PlaybackSpeed_DefaultIsOne()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Assert
        Assert.Equal(1.0f, player.PlaybackSpeed);
    }

    [Fact]
    public void ReplayPlayer_PlaybackSpeed_CanBeSet()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.PlaybackSpeed = 2.0f;

        // Assert
        Assert.Equal(2.0f, player.PlaybackSpeed);
    }

    [Fact]
    public void ReplayPlayer_PlaybackSpeed_EnforcesMinimum()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 0.1f);
    }

    [Fact]
    public void ReplayPlayer_PlaybackSpeed_EnforcesMaximum()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.PlaybackSpeed = 5.0f);
    }

    [Fact]
    public void ReplayPlayer_LoadReplayData_SetsIsLoaded()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replayData = CreateTestReplayData();

        // Act
        player.LoadReplay(replayData);

        // Assert
        Assert.True(player.IsLoaded);
        Assert.Equal(0, player.CurrentFrame);
        Assert.Equal(replayData.FrameCount, player.TotalFrames);
    }

    [Fact]
    public void ReplayPlayer_UnloadReplay_ClearsState()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act
        player.UnloadReplay();

        // Assert
        Assert.False(player.IsLoaded);
        Assert.Equal(-1, player.CurrentFrame);
        Assert.Equal(0, player.TotalFrames);
    }

    [Fact]
    public void ReplayPlayer_Play_WithNoReplay_ThrowsInvalidOperation()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.Play());
    }

    [Fact]
    public void ReplayPlayer_Play_TransitionsToPlaying()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act
        player.Play();

        // Assert
        Assert.Equal(PlaybackState.Playing, player.State);
    }

    [Fact]
    public void ReplayPlayer_Pause_TransitionsToPaused()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        // Act
        player.Pause();

        // Assert
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void ReplayPlayer_Stop_TransitionsToStopped()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        // Act
        player.Stop();

        // Assert
        Assert.Equal(PlaybackState.Stopped, player.State);
        Assert.Equal(0, player.CurrentFrame);
    }

    [Fact]
    public void ReplayPlayer_SeekToFrame_SetsCurrentFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act
        player.SeekToFrame(5);

        // Assert
        Assert.Equal(5, player.CurrentFrame);
        // Seeking from Stopped state stays Stopped
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void ReplayPlayer_SeekToFrame_WhilePlaying_PausesPlayback()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        // Act
        player.SeekToFrame(5);

        // Assert
        Assert.Equal(5, player.CurrentFrame);
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void ReplayPlayer_Step_AdvancesFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act
        player.Step(3);

        // Assert
        Assert.Equal(3, player.CurrentFrame);
        // Step from Stopped state stays Stopped
        Assert.Equal(PlaybackState.Stopped, player.State);
    }

    [Fact]
    public void ReplayPlayer_Step_WhilePlaying_PausesPlayback()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        // Act
        player.Step(1);

        // Assert
        Assert.Equal(PlaybackState.Paused, player.State);
    }

    [Fact]
    public void ReplayPlayer_StepNegative_GoesBackward()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.SeekToFrame(5);

        // Act
        player.Step(-2);

        // Assert
        Assert.Equal(3, player.CurrentFrame);
    }

    [Fact]
    public void ReplayPlayer_GetCurrentFrame_ReturnsFrameData()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.SeekToFrame(2);

        // Act
        var frame = player.GetCurrentFrame();

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(2, frame.FrameNumber);
    }

    [Fact]
    public void ReplayPlayer_GetFrame_ReturnsSpecificFrame()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act
        var frame = player.GetFrame(3);

        // Assert
        Assert.Equal(3, frame.FrameNumber);
    }

    [Fact]
    public void ReplayPlayer_GetFrame_OutOfRange_Throws()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.GetFrame(100));
    }

    #endregion

    #region Event Subscription Tests

    [Fact]
    public void ReplayPlayer_FrameChangedEvent_FiresOnSeek()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        var firedFrame = -1;
        player.FrameChanged += frame => firedFrame = frame;

        // Act
        player.SeekToFrame(5);

        // Assert
        Assert.Equal(5, firedFrame);
    }

    [Fact]
    public void ReplayPlayer_PlaybackStartedEvent_FiresOnPlay()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());

        var fired = false;
        player.PlaybackStarted += () => fired = true;

        // Act
        player.Play();

        // Assert
        Assert.True(fired);
    }

    [Fact]
    public void ReplayPlayer_PlaybackPausedEvent_FiresOnPause()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        var fired = false;
        player.PlaybackPaused += () => fired = true;

        // Act
        player.Pause();

        // Assert
        Assert.True(fired);
    }

    [Fact]
    public void ReplayPlayer_PlaybackStoppedEvent_FiresOnStop()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.LoadReplay(CreateTestReplayData());
        player.Play();

        var fired = false;
        player.PlaybackStopped += () => fired = true;

        // Act
        player.Stop();

        // Assert
        Assert.True(fired);
    }

    #endregion

    #region Helper Methods

    private static ReplayData CreateTestReplayData()
    {
        var frames = new List<ReplayFrame>();
        var elapsedTime = TimeSpan.Zero;
        var deltaTime = TimeSpan.FromMilliseconds(16.67);

        for (int i = 0; i < 10; i++)
        {
            var events = new List<ReplayEvent>
            {
                new() { Type = ReplayEventType.FrameStart, Timestamp = TimeSpan.Zero }
            };

            // Add some sample events
            if (i == 1)
            {
                events.Add(new ReplayEvent
                {
                    Type = ReplayEventType.EntityCreated,
                    EntityId = 42,
                    Timestamp = TimeSpan.FromMilliseconds(1)
                });
            }

            if (i == 5)
            {
                events.Add(new ReplayEvent
                {
                    Type = ReplayEventType.ComponentChanged,
                    EntityId = 42,
                    ComponentTypeName = "Health",
                    Data = new Dictionary<string, object>
                    {
                        ["Current"] = new Dictionary<string, object>
                        {
                            ["before"] = 100,
                            ["after"] = 85
                        }
                    },
                    Timestamp = TimeSpan.FromMilliseconds(8)
                });
            }

            events.Add(new ReplayEvent { Type = ReplayEventType.FrameEnd, Timestamp = deltaTime });

            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = deltaTime,
                ElapsedTime = elapsedTime,
                Events = events
            });

            elapsedTime += deltaTime;
        }

        return new ReplayData
        {
            Name = "Test Replay",
            Description = "Test replay for unit tests",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = elapsedTime,
            FrameCount = frames.Count,
            Frames = frames,
            Snapshots = []
        };
    }

    #endregion
}
