using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// A loaded sprite atlas asset containing named sprite regions within a texture.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SpriteAtlasAsset"/> represents a texture atlas with named sprite
/// regions. It supports both TexturePacker and Aseprite JSON export formats.
/// </para>
/// <para>
/// Sprites can be retrieved by name using <see cref="TryGetSprite"/> or as
/// animation sequences using <see cref="GetSpritesByPrefix"/>.
/// </para>
/// <para>
/// Disposing a SpriteAtlasAsset releases the underlying texture. When loaded
/// through <see cref="AssetManager"/>, the texture is managed as a dependency
/// and reference counted appropriately.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var atlas = assetManager.Load&lt;SpriteAtlasAsset&gt;("sprites/player.json");
///
/// // Get a single sprite
/// if (atlas.Asset.TryGetSprite("player_idle_0", out var sprite))
/// {
///     renderer.DrawTextureRegion(atlas.Asset.Texture, destRect, sprite.SourceRect);
/// }
///
/// // Get all frames for an animation
/// var runFrames = atlas.Asset.GetSpritesByPrefix("player_run_").ToList();
/// </code>
/// </example>
public sealed class SpriteAtlasAsset : IDisposable
{
    private readonly TextureAsset textureAsset;
    private readonly Dictionary<string, SpriteRegion> sprites;
    private bool disposed;

    /// <summary>
    /// Gets the underlying GPU texture handle.
    /// </summary>
    public TextureHandle Texture => textureAsset.Handle;

    /// <summary>
    /// Gets the width of the atlas texture in pixels.
    /// </summary>
    public int Width => textureAsset.Width;

    /// <summary>
    /// Gets the height of the atlas texture in pixels.
    /// </summary>
    public int Height => textureAsset.Height;

    /// <summary>
    /// Gets all sprite regions in this atlas.
    /// </summary>
    public IReadOnlyDictionary<string, SpriteRegion> Sprites => sprites;

    /// <summary>
    /// Gets the number of sprites in this atlas.
    /// </summary>
    public int SpriteCount => sprites.Count;

    /// <summary>
    /// Gets the size of the atlas in bytes (estimated).
    /// </summary>
    public long SizeBytes => textureAsset.SizeBytes + (sprites.Count * 64);

    /// <summary>
    /// Creates a new sprite atlas asset.
    /// </summary>
    /// <param name="textureAsset">The underlying texture asset.</param>
    /// <param name="regions">The sprite regions within the atlas.</param>
    internal SpriteAtlasAsset(TextureAsset textureAsset, IEnumerable<SpriteRegion> regions)
    {
        this.textureAsset = textureAsset;
        sprites = regions.ToDictionary(r => r.Name, r => r);
    }

    /// <summary>
    /// Tries to get a sprite by name.
    /// </summary>
    /// <param name="name">The name of the sprite.</param>
    /// <param name="region">The sprite region if found.</param>
    /// <returns>True if the sprite was found, false otherwise.</returns>
    public bool TryGetSprite(string name, out SpriteRegion region)
        => sprites.TryGetValue(name, out region);

    /// <summary>
    /// Gets a sprite by name.
    /// </summary>
    /// <param name="name">The name of the sprite.</param>
    /// <returns>The sprite region.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the sprite doesn't exist.</exception>
    public SpriteRegion GetSprite(string name)
        => sprites[name];

    /// <summary>
    /// Gets all sprites whose names start with the given prefix, ordered by name.
    /// </summary>
    /// <param name="prefix">The name prefix to match.</param>
    /// <returns>An enumerable of matching sprite regions.</returns>
    /// <remarks>
    /// This is useful for retrieving animation frames where sprites are named
    /// with a common prefix followed by a frame number (e.g., "player_run_0",
    /// "player_run_1", "player_run_2").
    /// </remarks>
    public IEnumerable<SpriteRegion> GetSpritesByPrefix(string prefix)
        => sprites.Values
            .Where(s => s.Name.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(s => s.Name, StringComparer.Ordinal);

    /// <summary>
    /// Gets all sprite names in this atlas.
    /// </summary>
    /// <returns>An enumerable of sprite names.</returns>
    public IEnumerable<string> GetSpriteNames() => sprites.Keys;

    /// <summary>
    /// Checks if a sprite with the given name exists.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True if the sprite exists.</returns>
    public bool Contains(string name) => sprites.ContainsKey(name);

    /// <summary>
    /// Releases the underlying texture resource.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        textureAsset.Dispose();
    }
}
