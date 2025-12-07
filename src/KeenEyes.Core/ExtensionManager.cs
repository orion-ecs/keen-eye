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
/// </remarks>
internal sealed class ExtensionManager
{
    private readonly Dictionary<Type, object> extensions = [];

    /// <summary>
    /// Sets or updates an extension value.
    /// </summary>
    /// <typeparam name="T">The extension type. Must be a reference type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    internal void SetExtension<T>(T extension) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        extensions[typeof(T)] = extension;
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
        if (!extensions.TryGetValue(typeof(T), out var extension))
        {
            throw new InvalidOperationException(
                $"No extension of type '{typeof(T).Name}' exists in this world.");
        }
        return (T)extension;
    }

    /// <summary>
    /// Tries to get an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found; otherwise, null.</param>
    /// <returns>True if the extension was found; false otherwise.</returns>
    internal bool TryGetExtension<T>([MaybeNullWhen(false)] out T extension) where T : class
    {
        if (extensions.TryGetValue(typeof(T), out var value))
        {
            extension = (T)value;
            return true;
        }
        extension = null;
        return false;
    }

    /// <summary>
    /// Checks if an extension of the specified type exists.
    /// </summary>
    /// <typeparam name="T">The extension type to check for.</typeparam>
    /// <returns>True if the extension exists; false otherwise.</returns>
    internal bool HasExtension<T>() where T : class
    {
        return extensions.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Removes an extension.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    internal bool RemoveExtension<T>() where T : class
    {
        return extensions.Remove(typeof(T));
    }

    /// <summary>
    /// Clears all extensions.
    /// </summary>
    internal void Clear()
    {
        extensions.Clear();
    }
}
