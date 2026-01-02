// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Sources;

/// <summary>
/// Command to add a NuGet source.
/// </summary>
internal sealed class SourcesAddCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "add";

    /// <inheritdoc />
    public string Description => "Add a NuGet package source";

    /// <inheritdoc />
    public string Usage => "add <name> <url> [--default]";

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

        if (args.Length < 2)
        {
            return Task.FromResult(CommandResult.InvalidArguments("Both name and URL are required."));
        }

        var name = args[0];
        var url = args[1];
        var makeDefault = false;

        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--default" or "-d":
                    makeDefault = true;
                    break;
                case "-h" or "--help":
                    ShowHelp(output);
                    return Task.FromResult(CommandResult.Success());
                default:
                    return Task.FromResult(CommandResult.InvalidArguments($"Unknown option: {args[i]}"));
            }
        }

        // Validate URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            return Task.FromResult(CommandResult.InvalidArguments("URL must be a valid HTTP(S) URL."));
        }

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            // Check if name already exists
            var existing = registry.GetSources().FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                return Task.FromResult(CommandResult.Failure($"A source named '{name}' already exists."));
            }

            registry.AddSource(name, url, makeDefault);
            registry.Save();

            output.WriteSuccess($"Added source '{name}' ({url})");

            if (makeDefault)
            {
                output.WriteLine("Set as default source.");
            }

            return Task.FromResult(CommandResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandResult.Failure($"Failed to add source: {ex.Message}"));
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes sources add <name> <url> [options]");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <name>           A friendly name for the source");
        output.WriteLine("  <url>            The NuGet V3 feed URL");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --default, -d    Make this the default source");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes sources add mycompany https://pkgs.mycompany.com/v3/index.json");
        output.WriteLine("  keeneyes sources add internal https://nuget.internal.com/v3/index.json --default");
    }
}
