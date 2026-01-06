using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.FSM;
using KeenEyes.AI.Utility;

namespace KeenEyes.AI;

/// <summary>
/// Extension API for AI operations in a world.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the primary API for AI operations. It enables
/// access to debug information, AI entity enumeration, and blackboard management.
/// </para>
/// <para>
/// Access this API through the world extension system:
/// <code>
/// var ai = world.GetExtension&lt;AIContext&gt;();
/// var states = ai.GetAllStateMachineStates();
/// </code>
/// </para>
/// </remarks>
[PluginExtension("AI")]
public sealed class AIContext
{
    private readonly IWorld world;

    /// <summary>
    /// Gets the world this context belongs to.
    /// </summary>
    public IWorld World => world;

    internal AIContext(IWorld world)
    {
        this.world = world;
    }

    #region FSM Operations

    /// <summary>
    /// Gets the current state name for an entity with a state machine.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The current state name, or null if the entity has no state machine or is not initialized.</returns>
    public string? GetCurrentStateName(Entity entity)
    {
        if (!world.Has<StateMachineComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<StateMachineComponent>(entity);
        if (!component.IsInitialized || component.Definition == null)
        {
            return null;
        }

        var state = component.Definition.GetState(component.CurrentStateIndex);
        return state?.Name;
    }

    /// <summary>
    /// Forces a state transition for an entity.
    /// </summary>
    /// <param name="entity">The entity to transition.</param>
    /// <param name="stateIndex">The index of the target state.</param>
    /// <returns>True if the transition was successful.</returns>
    public bool ForceStateTransition(Entity entity, int stateIndex)
    {
        if (!world.Has<StateMachineComponent>(entity))
        {
            return false;
        }

        ref var component = ref world.Get<StateMachineComponent>(entity);
        if (!component.IsInitialized || component.Definition == null)
        {
            return false;
        }

        if (stateIndex < 0 || stateIndex >= component.Definition.States.Count)
        {
            return false;
        }

        // Exit current state
        var blackboard = component.GetOrCreateBlackboard();
        var currentState = component.Definition.GetState(component.CurrentStateIndex);
        currentState?.ExecuteExitActions(entity, blackboard, world);

        // Transition
        component.PreviousStateIndex = component.CurrentStateIndex;
        component.CurrentStateIndex = stateIndex;
        component.TimeInState = 0f;
        component.StateJustEntered = true;

        return true;
    }

    /// <summary>
    /// Gets all entities with active state machines.
    /// </summary>
    /// <returns>An enumerable of entities with state machine components.</returns>
    public IEnumerable<Entity> GetStateMachineEntities()
    {
        return world.Query<StateMachineComponent>();
    }

    #endregion

    #region Behavior Tree Operations

    /// <summary>
    /// Gets the last execution result for an entity's behavior tree.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The last result, or null if the entity has no behavior tree.</returns>
    public BTNodeState? GetBehaviorTreeResult(Entity entity)
    {
        if (!world.Has<BehaviorTreeComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<BehaviorTreeComponent>(entity);
        return component.LastResult;
    }

    /// <summary>
    /// Gets the currently running node for an entity's behavior tree.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The running node, or null if none is running.</returns>
    public BTNode? GetRunningNode(Entity entity)
    {
        if (!world.Has<BehaviorTreeComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<BehaviorTreeComponent>(entity);
        return component.RunningNode;
    }

    /// <summary>
    /// Resets an entity's behavior tree to its initial state.
    /// </summary>
    /// <param name="entity">The entity to reset.</param>
    /// <returns>True if the reset was successful.</returns>
    public bool ResetBehaviorTree(Entity entity)
    {
        if (!world.Has<BehaviorTreeComponent>(entity))
        {
            return false;
        }

        ref var component = ref world.Get<BehaviorTreeComponent>(entity);
        if (component.Definition == null)
        {
            return false;
        }

        component.Definition.Reset();
        component.RunningNode = null;
        component.LastResult = BTNodeState.Success;

        return true;
    }

    /// <summary>
    /// Gets all entities with active behavior trees.
    /// </summary>
    /// <returns>An enumerable of entities with behavior tree components.</returns>
    public IEnumerable<Entity> GetBehaviorTreeEntities()
    {
        return world.Query<BehaviorTreeComponent>();
    }

    #endregion

    #region Utility AI Operations

    /// <summary>
    /// Gets the current action for an entity using utility AI.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The current action, or null if none is selected.</returns>
    public UtilityAction? GetCurrentUtilityAction(Entity entity)
    {
        if (!world.Has<UtilityComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<UtilityComponent>(entity);
        return component.CurrentAction;
    }

    /// <summary>
    /// Scores all actions for an entity and returns the results.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>A list of action-score pairs, sorted by score descending.</returns>
    public IReadOnlyList<(UtilityAction Action, float Score)> ScoreAllActions(Entity entity)
    {
        if (!world.Has<UtilityComponent>(entity))
        {
            return [];
        }

        ref readonly var component = ref world.Get<UtilityComponent>(entity);
        if (component.Definition == null || component.Definition.Actions.Count == 0)
        {
            return [];
        }

        var blackboard = component.Blackboard ?? new Blackboard();
        var results = new List<(UtilityAction Action, float Score)>(component.Definition.Actions.Count);

        foreach (var action in component.Definition.Actions)
        {
            var score = action.CalculateScore(entity, blackboard, world);
            results.Add((action, score));
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        return results;
    }

    /// <summary>
    /// Forces a re-evaluation of the entity's utility AI on the next tick.
    /// </summary>
    /// <param name="entity">The entity to re-evaluate.</param>
    /// <returns>True if the entity has a utility component.</returns>
    public bool ForceUtilityEvaluation(Entity entity)
    {
        if (!world.Has<UtilityComponent>(entity))
        {
            return false;
        }

        ref var component = ref world.Get<UtilityComponent>(entity);
        component.TimeSinceEvaluation = component.EvaluationInterval;

        return true;
    }

    /// <summary>
    /// Gets all entities with active utility AI.
    /// </summary>
    /// <returns>An enumerable of entities with utility components.</returns>
    public IEnumerable<Entity> GetUtilityAIEntities()
    {
        return world.Query<UtilityComponent>();
    }

    #endregion

    #region Blackboard Operations

    /// <summary>
    /// Gets the blackboard for an entity using any AI component.
    /// </summary>
    /// <param name="entity">The entity to query.</param>
    /// <returns>The blackboard, or null if the entity has no AI components.</returns>
    public Blackboard? GetBlackboard(Entity entity)
    {
        if (world.Has<StateMachineComponent>(entity))
        {
            ref readonly var fsm = ref world.Get<StateMachineComponent>(entity);
            return fsm.Blackboard;
        }

        if (world.Has<BehaviorTreeComponent>(entity))
        {
            ref readonly var bt = ref world.Get<BehaviorTreeComponent>(entity);
            return bt.Blackboard;
        }

        if (world.Has<UtilityComponent>(entity))
        {
            ref readonly var utility = ref world.Get<UtilityComponent>(entity);
            return utility.Blackboard;
        }

        return null;
    }

    /// <summary>
    /// Sets a value in an entity's blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="key">The blackboard key.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>True if the value was set successfully.</returns>
    public bool SetBlackboardValue<T>(Entity entity, string key, T value) where T : notnull
    {
        var blackboard = GetBlackboard(entity);
        if (blackboard == null)
        {
            return false;
        }

        blackboard.Set(key, value);
        return true;
    }

    /// <summary>
    /// Gets a value from an entity's blackboard.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="entity">The entity to query.</param>
    /// <param name="key">The blackboard key.</param>
    /// <param name="value">The retrieved value.</param>
    /// <returns>True if the value was found.</returns>
    public bool TryGetBlackboardValue<T>(Entity entity, string key, out T? value)
    {
        var blackboard = GetBlackboard(entity);
        if (blackboard == null)
        {
            value = default;
            return false;
        }

        return blackboard.TryGet(key, out value);
    }

    #endregion

    #region Debug Information

    /// <summary>
    /// Gets a summary of AI usage in the world.
    /// </summary>
    /// <returns>Statistics about AI component usage.</returns>
    public AIStatistics GetStatistics()
    {
        var stats = new AIStatistics();

        foreach (var entity in world.Query<StateMachineComponent>())
        {
            ref readonly var component = ref world.Get<StateMachineComponent>(entity);
            stats.StateMachineCount++;
            if (component.Enabled)
            {
                stats.ActiveStateMachineCount++;
            }
        }

        foreach (var entity in world.Query<BehaviorTreeComponent>())
        {
            ref readonly var component = ref world.Get<BehaviorTreeComponent>(entity);
            stats.BehaviorTreeCount++;
            if (component.Enabled)
            {
                stats.ActiveBehaviorTreeCount++;
            }
        }

        foreach (var entity in world.Query<UtilityComponent>())
        {
            ref readonly var component = ref world.Get<UtilityComponent>(entity);
            stats.UtilityAICount++;
            if (component.Enabled)
            {
                stats.ActiveUtilityAICount++;
            }
        }

        return stats;
    }

    #endregion
}

/// <summary>
/// Statistics about AI usage in a world.
/// </summary>
public struct AIStatistics
{
    /// <summary>
    /// Total number of entities with state machine components.
    /// </summary>
    public int StateMachineCount;

    /// <summary>
    /// Number of enabled state machines.
    /// </summary>
    public int ActiveStateMachineCount;

    /// <summary>
    /// Total number of entities with behavior tree components.
    /// </summary>
    public int BehaviorTreeCount;

    /// <summary>
    /// Number of enabled behavior trees.
    /// </summary>
    public int ActiveBehaviorTreeCount;

    /// <summary>
    /// Total number of entities with utility AI components.
    /// </summary>
    public int UtilityAICount;

    /// <summary>
    /// Number of enabled utility AI systems.
    /// </summary>
    public int ActiveUtilityAICount;

    /// <summary>
    /// Gets the total number of AI components.
    /// </summary>
    public readonly int TotalCount => StateMachineCount + BehaviorTreeCount + UtilityAICount;

    /// <summary>
    /// Gets the total number of active AI components.
    /// </summary>
    public readonly int TotalActiveCount => ActiveStateMachineCount + ActiveBehaviorTreeCount + ActiveUtilityAICount;
}
