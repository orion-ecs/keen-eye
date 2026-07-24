using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles;
using KeenEyes.Particles.Components;
using KeenEyes.Particles.Data;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Owns every particle emitter in the game: the comet-trail cone on the ball,
/// Floor Smash fragment and spark-ring bursts, graze sparks, and the death
/// ember shatter. Emitters live in design space, so the render system's camera
/// matrix applies to particles exactly as it does to everything else.
/// </summary>
/// <remarks>
/// <para>
/// FRAGMENT BUDGET — the pooling/LOD lesson in miniature, and half of the
/// readability contract: burst emitters are tracked in spawn order and hard
/// capped at <see cref="Tuning.MaxLiveBursts"/>. When a smash lands while the
/// shaft is already busy, the OLDEST burst is despawned to make room, so
/// particle density near the floor rows stays bounded no matter how hot the
/// run gets. Each burst also carries a <see cref="BurstEffect"/> countdown and
/// is despawned when its particles have expired.
/// </para>
/// <para>
/// The system is a read-only observer of simulation state: it consumes
/// <see cref="FrameEvents"/> and spawns emitter entities, but no simulation
/// system knows those entities exist. Headless mode installs no particles
/// plugin, so the extension guard makes this a no-op there.
/// </para>
/// </remarks>
public sealed class VfxSystem : SystemBase
{
    private static readonly float[] trailRateByTier = [12f, 30f, 55f, 85f];

    private readonly List<Entity> liveBursts = [];

    private Entity trailEmitter;
    private bool trailEmitterSpawned;
    private int trailTierApplied = -1;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        if (!juice.PresentationAvailable
            || !World.TryGetExtension<ParticleManager>(out _))
        {
            return;
        }

        AgeAndCullBursts(deltaTime);

        if (!juice.Enabled)
        {
            // Juice off: silence the trail cone and drop pending bursts.
            if (trailEmitterSpawned)
            {
                World.Get<ParticleEmitter>(trailEmitter).IsPlaying = false;
            }

            return;
        }

        UpdateTrailEmitter();

        ref readonly var events = ref World.GetSingleton<FrameEvents>();

        if (events.Smashed)
        {
            SpawnSmashBursts(in events);
        }

        if (events.Grazes > 0)
        {
            SpawnGrazeSparks(in events);
        }

        if (events.CrackStarted)
        {
            SpawnCrackDust(in events);
        }

        if (events.Crumbled)
        {
            SpawnCrumbleBursts(in events);
        }

        if (events.Bumped)
        {
            SpawnBumpRing(in events);
        }

        ref var death = ref World.GetSingleton<DeathSequenceState>();
        if (death.Active && !death.EmberBurstSpawned)
        {
            SpawnDeathEmbers();
            death.EmberBurstSpawned = true;
        }
    }

    /// <summary>
    /// Keeps the cone emitter glued to the ball, pointing against its motion,
    /// with emission rate and colors scaled by heat tier.
    /// </summary>
    private void UpdateTrailEmitter()
    {
        var playing = World.GetSingleton<GameState>().Phase == GamePhase.Playing;
        var tier = World.GetSingleton<HeatState>().Tier;

        foreach (var ballEntity in World.Query<Ball, Position2D, Velocity2D>())
        {
            ref readonly var position = ref World.Get<Position2D>(ballEntity);
            ref readonly var velocity = ref World.Get<Velocity2D>(ballEntity);

            if (!trailEmitterSpawned)
            {
                trailEmitter = World.Spawn("TrailEmitter")
                    .With(new Transform2D(new Vector2(position.X, position.Y), 0f, Vector2.One))
                    .With(ParticleEmitter.Default with
                    {
                        EmissionRate = trailRateByTier[0],
                        Shape = EmissionShape.Cone(6f, 0.7f, -Vector2.UnitY),
                        Space = ParticleSpace.World,
                        LifetimeMin = 0.35f,
                        LifetimeMax = 0.65f,
                        StartSizeMin = 6f,
                        StartSizeMax = 12f,
                        StartSpeedMin = 30f,
                        StartSpeedMax = 80f,
                        BlendMode = BlendMode.Additive,
                        StartColor = Vector4.One,
                    })
                    .With(new ParticleEmitterModifiers
                    {
                        HasSizeOverLifetime = true,
                        SizeCurve = ParticleCurve.LinearFadeOut(),
                        HasColorOverLifetime = true,
                        ColorGradient = TrailGradient(0),
                    })
                    .Build();
                trailEmitterSpawned = true;
                trailTierApplied = 0;
            }

            ref var transform = ref World.Get<Transform2D>(trailEmitter);
            transform.Position = new Vector2(position.X, position.Y);

            ref var emitter = ref World.Get<ParticleEmitter>(trailEmitter);
            emitter.IsPlaying = playing;
            emitter.EmissionRate = trailRateByTier[Math.Clamp(tier, 0, trailRateByTier.Length - 1)];

            // Cone points against the motion so particles stream out behind.
            var speed = velocity.X * velocity.X + velocity.Y * velocity.Y;
            var direction = speed > 1f
                ? Vector2.Normalize(new Vector2(-velocity.X, -velocity.Y))
                : -Vector2.UnitY;
            emitter.Shape = EmissionShape.Cone(6f, 0.7f, direction);

            if (tier != trailTierApplied)
            {
                World.Get<ParticleEmitterModifiers>(trailEmitter).ColorGradient = TrailGradient(tier);
                trailTierApplied = tier;
            }

            break;
        }
    }

    /// <summary>
    /// White-hot core fading through the tier color into transparent smoke —
    /// the color-over-life story of a burning thing.
    /// </summary>
    private static ParticleGradient TrailGradient(int tier)
    {
        var tierColor = Tuning.TierPalettes[Math.Clamp(tier, 0, Tuning.TierPalettes.Length - 1)].Trail;
        return ParticleGradient.FromPoints(
        [
            (0.00f, new Vector4(1f, 1f, 1f, 0.9f)),
            (0.35f, tierColor with { W = 0.8f }),
            (0.75f, new Vector4(0.35f, 0.32f, 0.38f, 0.35f)),
            (1.00f, new Vector4(0.25f, 0.22f, 0.28f, 0f)),
        ]);
    }

    /// <summary>
    /// The smashed floor despawns into rotated rect fragments (one burst per
    /// slab, sized by slab share) plus a radial spark ring at the impact point.
    /// </summary>
    private void SpawnSmashBursts(in FrameEvents events)
    {
        var palette = World.GetSingleton<Palette>();
        var gapLeft = events.SmashGapCenterX - events.SmashGapWidth / 2f;
        var gapRight = events.SmashGapCenterX + events.SmashGapWidth / 2f;
        var slabY = events.SmashY + Tuning.FloorThickness / 2f;

        // Fragment counts proportional to slab width, totalling ~SmashFragmentCount.
        var leftWidth = Math.Max(gapLeft, 0f);
        var rightWidth = Math.Max(Tuning.ShaftWidth - gapRight, 0f);
        var totalWidth = Math.Max(leftWidth + rightWidth, 1f);

        SpawnFragmentSlab(leftWidth / 2f, slabY, leftWidth,
            (int)(Tuning.SmashFragmentCount * (leftWidth / totalWidth)), palette.FloorFill);
        SpawnFragmentSlab(gapRight + rightWidth / 2f, slabY, rightWidth,
            (int)(Tuning.SmashFragmentCount * (rightWidth / totalWidth)), palette.FloorFill);

        // Radial spark ring at the point of impact.
        SpawnBurst(events.SmashX, events.SmashY, ParticleEmitter.Burst(Tuning.SmashSparkCount, 0.45f) with
        {
            Shape = EmissionShape.Circle(18f),
            StartSpeedMin = 260f,
            StartSpeedMax = 430f,
            StartSizeMin = 3f,
            StartSizeMax = 6f,
            BlendMode = BlendMode.Additive,
            StartColor = palette.Trail,
        }, new ParticleEmitterModifiers
        {
            HasColorOverLifetime = true,
            ColorGradient = ParticleGradient.FadeOut(palette.Trail),
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut(),
        }, rectFragments: false, ttl: 0.6f);
    }

    private void SpawnFragmentSlab(float centerX, float centerY, float width, int count, Vector4 floorColor)
    {
        if (width < 1f || count <= 0)
        {
            return;
        }

        // Fragments are debris, not light: alpha blend, gravity, spin, shrink.
        SpawnBurst(centerX, centerY, ParticleEmitter.Burst(count, 0.9f) with
        {
            Shape = EmissionShape.Box(width, Tuning.FloorThickness),
            StartSpeedMin = 120f,
            StartSpeedMax = 380f,
            StartSizeMin = 8f,
            StartSizeMax = 18f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            BlendMode = BlendMode.Alpha,
            StartColor = (floorColor * 1.3f) with { W = 1f },
        }, new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityY = 1300f,
            Drag = 0.1f,
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut(),
            HasRotationOverLifetime = true,
            RotationSpeed = 4f,
            RotationCurve = ParticleCurve.Constant(1f),
        }, rectFragments: true, ttl: 1.1f);
    }

    private void SpawnGrazeSparks(in FrameEvents events)
    {
        var palette = World.GetSingleton<Palette>();

        // MagicSparkles-style near-miss glitter: small, fast, additive.
        SpawnBurst(events.GrazeX, events.GrazeY, ParticleEmitter.Burst(Tuning.GrazeSparkCount, 0.55f) with
        {
            Shape = EmissionShape.Sphere(6f),
            StartSpeedMin = 40f,
            StartSpeedMax = 150f,
            StartSizeMin = 2f,
            StartSizeMax = 5f,
            StartRotationMin = 0f,
            StartRotationMax = MathF.PI * 2f,
            BlendMode = BlendMode.Additive,
            StartColor = palette.UiAccent,
        }, new ParticleEmitterModifiers
        {
            HasColorOverLifetime = true,
            ColorGradient = ParticleGradient.FromPoints(
            [
                (0.0f, new Vector4(1f, 1f, 1f, 1f)),
                (0.4f, palette.UiAccent),
                (1.0f, palette.UiAccent with { W = 0f }),
            ]),
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut(),
        }, rectFragments: false, ttl: 0.7f);
    }

    /// <summary>
    /// A pinch of dust puffing off a Brittle floor the instant it starts
    /// cracking — the visible half of the telegraph (the crackle SFX is the
    /// audible half).
    /// </summary>
    private void SpawnCrackDust(in FrameEvents events)
    {
        var palette = World.GetSingleton<Palette>();

        SpawnBurst(events.CrackX, events.CrackY, ParticleEmitter.Burst(10, 0.6f) with
        {
            Shape = EmissionShape.Box(60f, 4f),
            StartSpeedMin = 15f,
            StartSpeedMax = 60f,
            StartSizeMin = 2f,
            StartSizeMax = 4f,
            BlendMode = BlendMode.Alpha,
            StartColor = (palette.FloorOutline * 0.8f) with { W = 0.7f },
        }, new ParticleEmitterModifiers
        {
            HasGravity = true,
            GravityY = 350f,
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut(),
        }, rectFragments: false, ttl: 0.7f);
    }

    /// <summary>
    /// A crumbling Brittle floor breaks into the same rotated-rect debris a
    /// Floor Smash produces — one fragment vocabulary for "a floor died",
    /// however it died.
    /// </summary>
    private void SpawnCrumbleBursts(in FrameEvents events)
    {
        var palette = World.GetSingleton<Palette>();
        var gapLeft = events.CrumbleGapCenterX - events.CrumbleGapWidth / 2f;
        var gapRight = events.CrumbleGapCenterX + events.CrumbleGapWidth / 2f;
        var slabY = events.CrumbleY + Tuning.FloorThickness / 2f;

        var leftWidth = Math.Max(gapLeft, 0f);
        var rightWidth = Math.Max(Tuning.ShaftWidth - gapRight, 0f);
        var totalWidth = Math.Max(leftWidth + rightWidth, 1f);

        SpawnFragmentSlab(leftWidth / 2f, slabY, leftWidth,
            (int)(Tuning.SmashFragmentCount * (leftWidth / totalWidth)), palette.FloorFill);
        SpawnFragmentSlab(gapRight + rightWidth / 2f, slabY, rightWidth,
            (int)(Tuning.SmashFragmentCount * (rightWidth / totalWidth)), palette.FloorFill);
    }

    /// <summary>
    /// A small upward spark fan where a Bumper flung the ball — the launch is
    /// loud enough on its own; this just marks the contact point.
    /// </summary>
    private void SpawnBumpRing(in FrameEvents events)
    {
        var palette = World.GetSingleton<Palette>();

        SpawnBurst(events.BumpX, events.BumpY, ParticleEmitter.Burst(12, 0.4f) with
        {
            Shape = EmissionShape.Cone(10f, 0.9f, -Vector2.UnitY),
            StartSpeedMin = 120f,
            StartSpeedMax = 260f,
            StartSizeMin = 2f,
            StartSizeMax = 5f,
            BlendMode = BlendMode.Additive,
            StartColor = palette.UiAccent,
        }, new ParticleEmitterModifiers
        {
            HasColorOverLifetime = true,
            ColorGradient = ParticleGradient.FadeOut(palette.UiAccent),
            HasSizeOverLifetime = true,
            SizeCurve = ParticleCurve.LinearFadeOut(),
        }, rectFragments: false, ttl: 0.5f);
    }

    private void SpawnDeathEmbers()
    {
        var palette = World.GetSingleton<Palette>();

        foreach (var ballEntity in World.Query<Ball, Position2D>())
        {
            ref readonly var position = ref World.Get<Position2D>(ballEntity);

            // The ball shatters into slow embers that drift upward in slow-mo.
            SpawnBurst(position.X, position.Y, ParticleEmitter.Burst(Tuning.DeathEmberCount, 1.6f) with
            {
                Shape = EmissionShape.Sphere(14f),
                StartSpeedMin = 60f,
                StartSpeedMax = 220f,
                StartSizeMin = 3f,
                StartSizeMax = 8f,
                BlendMode = BlendMode.Additive,
                StartColor = palette.Ball,
            }, new ParticleEmitterModifiers
            {
                HasGravity = true,
                GravityY = -60f,
                Drag = 0.5f,
                HasColorOverLifetime = true,
                ColorGradient = ParticleGradient.FromPoints(
                [
                    (0.0f, new Vector4(1f, 1f, 1f, 1f)),
                    (0.3f, palette.Ball),
                    (1.0f, palette.Trail with { W = 0f }),
                ]),
                HasSizeOverLifetime = true,
                SizeCurve = ParticleCurve.LinearFadeOut(),
            }, rectFragments: false, ttl: 1.8f);
            break;
        }
    }

    private void SpawnBurst(
        float x, float y,
        ParticleEmitter emitter,
        ParticleEmitterModifiers modifiers,
        bool rectFragments,
        float ttl)
    {
        // Fragment budget: hard cap, oldest-first eviction.
        while (liveBursts.Count >= Tuning.MaxLiveBursts)
        {
            var oldest = liveBursts[0];
            liveBursts.RemoveAt(0);
            if (World.IsAlive(oldest))
            {
                World.Despawn(oldest);
            }
        }

        var builder = World.Spawn()
            .With(new Transform2D(new Vector2(x, y), 0f, Vector2.One))
            .With(emitter)
            .With(modifiers)
            .With(new BurstEffect { SecondsRemaining = ttl });

        if (rectFragments)
        {
            builder = builder.WithTag<RectFragments>();
        }

        liveBursts.Add(builder.Build());
    }

    private void AgeAndCullBursts(float deltaTime)
    {
        for (var i = liveBursts.Count - 1; i >= 0; i--)
        {
            var entity = liveBursts[i];
            if (!World.IsAlive(entity))
            {
                liveBursts.RemoveAt(i);
                continue;
            }

            ref var burst = ref World.Get<BurstEffect>(entity);
            burst.SecondsRemaining -= deltaTime;
            if (burst.SecondsRemaining <= 0f)
            {
                World.Despawn(entity);
                liveBursts.RemoveAt(i);
            }
        }
    }
}
