using KeenEyes.AI;
using KeenEyes.AI.FSM;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.FSM;

/// <summary>
/// Tests for the State class.
/// </summary>
public class StateTests
{
    #region Basic Tests

    [Fact]
    public void State_WithName_HasCorrectName()
    {
        var state = new State { Name = "Idle" };

        state.Name.ShouldBe("Idle");
    }

    [Fact]
    public void State_DefaultsToEmptyActionLists()
    {
        var state = new State();

        state.OnEnterActions.ShouldBeEmpty();
        state.OnUpdateActions.ShouldBeEmpty();
        state.OnExitActions.ShouldBeEmpty();
    }

    #endregion

    #region ExecuteEnterActions Tests

    [Fact]
    public void ExecuteEnterActions_ExecutesAllActions()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var enterCount = 0;
        var action = new TestAction(() => enterCount++);

        var state = new State
        {
            Name = "Test",
            OnEnterActions = [action]
        };

        state.ExecuteEnterActions(entity, blackboard, world);

        enterCount.ShouldBe(1);
    }

    [Fact]
    public void ExecuteEnterActions_ExecutesMultipleActions()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var count = 0;
        var action1 = new TestAction(() => count++);
        var action2 = new TestAction(() => count++);
        var action3 = new TestAction(() => count++);

        var state = new State
        {
            Name = "Test",
            OnEnterActions = [action1, action2, action3]
        };

        state.ExecuteEnterActions(entity, blackboard, world);

        count.ShouldBe(3);
    }

    #endregion

    #region ExecuteUpdateActions Tests

    [Fact]
    public void ExecuteUpdateActions_ExecutesAllActions()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var updateCount = 0;
        var action = new TestAction(() => updateCount++);

        var state = new State
        {
            Name = "Test",
            OnUpdateActions = [action]
        };

        state.ExecuteUpdateActions(entity, blackboard, world);

        updateCount.ShouldBe(1);
    }

    #endregion

    #region ExecuteExitActions Tests

    [Fact]
    public void ExecuteExitActions_ExecutesAllActions()
    {
        using var world = new World();
        var blackboard = new Blackboard();
        var entity = world.Spawn().Build();

        var exitCount = 0;
        var action = new TestAction(() => exitCount++);

        var state = new State
        {
            Name = "Test",
            OnExitActions = [action]
        };

        state.ExecuteExitActions(entity, blackboard, world);

        exitCount.ShouldBe(1);
    }

    #endregion

    #region ResetActions Tests

    [Fact]
    public void ResetActions_ResetsAllActions()
    {
        var action1 = new TestAction();
        var action2 = new TestAction();

        var state = new State
        {
            Name = "Test",
            OnEnterActions = [action1],
            OnUpdateActions = [action2]
        };

        // Set state to something other than default
        action1.SetState(BTNodeState.Success);
        action2.SetState(BTNodeState.Failure);

        state.ResetActions();

        action1.WasReset.ShouldBeTrue();
        action2.WasReset.ShouldBeTrue();
    }

    #endregion
}

/// <summary>
/// Test action for unit testing.
/// </summary>
internal sealed class TestAction(Action? onExecute = null) : IAIAction
{
    private BTNodeState state = BTNodeState.Success;

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
