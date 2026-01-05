using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace KeenEyes.Localization;

/// <summary>
/// A basic message formatter supporting positional ({0}, {1}) and named ({name}) placeholders.
/// </summary>
/// <remarks>
/// <para>
/// This is the default formatter used when no specific formatter is configured.
/// It provides simple string interpolation without ICU MessageFormat features
/// like pluralization or select expressions.
/// </para>
/// <para>
/// Placeholder syntax:
/// </para>
/// <list type="bullet">
/// <item><description><c>{0}</c>, <c>{1}</c> - Positional placeholders (zero-indexed)</description></item>
/// <item><description><c>{name}</c>, <c>{count}</c> - Named placeholders (property names)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var formatter = new SimpleFormatter();
///
/// // Positional placeholders
/// var result = formatter.Format("Hello, {0}!", new object[] { "World" }, Locale.EnglishUS);
/// // Returns: "Hello, World!"
///
/// // Named placeholders with anonymous object
/// var result = formatter.Format("Hello, {name}!", new { name = "World" }, Locale.EnglishUS);
/// // Returns: "Hello, World!"
///
/// // Named placeholders with dictionary
/// var args = new Dictionary&lt;string, object?&gt; { ["name"] = "World" };
/// var result = formatter.Format("Hello, {name}!", args, Locale.EnglishUS);
/// // Returns: "Hello, World!"
/// </code>
/// </example>
public sealed partial class SimpleFormatter : IMessageFormatter
{
    /// <summary>
    /// Gets the singleton instance of <see cref="SimpleFormatter"/>.
    /// </summary>
    public static SimpleFormatter Instance { get; } = new();

    /// <inheritdoc />
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object?> instead.")]
    public string Format(string template, object? args, Locale locale)
    {
        if (!TryFormat(template, args, locale, out var result))
        {
            return template;
        }
        return result!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object?> instead.")]
    public bool TryFormat(string template, object? args, Locale locale, out string? result)
    {
        ArgumentNullException.ThrowIfNull(template);

        if (args is null)
        {
            result = template;
            return true;
        }

        try
        {
            // Handle array/positional arguments
            if (args is object?[] positionalArgs)
            {
                result = string.Format(
                    CultureInfo.GetCultureInfo(locale.Code),
                    template,
                    positionalArgs);
                return true;
            }

            // Handle dictionary arguments
            if (args is IDictionary<string, object?> dictArgs)
            {
                result = FormatWithDictionary(template, dictArgs, locale);
                return true;
            }

            // Handle anonymous objects or POCOs by converting to dictionary
            var argsDict = ConvertToDictionary(args);
            result = FormatWithDictionary(template, argsDict, locale);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    private static string FormatWithDictionary(
        string template,
        IDictionary<string, object?> args,
        Locale locale)
    {
        var culture = CultureInfo.GetCultureInfo(locale.Code);

        return NamedPlaceholderRegex().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (args.TryGetValue(name, out var value))
            {
                return value switch
                {
                    IFormattable formattable => formattable.ToString(null, culture),
                    null => string.Empty,
                    _ => value.ToString() ?? string.Empty
                };
            }
            // Keep placeholder if no matching argument
            return match.Value;
        });
    }

    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object?> instead.")]
    private static Dictionary<string, object?> ConvertToDictionary(object obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        var type = obj.GetType();

        foreach (var prop in type.GetProperties())
        {
            if (prop.CanRead)
            {
                dict[prop.Name] = prop.GetValue(obj);
            }
        }

        return dict;
    }

    [GeneratedRegex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled)]
    private static partial Regex NamedPlaceholderRegex();
}
