// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Cli;
using KeenEyes.Cli.Commands;
using KeenEyes.Cli.Commands.Migrate;
using KeenEyes.Cli.Commands.Plugin;
using KeenEyes.Cli.Commands.Sources;

// Build command registry
var commands = BuildCommands();

// Parse global options and route to commands
return await RunAsync(args, commands);

static Dictionary<string, ICommand> BuildCommands()
{
    return new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
    {
        // Plugin commands are accessed via "plugin <subcommand>"
        ["plugin"] = new PluginCommandGroup(),

        // Sources commands are accessed via "sources <subcommand>" (alias for "plugin sources")
        ["sources"] = new SourcesCommandGroup(),

        // Migration command for batch upgrading save files
        ["migrate"] = new MigrateCommand(),
    };
}

static async Task<int> RunAsync(string[] args, Dictionary<string, ICommand> commands)
{
    var output = new ConsoleOutput();
    var cts = new CancellationTokenSource();

    // Handle Ctrl+C
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    // Parse global options
    var (globalArgs, remainingArgs) = ParseGlobalOptions(args, output);

    if (globalArgs.ShowHelp || remainingArgs.Length == 0)
    {
        ShowHelp(output, commands);
        return 0;
    }

    if (globalArgs.ShowVersion)
    {
        ShowVersion(output);
        return 0;
    }

    // Get command name
    var commandName = remainingArgs[0];
    var commandArgs = remainingArgs.Length > 1 ? remainingArgs[1..] : [];

    if (!commands.TryGetValue(commandName, out var command))
    {
        output.WriteError($"Unknown command: {commandName}");
        output.WriteLine();
        ShowHelp(output, commands);
        return 1;
    }

    try
    {
        var result = await command.ExecuteAsync(commandArgs, output, cts.Token);

        if (result.Message != null)
        {
            if (result.IsSuccess)
            {
                output.WriteLine(result.Message);
            }
            else
            {
                output.WriteError(result.Message);
            }
        }

        return result.ExitCode;
    }
    catch (OperationCanceledException)
    {
        output.WriteLine();
        output.WriteWarning("Operation cancelled.");
        return 130;
    }
    catch (Exception ex)
    {
        output.WriteError(ex.Message);
        if (output.Verbose)
        {
            output.WriteError(ex.StackTrace ?? string.Empty);
        }

        return 1;
    }
}

static (GlobalOptions options, string[] remainingArgs) ParseGlobalOptions(string[] args, ConsoleOutput output)
{
    var options = new GlobalOptions();
    var remaining = new List<string>();
    var skipNext = false;

    for (var i = 0; i < args.Length; i++)
    {
        if (skipNext)
        {
            skipNext = false;
            continue;
        }

        var arg = args[i];

        switch (arg)
        {
            case "-h" or "--help":
                options.ShowHelp = true;
                break;
            case "-v" or "--version":
                options.ShowVersion = true;
                break;
            case "--verbose":
                output.Verbose = true;
                break;
            case "-q" or "--quiet":
                output.Quiet = true;
                break;
            default:
                remaining.Add(arg);
                break;
        }
    }

    return (options, remaining.ToArray());
}

static void ShowHelp(IConsoleOutput output, Dictionary<string, ICommand> commands)
{
    output.WriteLine("KeenEyes CLI - Plugin management for the KeenEyes Editor");
    output.WriteLine();
    output.WriteLine("Usage: keeneyes <command> [options]");
    output.WriteLine();
    output.WriteLine("Commands:");

    foreach (var (name, command) in commands.OrderBy(c => c.Key))
    {
        output.WriteLine($"  {name,-12} {command.Description}");
    }

    output.WriteLine();
    output.WriteLine("Global Options:");
    output.WriteLine("  -h, --help      Show help information");
    output.WriteLine("  -v, --version   Show version information");
    output.WriteLine("  --verbose       Show verbose output");
    output.WriteLine("  -q, --quiet     Suppress non-essential output");
    output.WriteLine();
    output.WriteLine("Use 'keeneyes <command> --help' for more information about a command.");
}

static void ShowVersion(IConsoleOutput output)
{
    var version = typeof(Program).Assembly.GetName().Version;
    output.WriteLine($"keeneyes {version?.ToString(3) ?? "0.0.0"}");
}

file record struct GlobalOptions
{
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
}
