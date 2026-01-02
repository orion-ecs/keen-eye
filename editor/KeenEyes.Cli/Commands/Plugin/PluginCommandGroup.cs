// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command group for plugin management commands.
/// </summary>
internal sealed class PluginCommandGroup : ICommand
{
    private readonly Dictionary<string, ICommand> subcommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginCommandGroup"/> class.
    /// </summary>
    public PluginCommandGroup()
    {
        subcommands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["install"] = new PluginInstallCommand(),
            ["uninstall"] = new PluginUninstallCommand(),
            ["search"] = new PluginSearchCommand(),
            ["list"] = new PluginListCommand(),
            ["update"] = new PluginUpdateCommand(),
            ["sources"] = new Sources.SourcesCommandGroup(),
        };
    }

    /// <inheritdoc />
    public string Name => "plugin";

    /// <inheritdoc />
    public string Description => "Manage editor plugins";

    /// <inheritdoc />
    public string Usage => "plugin <command> [options]";

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
            output.WriteError($"Unknown plugin command: {subcommandName}");
            output.WriteLine();
            ShowHelp(output);
            return CommandResult.InvalidArguments($"Unknown command: {subcommandName}");
        }

        return await subcommand.ExecuteAsync(subcommandArgs, output, cancellationToken);
    }

    private void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin <command> [options]");
        output.WriteLine();
        output.WriteLine("Commands:");

        foreach (var (name, command) in subcommands.OrderBy(c => c.Key))
        {
            output.WriteLine($"  {name,-12} {command.Description}");
        }

        output.WriteLine();
        output.WriteLine("Use 'keeneyes plugin <command> --help' for more information.");
    }
}
