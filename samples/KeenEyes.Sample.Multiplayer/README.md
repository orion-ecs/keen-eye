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

The sample supports three transport modes: Local, TCP, and UDP.

### Local Mode (Default)

Runs both server and client in a single process for quick testing:

```bash
cd samples/KeenEyes.Sample.Multiplayer
dotnet run
```

### TCP Mode

For real network testing, run server and client in separate terminals:

```bash
# Terminal 1: Start server
dotnet run -- --transport tcp --server

# Terminal 2: Start client
dotnet run -- --transport tcp --client
```

### UDP Mode

Same pattern as TCP, with configurable reliability:

```bash
# Terminal 1: Start server
dotnet run -- --transport udp --server --port 8888

# Terminal 2: Start client
dotnet run -- --transport udp --client --port 8888
```

### Command-Line Options

| Option | Short | Description |
|--------|-------|-------------|
| `--transport` | `-t` | Transport type: `local`, `tcp`, `udp` |
| `--server` | `-s` | Run as server |
| `--client` | `-c` | Run as client |
| `--address` | `-a` | Server address (default: `localhost`) |
| `--port` | `-p` | Port number (default: `7777`) |
| `--help` | `-h` | Show help |

### Examples

```bash
# Connect to remote server
dotnet run -- -t tcp -c -a 192.168.1.100 -p 7777

# UDP server on custom port
dotnet run -- -t udp -s -p 9999
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
| `Program.cs` | Main demo with Local, TCP, and UDP modes |
| `TransportOptions.cs` | Command-line argument parsing and transport factory |
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

This sample demonstrates all built-in transports. Use `--help` to see available options.

| Transport | Package | Use Case |
|-----------|---------|----------|
| `LocalTransport` | `KeenEyes.Network` | In-process testing, unit tests |
| `TcpTransport` | `KeenEyes.Network.Transport.Tcp` | Reliable ordered delivery, NAT-friendly |
| `UdpTransport` | `KeenEyes.Network.Transport.Udp` | Low-latency, configurable reliability |

### Choosing a Transport

- **Local**: Quick testing without network overhead. Great for development.
- **TCP**: Simple and reliable. Works through NAT/firewalls. Good for turn-based or slower-paced games.
- **UDP**: Lower latency, but requires reliability handling. Best for fast-paced games.

### Bring Your Own Transport

Implement `INetworkTransport` to integrate third-party networking libraries:

```csharp
using KeenEyes.Network.Transport;

public class SteamTransport : INetworkTransport
{
    // Implement interface using Steamworks.NET
}
```

Popular options:
- **Steam Networking** - Steamworks.NET for Steam games
- **LiteNetLib** - Lightweight reliable UDP
- **ENet** - Fast, reliable UDP with channels

## See Also

- [Networking Guide](/docs/networking.md) - Full documentation
- [Plugin System](/docs/plugins.md) - How plugins work
