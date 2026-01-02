// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.NuGet;

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command to search for plugins on NuGet.
/// </summary>
internal sealed class PluginSearchCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "search";

    /// <inheritdoc />
    public string Description => "Search for plugins on NuGet";

    /// <inheritdoc />
    public string Usage => "search <query> [--source <url>] [--take <n>]";

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

        // Parse arguments
        var query = args[0];
        string? source = null;
        var take = 20;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--source" or "-s" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--take" or "-t" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var takeValue))
                    {
                        take = takeValue;
                    }

                    break;
                case "-h" or "--help":
                    ShowHelp(output);
                    return CommandResult.Success();
                default:
                    return CommandResult.InvalidArguments($"Unknown option: {args[i]}");
            }
        }

        output.WriteVerbose($"Searching for '{query}'...");

        try
        {
            var nugetClient = new NuGetClient();
            var results = await nugetClient.SearchAsync(query, source, take, cancellationToken);

            if (results.Count == 0)
            {
                output.WriteLine($"No packages found matching '{query}'.");
                return CommandResult.Success();
            }

            output.WriteLine($"Found {results.Count} package(s):");
            output.WriteLine();

            foreach (var result in results)
            {
                output.WriteLine($"  {result.PackageId} ({result.LatestVersion})");
                if (!string.IsNullOrWhiteSpace(result.Description))
                {
                    var description = result.Description.Length > 80
                        ? result.Description[..77] + "..."
                        : result.Description;
                    output.WriteLine($"    {description}");
                }

                if (result.DownloadCount.HasValue)
                {
                    output.WriteLine($"    Downloads: {result.DownloadCount:N0}");
                }

                output.WriteLine();
            }

            return CommandResult.Success();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Search failed: {ex.Message}");
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin search <query> [options]");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <query>          Search term (package ID or keywords)");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --source, -s     NuGet source URL (default: nuget.org)");
        output.WriteLine("  --take, -t       Maximum results to show (default: 20)");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes plugin search KeenEyes");
        output.WriteLine("  keeneyes plugin search physics --take 10");
        output.WriteLine("  keeneyes plugin search MyPlugin --source https://my-feed.example.com/v3/index.json");
    }
}
