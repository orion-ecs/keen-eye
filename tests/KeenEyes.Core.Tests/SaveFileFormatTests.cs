using KeenEyes.Serialization;
using CompressionMode = KeenEyes.Serialization.CompressionMode;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the SaveFileFormat class.
/// </summary>
public class SaveFileFormatTests
{
    #region Write and Read Tests

    [Fact]
    public void Write_CreatesValidKsaveFile()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        Assert.NotNull(fileData);
        Assert.True(fileData.Length > 0);
        Assert.True(SaveFileFormat.IsValidFormat(fileData));
    }

    [Fact]
    public void Write_StartsWithMagicBytes()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        Assert.Equal((byte)'K', fileData[0]);
        Assert.Equal((byte)'S', fileData[1]);
        Assert.Equal((byte)'A', fileData[2]);
        Assert.Equal((byte)'V', fileData[3]);
    }

    [Fact]
    public void Read_ReturnsOriginalData()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { Compression = CompressionMode.None, IncludeChecksum = false };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var (readSlotInfo, readSnapshotData) = SaveFileFormat.Read(fileData);

        Assert.Equal(slotInfo.SlotName, readSlotInfo.SlotName);
        Assert.Equal(slotInfo.DisplayName, readSlotInfo.DisplayName);
        Assert.Equal(snapshotData, readSnapshotData);
    }

    [Fact]
    public void Read_WithGZipCompression_DecompressesCorrectly()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();
        var options = new SaveSlotOptions { Compression = CompressionMode.GZip };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var (readSlotInfo, readSnapshotData) = SaveFileFormat.Read(fileData);

        Assert.Equal(snapshotData, readSnapshotData);
        Assert.Equal(CompressionMode.GZip, readSlotInfo.Compression);
    }

    [Fact]
    public void Read_WithBrotliCompression_DecompressesCorrectly()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();
        var options = new SaveSlotOptions { Compression = CompressionMode.Brotli };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var (readSlotInfo, readSnapshotData) = SaveFileFormat.Read(fileData);

        Assert.Equal(snapshotData, readSnapshotData);
        Assert.Equal(CompressionMode.Brotli, readSlotInfo.Compression);
    }

    [Fact]
    public void Read_WithChecksum_ValidatesSuccessfully()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { IncludeChecksum = true };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var (readSlotInfo, readSnapshotData) = SaveFileFormat.Read(fileData, validateChecksum: true);

        Assert.NotNull(readSlotInfo.Checksum);
        Assert.Equal(snapshotData, readSnapshotData);
    }

    [Fact]
    public void Read_WithCorruptedData_ThrowsInvalidDataException()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { IncludeChecksum = true, Compression = CompressionMode.None };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        // Corrupt the data by modifying a byte in the compressed section
        // Find where the data starts (after header + metadata)
        var headerSize = 16;
        var metadataLength = BitConverter.ToInt32(fileData, 8);
        var dataStart = headerSize + metadataLength;
        fileData[dataStart + 5] = (byte)(fileData[dataStart + 5] ^ 0xFF);

        Assert.Throws<InvalidDataException>(() => SaveFileFormat.Read(fileData, validateChecksum: true));
    }

    #endregion

    #region ReadMetadata Tests

    [Fact]
    public void ReadMetadata_ReturnsSlotInfoOnly()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var readSlotInfo = SaveFileFormat.ReadMetadata(fileData);

        Assert.Equal(slotInfo.SlotName, readSlotInfo.SlotName);
        Assert.Equal(slotInfo.DisplayName, readSlotInfo.DisplayName);
    }

    [Fact]
    public void ReadMetadata_DoesNotLoadSnapshotData()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        // ReadMetadata should be fast since it doesn't decompress the data
        using var stream = new MemoryStream(fileData);
        SaveFileFormat.ReadMetadata(stream);

        // Verify stream position is at the end of metadata, not end of file
        Assert.True(stream.Position < stream.Length);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_WithValidFile_ReturnsSlotInfo()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { IncludeChecksum = true };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var validatedInfo = SaveFileFormat.Validate(fileData);

        Assert.NotNull(validatedInfo);
        Assert.True(validatedInfo!.IsValid);
        Assert.Null(validatedInfo.ValidationError);
    }

    [Fact]
    public void Validate_WithCorruptedChecksum_ReturnsErrorInfo()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { IncludeChecksum = true, Compression = CompressionMode.None };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        // Corrupt the data
        var headerSize = 16;
        var metadataLength = BitConverter.ToInt32(fileData, 8);
        var dataStart = headerSize + metadataLength;
        fileData[dataStart + 1] = (byte)(fileData[dataStart + 1] ^ 0xFF);

        var validatedInfo = SaveFileFormat.Validate(fileData);

        Assert.NotNull(validatedInfo);
        Assert.False(validatedInfo!.IsValid);
        Assert.Contains("Checksum mismatch", validatedInfo.ValidationError);
    }

    [Fact]
    public void Validate_WithInvalidMagic_ReturnsNull()
    {
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };

        var validatedInfo = SaveFileFormat.Validate(invalidData);

        Assert.Null(validatedInfo);
    }

    #endregion

    #region IsValidFormat Tests

    [Fact]
    public void IsValidFormat_WithValidHeader_ReturnsTrue()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);

        Assert.True(SaveFileFormat.IsValidFormat(fileData));
    }

    [Fact]
    public void IsValidFormat_WithInvalidMagic_ReturnsFalse()
    {
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00 };

        Assert.False(SaveFileFormat.IsValidFormat(invalidData));
    }

    [Fact]
    public void IsValidFormat_WithTooSmallData_ReturnsFalse()
    {
        var tooSmall = new byte[] { (byte)'K', (byte)'S', (byte)'A' };

        Assert.False(SaveFileFormat.IsValidFormat(tooSmall));
    }

    [Fact]
    public void IsValidFormat_WithNullData_ReturnsFalse()
    {
        Assert.False(SaveFileFormat.IsValidFormat((byte[])null!));
    }

    [Fact]
    public void IsValidFormat_Stream_WithValidHeader_ReturnsTrue()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        using var stream = new MemoryStream(fileData);

        Assert.True(SaveFileFormat.IsValidFormat(stream));
    }

    [Fact]
    public void IsValidFormat_Stream_ResetPositionAfterCheck()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        using var stream = new MemoryStream(fileData);
        stream.Position = 10; // Set a non-zero position

        SaveFileFormat.IsValidFormat(stream);

        Assert.Equal(10, stream.Position);
    }

    #endregion

    #region Compression Tests

    [Fact]
    public void Write_WithGZip_ProducesSmallerOutput()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();

        var uncompressedOptions = new SaveSlotOptions { Compression = CompressionMode.None };
        var compressedOptions = new SaveSlotOptions { Compression = CompressionMode.GZip };

        var uncompressedFile = SaveFileFormat.Write(slotInfo, snapshotData, uncompressedOptions);
        var compressedFile = SaveFileFormat.Write(slotInfo, snapshotData, compressedOptions);

        Assert.True(compressedFile.Length < uncompressedFile.Length,
            $"Compressed ({compressedFile.Length}) should be smaller than uncompressed ({uncompressedFile.Length})");
    }

    [Fact]
    public void Write_WithBrotli_ProducesSmallerOutput()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();

        var uncompressedOptions = new SaveSlotOptions { Compression = CompressionMode.None };
        var compressedOptions = new SaveSlotOptions { Compression = CompressionMode.Brotli };

        var uncompressedFile = SaveFileFormat.Write(slotInfo, snapshotData, uncompressedOptions);
        var compressedFile = SaveFileFormat.Write(slotInfo, snapshotData, compressedOptions);

        Assert.True(compressedFile.Length < uncompressedFile.Length,
            $"Compressed ({compressedFile.Length}) should be smaller than uncompressed ({uncompressedFile.Length})");
    }

    [Fact]
    public void Write_UpdatesSlotInfoWithSizes()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateLargeTestSnapshotData();
        var options = new SaveSlotOptions { Compression = CompressionMode.GZip };

        var fileData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        var (readSlotInfo, _) = SaveFileFormat.Read(fileData);

        Assert.Equal(snapshotData.Length, readSlotInfo.UncompressedSize);
        Assert.True(readSlotInfo.CompressedSize < readSlotInfo.UncompressedSize);
    }

    #endregion

    #region Stream Tests

    [Fact]
    public void Write_ToStream_CreatesValidFile()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = SaveSlotOptions.Default;

        using var stream = new MemoryStream();
        SaveFileFormat.Write(stream, slotInfo, snapshotData, options);

        stream.Position = 0;
        Assert.True(SaveFileFormat.IsValidFormat(stream));
    }

    [Fact]
    public void Read_FromStream_ReturnsOriginalData()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();
        var options = new SaveSlotOptions { Compression = CompressionMode.None };

        using var writeStream = new MemoryStream();
        SaveFileFormat.Write(writeStream, slotInfo, snapshotData, options);

        writeStream.Position = 0;
        var (readSlotInfo, readSnapshotData) = SaveFileFormat.Read(writeStream);

        Assert.Equal(slotInfo.SlotName, readSlotInfo.SlotName);
        Assert.Equal(snapshotData, readSnapshotData);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Read_WithUnsupportedVersion_ThrowsInvalidDataException()
    {
        // Create a file with version 255
        var fileData = new byte[]
        {
            (byte)'K', (byte)'S', (byte)'A', (byte)'V', // Magic
            0xFF, 0x00, // Version = 255
            0x00, 0x00, // Flags
            0x02, 0x00, 0x00, 0x00, // MetadataLength = 2
            0x02, 0x00, 0x00, 0x00, // DataLength = 2
            (byte)'{', (byte)'}', // Empty JSON
            0x00, 0x00 // Data
        };

        var ex = Assert.Throws<InvalidDataException>(() => SaveFileFormat.Read(fileData));
        Assert.Contains("version", ex.Message.ToLower());
    }

    [Fact]
    public void Write_ThrowsOnNullStream()
    {
        var slotInfo = CreateTestSlotInfo();
        var snapshotData = CreateTestSnapshotData();

        Assert.Throws<ArgumentNullException>(() =>
            SaveFileFormat.Write(null!, slotInfo, snapshotData, SaveSlotOptions.Default));
    }

    [Fact]
    public void Write_ThrowsOnNullSlotInfo()
    {
        using var stream = new MemoryStream();
        var snapshotData = CreateTestSnapshotData();

        Assert.Throws<ArgumentNullException>(() =>
            SaveFileFormat.Write(stream, null!, snapshotData, SaveSlotOptions.Default));
    }

    [Fact]
    public void Write_ThrowsOnNullSnapshotData()
    {
        using var stream = new MemoryStream();
        var slotInfo = CreateTestSlotInfo();

        Assert.Throws<ArgumentNullException>(() =>
            SaveFileFormat.Write(stream, slotInfo, null!, SaveSlotOptions.Default));
    }

    [Fact]
    public void Read_ThrowsOnInvalidMagic()
    {
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        Assert.Throws<InvalidDataException>(() => SaveFileFormat.Read(invalidData));
    }

    #endregion

    #region Helper Methods

    private static SaveSlotInfo CreateTestSlotInfo() => new()
    {
        SlotName = "test_slot",
        DisplayName = "Test Save",
        CreatedAt = DateTimeOffset.UtcNow,
        ModifiedAt = DateTimeOffset.UtcNow,
        PlayTime = TimeSpan.FromMinutes(30),
        SaveCount = 1,
        EntityCount = 10,
        AppVersion = "1.0.0"
    };

    private static byte[] CreateTestSnapshotData()
    {
        // Simulate a small snapshot
        return System.Text.Encoding.UTF8.GetBytes(
            """{"version":1,"timestamp":"2024-01-15T10:00:00Z","entities":[],"singletons":[]}""");
    }

    private static byte[] CreateLargeTestSnapshotData()
    {
        // Create larger data that compresses well
        var sb = new System.Text.StringBuilder();
        sb.Append("""{"version":1,"timestamp":"2024-01-15T10:00:00Z","entities":[""");

        for (int i = 0; i < 100; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append($$$"""{"id":{{{i}}},"name":"Entity{{{i}}}","components":[{"typeName":"Position","data":{"x":{{{i * 10}}},"y":{{{i * 20}}}}}]}""");
        }

        sb.Append("""],"singletons":[]}""");
        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    #endregion
}
