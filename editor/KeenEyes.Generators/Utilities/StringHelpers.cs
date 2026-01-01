// Copyright (c) KeenEyes Contributors. Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;

namespace KeenEyes.Generators.Utilities;

/// <summary>
/// Provides string manipulation utilities for source generators.
/// </summary>
internal static class StringHelpers
{
    /// <summary>
    /// The string used by Roslyn to represent the global namespace.
    /// </summary>
    public const string GlobalNamespace = "<global namespace>";

    /// <summary>
    /// Determines if a namespace string represents a valid namespace that should be emitted.
    /// </summary>
    /// <param name="ns">The namespace string to check.</param>
    /// <returns><c>true</c> if the namespace should be emitted; <c>false</c> if it's empty or global.</returns>
    public static bool IsValidNamespace(string? ns)
    {
        return !string.IsNullOrEmpty(ns) && ns != GlobalNamespace;
    }

    /// <summary>
    /// Appends a file-scoped namespace declaration if the namespace is valid.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="ns">The namespace string.</param>
    /// <remarks>
    /// Does nothing if the namespace is null, empty, or the global namespace.
    /// Appends a file-scoped namespace declaration (e.g., "namespace Foo;") followed by a newline.
    /// </remarks>
    public static void AppendNamespaceDeclaration(StringBuilder sb, string? ns)
    {
        if (IsValidNamespace(ns))
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Gets the fully qualified type name with global:: prefix.
    /// </summary>
    /// <param name="ns">The namespace (may be null, empty, or global namespace).</param>
    /// <param name="typeName">The type name.</param>
    /// <returns>The fully qualified name like "global::Namespace.Type" or "global::Type" for global namespace.</returns>
    public static string GetFullTypeName(string? ns, string typeName)
    {
        return IsValidNamespace(ns)
            ? $"global::{ns}.{typeName}"
            : $"global::{typeName}";
    }

    /// <summary>
    /// Converts a PascalCase string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase version of the input string.</returns>
    /// <remarks>
    /// <para>
    /// Handles the following edge cases:
    /// <list type="bullet">
    /// <item><description>Empty/null strings are returned as-is</description></item>
    /// <item><description>Numeric prefixes are preserved (e.g., "2D" → "2D")</description></item>
    /// <item><description>All-caps acronyms are fully lowercased (e.g., "HTTP" → "http")</description></item>
    /// <item><description>Acronym prefixes are lowercased until the last capital before lowercase
    /// (e.g., "XMLParser" → "xmlParser", "HTTPRequest" → "httpRequest")</description></item>
    /// <item><description>Single characters are lowercased (e.g., "X" → "x")</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Handle numeric prefix - can't lowercase a digit, return as-is
        if (char.IsDigit(value[0]))
        {
            return value;
        }

        // Handle single character
        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

        // Check if it's all uppercase (acronym like "HTTP", "XML", "ID")
        var allUpper = true;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsLetter(value[i]) && !char.IsUpper(value[i]))
            {
                allUpper = false;
                break;
            }
        }

        if (allUpper)
        {
            return value.ToLowerInvariant();
        }

        // Find the run of consecutive uppercase letters at the start (acronym prefix)
        // e.g., "XMLParser" -> we want to lowercase "XML" except the last 'L' which starts "Parser"
        var upperCount = 0;
        while (upperCount < value.Length && char.IsUpper(value[upperCount]))
        {
            upperCount++;
        }

        // If there's an acronym prefix (more than 1 uppercase), lowercase all but the last one
        // "XMLParser" (upperCount=3) -> lowercase first 2, keep 'L' for "Parser"
        // "XData" (upperCount=1) -> lowercase first 1
        if (upperCount > 1)
        {
            // Lowercase the acronym except the last letter (which starts the next word)
            var prefixLength = upperCount - 1;
            return value.Substring(0, prefixLength).ToLowerInvariant() + value.Substring(prefixLength);
        }

        // Standard PascalCase -> camelCase: just lowercase the first letter
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
