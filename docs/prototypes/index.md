# Game Prototypes

This directory contains design documents for game prototypes that demonstrate KeenEyes ECS capabilities.

## Purpose

These prototypes serve as:
- **Validation** - Prove the engine can handle real game scenarios
- **Showcase** - Demonstrate ECS strengths to potential users
- **Learning** - Provide examples for developers new to ECS
- **Stress tests** - Exercise performance characteristics

## Prototypes

| Prototype | Focus | Entity Count | Complexity |
|-----------|-------|--------------|------------|
| [Bullet Hell](bullet-hell.md) | High spawn/despawn, bulk processing | 5,000-10,000 | Low |
| [Tower Defense](tower-defense.md) | Composition, queries, system ordering | 100-500 | Medium |
| [Roguelike](roguelike.md) | Deep composition, runtime modification | 50-200 | Medium-High |

## Editor Integration

See [Editor Integration](editor-integration.md) for how to implement these prototypes using:
- The `.kescene` file format
- Source-generated spawn methods
- Editor workflows and tools

## ECS Strengths by Prototype

### Bullet Hell

- **Bulk entity processing** - Update 10,000 bullets per frame
- **High spawn/despawn rates** - 500+ bullets spawned per second
- **Component combinations** - Homing + Accelerating + Splitting bullets
- **Efficient queries** - `With<Bullet>().Without<PlayerOwned>()`

### Tower Defense

- **Composition over inheritance** - Towers built from components
- **Query filtering** - `With<Enemy>().Without<Dead>().Without<Flying>()`
- **System ordering** - Movement → Targeting → Firing → Damage
- **Entity relationships** - Projectiles track targets

### Roguelike

- **Everything is entities** - Items, effects, abilities are all entities
- **Deep composition** - Fire Sword = MeleeDamage + ElementalDamage + OnHitEffect
- **Runtime modification** - Add/remove components to change behavior
- **Flexible queries** - Find equipped weapons, consumable healing items

## Implementation Priority

1. **Bullet Hell** - Fastest to implement, most visually impressive
2. **Tower Defense** - Good balance of features and complexity
3. **Roguelike** - Most comprehensive, best for documentation

## Technical Notes

All prototypes are designed with:
- No reflection (Native AOT compatible)
- No static state (multiple worlds possible)
- Turn-based or real-time as appropriate
- Testable systems

## Example Scene Files

The `examples/` directory contains sample `.kescene` files:

```
examples/
├── bullet-hell/
│   └── prefabs/
│       ├── Player.kescene
│       └── Turret.kescene
├── tower-defense/
│   └── prefabs/
│       └── ArrowTower.kescene
└── roguelike/
    └── prefabs/
        └── FlamingSword.kescene
```

These demonstrate component composition in the scene format.

## Related Documentation

- [ECS Concepts](../concepts.md)
- [Components](../components.md)
- [Queries](../queries.md)
- [Systems](../systems.md)
- [Unified Scene Model (ADR-011)](../adr/011-unified-scene-model.md)
- [Scene Editor Architecture](../research/scene-editor-architecture.md)
