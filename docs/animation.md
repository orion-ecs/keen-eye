# Animation

The `KeenEyes.Animation` library provides ECS-native animation support: skeletal keyframe playback, animator state machines, sprite sheet animation, and property tweening. Animation assets (clips, sprite sheets, controllers) are plain data registered with a manager and referenced from components by integer ID, keeping components pure data per ECS principles.

## Overview

Animation state in KeenEyes always follows the same split:

- **Assets** (`AnimationClip`, `SpriteSheet`, `AnimatorController`) are shared, immutable-ish data owned by `AnimationManager`.
- **Components** (`AnimationPlayer`, `Animator`, `SpriteAnimator`, tween components) hold per-entity playback state and reference an asset by its integer ID (`ClipId`, `SheetId`, `ControllerId`).
- **Systems** advance playback time each frame, sample curves, and write results (bone transforms, sprite source rectangles, tween values).

This means multiple entities can share one `AnimationClip` or `SpriteSheet` while each tracks its own time, speed, and weight independently.

## Quick Start

### Installation

```csharp
using KeenEyes.Animation;

using var world = new World();

// Install with default configuration
world.InstallPlugin(new AnimationPlugin());
```

`AnimationPlugin.Install` registers the following components:

- `AnimationPlayer`, `Animator`, `SpriteAnimator`, `BoneReference`
- `TweenFloat`, `TweenVector2`, `TweenVector3`, `TweenVector4`

and the following systems, all in `SystemPhase.Update` (order matters, since later systems depend on earlier ones having run):

| Order | System | Responsibility |
|-------|--------|-----------------|
| 50 | `AnimatorSystem` | Advances state-machine time and resolves transitions/crossfades |
| 51 | `AnimationPlayerSystem` | Advances `AnimationPlayer` time and handles wrap modes |
| 52 | `SpriteAnimationSystem` | Advances `SpriteAnimator` time and resolves the current frame |
| 53 | `AnimationEventSystem` | Detects and publishes clip events crossed this frame |
| 55 | `SkeletonPoseSystem` | Samples clips and writes pose data to bone `Transform3D`s |
| 60 | `TweenSystem` | Advances all tween components and computes `CurrentValue` |

It also creates an `AnimationManager` and exposes it as a world extension via `context.SetExtension`.

> `SkinnedMeshBoneSystem` (GPU bone matrix computation for `SkinnedMesh` entities) exists in `KeenEyes.Animation.Systems` but is **not** registered by `AnimationPlugin`; add it explicitly with `world.AddSystem<SkinnedMeshBoneSystem>(...)` if you render skinned meshes. Similarly, the IK components (`IKRig`, `IKChainReference`, `IKConstraint`, `IKTarget`) and `LookAtTarget` are not registered by the plugin and have no bundled solver systems yet — see [IK Rigs](#ik-rigs-unwired) below.

### Registering Assets

Access the manager through the world extension and register assets before referencing them from components:

```csharp
var animations = world.GetExtension<AnimationManager>();

var walkClip = new AnimationClip { Name = "Walk", WrapMode = WrapMode.Loop };
// ... populate walkClip.AddBoneTrack(...) ...
var walkClipId = animations.RegisterClip(walkClip);

var runSheet = new SpriteSheet { Name = "Run" };
runSheet.AddGridFrames(columns: 8, rows: 1, frameCount: 8, frameDuration: 0.1f);
var runSheetId = animations.RegisterSpriteSheet(runSheet);
```

`AnimationManager` also exposes `GetClip`/`TryGetClip`, `GetSpriteSheet`/`TryGetSpriteSheet`, `GetController`/`TryGetController`, matching `Unregister*` methods, and `ClipCount`/`SpriteSheetCount`/`ControllerCount` counters.

### Your First Animated Entity

```csharp
using KeenEyes.Animation.Components;

var character = world.Spawn()
    .With(Transform3D.Identity)
    .With(AnimationPlayer.ForClip(walkClipId))
    .Build();

var sprite = world.Spawn()
    .With(new Transform2D(position, 0, Vector2.One))
    .With(SpriteAnimator.ForSheet(runSheetId))
    .Build();

var fading = world.Spawn()
    .With(TweenFloat.Create(1f, 0f, 2f, EaseType.QuadOut))
    .Build();
```

## Core Concepts

### AnimationPlayer

`AnimationPlayer` provides basic single-clip playback: it stores `ClipId`, `Time`, `Speed`, `IsPlaying`, an optional `WrapModeOverride`, a blend `Weight`, and a system-managed `IsComplete` flag. Use the static factories `AnimationPlayer.Default` and `AnimationPlayer.ForClip(clipId, autoPlay)` to construct one. For state-machine-driven playback with multiple clips and transitions, use `Animator` instead.

```csharp
var entity = world.Spawn()
    .With(Transform3D.Identity)
    .With(new AnimationPlayer { ClipId = walkClipId, IsPlaying = true })
    .Build();
```

`AnimationPlayerSystem` advances `Time` each frame by `deltaTime * Speed * clip.Speed` and applies the clip's `WrapMode` (`Once`, `Loop`, `PingPong`, `ClampForever`).

### Animator and AnimatorController

`Animator` is the state-machine component: it references an `AnimatorController` by `ControllerId` and tracks `CurrentStateHash`, `StateTime`, and in-progress transition data (`NextStateHash`, `TransitionProgress`, `TransitionDuration`, `NextStateTime`). Because components must stay pure data, you trigger a transition by setting `TriggerStateHash` directly — `AnimatorSystem` clears it after processing. `Animator.GetStateHash(string)` converts a state name to the hash the component expects.

```csharp
var entity = world.Spawn()
    .With(Transform3D.Identity)
    .With(new Animator { ControllerId = humanoidControllerId })
    .Build();

ref var animator = ref world.Get<Animator>(entity);
animator.TriggerStateHash = Animator.GetStateHash("Jump");
```

An `AnimatorController` is built from `AnimatorState` entries (each with a `ClipId` and `Speed`) connected by `AnimatorTransition`s (`TargetStateHash`, crossfade `Duration`, optional normalized `ExitTime`, and an `EaseType`):

```csharp
var controller = new AnimatorController { Name = "Humanoid" };

var idle = new AnimatorState { Name = "Idle", ClipId = idleClipId };
idle.AddTransition("Jump", duration: 0.15f);

var jump = new AnimatorState { Name = "Jump", ClipId = jumpClipId };
jump.AddTransition("Idle", duration: 0.2f, exitTime: 0.9f);

controller.AddState(idle, isDefault: true);
controller.AddState(jump);

var controllerId = animations.RegisterController(controller);
```

`AnimatorSystem` advances `StateTime`, resolves a triggered or exit-time transition into a crossfade, and blends `TransitionProgress` toward 1 before completing the switch. `SkeletonPoseSystem` reads that state, samples both the current and (if transitioning) next clip's bone tracks, and blends them with `Vector3.Lerp`/`Quaternion.Slerp`.

### Skeletal Animation: BoneReference and AnimationClip

Skeleton bones are ordinary entities marked with `BoneReference { BoneName, SkeletonRootId }` and a `Transform3D`, parented under the skeleton root (the entity carrying `AnimationPlayer` or `Animator`):

```csharp
var hip = world.Spawn()
    .With(Transform3D.Identity)
    .With(new BoneReference { BoneName = "Hip", SkeletonRootId = characterEntity.Id })
    .Build();
```

An `AnimationClip` holds one `BoneTrack` per animated bone (added with `AddBoneTrack`), each with optional `PositionCurve`/`RotationCurve`/`ScaleCurve` and a `Sample(time, out position, out rotation, out scale)` method. `SkeletonPoseSystem` queries all `BoneReference` + `Transform3D` entities each frame, looks up the sampled pose for their `SkeletonRootId`, and writes `Position`/`Rotation`/`Scale` onto the bone's `Transform3D`.

`SkeletonFactory.InstantiateSkeleton(world, bones, rootEntity)` is a helper for building a bone hierarchy from `BoneSetupData[]` (bone name, parent index, bind-pose matrix) in one call, returning the bone entity IDs in skeleton order; `SkeletonFactory.DespawnSkeleton` and `SkeletonFactory.FindBoneByName` round out cleanup and lookup.

### Skinned Mesh Rendering

`SkinnedMesh` marks a mesh entity for GPU skinning: it carries `MeshAssetId`, `BoneEntityIds` (in skeleton order), `InverseBindMatrices`, and a `Generation` counter. `SkinnedMeshBoneSystem` (not auto-registered — see above) walks each bone entity's world transform, multiplies by its inverse bind matrix, and stores the result in a `BoneMatrixBuffer`, which tracks per-bone generations so only changed matrices need re-uploading to the GPU.

```csharp
var character = world.Spawn()
    .With(Transform3D.Identity)
    .With(SkinnedMesh.Create(meshAssetId, boneEntityIds, inverseBindMatrices))
    .Build();
```

### Sprite Animation

`SpriteAnimator` drives frame-based 2D animation against a `SpriteSheet` asset: `SheetId`, `Time`, `Speed`, `IsPlaying`, an optional `WrapModeOverride`, and system-managed `CurrentFrame`/`SourceRect`/`IsComplete` outputs. Build a sheet with `AddFrame` (one frame at a time) or `AddGridFrames` (uniform grid layout), then reference it by ID:

```csharp
var runSheet = new SpriteSheet { Name = "Run", WrapMode = WrapMode.Loop };
runSheet.AddGridFrames(columns: 8, rows: 1, frameCount: 8, frameDuration: 0.1f);
var runSheetId = animations.RegisterSpriteSheet(runSheet);

var entity = world.Spawn()
    .With(new Transform2D(position, 0, Vector2.One))
    .With(new SpriteAnimator { SheetId = runSheetId, IsPlaying = true })
    .Build();
```

`SpriteAnimationSystem` advances `Time`, resolves the wrap mode, and calls `sheet.GetFrameAtTime` to update `CurrentFrame` and `SourceRect` — the latter can be fed directly to `I2DRenderer.DrawTextureRegion`.

### Animation Events

`AnimationClip.Events` is an `AnimationEventTrack` of named, timed `AnimationEvent`s (`Time`, `EventName`, optional `Parameter`), added with `AddEvent`. Each frame, `AnimationEventSystem` compares each playing `AnimationPlayer`'s `PreviousTime`/`Time` against the clip's events and publishes an `AnimationEventTriggeredEvent` (`Entity`, `EventName`, `Parameter`, `Time`, `ClipId`) for every event crossed:

```csharp
walkClip.Events.AddEvent(0.3f, "footstep");

world.Events.Subscribe<AnimationEventTriggeredEvent>(e =>
{
    if (e.EventName == "footstep")
    {
        PlayFootstepSound(e.Entity, e.Parameter);
    }
});
```

Event dispatch can be disabled via `AnimationConfig.EnableEvents` if you don't need it.

### Property Tweening

`TweenFloat`, `TweenVector2`, `TweenVector3`, and `TweenVector4` interpolate a value from `StartValue` to `EndValue` over `Duration` seconds, with an `EaseType`, and optional `Loop`/`PingPong`. Each exposes a static `Create(start, end, duration, ease)` factory and writes its result to `CurrentValue` (also tracking `ElapsedTime` and `IsComplete`):

```csharp
using KeenEyes.Animation.Tweening;

var fading = world.Spawn()
    .With(TweenFloat.Create(1f, 0f, 2f, EaseType.QuadOut))
    .Build();

// Later, in a user system, read CurrentValue and apply it to whatever it drives:
ref readonly var tween = ref world.Get<TweenFloat>(fading);
var alpha = tween.CurrentValue;
```

`TweenSystem` advances all four tween component types every frame and evaluates the easing curve via `Easing.Evaluate(EaseType, t)`. `EaseType` covers linear plus in/out/in-out variants of quadratic, cubic, quartic, quintic, sine, exponential, circular, elastic, back, and bounce curves. `AnimationConfig.MaxTweensPerEntity` documents an intended per-entity cap (default 16) but is not currently enforced by `TweenSystem`.

### IK Rigs (unwired)

`KeenEyes.Animation.IK` and the `IKManager` class provide the data model for inverse-kinematics rigs: `IKRigDefinition` (a named collection of `IKChainDefinition`s, built with `AddTwoBoneChain`/`AddFABRIKChain`), `IKChainDefinition` (bone names root-to-tip, `IKSolverType` — `TwoBone`, `FABRIK`, `CCD`, or `LookAt` — iteration/tolerance settings), and the `IKRig`, `IKChainReference`, `IKConstraint`, and `IKTarget` components that would attach a rig to a skeleton entity. `IKManager` registers rigs/chains and looks up solver instances by `IKSolverType` via `RegisterSolver`/`GetSolver`/`GetSolverForType`.

As of this writing, **`AnimationPlugin` does not register the IK components, does not create/expose an `IKManager` extension, and ships no `IIKSolver` implementations or IK system** — the types exist as a foundation for a future feature. If you want to use them today, construct your own `IKManager`, register solver implementations of `IIKSolver`, register the IK components with `world.Components.Register<T>()`, and drive solving from a custom system.

## Performance

- **Per-frame caching:** `SkeletonPoseSystem` builds a per-frame `Dictionary` cache of active `AnimationPlayer`/`Animator` states before iterating bones, so sampling cost for a skeleton's clip is paid once regardless of bone count.
- **Dirty-tracked GPU uploads:** `BoneMatrixBuffer` only marks itself dirty when a bone's generation counter advances, and `GetDirtyBoneIndices`/`GetDirtyBoneCount` let a render system upload only the bones that actually changed.
- **Shared assets:** Because clips, sprite sheets, and controllers are registered once in `AnimationManager` and referenced by ID, many entities can share the same animation data without duplicating curve or frame data per entity.
- **Disable unused features:** Set `AnimationConfig.EnableEvents = false` if you don't use animation events, to skip event-range checks in `AnimationEventSystem`.

## Next Steps

- [Plugins Guide](plugins.md) - How plugins work
- [Systems Guide](systems.md) - System design patterns
- [Components Guide](components.md) - Component registration and builders
- [Animation System Design](research/animation-system.md) - Original design document (aspirational; the shipped implementation differs in several details, e.g. ID-based asset references rather than direct object references)
