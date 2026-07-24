namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Resolves ball-versus-floor collisions: landing, resting (being carried upward),
/// slipping into a gap, clean gap-through detection, and — at Plasma tier and
/// above — the Floor Smash.
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
/// score bonus, and fire the hitstop/camera/particle payoff. A smash can never
/// trigger on two consecutive floors, so the flow keeps a rhythm of
/// smash → thread → smash. Smashing is a pure function of simulation state, so
/// the headless <c>--simulate</c> mode replays it identically.
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
        ref var position = ref World.Get<Position2D>(ballEntity);
        ref var velocity = ref World.Get<Velocity2D>(ballEntity);
        ref var events = ref World.GetSingleton<FrameEvents>();

        if (World.Has<RestingOn>(ballEntity))
        {
            UpdateResting(ballEntity, radius, ref position, ref velocity);
            return;
        }

        var tier = World.GetSingleton<HeatState>().Tier;
        ref var smashState = ref World.GetSingleton<SmashState>();

        // Airborne: row-scan the floors for landings, smashes, and gap-throughs.
        var landedOn = default(Entity);
        var landed = false;
        var smashedFloor = default(Entity);
        var smashed = false;

        foreach (var floorEntity in World.Query<Floor, Position2D>())
        {
            ref var floor = ref World.Get<Floor>(floorEntity);
            var floorTop = World.Get<Position2D>(floorEntity).Y;
            var ballBottom = position.Y + radius;

            // Contact: the ball's bottom is within the floor slab (plus a small
            // tolerance for fast frames) while falling, and not over the gap.
            if (!landed && !smashed
                && velocity.Y >= 0f
                && ballBottom >= floorTop
                && ballBottom <= floorTop + floor.Thickness + Tuning.LandingTolerance
                && !IsOverGap(position.X, radius, in floor))
            {
                // Hot enough to smash, and not the floor immediately after the
                // last smashed one? The impact shatters the floor instead of
                // stopping the fall.
                if (tier >= Tuning.SmashMinTier
                    && floor.Index != smashState.LastSmashedFloorIndex + 1)
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

                position.Y = floorTop - radius;
                events.LandingSpeed = velocity.Y;
                velocity.Y = 0f;
                landedOn = floorEntity;
                landed = true;
                events.Landed = true;
                continue;
            }

            // Clean gap-through: the ball's center has passed below a floor it
            // never rested on top of at this moment — the only way down is
            // through the gap.
            if (!floor.Cleared && position.Y > floorTop + floor.Thickness)
            {
                floor.Cleared = true;
                events.GapsPassed++;
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

    private void UpdateResting(Entity ballEntity, float radius, ref Position2D position, ref Velocity2D velocity)
    {
        var restingOn = World.Get<RestingOn>(ballEntity);
        var floorEntity = restingOn.FloorEntity;

        if (!World.IsAlive(floorEntity) || !World.Has<Floor>(floorEntity))
        {
            // The floor scrolled away and was despawned out from under us.
            World.Remove<RestingOn>(ballEntity);
            return;
        }

        ref readonly var floor = ref World.Get<Floor>(floorEntity);
        var floorTop = World.Get<Position2D>(floorEntity).Y;

        // The floor carries the resting ball upward, toward the Furnace.
        position.Y = floorTop - radius;
        velocity.Y = 0f;

        // Steered over the gap? Let go and fall.
        if (IsOverGap(position.X, radius, in floor))
        {
            World.Remove<RestingOn>(ballEntity);
        }
    }

    private static bool IsOverGap(float ballX, float radius, in Floor floor)
    {
        // The ball only fits through when it is fully inside the gap.
        var gapLeft = floor.GapCenterX - floor.GapWidth / 2f;
        var gapRight = floor.GapCenterX + floor.GapWidth / 2f;
        return ballX > gapLeft + radius && ballX < gapRight - radius;
    }
}
