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
- With `AnimationConfig.EnableIK`: `IKRig`, `IKChainReference`, `IKTarget`, `IKConstraint`
- With `AnimationConfig.EnableGpuSkinning`: `SkinnedMesh`

and the following systems, all in `SystemPhase.Update` (order matters, since later systems depend on earlier ones having run):

| Order | System | Responsibility |
|-------|--------|-----------------|
| 50 | `AnimatorSystem` | Advances state-machine time and resolves transitions/crossfades |
| 51 | `AnimationPlayerSystem` | Advances `AnimationPlayer` time and handles wrap modes |
| 52 | `SpriteAnimationSystem` | Advances `SpriteAnimator` time and resolves the current frame |
| 53 | `AnimationEventSystem` | Detects and publishes clip events crossed this frame |
| 55 | `SkeletonPoseSystem` | Samples clips and writes pose data to bone `Transform3D`s |
| 57 | `IKSolverSystem` | Solves IK chains on top of the FK pose (only with `EnableIK`) |
| 60 | `TweenSystem` | Advances all tween components and computes `CurrentValue` |
| 80 | `SkinnedMeshBoneSystem` | Computes GPU bone matrices from final transforms (only with `EnableGpuSkinning`) |

It also creates an `AnimationManager` and exposes it as a world extension via `context.SetExtension`. With `EnableIK` it additionally exposes an `IKManager` extension preloaded with the `TwoBone` and `FABRIK` solvers.

> Both `EnableIK` and `EnableGpuSkinning` default to **false**: worlds only pay for the IK and skinning systems when they opt in. `LookAtTarget` is still not registered by the plugin and has no bundled system yet. See [Inverse Kinematics](#inverse-kinematics) below for IK usage.

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

`SkinnedMesh` marks a mesh entity for GPU skinning: it carries `MeshAssetId`, `BoneEntityIds` (in skeleton order), `InverseBindMatrices`, and a `Generation` counter. `SkinnedMeshBoneSystem` (registered at order 80 when `AnimationConfig.EnableGpuSkinning` is set — see above) walks each bone entity's world transform, multiplies by its inverse bind matrix, and stores the result in a `BoneMatrixBuffer`, which tracks per-bone generations so only changed matrices need re-uploading to the GPU.

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

### Inverse Kinematics

`KeenEyes.Animation.IK` and the `IKManager` class provide the data model for inverse-kinematics rigs: `IKRigDefinition` (a named collection of `IKChainDefinition`s, built with `AddTwoBoneChain`/`AddFABRIKChain`), `IKChainDefinition` (bone names root-to-tip, `IKSolverType` — `TwoBone`, `FABRIK`, `CCD`, or `LookAt` — iteration/tolerance settings), and the `IKRig`, `IKChainReference`, `IKConstraint`, and `IKTarget` components that attach a rig to a skeleton. `IKManager` registers rigs/chains and looks up solver instances by `IKSolverType` via `RegisterSolver`/`GetSolver`/`GetSolverForType`.

IK is opt-in: install the plugin with `AnimationConfig.EnableIK = true` and the plugin registers the four IK components, exposes an `IKManager` world extension with the two bundled `IIKSolver` implementations (`TwoBoneSolver` for three-bone limbs, `FABRIKSolver` for chains of two or more bones), and adds `IKSolverSystem` at order 57 — after `SkeletonPoseSystem` (order 55) writes the FK pose, so IK layers on top of the sampled animation.

```csharp
world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableIK = true }));

// 1. Register the chain (bone names root-to-tip must match the BoneReference names)
var ik = world.GetExtension<IKManager>();
var chainId = ik.RegisterChain(
    IKChainDefinition.TwoBone("LeftArm", "UpperArm.L", "Forearm.L", "Hand.L", Vector3.UnitZ));

// 2. Spawn a target entity holding the world-space goal
var target = world.Spawn()
    .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
    .Build();

// 3. Attach the chain reference to the end-effector bone (the tip, e.g. the hand)
world.Add(handBone, IKChainReference.ForChain(chainId, target.Id));

world.Update(deltaTime);  // FK pose at order 55, IK solve at order 57
```

Each frame, `IKSolverSystem` queries entities with `IKChainReference` + `BoneReference` + `Transform3D`, resolves the chain definition from `IKManager`, and collects the chain's bones by walking the entity hierarchy up from the end effector, validating each bone's `BoneReference.BoneName` against the definition's `BoneNames`. The solver is chosen by the chain's `IKSolverType`, falling back to FABRIK when the configured solver is unavailable or cannot handle the chain's bone count (e.g. a `TwoBone` chain that is not exactly three bones); chains with no usable solver are skipped. If the target's `PoleTargetEntityId` refers to a live entity with a `Transform3D`, its world position is passed to the solver as the pole; otherwise the chain's `PoleVector` applies. Bones carrying an `IKConstraint` component have their joint limits applied by the solvers automatically.

**FK/IK blending:** the effective weight is `IKRig.GlobalWeight × IKChainReference.Weight × IKTarget.Weight`, clamped to [0, 1]. The `IKRig` component is looked up on the end effector's `SkeletonRootId` entity and is optional (its absence means global weight 1). Weight 0 skips the chain entirely, leaving the FK pose untouched; weight 1 applies the full IK result; anything in between slerps each bone's local rotation from the FK pose toward the IK pose.

**Graceful degradation:** disabled rigs/chains (`IKRig.Enabled`, `IKChainReference.Enabled`), missing or despawned target entities, unregistered chain IDs, and incomplete or misnamed bone hierarchies are all skipped without throwing — the FK pose is never corrupted by a misconfigured chain.

Note that the `CCD` and `LookAt` solver types have no bundled implementations yet; chains configured with them fall back to FABRIK (register your own `IIKSolver` with `IKManager.RegisterSolver` to override). `LookAtTarget` is likewise not yet driven by any bundled system.

### Root Motion

Root motion extracts the movement of a designated root bone (typically "Root" or "Hips") from the playing animation and delivers it to the skeleton root entity, so walk/run/turn clips drive the character through the world instead of playing in place.

Root motion is opt-in: install the plugin with `AnimationConfig.EnableRootMotion = true` and the plugin registers the `RootMotion` component and adds `RootMotionSystem` at order 56 — directly after `SkeletonPoseSystem` (order 55) writes the FK pose and before IK (order 57) consumes bone transforms.

```csharp
world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableRootMotion = true }));

var character = world.Spawn()
    .With(Transform3D.Identity)
    .With(AnimationPlayer.ForClip(walkClipId))
    .With(RootMotion.ForBone("Root"))
    .Build();
```

Each frame, `RootMotionSystem` samples the root bone's track at the previous and current playback times and computes the frame delta (position by difference, rotation as `current * inverse(previous)`). The delta is transformed into the entity's space using its current orientation, scaled by `PositionScale`/`RotationScale`, and delivered according to `Mode`:

- **`RootMotionMode.ApplyToEntity`** (default): the delta is applied directly to the entity's `Transform3D`.
- **`RootMotionMode.ExposeDelta`**: the entity is left untouched and the delta is written to `RootMotion.DeltaPosition`/`DeltaRotation` for a character controller or physics system to consume (the fields are populated in both modes).

The root bone's animated local translation (and rotation, when `ApplyRotation` is set) is suppressed after extraction so the motion isn't applied twice. With `PlanarOnly`, the delta is restricted to the XZ plane and the bone keeps its animated Y translation — vertical motion (crouching, bobbing) stays in the skeleton while horizontal motion drives the entity. `ApplyPosition`/`ApplyRotation` select which channels are extracted at all.

Looping is handled seamlessly: when `WrapMode.Loop` playback wraps between frames, the delta accumulates `[previousTime → clip end]` plus `[clip start → currentTime]`, so the loop seam produces continuous motion with no teleport back. Under an `Animator` crossfade, both clips' root deltas are blended with the same weight (`TransitionProgress`) that the pose blend uses, so root velocity matches the blended pose and feet don't slide during transitions.

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
