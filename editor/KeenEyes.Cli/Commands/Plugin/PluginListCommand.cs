// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command to list installed plugins.
/// </summary>
internal sealed class PluginListCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "list";

    /// <inheritdoc />
    public string Description => "List installed plugins";

    /// <inheritdoc />
    public string Usage => "list [--all | --enabled | --disabled]";

    /// <inheritdoc />
    public Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default)
    {
        // Parse filter option
        var filter = PluginFilter.All;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--all" or "-a":
                    filter = PluginFilter.All;
                    break;
                case "--enabled" or "-e":
                    filter = PluginFilter.Enabled;
                    break;
                case "--disabled" or "-d":
                    filter = PluginFilter.Disabled;
                    break;
                case "-h" or "--help":
                    ShowHelp(output);
                    return Task.FromResult(CommandResult.Success());
                default:
                    return Task.FromResult(CommandResult.InvalidArguments($"Unknown option: {args[i]}"));
            }
        }

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            var plugins = registry.GetInstalledPlugins();

            // Apply filter
            var filtered = filter switch
            {
                PluginFilter.Enabled => plugins.Where(p => p.Enabled).ToList(),
                PluginFilter.Disabled => plugins.Where(p => !p.Enabled).ToList(),
                _ => plugins.ToList()
            };

            if (filtered.Count == 0)
            {
                var filterText = filter switch
                {
                    PluginFilter.Enabled => "enabled ",
                    PluginFilter.Disabled => "disabled ",
                    _ => ""
                };
                output.WriteLine($"No {filterText}plugins installed.");
                return Task.FromResult(CommandResult.Success());
            }

            output.WriteLine($"Installed plugins ({filtered.Count}):");
            output.WriteLine();

            foreach (var plugin in filtered.OrderBy(p => p.PackageId))
            {
                var status = plugin.Enabled ? "[enabled]" : "[disabled]";
                output.WriteLine($"  {plugin.PackageId} v{plugin.Version} {status}");
                if (!string.IsNullOrWhiteSpace(plugin.Source))
                {
                    output.WriteVerbose($"    Source: {plugin.Source}");
                }

                output.WriteVerbose($"    Installed: {plugin.InstalledAt:yyyy-MM-dd HH:mm}");
            }

            return Task.FromResult(CommandResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(CommandResult.Failure($"Failed to list plugins: {ex.Message}"));
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin list [options]");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --all, -a        Show all plugins (default)");
        output.WriteLine("  --enabled, -e    Show only enabled plugins");
        output.WriteLine("  --disabled, -d   Show only disabled plugins");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes plugin list");
        output.WriteLine("  keeneyes plugin list --enabled");
    }

    private enum PluginFilter
    {
        All,
        Enabled,
        Disabled
    }
}
