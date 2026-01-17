using System.Text.Json;
using KeenEyes.TestBridge.AI;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles AI debugging commands for behavior trees, FSMs, utility AI, and blackboards.
/// </summary>
internal sealed class AICommandHandler(IAIController aiController) : ICommandHandler
{
    public string Prefix => "ai";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Statistics
            "getStatistics" => await aiController.GetStatisticsAsync(cancellationToken),

            // Behavior Tree
            "getBehaviorTreeEntities" => await aiController.GetBehaviorTreeEntitiesAsync(cancellationToken),
            "getBehaviorTreeState" => await HandleGetBehaviorTreeStateAsync(args, cancellationToken),
            "resetBehaviorTree" => await HandleResetBehaviorTreeAsync(args, cancellationToken),

            // FSM
            "getStateMachineEntities" => await aiController.GetStateMachineEntitiesAsync(cancellationToken),
            "getStateMachineState" => await HandleGetStateMachineStateAsync(args, cancellationToken),
            "forceStateTransition" => await HandleForceStateTransitionAsync(args, cancellationToken),
            "forceStateTransitionByName" => await HandleForceStateTransitionByNameAsync(args, cancellationToken),

            // Utility AI
            "getUtilityAIEntities" => await aiController.GetUtilityAIEntitiesAsync(cancellationToken),
            "getUtilityAIState" => await HandleGetUtilityAIStateAsync(args, cancellationToken),
            "scoreAllActions" => await HandleScoreAllActionsAsync(args, cancellationToken),
            "forceUtilityEvaluation" => await HandleForceUtilityEvaluationAsync(args, cancellationToken),

            // Blackboard
            "getBlackboard" => await HandleGetBlackboardAsync(args, cancellationToken),
            "getBlackboardValue" => await HandleGetBlackboardValueAsync(args, cancellationToken),
            "setBlackboardValue" => await HandleSetBlackboardValueAsync(args, cancellationToken),
            "removeBlackboardValue" => await HandleRemoveBlackboardValueAsync(args, cancellationToken),
            "clearBlackboard" => await HandleClearBlackboardAsync(args, cancellationToken),

            _ => throw new InvalidOperationException($"Unknown AI command: {command}")
        };
    }

    #region Behavior Tree Handlers

    private async Task<BehaviorTreeSnapshot?> HandleGetBehaviorTreeStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.GetBehaviorTreeStateAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleResetBehaviorTreeAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.ResetBehaviorTreeAsync(entityId, cancellationToken);
    }

    #endregion

    #region FSM Handlers

    private async Task<StateMachineSnapshot?> HandleGetStateMachineStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.GetStateMachineStateAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleForceStateTransitionAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var stateIndex = GetRequiredInt(args, "stateIndex");
        return await aiController.ForceStateTransitionAsync(entityId, stateIndex, cancellationToken);
    }

    private async Task<bool> HandleForceStateTransitionByNameAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var stateName = GetRequiredString(args, "stateName");
        return await aiController.ForceStateTransitionByNameAsync(entityId, stateName, cancellationToken);
    }

    #endregion

    #region Utility AI Handlers

    private async Task<UtilityAISnapshot?> HandleGetUtilityAIStateAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.GetUtilityAIStateAsync(entityId, cancellationToken);
    }

    private async Task<IReadOnlyList<UtilityScoreSnapshot>> HandleScoreAllActionsAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.ScoreAllActionsAsync(entityId, cancellationToken);
    }

    private async Task<bool> HandleForceUtilityEvaluationAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.ForceUtilityEvaluationAsync(entityId, cancellationToken);
    }

    #endregion

    #region Blackboard Handlers

    private async Task<IReadOnlyList<BlackboardEntrySnapshot>> HandleGetBlackboardAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.GetBlackboardAsync(entityId, cancellationToken);
    }

    private async Task<BlackboardEntrySnapshot?> HandleGetBlackboardValueAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var key = GetRequiredString(args, "key");
        return await aiController.GetBlackboardValueAsync(entityId, key, cancellationToken);
    }

    private async Task<bool> HandleSetBlackboardValueAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var key = GetRequiredString(args, "key");
        var value = GetRequiredJsonElement(args, "value");
        return await aiController.SetBlackboardValueAsync(entityId, key, value, cancellationToken);
    }

    private async Task<bool> HandleRemoveBlackboardValueAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        var key = GetRequiredString(args, "key");
        return await aiController.RemoveBlackboardValueAsync(entityId, key, cancellationToken);
    }

    private async Task<bool> HandleClearBlackboardAsync(JsonElement? args, CancellationToken cancellationToken)
    {
        var entityId = GetRequiredInt(args, "entityId");
        return await aiController.ClearBlackboardAsync(entityId, cancellationToken);
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static JsonElement GetRequiredJsonElement(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop;
    }

    #endregion
}
