using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Scrolls floors upward, escalates scroll speed with depth, spawns new floors
/// below the view, and despawns floors that scroll past the Furnace ceiling.
/// </summary>
/// <remarks>
/// <para>
/// Floors form a ring-buffer style window over an infinite procedural shaft: only
/// the ~10 floors near the view exist as entities at any time. Each floor's gap is
/// a pure function of the run seed and the floor index (see <see cref="FloorLayout"/>),
/// so the layout is identical for every replay of a seed regardless of frame rate
/// or window size.
/// </para>
/// <para>
/// Floors also carry a <c>Transform3D</c> mirrored from their gameplay
/// <see cref="Position2D"/> plus the <c>SpatialIndexed</c> tag, which keeps them in
/// the quadtree that <see cref="GrazeDetectionSystem"/> queries. The mirror is
/// updated in the same loop that scrolls the floor, so the index is never more
/// than one frame behind.
/// </para>
/// <para>
/// Spawning and despawning run in every phase so the shaft is already populated on
/// the Ready screen; scrolling and depth accumulation only happen while Playing.
/// </para>
/// </remarks>
public sealed class FloorScrollSystem : SystemBase
{
    private readonly List<Entity> despawnList = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        ref var scroll = ref World.GetSingleton<ScrollState>();
        var runConfig = World.GetSingleton<RunConfig>();
        var seed = runConfig.Seed;
        var settings = runConfig.Settings;
        var playing = World.GetSingleton<GameState>().Phase == GamePhase.Playing;

        if (playing)
        {
            var dt = deltaTime * World.GetSingleton<TimeScale>().Value;

            // Scroll speed escalates with depth: the deeper you are, the faster
            // the Furnace chases you. The curve is the mode's (Ember Garden's is
            // flat), and a Flashover Surge spikes it.
            scroll.Speed = Math.Min(
                settings.BaseScrollSpeed + scroll.Depth * settings.ScrollSpeedPerMeter,
                settings.MaxScrollSpeed);

            if (World.GetSingleton<SurgeState>().Active)
            {
                scroll.Speed *= Tuning.SurgeScrollMultiplier;
            }

            var rise = scroll.Speed * dt;
            scroll.Depth += rise / Tuning.UnitsPerMeter;

            foreach (var entity in World.Query<Floor, Position2D, Transform3D>())
            {
                ref var position = ref World.Get<Position2D>(entity);
                position.Y -= rise;

                // Mirror into the spatial index's position source.
                ref var transform = ref World.Get<Transform3D>(entity);
                transform.Position = new Vector3(Tuning.ShaftWidth / 2f, position.Y, 0f);
            }
        }

        // Despawn floors that scrolled above the ceiling. Collect first, despawn
        // after: never structurally modify the world during query iteration.
        despawnList.Clear();
        var lowestY = float.MinValue;
        var anyFloors = false;

        foreach (var entity in World.Query<Floor, Position2D>())
        {
            var y = World.Get<Position2D>(entity).Y;
            if (y < Tuning.CeilingY - Tuning.FloorDespawnMargin)
            {
                despawnList.Add(entity);
            }
            else
            {
                anyFloors = true;
                lowestY = Math.Max(lowestY, y);
            }
        }

        foreach (var entity in despawnList)
        {
            World.Despawn(entity);
        }

        // Keep the shaft filled one floor beyond the bottom of the view. The
        // spacing is a mode knob: Daily Inferno packs the shaft denser.
        while (!anyFloors || lowestY < Tuning.ShaftHeight + settings.FloorSpacing)
        {
            var y = anyFloors ? lowestY + settings.FloorSpacing : Tuning.FirstFloorY;
            SpawnFloor(seed, scroll.NextFloorIndex, y);
            scroll.NextFloorIndex++;
            lowestY = y;
            anyFloors = true;
        }
    }

    private void SpawnFloor(ulong seed, int floorIndex, float y)
    {
        var (gapCenterX, gapWidth) = FloorLayout.GapForFloor(seed, floorIndex);

        World.Spawn()
            .With(new Floor
            {
                Index = floorIndex,
                GapCenterX = gapCenterX,
                GapWidth = gapWidth,
                Thickness = Tuning.FloorThickness,
                Kind = FloorLayout.KindForFloor(seed, floorIndex),
            })
            .With(new Position2D { X = 0f, Y = y })
            .With(new Transform3D(new Vector3(Tuning.ShaftWidth / 2f, y, 0f), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();
    }
}
