using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Leaves;
using KeenEyes.AI.FSM;
using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests;

/// <summary>
/// Tests for the AIPlugin.
/// </summary>
public class AIPluginTests
{
    #region Plugin Properties Tests

    [Fact]
    public void Name_ReturnsAI()
    {
        var plugin = new AIPlugin();

        plugin.Name.ShouldBe("AI");
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_RegistersAIContext()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var context = world.GetExtension<AIContext>();
        context.ShouldNotBeNull();
    }

    [Fact]
    public void Install_RegistersStateMachineComponent()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        // Should not throw - component is registered
        Should.NotThrow(() =>
        {
            var entity = world.Spawn()
                .With(StateMachineComponent.Create(fsm))
                .Build();
        });
    }

    [Fact]
    public void Install_RegistersBehaviorTreeComponent()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var bt = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = new TestPluginBTNode(BTNodeState.Success)
        };

        // Should not throw - component is registered
        Should.NotThrow(() =>
        {
            var entity = world.Spawn()
                .With(BehaviorTreeComponent.Create(bt))
                .Build();
        });
    }

    [Fact]
    public void Install_RegistersUtilityComponent()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Idle", Action = new TestPluginAction() }
            ]
        };

        // Should not throw - component is registered
        Should.NotThrow(() =>
        {
            var entity = world.Spawn()
                .With(UtilityComponent.Create(ai))
                .Build();
        });
    }

    [Fact]
    public void Install_RegistersStateMachineSystem()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        // System should have processed the entity
        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Install_RegistersBehaviorTreeSystem()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var node = new TestPluginBTNode(BTNodeState.Success);
        var bt = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "Test",
            Root = node
        };

        var entity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Update(0.016f);

        // System should have processed the entity
        node.ExecuteCount.ShouldBe(1);
    }

    [Fact]
    public void Install_RegistersUtilitySystem()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var executeCount = 0;
        var action = new TestPluginAction(() => executeCount++);

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Test", Action = action }
            ]
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        // System should have processed the entity
        executeCount.ShouldBe(1);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_RemovesAIContext()
    {
        using var world = new World();
        var plugin = new AIPlugin();

        world.InstallPlugin(plugin);
        world.UninstallPlugin("AI");

        // GetExtension throws when extension doesn't exist
        Should.Throw<InvalidOperationException>(() => world.GetExtension<AIContext>());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Plugin_SupportsMultipleAITypes()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        // Create entities with different AI types
        var fsm = new StateMachine
        {
            Name = "FSM",
            States = [new State { Name = "Idle" }]
        };

        var bt = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "BT",
            Root = new TestPluginBTNode(BTNodeState.Success)
        };

        var ai = new UtilityAI
        {
            Name = "Utility",
            Actions = [new UtilityAction { Name = "Test", Action = new TestPluginAction() }]
        };

        var fsmEntity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        var btEntity = world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        var aiEntity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        // All should be initialized
        world.Get<StateMachineComponent>(fsmEntity).IsInitialized.ShouldBeTrue();
        world.Get<BehaviorTreeComponent>(btEntity).IsInitialized.ShouldBeTrue();
        world.Get<UtilityComponent>(aiEntity).IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Plugin_ProcessesSystemsInOrder()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        var order = new List<string>();

        // Create an entity with all AI types that track execution order
        var fsm = new StateMachine
        {
            Name = "FSM",
            States = [
                new State
                {
                    Name = "Idle",
                    OnUpdateActions = [new OrderTrackingAction(order, "FSM")]
                }
            ]
        };

        var bt = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "BT",
            Root = new ActionNode
            {
                Name = "Track",
                Action = new OrderTrackingAction(order, "BT")
            }
        };

        var ai = new UtilityAI
        {
            Name = "Utility",
            Actions = [
                new UtilityAction
                {
                    Name = "Test",
                    Action = new OrderTrackingAction(order, "Utility")
                }
            ]
        };

        world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        // Systems should execute in order: FSM (100) -> BT (110) -> Utility (120)
        order.Count.ShouldBe(3);
        order[0].ShouldBe("FSM");
        order[1].ShouldBe("BT");
        order[2].ShouldBe("Utility");
    }

    #endregion

    #region AIContext Tests

    [Fact]
    public void AIContext_GetStatistics_ReturnsCorrectCounts()
    {
        using var world = new World();

        world.InstallPlugin(new AIPlugin());

        // Create entities with different AI types
        var fsm = new StateMachine
        {
            Name = "FSM",
            States = [new State { Name = "Idle" }]
        };

        var bt = new KeenEyes.AI.BehaviorTree.BehaviorTree
        {
            Name = "BT",
            Root = new TestPluginBTNode(BTNodeState.Success)
        };

        var ai = new UtilityAI
        {
            Name = "Utility",
            Actions = [new UtilityAction { Name = "Test", Action = new TestPluginAction() }]
        };

        world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Spawn()
            .With(BehaviorTreeComponent.Create(bt))
            .Build();

        world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        var context = world.GetExtension<AIContext>()!;
        var stats = context.GetStatistics();

        stats.StateMachineCount.ShouldBe(1);
        stats.BehaviorTreeCount.ShouldBe(1);
        stats.UtilityAICount.ShouldBe(1);
    }

    #endregion
}

/// <summary>
/// Test BT node for plugin tests.
/// </summary>
internal sealed class TestPluginBTNode(BTNodeState resultState) : BTNode
{
    public int ExecuteCount { get; private set; }

    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        ExecuteCount++;
        LastState = resultState;
        return resultState;
    }

    public override void Reset()
    {
        base.Reset();
    }
}

/// <summary>
/// Test action for plugin tests.
/// </summary>
internal sealed class TestPluginAction(Action? onExecute = null) : IAIAction
{
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        onExecute?.Invoke();
        return BTNodeState.Running;
    }

    public void Reset()
    {
    }

    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
    }
}

/// <summary>
/// Action that tracks execution order.
/// </summary>
internal sealed class OrderTrackingAction(List<string> order, string name) : IAIAction
{
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        order.Add(name);
        return BTNodeState.Success;
    }

    public void Reset()
    {
    }

    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
    }
}
