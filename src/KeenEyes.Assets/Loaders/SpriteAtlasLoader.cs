using System.Numerics;
using System.Text.Json;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for sprite atlas assets in TexturePacker and Aseprite JSON formats.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SpriteAtlasLoader"/> supports two common sprite atlas formats:
/// <list type="bullet">
///   <item><description>TexturePacker JSON (hash or array format)</description></item>
///   <item><description>Aseprite JSON export</description></item>
/// </list>
/// </para>
/// <para>
/// The loader automatically detects the format based on the JSON structure
/// and metadata. It loads the referenced texture as a dependency, ensuring
/// proper reference counting and cleanup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load a sprite atlas
/// var atlasHandle = assetManager.Load&lt;SpriteAtlasAsset&gt;("sprites/player.json");
/// var atlas = atlasHandle.Asset;
///
/// // Draw a sprite from the atlas
/// if (atlas.TryGetSprite("player_idle_0", out var sprite))
/// {
///     renderer.DrawTextureRegion(atlas.Texture, destRect, sprite.SourceRect);
/// }
/// </code>
/// </example>
public sealed class SpriteAtlasLoader : IAssetLoader<SpriteAtlasAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".atlas", ".json"];

    /// <inheritdoc />
    public SpriteAtlasAsset Load(Stream stream, AssetLoadContext context)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // Parse the JSON and extract sprite regions
        var (imagePath, regions) = ParseAtlasJson(json, context.Path);

        // Resolve the texture path relative to the atlas file
        var atlasDir = Path.GetDirectoryName(context.Path) ?? "";
        var texturePath = Path.Combine(atlasDir, imagePath).Replace('\\', '/');

        // Load the texture as a dependency
        var textureHandle = context.Manager.LoadDependency<TextureAsset>(context.Path, texturePath);
        var textureAsset = textureHandle.Asset
            ?? throw new AssetLoadException(context.Path, typeof(SpriteAtlasAsset),
                $"Failed to load atlas texture: {texturePath}");

        return new SpriteAtlasAsset(textureAsset, regions);
    }

    /// <inheritdoc />
    public async Task<SpriteAtlasAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // JSON parsing is CPU-bound, run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(SpriteAtlasAsset asset) => asset.SizeBytes;

    private static (string imagePath, List<SpriteRegion> regions) ParseAtlasJson(string json, string path)
    {
        // First, try to detect the format by peeking at the JSON structure
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Check for Aseprite by looking at the meta.app property
        if (root.TryGetProperty("meta", out var meta) &&
            meta.TryGetProperty("app", out var app))
        {
            var appName = app.GetString() ?? "";
            if (appName.Contains("Aseprite", StringComparison.OrdinalIgnoreCase))
            {
                return ParseAsepriteFormat(json, path);
            }
        }

        // Default to TexturePacker format
        return ParseTexturePackerFormat(json, path);
    }

    private static (string, List<SpriteRegion>) ParseTexturePackerFormat(string json, string path)
    {
        var atlas = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.TexturePackerAtlas)
            ?? throw new AssetLoadException(path, typeof(SpriteAtlasAsset), "Invalid TexturePacker atlas format");

        var imagePath = atlas.Meta?.Image
            ?? throw new AssetLoadException(path, typeof(SpriteAtlasAsset), "Atlas missing image path");

        var regions = new List<SpriteRegion>();

        if (atlas.Frames != null)
        {
            foreach (KeyValuePair<string, TexturePackerFrame> kvp in atlas.Frames)
            {
                var name = kvp.Key;
                var frame = kvp.Value;

                if (frame.Frame == null)
                {
                    continue;
                }

                var pivot = frame.Pivot != null
                    ? new Vector2(frame.Pivot.X, frame.Pivot.Y)
                    : new Vector2(0.5f, 0.5f);

                var originalSize = frame.SourceSize != null
                    ? new Vector2(frame.SourceSize.W, frame.SourceSize.H)
                    : new Vector2(frame.Frame.W, frame.Frame.H);

                var sourceRect = new Rectangle(
                    frame.Frame.X,
                    frame.Frame.Y,
                    frame.Frame.W,
                    frame.Frame.H);

                regions.Add(new SpriteRegion(
                    name,
                    sourceRect,
                    originalSize,
                    pivot,
                    frame.Rotated));
            }
        }

        return (imagePath, regions);
    }

    private static (string, List<SpriteRegion>) ParseAsepriteFormat(string json, string path)
    {
        var atlas = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AsepriteAtlas)
            ?? throw new AssetLoadException(path, typeof(SpriteAtlasAsset), "Invalid Aseprite atlas format");

        var imagePath = atlas.Meta?.Image
            ?? throw new AssetLoadException(path, typeof(SpriteAtlasAsset), "Atlas missing image path");

        var regions = new List<SpriteRegion>();

        if (atlas.Frames != null)
        {
            foreach (KeyValuePair<string, AsepriteFrame> kvp in atlas.Frames)
            {
                var name = kvp.Key;
                var frame = kvp.Value;

                if (frame.Frame == null)
                {
                    continue;
                }

                var originalSize = frame.SourceSize != null
                    ? new Vector2(frame.SourceSize.W, frame.SourceSize.H)
                    : new Vector2(frame.Frame.W, frame.Frame.H);

                var sourceRect = new Rectangle(
                    frame.Frame.X,
                    frame.Frame.Y,
                    frame.Frame.W,
                    frame.Frame.H);

                // Aseprite doesn't export pivots by default; use center
                var pivot = new Vector2(0.5f, 0.5f);

                regions.Add(new SpriteRegion(
                    name,
                    sourceRect,
                    originalSize,
                    pivot,
                    frame.Rotated,
                    frame.Duration));
            }
        }

        return (imagePath, regions);
    }
}
