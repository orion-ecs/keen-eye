# KeenEyes Documentation

[![Build and Test](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml)
[![Coverage Status](https://coveralls.io/repos/github/orion-ecs/keen-eye/badge.svg)](https://coveralls.io/github/orion-ecs/keen-eye)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Welcome to the KeenEyes ECS framework documentation.

## What is KeenEyes?

KeenEyes is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Quick Start

```csharp
using KeenEyes.Core;

// Create a world
using var world = new World();

// Create an entity with components using the fluent builder
var entity = world.Spawn()
    .With(new Position { X = 10, Y = 20 })
    .With(new Velocity { X = 1, Y = 0 })
    .Build();

// Query and process entities
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);
    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

## Key Features

- **No Static State** - All state is instance-based. Each `World` is completely isolated.
- **Components are Structs** - Cache-friendly, value semantics for optimal performance.
- **Entities are IDs** - Lightweight `(int Id, int Version)` tuples for staleness detection.
- **Fluent Queries** - `world.Query<A, B>().With<C>().Without<D>()`
- **Source Generators** - Reduce boilerplate while maintaining performance.
- **Parallel Execution** - Automatic system batching and job system for multi-threaded processing.
- **Native AOT Compatible** - No reflection in production code.

## Documentation Sections

### Learn

| Section | Description |
|---------|-------------|
| [Getting Started](getting-started.md) | Build your first ECS application step-by-step |
| [Core Concepts](concepts.md) | Understand ECS fundamentals: World, Entity, Component, System |
| [Cookbook](cookbook/index.md) | Practical recipes for common game patterns |

### Reference

| Section | Description |
|---------|-------------|
| [Features](#features-guide) | Complete guides for all KeenEyes features |
| [Libraries](#libraries) | Package documentation: Abstractions, Common, Spatial, Graphics |
| [API Reference](../api/index.md) | Auto-generated API documentation |

### Understand

| Section | Description |
|---------|-------------|
| [Design Philosophy](philosophy/index.md) | Why KeenEyes is designed the way it is |
| [Architecture Decisions](#architecture-decisions) | Detailed ADRs explaining key decisions |
| [Research](research/index.md) | Technical research for planned features |

### Help

| Section | Description |
|---------|-------------|
| [Troubleshooting](troubleshooting.md) | Common issues and solutions |

## Cookbook Highlights

Jump straight to practical examples:

| Recipe | What You'll Learn |
|--------|-------------------|
| [Basic Movement](cookbook/basic-movement.md) | Position, velocity, acceleration, delta-time |
| [Health & Damage](cookbook/health-damage.md) | Damage events, healing, death, invulnerability |
| [State Machines](cookbook/state-machines.md) | AI states, transitions, behavior systems |
| [Entity Pooling](cookbook/entity-pooling.md) | Reuse entities to avoid allocation |
| [Physics Integration](cookbook/physics-integration.md) | Sync ECS with physics engines |
| [Input Handling](cookbook/input-handling.md) | Keyboard, mouse, gamepad, action mapping |
| [Scene Management](cookbook/scene-management.md) | Load, unload, and transition between scenes |

## Design Philosophy

KeenEyes makes deliberate choices that differ from many game engines:

- **[Explicit over implicit](philosophy/index.md)** - No magic auto-registration
- **[Composition over inheritance](philosophy/why-ecs.md)** - Build entities from small components
- **[Data-oriented design](philosophy/why-ecs.md)** - Cache-friendly memory layouts
- **[No hidden dependencies](philosophy/no-static-state.md)** - Every dependency is visible
- **[Source generators over reflection](philosophy/source-generators.md)** - Compile-time code generation
- **[Native AOT compatible](philosophy/native-aot.md)** - Works with ahead-of-time compilation

## Modular Architecture

KeenEyes is designed as a **fully-featured game engine** that is also **completely customizable**. Rather than a monolithic framework, KeenEyes uses a layered architecture with clear abstraction boundaries.

```
┌─────────────────────────────────────────────────────────┐
│                    Your Game                            │
├─────────────────────────────────────────────────────────┤
│  KeenEyes.Graphics   │  KeenEyes.Audio   │  Physics     │
│  (Silk.NET OpenGL)   │  (OpenAL)         │  (Your Pick) │
├──────────────────────┴───────────────────┴──────────────┤
│              KeenEyes.Core (ECS Runtime)                │
├─────────────────────────────────────────────────────────┤
│           KeenEyes.Abstractions (Interfaces)            │
└─────────────────────────────────────────────────────────┘
```

| Package | Purpose |
|---------|---------|
| **KeenEyes.Abstractions** | Core interfaces (`IWorld`, `ISystem`, `IComponent`) - no implementation dependencies |
| **KeenEyes.Core** | Full ECS runtime with archetype storage, queries, and system execution |
| **KeenEyes.Graphics** | OpenGL/Vulkan rendering via Silk.NET |
| **KeenEyes.Audio** | Spatial audio via OpenAL |
| **KeenEyes.Spatial** | Transform components and spatial partitioning |

### Swap Any Subsystem

Don't like our physics implementation? Use your own. Prefer FMOD over OpenAL? Swap it out:

```csharp
// The default setup - everything works out of the box
using var world = new WorldBuilder()
    .WithPlugin<SilkGraphicsPlugin>()     // Built-in OpenGL rendering
    .WithPlugin<OpenALAudioPlugin>()      // Built-in audio
    .Build();

// Or bring your own implementations
using var world = new WorldBuilder()
    .WithPlugin<MyCustomRendererPlugin>()  // Your Vulkan renderer
    .WithPlugin<FmodAudioPlugin>()         // Third-party audio
    .WithPlugin<BulletPhysicsPlugin>()     // Your physics choice
    .Build();
```

## Features Guide

### Entity Features
- [Entities](entities.md) - Entity lifecycle and management
- [Bundles](bundles.md) - Group related components
- [Prefabs](prefabs.md) - Reusable entity templates
- [Relationships](relationships.md) - Parent-child entity hierarchies
- [String Tags](string-tags.md) - Runtime-flexible entity tagging

### Component Features
- [Components](components.md) - Component patterns and best practices
- [Change Tracking](change-tracking.md) - Track component modifications
- [Component Validation](component-validation.md) - Enforce component constraints
- [Events](events.md) - Component and entity lifecycle events

### System Features
- [Systems](systems.md) - System design patterns
- [Queries](queries.md) - Filtering and iterating entities
- [Command Buffer](command-buffer.md) - Safe entity modification during iteration
- [Messaging](messaging.md) - Inter-system communication patterns
- [Parallelism](parallelism.md) - Parallel system execution and job system

### World Features
- [Singletons](singletons.md) - World-level resources
- [Serialization](serialization.md) - Save and restore world state
- [Plugins](plugins.md) - Modular extensions and feature packaging
- [Logging](logging.md) - Pluggable logging system

### UI Features
- [UI System](ui.md) - Retained-mode UI with ECS entities
- Widget Factory - Pre-built widgets (buttons, panels, sliders, etc.)
- Anchor-based Layout - Responsive positioning system
- Flexbox Containers - Automatic child arrangement

## Libraries

- [Abstractions](abstractions.md) - Lightweight interfaces for plugin development
- [Common](common.md) - Shared utilities (float extensions, velocity components)
- [Spatial](spatial.md) - 3D transform components with System.Numerics
  - [Getting Started with Spatial Partitioning](spatial-partitioning/getting-started.md)
  - [Strategy Selection Guide](spatial-partitioning/strategy-selection.md)
  - [Performance Tuning](spatial-partitioning/performance-tuning.md)
- [Graphics](graphics.md) - OpenGL/Vulkan rendering with Silk.NET
- [Input](input.md) - Keyboard, mouse, and gamepad input handling
- [UI](ui.md) - ECS-based retained-mode UI system

## Architecture Decisions

Key design decisions are documented as Architecture Decision Records:

- [ADR-001: World Manager Architecture](adr/001-world-manager-architecture.md) - Refactoring World into specialized managers
- [ADR-002: Complete IWorld Event System](adr/002-iworld-entity-lifecycle-events.md) - Entity lifecycle events
- [ADR-003: CommandBuffer Abstraction](adr/003-command-buffer-abstraction.md) - Plugin isolation (20-50x performance boost)
- [ADR-004: Reflection Elimination](adr/004-reflection-elimination.md) - Native AOT compatibility

## Research

Technical research reports for planned features:

### Planned Systems
- [Graphics & Input Abstraction](research/graphics-input-abstraction.md)
- [UI System](research/ui-system.md)
- [Audio System](research/audio-system.md)
- [Animation System](research/animation-system.md)
- [AI System](research/ai-system.md)
- [Particle System](research/particle-system.md)

### Foundation Research
- [OpenGL C# Bindings](research/opengl-csharp-bindings.md)
- [Shader Management](research/shader-management.md)
- [Windowing & Input](research/windowing-input.md)
- [Cross-Platform Deployment](research/cross-platform-deployment.md)

## Getting Help

- [Troubleshooting](troubleshooting.md) - Common issues and solutions
- [GitHub Issues](https://github.com/orion-ecs/keen-eye/issues) - Bug reports and feature requests

## API Reference

See the [API Documentation](../api/index.md) for detailed reference of all public types.
