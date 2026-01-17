using System.Text.Json;
using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.FSM;
using KeenEyes.AI.Utility;
using KeenEyes.TestBridge.AI;

namespace KeenEyes.TestBridge.AIImpl;

/// <summary>
/// In-process implementation of <see cref="IAIController"/>.
/// </summary>
internal sealed class AIControllerImpl(World world) : IAIController
{
    #region Statistics

    /// <inheritdoc />
    public Task<AIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(new AIStatisticsSnapshot());
        }

        var stats = ai.GetStatistics();
        return Task.FromResult(new AIStatisticsSnapshot
        {
            StateMachineCount = stats.StateMachineCount,
            ActiveStateMachineCount = stats.ActiveStateMachineCount,
            BehaviorTreeCount = stats.BehaviorTreeCount,
            ActiveBehaviorTreeCount = stats.ActiveBehaviorTreeCount,
            UtilityAICount = stats.UtilityAICount,
            ActiveUtilityAICount = stats.ActiveUtilityAICount
        });
    }

    #endregion

    #region Behavior Tree Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetBehaviorTreeEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<BehaviorTreeComponent>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<BehaviorTreeSnapshot?> GetBehaviorTreeStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<BehaviorTreeComponent>(entity))
        {
            return Task.FromResult<BehaviorTreeSnapshot?>(null);
        }

        ref readonly var component = ref world.Get<BehaviorTreeComponent>(entity);

        return Task.FromResult<BehaviorTreeSnapshot?>(new BehaviorTreeSnapshot
        {
            EntityId = entityId,
            Enabled = component.Enabled,
            IsInitialized = component.IsInitialized,
            TreeName = component.Definition?.Name,
            LastResult = component.LastResult.ToString(),
            RunningNodeName = component.RunningNode?.Name,
            RunningNodeType = component.RunningNode?.GetType().Name,
            BlackboardEntryCount = component.Blackboard?.Count ?? 0
        });
    }

    /// <inheritdoc />
    public Task<bool> ResetBehaviorTreeAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(ai.ResetBehaviorTree(entity));
    }

    #endregion

    #region FSM Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetStateMachineEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<StateMachineComponent>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<StateMachineSnapshot?> GetStateMachineStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<StateMachineComponent>(entity))
        {
            return Task.FromResult<StateMachineSnapshot?>(null);
        }

        ref readonly var component = ref world.Get<StateMachineComponent>(entity);
        var definition = component.Definition;

        List<StateInfoSnapshot>? stateInfos = null;
        if (definition != null)
        {
            stateInfos = [];
            for (var i = 0; i < definition.States.Count; i++)
            {
                var state = definition.States[i];
                stateInfos.Add(new StateInfoSnapshot
                {
                    Index = i,
                    Name = state.Name,
                    IsCurrent = i == component.CurrentStateIndex,
                    EnterActionCount = state.OnEnterActions?.Count ?? 0,
                    UpdateActionCount = state.OnUpdateActions?.Count ?? 0,
                    ExitActionCount = state.OnExitActions?.Count ?? 0,
                    TransitionCount = definition.Transitions.Count(t => t.FromStateIndex == i)
                });
            }
        }

        string? previousStateName = null;
        if (definition != null && component.PreviousStateIndex >= 0 && component.PreviousStateIndex < definition.States.Count)
        {
            previousStateName = definition.States[component.PreviousStateIndex].Name;
        }

        return Task.FromResult<StateMachineSnapshot?>(new StateMachineSnapshot
        {
            EntityId = entityId,
            Enabled = component.Enabled,
            IsInitialized = component.IsInitialized,
            MachineName = definition?.Name,
            CurrentStateIndex = component.CurrentStateIndex,
            CurrentStateName = component.CurrentStateName,
            PreviousStateIndex = component.PreviousStateIndex >= 0 ? component.PreviousStateIndex : null,
            PreviousStateName = previousStateName,
            TimeInState = component.TimeInState,
            StateJustEntered = component.StateJustEntered,
            States = stateInfos,
            BlackboardEntryCount = component.Blackboard?.Count ?? 0
        });
    }

    /// <inheritdoc />
    public Task<bool> ForceStateTransitionAsync(int entityId, int stateIndex, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(ai.ForceStateTransition(entity, stateIndex));
    }

    /// <inheritdoc />
    public Task<bool> ForceStateTransitionByNameAsync(int entityId, string stateName, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<StateMachineComponent>(entity))
        {
            return Task.FromResult(false);
        }

        ref readonly var component = ref world.Get<StateMachineComponent>(entity);
        var definition = component.Definition;
        if (definition == null)
        {
            return Task.FromResult(false);
        }

        // Find state index by name
        var stateIndex = -1;
        for (var i = 0; i < definition.States.Count; i++)
        {
            if (string.Equals(definition.States[i].Name, stateName, StringComparison.OrdinalIgnoreCase))
            {
                stateIndex = i;
                break;
            }
        }

        if (stateIndex < 0)
        {
            return Task.FromResult(false);
        }

        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(ai.ForceStateTransition(entity, stateIndex));
    }

    #endregion

    #region Utility AI Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetUtilityAIEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<UtilityComponent>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<UtilityAISnapshot?> GetUtilityAIStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UtilityComponent>(entity))
        {
            return Task.FromResult<UtilityAISnapshot?>(null);
        }

        ref readonly var component = ref world.Get<UtilityComponent>(entity);
        var definition = component.Definition;

        return Task.FromResult<UtilityAISnapshot?>(new UtilityAISnapshot
        {
            EntityId = entityId,
            Enabled = component.Enabled,
            IsInitialized = component.IsInitialized,
            AIName = definition?.Name,
            CurrentActionName = component.CurrentAction?.Name,
            SelectionMode = definition?.SelectionMode.ToString() ?? "Unknown",
            EvaluationInterval = component.EvaluationInterval,
            TimeSinceEvaluation = component.TimeSinceEvaluation,
            SelectionThreshold = definition?.SelectionThreshold ?? 0f,
            ActionCount = definition?.Actions.Count ?? 0,
            BlackboardEntryCount = component.Blackboard?.Count ?? 0
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UtilityScoreSnapshot>> ScoreAllActionsAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult<IReadOnlyList<UtilityScoreSnapshot>>([]);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UtilityComponent>(entity))
        {
            return Task.FromResult<IReadOnlyList<UtilityScoreSnapshot>>([]);
        }

        ref readonly var component = ref world.Get<UtilityComponent>(entity);
        var currentAction = component.CurrentAction;
        var scores = ai.ScoreAllActions(entity);

        var snapshots = new List<UtilityScoreSnapshot>();
        foreach (var (action, score) in scores)
        {
            snapshots.Add(new UtilityScoreSnapshot
            {
                ActionName = action.Name,
                Score = score,
                Weight = action.Weight,
                IsSelected = action == currentAction,
                ConsiderationCount = action.Considerations.Count
            });
        }

        return Task.FromResult<IReadOnlyList<UtilityScoreSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public Task<bool> ForceUtilityEvaluationAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(ai.ForceUtilityEvaluation(entity));
    }

    #endregion

    #region Blackboard Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<BlackboardEntrySnapshot>> GetBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>([]);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>([]);
        }

        var blackboard = ai.GetBlackboard(entity);
        if (blackboard == null)
        {
            return Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>([]);
        }

        var entries = GetBlackboardEntries(blackboard);
        return Task.FromResult<IReadOnlyList<BlackboardEntrySnapshot>>(entries);
    }

    /// <inheritdoc />
    public Task<BlackboardEntrySnapshot?> GetBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult<BlackboardEntrySnapshot?>(null);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult<BlackboardEntrySnapshot?>(null);
        }

        var blackboard = ai.GetBlackboard(entity);
        if (blackboard == null || !blackboard.Has(key))
        {
            return Task.FromResult<BlackboardEntrySnapshot?>(null);
        }

        // Get value using reflection-free approach
        // We need to use object since Blackboard stores as object internally
        if (!blackboard.TryGet<object>(key, out var value) || value == null)
        {
            return Task.FromResult<BlackboardEntrySnapshot?>(null);
        }

        return Task.FromResult<BlackboardEntrySnapshot?>(CreateBlackboardEntry(key, value));
    }

    /// <inheritdoc />
    public Task<bool> SetBlackboardValueAsync(int entityId, string key, JsonElement value, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        var blackboard = ai.GetBlackboard(entity);
        if (blackboard == null)
        {
            return Task.FromResult(false);
        }

        // Convert JsonElement to appropriate type
        var convertedValue = ConvertJsonValue(value);
        if (convertedValue == null)
        {
            return Task.FromResult(false);
        }

        blackboard.Set(key, convertedValue);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> RemoveBlackboardValueAsync(int entityId, string key, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        var blackboard = ai.GetBlackboard(entity);
        if (blackboard == null)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(blackboard.Remove(key));
    }

    /// <inheritdoc />
    public Task<bool> ClearBlackboardAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<AIContext>(out var ai))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        var blackboard = ai.GetBlackboard(entity);
        if (blackboard == null)
        {
            return Task.FromResult(false);
        }

        blackboard.Clear();
        return Task.FromResult(true);
    }

    #endregion

    #region Helper Methods

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields - Acceptable for debug tooling
    private static List<BlackboardEntrySnapshot> GetBlackboardEntries(Blackboard blackboard)
    {
        // Blackboard doesn't expose its keys directly, so we need to use reflection
        // This is acceptable for debug tooling
        var entries = new List<BlackboardEntrySnapshot>();

        var dataField = typeof(Blackboard).GetField("data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (dataField?.GetValue(blackboard) is Dictionary<string, object> data)
        {
            foreach (var kvp in data)
            {
                entries.Add(CreateBlackboardEntry(kvp.Key, kvp.Value));
            }
        }

        return entries;
    }
#pragma warning restore S3011

#pragma warning disable IL2026, IL3050 // AOT compatibility - Acceptable for debug tooling which uses dynamic serialization
    private static BlackboardEntrySnapshot CreateBlackboardEntry(string key, object value)
    {
        var valueType = value.GetType();
        string? valueString;
        JsonElement? jsonValue = null;

        try
        {
            // Try to serialize the value to JSON
            var jsonString = JsonSerializer.Serialize(value);
            jsonValue = JsonDocument.Parse(jsonString).RootElement.Clone();
            valueString = valueType.IsPrimitive || value is string ? value.ToString() : jsonString;
        }
        catch
        {
            // If serialization fails, just use ToString()
            valueString = value.ToString();
        }
#pragma warning restore IL2026, IL3050

        return new BlackboardEntrySnapshot
        {
            Key = key,
            ValueType = valueType.Name,
            Value = jsonValue,
            ValueString = valueString
        };
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var intVal) => intVal,
            JsonValueKind.Number when element.TryGetInt64(out var longVal) => longVal,
            JsonValueKind.Number when element.TryGetDouble(out var doubleVal) => (float)doubleVal,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText() // For objects and arrays, store as string
        };
    }

    #endregion
}
