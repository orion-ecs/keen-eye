namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Advances the <see cref="MusicClock"/> singleton by SCALED delta time while
/// the run is live.
/// </summary>
/// <remarks>
/// The music clock is the shared timebase for every beat- or duration-synced
/// mechanic: Pulse floor phasing, the Flashover Surge window, and the Daily
/// Inferno time limit. It advances with the same scaled delta time the rest of
/// the simulation integrates with — never the wall clock — so hitstop pauses
/// the beat along with the world and a headless <c>--simulate</c> run replays
/// every beat-synced event bit-for-bit. (The audio stems merely PLAY OVER this
/// clock; the clock never reads the audio device.)
/// </remarks>
public sealed class MusicClockSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        ref var clock = ref World.GetSingleton<MusicClock>();
        clock.Seconds += deltaTime * World.GetSingleton<TimeScale>().Value;
    }
}
