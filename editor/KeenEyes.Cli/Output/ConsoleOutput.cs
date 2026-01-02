// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli;

/// <summary>
/// Default console output implementation.
/// </summary>
internal sealed class ConsoleOutput : IConsoleOutput
{
    /// <inheritdoc />
    public bool Verbose { get; set; }

    /// <inheritdoc />
    public bool Quiet { get; set; }

    /// <inheritdoc />
    public void Write(string message)
    {
        if (!Quiet)
        {
            Console.Write(message);
        }
    }

    /// <inheritdoc />
    public void WriteLine()
    {
        if (!Quiet)
        {
            Console.WriteLine();
        }
    }

    /// <inheritdoc />
    public void WriteLine(string message)
    {
        if (!Quiet)
        {
            Console.WriteLine(message);
        }
    }

    /// <inheritdoc />
    public void WriteLine(string format, params object[] args)
    {
        if (!Quiet)
        {
            Console.WriteLine(format, args);
        }
    }

    /// <inheritdoc />
    public void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"error: {message}");
        Console.ForegroundColor = originalColor;
    }

    /// <inheritdoc />
    public void WriteError(string format, params object[] args)
    {
        WriteError(string.Format(format, args));
    }

    /// <inheritdoc />
    public void WriteWarning(string message)
    {
        if (!Quiet)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"warning: {message}");
            Console.ForegroundColor = originalColor;
        }
    }

    /// <inheritdoc />
    public void WriteSuccess(string message)
    {
        if (!Quiet)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
    }

    /// <inheritdoc />
    public void WriteVerbose(string message)
    {
        if (Verbose && !Quiet)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"verbose: {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
