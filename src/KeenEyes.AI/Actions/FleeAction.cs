using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Actions;

/// <summary>
/// AI action that moves an entity away from a threat using pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// This action computes a position away from the threat and navigates to it.
/// The threat can be an entity (via <see cref="BBKeys.ThreatSource"/>) or a
/// position (via <see cref="BBKeys.ThreatPosition"/>).
/// </para>
/// <para>
/// The action returns <see cref="BTNodeState.Running"/> while fleeing,
/// <see cref="BTNodeState.Success"/> when safe distance is reached, and
/// <see cref="BTNodeState.Failure"/> if no escape route can be found.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var flee = new FleeAction
/// {
///     MinFleeDistance = 15.0f,
///     SampleRadius = 20.0f,
///     SampleCount = 8
/// };
/// blackboard.Set(BBKeys.ThreatSource, enemyEntity);
/// </code>
/// </example>
public sealed class FleeAction : IAIAction
{
    private bool pathRequested;
    private Vector3 fleeDestination;
    private int retryCount;

    /// <summary>
    /// The minimum distance to maintain from the threat.
    /// </summary>
    public float MinFleeDistance { get; set; } = 10.0f;

    /// <summary>
    /// The radius to search for flee destinations.
    /// </summary>
    public float SampleRadius { get; set; } = 15.0f;

    /// <summary>
    /// The number of positions to sample when finding a flee destination.
    /// </summary>
    /// <remarks>
    /// Higher values find better escape routes but use more CPU.
    /// </remarks>
    public int SampleCount { get; set; } = 8;

    /// <summary>
    /// Maximum number of path retries before failing.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to continuously update the flee direction as the threat moves.
    /// </summary>
    public bool UpdateWhileFleeing { get; set; } = true;

    /// <summary>
    /// The interval in seconds between flee path updates.
    /// </summary>
    public float UpdateInterval { get; set; } = 1.0f;

    private float timeSinceLastUpdate;

    /// <inheritdoc/>
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Verify entity has required components
        if (!world.Has<NavMeshAgent>(entity) || !world.Has<Transform3D>(entity))
        {
            return BTNodeState.Failure;
        }

        // Get navigation context
        if (!world.TryGetExtension<NavigationContext>(out var nav) || nav is null)
        {
            return BTNodeState.Failure;
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        ref readonly var transform = ref world.Get<Transform3D>(entity);

        // Get threat position
        var threatPosition = GetThreatPosition(blackboard, world);
        if (!threatPosition.HasValue)
        {
            return BTNodeState.Failure;
        }

        blackboard.Set(BBKeys.ThreatPosition, threatPosition.Value);

        // Check if we're safe
        var distanceFromThreat = Vector3.Distance(transform.Position, threatPosition.Value);
        if (distanceFromThreat >= MinFleeDistance && agent.IsStopped)
        {
            return BTNodeState.Success;
        }

        var deltaTime = blackboard.Get(BBKeys.DeltaTime, 0f);
        timeSinceLastUpdate += deltaTime;

        // Calculate flee destination if needed
        bool shouldUpdatePath = !pathRequested ||
                                (UpdateWhileFleeing && timeSinceLastUpdate >= UpdateInterval);

        if (shouldUpdatePath)
        {
            var newDestination = CalculateFleeDestination(
                transform.Position,
                threatPosition.Value,
                nav,
                world);

            if (newDestination.HasValue)
            {
                fleeDestination = newDestination.Value;
                nav.SetDestination(entity, fleeDestination);
                pathRequested = true;
                timeSinceLastUpdate = 0f;
                retryCount = 0;
                blackboard.Set(BBKeys.Destination, fleeDestination);
            }
            else if (!pathRequested)
            {
                retryCount++;
                if (retryCount >= MaxRetries)
                {
                    return BTNodeState.Failure;
                }
            }
        }

        // Check for pathfinding failure
        if (!agent.HasPath && !agent.PathPending && pathRequested)
        {
            retryCount++;
            if (retryCount >= MaxRetries)
            {
                return BTNodeState.Failure;
            }

            // Try a new flee destination
            pathRequested = false;
            timeSinceLastUpdate = UpdateInterval; // Force recalculation
        }

        // Check if we've reached the flee destination
        if (agent.HasPath && agent.RemainingDistance <= agent.StoppingDistance)
        {
            if (distanceFromThreat >= MinFleeDistance)
            {
                return BTNodeState.Success;
            }

            // Not far enough, find a new flee point
            pathRequested = false;
            timeSinceLastUpdate = UpdateInterval;
        }

        // Store current path in blackboard
        if (agent.HasPath && nav.TryGetAgentState(entity, out var state))
        {
            blackboard.Set(BBKeys.CurrentPath, state.Path);
        }

        return BTNodeState.Running;
    }

    private static Vector3? GetThreatPosition(Blackboard blackboard, IWorld world)
    {
        // Try to get threat entity first
        if (blackboard.TryGet<Entity>(BBKeys.ThreatSource, out var threatEntity) &&
            threatEntity != Entity.Null &&
            world.IsAlive(threatEntity) &&
            world.Has<Transform3D>(threatEntity))
        {
            ref readonly var threatTransform = ref world.Get<Transform3D>(threatEntity);
            return threatTransform.Position;
        }

        // Fall back to threat position
        if (blackboard.TryGet<Vector3>(BBKeys.ThreatPosition, out var position))
        {
            return position;
        }

        return null;
    }

    private Vector3? CalculateFleeDestination(
        Vector3 currentPosition,
        Vector3 threatPosition,
        NavigationContext nav,
        IWorld world)
    {
        // Calculate direction away from threat
        var awayDirection = Vector3.Normalize(currentPosition - threatPosition);

        if (awayDirection == Vector3.Zero)
        {
            // If we're exactly at the threat position, pick a random direction
            awayDirection = new Vector3(1, 0, 0);
        }

        Vector3? bestPosition = null;
        float bestScore = float.MinValue;

        // Sample positions in a cone away from the threat
        for (int i = 0; i < SampleCount; i++)
        {
            // Calculate angle offset for this sample
            float angleOffset = (i - SampleCount / 2) * (MathF.PI / SampleCount);

            // Rotate the away direction
            var sampleDirection = RotateVector(awayDirection, angleOffset);

            // Calculate sample position
            var samplePosition = currentPosition + sampleDirection * SampleRadius;

            // Check if position is navigable
            var nearestPoint = nav.FindNearestPoint(samplePosition, 5f);
            if (nearestPoint.HasValue)
            {
                var candidatePosition = nearestPoint.Value.Position;
                var distanceFromThreat = Vector3.Distance(candidatePosition, threatPosition);

                // Score based on distance from threat
                if (distanceFromThreat > bestScore)
                {
                    bestScore = distanceFromThreat;
                    bestPosition = candidatePosition;
                }
            }
        }

        return bestPosition;
    }

    private static Vector3 RotateVector(Vector3 vector, float angle)
    {
        // Rotate around Y axis (assuming Y is up)
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);

        return new Vector3(
            vector.X * cos - vector.Z * sin,
            vector.Y,
            vector.X * sin + vector.Z * cos);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        pathRequested = false;
        fleeDestination = Vector3.Zero;
        retryCount = 0;
        timeSinceLastUpdate = 0f;
    }

    /// <inheritdoc/>
    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        Reset();

        // Stop the agent when interrupted
        if (world.Has<NavMeshAgent>(entity) &&
            world.TryGetExtension<NavigationContext>(out var nav) &&
            nav is not null)
        {
            nav.Stop(entity);
        }
    }
}
