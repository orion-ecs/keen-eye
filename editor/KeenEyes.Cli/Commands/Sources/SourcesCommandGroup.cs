// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli.Commands.Sources;

/// <summary>
/// Command group for managing NuGet sources.
/// </summary>
internal sealed class SourcesCommandGroup : ICommand
{
    private readonly Dictionary<string, ICommand> subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourcesCommandGroup"/> class.
    /// </summary>
    public SourcesCommandGroup()
    {
        subcommands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["list"] = new SourcesListCommand(),
            ["add"] = new SourcesAddCommand(),
            ["remove"] = new SourcesRemoveCommand(),
        };
    }

    /// <inheritdoc />
    public string Name => "sources";

    /// <inheritdoc />
    public string Description => "Manage NuGet package sources";

    /// <inheritdoc />
    public string Usage => "sources <command> [options]";

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            ShowHelp(output);
            return CommandResult.Success();
        }

        var subcommandName = args[0];
        var subcommandArgs = args.Length > 1 ? args[1..] : [];

        if (!subcommands.TryGetValue(subcommandName, out var subcommand))
        {
            output.WriteError($"Unknown sources command: {subcommandName}");
            output.WriteLine();
            ShowHelp(output);
            return CommandResult.InvalidArguments($"Unknown command: {subcommandName}");
        }

        return await subcommand.ExecuteAsync(subcommandArgs, output, cancellationToken);
    }

    private void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes sources <command> [options]");
        output.WriteLine("       keeneyes plugin sources <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");

        foreach (var (name, command) in subcommands.OrderBy(c => c.Key))
        {
            output.WriteLine($"  {name,-12} {command.Description}");
        }

        output.WriteLine();
        output.WriteLine("Use 'keeneyes sources <command> --help' for more information.");
    }
}
