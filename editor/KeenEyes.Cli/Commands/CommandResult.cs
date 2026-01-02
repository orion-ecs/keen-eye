// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli.Commands;

/// <summary>
/// Represents the result of a CLI command execution.
/// </summary>
/// <param name="ExitCode">The process exit code (0 for success).</param>
/// <param name="Message">Optional message to display.</param>
internal readonly record struct CommandResult(int ExitCode, string? Message = null)
{
    /// <summary>
    /// Gets a value indicating whether the command succeeded.
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CommandResult Success() => new(0);

    /// <summary>
    /// Creates a successful result with a message.
    /// </summary>
    public static CommandResult Success(string message) => new(0, message);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static CommandResult Failure(string message, int exitCode = 1) => new(exitCode, message);

    /// <summary>
    /// Creates a result indicating invalid arguments.
    /// </summary>
    public static CommandResult InvalidArguments(string message) => new(2, message);

    /// <summary>
    /// Creates a result indicating the command was cancelled.
    /// </summary>
    public static CommandResult Cancelled() => new(130, "Operation cancelled.");
}
