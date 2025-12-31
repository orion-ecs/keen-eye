// Copyright (c) KeenEyes Contributors. Licensed under the MIT License. See LICENSE in the project root for license information.

namespace KeenEyes.Generators.Utilities;

/// <summary>
/// Provides string manipulation utilities for source generators.
/// </summary>
internal static class StringHelpers
{
    /// <summary>
    /// Converts a PascalCase string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase version of the input string.</returns>
    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Handle names like "X", "Y", "Z" - keep them lowercase
        if (value.Length == 1)
        {
            return value.ToLowerInvariant();
        }

        // Handle PascalCase -> camelCase
        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
