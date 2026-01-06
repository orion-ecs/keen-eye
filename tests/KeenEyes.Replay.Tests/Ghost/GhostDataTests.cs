using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for the <see cref="GhostData"/> class and related types.
/// </summary>
public class GhostDataTests
{
    #region GhostData Tests

    [Fact]
    public void GhostData_RequiredProperties_AreSet()
    {
        // Act
        var data = new GhostData
        {
            Name = "Test Ghost",
            EntityName = "Player",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMinutes(5),
            FrameCount = 100,
            Frames = []
        };

        // Assert
        Assert.Equal("Test Ghost", data.Name);
        Assert.Equal("Player", data.EntityName);
        Assert.Equal(100, data.FrameCount);
    }

    [Fact]
    public void GhostData_Version_DefaultIsCurrentVersion()
    {
        // Act
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = []
        };

        // Assert
        Assert.Equal(GhostData.CurrentVersion, data.Version);
    }

    [Fact]
    public void GhostData_TotalDistance_ReturnsLastFrameDistance()
    {
        // Arrange
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2),
            FrameCount = 3,
            Frames =
            [
                new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero) { Distance = 0f },
                new GhostFrame(Vector3.UnitX, Quaternion.Identity, TimeSpan.FromSeconds(1)) { Distance = 1f },
                new GhostFrame(new Vector3(2, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(2)) { Distance = 2f }
            ]
        };

        // Assert
        Assert.Equal(2f, data.TotalDistance);
    }

    [Fact]
    public void GhostData_TotalDistance_EmptyFrames_ReturnsZero()
    {
        // Arrange
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = []
        };

        // Assert
        Assert.Equal(0f, data.TotalDistance);
    }

    [Fact]
    public void GhostData_AverageFrameRate_CalculatesCorrectly()
    {
        // Arrange
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(2),
            FrameCount = 120,
            Frames = []
        };

        // Assert
        Assert.Equal(60, data.AverageFrameRate);
    }

    [Fact]
    public void GhostData_AverageFrameRate_ZeroDuration_ReturnsZero()
    {
        // Arrange
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 100,
            Frames = []
        };

        // Assert
        Assert.Equal(0, data.AverageFrameRate);
    }

    [Fact]
    public void GhostData_Metadata_CanBeSet()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["TrackName"] = "Rainbow Road",
            ["BestTime"] = 123.45
        };

        // Act
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Metadata = metadata
        };

        // Assert
        Assert.NotNull(data.Metadata);
        Assert.Equal("Rainbow Road", data.Metadata["TrackName"]);
    }

    [Fact]
    public void GhostData_Metadata_CanBeNull()
    {
        // Act
        var data = new GhostData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Metadata = null
        };

        // Assert
        Assert.Null(data.Metadata);
    }

    #endregion

    #region GhostVisualConfig Tests

    [Fact]
    public void GhostVisualConfig_Default_HasExpectedValues()
    {
        // Act
        var config = GhostVisualConfig.Default;

        // Assert
        Assert.Equal(Vector4.One, config.TintColor);
        Assert.Equal(0.5f, config.Opacity);
        Assert.Null(config.Label);
        Assert.False(config.CastsShadows);
        Assert.False(config.ReceivesShadows);
        Assert.False(config.ShowOutline);
    }

    [Fact]
    public void GhostVisualConfig_WithOpacity_ReturnsNewConfig()
    {
        // Arrange
        var config = new GhostVisualConfig();

        // Act
        var newConfig = config.WithOpacity(0.8f);

        // Assert
        Assert.Equal(0.5f, config.Opacity); // Original unchanged
        Assert.Equal(0.8f, newConfig.Opacity);
    }

    [Fact]
    public void GhostVisualConfig_WithOpacity_ClampsValue()
    {
        // Arrange
        var config = new GhostVisualConfig();

        // Act
        var lowConfig = config.WithOpacity(-0.5f);
        var highConfig = config.WithOpacity(1.5f);

        // Assert
        Assert.Equal(0f, lowConfig.Opacity);
        Assert.Equal(1f, highConfig.Opacity);
    }

    [Fact]
    public void GhostVisualConfig_WithTint_ReturnsNewConfig()
    {
        // Arrange
        var config = new GhostVisualConfig();

        // Act
        var newConfig = config.WithTint(1f, 0f, 0f);

        // Assert
        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), newConfig.TintColor);
    }

    [Fact]
    public void GhostVisualConfig_WithLabel_ReturnsNewConfig()
    {
        // Arrange
        var config = new GhostVisualConfig();

        // Act
        var newConfig = config.WithLabel("Personal Best");

        // Assert
        Assert.Null(config.Label); // Original unchanged
        Assert.Equal("Personal Best", newConfig.Label);
    }

    [Fact]
    public void GhostVisualConfig_AllProperties_CanBeSet()
    {
        // Act
        var config = new GhostVisualConfig
        {
            TintColor = new Vector4(0f, 1f, 0f, 1f),
            Opacity = 0.75f,
            Label = "Ghost",
            CastsShadows = true,
            ReceivesShadows = true,
            ShowOutline = true,
            OutlineColor = new Vector4(1f, 1f, 0f, 1f)
        };

        // Assert
        Assert.Equal(new Vector4(0f, 1f, 0f, 1f), config.TintColor);
        Assert.Equal(0.75f, config.Opacity);
        Assert.Equal("Ghost", config.Label);
        Assert.True(config.CastsShadows);
        Assert.True(config.ReceivesShadows);
        Assert.True(config.ShowOutline);
        Assert.Equal(new Vector4(1f, 1f, 0f, 1f), config.OutlineColor);
    }

    #endregion

    #region GhostSyncMode Tests

    [Fact]
    public void GhostSyncMode_AllValuesAreDefined()
    {
        // Assert
        Assert.Equal(0, (int)GhostSyncMode.TimeSynced);
        Assert.Equal(1, (int)GhostSyncMode.FrameSynced);
        Assert.Equal(2, (int)GhostSyncMode.DistanceSynced);
        Assert.Equal(3, (int)GhostSyncMode.Independent);
    }

    #endregion

    #region GhostPlaybackState Tests

    [Fact]
    public void GhostPlaybackState_AllValuesAreDefined()
    {
        // Assert
        Assert.Equal(0, (int)GhostPlaybackState.Stopped);
        Assert.Equal(1, (int)GhostPlaybackState.Playing);
        Assert.Equal(2, (int)GhostPlaybackState.Paused);
    }

    #endregion

    #region GhostFormatException Tests

    [Fact]
    public void GhostFormatException_InvalidMagicBytes_CreatesCorrectMessage()
    {
        // Act
        var exception = GhostFormatException.InvalidMagicBytes("/path/to/file.keghost");

        // Assert
        Assert.Contains("magic bytes", exception.Message);
        Assert.Equal("/path/to/file.keghost", exception.FilePath);
        Assert.Equal("InvalidMagicBytes", exception.FormatIssue);
    }

    [Fact]
    public void GhostFormatException_Corrupted_CreatesCorrectMessage()
    {
        // Act
        var exception = GhostFormatException.Corrupted("/path/to/file.keghost", "Additional info");

        // Assert
        Assert.Contains("corrupted", exception.Message);
        Assert.Contains("Additional info", exception.Message);
        Assert.Equal("Corrupted", exception.FormatIssue);
    }

    [Fact]
    public void GhostFormatException_ChecksumMismatch_CreatesCorrectMessage()
    {
        // Act
        var exception = GhostFormatException.ChecksumMismatch("ABC123", "DEF456");

        // Assert
        Assert.Contains("ABC123", exception.Message);
        Assert.Contains("DEF456", exception.Message);
        Assert.Contains("checksum", exception.Message.ToLower());
    }

    #endregion

    #region GhostVersionException Tests

    [Fact]
    public void GhostVersionException_Constructor_SetsProperties()
    {
        // Act
        var exception = new GhostVersionException(2, 1, "/path/to/file.keghost");

        // Assert
        Assert.Equal(2, exception.FileVersion);
        Assert.Equal(1, exception.SupportedVersion);
        Assert.Equal("/path/to/file.keghost", exception.FilePath);
        Assert.Contains("version 2", exception.Message);
    }

    [Fact]
    public void GhostVersionException_UnknownVersion_CreatesCorrectMessage()
    {
        // Act
        var exception = GhostVersionException.UnknownVersion("Details here", "/path/file.keghost");

        // Assert
        Assert.Contains("unknown version", exception.Message.ToLower());
        Assert.Contains("Details here", exception.Message);
    }

    #endregion
}
