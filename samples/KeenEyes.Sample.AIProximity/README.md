# AI Proximity Detection Sample

Demonstrates AI sensory detection (vision and hearing) using spatial queries. Shows how guards can detect players within different sensory ranges and respond with state-based behavior.

## What This Sample Shows

- **Vision Detection**: Using `QueryRadius()` for line-of-sight detection
- **Hearing Detection**: Distance-based sound propagation with noise levels
- **Alert Broadcasting**: Guards alerting nearby guards when threats detected
- **AI State Machine**: Idle → Searching → Alert state transitions
- **Multi-Sensory AI**: Combining vision, hearing, and communication
- **Performance**: Efficient proximity queries for many AI agents

## Running the Sample

```bash
cd samples/KeenEyes.Sample.AIProximity
dotnet run
```

## Expected Output

The sample runs a simulation with 50 guards and 10 moving players:

```
=== AI Proximity Detection Sample ===

Simulating 50 guards and 10 players
World size: 500x500
Vision range: 50, Hearing: 100, Alert: 150
Running 200 frames...

....

Total time: 156ms
Average frame time: 0.78ms

Detection Summary:
  Vision detections: 342
  Hearing detections: 187
  Alert broadcasts: 89
  Guards in Alert state: 4
  Guards in Searching state: 12

Average per frame:
  Vision checks: 1.7
  Hearing checks: 0.9
```

## Key Observations

### Sensory Ranges

- **Vision (50 units)**: Short range, high confidence
  - ~1.7 vision detections per frame
  - Immediate transition to Alert state
  - Line-of-sight required

- **Hearing (100 units)**: Medium range, requires noise
  - ~0.9 hearing detections per frame
  - Transition to Searching state
  - Affected by player noise level

- **Alert (150 units)**: Long range communication
  - Guards broadcast to nearby guards
  - Cascading alert propagation
  - Coordinated response

### State Behavior

1. **Idle**: Normal patrol, checking for threats
2. **Searching**: Heard something, investigating for 3-5 seconds
3. **Alert**: Saw player, actively pursuing and broadcasting

At any given time:
- ~60-70% guards in Idle state
- ~20-30% guards in Searching state
- ~5-10% guards in Alert state

### Performance

- **0.78ms per frame** for 50 AI agents + 10 players
- Efficient Grid strategy for 2D proximity queries
- Minimal overhead for sensory checks

## How AI Proximity Detection Works

### 1. Vision Detection (Line-of-Sight)

```csharp
private bool CanSeePlayer(Vector3 guardPos, float visionRange)
{
    // Query nearby players within vision range
    foreach (var player in spatial.QueryRadius<Player>(guardPos, visionRange))
    {
        var playerPos = World.Get<Transform3D>(player).Position;

        // Exact distance check (sphere vs sphere)
        float distSq = Vector3.DistanceSquared(guardPos, playerPos);
        if (distSq <= visionRange * visionRange)
        {
            // In real game: also check for obstacles (raycasting)
            return true;
        }
    }
    return false;
}
```

### 2. Hearing Detection (Noise-Based)

```csharp
private bool CanHearPlayer(Vector3 guardPos, float hearingRange)
{
    // Query nearby entities within hearing range
    foreach (var player in spatial.QueryRadius<Player>(guardPos, hearingRange))
    {
        var noiseLevel = World.Get<Noisy>(player).NoiseLevel;  // 0.0 to 1.0

        // Effective hearing distance depends on noise level
        float effectiveRange = hearingRange * noiseLevel;
        float distSq = Vector3.DistanceSquared(guardPos, playerPos);

        if (distSq <= effectiveRange * effectiveRange)
        {
            return true;  // Heard the player
        }
    }
    return false;
}
```

### 3. Alert Broadcasting

```csharp
private void BroadcastAlert(Vector3 guardPos, float alertRange)
{
    // Alert nearby guards within alert range
    foreach (var otherGuard in spatial.QueryRadius<Guard>(guardPos, alertRange))
    {
        ref var guard = ref World.Get<Guard>(otherGuard);

        if (guard.State == GuardState.Idle)
        {
            // Nearby guard enters searching state
            guard.State = GuardState.Searching;
            guard.SearchTimer = 4.0f;
        }
    }
}
```

### 4. State Machine Update

```csharp
private void UpdateGuardState(ref Guard guard, Vector3 pos, float deltaTime)
{
    switch (guard.State)
    {
        case GuardState.Idle:
            if (CanSeePlayer(pos, guard.VisionRange))
            {
                guard.State = GuardState.Alert;
            }
            else if (CanHearPlayer(pos, guard.HearingRange))
            {
                guard.State = GuardState.Searching;
                guard.SearchTimer = 3.0f;
            }
            break;

        case GuardState.Searching:
            guard.SearchTimer -= deltaTime;
            if (guard.SearchTimer <= 0)
            {
                guard.State = GuardState.Idle;  // Give up search
            }
            else if (CanSeePlayer(pos, guard.VisionRange))
            {
                guard.State = GuardState.Alert;  // Found them!
            }
            break;

        case GuardState.Alert:
            if (!CanSeePlayer(pos, guard.VisionRange))
            {
                guard.State = GuardState.Searching;  // Lost sight
                guard.SearchTimer = 5.0f;
            }
            else
            {
                BroadcastAlert(pos, guard.AlertRange);  // Call for backup
            }
            break;
    }
}
```

## Code Structure

### Components

- `Transform3D` - Position, rotation, scale
- `Guard` - AI agent with vision/hearing/alert ranges and state
- `Velocity` - Movement velocity for players
- `Noisy` - Noise level (0.0 silent, 1.0 loud) affecting hearing detection
- `Player` - Tag component for player entities
- `SpatialIndexed` - Tag for spatial index inclusion

### Systems

- `PlayerMovementSystem` - Moves players with random direction changes
- `GuardAISystem` - Handles all guard AI logic (vision, hearing, state, alerts)
- `StatsReportSystem` - Tracks and reports detection statistics

### Guard State Machine

```
       Idle
        ↓ ↑
    (hear)|(lose sight, timeout)
        ↓ ↑
    Searching
        ↓ ↑
     (see)|(lose sight)
        ↓ ↑
       Alert
```

## Integration with Real Games

### Obstacle Occlusion (Raycasting)

Add line-of-sight checks with obstacles:

```csharp
private bool CanSeePlayer(Vector3 guardPos, Vector3 playerPos, float visionRange)
{
    float distSq = Vector3.DistanceSquared(guardPos, playerPos);
    if (distSq > visionRange * visionRange)
    {
        return false;
    }

    // Raycast to check for obstacles
    var dir = Vector3.Normalize(playerPos - guardPos);
    if (Physics.Raycast(guardPos, dir, out var hit, MathF.Sqrt(distSq)))
    {
        // Hit an obstacle before reaching player
        return hit.Entity == playerEntity;
    }

    return true;
}
```

### Field-of-View Cone

Restrict vision to a cone in front of guard:

```csharp
private bool CanSeePlayer(Transform3D guard, Vector3 playerPos, float visionRange, float fovDegrees)
{
    var toPlayer = playerPos - guard.Position;
    float distSq = toPlayer.LengthSquared();

    if (distSq > visionRange * visionRange)
    {
        return false;
    }

    // Check if player is within FOV cone
    var forward = guard.Forward();
    var dirToPlayer = Vector3.Normalize(toPlayer);
    float dot = Vector3.Dot(forward, dirToPlayer);
    float fovRadians = fovDegrees * MathF.PI / 180f;
    float minDot = MathF.Cos(fovRadians / 2f);

    return dot >= minDot;
}
```

### Sound Propagation with Walls

Reduce hearing range when walls are in the way:

```csharp
private bool CanHearPlayer(Vector3 guardPos, Vector3 playerPos, float hearingRange, float noiseLevel)
{
    float effectiveRange = hearingRange * noiseLevel;
    float distSq = Vector3.DistanceSquared(guardPos, playerPos);

    if (distSq > effectiveRange * effectiveRange)
    {
        return false;
    }

    // Count walls between guard and player
    int wallCount = CountWallsBetween(guardPos, playerPos);

    // Each wall reduces hearing range by 30%
    float wallAttenuation = MathF.Pow(0.7f, wallCount);
    effectiveRange *= wallAttenuation;

    return distSq <= effectiveRange * effectiveRange;
}
```

### Stealth Mechanics

Add crouch/sprint states affecting noise:

```csharp
[Component]
public partial struct Noisy
{
    public PlayerMovementState MovementState;

    public float NoiseLevel => MovementState switch
    {
        PlayerMovementState.Idle => 0.1f,      // Nearly silent
        PlayerMovementState.Crouching => 0.2f, // Very quiet
        PlayerMovementState.Walking => 0.5f,   // Moderate
        PlayerMovementState.Running => 1.0f,   // Loud
        _ => 0.5f
    };
}
```

### Sight Memory

Guards remember last known position:

```csharp
[Component]
public partial struct Guard
{
    // ... existing fields ...
    public Vector3 LastKnownPlayerPosition;
    public float TimeSinceLastSeen;
}

// In GuardAISystem:
if (CanSeePlayer(guardPos, out var playerPos))
{
    guard.LastKnownPlayerPosition = playerPos;
    guard.TimeSinceLastSeen = 0f;
    guard.State = GuardState.Alert;
}
else if (guard.State == GuardState.Alert)
{
    guard.TimeSinceLastSeen += deltaTime;

    if (guard.TimeSinceLastSeen < 5.0f)
    {
        // Move toward last known position
        MoveToward(guardPos, guard.LastKnownPlayerPosition);
    }
    else
    {
        // Give up search
        guard.State = GuardState.Idle;
    }
}
```

## Tuning for Your Game

### Sensory Ranges

```csharp
// Stealth game (shorter ranges, higher tension)
VisionRange = 30f;
HearingRange = 60f;
AlertRange = 80f;

// Action game (longer ranges, more aggressive)
VisionRange = 80f;
HearingRange = 150f;
AlertRange = 200f;

// Horror game (limited vision, enhanced hearing)
VisionRange = 20f;
HearingRange = 120f;
AlertRange = 100f;
```

### Grid Configuration

```csharp
// Cell size should accommodate largest sensory range
Grid = new GridConfig
{
    CellSize = MathF.Max(VisionRange, HearingRange) * 2f,
    WorldMin = sceneBounds.Min,
    WorldMax = sceneBounds.Max
}
```

### State Timers

```csharp
// Quick to alert, slow to calm down (tense gameplay)
SearchTimer = 10.0f;  // 10 seconds of searching
AlertCooldown = 5.0f;

// Quick to forget (forgiving stealth)
SearchTimer = 2.0f;
AlertCooldown = 1.0f;
```

## Performance Tips

### 1. Cache Query Results

Don't query every frame for every guard:

```csharp
private int framesSinceLastVisionCheck = 0;
private const int VisionCheckInterval = 5;  // Check every 5 frames

if (framesSinceLastVisionCheck >= VisionCheckInterval)
{
    cachedCanSeePlayer = CanSeePlayer(guardPos, visionRange);
    framesSinceLastVisionCheck = 0;
}
else
{
    framesSinceLastVisionCheck++;
}
```

### 2. Distance Culling

Don't update AI too far from player:

```csharp
var playerPos = GetNearestPlayerPosition();
float distToPlayer = Vector3.Distance(guardPos, playerPos);

if (distToPlayer > 500f)
{
    // Too far away, skip AI update
    continue;
}
```

### 3. Priority Sorting

Update closest guards first, far guards less frequently:

```csharp
var guardsWithDistance = guards
    .Select(g => (guard: g, dist: Vector3.Distance(g.Position, playerPos)))
    .OrderBy(x => x.dist)
    .ToList();

// Update closest 20 guards every frame
for (int i = 0; i < Math.Min(20, guardsWithDistance.Count); i++)
{
    UpdateGuard(guardsWithDistance[i].guard);
}

// Update remaining guards every 5 frames
if (frameCount % 5 == 0)
{
    for (int i = 20; i < guardsWithDistance.Count; i++)
    {
        UpdateGuard(guardsWithDistance[i].guard);
    }
}
```

## Common Scenarios

### Stealth Game

```csharp
// Guards patrol routes, player sneaks by
VisionRange = 40f;        // Limited vision
HearingRange = 80f;       // Enhanced hearing
AlertRange = 120f;        // Cooperative guards
CellSize = 160f;          // 2x hearing range
SearchTimer = 8.0f;       // Long search time
```

### Tower Defense

```csharp
// Towers detect enemies entering range
VisionRange = 150f;       // Long range detection
HearingRange = 0f;        // No hearing
AlertRange = 0f;          // No cooperation
CellSize = 300f;          // Large cells
```

### Survival Horror

```csharp
// Monsters hunt player by sound
VisionRange = 25f;        // Very limited vision
HearingRange = 200f;      // Excellent hearing
AlertRange = 300f;        // Call other monsters
NoiseLevel = 1.0f;        // Player is always loud
```

## Next Steps

- See [Performance Tuning Guide](../../docs/spatial-partitioning/performance-tuning.md)
- Add field-of-view cone for vision
- Implement obstacle occlusion with raycasting
- Add patrol routes for guards
- Implement stealth mechanics (crouch, cover)
- Add sight memory (last known position)

## Related Samples

- [Collision Detection](../KeenEyes.Sample.CollisionDetection) - Broadphase/narrowphase collision
- [Rendering Culling](../KeenEyes.Sample.RenderingCulling) - Frustum culling for 3D rendering
