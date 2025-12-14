# KeenEyes Documentation

[![Build and Test](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/orion-ecs/keen-eye/actions/workflows/build.yml)
[![Coverage Status](https://coveralls.io/repos/github/orion-ecs/keen-eye/badge.svg)](https://coveralls.io/github/orion-ecs/keen-eye)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Welcome to the KeenEyes ECS framework documentation.

## What is KeenEyes?

KeenEyes is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Key Features

- **No Static State** - All state is instance-based. Each `World` is completely isolated.
- **Components are Structs** - Cache-friendly, value semantics for optimal performance.
- **Entities are IDs** - Lightweight `(int Id, int Version)` tuples for staleness detection.
- **Fluent Queries** - `world.Query<A, B>().With<C>().Without<D>()`
- **Source Generators** - Reduce boilerplate while maintaining performance.
- **Parallel Execution** - Automatic system batching and job system for multi-threaded processing.

## Getting Started

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
    // Process entities with Position and Velocity
}
```

## Documentation

### Tutorials
- [Getting Started](getting-started.md) - Build your first ECS application

### Concepts
- [Core Concepts](concepts.md) - Understand ECS fundamentals

### Guides
- [Entities](entities.md) - Entity lifecycle and management
- [Components](components.md) - Component patterns and best practices
- [Queries](queries.md) - Filtering and iterating entities
- [Systems](systems.md) - System design patterns
- [Messaging](messaging.md) - Inter-system communication patterns
- [Plugins](plugins.md) - Modular extensions and feature packaging
- [Parallelism](parallelism.md) - Parallel system execution and job system
- [Command Buffer](command-buffer.md) - Safe entity modification during iteration
- [Singletons](singletons.md) - World-level resources
- [Relationships](relationships.md) - Parent-child entity hierarchies
- [Events](events.md) - Component and entity lifecycle events
- [Change Tracking](change-tracking.md) - Track component modifications
- [Prefabs](prefabs.md) - Reusable entity templates
- [String Tags](string-tags.md) - Runtime-flexible entity tagging
- [Component Validation](component-validation.md) - Enforce component constraints
- [Serialization](serialization.md) - Save and restore world state
- [Logging](logging.md) - Pluggable logging system

### Libraries
- [Abstractions](abstractions.md) - Lightweight interfaces for plugin development
- [Spatial](spatial.md) - 3D transform components with System.Numerics
  - [Getting Started with Spatial Partitioning](spatial-partitioning/getting-started.md) - Grid, Quadtree, and Octree queries
  - [Strategy Selection Guide](spatial-partitioning/strategy-selection.md) - Choose the right spatial strategy
  - [Performance Tuning](spatial-partitioning/performance-tuning.md) - Optimize spatial queries
- [Graphics](graphics.md) - OpenGL/Vulkan rendering with Silk.NET

## Architecture Decisions

- [ADR-001: World Manager Architecture](adr/001-world-manager-architecture.md) - Refactoring World into specialized managers
- [ADR-002: Complete IWorld Event System](adr/002-iworld-entity-lifecycle-events.md) - Entity lifecycle events and complete IWorld interface
- [ADR-003: CommandBuffer Abstraction](adr/003-command-buffer-abstraction.md) - Plugin isolation and reflection elimination (20-50x performance boost)

## Research

Technical research reports for future features and integrations:

### Graphics & Rendering
- [OpenGL C# Bindings](research/opengl-csharp-bindings.md) - Evaluate OpenGL binding libraries for .NET
- [Shader Management](research/shader-management.md) - Shader compilation, caching, and hot reload
- [Windowing & Input](research/windowing-input.md) - Cross-platform window and input handling

### Engine Features
- [Framework Editor](research/framework-editor.md) - Feasibility study for building an ECS editor
- [Asset Loading](research/asset-loading.md) - Asset pipeline and resource management
- [Audio Systems](research/audio-systems.md) - Audio playback and spatial sound
- [Game Math Libraries](research/game-math-libraries.md) - Vector, matrix, and math library options

### Deployment
- [Cross-Platform Deployment](research/cross-platform-deployment.md) - Publishing to Windows, Linux, macOS

## API Reference

See the [API Documentation](../api/index.md) for detailed reference.
