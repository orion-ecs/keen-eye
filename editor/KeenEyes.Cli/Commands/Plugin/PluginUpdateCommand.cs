// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Installation;
using KeenEyes.Editor.Plugins.NuGet;
using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command to update installed plugins.
/// </summary>
internal sealed class PluginUpdateCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "update";

    /// <inheritdoc />
    public string Description => "Update installed plugins";

    /// <inheritdoc />
    public string Usage => "update [<package-id>] [--all]";

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(
        string[] args,
        IConsoleOutput output,
        CancellationToken cancellationToken = default)
    {
        if (args.Length > 0 && args[0] is "-h" or "--help")
        {
            ShowHelp(output);
            return CommandResult.Success();
        }

        // Parse arguments
        string? packageId = null;
        var updateAll = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--all" or "-a":
                    updateAll = true;
                    break;
                case "-h" or "--help":
                    ShowHelp(output);
                    return CommandResult.Success();
                default:
                    if (args[i].StartsWith('-'))
                    {
                        return CommandResult.InvalidArguments($"Unknown option: {args[i]}");
                    }

                    packageId = args[i];
                    break;
            }
        }

        if (packageId == null && !updateAll)
        {
            output.WriteError("Specify a package ID or use --all to update all plugins.");
            return CommandResult.InvalidArguments("No package specified.");
        }

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            var nugetClient = new NuGetClient();
            var installer = new PluginInstaller(nugetClient, registry);

            IReadOnlyList<InstalledPluginEntry> pluginsToUpdate;

            if (updateAll)
            {
                pluginsToUpdate = registry.GetInstalledPlugins();
                output.WriteLine($"Checking {pluginsToUpdate.Count} plugin(s) for updates...");
            }
            else
            {
                var entry = registry.GetInstalledPlugin(packageId!);
                if (entry == null)
                {
                    return CommandResult.Failure($"Plugin '{packageId}' is not installed.");
                }

                pluginsToUpdate = [entry];
            }

            var updatedCount = 0;

            foreach (var plugin in pluginsToUpdate)
            {
                cancellationToken.ThrowIfCancellationRequested();

                output.WriteVerbose($"Checking {plugin.PackageId}...");

                // Get latest version
                var latestVersion = await nugetClient.GetLatestVersionAsync(
                    plugin.PackageId,
                    plugin.Source,
                    cancellationToken);

                if (latestVersion == null)
                {
                    output.WriteWarning($"Could not find {plugin.PackageId} on NuGet.");
                    continue;
                }

                if (latestVersion == plugin.Version)
                {
                    output.WriteVerbose($"{plugin.PackageId} is already at latest version ({plugin.Version}).");
                    continue;
                }

                output.WriteLine($"Updating {plugin.PackageId}: {plugin.Version} -> {latestVersion}");

                // Create and execute update plan
                var plan = await installer.CreatePlanAsync(
                    plugin.PackageId,
                    latestVersion,
                    plugin.Source,
                    cancellationToken);

                if (!plan.IsValid)
                {
                    output.WriteError($"Failed to plan update for {plugin.PackageId}: {plan.ErrorMessage}");
                    continue;
                }

                var progress = new Progress<string>(msg => output.WriteVerbose(msg));
                var result = await installer.ExecuteAsync(plan, progress, cancellationToken);

                if (result.Success)
                {
                    output.WriteSuccess($"Updated {plugin.PackageId} to v{latestVersion}");
                    updatedCount++;
                }
                else
                {
                    output.WriteError($"Failed to update {plugin.PackageId}: {result.ErrorMessage}");
                }
            }

            if (updateAll)
            {
                output.WriteLine();
                output.WriteLine($"Updated {updatedCount} of {pluginsToUpdate.Count} plugin(s).");
            }

            return CommandResult.Success();
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Cancelled();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Update failed: {ex.Message}");
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin update [<package-id>] [options]");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <package-id>     The plugin package ID to update (optional with --all)");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --all, -a        Update all installed plugins");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes plugin update KeenEyes.PhysicsEditor");
        output.WriteLine("  keeneyes plugin update --all");
    }
}
