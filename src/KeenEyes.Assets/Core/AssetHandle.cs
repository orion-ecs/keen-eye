namespace KeenEyes.Assets;

/// <summary>
/// A type-safe handle to a loaded asset with automatic reference counting.
/// </summary>
/// <typeparam name="T">The type of asset this handle references.</typeparam>
/// <remarks>
/// <para>
/// Asset handles use reference counting to manage asset lifetime. Each handle
/// holds a reference to the asset, and the asset is eligible for unloading
/// when all handles have been disposed.
/// </para>
/// <para>
/// Always dispose handles when you're done with them to release the reference.
/// Failing to dispose handles will keep assets in memory indefinitely.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load and use an asset
/// using var texture = assets.Load&lt;TextureAsset&gt;("textures/player.png");
/// if (texture.IsLoaded)
/// {
///     graphics.DrawSprite(texture.Asset!.Handle, position);
/// }
/// // Handle is disposed here, releasing the reference
/// </code>
/// </example>
public readonly struct AssetHandle<T> : IDisposable, IEquatable<AssetHandle<T>>
    where T : class, IDisposable
{
    /// <summary>
    /// Gets an invalid asset handle.
    /// </summary>
    public static AssetHandle<T> Invalid => default;

    internal readonly int Id;
    internal readonly AssetManager? Manager;

    /// <summary>
    /// Creates a new asset handle.
    /// </summary>
    /// <param name="id">The internal asset ID.</param>
    /// <param name="manager">The asset manager that owns this asset.</param>
    internal AssetHandle(int id, AssetManager manager)
    {
        Id = id;
        Manager = manager;
    }

    /// <summary>
    /// Gets whether this handle is valid (refers to a tracked asset).
    /// </summary>
    /// <remarks>
    /// A valid handle may still refer to an asset that has not finished loading
    /// or has failed to load. Check <see cref="State"/> or <see cref="IsLoaded"/>
    /// to determine if the asset is ready to use.
    /// </remarks>
    public bool IsValid => Id > 0 && Manager != null;

    /// <summary>
    /// Gets the current state of the asset.
    /// </summary>
    public AssetState State => IsValid ? Manager!.GetState(Id) : AssetState.Invalid;

    /// <summary>
    /// Gets whether the asset is loaded and ready to use.
    /// </summary>
    public bool IsLoaded => State == AssetState.Loaded;

    /// <summary>
    /// Gets whether the asset is currently loading.
    /// </summary>
    public bool IsLoading => State is AssetState.Pending or AssetState.Loading;

    /// <summary>
    /// Gets whether the asset failed to load.
    /// </summary>
    public bool IsFailed => State == AssetState.Failed;

    /// <summary>
    /// Gets the loaded asset, or null if not loaded.
    /// </summary>
    /// <remarks>
    /// Check <see cref="IsLoaded"/> before accessing this property.
    /// This property returns null if the asset is not loaded, failed to load,
    /// or has been unloaded.
    /// </remarks>
    public T? Asset => IsValid ? Manager!.TryGetAsset<T>(Id) : null;

    /// <summary>
    /// Gets the path of this asset.
    /// </summary>
    public string? Path => IsValid ? Manager!.GetPath(Id) : null;

    /// <summary>
    /// Releases this handle's reference to the asset.
    /// </summary>
    /// <remarks>
    /// After disposal, this handle becomes invalid. The asset may be unloaded
    /// if this was the last reference (depending on cache policy).
    /// </remarks>
    public void Dispose()
    {
        if (IsValid)
        {
            Manager!.Release(Id);
        }
    }

    /// <summary>
    /// Acquires an additional reference to this asset.
    /// </summary>
    /// <returns>A new handle to the same asset.</returns>
    /// <remarks>
    /// Use this when you need to share an asset reference. The returned handle
    /// must also be disposed when no longer needed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
    public AssetHandle<T> Acquire()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot acquire reference from an invalid handle.");
        }

        Manager!.AddRef(Id);
        return new AssetHandle<T>(Id, Manager);
    }

    /// <inheritdoc />
    public bool Equals(AssetHandle<T> other)
        => Id == other.Id && ReferenceEquals(Manager, other.Manager);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is AssetHandle<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Id, Manager);

    /// <summary>
    /// Compares two asset handles for equality.
    /// </summary>
    public static bool operator ==(AssetHandle<T> left, AssetHandle<T> right)
        => left.Equals(right);

    /// <summary>
    /// Compares two asset handles for inequality.
    /// </summary>
    public static bool operator !=(AssetHandle<T> left, AssetHandle<T> right)
        => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString()
        => IsValid ? $"AssetHandle<{typeof(T).Name}>({Path}, {State})" : "AssetHandle<Invalid>";
}
