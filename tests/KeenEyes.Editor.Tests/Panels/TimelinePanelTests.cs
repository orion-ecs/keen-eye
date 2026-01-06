// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Panels;

namespace KeenEyes.Editor.Tests.Panels;

/// <summary>
/// Tests for <see cref="TimelinePanel"/> functionality.
/// </summary>
public sealed class TimelinePanelTests
{
    #region GetSpeedValue Tests

    [Fact]
    public void GetSpeedValue_AtIndex0_ReturnsQuarterSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(0);

        // Assert
        Assert.Equal(0.25f, speed);
    }

    [Fact]
    public void GetSpeedValue_AtIndex1_ReturnsHalfSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(1);

        // Assert
        Assert.Equal(0.5f, speed);
    }

    [Fact]
    public void GetSpeedValue_AtIndex2_ReturnsNormalSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(2);

        // Assert
        Assert.Equal(1f, speed);
    }

    [Fact]
    public void GetSpeedValue_AtIndex3_ReturnsDoubleSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(3);

        // Assert
        Assert.Equal(2f, speed);
    }

    [Fact]
    public void GetSpeedValue_AtIndex4_ReturnsQuadrupleSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(4);

        // Assert
        Assert.Equal(4f, speed);
    }

    [Fact]
    public void GetSpeedValue_NegativeIndex_ReturnsDefaultNormalSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(-1);

        // Assert
        Assert.Equal(1f, speed);
    }

    [Fact]
    public void GetSpeedValue_IndexOutOfRange_ReturnsDefaultNormalSpeed()
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(100);

        // Assert
        Assert.Equal(1f, speed);
    }

    [Theory]
    [InlineData(0, 0.25f)]
    [InlineData(1, 0.5f)]
    [InlineData(2, 1f)]
    [InlineData(3, 2f)]
    [InlineData(4, 4f)]
    public void GetSpeedValue_AllValidIndices_ReturnCorrectValues(int index, float expectedSpeed)
    {
        // Act
        var speed = TimelinePanel.GetSpeedValue(index);

        // Assert
        Assert.Equal(expectedSpeed, speed);
    }

    #endregion

    #region Speed Value Consistency Tests

    [Fact]
    public void GetSpeedValue_AllValidIndices_AreInAscendingOrder()
    {
        // Act
        var speeds = Enumerable.Range(0, 5).Select(TimelinePanel.GetSpeedValue).ToList();

        // Assert - Each speed should be greater than the previous
        for (int i = 1; i < speeds.Count; i++)
        {
            Assert.True(speeds[i] > speeds[i - 1],
                $"Speed at index {i} ({speeds[i]}) should be greater than speed at index {i - 1} ({speeds[i - 1]})");
        }
    }

    [Fact]
    public void GetSpeedValue_AllValidIndices_ArePositive()
    {
        // Assert - All speeds are positive
        for (int i = 0; i < 5; i++)
        {
            var speed = TimelinePanel.GetSpeedValue(i);
            Assert.True(speed > 0, $"Speed at index {i} should be positive");
        }
    }

    [Fact]
    public void GetSpeedValue_DefaultSpeed_IsAtMiddleIndex()
    {
        // The default 1x speed should be at index 2 (middle of 0-4)
        var speed = TimelinePanel.GetSpeedValue(2);
        Assert.Equal(1f, speed);
    }

    #endregion

    #region State Component Tests

    [Fact]
    public void TimelinePanelState_IsStruct()
    {
        // Arrange & Act
        var type = typeof(TimelinePanelState);

        // Assert
        Assert.True(type.IsValueType);
    }

    [Fact]
    public void TimelinePanelState_DefaultValues_AreDefault()
    {
        // Arrange
        var state = new TimelinePanelState();

        // Assert - Primitive fields should have default values
        // Entity fields default to Entity(0, 0) which has IsValid = true (Id >= 0)
        // This is expected behavior - Entity.Null (-1, 0) is the invalid sentinel
        Assert.Null(state.ReplayPlayer);
        Assert.Equal(0, state.CurrentSpeedIndex);
        Assert.Equal(default, state.Font);
    }

    [Fact]
    public void TimelineScrubberTag_IsStruct()
    {
        // Arrange & Act
        var type = typeof(TimelineScrubberTag);

        // Assert
        Assert.True(type.IsValueType);
    }

    [Fact]
    public void TimelineSnapshotTrackTag_IsStruct()
    {
        // Arrange & Act
        var type = typeof(TimelineSnapshotTrackTag);

        // Assert
        Assert.True(type.IsValueType);
    }

    [Fact]
    public void TimelineEventTrackTag_IsStruct()
    {
        // Arrange & Act
        var type = typeof(TimelineEventTrackTag);

        // Assert
        Assert.True(type.IsValueType);
    }

    [Fact]
    public void TimelineMarkerTag_StoresFrameNumber()
    {
        // Arrange
        var tag = new TimelineMarkerTag { FrameNumber = 42 };

        // Assert
        Assert.Equal(42, tag.FrameNumber);
    }

    [Fact]
    public void TimelineMarkerTag_DefaultFrameNumberIsZero()
    {
        // Arrange
        var tag = new TimelineMarkerTag();

        // Assert
        Assert.Equal(0, tag.FrameNumber);
    }

    #endregion
}
