using KeenEyes.AI;
using KeenEyes.AI.BehaviorTree;
using KeenEyes.AI.BehaviorTree.Decorators;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.BehaviorTree;

/// <summary>
/// Tests for decorator nodes.
/// </summary>
public class DecoratorTests
{
    #region Inverter Tests

    [Fact]
    public void Inverter_WithSuccessChild_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var inverter = new Inverter
        {
            Name = "TestInverter",
            Child = new TestBTNode(BTNodeState.Success)
        };

        var result = inverter.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    [Fact]
    public void Inverter_WithFailureChild_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var inverter = new Inverter
        {
            Name = "TestInverter",
            Child = new TestBTNode(BTNodeState.Failure)
        };

        var result = inverter.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Inverter_WithRunningChild_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var inverter = new Inverter
        {
            Name = "TestInverter",
            Child = new TestBTNode(BTNodeState.Running)
        };

        var result = inverter.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void Inverter_WithNoChild_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var inverter = new Inverter
        {
            Name = "TestInverter",
            Child = null
        };

        var result = inverter.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
    }

    #endregion

    #region Succeeder Tests

    [Fact]
    public void Succeeder_WithSuccessChild_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var succeeder = new Succeeder
        {
            Name = "TestSucceeder",
            Child = new TestBTNode(BTNodeState.Success)
        };

        var result = succeeder.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Succeeder_WithFailureChild_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var succeeder = new Succeeder
        {
            Name = "TestSucceeder",
            Child = new TestBTNode(BTNodeState.Failure)
        };

        var result = succeeder.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void Succeeder_WithRunningChild_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var succeeder = new Succeeder
        {
            Name = "TestSucceeder",
            Child = new TestBTNode(BTNodeState.Running)
        };

        var result = succeeder.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    #endregion

    #region Repeater Tests

    [Fact]
    public void Repeater_WithSpecificCount_RepeatsNTimes()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var repeater = new Repeater
        {
            Name = "TestRepeater",
            Child = child,
            Count = 3
        };

        // Repeater executes child once per call, returns Running until count is reached
        // First call: child succeeds, count=1, returns Running
        repeater.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);
        child.ExecuteCount.ShouldBe(1);

        // Second call: child succeeds, count=2, returns Running
        repeater.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);
        child.ExecuteCount.ShouldBe(2);

        // Third call: child succeeds, count=3, returns Success (count reached)
        var result = repeater.Execute(entity, blackboard, world);
        result.ShouldBe(BTNodeState.Success);
        child.ExecuteCount.ShouldBe(3);
    }

    [Fact]
    public void Repeater_WithNegativeCount_RepeatsIndefinitely()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var repeater = new Repeater
        {
            Name = "TestRepeater",
            Child = child,
            Count = -1 // -1 means infinite
        };

        // Execute multiple times - should always return Running
        repeater.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);
        repeater.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);
        repeater.Execute(entity, blackboard, world).ShouldBe(BTNodeState.Running);

        child.ExecuteCount.ShouldBe(3);
    }

    [Fact]
    public void Repeater_WithZeroCount_ReturnsSuccessImmediately()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var repeater = new Repeater
        {
            Name = "TestRepeater",
            Child = child,
            Count = 0 // 0 means already at count - returns success immediately
        };

        var result = repeater.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
        child.ExecuteCount.ShouldBe(0); // Child never executed
    }

    [Fact]
    public void Repeater_WithFailingChild_StopsOnFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        // Use a child that always fails
        var child = new TestBTNode(BTNodeState.Failure);
        var repeater = new Repeater
        {
            Name = "TestRepeater",
            Child = child,
            Count = 5
        };

        // First execute: child fails, repeater stops and returns Failure
        var result = repeater.Execute(entity, blackboard, world);
        result.ShouldBe(BTNodeState.Failure);
        child.ExecuteCount.ShouldBe(1);
    }

    #endregion

    #region UntilFail Tests

    [Fact]
    public void UntilFail_WithSuccessChild_ReturnsRunning()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var untilFail = new UntilFail
        {
            Name = "TestUntilFail",
            Child = new TestBTNode(BTNodeState.Success)
        };

        var result = untilFail.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Running);
    }

    [Fact]
    public void UntilFail_WithFailingChild_ReturnsSuccess()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var untilFail = new UntilFail
        {
            Name = "TestUntilFail",
            Child = new TestBTNode(BTNodeState.Failure)
        };

        var result = untilFail.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    [Fact]
    public void UntilFail_WithEventuallyFailingChild_ReturnsSuccessOnFail()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        // Use a TestBTNode that starts with Failure directly, since
        // UntilFail resets the child after each Success (resetting StateChangingBTNode)
        var untilFail = new UntilFail
        {
            Name = "TestUntilFail",
            Child = new TestBTNode(BTNodeState.Failure)
        };

        // First execute - child returns Failure, UntilFail returns Success
        var result = untilFail.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
    }

    #endregion

    #region Cooldown Tests

    [Fact]
    public void Cooldown_FirstExecute_ExecutesChild()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var cooldown = new Cooldown
        {
            Name = "TestCooldown",
            Child = child,
            Duration = 1.0f
        };

        var result = cooldown.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
        child.ExecuteCount.ShouldBe(1);
    }

    [Fact]
    public void Cooldown_DuringCooldown_ReturnsFailure()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.Time, 0f);
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var cooldown = new Cooldown
        {
            Name = "TestCooldown",
            Child = child,
            Duration = 1.0f
        };

        // First execute
        cooldown.Execute(entity, blackboard, world);
        child.ResetExecuteCount();

        // Second execute while still on cooldown (time hasn't changed)
        var result = cooldown.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Failure);
        child.ExecuteCount.ShouldBe(0);
    }

    [Fact]
    public void Cooldown_AfterCooldown_ExecutesChild()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        blackboard.Set(BBKeys.Time, 0f);
        var entity = world.Spawn().Build();

        var child = new TestBTNode(BTNodeState.Success);
        var cooldown = new Cooldown
        {
            Name = "TestCooldown",
            Child = child,
            Duration = 1.0f
        };

        // First execute at time 0
        cooldown.Execute(entity, blackboard, world);
        child.ResetExecuteCount();

        // Second execute at time 2 (after cooldown)
        blackboard.Set(BBKeys.Time, 2.0f);
        var result = cooldown.Execute(entity, blackboard, world);

        result.ShouldBe(BTNodeState.Success);
        child.ExecuteCount.ShouldBe(1);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Decorator_Reset_ResetsChild()
    {
        var child = new TestBTNode(BTNodeState.Success);
        var inverter = new Inverter
        {
            Name = "TestInverter",
            Child = child
        };

        inverter.Reset();

        child.WasReset.ShouldBeTrue();
    }

    #endregion
}
