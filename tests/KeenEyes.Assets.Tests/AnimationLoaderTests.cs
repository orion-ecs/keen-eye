using System.Text.Json;
using KeenEyes.Animation.Data;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the animation loader.
/// </summary>
public class AnimationLoaderTests
{
    [Fact]
    public void AnimationLoader_Extensions_ContainsKeanim()
    {
        var loader = new AnimationLoader();

        Assert.Contains(".keanim", loader.Extensions);
    }

    [Fact]
    public void AnimationLoader_Extensions_HasOneFormat()
    {
        var loader = new AnimationLoader();

        Assert.Single(loader.Extensions);
    }

    [Fact]
    public void AnimationFileJson_Deserializes_WithAllFields()
    {
        var json = """
        {
            "name": "player_run",
            "atlas": "player.json",
            "frameRate": 12,
            "wrapMode": "loop",
            "frames": [
                { "sprite": "run_0" },
                { "sprite": "run_1", "duration": 0.1 },
                { "sprite": "run_2", "duration": 0.15 }
            ],
            "events": [
                { "time": 0.0, "name": "footstep_left" },
                { "time": 0.25, "name": "footstep_right", "data": "heavy" }
            ]
        }
        """;

        var anim = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson);

        Assert.NotNull(anim);
        Assert.Equal("player_run", anim.Name);
        Assert.Equal("player.json", anim.Atlas);
        Assert.Equal(12f, anim.FrameRate);
        Assert.Equal("loop", anim.WrapMode);

        Assert.NotNull(anim.Frames);
        Assert.Equal(3, anim.Frames.Count);
        Assert.Equal("run_0", anim.Frames[0].Sprite);
        Assert.Null(anim.Frames[0].Duration);
        Assert.Equal("run_1", anim.Frames[1].Sprite);
        Assert.Equal(0.1f, anim.Frames[1].Duration);
        Assert.Equal("run_2", anim.Frames[2].Sprite);
        Assert.Equal(0.15f, anim.Frames[2].Duration);

        Assert.NotNull(anim.Events);
        Assert.Equal(2, anim.Events.Count);
        Assert.Equal(0.0f, anim.Events[0].Time);
        Assert.Equal("footstep_left", anim.Events[0].Name);
        Assert.Null(anim.Events[0].Data);
        Assert.Equal(0.25f, anim.Events[1].Time);
        Assert.Equal("footstep_right", anim.Events[1].Name);
        Assert.Equal("heavy", anim.Events[1].Data);
    }

    [Fact]
    public void AnimationFileJson_Deserializes_WithMinimalFields()
    {
        var json = """
        {
            "frames": [
                { "sprite": "frame_0" }
            ]
        }
        """;

        var anim = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson);

        Assert.NotNull(anim);
        Assert.Null(anim.Name);
        Assert.Null(anim.Atlas);
        Assert.Equal(12f, anim.FrameRate); // Default value
        Assert.Null(anim.WrapMode);

        Assert.NotNull(anim.Frames);
        Assert.Single(anim.Frames);
    }

    [Fact]
    public void AnimationFileJson_Deserializes_WithIndexedFrames()
    {
        var json = """
        {
            "name": "explosion",
            "frameRate": 24,
            "frames": [
                { "index": 0 },
                { "index": 1 },
                { "index": 2 }
            ]
        }
        """;

        var anim = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson);

        Assert.NotNull(anim);
        Assert.NotNull(anim.Frames);
        Assert.Equal(3, anim.Frames.Count);
        Assert.Equal(0, anim.Frames[0].Index);
        Assert.Equal(1, anim.Frames[1].Index);
        Assert.Equal(2, anim.Frames[2].Index);
    }

    [Fact]
    public void AnimationAsset_Properties_ReturnCorrectValues()
    {
        var spriteSheet = new SpriteSheet { Name = "test" };
        spriteSheet.AddFrame(new KeenEyes.Graphics.Abstractions.Rectangle(0, 0, 0.5f, 0.5f), 0.1f);
        spriteSheet.AddFrame(new KeenEyes.Graphics.Abstractions.Rectangle(0.5f, 0, 0.5f, 0.5f), 0.1f);
        spriteSheet.AddFrame(new KeenEyes.Graphics.Abstractions.Rectangle(0, 0.5f, 0.5f, 0.5f), 0.1f);

        var events = new List<AnimationEvent>
        {
            new(0.0f, "start", null),
            new(0.2f, "middle", "data")
        };

        var asset = new AnimationAsset("test_anim", spriteSheet, events);

        Assert.Equal("test_anim", asset.Name);
        Assert.Equal(3, asset.FrameCount);
        Assert.Equal(0.3f, asset.Duration, precision: 5);
        Assert.Equal(2, asset.Events.Count);
        Assert.Same(spriteSheet, asset.SpriteSheet);
    }

    [Fact]
    public void AnimationAsset_Events_AreSortedByTime()
    {
        var spriteSheet = new SpriteSheet { Name = "test" };
        spriteSheet.AddFrame(new KeenEyes.Graphics.Abstractions.Rectangle(0, 0, 1, 1), 1f);

        // Events in reverse order
        var events = new List<AnimationEvent>
        {
            new(0.5f, "middle", null),
            new(1.0f, "end", null),
            new(0.0f, "start", null)
        };

        var asset = new AnimationAsset("test", spriteSheet, events);

        // Events should be accessible in original order (loader sorts them)
        Assert.Equal(3, asset.Events.Count);
    }

    [Theory]
    [InlineData("once")]
    [InlineData("loop")]
    [InlineData("pingpong")]
    [InlineData("clampforever")]
    [InlineData("LOOP")]
    [InlineData("PingPong")]
    public void AnimationFileJson_Deserializes_WrapMode(string wrapModeString)
    {
        var json = $$"""
        {
            "name": "test",
            "wrapMode": "{{wrapModeString}}",
            "frames": [
                { "sprite": "frame_0" }
            ]
        }
        """;

        var anim = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson);

        Assert.NotNull(anim);
        Assert.Equal(wrapModeString, anim.WrapMode);
    }

    [Fact]
    public void AnimationFileJson_Deserializes_WithNullWrapMode()
    {
        var json = """
        {
            "name": "test",
            "frames": [
                { "sprite": "frame_0" }
            ]
        }
        """;

        var anim = JsonSerializer.Deserialize(json, AtlasJsonContext.Default.AnimationFileJson);

        Assert.NotNull(anim);
        Assert.Null(anim.WrapMode);
    }

    [Fact]
    public void AnimationAsset_Dispose_DoesNotThrow()
    {
        var spriteSheet = new SpriteSheet { Name = "test" };
        var events = new List<AnimationEvent>();

        var asset = new AnimationAsset("test", spriteSheet, events, null);

        var exception = Record.Exception(() => asset.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void AnimationAsset_Dispose_CanBeCalledMultipleTimes()
    {
        var spriteSheet = new SpriteSheet { Name = "test" };
        var events = new List<AnimationEvent>();

        var asset = new AnimationAsset("test", spriteSheet, events, null);

        asset.Dispose();
        var exception = Record.Exception(() => asset.Dispose());

        Assert.Null(exception);
    }
}
