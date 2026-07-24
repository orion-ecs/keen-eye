using System.Text.RegularExpressions;

namespace KeenEyes.Logging.Providers;

/// <summary>
/// Shared helper for matching log categories against wildcard patterns.
/// </summary>
/// <remarks>
/// Implements the wildcard semantics documented on <see cref="LogQuery.CategoryPattern"/>:
/// <c>*</c> matches any sequence of characters and <c>?</c> matches any single character.
/// Matching is anchored (whole-string) and case-insensitive. Shared by all
/// <see cref="ILogQueryable"/> providers so category filtering is consistent.
/// </remarks>
internal static class LogCategoryPattern
{
    /// <summary>
    /// Converts a wildcard category pattern into an anchored, case-insensitive regex.
    /// </summary>
    /// <param name="pattern">The wildcard pattern (supporting <c>*</c> and <c>?</c>).</param>
    /// <returns>A compiled regex that matches categories against the pattern.</returns>
    public static Regex ToRegex(string pattern)
    {
        // Escape regex special characters except * and ?
        var escaped = Regex.Escape(pattern);

        // Replace wildcard characters with regex equivalents
        // * matches any sequence of characters
        // ? matches any single character
        var regexPattern = escaped
            .Replace("\\*", ".*")
            .Replace("\\?", ".");

        // Timeout guards against pathological patterns (S6444); wildcard-derived
        // patterns are simple, so 100ms is far beyond any legitimate match time.
        return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    }
}
