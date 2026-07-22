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

        return BuildGhost(replay, entity => entity.Name == entityName, entityName);
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

        return BuildGhost(replay, entity => entity.Id == entityId, entityName: null);
    }

    /// <summary>
    /// Builds ghost data for the entity matched by <paramref name="matchEntity"/> across the
    /// replay's reconstructed states.
    /// </summary>
    /// <param name="replay">The source replay data.</param>
    /// <param name="matchEntity">Predicate that selects the target entity in each state.</param>
    /// <param name="entityName">
    /// The known entity name, or <c>null</c> to capture it from the first matching entity
    /// (used by the by-id overload where the name is not known up front).
    /// </param>
    /// <returns>The extracted ghost data, or null when no frames were produced.</returns>
    private GhostData? BuildGhost(ReplayData replay, Func<SerializedEntity, bool> matchEntity, string? entityName)
    {
        var frames = new List<GhostFrame>();
        TimeSpan lastFrameTime = TimeSpan.MinValue;
        float cumulativeDistance = 0f;
        Vector3? lastPosition = null;
        var resolvedName = entityName;

        // Each marker contributes a frame: keyframes are read directly, delta markers are
        // reconstructed by applying the delta chain from the governing keyframe.
        foreach (var (marker, state) in ReconstructStates(replay))
        {
            // Skip if we haven't passed the minimum interval
            if (MinFrameInterval > TimeSpan.Zero &&
                marker.ElapsedTime - lastFrameTime < MinFrameInterval)
            {
                continue;
            }

            // Find the entity in the reconstructed state
            var entity = state.Entities.FirstOrDefault(matchEntity);
            if (entity is null)
            {
                continue;
            }

            // Capture the entity name from the first occurrence when it was not supplied.
            resolvedName ??= entity.Name;

            // Find the Transform3D component
            var transformComponent = entity.Components
                .FirstOrDefault(c => c.TypeName.StartsWith(Transform3DTypeName, StringComparison.Ordinal));

            if (transformComponent is null || transformComponent.Data is null)
            {
                continue;
            }

            // Parse the transform data. Malformed component data yields null and the frame
            // is skipped, so a single corrupt marker does not abort the whole extraction.
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
            var ghostFrame = new GhostFrame(position, rotation, marker.ElapsedTime)
            {
                Scale = scale,
                Distance = cumulativeDistance
            };

            frames.Add(ghostFrame);
            lastFrameTime = marker.ElapsedTime;
        }

        if (frames.Count == 0)
        {
            return null;
        }

        return new GhostData
        {
            Name = replay.Name,
            EntityName = resolvedName,
            RecordingStarted = replay.RecordingStarted,
            Duration = frames[^1].ElapsedTime - frames[0].ElapsedTime,
            FrameCount = frames.Count,
            Frames = frames,
            Metadata = replay.Metadata
        };
    }

    /// <summary>
    /// Reconstructs the full world state at each snapshot marker in recording order.
    /// </summary>
    /// <param name="replay">The source replay data.</param>
    /// <returns>
    /// A sequence of markers paired with the world state at that marker. Keyframes yield
    /// their stored snapshot directly; delta markers yield the state produced by applying
    /// their delta to the running reconstructed state.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A delta marker that precedes the first keyframe cannot be reconstructed and is
    /// skipped. If applying a delta throws (a corrupt or inconsistent marker), that marker
    /// is skipped and the running baseline is discarded: because each delta is relative to
    /// the immediately preceding marker, a broken link makes every subsequent delta
    /// unreconstructable until the next keyframe re-establishes a self-contained baseline.
    /// </para>
    /// </remarks>
    private static IEnumerable<(SnapshotMarker Marker, WorldSnapshot State)> ReconstructStates(ReplayData replay)
    {
        WorldSnapshot? currentState = null;

        foreach (var marker in replay.Snapshots)
        {
            if (marker.Snapshot is not null)
            {
                // Keyframe: a self-contained restore point resets the running state.
                currentState = marker.Snapshot;
            }
            else if (marker.Delta is not null && currentState is not null)
            {
                WorldSnapshot next;
                try
                {
                    next = DeltaSnapshotApplier.Apply(currentState, marker.Delta);
                }
                catch (Exception)
                {
                    // Broad catch: a malformed delta must not abort extraction. Skip this
                    // marker and invalidate the chain until the next keyframe.
                    currentState = null;
                    continue;
                }

                currentState = next;
            }
            else
            {
                // Delta marker with no governing keyframe, or a marker carrying neither a
                // snapshot nor a delta: nothing to reconstruct from.
                continue;
            }

            yield return (marker, currentState);
        }
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

        // Discover candidate entity names from keyframes, which carry full state. Each named
        // entity is then extracted via ExtractGhost, whose reconstruction fills in the
        // frames contributed by the intervening delta markers.
        var entityNames = replay.Snapshots
            .Where(s => s.Snapshot is not null)
            .SelectMany(s => s.Snapshot!.Entities)
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
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            // Malformed transform data (wrong JSON shape, or a value that is not a number).
            // Treat as unreadable so the caller skips the frame rather than throwing.
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
