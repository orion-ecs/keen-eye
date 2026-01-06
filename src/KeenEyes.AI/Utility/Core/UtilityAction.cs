using KeenEyes.Common;

namespace KeenEyes.AI.Utility;

/// <summary>
/// Represents an action that can be scored and selected by utility AI.
/// </summary>
/// <remarks>
/// <para>
/// A utility action combines an <see cref="IAIAction"/> with a set of
/// <see cref="Consideration"/>s that determine when the action is appropriate.
/// </para>
/// <para>
/// The score is calculated by multiplying all consideration outputs together,
/// scaled by the base weight. Actions with higher scores are more likely to
/// be selected.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var attackAction = new UtilityAction
/// {
///     Name = "Attack",
///     Action = new AttackAction(),
///     Weight = 1f,
///     Considerations = [
///         new Consideration
///         {
///             Name = "Target in range",
///             Input = new DistanceInput { MaxDistance = 10f },
///             Curve = new LinearCurve { Slope = -1f, YShift = 1f }
///         },
///         new Consideration
///         {
///             Name = "Have enough health",
///             Input = new HealthInput(),
///             Curve = new LogisticCurve { Midpoint = 0.3f, Steepness = 10f }
///         }
///     ]
/// };
/// </code>
/// </example>
public sealed class UtilityAction
{
    /// <summary>
    /// Gets or sets the name of this action.
    /// </summary>
    /// <remarks>
    /// Used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to execute when selected.
    /// </summary>
    public IAIAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the considerations that determine this action's score.
    /// </summary>
    public List<Consideration> Considerations { get; set; } = [];

    /// <summary>
    /// Gets or sets the base weight of this action.
    /// </summary>
    /// <remarks>
    /// The final score is multiplied by this weight. Use to bias certain actions.
    /// </remarks>
    public float Weight { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the last calculated score (for debugging).
    /// </summary>
    public float LastScore { get; private set; }

    /// <summary>
    /// Calculates the utility score for this action.
    /// </summary>
    /// <param name="entity">The entity being evaluated.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>The action's utility score.</returns>
    public float CalculateScore(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Considerations.Count == 0)
        {
            LastScore = Weight;
            return Weight;
        }

        var score = Weight;

        foreach (var consideration in Considerations)
        {
            var considerationScore = consideration.Evaluate(entity, blackboard, world);
            score *= considerationScore;

            // Early exit if score becomes negligible
            if (score.IsApproximatelyZero())
            {
                LastScore = 0f;
                return 0f;
            }
        }

        // Apply compensation factor to prevent bias against actions with more considerations
        // This ensures that actions with more considerations aren't unfairly penalized
        // by the multiplicative scoring (where each factor < 1 reduces the total)
        var modificationFactor = 1f - (1f / Considerations.Count);
        var makeUpValue = (1f - score) * modificationFactor;
        score += makeUpValue * score;

        LastScore = score;
        return score;
    }
}
