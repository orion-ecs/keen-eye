using KeenEyes.AI;
using KeenEyes.AI.FSM;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.FSM;

/// <summary>
/// Tests for the StateTransition class.
/// </summary>
public class StateTransitionTests
{
    #region Basic Tests

    [Fact]
    public void StateTransition_WithIndices_HasCorrectValues()
    {
        var transition = new StateTransition
        {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Priority = 10
        };

        transition.FromStateIndex.ShouldBe(0);
        transition.ToStateIndex.ShouldBe(1);
        transition.Priority.ShouldBe(10);
    }

    [Fact]
    public void StateTransition_DefaultPriority_IsZero()
    {
        var transition = new StateTransition();

        transition.Priority.ShouldBe(0);
    }

    #endregion

    #region Evaluate Tests

    [Fact]
    public void Evaluate_WithNullCondition_ReturnsTrue()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var transition = new StateTransition
        {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Condition = null
        };

        var result = transition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_WithTrueCondition_ReturnsTrue()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var transition = new StateTransition
        {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Condition = new TestCondition(true)
        };

        var result = transition.Evaluate(entity, blackboard, world);

        result.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_WithFalseCondition_ReturnsFalse()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var transition = new StateTransition
        {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Condition = new TestCondition(false)
        };

        var result = transition.Evaluate(entity, blackboard, world);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_PassesCorrectParameters()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        Entity? receivedEntity = null;
        Blackboard? receivedBlackboard = null;
        IWorld? receivedWorld = null;

        var condition = new TestCondition(true, (e, bb, w) =>
        {
            receivedEntity = e;
            receivedBlackboard = bb;
            receivedWorld = w;
        });

        var transition = new StateTransition
        {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Condition = condition
        };

        transition.Evaluate(entity, blackboard, world);

        receivedEntity.ShouldBe(entity);
        receivedBlackboard.ShouldBe(blackboard);
        receivedWorld.ShouldBe(world);
    }

    #endregion
}

/// <summary>
/// Test condition for unit testing.
/// </summary>
internal sealed class TestCondition(bool result, Action<Entity, Blackboard, IWorld>? onEvaluate = null) : ICondition
{
    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        onEvaluate?.Invoke(entity, blackboard, world);
        return result;
    }
}
