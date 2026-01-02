// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Cli;

namespace KeenEyes.Cli.Tests;

/// <summary>
/// Test implementation of IConsoleOutput that captures output.
/// </summary>
internal sealed class TestConsoleOutput : IConsoleOutput
{
    private readonly List<string> lines = [];
    private readonly List<string> errors = [];
    private readonly List<string> warnings = [];
    private readonly List<string> successes = [];
    private readonly List<string> verbose = [];

    public bool Verbose { get; set; }
    public bool Quiet { get; set; }

    public IReadOnlyList<string> Lines => lines;
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;
    public IReadOnlyList<string> Successes => successes;
    public IReadOnlyList<string> VerboseMessages => verbose;

    public void Write(string message)
    {
        // Append to last line if exists
        if (lines.Count > 0)
        {
            lines[^1] += message;
        }
        else
        {
            lines.Add(message);
        }
    }

    public void WriteLine()
    {
        lines.Add(string.Empty);
    }

    public void WriteLine(string message)
    {
        lines.Add(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        lines.Add(string.Format(format, args));
    }

    public void WriteError(string message)
    {
        errors.Add(message);
    }

    public void WriteError(string format, params object[] args)
    {
        errors.Add(string.Format(format, args));
    }

    public void WriteWarning(string message)
    {
        warnings.Add(message);
    }

    public void WriteSuccess(string message)
    {
        successes.Add(message);
    }

    public void WriteVerbose(string message)
    {
        verbose.Add(message);
    }

    public void Clear()
    {
        lines.Clear();
        errors.Clear();
        warnings.Clear();
        successes.Clear();
        verbose.Clear();
    }
}
