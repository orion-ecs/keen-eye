// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command to uninstall a plugin.
/// </summary>
internal sealed class PluginUninstallCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "uninstall";

    /// <inheritdoc />
    public string Description => "Uninstall a plugin";

    /// <inheritdoc />
    public string Usage => "uninstall <package-id>";

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

        var packageId = args[0];

        output.WriteLine($"Uninstalling plugin: {packageId}...");

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            if (!registry.IsInstalled(packageId))
            {
                return CommandResult.Failure($"Plugin '{packageId}' is not installed.");
            }

            var entry = registry.GetInstalledPlugin(packageId);
            if (entry == null)
            {
                return CommandResult.Failure($"Plugin '{packageId}' not found in registry.");
            }

            // Check for dependents
            var dependents = registry.GetDependentPlugins(packageId);
            if (dependents.Count > 0)
            {
                output.WriteWarning($"The following plugins depend on '{packageId}':");
                foreach (var dependent in dependents)
                {
                    output.WriteLine($"  {dependent.PackageId} v{dependent.Version}");
                }

                output.WriteLine();
                output.WriteWarning("Use --force to uninstall anyway (may break dependent plugins).");

                if (!args.Contains("--force"))
                {
                    return CommandResult.Failure("Cannot uninstall: plugin has dependents.");
                }
            }

            // Remove from registry
            registry.UnregisterPlugin(packageId);
            registry.Save();

            // Note: We don't delete from NuGet cache as other tools may use it
            // The plugin is simply unregistered from the KeenEyes plugin list

            output.WriteSuccess($"Successfully uninstalled {packageId}.");

            await Task.CompletedTask;
            return CommandResult.Success();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Uninstall failed: {ex.Message}");
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin uninstall <package-id> [options]");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <package-id>     The plugin package ID to uninstall");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --force          Uninstall even if other plugins depend on it");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes plugin uninstall KeenEyes.PhysicsEditor");
        output.WriteLine("  keeneyes plugin uninstall KeenEyes.PhysicsEditor --force");
    }
}
