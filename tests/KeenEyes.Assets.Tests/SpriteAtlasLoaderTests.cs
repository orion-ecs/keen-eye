using System.Numerics;
using System.Text.Json;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the sprite atlas loader.
/// </summary>
public class SpriteAtlasLoaderTests
{
    [Fact]
    public void SpriteAtlasLoader_Extensions_ContainsAtlas()
    {
        var loader = new SpriteAtlasLoader();

        Assert.Contains(".atlas", loader.Extensions);
    }

    [Fact]
    public void SpriteAtlasLoader_Extensions_ContainsJson()
    {
        var loader = new SpriteAtlasLoader();

        Assert.Contains(".json", loader.Extensions);
    }

    [Fact]
    public void SpriteAtlasLoader_Extensions_HasTwoFormats()
    {
        var loader = new SpriteAtlasLoader();

        Assert.Equal(2, loader.Extensions.Count);
    }

    [Fact]
    public void SpriteRegion_Width_ReturnsSourceRectWidth_WhenNotRotated()
    {
        var region = new SpriteRegion(
            "test",
            new Rectangle(0, 0, 32, 64),
            new Vector2(32, 64),
            new Vector2(0.5f, 0.5f),
            Rotated: false);

        Assert.Equal(32f, region.Width);
        Assert.Equal(64f, region.Height);
    }

    [Fact]
    public void SpriteRegion_Width_ReturnsSwapped_WhenRotated()
    {
        var region = new SpriteRegion(
            "test",
            new Rectangle(0, 0, 32, 64),
            new Vector2(64, 32),
            new Vector2(0.5f, 0.5f),
            Rotated: true);

        // When rotated, the stored rect is rotated, so width/height swap
        Assert.Equal(64f, region.Width);
        Assert.Equal(32f, region.Height);
    }

    [Fact]
    public void SpriteRegion_GetUVRect_ReturnsNormalizedCoordinates()
    {
        var region = new SpriteRegion(
            "test",
            new Rectangle(64, 128, 32, 32),
            new Vector2(32, 32),
            new Vector2(0.5f, 0.5f));

        var uvRect = region.GetUVRect(256, 256);

        Assert.Equal(0.25f, uvRect.X);  // 64/256
        Assert.Equal(0.5f, uvRect.Y);   // 128/256
        Assert.Equal(0.125f, uvRect.Width);  // 32/256
        Assert.Equal(0.125f, uvRect.Height); // 32/256
    }

    [Fact]
    public void SpriteRegion_PivotPixels_ReturnsCorrectPixelPosition()
    {
        var region = new SpriteRegion(
            "test",
            new Rectangle(0, 0, 100, 50),
            new Vector2(100, 50),
            new Vector2(0.25f, 0.75f));

        var pivotPixels = region.PivotPixels;

        Assert.Equal(25f, pivotPixels.X);   // 100 * 0.25
        Assert.Equal(37.5f, pivotPixels.Y); // 50 * 0.75
    }

    [Fact]
    public void TexturePackerFormat_ParsesCorrectly()
    {
        // Test that the TexturePacker JSON model can be deserialized
        var json = """
        {
            "frames": {
                "player_idle_0": {
                    "frame": { "x": 0, "y": 0, "w": 32, "h": 32 },
                    "rotated": false,
                    "trimmed": false,
                    "spriteSourceSize": { "x": 0, "y": 0, "w": 32, "h": 32 },
                    "sourceSize": { "w": 32, "h": 32 },
                    "pivot": { "x": 0.5, "y": 0.5 }
                },
                "player_idle_1": {
                    "frame": { "x": 32, "y": 0, "w": 32, "h": 32 },
                    "rotated": false,
                    "trimmed": false,
                    "spriteSourceSize": { "x": 0, "y": 0, "w": 32, "h": 32 },
                    "sourceSize": { "w": 32, "h": 32 },
                    "pivot": { "x": 0.5, "y": 0.5 }
                }
            },
            "meta": {
                "app": "https://www.codeandweb.com/texturepacker",
                "version": "1.0",
                "image": "player.png",
                "format": "RGBA8888",
                "size": { "w": 256, "h": 256 },
                "scale": "1"
            }
        }
        """;

        var atlas = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.TexturePackerAtlas);

        Assert.NotNull(atlas);
        Assert.NotNull(atlas.Frames);
        Assert.Equal(2, atlas.Frames.Count);
        Assert.Contains("player_idle_0", atlas.Frames.Keys);
        Assert.Contains("player_idle_1", atlas.Frames.Keys);

        var frame = atlas.Frames["player_idle_0"];
        Assert.NotNull(frame.Frame);
        Assert.Equal(0, frame.Frame.X);
        Assert.Equal(0, frame.Frame.Y);
        Assert.Equal(32, frame.Frame.W);
        Assert.Equal(32, frame.Frame.H);
        Assert.False(frame.Rotated);

        Assert.NotNull(atlas.Meta);
        Assert.Equal("player.png", atlas.Meta.Image);
    }

    [Fact]
    public void AsepriteFormat_ParsesCorrectly()
    {
        // Test that the Aseprite JSON model can be deserialized
        var json = """
        {
            "frames": {
                "player 0.aseprite": {
                    "frame": { "x": 0, "y": 0, "w": 16, "h": 16 },
                    "rotated": false,
                    "trimmed": false,
                    "spriteSourceSize": { "x": 0, "y": 0, "w": 16, "h": 16 },
                    "sourceSize": { "w": 16, "h": 16 },
                    "duration": 100
                },
                "player 1.aseprite": {
                    "frame": { "x": 16, "y": 0, "w": 16, "h": 16 },
                    "rotated": false,
                    "trimmed": false,
                    "spriteSourceSize": { "x": 0, "y": 0, "w": 16, "h": 16 },
                    "sourceSize": { "w": 16, "h": 16 },
                    "duration": 200
                }
            },
            "meta": {
                "app": "http://www.aseprite.org/",
                "version": "1.3.9-x64",
                "image": "player.png",
                "format": "RGBA8888",
                "size": { "w": 128, "h": 128 },
                "scale": "1",
                "frameTags": [
                    { "name": "idle", "from": 0, "to": 1, "direction": "forward", "color": "#000000ff" }
                ],
                "layers": [
                    { "name": "Layer 1", "opacity": 255, "blendMode": "normal" }
                ]
            }
        }
        """;

        var atlas = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AsepriteAtlas);

        Assert.NotNull(atlas);
        Assert.NotNull(atlas.Frames);
        Assert.Equal(2, atlas.Frames.Count);

        var frame = atlas.Frames["player 0.aseprite"];
        Assert.NotNull(frame.Frame);
        Assert.Equal(0, frame.Frame.X);
        Assert.Equal(16, frame.Frame.W);
        Assert.Equal(100, frame.Duration);

        var frame2 = atlas.Frames["player 1.aseprite"];
        Assert.Equal(200, frame2.Duration);

        Assert.NotNull(atlas.Meta);
        Assert.Equal("player.png", atlas.Meta.Image);
        Assert.Contains("Aseprite", atlas.Meta.App, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(atlas.Meta.FrameTags);
        Assert.Single(atlas.Meta.FrameTags);
        Assert.Equal("idle", atlas.Meta.FrameTags[0].Name);
    }

    [Fact]
    public void SpriteAtlasAsset_TryGetSprite_ReturnsTrue_WhenSpriteExists()
    {
        var mockTexture = CreateMockTextureAsset(256, 256);
        var regions = new List<SpriteRegion>
        {
            new("sprite1", new Rectangle(0, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("sprite2", new Rectangle(32, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f))
        };

        var atlas = new SpriteAtlasAsset(mockTexture, regions);

        Assert.True(atlas.TryGetSprite("sprite1", out var sprite));
        Assert.Equal("sprite1", sprite.Name);
        Assert.Equal(0f, sprite.SourceRect.X);
    }

    [Fact]
    public void SpriteAtlasAsset_TryGetSprite_ReturnsFalse_WhenSpriteDoesNotExist()
    {
        var mockTexture = CreateMockTextureAsset(256, 256);
        var regions = new List<SpriteRegion>
        {
            new("sprite1", new Rectangle(0, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f))
        };

        var atlas = new SpriteAtlasAsset(mockTexture, regions);

        Assert.False(atlas.TryGetSprite("nonexistent", out _));
    }

    [Fact]
    public void SpriteAtlasAsset_GetSpritesByPrefix_ReturnsMatchingSprites()
    {
        var mockTexture = CreateMockTextureAsset(256, 256);
        var regions = new List<SpriteRegion>
        {
            new("player_run_0", new Rectangle(0, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("player_run_1", new Rectangle(32, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("player_run_2", new Rectangle(64, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("enemy_idle_0", new Rectangle(0, 32, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f))
        };

        var atlas = new SpriteAtlasAsset(mockTexture, regions);
        var runFrames = atlas.GetSpritesByPrefix("player_run_").ToList();

        Assert.Equal(3, runFrames.Count);
        Assert.Equal("player_run_0", runFrames[0].Name);
        Assert.Equal("player_run_1", runFrames[1].Name);
        Assert.Equal("player_run_2", runFrames[2].Name);
    }

    [Fact]
    public void SpriteAtlasAsset_Contains_ReturnsCorrectly()
    {
        var mockTexture = CreateMockTextureAsset(256, 256);
        var regions = new List<SpriteRegion>
        {
            new("sprite1", new Rectangle(0, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f))
        };

        var atlas = new SpriteAtlasAsset(mockTexture, regions);

        Assert.True(atlas.Contains("sprite1"));
        Assert.False(atlas.Contains("sprite2"));
    }

    [Fact]
    public void SpriteAtlasAsset_SpriteCount_ReturnsCorrectCount()
    {
        var mockTexture = CreateMockTextureAsset(256, 256);
        var regions = new List<SpriteRegion>
        {
            new("sprite1", new Rectangle(0, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("sprite2", new Rectangle(32, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f)),
            new("sprite3", new Rectangle(64, 0, 32, 32), new Vector2(32, 32), new Vector2(0.5f, 0.5f))
        };

        var atlas = new SpriteAtlasAsset(mockTexture, regions);

        Assert.Equal(3, atlas.SpriteCount);
    }

    private static TextureAsset CreateMockTextureAsset(int width, int height)
    {
        // Use reflection to create a TextureAsset since its constructor is internal
        var constructor = typeof(TextureAsset).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(TextureHandle), typeof(int), typeof(int), typeof(TextureFormat), typeof(IGraphicsContext)],
            null);

        if (constructor != null)
        {
            return (TextureAsset)constructor.Invoke([new TextureHandle(1), width, height, TextureFormat.Rgba8, null]);
        }

        throw new InvalidOperationException("Could not create mock TextureAsset");
    }
}
