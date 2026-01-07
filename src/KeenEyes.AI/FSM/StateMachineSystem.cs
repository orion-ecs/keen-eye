namespace KeenEyes.AI.FSM;

/// <summary>
/// System that evaluates and executes finite state machines.
/// </summary>
/// <remarks>
/// <para>
/// This system processes all entities with a <see cref="StateMachineComponent"/>,
/// performing the following operations each tick:
/// </para>
/// <list type="number">
/// <item><description>Initialize state machines that haven't been initialized</description></item>
/// <item><description>Execute OnEnter actions for newly entered states</description></item>
/// <item><description>Evaluate transitions in priority order</description></item>
/// <item><description>If a transition triggers, execute OnExit and OnEnter actions</description></item>
/// <item><description>Execute OnUpdate actions for the current state</description></item>
/// <item><description>Update time tracking</description></item>
/// </list>
/// <para>
/// The system updates blackboard time values (<see cref="BBKeys.Time"/> and
/// <see cref="BBKeys.DeltaTime"/>) each tick for use by conditions and actions.
/// </para>
/// </remarks>
public sealed class StateMachineSystem : SystemBase
{
    private float totalTime;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        totalTime += deltaTime;

        foreach (var entity in World.Query<StateMachineComponent>())
        {
            ref var fsm = ref World.Get<StateMachineComponent>(entity);

            // Skip if disabled or no definition
            if (!fsm.Enabled || fsm.Definition == null)
            {
                continue;
            }

            var definition = fsm.Definition;
            var blackboard = fsm.GetOrCreateBlackboard();

            // Update time values in blackboard
            blackboard.Set(BBKeys.Time, totalTime);
            blackboard.Set(BBKeys.DeltaTime, deltaTime);

            // Initialize if needed
            if (!fsm.IsInitialized)
            {
                InitializeStateMachine(ref fsm, entity, blackboard, definition);
            }

            // Handle state just entered (execute OnEnter if we haven't yet)
            if (fsm.StateJustEntered)
            {
                var currentState = definition.GetState(fsm.CurrentStateIndex);
                currentState.ExecuteEnterActions(entity, blackboard, World);
                fsm.StateJustEntered = false;
            }

            // Evaluate transitions
            var transitioned = EvaluateTransitions(ref fsm, entity, blackboard, definition);

            // Execute current state's update actions
            if (!transitioned)
            {
                var currentState = definition.GetState(fsm.CurrentStateIndex);
                currentState.ExecuteUpdateActions(entity, blackboard, World);
            }

            // Update time in state
            fsm.TimeInState += deltaTime;
        }
    }

    private static void InitializeStateMachine(
        ref StateMachineComponent fsm,
        Entity entity,
        Blackboard blackboard,
        StateMachine definition)
    {
        // Validate the definition
        var error = definition.Validate();
        if (error != null)
        {
            // Log warning and disable
            // In production, this could use a logging system
            fsm.IsInitialized = true;
            return;
        }

        // Set initial state
        fsm.CurrentStateIndex = definition.InitialStateIndex;
        fsm.PreviousStateIndex = -1;
        fsm.TimeInState = 0f;
        fsm.StateJustEntered = true;
        fsm.IsInitialized = true;
    }

    private bool EvaluateTransitions(
        ref StateMachineComponent fsm,
        Entity entity,
        Blackboard blackboard,
        StateMachine definition)
    {
        // Check transitions from current state (sorted by priority, descending)
        foreach (var transition in definition.GetTransitionsFrom(fsm.CurrentStateIndex))
        {
            if (transition.Evaluate(entity, blackboard, World))
            {
                // Transition triggered!
                PerformTransition(ref fsm, entity, blackboard, definition, transition);
                return true;
            }
        }

        return false;
    }

    private void PerformTransition(
        ref StateMachineComponent fsm,
        Entity entity,
        Blackboard blackboard,
        StateMachine definition,
        StateTransition transition)
    {
        // Execute OnExit for current state
        var currentState = definition.GetState(fsm.CurrentStateIndex);
        currentState.ExecuteExitActions(entity, blackboard, World);

        // Update state indices
        fsm.PreviousStateIndex = fsm.CurrentStateIndex;
        fsm.CurrentStateIndex = transition.ToStateIndex;
        fsm.TimeInState = 0f;
        fsm.StateJustEntered = true;

        // Reset actions in the new state
        var newState = definition.GetState(fsm.CurrentStateIndex);
        newState.ResetActions();

        // Execute OnEnter for new state
        newState.ExecuteEnterActions(entity, blackboard, World);
        fsm.StateJustEntered = false;
    }
}
