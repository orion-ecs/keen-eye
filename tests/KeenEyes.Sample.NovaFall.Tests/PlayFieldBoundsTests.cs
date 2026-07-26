using KeenEyes.Common;

namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Property tests for NOVAFALL's hardest gameplay invariant: the ball is always
/// inside the shaft, and the shaft always has a floor below it.
/// </summary>
/// <remarks>
/// <para>
/// THE BUG THESE GUARD — the ball falls faster (<see cref="Tuning.MaxFallSpeed"/>)
/// than the shaft rises (<see cref="Tuning.MaxScrollSpeed"/>), so a player who
/// threads several gaps in a row genuinely outruns the shaft. Before the
/// shaft-floor clamp, such a run left the play field: floors were only ever
/// maintained down to a fixed screen-space band, so once the ball was below it no
/// floor could ever exist beneath it, <see cref="CollisionSystem"/> could never
/// fire again, and — because depth accrues from the scroll speed rather than from
/// the ball — the run became unloseable with an ever-climbing score.
/// </para>
/// <para>
/// TEACHING NOTE — why property tests instead of example tests? The escape was
/// reachable in ordinary play but only with luck, which is exactly the shape of
/// bug an example test misses and a simulated invariant sweep catches. The
/// headless harness (the same one <c>--simulate</c> uses) makes thousands of
/// frames across many seeds cheap, and the adversarial steering below reproduces
/// the lucky run on purpose rather than hoping for it.
/// </para>
/// </remarks>
public class PlayFieldBoundsTests
{
    /// <summary>Fixed simulation timestep, matching headless <c>--simulate</c> mode.</summary>
    private const float FixedDeltaTime = 1f / 60f;

    /// <summary>Slack for accumulated float error when comparing positions.</summary>
    private const float PositionTolerance = 0.01f;

    /// <summary>
    /// The lowest center Y the ball may ever reach: the shaft's full extent minus
    /// its radius, mirroring the horizontal wall clamp. Derived here exactly as
    /// <see cref="BallMovementSystem"/> derives it, so the test pins the rule and
    /// not a magic number.
    /// </summary>
    private const float BallMaxY = Tuning.ShaftHeight - Tuning.BallRadius;

    /// <summary>How a simulated run steers, in place of a human at the keyboard.</summary>
    private enum Steering
    {
        /// <summary>Hands off the controls — the ball falls straight down.</summary>
        None,

        /// <summary>
        /// Always steer toward the gap in the nearest floor below. This is the
        /// "lucky threading" run that caused the reported escape, made relentless.
        /// </summary>
        TowardNearestGap,
    }

    #region Invariant sweeps

    [Theory]
    [InlineData(0xD00DFEEDUL)]
    [InlineData(0x5EEDF00DUL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    [InlineData(0xFEEDBEEFCAFEUL)]
    public void PlayFieldInvariants_EverySteeringAndMode_HoldForThousandsOfFrames(ulong seed)
    {
        // Both modes matter: FREEFALL escalates the scroll and can end the run,
        // EMBER GARDEN never ends one, so it is the mode a runaway would show up
        // in. Both steering strategies matter: hands-off is the common case,
        // gap-chasing is the case that broke.
        foreach (var mode in new[] { GameMode.Freefall, GameMode.EmberGarden })
        {
            foreach (var steering in new[] { Steering.None, Steering.TowardNearestGap })
            {
                SimulateAndAssertInvariants(seed, mode, steering, frames: 1800);
            }
        }
    }

    [Fact]
    public void PlayFieldInvariants_AdversarialGapChasing_OutrunsTheShaftAndStillHolds()
    {
        // A run long enough that the ball spends most of it pinned to the shaft
        // floor: gap-chasing reaches terminal velocity within a second, and
        // terminal velocity is more than three times the fastest the shaft rises.
        var result = SimulateAndAssertInvariants(
            0xD00DFEEDUL, GameMode.EmberGarden, Steering.TowardNearestGap, frames: 6000);

        Assert.True(
            result.FramesOnShaftFloor > 1000,
            $"expected the gap-chasing ball to ride the shaft floor; only {result.FramesOnShaftFloor} frames did");
        Assert.True(result.DeepestBallY <= BallMaxY + PositionTolerance);
    }

    [Fact]
    public void Score_LongGapChasingRun_StaysBoundedByDepthTimesMaxMultiplier()
    {
        // Score integrates meters of depth times the live heat multiplier, plus
        // flat event bonuses. The multiplier is capped at the Nova tier, so a
        // legitimate score can never exceed depth x 8 plus the bonuses actually
        // earned. The reported bug never broke that formula — it broke the thing
        // that makes the formula honest, namely that depth is only survivable
        // while the ball is in play. Hence both assertions here.
        using var world = CreateSimulatedWorld(0xD00DFEEDUL, GameMode.Freefall);
        var ball = RequireBall(world);

        for (var i = 0; i < 6000; i++)
        {
            SetSteering(world, ball, Steering.TowardNearestGap);
            world.Update(FixedDeltaTime);
        }

        var depth = world.GetSingleton<ScrollState>().Depth;
        var score = world.GetSingleton<ScoreState>().Score;
        var counters = world.GetSingleton<RunEventCounters>();

        // A bound over a run that went nowhere would prove nothing.
        Assert.True(depth > 100f, $"the gap-chasing run only reached {depth}m");

        var maxMultiplier = HeatSystem.MultiplierForTier(3);
        var bonuses = (counters.Grazes * Tuning.GrazeScoreBonus)
            + (counters.Smashes * Tuning.SmashScoreBonus)
            + (counters.SurgeWindows * Tuning.SurgeSweepBonus);

        // The +1 absorbs float accumulation across 6000 integration steps.
        Assert.True(
            score <= (depth * maxMultiplier) + bonuses + 1.0,
            $"score {score} exceeds the bound for depth {depth}m (bonuses {bonuses})");

        // And the ball that earned it is still a target the Furnace can reach.
        var ballY = world.Get<Position2D>(ball).Y;
        Assert.True(
            ballY <= BallMaxY + PositionTolerance,
            $"score {score} was earned by a ball outside the play field at Y = {ballY}");
    }

    [Fact]
    public void Run_WithoutSteering_StillEndsInDeath()
    {
        // The complaint was an UNLOSEABLE run. A hands-off FREEFALL run must
        // still end: the ball lands, the floor carries it into the Furnace, and
        // the crusher does its job.
        using var world = CreateSimulatedWorld(0x5EEDF00DUL, GameMode.Freefall);

        for (var i = 0; i < 3600 && world.GetSingleton<GameState>().Phase != GamePhase.Dead; i++)
        {
            world.Update(FixedDeltaTime);
        }

        Assert.Equal(GamePhase.Dead, world.GetSingleton<GameState>().Phase);
    }

    #endregion

    #region Reported-state regression

    [Fact]
    public void Ball_ForcedBelowTheOldSpawnBand_IsClampedBackIntoTheFieldAndCollidesAgain()
    {
        // The exact state the player reported: the ball below every floor the old
        // spawn rule would ever maintain (a fixed band at
        // ShaftHeight + FloorSpacing), falling at terminal velocity, with nothing
        // left to collide with. THIS TEST FAILS ON THE PRE-FIX CODE.
        using var world = CreateSimulatedWorld(0xD00DFEEDUL, GameMode.Freefall);
        var ball = RequireBall(world);

        // One frame to populate the shaft the way the real game does.
        world.Update(FixedDeltaTime);

        // Hard against the left wall: gaps are kept at least Tuning.GapWallMargin
        // from a wall, so a ball parked there is never over one and is guaranteed
        // to be caught by the next floor that rises into it.
        world.Get<Position2D>(ball).X = Tuning.BallRadius;
        world.Get<Position2D>(ball).Y = Tuning.ShaftHeight + (4f * Tuning.FloorSpacing);
        world.Get<Velocity2D>(ball).Y = Tuning.MaxFallSpeed;

        world.Update(FixedDeltaTime);

        Assert.True(
            world.Get<Position2D>(ball).Y <= BallMaxY + PositionTolerance,
            $"escaped ball was not brought back into the field: Y = {world.Get<Position2D>(ball).Y}");

        // ...and collisions resume: a rising floor catches the bottomed-out ball.
        var collided = false;
        for (var i = 0; i < 900 && !collided; i++)
        {
            world.Update(FixedDeltaTime);
            collided = world.Has<RestingOn>(ball) || world.GetSingleton<FrameEvents>().Landed;
        }

        Assert.True(collided, "the recovered ball never collided with a floor again");
    }

    [Fact]
    public void FloorSpawning_WithTheClampBypassed_StillKeepsAFloorBelowTheBall()
    {
        // Defence in depth, tested on its own terms: with the clamp switched off
        // there is nothing keeping the ball in the field, so floor spawning has to
        // be the thing that makes "no floor below the ball" impossible. Disabling
        // one system to test the guard behind it is why systems are individually
        // addressable. THIS TEST FAILS ON THE PRE-FIX CODE.
        using var world = CreateSimulatedWorld(0xD00DFEEDUL, GameMode.EmberGarden);
        world.GetSystem<BallMovementSystem>()!.Enabled = false;

        var ball = RequireBall(world);
        var forcedY = Tuning.ShaftHeight + (6f * Tuning.FloorSpacing);

        for (var i = 0; i < 120; i++)
        {
            // Re-force the position every frame: a landing would otherwise lift
            // the ball back into the field and end the scenario early.
            world.Get<Position2D>(ball).Y = forcedY;
            world.Update(FixedDeltaTime);

            Assert.True(
                TryFindDeepestFloorY(world, out var deepestFloorY) && deepestFloorY > forcedY,
                $"frame {i}: no floor exists below a ball at Y = {forcedY} (deepest floor {deepestFloorY})");
        }
    }

    [Fact]
    public void Ball_RestingOnTheShaftFloor_DoesNotAccumulateFallVelocity()
    {
        // Velocity the ball can never spend would turn its next floor contact into
        // a colossal fake impact (squash scales with landing speed) and would let
        // one slow frame teleport it back out of the field.
        using var world = CreateSimulatedWorld(0xD00DFEEDUL, GameMode.EmberGarden);
        var ball = RequireBall(world);

        world.Get<Position2D>(ball).X = Tuning.BallRadius;
        world.Get<Position2D>(ball).Y = BallMaxY;
        world.Get<Velocity2D>(ball).Y = Tuning.MaxFallSpeed;

        // One frame's worth of gravity is all the speed a bottomed-out ball may
        // ever hold: it is applied, spent against the clamp, and zeroed again.
        var oneFrameOfGravity = Tuning.Gravity * FixedDeltaTime;

        for (var i = 0; i < 30; i++)
        {
            world.Update(FixedDeltaTime);

            if (world.Has<RestingOn>(ball))
            {
                // A floor rose into the ball and caught it — the self-correcting
                // behaviour this fix exists to restore. Nothing left to measure.
                return;
            }

            Assert.Equal(BallMaxY, world.Get<Position2D>(ball).Y, tolerance: PositionTolerance);
            Assert.True(
                world.Get<Velocity2D>(ball).Y <= oneFrameOfGravity + PositionTolerance,
                $"frame {i}: banked fall velocity {world.Get<Velocity2D>(ball).Y}");
        }
    }

    #endregion

    #region Harness

    /// <summary>Per-frame invariant results, kept for the sweep's own assertions.</summary>
    private readonly record struct SweepResult(float DeepestBallY, int FramesOnShaftFloor);

    /// <summary>
    /// Steps a headless run and asserts, every single frame, that the ball is
    /// inside the play field and that a floor exists below it.
    /// </summary>
    private static SweepResult SimulateAndAssertInvariants(
        ulong seed, GameMode mode, Steering steering, int frames)
    {
        using var world = CreateSimulatedWorld(seed, mode);
        var ball = RequireBall(world);

        var deepestBallY = float.MinValue;
        var framesOnShaftFloor = 0;

        for (var i = 0; i < frames; i++)
        {
            SetSteering(world, ball, steering);
            world.Update(FixedDeltaTime);

            var ballY = world.Get<Position2D>(ball).Y;
            deepestBallY = Math.Max(deepestBallY, ballY);

            Assert.True(
                ballY <= BallMaxY + PositionTolerance,
                $"seed {seed} / {mode} / {steering}, frame {i}: ball escaped the field at Y = {ballY}");

            if (ballY.ApproximatelyEquals(BallMaxY, epsilon: 0.5f))
            {
                framesOnShaftFloor++;
            }

            // A dead run stops moving anything; the field invariant above still
            // has to hold, but "a floor below the ball" is only meaningful while
            // the shaft is live.
            if (world.GetSingleton<GameState>().Phase != GamePhase.Playing)
            {
                continue;
            }

            Assert.True(
                TryFindDeepestFloorY(world, out var deepestFloorY) && deepestFloorY > ballY,
                $"seed {seed} / {mode} / {steering}, frame {i}: no floor below a ball at Y = {ballY}");
        }

        return new SweepResult(deepestBallY, framesOnShaftFloor);
    }

    /// <summary>
    /// Builds a world exactly the way <c>--simulate</c> does — no presentation
    /// plugins, no save files — and starts the run.
    /// </summary>
    private static World CreateSimulatedWorld(ulong seed, GameMode mode)
    {
        var world = new World();

        GameSetup.InstallSimulationPlugins(world);
        GameSetup.InitializeSingletons(world, seed, pinSeed: true, presentation: false, mode);
        GameSetup.RegisterSystems(world, fontPath: null);
        GameSetup.StartRun(world, seed);

        // No input device exists to press a start key, so the harness dives itself.
        world.GetSingleton<GameState>().Phase = GamePhase.Playing;

        return world;
    }

    private static Entity RequireBall(World world)
    {
        foreach (var entity in world.Query<Ball>())
        {
            return entity;
        }

        throw new InvalidOperationException("the run has no ball entity");
    }

    /// <summary>
    /// Writes the strategy's steering axis onto the ball, standing in for
    /// <see cref="InputSteerSystem"/> (which no-ops with no input device).
    /// </summary>
    private static void SetSteering(World world, Entity ball, Steering steering)
    {
        world.Get<SteerInput>(ball).Axis = steering switch
        {
            Steering.TowardNearestGap => AxisTowardNearestGapBelow(world, ball),
            _ => 0f,
        };
    }

    /// <summary>
    /// Returns the steering axis that aims the ball at the gap in the nearest
    /// floor below it — a relentless, deterministic stand-in for a lucky player.
    /// </summary>
    private static float AxisTowardNearestGapBelow(World world, Entity ball)
    {
        var ballPosition = world.Get<Position2D>(ball);
        var musicSeconds = world.GetSingleton<MusicClock>().Seconds;

        var nearestFloorY = float.MaxValue;
        var targetX = ballPosition.X;

        foreach (var entity in world.Query<Floor, Position2D>())
        {
            var floorY = world.Get<Position2D>(entity).Y;
            if (floorY <= ballPosition.Y || floorY >= nearestFloorY)
            {
                continue;
            }

            ref readonly var floor = ref world.Get<Floor>(entity);

            // Ignore a gap the ball could not fit through anyway (a Pulse floor
            // mid-close), so the chase never aims at a closed slab.
            if (FloorLayout.EffectiveGapWidth(in floor, musicSeconds) <= 2f * Tuning.BallRadius)
            {
                continue;
            }

            nearestFloorY = floorY;
            targetX = floor.GapCenterX;
        }

        var delta = targetX - ballPosition.X;

        // Inside a design unit of the target is close enough — never == on floats.
        return delta.IsApproximatelyZero(epsilon: 1f) ? 0f : Math.Sign(delta);
    }

    private static bool TryFindDeepestFloorY(World world, out float deepestFloorY)
    {
        deepestFloorY = float.MinValue;
        var found = false;

        foreach (var entity in world.Query<Floor, Position2D>())
        {
            deepestFloorY = Math.Max(deepestFloorY, world.Get<Position2D>(entity).Y);
            found = true;
        }

        return found;
    }

    #endregion
}
