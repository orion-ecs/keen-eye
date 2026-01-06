# AI System Guide

The AI system provides comprehensive decision-making capabilities for game entities through three complementary paradigms: Finite State Machines, Behavior Trees, and Utility AI.

## What is KeenEyes.AI?

KeenEyes.AI is a plugin that adds AI capabilities to the ECS world:

1. **Finite State Machines (FSM)** - Simple, explicit state transitions
2. **Behavior Trees (BT)** - Hierarchical, modular behaviors
3. **Utility AI** - Score-based action selection

These can be used independently or combined for complex AI behavior.

**Key Design:** AI logic is data-driven (definitions are assets), state is stored in components, and systems evaluate decisions each tick.

## When to Use Each Approach

| Approach | Best For | Complexity | Flexibility |
|----------|----------|------------|-------------|
| **FSM** | Simple NPCs, UI states, doors | Low | Limited |
| **Behavior Tree** | Complex behaviors, reusable patterns | Medium | High |
| **Utility AI** | Dynamic priorities, many options | High | Very High |

### Examples by Type

- **FSM:** Door (Open/Closed), simple enemy (Patrol/Chase/Attack), NPC conversation states
- **Behavior Tree:** Complex enemy AI, boss patterns, companion NPCs
- **Utility AI:** NPCs with needs (hunger, tiredness), tactical combat decisions, squad coordination

## Quick Start

### Installation

```csharp
using KeenEyes.AI;
using var world = new World();

// Install the AI plugin
world.InstallPlugin(new AIPlugin());
```

The plugin registers:
- `StateMachineSystem` (order 100) - Evaluates FSM state transitions
- `BehaviorTreeSystem` (order 110) - Executes behavior tree logic
- `UtilitySystem` (order 120) - Scores and selects utility actions

### Your First State Machine

```csharp
// Define the state machine
var enemyFSM = new StateMachine
{
    Name = "EnemyAI",
    States = [
        new State { Name = "Patrol", OnUpdateActions = [new PatrolAction()] },
        new State { Name = "Chase", OnUpdateActions = [new ChaseAction()] },
        new State { Name = "Attack", OnUpdateActions = [new AttackAction()] }
    ],
    Transitions = [
        new StateTransition { FromStateIndex = 0, ToStateIndex = 1, Condition = new SeePlayerCondition() },
        new StateTransition { FromStateIndex = 1, ToStateIndex = 2, Condition = new InRangeCondition() },
        new StateTransition { FromStateIndex = 1, ToStateIndex = 0, Condition = new LostPlayerCondition() },
        new StateTransition { FromStateIndex = 2, ToStateIndex = 1, Condition = new OutOfRangeCondition() }
    ],
    InitialStateIndex = 0
};

// Create an entity with the state machine
var enemy = world.Spawn()
    .With(StateMachineComponent.Create(enemyFSM))
    .Build();
```

### Your First Behavior Tree

```csharp
// Define the behavior tree
var enemyBT = new BehaviorTree
{
    Name = "EnemyBT",
    Root = new Selector
    {
        Children = [
            new Sequence { Children = [
                new ConditionNode { Condition = new InRangeCondition() },
                new ActionNode { Action = new AttackAction() }
            ]},
            new Sequence { Children = [
                new ConditionNode { Condition = new CanSeePlayerCondition() },
                new ActionNode { Action = new ChaseAction() }
            ]},
            new ActionNode { Action = new PatrolAction() }
        ]
    }
};

// Create an entity with the behavior tree
var guard = world.Spawn()
    .With(BehaviorTreeComponent.Create(enemyBT))
    .Build();
```

### Your First Utility AI

```csharp
// Define the utility brain
var guardAI = new UtilityAI
{
    Name = "Guard",
    SelectionThreshold = 0.1f,
    SelectionMode = UtilitySelectionMode.HighestScore,
    Actions = [
        new UtilityAction
        {
            Name = "Attack",
            Action = new AttackAction(),
            Considerations = [
                new Consideration { Input = new DistanceInput(), Curve = new LinearCurve { Slope = -1, YShift = 1 } }
            ]
        },
        new UtilityAction
        {
            Name = "Patrol",
            Action = new PatrolAction(),
            Weight = 0.3f // Default fallback
        }
    ]
};

// Create an entity with utility AI
var npc = world.Spawn()
    .With(UtilityComponent.Create(guardAI))
    .Build();
```

---

## Finite State Machines

State machines are ideal for simple AI behaviors with clear, discrete modes of operation.

### Core Concepts

| Type | Purpose |
|------|---------|
| `StateMachine` | Container with states and transitions |
| `State` | Named state with enter/update/exit actions |
| `StateTransition` | Condition-based transition with priority |
| `StateMachineComponent` | ECS component holding runtime state |
| `StateMachineSystem` | Evaluates transitions and executes actions |

### StateMachine Definition

```csharp
public sealed class StateMachine
{
    public string Name { get; set; }                    // For debugging
    public List<State> States { get; set; }             // Available states
    public List<StateTransition> Transitions { get; set; }  // State changes
    public int InitialStateIndex { get; set; }          // Starting state
}
```

### State Definition

```csharp
public sealed class State
{
    public string Name { get; set; }

    // Actions executed on state lifecycle
    public List<IAIAction>? OnEnterActions { get; set; }   // When entering state
    public List<IAIAction>? OnUpdateActions { get; set; }  // Each tick in state
    public List<IAIAction>? OnExitActions { get; set; }    // When leaving state
}
```

### StateTransition Definition

```csharp
public sealed class StateTransition
{
    public int FromStateIndex { get; set; }    // Source state
    public int ToStateIndex { get; set; }      // Target state
    public ICondition Condition { get; set; }  // When to transition
    public float Priority { get; set; }        // Higher = checked first
}
```

### StateMachineComponent

```csharp
// Attach to entities
var enemy = world.Spawn()
    .With(StateMachineComponent.Create(enemyFSM))
    .Build();

// Access runtime state
ref var fsm = ref world.Get<StateMachineComponent>(enemy);
Console.WriteLine($"Current state: {fsm.CurrentStateName}");
Console.WriteLine($"Time in state: {fsm.TimeInState}s");
```

### Complete FSM Example

```csharp
var doorFSM = new StateMachine
{
    Name = "Door",
    States = [
        new State { Name = "Closed" },
        new State {
            Name = "Opening",
            OnEnterActions = [new PlaySoundAction { Sound = "door_open" }]
        },
        new State { Name = "Open" },
        new State {
            Name = "Closing",
            OnEnterActions = [new PlaySoundAction { Sound = "door_close" }]
        }
    ],
    Transitions = [
        // Closed -> Opening when triggered
        new StateTransition {
            FromStateIndex = 0,
            ToStateIndex = 1,
            Condition = new BlackboardCondition { Key = "Triggered", Value = true }
        },
        // Opening -> Open after animation
        new StateTransition {
            FromStateIndex = 1,
            ToStateIndex = 2,
            Condition = new TimeInStateCondition { MinTime = 1.0f }
        },
        // Open -> Closing when not triggered
        new StateTransition {
            FromStateIndex = 2,
            ToStateIndex = 3,
            Condition = new BlackboardCondition { Key = "Triggered", Value = false }
        },
        // Closing -> Closed after animation
        new StateTransition {
            FromStateIndex = 3,
            ToStateIndex = 0,
            Condition = new TimeInStateCondition { MinTime = 1.0f }
        }
    ],
    InitialStateIndex = 0
};
```

---

## Behavior Trees

Behavior trees provide hierarchical, modular behavior composition. The tree is evaluated from the root each tick, with nodes returning Success, Failure, or Running.

### Core Concepts

| Type | Purpose |
|------|---------|
| `BehaviorTree` | Container with root node |
| `BTNode` | Base class for all nodes |
| `BTNodeState` | Return value: Success, Failure, Running |
| `BehaviorTreeComponent` | ECS component holding runtime state |
| `BehaviorTreeSystem` | Executes tree each tick |

### BTNodeState

```csharp
public enum BTNodeState
{
    Running,    // Still executing (multi-frame action)
    Success,    // Completed successfully
    Failure     // Failed
}
```

### Composite Nodes

Composite nodes have multiple children and control flow based on child results.

#### Selector (OR Logic)

Returns Success on first child success, Failure if all children fail.

```csharp
// Try attack, then chase, then patrol (first success wins)
var selector = new Selector
{
    Children = [
        new ActionNode { Action = new AttackAction() },
        new ActionNode { Action = new ChaseAction() },
        new ActionNode { Action = new PatrolAction() }  // Fallback
    ]
};
```

#### Sequence (AND Logic)

Returns Failure on first child failure, Success if all children succeed.

```csharp
// Must complete all steps in order
var sequence = new Sequence
{
    Children = [
        new ConditionNode { Condition = new InRangeCondition() },
        new ActionNode { Action = new TurnToTargetAction() },
        new ActionNode { Action = new AttackAction() }
    ]
};
```

#### Parallel

Runs all children simultaneously.

```csharp
var parallel = new Parallel
{
    SuccessPolicy = ParallelPolicy.RequireAll,  // All must succeed
    FailurePolicy = ParallelPolicy.RequireOne,  // One failure = failure
    Children = [
        new ActionNode { Action = new MoveAction() },
        new ActionNode { Action = new PlayAnimationAction() }
    ]
};
```

#### RandomSelector

Selects children based on weighted random.

```csharp
var random = new RandomSelector
{
    Children = [
        new ActionNode { Action = new TauntAction() },
        new ActionNode { Action = new GrowlAction() },
        new ActionNode { Action = new IdleAction() }
    ]
};
```

### Decorator Nodes

Decorators modify behavior of a single child.

#### Inverter

Flips Success/Failure.

```csharp
// Succeeds if enemy is NOT visible
var notVisible = new Inverter
{
    Child = new ConditionNode { Condition = new CanSeeEnemyCondition() }
};
```

#### Repeater

Loops child N times (or infinitely).

```csharp
// Attack 3 times
var repeatAttack = new Repeater
{
    Count = 3,  // -1 for infinite
    Child = new ActionNode { Action = new AttackAction() }
};
```

#### UntilFail

Repeats until child fails.

```csharp
// Keep patrolling until we see something
var patrol = new UntilFail
{
    Child = new Sequence { Children = [
        new ActionNode { Action = new MoveToNextWaypoint() },
        new Inverter { Child = new ConditionNode { Condition = new CanSeeEnemyCondition() } }
    ]}
};
```

#### Cooldown

Prevents execution for a duration after success.

```csharp
// Only attack every 2 seconds
var cooldownAttack = new Cooldown
{
    Duration = 2.0f,
    Child = new ActionNode { Action = new AttackAction() }
};
```

#### Succeeder

Always returns Success (ignores child result).

```csharp
var alwaysSucceed = new Succeeder
{
    Child = new ActionNode { Action = new OptionalAction() }
};
```

### Leaf Nodes

Leaf nodes are terminal nodes that do actual work.

#### ActionNode

Executes an `IAIAction`.

```csharp
var attack = new ActionNode { Action = new AttackAction() };
```

#### ConditionNode

Checks an `ICondition`.

```csharp
var checkRange = new ConditionNode { Condition = new InRangeCondition() };
```

#### WaitNode

Pauses execution for a duration.

```csharp
var pause = new WaitNode { Duration = 1.5f };
```

### Complete Behavior Tree Example

```csharp
var bossBT = new BehaviorTree
{
    Name = "BossAI",
    Root = new Selector
    {
        Children = [
            // Phase 1: Low health - flee and heal
            new Sequence { Children = [
                new ConditionNode { Condition = new LowHealthCondition { Threshold = 0.2f } },
                new ActionNode { Action = new FleeAction() },
                new ActionNode { Action = new HealAction() }
            ]},

            // Phase 2: Player close - melee combo
            new Sequence { Children = [
                new ConditionNode { Condition = new InRangeCondition { Range = 5f } },
                new Repeater { Count = 3, Child =
                    new ActionNode { Action = new MeleeAttackAction() }
                }
            ]},

            // Phase 3: Player medium range - ranged attack with cooldown
            new Sequence { Children = [
                new ConditionNode { Condition = new InRangeCondition { Range = 20f } },
                new Cooldown { Duration = 3f, Child =
                    new ActionNode { Action = new RangedAttackAction() }
                }
            ]},

            // Default: Move toward player
            new ActionNode { Action = new ChaseAction() }
        ]
    }
};
```

---

## Utility AI

Utility AI scores all available actions and selects the best one. Ideal for complex, dynamic behavior where the "best" action depends on multiple factors.

### Core Concepts

| Type | Purpose |
|------|---------|
| `UtilityAI` | Brain with scoreable actions |
| `UtilityAction` | Action with considerations |
| `Consideration` | Input + response curve = score |
| `ResponseCurve` | Maps input to output |
| `UtilityComponent` | ECS component |
| `UtilitySystem` | Evaluates and selects actions |

### UtilityAI Definition

```csharp
public sealed class UtilityAI
{
    public string Name { get; set; }
    public List<UtilityAction> Actions { get; set; }
    public float SelectionThreshold { get; set; }  // Minimum score to consider
    public UtilitySelectionMode SelectionMode { get; set; }
    public int TopNCount { get; set; }  // For TopN mode
}
```

### Selection Modes

| Mode | Behavior |
|------|----------|
| `HighestScore` | Always pick the highest scoring action |
| `WeightedRandom` | Probabilistic selection based on scores |
| `TopN` | Random from top N candidates |

### UtilityAction Definition

```csharp
public sealed class UtilityAction
{
    public string Name { get; set; }
    public IAIAction Action { get; set; }           // What to execute
    public List<Consideration> Considerations { get; set; }  // How to score
    public float Weight { get; set; } = 1f;         // Base score multiplier
}
```

### Considerations

A consideration combines an input (value from the world) with a response curve (how to interpret that value).

```csharp
public sealed class Consideration
{
    public string Name { get; set; }
    public IConsiderationInput Input { get; set; }  // Gets 0-1 value
    public ResponseCurve Curve { get; set; }        // Maps input to score
}
```

### Built-in Consideration Inputs

| Input | Returns |
|-------|---------|
| `DistanceInput` | Distance to target normalized to max range |
| `HealthInput` | Current health as percentage of max |
| `TimeInput` | Time since some event |
| `BlackboardInput` | Value from blackboard |

### Response Curves

Response curves map an input value (0-1) to an output score (0-1).

#### LinearCurve

```csharp
// Higher score when closer (distance=0 -> score=1)
var closerIsBetter = new LinearCurve { Slope = -1f, YShift = 1f };

// Identity (score = input)
var identity = new LinearCurve { Slope = 1f };
```

#### ExponentialCurve

```csharp
// Rapidly increasing at high input
var rapid = new ExponentialCurve { Exponent = 2f };

// Slowly increasing at high input
var slow = new ExponentialCurve { Exponent = 0.5f };
```

#### LogisticCurve (S-Curve)

```csharp
// Sharp transition around midpoint
var sCurve = new LogisticCurve
{
    Steepness = 10f,
    Midpoint = 0.5f
};
```

#### StepCurve

```csharp
// Binary: 0 below threshold, 1 above
var step = new StepCurve { Threshold = 0.3f };
```

### Complete Utility AI Example

```csharp
var simsNPC = new UtilityAI
{
    Name = "SimsNPC",
    SelectionMode = UtilitySelectionMode.HighestScore,
    SelectionThreshold = 0.1f,
    Actions = [
        // Eat when hungry
        new UtilityAction
        {
            Name = "Eat",
            Action = new EatAction(),
            Considerations = [
                new Consideration {
                    Name = "Hunger",
                    Input = new BlackboardInput { Key = "Hunger" },
                    Curve = new ExponentialCurve { Exponent = 2f }
                }
            ]
        },

        // Sleep when tired
        new UtilityAction
        {
            Name = "Sleep",
            Action = new SleepAction(),
            Considerations = [
                new Consideration {
                    Name = "Tiredness",
                    Input = new BlackboardInput { Key = "Tiredness" },
                    Curve = new LogisticCurve { Steepness = 8f, Midpoint = 0.6f }
                }
            ]
        },

        // Socialize when lonely
        new UtilityAction
        {
            Name = "Socialize",
            Action = new SocializeAction(),
            Considerations = [
                new Consideration {
                    Name = "Loneliness",
                    Input = new BlackboardInput { Key = "Loneliness" },
                    Curve = new LinearCurve { Slope = 1f }
                },
                new Consideration {
                    Name = "NotTooTired",
                    Input = new BlackboardInput { Key = "Tiredness" },
                    Curve = new LinearCurve { Slope = -1f, YShift = 1f }
                }
            ]
        },

        // Default idle
        new UtilityAction
        {
            Name = "Idle",
            Action = new IdleAction(),
            Weight = 0.2f
        }
    ]
};
```

---

## Blackboard System

The blackboard is a key-value store for sharing state between AI nodes and actions.

### Basic Usage

```csharp
var blackboard = new Blackboard();

// Set values
blackboard.Set("Target", playerEntity);
blackboard.Set("AlertLevel", 0.8f);
blackboard.Set("PatrolIndex", 3);

// Get values
var target = blackboard.Get<Entity>("Target");
var alert = blackboard.Get<float>("AlertLevel");

// Get with default
var health = blackboard.Get("Health", 100f);

// Check existence
if (blackboard.Has("Target"))
{
    // ...
}

// Try get
if (blackboard.TryGet<Entity>("Target", out var t))
{
    // ...
}

// Remove
blackboard.Remove("Target");

// Clear all
blackboard.Clear();
```

### Common Blackboard Keys

Use the `BBKeys` class for standard key names:

```csharp
using KeenEyes.AI;

// Time
blackboard.Set(BBKeys.Time, currentTime);
blackboard.Set(BBKeys.DeltaTime, deltaTime);

// Targeting
blackboard.Set(BBKeys.Target, enemyEntity);
blackboard.Set(BBKeys.TargetPosition, position);
blackboard.Set(BBKeys.TargetLastSeen, lastSeenTime);

// Self
blackboard.Set(BBKeys.Health, currentHealth);
blackboard.Set(BBKeys.Ammo, ammoCount);
blackboard.Set(BBKeys.AlertLevel, 0.5f);

// Navigation
blackboard.Set(BBKeys.Destination, destination);
blackboard.Set(BBKeys.CurrentPath, pathPoints);
blackboard.Set(BBKeys.PatrolIndex, waypointIndex);
```

---

## AIContext API

The `AIContext` extension provides debug information and AI manipulation.

### Accessing AIContext

```csharp
// Via GetExtension
var ai = world.GetExtension<AIContext>();

// Or with C# 13 extension members
var ai = world.AI;
```

### FSM Operations

```csharp
// Get current state name
string? state = ai.GetCurrentStateName(enemy);

// Force a state transition
ai.ForceStateTransition(enemy, stateIndex: 2);

// Get all FSM entities
foreach (var entity in ai.GetStateMachineEntities())
{
    // ...
}
```

### Behavior Tree Operations

```csharp
// Get last execution result
BTNodeState? result = ai.GetBehaviorTreeResult(guard);

// Get currently running node
BTNode? running = ai.GetRunningNode(guard);

// Reset tree to initial state
ai.ResetBehaviorTree(guard);

// Get all BT entities
foreach (var entity in ai.GetBehaviorTreeEntities())
{
    // ...
}
```

### Utility AI Operations

```csharp
// Get current action
UtilityAction? action = ai.GetCurrentUtilityAction(npc);

// Score all actions (for debugging)
var scores = ai.ScoreAllActions(npc);
foreach (var (act, score) in scores)
{
    Console.WriteLine($"{act.Name}: {score:F2}");
}

// Force re-evaluation next tick
ai.ForceUtilityEvaluation(npc);

// Get all Utility AI entities
foreach (var entity in ai.GetUtilityAIEntities())
{
    // ...
}
```

### Blackboard Operations

```csharp
// Get entity's blackboard
var blackboard = ai.GetBlackboard(entity);

// Set/get values through AIContext
ai.SetBlackboardValue(entity, "Target", player);
if (ai.TryGetBlackboardValue<Entity>(entity, "Target", out var target))
{
    // ...
}
```

### Statistics

```csharp
var stats = ai.GetStatistics();
Console.WriteLine($"State Machines: {stats.StateMachineCount} ({stats.ActiveStateMachineCount} active)");
Console.WriteLine($"Behavior Trees: {stats.BehaviorTreeCount} ({stats.ActiveBehaviorTreeCount} active)");
Console.WriteLine($"Utility AI: {stats.UtilityAICount} ({stats.ActiveUtilityAICount} active)");
Console.WriteLine($"Total: {stats.TotalCount} ({stats.TotalActiveCount} active)");
```

---

## Creating Custom Actions

Implement `IAIAction` to create custom actions.

### IAIAction Interface

```csharp
public interface IAIAction
{
    BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world);
}
```

### Example: MoveToTargetAction

```csharp
public class MoveToTargetAction : IAIAction
{
    public float Speed { get; set; } = 5f;
    public float ArrivalDistance { get; set; } = 1f;

    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get target from blackboard
        if (!blackboard.TryGet<Vector3>(BBKeys.TargetPosition, out var targetPos))
        {
            return BTNodeState.Failure;  // No target
        }

        ref var transform = ref world.Get<Transform3D>(entity);
        var direction = targetPos - transform.Position;
        var distance = direction.Length();

        // Arrived?
        if (distance <= ArrivalDistance)
        {
            return BTNodeState.Success;
        }

        // Move toward target
        var deltaTime = blackboard.Get<float>(BBKeys.DeltaTime);
        var movement = Vector3.Normalize(direction) * Speed * deltaTime;
        transform.Position += movement;

        return BTNodeState.Running;  // Still moving
    }
}
```

---

## Creating Custom Conditions

Implement `ICondition` to create custom conditions.

### ICondition Interface

```csharp
public interface ICondition
{
    bool Evaluate(Entity entity, Blackboard blackboard, IWorld world);
}
```

### Example: InRangeCondition

```csharp
public class InRangeCondition : ICondition
{
    public float Range { get; set; } = 10f;

    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (!blackboard.TryGet<Entity>(BBKeys.Target, out var target))
        {
            return false;
        }

        if (!world.IsAlive(target))
        {
            return false;
        }

        var myPos = world.Get<Transform3D>(entity).Position;
        var targetPos = world.Get<Transform3D>(target).Position;
        var distSq = Vector3.DistanceSquared(myPos, targetPos);

        return distSq <= Range * Range;
    }
}
```

---

## Combining AI Systems

### FSM with Behavior Trees

Use FSM for high-level state, behavior trees for complex state behavior:

```csharp
// FSM states that run behavior trees
var combatBT = new BehaviorTree { /* ... */ };
var patrolBT = new BehaviorTree { /* ... */ };

var hybridFSM = new StateMachine
{
    States = [
        new State {
            Name = "Patrol",
            OnUpdateActions = [new RunBehaviorTreeAction { Tree = patrolBT }]
        },
        new State {
            Name = "Combat",
            OnUpdateActions = [new RunBehaviorTreeAction { Tree = combatBT }]
        }
    ],
    Transitions = [/* ... */]
};
```

### Utility AI for Selection, FSM for Execution

Use Utility AI to pick high-level goals, FSM to execute them:

```csharp
public class ExecuteGoalAction : IAIAction
{
    private readonly Dictionary<string, StateMachine> goalFSMs;

    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        var goal = blackboard.Get<string>("CurrentGoal");
        if (goalFSMs.TryGetValue(goal, out var fsm))
        {
            // Execute FSM for this goal
            return ExecuteFSM(fsm, entity, blackboard, world);
        }
        return BTNodeState.Failure;
    }
}
```

---

## Performance Considerations

### Throttling AI Updates

For many AI entities, throttle evaluation frequency:

```csharp
// UtilityComponent supports evaluation interval
var npc = world.Spawn()
    .With(UtilityComponent.Create(brain) with { EvaluationInterval = 0.5f })
    .Build();
```

### Entity Count Guidelines

| AI Type | Typical Entity Count | Notes |
|---------|---------------------|-------|
| FSM | 1000+ | Very lightweight, state + transitions |
| Behavior Tree | 100-500 | Depends on tree depth/complexity |
| Utility AI | 100-300 | Depends on action/consideration count |

### Tips

1. **Share definitions** - Reuse StateMachine/BehaviorTree/UtilityAI instances across entities
2. **Limit tree depth** - Keep behavior trees shallow when possible
3. **Reduce considerations** - More considerations = more evaluations
4. **Use early-out conditions** - Put cheap checks first in sequences

---

## Sample Project

See `samples/KeenEyes.Sample.AIProximity/` for a complete example demonstrating:

- Vision and hearing detection using spatial queries
- State machine transitions (Idle -> Searching -> Alert)
- Alert broadcasting between guards
- Performance optimization for many AI agents

### Running the Sample

```bash
cd samples/KeenEyes.Sample.AIProximity
dotnet run
```

---

## Next Steps

- [Plugins Guide](plugins.md) - How plugins work
- [Systems Guide](systems.md) - System design patterns
- [Spatial Partitioning](spatial.md) - For proximity queries in AI
- [AI System Design](research/ai-system.md) - Original design document
