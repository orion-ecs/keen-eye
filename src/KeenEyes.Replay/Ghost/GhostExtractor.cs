using System.Numerics;
using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Extracts ghost data from full replay recordings.
/// </summary>
/// <remarks>
/// <para>
/// The ghost extractor traverses replay snapshots to extract transform data
/// for a specific entity. This produces lightweight ghost data that contains
/// only position and rotation information, significantly smaller than the
/// full replay.
/// </para>
/// <para>
/// The extractor looks for Transform3D components on entities, extracting
/// position, rotation, and scale values for each snapshot in the replay.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var extractor = new GhostExtractor();
///
/// // Extract ghost for the player entity
/// var ghostData = extractor.ExtractGhost(replayData, "Player");
///
/// if (ghostData is not null)
/// {
///     // Save to file
///     GhostFileFormat.WriteToFile("player_ghost.keghost", ghostData);
/// }
/// </code>
/// </example>
public sealed class GhostExtractor
{
    private const string Transform3DTypeName = "KeenEyes.Common.Transform3D";

    /// <summary>
    /// Gets or sets the minimum interval between ghost frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When extracting from replays with very high frame rates, this interval
    /// can be used to reduce the number of ghost frames, saving storage space.
    /// </para>
    /// <para>
    /// Default is <see cref="TimeSpan.Zero"/>, meaning all available frames
    /// are extracted.
    /// </para>
    /// </remarks>
    public TimeSpan MinFrameInterval { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets whether to calculate distance traveled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, the extractor calculates cumulative distance traveled
    /// based on position changes between frames. This enables distance-synced
    /// playback mode.
    /// </para>
    /// <para>
    /// Default is true.
    /// </para>
    /// </remarks>
    public bool CalculateDistance { get; set; } = true;

    /// <summary>
    /// Extracts ghost data for a named entity from a replay.
    /// </summary>
    /// <param name="replay">The source replay data.</param>
    /// <param name="entityName">The name of the entity to extract.</param>
    /// <returns>
    /// The extracted ghost data, or null if the entity was not found or
    /// has no transform data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replay"/> or <paramref name="entityName"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The extractor looks for the entity in each snapshot and extracts
    /// its Transform3D component data. If the entity is not found in a
    /// snapshot, that frame is skipped.
    /// </para>
    /// <para>
    /// For best results, ensure the replay was recorded with snapshots
    /// at a reasonable interval (e.g., every 0.1 - 1.0 seconds).
    /// </para>
    /// </remarks>
    public GhostData? ExtractGhost(ReplayData replay, string entityName)
    {
        ArgumentNullException.ThrowIfNull(replay);
        ArgumentNullException.ThrowIfNull(entityName);

        if (replay.Snapshots.Count == 0)
        {
            return null;
        }

        var frames = new List<GhostFrame>();
        TimeSpan lastFrameTime = TimeSpan.MinValue;
        float cumulativeDistance = 0f;
        Vector3? lastPosition = null;

        foreach (var snapshotMarker in replay.Snapshots)
        {
            // Skip if we haven't passed the minimum interval
            if (MinFrameInterval > TimeSpan.Zero &&
                snapshotMarker.ElapsedTime - lastFrameTime < MinFrameInterval)
            {
                continue;
            }

            // Find the entity in this snapshot
            var entity = snapshotMarker.Snapshot.Entities
                .FirstOrDefault(e => e.Name == entityName);

            if (entity is null)
            {
                continue;
            }

            // Find the Transform3D component
            var transformComponent = entity.Components
                .FirstOrDefault(c => c.TypeName.StartsWith(Transform3DTypeName, StringComparison.Ordinal));

            if (transformComponent is null || transformComponent.Data is null)
            {
                continue;
            }

            // Parse the transform data
            var transform = ParseTransform3D(transformComponent.Data.Value);
            if (transform is null)
            {
                continue;
            }

            var (position, rotation, scale) = transform.Value;

            // Calculate distance if enabled
            if (CalculateDistance && lastPosition.HasValue)
            {
                cumulativeDistance += Vector3.Distance(lastPosition.Value, position);
            }
            lastPosition = position;

            // Create the ghost frame
            var ghostFrame = new GhostFrame(position, rotation, snapshotMarker.ElapsedTime)
            {
                Scale = scale,
                Distance = cumulativeDistance
            };

            frames.Add(ghostFrame);
            lastFrameTime = snapshotMarker.ElapsedTime;
        }

        if (frames.Count == 0)
        {
            return null;
        }

        return new GhostData
        {
            Name = replay.Name,
            EntityName = entityName,
            RecordingStarted = replay.RecordingStarted,
            Duration = frames[^1].ElapsedTime - frames[0].ElapsedTime,
            FrameCount = frames.Count,
            Frames = frames,
            Metadata = replay.Metadata
        };
    }

    /// <summary>
    /// Extracts ghost data for an entity by ID from a replay.
    /// </summary>
    /// <param name="replay">The source replay data.</param>
    /// <param name="entityId">The ID of the entity to extract.</param>
    /// <returns>
    /// The extracted ghost data, or null if the entity was not found or
    /// has no transform data.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replay"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This overload extracts by entity ID instead of name. Note that entity
    /// IDs may not be stable across different recording sessions.
    /// </para>
    /// </remarks>
    public GhostData? ExtractGhostById(ReplayData replay, int entityId)
    {
        ArgumentNullException.ThrowIfNull(replay);

        if (replay.Snapshots.Count == 0)
        {
            return null;
        }

        var frames = new List<GhostFrame>();
        TimeSpan lastFrameTime = TimeSpan.MinValue;
        float cumulativeDistance = 0f;
        Vector3? lastPosition = null;
        string? entityName = null;

        foreach (var snapshotMarker in replay.Snapshots)
        {
            // Skip if we haven't passed the minimum interval
            if (MinFrameInterval > TimeSpan.Zero &&
                snapshotMarker.ElapsedTime - lastFrameTime < MinFrameInterval)
            {
                continue;
            }

            // Find the entity in this snapshot
            var entity = snapshotMarker.Snapshot.Entities
                .FirstOrDefault(e => e.Id == entityId);

            if (entity is null)
            {
                continue;
            }

            // Capture entity name from first occurrence
            entityName ??= entity.Name;

            // Find the Transform3D component
            var transformComponent = entity.Components
                .FirstOrDefault(c => c.TypeName.StartsWith(Transform3DTypeName, StringComparison.Ordinal));

            if (transformComponent is null || transformComponent.Data is null)
            {
                continue;
            }

            // Parse the transform data
            var transform = ParseTransform3D(transformComponent.Data.Value);
            if (transform is null)
            {
                continue;
            }

            var (position, rotation, scale) = transform.Value;

            // Calculate distance if enabled
            if (CalculateDistance && lastPosition.HasValue)
            {
                cumulativeDistance += Vector3.Distance(lastPosition.Value, position);
            }
            lastPosition = position;

            // Create the ghost frame
            var ghostFrame = new GhostFrame(position, rotation, snapshotMarker.ElapsedTime)
            {
                Scale = scale,
                Distance = cumulativeDistance
            };

            frames.Add(ghostFrame);
            lastFrameTime = snapshotMarker.ElapsedTime;
        }

        if (frames.Count == 0)
        {
            return null;
        }

        return new GhostData
        {
            Name = replay.Name,
            EntityName = entityName,
            RecordingStarted = replay.RecordingStarted,
            Duration = frames[^1].ElapsedTime - frames[0].ElapsedTime,
            FrameCount = frames.Count,
            Frames = frames,
            Metadata = replay.Metadata
        };
    }

    /// <summary>
    /// Extracts ghost data for all named entities from a replay.
    /// </summary>
    /// <param name="replay">The source replay data.</param>
    /// <returns>
    /// A dictionary mapping entity names to their extracted ghost data.
    /// Entities without transform data are not included.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replay"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method extracts ghosts for all named entities that have Transform3D
    /// components. Unnamed entities are skipped.
    /// </para>
    /// </remarks>
    public Dictionary<string, GhostData> ExtractAllGhosts(ReplayData replay)
    {
        ArgumentNullException.ThrowIfNull(replay);

        var result = new Dictionary<string, GhostData>();

        if (replay.Snapshots.Count == 0)
        {
            return result;
        }

        // Find all unique entity names with transforms
        var entityNames = replay.Snapshots
            .SelectMany(s => s.Snapshot.Entities)
            .Where(e => e.Name is not null &&
                        e.Components.Any(c => c.TypeName.StartsWith(Transform3DTypeName, StringComparison.Ordinal)))
            .Select(e => e.Name!)
            .Distinct()
            .ToList();

        // Extract ghost for each entity
        foreach (var name in entityNames)
        {
            var ghost = ExtractGhost(replay, name);
            if (ghost is not null)
            {
                result[name] = ghost;
            }
        }

        return result;
    }

    /// <summary>
    /// Parses Transform3D data from a JSON element.
    /// </summary>
    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale)? ParseTransform3D(JsonElement element)
    {
        try
        {
            Vector3 position = Vector3.Zero;
            Quaternion rotation = Quaternion.Identity;
            Vector3 scale = Vector3.One;

            // Try to get Position property
            if (element.TryGetProperty("position", out var posElement) ||
                element.TryGetProperty("Position", out posElement))
            {
                position = ParseVector3(posElement);
            }

            // Try to get Rotation property
            if (element.TryGetProperty("rotation", out var rotElement) ||
                element.TryGetProperty("Rotation", out rotElement))
            {
                rotation = ParseQuaternion(rotElement);
            }

            // Try to get Scale property
            if (element.TryGetProperty("scale", out var scaleElement) ||
                element.TryGetProperty("Scale", out scaleElement))
            {
                scale = ParseVector3(scaleElement);
            }

            return (position, rotation, scale);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a Vector3 from a JSON element.
    /// </summary>
    private static Vector3 ParseVector3(JsonElement element)
    {
        float x = 0f, y = 0f, z = 0f;

        if (element.TryGetProperty("x", out var xProp) || element.TryGetProperty("X", out xProp))
        {
            x = xProp.GetSingle();
        }

        if (element.TryGetProperty("y", out var yProp) || element.TryGetProperty("Y", out yProp))
        {
            y = yProp.GetSingle();
        }

        if (element.TryGetProperty("z", out var zProp) || element.TryGetProperty("Z", out zProp))
        {
            z = zProp.GetSingle();
        }

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Parses a Quaternion from a JSON element.
    /// </summary>
    private static Quaternion ParseQuaternion(JsonElement element)
    {
        float x = 0f, y = 0f, z = 0f, w = 1f;

        if (element.TryGetProperty("x", out var xProp) || element.TryGetProperty("X", out xProp))
        {
            x = xProp.GetSingle();
        }

        if (element.TryGetProperty("y", out var yProp) || element.TryGetProperty("Y", out yProp))
        {
            y = yProp.GetSingle();
        }

        if (element.TryGetProperty("z", out var zProp) || element.TryGetProperty("Z", out zProp))
        {
            z = zProp.GetSingle();
        }

        if (element.TryGetProperty("w", out var wProp) || element.TryGetProperty("W", out wProp))
        {
            w = wProp.GetSingle();
        }

        return new Quaternion(x, y, z, w);
    }
}
