using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.Navigation.Events;

namespace KeenEyes.Navigation.Systems;

/// <summary>
/// System that moves agents along their computed paths.
/// </summary>
/// <remarks>
/// <para>
/// This system processes all entities with <see cref="NavMeshAgent"/> and
/// <see cref="Transform3D"/> components, moving them toward their destinations
/// along their computed paths.
/// </para>
/// <para>
/// The system:
/// </para>
/// <list type="bullet">
/// <item><description>Computes steering velocities based on path waypoints</description></item>
/// <item><description>Applies acceleration and speed limits</description></item>
/// <item><description>Advances to the next waypoint when close enough</description></item>
/// <item><description>Updates agent state (remaining distance, steering target, etc.)</description></item>
/// </list>
/// </remarks>
internal sealed class NavMeshAgentSystem : SystemBase
{
    private NavigationContext? context;
    private NavigationConfig? config;
    private bool crowdSupported;

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension<NavigationContext>(out var ctx) || ctx is null)
        {
            throw new InvalidOperationException("NavMeshAgentSystem requires NavigationContext extension.");
        }

        context = ctx;
        config = ctx.Config;

        // When the provider supports crowd simulation, entities with a
        // CrowdAgent component are steered by CrowdSteeringSystem instead.
        crowdSupported = ctx.Provider is ICrowdNavigationProvider;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (context == null || config == null)
        {
            return;
        }

        // Process all agents with paths
        foreach (var entity in World.Query<NavMeshAgent, Transform3D>())
        {
            // Crowd-simulated agents are handled by CrowdSteeringSystem
            if (crowdSupported && World.Has<CrowdAgent>(entity))
            {
                continue;
            }

            ref var agent = ref World.Get<NavMeshAgent>(entity);
            ref var transform = ref World.Get<Transform3D>(entity);

            // Skip stopped agents or those without paths
            if (agent.IsStopped || !agent.HasPath || agent.PathPending)
            {
                continue;
            }

            // Get navigation state
            if (!context.TryGetAgentState(entity, out var state) || !state.Path.IsValid)
            {
                continue;
            }

            // Process movement
            ProcessAgentMovement(entity, ref agent, ref transform, ref state, deltaTime);

            // Update state
            context.SetAgentState(entity, state);
        }
    }

    private void ProcessAgentMovement(
        Entity entity,
        ref NavMeshAgent agent,
        ref Transform3D transform,
        ref AgentNavigationState state,
        float deltaTime)
    {
        var path = state.Path;

        // Continue an in-progress off-mesh traversal before anything else
        if (state.IsTraversingOffMeshLink)
        {
            TraverseOffMeshLink(entity, ref agent, ref transform, ref state, deltaTime);
            return;
        }

        // Check if we've reached the end of the path
        if (state.CurrentWaypointIndex >= path.Count)
        {
            CompleteNavigation(ref agent);
            return;
        }

        // Get current waypoint position
        var targetWaypoint = path[state.CurrentWaypointIndex].Position;
        agent.SteeringTarget = targetWaypoint;

        // Calculate distance to waypoint
        var toWaypoint = targetWaypoint - transform.Position;
        float distanceToWaypoint = toWaypoint.Length();

        // Check if we've reached the waypoint
        float reachDistance = config!.WaypointReachDistance;
        if (distanceToWaypoint <= reachDistance)
        {
            // Reaching an off-mesh connection entry switches the agent into
            // link traversal instead of normal waypoint advancement.
            if (IsOffMeshEntry(path, state.CurrentWaypointIndex))
            {
                BeginOffMeshTraversal(entity, ref agent, ref state);
                return;
            }

            state.CurrentWaypointIndex++;

            // Check if this was the last waypoint
            if (state.CurrentWaypointIndex >= path.Count)
            {
                CompleteNavigation(ref agent);
                return;
            }

            // Update target to next waypoint
            targetWaypoint = path[state.CurrentWaypointIndex].Position;
            agent.SteeringTarget = targetWaypoint;
            toWaypoint = targetWaypoint - transform.Position;
            distanceToWaypoint = toWaypoint.Length();
        }

        // Calculate desired velocity
        Vector3 desiredVelocity;
        if (distanceToWaypoint > 0.001f)
        {
            var direction = toWaypoint / distanceToWaypoint;

            // Apply auto-braking near destination
            float speed = agent.Speed;
            if (agent.AutoBraking && state.CurrentWaypointIndex == path.Count - 1)
            {
                float brakingDistance = agent.StoppingDistance * 2f;
                if (distanceToWaypoint < brakingDistance)
                {
                    speed *= distanceToWaypoint / brakingDistance;
                }
            }

            desiredVelocity = direction * speed;
        }
        else
        {
            desiredVelocity = Vector3.Zero;
        }

        // Apply acceleration
        var velocityDiff = desiredVelocity - agent.DesiredVelocity;
        float maxChange = agent.Acceleration * deltaTime;
        if (velocityDiff.LengthSquared() > maxChange * maxChange)
        {
            velocityDiff = Vector3.Normalize(velocityDiff) * maxChange;
        }

        agent.DesiredVelocity += velocityDiff;

        // Move the agent
        var movement = agent.DesiredVelocity * deltaTime;
        transform.Position += movement;
        state.DistanceTraveled += movement.Length();

        // Update remaining distance
        agent.RemainingDistance = CalculateRemainingDistance(
            transform.Position,
            path,
            state.CurrentWaypointIndex);

        // Check if we've arrived at the final destination
        float distanceToFinal = Vector3.Distance(transform.Position, path.End.Position);
        if (distanceToFinal <= agent.StoppingDistance)
        {
            CompleteNavigation(ref agent);
        }
    }

    private static bool IsOffMeshEntry(Abstractions.NavPath path, int waypointIndex)
        => (path[waypointIndex].Properties & NavPointProperties.OffMeshConnection) != 0
            && waypointIndex + 1 < path.Count;

    private void BeginOffMeshTraversal(Entity entity, ref NavMeshAgent agent, ref AgentNavigationState state)
    {
        var entry = state.Path[state.CurrentWaypointIndex];
        var exit = state.Path[state.CurrentWaypointIndex + 1];

        state.IsTraversingOffMeshLink = true;
        state.OffMeshLinkStart = entry.Position;
        state.OffMeshLinkEnd = exit.Position;
        state.OffMeshLinkAreaType = entry.AreaType;
        state.OffMeshLinkProgress = 0f;
        state.OffMeshLinkCostModifier = ResolveCostModifier(entry.Position, exit.Position);

        agent.SteeringTarget = exit.Position;

        World.Send(new OffMeshLinkTraversalStarted(entity, entry.Position, exit.Position, entry.AreaType));
    }

    private void TraverseOffMeshLink(
        Entity entity,
        ref NavMeshAgent agent,
        ref Transform3D transform,
        ref AgentNavigationState state,
        float deltaTime)
    {
        var start = state.OffMeshLinkStart;
        var end = state.OffMeshLinkEnd;
        float length = Vector3.Distance(start, end);

        // Traversal speed is the agent's speed scaled down by the link's cost
        // modifier (a modifier of 2 makes the crossing take twice as long).
        float costModifier = MathF.Max(state.OffMeshLinkCostModifier, 0.001f);
        float traversalSpeed = agent.Speed / costModifier;

        if (length.IsApproximatelyZero())
        {
            state.OffMeshLinkProgress = 1f;
        }
        else
        {
            state.OffMeshLinkProgress += traversalSpeed * deltaTime / length;
            agent.DesiredVelocity = Vector3.Normalize(end - start) * traversalSpeed;
        }

        if (state.OffMeshLinkProgress >= 1f)
        {
            // Land exactly on the exit point and resume normal path following
            // from the waypoint after the landing point.
            var previousPosition = transform.Position;
            transform.Position = end;
            state.DistanceTraveled += Vector3.Distance(previousPosition, end);
            state.IsTraversingOffMeshLink = false;
            state.CurrentWaypointIndex += 2;

            World.Send(new OffMeshLinkTraversalCompleted(entity, start, end, state.OffMeshLinkAreaType));

            agent.RemainingDistance = CalculateRemainingDistance(
                transform.Position,
                state.Path,
                state.CurrentWaypointIndex);

            if (state.CurrentWaypointIndex >= state.Path.Count)
            {
                CompleteNavigation(ref agent);
            }

            return;
        }

        var newPosition = Vector3.Lerp(start, end, state.OffMeshLinkProgress);
        state.DistanceTraveled += Vector3.Distance(transform.Position, newPosition);
        transform.Position = newPosition;
        agent.RemainingDistance = Vector3.Distance(newPosition, end) + CalculateRemainingDistance(
            end,
            state.Path,
            state.CurrentWaypointIndex + 2);
    }

    /// <summary>
    /// Resolves the cost modifier for a traversal by matching the traversal
    /// endpoints against <see cref="OffMeshLink"/> components in the world.
    /// </summary>
    /// <remarks>
    /// Link endpoints are snapped onto the navigation mesh during the build, so
    /// matching uses a tolerance derived from each link's radius. Returns 1
    /// when no matching link entity exists (e.g., the mesh was baked from
    /// definitions that are not present in this world).
    /// </remarks>
    private float ResolveCostModifier(Vector3 start, Vector3 end)
    {
        foreach (var linkEntity in World.Query<OffMeshLink>())
        {
            ref readonly var link = ref World.Get<OffMeshLink>(linkEntity);

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
                return MathF.Max(link.CostModifier, 0.001f);
            }
        }

        return 1f;
    }

    private static void CompleteNavigation(ref NavMeshAgent agent)
    {
        agent.HasPath = false;
        agent.IsStopped = true;
        agent.DesiredVelocity = Vector3.Zero;
        agent.RemainingDistance = 0f;
    }

    private static float CalculateRemainingDistance(
        Vector3 currentPosition,
        Abstractions.NavPath path,
        int currentWaypointIndex)
    {
        if (currentWaypointIndex >= path.Count)
        {
            return 0f;
        }

        // Distance to current waypoint
        float distance = Vector3.Distance(currentPosition, path[currentWaypointIndex].Position);

        // Add distances between remaining waypoints
        for (int i = currentWaypointIndex + 1; i < path.Count; i++)
        {
            distance += path[i - 1].DistanceTo(path[i]);
        }

        return distance;
    }
}
