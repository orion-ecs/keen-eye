namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down NOVAFALL's procedural floor script: the same seed must produce the
/// identical sequence of gaps AND personalities, whether computed as a pure
/// function or spawned by two independently simulated worlds.
/// </summary>
public class FloorScriptTests
{
    /// <summary>A fixed seed whose script the golden tests pin down.</summary>
    private const ulong GoldenSeed = 0xD00DFEED;

    #region Cross-world determinism

    [Fact]
    public void FloorScript_SameSeed_TwoFreshWorlds_SpawnIdenticalFloors()
    {
        // Ember Garden has no death, so a long unattended run keeps spawning
        // floors — the mode-as-config knobs doing test-harness duty.
        var scriptA = CaptureSpawnedFloorScript(GoldenSeed, frames: 7200);
        var scriptB = CaptureSpawnedFloorScript(GoldenSeed, frames: 7200);

        Assert.True(scriptA.Count >= 30, $"expected a long script, captured {scriptA.Count} floors");
        Assert.Equal(scriptA.Count, scriptB.Count);

        foreach (var (index, floorA) in scriptA)
        {
            var floorB = scriptB[index];
            Assert.Equal(floorA.Kind, floorB.Kind);
            Assert.Equal(floorA.GapCenterX, floorB.GapCenterX);
            Assert.Equal(floorA.GapWidth, floorB.GapWidth);
        }
    }

    [Fact]
    public void FloorScript_SpawnedFloors_MatchPureLayoutFunctions()
    {
        var script = CaptureSpawnedFloorScript(GoldenSeed, frames: 3600);

        foreach (var (index, floor) in script)
        {
            var (gapCenterX, gapWidth) = FloorLayout.GapForFloor(GoldenSeed, index);
            Assert.Equal(gapCenterX, floor.GapCenterX);
            Assert.Equal(gapWidth, floor.GapWidth);
            Assert.Equal(FloorLayout.KindForFloor(GoldenSeed, index), floor.Kind);
        }
    }

    #endregion

    #region Golden script (regression guard)

    [Fact]
    public void KindForFloor_BeforeBrittleUnlock_IsAlwaysStandard()
    {
        for (var index = 0; index < Tuning.BrittleMinFloorIndex; index++)
        {
            Assert.Equal(FloorKind.Standard, FloorLayout.KindForFloor(GoldenSeed, index));
        }
    }

    [Fact]
    public void KindForFloor_GoldenSeed_MatchesPinnedScript()
    {
        // The exact personality placements for the golden seed. If the kind
        // stream, its salt, the phase-in indexes, or the chance bands change,
        // this fails — which is the point: the floor script is a contract.
        var expectedNonStandard = new Dictionary<int, FloorKind>
        {
            [19] = FloorKind.Brittle,
            [35] = FloorKind.Brittle,
            [39] = FloorKind.Bumper,
            [42] = FloorKind.Pulse,
            [45] = FloorKind.Brittle,
            [46] = FloorKind.Pulse,
            [47] = FloorKind.Bumper,
            [51] = FloorKind.Pulse,
            [54] = FloorKind.Brittle,
            [55] = FloorKind.Brittle,
            [58] = FloorKind.Pulse,
            [60] = FloorKind.Brittle,
            [68] = FloorKind.Brittle,
            [72] = FloorKind.Bumper,
            [74] = FloorKind.Brittle,
            [78] = FloorKind.Pulse,
        };

        for (var index = 0; index < 80; index++)
        {
            var expected = expectedNonStandard.TryGetValue(index, out var kind)
                ? kind
                : FloorKind.Standard;
            Assert.Equal(expected, FloorLayout.KindForFloor(GoldenSeed, index));
        }
    }

    [Fact]
    public void KindForFloor_GoldenSeed_PersonalitiesStayMinoritySpice()
    {
        var counts = new Dictionary<FloorKind, int>();
        const int sample = 80;
        for (var index = 0; index < sample; index++)
        {
            var kind = FloorLayout.KindForFloor(GoldenSeed, index);
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
        }

        Assert.Equal(64, counts.GetValueOrDefault(FloorKind.Standard));
        Assert.Equal(8, counts.GetValueOrDefault(FloorKind.Brittle));
        Assert.Equal(3, counts.GetValueOrDefault(FloorKind.Bumper));
        Assert.Equal(5, counts.GetValueOrDefault(FloorKind.Pulse));

        // The design cap: personalities are ~25% of floors, never the majority.
        var personalities = sample - counts.GetValueOrDefault(FloorKind.Standard);
        Assert.True(personalities <= sample * 0.3f, $"{personalities} personalities in {sample} floors");
    }

    [Fact]
    public void GapForFloor_GoldenSeed_MatchesPinnedValues()
    {
        var (center0, width0) = FloorLayout.GapForFloor(GoldenSeed, 0);
        Assert.Equal(482.338f, center0, tolerance: 0.001f);
        Assert.Equal(103.472f, width0, tolerance: 0.001f);

        var (center40, width40) = FloorLayout.GapForFloor(GoldenSeed, 40);
        Assert.Equal(376.559f, center40, tolerance: 0.001f);
        Assert.Equal(138.526f, width40, tolerance: 0.001f);
    }

    #endregion

    /// <summary>
    /// Builds a world exactly the way <c>--simulate</c> does (no presentation
    /// plugins), runs it unattended, and records every floor as it spawns.
    /// </summary>
    private static Dictionary<int, Floor> CaptureSpawnedFloorScript(ulong seed, int frames)
    {
        using var world = new World();

        GameSetup.InstallSimulationPlugins(world);
        GameSetup.InitializeSingletons(
            world, seed, pinSeed: true, presentation: false, GameMode.EmberGarden);
        GameSetup.RegisterSystems(world, fontPath: null);
        GameSetup.StartRun(world, seed);

        world.GetSingleton<GameState>().Phase = GamePhase.Playing;

        var script = new Dictionary<int, Floor>();
        const float fixedDeltaTime = 1f / 60f;

        for (var i = 0; i < frames; i++)
        {
            world.Update(fixedDeltaTime);

            foreach (var entity in world.Query<Floor>())
            {
                ref readonly var floor = ref world.Get<Floor>(entity);
                script.TryAdd(floor.Index, floor);
            }
        }

        return script;
    }
}
