// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// A single security finding from static analysis.
/// </summary>
public sealed class SecurityFinding
{
    /// <summary>
    /// Gets or sets the detected pattern type.
    /// </summary>
    public required DetectionPattern Pattern { get; init; }

    /// <summary>
    /// Gets or sets the severity of this finding.
    /// </summary>
    public required SecuritySeverity Severity { get; init; }

    /// <summary>
    /// Gets or sets a human-readable description of the finding.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets or sets the location in code (e.g., "Namespace.Type.Method").
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// Gets or sets the specific member reference that triggered this finding (e.g., "System.IO.File.ReadAllText").
    /// </summary>
    public string? MemberReference { get; init; }

    /// <summary>
    /// Gets or sets the IL offset where the pattern was detected.
    /// </summary>
    public int? ILOffset { get; init; }

    /// <summary>
    /// Creates a finding for a reflection usage.
    /// </summary>
    public static SecurityFinding Reflection(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.Reflection,
        Severity = SecuritySeverity.Medium,
        Description = $"Uses reflection API: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <summary>
    /// Creates a finding for unsafe code.
    /// </summary>
    public static SecurityFinding Unsafe(string location) => new()
    {
        Pattern = DetectionPattern.UnsafeCode,
        Severity = SecuritySeverity.High,
        Description = "Contains unsafe code or pointer operations",
        Location = location
    };

    /// <summary>
    /// Creates a finding for P/Invoke usage.
    /// </summary>
    public static SecurityFinding PInvoke(string location, string dllName, string entryPoint) => new()
    {
        Pattern = DetectionPattern.PInvoke,
        Severity = SecuritySeverity.High,
        Description = $"P/Invoke call to {dllName}::{entryPoint}",
        Location = location,
        MemberReference = $"{dllName}::{entryPoint}"
    };

    /// <summary>
    /// Creates a finding for file system access.
    /// </summary>
    public static SecurityFinding FileSystem(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.FileSystemAccess,
        Severity = SecuritySeverity.Medium,
        Description = $"File system access: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <summary>
    /// Creates a finding for network access.
    /// </summary>
    public static SecurityFinding Network(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.NetworkAccess,
        Severity = SecuritySeverity.Medium,
        Description = $"Network access: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <summary>
    /// Creates a finding for process execution.
    /// </summary>
    public static SecurityFinding ProcessExec(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.ProcessExecution,
        Severity = SecuritySeverity.Critical,
        Description = $"Process execution: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <summary>
    /// Creates a finding for environment access.
    /// </summary>
    public static SecurityFinding Environment(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.EnvironmentAccess,
        Severity = SecuritySeverity.Low,
        Description = $"Environment access: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <summary>
    /// Creates a finding for assembly loading.
    /// </summary>
    public static SecurityFinding AssemblyLoad(string location, string memberReference) => new()
    {
        Pattern = DetectionPattern.AssemblyLoading,
        Severity = SecuritySeverity.High,
        Description = $"Dynamic assembly loading: {memberReference}",
        Location = location,
        MemberReference = memberReference
    };

    /// <inheritdoc />
    public override string ToString() =>
        $"[{Severity}] {Pattern}: {Description} at {Location}";
}
