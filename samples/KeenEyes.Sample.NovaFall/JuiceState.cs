using System.Numerics;

namespace KeenEyes.Sample.NovaFall;

// ============================================================================
// Presentation-side ("juice") state. Everything in this file is read by the
// render/audio/UI layers and written by juice systems; NONE of it feeds back
// into the simulation, which is what keeps the headless --simulate mode
// byte-identical whether or not any of it exists.
// ============================================================================

/// <summary>
/// The live world palette. Every draw call in the render system reads its
/// colors from here, so re-theming the whole game is a single singleton write.
/// On a heat tier change, <see cref="PaletteSystem"/> tweens each channel
/// toward the new tier's palette over ~0.6 seconds.
/// </summary>
public struct Palette
{
    /// <summary>Shaft background color.</summary>
    public Vector4 Background;

    /// <summary>Floor slab fill color.</summary>
    public Vector4 FloorFill;

    /// <summary>Floor slab outline color (the 2px readability contrast line).</summary>
    public Vector4 FloorOutline;

    /// <summary>Ball fill color.</summary>
    public Vector4 Ball;

    /// <summary>Comet trail, glow, and spark color.</summary>
    public Vector4 Trail;

    /// <summary>HUD accent color (heat bar, toasts).</summary>
    public Vector4 UiAccent;
}

/// <summary>
/// Identifies which <see cref="Palette"/> channel a palette tween entity drives.
/// </summary>
public enum PaletteChannelKind
{
    /// <summary>Drives <see cref="Palette.Background"/>.</summary>
    Background,

    /// <summary>Drives <see cref="Palette.FloorFill"/>.</summary>
    FloorFill,

    /// <summary>Drives <see cref="Palette.FloorOutline"/>.</summary>
    FloorOutline,

    /// <summary>Drives <see cref="Palette.Ball"/>.</summary>
    Ball,

    /// <summary>Drives <see cref="Palette.Trail"/>.</summary>
    Trail,

    /// <summary>Drives <see cref="Palette.UiAccent"/>.</summary>
    UiAccent,
}

/// <summary>
/// Marks an entity as the tween driver for one palette channel. The entity
/// also carries a <c>TweenVector4</c>; <see cref="PaletteSystem"/> retargets the
/// tween on tier changes and copies its current value into the palette singleton.
/// </summary>
[Component]
public partial struct PaletteChannel
{
    /// <summary>The palette channel this entity drives.</summary>
    public PaletteChannelKind Kind;
}

/// <summary>
/// The camera as an actor: shake trauma, smash kick, and speed/crush zoom.
/// Written by <see cref="CameraSystem"/>, composed into the orthographic
/// projection matrix by the render system.
/// </summary>
public struct CameraState
{
    /// <summary>Shake energy in [0, 1]; the offset scales with trauma squared.</summary>
    public float Trauma;

    /// <summary>Current shake offset in design units (magnitude kept under 6).</summary>
    public Vector2 ShakeOffset;

    /// <summary>Current zoom factor (1 = neutral; &lt;1 zoomed out, &gt;1 zoomed in).</summary>
    public float Zoom;

    /// <summary>Downward view kick from a Floor Smash, decaying to zero.</summary>
    public float KickY;

    /// <summary>Accumulated real time driving the shake noise.</summary>
    public float NoisePhase;
}

/// <summary>
/// Ring buffer of recent ball positions for the comet trail ribbon.
/// The array reference lives inside the singleton struct; the buffer itself is
/// allocated once by <see cref="TrailSystem"/>.
/// </summary>
public struct TrailState
{
    /// <summary>Ring buffer storage, newest at <see cref="Head"/>.</summary>
    public Vector2[]? Points;

    /// <summary>Index of the most recent sample.</summary>
    public int Head;

    /// <summary>Number of valid samples in the buffer.</summary>
    public int Count;
}

/// <summary>
/// State machine for the death beat: white flash, slow-motion shatter,
/// 400 ms of true silence, then the score card.
/// </summary>
public struct DeathSequenceState
{
    /// <summary>True while the death choreography is running or finished.</summary>
    public bool Active;

    /// <summary>Real seconds since death.</summary>
    public float Timer;

    /// <summary>Alpha of the full-screen white flash overlay.</summary>
    public float FlashAlpha;

    /// <summary>Set once the ember shatter burst has been requested.</summary>
    public bool EmberBurstSpawned;

    /// <summary>Set once all audio has been stopped for the silence beat.</summary>
    public bool AudioSilenced;

    /// <summary>True once the score card should be visible.</summary>
    public bool ScoreCardVisible;
}

/// <summary>
/// Juice configuration: the J-key toggle and whether presentation systems are
/// available at all (false in headless <c>--simulate</c> mode).
/// </summary>
public struct JuiceConfig
{
    /// <summary>When false, all juice systems idle: the A/B readability demo.</summary>
    public bool Enabled;

    /// <summary>
    /// True only in the windowed build. Juice systems early-out when false, so
    /// the headless simulation never pays for (or is perturbed by) presentation.
    /// </summary>
    public bool PresentationAvailable;
}

/// <summary>
/// Marks a short-lived burst-effect emitter entity spawned by
/// <see cref="VfxSystem"/>, with its remaining lifetime for cleanup.
/// </summary>
[Component]
public partial struct BurstEffect
{
    /// <summary>Real seconds until this burst entity is despawned.</summary>
    public float SecondsRemaining;
}

/// <summary>
/// Tag for burst emitters whose particles should be drawn as rotated rectangle
/// fragments (via thick line segments) instead of circles. Interpreting pool
/// data at draw time is the render system's privilege once it owns the pass.
/// </summary>
[TagComponent]
public partial struct RectFragments
{
}
