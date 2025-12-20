using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Animation.Data;

/// <summary>
/// A single frame in a sprite animation.
/// </summary>
/// <param name="SourceRect">The source rectangle in the texture (normalized 0-1 coordinates).</param>
/// <param name="Duration">The duration of this frame in seconds.</param>
public readonly record struct SpriteFrame(Rectangle SourceRect, float Duration);

/// <summary>
/// A sprite sheet containing frames for sprite-based animation.
/// </summary>
/// <remarks>
/// Sprite sheets are shared asset data. Multiple entities can reference the same sheet.
/// Animation state lives in the <see cref="Components.SpriteAnimator"/> component.
/// </remarks>
public sealed class SpriteSheet
{
    private readonly List<SpriteFrame> frames = [];

    /// <summary>
    /// Gets or sets the name of this sprite sheet.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the texture handle containing the sprite frames.
    /// </summary>
    public TextureHandle Texture { get; set; }

    /// <summary>
    /// Gets the frames in this sprite sheet.
    /// </summary>
    public IReadOnlyList<SpriteFrame> Frames => frames;

    /// <summary>
    /// Gets the total duration of all frames.
    /// </summary>
    public float TotalDuration { get; private set; }

    /// <summary>
    /// Gets or sets the default wrap mode for animations using this sheet.
    /// </summary>
    public WrapMode WrapMode { get; set; } = WrapMode.Loop;

    /// <summary>
    /// Adds a frame to the sprite sheet.
    /// </summary>
    /// <param name="sourceRect">The source rectangle in normalized texture coordinates.</param>
    /// <param name="duration">The duration of this frame.</param>
    public void AddFrame(Rectangle sourceRect, float duration)
    {
        frames.Add(new SpriteFrame(sourceRect, duration));
        TotalDuration += duration;
    }

    /// <summary>
    /// Adds frames from a uniform grid layout.
    /// </summary>
    /// <param name="columns">Number of columns in the grid.</param>
    /// <param name="rows">Number of rows in the grid.</param>
    /// <param name="frameCount">Total number of frames to add.</param>
    /// <param name="frameDuration">Duration of each frame.</param>
    public void AddGridFrames(int columns, int rows, int frameCount, float frameDuration)
    {
        var frameWidth = 1f / columns;
        var frameHeight = 1f / rows;

        for (var i = 0; i < frameCount; i++)
        {
            var col = i % columns;
            var row = i / columns;

            var sourceRect = new Rectangle(
                col * frameWidth,
                row * frameHeight,
                frameWidth,
                frameHeight);

            AddFrame(sourceRect, frameDuration);
        }
    }

    /// <summary>
    /// Gets the frame index and source rectangle for a given time.
    /// </summary>
    /// <param name="time">The current animation time.</param>
    /// <param name="wrapMode">The wrap mode to use.</param>
    /// <returns>The frame index and source rectangle.</returns>
    public (int Index, SpriteFrame Frame) GetFrameAtTime(float time, WrapMode? wrapMode = null)
    {
        if (frames.Count == 0)
        {
            return (0, default);
        }

        var mode = wrapMode ?? WrapMode;
        var wrappedTime = WrapTime(time, mode);

        var accumulated = 0f;
        for (var i = 0; i < frames.Count; i++)
        {
            accumulated += frames[i].Duration;
            if (wrappedTime < accumulated)
            {
                return (i, frames[i]);
            }
        }

        return (frames.Count - 1, frames[^1]);
    }

    private float WrapTime(float time, WrapMode mode)
    {
        if (TotalDuration <= 0f)
        {
            return 0f;
        }

        return mode switch
        {
            WrapMode.Once => Math.Clamp(time, 0f, TotalDuration),
            WrapMode.Loop => time >= 0f ? time % TotalDuration : TotalDuration + (time % TotalDuration),
            WrapMode.PingPong => WrapPingPong(time),
            WrapMode.ClampForever => Math.Max(time, 0f),
            _ => time
        };
    }

    private float WrapPingPong(float time)
    {
        var cycles = (int)(time / TotalDuration);
        var t = time % TotalDuration;
        return (cycles % 2 == 1) ? TotalDuration - t : t;
    }
}
