using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Manages plugin-provided extension APIs.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all extension operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Extensions are typically set by plugins to expose custom APIs.
/// For example, a physics plugin might expose a <c>PhysicsWorld</c> extension
/// that provides raycast and collision query methods.
/// </para>
/// <para>
/// This class is thread-safe: all extension operations can be called concurrently
/// from multiple threads.
/// </para>
/// </remarks>
internal sealed class ExtensionManager
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<Type, object> extensions = [];

    /// <summary>
    /// Sets or updates an extension value.
    /// </summary>
    /// <typeparam name="T">The extension type. Must be a reference type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// If an extension of the same type already exists and implements <see cref="IDisposable"/>,
    /// it will be disposed before being replaced.
    /// </remarks>
    internal void SetExtension<T>(T extension) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        lock (syncRoot)
        {
            if (extensions.TryGetValue(typeof(T), out var existing))
            {
                (existing as IDisposable)?.Dispose();
            }
            extensions[typeof(T)] = extension;
        }
    }

    /// <summary>
    /// Gets an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no extension of type <typeparamref name="T"/> exists.
    /// </exception>
    internal T GetExtension<T>() where T : class
    {
        lock (syncRoot)
        {
            if (!extensions.TryGetValue(typeof(T), out var extension))
            {
                throw new InvalidOperationException(
                    $"No extension of type '{typeof(T).Name}' exists in this world.");
            }
            return (T)extension;
        }
    }

    /// <summary>
    /// Tries to get an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found; otherwise, null.</param>
    /// <returns>True if the extension was found; false otherwise.</returns>
    internal bool TryGetExtension<T>([MaybeNullWhen(false)] out T extension) where T : class
    {
        lock (syncRoot)
        {
            if (extensions.TryGetValue(typeof(T), out var value))
            {
                extension = (T)value;
                return true;
            }
            extension = null;
            return false;
        }
    }

    /// <summary>
    /// Checks if an extension of the specified type exists.
    /// </summary>
    /// <typeparam name="T">The extension type to check for.</typeparam>
    /// <returns>True if the extension exists; false otherwise.</returns>
    internal bool HasExtension<T>() where T : class
    {
        lock (syncRoot)
        {
            return extensions.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Removes an extension.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    /// <remarks>
    /// If the extension implements <see cref="IDisposable"/>, it will be disposed.
    /// </remarks>
    internal bool RemoveExtension<T>() where T : class
    {
        lock (syncRoot)
        {
            if (extensions.TryGetValue(typeof(T), out var existing))
            {
                (existing as IDisposable)?.Dispose();
                return extensions.Remove(typeof(T));
            }
            return false;
        }
    }

    /// <summary>
    /// Clears all extensions.
    /// </summary>
    /// <remarks>
    /// All extensions implementing <see cref="IDisposable"/> will be disposed.
    /// </remarks>
    internal void Clear()
    {
        lock (syncRoot)
        {
            foreach (var extension in extensions.Values)
            {
                (extension as IDisposable)?.Dispose();
            }
            extensions.Clear();
        }
    }
}
