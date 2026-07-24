using KeenEyes.Spatial;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Shared world setup used by the windowed game, the headless
/// <c>--simulate</c> mode, and the determinism test project — one bootstrap,
/// three consumers, zero drift.
/// </summary>
public static class GameSetup
{
    /// <summary>
    /// Installs the plugins the SIMULATION itself depends on — shared by every
    /// consumer so gameplay (graze detection's quadtree queries) behaves
    /// identically with or without a window. Presentation plugins never
    /// belong here.
    /// </summary>
    /// <param name="world">The world to install into.</param>
    public static void InstallSimulationPlugins(World world)
    {
        world.InstallPlugin(new SpatialPlugin(new SpatialConfig
        {
            Strategy = SpatialStrategy.Quadtree,
        }));
    }

    /// <summary>
    /// Registers every game system with explicit phases and orders. Shared by
    /// the windowed game, the headless simulation, and the tests so all three
    /// run the same pipeline — juice systems are registered everywhere and
    /// no-op when their extension (or presentation entirely) is absent.
    /// </summary>
    /// <param name="world">The world to register into.</param>
    /// <param name="fontPath">Path to a TTF font for the HUD, or null.</param>
    public static void RegisterSystems(World world, string? fontPath)
    {
        // EarlyUpdate: react to LAST frame's events, clear them, then let the
        // Adrenaline Save re-assert its slow motion for the coming frame.
        world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
        world.AddSystem<HitstopSystem>(SystemPhase.EarlyUpdate, order: 10);
        world.AddSystem<FrameEventsClearSystem>(SystemPhase.EarlyUpdate, order: 50);
        world.AddSystem<AdrenalineSystem>(SystemPhase.EarlyUpdate, order: 55);

        // Update: simulation first (input → clock → motion → floors → collision
        // → surge → scoring → flow)...
        world.AddSystem<InputSteerSystem>(SystemPhase.Update, order: 0);
        world.AddSystem<JuiceToggleSystem>(SystemPhase.Update, order: 2);
        world.AddSystem<MusicClockSystem>(SystemPhase.Update, order: 5);
        world.AddSystem<BallMovementSystem>(SystemPhase.Update, order: 10);
        world.AddSystem<FloorScrollSystem>(SystemPhase.Update, order: 20);
        world.AddSystem<FloorPersonalitySystem>(SystemPhase.Update, order: 25);
        world.AddSystem<GrazeDetectionSystem>(SystemPhase.Update, order: 28);
        world.AddSystem<CollisionSystem>(SystemPhase.Update, order: 30);
        world.AddSystem<SurgeSystem>(SystemPhase.Update, order: 34);
        world.AddSystem<HeatSystem>(SystemPhase.Update, order: 40);
        world.AddSystem<ScoreSystem>(SystemPhase.Update, order: 50);
        world.AddSystem<CrushSystem>(SystemPhase.Update, order: 60);
        world.AddSystem<GameFlowSystem>(SystemPhase.Update, order: 70);
        world.AddSystem<ProfileSystem>(SystemPhase.Update, order: 72);

        // ...then the juice, consuming this frame's events. (The animation
        // plugin's TweenSystem runs at order 60, so tween values are fresh here.)
        world.AddSystem<PaletteSystem>(SystemPhase.Update, order: 76);
        world.AddSystem<SquashStretchSystem>(SystemPhase.Update, order: 77);
        world.AddSystem<TrailSystem>(SystemPhase.Update, order: 78);
        world.AddSystem<DeathSequenceSystem>(SystemPhase.Update, order: 79);
        world.AddSystem<VfxSystem>(SystemPhase.Update, order: 80);
        world.AddSystem<NovaFallAudioSystem>(SystemPhase.Update, order: 84);
        world.AddSystem(new HudSystem(fontPath), SystemPhase.Update, order: 88, runsBefore: [], runsAfter: []);

        world.AddSystem(new NovaFallRenderSystem(fontPath), SystemPhase.Render, order: 0, runsBefore: [], runsAfter: []);
    }

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
    /// <param name="mode">The game mode the world starts in.</param>
    public static void InitializeSingletons(
        IWorld world, ulong seed, bool pinSeed, bool presentation, GameMode mode = GameMode.Freefall)
    {
        world.SetSingleton(new RunConfig
        {
            Seed = seed,
            PinSeed = pinSeed,
            Mode = mode,
            Settings = ModeSettings.For(mode),
        });
        world.SetSingleton(new ScrollState { Speed = ModeSettings.For(mode).BaseScrollSpeed });
        world.SetSingleton(new HeatState());
        world.SetSingleton(new ScoreState());
        world.SetSingleton(new GameState { Phase = GamePhase.Ready });
        world.SetSingleton(new TimeScale { Value = 1f });
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new SmashState { LastSmashedFloorIndex = int.MinValue / 2 });
        world.SetSingleton(new ComboState());
        world.SetSingleton(new RunEventCounters());
        world.SetSingleton(new HitstopState());
        world.SetSingleton(new MusicClock());
        world.SetSingleton(new SurgeState { NextSurgeFloor = Tuning.SurgePeriodFloors });
        world.SetSingleton(new AdrenalineState { Available = true });
        world.SetSingleton(new MenuState { SelectedMode = mode });
        world.SetSingleton(new ProfileState { Profile = new PlayerProfile() });
        world.SetSingleton(new JuiceConfig { Enabled = true, PresentationAvailable = presentation });
        world.SetSingleton(new CameraState { Zoom = 1f });
        world.SetSingleton(new TrailState());
        world.SetSingleton(new DeathSequenceState());
        world.SetSingleton(Tuning.TierPalettes[0]);
    }

    /// <summary>
    /// Resets the world for a fresh run: despawns the previous ball and floors,
    /// resets per-run singletons (preserving the session best score and the
    /// profile), and spawns a new ball. Floors are populated lazily by
    /// <see cref="FloorScrollSystem"/> on the next update.
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

        world.SetSingleton(new ScrollState { Speed = runConfig.Settings.BaseScrollSpeed });
        world.SetSingleton(new HeatState());
        world.SetSingleton(new FrameEvents());
        world.SetSingleton(new SmashState { LastSmashedFloorIndex = int.MinValue / 2 });
        world.SetSingleton(new ComboState());
        world.SetSingleton(new RunEventCounters());
        world.SetSingleton(new HitstopState());
        world.SetSingleton(new MusicClock());
        world.SetSingleton(new SurgeState { NextSurgeFloor = Tuning.SurgePeriodFloors });
        world.SetSingleton(new AdrenalineState { Available = true });
        world.SetSingleton(new DeathSequenceState());

        ref var menu = ref world.GetSingleton<MenuState>();
        menu.LastRunTimedOut = false;

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
