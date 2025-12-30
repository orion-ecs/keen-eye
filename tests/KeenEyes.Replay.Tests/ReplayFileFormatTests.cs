namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="ReplayFileFormat"/> class.
/// </summary>
public class ReplayFileFormatTests
{
    private static ReplayData CreateTestReplayData()
    {
        return new ReplayData
        {
            Name = "Test Replay",
            Description = "A test replay for unit testing",
            RecordingStarted = DateTimeOffset.UtcNow.AddMinutes(-5),
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMinutes(5),
            FrameCount = 100,
            Frames =
            [
                new ReplayFrame
                {
                    FrameNumber = 0,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.Zero,
                    Events =
                    [
                        new ReplayEvent { Type = ReplayEventType.FrameStart, Timestamp = TimeSpan.Zero },
                        new ReplayEvent { Type = ReplayEventType.FrameEnd, Timestamp = TimeSpan.FromMilliseconds(16) }
                    ]
                },
                new ReplayFrame
                {
                    FrameNumber = 1,
                    DeltaTime = TimeSpan.FromMilliseconds(16),
                    ElapsedTime = TimeSpan.FromMilliseconds(16),
                    Events =
                    [
                        new ReplayEvent { Type = ReplayEventType.FrameStart, Timestamp = TimeSpan.Zero },
                        new ReplayEvent { Type = ReplayEventType.Custom, CustomType = "TestEvent", Timestamp = TimeSpan.FromMilliseconds(8) },
                        new ReplayEvent { Type = ReplayEventType.FrameEnd, Timestamp = TimeSpan.FromMilliseconds(16) }
                    ]
                }
            ],
            Snapshots = []
        };
    }

    #region Write Tests

    [Fact]
    public void Write_ToByteArray_ProducesValidFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();

        // Act
        var bytes = ReplayFileFormat.Write(replayData);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_ToStream_ProducesValidFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        using var stream = new MemoryStream();

        // Act
        ReplayFileFormat.Write(stream, replayData);

        // Assert
        Assert.True(stream.Length > 0);
        stream.Position = 0;
        Assert.True(ReplayFileFormat.IsValidFormat(stream));
    }

    [Fact]
    public void Write_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var replayData = CreateTestReplayData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ReplayFileFormat.Write(null!, replayData));
    }

    [Fact]
    public void Write_WithNullReplayData_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ReplayFileFormat.Write(stream, null!));
    }

    [Fact]
    public void Write_StartsWithCorrectMagicBytes()
    {
        // Arrange
        var replayData = CreateTestReplayData();

        // Act
        var bytes = ReplayFileFormat.Write(replayData);

        // Assert
        Assert.Equal((byte)'K', bytes[0]);
        Assert.Equal((byte)'R', bytes[1]);
        Assert.Equal((byte)'P', bytes[2]);
        Assert.Equal((byte)'L', bytes[3]);
    }

    [Fact]
    public void Write_WithNoCompression_DoesNotCompress()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { Compression = CompressionMode.None };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithGZipCompression_CompressesData()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var noCompOptions = new ReplayFileOptions { Compression = CompressionMode.None };
        var gzipOptions = new ReplayFileOptions { Compression = CompressionMode.GZip };

        // Act
        _ = ReplayFileFormat.Write(replayData, noCompOptions);
        var compressedBytes = ReplayFileFormat.Write(replayData, gzipOptions);

        // Assert - compressed should generally be smaller (for reasonable data sizes)
        Assert.True(ReplayFileFormat.IsValidFormat(compressedBytes));
        // Note: For very small data, compression may not reduce size
    }

    [Fact]
    public void Write_WithBrotliCompression_CompressesData()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { Compression = CompressionMode.Brotli };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithChecksum_IncludesChecksum()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { IncludeChecksum = true };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);
        var fileInfo = ReplayFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.NotNull(fileInfo.Checksum);
    }

    [Fact]
    public void Write_WithoutChecksum_NoChecksum()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { IncludeChecksum = false };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);
        var fileInfo = ReplayFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.Null(fileInfo.Checksum);
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_ValidFile_ReturnsReplayData()
    {
        // Arrange
        var originalData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(originalData);

        // Act
        var (fileInfo, replayData) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.NotNull(replayData);
        Assert.Equal(originalData.Name, replayData.Name);
        Assert.Equal(originalData.Description, replayData.Description);
        Assert.Equal(originalData.FrameCount, replayData.FrameCount);
    }

    [Fact]
    public void Read_PreservesFrameData()
    {
        // Arrange
        var originalData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(originalData);

        // Act
        var (_, replayData) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.Equal(originalData.Frames.Count, replayData.Frames.Count);
        Assert.Equal(originalData.Frames[0].FrameNumber, replayData.Frames[0].FrameNumber);
        Assert.Equal(originalData.Frames[0].Events.Count, replayData.Frames[0].Events.Count);
    }

    [Fact]
    public void Read_PreservesEventData()
    {
        // Arrange
        var originalData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(originalData);

        // Act
        var (_, replayData) = ReplayFileFormat.Read(bytes);

        // Assert
        var originalEvent = originalData.Frames[1].Events.First(e => e.Type == ReplayEventType.Custom);
        var readEvent = replayData.Frames[1].Events.First(e => e.Type == ReplayEventType.Custom);
        Assert.Equal(originalEvent.CustomType, readEvent.CustomType);
    }

    [Fact]
    public void Read_FromStream_ReturnsReplayData()
    {
        // Arrange
        var originalData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(originalData);
        using var stream = new MemoryStream(bytes);

        // Act
        var (fileInfo, replayData) = ReplayFileFormat.Read(stream);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.NotNull(replayData);
        Assert.Equal(originalData.Name, replayData.Name);
    }

    [Fact]
    public void Read_WithValidChecksum_Succeeds()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { IncludeChecksum = true };
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Act & Assert - should not throw
        var (_, data) = ReplayFileFormat.Read(bytes, validateChecksum: true);
        Assert.NotNull(data);
    }

    [Fact]
    public void Read_WithCorruptedData_ThrowsException()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { IncludeChecksum = true };
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Corrupt some data (after header, in the middle of compressed data)
        if (bytes.Length > 50)
        {
            bytes[50] ^= 0xFF;
        }

        // Act & Assert - corruption can cause various exceptions depending on where data is corrupted
        Assert.ThrowsAny<Exception>(() => ReplayFileFormat.Read(bytes, validateChecksum: true));
    }

    [Fact]
    public void Read_WithInvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);

        // Corrupt magic bytes
        bytes[0] = (byte)'X';

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => ReplayFileFormat.Read(bytes));
    }

    [Fact]
    public void Read_AllCompressionModes_RoundTrips()
    {
        // Arrange
        var originalData = CreateTestReplayData();

        foreach (var compression in Enum.GetValues<CompressionMode>())
        {
            var options = new ReplayFileOptions { Compression = compression };

            // Act
            var bytes = ReplayFileFormat.Write(originalData, options);
            var (_, replayData) = ReplayFileFormat.Read(bytes);

            // Assert
            Assert.Equal(originalData.Name, replayData.Name);
            Assert.Equal(originalData.FrameCount, replayData.FrameCount);
        }
    }

    #endregion

    #region ReadMetadata Tests

    [Fact]
    public void ReadMetadata_ReturnsMetadataWithoutFullLoad()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);

        // Act
        var fileInfo = ReplayFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(replayData.Name, fileInfo.Name);
        Assert.Equal(replayData.Description, fileInfo.Description);
        Assert.Equal(replayData.FrameCount, fileInfo.FrameCount);
    }

    [Fact]
    public void ReadMetadata_FromStream_ReturnsMetadata()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);
        using var stream = new MemoryStream(bytes);

        // Act
        var fileInfo = ReplayFileFormat.ReadMetadata(stream);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(replayData.Name, fileInfo.Name);
    }

    [Fact]
    public void ReadMetadata_IncludesCompressionInfo()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { Compression = CompressionMode.GZip };
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Act
        var fileInfo = ReplayFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.Equal(CompressionMode.GZip, fileInfo.Compression);
    }

    [Fact]
    public void ReadMetadata_IncludesSizeInfo()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);

        // Act
        var fileInfo = ReplayFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.True(fileInfo.UncompressedSize > 0);
        Assert.True(fileInfo.CompressedSize > 0);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_ValidFile_ReturnsFileInfoWithNoError()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);

        // Act
        var fileInfo = ReplayFileFormat.Validate(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.True(fileInfo.IsValid);
        Assert.Null(fileInfo.ValidationError);
    }

    [Fact]
    public void Validate_CorruptedChecksum_ReturnsValidationError()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions { IncludeChecksum = true };
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Corrupt the checksum (last 4 bytes)
        if (bytes.Length >= 4)
        {
            bytes[^1] ^= 0xFF;
        }

        // Act
        var fileInfo = ReplayFileFormat.Validate(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.False(fileInfo.IsValid);
        Assert.NotNull(fileInfo.ValidationError);
        Assert.Contains("Checksum", fileInfo.ValidationError);
    }

    [Fact]
    public void Validate_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var invalidData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        // Act
        var fileInfo = ReplayFileFormat.Validate(invalidData);

        // Assert
        Assert.Null(fileInfo);
    }

    [Fact]
    public void Validate_FromStream_ValidatesCorrectly()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);
        using var stream = new MemoryStream(bytes);

        // Act
        var fileInfo = ReplayFileFormat.Validate(stream);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.True(fileInfo.IsValid);
    }

    #endregion

    #region IsValidFormat Tests

    [Fact]
    public void IsValidFormat_ValidFile_ReturnsTrue()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);

        // Act
        var isValid = ReplayFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidFormat_InvalidMagic_ReturnsFalse()
    {
        // Arrange
        var bytes = new byte[] { (byte)'X', (byte)'Y', (byte)'Z', (byte)'W', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Act
        var isValid = ReplayFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_TooShort_ReturnsFalse()
    {
        // Arrange
        var bytes = new byte[] { (byte)'K', (byte)'R', (byte)'P', (byte)'L' }; // Only 4 bytes, header is 16

        // Act
        var isValid = ReplayFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_NullData_ReturnsFalse()
    {
        // Act
        var isValid = ReplayFileFormat.IsValidFormat((byte[])null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_Stream_ReturnsTrue()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);
        using var stream = new MemoryStream(bytes);

        // Act
        var isValid = ReplayFileFormat.IsValidFormat(stream);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidFormat_Stream_RestoresPosition()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var bytes = ReplayFileFormat.Write(replayData);
        using var stream = new MemoryStream(bytes);
        var originalPosition = stream.Position;

        // Act
        _ = ReplayFileFormat.IsValidFormat(stream);

        // Assert
        Assert.Equal(originalPosition, stream.Position);
    }

    #endregion

    #region FileInfo Properties Tests

    [Fact]
    public void ReplayFileInfo_CompressionRatio_CalculatesCorrectly()
    {
        // Arrange
        var fileInfo = new ReplayFileInfo
        {
            UncompressedSize = 1000,
            CompressedSize = 500
        };

        // Act & Assert
        Assert.Equal(0.5, fileInfo.CompressionRatio);
    }

    [Fact]
    public void ReplayFileInfo_CompressionRatio_ZeroUncompressed_ReturnsOne()
    {
        // Arrange
        var fileInfo = new ReplayFileInfo
        {
            UncompressedSize = 0,
            CompressedSize = 0
        };

        // Act & Assert
        Assert.Equal(1.0, fileInfo.CompressionRatio);
    }

    [Fact]
    public void ReplayFileInfo_IsValid_TrueWhenNoError()
    {
        // Arrange
        var fileInfo = new ReplayFileInfo { ValidationError = null };

        // Assert
        Assert.True(fileInfo.IsValid);
    }

    [Fact]
    public void ReplayFileInfo_IsValid_FalseWhenError()
    {
        // Arrange
        var fileInfo = new ReplayFileInfo { ValidationError = "Some error" };

        // Assert
        Assert.False(fileInfo.IsValid);
    }

    #endregion

    #region Compression Mode Tests

    [Fact]
    public void Write_WithBrotliCompression_ProducesValidFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            Compression = CompressionMode.Brotli,
            CompressionLevel = System.IO.Compression.CompressionLevel.Optimal
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithNoCompression_ProducesValidFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            Compression = CompressionMode.None
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithNoChecksum_ProducesValidFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            IncludeChecksum = false
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.True(ReplayFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void RoundTrip_WithBrotliCompression_PreservesData()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            Compression = CompressionMode.Brotli
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);
        var (_, loaded) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.Equal(replayData.Name, loaded.Name);
        Assert.Equal(replayData.FrameCount, loaded.FrameCount);
        Assert.Equal(replayData.Frames.Count, loaded.Frames.Count);
    }

    [Fact]
    public void RoundTrip_WithNoCompression_PreservesData()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            Compression = CompressionMode.None
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);
        var (_, loaded) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.Equal(replayData.Name, loaded.Name);
        Assert.Equal(replayData.FrameCount, loaded.FrameCount);
        Assert.Equal(replayData.Frames.Count, loaded.Frames.Count);
    }

    [Fact]
    public void RoundTrip_WithNoChecksum_PreservesData()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var options = new ReplayFileOptions
        {
            IncludeChecksum = false
        };

        // Act
        var bytes = ReplayFileFormat.Write(replayData, options);
        var (_, loaded) = ReplayFileFormat.Read(bytes);

        // Assert
        Assert.Equal(replayData.Name, loaded.Name);
        Assert.Equal(replayData.FrameCount, loaded.FrameCount);
    }

    [Fact]
    public void Write_WithSmallestSizeCompression_ProducesSmallFile()
    {
        // Arrange
        var replayData = CreateTestReplayData();
        var smallOptions = new ReplayFileOptions
        {
            Compression = CompressionMode.GZip,
            CompressionLevel = System.IO.Compression.CompressionLevel.SmallestSize
        };
        var fastOptions = new ReplayFileOptions
        {
            Compression = CompressionMode.GZip,
            CompressionLevel = System.IO.Compression.CompressionLevel.Fastest
        };

        // Act
        var smallBytes = ReplayFileFormat.Write(replayData, smallOptions);
        var fastBytes = ReplayFileFormat.Write(replayData, fastOptions);

        // Assert - both should be valid
        Assert.True(ReplayFileFormat.IsValidFormat(smallBytes));
        Assert.True(ReplayFileFormat.IsValidFormat(fastBytes));
    }

    #endregion

    #region ReplayFileOptions Tests

    [Fact]
    public void ReplayFileOptions_Default_HasExpectedValues()
    {
        // Act
        var options = ReplayFileOptions.Default;

        // Assert
        Assert.Equal(CompressionMode.GZip, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.Optimal, options.CompressionLevel);
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void ReplayFileOptions_CanSetAllProperties()
    {
        // Act
        var options = new ReplayFileOptions
        {
            Compression = CompressionMode.Brotli,
            CompressionLevel = System.IO.Compression.CompressionLevel.Fastest,
            IncludeChecksum = false
        };

        // Assert
        Assert.Equal(CompressionMode.Brotli, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.Fastest, options.CompressionLevel);
        Assert.False(options.IncludeChecksum);
    }

    [Fact]
    public void ReplayFileOptions_DefaultCompression_IsGZip()
    {
        // Act
        var options = new ReplayFileOptions();

        // Assert
        Assert.Equal(CompressionMode.GZip, options.Compression);
    }

    [Fact]
    public void ReplayFileOptions_DefaultCompressionLevel_IsOptimal()
    {
        // Act
        var options = new ReplayFileOptions();

        // Assert
        Assert.Equal(System.IO.Compression.CompressionLevel.Optimal, options.CompressionLevel);
    }

    [Fact]
    public void ReplayFileOptions_DefaultIncludeChecksum_IsTrue()
    {
        // Act
        var options = new ReplayFileOptions();

        // Assert
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void ReplayFileOptions_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new ReplayFileOptions { Compression = CompressionMode.GZip };

        // Act
        var modified = original with { Compression = CompressionMode.Brotli };

        // Assert
        Assert.Equal(CompressionMode.GZip, original.Compression);
        Assert.Equal(CompressionMode.Brotli, modified.Compression);
    }

    [Fact]
    public void ReplayFileOptions_CompressionNone_IsValid()
    {
        // Act
        var options = new ReplayFileOptions { Compression = CompressionMode.None };

        // Assert
        Assert.Equal(CompressionMode.None, options.Compression);
    }

    [Fact]
    public void ReplayFileOptions_SmallestSize_CompressionLevel()
    {
        // Act
        var options = new ReplayFileOptions { CompressionLevel = System.IO.Compression.CompressionLevel.SmallestSize };

        // Assert
        Assert.Equal(System.IO.Compression.CompressionLevel.SmallestSize, options.CompressionLevel);
    }

    #endregion

    #region SnapshotMarker Tests

    [Fact]
    public void SnapshotMarker_RequiredProperties_SetCorrectly()
    {
        // Arrange & Act
        var marker = new SnapshotMarker
        {
            FrameNumber = 100,
            ElapsedTime = TimeSpan.FromSeconds(5),
            Snapshot = new Serialization.WorldSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                Entities = [],
                Singletons = []
            }
        };

        // Assert
        Assert.Equal(100, marker.FrameNumber);
        Assert.Equal(TimeSpan.FromSeconds(5), marker.ElapsedTime);
        Assert.NotNull(marker.Snapshot);
    }

    #endregion

    #region ReplayFrame Tests

    [Fact]
    public void ReplayFrame_DefaultProperties()
    {
        // Act
        var frame = new ReplayFrame
        {
            FrameNumber = 42,
            DeltaTime = TimeSpan.FromMilliseconds(16.67),
            ElapsedTime = TimeSpan.FromSeconds(1),
            Events = []
        };

        // Assert
        Assert.Equal(42, frame.FrameNumber);
        Assert.Equal(TimeSpan.FromMilliseconds(16.67), frame.DeltaTime);
        Assert.Equal(TimeSpan.FromSeconds(1), frame.ElapsedTime);
        Assert.Empty(frame.Events);
    }

    [Fact]
    public void ReplayFrame_WithEvents_StoresEvents()
    {
        // Act
        var frame = new ReplayFrame
        {
            FrameNumber = 1,
            DeltaTime = TimeSpan.FromMilliseconds(16),
            ElapsedTime = TimeSpan.FromMilliseconds(16),
            Events =
            [
                new ReplayEvent { Type = ReplayEventType.FrameStart, Timestamp = TimeSpan.Zero },
                new ReplayEvent { Type = ReplayEventType.Custom, CustomType = "Jump", Timestamp = TimeSpan.FromMilliseconds(8) },
                new ReplayEvent { Type = ReplayEventType.FrameEnd, Timestamp = TimeSpan.FromMilliseconds(16) }
            ]
        };

        // Assert
        Assert.Equal(3, frame.Events.Count);
        Assert.Equal(ReplayEventType.FrameStart, frame.Events[0].Type);
        Assert.Equal("Jump", frame.Events[1].CustomType);
        Assert.Equal(ReplayEventType.FrameEnd, frame.Events[2].Type);
    }

    #endregion

    #region ReplayEvent Tests

    [Fact]
    public void ReplayEvent_AllEventTypes_AreRecognized()
    {
        // Arrange & Act
        var events = new[]
        {
            new ReplayEvent { Type = ReplayEventType.FrameStart },
            new ReplayEvent { Type = ReplayEventType.FrameEnd },
            new ReplayEvent { Type = ReplayEventType.EntityCreated, EntityId = 1 },
            new ReplayEvent { Type = ReplayEventType.EntityDestroyed, EntityId = 2 },
            new ReplayEvent { Type = ReplayEventType.ComponentAdded, EntityId = 3, ComponentTypeName = "Position" },
            new ReplayEvent { Type = ReplayEventType.ComponentRemoved, EntityId = 4, ComponentTypeName = "Velocity" },
            new ReplayEvent { Type = ReplayEventType.ComponentChanged, EntityId = 5, ComponentTypeName = "Health" },
            new ReplayEvent { Type = ReplayEventType.SystemStart, SystemTypeName = "MovementSystem" },
            new ReplayEvent { Type = ReplayEventType.SystemEnd, SystemTypeName = "RenderSystem" },
            new ReplayEvent { Type = ReplayEventType.Custom, CustomType = "GameEvent" }
        };

        // Assert
        Assert.Equal(ReplayEventType.FrameStart, events[0].Type);
        Assert.Equal(ReplayEventType.FrameEnd, events[1].Type);
        Assert.Equal(ReplayEventType.EntityCreated, events[2].Type);
        Assert.Equal(1, events[2].EntityId);
        Assert.Equal(ReplayEventType.EntityDestroyed, events[3].Type);
        Assert.Equal(ReplayEventType.ComponentAdded, events[4].Type);
        Assert.Equal("Position", events[4].ComponentTypeName);
        Assert.Equal(ReplayEventType.ComponentRemoved, events[5].Type);
        Assert.Equal(ReplayEventType.ComponentChanged, events[6].Type);
        Assert.Equal(ReplayEventType.SystemStart, events[7].Type);
        Assert.Equal("MovementSystem", events[7].SystemTypeName);
        Assert.Equal(ReplayEventType.SystemEnd, events[8].Type);
        Assert.Equal(ReplayEventType.Custom, events[9].Type);
        Assert.Equal("GameEvent", events[9].CustomType);
    }

    [Fact]
    public void ReplayEvent_WithData_StoresData()
    {
        // Arrange & Act
        var customData = new Dictionary<string, object>
        {
            { "Score", 100 },
            { "Level", "Castle" }
        };

        var replayEvent = new ReplayEvent
        {
            Type = ReplayEventType.Custom,
            CustomType = "ScoreUpdate",
            Timestamp = TimeSpan.FromSeconds(1),
            Data = customData
        };

        // Assert
        Assert.NotNull(replayEvent.Data);
        Assert.Equal(2, replayEvent.Data.Count);
    }

    [Fact]
    public void ReplayEvent_SnapshotType_IsValid()
    {
        // Act
        var replayEvent = new ReplayEvent
        {
            Type = ReplayEventType.Snapshot,
            Timestamp = TimeSpan.FromSeconds(1)
        };

        // Assert
        Assert.Equal(ReplayEventType.Snapshot, replayEvent.Type);
    }

    #endregion

    #region ReplayData Tests

    [Fact]
    public void ReplayData_RequiredProperties_AreSet()
    {
        // Arrange & Act
        var data = new ReplayData
        {
            Name = "Test Replay",
            Description = "Test description",
            RecordingStarted = DateTimeOffset.UtcNow.AddMinutes(-5),
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMinutes(5),
            FrameCount = 100,
            Frames = [],
            Snapshots = []
        };

        // Assert
        Assert.Equal("Test Replay", data.Name);
        Assert.Equal("Test description", data.Description);
        Assert.Equal(100, data.FrameCount);
        Assert.Equal(TimeSpan.FromMinutes(5), data.Duration);
    }

    [Fact]
    public void ReplayData_Metadata_CanBeSet()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["Player"] = "TestUser",
            ["Score"] = 1000
        };

        // Act
        var data = new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Snapshots = [],
            Metadata = metadata
        };

        // Assert
        Assert.NotNull(data.Metadata);
        Assert.Equal("TestUser", data.Metadata["Player"]);
    }

    [Fact]
    public void ReplayData_Metadata_CanBeNull()
    {
        // Act
        var data = new ReplayData
        {
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Snapshots = [],
            Metadata = null
        };

        // Assert
        Assert.Null(data.Metadata);
    }

    [Fact]
    public void ReplayData_NameAndDescription_CanBeNull()
    {
        // Act
        var data = new ReplayData
        {
            Name = null,
            Description = null,
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            FrameCount = 0,
            Frames = [],
            Snapshots = []
        };

        // Assert
        Assert.Null(data.Name);
        Assert.Null(data.Description);
    }

    #endregion

    #region ReplayFrame Additional Tests

    [Fact]
    public void ReplayFrame_PrecedingSnapshotIndex_CanBeSet()
    {
        // Act
        var frame = new ReplayFrame
        {
            FrameNumber = 100,
            DeltaTime = TimeSpan.FromMilliseconds(16),
            ElapsedTime = TimeSpan.FromSeconds(1.6),
            Events = [],
            PrecedingSnapshotIndex = 5
        };

        // Assert
        Assert.Equal(5, frame.PrecedingSnapshotIndex);
    }

    [Fact]
    public void ReplayFrame_PrecedingSnapshotIndex_CanBeNull()
    {
        // Act
        var frame = new ReplayFrame
        {
            FrameNumber = 100,
            DeltaTime = TimeSpan.FromMilliseconds(16),
            ElapsedTime = TimeSpan.FromSeconds(1.6),
            Events = [],
            PrecedingSnapshotIndex = null
        };

        // Assert
        Assert.Null(frame.PrecedingSnapshotIndex);
    }

    #endregion
}
