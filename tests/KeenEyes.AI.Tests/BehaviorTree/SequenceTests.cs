using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Composites;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.BehaviorTree;

/// <summary>
/// Tests for the Sequence composite node.
/// </summary>
public class SequenceTests
{
    #region Success Tests

    [Fact]
    public void Execute_WithAllChildrenSucceeding_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Success)
            ]
        };

        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Execute_ExecutesAllChildrenInOrder()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var node1 = new TestBTNode(BTNodeState.Success);
        var node2 = new TestBTNode(BTNodeState.Success);
        var node3 = new TestBTNode(BTNodeState.Success);

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [node1, node2, node3]
        };

        sequence.Execute(entity, blackboard, world);

        node1.ExecuteCount.ShouldBe(1);
        node2.ExecuteCount.ShouldBe(1);
        node3.ExecuteCount.ShouldBe(1);
    }

    #endregion

    #region Failure Tests

    [Fact]
    public void Execute_WithFirstChildFailing_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Success)
            ]
        };

        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void Execute_WithMiddleChildFailing_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Failure),
                new TestBTNode(BTNodeState.Success)
            ]
        };

        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void Execute_WithFailingChild_StopsEarly()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var thirdNode = new TestBTNode(BTNodeState.Success);

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Failure),
                thirdNode
            ]
        };

        sequence.Execute(entity, blackboard, world);

        thirdNode.ExecuteCount.ShouldBe(0);
    }

    [Fact]
    public void Execute_WithNoChildren_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = []
        };

        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    #endregion

    #region Running Tests

    [Fact]
    public void Execute_WithRunningChild_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [
                new TestBTNode(BTNodeState.Success),
                new TestBTNode(BTNodeState.Running)
            ]
        };

        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void Execute_ResumesFromRunningChild()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var firstNode = new TestBTNode(BTNodeState.Success);
        var runningNode = new StateChangingBTNode([BTNodeState.Running, BTNodeState.Success]);
        var thirdNode = new TestBTNode(BTNodeState.Success);

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [firstNode, runningNode, thirdNode]
        };

        // First execute - returns Running after second node
        sequence.Execute(entity, blackboard, world);
        firstNode.ResetExecuteCount();

        // Second execute - should resume from running node, then execute third
        var result = sequence.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
        firstNode.ExecuteCount.ShouldBe(0); // Should not have been called again
        thirdNode.ExecuteCount.ShouldBe(1); // Should have been called
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ResetsAllChildren()
    {
        var child1 = new TestBTNode(BTNodeState.Success);
        var child2 = new TestBTNode(BTNodeState.Success);

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [child1, child2]
        };

        sequence.Reset();

        child1.WasReset.ShouldBeTrue();
        child2.WasReset.ShouldBeTrue();
    }

    [Fact]
    public void Reset_ResetsCurrentChildIndex()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var node1 = new TestBTNode(BTNodeState.Success);
        var runningNode = new StateChangingBTNode([BTNodeState.Running, BTNodeState.Success]);

        var sequence = new Sequence
        {
            Name = "TestSequence",
            Children = [node1, runningNode]
        };

        // Execute to set current index to 1
        sequence.Execute(entity, blackboard, world);

        // Reset
        sequence.Reset();
        node1.ResetExecuteCount();

        // Execute again - should start from first child
        sequence.Execute(entity, blackboard, world);

        node1.ExecuteCount.ShouldBe(1);
    }

    #endregion
}
