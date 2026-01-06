namespace KeenEyes.AI.Utility;

/// <summary>
/// Container for a utility AI decision-making system.
/// </summary>
/// <remarks>
/// <para>
/// Utility AI scores all available actions and selects the best one (or uses
/// weighted random selection). Each action has considerations that determine
/// when it's appropriate.
/// </para>
/// <para>
/// Utility AI is ideal for complex, dynamic behavior where the "best" action
/// depends on multiple factors that can change rapidly, such as:
/// </para>
/// <list type="bullet">
/// <item><description>NPCs with needs (hunger, tiredness, social)</description></item>
/// <item><description>Tactical combat decisions</description></item>
/// <item><description>Squad coordination</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var guardAI = new UtilityAI
/// {
///     Name = "Guard",
///     SelectionThreshold = 0.1f,
///     SelectionMode = UtilitySelectionMode.HighestScore,
///     Actions = [
///         new UtilityAction
///         {
///             Name = "Attack",
///             Action = new AttackAction(),
///             Considerations = [
///                 new Consideration { Input = new DistanceInput(), Curve = new LinearCurve { Slope = -1, YShift = 1 } }
///             ]
///         },
///         new UtilityAction
///         {
///             Name = "Patrol",
///             Action = new PatrolAction(),
///             Weight = 0.3f // Default fallback
///         }
///     ]
/// };
/// </code>
/// </example>
public sealed class UtilityAI
{
    /// <summary>
    /// Gets or sets the name of this utility AI brain.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the available actions.
    /// </summary>
    public List<UtilityAction> Actions { get; set; } = [];

    /// <summary>
    /// Gets or sets the minimum score required to consider an action.
    /// </summary>
    /// <remarks>
    /// Actions scoring below this threshold are ignored.
    /// </remarks>
    public float SelectionThreshold { get; set; }

    /// <summary>
    /// Gets or sets how actions are selected from candidates.
    /// </summary>
    public UtilitySelectionMode SelectionMode { get; set; } = UtilitySelectionMode.HighestScore;

    /// <summary>
    /// Gets or sets the number of top actions to consider when using <see cref="UtilitySelectionMode.TopN"/>.
    /// </summary>
    public int TopNCount { get; set; } = 3;

    /// <summary>
    /// Validates the utility AI definition.
    /// </summary>
    /// <returns>An error message if validation fails; otherwise, null.</returns>
    public string? Validate()
    {
        if (Actions.Count == 0)
        {
            return "Utility AI must have at least one action.";
        }

        return null;
    }

    /// <summary>
    /// Scores all actions and selects the best one according to the selection mode.
    /// </summary>
    /// <param name="entity">The entity making the decision.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>The selected action, or null if no action meets the threshold.</returns>
    public UtilityAction? SelectAction(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Actions.Count == 0)
        {
            return null;
        }

        // Score all actions
        var scoredActions = new List<(UtilityAction action, float score)>(Actions.Count);

        foreach (var action in Actions)
        {
            var score = action.CalculateScore(entity, blackboard, world);

            if (score >= SelectionThreshold)
            {
                scoredActions.Add((action, score));
            }
        }

        if (scoredActions.Count == 0)
        {
            return null;
        }

        // Select based on mode
        return SelectionMode switch
        {
            UtilitySelectionMode.HighestScore => SelectHighestScore(scoredActions),
            UtilitySelectionMode.WeightedRandom => SelectWeightedRandom(scoredActions),
            UtilitySelectionMode.TopN => SelectTopN(scoredActions),
            _ => SelectHighestScore(scoredActions)
        };
    }

    private static UtilityAction SelectHighestScore(List<(UtilityAction action, float score)> scoredActions)
    {
        var best = scoredActions[0];

        for (var i = 1; i < scoredActions.Count; i++)
        {
            if (scoredActions[i].score > best.score)
            {
                best = scoredActions[i];
            }
        }

        return best.action;
    }

    private static UtilityAction SelectWeightedRandom(List<(UtilityAction action, float score)> scoredActions)
    {
        var totalScore = 0f;
        foreach (var (_, score) in scoredActions)
        {
            totalScore += score;
        }

        var random = Random.Shared.NextSingle() * totalScore;
        var cumulative = 0f;

        foreach (var (action, score) in scoredActions)
        {
            cumulative += score;
            if (random <= cumulative)
            {
                return action;
            }
        }

        // Fallback (shouldn't happen)
        return scoredActions[^1].action;
    }

    private UtilityAction SelectTopN(List<(UtilityAction action, float score)> scoredActions)
    {
        // Sort by score descending
        scoredActions.Sort((a, b) => b.score.CompareTo(a.score));

        // Take top N
        var topCount = Math.Min(TopNCount, scoredActions.Count);
        var selectedIndex = Random.Shared.Next(topCount);

        return scoredActions[selectedIndex].action;
    }
}
