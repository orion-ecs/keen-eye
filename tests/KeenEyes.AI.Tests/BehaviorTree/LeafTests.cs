using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Leaves;
using KeenEyes.AI.Tests.FSM;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.BehaviorTree;

/// <summary>
/// Tests for leaf nodes (ActionNode, ConditionNode, WaitNode).
/// </summary>
public class LeafTests
{
    #region ActionNode Tests

    [Fact]
    public void ActionNode_Execute_CallsAction()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var executeCount = 0;
        var action = new TestAction(() => executeCount++);

        var actionNode = new ActionNode
        {
            Name = "TestAction",
            Action = action
        };

        actionNode.Execute(entity, blackboard, world);

        executeCount.ShouldBe(1);
    }

    [Fact]
    public void ActionNode_Execute_ReturnsActionResult()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var action = new TestAction();
        action.SetState(BTNodeState.Success);

        var actionNode = new ActionNode
        {
            Name = "TestAction",
            Action = action
        };

        var result = actionNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void ActionNode_WithNullAction_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var actionNode = new ActionNode
        {
            Name = "TestAction",
            Action = null
        };

        var result = actionNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void ActionNode_Reset_ResetsAction()
    {
        var action = new TestAction();

        var actionNode = new ActionNode
        {
            Name = "TestAction",
            Action = action
        };

        actionNode.Reset();

        action.WasReset.ShouldBeTrue();
    }

    [Fact]
    public void ActionNode_OnInterrupted_InterruptsAction()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var wasInterrupted = false;
        var action = new InterruptibleAction(() => wasInterrupted = true);

        var actionNode = new ActionNode
        {
            Name = "TestAction",
            Action = action
        };

        actionNode.OnInterrupted(entity, blackboard, world);

        wasInterrupted.ShouldBeTrue();
    }

    #endregion

    #region ConditionNode Tests

    [Fact]
    public void ConditionNode_WithTrueCondition_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var conditionNode = new ConditionNode
        {
            Name = "TestCondition",
            Condition = new TestCondition(true)
        };

        var result = conditionNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void ConditionNode_WithFalseCondition_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var conditionNode = new ConditionNode
        {
            Name = "TestCondition",
            Condition = new TestCondition(false)
        };

        var result = conditionNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void ConditionNode_WithNullCondition_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var conditionNode = new ConditionNode
        {
            Name = "TestCondition",
            Condition = null
        };

        var result = conditionNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    #endregion

    #region WaitNode Tests

    [Fact]
    public void WaitNode_BeforeDurationComplete_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.DeltaTime, 0.5f);
        var entity = world.Spawn().Build();

        var waitNode = new WaitNode
        {
            Name = "TestWait",
            Duration = 2.0f
        };

        var result = waitNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void WaitNode_AfterDurationComplete_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.DeltaTime, 1.0f);
        var entity = world.Spawn().Build();

        var waitNode = new WaitNode
        {
            Name = "TestWait",
            Duration = 2.0f
        };

        // First execute: elapsed = 0 + 1 = 1, not >= 2, returns Running
        waitNode.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);

        // Second execute: elapsed = 1 + 1 = 2, >= 2, returns Success
        var result = waitNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void WaitNode_Reset_ResetsElapsedTime()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.DeltaTime, 1.0f);
        var entity = world.Spawn().Build();

        var waitNode = new WaitNode
        {
            Name = "TestWait",
            Duration = 2.0f
        };

        // Execute to accumulate some time
        waitNode.Execute(entity, blackboard, world);

        // Reset
        waitNode.Reset();

        // Execute again - should still be running
        var result = waitNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void WaitNode_WithZeroDuration_ReturnsImmediateSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.DeltaTime, 0.1f);
        var entity = world.Spawn().Build();

        var waitNode = new WaitNode
        {
            Name = "TestWait",
            Duration = 0f
        };

        var result = waitNode.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    #endregion
}

/// <summary>
/// An action that can track interruptions.
/// </summary>
internal sealed class InterruptibleAction(Action onInterrupted) : IAIAction
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
