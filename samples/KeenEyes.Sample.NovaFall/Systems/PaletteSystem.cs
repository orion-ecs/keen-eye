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
/// </remarks>
public sealed class PaletteSystem : SystemBase
{
    private static readonly PaletteChannelKind[] channelKinds = Enum.GetValues<PaletteChannelKind>();

    private bool channelsSpawned;
    private int lastTier;

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
