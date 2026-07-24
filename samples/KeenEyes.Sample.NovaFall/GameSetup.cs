namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Shared world setup used by both the windowed game and the headless
/// <c>--simulate</c> mode.
/// </summary>
public static class GameSetup
{
    /// <summary>
    /// Installs all game singletons with their initial values. Call once per world,
    /// before the first <see cref="StartRun"/>.
    /// </summary>
    /// <param name="world">The world to initialize.</param>
    /// <param name="seed">The seed for the first run.</param>
    /// <param name="pinSeed">When true, every restart reuses <paramref name="seed"/>.</param>
    /// <param name="presentation">
    /// True in the windowed build; false in headless <c>--simulate</c> mode. Juice
    /// systems idle when this is false, so the simulation runs identically with or
    /// without a window.
    /// </param>
    public static void InitializeSingletons(IWorld world, ulong seed, bool pinSeed, bool presentation)
    {
        world.SetSingleton(new RunConfig { Seed = seed, PinSeed = pinSeed });
        world.SetSingleton(new ScrollState { Speed = Tuning.BaseScrollSpeed });
        world.SetSingleton(new HeatState());
        world.SetSingleton(new ScoreState());
        world.SetSingleton(new GameState { Phase = GamePhase.Ready });
        world.SetSingleton(new TimeScale { Value = 1f });
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new SmashState { LastSmashedFloorIndex = int.MinValue / 2 });
        world.SetSingleton(new ComboState());
        world.SetSingleton(new RunEventCounters());
        world.SetSingleton(new HitstopState());
        world.SetSingleton(new JuiceConfig { Enabled = true, PresentationAvailable = presentation });
        world.SetSingleton(new CameraState { Zoom = 1f });
        world.SetSingleton(new TrailState());
        world.SetSingleton(new DeathSequenceState());
        world.SetSingleton(Tuning.TierPalettes[0]);
    }

    /// <summary>
    /// Resets the world for a fresh run: despawns the previous ball and floors,
    /// resets per-run singletons (preserving the session best score), and spawns a
    /// new ball. Floors are populated lazily by <see cref="FloorScrollSystem"/> on
    /// the next update.
    /// </summary>
    /// <param name="world">The world to reset.</param>
    /// <param name="seed">The seed for the new run.</param>
    public static void StartRun(IWorld world, ulong seed)
    {
        // Collect first, despawn after: never structurally modify the world while
        // a query enumerator is live.
        var stale = new List<Entity>();
        foreach (var entity in world.Query<Ball>())
        {
            stale.Add(entity);
        }

        foreach (var entity in world.Query<Floor>())
        {
            stale.Add(entity);
        }

        foreach (var entity in stale)
        {
            world.Despawn(entity);
        }

        ref var runConfig = ref world.GetSingleton<RunConfig>();
        runConfig.Seed = seed;

        world.SetSingleton(new ScrollState { Speed = Tuning.BaseScrollSpeed });
        world.SetSingleton(new HeatState());
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new SmashState { LastSmashedFloorIndex = int.MinValue / 2 });
        world.SetSingleton(new ComboState());
        world.SetSingleton(new RunEventCounters());
        world.SetSingleton(new HitstopState());
        world.SetSingleton(new DeathSequenceState());

        ref var timeScale = ref world.GetSingleton<TimeScale>();
        timeScale.Value = 1f;

        ref var score = ref world.GetSingleton<ScoreState>();
        score.Score = 0;
        score.LastDepth = 0;

        // Presentation state resets too, so a restart starts visually clean.
        ref var trail = ref world.GetSingleton<TrailState>();
        trail.Head = 0;
        trail.Count = 0;

        ref var camera = ref world.GetSingleton<CameraState>();
        camera.Trauma = 0f;
        camera.KickY = 0f;
        camera.Zoom = 1f;

        world.Spawn("Ball")
            .With(new Ball { Radius = Tuning.BallRadius })
            .With(new Position2D { X = Tuning.BallSpawnX, Y = Tuning.BallSpawnY })
            .With(new Velocity2D())
            .With(new SteerInput())
            .Build();
    }
}
