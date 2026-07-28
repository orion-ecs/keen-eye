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
// PHASE C — modes and meta. The shaft gets personalities and the game gets a life
// outside a single run:
//   - FLOOR PERSONALITIES: Brittle (cracks on landing, crumbles 0.65s later —
//     every hazard telegraphs >= 0.6s before it can kill), Bumper (elastic launch
//     back up toward the Furnace), Pulse (gap breathes open/closed on the music
//     beat, close telegraphed by shrinking edges)
//   - FLASHOVER SURGE: every 40 cleared floors, 10 seconds where the scroll
//     spikes, EVERY floor smashes at any tier, the shaft burns white-hot, and
//     the lead stem swaps to its surge variant; 5+ smashes = +1000 Surge Sweep
//   - ADRENALINE SAVE: once per run, the frame the Furnace would kill, time
//     snaps to 20% for 1.5 real seconds — muffled audio, desaturated world,
//     one last steer
//   - MODES AS CONFIGURATION: FREEFALL / DAILY INFERNO (3-minute time attack on
//     a date-hashed seed, 3 attempts, local medals) / EMBER GARDEN (no crusher,
//     no death, pad-only mix) — the same systems, different RunConfig knobs
//   - PERSISTENCE: per-mode bests, daily medals and attempts, and cosmetic
//     styles, saved through KeenEyes.Persistence to the platform app-data dir
//   - TESTBRIDGE: the running game is MCP-inspectable on the named pipe
//     "KeenEyes.NovaFall.TestBridge", exactly like the editor
//
// Controls:
//   A / Left Arrow / Left Stick   - Steer left (in menus: cycle the active row)
//   D / Right Arrow / Left Stick  - Steer right (in menus: cycle the active row)
//   Tab                           - Ready screen: switch row (mode <-> style)
//   Space / Enter                 - Ready screen: dive; death screen: back to menu
//   J                             - Toggle all juice (the A/B readability demo)
//   Escape                        - Exit
//
// Command line:
//   --seed <n>        Pin the run seed (same layout every run)
//   --mode <name>     Start in a mode: freefall (default), daily, ember
//   --simulate <n>    Headless determinism check: run n frames without a window
//                     and print the procedural layout, event counts, and final
//                     state
//
// NOTE: The windowed mode requires a display; --simulate does not, and installs
// no graphics, audio, particle, animation, or UI plugins at all — every juice
// system no-ops without its extension, and it never reads or writes save files.

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
using KeenEyes.TestBridge;
using KeenEyes.TestBridge.Ipc;
using KeenEyes.UI;

// --- Parse arguments ---

var pinnedSeed = default(ulong?);
var simulateFrames = default(int?);
var startMode = GameMode.Freefall;

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
    else if (args[i] == "--mode")
    {
        startMode = args[i + 1].ToLowerInvariant() switch
        {
            "daily" => GameMode.DailyInferno,
            "ember" => GameMode.EmberGarden,
            _ => GameMode.Freefall,
        };
    }
}

// --- Headless determinism mode ---

if (simulateFrames is int frames)
{
    RunHeadlessSimulation(frames, pinnedSeed ?? 0x5EEDF00DUL, startMode);
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
Console.WriteLine("  A / Left Arrow / Left Stick   - Steer left / cycle menu row");
Console.WriteLine("  D / Right Arrow / Left Stick  - Steer right / cycle menu row");
Console.WriteLine("  Tab                           - Ready screen: switch row (mode <-> style)");
Console.WriteLine("  Space / Enter                 - Dive / back to menu");
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
//
// The window, graphics and input plugins are the hard requirements, and they are
// the ones that touch the platform: SilkWindowPlugin.Install creates the window
// (and therefore initializes GLFW) right here, so on a machine with no display
// this is where startup fails - before the game loop is ever reached. Guarding
// only the loop would let that surface as an unhandled crash, so the same
// diagnostic covers the installation.
try
{
    world.InstallPlugin(new SilkWindowPlugin(windowConfig));
    world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
    world.InstallPlugin(new SilkInputPlugin(inputConfig));
}
catch (Exception ex)
{
    ReportStartupFailure(ex);
    return;
}

GameSetup.InstallSimulationPlugins(world);
world.InstallPlugin(new ParticlesPlugin());
world.InstallPlugin(new AnimationPlugin());

// Audio is optional hardware. Installing the plugin only registers systems and the
// context - the OpenAL device is opened when the window loads, so a machine with no
// audio runtime or no output device cannot fail here, and does not fail the window
// loop either: the context reports IsInitialized == false and NovaFallAudioSystem
// prints one warning and plays the game silently.
world.InstallPlugin(new SilkAudioPlugin());
world.InstallPlugin(new UIPlugin());

// NOVAFALL draws the particle pools itself, inside its own camera pass, so the
// readability contract (particles UNDER floors) and the camera shake apply to
// them. The stock render pass would draw the same pools a second time in plain
// window coordinates, so it is switched off. Spawning and simulation of the
// particles remain entirely the plugin's.
world.GetSystem<ParticleRenderSystem>()!.Enabled = false;

// The persistent profile lives in the platform app-data directory; loading is
// corruption-tolerant (a bad file means fresh state, never a crash).
var saveDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "KeenEyes.NovaFall", "Saves");
var profile = ProfilePersistence.Load(saveDirectory);
var today = DateOnly.FromDateTime(DateTime.Now);

// Daily Inferno always plays today's shared, date-hashed seed.
var seed = startMode == GameMode.DailyInferno
    ? DailySchedule.SeedForDate(today)
    : pinnedSeed ?? (ulong)Environment.TickCount64;

GameSetup.InitializeSingletons(world, seed, pinSeed: pinnedSeed is not null, presentation: true, startMode);
world.SetSingleton(new ProfileState
{
    Profile = profile,
    SaveEnabled = true,
    SaveDirectory = saveDirectory,
    TodayKey = DailySchedule.DateKey(today),
});
GameSetup.RegisterSystems(world, fontPath);
GameSetup.StartRun(world, seed);

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Run seed: {seed}"));
Console.WriteLine($"Mode: {ModeCatalog.NameOf(startMode)}");
Console.WriteLine("Starting...");

// TestBridge: expose the live game world to external tools (the MCP server)
// over a named pipe, following the editor's integration pattern. Windowed
// mode only — headless CI runs stay free of IPC endpoints.
//
// The start is blocked on rather than awaited ON PURPOSE, and this is not a
// tidy-up candidate: an 'await' here would resume the rest of this file - Run()
// included - on a thread-pool thread, and Run() is what creates the OS window.
// macOS/AppKit only permits that on the process main thread and aborts the
// process outright otherwise (#1364). Everything up to Run() therefore stays
// synchronous. Blocking (rather than fire-and-forget) also keeps a failed start
// reportable: it surfaces here instead of in an unobserved task.
var bridgePlugin = new TestBridgePlugin(new TestBridgeOptions { EnableIpc = true });
var bridgeServer = default(IpcBridgeServer);
try
{
    world.InstallPlugin(bridgePlugin);
    bridgeServer = new IpcBridgeServer(
        world.GetExtension<ITestBridge>(),
        new IpcOptions { PipeName = "KeenEyes.NovaFall.TestBridge" });
    bridgeServer.StartAsync().GetAwaiter().GetResult();
    Console.WriteLine("[TestBridge] IPC server started on pipe: KeenEyes.NovaFall.TestBridge");
}
catch (Exception ex)
{
    // The bridge is a debugging aid; the game must run fine without it.
    Console.WriteLine($"[TestBridge] Unavailable: {ex.Message}");
}

try
{
    world.CreateRunner()
        .OnReady(() =>
        {
            var input = world.GetExtension<IInputContext>();
            var keyboard = InputDevices.FirstKeyboard(input);
            if (keyboard is not null)
            {
                keyboard.OnKeyDown += eventArgs =>
                {
                    if (eventArgs.Key == Key.Escape)
                    {
                        Console.WriteLine("Escape pressed - exiting...");
                        System.Environment.Exit(0);
                    }
                };
            }
            else
            {
                Console.WriteLine("No keyboard detected — close the window to exit.");
            }

            if (input.ConnectedGamepadCount == 0)
            {
                Console.WriteLine("No gamepad detected — keyboard controls cover everything.");
            }

            // The UI layout system needs to know the pixel canvas size.
            if (world.TryGetExtension<KeenEyes.Graphics.Abstractions.IGraphicsContext>(out var graphics)
                && graphics is not null)
            {
                world.GetSystem<UILayoutSystem>()?.SetScreenSize(graphics.Width, graphics.Height);
            }

            Console.WriteLine("Ready. A/D cycles the mode, Tab switches to styles, Space dives.");
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
    // Top-level demo entry point: Run() covers BOTH startup (window/graphics/GL
    // creation, which can surface a wide range of platform exceptions) and the frame
    // loop itself, so anything escaping here may have come from either. A demo
    // recovers by reporting the failure and exiting rather than crashing, so a
    // catch-all is appropriate here.
    ReportRunFailure(ex);
}
finally
{
    // TestBridge teardown mirrors the editor's: stop the pipe server before the
    // world (and the bridge plugin inside it) is disposed. Awaiting is safe here
    // and nowhere above: Run() has already returned, so the only work the
    // continuation can land on a thread-pool thread is teardown.
    if (bridgeServer is not null)
    {
        await bridgeServer.StopAsync();
        bridgeServer.Dispose();
    }
}

Console.WriteLine("Sample complete!");

// Reports a failure the honest way: WHAT failed comes first, because a blanket
// "requires a display" is actively misleading on a machine that has one and hides
// the real exception type behind a wrong diagnosis. The requirement list follows as
// a suggestion, never as the stated cause.
static void ReportStartupFailure(Exception ex)
{
    Console.WriteLine($"Startup failed: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(PlatformHint());
}

// Same idea for the loop, minus the false claim of "startup": Run() returns only
// once the frame loop ends, so a throw from frame 1 lands here too, and labelling
// that a startup failure sends readers hunting in entirely the wrong place.
static void ReportRunFailure(Exception ex)
{
    Console.WriteLine($"NOVAFALL stopped: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine("Run() covers both startup and the frame loop, so this may be a fault from the "
        + "first frames rather than from starting up.");
    Console.WriteLine(PlatformHint());
}

static string PlatformHint()
    => "Windowed mode needs a display, a GPU driver, and OpenGL 3.3; the line above says which of those "
        + "actually failed. For a check that needs none of them, run with --simulate 600.";

// Headless determinism harness: builds the world with no window, graphics,
// audio, particle, animation, or UI plugins (every juice system no-ops), steps
// a fixed number of frames at a fixed timestep, and prints the procedural
// layout, deterministic event counters, and final state. Running this twice
// with the same seed and mode must produce byte-identical output. It never
// touches save files: the profile stays in memory, keeping CI hermetic.
static void RunHeadlessSimulation(int frames, ulong seed, GameMode mode)
{
    using var world = new World();

    GameSetup.InstallSimulationPlugins(world);
    GameSetup.InitializeSingletons(world, seed, pinSeed: true, presentation: false, mode);
    GameSetup.RegisterSystems(world, fontPath: null);
    GameSetup.StartRun(world, seed);

    // No input exists to press a start key, so the harness starts the run itself.
    world.GetSingleton<GameState>().Phase = GamePhase.Playing;

    const float fixedDeltaTime = 1f / 60f;
    for (var i = 0; i < frames; i++)
    {
        world.Update(fixedDeltaTime);
    }

    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"NOVAFALL simulation: seed={seed} frames={frames} mode={ModeCatalog.NameOf(mode)}"));

    for (var i = 0; i < 10; i++)
    {
        var (gapCenterX, gapWidth) = FloorLayout.GapForFloor(seed, i);
        var kind = FloorLayout.KindForFloor(seed, i);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"floor {i}: gapCenter={gapCenterX:F3} gapWidth={gapWidth:F3} kind={kind}"));
    }

    var counters = world.GetSingleton<RunEventCounters>();
    Console.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"events: smashes={counters.Smashes} grazes={counters.Grazes} crumbles={counters.Crumbles} " +
        $"bumps={counters.Bumps} surges={counters.SurgeWindows} adrenalineSaves={counters.AdrenalineSavesUsed}"));

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
