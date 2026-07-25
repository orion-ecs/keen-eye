using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Owns every sound in the game: four synced music stems cross-faded by heat
/// tier and the Flashover Surge, a fall-speed-pitched wind loop, a
/// crush-proximity rumble, event one-shots (graze ting, tier-up swell, smash
/// crunch, brittle crackle, bumper boing, death impact), the Adrenaline Save's
/// muffled duck, and the 400 ms of true silence in the death beat.
/// </summary>
/// <remarks>
/// <para>
/// STEM MIXING — the four loops (<c>music-pad</c>, <c>music-pulse</c>,
/// <c>music-lead</c>, <c>music-lead-surge</c>) are exactly the same length and
/// tempo and are started on the Music channel in the same frame, so they stay
/// sample-locked forever. Intensity comes from mixing, not switching: the pad
/// always plays, the pulse fades in at Flame, the lead at Plasma — and a
/// Flashover Surge cross-fades the ordinary lead OUT and its surge variant IN,
/// so even the event swap is a mix move, never a restart. Ember Garden's
/// <see cref="StemMix.PadOnly"/> setting mutes everything but a ducked pad.
/// </para>
/// <para>
/// PITCH AS PARAMETER — with WAV-only audio, per-sound pitch is the expressive
/// axis: wind pitch maps to fall speed, the graze ting steps up per consecutive
/// graze, and the smash crunch scales with impact speed.
/// </para>
/// <para>
/// All fades use real delta time. Missing asset files, a missing audio context,
/// or an audio device the machine cannot provide (no OpenAL runtime, no output
/// device) disable the system gracefully: one console warning, then silence, and
/// the game keeps playing. This is the pattern to copy - audio is optional
/// hardware, so a game must never require it to start.
/// </para>
/// </remarks>
public sealed class NovaFallAudioSystem : SystemBase
{
    private IAudioContext? audio;

    private AudioClipHandle padClip;
    private AudioClipHandle pulseClip;
    private AudioClipHandle leadClip;
    private AudioClipHandle surgeLeadClip;
    private AudioClipHandle windClip;
    private AudioClipHandle rumbleClip;
    private AudioClipHandle tingClip;
    private AudioClipHandle swellClip;
    private AudioClipHandle crunchClip;
    private AudioClipHandle crackleClip;
    private AudioClipHandle boingClip;
    private AudioClipHandle impactClip;

    private SoundHandle padSound;
    private SoundHandle pulseSound;
    private SoundHandle leadSound;
    private SoundHandle surgeLeadSound;
    private SoundHandle windSound;
    private SoundHandle rumbleSound;

    private float padVolume;
    private float pulseVolume;
    private float leadVolume;
    private float surgeLeadVolume;

    private bool loadAttempted;
    private bool loaded;
    private bool unavailableReported;
    private bool loopsActive;
    private bool deathImpactPlayed;
    private bool silenced;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        if (!juice.PresentationAvailable
            || !World.TryGetExtension<IAudioContext>(out var context) || context is null)
        {
            return;
        }

        // The extension exists as soon as the audio plugin is installed, but the device is
        // opened later, when the window loads - and it can fail (no OpenAL runtime installed,
        // no output device). Playing into an uninitialized context throws, so check first and
        // fall silent instead.
        if (!context.IsInitialized)
        {
            ReportAudioUnavailable(context.InitializationError);
            return;
        }

        audio = context;
        if (!EnsureClipsLoaded())
        {
            return;
        }

        var phase = World.GetSingleton<GameState>().Phase;
        ref readonly var death = ref World.GetSingleton<DeathSequenceState>();

        if (phase == GamePhase.Dead)
        {
            UpdateDeathBeat(in death, juice.Enabled);
            return;
        }

        deathImpactPlayed = false;
        silenced = false;

        if (phase != GamePhase.Playing)
        {
            return;
        }

        if (!loopsActive)
        {
            StartLoops();
        }

        UpdateStemMix(deltaTime, juice.Enabled);
        UpdateWindAndRumble(juice.Enabled);
        PlayEventOneShots(juice.Enabled);
    }

    /// <summary>
    /// Warns once that the game is running without sound, naming the reason the backend gave.
    /// </summary>
    /// <param name="error">The backend failure, or null when audio simply never initialized.</param>
    private void ReportAudioUnavailable(AudioException? error)
    {
        if (unavailableReported)
        {
            return;
        }

        unavailableReported = true;
        Console.WriteLine(error is null
            ? "Audio unavailable - continuing without sound."
            : $"Audio unavailable: {error.Message} Continuing without sound.");
    }

    private bool EnsureClipsLoaded()
    {
        if (loadAttempted)
        {
            return loaded;
        }

        loadAttempted = true;
        try
        {
            padClip = LoadClip("music-pad.wav");
            pulseClip = LoadClip("music-pulse.wav");
            leadClip = LoadClip("music-lead.wav");
            surgeLeadClip = LoadClip("music-lead-surge.wav");
            windClip = LoadClip("wind-loop.wav");
            rumbleClip = LoadClip("crush-rumble.wav");
            tingClip = LoadClip("graze-ting.wav");
            swellClip = LoadClip("tier-up-swell.wav");
            crunchClip = LoadClip("smash-crunch.wav");
            crackleClip = LoadClip("brittle-crackle.wav");
            boingClip = LoadClip("bumper-boing.wav");
            impactClip = LoadClip("death-impact.wav");
            loaded = true;
        }
        catch (Exception ex) when (ex is IOException or AudioLoadException)
        {
            Console.WriteLine($"Audio disabled - could not load sound assets: {ex.Message}");
        }

        return loaded;
    }

    private AudioClipHandle LoadClip(string fileName)
        => audio!.LoadClip(Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", fileName));

    /// <summary>
    /// Starts every loop in the same frame so the stems stay sample-locked.
    /// Everything except the pad starts at zero volume and is mixed in later.
    /// </summary>
    private void StartLoops()
    {
        var music = PlaybackOptions.Default with { Loop = true, Channel = AudioChannel.Music };
        padSound = audio!.Play(padClip, music with { Volume = 0.75f });
        pulseSound = audio.Play(pulseClip, music with { Volume = 0f });
        leadSound = audio.Play(leadClip, music with { Volume = 0f });
        surgeLeadSound = audio.Play(surgeLeadClip, music with { Volume = 0f });

        var ambient = PlaybackOptions.Default with { Loop = true, Channel = AudioChannel.Ambient, Volume = 0f };
        windSound = audio.Play(windClip, ambient);
        rumbleSound = audio.Play(rumbleClip, ambient);

        padVolume = 0.75f;
        pulseVolume = 0f;
        leadVolume = 0f;
        surgeLeadVolume = 0f;
        loopsActive = true;
    }

    private void UpdateStemMix(float deltaTime, bool juiceEnabled)
    {
        var tier = World.GetSingleton<HeatState>().Tier;
        var mix = World.GetSingleton<RunConfig>().Settings.Music;
        var surging = World.GetSingleton<SurgeState>().Active;

        float padTarget;
        float pulseTarget;
        float leadTarget;
        float surgeLeadTarget;

        if (mix == StemMix.PadOnly)
        {
            // Ember Garden: a ducked pad and nothing else. The other stems keep
            // looping silently, so switching modes never breaks the sample lock.
            padTarget = Tuning.EmberPadVolume;
            pulseTarget = 0f;
            leadTarget = 0f;
            surgeLeadTarget = 0f;
        }
        else
        {
            // Pad always (swelling slightly with tier); pulse from Flame; lead
            // from Plasma; Nova pushes everything up. A Flashover Surge swaps
            // the lead for its surge variant — as a cross-fade, never a restart.
            padTarget = 0.75f + 0.10f * tier / 3f;
            pulseTarget = juiceEnabled && tier >= 1 ? (tier >= 3 ? 0.70f : 0.55f) : 0f;
            leadTarget = juiceEnabled && tier >= 2 && !surging ? (tier >= 3 ? 0.65f : 0.50f) : 0f;
            surgeLeadTarget = juiceEnabled && surging ? 0.70f : 0f;
        }

        padVolume = MoveTowards(padVolume, padTarget, Tuning.StemFadePerSecond * deltaTime);
        pulseVolume = MoveTowards(pulseVolume, pulseTarget, Tuning.StemFadePerSecond * deltaTime);
        leadVolume = MoveTowards(leadVolume, leadTarget, Tuning.StemFadePerSecond * deltaTime);
        surgeLeadVolume = MoveTowards(surgeLeadVolume, surgeLeadTarget, Tuning.StemFadePerSecond * deltaTime);

        // The Adrenaline Save muffles the music channel — the world holds its
        // breath — while the ambient danger cues (wind, rumble) stay honest.
        var duck = World.GetSingleton<AdrenalineState>().Active ? Tuning.AdrenalineMusicDuck : 1f;

        audio!.SetVolume(padSound, padVolume * duck);
        audio.SetVolume(pulseSound, pulseVolume * duck);
        audio.SetVolume(leadSound, leadVolume * duck);
        audio.SetVolume(surgeLeadSound, surgeLeadVolume * duck);
    }

    private void UpdateWindAndRumble(bool juiceEnabled)
    {
        if (!juiceEnabled)
        {
            audio!.SetVolume(windSound, 0f);
            audio.SetVolume(rumbleSound, 0f);
            return;
        }

        foreach (var entity in World.Query<Ball, Position2D, Velocity2D>())
        {
            ref readonly var position = ref World.Get<Position2D>(entity);
            ref readonly var velocity = ref World.Get<Velocity2D>(entity);

            // Wind pitch and volume follow fall speed.
            var speedFraction = Math.Clamp(velocity.Y / Tuning.MaxFallSpeed, 0f, 1f);
            audio!.SetPitch(windSound, float.Lerp(Tuning.WindPitchMin, Tuning.WindPitchMax, speedFraction));
            audio.SetVolume(windSound, 0.05f + 0.55f * speedFraction);

            // Rumble volume follows Furnace proximity.
            var ceilingDistance = position.Y - Tuning.CeilingY;
            var danger = 1f - Math.Clamp(ceilingDistance / Tuning.CrushProximityRange, 0f, 1f);
            audio.SetVolume(rumbleSound, 0.85f * danger);
            break;
        }
    }

    private void PlayEventOneShots(bool juiceEnabled)
    {
        if (!juiceEnabled)
        {
            return;
        }

        ref readonly var events = ref World.GetSingleton<FrameEvents>();

        if (events.Grazes > 0)
        {
            // The ting climbs with the graze chain — the audible combo ladder.
            var chain = Math.Min(World.GetSingleton<ComboState>().ConsecutiveGrazes, Tuning.GrazePitchCap);
            audio!.Play(tingClip, PlaybackOptions.Default with
            {
                Volume = 0.7f,
                Pitch = 1f + Tuning.GrazePitchStep * chain,
            });
        }

        if (events.TierChanged && events.TierTo > events.TierFrom)
        {
            audio!.Play(swellClip, PlaybackOptions.Default with { Volume = 0.8f });
        }

        if (events.Smashed)
        {
            // Crunch scales with the kinetic energy of the impact.
            var impact = Math.Clamp(events.SmashImpactSpeed / Tuning.MaxFallSpeed, 0f, 1f);
            audio!.Play(crunchClip, PlaybackOptions.Default with
            {
                Volume = 0.55f + 0.45f * impact,
                Pitch = 0.85f + 0.4f * impact,
            });
        }

        if (events.CrackStarted)
        {
            // The audible half of the Brittle telegraph: this starts a full
            // crumble delay (>= 0.6 s) before the floor lets go.
            audio!.Play(crackleClip, PlaybackOptions.Default with { Volume = 0.8f });
        }

        if (events.Crumbled)
        {
            // The crumble reuses the smash crunch, smaller and drier — same
            // vocabulary, lower stakes.
            audio!.Play(crunchClip, PlaybackOptions.Default with { Volume = 0.4f, Pitch = 1.2f });
        }

        if (events.Bumped)
        {
            var impact = Math.Clamp(events.BumpImpactSpeed / Tuning.MaxFallSpeed, 0f, 1f);
            audio!.Play(boingClip, PlaybackOptions.Default with
            {
                Volume = 0.6f + 0.3f * impact,
                Pitch = 0.9f + 0.25f * impact,
            });
        }
    }

    private void UpdateDeathBeat(in DeathSequenceState death, bool juiceEnabled)
    {
        if (!deathImpactPlayed && juiceEnabled)
        {
            audio!.Play(impactClip, PlaybackOptions.Default with { Volume = 0.9f });
            deathImpactPlayed = true;
        }

        // True silence: every loop and every ringing one-shot stops at once.
        if (death.AudioSilenced && !silenced)
        {
            audio!.StopAll();
            loopsActive = false;
            silenced = true;
        }
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        var delta = target - current;
        return Math.Abs(delta) <= maxDelta ? target : current + Math.Sign(delta) * maxDelta;
    }
}
