using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Handles reading and writing .keghost container files.
/// </summary>
/// <remarks>
/// <para>
/// The .keghost format is a lightweight binary container for ghost recordings:
/// </para>
/// <list type="bullet">
/// <item><description>Header with magic bytes, version, and flags</description></item>
/// <item><description>Ghost metadata (JSON)</description></item>
/// <item><description>Compressed ghost data</description></item>
/// <item><description>CRC32 checksum for corruption detection</description></item>
/// </list>
/// <para>
/// File structure:
/// <code>
/// [Header: 16 bytes]
///   - Magic: "KGHO" (4 bytes)
///   - Version: uint16 (2 bytes)
///   - Flags: uint16 (2 bytes)
///   - MetadataLength: uint32 (4 bytes)
///   - DataLength: uint32 (4 bytes)
/// [Metadata: variable]
///   - JSON-encoded GhostFileInfo
/// [Data: variable]
///   - Compressed ghost data
/// [Checksum: 4 bytes, optional]
///   - CRC32 hash of compressed data
/// </code>
/// </para>
/// </remarks>
public static class GhostFileFormat
{
    /// <summary>
    /// Magic bytes identifying .keghost files.
    /// </summary>
    public static ReadOnlySpan<byte> Magic => "KGHO"u8;

    /// <summary>
    /// Current file format version.
    /// </summary>
    public const ushort CurrentVersion = 1;

    /// <summary>
    /// Default file extension for ghost files.
    /// </summary>
    public const string Extension = ".keghost";

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
    /// Writes a ghost file to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="ghostData">The ghost data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public static void Write(
        Stream stream,
        GhostData ghostData,
        GhostFileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(ghostData);

        options ??= GhostFileOptions.Default;

        // Serialize ghost data to JSON
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(ghostData, GhostJsonContext.Default.GhostData);

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
        var fileInfo = new GhostFileInfo
        {
            Name = ghostData.Name,
            EntityName = ghostData.EntityName,
            RecordingStarted = ghostData.RecordingStarted,
            Duration = ghostData.Duration,
            FrameCount = ghostData.FrameCount,
            TotalDistance = ghostData.TotalDistance,
            UncompressedSize = jsonData.Length,
            CompressedSize = compressedData.Length,
            Compression = options.Compression,
            Checksum = checksum,
            DataVersion = ghostData.Version
        };

        // Serialize metadata
        var metadataBytes = JsonSerializer.SerializeToUtf8Bytes(fileInfo, GhostJsonContext.Default.GhostFileInfo);

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
    /// Writes a ghost file to a byte array.
    /// </summary>
    /// <param name="ghostData">The ghost data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    /// <returns>The complete ghost file as a byte array.</returns>
    public static byte[] Write(GhostData ghostData, GhostFileOptions? options = null)
    {
        using var stream = new MemoryStream();
        Write(stream, ghostData, options);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a ghost file to disk.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="ghostData">The ghost data to save.</param>
    /// <param name="options">Options controlling compression and checksum.</param>
    public static void WriteToFile(string path, GhostData ghostData, GhostFileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(stream, ghostData, options);
    }

    /// <summary>
    /// Reads ghost file metadata from a stream without loading the full data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The ghost file metadata.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static GhostFileInfo ReadMetadata(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (_, metadataLength, _) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);

        return JsonSerializer.Deserialize(metadataBytes, GhostJsonContext.Default.GhostFileInfo)
            ?? throw new InvalidDataException("Failed to deserialize ghost file metadata.");
    }

    /// <summary>
    /// Reads ghost file metadata from a byte array.
    /// </summary>
    /// <param name="data">The ghost file data.</param>
    /// <returns>The ghost file metadata.</returns>
    public static GhostFileInfo ReadMetadata(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ReadMetadata(stream);
    }

    /// <summary>
    /// Reads ghost file metadata from disk.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <returns>The ghost file metadata.</returns>
    public static GhostFileInfo ReadMetadataFromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return ReadMetadata(stream);
    }

    /// <summary>
    /// Reads the complete ghost file including data.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and ghost data.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is invalid or corrupted.</exception>
    public static (GhostFileInfo FileInfo, GhostData Data) Read(Stream stream, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        var (flags, metadataLength, dataLength) = ReadHeader(reader);

        // Read metadata
        var metadataBytes = reader.ReadBytes(metadataLength);
        var fileInfo = JsonSerializer.Deserialize(metadataBytes, GhostJsonContext.Default.GhostFileInfo)
            ?? throw new InvalidDataException("Failed to deserialize ghost file metadata.");

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

        // Deserialize ghost data
        var ghostData = JsonSerializer.Deserialize(jsonData, GhostJsonContext.Default.GhostData)
            ?? throw new InvalidDataException("Failed to deserialize ghost data.");

        return (fileInfo, ghostData);
    }

    /// <summary>
    /// Reads the complete ghost file from a byte array.
    /// </summary>
    /// <param name="data">The ghost file data.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and ghost data.</returns>
    public static (GhostFileInfo FileInfo, GhostData Data) Read(byte[] data, bool validateChecksum = true)
    {
        using var stream = new MemoryStream(data);
        return Read(stream, validateChecksum);
    }

    /// <summary>
    /// Reads the complete ghost file from disk.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>A tuple containing the file info and ghost data.</returns>
    public static (GhostFileInfo FileInfo, GhostData Data) ReadFromFile(string path, bool validateChecksum = true)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(stream, validateChecksum);
    }

    /// <summary>
    /// Validates a ghost file without fully loading the data.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static GhostFileInfo? Validate(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read and validate header
            var (flags, metadataLength, dataLength) = ReadHeader(reader);

            // Read metadata
            var metadataBytes = reader.ReadBytes(metadataLength);
            var fileInfo = JsonSerializer.Deserialize(metadataBytes, GhostJsonContext.Default.GhostFileInfo);

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
    /// Validates a ghost file from a byte array.
    /// </summary>
    /// <param name="data">The ghost file data.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static GhostFileInfo? Validate(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Validate(stream);
    }

    /// <summary>
    /// Validates a ghost file from disk.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns>The file info with validation result, or null if completely invalid.</returns>
    public static GhostFileInfo? ValidateFile(string path)
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
    /// Checks if a stream contains a valid .keghost file header.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <returns>True if the stream appears to be a .keghost file.</returns>
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
    /// Checks if a byte array contains a valid .keghost file header.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data appears to be a .keghost file.</returns>
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
            throw new InvalidDataException("Invalid ghost file: missing or incorrect magic bytes.");
        }

        // Read version
        var version = reader.ReadUInt16();
        if (version > CurrentVersion)
        {
            throw new InvalidDataException(
                $"Ghost file version {version} is not supported. Maximum supported version is {CurrentVersion}.");
        }

        // Read flags and lengths
        var flags = (FormatOption)reader.ReadUInt16();
        var metadataLength = reader.ReadInt32();
        var dataLength = reader.ReadInt32();

        if (metadataLength <= 0 || dataLength < 0)
        {
            throw new InvalidDataException("Invalid ghost file: invalid metadata or data length.");
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
