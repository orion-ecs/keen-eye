using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.AIProximity;

/// <summary>
/// Stats for tracking AI detection performance.
/// </summary>
public class DetectionStats
{
    /// <summary>Total number of vision-based detections across the run.</summary>
    public int TotalVisionDetections;

    /// <summary>Total number of hearing-based detections across the run.</summary>
    public int TotalHearingDetections;

    /// <summary>Total number of alert broadcasts between guards.</summary>
    public int TotalAlertBroadcasts;

    /// <summary>Number of guards currently in the Alert state.</summary>
    public int GuardsInAlertState;

    /// <summary>Number of guards currently in the Searching state.</summary>
    public int GuardsInSearchingState;
}

/// <summary>
/// System that moves players around the world.
/// </summary>
public class PlayerMovementSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>().With<Player>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref var velocity = ref World.Get<Velocity>(entity);

            // Move player
            transform.Position += velocity.Value * deltaTime;

            // Wrap around world bounds (toroidal world)
            const float halfSize = Program.WorldSize / 2;
            if (transform.Position.X < -halfSize)
            {
                transform.Position.X += Program.WorldSize;
            }

            if (transform.Position.X > halfSize)
            {
                transform.Position.X -= Program.WorldSize;
            }

            if (transform.Position.Z < -halfSize)
            {
                transform.Position.Z += Program.WorldSize;
            }

            if (transform.Position.Z > halfSize)
            {
                transform.Position.Z -= Program.WorldSize;
            }

            // Randomly change direction occasionally
            // Use World's seeded RNG for deterministic replay support
            if (World.NextFloat() < 0.02f)
            {
                velocity.Value = new Vector3(
                    World.NextFloat() * 20f - 10f,
                    0,
                    World.NextFloat() * 20f - 10f);
            }
        }
    }
}

/// <summary>
/// System that handles guard AI sensory detection and state management.
/// </summary>
public class GuardAISystem(DetectionStats stats) : SystemBase
{
    private SpatialQueryApi? spatial;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Reset per-frame stats
        stats.GuardsInAlertState = 0;
        stats.GuardsInSearchingState = 0;

        foreach (var guardEntity in World.Query<Transform3D, Guard>())
        {
            ref readonly var guardTransform = ref World.Get<Transform3D>(guardEntity);
            ref var guard = ref World.Get<Guard>(guardEntity);

            // Update guard state based on sensory input
            UpdateGuardState(guardEntity, ref guard, guardTransform, deltaTime);

            // Count guards by state
            if (guard.State == GuardState.Alert)
            {
                stats.GuardsInAlertState++;
            }
            else if (guard.State == GuardState.Searching)
            {
                stats.GuardsInSearchingState++;
            }
        }
    }

    private void UpdateGuardState(Entity guardEntity, ref Guard guard, Transform3D guardTransform, float deltaTime)
    {
        switch (guard.State)
        {
            case GuardState.Idle:
                CheckForThreats(ref guard, guardTransform);
                break;

            case GuardState.Searching:
                guard.SearchTimer -= deltaTime;
                if (guard.SearchTimer <= 0)
                {
                    guard.State = GuardState.Idle;
                }
                else
                {
                    // While searching, still check for visual confirmation
                    CheckForThreats(ref guard, guardTransform);
                }
                break;

            case GuardState.Alert:
                // In alert state, check if player is still visible
                if (!CanSeePlayer(guardTransform, guard.VisionRange))
                {
                    // Lost sight, return to searching
                    guard.State = GuardState.Searching;
                    guard.SearchTimer = 5.0f;
                }
                else
                {
                    // Broadcast alert to nearby guards
                    BroadcastAlert(guardEntity, guardTransform, guard.AlertRange);
                }
                break;
        }
    }

    private void CheckForThreats(ref Guard guard, Transform3D guardTransform)
    {
        // Vision check (can see players)
        if (CanSeePlayer(guardTransform, guard.VisionRange))
        {
            guard.State = GuardState.Alert;
            stats.TotalVisionDetections++;
            return;
        }

        // Hearing check (can hear noisy players further away)
        if (CanHearPlayer(guardTransform, guard.HearingRange) && guard.State == GuardState.Idle)
        {
            guard.State = GuardState.Searching;
            guard.SearchTimer = 3.0f;
            stats.TotalHearingDetections++;
        }
    }

    private bool CanSeePlayer(Transform3D guardTransform, float visionRange)
    {
        // Query nearby entities within vision range
        foreach (var entity in spatial!.QueryRadius<Player>(guardTransform.Position, visionRange))
        {
            ref readonly var playerTransform = ref World.Get<Transform3D>(entity);

            // Line-of-sight check (simplified - no obstacles)
            float distSq = Vector3.DistanceSquared(guardTransform.Position, playerTransform.Position);
            if (distSq <= visionRange * visionRange)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanHearPlayer(Transform3D guardTransform, float hearingRange)
    {
        // Query nearby entities within hearing range
        foreach (var entity in spatial!.QueryRadius<Player>(guardTransform.Position, hearingRange))
        {
            if (!World.Has<Noisy>(entity))
            {
                continue;
            }

            ref readonly var playerTransform = ref World.Get<Transform3D>(entity);
            ref readonly var noisy = ref World.Get<Noisy>(entity);

            // Hearing distance affected by noise level
            float effectiveRange = hearingRange * noisy.NoiseLevel;
            float distSq = Vector3.DistanceSquared(guardTransform.Position, playerTransform.Position);

            if (distSq <= effectiveRange * effectiveRange)
            {
                return true;
            }
        }

        return false;
    }

    private void BroadcastAlert(Entity sourceGuard, Transform3D sourceTransform, float alertRange)
    {
        // Alert nearby guards (they join the alert state)
        foreach (var otherGuard in spatial!.QueryRadius<Guard>(sourceTransform.Position, alertRange))
        {
            if (otherGuard == sourceGuard)
            {
                continue;
            }

            ref var guard = ref World.Get<Guard>(otherGuard);
            if (guard.State == GuardState.Idle)
            {
                guard.State = GuardState.Searching;
                guard.SearchTimer = 4.0f;
                stats.TotalAlertBroadcasts++;
            }
        }
    }
}
