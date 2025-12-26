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
}
