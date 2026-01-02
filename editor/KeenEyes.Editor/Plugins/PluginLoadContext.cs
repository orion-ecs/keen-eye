// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Loader;

namespace KeenEyes.Editor.Plugins;

/// <summary>
/// An isolated assembly load context for loading editor plugins.
/// </summary>
/// <remarks>
/// <para>
/// Each plugin is loaded into its own <see cref="PluginLoadContext"/> to provide:
/// </para>
/// <list type="bullet">
/// <item>Assembly isolation - plugin dependencies don't conflict with other plugins</item>
/// <item>Optional unloading - collectible contexts can be unloaded for hot reload</item>
/// <item>Type identity - shared assemblies (KeenEyes.*) are loaded from the host</item>
/// </list>
/// </remarks>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;
    private readonly HashSet<string> sharedAssemblies;

    /// <summary>
    /// Gets the plugin ID this context was created for.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets the path to the plugin's main assembly.
    /// </summary>
    public string PluginPath { get; }

    /// <summary>
    /// Creates a new plugin load context.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="pluginPath">Path to the plugin's main assembly.</param>
    /// <param name="isCollectible">
    /// If true, the context can be unloaded. This is required for hot reload
    /// but adds memory overhead.
    /// </param>
    public PluginLoadContext(string pluginId, string pluginPath, bool isCollectible)
        : base(name: $"Plugin:{pluginId}", isCollectible: isCollectible)
    {
        PluginId = pluginId;
        PluginPath = pluginPath;
        resolver = new AssemblyDependencyResolver(pluginPath);

        // Assemblies that should be loaded from the host context to ensure
        // type identity across plugin boundaries. Plugins reference the same
        // IEditorPlugin, IEditorContext, etc. as the host.
        sharedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // KeenEyes assemblies
            "KeenEyes.Core",
            "KeenEyes.Abstractions",
            "KeenEyes.Common",
            "KeenEyes.Editor",
            "KeenEyes.Editor.Abstractions",

            // System assemblies are handled by the default context automatically
        };
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // For shared assemblies, delegate to the default context to ensure
        // type identity. This means IEditorPlugin from the plugin is the same
        // type as IEditorPlugin in the host.
        if (assemblyName.Name != null && sharedAssemblies.Contains(assemblyName.Name))
        {
            // Return null to fall back to the default load context
            return null;
        }

        // Try to resolve the assembly from the plugin's directory
        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Fall back to default context for other assemblies (framework, etc.)
        return null;
    }

    /// <inheritdoc/>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        // Try to resolve native libraries from the plugin's directory
        var libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }

    /// <summary>
    /// Adds an assembly name to the shared assemblies list.
    /// </summary>
    /// <remarks>
    /// Shared assemblies are loaded from the host context instead of the plugin's
    /// directory. This is necessary for type identity across plugin boundaries.
    /// </remarks>
    /// <param name="assemblyName">The assembly name (without .dll extension).</param>
    public void AddSharedAssembly(string assemblyName)
    {
        sharedAssemblies.Add(assemblyName);
    }
}
