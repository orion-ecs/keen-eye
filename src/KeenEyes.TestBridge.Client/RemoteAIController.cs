using System.Text.Json;
using KeenEyes.TestBridge.AI;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IAIController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteAIController(TestBridgeClient client) : IAIController
{
    #region Statistics

    /// <inheritdoc />
    public async Task<AIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<AIStatisticsSnapshot>(
            "ai.getStatistics",
            null,
            cancellationToken) ?? new AIStatisticsSnapshot();
    }

    #endregion

    #region Behavior Tree Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetBehaviorTreeEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "ai.getBehaviorTreeEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<BehaviorTreeSnapshot?> GetBehaviorTreeStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<BehaviorTreeSnapshot?>(
            "ai.getBehaviorTreeState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ResetBehaviorTreeAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.resetBehaviorTree",
            new { entityId },
            cancellationToken);
    }

    #endregion

    #region FSM Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetStateMachineEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "ai.getStateMachineEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<StateMachineSnapshot?> GetStateMachineStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<StateMachineSnapshot?>(
            "ai.getStateMachineState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ForceStateTransitionAsync(int entityId, int stateIndex, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.forceStateTransition",
            new { entityId, stateIndex },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ForceStateTransitionByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.forceStateTransitionByName",
            new { entityId, stateName },
            cancellationToken);
    }

    #endregion

    #region Utility AI Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetUtilityAIEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<int[]>(
            "ai.getUtilityAIEntities",
            null,
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<UtilityAISnapshot?> GetUtilityAIStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<UtilityAISnapshot?>(
            "ai.getUtilityAIState",
            new { entityId },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UtilityScoreSnapshot>> ScoreAllActionsAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<UtilityScoreSnapshot[]>(
            "ai.scoreAllActions",
            new { entityId },
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<bool> ForceUtilityEvaluationAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.forceUtilityEvaluation",
            new { entityId },
            cancellationToken);
    }

    #endregion

    #region Blackboard Operations

    /// <inheritdoc />
    public async Task<IReadOnlyList<BlackboardEntrySnapshot>> GetBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var result = await client.SendRequestAsync<BlackboardEntrySnapshot[]>(
            "ai.getBlackboard",
            new { entityId },
            cancellationToken);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<BlackboardEntrySnapshot?> GetBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<BlackboardEntrySnapshot?>(
            "ai.getBlackboardValue",
            new { entityId, key },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetBlackboardValueAsync(int entityId, string key, JsonElement value, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.setBlackboardValue",
            new { entityId, key, value },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.removeBlackboardValue",
            new { entityId, key },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ClearBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
    {
        return await client.SendRequestAsync<bool>(
            "ai.clearBlackboard",
            new { entityId },
            cancellationToken);
    }

    #endregion
}
