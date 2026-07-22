using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Events;

namespace KeenEyes.Navigation.Systems;

/// <summary>
/// System that steers crowd-simulated agents using the provider's crowd simulation.
/// </summary>
/// <remarks>
/// <para>
/// This system processes entities with <see cref="NavMeshAgent"/>,
/// <see cref="CrowdAgent"/>, and <see cref="Transform3D"/> components. Agents
/// are registered with the crowd, their destinations forwarded as move targets,
/// and after advancing the simulation once per frame their simulated positions
/// and velocities are written back to the components.
/// </para>
/// <para>
/// If the active navigation provider does not implement
/// <see cref="ICrowdNavigationProvider"/>, this system is a no-op and crowd
/// agents fall back to plain waypoint steering in <see cref="NavMeshAgentSystem"/>.
/// </para>
/// </remarks>
internal sealed class CrowdSteeringSystem : SystemBase
{
    /// <summary>
    /// The endpoints and area type of an off-mesh traversal in progress,
    /// captured when the traversal starts so the completion event reports the
    /// same values.
    /// </summary>
    private readonly record struct ActiveTraversal(Vector3 Start, Vector3 End, NavAreaType AreaType);

    private readonly Dictionary<Entity, Vector3> requestedTargets = [];
    private readonly Dictionary<Entity, ActiveTraversal> activeTraversals = [];
    private readonly List<Entity> staleEntities = [];
    private ICrowdNavigationProvider? crowdProvider;
    private NavigationConfig? config;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension(out NavigationContext? ctx) || ctx is null)
        {
            throw new InvalidOperationException("CrowdSteeringSystem requires NavigationContext extension.");
        }

        config = ctx.Config;

        // Crowd support is optional; without it this system does nothing and
        // crowd agents degrade to plain waypoint steering.
        crowdProvider = ctx.Provider as ICrowdNavigationProvider;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (crowdProvider is not { IsReady: true })
        {
            return;
        }

        RegisterAgentsAndTargets();

        if (crowdProvider.CrowdAgentCount == 0)
        {
            return;
        }

        crowdProvider.UpdateCrowd(deltaTime);

        WriteBackSimulationResults();
        PruneStaleTargets();
    }

    private void RegisterAgentsAndTargets()
    {
        foreach (var entity in World.Query<NavMeshAgent, CrowdAgent, Transform3D>())
        {
            ref var agent = ref World.Get<NavMeshAgent>(entity);
            ref readonly var crowdAgent = ref World.Get<CrowdAgent>(entity);
            ref readonly var transform = ref World.Get<Transform3D>(entity);

            // Register with the crowd on first sight
            if (!crowdProvider!.TryGetCrowdAgentState(entity, out _))
            {
                if (!crowdProvider.TryAddCrowdAgent(entity, transform.Position, in agent, in crowdAgent))
                {
                    continue;
                }

                agent.IsOnNavMesh = true;
            }

            if (agent.IsStopped)
            {
                // Cancel the crowd move target once when the agent stops
                if (requestedTargets.Remove(entity))
                {
                    crowdProvider.ResetCrowdMoveTarget(entity);
                }

                continue;
            }

            // Forward the destination when it changes
            bool destinationChanged = !requestedTargets.TryGetValue(entity, out var target) ||
                Vector3.DistanceSquared(target, agent.Destination) > 1e-6f;

            if (destinationChanged && crowdProvider.RequestCrowdMoveTarget(entity, agent.Destination))
            {
                requestedTargets[entity] = agent.Destination;
            }
        }
    }

    private void WriteBackSimulationResults()
    {
        foreach (var entity in World.Query<NavMeshAgent, CrowdAgent, Transform3D>())
        {
            ref var agent = ref World.Get<NavMeshAgent>(entity);

            if (!crowdProvider!.TryGetCrowdAgentState(entity, out var state))
            {
                continue;
            }

            PublishTraversalTransitions(entity, in state);

            if (agent.IsStopped)
            {
                continue;
            }

            ref var transform = ref World.Get<Transform3D>(entity);
            transform.Position = state.Position;
            agent.DesiredVelocity = state.Velocity;
            agent.SteeringTarget = state.Position + state.DesiredVelocity;
            agent.RemainingDistance = Vector3.Distance(state.Position, agent.Destination);

            if (agent.RemainingDistance <= agent.StoppingDistance)
            {
                CompleteNavigation(entity, ref agent);
            }
        }
    }

    private void CompleteNavigation(Entity entity, ref NavMeshAgent agent)
    {
        agent.HasPath = false;
        agent.PathPending = false;
        agent.IsStopped = true;
        agent.DesiredVelocity = Vector3.Zero;
        agent.RemainingDistance = 0f;

        requestedTargets.Remove(entity);
        crowdProvider!.ResetCrowdMoveTarget(entity);
    }

    /// <summary>
    /// Sends <see cref="OffMeshLinkTraversalStarted"/> and
    /// <see cref="OffMeshLinkTraversalCompleted"/> events when the crowd
    /// simulation moves an agent onto or off an off-mesh connection, mirroring
    /// the events plain agents receive from <see cref="NavMeshAgentSystem"/>.
    /// </summary>
    private void PublishTraversalTransitions(Entity entity, in CrowdAgentState state)
    {
        bool wasTraversing = activeTraversals.TryGetValue(entity, out var traversal);

        if (state.IsTraversingOffMeshLink && !wasTraversing)
        {
            var areaType = ResolveLinkAreaType(state.OffMeshLinkStart, state.OffMeshLinkEnd);
            activeTraversals[entity] = new ActiveTraversal(state.OffMeshLinkStart, state.OffMeshLinkEnd, areaType);
            World.Send(new OffMeshLinkTraversalStarted(entity, state.OffMeshLinkStart, state.OffMeshLinkEnd, areaType));
        }
        else if (!state.IsTraversingOffMeshLink && wasTraversing)
        {
            activeTraversals.Remove(entity);
            World.Send(new OffMeshLinkTraversalCompleted(entity, traversal.Start, traversal.End, traversal.AreaType));
        }
    }

    /// <summary>
    /// Resolves the area type for a traversal by matching the traversal
    /// endpoints against <see cref="OffMeshLink"/> components in the world,
    /// falling back to <see cref="NavAreaType.OffMeshLink"/> when the mesh was
    /// baked from definitions that are not present in this world.
    /// </summary>
    private NavAreaType ResolveLinkAreaType(Vector3 start, Vector3 end)
    {
        foreach (var linkEntity in World.Query<OffMeshLink>())
        {
            ref readonly var link = ref World.Get<OffMeshLink>(linkEntity);

            // Link endpoints are snapped onto the mesh during the build, so
            // matching uses a tolerance derived from each link's radius.
            float tolerance = link.Radius + config!.WaypointReachDistance;
            float toleranceSq = tolerance * tolerance;

            bool forward =
                Vector3.DistanceSquared(link.Start, start) <= toleranceSq &&
                Vector3.DistanceSquared(link.End, end) <= toleranceSq;
            bool reverse = link.Bidirectional &&
                Vector3.DistanceSquared(link.End, start) <= toleranceSq &&
                Vector3.DistanceSquared(link.Start, end) <= toleranceSq;

            if (forward || reverse)
            {
                return link.AreaType;
            }
        }

        return NavAreaType.OffMeshLink;
    }

    private void PruneStaleTargets()
    {
        // Entities removed from the crowd (destroyed or component removed)
        // must not leak requested-target or active-traversal entries.
        foreach (var entity in requestedTargets.Keys)
        {
            if (!crowdProvider!.TryGetCrowdAgentState(entity, out _))
            {
                staleEntities.Add(entity);
            }
        }

        foreach (var entity in staleEntities)
        {
            requestedTargets.Remove(entity);
        }

        staleEntities.Clear();

        foreach (var entity in activeTraversals.Keys)
        {
            if (!crowdProvider!.TryGetCrowdAgentState(entity, out _))
            {
                staleEntities.Add(entity);
            }
        }

        foreach (var entity in staleEntities)
        {
            activeTraversals.Remove(entity);
        }

        staleEntities.Clear();
    }
}
