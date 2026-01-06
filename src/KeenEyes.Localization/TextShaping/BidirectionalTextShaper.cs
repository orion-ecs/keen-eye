using System.Text;

namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Text shaper that handles bidirectional text (mixed LTR and RTL content).
/// </summary>
/// <remarks>
/// <para>
/// This shaper implements a simplified version of the Unicode Bidirectional Algorithm (UBA)
/// to properly order text that contains both left-to-right and right-to-left characters.
/// </para>
/// <para>
/// Common use cases:
/// <list type="bullet">
///   <item><description>English text with Arabic words</description></item>
///   <item><description>Hebrew text with English technical terms</description></item>
///   <item><description>Numbers in RTL text</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var shaper = new BidirectionalTextShaper();
/// string result = shaper.Shape("Hello مرحبا World", Locale.EnglishUS);
/// // Returns: "Hello ابحرم World" (Arabic reversed for LTR base)
/// </code>
/// </example>
public sealed class BidirectionalTextShaper : ITextShaper
{
    private readonly ArabicTextShaper arabicShaper = new();

    /// <inheritdoc />
    public IEnumerable<ScriptType> SupportedScripts =>
    [
        ScriptType.Latin,
        ScriptType.Arabic,
        ScriptType.Hebrew,
        ScriptType.Cyrillic,
        ScriptType.Greek
    ];

    /// <inheritdoc />
    public bool SupportsScript(ScriptType script) => script switch
    {
        ScriptType.Latin => true,
        ScriptType.Arabic => true,
        ScriptType.Hebrew => true,
        ScriptType.Cyrillic => true,
        ScriptType.Greek => true,
        _ => false
    };

    /// <inheritdoc />
    public string Shape(string text, Locale locale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Determine base direction from locale
        var baseDirection = locale.TextDirection;

        // Split into runs of same-direction text
        var runs = IdentifyDirectionalRuns(text);

        // If no RTL content found, return as-is (with Arabic shaping if needed)
        if (!runs.Any(r => r.IsRtl))
        {
            return text;
        }

        // Process and reorder runs
        var result = new StringBuilder(text.Length);

        if (baseDirection == TextDirection.LeftToRight)
        {
            ProcessLtrBase(runs, result);
        }
        else
        {
            ProcessRtlBase(runs, result);
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public ShapingResult ShapeWithInfo(string text, Locale locale)
    {
        var runs = IdentifyDirectionalRuns(text);
        bool hasRtl = runs.Any(r => r.IsRtl);
        bool hasLtr = runs.Any(r => !r.IsRtl);

        string shapedText = Shape(text, locale);

        var scripts = new List<ScriptType>();
        foreach (var run in runs)
        {
            if (run.IsRtl)
            {
                scripts.Add(ScriptType.Arabic); // Simplified - could detect Hebrew vs Arabic
            }
            else if (run.Text.Any(char.IsLetter))
            {
                scripts.Add(ScriptType.Latin);
            }
        }

        return new ShapingResult(
            ShapedText: shapedText,
            OriginalText: text,
            BaseDirection: locale.TextDirection,
            ContainsRtl: hasRtl,
            IsMixedDirection: hasRtl && hasLtr,
            DetectedScripts: scripts.Distinct().ToList());
    }

    private void ProcessLtrBase(List<DirectionalRun> runs, StringBuilder result)
    {
        foreach (var run in runs)
        {
            if (run.IsRtl)
            {
                // Shape Arabic text and reverse for display in LTR context
                string shaped = arabicShaper.Shape(run.Text, Locale.Arabic);
                result.Append(ReverseString(shaped));
            }
            else
            {
                result.Append(run.Text);
            }
        }
    }

    private void ProcessRtlBase(List<DirectionalRun> runs, StringBuilder result)
    {
        // In RTL base, process runs in reverse order
        for (int i = runs.Count - 1; i >= 0; i--)
        {
            var run = runs[i];
            if (run.IsRtl)
            {
                // Shape Arabic text (already in correct order for RTL display)
                string shaped = arabicShaper.Shape(run.Text, Locale.Arabic);
                result.Append(shaped);
            }
            else
            {
                // LTR text needs to be kept as-is within the RTL flow
                result.Append(run.Text);
            }
        }
    }

    private static List<DirectionalRun> IdentifyDirectionalRuns(string text)
    {
        var runs = new List<DirectionalRun>();
        if (string.IsNullOrEmpty(text))
        {
            return runs;
        }

        var currentRun = new StringBuilder();
        bool? currentIsRtl = null;

        foreach (char c in text)
        {
            bool isRtl = IsRtlCharacter(c);
            bool isNeutral = IsNeutralCharacter(c);

            if (isNeutral)
            {
                // Neutral characters (spaces, punctuation) join the current run
                currentRun.Append(c);
            }
            else if (currentIsRtl == null || currentIsRtl == isRtl)
            {
                currentIsRtl = isRtl;
                currentRun.Append(c);
            }
            else
            {
                // Direction change - save current run
                if (currentRun.Length > 0)
                {
                    runs.Add(new DirectionalRun(currentRun.ToString(), currentIsRtl.Value));
                    currentRun.Clear();
                }
                currentIsRtl = isRtl;
                currentRun.Append(c);
            }
        }

        // Add final run
        if (currentRun.Length > 0)
        {
            runs.Add(new DirectionalRun(currentRun.ToString(), currentIsRtl ?? false));
        }

        return runs;
    }

    private static bool IsRtlCharacter(char c)
    {
        // Arabic: U+0600-U+06FF, U+FB50-U+FDFF, U+FE70-U+FEFF
        // Hebrew: U+0590-U+05FF
        return (c >= '\u0600' && c <= '\u06FF') ||
               (c >= '\uFB50' && c <= '\uFDFF') ||
               (c >= '\uFE70' && c <= '\uFEFF') ||
               (c >= '\u0590' && c <= '\u05FF');
    }

    private static bool IsNeutralCharacter(char c)
    {
        // Spaces, digits, and common punctuation are neutral
        return char.IsWhiteSpace(c) ||
               char.IsDigit(c) ||
               char.IsPunctuation(c) ||
               char.IsSymbol(c);
    }

    private static string ReverseString(string s)
    {
        char[] arr = s.ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    private readonly record struct DirectionalRun(string Text, bool IsRtl);
}
