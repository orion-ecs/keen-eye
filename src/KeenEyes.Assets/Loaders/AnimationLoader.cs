using System.Text.Json;
using KeenEyes.Animation.Data;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for .keanim animation files.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AnimationLoader"/> loads custom animation files that define
/// sprite-based animations with frame data and events. The format supports:
/// <list type="bullet">
///   <item><description>Named sprite references from an atlas</description></item>
///   <item><description>Per-frame or uniform timing</description></item>
///   <item><description>Animation events at specific times</description></item>
///   <item><description>Configurable wrap modes</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example .keanim file:
/// // {
/// //   "name": "player_run",
/// //   "atlas": "player.json",
/// //   "frameRate": 12,
/// //   "wrapMode": "loop",
/// //   "frames": [
/// //     { "sprite": "run_0" },
/// //     { "sprite": "run_1" },
/// //     { "sprite": "run_2", "duration": 0.15 }
/// //   ],
/// //   "events": [
/// //     { "time": 0.0, "name": "footstep_left" },
/// //     { "time": 0.25, "name": "footstep_right" }
/// //   ]
/// // }
/// </code>
/// </example>
public sealed class AnimationLoader : IAssetLoader<AnimationAsset>
{
    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".keanim"];

    /// <inheritdoc />
    public AnimationAsset Load(Stream stream, AssetLoadContext context)
    {
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var animJson = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson)
            ?? throw new AssetLoadException(context.Path, typeof(AnimationAsset), "Invalid animation file format");

        var name = animJson.Name ?? Path.GetFileNameWithoutExtension(context.Path);
        var wrapMode = ParseWrapMode(animJson.WrapMode);
        var defaultFrameDuration = 1f / Math.Max(animJson.FrameRate, 1f);

        // Load atlas if specified
        SpriteAtlasAsset? atlas = null;
        if (!string.IsNullOrEmpty(animJson.Atlas))
        {
            var animDir = Path.GetDirectoryName(context.Path) ?? "";
            var atlasPath = Path.Combine(animDir, animJson.Atlas).Replace('\\', '/');

            var atlasHandle = context.Manager.LoadDependency<SpriteAtlasAsset>(context.Path, atlasPath);
            atlas = atlasHandle.Asset;
        }

        // Build sprite sheet
        var spriteSheet = new SpriteSheet
        {
            Name = name,
            WrapMode = wrapMode
        };

        // Set texture from atlas if available
        if (atlas != null)
        {
            spriteSheet.Texture = atlas.Texture;
        }

        // Add frames
        if (animJson.Frames != null)
        {
            foreach (var frameJson in animJson.Frames)
            {
                var duration = frameJson.Duration ?? defaultFrameDuration;
                Rectangle sourceRect;

                if (atlas != null && !string.IsNullOrEmpty(frameJson.Sprite))
                {
                    // Get sprite from atlas
                    if (atlas.TryGetSprite(frameJson.Sprite, out var region))
                    {
                        // Convert to normalized UV coordinates
                        sourceRect = region.GetUVRect(atlas.Width, atlas.Height);
                    }
                    else
                    {
                        // Sprite not found - use placeholder
                        sourceRect = new Rectangle(0, 0, 1, 1);
                    }
                }
                else
                {
                    // No atlas or no sprite name - use full texture
                    sourceRect = new Rectangle(0, 0, 1, 1);
                }

                spriteSheet.AddFrame(sourceRect, duration);
            }
        }

        // Parse events
        var events = new List<AnimationEvent>();
        if (animJson.Events != null)
        {
            foreach (var eventJson in animJson.Events)
            {
                if (!string.IsNullOrEmpty(eventJson.Name))
                {
                    events.Add(new AnimationEvent(eventJson.Time, eventJson.Name, eventJson.Data));
                }
            }

            // Sort by time
            events.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        return new AnimationAsset(name, spriteSheet, events, atlas);
    }

    /// <inheritdoc />
    public async Task<AnimationAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // JSON parsing is CPU-bound, run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(AnimationAsset asset) => asset.SizeBytes;

    private static WrapMode ParseWrapMode(string? mode) => mode?.ToLowerInvariant() switch
    {
        "once" => WrapMode.Once,
        "loop" => WrapMode.Loop,
        "pingpong" => WrapMode.PingPong,
        "clampforever" => WrapMode.ClampForever,
        _ => WrapMode.Loop
    };
}
