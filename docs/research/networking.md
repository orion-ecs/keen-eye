# Network Synchronization - Research Report

**Date:** December 2024
**Purpose:** Research networking approaches for implementing multiplayer support in KeenEyes ECS, evaluating entity replication patterns, synchronization strategies, and transport abstractions.

## Executive Summary

KeenEyes has excellent infrastructure for networking already in place. The **delta snapshot system**, **change tracking**, **entity versioning**, and **plugin architecture** provide a solid foundation. The recommended approach is a **server-authoritative state synchronization** model with **client-side prediction**, implemented as a modular plugin with transport abstraction.

Key findings:
- KeenEyes' existing `DeltaSnapshot` system is ideal for network delta compression
- The `ChangeTracker` enables efficient dirty-flag based sync
- `WorldEntityRef` already supports cross-world entity references (client→server mapping)
- Modern ECS networking (Unity Netcode, Bevy Replicon) converges on similar patterns

---

## Existing KeenEyes Infrastructure

### Ready-to-Use Features

| Feature | Location | Networking Application |
|---------|----------|----------------------|
| **Delta Snapshots** | `DeltaSnapshot.cs`, `DeltaDiffer.cs` | Send only changed state each tick |
| **Change Tracking** | `ChangeTracker.cs` | Dirty flags for selective sync |
| **Entity Versioning** | `Entity.cs` (Id + Version) | Staleness detection |
| **WorldEntityRef** | `WorldEntityRef.cs` | Client→Server entity mapping |
| **Component Serialization** | `IComponentSerializer.cs` | AOT-compatible serialization |
| **Plugin System** | `IWorldPlugin.cs` | Modular networking plugin |
| **System Phases** | `SystemPhase.cs` | EarlyUpdate (recv), LateUpdate (send) |
| **System Hooks** | `SystemHookManager.cs` | Network profiling/logging |
| **Binary Serialization** | `BinarySnapshotCodec.cs` | Bandwidth-efficient encoding |

### Client-Server Test Infrastructure

The codebase includes `ClientServerIntegrationTests.cs` demonstrating:
- Multiple isolated worlds (server + clients)
- Server-authoritative updates
- Entity staleness detection via version numbers
- Cross-world entity references

This validates the architecture is suitable for networking.

---

## Networking Approaches Comparison

### 1. Deterministic Lockstep

**How it works:** All clients execute the same inputs in the same order. Only inputs are transmitted.

| Pros | Cons |
|------|------|
| Minimal bandwidth (inputs only) | Requires perfect determinism |
| No desync if determinism holds | Floating-point differences break it |
| Works well for RTS games | High latency (wait for all inputs) |
| Replays are trivial (replay inputs) | Late joiners must replay from start |

**When to use:** Turn-based games, RTS with low entity counts, fighting games.

**Not recommended for KeenEyes** due to .NET floating-point non-determinism across platforms.

### 2. Snapshot Interpolation

**How it works:** Server sends full world snapshots; clients interpolate between received states.

| Pros | Cons |
|------|------|
| Simple implementation | High bandwidth |
| Naturally handles late joiners | Input latency (no prediction) |
| Server is always authoritative | Sluggish player controls |

**When to use:** Spectator modes, slow-paced games, debugging.

### 3. State Synchronization with Prediction (Recommended)

**How it works:** Server sends state updates; clients predict local state and reconcile with server.

| Pros | Cons |
|------|------|
| Responsive controls (prediction) | More complex implementation |
| Efficient bandwidth (delta encoding) | Requires rollback/reconciliation |
| Handles packet loss gracefully | Prediction errors cause "pops" |
| Industry standard approach | Need interpolation smoothing |

**When to use:** Action games, FPS, third-person games, most multiplayer scenarios.

**Recommended for KeenEyes** - matches existing infrastructure and industry best practices.

---

## Reference Implementations

### Unity Netcode for Entities

Unity's official ECS networking solution provides valuable patterns:

**Ghost System:**
- "Ghosts" are networked entities owned by server
- `[GhostField]` attribute marks replicated fields
- Automatic delta compression via change bitmasks
- Per-field quantization (floats → integers)

**Synchronization Features:**
- Interpolation/extrapolation options per component
- Partial snapshots when exceeding MTU
- Priority system for important entities
- Composite flag controls change detection granularity

**Prediction System:**
- Client-side prediction for owned entities
- `GhostPredictionSmoothingSystem` handles reconciliation
- Per-ghost, per-client prediction opt-in

**Key Insight:** Unity generates serialization code via source generators, similar to KeenEyes' approach.

### Bevy Replicon (Rust)

Server-authoritative replication for Bevy ECS:

**Architecture:**
- Transport-agnostic (works with renet, quinnet, etc.)
- Same game logic for singleplayer/client/server
- Automatic world replication
- ECS relationships for replication grouping

**Features:**
- Custom serialization support
- Visibility control (what clients see)
- Authorization for access management
- Remote events/triggers

**Key Insight:** Transport abstraction is critical - don't couple to a specific protocol.

### Source Engine (Valve)

Battle-tested networking from CS:GO, L4D2:

**Entity Interpolation:**
- Clients render 100ms in the past
- Smooth interpolation between snapshots
- Eliminates jitter from network variance

**Input Prediction:**
- Client runs same code as server
- Immediate feedback for player actions
- Smooth correction on misprediction

**Lag Compensation:**
- Server maintains 1-second position history
- Rewinds other players to command time
- Fair hit detection despite latency

**Tick Rates:**
- Discrete simulation steps (64-128 ticks/sec typical)
- Consistent physics across all clients

### Glenn Fiedler's Patterns

Industry-standard techniques from networking expert:

**Priority Accumulator:**
- Not all entities updated every frame
- Priority accumulates over time
- Bandwidth distributed intelligently

**Jitter Buffer:**
- Hold packets briefly (4-5 frames)
- Deliver at consistent intervals
- Prevents extrapolation divergence

**Quantization on Both Sides:**
- Quantize state identically on client/server
- Prevents "pops" when updates arrive
- Client predicts from quantized state

**Visual Smoothing via Error Offsets:**
- Don't smooth simulation state
- Maintain position/rotation error offsets
- Gradually reduce offsets each frame
- Adaptive factors (small vs large errors)

**Delta Compression:**
- Encode relative to acknowledged baseline
- "Smallest three" for quaternions (128→29 bits)
- Bound and quantize velocities
- Position quantization (512 values/meter)

---

## Recommended Architecture

### Network Plugin Structure

```
KeenEyes.Network/
├── Transport/
│   ├── INetworkTransport.cs        # Transport abstraction
│   ├── UdpTransport.cs             # UDP with reliability layer
│   ├── WebSocketTransport.cs       # Browser-compatible
│   └── LocalTransport.cs           # Testing/singleplayer
├── Protocol/
│   ├── IReliabilityLayer.cs        # Packet reliability
│   ├── SequenceBuffer.cs           # Packet ordering
│   └── AcknowledgmentTracker.cs    # ACK tracking for delta baseline
├── Replication/
│   ├── ReplicationManager.cs       # Entity lifecycle sync
│   ├── NetworkIdMap.cs             # Local→Network ID mapping
│   ├── OwnershipTracker.cs         # Authority tracking
│   └── VisibilityManager.cs        # Per-client visibility
├── Synchronization/
│   ├── ComponentSyncStrategy.cs    # Base strategy
│   ├── InterpolatedSync.cs         # Smooth remote entities
│   ├── PredictedSync.cs            # Client-predicted entities
│   └── QuantizationHelpers.cs      # Float→int encoding
├── Prediction/
│   ├── InputBuffer.cs              # Client input history
│   ├── StateBuffer.cs              # Predicted state history
│   ├── PredictionManager.cs        # Rollback/replay
│   └── ReconciliationSystem.cs     # Server correction
├── Systems/
│   ├── NetworkReceiveSystem.cs     # EarlyUpdate phase
│   ├── NetworkSendSystem.cs        # LateUpdate phase
│   ├── InterpolationSystem.cs      # Update phase
│   └── PredictionSystem.cs         # Update phase
├── Components/
│   ├── NetworkEntity.cs            # Network ID + ownership
│   ├── Interpolated.cs             # Tag for interpolation
│   ├── Predicted.cs                # Tag for prediction
│   └── NetworkOwner.cs             # Authority info
└── NetworkPlugin.cs                # Plugin entry point
```

### Source Generator Extensions

```csharp
[Component]
[Replicated]  // Generate network serialization
public partial struct Position
{
    [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
    public float X;

    [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
    public float Y;
}

[Component]
[Replicated(Interpolated = true)]  // Generate interpolation helpers
public partial struct Rotation
{
    [SmallestThree]  // Use quaternion compression
    public Quaternion Value;
}

[Component]
[Replicated(Predicted = true)]  // Generate prediction/rollback
public partial struct Velocity
{
    public float X;
    public float Y;
}
```

Generated code would include:
- `Serialize(ref BitWriter writer)` - Quantized binary encoding
- `Deserialize(ref BitReader reader)` - Quantized binary decoding
- `Interpolate(in T from, in T to, float t)` - Lerp helper
- `GetDeltaBits(in T baseline)` - Delta encoding
- `ApplyDelta(in T baseline, BitReader reader)` - Delta decoding

### System Phase Layout

```
┌─────────────────────────────────────────────────────────────┐
│                      Frame Start                            │
├─────────────────────────────────────────────────────────────┤
│  EarlyUpdate Phase                                          │
│  ├─ NetworkReceiveSystem     ← Process incoming packets     │
│  ├─ ReconciliationSystem     ← Apply server corrections     │
│  └─ EntityReplicationSystem  ← Create/destroy entities      │
├─────────────────────────────────────────────────────────────┤
│  FixedUpdate Phase                                          │
│  ├─ InputSystem              ← Sample local inputs          │
│  ├─ PredictionSystem         ← Predict local entities       │
│  └─ PhysicsSystem            ← Simulate physics             │
├─────────────────────────────────────────────────────────────┤
│  Update Phase                                               │
│  ├─ InterpolationSystem      ← Interpolate remote entities  │
│  └─ GameLogicSystems         ← Game-specific logic          │
├─────────────────────────────────────────────────────────────┤
│  LateUpdate Phase                                           │
│  ├─ NetworkSendSystem        ← Send state updates           │
│  └─ BandwidthMonitorSystem   ← Track network usage          │
├─────────────────────────────────────────────────────────────┤
│                       Frame End                             │
└─────────────────────────────────────────────────────────────┘
```

### Leveraging Existing Infrastructure

**Delta Snapshots → Network Delta Encoding:**
```csharp
// Server: Generate delta from last acknowledged state
var baseline = clientState.LastAcknowledgedSnapshot;
var current = world.CreateSnapshot();
var delta = DeltaDiffer.CreateDelta(baseline, current);

// Only send if there are changes
if (!delta.IsEmpty)
{
    var bytes = BinaryDeltaCodec.Encode(delta);
    transport.Send(clientId, bytes);
}
```

**Change Tracking → Selective Sync:**
```csharp
// Only iterate entities with dirty Position components
foreach (var entity in world.GetDirtyEntities<Position>())
{
    ref readonly var pos = ref world.Get<Position>(entity);
    // Add to outgoing packet
}
world.ClearDirtyFlags<Position>();
```

**WorldEntityRef → Client-Server Mapping:**
```csharp
// Client stores reference to server entity
[Component]
public partial struct ServerEntityRef
{
    public WorldEntityRef Ref;  // Points to server world entity
}

// On receiving server update
var localEntity = networkIdMap.GetLocalEntity(serverNetworkId);
if (serverEntityRef.Ref.TryResolve(serverWorld, out var serverEntity))
{
    // Update local entity from server state
}
```

---

## Synchronization Strategies

### Strategy 1: Interpolated (Remote Entities)

For entities controlled by other players or server:

```
Server State:  S0 ────────── S1 ────────── S2 ────────── S3
                 \            \            \
Client Render:    └─ lerp ─────┴─ lerp ─────┴─ lerp ─────→
                 (100ms behind server)
```

**Implementation:**
- Buffer 2-3 snapshots
- Render at `serverTime - interpolationDelay`
- Lerp between surrounding snapshots
- Extrapolate briefly if packet lost

### Strategy 2: Predicted (Local Player)

For the entity controlled by local player:

```
Client Input:    I0 ─── I1 ─── I2 ─── I3 ─── I4
                  ↓      ↓      ↓      ↓      ↓
Client State:    P0 ─── P1 ─── P2 ─── P3 ─── P4  (predicted)
                                       ↓
Server Confirm:  ─────────────────── S3 ─────────  (authoritative)
                                       ↓
Reconcile:                            P3'         (corrected)
```

**Implementation:**
1. Buffer inputs with sequence numbers
2. Predict state locally using buffered inputs
3. When server state arrives, compare with predicted
4. If mismatch: rollback to server state, replay inputs

### Strategy 3: Authoritative (Server-Only)

For entities only the server controls (NPCs, world state):

```
Server:    Calculate ─→ Broadcast
Client:    Receive ─→ Apply (no prediction)
```

**Implementation:**
- No client prediction
- Direct state application
- Optional interpolation for smoothness

---

## Transport Abstraction

### Interface Design

```csharp
public interface INetworkTransport : IDisposable
{
    /// <summary>Current connection state.</summary>
    ConnectionState State { get; }

    /// <summary>Event raised when connection state changes.</summary>
    event Action<ConnectionState>? StateChanged;

    /// <summary>Event raised when data is received.</summary>
    event Action<int, ReadOnlySpan<byte>>? DataReceived;

    /// <summary>Connects to a remote endpoint (client).</summary>
    Task ConnectAsync(string address, int port, CancellationToken ct = default);

    /// <summary>Starts listening for connections (server).</summary>
    Task ListenAsync(int port, CancellationToken ct = default);

    /// <summary>Sends data to a specific client (server) or the server (client).</summary>
    void Send(int connectionId, ReadOnlySpan<byte> data, DeliveryMode mode);

    /// <summary>Disconnects a specific client (server) or from server (client).</summary>
    void Disconnect(int connectionId);

    /// <summary>Processes incoming/outgoing data. Call once per frame.</summary>
    void Update();
}

public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting }

public enum DeliveryMode
{
    Unreliable,           // Fire and forget (position updates)
    UnreliableSequenced,  // Drop old packets (input)
    ReliableUnordered,    // Guaranteed delivery (events)
    ReliableOrdered       // Guaranteed delivery + order (chat, RPC)
}
```

### Transport Options

| Transport | Use Case | Latency | Reliability |
|-----------|----------|---------|-------------|
| **UDP + Custom Reliability** | Desktop games | Lowest | Custom |
| **WebSocket** | Browser games | Medium | TCP-based |
| **WebRTC** | Browser P2P | Low | Custom |
| **Steam Networking** | Steam games | Low | Built-in |

---

## Bandwidth Optimization

### Quantization Guidelines

| Data Type | Raw Size | Quantized | Technique |
|-----------|----------|-----------|-----------|
| Position (3D) | 96 bits | 48-60 bits | Bounded range, 512/meter |
| Rotation (Quat) | 128 bits | 29 bits | Smallest three |
| Velocity | 96 bits | 33 bits | Bounded, 11 bits/axis |
| Boolean | 8 bits | 1 bit | Bit packing |
| Enum (8 values) | 32 bits | 3 bits | Bit packing |

### Delta Compression Flow

```
Frame N:
  1. Server creates snapshot
  2. Find baseline (client's last ACK'd snapshot)
  3. Diff current vs baseline
  4. Encode only changed components
  5. Send delta + sequence number

Frame N+1:
  1. Receive client ACK for sequence
  2. Update baseline to ACK'd snapshot
  3. Next delta is relative to new baseline
```

### Priority System

Not all entities need updates every frame:

```csharp
public class PriorityAccumulator
{
    private readonly Dictionary<Entity, float> priorities = new();

    public void Update(Entity entity, float basePriority, float distance)
    {
        // Priority increases over time since last update
        // Closer entities have higher priority
        // Important entities (players) have higher base priority
        var priority = basePriority / (1 + distance * 0.1f);
        priorities[entity] = priorities.GetValueOrDefault(entity) + priority;
    }

    public IEnumerable<Entity> GetTopPriority(int count)
    {
        return priorities
            .OrderByDescending(p => p.Value)
            .Take(count)
            .Select(p => p.Key);
    }

    public void MarkSent(Entity entity) => priorities[entity] = 0;
}
```

---

## Error Handling & Edge Cases

### Packet Loss

**Unreliable packets (state updates):**
- Use sequence numbers to detect gaps
- Extrapolate briefly from last known state
- Next packet will correct

**Reliable packets (events):**
- Automatic retransmission
- Idempotent event handlers

### Late Joiners

1. Server sends full snapshot (not delta)
2. Client creates all entities
3. Subsequent frames use delta encoding

### Entity Lifecycle

**Server creates entity:**
1. Assign network ID
2. Include in next snapshot with full component data
3. Client creates local entity, maps network ID

**Server destroys entity:**
1. Include destruction in delta
2. Client despawns local entity
3. Clean up network ID mapping

### Ownership Transfer

```csharp
// Server transfers ownership
public void TransferOwnership(Entity entity, int newOwnerClientId)
{
    ref var owner = ref world.Get<NetworkOwner>(entity);
    owner.ClientId = newOwnerClientId;

    // Force full sync to new owner
    replicationManager.ForceFullSync(entity, newOwnerClientId);

    // Notify old owner to stop predicting
    SendOwnershipLost(owner.ClientId, entity);
}
```

---

## Implementation Phases

### Phase 1: Foundation
- [ ] Transport abstraction interface
- [ ] Local transport for testing
- [ ] Basic packet serialization
- [ ] Connection management

### Phase 2: Entity Replication
- [ ] Network ID assignment
- [ ] Entity creation/destruction sync
- [ ] Full snapshot encoding/decoding
- [ ] Basic component serialization

### Phase 3: Delta Compression
- [ ] Integrate with existing DeltaSnapshot
- [ ] Acknowledgment tracking
- [ ] Baseline management
- [ ] Delta encoding/decoding

### Phase 4: Interpolation
- [ ] Snapshot buffer
- [ ] Interpolation system
- [ ] Jitter buffer
- [ ] Extrapolation fallback

### Phase 5: Prediction
- [ ] Input buffering
- [ ] State prediction
- [ ] Server reconciliation
- [ ] Rollback/replay

### Phase 6: Optimization
- [ ] Priority accumulator
- [ ] Quantization helpers
- [ ] Bandwidth monitoring
- [ ] Adaptive send rates

### Phase 7: Source Generators
- [ ] `[Replicated]` attribute
- [ ] Serialization generation
- [ ] Interpolation generation
- [ ] Prediction generation

---

## Testing Strategy

### Unit Tests
- Serialization round-trips
- Delta encoding correctness
- Sequence number handling
- Priority accumulator behavior

### Integration Tests
- Multi-world client/server scenarios
- Entity lifecycle sync
- Prediction accuracy
- Reconciliation correctness

### Simulation Tests
- Artificial latency injection
- Packet loss simulation
- Jitter simulation
- Bandwidth limits

### Sample Project
- Simple multiplayer demo
- Position sync with interpolation
- Player-controlled prediction
- Chat system (reliable messaging)

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Floating-point non-determinism | Medium | Use state sync, not lockstep |
| Bandwidth overhead | Medium | Delta compression, quantization |
| Prediction errors | Low | Smooth correction, visual offsets |
| Transport compatibility | Low | Abstraction layer |
| AOT compatibility | Low | Existing serialization patterns |

---

## Recommendations

### Primary Approach: State Synchronization

Implement **server-authoritative state synchronization** with:
1. Delta compression using existing `DeltaSnapshot` infrastructure
2. Client-side prediction for responsive controls
3. Interpolation for smooth remote entity rendering
4. Transport abstraction for flexibility

### Implementation Priority

1. **Start with full snapshots** - Get basic sync working
2. **Add delta compression** - Leverage existing infrastructure
3. **Add interpolation** - Smooth remote entities
4. **Add prediction** - Responsive local player
5. **Optimize** - Quantization, priority, bandwidth

### Package Structure

```
KeenEyes.Network           # Core networking (transport-agnostic)
KeenEyes.Network.Udp       # UDP transport implementation
KeenEyes.Network.WebSocket # WebSocket transport
```

### Source Generator Integration

Extend existing generator infrastructure:
- `[Replicated]` attribute for network serialization
- Generate quantized serializers
- Generate interpolation helpers
- Generate prediction/rollback code

---

## Sources

### Industry Resources
- [Glenn Fiedler - State Synchronization](https://gafferongames.com/post/state_synchronization/) - Priority accumulators, jitter buffers
- [Glenn Fiedler - Snapshot Compression](https://gafferongames.com/post/snapshot_compression/) - Delta encoding, quantization
- [Glenn Fiedler - Networked Physics](https://gafferongames.com/post/introduction_to_networked_physics/) - Authority models
- [Valve - Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking) - Interpolation, prediction, lag compensation

### ECS Framework Networking
- [Unity Netcode for Entities - Ghost Snapshots](https://docs.unity3d.com/Packages/com.unity.netcode@1.3/manual/ghost-snapshots.html)
- [Unity Netcode for Entities - Prediction](https://docs.unity3d.com/Packages/com.unity.netcode@1.3/manual/prediction.html)
- [Bevy Replicon](https://github.com/lifescapegame/bevy_replicon) - Server-authoritative ECS replication
- [Renet](https://github.com/lucaspoffo/renet) - Rust game networking library

### KeenEyes Infrastructure
- `src/KeenEyes.Core/Serialization/DeltaSnapshot.cs` - Existing delta system
- `src/KeenEyes.Core/Events/ChangeTracker.cs` - Dirty flag tracking
- `src/KeenEyes.Abstractions/WorldEntityRef.cs` - Cross-world references
- `tests/KeenEyes.Core.Tests/ClientServerIntegrationTests.cs` - Multi-world tests
