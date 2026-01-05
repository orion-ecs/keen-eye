using System.Collections;
using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Represents a computed navigation path with waypoints and metadata.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NavPath"/> contains the result of a pathfinding query,
/// including the sequence of waypoints to follow and information about
/// whether the path is complete or partial.
/// </para>
/// <para>
/// Use <see cref="IsValid"/> to check if the path was successfully computed
/// before attempting to follow it.
/// </para>
/// </remarks>
/// <param name="waypoints">The waypoints comprising the path.</param>
/// <param name="isComplete">
/// Whether the path reaches the destination.
/// False indicates a partial path to the closest reachable point.
/// </param>
/// <param name="totalCost">The total traversal cost of the path.</param>
public sealed class NavPath(NavPoint[] waypoints, bool isComplete, float totalCost) : IReadOnlyList<NavPoint>
{
    private readonly NavPoint[] waypoints = waypoints ?? [];

    /// <summary>
    /// Gets an empty path representing a failed or invalid path query.
    /// </summary>
    public static NavPath Empty { get; } = new([], false, 0f);

    /// <summary>
    /// Gets a value indicating whether this path is valid (has waypoints).
    /// </summary>
    public bool IsValid => waypoints.Length > 0;

    /// <summary>
    /// Gets a value indicating whether this path reaches the intended destination.
    /// </summary>
    /// <remarks>
    /// When false, this is a partial path to the closest reachable point.
    /// </remarks>
    public bool IsComplete { get; } = isComplete;

    /// <summary>
    /// Gets the total traversal cost of this path.
    /// </summary>
    /// <remarks>
    /// Cost is computed based on distance and area type cost multipliers.
    /// </remarks>
    public float TotalCost { get; } = totalCost;

    /// <summary>
    /// Gets the total length of the path in world units.
    /// </summary>
    public float Length
    {
        get
        {
            if (waypoints.Length < 2)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 1; i < waypoints.Length; i++)
            {
                total += waypoints[i - 1].DistanceTo(waypoints[i]);
            }

            return total;
        }
    }

    /// <summary>
    /// Gets the number of waypoints in this path.
    /// </summary>
    public int Count => waypoints.Length;

    /// <summary>
    /// Gets the starting point of the path.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the path is empty.</exception>
    public NavPoint Start => waypoints.Length > 0
        ? waypoints[0]
        : throw new InvalidOperationException("Cannot get start of empty path.");

    /// <summary>
    /// Gets the ending point of the path.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the path is empty.</exception>
    public NavPoint End => waypoints.Length > 0
        ? waypoints[^1]
        : throw new InvalidOperationException("Cannot get end of empty path.");

    /// <summary>
    /// Gets the waypoint at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the waypoint.</param>
    /// <returns>The waypoint at the specified index.</returns>
    public NavPoint this[int index] => waypoints[index];

    /// <summary>
    /// Gets the waypoints as a read-only span.
    /// </summary>
    /// <returns>A span containing all waypoints.</returns>
    public ReadOnlySpan<NavPoint> AsSpan() => waypoints;

    /// <summary>
    /// Samples a position along the path at the specified distance from the start.
    /// </summary>
    /// <param name="distance">The distance from the start of the path.</param>
    /// <returns>
    /// The interpolated position at the specified distance.
    /// Returns the end position if distance exceeds path length.
    /// </returns>
    public Vector3 SamplePosition(float distance)
    {
        if (waypoints.Length == 0)
        {
            return Vector3.Zero;
        }

        if (waypoints.Length == 1 || distance <= 0)
        {
            return waypoints[0].Position;
        }

        float accumulated = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            float segmentLength = waypoints[i - 1].DistanceTo(waypoints[i]);
            if (accumulated + segmentLength >= distance)
            {
                float t = (distance - accumulated) / segmentLength;
                return Vector3.Lerp(waypoints[i - 1].Position, waypoints[i].Position, t);
            }

            accumulated += segmentLength;
        }

        return waypoints[^1].Position;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the waypoints.
    /// </summary>
    /// <returns>An enumerator for the waypoints.</returns>
    public IEnumerator<NavPoint> GetEnumerator()
    {
        for (int i = 0; i < waypoints.Length; i++)
        {
            yield return waypoints[i];
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
