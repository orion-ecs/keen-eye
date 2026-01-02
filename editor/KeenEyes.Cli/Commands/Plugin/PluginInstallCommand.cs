// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Installation;
using KeenEyes.Editor.Plugins.NuGet;
using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Cli.Commands.Plugin;

/// <summary>
/// Command to install a plugin from NuGet.
/// </summary>
internal sealed class PluginInstallCommand : ICommand
{
    /// <inheritdoc />
    public string Name => "install";

    /// <inheritdoc />
    public string Description => "Install a plugin from NuGet";

    /// <inheritdoc />
    public string Usage => "install <package-id> [--version <ver>] [--source <url>]";

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
        var packageId = args[0];
        string? version = null;
        string? source = null;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--version" or "-v" when i + 1 < args.Length:
                    version = args[++i];
                    break;
                case "--source" or "-s" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "-h" or "--help":
                    ShowHelp(output);
                    return CommandResult.Success();
                default:
                    return CommandResult.InvalidArguments($"Unknown option: {args[i]}");
            }
        }

        output.WriteLine($"Installing plugin: {packageId}{(version != null ? $" v{version}" : "")}...");

        try
        {
            var registry = new PluginRegistry();
            registry.Load();

            if (registry.IsInstalled(packageId))
            {
                var existing = registry.GetInstalledPlugin(packageId);
                if (existing != null && (version == null || existing.Version == version))
                {
                    output.WriteWarning($"Plugin '{packageId}' is already installed (v{existing.Version}).");
                    return CommandResult.Success();
                }
            }

            var nugetClient = new NuGetClient();
            var installer = new PluginInstaller(nugetClient, registry);

            // Create installation plan
            output.WriteVerbose("Resolving package and dependencies...");
            var plan = await installer.CreatePlanAsync(packageId, version, source, cancellationToken);

            if (!plan.IsValid)
            {
                return CommandResult.Failure(plan.ErrorMessage ?? "Failed to create installation plan.");
            }

            // Show what will be installed
            if (plan.PackagesToInstall.Count > 1)
            {
                output.WriteLine("The following packages will be installed:");
                foreach (var pkg in plan.PackagesToInstall)
                {
                    output.WriteLine($"  {pkg.PackageId} v{pkg.Version}");
                }

                output.WriteLine();
            }

            // Execute installation
            var progress = new Progress<string>(msg => output.WriteVerbose(msg));
            var result = await installer.ExecuteAsync(plan, progress, cancellationToken);

            if (!result.Success)
            {
                return CommandResult.Failure(result.ErrorMessage ?? "Installation failed.");
            }

            output.WriteSuccess($"Successfully installed {packageId}" +
                               (result.InstalledVersion != null ? $" v{result.InstalledVersion}" : ""));

            return CommandResult.Success();
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Installation failed: {ex.Message}");
        }
    }

    private static void ShowHelp(IConsoleOutput output)
    {
        output.WriteLine("Usage: keeneyes plugin install <package-id> [options]");
        output.WriteLine();
        output.WriteLine("Arguments:");
        output.WriteLine("  <package-id>     The NuGet package ID to install");
        output.WriteLine();
        output.WriteLine("Options:");
        output.WriteLine("  --version, -v    Specific version to install (default: latest)");
        output.WriteLine("  --source, -s     NuGet source URL (default: nuget.org)");
        output.WriteLine("  --help, -h       Show help information");
        output.WriteLine();
        output.WriteLine("Examples:");
        output.WriteLine("  keeneyes plugin install KeenEyes.PhysicsEditor");
        output.WriteLine("  keeneyes plugin install KeenEyes.PhysicsEditor --version 1.2.0");
        output.WriteLine("  keeneyes plugin install MyPlugin --source https://my-feed.example.com/v3/index.json");
    }
}
