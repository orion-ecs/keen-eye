using KeenEyes.Logging;

namespace KeenEyes.Testing.Logging;

/// <summary>
/// Fluent assertions for log verification.
/// </summary>
public static class LogAssertions
{
    /// <summary>
    /// Asserts that at least one message was logged at the specified level.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="level">The log level to check for.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no message was logged at the level.</exception>
    public static MockLogProvider ShouldHaveLogged(this MockLogProvider provider, LogLevel level)
    {
        if (!provider.Entries.Any(e => e.Level == level))
        {
            throw new AssertionException($"Expected at least one log entry at level {level}, but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that at least one error was logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no error was logged.</exception>
    public static MockLogProvider ShouldHaveLoggedError(this MockLogProvider provider)
    {
        if (!provider.Errors.Any())
        {
            throw new AssertionException("Expected at least one error log entry, but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that at least one warning was logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no warning was logged.</exception>
    public static MockLogProvider ShouldHaveLoggedWarning(this MockLogProvider provider)
    {
        if (!provider.Warnings.Any())
        {
            throw new AssertionException("Expected at least one warning log entry, but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that a message containing the specified text was logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="text">The text to search for (case-insensitive).</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching message was found.</exception>
    public static MockLogProvider ShouldHaveLoggedContaining(this MockLogProvider provider, string text)
    {
        if (!provider.Entries.Any(e => e.Message.Contains(text, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AssertionException($"Expected a log entry containing '{text}', but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that a message matching the predicate was logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching entry was found.</exception>
    public static MockLogProvider ShouldHaveLoggedMatching(this MockLogProvider provider, Func<LogEntry, bool> predicate)
    {
        if (!provider.Entries.Any(predicate))
        {
            throw new AssertionException("Expected a log entry matching the predicate, but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no errors were logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when errors were logged.</exception>
    public static MockLogProvider ShouldNotHaveLoggedErrors(this MockLogProvider provider)
    {
        var errors = provider.Errors.ToList();
        if (errors.Count > 0)
        {
            var messages = string.Join(", ", errors.Take(3).Select(e => $"'{e.Message}'"));
            throw new AssertionException($"Expected no error log entries, but found {errors.Count}: {messages}");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no warnings were logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when warnings were logged.</exception>
    public static MockLogProvider ShouldNotHaveLoggedWarnings(this MockLogProvider provider)
    {
        var warnings = provider.Warnings.ToList();
        if (warnings.Count > 0)
        {
            var messages = string.Join(", ", warnings.Take(3).Select(e => $"'{e.Message}'"));
            throw new AssertionException($"Expected no warning log entries, but found {warnings.Count}: {messages}");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that exactly the specified number of entries were logged at the given level.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="level">The log level to count.</param>
    /// <param name="count">The expected count.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockLogProvider ShouldHaveLoggedExactly(this MockLogProvider provider, LogLevel level, int count)
    {
        var actual = provider.Entries.Count(e => e.Level == level);
        if (actual != count)
        {
            throw new AssertionException($"Expected exactly {count} log entries at level {level}, but found {actual}.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that at least the specified number of entries were logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="count">The minimum expected count.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when fewer entries were logged.</exception>
    public static MockLogProvider ShouldHaveLoggedAtLeast(this MockLogProvider provider, int count)
    {
        if (provider.Entries.Count < count)
        {
            throw new AssertionException($"Expected at least {count} log entries, but found {provider.Entries.Count}.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that a message was logged for the specified category.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="category">The category to check for.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no entry was found for the category.</exception>
    public static MockLogProvider ShouldHaveLoggedForCategory(this MockLogProvider provider, string category)
    {
        if (!provider.Entries.Any(e => e.Category == category))
        {
            throw new AssertionException($"Expected a log entry for category '{category}', but none were found.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no entries were logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when entries were logged.</exception>
    public static MockLogProvider ShouldBeEmpty(this MockLogProvider provider)
    {
        if (provider.Entries.Count > 0)
        {
            throw new AssertionException($"Expected no log entries, but found {provider.Entries.Count}.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no fatal errors were logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when fatal errors were logged.</exception>
    public static MockLogProvider ShouldNotHaveLoggedFatals(this MockLogProvider provider)
    {
        var fatals = provider.Fatals.ToList();
        if (fatals.Count > 0)
        {
            var messages = string.Join(", ", fatals.Take(3).Select(e => $"'{e.Message}'"));
            throw new AssertionException($"Expected no fatal log entries, but found {fatals.Count}: {messages}");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that a log entry with a specific property was logged.
    /// </summary>
    /// <param name="provider">The mock log provider.</param>
    /// <param name="propertyName">The property name to check for.</param>
    /// <param name="propertyValue">The expected property value (optional).</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching entry was found.</exception>
    public static MockLogProvider ShouldHaveLoggedWithProperty(this MockLogProvider provider, string propertyName, object? propertyValue = null)
    {
        var matches = provider.Entries.Where(e =>
            e.Properties != null &&
            e.Properties.ContainsKey(propertyName) &&
            (propertyValue == null || Equals(e.Properties[propertyName], propertyValue)));

        if (!matches.Any())
        {
            var message = propertyValue == null
                ? $"Expected a log entry with property '{propertyName}', but none were found."
                : $"Expected a log entry with property '{propertyName}' = '{propertyValue}', but none were found.";
            throw new AssertionException(message);
        }

        return provider;
    }
}
