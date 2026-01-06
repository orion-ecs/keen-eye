using System.Text;

namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Text shaper for Arabic script that handles contextual letter forms.
/// </summary>
/// <remarks>
/// <para>
/// Arabic letters have different forms depending on their position in a word:
/// </para>
/// <list type="bullet">
///   <item><description><b>Isolated</b> - letter appears alone</description></item>
///   <item><description><b>Initial</b> - letter at the start of a word</description></item>
///   <item><description><b>Medial</b> - letter in the middle of a word</description></item>
///   <item><description><b>Final</b> - letter at the end of a word</description></item>
/// </list>
/// <para>
/// Some letters (like ا alif, د dal, ذ dhal, ر ra, ز zay, و waw) do not connect
/// to the following letter, affecting the form of subsequent letters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var shaper = new ArabicTextShaper();
/// string shaped = shaper.Shape("مرحبا", Locale.Arabic);
/// // Returns Arabic text with proper contextual letter forms
/// </code>
/// </example>
public sealed class ArabicTextShaper : ITextShaper
{
    // Arabic letter form mappings: [isolated, initial, medial, final]
    // Only includes letters that have contextual forms
    private static readonly Dictionary<char, char[]> arabicForms = new()
    {
        // Alif - does not connect to following letter
        ['ا'] = ['ا', 'ا', 'ﺎ', 'ﺎ'],
        // Ba
        ['ب'] = ['ب', 'ﺑ', 'ﺒ', 'ﺐ'],
        // Ta
        ['ت'] = ['ت', 'ﺗ', 'ﺘ', 'ﺖ'],
        // Tha
        ['ث'] = ['ث', 'ﺛ', 'ﺜ', 'ﺚ'],
        // Jim
        ['ج'] = ['ج', 'ﺟ', 'ﺠ', 'ﺞ'],
        // Ha
        ['ح'] = ['ح', 'ﺣ', 'ﺤ', 'ﺢ'],
        // Kha
        ['خ'] = ['خ', 'ﺧ', 'ﺨ', 'ﺦ'],
        // Dal - does not connect to following letter
        ['د'] = ['د', 'د', 'ﺪ', 'ﺪ'],
        // Dhal - does not connect to following letter
        ['ذ'] = ['ذ', 'ذ', 'ﺬ', 'ﺬ'],
        // Ra - does not connect to following letter
        ['ر'] = ['ر', 'ر', 'ﺮ', 'ﺮ'],
        // Zay - does not connect to following letter
        ['ز'] = ['ز', 'ز', 'ﺰ', 'ﺰ'],
        // Sin
        ['س'] = ['س', 'ﺳ', 'ﺴ', 'ﺲ'],
        // Shin
        ['ش'] = ['ش', 'ﺷ', 'ﺸ', 'ﺶ'],
        // Sad
        ['ص'] = ['ص', 'ﺻ', 'ﺼ', 'ﺺ'],
        // Dad
        ['ض'] = ['ض', 'ﺿ', 'ﻀ', 'ﺾ'],
        // Tah
        ['ط'] = ['ط', 'ﻃ', 'ﻄ', 'ﻂ'],
        // Dhah
        ['ظ'] = ['ظ', 'ﻇ', 'ﻈ', 'ﻆ'],
        // Ain
        ['ع'] = ['ع', 'ﻋ', 'ﻌ', 'ﻊ'],
        // Ghain
        ['غ'] = ['غ', 'ﻏ', 'ﻐ', 'ﻎ'],
        // Fa
        ['ف'] = ['ف', 'ﻓ', 'ﻔ', 'ﻒ'],
        // Qaf
        ['ق'] = ['ق', 'ﻗ', 'ﻘ', 'ﻖ'],
        // Kaf
        ['ك'] = ['ك', 'ﻛ', 'ﻜ', 'ﻚ'],
        // Lam
        ['ل'] = ['ل', 'ﻟ', 'ﻠ', 'ﻞ'],
        // Mim
        ['م'] = ['م', 'ﻣ', 'ﻤ', 'ﻢ'],
        // Nun
        ['ن'] = ['ن', 'ﻧ', 'ﻨ', 'ﻦ'],
        // Ha
        ['ه'] = ['ه', 'ﻫ', 'ﻬ', 'ﻪ'],
        // Waw - does not connect to following letter
        ['و'] = ['و', 'و', 'ﻮ', 'ﻮ'],
        // Ya
        ['ي'] = ['ي', 'ﻳ', 'ﻴ', 'ﻲ'],
        // Alif with hamza above
        ['أ'] = ['أ', 'أ', 'ﺄ', 'ﺄ'],
        // Alif with hamza below
        ['إ'] = ['إ', 'إ', 'ﺈ', 'ﺈ'],
        // Alif madda
        ['آ'] = ['آ', 'آ', 'ﺂ', 'ﺂ'],
        // Hamza on waw
        ['ؤ'] = ['ؤ', 'ؤ', 'ﺆ', 'ﺆ'],
        // Hamza on ya
        ['ئ'] = ['ئ', 'ﺋ', 'ﺌ', 'ﺊ'],
        // Ta marbuta - does not connect to following letter
        ['ة'] = ['ة', 'ة', 'ﺔ', 'ﺔ'],
        // Alif maksura - does not connect to following letter
        ['ى'] = ['ى', 'ى', 'ﻰ', 'ﻰ'],
    };

    // Letters that do not connect to the following letter (right-side non-joiners)
    private static readonly HashSet<char> nonJoiningRight =
    [
        'ا', 'أ', 'إ', 'آ', 'د', 'ذ', 'ر', 'ز', 'و', 'ؤ', 'ة', 'ى'
    ];

    /// <inheritdoc />
    public IEnumerable<ScriptType> SupportedScripts => [ScriptType.Arabic];

    /// <inheritdoc />
    public bool SupportsScript(ScriptType script) => script == ScriptType.Arabic;

    /// <inheritdoc />
    public string Shape(string text, Locale locale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var result = new StringBuilder(text.Length);
        int i = 0;

        while (i < text.Length)
        {
            char current = text[i];

            // Check if this is an Arabic letter
            if (!IsArabicLetter(current))
            {
                result.Append(current);
                i++;
                continue;
            }

            // Determine the form based on context
            bool prevConnects = i > 0 && IsArabicLetter(text[i - 1]) && !nonJoiningRight.Contains(text[i - 1]);
            bool nextConnects = i < text.Length - 1 && IsArabicLetter(text[i + 1]);

            char shapedChar = GetContextualForm(current, prevConnects, nextConnects);
            result.Append(shapedChar);
            i++;
        }

        return result.ToString();
    }

    /// <inheritdoc />
    public ShapingResult ShapeWithInfo(string text, Locale locale)
    {
        string shapedText = Shape(text, locale);

        return new ShapingResult(
            ShapedText: shapedText,
            OriginalText: text,
            BaseDirection: TextDirection.RightToLeft,
            ContainsRtl: ContainsArabic(text),
            IsMixedDirection: ContainsMixedDirections(text),
            DetectedScripts: [ScriptType.Arabic]);
    }

    /// <summary>
    /// Determines whether a character is an Arabic letter.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns><c>true</c> if the character is Arabic; otherwise, <c>false</c>.</returns>
    public static bool IsArabicLetter(char c)
    {
        // Arabic Unicode range: U+0600-U+06FF (Arabic)
        // Also check U+FB50-U+FDFF (Arabic Presentation Forms-A)
        // And U+FE70-U+FEFF (Arabic Presentation Forms-B)
        return (c >= '\u0600' && c <= '\u06FF') ||
               (c >= '\uFB50' && c <= '\uFDFF') ||
               (c >= '\uFE70' && c <= '\uFEFF');
    }

    /// <summary>
    /// Gets the contextual form of an Arabic letter.
    /// </summary>
    /// <param name="c">The base letter.</param>
    /// <param name="prevConnects">Whether the previous letter connects.</param>
    /// <param name="nextConnects">Whether the next letter connects.</param>
    /// <returns>The appropriate contextual form.</returns>
    private static char GetContextualForm(char c, bool prevConnects, bool nextConnects)
    {
        if (!arabicForms.TryGetValue(c, out var forms))
        {
            return c; // No contextual forms defined
        }

        // Determine form index: 0=isolated, 1=initial, 2=medial, 3=final
        int formIndex;
        if (!prevConnects && !nextConnects)
        {
            formIndex = 0; // Isolated
        }
        else if (!prevConnects && nextConnects)
        {
            formIndex = 1; // Initial
        }
        else if (prevConnects && nextConnects)
        {
            formIndex = 2; // Medial
        }
        else // prevConnects && !nextConnects
        {
            formIndex = 3; // Final
        }

        return forms[formIndex];
    }

    private static bool ContainsArabic(string text)
    {
        foreach (char c in text)
        {
            if (IsArabicLetter(c))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsMixedDirections(string text)
    {
        bool hasLtr = false;
        bool hasRtl = false;

        foreach (char c in text)
        {
            if (IsArabicLetter(c) || (c >= '\u0590' && c <= '\u05FF')) // Hebrew range
            {
                hasRtl = true;
            }
            else if (char.IsLetter(c))
            {
                hasLtr = true;
            }

            if (hasLtr && hasRtl)
            {
                return true;
            }
        }

        return false;
    }
}
