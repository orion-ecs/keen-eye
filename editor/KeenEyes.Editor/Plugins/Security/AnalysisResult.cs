// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Result of static analysis on a plugin assembly.
/// </summary>
public sealed class AnalysisResult
{
    /// <summary>
    /// Gets the path to the analyzed assembly.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether the analysis passed (no blocking findings).
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Gets the list of security findings from analysis.
    /// </summary>
    public required IReadOnlyList<SecurityFinding> Findings { get; init; }

    /// <summary>
    /// Gets the timestamp when analysis was performed.
    /// </summary>
    public required DateTime AnalysisTimestamp { get; init; }

    /// <summary>
    /// Gets the SHA256 hash of the analyzed assembly for cache invalidation.
    /// </summary>
    public required string AssemblyHash { get; init; }

    /// <summary>
    /// Gets the assembly name that was analyzed.
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// Gets the assembly version that was analyzed.
    /// </summary>
    public string? AssemblyVersion { get; init; }

    /// <summary>
    /// Gets an error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether analysis completed successfully.
    /// </summary>
    public bool AnalysisCompleted => ErrorMessage == null;

    /// <summary>
    /// Gets findings filtered by minimum severity.
    /// </summary>
    public IEnumerable<SecurityFinding> GetFindingsBySeverity(SecuritySeverity minSeverity) =>
        Findings.Where(f => f.Severity >= minSeverity);

    /// <summary>
    /// Gets findings for a specific pattern.
    /// </summary>
    public IEnumerable<SecurityFinding> GetFindingsByPattern(DetectionPattern pattern) =>
        Findings.Where(f => f.Pattern == pattern);

    /// <summary>
    /// Gets a value indicating whether any findings have the specified severity or higher.
    /// </summary>
    public bool HasFindingsAtOrAbove(SecuritySeverity severity) =>
        Findings.Any(f => f.Severity >= severity);

    /// <summary>
    /// Gets a summary of findings by pattern.
    /// </summary>
    public IReadOnlyDictionary<DetectionPattern, int> GetFindingSummary() =>
        Findings
            .GroupBy(f => f.Pattern)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// Creates a successful analysis result with no findings.
    /// </summary>
    public static AnalysisResult Clean(string assemblyPath, string hash) => new()
    {
        AssemblyPath = assemblyPath,
        Passed = true,
        Findings = [],
        AnalysisTimestamp = DateTime.UtcNow,
        AssemblyHash = hash
    };

    /// <summary>
    /// Creates a failed analysis result due to an error.
    /// </summary>
    public static AnalysisResult Error(string assemblyPath, string error) => new()
    {
        AssemblyPath = assemblyPath,
        Passed = false,
        Findings = [],
        AnalysisTimestamp = DateTime.UtcNow,
        AssemblyHash = string.Empty,
        ErrorMessage = error
    };

    /// <inheritdoc />
    public override string ToString()
    {
        if (!AnalysisCompleted)
        {
            return $"Analysis failed: {ErrorMessage}";
        }

        return Passed
            ? $"Passed: {AssemblyPath} (no findings)"
            : $"Issues found: {AssemblyPath} ({Findings.Count} findings)";
    }
}
