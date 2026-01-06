// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.FSM;
using KeenEyes.AI.Utility;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;

namespace KeenEyes.Editor.AI;

/// <summary>
/// Editor panel for debugging AI systems.
/// </summary>
/// <remarks>
/// <para>
/// The AI Debug panel provides visualization for:
/// - Finite State Machines (current state, transitions)
/// - Behavior Trees (execution path, node states)
/// - Utility AI (action scores, selected action)
/// - Blackboard values
/// </para>
/// </remarks>
public sealed class AIDebugPanel : IEditorPanel
{
    private PanelContext? context;
    private Entity rootEntity;
    private Entity? selectedEntity;
    private AIDebugTab currentTab = AIDebugTab.Overview;

    /// <inheritdoc/>
    public Entity RootEntity => rootEntity;

    /// <inheritdoc/>
    public void Initialize(PanelContext panelContext)
    {
        context = panelContext;

        // Create the root entity for the panel UI
        rootEntity = panelContext.EditorWorld.Spawn("AIDebugPanel").Build();
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        if (context == null)
        {
            return;
        }

        // Get the scene world to access AI entities
        // In a real implementation, this would get the current scene world
        // and update the debug visualization
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (context != null && rootEntity.IsValid)
        {
            context.EditorWorld.Despawn(rootEntity);
        }
    }

    /// <summary>
    /// Gets or sets the currently selected entity for debugging.
    /// </summary>
    public Entity? SelectedEntity
    {
        get => selectedEntity;
        set => selectedEntity = value;
    }

    /// <summary>
    /// Gets or sets the current debug tab.
    /// </summary>
    public AIDebugTab CurrentTab
    {
        get => currentTab;
        set => currentTab = value;
    }

    /// <summary>
    /// Gets the layout data for the AI debug panel.
    /// </summary>
    /// <param name="sceneWorld">The scene world to inspect.</param>
    /// <returns>The panel layout data.</returns>
    public AIDebugPanelLayout GetLayout(IWorld? sceneWorld)
    {
        var layout = new AIDebugPanelLayout
        {
            CurrentTab = currentTab,
            SelectedEntity = selectedEntity
        };

        if (sceneWorld == null)
        {
            return layout;
        }

        // Populate AI entities
        layout.AIEntities = GetAIEntities(sceneWorld).ToList();

        // Get statistics
        if (sceneWorld.TryGetExtension<AIContext>(out var aiContext) && aiContext != null)
        {
            layout.Statistics = aiContext.GetStatistics();
        }

        // Get detailed info for selected entity
        if (selectedEntity.HasValue && sceneWorld.IsAlive(selectedEntity.Value))
        {
            var entity = selectedEntity.Value;
            layout.StateMachineInfo = GetStateMachineInfo(sceneWorld, entity);
            layout.BehaviorTreeInfo = GetBehaviorTreeInfo(sceneWorld, entity);
            layout.UtilityAIInfo = GetUtilityAIInfo(sceneWorld, entity);
            layout.BlackboardInfo = GetBlackboardInfo(sceneWorld, entity);
        }

        return layout;
    }

    private static IEnumerable<Entity> GetAIEntities(IWorld world)
    {
        var entities = new HashSet<Entity>();

        foreach (var entity in world.Query<StateMachineComponent>())
        {
            entities.Add(entity);
        }

        foreach (var entity in world.Query<BehaviorTreeComponent>())
        {
            entities.Add(entity);
        }

        foreach (var entity in world.Query<UtilityComponent>())
        {
            entities.Add(entity);
        }

        return entities;
    }

    private static StateMachineDebugInfo? GetStateMachineInfo(IWorld world, Entity entity)
    {
        if (!world.Has<StateMachineComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<StateMachineComponent>(entity);
        if (component.Definition == null)
        {
            return null;
        }

        var definition = component.Definition;
        var states = new List<StateDebugInfo>();

        for (var i = 0; i < definition.States.Count; i++)
        {
            var state = definition.States[i];
            states.Add(new StateDebugInfo
            {
                Index = i,
                Name = state.Name,
                IsCurrent = i == component.CurrentStateIndex
            });
        }

        return new StateMachineDebugInfo
        {
            Name = definition.Name,
            IsEnabled = component.Enabled,
            CurrentStateIndex = component.CurrentStateIndex,
            CurrentStateName = component.CurrentStateName,
            PreviousStateIndex = component.PreviousStateIndex,
            TimeInState = component.TimeInState,
            States = states
        };
    }

    private static BehaviorTreeDebugInfo? GetBehaviorTreeInfo(IWorld world, Entity entity)
    {
        if (!world.Has<BehaviorTreeComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<BehaviorTreeComponent>(entity);
        if (component.Definition == null)
        {
            return null;
        }

        var nodeInfos = new List<BTNodeDebugInfo>();
        if (component.Definition.Root != null)
        {
            CollectNodeInfos(component.Definition.Root, 0, nodeInfos);
        }

        return new BehaviorTreeDebugInfo
        {
            Name = component.Definition.Name,
            IsEnabled = component.Enabled,
            LastResult = component.LastResult,
            RunningNodeName = component.RunningNode?.Name,
            Nodes = nodeInfos
        };
    }

    private static void CollectNodeInfos(BTNode node, int depth, List<BTNodeDebugInfo> infos)
    {
        infos.Add(new BTNodeDebugInfo
        {
            Name = node.Name,
            TypeName = node.GetType().Name,
            Depth = depth,
            LastState = node.LastState
        });

        if (node is CompositeNode composite)
        {
            foreach (var child in composite.Children)
            {
                CollectNodeInfos(child, depth + 1, infos);
            }
        }
        else if (node is DecoratorNode decorator && decorator.Child != null)
        {
            CollectNodeInfos(decorator.Child, depth + 1, infos);
        }
    }

    private static UtilityAIDebugInfo? GetUtilityAIInfo(IWorld world, Entity entity)
    {
        if (!world.Has<UtilityComponent>(entity))
        {
            return null;
        }

        ref readonly var component = ref world.Get<UtilityComponent>(entity);
        if (component.Definition == null)
        {
            return null;
        }

        var actionInfos = new List<UtilityActionDebugInfo>();
        var blackboard = component.Blackboard ?? new Blackboard();

        foreach (var action in component.Definition.Actions)
        {
            var score = action.CalculateScore(entity, blackboard, world);
            actionInfos.Add(new UtilityActionDebugInfo
            {
                Name = action.Name,
                Score = score,
                IsCurrent = action == component.CurrentAction
            });
        }

        // Sort by score descending
        actionInfos.Sort((a, b) => b.Score.CompareTo(a.Score));

        return new UtilityAIDebugInfo
        {
            Name = component.Definition.Name,
            IsEnabled = component.Enabled,
            SelectionMode = component.Definition.SelectionMode,
            SelectionThreshold = component.Definition.SelectionThreshold,
            CurrentActionName = component.CurrentAction?.Name,
            Actions = actionInfos
        };
    }

    private static BlackboardDebugInfo? GetBlackboardInfo(IWorld world, Entity entity)
    {
        Blackboard? blackboard = null;

        if (world.Has<StateMachineComponent>(entity))
        {
            ref readonly var fsm = ref world.Get<StateMachineComponent>(entity);
            blackboard = fsm.Blackboard;
        }
        else if (world.Has<BehaviorTreeComponent>(entity))
        {
            ref readonly var bt = ref world.Get<BehaviorTreeComponent>(entity);
            blackboard = bt.Blackboard;
        }
        else if (world.Has<UtilityComponent>(entity))
        {
            ref readonly var utility = ref world.Get<UtilityComponent>(entity);
            blackboard = utility.Blackboard;
        }

        if (blackboard == null)
        {
            return null;
        }

        // Note: Blackboard doesn't expose enumeration, so we return a placeholder
        // A real implementation would need to extend Blackboard to support this
        return new BlackboardDebugInfo
        {
            EntryCount = blackboard.Count
        };
    }
}

/// <summary>
/// Debug tabs in the AI debug panel.
/// </summary>
public enum AIDebugTab
{
    /// <summary>
    /// Overview tab showing all AI entities and statistics.
    /// </summary>
    Overview,

    /// <summary>
    /// State Machine tab for FSM debugging.
    /// </summary>
    StateMachine,

    /// <summary>
    /// Behavior Tree tab for BT debugging.
    /// </summary>
    BehaviorTree,

    /// <summary>
    /// Utility AI tab for utility debugging.
    /// </summary>
    UtilityAI,

    /// <summary>
    /// Blackboard tab for viewing shared state.
    /// </summary>
    Blackboard
}

/// <summary>
/// Layout data for the AI debug panel.
/// </summary>
public sealed class AIDebugPanelLayout
{
    /// <summary>
    /// Gets or sets the current tab.
    /// </summary>
    public AIDebugTab CurrentTab { get; set; }

    /// <summary>
    /// Gets or sets the selected entity.
    /// </summary>
    public Entity? SelectedEntity { get; set; }

    /// <summary>
    /// Gets or sets the list of AI entities.
    /// </summary>
    public List<Entity> AIEntities { get; set; } = [];

    /// <summary>
    /// Gets or sets the AI statistics.
    /// </summary>
    public AIStatistics Statistics { get; set; }

    /// <summary>
    /// Gets or sets the state machine debug info.
    /// </summary>
    public StateMachineDebugInfo? StateMachineInfo { get; set; }

    /// <summary>
    /// Gets or sets the behavior tree debug info.
    /// </summary>
    public BehaviorTreeDebugInfo? BehaviorTreeInfo { get; set; }

    /// <summary>
    /// Gets or sets the utility AI debug info.
    /// </summary>
    public UtilityAIDebugInfo? UtilityAIInfo { get; set; }

    /// <summary>
    /// Gets or sets the blackboard debug info.
    /// </summary>
    public BlackboardDebugInfo? BlackboardInfo { get; set; }
}

/// <summary>
/// Debug information for a state machine.
/// </summary>
public sealed class StateMachineDebugInfo
{
    /// <summary>
    /// Gets or sets the state machine name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the state machine is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the current state index.
    /// </summary>
    public int CurrentStateIndex { get; set; }

    /// <summary>
    /// Gets or sets the current state name.
    /// </summary>
    public string? CurrentStateName { get; set; }

    /// <summary>
    /// Gets or sets the previous state index.
    /// </summary>
    public int PreviousStateIndex { get; set; }

    /// <summary>
    /// Gets or sets the time spent in the current state.
    /// </summary>
    public float TimeInState { get; set; }

    /// <summary>
    /// Gets or sets the list of states.
    /// </summary>
    public List<StateDebugInfo> States { get; set; } = [];
}

/// <summary>
/// Debug information for a state.
/// </summary>
public sealed class StateDebugInfo
{
    /// <summary>
    /// Gets or sets the state index.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the state name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the current state.
    /// </summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Debug information for a behavior tree.
/// </summary>
public sealed class BehaviorTreeDebugInfo
{
    /// <summary>
    /// Gets or sets the behavior tree name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the behavior tree is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last execution result.
    /// </summary>
    public BTNodeState LastResult { get; set; }

    /// <summary>
    /// Gets or sets the name of the currently running node.
    /// </summary>
    public string? RunningNodeName { get; set; }

    /// <summary>
    /// Gets or sets the list of nodes.
    /// </summary>
    public List<BTNodeDebugInfo> Nodes { get; set; } = [];
}

/// <summary>
/// Debug information for a behavior tree node.
/// </summary>
public sealed class BTNodeDebugInfo
{
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the depth in the tree.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets the last execution state.
    /// </summary>
    public BTNodeState LastState { get; set; }
}

/// <summary>
/// Debug information for utility AI.
/// </summary>
public sealed class UtilityAIDebugInfo
{
    /// <summary>
    /// Gets or sets the utility AI name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the utility AI is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the selection mode.
    /// </summary>
    public UtilitySelectionMode SelectionMode { get; set; }

    /// <summary>
    /// Gets or sets the selection threshold.
    /// </summary>
    public float SelectionThreshold { get; set; }

    /// <summary>
    /// Gets or sets the current action name.
    /// </summary>
    public string? CurrentActionName { get; set; }

    /// <summary>
    /// Gets or sets the list of actions with their scores.
    /// </summary>
    public List<UtilityActionDebugInfo> Actions { get; set; } = [];
}

/// <summary>
/// Debug information for a utility action.
/// </summary>
public sealed class UtilityActionDebugInfo
{
    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action's score.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current action.
    /// </summary>
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Debug information for a blackboard.
/// </summary>
public sealed class BlackboardDebugInfo
{
    /// <summary>
    /// Gets or sets the number of entries.
    /// </summary>
    public int EntryCount { get; set; }
}
