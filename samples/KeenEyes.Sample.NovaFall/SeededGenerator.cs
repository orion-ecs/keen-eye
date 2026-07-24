namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// A tiny deterministic pseudo-random generator based on SplitMix64.
/// </summary>
/// <remarks>
/// <para>
/// NOVAFALL derives all of its procedural content (floor gap positions and widths)
/// from this generator so that a run is a pure function of its seed: the same seed
/// always produces the same shaft, on any machine, with or without a window. That
/// property powers the headless <c>--simulate</c> determinism check and, in a later
/// phase, replay verification.
/// </para>
/// <para>
/// The repo analyzer (KEEN030) forbids <c>System.Random</c> outside of
/// <c>World.Next*</c>; a self-contained SplitMix64 struct is the established pattern
/// for content generation that must be replayable from an explicit seed.
/// </para>
/// </remarks>
/// <param name="seed">The seed value. Equal seeds produce identical sequences.</param>
public struct SeededGenerator(ulong seed)
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;

    private ulong state = seed;

    /// <summary>
    /// Creates a generator for a specific floor of a run.
    /// </summary>
    /// <remarks>
    /// The stream depends only on the run seed and the floor index, never on spawn
    /// order or elapsed time, which is what makes floor generation window-independent.
    /// </remarks>
    /// <param name="seed">The run seed from <see cref="RunConfig"/>.</param>
    /// <param name="floorIndex">The sequential index of the floor.</param>
    /// <returns>A generator whose sequence is unique to (seed, floorIndex).</returns>
    public static SeededGenerator ForFloor(ulong seed, int floorIndex)
        => new(Mix(seed + ((ulong)floorIndex + 1) * Gamma));

    /// <summary>
    /// Derives the seed for the next run from the current one, so successive runs
    /// get fresh layouts without any wall-clock or <c>System.Random</c> dependency.
    /// </summary>
    /// <param name="seed">The current run seed.</param>
    /// <returns>A well-mixed successor seed.</returns>
    public static ulong NextSeed(ulong seed) => Mix(seed + Gamma);

    /// <summary>
    /// Returns the next value in the sequence as a float in [0, 1).
    /// </summary>
    /// <returns>A uniformly distributed float in [0, 1).</returns>
    public float NextFloat()
    {
        // Take the top 24 bits so the value fits a float mantissa exactly.
        return (NextUInt64() >> 40) * (1f / (1 << 24));
    }

    /// <summary>
    /// Returns the next value in the sequence as a float in [min, max).
    /// </summary>
    /// <param name="min">Inclusive lower bound.</param>
    /// <param name="max">Exclusive upper bound.</param>
    /// <returns>A uniformly distributed float in [min, max).</returns>
    public float NextRange(float min, float max) => min + NextFloat() * (max - min);

    private ulong NextUInt64()
    {
        state += Gamma;
        return Mix(state);
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
