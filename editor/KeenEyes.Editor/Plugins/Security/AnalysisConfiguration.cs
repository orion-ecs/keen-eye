// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Configuration for assembly security analysis.
/// </summary>
public sealed class AnalysisConfiguration
{
    /// <summary>
    /// Gets the default analysis configuration.
    /// </summary>
    public static AnalysisConfiguration Default { get; } = new()
    {
        Mode = AnalysisMode.WarnOnly,
        EnabledPatterns = Enum.GetValues<DetectionPattern>().ToHashSet(),
        BlockingSeverity = SecuritySeverity.Critical,
        Exceptions = []
    };

    /// <summary>
    /// Gets or sets the analysis mode (warn, block, or prompt).
    /// </summary>
    public AnalysisMode Mode { get; init; } = AnalysisMode.WarnOnly;

    /// <summary>
    /// Gets or sets the patterns to detect during analysis.
    /// </summary>
    public IReadOnlySet<DetectionPattern> EnabledPatterns { get; init; } =
        Enum.GetValues<DetectionPattern>().ToHashSet();

    /// <summary>
    /// Gets or sets the minimum severity that will block loading (when Mode is Block).
    /// </summary>
    public SecuritySeverity BlockingSeverity { get; init; } = SecuritySeverity.Critical;

    /// <summary>
    /// Gets or sets patterns to exclude from analysis (known safe uses).
    /// </summary>
    public IReadOnlyList<PatternException> Exceptions { get; init; } = [];

    /// <summary>
    /// Checks if a specific pattern is enabled for detection.
    /// </summary>
    public bool IsPatternEnabled(DetectionPattern pattern) => EnabledPatterns.Contains(pattern);

    /// <summary>
    /// Checks if a finding is excepted by configuration.
    /// </summary>
    public bool IsExcepted(SecurityFinding finding)
    {
        foreach (var exception in Exceptions)
        {
            if (exception.Matches(finding))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates a strict configuration that blocks on high severity findings.
    /// </summary>
    public static AnalysisConfiguration Strict { get; } = new()
    {
        Mode = AnalysisMode.Block,
        EnabledPatterns = Enum.GetValues<DetectionPattern>().ToHashSet(),
        BlockingSeverity = SecuritySeverity.High,
        Exceptions = []
    };

    /// <summary>
    /// Creates a permissive configuration that only warns.
    /// </summary>
    public static AnalysisConfiguration Permissive { get; } = new()
    {
        Mode = AnalysisMode.WarnOnly,
        EnabledPatterns = Enum.GetValues<DetectionPattern>().ToHashSet(),
        BlockingSeverity = SecuritySeverity.Critical,
        Exceptions = []
    };
}

/// <summary>
/// Represents an exception rule for security analysis.
/// </summary>
public sealed class PatternException
{
    /// <summary>
    /// Gets or sets the pattern to except.
    /// </summary>
    public DetectionPattern? Pattern { get; init; }

    /// <summary>
    /// Gets or sets the member reference pattern to except (supports wildcards).
    /// </summary>
    public string? MemberPattern { get; init; }

    /// <summary>
    /// Gets or sets a description of why this exception exists.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Checks if this exception matches a finding.
    /// </summary>
    public bool Matches(SecurityFinding finding)
    {
        if (Pattern.HasValue && finding.Pattern != Pattern.Value)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(MemberPattern) && finding.MemberReference != null)
        {
            // Simple wildcard matching
            if (MemberPattern.EndsWith('*'))
            {
                var prefix = MemberPattern[..^1];
                return finding.MemberReference.StartsWith(prefix, StringComparison.Ordinal);
            }

            return string.Equals(MemberPattern, finding.MemberReference, StringComparison.Ordinal);
        }

        return true;
    }
}
