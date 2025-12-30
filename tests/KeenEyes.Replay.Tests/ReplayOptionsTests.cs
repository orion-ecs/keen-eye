namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="ReplayOptions"/> record.
/// </summary>
public class ReplayOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultOptions_HasExpectedDefaults()
    {
        // Arrange & Act
        var options = new ReplayOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), options.SnapshotInterval);
        Assert.True(options.RecordSystemEvents);
        Assert.True(options.RecordEntityEvents);
        Assert.True(options.RecordComponentEvents);
        Assert.Null(options.SystemEventPhase);
        Assert.Null(options.MaxFrames);
        Assert.Null(options.MaxDuration);
        Assert.False(options.UseRingBuffer);
        Assert.Null(options.DefaultRecordingName);
    }

    #endregion

    #region SnapshotInterval Tests

    [Fact]
    public void SnapshotInterval_CanBeSetToCustomValue()
    {
        // Arrange & Act
        var options = new ReplayOptions { SnapshotInterval = TimeSpan.FromSeconds(5) };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), options.SnapshotInterval);
    }

    [Fact]
    public void SnapshotInterval_CanBeSetToZero()
    {
        // Arrange & Act
        var options = new ReplayOptions { SnapshotInterval = TimeSpan.Zero };

        // Assert
        Assert.Equal(TimeSpan.Zero, options.SnapshotInterval);
    }

    #endregion

    #region Event Recording Flags Tests

    [Fact]
    public void RecordSystemEvents_CanBeDisabled()
    {
        // Arrange & Act
        var options = new ReplayOptions { RecordSystemEvents = false };

        // Assert
        Assert.False(options.RecordSystemEvents);
    }

    [Fact]
    public void RecordEntityEvents_CanBeDisabled()
    {
        // Arrange & Act
        var options = new ReplayOptions { RecordEntityEvents = false };

        // Assert
        Assert.False(options.RecordEntityEvents);
    }

    [Fact]
    public void RecordComponentEvents_CanBeDisabled()
    {
        // Arrange & Act
        var options = new ReplayOptions { RecordComponentEvents = false };

        // Assert
        Assert.False(options.RecordComponentEvents);
    }

    #endregion

    #region SystemEventPhase Tests

    [Fact]
    public void SystemEventPhase_CanBeSetToUpdate()
    {
        // Arrange & Act
        var options = new ReplayOptions { SystemEventPhase = SystemPhase.Update };

        // Assert
        Assert.Equal(SystemPhase.Update, options.SystemEventPhase);
    }

    [Fact]
    public void SystemEventPhase_CanBeSetToFixedUpdate()
    {
        // Arrange & Act
        var options = new ReplayOptions { SystemEventPhase = SystemPhase.FixedUpdate };

        // Assert
        Assert.Equal(SystemPhase.FixedUpdate, options.SystemEventPhase);
    }

    [Fact]
    public void SystemEventPhase_CanBeSetToLateUpdate()
    {
        // Arrange & Act
        var options = new ReplayOptions { SystemEventPhase = SystemPhase.LateUpdate };

        // Assert
        Assert.Equal(SystemPhase.LateUpdate, options.SystemEventPhase);
    }

    #endregion

    #region MaxFrames Tests

    [Fact]
    public void MaxFrames_CanBeSetToValue()
    {
        // Arrange & Act
        var options = new ReplayOptions { MaxFrames = 1000 };

        // Assert
        Assert.Equal(1000, options.MaxFrames);
    }

    [Fact]
    public void MaxFrames_CanBeNull()
    {
        // Arrange & Act
        var options = new ReplayOptions { MaxFrames = null };

        // Assert
        Assert.Null(options.MaxFrames);
    }

    #endregion

    #region MaxDuration Tests

    [Fact]
    public void MaxDuration_CanBeSetToValue()
    {
        // Arrange & Act
        var options = new ReplayOptions { MaxDuration = TimeSpan.FromMinutes(5) };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), options.MaxDuration);
    }

    [Fact]
    public void MaxDuration_CanBeNull()
    {
        // Arrange & Act
        var options = new ReplayOptions { MaxDuration = null };

        // Assert
        Assert.Null(options.MaxDuration);
    }

    #endregion

    #region UseRingBuffer Tests

    [Fact]
    public void UseRingBuffer_CanBeEnabled()
    {
        // Arrange & Act
        var options = new ReplayOptions { UseRingBuffer = true };

        // Assert
        Assert.True(options.UseRingBuffer);
    }

    [Fact]
    public void UseRingBuffer_WithMaxFrames_CreatesRollingWindow()
    {
        // Arrange & Act
        var options = new ReplayOptions
        {
            UseRingBuffer = true,
            MaxFrames = 100
        };

        // Assert
        Assert.True(options.UseRingBuffer);
        Assert.Equal(100, options.MaxFrames);
    }

    #endregion

    #region DefaultRecordingName Tests

    [Fact]
    public void DefaultRecordingName_CanBeSet()
    {
        // Arrange & Act
        var options = new ReplayOptions { DefaultRecordingName = "Debug Session" };

        // Assert
        Assert.Equal("Debug Session", options.DefaultRecordingName);
    }

    [Fact]
    public void DefaultRecordingName_CanBeEmpty()
    {
        // Arrange & Act
        var options = new ReplayOptions { DefaultRecordingName = string.Empty };

        // Assert
        Assert.Equal(string.Empty, options.DefaultRecordingName);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Options_WithSameValues_AreEqual()
    {
        // Arrange
        var options1 = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(2),
            MaxFrames = 500,
            RecordSystemEvents = false
        };
        var options2 = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(2),
            MaxFrames = 500,
            RecordSystemEvents = false
        };

        // Assert
        Assert.Equal(options1, options2);
    }

    [Fact]
    public void Options_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var options1 = new ReplayOptions { MaxFrames = 500 };
        var options2 = new ReplayOptions { MaxFrames = 1000 };

        // Assert
        Assert.NotEqual(options1, options2);
    }

    [Fact]
    public void Options_CanBeClonedWithWith()
    {
        // Arrange
        var original = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(2),
            MaxFrames = 500
        };

        // Act
        var modified = original with { MaxFrames = 1000 };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(2), modified.SnapshotInterval);
        Assert.Equal(1000, modified.MaxFrames);
        Assert.Equal(500, original.MaxFrames); // Original unchanged
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public void Options_FullConfiguration_SetsAllProperties()
    {
        // Arrange & Act
        var options = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(5),
            RecordSystemEvents = true,
            RecordEntityEvents = true,
            RecordComponentEvents = false,
            SystemEventPhase = SystemPhase.Update,
            MaxFrames = 3600,
            MaxDuration = TimeSpan.FromMinutes(1),
            UseRingBuffer = true,
            DefaultRecordingName = "Crash Replay Buffer"
        };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), options.SnapshotInterval);
        Assert.True(options.RecordSystemEvents);
        Assert.True(options.RecordEntityEvents);
        Assert.False(options.RecordComponentEvents);
        Assert.Equal(SystemPhase.Update, options.SystemEventPhase);
        Assert.Equal(3600, options.MaxFrames);
        Assert.Equal(TimeSpan.FromMinutes(1), options.MaxDuration);
        Assert.True(options.UseRingBuffer);
        Assert.Equal("Crash Replay Buffer", options.DefaultRecordingName);
    }

    #endregion
}
