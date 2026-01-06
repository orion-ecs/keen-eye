using KeenEyes.AI;
using KeenEyes.AI.FSM;

namespace KeenEyes.AI.Tests.FSM;

/// <summary>
/// Tests for the StateMachineComponent struct.
/// </summary>
public class StateMachineComponentTests
{
    #region Create Tests

    [Fact]
    public void Create_SetsDefinition()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.Definition.ShouldBe(fsm);
    }

    [Fact]
    public void Create_SetsEnabled()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Create_SetsCurrentStateIndexToInitial()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ],
            InitialStateIndex = 1
        };

        var component = StateMachineComponent.Create(fsm);

        component.CurrentStateIndex.ShouldBe(1);
    }

    [Fact]
    public void Create_SetsPreviousStateIndexToNegativeOne()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.PreviousStateIndex.ShouldBe(-1);
    }

    [Fact]
    public void Create_SetsTimeInStateToZero()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.TimeInState.ShouldBe(0f);
    }

    [Fact]
    public void Create_SetsStateJustEnteredToTrue()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.StateJustEntered.ShouldBeTrue();
    }

    [Fact]
    public void Create_SetsIsInitializedToFalse()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);

        component.IsInitialized.ShouldBeFalse();
    }

    #endregion

    #region GetOrCreateBlackboard Tests

    [Fact]
    public void GetOrCreateBlackboard_CreatesBlackboard()
    {
        var component = new StateMachineComponent();

        var blackboard = component.GetOrCreateBlackboard();

        blackboard.ShouldNotBeNull();
    }

    [Fact]
    public void GetOrCreateBlackboard_ReturnsSameInstance()
    {
        var component = new StateMachineComponent();

        var blackboard1 = component.GetOrCreateBlackboard();
        var blackboard2 = component.GetOrCreateBlackboard();

        blackboard1.ShouldBeSameAs(blackboard2);
    }

    [Fact]
    public void GetOrCreateBlackboard_ReturnsExistingIfSet()
    {
        var existingBlackboard = new Blackboard();
        var component = new StateMachineComponent
        {
            Blackboard = existingBlackboard
        };

        var blackboard = component.GetOrCreateBlackboard();

        blackboard.ShouldBeSameAs(existingBlackboard);
    }

    #endregion

    #region CurrentStateName Tests

    [Fact]
    public void CurrentStateName_ReturnsCorrectName()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ]
        };

        var component = StateMachineComponent.Create(fsm);
        component.CurrentStateIndex = 1;

        component.CurrentStateName.ShouldBe("Patrol");
    }

    [Fact]
    public void CurrentStateName_WithNullDefinition_ReturnsNull()
    {
        var component = new StateMachineComponent { Definition = null };

        component.CurrentStateName.ShouldBeNull();
    }

    [Fact]
    public void CurrentStateName_WithInvalidIndex_ReturnsNull()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        var component = StateMachineComponent.Create(fsm);
        component.CurrentStateIndex = 10;

        component.CurrentStateName.ShouldBeNull();
    }

    #endregion
}
