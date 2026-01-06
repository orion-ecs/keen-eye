using KeenEyes.AI;
using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.Utility;

/// <summary>
/// Tests for the UtilitySystem.
/// </summary>
public class UtilitySystemTests
{
    #region Initialization Tests

    [Fact]
    public void Update_InitializesUtilityAI()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction
                {
                    Name = "Idle",
                    Action = new TestUtilityAction()
                }
            ]
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<UtilityComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Update_SetsTimeInBlackboard()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Idle", Action = new TestUtilityAction() }
            ]
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.5f);

        ref var component = ref world.Get<UtilityComponent>(entity);
        var blackboard = component.GetOrCreateBlackboard();

        blackboard.Get<float>(BBKeys.DeltaTime).ShouldBe(0.5f);
    }

    #endregion

    #region Disabled Tests

    [Fact]
    public void Update_SkipsDisabledComponents()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var executeCount = 0;
        var action = new TestUtilityAction(() => executeCount++);

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Test", Action = action }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.Enabled = false;

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        executeCount.ShouldBe(0);
    }

    [Fact]
    public void Update_SkipsNullDefinition()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var component = new UtilityComponent
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

    #region Action Selection Tests

    [Fact]
    public void Update_SelectsHighestScoringAction()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var lowAction = new TestUtilityAction();
        var highAction = new TestUtilityAction();

        var ai = new UtilityAI
        {
            Name = "Test",
            SelectionMode = UtilitySelectionMode.HighestScore,
            Actions = [
                new UtilityAction
                {
                    Name = "Low",
                    Action = lowAction,
                    Weight = 0.3f
                },
                new UtilityAction
                {
                    Name = "High",
                    Action = highAction,
                    Weight = 0.8f
                }
            ]
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<UtilityComponent>(entity);
        component.CurrentAction!.Name.ShouldBe("High");
    }

    [Fact]
    public void Update_ExecutesSelectedAction()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var executeCount = 0;
        var action = new TestUtilityAction(() => executeCount++);

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

        executeCount.ShouldBe(1);
    }

    [Fact]
    public void Update_RespectsSelectionThreshold()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            SelectionThreshold = 0.5f,
            Actions = [
                new UtilityAction
                {
                    Name = "BelowThreshold",
                    Action = new TestUtilityAction(),
                    Weight = 0.3f
                }
            ]
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<UtilityComponent>(entity);
        component.CurrentAction.ShouldBeNull();
    }

    #endregion

    #region Evaluation Interval Tests

    [Fact]
    public void Update_EvaluatesImmediatelyOnFirstUpdate()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction
                {
                    Name = "Test",
                    Action = new TestUtilityAction()
                }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 1.0f; // Long interval

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        ref var updatedComponent = ref world.Get<UtilityComponent>(entity);
        updatedComponent.CurrentAction.ShouldNotBeNull();
    }

    [Fact]
    public void Update_RespectsEvaluationInterval()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var evaluationCount = 0;

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction
                {
                    Name = "Test",
                    Action = new TestUtilityAction(),
                    Considerations = [
                        new Consideration
                        {
                            Name = "Track",
                            Input = new TrackingConsiderationInput(() => evaluationCount++, 0.5f),
                            Curve = new LinearCurve { Slope = 1f }
                        }
                    ]
                }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 1.0f;

        var entity = world.Spawn()
            .With(component)
            .Build();

        // First update - should evaluate
        world.Update(0.016f);
        var firstCount = evaluationCount;

        // Second update (not enough time) - should not re-evaluate
        world.Update(0.016f);

        evaluationCount.ShouldBe(firstCount);
    }

    [Fact]
    public void Update_ReEvaluatesAfterIntervalPasses()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var evaluationCount = 0;

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction
                {
                    Name = "Test",
                    Action = new TestUtilityAction(),
                    Considerations = [
                        new Consideration
                        {
                            Name = "Track",
                            Input = new TrackingConsiderationInput(() => evaluationCount++, 0.5f),
                            Curve = new LinearCurve { Slope = 1f }
                        }
                    ]
                }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 0.5f;

        var entity = world.Spawn()
            .With(component)
            .Build();

        // First update
        world.Update(0.016f);
        var firstCount = evaluationCount;

        // Update that passes interval
        world.Update(0.6f);

        evaluationCount.ShouldBeGreaterThan(firstCount);
    }

    #endregion

    #region Action Transition Tests

    [Fact]
    public void Update_InterruptsOldActionOnChange()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var wasInterrupted = false;
        var oldAction = new InterruptibleUtilityAction(() => wasInterrupted = true);

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Old", Action = oldAction, Weight = 1f },
                new UtilityAction { Name = "New", Action = new TestUtilityAction(), Weight = 2f }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 0f; // Evaluate every tick

        var entity = world.Spawn()
            .With(component)
            .Build();

        // Manually set current action to simulate it being selected previously
        ref var comp = ref world.Get<UtilityComponent>(entity);
        comp.CurrentAction = ai.Actions[0];
        comp.IsInitialized = true;

        world.Update(0.016f);

        wasInterrupted.ShouldBeTrue();
    }

    [Fact]
    public void Update_ResetsActionOnCompletion()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var action = new TestUtilityAction();
        action.SetState(BTNodeState.Success); // Completes immediately

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Test", Action = action }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 10f; // Long interval

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        action.WasReset.ShouldBeTrue();
    }

    [Fact]
    public void Update_TriggersReEvaluationOnActionComplete()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var action = new TestUtilityAction();
        action.SetState(BTNodeState.Success); // Completes immediately

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [
                new UtilityAction { Name = "Test", Action = action }
            ]
        };

        var component = UtilityComponent.Create(ai);
        component.EvaluationInterval = 10f; // Long interval

        var entity = world.Spawn()
            .With(component)
            .Build();

        world.Update(0.016f);

        ref var comp = ref world.Get<UtilityComponent>(entity);
        // TimeSinceEvaluation should be set to EvaluationInterval to trigger re-evaluation
        comp.TimeSinceEvaluation.ShouldBe(10f);
    }

    #endregion

    #region Invalid Definition Tests

    [Fact]
    public void Update_WithInvalidDefinition_DisablesComponent()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var ai = new UtilityAI
        {
            Name = "Test",
            Actions = [] // Invalid - no actions
        };

        var entity = world.Spawn()
            .With(UtilityComponent.Create(ai))
            .Build();

        world.Update(0.016f);

        ref var component = ref world.Get<UtilityComponent>(entity);
        component.IsInitialized.ShouldBeTrue();
        component.Enabled.ShouldBeFalse();
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public void Update_ProcessesMultipleEntities()
    {
        using var world = new World();
        world.InstallPlugin(new AIPlugin());

        var count1 = 0;
        var count2 = 0;

        var ai1 = new UtilityAI
        {
            Name = "AI1",
            Actions = [
                new UtilityAction { Name = "Test", Action = new TestUtilityAction(() => count1++) }
            ]
        };

        var ai2 = new UtilityAI
        {
            Name = "AI2",
            Actions = [
                new UtilityAction { Name = "Test", Action = new TestUtilityAction(() => count2++) }
            ]
        };

        var entity1 = world.Spawn()
            .With(UtilityComponent.Create(ai1))
            .Build();

        var entity2 = world.Spawn()
            .With(UtilityComponent.Create(ai2))
            .Build();

        world.Update(0.016f);

        count1.ShouldBe(1);
        count2.ShouldBe(1);
    }

    #endregion
}

/// <summary>
/// Test utility action for unit testing.
/// </summary>
internal sealed class TestUtilityAction(Action? onExecute = null) : IAIAction
{
    private BTNodeState state = BTNodeState.Running;

    public bool WasReset { get; private set; }

    public void SetState(BTNodeState newState) => state = newState;

    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        onExecute?.Invoke();
        return state;
    }

    public void Reset()
    {
        WasReset = true;
    }

    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
    }
}

/// <summary>
/// Test action that tracks interruptions.
/// </summary>
internal sealed class InterruptibleUtilityAction(Action onInterrupted) : IAIAction
{
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        return BTNodeState.Running;
    }

    public void Reset()
    {
    }

    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        onInterrupted();
    }
}
