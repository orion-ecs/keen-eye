using System.Numerics;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Represents a single frame of ghost transform data.
/// </summary>
/// <remarks>
/// <para>
/// A ghost frame contains the minimal transform data needed to display a ghost
/// at a specific point in time. Unlike full replay frames which contain all
/// entity events and component data, ghost frames only store position, rotation,
/// and optional distance for efficient storage and playback.
/// </para>
/// <para>
/// Ghost frames are stored contiguously in <see cref="GhostData.Frames"/> for
/// cache-friendly iteration during playback.
/// </para>
/// </remarks>
/// <param name="Position">The world position of the entity at this frame.</param>
/// <param name="Rotation">The rotation of the entity at this frame.</param>
/// <param name="ElapsedTime">The elapsed time since the start of recording.</param>
public readonly record struct GhostFrame(
    Vector3 Position,
    Quaternion Rotation,
    TimeSpan ElapsedTime)
{
    /// <summary>
    /// Gets or sets the distance traveled along a track (for distance-synced playback).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is only meaningful when using <see cref="GhostSyncMode.DistanceSynced"/>.
    /// It represents the cumulative distance the entity has traveled from the start.
    /// </para>
    /// <para>
    /// Default value is 0, indicating either the start of the track or that
    /// distance tracking is not enabled.
    /// </para>
    /// </remarks>
    public float Distance { get; init; }

    /// <summary>
    /// Gets or sets the scale of the entity at this frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scale is optional and defaults to <see cref="Vector3.One"/> (uniform scale of 1).
    /// Most ghosts don't need scale changes, so this defaults to identity.
    /// </para>
    /// <para>
    /// If your ghost needs scale animation (e.g., for visual effects),
    /// set this value during extraction.
    /// </para>
    /// </remarks>
    public Vector3 Scale { get; init; } = Vector3.One;

    /// <summary>
    /// Creates a ghost frame from a Transform3D component.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation.</param>
    /// <param name="scale">The scale.</param>
    /// <param name="elapsedTime">The elapsed time since recording start.</param>
    /// <param name="distance">The optional distance traveled.</param>
    /// <returns>A new ghost frame with the specified transform data.</returns>
    public static GhostFrame Create(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        TimeSpan elapsedTime,
        float distance = 0f)
        => new(position, rotation, elapsedTime)
        {
            Scale = scale,
            Distance = distance
        };

    /// <summary>
    /// Interpolates between two ghost frames.
    /// </summary>
    /// <param name="a">The first frame.</param>
    /// <param name="b">The second frame.</param>
    /// <param name="t">The interpolation factor (0.0 to 1.0).</param>
    /// <returns>A new frame with interpolated values.</returns>
    /// <remarks>
    /// <para>
    /// Position, scale, and distance are linearly interpolated.
    /// Rotation is spherically interpolated (Slerp) for smooth orientation blending.
    /// Elapsed time is linearly interpolated.
    /// </para>
    /// </remarks>
    public static GhostFrame Lerp(in GhostFrame a, in GhostFrame b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        return new GhostFrame(
            Position: Vector3.Lerp(a.Position, b.Position, t),
            Rotation: Quaternion.Slerp(a.Rotation, b.Rotation, t),
            ElapsedTime: TimeSpan.FromTicks((long)(a.ElapsedTime.Ticks + (b.ElapsedTime.Ticks - a.ElapsedTime.Ticks) * t)))
        {
            Scale = Vector3.Lerp(a.Scale, b.Scale, t),
            Distance = a.Distance + (b.Distance - a.Distance) * t
        };
    }
}
