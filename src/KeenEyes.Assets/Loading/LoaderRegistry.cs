using System.Collections.Concurrent;

namespace KeenEyes.Assets;

/// <summary>
/// Registry mapping file extensions to asset loaders.
/// </summary>
internal sealed class LoaderRegistry
{
    private readonly ConcurrentDictionary<string, object> loadersByExtension = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, object> loadersByType = new();

    /// <summary>
    /// Registers a loader for its supported extensions.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="loader">The loader to register.</param>
    public void Register<T>(IAssetLoader<T> loader) where T : class, IDisposable
    {
        loadersByType[typeof(T)] = loader;

        foreach (var ext in loader.Extensions)
        {
            var normalizedExt = NormalizeExtension(ext);
            loadersByExtension[normalizedExt] = loader;
        }
    }

    /// <summary>
    /// Gets a loader for the specified asset type and file extension.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="extension">The file extension.</param>
    /// <returns>The loader, or null if not found.</returns>
    public IAssetLoader<T>? GetLoader<T>(string extension) where T : class, IDisposable
    {
        var normalizedExt = NormalizeExtension(extension);

        if (loadersByExtension.TryGetValue(normalizedExt, out var loader) &&
            loader is IAssetLoader<T> typedLoader)
        {
            return typedLoader;
        }

        return null;
    }

    /// <summary>
    /// Gets a loader for the specified asset type.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <returns>The loader, or null if not found.</returns>
    public IAssetLoader<T>? GetLoader<T>() where T : class, IDisposable
    {
        if (loadersByType.TryGetValue(typeof(T), out var loader) &&
            loader is IAssetLoader<T> typedLoader)
        {
            return typedLoader;
        }

        return null;
    }

    /// <summary>
    /// Checks if a loader is registered for the given extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <returns>True if a loader is registered.</returns>
    public bool HasLoader(string extension)
    {
        var normalizedExt = NormalizeExtension(extension);
        return loadersByExtension.ContainsKey(normalizedExt);
    }

    /// <summary>
    /// Gets all registered extensions.
    /// </summary>
    /// <returns>Collection of supported extensions.</returns>
    public IEnumerable<string> GetSupportedExtensions()
        => loadersByExtension.Keys;

    private static string NormalizeExtension(string extension)
    {
        // Ensure extension starts with a dot and is lowercase
        if (string.IsNullOrEmpty(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
    }
}
