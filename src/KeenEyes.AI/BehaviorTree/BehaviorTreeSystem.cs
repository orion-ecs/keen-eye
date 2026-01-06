namespace KeenEyes.AI.BehaviorTree;

/// <summary>
/// System that evaluates and executes behavior trees.
/// </summary>
/// <remarks>
/// <para>
/// This system processes all entities with a <see cref="BehaviorTreeComponent"/>,
/// executing their behavior trees each tick.
/// </para>
/// <para>
/// The system:
/// </para>
/// <list type="bullet">
/// <item><description>Initializes behavior trees on first tick</description></item>
/// <item><description>Updates blackboard time values</description></item>
/// <item><description>Executes the tree from the root (or running node)</description></item>
/// <item><description>Handles tree completion and restarts</description></item>
/// </list>
/// </remarks>
public sealed class BehaviorTreeSystem : SystemBase
{
    private float totalTime;

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        totalTime += deltaTime;

        foreach (var entity in World.Query<BehaviorTreeComponent>())
        {
            ref var bt = ref World.Get<BehaviorTreeComponent>(entity);

            // Skip if disabled or no definition
            if (!bt.Enabled || bt.Definition == null)
            {
                continue;
            }

            var definition = bt.Definition;
            var blackboard = bt.GetOrCreateBlackboard();

            // Update time values in blackboard
            blackboard.Set(BBKeys.Time, totalTime);
            blackboard.Set(BBKeys.DeltaTime, deltaTime);

            // Initialize if needed
            if (!bt.IsInitialized)
            {
                InitializeBehaviorTree(ref bt, definition);
            }

            // Execute the tree
            var result = definition.Execute(entity, blackboard, World);
            bt.LastResult = result;

            // Handle completion
            if (result != BTNodeState.Running)
            {
                // Tree completed - reset for next tick
                bt.RunningNode = null;
                definition.Reset();
            }
        }
    }

    private static void InitializeBehaviorTree(ref BehaviorTreeComponent bt, BehaviorTree definition)
    {
        // Validate the definition
        var error = definition.Validate();
        if (error != null)
        {
            // Log warning and mark as initialized (to avoid repeated validation)
            bt.IsInitialized = true;
            bt.Enabled = false;
            return;
        }

        bt.IsInitialized = true;
    }
}
