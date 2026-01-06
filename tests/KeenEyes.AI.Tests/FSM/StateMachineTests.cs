using KeenEyes.AI.FSM;

namespace KeenEyes.AI.Tests.FSM;

/// <summary>
/// Tests for the StateMachine class.
/// </summary>
public class StateMachineTests
{
    #region Validation Tests

    [Fact]
    public void Validate_WithNoStates_ReturnsError()
    {
        var fsm = new StateMachine
        {
            Name = "Empty",
            States = []
        };

        var error = fsm.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("at least one state");
    }

    [Fact]
    public void Validate_WithStates_ReturnsNull()
    {
        var fsm = new StateMachine
        {
            Name = "Valid",
            States = [new State { Name = "Idle" }]
        };

        var error = fsm.Validate();

        error.ShouldBeNull();
    }

    [Fact]
    public void Validate_WithInvalidInitialStateIndex_ReturnsError()
    {
        var fsm = new StateMachine
        {
            Name = "Invalid",
            States = [new State { Name = "Idle" }],
            InitialStateIndex = 5
        };

        var error = fsm.Validate();

        error.ShouldNotBeNull();
    }

    [Fact]
    public void Validate_WithNegativeInitialStateIndex_ReturnsError()
    {
        var fsm = new StateMachine
        {
            Name = "Invalid",
            States = [new State { Name = "Idle" }],
            InitialStateIndex = -1
        };

        var error = fsm.Validate();

        error.ShouldNotBeNull();
    }

    #endregion

    #region GetState Tests

    [Fact]
    public void GetState_WithValidIndex_ReturnsState()
    {
        var idleState = new State { Name = "Idle" };
        var patrolState = new State { Name = "Patrol" };

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [idleState, patrolState]
        };

        var state = fsm.GetState(0);

        state.ShouldBe(idleState);
    }

    [Fact]
    public void GetState_WithSecondIndex_ReturnsCorrectState()
    {
        var idleState = new State { Name = "Idle" };
        var patrolState = new State { Name = "Patrol" };

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [idleState, patrolState]
        };

        var state = fsm.GetState(1);

        state.ShouldBe(patrolState);
    }

    [Fact]
    public void GetState_WithInvalidIndex_ThrowsArgumentOutOfRange()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        Should.Throw<ArgumentOutOfRangeException>(() => fsm.GetState(5));
    }

    [Fact]
    public void GetState_WithNegativeIndex_ThrowsArgumentOutOfRange()
    {
        var fsm = new StateMachine
        {
            Name = "Test",
            States = [new State { Name = "Idle" }]
        };

        Should.Throw<ArgumentOutOfRangeException>(() => fsm.GetState(-1));
    }

    #endregion

    #region GetTransitionsFrom Tests

    [Fact]
    public void GetTransitionsFrom_WithMatchingTransitions_ReturnsTransitions()
    {
        var transition1 = new StateTransition { FromStateIndex = 0, ToStateIndex = 1, Priority = 1 };
        var transition2 = new StateTransition { FromStateIndex = 0, ToStateIndex = 2, Priority = 2 };
        var transition3 = new StateTransition { FromStateIndex = 1, ToStateIndex = 0, Priority = 1 };

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" },
                new State { Name = "Chase" }
            ],
            Transitions = [transition1, transition2, transition3]
        };

        var transitions = fsm.GetTransitionsFrom(0).ToList();

        transitions.Count.ShouldBe(2);
        transitions.ShouldContain(transition1);
        transitions.ShouldContain(transition2);
    }

    [Fact]
    public void GetTransitionsFrom_ReturnsTransitionsSortedByPriorityDescending()
    {
        var transition1 = new StateTransition { FromStateIndex = 0, ToStateIndex = 1, Priority = 1 };
        var transition2 = new StateTransition { FromStateIndex = 0, ToStateIndex = 2, Priority = 10 };
        var transition3 = new StateTransition { FromStateIndex = 0, ToStateIndex = 3, Priority = 5 };

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "State0" },
                new State { Name = "State1" },
                new State { Name = "State2" },
                new State { Name = "State3" }
            ],
            Transitions = [transition1, transition2, transition3]
        };

        var transitions = fsm.GetTransitionsFrom(0).ToList();

        transitions[0].Priority.ShouldBe(10);
        transitions[1].Priority.ShouldBe(5);
        transitions[2].Priority.ShouldBe(1);
    }

    [Fact]
    public void GetTransitionsFrom_WithNoMatchingTransitions_ReturnsEmpty()
    {
        var transition = new StateTransition { FromStateIndex = 1, ToStateIndex = 0 };

        var fsm = new StateMachine
        {
            Name = "Test",
            States = [
                new State { Name = "Idle" },
                new State { Name = "Patrol" }
            ],
            Transitions = [transition]
        };

        var transitions = fsm.GetTransitionsFrom(0).ToList();

        transitions.ShouldBeEmpty();
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void Name_CanBeSetAndRetrieved()
    {
        var fsm = new StateMachine { Name = "GuardAI" };

        fsm.Name.ShouldBe("GuardAI");
    }

    [Fact]
    public void InitialStateIndex_DefaultsToZero()
    {
        var fsm = new StateMachine();

        fsm.InitialStateIndex.ShouldBe(0);
    }

    [Fact]
    public void InitialStateIndex_CanBeSet()
    {
        var fsm = new StateMachine { InitialStateIndex = 2 };

        fsm.InitialStateIndex.ShouldBe(2);
    }

    #endregion
}
