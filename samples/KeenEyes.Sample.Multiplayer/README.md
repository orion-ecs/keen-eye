# KeenEyes Multiplayer Sample

This sample demonstrates the KeenEyes networking system with server-authoritative replication.

## Features Demonstrated

- **Server-Authoritative Networking**: Server owns game state and replicates to clients
- **Entity Replication**: Automatic synchronization of networked entities
- **Network ID Management**: Mapping between server and client entity IDs
- **Full Snapshots**: Late joiners receive complete world state
- **Entity Hierarchy**: Parent-child relationships replicated to clients
- **Delta Compression**: Bandwidth-efficient component updates

## Running the Sample

```bash
cd samples/KeenEyes.Sample.Multiplayer
dotnet run
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        SERVER                                │
│  - Owns authoritative game state                            │
│  - Processes client inputs                                  │
│  - Replicates entity changes to all clients                 │
└─────────────────────────────────────────────────────────────┘
              │                    │
              ▼                    ▼
┌─────────────────────┐  ┌─────────────────────┐
│      CLIENT 1       │  │      CLIENT 2       │
│  - Receives updates │  │  - Receives updates │
│  - Local prediction │  │  - Interpolation    │
│  - Sends inputs     │  │  - Sends inputs     │
└─────────────────────┘  └─────────────────────┘
```

## Code Structure

| File | Description |
|------|-------------|
| `Program.cs` | Main demo showing server/client setup and game loop |
| `Components.cs` | Networked components (Position, Velocity, PlayerInput) |
| `GameSerializer.cs` | Network serializer for component replication |

## Key Concepts

### 1. Server Setup

```csharp
var serverPlugin = new NetworkServerPlugin(transport, new ServerNetworkConfig
{
    TickRate = 60,
    MaxClients = 4,
    Serializer = new GameSerializer(),
});
world.InstallPlugin(serverPlugin);
```

### 2. Client Setup

```csharp
var clientPlugin = new NetworkClientPlugin(transport, new ClientNetworkConfig
{
    ServerAddress = "localhost",
    ServerPort = 7777,
    EnablePrediction = true,
    Serializer = new GameSerializer(),
});
world.InstallPlugin(clientPlugin);
```

### 3. Registering Networked Entities

```csharp
// Create entity with components
var player = world.Spawn()
    .WithPosition(x: 100, y: 100)
    .WithVelocity(x: 0, y: 0)
    .Build();

// Register for network replication (assigns NetworkId)
var networkId = serverPlugin.RegisterNetworkedEntity(player, ownerId: clientId);
```

### 4. Delta Compression

Components implementing `INetworkDeltaSerializable<T>` only send changed fields:

```csharp
public uint GetDirtyMask(in Position baseline)
{
    uint mask = 0;
    if (MathF.Abs(X - baseline.X) > 0.001f) mask |= 1;
    if (MathF.Abs(Y - baseline.Y) > 0.001f) mask |= 2;
    return mask;
}
```

### 5. Late Joiner Support

New clients automatically receive the full world state:

```csharp
serverPlugin.SendFullSnapshot(clientId);
```

## Transport Layer

This sample uses `LocalTransport` for in-process testing. For production networking, KeenEyes provides optional transport packages:

| Package | Transport | Use Case |
|---------|-----------|----------|
| `KeenEyes.Network.Transport.Tcp` | `TcpTransport` | Reliable ordered delivery, NAT-friendly |
| `KeenEyes.Network.Transport.Udp` | `UdpTransport` | Low-latency, configurable reliability |

Example with `TcpTransport`:
```csharp
using KeenEyes.Network.Transport.Tcp;

// Server
var serverTransport = new TcpTransport();
await serverTransport.ListenAsync(7777);

// Client
var clientTransport = new TcpTransport();
await clientTransport.ConnectAsync("game.example.com", 7777);
```

Or bring your own transport by implementing `INetworkTransport` (Steam, LiteNetLib, etc.).

## See Also

- [Networking Guide](/docs/networking.md) - Full documentation
- [Plugin System](/docs/plugins.md) - How plugins work
