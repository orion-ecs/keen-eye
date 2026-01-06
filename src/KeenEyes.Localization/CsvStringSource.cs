using System.Text;

namespace KeenEyes.Localization;

/// <summary>
/// A string source that loads translations from CSV files for spreadsheet workflows.
/// </summary>
/// <remarks>
/// <para>
/// CSV files provide an easy way for translators to work with translations using
/// spreadsheet applications like Excel or Google Sheets.
/// </para>
/// <para>
/// Expected CSV format:
/// </para>
/// <code>
/// key,en,es,ar,ja
/// menu.start,Start Game,Iniciar Juego,ابدأ اللعبة,ゲームを始める
/// menu.quit,Quit,Salir,خروج,終了
/// </code>
/// <para>
/// The first column must be "key". Subsequent columns are locale codes.
/// Values containing commas, quotes, or newlines should be quoted.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load from file
/// var source = CsvStringSource.FromFile("translations.csv");
/// localization.AddSource(source);
///
/// // Export for translators
/// CsvStringSource.Export("translations_export.csv", existingSource, [Locale.EnglishUS, Locale.Spanish]);
/// </code>
/// </example>
public sealed class CsvStringSource : IStringSource
{
    private readonly Dictionary<Locale, Dictionary<string, string>> translations = [];
    private readonly List<Locale> localeOrder = [];

    private CsvStringSource()
    {
    }

    /// <inheritdoc />
    public IEnumerable<Locale> SupportedLocales => translations.Keys;

    /// <summary>
    /// Gets the locales in the order they appear in the CSV header.
    /// </summary>
    public IReadOnlyList<Locale> LocaleOrder => localeOrder;

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
    /// Gets all keys in this source across all locales.
    /// </summary>
    public IEnumerable<string> AllKeys
    {
        get
        {
            var keys = new HashSet<string>();
            foreach (var localeStrings in translations.Values)
            {
                foreach (var key in localeStrings.Keys)
                {
                    keys.Add(key);
                }
            }
            return keys;
        }
    }

    /// <summary>
    /// Creates a string source from a CSV string.
    /// </summary>
    /// <param name="csv">The CSV content.</param>
    /// <returns>A new <see cref="CsvStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FormatException">Thrown if the CSV format is invalid.</exception>
    public static CsvStringSource FromString(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);

        var source = new CsvStringSource();
        var lines = ParseCsvLines(csv);

        if (lines.Count == 0)
        {
            return source;
        }

        // Parse header row
        var header = lines[0];
        if (header.Count == 0 || !header[0].Equals("key", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("CSV header must start with 'key' column");
        }

        // Extract locales from header
        var locales = new List<Locale>();
        for (int i = 1; i < header.Count; i++)
        {
            var locale = new Locale(header[i].Trim());
            locales.Add(locale);
            source.localeOrder.Add(locale);
            source.translations[locale] = [];
        }

        // Parse data rows
        for (int rowIndex = 1; rowIndex < lines.Count; rowIndex++)
        {
            var row = lines[rowIndex];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0]))
            {
                continue; // Skip empty rows
            }

            var key = row[0].Trim();

            for (int colIndex = 1; colIndex < row.Count && colIndex <= locales.Count; colIndex++)
            {
                var locale = locales[colIndex - 1];
                var value = row[colIndex];

                if (!string.IsNullOrEmpty(value))
                {
                    source.translations[locale][key] = value;
                }
            }
        }

        return source;
    }

    /// <summary>
    /// Creates a string source by loading a CSV file.
    /// </summary>
    /// <param name="path">The path to the CSV file.</param>
    /// <returns>A new <see cref="CsvStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="FormatException">Thrown if the CSV format is invalid.</exception>
    public static CsvStringSource FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var csv = File.ReadAllText(path, Encoding.UTF8);
        return FromString(csv);
    }

    /// <summary>
    /// Creates a string source by loading a CSV file asynchronously.
    /// </summary>
    /// <param name="path">The path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A new <see cref="CsvStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="FormatException">Thrown if the CSV format is invalid.</exception>
    public static async Task<CsvStringSource> FromFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        var csv = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        return FromString(csv);
    }

    /// <summary>
    /// Creates a string source from a stream.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <returns>A new <see cref="CsvStringSource"/> containing the parsed translations.</returns>
    /// <exception cref="FormatException">Thrown if the CSV format is invalid.</exception>
    public static CsvStringSource FromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var csv = reader.ReadToEnd();
        return FromString(csv);
    }

    /// <summary>
    /// Exports translations to CSV format.
    /// </summary>
    /// <param name="source">The string source to export.</param>
    /// <param name="locales">The locales to include in the export.</param>
    /// <returns>The CSV content as a string.</returns>
    public static string ToCsv(IStringSource source, IEnumerable<Locale> locales)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(locales);

        var localeList = locales.ToList();
        var allKeys = new HashSet<string>();

        foreach (var locale in localeList)
        {
            foreach (var key in source.GetKeys(locale))
            {
                allKeys.Add(key);
            }
        }

        var sb = new StringBuilder();

        // Write header
        sb.Append("key");
        foreach (var locale in localeList)
        {
            sb.Append(',');
            sb.Append(locale.Code);
        }
        sb.AppendLine();

        // Write data rows
        foreach (var key in allKeys.OrderBy(k => k))
        {
            sb.Append(EscapeCsvValue(key));

            foreach (var locale in localeList)
            {
                sb.Append(',');
                if (source.TryGetString(locale, key, out var value) && value != null)
                {
                    sb.Append(EscapeCsvValue(value));
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports translations to a CSV file.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="source">The string source to export.</param>
    /// <param name="locales">The locales to include in the export.</param>
    public static void Export(string path, IStringSource source, IEnumerable<Locale> locales)
    {
        var csv = ToCsv(source, locales);
        File.WriteAllText(path, csv, Encoding.UTF8);
    }

    /// <summary>
    /// Exports translations to a CSV file asynchronously.
    /// </summary>
    /// <param name="path">The file path to write to.</param>
    /// <param name="source">The string source to export.</param>
    /// <param name="locales">The locales to include in the export.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public static async Task ExportAsync(
        string path,
        IStringSource source,
        IEnumerable<Locale> locales,
        CancellationToken cancellationToken = default)
    {
        var csv = ToCsv(source, locales);
        await File.WriteAllTextAsync(path, csv, Encoding.UTF8, cancellationToken);
    }

    /// <summary>
    /// Converts this source to CSV format.
    /// </summary>
    /// <returns>The CSV content as a string.</returns>
    public string ToCsv()
    {
        return ToCsv(this, localeOrder.Count > 0 ? localeOrder : SupportedLocales);
    }

    /// <summary>
    /// Merges translations from another CSV string.
    /// </summary>
    /// <param name="csv">The CSV content to merge.</param>
    /// <remarks>
    /// Existing keys will be overwritten with new values.
    /// </remarks>
    public void MergeFromString(string csv)
    {
        var other = FromString(csv);

        foreach (var locale in other.SupportedLocales)
        {
            if (!translations.ContainsKey(locale))
            {
                translations[locale] = [];
                localeOrder.Add(locale);
            }

            foreach (var key in other.GetKeys(locale))
            {
                if (other.TryGetString(locale, key, out var value) && value != null)
                {
                    translations[locale][key] = value;
                }
            }
        }
    }

    private static List<List<string>> ParseCsvLines(string csv)
    {
        var lines = new List<List<string>>();
        var currentLine = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < csv.Length)
        {
            char c = csv[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i += 2;
                        continue;
                    }
                    else
                    {
                        inQuotes = false;
                        i++;
                        continue;
                    }
                }
                else
                {
                    currentValue.Append(c);
                    i++;
                    continue;
                }
            }

            // Not in quotes
            if (c == '"')
            {
                inQuotes = true;
                i++;
            }
            else if (c == ',')
            {
                currentLine.Add(currentValue.ToString());
                currentValue.Clear();
                i++;
            }
            else if (c == '\r')
            {
                // Handle \r\n or just \r
                currentLine.Add(currentValue.ToString());
                currentValue.Clear();
                lines.Add(currentLine);
                currentLine = [];
                i++;
                if (i < csv.Length && csv[i] == '\n')
                {
                    i++;
                }
            }
            else if (c == '\n')
            {
                currentLine.Add(currentValue.ToString());
                currentValue.Clear();
                lines.Add(currentLine);
                currentLine = [];
                i++;
            }
            else
            {
                currentValue.Append(c);
                i++;
            }
        }

        // Add final value and line
        if (currentValue.Length > 0 || currentLine.Count > 0)
        {
            currentLine.Add(currentValue.ToString());
            lines.Add(currentLine);
        }

        return lines;
    }

    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        bool needsQuotes = value.Contains(',') ||
                           value.Contains('"') ||
                           value.Contains('\n') ||
                           value.Contains('\r');

        if (!needsQuotes)
        {
            return value;
        }

        // Escape quotes by doubling them and wrap in quotes
        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
