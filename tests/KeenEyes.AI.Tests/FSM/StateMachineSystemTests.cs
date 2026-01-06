using KeenEyes.AI;
using KeenEyes.AI.FSM;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.FSM;

/// <summary>
/// Tests for the StateMachineSystem class.
/// </summary>
public class StateMachineSystemTests
{
    #region Initialization Tests

    [Fact]
    public void Update_InitializesStateMachine()
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

        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Update_SetsTimeInBlackboard()
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

        world.Update(0.5f);

        ref var component = ref world.Get<StateMachineComponent>(entity);
        var blackboard = component.GetOrCreateBlackboard();

        blackboard.Get<float>(BBKeys.DeltaTime).ShouldBe(0.5f);
    }

    #endregion

    #region Disabled Tests

    [Fact]
    public void Update_SkipsDisabledStateMachines()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var enterCount = 0;
        var action = new TestAction(() => enterCount++);

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle", OnEnterActions = [action] }]
        };

        var component = StateMachineComponent.Create(fsm);
        component.Enabled = false;

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        enterCount.ShouldBe(0);
    }

    [Fact]
    public void Update_SkipsNullDefinition()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var component = new StateMachineComponent
        {
            Definition = null,
            Enabled = true
        };

        var entity = world.Spawn()
            .With(component)
            .Build();

        // Should not throw
        Should.NotThrow(() => world.Update(0.016f));
    }

    #endregion

    #region State Entry Tests

    [Fact]
    public void Update_ExecutesOnEnterActions()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var enterCount = 0;
        var action = new TestAction(() => enterCount++);

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle", OnEnterActions = [action] }]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        enterCount.ShouldBe(1);
    }

    [Fact]
    public void Update_ClearsStateJustEnteredAfterFirstUpdate()
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

        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.StateJustEntered.ShouldBeFalse();
    }

    #endregion

    #region Transition Tests

    [Fact]
    public void Update_EvaluatesTransitions()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ],
            Transitions = [
                new StateTransition
                {
                    FromStateIndex = 0,
                    ToStateIndex = 1,
                    Condition = new TestCondition(true)
                }
            ]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.CurrentStateIndex.ShouldBe(1);
    }

    [Fact]
    public void Update_ExecutesOnExitActionsOnTransition()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var exitCount = 0;
        var exitAction = new TestAction(() => exitCount++);

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle", OnExitActions = [exitAction] },
                new State { Name = "Patrol" }
            ],
            Transitions = [
                new StateTransition
                {
                    FromStateIndex = 0,
                    ToStateIndex = 1,
                    Condition = new TestCondition(true)
                }
            ]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        exitCount.ShouldBe(1);
    }

    [Fact]
    public void Update_UpdatesTimeInState()
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

        world.Update(0.5f);
        world.Update(0.3f);

        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.TimeInState.ShouldBeGreaterThan(0.7f);
    }

    [Fact]
    public void Update_ResetsTimeInStateOnTransition()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var transitionOnSecondUpdate = new ConditionalCondition();

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ],
            Transitions = [
                new StateTransition
                {
                    FromStateIndex = 0,
                    ToStateIndex = 1,
                    Condition = transitionOnSecondUpdate
                }
            ]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        // First update - no transition, TimeInState accumulates
        world.Update(0.5f);

        ref var beforeTransition = ref world.Get<StateMachineComponent>(entity);
        beforeTransition.TimeInState.ShouldBeGreaterThan(0.4f);

        // Enable transition
        transitionOnSecondUpdate.ShouldTransition = true;

        // Second update - transition occurs, TimeInState resets to 0 then deltaTime is added
        world.Update(0.3f);

        ref var component = ref world.Get<StateMachineComponent>(entity);
        // After transition, TimeInState is reset to 0 then deltaTime (0.3f) is added
        component.TimeInState.ShouldBe(0.3f, 0.01f);
    }

    [Fact]
    public void Update_DoesNotTransitionWhenConditionFalse()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ],
            Transitions = [
                new StateTransition
                {
                    FromStateIndex = 0,
                    ToStateIndex = 1,
                    Condition = new TestCondition(false)
                }
            ]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<StateMachineComponent>(entity);
        component.CurrentStateIndex.ShouldBe(0);
    }

    #endregion

    #region Update Actions Tests

    [Fact]
    public void Update_ExecutesOnUpdateActions()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var updateCount = 0;
        var updateAction = new TestAction(() => updateCount++);

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle", OnUpdateActions = [updateAction] }]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);
        world.Update(0.016f);

        updateCount.ShouldBe(2);
    }

    [Fact]
    public void Update_DoesNotExecuteUpdateActionsOnTransitionFrame()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var idleUpdateCount = 0;
        var idleUpdateAction = new TestAction(() => idleUpdateCount++);

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle", OnUpdateActions = [idleUpdateAction] },
                new State { Name = "Patrol" }
            ],
            Transitions = [
                new StateTransition
                {
                    FromStateIndex = 0,
                    ToStateIndex = 1,
                    Condition = new TestCondition(true)
                }
            ]
        };

        var entity = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        // Update actions should not be called because transition happened
        idleUpdateCount.ShouldBe(0);
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public void Update_ProcessesMultipleEntities()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var entity1 = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        var entity2 = world.Spawn()
            .With(StateMachineComponent.Create(fsm))
            .Build();

        world.Update(0.016f);

        world.Get<StateMachineComponent>(entity1).IsInitialized.ShouldBeTrue();
        world.Get<StateMachineComponent>(entity2).IsInitialized.ShouldBeTrue();
    }

    #endregion
}

/// <summary>
/// A condition that can be enabled/disabled programmatically.
/// </summary>
internal sealed class ConditionalCondition : ICondition
{
    public bool ShouldTransition { get; set; }

    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        return ShouldTransition;
    }
}
