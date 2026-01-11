namespace KeenEyes.Shaders.Compiler.Diagnostics;

/// <summary>
/// Provides suggestion algorithms for "did you mean?" style error messages.
/// </summary>
public static class SuggestionEngine
{
    /// <summary>
    /// Default maximum edit distance for suggestions.
    /// </summary>
    public const int DefaultMaxDistance = 2;

    /// <summary>
    /// Default maximum number of suggestions to return.
    /// </summary>
    public const int DefaultMaxSuggestions = 3;

    /// <summary>
    /// Gets suggestions for a mistyped identifier from a list of valid candidates.
    /// </summary>
    /// <param name="input">The input string that was not recognized.</param>
    /// <param name="candidates">The list of valid candidates to suggest from.</param>
    /// <param name="maxDistance">Maximum Levenshtein distance for a suggestion to be included.</param>
    /// <param name="maxSuggestions">Maximum number of suggestions to return.</param>
    /// <returns>A list of suggestions sorted by edit distance (closest first).</returns>
    public static IReadOnlyList<string> GetSuggestions(
        string input,
        IEnumerable<string> candidates,
        int maxDistance = DefaultMaxDistance,
        int maxSuggestions = DefaultMaxSuggestions)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Array.Empty<string>();
        }

        var suggestions = new List<(string candidate, int distance)>();
        var inputLower = input.ToLowerInvariant();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            var candidateLower = candidate.ToLowerInvariant();
            var distance = LevenshteinDistance(inputLower, candidateLower);

            // Also consider prefix matching for longer identifiers
            if (distance <= maxDistance || IsGoodPrefixMatch(inputLower, candidateLower))
            {
                suggestions.Add((candidate, distance));
            }
        }

        return suggestions
            .OrderBy(s => s.distance)
            .ThenBy(s => s.candidate, StringComparer.OrdinalIgnoreCase)
            .Take(maxSuggestions)
            .Select(s => s.candidate)
            .ToList();
    }

    /// <summary>
    /// Computes the Levenshtein (edit) distance between two strings.
    /// </summary>
    /// <param name="a">The first string.</param>
    /// <param name="b">The second string.</param>
    /// <returns>The minimum number of single-character edits needed to transform a into b.</returns>
    /// <remarks>
    /// The Levenshtein distance counts insertions, deletions, and substitutions.
    /// A distance of 0 means the strings are identical.
    /// </remarks>
    public static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return string.IsNullOrEmpty(b) ? 0 : b.Length;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a.Length;
        }

        var m = a.Length;
        var n = b.Length;

        // Use a single-row optimization for space efficiency
        var previousRow = new int[n + 1];
        var currentRow = new int[n + 1];

        // Initialize the first row (cost of inserting each character of b)
        for (var j = 0; j <= n; j++)
        {
            previousRow[j] = j;
        }

        for (var i = 1; i <= m; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= n; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        previousRow[j] + 1,      // Deletion
                        currentRow[j - 1] + 1),  // Insertion
                    previousRow[j - 1] + cost);  // Substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[n];
    }

    /// <summary>
    /// Checks if the input is a good prefix match for the candidate.
    /// This helps catch cases where the user typed a truncated identifier.
    /// </summary>
    private static bool IsGoodPrefixMatch(string input, string candidate)
    {
        // Input must be at least 3 characters for prefix matching
        if (input.Length < 3)
        {
            return false;
        }

        // Check if input is a prefix of candidate
        if (candidate.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if candidate is a prefix of input (user typed extra)
        if (input.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets suggestions for a component type name from known component types.
    /// </summary>
    /// <param name="input">The unrecognized component type name.</param>
    /// <param name="knownTypes">The known component type names.</param>
    /// <returns>A list of suggested component type names.</returns>
    public static IReadOnlyList<string> GetComponentSuggestions(
        string input,
        IEnumerable<string> knownTypes)
    {
        // For component types, allow a slightly higher distance
        // since type names can be longer
        return GetSuggestions(input, knownTypes, maxDistance: 3, maxSuggestions: 3);
    }

    /// <summary>
    /// Gets suggestions for a keyword from the KESL keyword list.
    /// </summary>
    /// <param name="input">The unrecognized token.</param>
    /// <returns>A list of suggested keywords.</returns>
    public static IReadOnlyList<string> GetKeywordSuggestions(string input)
    {
        var keywords = new[]
        {
            // Declaration keywords
            "component", "compute", "query", "params", "execute",
            // Access modifiers
            "read", "write", "optional", "without",
            // Control flow
            "if", "else", "for", "while", "return", "break", "continue",
            // Type keywords
            "float", "float2", "float3", "float4",
            "int", "int2", "int3", "int4",
            "uint", "uint2", "uint3", "uint4",
            "bool", "mat2", "mat3", "mat4",
            // Literals
            "true", "false"
        };

        return GetSuggestions(input, keywords, maxDistance: 2, maxSuggestions: 2);
    }

    /// <summary>
    /// Gets suggestions for a built-in function name.
    /// </summary>
    /// <param name="input">The unrecognized function name.</param>
    /// <returns>A list of suggested function names.</returns>
    public static IReadOnlyList<string> GetFunctionSuggestions(string input)
    {
        var functions = new[]
        {
            // Math functions
            "abs", "sign", "floor", "ceil", "round", "fract",
            "mod", "min", "max", "clamp", "mix", "step", "smoothstep",
            "sqrt", "pow", "exp", "log", "exp2", "log2",
            // Trigonometry
            "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
            "sinh", "cosh", "tanh",
            // Vector operations
            "length", "distance", "dot", "cross", "normalize", "reflect", "refract",
            // Matrix operations
            "transpose", "inverse", "determinant",
            // Texture operations
            "texture", "textureLod", "textureGrad"
        };

        return GetSuggestions(input, functions, maxDistance: 2, maxSuggestions: 3);
    }
}
