using System.Numerics;
using KeenEyes.Network.Components;

namespace KeenEyes.Network;

/// <summary>
/// Interest manager that filters replication by distance using an internal
/// uniform spatial grid.
/// </summary>
/// <remarks>
/// <para>
/// An entity is relevant to a client when it lies within
/// <see cref="ViewDistance"/> of any of the client's viewpoints. Entities for
/// which <see cref="PositionProvider"/> returns <see langword="null"/> have no
/// position and are treated as globally relevant (for example match state or
/// score entities).
/// </para>
/// <para>
/// <b>Viewpoint rule:</b> a client's viewpoints are the positions of the
/// entities it owns, as resolved by <see cref="PositionProvider"/>. When
/// <see cref="ViewpointProvider"/> is set and returns a non-null position for
/// a client, that position replaces the owned-entity viewpoints entirely
/// (useful for spectators or free-flying cameras). A client with no positioned
/// owned entities and no override only receives globally relevant entities and
/// its own entities (the server always replicates a client's own entities to it).
/// </para>
/// <para>
/// The grid buckets entity positions into cubic cells of <see cref="CellSize"/>
/// units. During a relevance update, occupied cells outside the cell-level
/// radius of every viewpoint are rejected wholesale; entities in the remaining
/// cells are checked against the exact <see cref="ViewDistance"/>.
/// </para>
/// </remarks>
public sealed class SpatialInterestManager : IInterestManager
{
    private readonly Dictionary<(int X, int Y, int Z), List<Entity>> grid = [];
    private readonly Dictionary<Entity, Vector3> positions = [];
    private readonly HashSet<Entity> globallyRelevant = [];
    private readonly Dictionary<int, List<Vector3>> ownedViewpoints = [];
    private readonly Dictionary<int, HashSet<Entity>> relevantByClient = [];

    /// <summary>
    /// Gets the edge length of a spatial grid cell, in world units.
    /// </summary>
    /// <remarks>
    /// Choose a value in the same order of magnitude as <see cref="ViewDistance"/>
    /// (for example a quarter to a half of it) so cell-level rejection stays coarse
    /// but effective. Must be positive.
    /// </remarks>
    public float CellSize { get; init; } = 100f;

    /// <summary>
    /// Gets the maximum distance from a client viewpoint at which entities are
    /// replicated, in world units.
    /// </summary>
    /// <remarks>Must be positive.</remarks>
    public float ViewDistance { get; init; } = 500f;

    /// <summary>
    /// Gets how often per-client relevance sets are recomputed, in updates per second.
    /// </summary>
    /// <remarks>
    /// Values less than or equal to zero recompute on every network tick.
    /// </remarks>
    public float UpdateFrequencyHz { get; init; } = 10f;

    /// <summary>
    /// Gets the callback that resolves an entity's world position.
    /// </summary>
    /// <remarks>
    /// Return <see langword="null"/> for entities without a spatial position;
    /// such entities are treated as globally relevant. The networking layer has
    /// no dependency on any particular transform component, so games supply
    /// this mapping explicitly.
    /// </remarks>
    public required Func<IWorld, Entity, Vector3?> PositionProvider { get; init; }

    /// <summary>
    /// Gets the optional per-client viewpoint override.
    /// </summary>
    /// <remarks>
    /// When set and returning a non-null position for a client, that position
    /// is used as the client's sole viewpoint instead of the positions of the
    /// entities it owns. Return <see langword="null"/> to fall back to the
    /// owned-entity rule for that client.
    /// </remarks>
    public Func<IWorld, int, Vector3?>? ViewpointProvider { get; init; }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CellSize"/> or <see cref="ViewDistance"/> is not positive.
    /// </exception>
    public void BeginUpdate(IWorld world, ReadOnlySpan<int> clientIds)
    {
        if (CellSize <= 0f)
        {
            throw new InvalidOperationException("SpatialInterestManager.CellSize must be positive.");
        }

        if (ViewDistance <= 0f)
        {
            throw new InvalidOperationException("SpatialInterestManager.ViewDistance must be positive.");
        }

        grid.Clear();
        positions.Clear();
        globallyRelevant.Clear();
        ownedViewpoints.Clear();
        relevantByClient.Clear();

        // Single pass: bucket positioned entities into the grid and collect
        // owned-entity positions as candidate viewpoints per owning client.
        foreach (var entity in world.Query<NetworkId>())
        {
            var position = PositionProvider(world, entity);
            if (position is null)
            {
                globallyRelevant.Add(entity);
                continue;
            }

            positions[entity] = position.Value;

            var cell = GetCell(position.Value, CellSize);
            if (!grid.TryGetValue(cell, out var cellEntities))
            {
                cellEntities = [];
                grid[cell] = cellEntities;
            }

            cellEntities.Add(entity);

            if (world.Has<NetworkOwner>(entity))
            {
                var ownerId = world.Get<NetworkOwner>(entity).ClientId;
                if (ownerId != NetworkOwner.ServerClientId)
                {
                    if (!ownedViewpoints.TryGetValue(ownerId, out var viewpoints))
                    {
                        viewpoints = [];
                        ownedViewpoints[ownerId] = viewpoints;
                    }

                    viewpoints.Add(position.Value);
                }
            }
        }

        foreach (var clientId in clientIds)
        {
            relevantByClient[clientId] = ComputeRelevantSet(ResolveViewpoints(world, clientId));
        }
    }

    /// <inheritdoc/>
    public bool IsRelevant(IWorld world, int clientId, Entity entity)
        => globallyRelevant.Contains(entity)
            || (relevantByClient.TryGetValue(clientId, out var relevant) && relevant.Contains(entity));

    private List<Vector3> ResolveViewpoints(IWorld world, int clientId)
    {
        var overridePosition = ViewpointProvider?.Invoke(world, clientId);
        if (overridePosition is not null)
        {
            return [overridePosition.Value];
        }

        return ownedViewpoints.TryGetValue(clientId, out var viewpoints) ? viewpoints : [];
    }

    private HashSet<Entity> ComputeRelevantSet(List<Vector3> viewpoints)
    {
        var result = new HashSet<Entity>();
        var cellRadius = GetCellRadius(ViewDistance, CellSize);
        var maxDistanceSquared = ViewDistance * ViewDistance;

        foreach (var viewpoint in viewpoints)
        {
            var viewCell = GetCell(viewpoint, CellSize);

            foreach (var (cell, cellEntities) in grid)
            {
                if (!IsWithinCellRadius(cell, viewCell, cellRadius))
                {
                    continue;
                }

                foreach (var entity in cellEntities)
                {
                    if (Vector3.DistanceSquared(positions[entity], viewpoint) <= maxDistanceSquared)
                    {
                        result.Add(entity);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Computes the grid cell containing a position, flooring toward negative infinity.
    /// </summary>
    internal static (int X, int Y, int Z) GetCell(Vector3 position, float cellSize)
        => ((int)MathF.Floor(position.X / cellSize),
            (int)MathF.Floor(position.Y / cellSize),
            (int)MathF.Floor(position.Z / cellSize));

    /// <summary>
    /// Computes the cell-level search radius that conservatively covers a view distance.
    /// </summary>
    internal static int GetCellRadius(float viewDistance, float cellSize)
        => (int)MathF.Ceiling(viewDistance / cellSize);

    /// <summary>
    /// Checks whether a cell lies within a Chebyshev radius of a center cell.
    /// </summary>
    internal static bool IsWithinCellRadius((int X, int Y, int Z) cell, (int X, int Y, int Z) center, int radius)
        => Math.Abs(cell.X - center.X) <= radius
            && Math.Abs(cell.Y - center.Y) <= radius
            && Math.Abs(cell.Z - center.Z) <= radius;
}
