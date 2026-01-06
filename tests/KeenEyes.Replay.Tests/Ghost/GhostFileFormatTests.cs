using System.Numerics;
using KeenEyes.Replay.Ghost;

namespace KeenEyes.Replay.Tests.Ghost;

/// <summary>
/// Unit tests for the <see cref="GhostFileFormat"/> class.
/// </summary>
public class GhostFileFormatTests
{
    private static GhostData CreateTestGhostData()
    {
        return new GhostData
        {
            Name = "Test Ghost",
            EntityName = "Player",
            RecordingStarted = DateTimeOffset.UtcNow.AddMinutes(-2),
            Duration = TimeSpan.FromMinutes(2),
            FrameCount = 100,
            Frames =
            [
                new GhostFrame(Vector3.Zero, Quaternion.Identity, TimeSpan.Zero)
                {
                    Scale = Vector3.One,
                    Distance = 0f
                },
                new GhostFrame(new Vector3(1, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(1))
                {
                    Scale = Vector3.One,
                    Distance = 1f
                },
                new GhostFrame(new Vector3(2, 0, 0), Quaternion.Identity, TimeSpan.FromSeconds(2))
                {
                    Scale = Vector3.One,
                    Distance = 2f
                }
            ]
        };
    }

    #region Write Tests

    [Fact]
    public void Write_ToByteArray_ProducesValidFile()
    {
        // Arrange
        var ghostData = CreateTestGhostData();

        // Act
        var bytes = GhostFileFormat.Write(ghostData);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.True(GhostFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_ToStream_ProducesValidFile()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        using var stream = new MemoryStream();

        // Act
        GhostFileFormat.Write(stream, ghostData);

        // Assert
        Assert.True(stream.Length > 0);
        stream.Position = 0;
        Assert.True(GhostFileFormat.IsValidFormat(stream));
    }

    [Fact]
    public void Write_WithNullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var ghostData = CreateTestGhostData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GhostFileFormat.Write(null!, ghostData));
    }

    [Fact]
    public void Write_WithNullGhostData_ThrowsArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GhostFileFormat.Write(stream, null!));
    }

    [Fact]
    public void Write_StartsWithCorrectMagicBytes()
    {
        // Arrange
        var ghostData = CreateTestGhostData();

        // Act
        var bytes = GhostFileFormat.Write(ghostData);

        // Assert - KGHO magic
        Assert.Equal((byte)'K', bytes[0]);
        Assert.Equal((byte)'G', bytes[1]);
        Assert.Equal((byte)'H', bytes[2]);
        Assert.Equal((byte)'O', bytes[3]);
    }

    [Fact]
    public void Write_WithNoCompression_DoesNotCompress()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { Compression = CompressionMode.None };

        // Act
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(GhostFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithGZipCompression_CompressesData()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var gzipOptions = new GhostFileOptions { Compression = CompressionMode.GZip };

        // Act
        var compressedBytes = GhostFileFormat.Write(ghostData, gzipOptions);

        // Assert
        Assert.True(GhostFileFormat.IsValidFormat(compressedBytes));
    }

    [Fact]
    public void Write_WithBrotliCompression_CompressesData()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { Compression = CompressionMode.Brotli };

        // Act
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Assert
        Assert.NotNull(bytes);
        Assert.True(GhostFileFormat.IsValidFormat(bytes));
    }

    [Fact]
    public void Write_WithChecksum_IncludesChecksum()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { IncludeChecksum = true };

        // Act
        var bytes = GhostFileFormat.Write(ghostData, options);
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.NotNull(fileInfo.Checksum);
    }

    [Fact]
    public void Write_WithoutChecksum_NoChecksum()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { IncludeChecksum = false };

        // Act
        var bytes = GhostFileFormat.Write(ghostData, options);
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.Null(fileInfo.Checksum);
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_ValidFile_ReturnsGhostData()
    {
        // Arrange
        var originalData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(originalData);

        // Act
        var (fileInfo, ghostData) = GhostFileFormat.Read(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.NotNull(ghostData);
        Assert.Equal(originalData.Name, ghostData.Name);
        Assert.Equal(originalData.EntityName, ghostData.EntityName);
        Assert.Equal(originalData.FrameCount, ghostData.FrameCount);
    }

    [Fact]
    public void Read_PreservesFrameData()
    {
        // Arrange
        var originalData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(originalData);

        // Act
        var (_, ghostData) = GhostFileFormat.Read(bytes);

        // Assert
        Assert.Equal(originalData.Frames.Count, ghostData.Frames.Count);
        Assert.Equal(originalData.Frames[0].Position, ghostData.Frames[0].Position);
        Assert.Equal(originalData.Frames[0].Rotation, ghostData.Frames[0].Rotation);
    }

    [Fact]
    public void Read_PreservesPosition()
    {
        // Arrange
        var originalData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(originalData);

        // Act
        var (_, ghostData) = GhostFileFormat.Read(bytes);

        // Assert
        Assert.Equal(originalData.Frames[1].Position, ghostData.Frames[1].Position);
        Assert.Equal(originalData.Frames[1].Distance, ghostData.Frames[1].Distance);
    }

    [Fact]
    public void Read_FromStream_ReturnsGhostData()
    {
        // Arrange
        var originalData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(originalData);
        using var stream = new MemoryStream(bytes);

        // Act
        var (fileInfo, ghostData) = GhostFileFormat.Read(stream);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.NotNull(ghostData);
        Assert.Equal(originalData.Name, ghostData.Name);
    }

    [Fact]
    public void Read_WithValidChecksum_Succeeds()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { IncludeChecksum = true };
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Act & Assert - should not throw
        var (_, data) = GhostFileFormat.Read(bytes, validateChecksum: true);
        Assert.NotNull(data);
    }

    [Fact]
    public void Read_WithCorruptedData_ThrowsException()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { IncludeChecksum = true };
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Corrupt some data (after header, in the middle of compressed data)
        if (bytes.Length > 50)
        {
            bytes[50] ^= 0xFF;
        }

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => GhostFileFormat.Read(bytes, validateChecksum: true));
    }

    [Fact]
    public void Read_WithInvalidMagic_ThrowsInvalidDataException()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Corrupt magic bytes
        bytes[0] = (byte)'X';

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => GhostFileFormat.Read(bytes));
    }

    [Fact]
    public void Read_AllCompressionModes_RoundTrips()
    {
        // Arrange
        var originalData = CreateTestGhostData();

        foreach (var compression in Enum.GetValues<CompressionMode>())
        {
            var options = new GhostFileOptions { Compression = compression };

            // Act
            var bytes = GhostFileFormat.Write(originalData, options);
            var (_, ghostData) = GhostFileFormat.Read(bytes);

            // Assert
            Assert.Equal(originalData.Name, ghostData.Name);
            Assert.Equal(originalData.FrameCount, ghostData.FrameCount);
        }
    }

    #endregion

    #region ReadMetadata Tests

    [Fact]
    public void ReadMetadata_ReturnsMetadataWithoutFullLoad()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Act
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(ghostData.Name, fileInfo.Name);
        Assert.Equal(ghostData.EntityName, fileInfo.EntityName);
        Assert.Equal(ghostData.FrameCount, fileInfo.FrameCount);
    }

    [Fact]
    public void ReadMetadata_FromStream_ReturnsMetadata()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);
        using var stream = new MemoryStream(bytes);

        // Act
        var fileInfo = GhostFileFormat.ReadMetadata(stream);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.Equal(ghostData.Name, fileInfo.Name);
    }

    [Fact]
    public void ReadMetadata_IncludesCompressionInfo()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { Compression = CompressionMode.GZip };
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Act
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.Equal(CompressionMode.GZip, fileInfo.Compression);
    }

    [Fact]
    public void ReadMetadata_IncludesSizeInfo()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Act
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.True(fileInfo.UncompressedSize > 0);
        Assert.True(fileInfo.CompressedSize > 0);
    }

    [Fact]
    public void ReadMetadata_IncludesTotalDistance()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Act
        var fileInfo = GhostFileFormat.ReadMetadata(bytes);

        // Assert
        Assert.Equal(ghostData.TotalDistance, fileInfo.TotalDistance);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_ValidFile_ReturnsFileInfoWithNoError()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Act
        var fileInfo = GhostFileFormat.Validate(bytes);

        // Assert
        Assert.NotNull(fileInfo);
        Assert.True(fileInfo.IsValid);
        Assert.Null(fileInfo.ValidationError);
    }

    [Fact]
    public void Validate_CorruptedChecksum_ReturnsValidationError()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var options = new GhostFileOptions { IncludeChecksum = true };
        var bytes = GhostFileFormat.Write(ghostData, options);

        // Corrupt the checksum (last 4 bytes)
        if (bytes.Length >= 4)
        {
            bytes[^1] ^= 0xFF;
        }

        // Act
        var fileInfo = GhostFileFormat.Validate(bytes);

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
        var fileInfo = GhostFileFormat.Validate(invalidData);

        // Assert
        Assert.Null(fileInfo);
    }

    #endregion

    #region IsValidFormat Tests

    [Fact]
    public void IsValidFormat_ValidFile_ReturnsTrue()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);

        // Act
        var isValid = GhostFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidFormat_InvalidMagic_ReturnsFalse()
    {
        // Arrange
        var bytes = new byte[] { (byte)'X', (byte)'Y', (byte)'Z', (byte)'W', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        // Act
        var isValid = GhostFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_TooShort_ReturnsFalse()
    {
        // Arrange
        var bytes = new byte[] { (byte)'K', (byte)'G', (byte)'H', (byte)'O' }; // Only 4 bytes, header is 16

        // Act
        var isValid = GhostFileFormat.IsValidFormat(bytes);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_NullData_ReturnsFalse()
    {
        // Act
        var isValid = GhostFileFormat.IsValidFormat((byte[])null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidFormat_Stream_ReturnsTrue()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);
        using var stream = new MemoryStream(bytes);

        // Act
        var isValid = GhostFileFormat.IsValidFormat(stream);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidFormat_Stream_RestoresPosition()
    {
        // Arrange
        var ghostData = CreateTestGhostData();
        var bytes = GhostFileFormat.Write(ghostData);
        using var stream = new MemoryStream(bytes);
        var originalPosition = stream.Position;

        // Act
        _ = GhostFileFormat.IsValidFormat(stream);

        // Assert
        Assert.Equal(originalPosition, stream.Position);
    }

    #endregion

    #region GhostFileOptions Tests

    [Fact]
    public void GhostFileOptions_Default_HasExpectedValues()
    {
        // Act
        var options = GhostFileOptions.Default;

        // Assert
        Assert.Equal(CompressionMode.GZip, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.Optimal, options.CompressionLevel);
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void GhostFileOptions_Smallest_HasExpectedValues()
    {
        // Act
        var options = GhostFileOptions.Smallest;

        // Assert
        Assert.Equal(CompressionMode.Brotli, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.SmallestSize, options.CompressionLevel);
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void GhostFileOptions_Fastest_HasExpectedValues()
    {
        // Act
        var options = GhostFileOptions.Fastest;

        // Assert
        Assert.Equal(CompressionMode.None, options.Compression);
        Assert.False(options.IncludeChecksum);
    }

    #endregion

    #region GhostFileInfo Tests

    [Fact]
    public void GhostFileInfo_CompressionRatio_CalculatesCorrectly()
    {
        // Arrange
        var fileInfo = new GhostFileInfo
        {
            UncompressedSize = 1000,
            CompressedSize = 500
        };

        // Act & Assert
        Assert.Equal(0.5, fileInfo.CompressionRatio);
    }

    [Fact]
    public void GhostFileInfo_CompressionRatio_ZeroUncompressed_ReturnsOne()
    {
        // Arrange
        var fileInfo = new GhostFileInfo
        {
            UncompressedSize = 0,
            CompressedSize = 0
        };

        // Act & Assert
        Assert.Equal(1.0, fileInfo.CompressionRatio);
    }

    [Fact]
    public void GhostFileInfo_IsValid_TrueWhenNoError()
    {
        // Arrange
        var fileInfo = new GhostFileInfo { ValidationError = null };

        // Assert
        Assert.True(fileInfo.IsValid);
    }

    [Fact]
    public void GhostFileInfo_IsValid_FalseWhenError()
    {
        // Arrange
        var fileInfo = new GhostFileInfo { ValidationError = "Some error" };

        // Assert
        Assert.False(fileInfo.IsValid);
    }

    #endregion
}
