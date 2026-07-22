using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Detour.Crowd;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Manages crowd simulation for entities using DotRecast's <c>DtCrowd</c>.
/// </summary>
/// <remarks>
/// <para>
/// This manager maps entities to crowd agents, forwards move targets, advances
/// the simulation each tick, and exposes the simulated position and velocity of
/// each agent. It is owned by <see cref="DotRecastProvider"/> and accessed
/// through the <see cref="ICrowdNavigationProvider"/> seam.
/// </para>
/// <para>
/// When the navigation mesh is replaced (for example after a rebuild), the
/// crowd is recreated on the new mesh and all registered agents are re-added at
/// their current positions with their previous move targets restored.
/// </para>
/// <para>
/// This class is not thread-safe; it is intended to be driven from the world
/// update on a single thread.
/// </para>
/// </remarks>
public sealed class DotRecastCrowdManager : IDisposable
{
    /// <summary>
    /// Default collision query range multiplier applied to the agent radius
    /// when <see cref="CrowdAgent.CollisionQueryRange"/> is zero.
    /// </summary>
    private const float CollisionQueryRangeFactor = 12f;

    /// <summary>
    /// Path optimization range multiplier applied to the agent radius,
    /// matching the Recast demo's recommended value.
    /// </summary>
    private const float PathOptimizationRangeFactor = 30f;

    private sealed class AgentEntry
    {
        public required DtCrowdAgent Agent { get; set; }
        public required DtCrowdAgentParams Parameters { get; init; }
        public Vector3? Target { get; set; }
    }

    private readonly Dictionary<Entity, AgentEntry> agents = [];
    private readonly float maxAgentRadius;
    private DtCrowd crowd;
    private bool disposed;

    /// <summary>
    /// Creates a crowd manager for the given navigation mesh.
    /// </summary>
    /// <param name="navMeshData">The navigation mesh to simulate on.</param>
    /// <param name="maxAgentRadius">
    /// The maximum radius of any agent that will be added to the crowd. Used to
    /// size the proximity grid and agent placement queries.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when navMeshData is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxAgentRadius is not positive.</exception>
    public DotRecastCrowdManager(NavMeshData navMeshData, float maxAgentRadius)
    {
        ArgumentNullException.ThrowIfNull(navMeshData);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAgentRadius);

        this.maxAgentRadius = maxAgentRadius;
        crowd = new DtCrowd(new DtCrowdConfig(maxAgentRadius), navMeshData.InternalNavMesh);
    }

    /// <summary>
    /// Gets the number of agents currently registered in the crowd.
    /// </summary>
    public int AgentCount => agents.Count;

    /// <summary>
    /// Replaces the navigation mesh, re-adding all registered agents.
    /// </summary>
    /// <param name="navMeshData">The new navigation mesh.</param>
    /// <exception cref="ArgumentNullException">Thrown when navMeshData is null.</exception>
    /// <remarks>
    /// Agents keep their current simulated positions and move targets. Agents
    /// whose position no longer lies on the new mesh remain registered but stay
    /// inactive until the mesh covers them again.
    /// </remarks>
    public void SetNavMesh(NavMeshData navMeshData)
    {
        ArgumentNullException.ThrowIfNull(navMeshData);
        ThrowIfDisposed();

        // Recreate the crowd rather than swapping the mesh in place: existing
        // agent corridors hold polygon references into the old mesh.
        crowd = new DtCrowd(new DtCrowdConfig(maxAgentRadius), navMeshData.InternalNavMesh);

        foreach (var entry in agents.Values)
        {
            var position = entry.Agent.npos;
            entry.Agent = crowd.AddAgent(position, entry.Parameters);

            if (entry.Target is { } target)
            {
                RequestMoveTargetCore(entry, target);
            }
        }
    }

    /// <summary>
    /// Registers an entity as a crowd agent at the given position.
    /// </summary>
    /// <param name="entity">The entity to register.</param>
    /// <param name="position">The agent's current world position.</param>
    /// <param name="agent">The agent's navigation parameters.</param>
    /// <param name="crowdAgent">The agent's crowd avoidance parameters.</param>
    /// <returns>
    /// True if the agent was added (or was already registered); false if the
    /// position could not be placed on the navigation mesh.
    /// </returns>
    public bool TryAddAgent(Entity entity, Vector3 position, in NavMeshAgent agent, in CrowdAgent crowdAgent)
    {
        ThrowIfDisposed();

        if (agents.ContainsKey(entity))
        {
            return true;
        }

        var parameters = BuildAgentParams(in agent, in crowdAgent);
        var dtAgent = crowd.AddAgent(ToRcVec3f(position), parameters);

        if (dtAgent.state == DtCrowdAgentState.DT_CROWDAGENT_STATE_INVALID)
        {
            crowd.RemoveAgent(dtAgent);
            return false;
        }

        agents[entity] = new AgentEntry { Agent = dtAgent, Parameters = parameters };
        return true;
    }

    /// <summary>
    /// Removes an entity from the crowd simulation.
    /// </summary>
    /// <param name="entity">The entity to remove. Unknown entities are ignored.</param>
    public void RemoveAgent(Entity entity)
    {
        if (agents.Remove(entity, out var entry))
        {
            crowd.RemoveAgent(entry.Agent);
        }
    }

    /// <summary>
    /// Requests that a crowd agent move toward the given target position.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <param name="target">The destination in world space.</param>
    /// <returns>
    /// True if the request was accepted; false if the entity is not registered
    /// or the target could not be mapped to the navigation mesh.
    /// </returns>
    public bool RequestMoveTarget(Entity entity, Vector3 target)
    {
        ThrowIfDisposed();

        if (!agents.TryGetValue(entity, out var entry))
        {
            return false;
        }

        return RequestMoveTargetCore(entry, target);
    }

    /// <summary>
    /// Cancels a crowd agent's current move target, letting it come to rest.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <returns>True if the agent had its target reset; false if not registered.</returns>
    public bool ResetMoveTarget(Entity entity)
    {
        ThrowIfDisposed();

        if (!agents.TryGetValue(entity, out var entry))
        {
            return false;
        }

        entry.Target = null;
        return crowd.ResetMoveTarget(entry.Agent);
    }

    /// <summary>
    /// Advances the crowd simulation by one step.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update, in seconds.</param>
    public void Update(float deltaTime)
    {
        ThrowIfDisposed();
        crowd.Update(deltaTime, null);
    }

    /// <summary>
    /// Gets the simulated state of a registered crowd agent.
    /// </summary>
    /// <param name="entity">The registered crowd agent entity.</param>
    /// <param name="state">The agent's simulated position and velocities.</param>
    /// <returns>True if the entity is registered in the crowd.</returns>
    public bool TryGetAgentState(Entity entity, out CrowdAgentState state)
    {
        if (!agents.TryGetValue(entity, out var entry))
        {
            state = default;
            return false;
        }

        state = new CrowdAgentState(
            ToVector3(entry.Agent.npos),
            ToVector3(entry.Agent.vel),
            ToVector3(entry.Agent.dvel));
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var entry in agents.Values)
        {
            crowd.RemoveAgent(entry.Agent);
        }

        agents.Clear();
        disposed = true;
    }

    private bool RequestMoveTargetCore(AgentEntry entry, Vector3 target)
    {
        var query = crowd.GetNavMeshQuery();
        var status = query.FindNearestPoly(
            ToRcVec3f(target),
            crowd.GetQueryExtents(),
            crowd.GetFilter(entry.Parameters.queryFilterType),
            out var polyRef,
            out var nearest,
            out _);

        if (status.Failed() || polyRef == 0)
        {
            return false;
        }

        if (!crowd.RequestMoveTarget(entry.Agent, polyRef, nearest))
        {
            return false;
        }

        entry.Target = target;
        return true;
    }

    private static DtCrowdAgentParams BuildAgentParams(in NavMeshAgent agent, in CrowdAgent crowdAgent)
    {
        float radius = crowdAgent.Radius > 0f ? crowdAgent.Radius : agent.Settings.Radius;
        float height = crowdAgent.Height > 0f ? crowdAgent.Height : agent.Settings.Height;
        float maxAcceleration = crowdAgent.MaxAcceleration > 0f ? crowdAgent.MaxAcceleration : agent.Acceleration;
        float maxSpeed = crowdAgent.MaxSpeed > 0f ? crowdAgent.MaxSpeed : agent.Speed;
        float collisionQueryRange = crowdAgent.CollisionQueryRange > 0f
            ? crowdAgent.CollisionQueryRange
            : radius * CollisionQueryRangeFactor;

        int updateFlags = DtCrowdAgentUpdateFlags.DT_CROWD_ANTICIPATE_TURNS
            | DtCrowdAgentUpdateFlags.DT_CROWD_OBSTACLE_AVOIDANCE
            | DtCrowdAgentUpdateFlags.DT_CROWD_OPTIMIZE_VIS
            | DtCrowdAgentUpdateFlags.DT_CROWD_OPTIMIZE_TOPO;

        if (crowdAgent.SeparationWeight > 0f)
        {
            updateFlags |= DtCrowdAgentUpdateFlags.DT_CROWD_SEPARATION;
        }

        return new DtCrowdAgentParams
        {
            radius = radius,
            height = height,
            maxAcceleration = maxAcceleration,
            maxSpeed = maxSpeed,
            collisionQueryRange = collisionQueryRange,
            pathOptimizationRange = radius * PathOptimizationRangeFactor,
            separationWeight = crowdAgent.SeparationWeight,
            updateFlags = updateFlags,
            obstacleAvoidanceType = Math.Clamp(crowdAgent.AvoidanceQuality, 0, 3),
            queryFilterType = 0
        };
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private static Vector3 ToVector3(RcVec3f v) => new(v.X, v.Y, v.Z);

    private static RcVec3f ToRcVec3f(Vector3 v) => new(v.X, v.Y, v.Z);
}
