using System.ComponentModel;
using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.AI;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for AI debugging: behavior trees, state machines, utility AI, and blackboards.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the AIPlugin debugging infrastructure via MCP, allowing inspection
/// and manipulation of AI components in running games.
/// </para>
/// <para>
/// Note: These tools require the AIPlugin to be installed in the target world.
/// Entities must have the appropriate AI component (BehaviorTreeComponent,
/// StateMachineComponent, or UtilityComponent) for the operations to work.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class AITools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "ai_get_statistics")]
    [Description("Get overall AI statistics including counts of behavior trees, state machines, and utility AI instances.")]
    public async Task<AIStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.AI.GetStatisticsAsync();
        return AIStatisticsResult.FromSnapshot(stats);
    }

    #endregion

    #region Behavior Trees

    [McpServerTool(Name = "ai_behavior_tree_list")]
    [Description("List all entities that have a BehaviorTreeComponent.")]
    public async Task<EntityListResult> GetBehaviorTreeEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.AI.GetBehaviorTreeEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "ai_behavior_tree_get")]
    [Description("Get the current state of an entity's behavior tree.")]
    public async Task<BehaviorTreeResult> GetBehaviorTreeState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.AI.GetBehaviorTreeStateAsync(entityId);

        if (snapshot == null)
        {
            return new BehaviorTreeResult
            {
                Success = false,
                Error = $"No behavior tree found on entity {entityId}"
            };
        }

        return BehaviorTreeResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "ai_behavior_tree_reset")]
    [Description("Reset an entity's behavior tree to its initial state.")]
    public async Task<OperationResult> ResetBehaviorTree(
        [Description("The entity ID to reset")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.ResetBehaviorTreeAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to reset behavior tree on entity {entityId}"
        };
    }

    #endregion

    #region State Machines

    [McpServerTool(Name = "ai_state_machine_list")]
    [Description("List all entities that have a StateMachineComponent.")]
    public async Task<EntityListResult> GetStateMachineEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.AI.GetStateMachineEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "ai_state_machine_get")]
    [Description("Get the current state of an entity's state machine, including all states and transitions.")]
    public async Task<StateMachineResult> GetStateMachineState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.AI.GetStateMachineStateAsync(entityId);

        if (snapshot == null)
        {
            return new StateMachineResult
            {
                Success = false,
                Error = $"No state machine found on entity {entityId}"
            };
        }

        return StateMachineResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "ai_state_machine_force_state")]
    [Description("Force a state machine to transition to a specific state by index.")]
    public async Task<OperationResult> ForceStateTransition(
        [Description("The entity ID")]
        int entityId,
        [Description("The state index to transition to")]
        int stateIndex)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.ForceStateTransitionAsync(entityId, stateIndex);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to transition to state {stateIndex} on entity {entityId}"
        };
    }

    [McpServerTool(Name = "ai_state_machine_force_state_by_name")]
    [Description("Force a state machine to transition to a specific state by name.")]
    public async Task<OperationResult> ForceStateTransitionByName(
        [Description("The entity ID")]
        int entityId,
        [Description("The state name to transition to")]
        string stateName)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.ForceStateTransitionByNameAsync(entityId, stateName);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to transition to state '{stateName}' on entity {entityId}"
        };
    }

    #endregion

    #region Utility AI

    [McpServerTool(Name = "ai_utility_list")]
    [Description("List all entities that have a UtilityComponent.")]
    public async Task<EntityListResult> GetUtilityAIEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.AI.GetUtilityAIEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "ai_utility_get")]
    [Description("Get the current state of an entity's utility AI, including current action and evaluation settings.")]
    public async Task<UtilityAIResult> GetUtilityAIState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.AI.GetUtilityAIStateAsync(entityId);

        if (snapshot == null)
        {
            return new UtilityAIResult
            {
                Success = false,
                Error = $"No utility AI found on entity {entityId}"
            };
        }

        return UtilityAIResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "ai_utility_score_all")]
    [Description("Evaluate and return scores for all available actions on a utility AI entity.")]
    public async Task<UtilityScoresResult> ScoreAllActions(
        [Description("The entity ID to score")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var scores = await bridge.AI.ScoreAllActionsAsync(entityId);
        return new UtilityScoresResult
        {
            Success = true,
            EntityId = entityId,
            Scores = scores,
            Count = scores.Count
        };
    }

    [McpServerTool(Name = "ai_utility_force_evaluation")]
    [Description("Force an immediate utility AI evaluation, bypassing the normal evaluation interval.")]
    public async Task<OperationResult> ForceUtilityEvaluation(
        [Description("The entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.ForceUtilityEvaluationAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to force utility evaluation on entity {entityId}"
        };
    }

    #endregion

    #region Blackboard

    [McpServerTool(Name = "ai_blackboard_get")]
    [Description("Get all entries in an entity's AI blackboard.")]
    public async Task<BlackboardResult> GetBlackboard(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.AI.GetBlackboardAsync(entityId);
        return new BlackboardResult
        {
            Success = true,
            EntityId = entityId,
            Entries = entries,
            Count = entries.Count
        };
    }

    [McpServerTool(Name = "ai_blackboard_get_value")]
    [Description("Get a specific value from an entity's AI blackboard.")]
    public async Task<BlackboardValueResult> GetBlackboardValue(
        [Description("The entity ID to query")]
        int entityId,
        [Description("The blackboard key")]
        string key)
    {
        var bridge = connection.GetBridge();
        var entry = await bridge.AI.GetBlackboardValueAsync(entityId, key);

        if (entry == null)
        {
            return new BlackboardValueResult
            {
                Success = false,
                Error = $"Key '{key}' not found in blackboard for entity {entityId}"
            };
        }

        return new BlackboardValueResult
        {
            Success = true,
            EntityId = entityId,
            Entry = entry
        };
    }

    [McpServerTool(Name = "ai_blackboard_set_value")]
    [Description("Set a value in an entity's AI blackboard. Supports strings, numbers, booleans, and null.")]
    public async Task<OperationResult> SetBlackboardValue(
        [Description("The entity ID")]
        int entityId,
        [Description("The blackboard key")]
        string key,
        [Description("The value to set (as JSON)")]
        JsonElement value)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.SetBlackboardValueAsync(entityId, key, value);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set blackboard value '{key}' on entity {entityId}"
        };
    }

    [McpServerTool(Name = "ai_blackboard_remove_value")]
    [Description("Remove a value from an entity's AI blackboard.")]
    public async Task<OperationResult> RemoveBlackboardValue(
        [Description("The entity ID")]
        int entityId,
        [Description("The blackboard key to remove")]
        string key)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.RemoveBlackboardValueAsync(entityId, key);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to remove blackboard key '{key}' on entity {entityId}"
        };
    }

    [McpServerTool(Name = "ai_blackboard_clear")]
    [Description("Clear all entries from an entity's AI blackboard.")]
    public async Task<OperationResult> ClearBlackboard(
        [Description("The entity ID")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.AI.ClearBlackboardAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to clear blackboard on entity {entityId}"
        };
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for AI statistics.
/// </summary>
public sealed record AIStatisticsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the total number of state machines.
    /// </summary>
    public int StateMachineCount { get; init; }

    /// <summary>
    /// Gets the number of active (enabled) state machines.
    /// </summary>
    public int ActiveStateMachineCount { get; init; }

    /// <summary>
    /// Gets the total number of behavior trees.
    /// </summary>
    public int BehaviorTreeCount { get; init; }

    /// <summary>
    /// Gets the number of active (enabled) behavior trees.
    /// </summary>
    public int ActiveBehaviorTreeCount { get; init; }

    /// <summary>
    /// Gets the total number of utility AI instances.
    /// </summary>
    public int UtilityAICount { get; init; }

    /// <summary>
    /// Gets the number of active (enabled) utility AI instances.
    /// </summary>
    public int ActiveUtilityAICount { get; init; }

    internal static AIStatisticsResult FromSnapshot(AIStatisticsSnapshot snapshot)
    {
        return new AIStatisticsResult
        {
            StateMachineCount = snapshot.StateMachineCount,
            ActiveStateMachineCount = snapshot.ActiveStateMachineCount,
            BehaviorTreeCount = snapshot.BehaviorTreeCount,
            ActiveBehaviorTreeCount = snapshot.ActiveBehaviorTreeCount,
            UtilityAICount = snapshot.UtilityAICount,
            ActiveUtilityAICount = snapshot.ActiveUtilityAICount
        };
    }
}

/// <summary>
/// Result for entity list queries.
/// </summary>
public sealed record EntityListResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the list of entity IDs.
    /// </summary>
    public IReadOnlyList<int>? EntityIds { get; init; }

    /// <summary>
    /// Gets the count of entities.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for behavior tree queries.
/// </summary>
public sealed record BehaviorTreeResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets whether the behavior tree is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the behavior tree is initialized.
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the tree definition name.
    /// </summary>
    public string? TreeName { get; init; }

    /// <summary>
    /// Gets the last result (Running, Success, Failure).
    /// </summary>
    public string? LastResult { get; init; }

    /// <summary>
    /// Gets the currently running node name.
    /// </summary>
    public string? RunningNodeName { get; init; }

    /// <summary>
    /// Gets the currently running node type.
    /// </summary>
    public string? RunningNodeType { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static BehaviorTreeResult FromSnapshot(BehaviorTreeSnapshot snapshot)
    {
        return new BehaviorTreeResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            Enabled = snapshot.Enabled,
            IsInitialized = snapshot.IsInitialized,
            TreeName = snapshot.TreeName,
            LastResult = snapshot.LastResult,
            RunningNodeName = snapshot.RunningNodeName,
            RunningNodeType = snapshot.RunningNodeType,
            BlackboardEntryCount = snapshot.BlackboardEntryCount
        };
    }
}

/// <summary>
/// Result for state machine queries.
/// </summary>
public sealed record StateMachineResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets whether the state machine is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the state machine is initialized.
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the state machine definition name.
    /// </summary>
    public string? MachineName { get; init; }

    /// <summary>
    /// Gets the current state index.
    /// </summary>
    public int CurrentStateIndex { get; init; }

    /// <summary>
    /// Gets the current state name.
    /// </summary>
    public string? CurrentStateName { get; init; }

    /// <summary>
    /// Gets the previous state index.
    /// </summary>
    public int? PreviousStateIndex { get; init; }

    /// <summary>
    /// Gets the previous state name.
    /// </summary>
    public string? PreviousStateName { get; init; }

    /// <summary>
    /// Gets the time spent in the current state.
    /// </summary>
    public float TimeInState { get; init; }

    /// <summary>
    /// Gets whether the state was just entered this frame.
    /// </summary>
    public bool StateJustEntered { get; init; }

    /// <summary>
    /// Gets all states in the machine.
    /// </summary>
    public IReadOnlyList<StateInfoSnapshot>? States { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static StateMachineResult FromSnapshot(StateMachineSnapshot snapshot)
    {
        return new StateMachineResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            Enabled = snapshot.Enabled,
            IsInitialized = snapshot.IsInitialized,
            MachineName = snapshot.MachineName,
            CurrentStateIndex = snapshot.CurrentStateIndex,
            CurrentStateName = snapshot.CurrentStateName,
            PreviousStateIndex = snapshot.PreviousStateIndex,
            PreviousStateName = snapshot.PreviousStateName,
            TimeInState = snapshot.TimeInState,
            StateJustEntered = snapshot.StateJustEntered,
            States = snapshot.States,
            BlackboardEntryCount = snapshot.BlackboardEntryCount
        };
    }
}

/// <summary>
/// Result for utility AI queries.
/// </summary>
public sealed record UtilityAIResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets whether the utility AI is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets whether the utility AI is initialized.
    /// </summary>
    public bool IsInitialized { get; init; }

    /// <summary>
    /// Gets the AI definition name.
    /// </summary>
    public string? AIName { get; init; }

    /// <summary>
    /// Gets the currently selected action name.
    /// </summary>
    public string? CurrentActionName { get; init; }

    /// <summary>
    /// Gets the selection mode (HighestScore, Weighted, etc.).
    /// </summary>
    public string? SelectionMode { get; init; }

    /// <summary>
    /// Gets the evaluation interval in seconds.
    /// </summary>
    public float EvaluationInterval { get; init; }

    /// <summary>
    /// Gets the time since last evaluation.
    /// </summary>
    public float TimeSinceEvaluation { get; init; }

    /// <summary>
    /// Gets the selection threshold for action changes.
    /// </summary>
    public float SelectionThreshold { get; init; }

    /// <summary>
    /// Gets the number of available actions.
    /// </summary>
    public int ActionCount { get; init; }

    /// <summary>
    /// Gets the number of entries in the blackboard.
    /// </summary>
    public int BlackboardEntryCount { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static UtilityAIResult FromSnapshot(UtilityAISnapshot snapshot)
    {
        return new UtilityAIResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            Enabled = snapshot.Enabled,
            IsInitialized = snapshot.IsInitialized,
            AIName = snapshot.AIName,
            CurrentActionName = snapshot.CurrentActionName,
            SelectionMode = snapshot.SelectionMode,
            EvaluationInterval = snapshot.EvaluationInterval,
            TimeSinceEvaluation = snapshot.TimeSinceEvaluation,
            SelectionThreshold = snapshot.SelectionThreshold,
            ActionCount = snapshot.ActionCount,
            BlackboardEntryCount = snapshot.BlackboardEntryCount
        };
    }
}

/// <summary>
/// Result for utility action scoring.
/// </summary>
public sealed record UtilityScoresResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the action scores.
    /// </summary>
    public IReadOnlyList<UtilityScoreSnapshot>? Scores { get; init; }

    /// <summary>
    /// Gets the count of actions scored.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for blackboard queries.
/// </summary>
public sealed record BlackboardResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the blackboard entries.
    /// </summary>
    public IReadOnlyList<BlackboardEntrySnapshot>? Entries { get; init; }

    /// <summary>
    /// Gets the count of entries.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for single blackboard value queries.
/// </summary>
public sealed record BlackboardValueResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets the blackboard entry.
    /// </summary>
    public BlackboardEntrySnapshot? Entry { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
