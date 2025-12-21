namespace KeenEyes.Assets;

/// <summary>
/// A serializable asset reference for use in ECS components.
/// </summary>
/// <typeparam name="T">The type of asset this reference points to.</typeparam>
/// <remarks>
/// <para>
/// <see cref="AssetRef{T}"/> stores an asset path that can be serialized with entity data.
/// At runtime, the <see cref="AssetResolutionSystem"/> automatically loads and resolves
/// the path to an actual <see cref="AssetHandle{T}"/>.
/// </para>
/// <para>
/// Use this in components when you need to reference assets that should be persisted
/// with the entity (e.g., sprite texture, audio clip).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a component with an asset reference
/// [Component]
/// public partial struct SpriteRenderer
/// {
///     public AssetRef&lt;TextureAsset&gt; Texture;
///     public Vector4 Color;
/// }
///
/// // Create an entity with an asset reference
/// world.Spawn()
///     .With(new SpriteRenderer
///     {
///         Texture = new AssetRef&lt;TextureAsset&gt; { Path = "textures/player.png" },
///         Color = Vector4.One
///     })
///     .Build();
///
/// // The AssetResolutionSystem will automatically load and resolve the texture
/// </code>
/// </example>
public struct AssetRef<T> : IComponent, IEquatable<AssetRef<T>>
    where T : class, IDisposable
{
    /// <summary>
    /// The path to the asset file.
    /// </summary>
    /// <remarks>
    /// This path is relative to the asset root directory. It is used for
    /// both loading the asset and for serialization.
    /// </remarks>
    public string Path;

    /// <summary>
    /// Internal handle ID set by the asset resolution system.
    /// </summary>
    /// <remarks>
    /// Do not set this manually. It is managed by <see cref="AssetResolutionSystem"/>.
    /// </remarks>
    internal int HandleId;

    /// <summary>
    /// Gets whether this reference has been resolved to a loaded handle.
    /// </summary>
    public readonly bool IsResolved => HandleId > 0;

    /// <summary>
    /// Gets whether this reference has a path set.
    /// </summary>
    public readonly bool HasPath => !string.IsNullOrEmpty(Path);

    /// <summary>
    /// Creates an asset reference from a path.
    /// </summary>
    /// <param name="path">The path to the asset.</param>
    /// <returns>An unresolved asset reference.</returns>
    public static AssetRef<T> FromPath(string path) => new() { Path = path };

    /// <summary>
    /// Clears the resolved handle, forcing re-resolution on next update.
    /// </summary>
    /// <remarks>
    /// Call this after changing the <see cref="Path"/> to trigger reloading.
    /// </remarks>
    public void Invalidate()
    {
        HandleId = 0;
    }

    /// <inheritdoc />
    public readonly bool Equals(AssetRef<T> other)
        => Path == other.Path;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj)
        => obj is AssetRef<T> other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode()
        => Path?.GetHashCode() ?? 0;

    /// <summary>
    /// Compares two asset references for equality.
    /// </summary>
    public static bool operator ==(AssetRef<T> left, AssetRef<T> right)
        => left.Equals(right);

    /// <summary>
    /// Compares two asset references for inequality.
    /// </summary>
    public static bool operator !=(AssetRef<T> left, AssetRef<T> right)
        => !left.Equals(right);

    /// <inheritdoc />
    public override readonly string ToString()
        => IsResolved ? $"AssetRef<{typeof(T).Name}>({Path}, Resolved)" : $"AssetRef<{typeof(T).Name}>({Path})";
}
