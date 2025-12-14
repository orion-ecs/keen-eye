using System;

namespace KeenEyes.Common;

/// <summary>
/// Extension methods for <see cref="float"/> providing tolerance-based comparisons.
/// </summary>
/// <remarks>
/// Direct equality comparisons (==) with floating-point numbers can be unreliable due to
/// precision issues. These methods provide well-established idioms for comparing floats
/// using a small tolerance (epsilon).
/// </remarks>
public static class FloatExtensions
{
    /// <summary>
    /// Default epsilon value for float comparisons.
    /// </summary>
    /// <remarks>
    /// This value (1e-6f) is suitable for most game development scenarios.
    /// For specific use cases requiring different precision, use the overload
    /// that accepts a custom epsilon.
    /// </remarks>
    public const float DefaultEpsilon = 1e-6f;

    /// <summary>
    /// Determines whether the float value is approximately zero within the default tolerance.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the absolute value is less than <see cref="DefaultEpsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// float threshold = 0.0000001f;
    /// if (threshold.IsApproximatelyZero())
    /// {
    ///     // Treat as zero
    /// }
    /// </code>
    /// </example>
    public static bool IsApproximatelyZero(this float value)
    {
        return Math.Abs(value) < DefaultEpsilon;
    }

    /// <summary>
    /// Determines whether the float value is approximately zero within the specified tolerance.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="epsilon">The tolerance for the comparison. Must be positive.</param>
    /// <returns><c>true</c> if the absolute value is less than <paramref name="epsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// float threshold = 0.0001f;
    /// const float customEpsilon = 1e-3f;
    /// if (threshold.IsApproximatelyZero(customEpsilon))
    /// {
    ///     // Treat as zero with custom precision
    /// }
    /// </code>
    /// </example>
    public static bool IsApproximatelyZero(this float value, float epsilon)
    {
        return Math.Abs(value) < epsilon;
    }

    /// <summary>
    /// Determines whether two float values are approximately equal within the default tolerance.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="other">The second value to compare.</param>
    /// <returns><c>true</c> if the absolute difference is less than <see cref="DefaultEpsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// float a = 1.0f;
    /// float b = 1.0000001f;
    /// if (a.ApproximatelyEquals(b))
    /// {
    ///     // Values are considered equal
    /// }
    /// </code>
    /// </example>
    public static bool ApproximatelyEquals(this float value, float other)
    {
        return Math.Abs(value - other) < DefaultEpsilon;
    }

    /// <summary>
    /// Determines whether two float values are approximately equal within the specified tolerance.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="other">The second value to compare.</param>
    /// <param name="epsilon">The tolerance for the comparison. Must be positive.</param>
    /// <returns><c>true</c> if the absolute difference is less than <paramref name="epsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// float a = 1.0f;
    /// float b = 1.001f;
    /// const float customEpsilon = 0.01f;
    /// if (a.ApproximatelyEquals(b, customEpsilon))
    /// {
    ///     // Values are considered equal with custom precision
    /// }
    /// </code>
    /// </example>
    public static bool ApproximatelyEquals(this float value, float other, float epsilon)
    {
        return Math.Abs(value - other) < epsilon;
    }
}
