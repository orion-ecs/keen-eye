namespace KeenEyes.AI.Utility;

/// <summary>
/// Represents a single factor in utility scoring.
/// </summary>
/// <remarks>
/// <para>
/// A consideration combines an input source with a response curve to produce
/// a utility score. Multiple considerations are multiplied together to determine
/// the final action score.
/// </para>
/// <para>
/// The input provides a normalized value (0-1), which is then transformed by
/// the response curve to produce the consideration's contribution to the score.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // "Prefer attacking when closer" consideration
/// var distanceConsideration = new Consideration
/// {
///     Name = "Distance to Target",
///     Input = new DistanceInput { MaxDistance = 10f },
///     Curve = new LinearCurve { Slope = -1f, YShift = 1f } // Closer = higher score
/// };
/// </code>
/// </example>
public sealed class Consideration
{
    /// <summary>
    /// Gets or sets the name of this consideration.
    /// </summary>
    /// <remarks>
    /// Used for debugging and visualization.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input source.
    /// </summary>
    public IConsiderationInput? Input { get; set; }

    /// <summary>
    /// Gets or sets the response curve.
    /// </summary>
    public ResponseCurve? Curve { get; set; }

    /// <summary>
    /// Gets or sets the last raw input value (for debugging).
    /// </summary>
    /// <param name="blackboard">The blackboard of the entity whose evaluation to read.</param>
    /// <returns>The raw input value from the entity's most recent evaluation.</returns>
    public float GetLastInputValue(Blackboard blackboard)
        => blackboard.GetMemory<ConsiderationMemory>(this).LastInputValue;

    /// <summary>
    /// Gets or sets the last output value (for debugging).
    /// </summary>
    /// <param name="blackboard">The blackboard of the entity whose evaluation to read.</param>
    /// <returns>The curved output score from the entity's most recent evaluation.</returns>
    public float GetLastOutputValue(Blackboard blackboard)
        => blackboard.GetMemory<ConsiderationMemory>(this).LastOutputValue;

    /// <summary>
    /// Evaluates this consideration for the given entity.
    /// </summary>
    /// <param name="entity">The entity being evaluated.</param>
    /// <param name="blackboard">The blackboard with shared state.</param>
    /// <param name="world">The ECS world.</param>
    /// <returns>The consideration's score (typically 0-1).</returns>
    public float Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        var memory = blackboard.GetMemory<ConsiderationMemory>(this);
        if (Input == null || Curve == null)
        {
            memory.LastInputValue = 1f;
            memory.LastOutputValue = 1f;
            return 1f;
        }

        memory.LastInputValue = Input.GetValue(entity, blackboard, world);
        memory.LastOutputValue = Curve.Evaluate(memory.LastInputValue);
        return memory.LastOutputValue;
    }

    /// <summary>
    /// Per-entity evaluation memory for <see cref="Consideration"/>.
    /// </summary>
    internal sealed class ConsiderationMemory
    {
        public float LastInputValue { get; set; }
        public float LastOutputValue { get; set; }
    }
}
