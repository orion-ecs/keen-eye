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

    // Types whose current instance is caller-owned (registered with owned: false).
    // The manager never disposes these on replacement, removal, or clear; the
    // registrant that created the instance is responsible for disposing it.
    // Owned extensions are the common case, so tracking only the exceptions keeps
    // this set small.
    private readonly HashSet<Type> unownedExtensions = [];

    /// <summary>
    /// Sets or updates an extension value.
    /// </summary>
    /// <typeparam name="T">The extension type. Must be a reference type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <param name="owned">
    /// When <c>true</c> (the default) the manager takes ownership of the instance and
    /// disposes it (if it implements <see cref="IDisposable"/>) when it is replaced,
    /// removed, or the world is torn down. When <c>false</c> the caller retains ownership
    /// and the manager never disposes it.
    /// </param>
    /// <remarks>
    /// If an owned extension of the same type already exists and implements
    /// <see cref="IDisposable"/>, it is disposed before being replaced. Re-setting the
    /// exact same instance never disposes it, regardless of ownership.
    /// </remarks>
    internal void SetExtension<T>(T extension, bool owned = true) where T : class
    {
        ArgumentNullException.ThrowIfNull(extension);
        lock (syncRoot)
        {
            // Only dispose the previous instance when it is a different, manager-owned
            // object. Re-setting the same instance (or replacing a caller-owned one)
            // must never dispose it out from under a live user.
            if (extensions.TryGetValue(typeof(T), out var existing)
                && !ReferenceEquals(existing, extension)
                && !unownedExtensions.Contains(typeof(T)))
            {
                (existing as IDisposable)?.Dispose();
            }

            extensions[typeof(T)] = extension;
            if (owned)
            {
                unownedExtensions.Remove(typeof(T));
            }
            else
            {
                unownedExtensions.Add(typeof(T));
            }
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
    /// If the extension is manager-owned and implements <see cref="IDisposable"/>, it is
    /// disposed. Caller-owned extensions (registered with <c>owned: false</c>) are removed
    /// without being disposed, leaving disposal to the registrant that created them.
    /// </remarks>
    internal bool RemoveExtension<T>() where T : class
    {
        lock (syncRoot)
        {
            if (extensions.Remove(typeof(T), out var existing))
            {
                // HashSet.Remove returns true when the type was caller-owned; in that
                // case the manager must not dispose it.
                if (!unownedExtensions.Remove(typeof(T)))
                {
                    (existing as IDisposable)?.Dispose();
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Clears all extensions.
    /// </summary>
    /// <remarks>
    /// All manager-owned extensions implementing <see cref="IDisposable"/> are disposed.
    /// Caller-owned extensions (registered with <c>owned: false</c>) are removed without
    /// being disposed.
    /// </remarks>
    internal void Clear()
    {
        lock (syncRoot)
        {
            foreach (var (type, extension) in extensions)
            {
                if (!unownedExtensions.Contains(type))
                {
                    (extension as IDisposable)?.Dispose();
                }
            }
            extensions.Clear();
            unownedExtensions.Clear();
        }
    }
}
