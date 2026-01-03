# Game Prototypes: Editor Integration

This document describes how to implement the three game prototypes using the KeenEyes editor and `.kescene` format.

---

## Table of Contents

1. [Scene Format Overview](#scene-format-overview)
2. [Project Structure](#project-structure)
3. [Bullet Hell: Editor Workflow](#bullet-hell-editor-workflow)
4. [Tower Defense: Editor Workflow](#tower-defense-editor-workflow)
5. [Roguelike: Editor Workflow](#roguelike-editor-workflow)
6. [Source Generation](#source-generation)
7. [Editor Features by Prototype](#editor-features-by-prototype)

---

## Scene Format Overview

Per [ADR-011](../adr/011-unified-scene-model.md), KeenEyes uses a **unified scene model** where everything is a scene:

| Usage | Traditional Name | KeenEyes | File Extension |
|-------|-----------------|----------|----------------|
| Level/Map | Scene | Scene | `.kescene` |
| Reusable template | Prefab | Scene | `.kescene` |
| World config | Settings | World | `.keworld` |

**Key insight**: A "Player prefab" and a "Main level" are both `.kescene` files. The difference is how you use them:
- **Prefab usage**: `world.Scenes.Spawn("Player")` - instantiate multiple times
- **Level usage**: `world.Scenes.Spawn("ForestLevel")` - load as current level

### Scene File Format

```json
{
  "$schema": "../../schemas/kescene.schema.json",
  "name": "Player",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Player",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Health": { "Current": 100, "Max": 100 }
      }
    },
    {
      "id": "hitbox",
      "name": "Hitbox",
      "parent": "root",
      "components": {
        "CircleCollider": { "Radius": 4 }
      }
    }
  ]
}
```

---

## Project Structure

Each prototype follows this structure:

```
MyGame/
├── MyGame.csproj                    # SDK-style project
├── Scenes/
│   ├── Levels/                      # Level scenes
│   │   ├── Main.kescene
│   │   ├── Stage1.kescene
│   │   └── ...
│   └── Prefabs/                     # Reusable entity scenes
│       ├── Player.kescene
│       ├── Enemy.kescene
│       └── ...
├── Assets/
│   ├── Sprites/
│   ├── Audio/
│   └── ...
├── Components/                      # Custom components
│   ├── GameComponents.cs
│   └── ...
├── Systems/                         # Custom systems
│   ├── MovementSystem.cs
│   └── ...
└── Worlds/
    └── Default.keworld              # World configuration
```

---

## Bullet Hell: Editor Workflow

### Prefab Scenes

#### `Scenes/Prefabs/Player.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "Player",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Player",
      "components": {
        "Position": { "X": 400, "Y": 500 },
        "Velocity": { "X": 0, "Y": 0 },
        "CircleCollider": { "Radius": 4 },
        "Player": {
          "Lives": 3,
          "InvincibilityTimer": 0,
          "Score": 0,
          "FireCooldown": 0
        },
        "Sprite": { "TextureId": "player", "Layer": 10 },
        "Tint": { "R": 0.3, "G": 0.8, "B": 1.0, "A": 1.0 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Turret.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "Turret",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Turret",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Enemy": { "Health": 50, "MaxHealth": 50, "PointValue": 100 },
        "BulletEmitter": {
          "Pattern": "Aimed",
          "FireRate": 2.0,
          "Cooldown": 0,
          "Phase": 0
        },
        "CircleCollider": { "Radius": 20 },
        "Sprite": { "TextureId": "turret", "Layer": 5 },
        "Tint": { "R": 1.0, "G": 0.3, "B": 0.3, "A": 1.0 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Spinner.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "Spinner",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Spinner",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Enemy": { "Health": 30, "MaxHealth": 30, "PointValue": 150 },
        "BulletEmitter": {
          "Pattern": "Spiral",
          "FireRate": 10.0,
          "Cooldown": 0,
          "Phase": 0
        },
        "CircleCollider": { "Radius": 15 },
        "Sprite": { "TextureId": "spinner", "Layer": 5 },
        "Tint": { "R": 0.8, "G": 0.3, "B": 1.0, "A": 1.0 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Bullets/BasicBullet.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "BasicBullet",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Bullet",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Velocity": { "X": 0, "Y": 0 },
        "Bullet": { "Lifetime": 5.0, "Damage": 1, "Size": 1.0 },
        "CircleCollider": { "Radius": 4 },
        "Tint": { "R": 1.0, "G": 0.3, "B": 0.3, "A": 1.0 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Bullets/HomingBullet.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "HomingBullet",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "HomingBullet",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Velocity": { "X": 0, "Y": 0 },
        "Bullet": { "Lifetime": 8.0, "Damage": 2, "Size": 1.2 },
        "Homing": { "TurnSpeed": 2.0 },
        "CircleCollider": { "Radius": 5 },
        "Tint": { "R": 1.0, "G": 0.8, "B": 0.2, "A": 1.0 }
      }
    }
  ]
}
```

### Level Scene

#### `Scenes/Levels/Stage1.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "Stage1",
  "version": 1,
  "entities": [
    {
      "id": "game_manager",
      "name": "GameManager",
      "components": {
        "WaveManager": { "CurrentWave": 1, "TimeBetweenWaves": 3.0 }
      }
    },
    {
      "id": "turret_1",
      "name": "Turret1",
      "components": {
        "Position": { "X": 200, "Y": 100 },
        "SceneRef": { "SceneName": "Turret" }
      }
    },
    {
      "id": "turret_2",
      "name": "Turret2",
      "components": {
        "Position": { "X": 600, "Y": 100 },
        "SceneRef": { "SceneName": "Turret" }
      }
    },
    {
      "id": "spinner_1",
      "name": "Spinner1",
      "components": {
        "Position": { "X": 400, "Y": 150 },
        "SceneRef": { "SceneName": "Spinner" }
      }
    }
  ]
}
```

### World Configuration

#### `Worlds/BulletHell.keworld`

```json
{
  "$schema": "../../schemas/keworld.schema.json",
  "name": "BulletHell",
  "version": 1,
  "settings": {
    "fixedTimeStep": 0.016667,
    "targetFps": 60
  },
  "plugins": ["Graphics2D", "Input", "Audio"],
  "systems": {
    "update": [
      "PlayerInputSystem",
      "EnemyAISystem",
      "BulletEmitterSystem",
      "HomingSystem",
      "AcceleratingSystem",
      "WavySystem",
      "MovementSystem",
      "ScreenBoundsSystem",
      "LifetimeSystem",
      "CollisionSystem",
      "DamageSystem",
      "DeathSystem",
      "ScoreSystem",
      "WaveSystem"
    ],
    "render": [
      "BackgroundRenderSystem",
      "SpriteRenderSystem",
      "GlowRenderSystem",
      "UIRenderSystem"
    ]
  }
}
```

### Editor Workflow

1. **Create prefabs** in `Scenes/Prefabs/` using the Inspector panel
2. **Design levels** by spawning prefab references with position overrides
3. **Configure bullet patterns** via BulletEmitter component in Inspector
4. **Test in editor** using Play Mode with time controls
5. **Profile** using the Profiler panel (target: 10k bullets @ 60fps)

### Key Editor Features Used

| Feature | Bullet Hell Usage |
|---------|-------------------|
| **Prefab spawning** | Bullets, enemies, player |
| **Component Inspector** | Tune fire rates, patterns, speeds |
| **Play Mode** | Test patterns in real-time |
| **Profiler** | Ensure 60fps with 10k entities |
| **Timeline (Replay)** | Debug bullet pattern timing |

---

## Tower Defense: Editor Workflow

### Prefab Scenes

#### `Scenes/Prefabs/Towers/ArrowTower.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "ArrowTower",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "ArrowTower",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Targeting": { "Range": 150, "Mode": "First" },
        "DamageDealer": { "Damage": 10, "AttackSpeed": 2.0, "Cooldown": 0 },
        "ProjectileFirer": { "ProjectilePrefab": "Arrow", "ProjectileSpeed": 400 },
        "Upgradeable": { "Level": 1, "UpgradeCost": 75 },
        "Sprite": { "TextureId": "tower_arrow", "Layer": 5 }
      }
    },
    {
      "id": "range_indicator",
      "name": "RangeIndicator",
      "parent": "root",
      "components": {
        "CircleRenderer": { "Radius": 150, "Color": { "R": 0.5, "G": 0.5, "B": 1.0, "A": 0.2 } },
        "EditorOnly": {}
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Towers/FrostTower.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "FrostTower",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "FrostTower",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Targeting": { "Range": 120, "Mode": "First" },
        "DamageDealer": { "Damage": 5, "AttackSpeed": 1.0, "Cooldown": 0 },
        "ProjectileFirer": { "ProjectilePrefab": "IceShard", "ProjectileSpeed": 300 },
        "SlowAura": { "Radius": 120, "SlowAmount": 0.5 },
        "Upgradeable": { "Level": 1, "UpgradeCost": 100 },
        "Sprite": { "TextureId": "tower_frost", "Layer": 5 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Enemies/Grunt.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "Grunt",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Grunt",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Health": { "Current": 50, "Max": 50 },
        "MoveSpeed": { "Base": 50, "Multiplier": 1.0 },
        "PathFollower": { "WaypointIndex": 0, "Progress": 0, "PathId": 0 },
        "Bounty": { "Gold": 10 },
        "LivesCost": { "Lives": 1 },
        "Sprite": { "TextureId": "enemy_grunt", "Layer": 6 }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Enemies/FlyingEnemy.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "FlyingEnemy",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "FlyingEnemy",
      "components": {
        "Position": { "X": 0, "Y": 0 },
        "Health": { "Current": 60, "Max": 60 },
        "MoveSpeed": { "Base": 40, "Multiplier": 1.0 },
        "PathFollower": { "WaypointIndex": 0, "Progress": 0, "PathId": 0 },
        "Bounty": { "Gold": 20 },
        "LivesCost": { "Lives": 2 },
        "Sprite": { "TextureId": "enemy_flying", "Layer": 8 }
      }
    }
  ]
}
```

### Level Scene with Path Data

#### `Scenes/Levels/Map1.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "Map1",
  "version": 1,
  "entities": [
    {
      "id": "game_manager",
      "name": "GameManager",
      "components": {
        "GameState": { "Gold": 200, "Lives": 20, "Wave": 0 },
        "WaveConfig": {
          "Waves": [
            { "Enemies": [{ "Type": "Grunt", "Count": 10, "Delay": 0.5 }] },
            { "Enemies": [{ "Type": "Grunt", "Count": 15, "Delay": 0.4 }] },
            { "Enemies": [
              { "Type": "Grunt", "Count": 10, "Delay": 0.5 },
              { "Type": "FlyingEnemy", "Count": 3, "Delay": 1.0 }
            ]}
          ]
        }
      }
    },
    {
      "id": "path_0",
      "name": "MainPath",
      "components": {
        "PathData": {
          "Id": 0,
          "Waypoints": [
            { "X": 0, "Y": 300 },
            { "X": 200, "Y": 300 },
            { "X": 200, "Y": 100 },
            { "X": 500, "Y": 100 },
            { "X": 500, "Y": 400 },
            { "X": 800, "Y": 400 }
          ]
        }
      }
    },
    {
      "id": "build_zone_1",
      "name": "BuildZone1",
      "components": {
        "Position": { "X": 100, "Y": 200 },
        "BuildableZone": { "Width": 50, "Height": 50 }
      }
    },
    {
      "id": "build_zone_2",
      "name": "BuildZone2",
      "components": {
        "Position": { "X": 350, "Y": 200 },
        "BuildableZone": { "Width": 50, "Height": 50 }
      }
    },
    {
      "id": "spawn_point",
      "name": "EnemySpawn",
      "components": {
        "Position": { "X": 0, "Y": 300 },
        "SpawnPoint": { "PathId": 0 }
      }
    },
    {
      "id": "goal",
      "name": "Goal",
      "components": {
        "Position": { "X": 800, "Y": 400 },
        "Goal": {}
      }
    }
  ]
}
```

### Editor Workflow

1. **Design the map** using the Viewport to place path waypoints
2. **Create build zones** as entities with BuildableZone components
3. **Configure waves** in the WaveConfig component via Inspector
4. **Create tower prefabs** with component composition
5. **Test balance** using Play Mode with wave controls
6. **Use Replay** to debug enemy pathing issues

### Custom Editor Tools (Future)

| Tool | Purpose |
|------|---------|
| **Path Editor** | Visual waypoint placement with bezier curves |
| **Wave Designer** | Timeline-based wave composition |
| **Tower Placement Preview** | Show range circles before placing |
| **Balance Analyzer** | Calculate DPS vs HP curves |

---

## Roguelike: Editor Workflow

### Prefab Scenes

#### `Scenes/Prefabs/Player.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "RoguePlayer",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Player",
      "components": {
        "GridPosition": { "X": 0, "Y": 0 },
        "Named": { "Name": "Hero" },
        "Renderable": { "Glyph": "@", "ForegroundColor": { "R": 1, "G": 1, "B": 1, "A": 1 }, "Layer": 10 },
        "Health": { "Current": 100, "Max": 100 },
        "CombatStats": { "Attack": 10, "Defense": 5, "Speed": 10, "Accuracy": 80, "Evasion": 10 },
        "Experience": { "Level": 1, "Current": 0, "ToNextLevel": 100 },
        "TurnActor": { "Energy": 0, "EnergyPerTick": 10 },
        "FieldOfView": { "Range": 8, "IsDirty": true }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Enemies/Goblin.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "Goblin",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Goblin",
      "components": {
        "GridPosition": { "X": 0, "Y": 0 },
        "Named": { "Name": "Goblin" },
        "Renderable": { "Glyph": "g", "ForegroundColor": { "R": 0.2, "G": 0.8, "B": 0.2, "A": 1 }, "Layer": 5 },
        "Health": { "Current": 25, "Max": 25 },
        "CombatStats": { "Attack": 5, "Defense": 2, "Speed": 8, "Accuracy": 70, "Evasion": 5 },
        "TurnActor": { "Energy": 0, "EnergyPerTick": 8 },
        "AIBehavior": { "Type": "Aggressive" },
        "ExperienceReward": { "Amount": 15 },
        "FieldOfView": { "Range": 6, "IsDirty": true }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Items/IronSword.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "IronSword",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Iron Sword",
      "components": {
        "GridPosition": { "X": 0, "Y": 0 },
        "Named": { "Name": "Iron Sword" },
        "Renderable": { "Glyph": "/", "ForegroundColor": { "R": 0.7, "G": 0.7, "B": 0.7, "A": 1 }, "Layer": 2 },
        "Equipable": { "Slot": "MainHand" },
        "MeleeDamage": { "MinDamage": 3, "MaxDamage": 8 },
        "Durability": { "Current": 50, "Max": 50 },
        "Value": { "Gold": 50 },
        "Rarity": { "Tier": "Common" }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Items/FlamingSword.kescene` (Composed Item)

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "FlamingSword",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Flaming Sword",
      "components": {
        "GridPosition": { "X": 0, "Y": 0 },
        "Named": { "Name": "Flaming Sword" },
        "Renderable": { "Glyph": "/", "ForegroundColor": { "R": 1.0, "G": 0.5, "B": 0.1, "A": 1 }, "Layer": 2 },
        "Equipable": { "Slot": "MainHand" },
        "MeleeDamage": { "MinDamage": 4, "MaxDamage": 10 },
        "ElementalDamage": { "Type": "Fire", "MinDamage": 2, "MaxDamage": 5 },
        "OnHitEffect": { "Effect": "Burning", "Chance": 30, "Duration": 3, "Potency": 2 },
        "Durability": { "Current": 40, "Max": 40 },
        "Value": { "Gold": 200 },
        "Rarity": { "Tier": "Rare" }
      }
    }
  ]
}
```

#### `Scenes/Prefabs/Items/HealthPotion.kescene`

```json
{
  "$schema": "../../../../schemas/kescene.schema.json",
  "name": "HealthPotion",
  "version": 1,
  "entities": [
    {
      "id": "root",
      "name": "Health Potion",
      "components": {
        "GridPosition": { "X": 0, "Y": 0 },
        "Named": { "Name": "Health Potion" },
        "Renderable": { "Glyph": "!", "ForegroundColor": { "R": 1.0, "G": 0.2, "B": 0.2, "A": 1 }, "Layer": 2 },
        "HealsOnUse": { "Amount": 25 },
        "Value": { "Gold": 30 },
        "Rarity": { "Tier": "Common" }
      }
    }
  ]
}
```

### Dungeon Templates

#### `Scenes/Templates/DungeonRoom_Start.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "DungeonRoomStart",
  "version": 1,
  "entities": [
    {
      "id": "room",
      "name": "StartingRoom",
      "components": {
        "RoomTemplate": {
          "Width": 10,
          "Height": 8,
          "Type": "Start",
          "FixedSpawns": [
            { "X": 5, "Y": 4, "Scene": "RoguePlayer" },
            { "X": 8, "Y": 4, "Scene": "StairsDown" }
          ]
        }
      }
    }
  ]
}
```

#### `Scenes/Templates/DungeonRoom_Combat.kescene`

```json
{
  "$schema": "../../../schemas/kescene.schema.json",
  "name": "DungeonRoomCombat",
  "version": 1,
  "entities": [
    {
      "id": "room",
      "name": "CombatRoom",
      "components": {
        "RoomTemplate": {
          "Width": 12,
          "Height": 10,
          "Type": "Combat",
          "EnemyBudget": { "Min": 2, "Max": 4, "ScaleWithFloor": true },
          "LootChance": 0.3
        }
      }
    }
  ]
}
```

### Editor Workflow

1. **Create item prefabs** by composing damage/effect components
2. **Design enemy archetypes** with appropriate stats/AI
3. **Build room templates** with spawn rules
4. **Configure loot tables** in room templates
5. **Test via Play Mode** with step-by-step turn execution
6. **Use Replay** to debug combat sequences

### Item Composition in Inspector

The Inspector allows creating complex items through component composition:

```
┌─────────────────────────────────────┐
│ Flaming Sword                  [⋮]  │
├─────────────────────────────────────┤
│ ▼ Named                        [x]  │
│   Name: [Flaming Sword      ]       │
├─────────────────────────────────────┤
│ ▼ Equipable                    [x]  │
│   Slot: [MainHand ▾]                │
├─────────────────────────────────────┤
│ ▼ MeleeDamage                  [x]  │
│   Min Damage: [  4  ]               │
│   Max Damage: [ 10  ]               │
├─────────────────────────────────────┤
│ ▼ ElementalDamage              [x]  │
│   Type: [Fire ▾]                    │
│   Min Damage: [  2  ]               │
│   Max Damage: [  5  ]               │
├─────────────────────────────────────┤
│ ▼ OnHitEffect                  [x]  │
│   Effect: [Burning ▾]               │
│   Chance: [ 30  ] %                 │
│   Duration: [  3  ] turns           │
│   Potency: [  2  ]                  │
├─────────────────────────────────────┤
│ ▼ Rarity                       [x]  │
│   Tier: [Rare ▾]                    │
├─────────────────────────────────────┤
│ [+] Add Component                   │
└─────────────────────────────────────┘
```

---

## Source Generation

The `.kescene` files are compiled to C# at build time by the source generator.

### Generated Code Example

For `Scenes/Prefabs/Player.kescene`:

```csharp
// <auto-generated />
namespace BulletHellGame;

public static partial class Scenes
{
    public static IReadOnlyList<string> All { get; } = [
        "Player", "Turret", "Spinner", "BasicBullet", "HomingBullet", "Stage1"
    ];

    public static Entity SpawnPlayer(IWorld world, Vector2? position = null)
    {
        var root = world.Spawn("Player")
            .With(new Position { X = position?.X ?? 400, Y = position?.Y ?? 500 })
            .With(new Velocity { X = 0, Y = 0 })
            .With(new CircleCollider { Radius = 4 })
            .With(new Player { Lives = 3, InvincibilityTimer = 0, Score = 0, FireCooldown = 0 })
            .With(new Sprite { TextureId = "player", Layer = 10 })
            .With(new Tint { R = 0.3f, G = 0.8f, B = 1.0f, A = 1.0f })
            .WithTag<PlayerTag>()
            .Build();

        return root;
    }

    public static Entity SpawnTurret(IWorld world, Vector2? position = null)
    {
        // ... similar pattern
    }

    public static void LoadStage1(IWorld world)
    {
        // Spawns game manager, turrets, spinners at configured positions
        world.Spawn("GameManager")
            .With(new WaveManager { CurrentWave = 1, TimeBetweenWaves = 3.0f })
            .Build();

        SpawnTurret(world, new Vector2(200, 100));
        SpawnTurret(world, new Vector2(600, 100));
        SpawnSpinner(world, new Vector2(400, 150));
    }
}
```

### Usage in Game Code

```csharp
// Create world with configuration
var world = WorldConfigs.CreateBulletHell();

// Load level (spawns all entities)
Scenes.LoadStage1(world);

// Spawn player separately (for respawn logic)
var player = Scenes.SpawnPlayer(world);

// Game loop
while (running)
{
    world.Update(deltaTime);
    renderer.Render(world);
}
```

---

## Editor Features by Prototype

| Feature | Bullet Hell | Tower Defense | Roguelike |
|---------|-------------|---------------|-----------|
| **Prefab spawning** | ★★★ Bullets, enemies | ★★★ Towers, enemies | ★★★ Items, enemies |
| **Hierarchy panel** | ★☆☆ Flat structure | ★★☆ Tower children | ★★★ Inventory trees |
| **Inspector** | ★★★ Pattern tuning | ★★★ Tower stats | ★★★ Item composition |
| **Viewport** | ★★★ 2D patterns | ★★★ Path editing | ★☆☆ ASCII-based |
| **Play Mode** | ★★★ Real-time test | ★★★ Wave testing | ★★★ Turn stepping |
| **Replay** | ★★☆ Pattern debug | ★★☆ Path debug | ★★★ Combat replay |
| **Profiler** | ★★★ Performance | ★☆☆ Less critical | ★☆☆ Turn-based |
| **Custom tools** | Pattern designer | Path/wave editor | Room template editor |

---

## Next Steps

1. **Phase 1**: Implement components in `KeenEyes.Core` or game project
2. **Phase 2**: Create scene files for prefabs
3. **Phase 3**: Test in editor with Play Mode
4. **Phase 4**: Profile and optimize
5. **Phase 5**: Add custom editor tools if needed

Each prototype demonstrates different ECS patterns:
- **Bullet Hell**: High entity count, bulk processing
- **Tower Defense**: Query filtering, component composition
- **Roguelike**: Deep composition, runtime modification
