namespace KeenEyes.Assets;

/// <summary>
/// Internal cache entry tracking an asset's state, reference count, and metadata.
/// </summary>
/// <param name="id">Unique identifier.</param>
/// <param name="path">File path.</param>
/// <param name="assetType">Type of asset.</param>
internal sealed class AssetEntry(int id, string path, Type assetType)
{
    private int refCount = 1; // Start with one reference

    /// <summary>
    /// Gets the unique identifier for this asset.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Gets the file path of this asset.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Gets the type of asset stored.
    /// </summary>
    public Type AssetType { get; } = assetType;

    /// <summary>
    /// Gets or sets the loaded asset instance.
    /// </summary>
    public object? Asset { get; set; }

    /// <summary>
    /// Gets or sets the current loading state.
    /// </summary>
    public AssetState State { get; set; } = AssetState.Pending;

    /// <summary>
    /// Gets or sets the exception if loading failed.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets the current reference count.
    /// </summary>
    public int RefCount => refCount;

    /// <summary>
    /// Gets or sets the last access time.
    /// </summary>
    public DateTime LastAccess { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the size of the asset in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Increments the reference count.
    /// </summary>
    /// <returns>The new reference count.</returns>
    public int AddRef()
    {
        LastAccess = DateTime.UtcNow;
        return Interlocked.Increment(ref refCount);
    }

    /// <summary>
    /// Decrements the reference count.
    /// </summary>
    /// <returns>True if the reference count reached zero.</returns>
    public bool Release()
    {
        var newCount = Interlocked.Decrement(ref refCount);
        return newCount <= 0;
    }

    /// <summary>
    /// Disposes the asset if it implements IDisposable.
    /// </summary>
    public void DisposeAsset()
    {
        if (Asset is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Asset = null;
        State = AssetState.Unloaded;
    }
}
