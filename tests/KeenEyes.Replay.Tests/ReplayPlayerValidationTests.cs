using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="ReplayPlayer"/> validation functionality.
/// </summary>
public class ReplayPlayerValidationTests
{
    #region SetValidationContext Tests

    [Fact]
    public void SetValidationContext_WithValidParameters_SetsContext()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();

        // Act
        player.SetValidationContext(world, serializer);

        // Assert - No exception thrown indicates success
    }

    [Fact]
    public void SetValidationContext_WithNullWorld_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var serializer = new TestComponentSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => player.SetValidationContext(null!, serializer));
    }

    [Fact]
    public void SetValidationContext_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => player.SetValidationContext(world, null!));
    }

    [Fact]
    public void SetValidationContext_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.Dispose();
        using var world = new World();
        var serializer = new TestComponentSerializer();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.SetValidationContext(world, serializer));
    }

    #endregion

    #region ClearValidationContext Tests

    [Fact]
    public void ClearValidationContext_AfterSettingContext_ClearsContext()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        // Load a simple replay
        var replayData = CreateReplayDataWithChecksums();
        player.LoadReplay(replayData);

        // Act
        player.ClearValidationContext();

        // Assert - ValidateCurrentFrame should throw since context is cleared
        Assert.Throws<InvalidOperationException>(() => player.ValidateCurrentFrame());
    }

    [Fact]
    public void ClearValidationContext_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.ClearValidationContext());
    }

    #endregion

    #region AutoValidate Tests

    [Fact]
    public void AutoValidate_Default_IsFalse()
    {
        // Arrange & Act
        using var player = new ReplayPlayer();

        // Assert
        Assert.False(player.AutoValidate);
    }

    [Fact]
    public void AutoValidate_SetToTrue_ReturnsTrue()
    {
        // Arrange
        using var player = new ReplayPlayer();

        // Act
        player.AutoValidate = true;

        // Assert
        Assert.True(player.AutoValidate);
    }

    [Fact]
    public void AutoValidate_SetToFalse_ReturnsFalse()
    {
        // Arrange
        using var player = new ReplayPlayer();
        player.AutoValidate = true;

        // Act
        player.AutoValidate = false;

        // Assert
        Assert.False(player.AutoValidate);
    }

    #endregion

    #region ValidateCurrentFrame Tests

    [Fact]
    public void ValidateCurrentFrame_WithNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.ValidateCurrentFrame());
    }

    [Fact]
    public void ValidateCurrentFrame_WithNoValidationContext_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replayData = CreateReplayDataWithChecksums();
        player.LoadReplay(replayData);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => player.ValidateCurrentFrame());
        Assert.Contains("validation context", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateCurrentFrame_WithNoChecksum_ReturnsTrue()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithoutChecksums();
        player.LoadReplay(replayData);

        // Act
        var result = player.ValidateCurrentFrame();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateCurrentFrame_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.ValidateCurrentFrame());
    }

    #endregion

    #region ValidateDeterminism Tests

    [Fact]
    public void ValidateDeterminism_WithNoReplayLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => player.ValidateDeterminism());
    }

    [Fact]
    public void ValidateDeterminism_WithNoValidationContext_ThrowsInvalidOperationException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        var replayData = CreateReplayDataWithChecksums();
        player.LoadReplay(replayData);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => player.ValidateDeterminism());
        Assert.Contains("validation context", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDeterminism_WithIterationsLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithChecksums();
        player.LoadReplay(replayData);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => player.ValidateDeterminism(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => player.ValidateDeterminism(-1));
    }

    [Fact]
    public void ValidateDeterminism_WithConsistentChecksums_ReturnsTrue()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithConsistentChecksums();
        player.LoadReplay(replayData);

        // Act
        var result = player.ValidateDeterminism(3);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateDeterminism_WithNoChecksums_ReturnsTrue()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithoutChecksums();
        player.LoadReplay(replayData);

        // Act
        var result = player.ValidateDeterminism();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateDeterminism_DefaultIterations_UsesThree()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithConsistentChecksums();
        player.LoadReplay(replayData);

        // Act - Should not throw with default parameter
        var result = player.ValidateDeterminism();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateDeterminism_WhenDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var player = new ReplayPlayer();
        player.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => player.ValidateDeterminism());
    }

    #endregion

    #region DesyncDetected Event Tests

    [Fact]
    public void DesyncDetected_WhenValidationFails_FiresEvent()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        var replayData = CreateReplayDataWithMismatchedChecksum();
        player.LoadReplay(replayData);

        ReplayDesyncException? receivedEvent = null;
        player.DesyncDetected += ex => receivedEvent = ex;

        // Act
        player.ValidateCurrentFrame();

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(0, receivedEvent!.Frame);
    }

    [Fact]
    public void DesyncDetected_WhenValidationPasses_DoesNotFireEvent()
    {
        // Arrange
        using var player = new ReplayPlayer();
        using var world = new World();
        var serializer = new TestComponentSerializer();
        player.SetValidationContext(world, serializer);

        // Create replay with checksum that matches empty world
        var emptyWorldChecksum = WorldChecksum.Calculate(world, serializer);
        var replayData = CreateReplayDataWithChecksum(emptyWorldChecksum);
        player.LoadReplay(replayData);

        bool eventFired = false;
        player.DesyncDetected += _ => eventFired = true;

        // Act
        player.ValidateCurrentFrame();

        // Assert
        Assert.False(eventFired);
    }

    #endregion

    #region Helper Methods

    private static ReplayData CreateReplayDataWithChecksums()
    {
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FrameCount = 3,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events = [],
                    Checksum = 0x12345678u
                },
                new ReplayFrame
                {
                    FrameNumber = 1,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(16),
                    Events = [],
                    Checksum = 0x23456789u
                },
                new ReplayFrame
                {
                    FrameNumber = 2,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(32),
                    Events = [],
                    Checksum = 0x3456789Au
                }
            ],
            Snapshots = []
        };
    }

    private static ReplayData CreateReplayDataWithoutChecksums()
    {
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FrameCount = 3,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events = [],
                    Checksum = null
                },
                new ReplayFrame
                {
                    FrameNumber = 1,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(16),
                    Events = [],
                    Checksum = null
                },
                new ReplayFrame
                {
                    FrameNumber = 2,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(32),
                    Events = [],
                    Checksum = null
                }
            ],
            Snapshots = []
        };
    }

    private static ReplayData CreateReplayDataWithConsistentChecksums()
    {
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(1),
            FrameCount = 5,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events = [],
                    Checksum = 0xAAAAAAAAu
                },
                new ReplayFrame
                {
                    FrameNumber = 1,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(16),
                    Events = [],
                    Checksum = 0xBBBBBBBBu
                },
                new ReplayFrame
                {
                    FrameNumber = 2,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(32),
                    Events = [],
                    Checksum = 0xCCCCCCCCu
                },
                new ReplayFrame
                {
                    FrameNumber = 3,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(48),
                    Events = [],
                    Checksum = 0xDDDDDDDDu
                },
                new ReplayFrame
                {
                    FrameNumber = 4,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(64),
                    Events = [],
                    Checksum = 0xEEEEEEEEu
                }
            ],
            Snapshots = []
        };
    }

    private static ReplayData CreateReplayDataWithMismatchedChecksum()
    {
        // Create replay data where the checksum won't match an empty world
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(16),
            FrameCount = 1,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events = [],
                    Checksum = 0xDEADBEEFu // This won't match any world state
                }
            ],
            Snapshots = []
        };
    }

    private static ReplayData CreateReplayDataWithChecksum(uint checksum)
    {
        return new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(16),
            FrameCount = 1,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events = [],
                    Checksum = checksum
                }
            ],
            Snapshots = []
        };
    }

    #endregion
}
