using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KeenEyes.Serialization;

/// <summary>
/// Handles reading and writing .ksave container files.
/// </summary>
/// <remarks>
/// <para>
/// The .ksave format is a binary container that stores:
/// </para>
/// <list type="bullet">
/// <item><description>Header with magic bytes, version, and flags</description></item>
/// <item><description>Save slot metadata (JSON)</description></item>
/// <item><description>Compressed world snapshot data</description></item>
/// <item><description>Optional SHA256 checksum</description></item>
/// </list>
/// <para>
/// File structure:
/// <code>
/// [Header: 16 bytes]
///   - Magic: "KSAV" (4 bytes)
///   - Version: uint16 (2 bytes)
///   - Flags: uint16 (2 bytes)
///   - MetadataLength: uint32 (4 bytes)
///   - DataLength: uint32 (4 bytes)
/// [Metadata: variable]
///   - JSON-encoded SaveSlotInfo
/// [Data: variable]
///   - Compressed snapshot data
/// [Checksum: 32 bytes, optional]
///   - SHA256 hash of compressed data
/// </code>
/// </para>
/// </remarks>
public static class SaveFileFormat
{
    /// <summary>
    /// Magic bytes identifying .ksave files.
    /// </summary>
    public static ReadOnlySpan<byte> Magic => "KSAV"u8;

    /// <summary>
    /// Current file format version.
    /// </summary>
    public const ushort CurrentVersion = 1;

    /// <summary>
    /// Default file extension for save files.
    /// </summary>
    public const string Extension = ".ksave";

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    private const int HeaderSize = 16;

    /// <summary>
    /// SHA256 checksum size in bytes.
    /// </summary>
    private const int ChecksumSize = 32;

    /// <summary>
    /// Maximum allowed metadata size (10MB) to prevent DoS via memory exhaustion.
    /// </summary>
    private const int MaxMetadataLength = 10_000_000;

    /// <summary>
    /// Maximum allowed compressed data size (500MB) to prevent DoS via memory exhaustion.
    /// </summary>
    private const int MaxCompressedDataLength = 500_000_000;

    /// <summary>
    /// Maximum allowed decompressed data size (500MB) to prevent decompression bomb attacks.
    /// </summary>
    private const long MaxDecompressedSize = 500_000_000;

    /// <summary>
    /// File format flags.
    /// </summary>
    [Flags]
    private enum FormatFlags : ushort
    {
        None = 0,
        /// <summary>Indicates a SHA256 checksum is present at the end of the file.</summary>
        HasChecksum = 1 << 0,
        /// <summary>Data is compressed with GZip.</summary>
        GZipCompressed = 1 << 1,
        /// <summary>Data is compressed with Brotli.</summary>
        BrotliCompressed = 1 << 2
    }

    /// <summary>
    /// Writes a save file to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="slotInfo">The save slot metadata.</param>
    /// <param name="snapshotData">The world snapshot data (binary or JSON bytes).</param>
    /// <param name="options">Save options for compression and checksum settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public static void Write(
        Stream stream,
        SaveSlotInfo slotInfo,
        byte[] snapshotData,
        SaveSlotOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(slotInfo);
        ArgumentNullException.ThrowIfNull(snapshotData);
        ArgumentNullException.ThrowIfNull(options);

        // Compress data if needed
        var (compressedData, compressionFlag) = CompressData(snapshotData, options.Compression, options.CompressionLevel);

        // Build flags
        var flags = compressionFlag;
        if (options.IncludeChecksum)
        {
            flags |= FormatFlags.HasChecksum;
        }

        // Update slot info with sizes
        var updatedSlotInfo = slotInfo with
        {
            CompressedSize = compressedData.Length,
            UncompressedSize = snapshotData.Length,
            Compression = options.Compression,
            Checksum = options.IncludeChecksum ? ComputeChecksum(compressedData) : null
        };

        // Serialize metadata
        var metadataJson = JsonSerializer.Serialize(updatedSlotInfo, SnapshotJsonContext.Default.SaveSlotInfo);
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write header
        writer.Write(Magic);
        writer.Write(CurrentVersion);
        writer.Write((ushort)flags);
        writer.Write(metadataBytes.Length);
        writer.Write(compressedData.Length);

        // Write metadata
        writer.Write(metadataBytes);

        // Write compressed data
        writer.Write(compressedData);

        // Write checksum if enabled
        if (options.IncludeChecksum && updatedSlotInfo.Checksum is not null)
        {
            var checksumBytes = Convert.FromHexString(updatedSlotInfo.Checksum);
            writer.Write(checksumBytes);
        }
    }

    /// <summary>
    /// Writes a save file to a byte array.
    /// </summary>
    /// <param name="slotInfo">The save slot metadata.</param>
    /// <param name="snapshotData">The world snapshot data.</param>
    /// <param name="options">Save options.</param>
    /// <returns>The complete save file as a byte array.</returns>
    public static byte[] Write(SaveSlotInfo slotInfo, byte[] snapshotData, SaveSlotOptions options)
    {
        using var stream = new MemoryStream();
        Write(stream, slotInfo, snapshotData, options);
        return stream.ToArray();
    }

    /// <summary>
    /// Reads save slot metadata from a stream without loading the full snapshot.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The save slot metadata, or null if the file is invalid.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static SaveSlotInfo ReadMetadata(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (_, metadataLength, _) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);
        var metadataJson = Encoding.UTF8.GetString(metadataBytes);

        return JsonSerializer.Deserialize(metadataJson, SnapshotJsonContext.Default.SaveSlotInfo)
            ?? throw new InvalidDataException("Failed to deserialize save slot metadata.");
    }

    /// <summary>
    /// Reads save slot metadata from a byte array.
    /// </summary>
    /// <param name="data">The save file data.</param>
    /// <returns>The save slot metadata.</returns>
    public static SaveSlotInfo ReadMetadata(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ReadMetadata(stream);
    }

    /// <summary>
    /// Reads the complete save file including snapshot data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the slot info and decompressed snapshot data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is invalid or corrupted.</exception>
    public static (SaveSlotInfo SlotInfo, byte[] SnapshotData) Read(Stream stream, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (flags, metadataLength, dataLength) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);
        var metadataJson = Encoding.UTF8.GetString(metadataBytes);
        var slotInfo = JsonSerializer.Deserialize(metadataJson, SnapshotJsonContext.Default.SaveSlotInfo)
            ?? throw new InvalidDataException("Failed to deserialize save slot metadata.");

        // Read compressed data
        var compressedData = reader.ReadBytes(dataLength);

        // Validate checksum if present and requested
        if ((flags & FormatFlags.HasChecksum) != 0 && validateChecksum)
        {
            var storedChecksumBytes = reader.ReadBytes(ChecksumSize);
            var storedChecksum = Convert.ToHexStringLower(storedChecksumBytes);
            var computedChecksum = ComputeChecksum(compressedData);

            if (!string.Equals(storedChecksum, computedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                var invalidInfo = slotInfo with
                {
                    ValidationError = $"Checksum mismatch: expected {storedChecksum}, got {computedChecksum}"
                };
                throw new InvalidDataException(invalidInfo.ValidationError);
            }
        }

        // Decompress data
        var compression = GetCompressionMode(flags);
        var snapshotData = DecompressData(compressedData, compression);

        return (slotInfo, snapshotData);
    }

    /// <summary>
    /// Reads the complete save file from a byte array.
    /// </summary>
    /// <param name="data">The save file data.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the slot info and decompressed snapshot data.</returns>
    public static (SaveSlotInfo SlotInfo, byte[] SnapshotData) Read(byte[] data, bool validateChecksum = true)
    {
        using var stream = new MemoryStream(data);
        return Read(stream, validateChecksum);
    }

    /// <summary>
    /// Validates a save file without fully loading the snapshot data.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <returns>The slot info with validation result, or null if completely invalid.</returns>
    public static SaveSlotInfo? Validate(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read and validate header
            var (flags, metadataLength, dataLength) = ReadHeader(reader);

            // Read metadata
            var metadataBytes = reader.ReadBytes(metadataLength);
            var metadataJson = Encoding.UTF8.GetString(metadataBytes);
            var slotInfo = JsonSerializer.Deserialize(metadataJson, SnapshotJsonContext.Default.SaveSlotInfo);

            if (slotInfo is null)
            {
                return null;
            }

            // Validate checksum if present
            if ((flags & FormatFlags.HasChecksum) != 0)
            {
                var compressedData = reader.ReadBytes(dataLength);
                var storedChecksumBytes = reader.ReadBytes(ChecksumSize);
                var storedChecksum = Convert.ToHexStringLower(storedChecksumBytes);
                var computedChecksum = ComputeChecksum(compressedData);

                if (!string.Equals(storedChecksum, computedChecksum, StringComparison.OrdinalIgnoreCase))
                {
                    return slotInfo with
                    {
                        ValidationError = $"Checksum mismatch: expected {storedChecksum}, got {computedChecksum}"
                    };
                }
            }

            return slotInfo;
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            return null;
        }
    }

    /// <summary>
    /// Validates a save file from a byte array.
    /// </summary>
    /// <param name="data">The save file data.</param>
    /// <returns>The slot info with validation result, or null if completely invalid.</returns>
    public static SaveSlotInfo? Validate(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Validate(stream);
    }

    /// <summary>
    /// Checks if a stream contains a valid .ksave file header.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <returns>True if the stream appears to be a .ksave file.</returns>
    public static bool IsValidFormat(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead || stream.Length < HeaderSize)
        {
            return false;
        }

        var position = stream.Position;
        try
        {
            Span<byte> magic = stackalloc byte[4];
            if (stream.Read(magic) != 4)
            {
                return false;
            }

            return magic.SequenceEqual(Magic);
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <summary>
    /// Checks if a byte array contains a valid .ksave file header.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data appears to be a .ksave file.</returns>
    public static bool IsValidFormat(byte[] data)
    {
        if (data is null || data.Length < HeaderSize)
        {
            return false;
        }

        return data.AsSpan(0, 4).SequenceEqual(Magic);
    }

    /// <summary>
    /// Reads and validates the file header.
    /// </summary>
    private static (FormatFlags Flags, int MetadataLength, int DataLength) ReadHeader(BinaryReader reader)
    {
        // Read magic
        Span<byte> magic = stackalloc byte[4];
        if (reader.Read(magic) != 4 || !magic.SequenceEqual(Magic))
        {
            throw new InvalidDataException("Invalid save file: missing or incorrect magic bytes.");
        }

        // Read version
        var version = reader.ReadUInt16();
        if (version > CurrentVersion)
        {
            throw new InvalidDataException(
                $"Save file version {version} is not supported. Maximum supported version is {CurrentVersion}.");
        }

        // Read flags and lengths
        var flags = (FormatFlags)reader.ReadUInt16();
        var metadataLength = reader.ReadInt32();
        var dataLength = reader.ReadInt32();

        if (metadataLength <= 0 || dataLength < 0)
        {
            throw new InvalidDataException("Invalid save file: invalid metadata or data length.");
        }

        // Validate lengths to prevent DoS via memory exhaustion
        if (metadataLength > MaxMetadataLength)
        {
            throw new InvalidDataException(
                $"Metadata length {metadataLength} exceeds maximum allowed size ({MaxMetadataLength} bytes).");
        }

        if (dataLength > MaxCompressedDataLength)
        {
            throw new InvalidDataException(
                $"Data length {dataLength} exceeds maximum allowed size ({MaxCompressedDataLength} bytes).");
        }

        return (flags, metadataLength, dataLength);
    }

    /// <summary>
    /// Compresses data using the specified compression mode.
    /// </summary>
    private static (byte[] CompressedData, FormatFlags Flag) CompressData(
        byte[] data,
        CompressionMode compression,
        CompressionLevel level)
    {
        if (compression == CompressionMode.None)
        {
            return (data, FormatFlags.None);
        }

        using var output = new MemoryStream();

        switch (compression)
        {
            case CompressionMode.GZip:
                using (var gzip = new GZipStream(output, level, leaveOpen: true))
                {
                    gzip.Write(data);
                }
                return (output.ToArray(), FormatFlags.GZipCompressed);

            case CompressionMode.Brotli:
                using (var brotli = new BrotliStream(output, level, leaveOpen: true))
                {
                    brotli.Write(data);
                }
                return (output.ToArray(), FormatFlags.BrotliCompressed);

            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, "Unknown compression mode.");
        }
    }

    /// <summary>
    /// Decompresses data using the specified compression mode.
    /// </summary>
    /// <exception cref="InvalidDataException">
    /// Thrown when the decompressed data exceeds the maximum allowed size (decompression bomb protection).
    /// </exception>
    private static byte[] DecompressData(byte[] compressedData, CompressionMode compression)
    {
        if (compression == CompressionMode.None)
        {
            return compressedData;
        }

        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();

        // Use bounded copy to prevent decompression bomb attacks
        switch (compression)
        {
            case CompressionMode.GZip:
                using (var gzip = new GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    CopyWithLimit(gzip, output, MaxDecompressedSize);
                }
                return output.ToArray();

            case CompressionMode.Brotli:
                using (var brotli = new BrotliStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    CopyWithLimit(brotli, output, MaxDecompressedSize);
                }
                return output.ToArray();

            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, "Unknown compression mode.");
        }
    }

    /// <summary>
    /// Copies from source to destination with a maximum size limit to prevent decompression bomb attacks.
    /// </summary>
    /// <param name="source">The source stream to read from.</param>
    /// <param name="destination">The destination stream to write to.</param>
    /// <param name="maxSize">The maximum number of bytes to copy.</param>
    /// <exception cref="InvalidDataException">
    /// Thrown when the source data exceeds the maximum allowed size.
    /// </exception>
    private static void CopyWithLimit(Stream source, Stream destination, long maxSize)
    {
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > maxSize)
            {
                throw new InvalidDataException(
                    $"Decompressed data exceeds maximum allowed size ({maxSize} bytes). " +
                    "This may indicate a decompression bomb attack.");
            }
            destination.Write(buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Gets the compression mode from flags.
    /// </summary>
    private static CompressionMode GetCompressionMode(FormatFlags flags)
    {
        if ((flags & FormatFlags.BrotliCompressed) != 0)
        {
            return CompressionMode.Brotli;
        }

        if ((flags & FormatFlags.GZipCompressed) != 0)
        {
            return CompressionMode.GZip;
        }

        return CompressionMode.None;
    }

    /// <summary>
    /// Computes a SHA256 checksum of the data.
    /// </summary>
    private static string ComputeChecksum(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexStringLower(hash);
    }
}
