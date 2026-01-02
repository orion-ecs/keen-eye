// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Sources;

/// <summary>
/// Command to list configured NuGet sources.
/// </summary>
internal sealed class SourcesListCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "list";

    /// <inheritdoc />
    public string Description => "List configured NuGet sources";

    /// <inheritdoc />
    public string Usage => "list";

    /// <inheritdoc />
    public Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            ShowHelp(output);
            return Task.FromResult(CommandResult.Success());
        }

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            var sources = registry.GetSources();

            if (sources.Count == 0)
            {
                output.WriteLine("No sources configured. Using default nuget.org.");
                return Task.FromResult(CommandResult.Success());
            }

            output.WriteLine("Configured package sources:");
            output.WriteLine();

            foreach (var source in sources)
            {
                var defaultIndicator = source.IsDefault ? " (default)" : "";
                output.WriteLine($"  {source.Name}{defaultIndicator}");
                output.WriteLine($"    {source.Url}");
                output.WriteLine();
            }

            return Task.FromResult(CommandResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandResult.Failure($"Failed to list sources: {ex.Message}"));
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes sources list");
        output.WriteLine();
        output.WriteLine("Lists all configured NuGet package sources for plugin installation.");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes sources list");
    }
}
