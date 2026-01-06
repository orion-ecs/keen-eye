using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Composites;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.BehaviorTree;

/// <summary>
/// Tests for the Selector composite node.
/// </summary>
public class SelectorTests
{
    #region Success Tests

    [Fact]
    public void Execute_WithFirstChildSuccess_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Failure)
            ]
        };

        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Execute_WithSecondChildSuccess_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Success)
            ]
        };

        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Execute_WithSuccessfulChild_StopsEarly()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var thirdNode = new TestBTNode(BTNodeState.Success);

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Success),
                thirdNode
            ]
        };

        selector.Execute(entity, blackboard, world);

        thirdNode.ExecuteCount.ShouldBe(0);
    }

    #endregion

    #region Failure Tests

    [Fact]
    public void Execute_WithAllChildrenFailing_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Failure)
            ]
        };

        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void Execute_WithNoChildren_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = []
        };

        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    #endregion

    #region Running Tests

    [Fact]
    public void Execute_WithRunningChild_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Running)
            ]
        };

        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void Execute_ResumesFromRunningChild()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var firstNode = new TestBTNode(BTNodeState.Failure);
        var runningNode = new StateChangingBTNode([BTNodeState.Running, BTNodeState.Success]);

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [firstNode, runningNode]
        };

        // First execute - returns Running
        selector.Execute(entity, blackboard, world);

        // Reset the first node's execute count to verify it's not called again
        firstNode.ResetExecuteCount();

        // Second execute - should resume from running node
        var result = selector.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
        firstNode.ExecuteCount.ShouldBe(0); // Should not have been called again
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ResetsAllChildren()
    {
        var child1 = new TestBTNode(BTNodeState.Success);
        var child2 = new TestBTNode(BTNodeState.Success);

        var selector = new Selector
        {
            Name = "TestSelector",
            Children = [child1, child2]
        };

        selector.Reset();

        child1.WasReset.ShouldBeTrue();
        child2.WasReset.ShouldBeTrue();
    }

    #endregion
}

/// <summary>
/// A test behavior tree node for unit testing.
/// </summary>
internal sealed class TestBTNode(BTNodeState resultState) : BTNode
{
    public int ExecuteCount { get; private set; }
    public bool WasReset { get; private set; }

    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        ExecuteCount++;
        LastState = resultState;
        return resultState;
    }

    public override void Reset()
    {
        base.Reset();
        WasReset = true;
    }

    public void ResetExecuteCount() => ExecuteCount = 0;
}

/// <summary>
/// A behavior tree node that changes state on each execution.
/// </summary>
internal sealed class StateChangingBTNode(BTNodeState[] states) : BTNode
{
    private int currentIndex;

    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        var state = states[Math.Min(currentIndex, states.Length - 1)];
        currentIndex++;
        LastState = state;
        return state;
    }

    public override void Reset()
    {
        base.Reset();
        currentIndex = 0;
    }
}
