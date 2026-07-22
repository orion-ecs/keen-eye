using System.Text.Json;
using KeenEyes.TestBridge.AI;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IAIController for testing MCP tools.
/// </summary>
/// <remarks>
/// Reports no AI components present, mirroring a world with no AIPlugin installed.
/// </remarks>
internal sealed class MockAIController : IAIController
{
    public Task<AIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AIStatisticsSnapshot());

    public Task<IReadOnlyList<int>> GetBehaviorTreeEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<BehaviorTreeSnapshot?> GetBehaviorTreeStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<BehaviorTreeSnapshot?>(null);

    public Task<bool> ResetBehaviorTreeAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<int>> GetStateMachineEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<StateMachineSnapshot?> GetStateMachineStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<StateMachineSnapshot?>(null);

    public Task<bool> ForceStateTransitionAsync(int entityId, int stateIndex, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> ForceStateTransitionByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<int>> GetUtilityAIEntitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<int>>([]);

    public Task<UtilityAISnapshot?> GetUtilityAIStateAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<UtilityAISnapshot?>(null);

    public Task<IReadOnlyList<UtilityScoreSnapshot>> ScoreAllActionsAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UtilityScoreSnapshot>>([]);

    public Task<bool> ForceUtilityEvaluationAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<BlackboardEntrySnapshot>> GetBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>([]);

    public Task<BlackboardEntrySnapshot?> GetBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
        => Task.FromResult<BlackboardEntrySnapshot?>(null);

    public Task<bool> SetBlackboardValueAsync(int entityId, string key, JsonElement value, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> RemoveBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> ClearBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
