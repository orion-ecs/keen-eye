// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli.Commands;

/// <summary>
/// Interface for CLI commands.
/// </summary>
internal interface ICommand
{
    /// <summary>
    /// Gets the command name (e.g., "install", "list").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the command description for help text.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the usage string (e.g., "install <package-id> [--version <ver>]").
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">Command arguments (excluding the command name itself).</param>
    /// <param name="output">Console output interface.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command result.</returns>
    Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default);
}
