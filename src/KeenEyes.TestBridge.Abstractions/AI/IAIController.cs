using System.Text.Json;

namespace KeenEyes.TestBridge.AI;

/// <summary>
/// Controller interface for AI debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to AI state including behavior trees,
/// finite state machines (FSMs), utility AI, and blackboard data. It enables
/// inspection and manipulation of AI components for debugging and testing.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires the AIPlugin to be installed on the world
/// for full functionality.
/// </para>
/// </remarks>
public interface IAIController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about AI component usage in the world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics about AI components.</returns>
    Task<AIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Behavior Tree Operations

    /// <summary>
    /// Gets all entities with behavior tree components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have behavior tree components.</returns>
    Task<IReadOnlyList<int>> GetBehaviorTreeEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a behavior tree for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The behavior tree state, or null if the entity has no behavior tree.</returns>
    Task<BehaviorTreeSnapshot?> GetBehaviorTreeStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a behavior tree to its initial state.
    /// </summary>
    /// <param name="entityId">The entity ID to reset.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the reset was successful.</returns>
    Task<bool> ResetBehaviorTreeAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion

    #region FSM Operations

    /// <summary>
    /// Gets all entities with state machine components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have state machine components.</returns>
    Task<IReadOnlyList<int>> GetStateMachineEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a state machine for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The state machine state, or null if the entity has no state machine.</returns>
    Task<StateMachineSnapshot?> GetStateMachineStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a state transition in a state machine.
    /// </summary>
    /// <param name="entityId">The entity ID to transition.</param>
    /// <param name="stateIndex">The index of the target state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transition was successful.</returns>
    Task<bool> ForceStateTransitionAsync(int entityId, int stateIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a state transition in a state machine by state name.
    /// </summary>
    /// <param name="entityId">The entity ID to transition.</param>
    /// <param name="stateName">The name of the target state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the transition was successful.</returns>
    Task<bool> ForceStateTransitionByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default);

    #endregion

    #region Utility AI Operations

    /// <summary>
    /// Gets all entities with utility AI components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have utility AI components.</returns>
    Task<IReadOnlyList<int>> GetUtilityAIEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of utility AI for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The utility AI state, or null if the entity has no utility AI.</returns>
    Task<UtilityAISnapshot?> GetUtilityAIStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scores all actions for a utility AI entity.
    /// </summary>
    /// <param name="entityId">The entity ID to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of action scores, sorted by score descending.</returns>
    Task<IReadOnlyList<UtilityScoreSnapshot>> ScoreAllActionsAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces a re-evaluation of utility AI on the next tick.
    /// </summary>
    /// <param name="entityId">The entity ID to re-evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity has a utility component.</returns>
    Task<bool> ForceUtilityEvaluationAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion

    #region Blackboard Operations

    /// <summary>
    /// Gets all entries from an entity's blackboard.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of blackboard entries.</returns>
    Task<IReadOnlyList<BlackboardEntrySnapshot>> GetBlackboardAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single value from an entity's blackboard.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="key">The blackboard key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The blackboard entry, or null if not found.</returns>
    Task<BlackboardEntrySnapshot?> GetBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in an entity's blackboard.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="key">The blackboard key.</param>
    /// <param name="value">The value as JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the value was set successfully.</returns>
    Task<bool> SetBlackboardValueAsync(int entityId, string key, JsonElement value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from an entity's blackboard.
    /// </summary>
    /// <param name="entityId">The entity ID to modify.</param>
    /// <param name="key">The blackboard key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the value was removed.</returns>
    Task<bool> RemoveBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all values from an entity's blackboard.
    /// </summary>
    /// <param name="entityId">The entity ID to clear.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blackboard was cleared.</returns>
    Task<bool> ClearBlackboardAsync(int entityId, CancellationToken cancellationToken = default);

    #endregion
}
