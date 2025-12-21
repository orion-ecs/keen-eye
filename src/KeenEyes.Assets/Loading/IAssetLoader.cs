namespace KeenEyes.Assets;

/// <summary>
/// Interface for loading a specific type of asset from a stream.
/// </summary>
/// <typeparam name="T">The type of asset this loader produces.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to add support for loading custom asset types.
/// Each loader declares which file extensions it handles via <see cref="Extensions"/>.
/// </para>
/// <para>
/// Loaders are registered with <see cref="AssetManager.RegisterLoader{T}(IAssetLoader{T})"/>
/// and are automatically selected based on file extension when loading assets.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyFormatLoader : IAssetLoader&lt;MyAsset&gt;
/// {
///     public IReadOnlyList&lt;string&gt; Extensions =&gt; [".myf"];
///
///     public MyAsset Load(Stream stream, AssetLoadContext context)
///     {
///         // Parse the stream and return the asset
///         return new MyAsset(stream);
///     }
///
///     public async Task&lt;MyAsset&gt; LoadAsync(
///         Stream stream, AssetLoadContext context, CancellationToken ct)
///     {
///         return await Task.Run(() =&gt; Load(stream, context), ct);
///     }
/// }
/// </code>
/// </example>
public interface IAssetLoader<T> where T : class, IDisposable
{
    /// <summary>
    /// Gets the file extensions this loader handles.
    /// </summary>
    /// <remarks>
    /// Extensions should include the leading dot (e.g., ".png", ".jpg").
    /// Case is ignored when matching extensions.
    /// </remarks>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Synchronously loads an asset from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the asset data.</param>
    /// <param name="context">Context information for the load operation.</param>
    /// <returns>The loaded asset.</returns>
    /// <exception cref="AssetLoadException">Thrown when loading fails.</exception>
    T Load(Stream stream, AssetLoadContext context);

    /// <summary>
    /// Asynchronously loads an asset from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the asset data.</param>
    /// <param name="context">Context information for the load operation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes with the loaded asset.</returns>
    /// <exception cref="AssetLoadException">Thrown when loading fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<T> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the size of the asset in bytes.
    /// </summary>
    /// <param name="asset">The loaded asset.</param>
    /// <returns>Estimated size in bytes, or -1 if unknown.</returns>
    /// <remarks>
    /// This is used for cache size management. If the size cannot be determined,
    /// return -1 and the cache will use a default estimate.
    /// </remarks>
    long EstimateSize(T asset) => -1;
}
