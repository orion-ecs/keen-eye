# Pathfinding & Navigation System

**Date:** January 2025
**Status:** Research Complete
**Author:** Claude (Anthropic)
**Issue:** [#430](https://github.com/orion-ecs/keen-eye/issues/430)

---

## Executive Summary

This document outlines the architecture for a pathfinding and navigation system in KeenEyes, designed as a modular plugin with abstraction layers that allow users to bring their own pathfinding implementations.

**Key Architectural Decisions:**

- **Plugin-based**: `NavigationPlugin` follows existing patterns (like `SpatialPlugin`, `PhysicsPlugin`)
- **Abstraction-first**: `INavigationProvider` interface allows custom implementations
- **DotRecast default**: Industry-standard navmesh via pure C# port of Recast/Detour
- **Editor integration**: NavMesh baking as editor command with visual debugging
- **AI integration**: Seamless integration with planned AI system via Blackboard
- **Native AOT compatible**: No reflection, factory-based component creation

---

## Table of Contents

1. [Current State](#current-state)
2. [Requirements](#requirements)
3. [Library Evaluation](#library-evaluation)
4. [Architecture Overview](#architecture-overview)
5. [Abstraction Layer Design](#abstraction-layer-design)
6. [Core Components](#core-components)
7. [Navigation Plugin](#navigation-plugin)
8. [AI System Integration](#ai-system-integration)
9. [Editor Integration](#editor-integration)
10. [Implementation Plan](#implementation-plan)

---

## Current State

### What Exists

| System | Status | Relevance |
|--------|--------|-----------|
| **KeenEyes.Spatial** | Complete | Broadphase queries for obstacle detection |
| **KeenEyes.Physics** | Complete | Raycasting, collision detection |
| **AI System Research** | Complete | Decision-making integration points |
| **Plugin Architecture** | Complete | Extension pattern, capability system |
| **Transform3D** | Complete | Position/rotation for agents |

### What's Missing

- Navigation mesh generation
- Pathfinding algorithm
- Path following/steering
- Dynamic obstacle handling
- Agent avoidance (crowd simulation)

---

## Requirements

### Functional Requirements

1. **Path Finding**: Calculate paths between two points avoiding obstacles
2. **NavMesh Generation**: Build walkable surfaces from level geometry
3. **Dynamic Obstacles**: Handle doors, moving platforms, destructibles
4. **Agent Variety**: Support different agent sizes (radius, height)
5. **Path Smoothing**: Produce natural-looking paths (funnel algorithm)
6. **Async Pathfinding**: Non-blocking path requests for many agents

### Non-Functional Requirements

1. **Native AOT Compatible**: No reflection in production code
2. **Zero-Allocation Hot Path**: Minimize GC pressure during pathfinding
3. **Thread-Safe Queries**: Multiple systems can query paths concurrently
4. **Deterministic**: Same inputs produce same paths (for networking)
5. **Abstraction**: Users can replace the pathfinding backend

### Open Questions from Issue #430

| Question | Recommendation |
|----------|----------------|
| 2D vs 3D focus? | **Both** - NavMesh for 3D, Grid for 2D (separate strategies) |
| NavMesh generation - runtime or editor-time? | **Editor-time by default**, runtime for dynamic levels |
| Dynamic obstacles? | **NavMesh carving** for static-ish, **local avoidance** for fully dynamic |
| Agent variety? | **Per-agent configuration** (radius, height, climb, slope) |
| Integration pattern? | **Standalone plugin** with AI integration via Blackboard |
| Recast/Detour? | **Yes** - DotRecast (pure C# port) as default implementation |

---

## Library Evaluation

### DotRecast (Recommended)

**Repository:** [github.com/ikpil/DotRecast](https://github.com/ikpil/DotRecast)
**License:** Zlib (permissive, compatible)

| Aspect | Details |
|--------|---------|
| **Type** | Pure C# port of Recast Navigation |
| **Modules** | Core, Recast, Detour, TileCache, Crowd, Dynamic |
| **NuGet** | `DotRecast.Detour` (2025.2.1) |
| **AOT** | Compatible (no reflection) |
| **Features** | NavMesh generation, pathfinding, streaming, crowds |

**Pros:**
- Industry standard (used in Unity, Unreal, Godot)
- Active development (2025 releases)
- Modular - can use just pathfinding without generation
- Crowd simulation (agent avoidance)
- Tile-based streaming for large worlds
- Dynamic mesh modification

**Cons:**
- Complex API (wrapping needed for ergonomics)
- Binary mesh format (need custom serialization for KeenEyes)
- No 2D-specific optimizations

### SharpNav (Alternative)

**Repository:** [github.com/Robmaister/SharpNav](https://github.com/Robmaister/SharpNav)
**License:** MIT

| Aspect | Details |
|--------|---------|
| **Type** | Independent C# navmesh implementation |
| **Status** | Mature but less active |
| **API** | Cleaner BCL-style API |

**Pros:**
- Clean C# API
- Single DLL, no native dependencies
- MIT license

**Cons:**
- Less feature-complete than DotRecast
- No crowd simulation
- Less active maintenance

### Recommendation

**Use DotRecast** as the default implementation with a clean abstraction layer:

1. DotRecast provides production-quality navmesh (Unity/Unreal proven)
2. Abstraction allows users to swap for SharpNav or custom implementations
3. Wrap DotRecast's API with KeenEyes-native types

---

## Architecture Overview

### Package Structure

```
KeenEyes.Navigation.Abstractions/     # Interfaces, components, attributes
├── INavigationProvider.cs            # Main pathfinding abstraction
├── INavigationMesh.cs                # NavMesh data abstraction
├── IPathRequest.cs                   # Async path request
├── IAgentSettings.cs                 # Agent configuration
├── Components/
│   ├── NavMeshAgent.cs               # Agent component
│   ├── NavMeshObstacle.cs            # Dynamic obstacle component
│   └── NavMeshSurface.cs             # Walkable area marker (editor)
└── Data/
    ├── NavPath.cs                    # Path result
    ├── NavPoint.cs                   # Point on navmesh
    └── AgentSettings.cs              # Agent config struct

KeenEyes.Navigation/                  # Default implementation
├── NavigationPlugin.cs               # IWorldPlugin entry point
├── NavigationConfig.cs               # Plugin configuration
├── NavigationContext.cs              # Extension API (world.Navigation)
├── Providers/
│   ├── NavMeshProvider.cs            # 3D NavMesh (DotRecast wrapper)
│   └── GridProvider.cs               # 2D Grid-based A*
├── Systems/
│   ├── PathRequestSystem.cs          # Process async path requests
│   ├── NavMeshAgentSystem.cs         # Move agents along paths
│   └── ObstacleUpdateSystem.cs       # Update dynamic obstacles
├── NavMesh/
│   ├── NavMeshBuilder.cs             # Build from geometry
│   ├── NavMeshData.cs                # Serializable mesh data
│   └── NavMeshQuery.cs               # Thread-safe query wrapper
└── Grid/
    ├── NavigationGrid.cs             # 2D walkable grid
    ├── AStarPathfinder.cs            # A* implementation
    └── GridObstacle.cs               # Grid-based obstacle

KeenEyes.Navigation.DotRecast/        # DotRecast integration (optional)
├── DotRecastProvider.cs              # INavigationProvider implementation
├── DotRecastMeshBuilder.cs           # NavMesh generation
├── DotRecastCrowdManager.cs          # Crowd simulation
└── Adapters/
    ├── PathAdapter.cs                # DotRecast → NavPath conversion
    └── MeshAdapter.cs                # DotRecast → NavMeshData conversion

KeenEyes.Navigation.Editor/           # Editor tooling
├── NavMeshBakeCommand.cs             # Bake navmesh from scene
├── NavMeshVisualizer.cs              # Debug rendering
├── NavMeshInspector.cs               # Custom inspector
└── NavMeshWindow.cs                  # Navigation settings window
```

### Dependency Graph

```
                    KeenEyes.Abstractions
                           │
                    KeenEyes.Common
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
KeenEyes.Navigation   KeenEyes.AI      KeenEyes.Spatial
  .Abstractions       (future)              │
        │                  │                │
        ▼                  │                │
KeenEyes.Navigation ◄──────┘                │
        │                                   │
        ├───────────────────────────────────┘
        │
        ▼
KeenEyes.Navigation.DotRecast  (optional, default impl)
        │
        ▼
    DotRecast.Detour (NuGet)
```

---

## Abstraction Layer Design

### Core Interface: INavigationProvider

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Abstraction for pathfinding implementations.
/// Users can provide custom implementations for different algorithms.
/// </summary>
public interface INavigationProvider : IDisposable
{
    /// <summary>
    /// Gets the navigation strategy this provider implements.
    /// </summary>
    NavigationStrategy Strategy { get; }

    /// <summary>
    /// Finds a path synchronously (use for infrequent queries).
    /// </summary>
    /// <param name="start">Start position in world space.</param>
    /// <param name="end">Target position in world space.</param>
    /// <param name="settings">Agent-specific settings (radius, height, etc.).</param>
    /// <returns>The computed path, or null if no path exists.</returns>
    NavPath? FindPath(Vector3 start, Vector3 end, AgentSettings settings);

    /// <summary>
    /// Requests a path asynchronously (use for many agents).
    /// </summary>
    /// <param name="start">Start position in world space.</param>
    /// <param name="end">Target position in world space.</param>
    /// <param name="settings">Agent-specific settings.</param>
    /// <returns>A path request that will be processed by the path system.</returns>
    IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings settings);

    /// <summary>
    /// Finds the nearest valid point on the navigation mesh.
    /// </summary>
    /// <param name="position">Position to project.</param>
    /// <param name="searchRadius">Maximum search radius.</param>
    /// <param name="result">The nearest valid navigation point.</param>
    /// <returns>True if a valid point was found.</returns>
    bool TryGetNearestPoint(Vector3 position, float searchRadius, out NavPoint result);

    /// <summary>
    /// Checks if a straight line between two points is walkable.
    /// </summary>
    bool Raycast(Vector3 start, Vector3 end, AgentSettings settings, out Vector3 hitPoint);

    /// <summary>
    /// Loads navigation data (navmesh, grid, etc.).
    /// </summary>
    void LoadData(INavigationMesh data);

    /// <summary>
    /// Updates a dynamic obstacle in the navigation system.
    /// </summary>
    void UpdateObstacle(Entity entity, Vector3 position, Vector3 size, bool enabled);

    /// <summary>
    /// Removes a dynamic obstacle from the navigation system.
    /// </summary>
    void RemoveObstacle(Entity entity);
}

/// <summary>
/// Strategy type for navigation (for configuration and debugging).
/// </summary>
public enum NavigationStrategy
{
    /// <summary>3D navigation mesh (polygonal walkable surfaces).</summary>
    NavMesh,

    /// <summary>2D grid-based pathfinding.</summary>
    Grid,

    /// <summary>Hierarchical pathfinding (large worlds).</summary>
    Hierarchical,

    /// <summary>Custom/user-provided implementation.</summary>
    Custom
}
```

### Path Data Structures

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Represents a computed navigation path.
/// </summary>
public readonly struct NavPath
{
    /// <summary>Ordered waypoints from start to destination.</summary>
    public IReadOnlyList<Vector3> Waypoints { get; init; }

    /// <summary>Total path length in world units.</summary>
    public float TotalLength { get; init; }

    /// <summary>Whether the path reaches the destination.</summary>
    public bool IsComplete { get; init; }

    /// <summary>Whether the path is valid.</summary>
    public bool IsValid => Waypoints.Count > 0;

    /// <summary>Number of waypoints in the path.</summary>
    public int Count => Waypoints.Count;

    /// <summary>Gets waypoint at index.</summary>
    public Vector3 this[int index] => Waypoints[index];
}

/// <summary>
/// A point on the navigation mesh with area metadata.
/// </summary>
public readonly struct NavPoint
{
    /// <summary>World position of the point.</summary>
    public Vector3 Position { get; init; }

    /// <summary>Navigation area type (walkable, water, road, etc.).</summary>
    public NavAreaType AreaType { get; init; }

    /// <summary>Polygon/cell reference for internal use.</summary>
    public long PolyRef { get; init; }
}

/// <summary>
/// Agent-specific navigation settings.
/// </summary>
public readonly struct AgentSettings
{
    /// <summary>Agent collision radius.</summary>
    public float Radius { get; init; }

    /// <summary>Agent height for vertical clearance.</summary>
    public float Height { get; init; }

    /// <summary>Maximum slope angle the agent can walk (degrees).</summary>
    public float MaxSlopeAngle { get; init; }

    /// <summary>Maximum step height the agent can climb.</summary>
    public float MaxStepHeight { get; init; }

    /// <summary>Navigation area mask (which areas agent can traverse).</summary>
    public NavAreaMask AreaMask { get; init; }

    /// <summary>Default humanoid agent settings.</summary>
    public static AgentSettings Default => new()
    {
        Radius = 0.5f,
        Height = 2.0f,
        MaxSlopeAngle = 45f,
        MaxStepHeight = 0.4f,
        AreaMask = NavAreaMask.All
    };
}
```

### Async Path Request Interface

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Represents an asynchronous pathfinding request.
/// </summary>
public interface IPathRequest
{
    /// <summary>Unique identifier for this request.</summary>
    int Id { get; }

    /// <summary>Current status of the request.</summary>
    PathRequestStatus Status { get; }

    /// <summary>The computed path (only valid when Status is Completed).</summary>
    NavPath? Result { get; }

    /// <summary>Start position of the path.</summary>
    Vector3 Start { get; }

    /// <summary>Target position of the path.</summary>
    Vector3 End { get; }

    /// <summary>Agent settings for this request.</summary>
    AgentSettings Settings { get; }

    /// <summary>Cancels the path request.</summary>
    void Cancel();
}

/// <summary>
/// Status of a path request.
/// </summary>
public enum PathRequestStatus
{
    /// <summary>Request is queued, not yet processing.</summary>
    Pending,

    /// <summary>Request is currently being computed.</summary>
    Computing,

    /// <summary>Path found successfully.</summary>
    Completed,

    /// <summary>No valid path exists.</summary>
    Failed,

    /// <summary>Request was cancelled.</summary>
    Cancelled
}
```

### Navigation Mesh Abstraction

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Abstraction for navigation mesh data.
/// Implementations wrap format-specific data (DotRecast, SharpNav, custom).
/// </summary>
public interface INavigationMesh
{
    /// <summary>Unique identifier for this mesh.</summary>
    string Id { get; }

    /// <summary>World-space bounds of the navigation mesh.</summary>
    BoundingBox Bounds { get; }

    /// <summary>Number of polygons/tiles in the mesh.</summary>
    int PolygonCount { get; }

    /// <summary>Whether the mesh supports dynamic obstacles.</summary>
    bool SupportsDynamicObstacles { get; }

    /// <summary>Serializes the mesh to binary format.</summary>
    byte[] ToBinary();

    /// <summary>Gets debug visualization data.</summary>
    NavMeshDebugData GetDebugData();
}

/// <summary>
/// Debug visualization data for navmesh rendering.
/// </summary>
public readonly struct NavMeshDebugData
{
    /// <summary>Polygon vertices for debug rendering.</summary>
    public IReadOnlyList<Vector3> Vertices { get; init; }

    /// <summary>Triangle indices.</summary>
    public IReadOnlyList<int> Indices { get; init; }

    /// <summary>Area type per polygon (for coloring).</summary>
    public IReadOnlyList<NavAreaType> AreaTypes { get; init; }
}
```

---

## Core Components

### NavMeshAgent Component

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Component for entities that navigate using pathfinding.
/// </summary>
[Component]
public partial struct NavMeshAgent
{
    /// <summary>Agent radius for collision avoidance.</summary>
    public float Radius;

    /// <summary>Agent height for vertical clearance.</summary>
    public float Height;

    /// <summary>Maximum movement speed.</summary>
    public float Speed;

    /// <summary>Turning speed in degrees per second.</summary>
    public float AngularSpeed;

    /// <summary>Maximum slope the agent can walk (degrees).</summary>
    public float MaxSlopeAngle;

    /// <summary>Maximum step height the agent can climb.</summary>
    public float MaxStepHeight;

    /// <summary>Stopping distance from destination.</summary>
    public float StoppingDistance;

    /// <summary>Current destination (Entity.Null if none).</summary>
    public Vector3 Destination;

    /// <summary>Whether the agent has a destination.</summary>
    public bool HasDestination;

    /// <summary>Whether the agent has reached its destination.</summary>
    public bool HasReachedDestination;

    /// <summary>Current path being followed.</summary>
    public NavPath? CurrentPath;

    /// <summary>Current waypoint index in the path.</summary>
    public int CurrentWaypointIndex;

    /// <summary>Area mask for traversable areas.</summary>
    public NavAreaMask AreaMask;

    /// <summary>Whether the agent is enabled.</summary>
    public bool Enabled;

    /// <summary>Creates agent settings from this component.</summary>
    public readonly AgentSettings ToSettings() => new()
    {
        Radius = Radius,
        Height = Height,
        MaxSlopeAngle = MaxSlopeAngle,
        MaxStepHeight = MaxStepHeight,
        AreaMask = AreaMask
    };
}
```

### NavMeshObstacle Component

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Component for dynamic obstacles that carve into the navmesh.
/// </summary>
[Component]
public partial struct NavMeshObstacle
{
    /// <summary>Shape of the obstacle.</summary>
    public ObstacleShape Shape;

    /// <summary>Size of the obstacle (box: half-extents, cylinder: radius/height).</summary>
    public Vector3 Size;

    /// <summary>Whether the obstacle carves into the navmesh.</summary>
    public bool Carve;

    /// <summary>Whether the obstacle is currently active.</summary>
    public bool Enabled;

    /// <summary>Internal reference for the navigation system.</summary>
    internal int ObstacleHandle;
}

/// <summary>
/// Shape of a navigation obstacle.
/// </summary>
public enum ObstacleShape
{
    /// <summary>Axis-aligned box obstacle.</summary>
    Box,

    /// <summary>Cylinder obstacle (common for characters).</summary>
    Cylinder
}
```

### Area Types and Masks

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Navigation area types for cost modification and filtering.
/// </summary>
public enum NavAreaType : byte
{
    /// <summary>Standard walkable ground.</summary>
    Walkable = 0,

    /// <summary>Unwalkable/blocked area.</summary>
    NotWalkable = 1,

    /// <summary>Water (swimmable, higher cost).</summary>
    Water = 2,

    /// <summary>Road (lower cost for faster movement).</summary>
    Road = 3,

    /// <summary>Grass (slightly higher cost).</summary>
    Grass = 4,

    /// <summary>Jump link (special traversal).</summary>
    Jump = 5,

    /// <summary>Door (conditionally walkable).</summary>
    Door = 6,

    /// <summary>User-defined area 1.</summary>
    Custom1 = 7,

    /// <summary>User-defined area 2.</summary>
    Custom2 = 8
}

/// <summary>
/// Bitmask for filtering which areas an agent can traverse.
/// </summary>
[Flags]
public enum NavAreaMask : uint
{
    None = 0,
    Walkable = 1 << NavAreaType.Walkable,
    Water = 1 << NavAreaType.Water,
    Road = 1 << NavAreaType.Road,
    Grass = 1 << NavAreaType.Grass,
    Jump = 1 << NavAreaType.Jump,
    Door = 1 << NavAreaType.Door,

    /// <summary>All ground-based areas (no water, no jump).</summary>
    Ground = Walkable | Road | Grass | Door,

    /// <summary>All traversable areas.</summary>
    All = 0xFFFFFFFF
}
```

---

## Navigation Plugin

### NavigationPlugin

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Plugin that provides pathfinding and navigation capabilities.
/// </summary>
/// <example>
/// <code>
/// // Install with default NavMesh provider
/// world.InstallPlugin(new NavigationPlugin());
///
/// // Install with custom configuration
/// world.InstallPlugin(new NavigationPlugin(new NavigationConfig
/// {
///     Strategy = NavigationStrategy.NavMesh,
///     MaxPathRequestsPerFrame = 10,
///     EnableCrowdSimulation = true
/// }));
///
/// // Install with custom provider (BYOP)
/// world.InstallPlugin(new NavigationPlugin(new MyCustomProvider()));
/// </code>
/// </example>
public sealed class NavigationPlugin : IWorldPlugin
{
    private readonly NavigationConfig config;
    private readonly INavigationProvider? customProvider;
    private NavigationContext? context;
    private EventSubscription? agentAddedSub;
    private EventSubscription? agentRemovedSub;
    private EventSubscription? obstacleAddedSub;
    private EventSubscription? obstacleRemovedSub;

    /// <summary>
    /// Creates a navigation plugin with default configuration.
    /// </summary>
    public NavigationPlugin()
        : this(new NavigationConfig())
    {
    }

    /// <summary>
    /// Creates a navigation plugin with custom configuration.
    /// </summary>
    public NavigationPlugin(NavigationConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid NavigationConfig: {error}", nameof(config));
        }

        this.config = config;
    }

    /// <summary>
    /// Creates a navigation plugin with a custom provider (BYOP).
    /// </summary>
    /// <param name="provider">Custom navigation provider implementation.</param>
    public NavigationPlugin(INavigationProvider provider)
    {
        customProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        config = new NavigationConfig { Strategy = provider.Strategy };
    }

    /// <inheritdoc/>
    public string Name => "Navigation";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<NavMeshAgent>();
        context.RegisterComponent<NavMeshObstacle>();

        // Create provider (custom or default based on strategy)
        INavigationProvider provider = customProvider ?? CreateDefaultProvider();

        // Create and expose the navigation context
        this.context = new NavigationContext(context.World, provider, config);
        context.SetExtension(this.context);

        // Register systems
        context.AddSystem<PathRequestSystem>(SystemPhase.Update, order: -50);
        context.AddSystem<NavMeshAgentSystem>(SystemPhase.Update, order: 0);
        context.AddSystem<ObstacleUpdateSystem>(SystemPhase.LateUpdate, order: -100);

        // Subscribe to component lifecycle events
        agentAddedSub = context.World.OnComponentAdded<NavMeshAgent>(OnAgentAdded);
        agentRemovedSub = context.World.OnComponentRemoved<NavMeshAgent>(OnAgentRemoved);
        obstacleAddedSub = context.World.OnComponentAdded<NavMeshObstacle>(OnObstacleAdded);
        obstacleRemovedSub = context.World.OnComponentRemoved<NavMeshObstacle>(OnObstacleRemoved);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from events
        agentAddedSub?.Dispose();
        agentRemovedSub?.Dispose();
        obstacleAddedSub?.Dispose();
        obstacleRemovedSub?.Dispose();

        // Remove extension
        context.RemoveExtension<NavigationContext>();

        // Dispose context (which disposes provider)
        this.context?.Dispose();
        this.context = null;

        // Systems auto-cleaned by PluginManager
    }

    private INavigationProvider CreateDefaultProvider()
    {
        return config.Strategy switch
        {
            NavigationStrategy.NavMesh => new NavMeshProvider(config),
            NavigationStrategy.Grid => new GridProvider(config),
            _ => throw new NotSupportedException($"Strategy {config.Strategy} requires custom provider")
        };
    }

    private void OnAgentAdded(Entity entity, NavMeshAgent agent) { /* Register with crowd */ }
    private void OnAgentRemoved(Entity entity, NavMeshAgent agent) { /* Unregister from crowd */ }
    private void OnObstacleAdded(Entity entity, NavMeshObstacle obstacle) { /* Add to navmesh */ }
    private void OnObstacleRemoved(Entity entity, NavMeshObstacle obstacle) { /* Remove from navmesh */ }
}
```

### NavigationContext (Extension API)

```csharp
namespace KeenEyes.Navigation;

/// <summary>
/// Extension API for navigation, accessed via world.GetExtension&lt;NavigationContext&gt;().
/// </summary>
/// <example>
/// <code>
/// var nav = world.GetExtension&lt;NavigationContext&gt;();
///
/// // Find a path synchronously
/// var path = nav.FindPath(start, end);
///
/// // Request path asynchronously (for many agents)
/// var request = nav.RequestPath(start, end, agentSettings);
///
/// // Set destination for an agent
/// nav.SetDestination(entity, targetPosition);
/// </code>
/// </example>
[PluginExtension("Navigation")]
public sealed class NavigationContext : IDisposable
{
    private readonly IWorld world;
    private readonly INavigationProvider provider;
    private readonly NavigationConfig config;
    private readonly ConcurrentQueue<IPathRequest> pendingRequests = new();

    internal NavigationContext(IWorld world, INavigationProvider provider, NavigationConfig config)
    {
        this.world = world;
        this.provider = provider;
        this.config = config;
    }

    /// <summary>
    /// Gets the underlying navigation provider.
    /// </summary>
    public INavigationProvider Provider => provider;

    /// <summary>
    /// Finds a path synchronously between two points.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">Target position.</param>
    /// <param name="settings">Optional agent settings (defaults to standard humanoid).</param>
    /// <returns>The computed path, or null if no path exists.</returns>
    public NavPath? FindPath(Vector3 start, Vector3 end, AgentSettings? settings = null)
    {
        return provider.FindPath(start, end, settings ?? AgentSettings.Default);
    }

    /// <summary>
    /// Requests a path asynchronously (processed by PathRequestSystem).
    /// </summary>
    public IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings? settings = null)
    {
        var request = provider.RequestPath(start, end, settings ?? AgentSettings.Default);
        pendingRequests.Enqueue(request);
        return request;
    }

    /// <summary>
    /// Sets the destination for a NavMeshAgent entity.
    /// </summary>
    public void SetDestination(Entity entity, Vector3 destination)
    {
        if (!world.Has<NavMeshAgent>(entity))
        {
            throw new InvalidOperationException($"Entity {entity} does not have NavMeshAgent component");
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Destination = destination;
        agent.HasDestination = true;
        agent.HasReachedDestination = false;

        // Request path for this agent
        var settings = agent.ToSettings();
        var currentPos = world.Get<Transform3D>(entity).Position;
        var request = RequestPath(currentPos, destination, settings);

        // Store request ID for tracking (could use separate component)
        // PathRequestSystem will update agent.CurrentPath when complete
    }

    /// <summary>
    /// Stops the agent and clears its destination.
    /// </summary>
    public void Stop(Entity entity)
    {
        if (world.Has<NavMeshAgent>(entity))
        {
            ref var agent = ref world.Get<NavMeshAgent>(entity);
            agent.HasDestination = false;
            agent.CurrentPath = null;
        }
    }

    /// <summary>
    /// Loads navigation mesh data.
    /// </summary>
    public void LoadNavMesh(INavigationMesh mesh)
    {
        provider.LoadData(mesh);
    }

    /// <summary>
    /// Loads navigation mesh from binary data.
    /// </summary>
    public void LoadNavMesh(byte[] data)
    {
        var mesh = NavMeshData.FromBinary(data);
        provider.LoadData(mesh);
    }

    /// <summary>
    /// Finds the nearest point on the navigation mesh.
    /// </summary>
    public bool TryGetNearestPoint(Vector3 position, out NavPoint result, float searchRadius = 10f)
    {
        return provider.TryGetNearestPoint(position, searchRadius, out result);
    }

    /// <summary>
    /// Raycasts against the navigation mesh.
    /// </summary>
    public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPoint, AgentSettings? settings = null)
    {
        return provider.Raycast(start, end, settings ?? AgentSettings.Default, out hitPoint);
    }

    /// <summary>
    /// Gets pending path requests for the PathRequestSystem.
    /// </summary>
    internal bool TryDequeuePendingRequest(out IPathRequest? request)
    {
        return pendingRequests.TryDequeue(out request);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        provider.Dispose();
    }
}
```

---

## AI System Integration

### Blackboard Keys

The AI system research defines navigation blackboard keys:

```csharp
public static class BBKeys
{
    // Navigation (defined in AI system)
    public const string Destination = "Destination";
    public const string CurrentPath = "CurrentPath";
    public const string PatrolIndex = "PatrolIndex";
}
```

### AI Actions Using Navigation

```csharp
namespace KeenEyes.AI.Actions;

/// <summary>
/// AI action that moves an entity to a destination using pathfinding.
/// </summary>
public sealed class MoveToAction : IAIAction
{
    public float ArrivalDistance { get; init; } = 1f;

    public BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        // Get navigation context
        var nav = world.GetExtension<NavigationContext>();
        if (nav == null)
        {
            return BTNodeState.Failure;
        }

        // Get destination from blackboard
        if (!bb.TryGet<Vector3>("Destination", out var destination))
        {
            return BTNodeState.Failure;
        }

        // Check if we have a NavMeshAgent
        if (!world.Has<NavMeshAgent>(entity))
        {
            return BTNodeState.Failure;
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        ref readonly var transform = ref world.Get<Transform3D>(entity);

        // Check if already at destination
        if (Vector3.Distance(transform.Position, destination) <= ArrivalDistance)
        {
            agent.HasDestination = false;
            return BTNodeState.Success;
        }

        // Set destination if not already set
        if (!agent.HasDestination || agent.Destination != destination)
        {
            nav.SetDestination(entity, destination);
        }

        // Still moving
        return BTNodeState.Running;
    }
}

/// <summary>
/// AI action that chases a target entity.
/// </summary>
public sealed class ChaseAction : IAIAction
{
    public float UpdateInterval { get; init; } = 0.5f;
    public float CatchDistance { get; init; } = 2f;

    private float timeSinceUpdate;

    public BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        var nav = world.GetExtension<NavigationContext>();
        if (nav == null) return BTNodeState.Failure;

        // Get target from blackboard
        if (!bb.TryGet<Entity>("Target", out var target) || !world.IsAlive(target))
        {
            return BTNodeState.Failure;
        }

        var targetPos = world.Get<Transform3D>(target).Position;
        var myPos = world.Get<Transform3D>(entity).Position;

        // Check if caught target
        if (Vector3.Distance(myPos, targetPos) <= CatchDistance)
        {
            return BTNodeState.Success;
        }

        // Update path periodically (target may move)
        timeSinceUpdate += bb.Get<float>("DeltaTime");
        if (timeSinceUpdate >= UpdateInterval)
        {
            timeSinceUpdate = 0;
            nav.SetDestination(entity, targetPos);
        }

        return BTNodeState.Running;
    }
}

/// <summary>
/// AI action that patrols between waypoints.
/// </summary>
public sealed class PatrolAction : IAIAction
{
    public float WaypointArrivalDistance { get; init; } = 1f;
    public bool Loop { get; init; } = true;

    public BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        var nav = world.GetExtension<NavigationContext>();
        if (nav == null) return BTNodeState.Failure;

        // Get waypoints from blackboard
        if (!bb.TryGet<Vector3[]>("PatrolWaypoints", out var waypoints) || waypoints.Length == 0)
        {
            return BTNodeState.Failure;
        }

        var currentIndex = bb.Get<int>("PatrolIndex");
        var currentWaypoint = waypoints[currentIndex];
        var myPos = world.Get<Transform3D>(entity).Position;

        // Check if reached current waypoint
        if (Vector3.Distance(myPos, currentWaypoint) <= WaypointArrivalDistance)
        {
            // Move to next waypoint
            currentIndex++;
            if (currentIndex >= waypoints.Length)
            {
                if (Loop)
                {
                    currentIndex = 0;
                }
                else
                {
                    return BTNodeState.Success;
                }
            }

            bb.Set("PatrolIndex", currentIndex);
            nav.SetDestination(entity, waypoints[currentIndex]);
        }
        else if (!world.Get<NavMeshAgent>(entity).HasDestination)
        {
            // Start moving to current waypoint
            nav.SetDestination(entity, currentWaypoint);
        }

        return BTNodeState.Running;
    }
}
```

---

## Editor Integration

### NavMesh Bake Pipeline

```csharp
namespace KeenEyes.Navigation.Editor;

/// <summary>
/// Editor command to bake navigation mesh from scene geometry.
/// </summary>
public sealed class NavMeshBakeCommand
{
    private readonly IWorld world;
    private readonly NavigationContext nav;

    /// <summary>
    /// Bakes navigation mesh from scene geometry.
    /// </summary>
    /// <param name="config">Bake configuration.</param>
    /// <returns>The generated navigation mesh.</returns>
    public async Task<INavigationMesh> BakeAsync(NavMeshBakeConfig config)
    {
        // 1. Collect geometry from scene
        var geometry = CollectGeometry(config);

        // 2. Generate navmesh using DotRecast
        var builder = new NavMeshBuilder(config);
        var mesh = await builder.BuildAsync(geometry);

        // 3. Optionally save to file
        if (!string.IsNullOrEmpty(config.OutputPath))
        {
            var data = mesh.ToBinary();
            await File.WriteAllBytesAsync(config.OutputPath, data);
        }

        return mesh;
    }

    private NavMeshGeometry CollectGeometry(NavMeshBakeConfig config)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Query entities with NavMeshSurface tag (walkable geometry)
        foreach (var entity in world.Query<NavMeshSurface>())
        {
            // Get mesh from entity (requires mesh component)
            if (world.TryGet<Mesh>(entity, out var mesh))
            {
                var transform = world.Get<Transform3D>(entity);
                AddMeshGeometry(vertices, triangles, mesh, transform);
            }
        }

        return new NavMeshGeometry
        {
            Vertices = vertices.ToArray(),
            Triangles = triangles.ToArray()
        };
    }
}

/// <summary>
/// Configuration for navmesh baking.
/// </summary>
public sealed class NavMeshBakeConfig
{
    /// <summary>Agent radius for path calculations.</summary>
    public float AgentRadius { get; init; } = 0.5f;

    /// <summary>Agent height for vertical clearance.</summary>
    public float AgentHeight { get; init; } = 2.0f;

    /// <summary>Maximum slope angle (degrees).</summary>
    public float MaxSlopeAngle { get; init; } = 45f;

    /// <summary>Maximum step height.</summary>
    public float MaxStepHeight { get; init; } = 0.4f;

    /// <summary>Cell size for voxelization (smaller = more detail).</summary>
    public float CellSize { get; init; } = 0.3f;

    /// <summary>Cell height for voxelization.</summary>
    public float CellHeight { get; init; } = 0.2f;

    /// <summary>Minimum region area to keep (filters small islands).</summary>
    public float MinRegionArea { get; init; } = 8f;

    /// <summary>Output file path for baked navmesh.</summary>
    public string? OutputPath { get; init; }
}
```

### NavMesh Visualizer

```csharp
namespace KeenEyes.Navigation.Editor;

/// <summary>
/// Debug visualization for navigation mesh.
/// </summary>
public sealed class NavMeshVisualizer
{
    private readonly NavigationContext nav;

    /// <summary>
    /// Renders the navigation mesh for debugging.
    /// </summary>
    public void Render(I3DRenderer renderer, NavMeshVisualizerOptions options)
    {
        var mesh = nav.Provider as IDebugVisualizableProvider;
        if (mesh == null) return;

        var debugData = mesh.GetDebugData();

        // Render navmesh polygons
        if (options.ShowPolygons)
        {
            RenderPolygons(renderer, debugData, options);
        }

        // Render polygon boundaries
        if (options.ShowEdges)
        {
            RenderEdges(renderer, debugData, options);
        }

        // Render agent paths
        if (options.ShowAgentPaths)
        {
            RenderAgentPaths(renderer, options);
        }
    }

    private void RenderPolygons(I3DRenderer renderer, NavMeshDebugData data, NavMeshVisualizerOptions options)
    {
        for (int i = 0; i < data.Indices.Count; i += 3)
        {
            var v0 = data.Vertices[data.Indices[i]];
            var v1 = data.Vertices[data.Indices[i + 1]];
            var v2 = data.Vertices[data.Indices[i + 2]];

            var areaType = data.AreaTypes[i / 3];
            var color = GetAreaColor(areaType, options.PolygonAlpha);

            renderer.DrawTriangle(v0, v1, v2, color);
        }
    }

    private static Color GetAreaColor(NavAreaType areaType, float alpha)
    {
        return areaType switch
        {
            NavAreaType.Walkable => new Color(0.4f, 0.7f, 0.4f, alpha),  // Green
            NavAreaType.Water => new Color(0.3f, 0.5f, 0.8f, alpha),     // Blue
            NavAreaType.Road => new Color(0.7f, 0.7f, 0.5f, alpha),      // Tan
            NavAreaType.Grass => new Color(0.3f, 0.6f, 0.3f, alpha),     // Dark green
            NavAreaType.Door => new Color(0.6f, 0.4f, 0.2f, alpha),      // Brown
            _ => new Color(0.5f, 0.5f, 0.5f, alpha)                       // Gray
        };
    }
}

/// <summary>
/// Options for navmesh visualization.
/// </summary>
public sealed class NavMeshVisualizerOptions
{
    public bool ShowPolygons { get; init; } = true;
    public bool ShowEdges { get; init; } = true;
    public bool ShowAgentPaths { get; init; } = true;
    public float PolygonAlpha { get; init; } = 0.3f;
    public Color EdgeColor { get; init; } = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color PathColor { get; init; } = new Color(1f, 0.5f, 0f, 1f);
}
```

### Editor Window

```csharp
namespace KeenEyes.Navigation.Editor;

/// <summary>
/// Editor window for navigation settings and baking.
/// </summary>
public sealed class NavigationWindow
{
    // Agent settings
    public float AgentRadius { get; set; } = 0.5f;
    public float AgentHeight { get; set; } = 2.0f;
    public float MaxSlope { get; set; } = 45f;
    public float StepHeight { get; set; } = 0.4f;

    // Bake settings
    public float CellSize { get; set; } = 0.3f;
    public float CellHeight { get; set; } = 0.2f;

    // Visualization
    public bool ShowNavMesh { get; set; } = true;
    public bool ShowAgentPaths { get; set; } = true;

    public async Task BakeNavMesh()
    {
        var config = new NavMeshBakeConfig
        {
            AgentRadius = AgentRadius,
            AgentHeight = AgentHeight,
            MaxSlopeAngle = MaxSlope,
            MaxStepHeight = StepHeight,
            CellSize = CellSize,
            CellHeight = CellHeight,
            OutputPath = GetNavMeshPath()
        };

        var command = new NavMeshBakeCommand(world, nav);
        var mesh = await command.BakeAsync(config);

        // Load into navigation system
        nav.LoadNavMesh(mesh);
    }
}
```

---

## Implementation Plan

### Phase 1: Abstractions & Core Types

**Goal:** Define interfaces and data structures

| Task | Effort | Priority |
|------|--------|----------|
| Create `KeenEyes.Navigation.Abstractions` project | Low | P0 |
| Define `INavigationProvider` interface | Medium | P0 |
| Define `INavigationMesh`, `IPathRequest` interfaces | Medium | P0 |
| Define components: `NavMeshAgent`, `NavMeshObstacle` | Medium | P0 |
| Define `NavPath`, `NavPoint`, `AgentSettings` structs | Low | P0 |
| Define `NavAreaType`, `NavAreaMask` enums | Low | P0 |

**Deliverable:** Abstract interfaces users can implement

### Phase 2: Grid-Based Provider (2D)

**Goal:** Simple pathfinding for 2D games

| Task | Effort | Priority |
|------|--------|----------|
| Implement `NavigationGrid` for 2D worlds | Medium | P0 |
| Implement A* pathfinder with diagonal movement | Medium | P0 |
| Implement `GridProvider` (INavigationProvider) | Medium | P0 |
| Add grid obstacle handling | Low | P1 |
| Unit tests for grid pathfinding | Medium | P0 |

**Deliverable:** Working 2D pathfinding

### Phase 3: NavMesh Provider (DotRecast)

**Goal:** Industry-standard 3D pathfinding

| Task | Effort | Priority |
|------|--------|----------|
| Create `KeenEyes.Navigation.DotRecast` project | Low | P0 |
| Wrap DotRecast query API | High | P0 |
| Implement `DotRecastProvider` | High | P0 |
| Implement path smoothing (funnel) | Medium | P0 |
| Implement dynamic obstacles | Medium | P1 |
| Integration tests with real navmesh | Medium | P0 |

**Deliverable:** Full-featured 3D navmesh pathfinding

### Phase 4: Navigation Plugin

**Goal:** ECS integration with systems

| Task | Effort | Priority |
|------|--------|----------|
| Implement `NavigationPlugin` | Medium | P0 |
| Implement `NavigationContext` (extension API) | Medium | P0 |
| Implement `PathRequestSystem` | Medium | P0 |
| Implement `NavMeshAgentSystem` | Medium | P0 |
| Implement `ObstacleUpdateSystem` | Medium | P1 |
| Component lifecycle event handling | Low | P0 |

**Deliverable:** Navigation as a plugin

### Phase 5: AI Integration

**Goal:** Seamless AI system integration

| Task | Effort | Priority |
|------|--------|----------|
| Implement `MoveToAction` for behavior trees | Low | P0 |
| Implement `ChaseAction` | Low | P0 |
| Implement `PatrolAction` | Low | P0 |
| Update AI research doc with integration | Low | P0 |
| Sample demonstrating AI + Navigation | Medium | P1 |

**Deliverable:** AI actions using pathfinding

### Phase 6: Editor Integration

**Goal:** NavMesh baking and visualization

| Task | Effort | Priority |
|------|--------|----------|
| Create `KeenEyes.Navigation.Editor` project | Low | P1 |
| Implement `NavMeshBakeCommand` | High | P1 |
| Implement `NavMeshVisualizer` | Medium | P1 |
| Create Navigation editor window | Medium | P1 |
| Save/load navmesh to .kenavmesh files | Medium | P1 |
| Add to editor plugin system | Low | P1 |

**Deliverable:** Visual navmesh editing

### Phase 7: Advanced Features

**Goal:** Production-ready features

| Task | Effort | Priority |
|------|--------|----------|
| Crowd simulation (agent avoidance) | High | P2 |
| Off-mesh links (jumping, climbing) | Medium | P2 |
| Hierarchical pathfinding for large worlds | High | P2 |
| Streaming navmesh tiles | High | P2 |
| Runtime navmesh regeneration | High | P2 |

**Deliverable:** Complete navigation system

---

## Summary

The navigation system follows KeenEyes' architectural patterns:

1. **Abstraction-first**: `INavigationProvider` allows users to bring their own implementation
2. **Plugin-based**: `NavigationPlugin` integrates cleanly with the ECS world
3. **DotRecast default**: Industry-standard navmesh via pure C# (no native dependencies)
4. **AI integration**: Seamless use from behavior trees via Blackboard
5. **Editor tooling**: Visual navmesh baking and debugging
6. **Native AOT**: No reflection, factory patterns for component creation

The modular design supports:
- **Simple 2D games**: Grid-based A* pathfinding
- **3D games**: Full navmesh with dynamic obstacles
- **Large worlds**: Hierarchical pathfinding and streaming
- **Custom needs**: Plug in custom implementations

---

## References

- [DotRecast](https://github.com/ikpil/DotRecast) - C# port of Recast Navigation
- [SharpNav](https://github.com/Robmaister/SharpNav) - Alternative C# navmesh library
- [Recast Navigation](https://recastnav.com/) - Original C++ implementation
- [AI System Research](ai-system.md) - Decision-making integration
- [Spatial Plugin](../../src/KeenEyes.Spatial/) - Broadphase query patterns
- [Plugin Architecture](../../src/KeenEyes.Core/PluginManager.cs) - Plugin patterns

---

## Appendix: Sample Usage

```csharp
// Setup world with navigation
using var world = new World();
world.InstallPlugin(new PhysicsPlugin());
world.InstallPlugin(new SpatialPlugin());
world.InstallPlugin(new NavigationPlugin(new NavigationConfig
{
    Strategy = NavigationStrategy.NavMesh,
    MaxPathRequestsPerFrame = 10
}));

// Load baked navmesh
var nav = world.GetExtension<NavigationContext>();
nav.LoadNavMesh(File.ReadAllBytes("level.kenavmesh"));

// Create an AI agent
var enemy = world.Spawn("Enemy")
    .With(new Transform3D(new Vector3(0, 0, 10)))
    .With(new NavMeshAgent
    {
        Radius = 0.5f,
        Height = 2f,
        Speed = 5f,
        AngularSpeed = 360f,
        MaxSlopeAngle = 45f,
        StoppingDistance = 1f,
        Enabled = true
    })
    .Build();

// Set destination (pathfinding happens automatically)
nav.SetDestination(enemy, new Vector3(50, 0, 50));

// Or use with AI behavior tree
var bb = new Blackboard();
bb.Set("Target", player);
bb.Set("PatrolWaypoints", new[] { pointA, pointB, pointC });

var behavior = new Selector
{
    Children = [
        new Sequence {
            Children = [
                new ConditionNode { Condition = new CanSeeTargetCondition() },
                new ActionNode { Action = new ChaseAction() }
            ]
        },
        new ActionNode { Action = new PatrolAction() }
    ]
};
```

---

## Appendix: BYOP (Bring Your Own Pathfinding)

Users can implement `INavigationProvider` for custom pathfinding:

```csharp
public sealed class MyCustomProvider : INavigationProvider
{
    public NavigationStrategy Strategy => NavigationStrategy.Custom;

    public NavPath? FindPath(Vector3 start, Vector3 end, AgentSettings settings)
    {
        // Custom implementation (e.g., A* on hex grid, flow fields, etc.)
        var waypoints = MyPathfinder.Compute(start, end, settings);
        return waypoints == null ? null : new NavPath
        {
            Waypoints = waypoints,
            TotalLength = CalculateLength(waypoints),
            IsComplete = true
        };
    }

    // ... implement other interface methods
}

// Use custom provider
world.InstallPlugin(new NavigationPlugin(new MyCustomProvider()));
```
