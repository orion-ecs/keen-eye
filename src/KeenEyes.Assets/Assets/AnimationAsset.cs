using KeenEyes.Animation.Data;

namespace KeenEyes.Assets;

/// <summary>
/// A loaded animation asset containing a sprite sheet with animation data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AnimationAsset"/> represents a sprite-based animation loaded from
/// a .keanim file. It wraps a <see cref="SpriteSheet"/> (containing frame data
/// and timing) along with animation events.
/// </para>
/// <para>
/// When loaded with an atlas reference, the animation uses sprites from that
/// atlas. The atlas is loaded as a dependency and reference counted properly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load an animation
/// var walkHandle = assetManager.Load&lt;AnimationAsset&gt;("animations/player_walk.keanim");
/// var walk = walkHandle.Asset;
///
/// // Use with a SpriteAnimator component
/// world.Spawn()
///     .With(new SpriteAnimator { SpriteSheet = walk.SpriteSheet })
///     .Build();
/// </code>
/// </example>
public sealed class AnimationAsset : IDisposable
{
    private readonly SpriteAtlasAsset? atlasAsset;
    private bool disposed;

    /// <summary>
    /// Gets the name of this animation.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the sprite sheet containing animation frames.
    /// </summary>
    public SpriteSheet SpriteSheet { get; }

    /// <summary>
    /// Gets the animation events.
    /// </summary>
    public IReadOnlyList<AnimationEvent> Events { get; }

    /// <summary>
    /// Gets the total duration of the animation in seconds.
    /// </summary>
    public float Duration => SpriteSheet.TotalDuration;

    /// <summary>
    /// Gets the number of frames in the animation.
    /// </summary>
    public int FrameCount => SpriteSheet.Frames.Count;

    /// <summary>
    /// Gets the size of the animation in bytes (estimated).
    /// </summary>
    public long SizeBytes => 256 + (Events.Count * 32) + (atlasAsset?.SizeBytes ?? 0);

    /// <summary>
    /// Creates a new animation asset.
    /// </summary>
    /// <param name="name">The animation name.</param>
    /// <param name="spriteSheet">The sprite sheet with frame data.</param>
    /// <param name="events">The animation events.</param>
    /// <param name="atlasAsset">Optional atlas asset (for lifecycle management).</param>
    internal AnimationAsset(
        string name,
        SpriteSheet spriteSheet,
        IReadOnlyList<AnimationEvent> events,
        SpriteAtlasAsset? atlasAsset = null)
    {
        Name = name;
        SpriteSheet = spriteSheet;
        Events = events;
        this.atlasAsset = atlasAsset;
    }

    /// <summary>
    /// Releases the animation resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        atlasAsset?.Dispose();
    }
}
