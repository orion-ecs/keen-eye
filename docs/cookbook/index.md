# Cookbook

The KeenEyes Cookbook provides practical, copy-paste-ready recipes for common game development patterns. Each recipe is self-contained and demonstrates best practices.

## How to Use This Cookbook

Each recipe follows a consistent structure:

1. **Problem** - What you're trying to solve
2. **Solution** - The recommended approach with complete code
3. **Why This Works** - Explanation of the design choices
4. **Variations** - Common modifications and alternatives

## Getting Started Recipes

Start here if you're new to KeenEyes:

| Recipe | Description |
|--------|-------------|
| [Basic Movement System](basic-movement.md) | Move entities with velocity and handle acceleration |
| [Health & Damage](health-damage.md) | Implement health, damage, healing, and death |
| [Entity Spawning Patterns](entity-spawning.md) | Create entities efficiently at runtime |

## Game Patterns

Common patterns for game logic:

| Recipe | Description |
|--------|-------------|
| [State Machine Entities](state-machines.md) | Implement FSM behavior for AI and game objects |
| [Inventory System](inventory-system.md) | Item management with components |
| [Timers & Cooldowns](timers-cooldowns.md) | Time-based mechanics like abilities and buffs |

## Performance Patterns

Optimize your ECS code:

| Recipe | Description |
|--------|-------------|
| [Entity Pooling](entity-pooling.md) | Reuse entities to avoid allocation |
| [Spatial Queries](spatial-queries.md) | Efficient proximity and collision checks |
| [Batch Operations](batch-operations.md) | Bulk create, modify, and destroy entities |

## Integration Patterns

Connect KeenEyes with external systems:

| Recipe | Description |
|--------|-------------|
| [Physics Integration](physics-integration.md) | Sync ECS with physics engines |
| [Input Handling](input-handling.md) | Process player input cleanly |
| [Scene Management](scene-management.md) | Load, unload, and transition between scenes |

## Contributing Recipes

Have a pattern that helped you? Recipes should:

- Solve a real, common problem
- Be complete and runnable
- Follow KeenEyes conventions
- Explain why, not just what
