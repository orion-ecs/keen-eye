namespace KeenEyes.AI.Utility;

/// <summary>
/// Interface for consideration input sources that provide normalized values for utility scoring.
/// </summary>
/// <remarks>
/// <para>
/// Consideration inputs extract relevant data from the entity, blackboard, or world
/// and return a normalized value (typically 0-1) that is then processed by a response curve.
/// </para>
/// <para>
/// Common inputs include:
/// </para>
/// <list type="bullet">
/// <item><description>Distance to target (normalized by max range)</description></item>
/// <item><description>Health percentage</description></item>
/// <item><description>Time since last action</description></item>
/// <item><description>Blackboard values</description></item>
/// </list>
/// </remarks>
public interface IConsiderationInput
{
    /// <summary>
    /// Gets the normalized input value.
    /// </summary>
    /// <param name="entity">The entity being evaluated.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>A value typically in the range [0, 1], though some inputs may exceed this.</returns>
    float GetValue(Entity entity, Blackboard blackboard, IWorld world);
}
