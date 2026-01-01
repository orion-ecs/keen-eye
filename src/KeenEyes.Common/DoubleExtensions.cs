using System;

namespace KeenEyes.Common;

/// <summary>
/// Extension methods for <see cref="double"/> providing tolerance-based comparisons.
/// </summary>
/// <remarks>
/// Direct equality comparisons (==) with floating-point numbers can be unreliable due to
/// precision issues. These methods provide well-established idioms for comparing doubles
/// using a small tolerance (epsilon).
/// </remarks>
public static class DoubleExtensions
{
    /// <summary>
    /// Default epsilon value for double comparisons.
    /// </summary>
    /// <remarks>
    /// This value (1e-9) provides higher precision than float comparisons,
    /// suitable for most scientific and game development scenarios.
    /// For specific use cases requiring different precision, use the overload
    /// that accepts a custom epsilon.
    /// </remarks>
    public const double DefaultEpsilon = 1e-9;

    /// <summary>
    /// Determines whether the double value is approximately zero within the default tolerance.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the absolute value is less than <see cref="DefaultEpsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// double threshold = 0.0000000001;
    /// if (threshold.IsApproximatelyZero())
    /// {
    ///     // Treat as zero
    /// }
    /// </code>
    /// </example>
    public static bool IsApproximatelyZero(this double value)
    {
        return Math.Abs(value) < DefaultEpsilon;
    }

    /// <summary>
    /// Determines whether the double value is approximately zero within the specified tolerance.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="epsilon">The tolerance for the comparison. Must be positive.</param>
    /// <returns><c>true</c> if the absolute value is less than <paramref name="epsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// double threshold = 0.0001;
    /// const double customEpsilon = 1e-3;
    /// if (threshold.IsApproximatelyZero(customEpsilon))
    /// {
    ///     // Treat as zero with custom precision
    /// }
    /// </code>
    /// </example>
    public static bool IsApproximatelyZero(this double value, double epsilon)
    {
        return Math.Abs(value) < epsilon;
    }

    /// <summary>
    /// Determines whether two double values are approximately equal within the default tolerance.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="other">The second value to compare.</param>
    /// <returns><c>true</c> if the absolute difference is less than <see cref="DefaultEpsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// double a = 1.0;
    /// double b = 1.0000000001;
    /// if (a.ApproximatelyEquals(b))
    /// {
    ///     // Values are considered equal
    /// }
    /// </code>
    /// </example>
    public static bool ApproximatelyEquals(this double value, double other)
    {
        return Math.Abs(value - other) < DefaultEpsilon;
    }

    /// <summary>
    /// Determines whether two double values are approximately equal within the specified tolerance.
    /// </summary>
    /// <param name="value">The first value to compare.</param>
    /// <param name="other">The second value to compare.</param>
    /// <param name="epsilon">The tolerance for the comparison. Must be positive.</param>
    /// <returns><c>true</c> if the absolute difference is less than <paramref name="epsilon"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// double a = 1.0;
    /// double b = 1.001;
    /// const double customEpsilon = 0.01;
    /// if (a.ApproximatelyEquals(b, customEpsilon))
    /// {
    ///     // Values are considered equal with custom precision
    /// }
    /// </code>
    /// </example>
    public static bool ApproximatelyEquals(this double value, double other, double epsilon)
    {
        return Math.Abs(value - other) < epsilon;
    }
}
