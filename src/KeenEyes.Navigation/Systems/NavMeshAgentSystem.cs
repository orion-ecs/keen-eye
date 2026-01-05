using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

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

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension(out NavigationContext? ctx) || ctx is null)
        {
            throw new InvalidOperationException("NavMeshAgentSystem requires NavigationContext extension.");
        }

        context = ctx;
        config = ctx.Config;
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
