// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Handles serialization and deserialization of navigation mesh files (.kenavmesh).
/// </summary>
/// <remarks>
/// <para>
/// The .kenavmesh format consists of:
/// - A JSON metadata section containing bake configuration
/// - Binary navmesh data from <see cref="NavMeshData"/>
/// </para>
/// <para>
/// This format allows loading navmeshes at runtime while preserving
/// the configuration used to generate them for re-baking.
/// </para>
/// </remarks>
public static class NavMeshSerializer
{
    /// <summary>
    /// The file extension for navmesh files.
    /// </summary>
    public const string FileExtension = ".kenavmesh";

    /// <summary>
    /// The current file format version.
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// Magic bytes to identify the file format.
    /// </summary>
    private static readonly byte[] MagicBytes = "KNAV"u8.ToArray();

    /// <summary>
    /// Saves a navmesh to a file.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    /// <param name="navMesh">The navmesh data to save.</param>
    /// <param name="config">The bake configuration used to generate the navmesh.</param>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    /// <exception cref="IOException">Thrown when file operations fail.</exception>
    public static void Save(string path, NavMeshData navMesh, NavMeshBakeConfig config)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(navMesh);
        ArgumentNullException.ThrowIfNull(config);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        // Write magic bytes
        writer.Write(MagicBytes);

        // Write version
        writer.Write(FormatVersion);

        // Write metadata as JSON
        var metadata = new NavMeshFileMetadata
        {
            AgentTypeName = config.AgentTypeName,
            AgentRadius = config.AgentRadius,
            AgentHeight = config.AgentHeight,
            MaxSlopeAngle = config.MaxSlopeAngle,
            MaxClimbHeight = config.MaxClimbHeight,
            CellSize = config.CellSize,
            CellHeight = config.CellHeight,
            UseTiles = config.UseTiles,
            TileSize = config.TileSize,
            PolygonCount = navMesh.PolygonCount,
            VertexCount = navMesh.VertexCount,
            BakedAt = DateTime.UtcNow
        };

        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        writer.Write(metadataJson);

        // Write navmesh binary data
        var navMeshData = navMesh.Serialize();
        writer.Write(navMeshData.Length);
        writer.Write(navMeshData);
    }

    /// <summary>
    /// Loads a navmesh from a file.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>The loaded navmesh result containing data and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file format is invalid.</exception>
    public static NavMeshLoadResult Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"NavMesh file not found: {path}", path);
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        // Read and verify magic bytes
        var magic = reader.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException($"Invalid navmesh file format: {path}");
        }

        // Read version
        int version = reader.ReadInt32();
        if (version > FormatVersion)
        {
            throw new InvalidDataException($"Unsupported navmesh format version: {version}");
        }

        // Read metadata
        var metadataJson = reader.ReadString();
        var metadata = JsonSerializer.Deserialize<NavMeshFileMetadata>(metadataJson, JsonOptions)
            ?? throw new InvalidDataException("Failed to parse navmesh metadata");

        // Read navmesh data
        int dataLength = reader.ReadInt32();
        var navMeshData = reader.ReadBytes(dataLength);
        var navMesh = NavMeshData.Deserialize(navMeshData);

        return new NavMeshLoadResult
        {
            NavMesh = navMesh,
            Metadata = metadata,
            FilePath = path
        };
    }

    /// <summary>
    /// Tries to load a navmesh from a file.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <param name="result">The loaded navmesh result if successful.</param>
    /// <returns>True if loading succeeded, false otherwise.</returns>
    public static bool TryLoad(string path, out NavMeshLoadResult? result)
    {
        result = null;

        try
        {
            result = Load(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads only the metadata from a navmesh file without loading the full mesh.
    /// </summary>
    /// <param name="path">The file path to read from.</param>
    /// <returns>The navmesh metadata.</returns>
    public static NavMeshFileMetadata ReadMetadata(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"NavMesh file not found: {path}", path);
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        // Read and verify magic bytes
        var magic = reader.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(MagicBytes))
        {
            throw new InvalidDataException($"Invalid navmesh file format: {path}");
        }

        // Read version
        int version = reader.ReadInt32();
        if (version > FormatVersion)
        {
            throw new InvalidDataException($"Unsupported navmesh format version: {version}");
        }

        // Read metadata only
        var metadataJson = reader.ReadString();
        return JsonSerializer.Deserialize<NavMeshFileMetadata>(metadataJson, JsonOptions)
            ?? throw new InvalidDataException("Failed to parse navmesh metadata");
    }

    /// <summary>
    /// Checks if a file is a valid navmesh file.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file is a valid navmesh file.</returns>
    public static bool IsValidNavMeshFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadBytes(4);
            return magic.AsSpan().SequenceEqual(MagicBytes);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all navmesh files in a directory.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>The paths to found navmesh files.</returns>
    public static IEnumerable<string> FindNavMeshFiles(string directory, bool recursive = true)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(directory, $"*{FileExtension}", searchOption))
        {
            if (IsValidNavMeshFile(file))
            {
                yield return file;
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Metadata stored in a navmesh file.
/// </summary>
public sealed class NavMeshFileMetadata
{
    /// <summary>
    /// Gets or sets the agent type name.
    /// </summary>
    public string AgentTypeName { get; set; } = "Humanoid";

    /// <summary>
    /// Gets or sets the agent radius used for baking.
    /// </summary>
    public float AgentRadius { get; set; }

    /// <summary>
    /// Gets or sets the agent height used for baking.
    /// </summary>
    public float AgentHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum slope angle.
    /// </summary>
    public float MaxSlopeAngle { get; set; }

    /// <summary>
    /// Gets or sets the maximum climb height.
    /// </summary>
    public float MaxClimbHeight { get; set; }

    /// <summary>
    /// Gets or sets the cell size used for voxelization.
    /// </summary>
    public float CellSize { get; set; }

    /// <summary>
    /// Gets or sets the cell height used for voxelization.
    /// </summary>
    public float CellHeight { get; set; }

    /// <summary>
    /// Gets or sets whether tiled baking was used.
    /// </summary>
    public bool UseTiles { get; set; }

    /// <summary>
    /// Gets or sets the tile size.
    /// </summary>
    public int TileSize { get; set; }

    /// <summary>
    /// Gets or sets the number of polygons in the navmesh.
    /// </summary>
    public int PolygonCount { get; set; }

    /// <summary>
    /// Gets or sets the number of vertices in the navmesh.
    /// </summary>
    public int VertexCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the navmesh was baked.
    /// </summary>
    public DateTime BakedAt { get; set; }
}

/// <summary>
/// Result of loading a navmesh file.
/// </summary>
public sealed class NavMeshLoadResult
{
    /// <summary>
    /// Gets the loaded navmesh data.
    /// </summary>
    public required NavMeshData NavMesh { get; init; }

    /// <summary>
    /// Gets the file metadata.
    /// </summary>
    public required NavMeshFileMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the path the navmesh was loaded from.
    /// </summary>
    public required string FilePath { get; init; }
}
