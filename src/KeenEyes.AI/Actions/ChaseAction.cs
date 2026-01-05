using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Actions;

/// <summary>
/// AI action that chases a target entity, replanning the path as the target moves.
/// </summary>
/// <remarks>
/// <para>
/// This action continuously tracks a target entity and updates the navigation path
/// at configurable intervals. The target can be specified directly or read from
/// the blackboard using <see cref="BBKeys.Target"/>.
/// </para>
/// <para>
/// The action returns <see cref="BTNodeState.Running"/> while chasing,
/// <see cref="BTNodeState.Success"/> when within <see cref="CatchDistance"/> of the target,
/// and <see cref="BTNodeState.Failure"/> if the target is lost or unreachable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var behavior = new Selector {
///     Children = [
///         new Sequence {
///             Children = [
///                 new ConditionNode { Condition = new CanSeeTargetCondition() },
///                 new ActionNode { Action = new ChaseAction { UpdateInterval = 0.5f } }
///             ]
///         },
///         new ActionNode { Action = new PatrolAction { Loop = true } }
///     ]
/// };
/// </code>
/// </example>
public sealed class ChaseAction : IAIAction
{
    private float timeSinceLastUpdate;
    private bool initialPathRequested;

    /// <summary>
    /// The target entity to chase.
    /// </summary>
    /// <remarks>
    /// If <see cref="Entity.Null"/>, the target is read from the blackboard using <see cref="BBKeys.Target"/>.
    /// </remarks>
    public Entity Target { get; set; } = Entity.Null;

    /// <summary>
    /// The interval in seconds between path updates.
    /// </summary>
    /// <remarks>
    /// Lower values provide more responsive tracking but increase CPU usage.
    /// Defaults to 0.5 seconds.
    /// </remarks>
    public float UpdateInterval { get; set; } = 0.5f;

    /// <summary>
    /// The distance at which the target is considered caught.
    /// </summary>
    public float CatchDistance { get; set; } = 1.5f;

    /// <summary>
    /// Maximum time without a valid path before failing in seconds.
    /// </summary>
    public float LostTargetTimeout { get; set; } = 3.0f;

    /// <summary>
    /// Whether to predict target movement for smoother chasing.
    /// </summary>
    public bool PredictTargetMovement { get; set; } = true;

    /// <summary>
    /// How far ahead to predict target movement in seconds.
    /// </summary>
    public float PredictionTime { get; set; } = 0.3f;

    /// <inheritdoc/>
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get the target entity
        var target = Target != Entity.Null
            ? Target
            : blackboard.Get<Entity?>(BBKeys.Target) ?? Entity.Null;

        // Verify entities exist and have required components
        if (target == Entity.Null || !world.IsAlive(target))
        {
            return BTNodeState.Failure;
        }

        if (!world.Has<NavMeshAgent>(entity) || !world.Has<Transform3D>(entity))
        {
            return BTNodeState.Failure;
        }

        if (!world.Has<Transform3D>(target))
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
        ref readonly var targetTransform = ref world.Get<Transform3D>(target);

        var targetPosition = targetTransform.Position;

        // Apply movement prediction if enabled
        if (PredictTargetMovement && world.Has<Velocity3D>(target))
        {
            ref readonly var targetVelocity = ref world.Get<Velocity3D>(target);
            targetPosition += targetVelocity.Value * PredictionTime;
        }

        // Update blackboard with target position
        blackboard.Set(BBKeys.TargetPosition, targetPosition);
        blackboard.Set(BBKeys.TargetLastSeen, targetPosition);

        // Check if we've caught the target
        var distanceToTarget = Vector3.Distance(transform.Position, targetPosition);
        if (distanceToTarget <= CatchDistance)
        {
            return BTNodeState.Success;
        }

        var deltaTime = blackboard.Get(BBKeys.DeltaTime, 0f);
        timeSinceLastUpdate += deltaTime;

        // Request initial path or update path at intervals
        bool shouldUpdatePath = !initialPathRequested || timeSinceLastUpdate >= UpdateInterval;

        if (shouldUpdatePath)
        {
            nav.SetDestination(entity, targetPosition);
            timeSinceLastUpdate = 0f;
            initialPathRequested = true;

            // Store update timing in blackboard
            blackboard.Set(BBKeys.ChaseUpdateInterval, UpdateInterval);
            blackboard.Set(BBKeys.ChaseTimeSinceUpdate, 0f);
        }
        else
        {
            blackboard.Set(BBKeys.ChaseTimeSinceUpdate, timeSinceLastUpdate);
        }

        // Check if path is valid
        if (!agent.HasPath && !agent.PathPending && initialPathRequested)
        {
            // Path computation failed - target may be unreachable
            return BTNodeState.Failure;
        }

        // Store current path in blackboard
        if (agent.HasPath && nav.TryGetAgentState(entity, out var state))
        {
            blackboard.Set(BBKeys.CurrentPath, state.Path);
        }

        return BTNodeState.Running;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        timeSinceLastUpdate = 0f;
        initialPathRequested = false;
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
