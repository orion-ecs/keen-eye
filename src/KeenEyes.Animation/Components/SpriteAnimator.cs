using KeenEyes.Animation.Data;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Component for frame-based sprite sheet animation.
/// </summary>
/// <remarks>
/// <para>
/// SpriteAnimator manages playback state for sprite sheet animations.
/// The sprite sheet data (frames, durations) is stored in <see cref="SpriteSheet"/>
/// assets and referenced by ID.
/// </para>
/// <para>
/// This component outputs a source rectangle that can be used with
/// <see cref="I2DRenderer.DrawTextureRegion"/> for rendering.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entity = world.Spawn()
///     .With(new Transform2D(position, 0, Vector2.One))
///     .With(new SpriteAnimator { SheetId = runSheetId, IsPlaying = true })
///     .Build();
/// </code>
/// </example>
[Component]
public partial struct SpriteAnimator
{
    /// <summary>
    /// The ID of the sprite sheet to use.
    /// </summary>
    public int SheetId;

    /// <summary>
    /// The current playback time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float Time;

    /// <summary>
    /// The current frame index.
    /// </summary>
    [BuilderIgnore]
    public int CurrentFrame;

    /// <summary>
    /// The playback speed multiplier.
    /// </summary>
    public float Speed;

    /// <summary>
    /// Whether the animation is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// The wrap mode override, or null to use the sheet's default.
    /// </summary>
    public WrapMode? WrapModeOverride;

    /// <summary>
    /// Whether the animation has completed (for non-looping animations).
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// The current source rectangle in normalized texture coordinates (output).
    /// </summary>
    [BuilderIgnore]
    public Rectangle SourceRect;

    /// <summary>
    /// Creates a default sprite animator.
    /// </summary>
    public static SpriteAnimator Default => new()
    {
        SheetId = -1,
        Time = 0f,
        CurrentFrame = 0,
        Speed = 1f,
        IsPlaying = false,
        WrapModeOverride = null,
        IsComplete = false,
        SourceRect = Rectangle.Empty
    };

    /// <summary>
    /// Creates a sprite animator for the specified sheet.
    /// </summary>
    /// <param name="sheetId">The sprite sheet ID.</param>
    /// <param name="autoPlay">Whether to start playing immediately.</param>
    /// <returns>A configured sprite animator.</returns>
    public static SpriteAnimator ForSheet(int sheetId, bool autoPlay = true) => Default with
    {
        SheetId = sheetId,
        IsPlaying = autoPlay
    };
}
