using System.Reflection;
using System.Runtime.Loader;

namespace KeenEyes.Editor.HotReload;

/// <summary>
/// A collectible assembly load context for loading game assemblies that can be unloaded.
/// </summary>
/// <remarks>
/// This context enables hot reload by allowing the game assembly to be unloaded
/// and replaced with a newly compiled version at runtime.
/// </remarks>
internal sealed class GameAssemblyContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Creates a new game assembly context.
    /// </summary>
    /// <param name="assemblyPath">Path to the main game assembly.</param>
    public GameAssemblyContext(string assemblyPath)
        : base(name: "GameCode", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve dependencies from the game assembly's directory
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Fall back to default context for framework assemblies
        return null;
    }

    /// <inheritdoc/>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }
}
