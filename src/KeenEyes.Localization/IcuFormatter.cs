using System.Diagnostics.CodeAnalysis;
using Jeffijoe.MessageFormat;

namespace KeenEyes.Localization;

/// <summary>
/// A message formatter implementing ICU MessageFormat for complex localization patterns.
/// </summary>
/// <remarks>
/// <para>
/// ICU MessageFormat supports advanced localization features:
/// </para>
/// <list type="bullet">
/// <item><description><b>Pluralization</b> - Handle singular/plural forms correctly per locale</description></item>
/// <item><description><b>Gender</b> - Select text based on grammatical gender</description></item>
/// <item><description><b>Select</b> - General-purpose conditional text selection</description></item>
/// </list>
/// <para>
/// This implementation uses the MessageFormat.NET library internally.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var formatter = new IcuFormatter();
///
/// // Pluralization
/// var pattern = "{count, plural, =0 {No items} =1 {One item} other {# items}}";
/// formatter.Format(pattern, new { count = 0 }, Locale.EnglishUS);  // "No items"
/// formatter.Format(pattern, new { count = 1 }, Locale.EnglishUS);  // "One item"
/// formatter.Format(pattern, new { count = 5 }, Locale.EnglishUS);  // "5 items"
///
/// // Gender selection
/// var pattern = "{gender, select, male {He} female {She} other {They}} found treasure!";
/// formatter.Format(pattern, new { gender = "male" }, Locale.EnglishUS);    // "He found treasure!"
/// formatter.Format(pattern, new { gender = "female" }, Locale.EnglishUS);  // "She found treasure!"
///
/// // Nested patterns
/// var pattern = "{gender, select, male {He has {count, plural, =1 {# coin} other {# coins}}} female {She has {count, plural, =1 {# coin} other {# coins}}} other {They have {count, plural, =1 {# coin} other {# coins}}}}";
/// formatter.Format(pattern, new { gender = "male", count = 5 }, Locale.EnglishUS);  // "He has 5 coins"
/// </code>
/// </example>
public sealed class IcuFormatter : IMessageFormatter
{
    /// <summary>
    /// Gets the singleton instance of <see cref="IcuFormatter"/>.
    /// </summary>
    public static IcuFormatter Instance { get; } = new();

    /// <inheritdoc />
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object> instead.")]
    public string Format(string template, object? args, Locale locale)
    {
        if (!TryFormat(template, args, locale, out var result))
        {
            return template;
        }
        return result!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object> instead.")]
    public bool TryFormat(string template, object? args, Locale locale, out string? result)
    {
        ArgumentNullException.ThrowIfNull(template);

        try
        {
            var formatter = new MessageFormatter(useCache: true, locale: locale.Code);

            var argsDict = args switch
            {
                null => new Dictionary<string, object?>(),
                IReadOnlyDictionary<string, object?> roDict => roDict,
                IDictionary<string, object?> dict => new Dictionary<string, object?>(dict),
                _ => ConvertToDictionary(args)
            };

            result = formatter.FormatMessage(template, argsDict);
            return true;
        }
        catch (Exception)
        {
            result = null;
            return false;
        }
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
}
