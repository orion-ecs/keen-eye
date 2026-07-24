using System.Numerics;
using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Palette as state: keeps the <see cref="Palette"/> singleton tracking the
/// current heat tier, cross-tweening every color channel over
/// <see cref="Tuning.PaletteTweenSeconds"/> whenever the tier changes.
/// </summary>
/// <remarks>
/// <para>
/// TEACHING NOTE — the tween shape. <c>KeenEyes.Animation</c>'s
/// <c>TweenSystem</c> advances <c>TweenVector4</c> components and computes
/// <c>CurrentValue</c>, but deliberately does not know what the value is FOR.
/// The pattern is: one entity per animated value (here, one per palette
/// channel, tagged with <see cref="PaletteChannel"/>), plus a small applier
/// system — this one — that retargets tweens on game events and copies
/// <c>CurrentValue</c> into whatever the value drives. Every rendering read
/// goes through the palette singleton, so the whole world changes mood from a
/// single tween source of truth.
/// </para>
/// <para>
/// Retargeting starts each tween from the palette's CURRENT color, not the old
/// tier's target, so a mid-tween tier change (Flame → Plasma → Nova in quick
/// succession) never pops.
/// </para>
/// <para>
/// Without the animation plugin (headless mode) the system snaps the palette
/// directly — same end state, no motion, nothing else to no-op.
/// </para>
/// <para>
/// PHASE C OVERRIDE CHANNELS — after the tier tween resolves, three modifiers
/// stack on top, in order: the Flashover Surge blends the whole palette toward
/// white-hot, an active Adrenaline Save desaturates it, and the selected
/// cosmetic style recolors the trail and ball. All three are presentation-only
/// reads of simulation state; none of them feeds anything back.
/// </para>
/// </remarks>
public sealed class PaletteSystem : SystemBase
{
    private static readonly PaletteChannelKind[] channelKinds = Enum.GetValues<PaletteChannelKind>();

    private bool channelsSpawned;
    private int lastTier;
    private float surgeBlend;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        ref var palette = ref World.GetSingleton<Palette>();
        var tier = World.GetSingleton<HeatState>().Tier;
        ref readonly var target = ref Tuning.TierPalettes[Math.Clamp(tier, 0, Tuning.TierPalettes.Length - 1)];

        var tweensAvailable = juice.PresentationAvailable
            && World.TryGetExtension<AnimationManager>(out _);

        if (!tweensAvailable)
        {
            // No tween machinery: snap to the tier target. Headless never
            // renders, so this only exists to keep the state honest.
            palette = target;
            ApplyOverrides(ref palette, deltaTime);
            return;
        }

        if (!channelsSpawned)
        {
            SpawnChannelEntities(in palette);
            channelsSpawned = true;
            lastTier = tier;
        }

        // Retarget on any tier difference rather than only on the TierChanged
        // event: death resets the tier without publishing one, and this also
        // self-heals after a restart.
        var retarget = tier != lastTier;
        lastTier = tier;

        foreach (var entity in World.Query<PaletteChannel, TweenVector4>())
        {
            var kind = World.Get<PaletteChannel>(entity).Kind;
            ref var tween = ref World.Get<TweenVector4>(entity);

            if (retarget)
            {
                // Start from wherever the channel is RIGHT NOW so back-to-back
                // tier changes glide instead of popping.
                tween.StartValue = tween.CurrentValue;
                tween.EndValue = GetChannel(in target, kind);
                tween.Duration = Tuning.PaletteTweenSeconds;
                tween.ElapsedTime = 0f;
                tween.IsPlaying = true;
                tween.IsComplete = false;
            }

            SetChannel(ref palette, kind, tween.CurrentValue);
        }

        ApplyOverrides(ref palette, deltaTime);
    }

    /// <summary>
    /// Stacks the Phase C override channels onto the tier palette: surge
    /// white-hot blend, adrenaline desaturation, then cosmetic recolors.
    /// </summary>
    private void ApplyOverrides(ref Palette palette, float deltaTime)
    {
        // Surge: blend toward white-hot, easing in and out on real time so the
        // window's start and end read as a wave, not a light switch.
        var surgeTarget = World.GetSingleton<SurgeState>().Active ? 1f : 0f;
        var delta = Math.Clamp(surgeTarget - surgeBlend, -1f, 1f);
        var maxStep = Tuning.SurgeBlendPerSecond * deltaTime;
        surgeBlend += Math.Clamp(delta, -maxStep, maxStep);

        if (surgeBlend > 0.001f)
        {
            var t = surgeBlend * Tuning.SurgePaletteStrength;
            palette.Background = Vector4.Lerp(palette.Background, Tuning.SurgePalette.Background, t);
            palette.FloorFill = Vector4.Lerp(palette.FloorFill, Tuning.SurgePalette.FloorFill, t);
            palette.FloorOutline = Vector4.Lerp(palette.FloorOutline, Tuning.SurgePalette.FloorOutline, t);
            palette.Ball = Vector4.Lerp(palette.Ball, Tuning.SurgePalette.Ball, t);
            palette.Trail = Vector4.Lerp(palette.Trail, Tuning.SurgePalette.Trail, t);
            palette.UiAccent = Vector4.Lerp(palette.UiAccent, Tuning.SurgePalette.UiAccent, t);
        }

        // Adrenaline: the world drains toward gray while the save window is open.
        if (World.GetSingleton<AdrenalineState>().Active)
        {
            palette.Background = Desaturate(palette.Background);
            palette.FloorFill = Desaturate(palette.FloorFill);
            palette.FloorOutline = Desaturate(palette.FloorOutline);
            palette.Ball = Desaturate(palette.Ball);
            palette.Trail = Desaturate(palette.Trail);
            palette.UiAccent = Desaturate(palette.UiAccent);
        }

        // Cosmetics: the selected style recolors the trail and ball only —
        // floors and background stay tier-driven for readability.
        if (World.GetSingleton<ProfileState>().Profile is { } profile)
        {
            var style = CosmeticStyles.All[Math.Clamp(profile.SelectedStyle, 0, CosmeticStyles.All.Length - 1)];
            if (style.TrailOverride is { } trailColor)
            {
                palette.Trail = trailColor;
            }

            if (style.BallOverride is { } ballColor)
            {
                palette.Ball = ballColor;
            }
        }
    }

    private static Vector4 Desaturate(Vector4 color)
    {
        var luminance = 0.299f * color.X + 0.587f * color.Y + 0.114f * color.Z;
        var gray = new Vector4(luminance, luminance, luminance, color.W);
        return Vector4.Lerp(color, gray, Tuning.AdrenalineDesaturation);
    }

    private void SpawnChannelEntities(in Palette current)
    {
        foreach (var kind in channelKinds)
        {
            var start = GetChannel(in current, kind);
            World.Spawn($"Palette.{kind}")
                .With(new PaletteChannel { Kind = kind })
                .With(TweenVector4.Create(start, start, duration: 0.01f, EaseType.CubicOut))
                .Build();
        }
    }

    private static System.Numerics.Vector4 GetChannel(in Palette palette, PaletteChannelKind kind) => kind switch
    {
        PaletteChannelKind.Background => palette.Background,
        PaletteChannelKind.FloorFill => palette.FloorFill,
        PaletteChannelKind.FloorOutline => palette.FloorOutline,
        PaletteChannelKind.Ball => palette.Ball,
        PaletteChannelKind.Trail => palette.Trail,
        PaletteChannelKind.UiAccent => palette.UiAccent,
        _ => palette.Background,
    };

    private static void SetChannel(ref Palette palette, PaletteChannelKind kind, System.Numerics.Vector4 value)
    {
        switch (kind)
        {
            case PaletteChannelKind.Background:
                palette.Background = value;
                break;
            case PaletteChannelKind.FloorFill:
                palette.FloorFill = value;
                break;
            case PaletteChannelKind.FloorOutline:
                palette.FloorOutline = value;
                break;
            case PaletteChannelKind.Ball:
                palette.Ball = value;
                break;
            case PaletteChannelKind.Trail:
                palette.Trail = value;
                break;
            case PaletteChannelKind.UiAccent:
                palette.UiAccent = value;
                break;
            default:
                break;
        }
    }
}
