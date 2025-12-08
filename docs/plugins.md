# Plugin System Guide

The plugin system enables modular, reusable extensions for KeenEyes worlds. Plugins can register systems, provide custom APIs, and encapsulate domain-specific functionality like physics, networking, or audio.

## What is a Plugin?

A plugin is a self-contained module that:

1. **Registers systems** - Adds systems to a world during installation
2. **Provides extensions** - Exposes custom APIs via the extension mechanism
3. **Manages lifecycle** - Handles setup on install and cleanup on uninstall

Plugins promote:
- **Modularity** - Package related functionality together
- **Reusability** - Share plugins across multiple worlds
- **Clean separation** - Isolate domain logic from application code

## Creating a Plugin

Implement the `IWorldPlugin` interface:

```csharp
using KeenEyes;

public class PhysicsPlugin : IWorldPlugin
{
    public string Name => "Physics";

    public void Install(PluginContext context)
    {
        // Register systems
        context.AddSystem<GravitySystem>(SystemPhase.FixedUpdate, order: 0);
        context.AddSystem<CollisionSystem>(SystemPhase.FixedUpdate, order: 10);
        context.AddSystem<IntegrationSystem>(SystemPhase.FixedUpdate, order: 20);

        // Expose custom API
        context.SetExtension(new PhysicsWorld(context.World));
    }

    public void Uninstall(PluginContext context)
    {
        // Cleanup (systems are auto-removed)
        context.RemoveExtension<PhysicsWorld>();
    }
}
```

### Plugin Name

The `Name` property must be unique within a world. Installing two plugins with the same name throws `InvalidOperationException`.

### Installation

During `Install()`, use `PluginContext` to:

- **Add systems** via `AddSystem<T>()` overloads
- **Add system groups** via `AddSystemGroup()`
- **Set extensions** via `SetExtension<T>()`

All systems registered via the context are automatically tracked and removed when the plugin is uninstalled.

### Uninstallation

During `Uninstall()`:
- Systems registered via `context.AddSystem()` are automatically removed
- Clean up any extensions you set
- Release any resources the plugin owns

## Installing Plugins

### Via WorldBuilder (Recommended)

```csharp
using var world = new WorldBuilder()
    .WithPlugin<PhysicsPlugin>()
    .WithPlugin<AudioPlugin>()
    .WithPlugin<NetworkPlugin>()
    .Build();
```

### Via World Directly

```csharp
using var world = new World();

// Install by type
world.InstallPlugin<PhysicsPlugin>();

// Or install by instance
var audioPlugin = new AudioPlugin(config);
world.InstallPlugin(audioPlugin);

// Chaining supported
world
    .InstallPlugin<PhysicsPlugin>()
    .InstallPlugin<AudioPlugin>();
```

## Querying Plugins

```csharp
// Check if installed
if (world.HasPlugin<PhysicsPlugin>())
{
    // Get plugin instance
    var physics = world.GetPlugin<PhysicsPlugin>();
}

// Get by name
if (world.HasPlugin("Physics"))
{
    var physics = world.GetPlugin("Physics");
}

// Enumerate all plugins
foreach (var plugin in world.GetPlugins())
{
    Console.WriteLine($"Installed: {plugin.Name}");
}
```

## Uninstalling Plugins

```csharp
// By type
bool removed = world.UninstallPlugin<PhysicsPlugin>();

// By name
bool removed = world.UninstallPlugin("Physics");
```

When uninstalled:
1. `Uninstall()` is called on the plugin
2. All systems registered via `PluginContext` are removed
3. The plugin is removed from the world's registry

## Extension API

Extensions let plugins expose custom APIs on the world:

### Setting Extensions

```csharp
public void Install(PluginContext context)
{
    // Create and register your API
    var physicsWorld = new PhysicsWorld(context.World);
    context.SetExtension(physicsWorld);
}
```

### Accessing Extensions

```csharp
// Get or throw
var physics = world.GetExtension<PhysicsWorld>();

// Safe access
if (world.TryGetExtension<PhysicsWorld>(out var physics))
{
    physics.SetGravity(9.8f);
}

// Check existence
if (world.HasExtension<PhysicsWorld>())
{
    // ...
}
```

### Generated Typed Access (C# 13+)

Use `[PluginExtension]` for compile-time typed properties:

```csharp
[PluginExtension("Physics")]
public class PhysicsWorld
{
    private readonly World world;

    public PhysicsWorld(World world) => this.world = world;

    public void SetGravity(float g) { /* ... */ }
    public void Raycast(Vector3 from, Vector3 to) { /* ... */ }
}
```

This generates an extension property allowing:

```csharp
// Instead of:
world.GetExtension<PhysicsWorld>().SetGravity(9.8f);

// Write:
world.Physics.SetGravity(9.8f);
```

For nullable extensions:

```csharp
[PluginExtension("Physics", Nullable = true)]
public class PhysicsWorld { /* ... */ }

// Generated property returns null if not set
world.Physics?.SetGravity(9.8f);
```

## System Registration

### Basic Registration

```csharp
// With phase and order
context.AddSystem<MovementSystem>(SystemPhase.Update, order: 10);

// With dependencies
context.AddSystem<CollisionSystem>(
    SystemPhase.FixedUpdate,
    order: 0,
    runsBefore: [typeof(DamageSystem)],
    runsAfter: [typeof(MovementSystem)]);
```

### System Groups

```csharp
var physicsGroup = new SystemGroup("Physics");
physicsGroup.AddSystem(new GravitySystem());
physicsGroup.AddSystem(new CollisionSystem());
physicsGroup.AddSystem(new IntegrationSystem());

context.AddSystemGroup(physicsGroup, SystemPhase.FixedUpdate, order: 0);
```

## Plugin Patterns

### Configuration Plugin

```csharp
public class DebugPlugin : IWorldPlugin
{
    private readonly DebugConfig config;

    public string Name => "Debug";

    public DebugPlugin(DebugConfig config) => this.config = config;

    public void Install(PluginContext context)
    {
        if (config.ShowStats)
        {
            context.AddSystem<StatsSystem>(SystemPhase.PostRender);
        }

        if (config.ShowColliders)
        {
            context.AddSystem<ColliderRenderSystem>(SystemPhase.Render);
        }

        context.SetExtension(new DebugStats());
    }

    public void Uninstall(PluginContext context)
    {
        context.RemoveExtension<DebugStats>();
    }
}

// Usage
var debug = new DebugPlugin(new DebugConfig
{
    ShowStats = true,
    ShowColliders = false
});
world.InstallPlugin(debug);
```

### Feature Plugin

```csharp
public class CombatPlugin : IWorldPlugin
{
    public string Name => "Combat";

    public void Install(PluginContext context)
    {
        // Create system group for ordered execution
        var combatGroup = new SystemGroup("Combat");
        combatGroup.AddSystem(new TargetingSystem());
        combatGroup.AddSystem(new AttackSystem());
        combatGroup.AddSystem(new DamageSystem());
        combatGroup.AddSystem(new DeathSystem());

        context.AddSystemGroup(combatGroup, SystemPhase.Update, order: 100);
    }

    public void Uninstall(PluginContext context)
    {
        // Systems auto-cleaned
    }
}
```

### Integration Plugin

```csharp
public class NetworkPlugin : IWorldPlugin
{
    private readonly NetworkConfig config;
    private NetworkManager? manager;

    public string Name => "Network";

    public NetworkPlugin(NetworkConfig config) => this.config = config;

    public void Install(PluginContext context)
    {
        manager = new NetworkManager(config);

        context.AddSystem(new NetworkSyncSystem(manager), SystemPhase.LateUpdate);
        context.AddSystem(new NetworkReceiveSystem(manager), SystemPhase.EarlyUpdate);

        context.SetExtension(manager);
    }

    public void Uninstall(PluginContext context)
    {
        context.RemoveExtension<NetworkManager>();
        manager?.Dispose();
        manager = null;
    }
}
```

## World Isolation

Each world has completely isolated plugins:

```csharp
using var world1 = new World();
using var world2 = new World();

world1.InstallPlugin<PhysicsPlugin>();
world2.InstallPlugin<AudioPlugin>();

// world1 has Physics, world2 has Audio
// No cross-contamination
```

## Lifecycle

1. **World created** - Empty plugin registry
2. **InstallPlugin()** - Plugin's `Install()` called
3. **World.Update()** - Plugin systems run normally
4. **UninstallPlugin()** - Plugin's `Uninstall()` called, systems removed
5. **World.Dispose()** - All plugins uninstalled automatically

## Best Practices

### Do

- Use `PluginContext.AddSystem()` so systems are auto-tracked
- Clean up extensions in `Uninstall()`
- Use unique, descriptive plugin names
- Provide configuration via constructor parameters
- Document what extensions your plugin provides

### Don't

- Register systems directly on `World` from plugins
- Leave extensions registered after uninstall
- Use static state within plugins
- Assume plugin installation order

## Next Steps

- [Systems Guide](systems.md) - System design patterns
- [Messaging Guide](messaging.md) - Inter-system communication
- [Events Guide](events.md) - Component and entity lifecycle events
