// NOVAFALL — a modern reinterpretation of the TI-83 classic "Fall Down".
//
// A ball falls down an endless shaft of scrolling floors. Steer LEFT/RIGHT — the
// only inputs — to drop through the gaps. Floors scroll upward and carry anything
// resting on them toward the Furnace ceiling at the top; touch it and die.
//
// The NOVAFALL twist: consecutive clean gap-throughs stoke a Heat resource through
// four tiers — Ember → Flame → Plasma → Nova — which gate the score multiplier
// (x1/x2/x4/x8). Landing on a floor halves heat; resting bleeds it slowly; only
// death resets it. Score is meters fallen times the current multiplier, ticking
// continuously.
//
// PHASE B — the juice pass. Heat is now spectacle and power, not just a number:
//   - Comet trail ribbon + additive cone particles, length/density by tier
//   - Concentric additive glow under the ball, sine-pulsed by heat
//   - The world palette itself tweens through four tier moods
//   - FLOOR SMASH: at Plasma+, landings shatter the floor (fragments, hitstop,
//     camera kick) at the cost of one heat tier
//   - GRAZE SPARKS: thread within 12 units of a floor edge for score, heat,
//     and a rising-pitch ting
//   - A hand-rolled camera: trauma shake, smash kick, speed zoom-out, crush
//     zoom-in — composed into the projection matrix each frame
//   - Three synced music stems cross-faded by tier, wind pitched by fall speed
//   - UI-kit HUD: heat bar with tier notches, combo toasts, death score card
//
// Controls:
//   A / Left Arrow / Left Stick   - Steer left
//   D / Right Arrow / Left Stick  - Steer right
//   Any steer key                 - Start / restart
//   J                             - Toggle all juice (the A/B readability demo)
//   Escape                        - Exit
//
// Command line:
//   --seed <n>        Pin the run seed (same layout every run)
//   --simulate <n>    Headless determinism check: run n frames without a window
//                     and print the procedural layout, event counts, and final
//                     state
//
// NOTE: The windowed mode requires a display; --simulate does not, and installs
// no graphics, audio, particle, animation, or UI plugins at all — every juice
// system no-ops without its extension.

using System.Globalization;
using System.Numerics;
using KeenEyes;
using KeenEyes.Animation;
using KeenEyes.Audio.Silk;
using KeenEyes.Graphics.Silk;
using KeenEyes.Input.Abstractions;
using KeenEyes.Input.Silk;
using KeenEyes.Particles;
using KeenEyes.Particles.Systems;
using KeenEyes.Platform.Silk;
using KeenEyes.Runtime;
using KeenEyes.Sample.NovaFall;
using KeenEyes.Spatial;
using KeenEyes.UI;

// --- Parse arguments ---

var pinnedSeed = default(ulong?);
var simulateFrames = default(int?);

for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--seed" && ulong.TryParse(args[i + 1], CultureInfo.InvariantCulture, out var parsedSeed))
    {
        pinnedSeed = parsedSeed;
    }
    else if (args[i] == "--simulate" && int.TryParse(args[i + 1], CultureInfo.InvariantCulture, out var parsedFrames))
    {
        simulateFrames = parsedFrames;
    }
}

// --- Headless determinism mode ---

if (simulateFrames is int frames)
{
    RunHeadlessSimulation(frames, pinnedSeed ?? 0x5EEDF00DUL);
    return;
}

// --- Windowed game ---

Console.WriteLine("NOVAFALL");
Console.WriteLine("========");
Console.WriteLine();
Console.WriteLine("Steer into the gaps. Do not touch the Furnace.");
Console.WriteLine("Burn hot enough and the floors break before you do.");
Console.WriteLine();
Console.WriteLine("Controls:");
Console.WriteLine("  A / Left Arrow / Left Stick   - Steer left");
Console.WriteLine("  D / Right Arrow / Left Stick  - Steer right");
Console.WriteLine("  Any steer key                 - Start / restart");
Console.WriteLine("  J                             - Toggle juice on/off");
Console.WriteLine("  Escape                        - Exit");
Console.WriteLine();

var fontPath = FindSystemFont();
if (fontPath is null)
{
    Console.WriteLine("Warning: no system font found; the score will show in the window title.");
}

var windowConfig = new WindowConfig
{
    Title = "NOVAFALL",
    Width = 720,
    Height = 1080,
    VSync = true
};

var graphicsConfig = new SilkGraphicsConfig
{
    ClearColor = new Vector4(0.03f, 0.05f, 0.12f, 1f), // dark navy shaft
    EnableDepthTest = false,
    EnableCulling = false
};

var inputConfig = new SilkInputConfig
{
    EnableGamepads = true,
    MaxGamepads = 1,
    GamepadDeadzone = 0.15f,
    CaptureMouseOnClick = false
};

using var world = new World();

// Install plugins (order matters: window first, then graphics and input, then
// the gameplay/juice plugins that build on them).
world.InstallPlugin(new SilkWindowPlugin(windowConfig));
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkInputPlugin(inputConfig));
InstallSimulationPlugins(world);
world.InstallPlugin(new ParticlesPlugin());
world.InstallPlugin(new AnimationPlugin());
world.InstallPlugin(new SilkAudioPlugin());
world.InstallPlugin(new UIPlugin());

// NOVAFALL draws the particle pools itself, inside its own camera pass, so the
// readability contract (particles UNDER floors) and the camera shake apply to
// them. The stock render pass would draw the same pools a second time in plain
// window coordinates, so it is switched off. Spawning and simulation of the
// particles remain entirely the plugin's.
world.GetSystem<ParticleRenderSystem>()!.Enabled = false;

// A new seed per run unless one is pinned on the command line.
var seed = pinnedSeed ?? (ulong)Environment.TickCount64;

GameSetup.InitializeSingletons(world, seed, pinSeed: pinnedSeed is not null, presentation: true);
RegisterSystems(world, fontPath);
GameSetup.StartRun(world, seed);

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Run seed: {seed}"));
Console.WriteLine("Starting...");

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            var input = world.GetExtension<IInputContext>();
            input.Keyboard.OnKeyDown += eventArgs =>
            {
                if (eventArgs.Key == Key.Escape)
                {
                    Console.WriteLine("Escape pressed - exiting...");
                    System.Environment.Exit(0);
                }
            };

            // The UI layout system needs to know the pixel canvas size.
            if (world.TryGetExtension<KeenEyes.Graphics.Abstractions.IGraphicsContext>(out var graphics)
                && graphics is not null)
            {
                world.GetSystem<UILayoutSystem>()?.SetScreenSize(graphics.Width, graphics.Height);
            }

            Console.WriteLine("Ready. Press A/D to dive. Press J to compare with the juice off.");
        })
        .OnResize((width, height) =>
        {
            // Gameplay lives in a fixed design space and the render system maps
            // it through the camera matrix, so only the UI needs the new size.
            world.GetSystem<UILayoutSystem>()?.SetScreenSize(width, height);
            Console.WriteLine($"Window resized to {width}x{height}");
        })
        .Run();
}
catch (Exception ex)
{
    // Top-level demo entry point: initializing the windowing/graphics stack can
    // surface a wide range of platform exceptions (missing display, driver, or GL
    // context errors). A demo recovers by reporting the failure and exiting rather
    // than crashing, so a catch-all is appropriate here.
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine("Windowed mode requires a display. Try --simulate 600 for the headless check.");
}

Console.WriteLine("Sample complete!");

// Installs the plugins the SIMULATION itself depends on — shared by the windowed
// game and the headless mode so gameplay (graze detection's quadtree queries)
// behaves identically in both. Keep juice plugins out of here.
static void InstallSimulationPlugins(World world)
{
    world.InstallPlugin(new SpatialPlugin(new SpatialConfig
    {
        Strategy = SpatialStrategy.Quadtree,
    }));
}

// Registers every game system with explicit phases and orders. Shared by the
// windowed game and the headless simulation so both run the same pipeline —
// juice systems are registered everywhere and no-op when their extension (or
// presentation entirely) is absent.
static void RegisterSystems(World world, string? fontPath)
{
    // EarlyUpdate: react to LAST frame's events, then clear them.
    world.AddSystem<CameraSystem>(SystemPhase.EarlyUpdate, order: 0);
    world.AddSystem<HitstopSystem>(SystemPhase.EarlyUpdate, order: 10);
    world.AddSystem<FrameEventsClearSystem>(SystemPhase.EarlyUpdate, order: 50);

    // Update: simulation first (input → motion → collision → scoring → flow)...
    world.AddSystem<InputSteerSystem>(SystemPhase.Update, order: 0);
    world.AddSystem<JuiceToggleSystem>(SystemPhase.Update, order: 2);
    world.AddSystem<BallMovementSystem>(SystemPhase.Update, order: 10);
    world.AddSystem<FloorScrollSystem>(SystemPhase.Update, order: 20);
    world.AddSystem<GrazeDetectionSystem>(SystemPhase.Update, order: 28);
    world.AddSystem<CollisionSystem>(SystemPhase.Update, order: 30);
    world.AddSystem<HeatSystem>(SystemPhase.Update, order: 40);
    world.AddSystem<ScoreSystem>(SystemPhase.Update, order: 50);
    world.AddSystem<CrushSystem>(SystemPhase.Update, order: 60);
    world.AddSystem<GameFlowSystem>(SystemPhase.Update, order: 70);

    // ...then the juice, consuming this frame's events. (The animation plugin's
    // TweenSystem runs at order 60, so tween values are fresh here.)
    world.AddSystem<PaletteSystem>(SystemPhase.Update, order: 76);
    world.AddSystem<SquashStretchSystem>(SystemPhase.Update, order: 77);
    world.AddSystem<TrailSystem>(SystemPhase.Update, order: 78);
    world.AddSystem<DeathSequenceSystem>(SystemPhase.Update, order: 79);
    world.AddSystem<VfxSystem>(SystemPhase.Update, order: 80);
    world.AddSystem<NovaFallAudioSystem>(SystemPhase.Update, order: 84);
    world.AddSystem(new HudSystem(fontPath), SystemPhase.Update, order: 88, runsBefore: [], runsAfter: []);

    world.AddSystem(new NovaFallRenderSystem(fontPath), SystemPhase.Render, order: 0, runsBefore: [], runsAfter: []);
}

// Headless determinism harness: builds the world with no window, graphics,
// audio, particle, animation, or UI plugins (every juice system no-ops), steps
// a fixed number of frames at a fixed timestep, and prints the procedural
// layout, deterministic event counters, and final state. Running this twice
// with the same seed must produce byte-identical output — the CI-safe
// determinism hook for a later phase's replay tests.
static void RunHeadlessSimulation(int frames, ulong seed)
{
    using var world = new World();

    InstallSimulationPlugins(world);
    GameSetup.InitializeSingletons(world, seed, pinSeed: true, presentation: false);
    RegisterSystems(world, fontPath: null);
    GameSetup.StartRun(world, seed);

    // No input exists to press a start key, so the harness starts the run itself.
    world.GetSingleton<GameState>().Phase = GamePhase.Playing;

    const float fixedDeltaTime = 1f / 60f;
    for (var i = 0; i < frames; i++)
    {
        world.Update(fixedDeltaTime);
    }

    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture, $"NOVAFALL simulation: seed={seed} frames={frames}"));

    for (var i = 0; i < 10; i++)
    {
        var (gapCenterX, gapWidth) = FloorLayout.GapForFloor(seed, i);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"floor {i}: gapCenter={gapCenterX:F3} gapWidth={gapWidth:F3}"));
    }

    var counters = world.GetSingleton<RunEventCounters>();
    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"events: smashes={counters.Smashes} grazes={counters.Grazes}"));

    var scroll = world.GetSingleton<ScrollState>();
    var score = world.GetSingleton<ScoreState>();
    var heat = world.GetSingleton<HeatState>();
    var phase = world.GetSingleton<GameState>().Phase;

    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"final: depth={scroll.Depth:F3}m speed={scroll.Speed:F3} floorsSpawned={scroll.NextFloorIndex} " +
        $"score={score.Score:F3} heat={heat.Heat:F3} tier={heat.Tier} phase={phase}"));
}

// Finds a usable system font for the HUD, or null if none exists.
static string? FindSystemFont()
{
    string[] candidates =
    [
        @"C:\Windows\Fonts\segoeui.ttf",
        @"C:\Windows\Fonts\arial.ttf",
        @"C:\Windows\Fonts\calibri.ttf",
        @"C:\Windows\Fonts\verdana.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "/usr/share/fonts/TTF/DejaVuSans.ttf",
        "/System/Library/Fonts/Helvetica.ttc",
        "/Library/Fonts/Arial.ttf"
    ];

    foreach (var path in candidates)
    {
        if (File.Exists(path))
        {
            return path;
        }
    }

    return null;
}
