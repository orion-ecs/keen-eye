// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using NuGet.Versioning;

namespace KeenEyes.Editor.Plugins.Dependencies;

/// <summary>
/// Represents a semantic version constraint for plugin dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Wraps <see cref="VersionRange"/> from NuGet.Versioning to provide
/// SemVer constraint parsing and matching for plugin dependencies.
/// </para>
/// <para>
/// Supports standard constraint formats:
/// </para>
/// <list type="bullet">
/// <item><c>1.0.0</c> - Exact version match</item>
/// <item><c>&gt;=1.0.0</c> - Greater than or equal</item>
/// <item><c>&gt;1.0.0</c> - Greater than</item>
/// <item><c>&lt;2.0.0</c> - Less than</item>
/// <item><c>&lt;=2.0.0</c> - Less than or equal</item>
/// <item><c>^1.0.0</c> - Compatible with (same major version)</item>
/// <item><c>~1.2.0</c> - Approximately equivalent (same major.minor)</item>
/// <item><c>&gt;=1.0.0 &lt;2.0.0</c> - Range (combined constraints)</item>
/// </list>
/// </remarks>
internal readonly struct VersionConstraint : IEquatable<VersionConstraint>
{
    private readonly VersionRange? range;

    private VersionConstraint(VersionRange range)
    {
        this.range = range;
    }

    /// <summary>
    /// Gets the original constraint string.
    /// </summary>
    public string ConstraintString => range?.OriginalString ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this is an empty/default constraint.
    /// </summary>
    public bool IsEmpty => range is null;

    /// <summary>
    /// Parses a version constraint string.
    /// </summary>
    /// <param name="constraint">The constraint string to parse.</param>
    /// <returns>The parsed version constraint.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the constraint string is null, empty, or invalid.
    /// </exception>
    public static VersionConstraint Parse(string constraint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(constraint);

        var normalizedConstraint = NormalizeConstraint(constraint);

        if (!VersionRange.TryParse(normalizedConstraint, out var range))
        {
            throw new ArgumentException(
                $"Invalid version constraint: '{constraint}'",
                nameof(constraint));
        }

        return new VersionConstraint(range);
    }

    /// <summary>
    /// Attempts to parse a version constraint string.
    /// </summary>
    /// <param name="constraint">The constraint string to parse.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed constraint if parsing succeeded,
    /// or a default value if parsing failed.
    /// </param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? constraint, out VersionConstraint result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(constraint))
        {
            return false;
        }

        var normalizedConstraint = NormalizeConstraint(constraint);

        if (!VersionRange.TryParse(normalizedConstraint, out var range))
        {
            return false;
        }

        result = new VersionConstraint(range);
        return true;
    }

    /// <summary>
    /// Determines whether the specified version satisfies this constraint.
    /// </summary>
    /// <param name="version">The version string to check.</param>
    /// <returns>
    /// <c>true</c> if the version satisfies the constraint; otherwise, <c>false</c>.
    /// </returns>
    public bool IsSatisfiedBy(string version)
    {
        if (range is null)
        {
            return false;
        }

        if (!NuGetVersion.TryParse(version, out var nugetVersion))
        {
            return false;
        }

        return range.Satisfies(nugetVersion);
    }

    /// <summary>
    /// Determines whether the specified version satisfies this constraint.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns>
    /// <c>true</c> if the version satisfies the constraint; otherwise, <c>false</c>.
    /// </returns>
    public bool IsSatisfiedBy(NuGetVersion version)
    {
        if (range is null)
        {
            return false;
        }

        return range.Satisfies(version);
    }

    /// <inheritdoc />
    public bool Equals(VersionConstraint other)
    {
        if (range is null && other.range is null)
        {
            return true;
        }

        if (range is null || other.range is null)
        {
            return false;
        }

        return range.Equals(other.range);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is VersionConstraint other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return range?.GetHashCode() ?? 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return range?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(VersionConstraint left, VersionConstraint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(VersionConstraint left, VersionConstraint right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Normalizes constraint syntax to NuGet format.
    /// </summary>
    /// <remarks>
    /// Converts npm/cargo-style constraints to NuGet VersionRange format:
    /// <list type="bullet">
    /// <item><c>^1.0.0</c> → <c>[1.0.0, 2.0.0)</c> (compatible with)</item>
    /// <item><c>~1.2.0</c> → <c>[1.2.0, 1.3.0)</c> (approximately)</item>
    /// <item><c>&gt;=1.0.0</c> → <c>[1.0.0, )</c></item>
    /// <item><c>&gt;1.0.0</c> → <c>(1.0.0, )</c></item>
    /// <item><c>&lt;=1.0.0</c> → <c>(, 1.0.0]</c></item>
    /// <item><c>&lt;1.0.0</c> → <c>(, 1.0.0)</c></item>
    /// </list>
    /// </remarks>
    private static string NormalizeConstraint(string constraint)
    {
        constraint = constraint.Trim();

        // Handle caret (compatible with) - allows changes that do not modify the left-most non-zero digit
        // ^1.2.3 → [1.2.3, 2.0.0)
        // ^0.2.3 → [0.2.3, 0.3.0)
        // ^0.0.3 → [0.0.3, 0.0.4)
        if (constraint.StartsWith('^'))
        {
            var versionPart = constraint[1..];
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                NuGetVersion upperBound;
                if (version.Major != 0)
                {
                    upperBound = new NuGetVersion(version.Major + 1, 0, 0);
                }
                else if (version.Minor != 0)
                {
                    upperBound = new NuGetVersion(0, version.Minor + 1, 0);
                }
                else
                {
                    upperBound = new NuGetVersion(0, 0, version.Patch + 1);
                }

                return $"[{version}, {upperBound})";
            }
        }

        // Handle tilde (approximately) - allows patch-level changes
        // ~1.2.3 → [1.2.3, 1.3.0)
        if (constraint.StartsWith('~'))
        {
            var versionPart = constraint[1..];
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                var upperBound = new NuGetVersion(version.Major, version.Minor + 1, 0);
                return $"[{version}, {upperBound})";
            }
        }

        // Handle comparison operators - convert to NuGet interval notation
        // >=1.0.0 → [1.0.0, )
        if (constraint.StartsWith(">="))
        {
            var versionPart = constraint[2..].Trim();
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                return $"[{version}, )";
            }
        }

        // >1.0.0 → (1.0.0, )
        if (constraint.StartsWith('>') && !constraint.StartsWith(">="))
        {
            var versionPart = constraint[1..].Trim();
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                return $"({version}, )";
            }
        }

        // <=1.0.0 → (, 1.0.0]
        if (constraint.StartsWith("<="))
        {
            var versionPart = constraint[2..].Trim();
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                return $"(, {version}]";
            }
        }

        // <1.0.0 → (, 1.0.0)
        if (constraint.StartsWith('<') && !constraint.StartsWith("<="))
        {
            var versionPart = constraint[1..].Trim();
            if (NuGetVersion.TryParse(versionPart, out var version))
            {
                return $"(, {version})";
            }
        }

        // Pass through other formats (exact version, NuGet interval notation)
        return constraint;
    }
}
