using System.Text.Json.Serialization;

namespace KeenEyes.Assets;

/// <summary>
/// Root model for .keanim animation file format.
/// </summary>
internal sealed class AnimationFileJson
{
    /// <summary>
    /// The name of the animation.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Path to the sprite atlas (relative to the animation file).
    /// </summary>
    [JsonPropertyName("atlas")]
    public string? Atlas { get; set; }

    /// <summary>
    /// Default frame rate (frames per second) when individual frame durations are not specified.
    /// </summary>
    [JsonPropertyName("frameRate")]
    public float FrameRate { get; set; } = 12f;

    /// <summary>
    /// Wrap mode for the animation.
    /// </summary>
    [JsonPropertyName("wrapMode")]
    public string? WrapMode { get; set; }

    /// <summary>
    /// The animation frames.
    /// </summary>
    [JsonPropertyName("frames")]
    public List<AnimationFrameJson>? Frames { get; set; }

    /// <summary>
    /// Events that fire during the animation.
    /// </summary>
    [JsonPropertyName("events")]
    public List<AnimationEventJson>? Events { get; set; }
}

/// <summary>
/// A frame in the animation.
/// </summary>
internal sealed class AnimationFrameJson
{
    /// <summary>
    /// The name of the sprite in the atlas.
    /// </summary>
    [JsonPropertyName("sprite")]
    public string? Sprite { get; set; }

    /// <summary>
    /// Duration of this frame in seconds (optional, uses 1/frameRate if not specified).
    /// </summary>
    [JsonPropertyName("duration")]
    public float? Duration { get; set; }

    /// <summary>
    /// Alternative: sprite index in the atlas (for indexed sprites).
    /// </summary>
    [JsonPropertyName("index")]
    public int? Index { get; set; }
}

/// <summary>
/// An event in the animation.
/// </summary>
internal sealed class AnimationEventJson
{
    /// <summary>
    /// Time when the event fires (in seconds).
    /// </summary>
    [JsonPropertyName("time")]
    public float Time { get; set; }

    /// <summary>
    /// The name of the event.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional data/parameter for the event.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
