namespace KeenEyes.AI.Utility;

/// <summary>
/// System that evaluates and executes utility AI decisions.
/// </summary>
/// <remarks>
/// <para>
/// This system processes all entities with a <see cref="UtilityComponent"/>,
/// scoring available actions and executing the selected one.
/// </para>
/// <para>
/// The system:
/// </para>
/// <list type="bullet">
/// <item><description>Initializes utility AI on first tick</description></item>
/// <item><description>Updates blackboard time values</description></item>
/// <item><description>Periodically re-evaluates actions based on <see cref="UtilityComponent.EvaluationInterval"/></description></item>
/// <item><description>Executes the current action each tick</description></item>
/// </list>
/// </remarks>
public sealed class UtilitySystem : SystemBase
{
    private float totalTime;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        totalTime += deltaTime;

        foreach (var entity in World.Query<UtilityComponent>())
        {
            ref var utility = ref World.Get<UtilityComponent>(entity);

            // Skip if disabled or no definition
            if (!utility.Enabled || utility.Definition == null)
            {
                continue;
            }

            var definition = utility.Definition;
            var blackboard = utility.GetOrCreateBlackboard();

            // Update time values in blackboard
            blackboard.Set(BBKeys.Time, totalTime);
            blackboard.Set(BBKeys.DeltaTime, deltaTime);

            // Initialize if needed
            if (!utility.IsInitialized)
            {
                InitializeUtilityAI(ref utility, definition);
            }

            // Check if we need to re-evaluate
            utility.TimeSinceEvaluation += deltaTime;

            if (ShouldEvaluate(ref utility))
            {
                EvaluateAndSelectAction(ref utility, entity, blackboard, definition, World);
            }

            // Execute current action
            if (utility.CurrentAction?.Action != null)
            {
                var state = utility.CurrentAction.Action.Execute(entity, blackboard, World);

                // If action completed (not running), trigger re-evaluation next tick
                if (state != BTNodeState.Running)
                {
                    utility.CurrentAction.Action.Reset();
                    utility.TimeSinceEvaluation = utility.EvaluationInterval;
                }
            }
        }
    }

    private static void InitializeUtilityAI(ref UtilityComponent utility, UtilityAI definition)
    {
        // Validate the definition
        var error = definition.Validate();
        if (error != null)
        {
            utility.IsInitialized = true;
            utility.Enabled = false;
            return;
        }

        utility.IsInitialized = true;
        utility.TimeSinceEvaluation = utility.EvaluationInterval; // Evaluate immediately
    }

    private static bool ShouldEvaluate(ref UtilityComponent utility)
    {
        // Evaluate if no current action
        if (utility.CurrentAction == null)
        {
            return true;
        }

        // Evaluate if interval has passed
        if (utility.EvaluationInterval <= 0)
        {
            return true; // Evaluate every tick
        }

        return utility.TimeSinceEvaluation >= utility.EvaluationInterval;
    }

    private static void EvaluateAndSelectAction(
        ref UtilityComponent utility,
        Entity entity,
        Blackboard blackboard,
        UtilityAI definition,
        IWorld world)
    {
        utility.TimeSinceEvaluation = 0f;

        var selectedAction = definition.SelectAction(entity, blackboard, world);

        // If selected action changed, interrupt the old one
        if (selectedAction != utility.CurrentAction && utility.CurrentAction?.Action != null)
        {
            utility.CurrentAction.Action.OnInterrupted(entity, blackboard, world);
            utility.CurrentAction.Action.Reset();
        }

        utility.CurrentAction = selectedAction;
    }
}
