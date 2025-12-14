using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for SaveSlotInfo, SaveSlotOptions, and related types.
/// </summary>
public class SaveSlotTests
{
    #region SaveSlotInfo Tests

    [Fact]
    public void SaveSlotInfo_RequiredProperties_MustBeSet()
    {
        var info = new SaveSlotInfo
        {
            SlotName = "slot1",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("slot1", info.SlotName);
        Assert.NotEqual(default, info.CreatedAt);
        Assert.NotEqual(default, info.ModifiedAt);
    }

    [Fact]
    public void SaveSlotInfo_DefaultValues_AreCorrect()
    {
        var info = new SaveSlotInfo
        {
            SlotName = "test",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        Assert.Null(info.DisplayName);
        Assert.Equal(TimeSpan.Zero, info.PlayTime);
        Assert.Equal(1, info.SaveCount);
        Assert.Equal(SaveFormat.Binary, info.Format);
        Assert.Equal(CompressionMode.GZip, info.Compression);
        Assert.Equal(0, info.CompressedSize);
        Assert.Equal(0, info.UncompressedSize);
        Assert.Null(info.Checksum);
        Assert.Equal(0, info.EntityCount);
        Assert.Null(info.ThumbnailBase64);
        Assert.Null(info.ThumbnailMimeType);
        Assert.Null(info.CustomMetadata);
        Assert.Null(info.AppVersion);
        Assert.Equal(1, info.FormatVersion);
        Assert.True(info.IsValid);
        Assert.Null(info.ValidationError);
    }

    [Fact]
    public void SaveSlotInfo_WithAllProperties_RoundTripsToJson()
    {
        var metadata = new Dictionary<string, object>
        {
            ["level"] = 15,
            ["location"] = "Dark Forest"
        };

        var info = new SaveSlotInfo
        {
            SlotName = "slot1",
            DisplayName = "Chapter 3 - The Forest",
            CreatedAt = DateTimeOffset.Parse("2024-01-15T10:30:00Z"),
            ModifiedAt = DateTimeOffset.Parse("2024-01-15T12:45:00Z"),
            PlayTime = TimeSpan.FromHours(2.5),
            SaveCount = 5,
            Format = SaveFormat.Binary,
            Compression = CompressionMode.Brotli,
            CompressedSize = 1024,
            UncompressedSize = 4096,
            Checksum = "abcd1234",
            EntityCount = 150,
            ThumbnailBase64 = "iVBORw0KGgo=",
            ThumbnailMimeType = "image/png",
            CustomMetadata = metadata,
            AppVersion = "1.2.3",
            FormatVersion = 1
        };

        var json = JsonSerializer.Serialize(info, SnapshotJsonContext.Default.SaveSlotInfo);
        var restored = JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.SaveSlotInfo);

        Assert.NotNull(restored);
        Assert.Equal(info.SlotName, restored!.SlotName);
        Assert.Equal(info.DisplayName, restored.DisplayName);
        Assert.Equal(info.PlayTime, restored.PlayTime);
        Assert.Equal(info.SaveCount, restored.SaveCount);
        Assert.Equal(info.Format, restored.Format);
        Assert.Equal(info.Compression, restored.Compression);
        Assert.Equal(info.CompressedSize, restored.CompressedSize);
        Assert.Equal(info.UncompressedSize, restored.UncompressedSize);
        Assert.Equal(info.Checksum, restored.Checksum);
        Assert.Equal(info.EntityCount, restored.EntityCount);
        Assert.Equal(info.ThumbnailBase64, restored.ThumbnailBase64);
        Assert.Equal(info.ThumbnailMimeType, restored.ThumbnailMimeType);
        Assert.Equal(info.AppVersion, restored.AppVersion);
        Assert.Equal(info.FormatVersion, restored.FormatVersion);
    }

    [Fact]
    public void SaveSlotInfo_IsValid_ReturnsFalseWhenValidationErrorSet()
    {
        var info = new SaveSlotInfo
        {
            SlotName = "corrupted",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            ValidationError = "Checksum mismatch"
        };

        Assert.False(info.IsValid);
        Assert.Equal("Checksum mismatch", info.ValidationError);
    }

    [Fact]
    public void SaveSlotInfo_WithExpression_CreatesCopy()
    {
        var original = new SaveSlotInfo
        {
            SlotName = "original",
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            SaveCount = 1
        };

        var copy = original with { SaveCount = 5, DisplayName = "Updated" };

        Assert.Equal("original", copy.SlotName);
        Assert.Equal(5, copy.SaveCount);
        Assert.Equal("Updated", copy.DisplayName);
        Assert.Equal(1, original.SaveCount);
    }

    #endregion

    #region SaveSlotOptions Tests

    [Fact]
    public void SaveSlotOptions_DefaultValues_AreCorrect()
    {
        var options = new SaveSlotOptions();

        Assert.Equal(SaveFormat.Binary, options.Format);
        Assert.Equal(CompressionMode.GZip, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.Optimal, options.CompressionLevel);
        Assert.True(options.IncludeChecksum);
        Assert.Null(options.DisplayName);
        Assert.Null(options.CustomMetadata);
        Assert.Equal(TimeSpan.Zero, options.PlayTime);
        Assert.Null(options.ThumbnailData);
        Assert.Null(options.ThumbnailMimeType);
        Assert.Null(options.AppVersion);
    }

    [Fact]
    public void SaveSlotOptions_Default_HasOptimalSettings()
    {
        var options = SaveSlotOptions.Default;

        Assert.Equal(SaveFormat.Binary, options.Format);
        Assert.Equal(CompressionMode.GZip, options.Compression);
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void SaveSlotOptions_Fast_HasFastestSettings()
    {
        var options = SaveSlotOptions.Fast;

        Assert.Equal(SaveFormat.Binary, options.Format);
        Assert.Equal(System.IO.Compression.CompressionLevel.Fastest, options.CompressionLevel);
        Assert.False(options.IncludeChecksum);
    }

    [Fact]
    public void SaveSlotOptions_Compact_HasSmallestSizeSettings()
    {
        var options = SaveSlotOptions.Compact;

        Assert.Equal(SaveFormat.Binary, options.Format);
        Assert.Equal(CompressionMode.Brotli, options.Compression);
        Assert.Equal(System.IO.Compression.CompressionLevel.SmallestSize, options.CompressionLevel);
        Assert.True(options.IncludeChecksum);
    }

    [Fact]
    public void SaveSlotOptions_Debug_HasReadableSettings()
    {
        var options = SaveSlotOptions.Debug;

        Assert.Equal(SaveFormat.Json, options.Format);
        Assert.Equal(CompressionMode.None, options.Compression);
        Assert.False(options.IncludeChecksum);
    }

    [Fact]
    public void SaveSlotOptions_WithThumbnail_StoresData()
    {
        var thumbnailData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes

        var options = new SaveSlotOptions
        {
            ThumbnailData = thumbnailData,
            ThumbnailMimeType = "image/png"
        };

        Assert.Equal(thumbnailData, options.ThumbnailData);
        Assert.Equal("image/png", options.ThumbnailMimeType);
    }

    [Fact]
    public void SaveSlotOptions_WithCustomMetadata_StoresMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            ["score"] = 1000,
            ["difficulty"] = "hard"
        };

        var options = new SaveSlotOptions
        {
            CustomMetadata = metadata
        };

        Assert.NotNull(options.CustomMetadata);
        Assert.Equal(1000, options.CustomMetadata["score"]);
        Assert.Equal("hard", options.CustomMetadata["difficulty"]);
    }

    [Fact]
    public void SaveSlotOptions_WithExpression_CreatesCopy()
    {
        var original = SaveSlotOptions.Default;
        var copy = original with { DisplayName = "My Save", IncludeChecksum = false };

        Assert.Equal("My Save", copy.DisplayName);
        Assert.False(copy.IncludeChecksum);
        Assert.True(SaveSlotOptions.Default.IncludeChecksum);
    }

    #endregion

    #region CompressionMode Tests

    [Fact]
    public void CompressionMode_None_HasValueZero()
    {
        Assert.Equal(0, (int)CompressionMode.None);
    }

    [Fact]
    public void CompressionMode_GZip_HasValueOne()
    {
        Assert.Equal(1, (int)CompressionMode.GZip);
    }

    [Fact]
    public void CompressionMode_Brotli_HasValueTwo()
    {
        Assert.Equal(2, (int)CompressionMode.Brotli);
    }

    [Fact]
    public void CompressionMode_SerializesToJson()
    {
        var mode = CompressionMode.Brotli;
        var json = JsonSerializer.Serialize(mode, SnapshotJsonContext.Default.CompressionMode);

        Assert.Equal("2", json);
    }

    [Fact]
    public void CompressionMode_DeserializesFromJson()
    {
        var mode = JsonSerializer.Deserialize("1", SnapshotJsonContext.Default.CompressionMode);

        Assert.Equal(CompressionMode.GZip, mode);
    }

    #endregion

    #region SaveFormat Tests

    [Fact]
    public void SaveFormat_Binary_HasValueZero()
    {
        Assert.Equal(0, (int)SaveFormat.Binary);
    }

    [Fact]
    public void SaveFormat_Json_HasValueOne()
    {
        Assert.Equal(1, (int)SaveFormat.Json);
    }

    [Fact]
    public void SaveFormat_SerializesToJson()
    {
        var format = SaveFormat.Json;
        var json = JsonSerializer.Serialize(format, SnapshotJsonContext.Default.SaveFormat);

        Assert.Equal("1", json);
    }

    [Fact]
    public void SaveFormat_DeserializesFromJson()
    {
        var format = JsonSerializer.Deserialize("0", SnapshotJsonContext.Default.SaveFormat);

        Assert.Equal(SaveFormat.Binary, format);
    }

    #endregion
}
