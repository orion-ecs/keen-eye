# AI System Architecture

This document outlines the architecture for the AI system in KeenEyes, providing decision-making patterns for game entities including state machines, behavior trees, and utility AI.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [AI Approaches](#ai-approaches)
3. [Architecture Overview](#architecture-overview)
4. [Finite State Machines](#finite-state-machines)
5. [Behavior Trees](#behavior-trees)
6. [Utility AI](#utility-ai)
7. [Blackboard System](#blackboard-system)
8. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes AI provides three complementary decision-making systems:

1. **Finite State Machines (FSM)** - Simple, explicit state transitions
2. **Behavior Trees (BT)** - Hierarchical, modular behaviors
3. **Utility AI** - Score-based action selection

These can be used independently or combined for complex AI behavior.

**Key Design:** AI logic is data-driven assets, state is in components, systems evaluate decisions.

---

## AI Approaches

### Comparison

| Approach | Best For | Complexity | Flexibility |
|----------|----------|------------|-------------|
| **FSM** | Simple NPCs, UI states | Low | Limited |
| **Behavior Tree** | Complex behaviors, reusable patterns | Medium | High |
| **Utility AI** | Dynamic priorities, many options | High | Very High |

### When to Use Each

- **FSM:** Door (Open/Closed), simple enemy (Patrol/Chase/Attack)
- **Behavior Tree:** Complex enemy AI, boss patterns, companions
- **Utility AI:** NPCs with needs (Sims-like), squad tactics, dynamic behavior

---

## Architecture Overview

### Project Structure

```
KeenEyes.AI/
├── KeenEyes.AI.csproj
├── AIPlugin.cs                    # IWorldPlugin entry point
│
├── Core/
│   ├── IAIContext.cs             # Extension API
│   ├── Blackboard.cs             # Shared data storage
│   └── AIConfig.cs               # Global settings
│
├── FSM/
│   ├── StateMachine.cs           # FSM asset
│   ├── State.cs                  # Single state
│   ├── Transition.cs             # State transition
│   ├── StateMachineComponent.cs  # Entity component
│   └── StateMachineSystem.cs     # Evaluation system
│
├── BehaviorTree/
│   ├── BehaviorTree.cs           # BT asset
│   ├── BTNode.cs                 # Base node
│   ├── BTNodeState.cs            # Running/Success/Failure
│   │
│   ├── Composites/
│   │   ├── Selector.cs           # OR logic
│   │   ├── Sequence.cs           # AND logic
│   │   ├── Parallel.cs           # Concurrent
│   │   └── RandomSelector.cs     # Weighted random
│   │
│   ├── Decorators/
│   │   ├── Inverter.cs           # NOT logic
│   │   ├── Repeater.cs           # Loop
│   │   ├── Succeeder.cs          # Always succeed
│   │   ├── UntilFail.cs          # Repeat until failure
│   │   └── Cooldown.cs           # Time-based gate
│   │
│   ├── Leaves/
│   │   ├── Condition.cs          # Check condition
│   │   ├── Action.cs             # Execute action
│   │   └── Wait.cs               # Pause execution
│   │
│   ├── BehaviorTreeComponent.cs  # Entity component
│   └── BehaviorTreeSystem.cs     # Evaluation system
│
├── Utility/
│   ├── UtilityAI.cs              # Utility brain asset
│   ├── UtilityAction.cs          # Scoreable action
│   ├── Consideration.cs          # Score input
│   ├── ResponseCurve.cs          # Score mapping
│   ├── UtilityComponent.cs       # Entity component
│   └── UtilitySystem.cs          # Evaluation system
│
└── Actions/
    ├── IAIAction.cs              # Action interface
    ├── MoveToAction.cs           # Built-in: move to target
    ├── AttackAction.cs           # Built-in: attack target
    ├── WaitAction.cs             # Built-in: wait duration
    └── PatrolAction.cs           # Built-in: waypoint patrol
```

---

## Finite State Machines

### StateMachine Asset

```csharp
public sealed class StateMachine
{
    public string Name { get; init; }
    public State[] States { get; init; }
    public Transition[] Transitions { get; init; }
    public int InitialStateIndex { get; init; }
}

public sealed class State
{
    public string Name { get; init; }

    // Optional actions
    public IAIAction? OnEnter { get; init; }
    public IAIAction? OnUpdate { get; init; }
    public IAIAction? OnExit { get; init; }
}

public sealed class Transition
{
    public int FromStateIndex { get; init; }
    public int ToStateIndex { get; init; }
    public ICondition Condition { get; init; }
    public float Priority { get; init; }  // Higher = checked first
}
```

### StateMachineComponent

```csharp
[Component]
public partial struct StateMachineComponent
{
    public StateMachine Machine;
    public int CurrentStateIndex;
    public float StateTime;
    public Blackboard Blackboard;
}
```

### StateMachineSystem

```csharp
public class StateMachineSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<StateMachineComponent>())
        {
            ref var fsm = ref World.Get<StateMachineComponent>(entity);
            var machine = fsm.Machine;

            // Check transitions (sorted by priority)
            foreach (var transition in machine.Transitions
                .Where(t => t.FromStateIndex == fsm.CurrentStateIndex)
                .OrderByDescending(t => t.Priority))
            {
                if (transition.Condition.Evaluate(entity, fsm.Blackboard, World))
                {
                    // Execute exit action
                    var currentState = machine.States[fsm.CurrentStateIndex];
                    currentState.OnExit?.Execute(entity, fsm.Blackboard, World);

                    // Transition
                    fsm.CurrentStateIndex = transition.ToStateIndex;
                    fsm.StateTime = 0;

                    // Execute enter action
                    var newState = machine.States[fsm.CurrentStateIndex];
                    newState.OnEnter?.Execute(entity, fsm.Blackboard, World);

                    break;
                }
            }

            // Execute update action
            var state = machine.States[fsm.CurrentStateIndex];
            state.OnUpdate?.Execute(entity, fsm.Blackboard, World);

            fsm.StateTime += deltaTime;
        }
    }
}
```

### FSM Example

```csharp
var enemyFSM = new StateMachine
{
    Name = "EnemyAI",
    States = [
        new State {
            Name = "Patrol",
            OnUpdate = new PatrolAction { WaypointTag = "PatrolPoint" }
        },
        new State {
            Name = "Chase",
            OnEnter = new PlaySoundAction { Sound = "alert" },
            OnUpdate = new ChaseAction { Speed = 5f }
        },
        new State {
            Name = "Attack",
            OnUpdate = new AttackAction { Damage = 10, Cooldown = 1f }
        }
    ],
    Transitions = [
        new Transition {
            FromStateIndex = 0, // Patrol
            ToStateIndex = 1,   // Chase
            Condition = new SeePlayerCondition { Range = 10f }
        },
        new Transition {
            FromStateIndex = 1, // Chase
            ToStateIndex = 2,   // Attack
            Condition = new InRangeCondition { Range = 2f }
        },
        new Transition {
            FromStateIndex = 1, // Chase
            ToStateIndex = 0,   // Patrol
            Condition = new LostPlayerCondition { Duration = 3f }
        },
        new Transition {
            FromStateIndex = 2, // Attack
            ToStateIndex = 1,   // Chase
            Condition = new NotInRangeCondition { Range = 2f }
        }
    ]
};
```

---

## Behavior Trees

### Core Types

```csharp
public enum BTNodeState
{
    Running,    // Still executing
    Success,    // Completed successfully
    Failure     // Failed
}

public abstract class BTNode
{
    public string Name { get; init; }

    public abstract BTNodeState Execute(Entity entity, Blackboard bb, IWorld world);
    public virtual void Reset() { }
}
```

### Composite Nodes

```csharp
// Selector (OR): Returns Success on first child success
public class Selector : BTNode
{
    public BTNode[] Children { get; init; }
    private int currentChild;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        while (currentChild < Children.Length)
        {
            var state = Children[currentChild].Execute(entity, bb, world);

            if (state == BTNodeState.Success)
            {
                currentChild = 0;
                return BTNodeState.Success;
            }

            if (state == BTNodeState.Running)
                return BTNodeState.Running;

            currentChild++;
        }

        currentChild = 0;
        return BTNodeState.Failure;
    }
}

// Sequence (AND): Returns Failure on first child failure
public class Sequence : BTNode
{
    public BTNode[] Children { get; init; }
    private int currentChild;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        while (currentChild < Children.Length)
        {
            var state = Children[currentChild].Execute(entity, bb, world);

            if (state == BTNodeState.Failure)
            {
                currentChild = 0;
                return BTNodeState.Failure;
            }

            if (state == BTNodeState.Running)
                return BTNodeState.Running;

            currentChild++;
        }

        currentChild = 0;
        return BTNodeState.Success;
    }
}

// Parallel: Run all children simultaneously
public class Parallel : BTNode
{
    public BTNode[] Children { get; init; }
    public ParallelPolicy SuccessPolicy { get; init; } = ParallelPolicy.RequireAll;
    public ParallelPolicy FailurePolicy { get; init; } = ParallelPolicy.RequireOne;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        int successCount = 0, failureCount = 0;

        foreach (var child in Children)
        {
            var state = child.Execute(entity, bb, world);
            if (state == BTNodeState.Success) successCount++;
            if (state == BTNodeState.Failure) failureCount++;
        }

        if (SuccessPolicy == ParallelPolicy.RequireOne && successCount > 0)
            return BTNodeState.Success;
        if (FailurePolicy == ParallelPolicy.RequireOne && failureCount > 0)
            return BTNodeState.Failure;
        if (SuccessPolicy == ParallelPolicy.RequireAll && successCount == Children.Length)
            return BTNodeState.Success;
        if (FailurePolicy == ParallelPolicy.RequireAll && failureCount == Children.Length)
            return BTNodeState.Failure;

        return BTNodeState.Running;
    }
}

public enum ParallelPolicy { RequireOne, RequireAll }
```

### Decorator Nodes

```csharp
// Inverter: Flip Success/Failure
public class Inverter : BTNode
{
    public BTNode Child { get; init; }

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        var state = Child.Execute(entity, bb, world);
        return state switch
        {
            BTNodeState.Success => BTNodeState.Failure,
            BTNodeState.Failure => BTNodeState.Success,
            _ => state
        };
    }
}

// Cooldown: Fail if called too recently
public class Cooldown : BTNode
{
    public BTNode Child { get; init; }
    public float Duration { get; init; }
    private float lastExecutionTime = float.MinValue;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        var time = bb.Get<float>("Time");

        if (time - lastExecutionTime < Duration)
            return BTNodeState.Failure;

        var state = Child.Execute(entity, bb, world);

        if (state == BTNodeState.Success)
            lastExecutionTime = time;

        return state;
    }
}

// Repeater: Loop child N times
public class Repeater : BTNode
{
    public BTNode Child { get; init; }
    public int Count { get; init; } = -1;  // -1 = infinite

    private int currentCount;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        if (Count >= 0 && currentCount >= Count)
        {
            currentCount = 0;
            return BTNodeState.Success;
        }

        var state = Child.Execute(entity, bb, world);

        if (state == BTNodeState.Running)
            return BTNodeState.Running;

        currentCount++;

        if (Count < 0 || currentCount < Count)
            return BTNodeState.Running;

        currentCount = 0;
        return BTNodeState.Success;
    }
}
```

### Leaf Nodes

```csharp
// Condition: Check a boolean condition
public class ConditionNode : BTNode
{
    public ICondition Condition { get; init; }

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
        => Condition.Evaluate(entity, bb, world)
            ? BTNodeState.Success
            : BTNodeState.Failure;
}

// Action: Execute an AI action
public class ActionNode : BTNode
{
    public IAIAction Action { get; init; }

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
        => Action.Execute(entity, bb, world);
}

// Wait: Pause for duration
public class WaitNode : BTNode
{
    public float Duration { get; init; }
    private float elapsed;

    public override BTNodeState Execute(Entity entity, Blackboard bb, IWorld world)
    {
        elapsed += bb.Get<float>("DeltaTime");

        if (elapsed >= Duration)
        {
            elapsed = 0;
            return BTNodeState.Success;
        }

        return BTNodeState.Running;
    }
}
```

### Behavior Tree Example

```csharp
// Enemy behavior: Attack if player in range, else chase, else patrol
var enemyBT = new BehaviorTree
{
    Name = "EnemyBT",
    Root = new Selector
    {
        Children = [
            // Try to attack
            new Sequence {
                Children = [
                    new ConditionNode { Condition = new InRangeCondition { Range = 2f } },
                    new ActionNode { Action = new AttackAction() }
                ]
            },
            // Try to chase
            new Sequence {
                Children = [
                    new ConditionNode { Condition = new CanSeePlayerCondition() },
                    new ActionNode { Action = new ChaseAction() }
                ]
            },
            // Default: patrol
            new ActionNode { Action = new PatrolAction() }
        ]
    }
};
```

---

## Utility AI

### Core Types

```csharp
public sealed class UtilityAI
{
    public string Name { get; init; }
    public UtilityAction[] Actions { get; init; }
    public float SelectionThreshold { get; init; } = 0.1f;  // Minimum score to consider
}

public sealed class UtilityAction
{
    public string Name { get; init; }
    public IAIAction Action { get; init; }
    public Consideration[] Considerations { get; init; }
    public float Weight { get; init; } = 1f;

    public float CalculateScore(Entity entity, Blackboard bb, IWorld world)
    {
        if (Considerations.Length == 0)
            return Weight;

        float score = Weight;

        foreach (var consideration in Considerations)
        {
            float input = consideration.Input.GetValue(entity, bb, world);
            float normalized = consideration.Curve.Evaluate(input);
            score *= normalized;

            // Early out if score is too low
            if (score < 0.01f)
                return 0;
        }

        // Compensation factor for number of considerations
        // Prevents actions with more considerations from being penalized
        float compensation = 1f + (1f - score) * (1f / Considerations.Length);
        return score * compensation;
    }
}

public sealed class Consideration
{
    public string Name { get; init; }
    public IConsiderationInput Input { get; init; }
    public ResponseCurve Curve { get; init; }
}
```

### Consideration Inputs

```csharp
public interface IConsiderationInput
{
    float GetValue(Entity entity, Blackboard bb, IWorld world);  // Returns 0-1
}

public class DistanceToTargetInput : IConsiderationInput
{
    public float MaxDistance { get; init; } = 20f;

    public float GetValue(Entity entity, Blackboard bb, IWorld world)
    {
        var target = bb.Get<Entity?>("Target");
        if (target == null) return 1f;

        var myPos = world.Get<Transform3D>(entity).Position;
        var targetPos = world.Get<Transform3D>(target.Value).Position;

        float distance = Vector3.Distance(myPos, targetPos);
        return Math.Clamp(distance / MaxDistance, 0, 1);
    }
}

public class HealthPercentInput : IConsiderationInput
{
    public float GetValue(Entity entity, Blackboard bb, IWorld world)
    {
        if (!world.Has<Health>(entity)) return 1f;

        ref readonly var health = ref world.Get<Health>(entity);
        return (float)health.Current / health.Max;
    }
}

public class HasAmmoInput : IConsiderationInput
{
    public float GetValue(Entity entity, Blackboard bb, IWorld world)
    {
        var ammo = bb.Get<int>("Ammo");
        var maxAmmo = bb.Get<int>("MaxAmmo");
        return (float)ammo / maxAmmo;
    }
}
```

### Response Curves

```csharp
public abstract class ResponseCurve
{
    public abstract float Evaluate(float input);
}

public class LinearCurve : ResponseCurve
{
    public float Slope { get; init; } = 1f;
    public float Offset { get; init; } = 0f;

    public override float Evaluate(float input)
        => Math.Clamp(Slope * input + Offset, 0, 1);
}

public class ExponentialCurve : ResponseCurve
{
    public float Exponent { get; init; } = 2f;

    public override float Evaluate(float input)
        => MathF.Pow(input, Exponent);
}

public class LogisticCurve : ResponseCurve
{
    public float Steepness { get; init; } = 10f;
    public float Midpoint { get; init; } = 0.5f;

    public override float Evaluate(float input)
        => 1f / (1f + MathF.Exp(-Steepness * (input - Midpoint)));
}

public class CustomCurve : ResponseCurve
{
    public AnimationCurve Curve { get; init; }

    public override float Evaluate(float input)
        => Curve.Evaluate(input);
}
```

### Utility System

```csharp
public class UtilitySystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<UtilityComponent>())
        {
            ref var utility = ref World.Get<UtilityComponent>(entity);
            var brain = utility.Brain;

            // Score all actions
            var scores = new (UtilityAction action, float score)[brain.Actions.Length];

            for (int i = 0; i < brain.Actions.Length; i++)
            {
                scores[i] = (
                    brain.Actions[i],
                    brain.Actions[i].CalculateScore(entity, utility.Blackboard, World)
                );
            }

            // Select best action above threshold
            var best = scores
                .Where(s => s.score >= brain.SelectionThreshold)
                .OrderByDescending(s => s.score)
                .FirstOrDefault();

            if (best.action != null && best.action != utility.CurrentAction)
            {
                utility.CurrentAction = best.action;
            }

            // Execute current action
            utility.CurrentAction?.Action.Execute(entity, utility.Blackboard, World);
        }
    }
}
```

### Utility AI Example

```csharp
var guardAI = new UtilityAI
{
    Name = "Guard",
    Actions = [
        new UtilityAction {
            Name = "Attack",
            Action = new AttackAction(),
            Considerations = [
                new Consideration {
                    Name = "Target in range",
                    Input = new DistanceToTargetInput { MaxDistance = 10f },
                    Curve = new LinearCurve { Slope = -1f, Offset = 1f } // Closer = higher
                },
                new Consideration {
                    Name = "I have health",
                    Input = new HealthPercentInput(),
                    Curve = new LogisticCurve { Steepness = 5f, Midpoint = 0.3f }
                }
            ]
        },
        new UtilityAction {
            Name = "Flee",
            Action = new FleeAction(),
            Considerations = [
                new Consideration {
                    Name = "Low health",
                    Input = new HealthPercentInput(),
                    Curve = new LinearCurve { Slope = -1f, Offset = 1f } // Lower = higher
                }
            ]
        },
        new UtilityAction {
            Name = "Patrol",
            Action = new PatrolAction(),
            Weight = 0.3f  // Default fallback
        }
    ]
};
```

---

## Blackboard System

### Blackboard

```csharp
public sealed class Blackboard
{
    private readonly Dictionary<string, object> data = new();

    public void Set<T>(string key, T value) => data[key] = value!;

    public T Get<T>(string key)
        => data.TryGetValue(key, out var value) ? (T)value : default!;

    public T Get<T>(string key, T defaultValue)
        => data.TryGetValue(key, out var value) ? (T)value : defaultValue;

    public bool TryGet<T>(string key, out T value)
    {
        if (data.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    public bool Has(string key) => data.ContainsKey(key);

    public void Remove(string key) => data.Remove(key);

    public void Clear() => data.Clear();
}
```

### Common Blackboard Keys

```csharp
public static class BBKeys
{
    // Time
    public const string Time = "Time";
    public const string DeltaTime = "DeltaTime";

    // Target
    public const string Target = "Target";
    public const string TargetPosition = "TargetPosition";
    public const string TargetLastSeen = "TargetLastSeen";

    // Self
    public const string Health = "Health";
    public const string Ammo = "Ammo";
    public const string AlertLevel = "AlertLevel";

    // Navigation
    public const string Destination = "Destination";
    public const string CurrentPath = "CurrentPath";
    public const string PatrolIndex = "PatrolIndex";
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `KeenEyes.AI` project
2. Implement Blackboard
3. Define IAIAction and ICondition interfaces
4. Create AIPlugin

**Milestone:** Foundation for AI systems

### Phase 2: Finite State Machines

1. Implement StateMachine asset
2. State and Transition classes
3. StateMachineComponent
4. StateMachineSystem
5. Common conditions

**Milestone:** Working FSM system

### Phase 3: Behavior Trees

1. BTNode base and state enum
2. Composite nodes (Selector, Sequence, Parallel)
3. Decorator nodes
4. Leaf nodes (Condition, Action, Wait)
5. BehaviorTreeSystem

**Milestone:** Working behavior trees

### Phase 4: Utility AI

1. UtilityAI and UtilityAction
2. Consideration and ResponseCurve
3. Consideration inputs
4. UtilitySystem
5. Score visualization

**Milestone:** Working utility AI

### Phase 5: Built-in Actions

1. MoveToAction (requires pathfinding integration)
2. AttackAction
3. PatrolAction
4. WaitAction
5. Custom action support

**Milestone:** Reusable AI behaviors

---

## Open Questions

1. **Pathfinding Integration** - How to integrate with Navigation system?
2. **Perception** - Sight, hearing, smell as separate system?
3. **Group AI** - Squads, formations, coordination?
4. **Debugging** - Visualization of AI decisions?
5. **Serialization** - How to save/load AI state?
6. **Performance** - Throttle AI updates for many entities?

---

## Related Issues

- Milestone #21: AI System
- Issue #426: Create KeenEyes.AI with FSM and behavior trees
- Issue #427: Implement utility AI and blackboard system
