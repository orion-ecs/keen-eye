namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Resolves ball-versus-floor collisions: landing, resting (being carried upward),
/// slipping into a gap, clean gap-through detection, floor personalities
/// (Brittle crack starts, Bumper launches, Pulse gap phasing), and — at Plasma
/// tier and above, or at any tier during a Flashover Surge — the Floor Smash.
/// </summary>
/// <remarks>
/// <para>
/// TEACHING NOTE — why a simple row scan instead of a spatial index?
/// </para>
/// <para>
/// NOVAFALL keeps roughly 10-15 live floors and exactly one ball, so a collision
/// test is ~15 AABB checks per frame — trivially cheap and, more importantly,
/// trivially correct. A quadtree (see <c>KeenEyes.Spatial</c>) would add tree
/// maintenance every frame (floors move every frame, so the tree churns
/// constantly) just to answer a query the flat scan answers faster. Spatial
/// partitioning earns its keep when BOTH sides of the test are numerous — hundreds
/// of projectiles against hundreds of colliders — because it turns an O(n*m)
/// pairing into O(n log m). With m = 15, the constant factors dominate and the
/// simplest code wins. Measure before you partition. (For the counterpoint, see
/// <see cref="GrazeDetectionSystem"/>, which uses the quadtree.)
/// </para>
/// <para>
/// FLOOR SMASH — at Plasma or Nova tier, what would have been a landing instead
/// shatters the floor: the ball keeps falling (at reduced speed), the floor
/// entity despawns, and downstream systems charge one full heat tier, award the
/// score bonus, and fire the hitstop/camera/particle payoff. Outside a surge a
/// smash can never trigger on two consecutive floors, so the flow keeps a rhythm
/// of smash → thread → smash; during a Flashover Surge every floor is smashable
/// at any tier and the rhythm restriction lifts — scheduled catharsis. Smashing
/// is a pure function of simulation state, so the headless <c>--simulate</c>
/// mode replays it identically.
/// </para>
/// <para>
/// PERSONALITIES — contact dispatches on <see cref="Floor.Kind"/>: a Bumper
/// launches the ball back up instead of catching it; a Brittle floor lands like
/// a Standard one but starts its crack telegraph (crumbling is
/// <see cref="FloorPersonalitySystem"/>'s job); a Pulse floor's gap width comes
/// from <see cref="FloorLayout.EffectiveGapWidth"/>, so a closed gap is simply
/// solid slab here — the same function rendering uses, keeping eyes and physics
/// in perfect agreement.
/// </para>
/// <para>
/// The system publishes its events into the <see cref="FrameEvents"/> singleton,
/// which <see cref="HeatSystem"/>, <see cref="ScoreSystem"/>, and the juice
/// systems consume.
/// </para>
/// </remarks>
public sealed class CollisionSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (World.GetSingleton<GameState>().Phase != GamePhase.Playing)
        {
            return;
        }

        // There is exactly one ball; grab it without structural changes mid-query.
        var ballEntity = default(Entity);
        var ballFound = false;
        foreach (var entity in World.Query<Ball, Position2D, Velocity2D>())
        {
            ballEntity = entity;
            ballFound = true;
            break;
        }

        if (!ballFound)
        {
            return;
        }

        var radius = World.Get<Ball>(ballEntity).Radius;
        var musicSeconds = World.GetSingleton<MusicClock>().Seconds;
        ref var position = ref World.Get<Position2D>(ballEntity);
        ref var velocity = ref World.Get<Velocity2D>(ballEntity);
        ref var events = ref World.GetSingleton<FrameEvents>();

        if (World.Has<RestingOn>(ballEntity))
        {
            UpdateResting(ballEntity, radius, musicSeconds, ref position, ref velocity);
            return;
        }

        var tier = World.GetSingleton<HeatState>().Tier;
        var surgeActive = World.GetSingleton<SurgeState>().Active;
        ref var smashState = ref World.GetSingleton<SmashState>();

        // Airborne: row-scan the floors for landings, smashes, and gap-throughs.
        var landedOn = default(Entity);
        var landed = false;
        var smashedFloor = default(Entity);
        var smashed = false;
        var bumped = false;

        foreach (var floorEntity in World.Query<Floor, Position2D>())
        {
            ref var floor = ref World.Get<Floor>(floorEntity);
            var floorTop = World.Get<Position2D>(floorEntity).Y;
            var ballBottom = position.Y + radius;

            // Contact: the ball's bottom is within the floor slab (plus a small
            // tolerance for fast frames) while falling, and not over the gap.
            if (!landed && !smashed && !bumped
                && velocity.Y >= 0f
                && ballBottom >= floorTop
                && ballBottom <= floorTop + floor.Thickness + Tuning.LandingTolerance
                && !IsOverGap(position.X, radius, in floor, musicSeconds))
            {
                // Hot enough to smash — or anything goes during a Flashover
                // Surge? The impact shatters the floor instead of stopping the
                // fall. The no-consecutive-floors rhythm rule also lifts during
                // a surge.
                if ((tier >= Tuning.SmashMinTier || surgeActive)
                    && (surgeActive || floor.Index != smashState.LastSmashedFloorIndex + 1))
                {
                    events.Smashed = true;
                    events.SmashX = position.X;
                    events.SmashY = floorTop;
                    events.SmashImpactSpeed = velocity.Y;
                    events.SmashGapCenterX = floor.GapCenterX;
                    events.SmashGapWidth = floor.GapWidth;

                    smashState.LastSmashedFloorIndex = floor.Index;
                    World.GetSingleton<RunEventCounters>().Smashes++;

                    // Brief fall-speed relief: punch through, don't free-fall.
                    velocity.Y *= Tuning.SmashFallRetention;

                    smashedFloor = floorEntity;
                    smashed = true;
                    continue;
                }

                // Bumper: an elastic launch upward instead of a landing. The
                // Furnace is UP — the launch is the hazard, and the floor's
                // distinct look is its warning.
                if (floor.Kind == FloorKind.Bumper)
                {
                    events.Bumped = true;
                    events.BumpX = position.X;
                    events.BumpY = floorTop;
                    events.BumpImpactSpeed = velocity.Y;

                    position.Y = floorTop - radius;
                    velocity.Y = -Tuning.BumperLaunchSpeed;
                    floor.WobbleSeconds = Tuning.BumperWobbleSeconds;
                    World.GetSingleton<RunEventCounters>().Bumps++;

                    bumped = true;
                    continue;
                }

                position.Y = floorTop - radius;
                events.LandingSpeed = velocity.Y;
                velocity.Y = 0f;
                landedOn = floorEntity;
                landed = true;
                events.Landed = true;

                // Brittle: landing starts the crack telegraph. The visual crack
                // and the crackle SFX begin NOW, a full crumble delay (>= 0.6 s,
                // the telegraph contract) before the floor gives way.
                if (floor.Kind == FloorKind.Brittle && !floor.Cracking)
                {
                    floor.Cracking = true;
                    floor.CrackSeconds = 0f;
                    events.CrackStarted = true;
                    events.CrackX = position.X;
                    events.CrackY = floorTop;
                }

                continue;
            }

            // Clean gap-through: the ball's center has passed below a floor it
            // never rested on top of at this moment — the only way down is
            // through the gap.
            if (!floor.Cleared && position.Y > floorTop + floor.Thickness)
            {
                floor.Cleared = true;
                events.GapsPassed++;

                // Feed the Flashover schedule: surges trigger on cleared floor
                // indexes, a pure function of the floor script.
                ref var surge = ref World.GetSingleton<SurgeState>();
                surge.DeepestClearedFloor = Math.Max(surge.DeepestClearedFloor, floor.Index);
            }
        }

        // Structural changes (component add, despawn) deferred until iteration
        // is done.
        if (landed)
        {
            World.Add(ballEntity, new RestingOn { FloorEntity = landedOn });
        }

        if (smashed)
        {
            // The floor is gone; VfxSystem turns the event into fragments.
            World.Despawn(smashedFloor);
        }
    }

    private void UpdateResting(
        Entity ballEntity, float radius, float musicSeconds,
        ref Position2D position, ref Velocity2D velocity)
    {
        var restingOn = World.Get<RestingOn>(ballEntity);
        var floorEntity = restingOn.FloorEntity;

        if (!World.IsAlive(floorEntity) || !World.Has<Floor>(floorEntity))
        {
            // The floor scrolled away (or a Brittle one crumbled) out from
            // under us.
            World.Remove<RestingOn>(ballEntity);
            return;
        }

        ref readonly var floor = ref World.Get<Floor>(floorEntity);
        var floorTop = World.Get<Position2D>(floorEntity).Y;

        // Once the floor has been carried into the Furnace band there is
        // nothing left to stand on: with the crusher on, death has already
        // happened; without it (Ember Garden), the ball lets go and falls.
        if (floorTop - radius < Tuning.CeilingY + radius)
        {
            World.Remove<RestingOn>(ballEntity);
            return;
        }

        // The floor carries the resting ball upward, toward the Furnace.
        position.Y = floorTop - radius;
        velocity.Y = 0f;

        // Steered over the gap (or a Pulse gap reopened under us)? Let go and fall.
        if (IsOverGap(position.X, radius, in floor, musicSeconds))
        {
            World.Remove<RestingOn>(ballEntity);
        }
    }

    private static bool IsOverGap(float ballX, float radius, in Floor floor, float musicSeconds)
    {
        // The ball only fits through when it is fully inside the gap. Pulse
        // floors shrink the effective gap as they phase closed.
        var gapWidth = FloorLayout.EffectiveGapWidth(in floor, musicSeconds);
        var gapLeft = floor.GapCenterX - gapWidth / 2f;
        var gapRight = floor.GapCenterX + gapWidth / 2f;
        return ballX > gapLeft + radius && ballX < gapRight - radius;
    }
}
