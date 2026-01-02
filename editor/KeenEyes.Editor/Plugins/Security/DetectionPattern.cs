// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Patterns that can be detected during static analysis of plugin assemblies.
/// </summary>
public enum DetectionPattern
{
    /// <summary>
    /// Use of reflection APIs (Type.GetMethod, Activator.CreateInstance, etc.).
    /// </summary>
    Reflection,

    /// <summary>
    /// Unsafe code blocks or pointer operations.
    /// </summary>
    UnsafeCode,

    /// <summary>
    /// Platform invocation (DllImport, LibraryImport).
    /// </summary>
    PInvoke,

    /// <summary>
    /// File system operations (System.IO).
    /// </summary>
    FileSystemAccess,

    /// <summary>
    /// Network operations (System.Net).
    /// </summary>
    NetworkAccess,

    /// <summary>
    /// Process execution (Process.Start, etc.).
    /// </summary>
    ProcessExecution,

    /// <summary>
    /// Environment variable access.
    /// </summary>
    EnvironmentAccess,

    /// <summary>
    /// Windows registry access.
    /// </summary>
    RegistryAccess,

    /// <summary>
    /// Manual thread creation (Thread, ThreadPool).
    /// </summary>
    ThreadCreation,

    /// <summary>
    /// Loading additional assemblies at runtime.
    /// </summary>
    AssemblyLoading,

    /// <summary>
    /// Cryptographic operations.
    /// </summary>
    Cryptography,

    /// <summary>
    /// Clipboard access.
    /// </summary>
    ClipboardAccess
}

/// <summary>
/// Severity levels for security findings.
/// </summary>
public enum SecuritySeverity
{
    /// <summary>
    /// Informational only, no security concern.
    /// </summary>
    Info,

    /// <summary>
    /// Minor concern, usually safe in context.
    /// </summary>
    Low,

    /// <summary>
    /// Should be reviewed by the user.
    /// </summary>
    Medium,

    /// <summary>
    /// Requires explicit justification or permission.
    /// </summary>
    High,

    /// <summary>
    /// Block loading unless explicitly allowed.
    /// </summary>
    Critical
}

/// <summary>
/// Mode for how analysis findings are handled.
/// </summary>
public enum AnalysisMode
{
    /// <summary>
    /// Log warnings but allow loading.
    /// </summary>
    WarnOnly,

    /// <summary>
    /// Block loading if patterns detected.
    /// </summary>
    Block,

    /// <summary>
    /// Require user confirmation for flagged patterns.
    /// </summary>
    PromptUser
}
