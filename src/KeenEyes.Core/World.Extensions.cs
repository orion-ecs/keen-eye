using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

public sealed partial class World
{
    #region Extensions

    /// <summary>
    /// Sets or updates an extension value for this world.
    /// </summary>
    /// <typeparam name="T">The extension type. Must be a reference type.</typeparam>
    /// <param name="extension">The extension instance to store.</param>
    /// <remarks>
    /// <para>
    /// Extensions are typically set by plugins to expose custom APIs.
    /// For example, a physics plugin might expose a <c>PhysicsWorld</c> extension
    /// that provides raycast and collision query methods.
    /// </para>
    /// <para>
    /// If an extension of type <typeparamref name="T"/> already exists, it will be replaced.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a plugin:
    /// world.SetExtension(new PhysicsWorld());
    ///
    /// // In application code:
    /// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
    /// var hit = physics.Raycast(origin, direction);
    /// </code>
    /// </example>
    /// <seealso cref="GetExtension{T}"/>
    /// <seealso cref="TryGetExtension{T}"/>
    /// <seealso cref="HasExtension{T}"/>
    public void SetExtension<T>(T extension) where T : class
        => extensionManager.SetExtension(extension);

    /// <summary>
    /// Gets an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <returns>The extension instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no extension of type <typeparamref name="T"/> exists in this world.
    /// Use <see cref="TryGetExtension{T}"/> or <see cref="HasExtension{T}"/> to check
    /// existence before calling this method if the extension may not exist.
    /// </exception>
    /// <example>
    /// <code>
    /// var physics = world.GetExtension&lt;PhysicsWorld&gt;();
    /// var hit = physics.Raycast(origin, direction);
    /// </code>
    /// </example>
    public T GetExtension<T>() where T : class
        => extensionManager.GetExtension<T>();

    /// <summary>
    /// Tries to get an extension by type.
    /// </summary>
    /// <typeparam name="T">The extension type to retrieve.</typeparam>
    /// <param name="extension">When this method returns, contains the extension if found; otherwise, null.</param>
    /// <returns>True if the extension was found; false otherwise.</returns>
    /// <example>
    /// <code>
    /// if (world.TryGetExtension&lt;PhysicsWorld&gt;(out var physics))
    /// {
    ///     var hit = physics.Raycast(origin, direction);
    /// }
    /// </code>
    /// </example>
    public bool TryGetExtension<T>([MaybeNullWhen(false)] out T extension) where T : class
        => extensionManager.TryGetExtension(out extension);

    /// <summary>
    /// Checks if an extension of the specified type exists.
    /// </summary>
    /// <typeparam name="T">The extension type to check for.</typeparam>
    /// <returns>True if the extension exists; false otherwise.</returns>
    public bool HasExtension<T>() where T : class
        => extensionManager.HasExtension<T>();

    /// <summary>
    /// Removes an extension from this world.
    /// </summary>
    /// <typeparam name="T">The extension type to remove.</typeparam>
    /// <returns>True if the extension was found and removed; false otherwise.</returns>
    public bool RemoveExtension<T>() where T : class
        => extensionManager.RemoveExtension<T>();

    #endregion
}
