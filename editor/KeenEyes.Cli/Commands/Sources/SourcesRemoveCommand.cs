// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Sources;

/// <summary>
/// Command to remove a NuGet source.
/// </summary>
internal sealed class SourcesRemoveCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "remove";

    /// <inheritdoc />
    public string Description => "Remove a NuGet package source";

    /// <inheritdoc />
    public string Usage => "remove <name>";

    /// <inheritdoc />
    public Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            ShowHelp(output);
            return Task.FromResult(CommandResult.Success());
        }

        var name = args[0];

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            var existing = registry.GetSources().FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                return Task.FromResult(CommandResult.Failure($"Source '{name}' not found."));
            }

            registry.RemoveSource(name);
            registry.Save();

            output.WriteSuccess($"Removed source '{name}'");

            return Task.FromResult(CommandResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandResult.Failure($"Failed to remove source: {ex.Message}"));
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes sources remove <name>");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <name>           The name of the source to remove");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes sources remove mycompany");
    }
}
