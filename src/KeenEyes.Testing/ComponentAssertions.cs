using System.Linq.Expressions;

namespace KeenEyes.Testing;

/// <summary>
/// Fluent assertion extensions for component value validation in tests.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide a fluent, readable syntax for asserting component values
/// in unit tests. They work directly on component structs and support both full
/// equality comparison and field-level assertions.
/// </para>
/// <para>
/// All comparisons are reflection-free and AOT-compatible, using expression trees
/// for field access and IEquatable for equality when available.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var position = world.Get&lt;Position&gt;(entity);
///
/// position.ShouldEqual(new Position { X = 10, Y = 20 });
/// position.ShouldMatch(p =&gt; p.X &gt; 0 &amp;&amp; p.Y &gt; 0);
/// position.ShouldHaveField(p =&gt; p.X, 10);
/// </code>
/// </example>
public static class ComponentAssertions
{
    /// <summary>
    /// Asserts that the component equals the expected value.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="expected">The expected component value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the values are not equal.</exception>
    /// <remarks>
    /// Uses <see cref="IEquatable{T}.Equals(T)"/> if available, otherwise falls back to
    /// <see cref="object.Equals(object)"/>.
    /// </remarks>
    public static T ShouldEqual<T>(this T actual, T expected, string? because = null)
        where T : struct, IComponent
    {
        bool areEqual;

        if (actual is IEquatable<T> equatable)
        {
            areEqual = equatable.Equals(expected);
        }
        else
        {
            areEqual = actual.Equals(expected);
        }

        if (!areEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to equal {expected}{reason}, but was {actual}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the component does not equal the specified value.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="unexpected">The value the component should not equal.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the values are equal.</exception>
    public static T ShouldNotEqual<T>(this T actual, T unexpected, string? because = null)
        where T : struct, IComponent
    {
        bool areEqual;

        if (actual is IEquatable<T> equatable)
        {
            areEqual = equatable.Equals(unexpected);
        }
        else
        {
            areEqual = actual.Equals(unexpected);
        }

        if (areEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to not equal {unexpected}{reason}, but it did.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the component matches the specified predicate.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="predicate">The predicate the component should satisfy.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the predicate returns false.</exception>
    /// <example>
    /// <code>
    /// position.ShouldMatch(p =&gt; p.X &gt;= 0 &amp;&amp; p.Y &gt;= 0, "position should be in positive quadrant");
    /// </code>
    /// </example>
    public static T ShouldMatch<T>(this T actual, Func<T, bool> predicate, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!predicate(actual))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to match predicate{reason}, but it did not. Actual value: {actual}");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the component does not match the specified predicate.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="predicate">The predicate the component should not satisfy.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the predicate returns true.</exception>
    public static T ShouldNotMatch<T>(this T actual, Func<T, bool> predicate, string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (predicate(actual))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to not match predicate{reason}, but it did. Actual value: {actual}");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that a specific field of the component has the expected value.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TField">The field type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="fieldSelector">Expression selecting the field to check.</param>
    /// <param name="expectedValue">The expected field value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the field value does not match.</exception>
    /// <example>
    /// <code>
    /// position.ShouldHaveField(p =&gt; p.X, 10);
    /// position.ShouldHaveField(p =&gt; p.Y, 20, "Y should match expected value");
    /// </code>
    /// </example>
    public static T ShouldHaveField<T, TField>(
        this T actual,
        Expression<Func<T, TField>> fieldSelector,
        TField expectedValue,
        string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var compiled = fieldSelector.Compile();
        var actualValue = compiled(actual);

        var fieldName = GetMemberName(fieldSelector);
        bool areEqual = EqualityComparer<TField>.Default.Equals(actualValue, expectedValue);

        if (!areEqual)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected {typeof(T).Name}.{fieldName} to equal {expectedValue}{reason}, but was {actualValue}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that a specific field of the component matches the predicate.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TField">The field type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="fieldSelector">Expression selecting the field to check.</param>
    /// <param name="predicate">The predicate the field value should satisfy.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the field value does not match the predicate.</exception>
    /// <example>
    /// <code>
    /// position.ShouldHaveFieldMatching(p =&gt; p.X, x =&gt; x &gt; 0 &amp;&amp; x &lt; 100);
    /// </code>
    /// </example>
    public static T ShouldHaveFieldMatching<T, TField>(
        this T actual,
        Expression<Func<T, TField>> fieldSelector,
        Func<TField, bool> predicate,
        string? because = null)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(fieldSelector);
        ArgumentNullException.ThrowIfNull(predicate);

        var compiled = fieldSelector.Compile();
        var actualValue = compiled(actual);

        var fieldName = GetMemberName(fieldSelector);

        if (!predicate(actualValue))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected {typeof(T).Name}.{fieldName} to match predicate{reason}, but it did not. Actual value: {actualValue}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that a specific field of the component is in the expected range (inclusive).
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TField">The field type, must be comparable.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="fieldSelector">Expression selecting the field to check.</param>
    /// <param name="min">The minimum expected value (inclusive).</param>
    /// <param name="max">The maximum expected value (inclusive).</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the field value is not in range.</exception>
    /// <example>
    /// <code>
    /// health.ShouldHaveFieldInRange(h =&gt; h.Current, 0, 100);
    /// </code>
    /// </example>
    public static T ShouldHaveFieldInRange<T, TField>(
        this T actual,
        Expression<Func<T, TField>> fieldSelector,
        TField min,
        TField max,
        string? because = null)
        where T : struct, IComponent
        where TField : IComparable<TField>
    {
        ArgumentNullException.ThrowIfNull(fieldSelector);

        var compiled = fieldSelector.Compile();
        var actualValue = compiled(actual);

        var fieldName = GetMemberName(fieldSelector);

        if (actualValue.CompareTo(min) < 0 || actualValue.CompareTo(max) > 0)
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected {typeof(T).Name}.{fieldName} to be in range [{min}, {max}]{reason}, but was {actualValue}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the component is the default value for its type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component is not the default value.</exception>
    public static T ShouldBeDefault<T>(this T actual, string? because = null)
        where T : struct, IComponent
    {
        if (!actual.Equals(default(T)))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to be default{reason}, but was {actual}.");
        }

        return actual;
    }

    /// <summary>
    /// Asserts that the component is not the default value for its type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="actual">The actual component value.</param>
    /// <param name="because">Optional reason for the assertion.</param>
    /// <returns>The actual component for method chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the component is the default value.</exception>
    public static T ShouldNotBeDefault<T>(this T actual, string? because = null)
        where T : struct, IComponent
    {
        if (actual.Equals(default(T)))
        {
            var reason = string.IsNullOrEmpty(because) ? "" : $" because {because}";
            throw new AssertionException(
                $"Expected component {typeof(T).Name} to not be default{reason}, but it was.");
        }

        return actual;
    }

    private static string GetMemberName<T, TField>(Expression<Func<T, TField>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression unaryMemberExpression)
        {
            return unaryMemberExpression.Member.Name;
        }

        return "Field";
    }
}
