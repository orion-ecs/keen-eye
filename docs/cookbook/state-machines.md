# State Machine Entities

## Problem

You want entities to have distinct behavioral states (idle, patrol, attack, flee) with clean transitions between them.

## Solution

### Components

```csharp
public enum AIStateType
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee,
    Dead
}

[Component]
public partial struct AIState : IComponent
{
    public AIStateType Current;
    public AIStateType Previous;
    public float TimeInState;
}

// State-specific data components
[Component]
public partial struct PatrolData : IComponent
{
    public int CurrentWaypointIndex;
    public float WaitTimer;
}

[Component]
public partial struct ChaseData : IComponent
{
    public Entity Target;
    public float LostTargetTimer;
}

[Component]
public partial struct AttackData : IComponent
{
    public Entity Target;
    public float Cooldown;
}
```

### State Machine System

```csharp
public class AIStateMachineSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.Update;

    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<AIState, Position>())
        {
            ref var state = ref World.Get<AIState>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            state.TimeInState += deltaTime;

            // Evaluate transitions
            var newState = EvaluateTransitions(entity, ref state, ref pos);

            if (newState != state.Current)
            {
                TransitionTo(buffer, entity, ref state, newState);
            }
        }

        buffer.Execute();
    }

    private AIStateType EvaluateTransitions(Entity entity, ref AIState state, ref Position pos)
    {
        switch (state.Current)
        {
            case AIStateType.Idle:
                // Check for enemies in range
                if (TryFindTarget(entity, pos, out _))
                    return AIStateType.Chase;

                // Random chance to start patrolling
                if (state.TimeInState > 3f)
                    return AIStateType.Patrol;
                break;

            case AIStateType.Patrol:
                if (TryFindTarget(entity, pos, out _))
                    return AIStateType.Chase;
                break;

            case AIStateType.Chase:
                if (World.TryGet<ChaseData>(entity, out var chase))
                {
                    if (!World.IsAlive(chase.Target))
                        return AIStateType.Idle;

                    var targetPos = World.Get<Position>(chase.Target);
                    var dist = Distance(pos, targetPos);

                    if (dist < 50)  // In attack range
                        return AIStateType.Attack;

                    if (dist > 500)  // Lost target
                        return AIStateType.Idle;
                }
                break;

            case AIStateType.Attack:
                if (World.TryGet<Health>(entity, out var health) && health.Current < 20)
                    return AIStateType.Flee;  // Low health, run!

                if (World.TryGet<AttackData>(entity, out var attack))
                {
                    if (!World.IsAlive(attack.Target))
                        return AIStateType.Idle;

                    var targetPos = World.Get<Position>(attack.Target);
                    if (Distance(pos, targetPos) > 100)  // Out of attack range
                        return AIStateType.Chase;
                }
                break;

            case AIStateType.Flee:
                if (World.TryGet<Health>(entity, out var h) && h.Current > 50)
                    return AIStateType.Idle;  // Recovered
                break;
        }

        return state.Current;  // No transition
    }

    private void TransitionTo(ICommandBuffer buffer, Entity entity, ref AIState state, AIStateType newState)
    {
        // Exit current state - clean up state-specific components
        switch (state.Current)
        {
            case AIStateType.Patrol:
                buffer.Remove<PatrolData>(entity);
                break;
            case AIStateType.Chase:
                buffer.Remove<ChaseData>(entity);
                break;
            case AIStateType.Attack:
                buffer.Remove<AttackData>(entity);
                break;
        }

        // Enter new state - add state-specific components
        switch (newState)
        {
            case AIStateType.Patrol:
                buffer.Add(entity, new PatrolData { CurrentWaypointIndex = 0 });
                break;
            case AIStateType.Chase:
                if (TryFindTarget(entity, World.Get<Position>(entity), out var target))
                    buffer.Add(entity, new ChaseData { Target = target });
                break;
            case AIStateType.Attack:
                if (World.TryGet<ChaseData>(entity, out var chase))
                    buffer.Add(entity, new AttackData { Target = chase.Target });
                break;
        }

        // Update state
        state.Previous = state.Current;
        state.Current = newState;
        state.TimeInState = 0f;
    }

    private bool TryFindTarget(Entity self, Position pos, out Entity target)
    {
        target = default;
        float closestDist = 300f;  // Detection range

        foreach (var candidate in World.Query<Position>().With<Player>())
        {
            var candidatePos = World.Get<Position>(candidate);
            var dist = Distance(pos, candidatePos);
            if (dist < closestDist)
            {
                closestDist = dist;
                target = candidate;
            }
        }

        return target.Id != 0;
    }

    private float Distance(Position a, Position b)
        => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
```

### State Behavior Systems

```csharp
public class PatrolBehaviorSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AIState, PatrolData, Position>())
        {
            ref readonly var state = ref World.Get<AIState>(entity);
            if (state.Current != AIStateType.Patrol)
                continue;

            ref var patrol = ref World.Get<PatrolData>(entity);
            ref var pos = ref World.Get<Position>(entity);

            // Patrol logic: move between waypoints
            var waypoint = GetWaypoint(patrol.CurrentWaypointIndex);
            MoveToward(ref pos, waypoint, 50f * deltaTime);

            if (Distance(pos, waypoint) < 5f)
            {
                patrol.WaitTimer += deltaTime;
                if (patrol.WaitTimer > 2f)
                {
                    patrol.CurrentWaypointIndex = (patrol.CurrentWaypointIndex + 1) % WaypointCount;
                    patrol.WaitTimer = 0f;
                }
            }
        }
    }
}

public class ChaseBehaviorSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AIState, ChaseData, Position>())
        {
            ref readonly var state = ref World.Get<AIState>(entity);
            if (state.Current != AIStateType.Chase)
                continue;

            ref readonly var chase = ref World.Get<ChaseData>(entity);
            ref var pos = ref World.Get<Position>(entity);

            if (!World.IsAlive(chase.Target))
                continue;

            var targetPos = World.Get<Position>(chase.Target);
            MoveToward(ref pos, targetPos, 100f * deltaTime);
        }
    }
}

public class AttackBehaviorSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AIState, AttackData>())
        {
            ref readonly var state = ref World.Get<AIState>(entity);
            if (state.Current != AIStateType.Attack)
                continue;

            ref var attack = ref World.Get<AttackData>(entity);
            attack.Cooldown -= deltaTime;

            if (attack.Cooldown <= 0 && World.IsAlive(attack.Target))
            {
                // Deal damage
                World.Add(attack.Target, new DamageReceived { Amount = 10, Source = entity });
                attack.Cooldown = 1f;  // Attack once per second
            }
        }
    }
}
```

## Why This Works

### State as Component

Using `AIState` as a component means:
- States are queryable: "Find all entities in Chase state"
- States are serializable: Save/load preserves AI state
- States are visible: Debug tools can inspect current state
- No inheritance: No `ChaseState : BaseState` hierarchies

### State-Specific Data as Components

Each state's data lives in separate components:
- Only exists when in that state (via add/remove on transitions)
- Queries naturally filter: `Query<ChaseData>` gets only chasing entities
- No wasted memory: Idle entities don't carry patrol data

### Separation of Concerns

- **State Machine System**: Handles transitions only
- **Behavior Systems**: Handle per-state logic
- **Transition Logic**: Centralized, easy to modify

### Transition Clarity

The `TransitionTo` method explicitly:
1. Exits the old state (cleanup)
2. Enters the new state (setup)
3. Resets timing

This makes state lifecycles predictable.

## Variations

### Hierarchical State Machines

```csharp
public enum CombatSubState
{
    Approaching,
    Attacking,
    Retreating
}

[Component]
public partial struct CombatState : IComponent
{
    public CombatSubState SubState;
    public float SubStateTimer;
}

// Combat state has its own sub-state machine
// Only active when AIState.Current == Combat
```

### Utility AI Hybrid

Combine FSM with utility scoring:

```csharp
public class UtilityAISystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AIState, Health, Position>())
        {
            ref var state = ref World.Get<AIState>(entity);
            ref readonly var health = ref World.Get<Health>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            // Score each possible state
            var scores = new Dictionary<AIStateType, float>
            {
                [AIStateType.Idle] = 0.1f,
                [AIStateType.Patrol] = 0.3f,
                [AIStateType.Chase] = ScoreChase(entity, pos),
                [AIStateType.Attack] = ScoreAttack(entity, pos),
                [AIStateType.Flee] = ScoreFlee(health),
            };

            // Pick highest scoring state
            var best = scores.MaxBy(kvp => kvp.Value).Key;

            if (best != state.Current)
            {
                // Transition...
            }
        }
    }

    private float ScoreFlee(Health health)
    {
        // Higher score when low health
        return 1f - (health.Current / (float)health.Max);
    }
}
```

### Event-Driven Transitions

```csharp
[Component]
public partial struct StateTransitionRequest : IComponent
{
    public AIStateType TargetState;
    public Entity Source;  // What triggered this transition
}

// External systems can request state changes
world.Add(enemy, new StateTransitionRequest
{
    TargetState = AIStateType.Flee,
    Source = loudNoise
});
```

### Debug Visualization

```csharp
public class AIDebugSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<AIState, Position>())
        {
            ref readonly var state = ref World.Get<AIState>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            // Draw state name above entity
            var color = state.Current switch
            {
                AIStateType.Idle => Color.Gray,
                AIStateType.Patrol => Color.Blue,
                AIStateType.Chase => Color.Yellow,
                AIStateType.Attack => Color.Red,
                AIStateType.Flee => Color.Purple,
                _ => Color.White
            };

            DebugDraw.Text(state.Current.ToString(), pos, color);
            DebugDraw.Text($"{state.TimeInState:F1}s", pos + new Vector2(0, 15), color);
        }
    }
}
```

## See Also

- [AI System Research](../research/ai-system.md) - Comprehensive AI architecture
- [Messaging Guide](../messaging.md) - Event-driven communication
- [Health & Damage](health-damage.md) - Combat integration
