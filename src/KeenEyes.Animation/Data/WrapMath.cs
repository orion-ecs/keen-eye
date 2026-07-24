namespace KeenEyes.Animation.Data;

/// <summary>
/// Sign-safe time-wrapping helpers shared by the animation wrap modes.
/// </summary>
/// <remarks>
/// C#'s <c>%</c> operator preserves the sign of the dividend, so a naive
/// <c>time % duration</c> leaves negative (reverse-playback) times negative and never
/// folds them into the <c>[0, duration)</c> range. These helpers use a positive-modulo
/// formula so both forward and reverse times wrap correctly.
/// </remarks>
internal static class WrapMath
{
    /// <summary>
    /// Wraps <paramref name="time"/> into the half-open range <c>[0, length)</c>,
    /// handling negative times correctly.
    /// </summary>
    /// <param name="time">The (possibly negative or unbounded) time.</param>
    /// <param name="length">The positive length to wrap within.</param>
    /// <returns>The wrapped time in <c>[0, length)</c>, or 0 when length is not positive.</returns>
    public static float Repeat(float time, float length)
    {
        if (length <= 0f)
        {
            return 0f;
        }

        return ((time % length) + length) % length;
    }

    /// <summary>
    /// Folds a monotonic <paramref name="time"/> into a triangle (ping-pong) wave over
    /// <c>[0, length]</c>, reflecting at each boundary.
    /// </summary>
    /// <param name="time">The (possibly negative or unbounded) monotonic time.</param>
    /// <param name="length">The positive half-period (clip duration) to reflect within.</param>
    /// <returns>The reflected time in <c>[0, length]</c>, or 0 when length is not positive.</returns>
    public static float PingPong(float time, float length)
    {
        if (length <= 0f)
        {
            return 0f;
        }

        var folded = Repeat(time, length * 2f);
        return folded <= length ? folded : (length * 2f) - folded;
    }
}
