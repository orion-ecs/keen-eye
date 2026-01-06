namespace KeenEyes.AI.Utility;

/// <summary>
/// Defines how the utility AI selects an action from scored candidates.
/// </summary>
public enum UtilitySelectionMode
{
    /// <summary>
    /// Always select the action with the highest score.
    /// </summary>
    /// <remarks>
    /// Most deterministic mode. The AI always picks the "best" action.
    /// Can lead to predictable behavior.
    /// </remarks>
    HighestScore,

    /// <summary>
    /// Select randomly, weighted by score.
    /// </summary>
    /// <remarks>
    /// Actions with higher scores are more likely to be selected, but lower-scored
    /// actions still have a chance. Creates more varied behavior.
    /// </remarks>
    WeightedRandom,

    /// <summary>
    /// Select randomly from the top N scoring actions.
    /// </summary>
    /// <remarks>
    /// Combines determinism (only considers high-scoring actions) with variation
    /// (random selection among top candidates).
    /// </remarks>
    TopN
}
