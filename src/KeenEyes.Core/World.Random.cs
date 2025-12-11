namespace KeenEyes;

/// <summary>
/// Random number generation support for the World.
/// </summary>
public sealed partial class World
{
    private readonly Random random;

    /// <summary>
    /// Gets a random integer between 0 (inclusive) and maxValue (exclusive).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>A 32-bit signed integer greater than or equal to 0, and less than maxValue.</returns>
    /// <exception cref="ArgumentOutOfRangeException">maxValue is less than 0.</exception>
    /// <remarks>
    /// <para>
    /// Uses the world's deterministic random number generator. If the world was created with a seed,
    /// this method will produce the same sequence of values across runs.
    /// </para>
    /// <para>
    /// Each world has its own isolated RNG state, ensuring deterministic behavior for replays and testing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Spawn enemy at random edge (0-3)
    /// int edge = world.NextInt(4);
    /// </code>
    /// </example>
    public int NextInt(int maxValue) => random.Next(maxValue);

    /// <summary>
    /// Gets a random integer between minValue (inclusive) and maxValue (exclusive).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
    /// <param name="maxValue">The exclusive upper bound of the random number returned.</param>
    /// <returns>
    /// A 32-bit signed integer greater than or equal to minValue and less than maxValue.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// minValue is greater than maxValue.
    /// </exception>
    /// <example>
    /// <code>
    /// // Random velocity between -10 and 10
    /// int velocity = world.NextInt(-10, 11);
    /// </code>
    /// </example>
    public int NextInt(int minValue, int maxValue) => random.Next(minValue, maxValue);

    /// <summary>
    /// Gets a random floating-point number between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    /// <returns>A single-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    /// <example>
    /// <code>
    /// // 30% chance of gravity
    /// bool hasGravity = world.NextFloat() &lt; 0.3f;
    ///
    /// // Random position in world
    /// float x = world.NextFloat() * worldWidth;
    /// float y = world.NextFloat() * worldHeight;
    /// </code>
    /// </example>
    public float NextFloat() => random.NextSingle();

    /// <summary>
    /// Gets a random double-precision floating-point number between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
    public double NextDouble() => random.NextDouble();

    /// <summary>
    /// Gets a random boolean value.
    /// </summary>
    /// <returns>True or false with equal probability.</returns>
    /// <example>
    /// <code>
    /// // Randomly choose between two options
    /// if (world.NextBool())
    /// {
    ///     entity.WithGravity(9.8f);
    /// }
    /// </code>
    /// </example>
    public bool NextBool() => random.Next(2) == 0;

    /// <summary>
    /// Returns a random boolean with the specified probability of being true.
    /// </summary>
    /// <param name="probability">The probability (0.0 to 1.0) that the result will be true.</param>
    /// <returns>True with the specified probability, false otherwise.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// probability is less than 0.0 or greater than 1.0.
    /// </exception>
    /// <example>
    /// <code>
    /// // 30% chance of gravity
    /// if (world.NextBool(0.3f))
    /// {
    ///     entity.WithGravity(9.8f);
    /// }
    /// </code>
    /// </example>
    public bool NextBool(float probability)
    {
        if (probability < 0.0f || probability > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0.0 and 1.0");
        }

        return NextFloat() < probability;
    }
}
