# Navigation & Pathfinding

The `KeenEyes.Navigation` library provides pathfinding, path following, and dynamic obstacle handling for the ECS world. It defines a pluggable `INavigationProvider` abstraction, with concrete implementations shipped in `KeenEyes.Navigation.Grid` (2D A* pathfinding) and `KeenEyes.Navigation.DotRecast` (3D navigation mesh pathfinding via DotRecast).

## Overview

Navigation is split across four packages:

| Package | Purpose |
|---------|---------|
| `KeenEyes.Navigation.Abstractions` | Core interfaces and types: `INavigationProvider`, `IPathRequest`, `NavPath`, `NavPoint`, `AgentSettings`, `NavAreaMask`, and the `NavMeshAgent`/`NavMeshObstacle`/`NavMeshSurface`/`NavMeshModifier` components |
| `KeenEyes.Navigation` | `NavigationPlugin` - the orchestration layer that runs pathfinding requests and moves agents each frame |
| `KeenEyes.Navigation.Grid` | `GridNavigationPlugin` - a grid-based `INavigationProvider` implementation using A* |
| `KeenEyes.Navigation.DotRecast` | `DotRecastNavigationPlugin` - a navmesh-based `INavigationProvider` implementation using DotRecast (a C# port of Recast/Detour) |

`NavigationPlugin` itself does not compute paths. It delegates to whichever `INavigationProvider` is registered in the world, so you must install a provider plugin (`GridNavigationPlugin` or `DotRecastNavigationPlugin`) before or alongside `NavigationPlugin`, or supply a custom provider via `NavigationConfig.CustomProvider`.

## Quick Start

### Installation

Install a navigation provider plugin, then `NavigationPlugin` on top of it:

```csharp
using KeenEyes.Navigation;
using KeenEyes.Navigation.Grid;

using var world = new World();

// Install a provider first - grid-based A* for this example
world.InstallPlugin(new GridNavigationPlugin(new GridConfig
{
    Width = 100,
    Height = 100,
    CellSize = 1f,
    AllowDiagonal = true
}));

// Install the navigation orchestration plugin
world.InstallPlugin(new NavigationPlugin());
```

`NavigationPlugin.Install` registers the `NavMeshAgent` and `NavMeshObstacle` components and adds these systems:

- `PathRequestSystem` - `SystemPhase.Update`, order `-100`. Drives `INavigationProvider.Update` and resolves completed path requests.
- `NavMeshAgentSystem` - `SystemPhase.Update`, order `0`. Moves agents along their computed paths. Only registered when `NavigationConfig.AgentSteeringEnabled` is `true` (the default).
- `ObstacleUpdateSystem` - `SystemPhase.LateUpdate`, order `0`. Tracks `NavMeshObstacle` movement and marks navigation data dirty for carving updates. Only registered when `NavigationConfig.DynamicObstaclesEnabled` is `true` (the default).

The plugin also exposes a `NavigationContext` extension (annotated `[PluginExtension("Navigation")]`), reachable as `world.GetExtension<NavigationContext>()` or the generated `world.Navigation` property.

### Your First Navigating Agent

```csharp
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

// Spawn an entity with a Transform3D and a NavMeshAgent
var agent = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(NavMeshAgent.Create())
    .Build();

// Request a path to a destination
var nav = world.Navigation;
nav.SetDestination(agent, new Vector3(10, 0, 10));
```

Each frame, `world.Update(deltaTime)` drives `PathRequestSystem` (which resolves the pending path request) and `NavMeshAgentSystem` (which steers the agent's `Transform3D` toward the destination). Progress can be inspected on the `NavMeshAgent` component itself: `HasPath`, `PathPending`, `RemainingDistance`, `IsStopped`.

## Core Concepts

### NavMeshAgent

`NavMeshAgent` is the component that opts an entity into navigation. It carries both movement configuration (`Speed`, `Acceleration`, `AngularSpeed`, `StoppingDistance`, `AutoBraking`) and live navigation state (`Destination`, `SteeringTarget`, `DesiredVelocity`, `RemainingDistance`, `HasPath`, `PathPending`, `IsOnNavMesh`, `IsStopped`):

```csharp
// Default settings suitable for a humanoid character
var defaultAgent = NavMeshAgent.Create();

// Custom AgentSettings and speed
var fastAgent = NavMeshAgent.Create(AgentSettings.Large, speed: 6f);
```

Agents also carry an `AreaMask` (a `NavAreaMask`) that restricts which `NavAreaType` regions the agent is allowed to traverse.

### NavigationContext

`NavigationContext` (accessible via `world.GetExtension<NavigationContext>()` / `world.Navigation`) is the primary API surface for driving navigation:

```csharp
var nav = world.Navigation;

// Start navigating toward a destination
nav.SetDestination(agent, new Vector3(10, 0, 10));

// Pause and resume without losing the current path
nav.Stop(agent);
nav.Resume(agent);

// Teleport an agent onto the navmesh without pathfinding
bool onMesh = nav.Warp(agent, new Vector3(3, 0, 3));

// Query pathfinding directly, independent of any agent entity
NavPath path = nav.FindPath(Vector3.Zero, new Vector3(20, 0, 20), AgentSettings.Default);
if (path.IsValid)
{
    foreach (NavPoint waypoint in path)
    {
        // waypoint.Position, waypoint.AreaType
    }
}

// Other query helpers
NavPoint? nearest = nav.FindNearestPoint(new Vector3(5, 0, 5));
bool navigable = nav.IsNavigable(new Vector3(5, 0, 5), AgentSettings.Default);
bool blocked = nav.Raycast(Vector3.Zero, new Vector3(10, 0, 0), out var hitPosition);
```

`NavigationContext` also exposes `IsReady`, `Strategy`, `ActiveAgentCount`, and `PendingRequestCount` for diagnostics, plus `Config` for read access to the active `NavigationConfig`.

### NavigationConfig

`NavigationConfig` controls the plugin's behavior:

```csharp
var config = new NavigationConfig
{
    Strategy = NavigationStrategy.Grid,
    MaxPathRequestsPerFrame = 10,
    MaxPendingRequests = 100,
    AgentSteeringEnabled = true,
    WaypointReachDistance = 0.5f,
    DynamicObstaclesEnabled = true,
    ObstacleUpdateInterval = 0.1f,
    AutoProjectToNavMesh = true,
    MaxProjectionDistance = 5f
};

world.InstallPlugin(new NavigationPlugin(config));
```

`NavigationConfig.Validate()` runs automatically when the plugin is constructed and throws `ArgumentException` if any value is out of range (for example, a non-positive `MaxPathRequestsPerFrame`).

### Dynamic Obstacles

Entities that should block or influence navigation carry a `NavMeshObstacle` component:

```csharp
using KeenEyes.Navigation.Abstractions.Components;

// A carving box obstacle - actually removes the area from the navmesh
var crate = world.Spawn()
    .With(new Transform3D(new Vector3(5, 0, 5), Quaternion.Identity, Vector3.One))
    .With(NavMeshObstacle.Box(new Vector3(1, 1, 1)))
    .Build();

// A non-carving cylindrical obstacle - used only for local avoidance
var pillar = world.Spawn()
    .With(new Transform3D(new Vector3(8, 0, 2), Quaternion.Identity, Vector3.One))
    .With(NavMeshObstacle.Cylinder(radius: 0.5f, height: 2f, carving: false))
    .Build();
```

`ObstacleUpdateSystem` tracks obstacle positions frame to frame and flags navigation data as dirty when a carving obstacle moves past `CarvingMoveThreshold`, subject to the `NavigationConfig.ObstacleUpdateInterval` throttle.

### Navmesh Surface Authoring

`NavMeshSurface` and `NavMeshModifier` mark geometry for inclusion in navmesh baking (used by navmesh-based providers such as `KeenEyes.Navigation.DotRecast`):

```csharp
// Mark a floor entity as walkable
world.Spawn("Ground")
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(NavMeshSurface.Create(NavAreaType.Walkable))
    .Build();

// Exclude a decoration from the navmesh entirely
world.Spawn("Decoration")
    .With(new Transform3D(new Vector3(5, 0, 5), Quaternion.Identity, Vector3.One))
    .With(NavMeshModifier.CreateExclude())
    .Build();
```

## Choosing a Provider

### Grid Navigation (`KeenEyes.Navigation.Grid`)

`GridNavigationPlugin` provides A* pathfinding over a `NavigationGrid` of `GridCell` entries. It is well suited to 2D games or tile-based movement:

```csharp
using KeenEyes.Navigation.Grid;

world.InstallPlugin(new GridNavigationPlugin(new GridConfig
{
    Width = 100,
    Height = 100,
    CellSize = 1f,
    AllowDiagonal = true,
    Heuristic = GridHeuristic.Octile
}));

var provider = world.GetExtension<GridNavigationProvider>();

// Block a cell directly
provider.Grid[10, 10] = GridCell.Blocked;

var path = provider.FindPath(Vector3.Zero, new Vector3(50, 0, 50), AgentSettings.Default);
```

`GridNavigationPlugin` registers `GridNavigationProvider` under both its concrete type and `INavigationProvider`, so `NavigationPlugin` can pick it up automatically as long as it's installed first.

### Navmesh Navigation (`KeenEyes.Navigation.DotRecast`)

`DotRecastNavigationPlugin` provides navmesh-based pathfinding via `DotRecastProvider`, suited to complex 3D environments:

```csharp
using KeenEyes.Navigation.DotRecast;

var meshConfig = new NavMeshConfig
{
    CellSize = 0.3f,
    AgentRadius = 0.5f,
    AgentHeight = 2.0f
};

// Build a navmesh from level geometry
var builder = new DotRecastMeshBuilder(meshConfig);
var navMeshData = builder.Build(vertices, indices);

world.InstallPlugin(new DotRecastNavigationPlugin(navMeshData));
```

Alternatively, construct `DotRecastNavigationPlugin()` with just a `NavMeshConfig` and call `DotRecastProvider.SetNavMesh` once a mesh is available.

### Custom Providers

Any type implementing `INavigationProvider` can be supplied directly, bypassing the built-in plugins:

```csharp
var config = new NavigationConfig
{
    Strategy = NavigationStrategy.Custom,
    CustomProvider = myCustomProvider
};

world.InstallPlugin(new NavigationPlugin(config));
```

## Performance

- `PathRequestSystem` runs first in `SystemPhase.Update` (order `-100`) so completed path results are applied to `NavMeshAgent` before `NavMeshAgentSystem` moves entities in the same frame.
- `NavigationConfig.MaxPathRequestsPerFrame` and provider-specific settings such as `GridConfig.RequestsPerUpdate` bound how many path computations happen per frame, trading path latency for frame-time stability.
- `ObstacleUpdateSystem` throttles navmesh-carving recalculation via `NavigationConfig.ObstacleUpdateInterval`, avoiding a full update every time a moving obstacle shifts slightly.
- `GridNavigationProvider.FindPath(GridCoordinate, GridCoordinate, Span<GridCoordinate>)` offers a zero-allocation path query for hot paths that can supply their own result buffer.

## Next Steps

- [Plugins Guide](plugins.md) - How `IWorldPlugin` installation, extensions, and lifecycle work
- [Systems Guide](systems.md) - System phases and ordering used by `PathRequestSystem`, `NavMeshAgentSystem`, and `ObstacleUpdateSystem`
- [AI System Guide](ai.md) - Pairing navigation with behavior trees or utility AI for NPC movement decisions
- [Pathfinding & Navigation Design](research/pathfinding-navigation.md) - Original design document
