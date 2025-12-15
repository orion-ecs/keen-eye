# Animation System Architecture

This document outlines the architecture for a flexible animation system in KeenEyes, supporting sprite animation, skeletal animation, and property tweening.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Animation Types](#animation-types)
3. [Architecture Overview](#architecture-overview)
4. [Sprite Animation](#sprite-animation)
5. [Skeletal Animation](#skeletal-animation)
6. [Property Animation](#property-animation)
7. [State Machines](#state-machines)
8. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes Animation provides three complementary animation systems:

1. **Sprite Animation** - Frame-by-frame 2D animations
2. **Skeletal Animation** - Bone-based deformation (2D and 3D)
3. **Property Animation** - Tween any component value over time

**Key Design:** Animations are assets (clips), state is in components, systems drive playback.

---

## Animation Types

### Comparison

| Type | Use Case | Data | Complexity |
|------|----------|------|------------|
| Sprite | 2D characters, effects | Frame indices | Simple |
| Skeletal | Complex characters | Bone transforms | Medium |
| Property | UI, procedural | Component values | Flexible |

### When to Use Each

- **Sprite Animation:** Pixel art, simple 2D games, UI effects
- **Skeletal Animation:** Characters with many animations, smooth blending needed
- **Property Animation:** UI transitions, camera effects, any numeric value

---

## Architecture Overview

### Project Structure

```
KeenEyes.Animation/
├── KeenEyes.Animation.csproj
├── AnimationPlugin.cs             # IWorldPlugin entry point
│
├── Core/
│   ├── IAnimationClip.cs         # Base animation interface
│   ├── AnimationCurve.cs         # Keyframe interpolation
│   ├── Easing.cs                 # Easing functions
│   └── AnimationContext.cs       # Extension API
│
├── Sprite/
│   ├── SpriteAnimation.cs        # Sprite clip asset
│   ├── SpriteAnimator.cs         # Component
│   ├── SpriteFrame.cs            # Frame data
│   └── SpriteAnimationSystem.cs  # Playback system
│
├── Skeletal/
│   ├── Skeleton.cs               # Bone hierarchy
│   ├── SkeletonAnimation.cs      # Skeletal clip asset
│   ├── SkeletalAnimator.cs       # Component
│   ├── Bone.cs                   # Single bone data
│   ├── BonePose.cs               # Runtime pose
│   └── SkeletalAnimationSystem.cs
│
├── Property/
│   ├── PropertyAnimation.cs      # Property clip asset
│   ├── PropertyAnimator.cs       # Component
│   ├── AnimationTrack.cs         # Single property track
│   ├── Tween.cs                  # Tween helper
│   └── PropertyAnimationSystem.cs
│
├── StateMachine/
│   ├── AnimationStateMachine.cs  # State machine asset
│   ├── AnimationState.cs         # Single state
│   ├── AnimationTransition.cs    # State transition
│   ├── StateMachineController.cs # Component
│   └── StateMachineSystem.cs     # Evaluation system
│
└── Blending/
    ├── AnimationBlender.cs       # Blend multiple animations
    ├── BlendTree.cs              # 1D/2D blend trees
    └── AnimationLayer.cs         # Layered animation
```

---

## Sprite Animation

### SpriteAnimation Asset

```csharp
public sealed class SpriteAnimation : IAnimationClip
{
    public string Name { get; init; }
    public float Duration => Frames.Length / FrameRate;
    public float FrameRate { get; init; } = 12f;

    public SpriteFrame[] Frames { get; init; }
    public WrapMode WrapMode { get; init; } = WrapMode.Loop;

    // Events at specific frames
    public AnimationEvent[] Events { get; init; } = [];
}

public readonly record struct SpriteFrame(
    int SpriteIndex,          // Index in sprite sheet
    Rectangle? SourceRect,    // Optional custom rect
    Vector2 Offset,           // Position offset
    float Duration            // Frame-specific duration (0 = use default)
);

public readonly record struct AnimationEvent(
    float Time,
    string Name,
    object? Parameter
);

public enum WrapMode
{
    Once,           // Play once, stop at end
    Loop,           // Loop forever
    PingPong,       // Forward then backward
    ClampForever    // Play once, hold last frame
}
```

### SpriteAnimator Component

```csharp
[Component]
public partial struct SpriteAnimator
{
    // Current animation
    public SpriteAnimation? CurrentAnimation;
    public int CurrentFrameIndex;
    public float Time;
    public float Speed;
    public bool Playing;

    // Queue for chained animations
    public SpriteAnimation? QueuedAnimation;

    // Events
    public AnimationEventFlags PendingEvents;
}

[Flags]
public enum AnimationEventFlags
{
    None = 0,
    FrameChanged = 1 << 0,
    AnimationStarted = 1 << 1,
    AnimationEnded = 1 << 2,
    AnimationLooped = 1 << 3,
    CustomEvent = 1 << 4
}
```

### SpriteAnimationSystem

```csharp
public class SpriteAnimationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<SpriteAnimator, Sprite>())
        {
            ref var animator = ref World.Get<SpriteAnimator>(entity);
            ref var sprite = ref World.Get<Sprite>(entity);

            if (!animator.Playing || animator.CurrentAnimation == null)
                continue;

            var anim = animator.CurrentAnimation;

            // Advance time
            animator.Time += deltaTime * animator.Speed;

            // Calculate frame
            float frameDuration = 1f / anim.FrameRate;
            int frameCount = anim.Frames.Length;

            int newFrame = (int)(animator.Time / frameDuration);

            // Handle wrap mode
            switch (anim.WrapMode)
            {
                case WrapMode.Once:
                    if (newFrame >= frameCount)
                    {
                        newFrame = frameCount - 1;
                        animator.Playing = false;
                        animator.PendingEvents |= AnimationEventFlags.AnimationEnded;
                    }
                    break;

                case WrapMode.Loop:
                    if (newFrame >= frameCount)
                    {
                        newFrame %= frameCount;
                        animator.Time %= anim.Duration;
                        animator.PendingEvents |= AnimationEventFlags.AnimationLooped;
                    }
                    break;

                case WrapMode.PingPong:
                    int cycle = newFrame / frameCount;
                    newFrame %= frameCount;
                    if (cycle % 2 == 1)
                        newFrame = frameCount - 1 - newFrame;
                    break;
            }

            // Update sprite
            if (newFrame != animator.CurrentFrameIndex)
            {
                animator.CurrentFrameIndex = newFrame;
                animator.PendingEvents |= AnimationEventFlags.FrameChanged;

                var frame = anim.Frames[newFrame];
                sprite.SpriteIndex = frame.SpriteIndex;
                if (frame.SourceRect.HasValue)
                    sprite.SourceRect = frame.SourceRect.Value;
            }

            // Process events
            ProcessEvents(entity, animator, anim);
        }
    }
}
```

---

## Skeletal Animation

### Skeleton Definition

```csharp
public sealed class Skeleton
{
    public string Name { get; init; }
    public Bone[] Bones { get; init; }
    public int RootBoneIndex { get; init; }

    // Bind pose (T-pose or rest pose)
    public BonePose[] BindPose { get; init; }
}

public readonly record struct Bone(
    string Name,
    int ParentIndex,      // -1 for root
    Vector3 LocalPosition,
    Quaternion LocalRotation,
    Vector3 LocalScale
);

public struct BonePose
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Matrix4x4 ToMatrix() =>
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateFromQuaternion(Rotation) *
        Matrix4x4.CreateTranslation(Position);

    public static BonePose Lerp(in BonePose a, in BonePose b, float t) => new()
    {
        Position = Vector3.Lerp(a.Position, b.Position, t),
        Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
        Scale = Vector3.Lerp(a.Scale, b.Scale, t)
    };
}
```

### SkeletonAnimation Asset

```csharp
public sealed class SkeletonAnimation : IAnimationClip
{
    public string Name { get; init; }
    public float Duration { get; init; }
    public WrapMode WrapMode { get; init; }

    // Keyframes per bone
    public BoneTrack[] Tracks { get; init; }

    public BonePose[] Sample(float time)
    {
        var poses = new BonePose[Tracks.Length];

        for (int i = 0; i < Tracks.Length; i++)
        {
            poses[i] = Tracks[i].Evaluate(time);
        }

        return poses;
    }
}

public sealed class BoneTrack
{
    public int BoneIndex { get; init; }
    public Keyframe<Vector3>[] PositionKeys { get; init; }
    public Keyframe<Quaternion>[] RotationKeys { get; init; }
    public Keyframe<Vector3>[] ScaleKeys { get; init; }

    public BonePose Evaluate(float time)
    {
        return new BonePose
        {
            Position = EvaluateTrack(PositionKeys, time, Vector3.Lerp),
            Rotation = EvaluateTrack(RotationKeys, time, Quaternion.Slerp),
            Scale = EvaluateTrack(ScaleKeys, time, Vector3.Lerp)
        };
    }
}

public readonly record struct Keyframe<T>(float Time, T Value);
```

### SkeletalAnimator Component

```csharp
[Component]
public partial struct SkeletalAnimator
{
    public Skeleton Skeleton;
    public SkeletonAnimation? CurrentAnimation;
    public float Time;
    public float Speed;
    public bool Playing;

    // Blending
    public SkeletonAnimation? BlendTarget;
    public float BlendTime;
    public float BlendDuration;

    // Output pose (computed each frame)
    public BonePose[] CurrentPose;
    public Matrix4x4[] BoneMatrices;  // For GPU skinning
}
```

### Animation Blending

```csharp
public static class AnimationBlender
{
    public static BonePose[] Blend(
        BonePose[] poseA,
        BonePose[] poseB,
        float weight)
    {
        var result = new BonePose[poseA.Length];

        for (int i = 0; i < poseA.Length; i++)
        {
            result[i] = BonePose.Lerp(poseA[i], poseB[i], weight);
        }

        return result;
    }

    public static BonePose[] BlendAdditive(
        BonePose[] basePose,
        BonePose[] additivePose,
        float weight)
    {
        var result = new BonePose[basePose.Length];

        for (int i = 0; i < basePose.Length; i++)
        {
            // Additive: add delta from reference pose
            result[i] = new BonePose
            {
                Position = basePose[i].Position + additivePose[i].Position * weight,
                Rotation = Quaternion.Slerp(
                    basePose[i].Rotation,
                    basePose[i].Rotation * additivePose[i].Rotation,
                    weight),
                Scale = basePose[i].Scale * Vector3.Lerp(Vector3.One, additivePose[i].Scale, weight)
            };
        }

        return result;
    }
}
```

---

## Property Animation

### PropertyAnimation Asset

```csharp
public sealed class PropertyAnimation : IAnimationClip
{
    public string Name { get; init; }
    public float Duration { get; init; }
    public WrapMode WrapMode { get; init; }

    public AnimationTrack[] Tracks { get; init; }
}

public abstract class AnimationTrack
{
    public string TargetPath { get; init; }  // Component.Property path
    public abstract void Apply(Entity entity, float time, IWorld world);
}

public sealed class FloatTrack : AnimationTrack
{
    public AnimationCurve Curve { get; init; }

    public override void Apply(Entity entity, float time, IWorld world)
    {
        float value = Curve.Evaluate(time);
        // Apply to component...
    }
}

public sealed class Vector3Track : AnimationTrack
{
    public AnimationCurve X { get; init; }
    public AnimationCurve Y { get; init; }
    public AnimationCurve Z { get; init; }

    public override void Apply(Entity entity, float time, IWorld world)
    {
        var value = new Vector3(
            X.Evaluate(time),
            Y.Evaluate(time),
            Z.Evaluate(time)
        );
        // Apply to component...
    }
}

public sealed class ColorTrack : AnimationTrack
{
    public Gradient Gradient { get; init; }

    public override void Apply(Entity entity, float time, IWorld world)
    {
        var color = Gradient.Evaluate(time / Duration);
        // Apply to component...
    }
}
```

### Tween API

For simple one-off animations, provide a tween helper:

```csharp
public static class Tween
{
    public static TweenBuilder<float> To(Entity entity, float target, float duration)
        => new(entity, target, duration);

    public static TweenBuilder<Vector3> To(Entity entity, Vector3 target, float duration)
        => new(entity, target, duration);

    public static TweenBuilder<Color> To(Entity entity, Color target, float duration)
        => new(entity, target, duration);
}

public class TweenBuilder<T>
{
    private readonly Entity entity;
    private readonly T target;
    private readonly float duration;
    private EasingFunction easing = Easing.Linear;
    private Action<Entity, T>? setter;

    public TweenBuilder<T> SetEasing(EasingFunction easing)
    {
        this.easing = easing;
        return this;
    }

    public TweenBuilder<T> OnUpdate(Action<Entity, T> setter)
    {
        this.setter = setter;
        return this;
    }

    public void Start(IWorld world)
    {
        // Create tween component on entity
        // TweenSystem will process it
    }
}

// Usage
Tween.To(entity, new Vector3(100, 200, 0), 0.5f)
     .SetEasing(Easing.EaseOutBack)
     .OnUpdate((e, v) => world.Get<Transform3D>(e).Position = v)
     .Start(world);
```

### Easing Functions

```csharp
public delegate float EasingFunction(float t);

public static class Easing
{
    public static float Linear(float t) => t;

    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
    public static float EaseInOutQuad(float t) =>
        t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;

    public static float EaseInCubic(float t) => t * t * t;
    public static float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);

    public static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return c3 * t * t * t - c1 * t * t;
    }

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1;
        return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
    }

    public static float EaseOutElastic(float t)
    {
        const float c4 = 2 * MathF.PI / 3;
        return t == 0 ? 0 : t == 1 ? 1 :
            MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
    }

    public static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1 / d1) return n1 * t * t;
        if (t < 2 / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5 / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }
}
```

---

## State Machines

### AnimationStateMachine Asset

```csharp
public sealed class AnimationStateMachine
{
    public string Name { get; init; }
    public AnimationState[] States { get; init; }
    public AnimationTransition[] Transitions { get; init; }
    public int DefaultStateIndex { get; init; }

    // Parameters that drive transitions
    public AnimationParameter[] Parameters { get; init; }
}

public sealed class AnimationState
{
    public string Name { get; init; }
    public IAnimationClip Animation { get; init; }
    public float Speed { get; init; } = 1f;

    // Optional blend tree instead of single animation
    public BlendTree? BlendTree { get; init; }
}

public sealed class AnimationTransition
{
    public int FromStateIndex { get; init; }
    public int ToStateIndex { get; init; }

    public TransitionCondition[] Conditions { get; init; }

    public float Duration { get; init; } = 0.25f;  // Blend duration
    public float ExitTime { get; init; }           // Normalized time to exit (0 = any time)
    public bool HasExitTime { get; init; }
}

public readonly record struct TransitionCondition(
    string ParameterName,
    ConditionMode Mode,
    float Threshold
);

public enum ConditionMode
{
    Greater,
    Less,
    Equals,
    NotEquals,
    True,
    False
}
```

### StateMachineController Component

```csharp
[Component]
public partial struct StateMachineController
{
    public AnimationStateMachine StateMachine;
    public int CurrentStateIndex;
    public float StateTime;

    // Active transition
    public int? TransitionIndex;
    public float TransitionTime;

    // Parameters
    public Dictionary<string, AnimationParameterValue> Parameters;
}

public struct AnimationParameterValue
{
    public AnimationParameterType Type;
    public float FloatValue;
    public int IntValue;
    public bool BoolValue;
}

public enum AnimationParameterType
{
    Float,
    Int,
    Bool,
    Trigger  // Auto-resets after consumed
}
```

### Usage Example

```csharp
// Set up character animation
var stateMachine = new AnimationStateMachine
{
    Name = "Character",
    Parameters = [
        new("Speed", AnimationParameterType.Float),
        new("IsGrounded", AnimationParameterType.Bool),
        new("Jump", AnimationParameterType.Trigger)
    ],
    States = [
        new AnimationState { Name = "Idle", Animation = idleAnim },
        new AnimationState { Name = "Walk", Animation = walkAnim },
        new AnimationState { Name = "Run", Animation = runAnim },
        new AnimationState { Name = "Jump", Animation = jumpAnim }
    ],
    Transitions = [
        // Idle -> Walk when Speed > 0.1
        new AnimationTransition {
            FromStateIndex = 0, ToStateIndex = 1,
            Conditions = [new("Speed", ConditionMode.Greater, 0.1f)]
        },
        // Walk -> Run when Speed > 0.5
        new AnimationTransition {
            FromStateIndex = 1, ToStateIndex = 2,
            Conditions = [new("Speed", ConditionMode.Greater, 0.5f)]
        },
        // Any -> Jump when Jump trigger
        new AnimationTransition {
            FromStateIndex = -1, ToStateIndex = 3,  // -1 = any state
            Conditions = [new("Jump", ConditionMode.True, 0)]
        }
    ]
};

// In game code
ref var controller = ref world.Get<StateMachineController>(player);
controller.Parameters["Speed"].FloatValue = velocity.Length();
controller.Parameters["Jump"].BoolValue = true; // Trigger jump
```

---

## Implementation Plan

### Phase 1: Sprite Animation

1. Create `KeenEyes.Animation` project
2. Implement SpriteAnimation asset
3. Implement SpriteAnimator component
4. Create SpriteAnimationSystem
5. Animation events

**Milestone:** 2D sprite animations working

### Phase 2: Property Animation

1. Implement AnimationCurve
2. Implement Easing functions
3. Create Tween API
4. Property animation tracks
5. PropertyAnimationSystem

**Milestone:** Tween any value

### Phase 3: Skeletal Animation

1. Skeleton and Bone structures
2. SkeletonAnimation asset
3. SkeletalAnimator component
4. Bone pose calculation
5. Animation blending

**Milestone:** Basic skeletal animation

### Phase 4: State Machines

1. AnimationStateMachine asset
2. State and Transition definitions
3. StateMachineController component
4. StateMachineSystem
5. Blend trees

**Milestone:** Full animation state machines

### Phase 5: Advanced Features

1. Animation layers
2. IK (Inverse Kinematics)
3. Root motion
4. Animation compression

---

## Open Questions

1. **Asset Format** - JSON, binary, or custom for animation clips?
2. **Spine/DragonBones** - Import from popular 2D tools?
3. **glTF** - Import 3D animations from glTF?
4. **Runtime Retargeting** - Apply animations to different skeletons?
5. **GPU Skinning** - Compute shader for bone transforms?
6. **Animation Events** - How to dispatch to user code?

---

## Related Issues

- Milestone #20: Animation System
- Issue #424: Create KeenEyes.Animation with sprite animation
- Issue #425: Implement skeletal animation and state machines
