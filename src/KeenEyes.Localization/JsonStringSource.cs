using System.Text.Json;

namespace KeenEyes.Localization;

/// <summary>
/// A string source that loads translations from JSON files.
/// </summary>
/// <remarks>
/// <para>
/// JSON files should be simple key-value objects. Nested keys are flattened using
/// dot notation (e.g., "menu.start" from {"menu": {"start": "..."}}).
/// </para>
/// <para>
/// Example JSON format:
/// </para>
/// <code>
/// {
///     "menu.start": "Start Game",
///     "menu.quit": "Quit",
///     "dialog.greeting": "Hello, {0}!"
/// }
/// </code>
/// <para>
/// Or nested format (automatically flattened):
/// </para>
/// <code>
/// {
///     "menu": {
///         "start": "Start Game",
///         "quit": "Quit"
///     },
///     "dialog": {
///         "greeting": "Hello, {0}!"
///     }
/// }
/// </code>
/// </remarks>
/// <example>
/// <code>
/// // Load from file
/// var source = await JsonStringSource.FromFileAsync("locales/en-US.json", Locale.EnglishUS);
/// localization.AddSource(source);
///
/// // Load from string
/// var json = File.ReadAllText("locales/ja-JP.json");
/// var source = JsonStringSource.FromString(json, Locale.JapaneseJP);
/// </code>
/// </example>
public sealed class JsonStringSource : IStringSource
{
    private readonly Dictionary<Locale, Dictionary<string, string>> translations = [];

    private JsonStringSource()
    {
    }

    /// <inheritdoc />
    public IEnumerable<Locale> SupportedLocales => translations.Keys;

    /// <inheritdoc />
    public bool TryGetString(Locale locale, string key, out string? value)
    {
        if (translations.TryGetValue(locale, out var strings) &&
            strings.TryGetValue(key, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetKeys(Locale locale)
    {
        if (translations.TryGetValue(locale, out var strings))
        {
            return strings.Keys;
        }

        return [];
    }

    /// <inheritdoc />
    public bool HasLocale(Locale locale) => translations.ContainsKey(locale);

    /// <summary>
    /// Creates a string source from a JSON string.
    /// </summary>
    /// <param name="json">The JSON content.</param>
    /// <param name="locale">The locale for these translations.</param>
    /// <returns>A new <see cref="JsonStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static JsonStringSource FromString(string json, Locale locale)
    {
        ArgumentNullException.ThrowIfNull(json);

        var source = new JsonStringSource();
        var strings = ParseJson(json);
        source.translations[locale] = strings;
        return source;
    }

    /// <summary>
    /// Creates a string source by loading a JSON file.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <param name="locale">The locale for these translations.</param>
    /// <returns>A new <see cref="JsonStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static JsonStringSource FromFile(string path, Locale locale)
    {
        ArgumentNullException.ThrowIfNull(path);

        var json = File.ReadAllText(path);
        return FromString(json, locale);
    }

    /// <summary>
    /// Creates a string source by loading a JSON file asynchronously.
    /// </summary>
    /// <param name="path">The path to the JSON file.</param>
    /// <param name="locale">The locale for these translations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A new <see cref="JsonStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static async Task<JsonStringSource> FromFileAsync(
        string path,
        Locale locale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return FromString(json, locale);
    }

    /// <summary>
    /// Creates a string source from a stream.
    /// </summary>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <param name="locale">The locale for these translations.</param>
    /// <returns>A new <see cref="JsonStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static JsonStringSource FromStream(Stream stream, Locale locale)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return FromString(json, locale);
    }

    /// <summary>
    /// Creates a string source from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <param name="locale">The locale for these translations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A new <see cref="JsonStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid.</exception>
    public static async Task<JsonStringSource> FromStreamAsync(
        Stream stream,
        Locale locale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);
        return FromString(json, locale);
    }

    /// <summary>
    /// Adds translations from another JSON string to an existing locale.
    /// </summary>
    /// <param name="json">The JSON content to merge.</param>
    /// <param name="locale">The locale to add translations to.</param>
    /// <remarks>
    /// If keys already exist, they will be overwritten with the new values.
    /// </remarks>
    public void MergeFromString(string json, Locale locale)
    {
        ArgumentNullException.ThrowIfNull(json);

        var newStrings = ParseJson(json);

        if (!translations.TryGetValue(locale, out var existing))
        {
            existing = [];
            translations[locale] = existing;
        }

        foreach (var (key, value) in newStrings)
        {
            existing[key] = value;
        }
    }

    private static Dictionary<string, string> ParseJson(string json)
    {
        var result = new Dictionary<string, string>();

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        FlattenJsonElement(document.RootElement, string.Empty, result);
        return result;
    }

    private static void FlattenJsonElement(
        JsonElement element,
        string prefix,
        Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    FlattenJsonElement(property.Value, key, result);
                }
                break;

            case JsonValueKind.String:
                result[prefix] = element.GetString()!;
                break;

            case JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                result[prefix] = "true";
                break;

            case JsonValueKind.False:
                result[prefix] = "false";
                break;

            case JsonValueKind.Null:
                result[prefix] = string.Empty;
                break;

            case JsonValueKind.Array:
                // Arrays are not supported for localization strings
                // Skip them silently
                break;
        }
    }
}
