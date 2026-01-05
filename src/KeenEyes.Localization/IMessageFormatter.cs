using System.Diagnostics.CodeAnalysis;

namespace KeenEyes.Localization;

/// <summary>
/// Defines a formatter for applying variable substitution and formatting to localized strings.
/// </summary>
/// <remarks>
/// <para>
/// Message formatters handle the transformation of template strings with placeholders
/// into final output strings by substituting variables.
/// </para>
/// <para>
/// Two implementations are provided:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="SimpleFormatter"/> - Basic positional and named placeholders using .NET format strings</description></item>
/// <item><description><see cref="IcuFormatter"/> - Full ICU MessageFormat support for pluralization, gender, and select expressions</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // SimpleFormatter example
/// var simple = new SimpleFormatter();
/// var result = simple.Format("Hello, {name}!", new { name = "World" }, Locale.EnglishUS);
/// // Returns: "Hello, World!"
///
/// // IcuFormatter example
/// var icu = new IcuFormatter();
/// var result = icu.Format("{count, plural, =0 {No items} =1 {One item} other {# items}}",
///     new { count = 5 }, Locale.EnglishUS);
/// // Returns: "5 items"
/// </code>
/// </example>
public interface IMessageFormatter
{
    /// <summary>
    /// Formats a template string by substituting the provided arguments.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="args">The arguments to substitute into the template.</param>
    /// <param name="locale">The locale to use for locale-sensitive formatting.</param>
    /// <returns>The formatted string with placeholders replaced by argument values.</returns>
    /// <remarks>
    /// <para>
    /// If formatting fails (e.g., due to malformed template or missing arguments),
    /// implementations should return the original template rather than throwing.
    /// </para>
    /// <para>
    /// When using anonymous objects as arguments, this method uses reflection which
    /// may not be compatible with Native AOT trimming. For AOT compatibility, use
    /// <see cref="Dictionary{TKey, TValue}"/> with string keys instead.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object?> instead.")]
    string Format(string template, object? args, Locale locale);

    /// <summary>
    /// Attempts to format a template string, returning success or failure.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="args">The arguments to substitute into the template.</param>
    /// <param name="locale">The locale to use for locale-sensitive formatting.</param>
    /// <param name="result">When successful, contains the formatted string.</param>
    /// <returns><c>true</c> if formatting succeeded; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// When using anonymous objects as arguments, this method uses reflection which
    /// may not be compatible with Native AOT trimming. For AOT compatibility, use
    /// <see cref="Dictionary{TKey, TValue}"/> with string keys instead.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object?> instead.")]
    bool TryFormat(string template, object? args, Locale locale, out string? result);
}
