// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli;

/// <summary>
/// Interface for console output, enabling testability.
/// </summary>
internal interface IConsoleOutput
{
    /// <summary>
    /// Writes a line to standard output.
    /// </summary>
    void WriteLine(string message);

    /// <summary>
    /// Writes a line to standard output with formatting.
    /// </summary>
    void WriteLine(string format, params object[] args);

    /// <summary>
    /// Writes to standard output without a newline.
    /// </summary>
    void Write(string message);

    /// <summary>
    /// Writes an error line to standard error.
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Writes an error line with formatting.
    /// </summary>
    void WriteError(string format, params object[] args);

    /// <summary>
    /// Writes a warning line.
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Writes a success line (typically green).
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes a blank line.
    /// </summary>
    void WriteLine();

    /// <summary>
    /// Gets or sets whether verbose output is enabled.
    /// </summary>
    bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets whether quiet mode is enabled (minimal output).
    /// </summary>
    bool Quiet { get; set; }

    /// <summary>
    /// Writes a verbose message (only shown if Verbose is true).
    /// </summary>
    void WriteVerbose(string message);
}
