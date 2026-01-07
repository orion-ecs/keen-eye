using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace KeenEyes.Replay;

/// <summary>
/// Handles reading and writing .kreplay container files.
/// </summary>
/// <remarks>
/// <para>
/// The .kreplay format is a binary container optimized for replay recordings:
/// </para>
/// <list type="bullet">
/// <item><description>Header with magic bytes, version, and flags</description></item>
/// <item><description>Replay metadata (JSON)</description></item>
/// <item><description>Compressed replay data</description></item>
/// <item><description>CRC32 checksum for corruption detection</description></item>
/// </list>
/// <para>
/// File structure:
/// <code>
/// [Header: 16 bytes]
///   - Magic: "KRPL" (4 bytes)
///   - Version: uint16 (2 bytes)
///   - Flags: uint16 (2 bytes)
///   - MetadataLength: uint32 (4 bytes)
///   - DataLength: uint32 (4 bytes)
/// [Metadata: variable]
///   - JSON-encoded ReplayFileInfo
/// [Data: variable]
///   - Compressed replay data
/// [Checksum: 4 bytes, optional]
///   - CRC32 hash of compressed data
/// </code>
/// </para>
/// </remarks>
public static class ReplayFileFormat
{
    /// <summary>
    /// Magic bytes identifying .kreplay files.
    /// </summary>
    public static ReadOnlySpan<byte> Magic => "KRPL"u8;

    /// <summary>
    /// Current file format version.
    /// </summary>
    public const ushort CurrentVersion = 1;

    /// <summary>
    /// Default file extension for replay files.
    /// </summary>
    public const string Extension = ".kreplay";

    /// <summary>
    /// Header size in bytes.
    /// </summary>
    private const int HeaderSize = 16;

    /// <summary>
    /// File format flags.
    /// </summary>
    [Flags]
    private enum FormatOption : ushort
    {
        None = 0,

        /// <summary>Indicates a CRC32 checksum is present at the end of the file.</summary>
        HasChecksum = 1 << 0,

        /// <summary>Data is compressed with GZip.</summary>
        GZipCompressed = 1 << 1,

        /// <summary>Data is compressed with Brotli.</summary>
        BrotliCompressed = 1 << 2
    }

    /// <summary>
    /// Writes a replay file to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="replayData">The replay data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public static void Write(
        Stream stream,
        ReplayData replayData,
        ReplayFileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(replayData);

        options ??= ReplayFileOptions.Default;

        // Serialize replay data to JSON
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(replayData, ReplayJsonContext.Default.ReplayData);

        // Compress data if needed
        var (compressedData, compressionFlag) = CompressData(jsonData, options.Compression, options.CompressionLevel);

        // Build flags
        var flags = compressionFlag;
        if (options.IncludeChecksum)
        {
            flags |= FormatOption.HasChecksum;
        }

        // Compute checksum if needed
        string? checksum = null;
        if (options.IncludeChecksum)
        {
            checksum = ComputeChecksum(compressedData);
        }

        // Build file info
        var fileInfo = new ReplayFileInfo
        {
            Name = replayData.Name,
            Description = replayData.Description,
            RecordingStarted = replayData.RecordingStarted,
            RecordingEnded = replayData.RecordingEnded,
            Duration = replayData.Duration,
            FrameCount = replayData.FrameCount,
            SnapshotCount = replayData.Snapshots.Count,
            UncompressedSize = jsonData.Length,
            CompressedSize = compressedData.Length,
            Compression = options.Compression,
            Checksum = checksum,
            DataVersion = replayData.Version
        };

        // Serialize metadata
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(fileInfo, ReplayJsonContext.Default.ReplayFileInfo);

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
        if (options.IncludeChecksum && checksum is not null)
        {
            var checksumValue = uint.Parse(checksum, System.Globalization.NumberStyles.HexNumber);
            writer.Write(checksumValue);
        }
    }

    /// <summary>
    /// Writes a replay file to a byte array.
    /// </summary>
    /// <param name="replayData">The replay data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    /// <returns>The complete replay file as a byte array.</returns>
    public static byte[] Write(ReplayData replayData, ReplayFileOptions? options = null)
    {
        using var stream = new MemoryStream();
        Write(stream, replayData, options);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a replay file to disk.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="replayData">The replay data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    public static void WriteToFile(string path, ReplayData replayData, ReplayFileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(stream, replayData, options);
    }

    /// <summary>
    /// Reads replay file metadata from a stream without loading the full data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The replay file metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static ReplayFileInfo ReadMetadata(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (_, metadataLength, _) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);

        return JsonSerializer.Deserialize(metadataBytes, ReplayJsonContext.Default.ReplayFileInfo)
            ?? throw new InvalidDataException("Failed to deserialize replay file metadata.");
    }

    /// <summary>
    /// Reads replay file metadata from a byte array.
    /// </summary>
    /// <param name="data">The replay file data.</param>
    /// <returns>The replay file metadata.</returns>
    public static ReplayFileInfo ReadMetadata(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ReadMetadata(stream);
    }

    /// <summary>
    /// Reads replay file metadata from disk.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <returns>The replay file metadata.</returns>
    public static ReplayFileInfo ReadMetadataFromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadMetadata(stream);
    }

    /// <summary>
    /// Reads the complete replay file including data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and replay data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is invalid or corrupted.</exception>
    public static (ReplayFileInfo FileInfo, ReplayData Data) Read(Stream stream, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (flags, metadataLength, dataLength) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);
        var fileInfo = JsonSerializer.Deserialize(metadataBytes, ReplayJsonContext.Default.ReplayFileInfo)
            ?? throw new InvalidDataException("Failed to deserialize replay file metadata.");

        // Read compressed data
        var compressedData = reader.ReadBytes(dataLength);

        // Validate checksum if present and requested
        if ((flags & FormatOption.HasChecksum) != 0 && validateChecksum)
        {
            var storedChecksum = reader.ReadUInt32();
            var computedChecksum = ComputeChecksumValue(compressedData);

            if (storedChecksum != computedChecksum)
            {
                var invalidInfo = fileInfo with
                {
                    ValidationError = $"Checksum mismatch: expected {storedChecksum:X8}, got {computedChecksum:X8}"
                };
                throw new InvalidDataException(invalidInfo.ValidationError);
            }
        }

        // Decompress data
        var compression = GetCompressionMode(flags);
        var jsonData = DecompressData(compressedData, compression);

        // Deserialize replay data
        var replayData = JsonSerializer.Deserialize(jsonData, ReplayJsonContext.Default.ReplayData)
            ?? throw new InvalidDataException("Failed to deserialize replay data.");

        return (fileInfo, replayData);
    }

    /// <summary>
    /// Reads the complete replay file from a byte array.
    /// </summary>
    /// <param name="data">The replay file data.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and replay data.</returns>
    public static (ReplayFileInfo FileInfo, ReplayData Data) Read(byte[] data, bool validateChecksum = true)
    {
        using var stream = new MemoryStream(data);
        return Read(stream, validateChecksum);
    }

    /// <summary>
    /// Reads the complete replay file from disk.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and replay data.</returns>
    public static (ReplayFileInfo FileInfo, ReplayData Data) ReadFromFile(string path, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(stream, validateChecksum);
    }

    /// <summary>
    /// Validates a replay file without fully loading the data.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static ReplayFileInfo? Validate(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read and validate header
            var (flags, metadataLength, dataLength) = ReadHeader(reader);

            // Read metadata
            var metadataBytes = reader.ReadBytes(metadataLength);
            var fileInfo = JsonSerializer.Deserialize(metadataBytes, ReplayJsonContext.Default.ReplayFileInfo);

            if (fileInfo is null)
            {
                return null;
            }

            // Validate checksum if present
            if ((flags & FormatOption.HasChecksum) != 0)
            {
                var compressedData = reader.ReadBytes(dataLength);
                var storedChecksum = reader.ReadUInt32();
                var computedChecksum = ComputeChecksumValue(compressedData);

                if (storedChecksum != computedChecksum)
                {
                    return fileInfo with
                    {
                        ValidationError = $"Checksum mismatch: expected {storedChecksum:X8}, got {computedChecksum:X8}"
                    };
                }
            }

            return fileInfo;
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException)
        {
            return null;
        }
    }

    /// <summary>
    /// Validates a replay file from a byte array.
    /// </summary>
    /// <param name="data">The replay file data.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static ReplayFileInfo? Validate(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Validate(stream);
    }

    /// <summary>
    /// Validates a replay file from disk.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static ReplayFileInfo? ValidateFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Validate(stream);
    }

    /// <summary>
    /// Checks if a stream contains a valid .kreplay file header.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <returns>True if the stream appears to be a .kreplay file.</returns>
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
    /// Checks if a byte array contains a valid .kreplay file header.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data appears to be a .kreplay file.</returns>
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
    private static (FormatOption Flags, int MetadataLength, int DataLength) ReadHeader(BinaryReader reader)
    {
        // Read magic
        Span<byte> magic = stackalloc byte[4];
        if (reader.Read(magic) != 4 || !magic.SequenceEqual(Magic))
        {
            throw new InvalidDataException("Invalid replay file: missing or incorrect magic bytes.");
        }

        // Read version
        var version = reader.ReadUInt16();
        if (version > CurrentVersion)
        {
            throw new InvalidDataException(
                $"Replay file version {version} is not supported. Maximum supported version is {CurrentVersion}.");
        }

        // Read flags and lengths
        var flags = (FormatOption)reader.ReadUInt16();
        var metadataLength = reader.ReadInt32();
        var dataLength = reader.ReadInt32();

        if (metadataLength <= 0 || dataLength < 0)
        {
            throw new InvalidDataException("Invalid replay file: invalid metadata or data length.");
        }

        return (flags, metadataLength, dataLength);
    }

    /// <summary>
    /// Compresses data using the specified compression mode.
    /// </summary>
    private static (byte[] CompressedData, FormatOption Flag) CompressData(
        byte[] data,
        CompressionMode compression,
        CompressionLevel level)
    {
        if (compression == CompressionMode.None)
        {
            return (data, FormatOption.None);
        }

        using var output = new MemoryStream();

        switch (compression)
        {
            case CompressionMode.GZip:
                using (var gzip = new GZipStream(output, level, leaveOpen: true))
                {
                    gzip.Write(data);
                }
                return (output.ToArray(), FormatOption.GZipCompressed);

            case CompressionMode.Brotli:
                using (var brotli = new BrotliStream(output, level, leaveOpen: true))
                {
                    brotli.Write(data);
                }
                return (output.ToArray(), FormatOption.BrotliCompressed);

            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, "Unknown compression mode.");
        }
    }

    /// <summary>
    /// Decompresses data using the specified compression mode.
    /// </summary>
    private static byte[] DecompressData(byte[] compressedData, CompressionMode compression)
    {
        if (compression == CompressionMode.None)
        {
            return compressedData;
        }

        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();

        switch (compression)
        {
            case CompressionMode.GZip:
                using (var gzip = new GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    gzip.CopyTo(output);
                }
                return output.ToArray();

            case CompressionMode.Brotli:
                using (var brotli = new BrotliStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    brotli.CopyTo(output);
                }
                return output.ToArray();

            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, "Unknown compression mode.");
        }
    }

    /// <summary>
    /// Gets the compression mode from flags.
    /// </summary>
    private static CompressionMode GetCompressionMode(FormatOption flags)
    {
        if ((flags & FormatOption.BrotliCompressed) != 0)
        {
            return CompressionMode.Brotli;
        }

        if ((flags & FormatOption.GZipCompressed) != 0)
        {
            return CompressionMode.GZip;
        }

        return CompressionMode.None;
    }

    /// <summary>
    /// Computes a CRC32 checksum of the data as a hex string.
    /// </summary>
    private static string ComputeChecksum(byte[] data)
    {
        return ComputeChecksumValue(data).ToString("X8");
    }

    /// <summary>
    /// Computes a CRC32 checksum of the data as a uint.
    /// </summary>
    private static uint ComputeChecksumValue(byte[] data)
    {
        return Crc32.HashToUInt32(data);
    }
}
