using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Animation.Systems;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Tests for the RootMotion component and RootMotionSystem.
/// </summary>
public class RootMotionTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Helpers

    private World CreateWorldWithRootMotion()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableRootMotion = true }));
        return world;
    }

    private static AnimationClip CreateLinearPositionClip(
        string name, float duration, Vector3 endPosition, WrapMode wrapMode, string boneName = "Root")
    {
        var positionCurve = new Vector3Curve();
        positionCurve.AddKeyframe(0f, Vector3.Zero);
        positionCurve.AddKeyframe(duration, endPosition);

        var clip = new AnimationClip { Name = name, Duration = duration, WrapMode = wrapMode };
        clip.AddBoneTrack(new BoneTrack { BoneName = boneName, PositionCurve = positionCurve });
        return clip;
    }

    private static AnimationClip CreateLinearYawClip(
        string name, float duration, float endYawRadians, WrapMode wrapMode, string boneName = "Root")
    {
        var rotationCurve = new QuaternionCurve();
        rotationCurve.AddKeyframe(0f, Quaternion.Identity);
        rotationCurve.AddKeyframe(duration, Quaternion.CreateFromAxisAngle(Vector3.UnitY, endYawRadians));

        var clip = new AnimationClip { Name = name, Duration = duration, WrapMode = wrapMode };
        clip.AddBoneTrack(new BoneTrack { BoneName = boneName, RotationCurve = rotationCurve });
        return clip;
    }

    #endregion

    #region AnimationPlayer Extraction Tests

    [Fact]
    public void Update_ForwardWalkClipFullPlaythrough_MovesEntityByClipDisplacement()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 2f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root"))
            .Build();

        var bone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 10; i++)
        {
            playerSystem.Update(0.1f);
            poseSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        // Sum of per-frame deltas equals the clip's total root displacement
        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.X.ShouldBe(0f, 0.001f);
        transform.Position.Y.ShouldBe(0f, 0.001f);
        transform.Position.Z.ShouldBe(2f, 0.001f);

        // Root bone's animated translation is fully suppressed
        ref readonly var boneTransform = ref world.Get<Transform3D>(bone);
        boneTransform.Position.Length().IsApproximatelyZero().ShouldBeTrue();
    }

    [Fact]
    public void Update_LoopWrapBetweenFrames_ProducesContinuousMotion()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 1f), WrapMode.Loop));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root"))
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(rootMotionSystem);

        // dt = 0.4: the third frame wraps (1.2 -> 0.2) and must not teleport back
        var previousZ = 0f;
        for (var i = 0; i < 3; i++)
        {
            playerSystem.Update(0.4f);
            rootMotionSystem.Update(0.4f);

            ref readonly var transform = ref world.Get<Transform3D>(root);
            var frameDelta = transform.Position.Z - previousZ;
            frameDelta.ShouldBe(0.4f, 0.001f);
            previousZ = transform.Position.Z;
        }

        previousZ.ShouldBe(1.2f, 0.001f);
    }

    [Fact]
    public void Update_PlanarMode_LeavesEntityYUnchangedWhileBoneYAnimates()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("WalkRamp", 1f, new Vector3(0f, 3f, 2f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root") with { PlanarOnly = true })
            .Build();

        var bone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 5; i++)
        {
            playerSystem.Update(0.1f);
            poseSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        // Entity: horizontal motion applied, vertical motion suppressed
        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.Y.IsApproximatelyZero().ShouldBeTrue();
        transform.Position.Z.ShouldBe(1f, 0.001f);

        // Bone: keeps its animated Y translation, X/Z zeroed
        ref readonly var boneTransform = ref world.Get<Transform3D>(bone);
        boneTransform.Position.X.IsApproximatelyZero().ShouldBeTrue();
        boneTransform.Position.Y.ShouldBe(1.5f, 0.001f);
        boneTransform.Position.Z.IsApproximatelyZero().ShouldBeTrue();
    }

    [Fact]
    public void Update_ExposeMode_PopulatesDeltaFieldsWithoutMovingEntity()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 2f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root") with { Mode = RootMotionMode.ExposeDelta })
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 3; i++)
        {
            playerSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);

            // Delta fields populated each frame; entity untouched
            ref readonly var rootMotion = ref world.Get<RootMotion>(root);
            rootMotion.DeltaPosition.Z.ShouldBe(0.2f, 0.001f);

            ref readonly var transform = ref world.Get<Transform3D>(root);
            transform.Position.Length().IsApproximatelyZero().ShouldBeTrue();
        }
    }

    [Fact]
    public void Update_ApplyModeWithRotation_RotatesEntityByClipYaw()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearYawClip("Turn", 1f, MathF.PI / 2f, WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root"))
            .Build();

        var bone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 10; i++)
        {
            playerSystem.Update(0.1f);
            poseSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        // Accumulated rotation deltas equal the clip's total 90-degree yaw
        ref readonly var transform = ref world.Get<Transform3D>(root);
        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        MathF.Abs(Quaternion.Dot(transform.Rotation, expected)).ShouldBeGreaterThan(0.9999f);

        // Root bone's animated rotation is suppressed to identity
        ref readonly var boneTransform = ref world.Get<Transform3D>(bone);
        MathF.Abs(Quaternion.Dot(boneTransform.Rotation, Quaternion.Identity)).ShouldBeGreaterThan(0.9999f);
    }

    [Fact]
    public void Update_EntityRotated_TransformsDeltaIntoEntitySpace()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 1f), WrapMode.Once));

        // Entity faces +90 degrees yaw: local +Z motion becomes world +X motion
        var initialRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var root = world.Spawn()
            .With(new Transform3D(Vector3.Zero, initialRotation, Vector3.One))
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root") with { ApplyRotation = false })
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 10; i++)
        {
            playerSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.X.ShouldBe(1f, 0.001f);
        transform.Position.Y.ShouldBe(0f, 0.001f);
        transform.Position.Z.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void Update_PositionScale_ScalesDisplacement()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 1f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root") with { PositionScale = 2f })
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(rootMotionSystem);

        playerSystem.Update(0.1f);
        rootMotionSystem.Update(0.1f);

        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.Z.ShouldBe(0.2f, 0.001f);
    }

    #endregion

    #region Animator Crossfade Tests

    [Fact]
    public void Update_AnimatorCrossfade_BlendsDeltasByTransitionWeight()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();

        // Walk: 1 unit/s along Z. Run: 3 units/s along Z.
        var walkClipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 2f, new Vector3(0f, 0f, 2f), WrapMode.Loop));
        var runClipId = manager.RegisterClip(
            CreateLinearPositionClip("Run", 2f, new Vector3(0f, 0f, 6f), WrapMode.Loop));

        var controller = new AnimatorController { Name = "Locomotion" };
        var walkState = new AnimatorState { Name = "Walk", ClipId = walkClipId };
        walkState.AddTransition("Run", duration: 1f);
        var runState = new AnimatorState { Name = "Run", ClipId = runClipId };
        controller.AddState(walkState, isDefault: true);
        controller.AddState(runState);
        var controllerId = manager.RegisterController(controller);

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(Animator.ForController(controllerId))
            .With(RootMotion.ForBone("Root"))
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var animatorSystem = new AnimatorSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(animatorSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        // Frame 1: pure walk (1 unit/s * 0.1s)
        animatorSystem.Update(0.1f);
        poseSystem.Update(0.1f);
        rootMotionSystem.Update(0.1f);

        ref readonly var afterWalk = ref world.Get<Transform3D>(root);
        afterWalk.Position.Z.ShouldBe(0.1f, 0.001f);

        // Frame 2: crossfading to run with TransitionProgress = 0.1
        ref var animator = ref world.Get<Animator>(root);
        animator.TriggerStateHash = Animator.GetStateHash("Run");

        animatorSystem.Update(0.1f);
        poseSystem.Update(0.1f);
        rootMotionSystem.Update(0.1f);

        // Blended delta = lerp(walkDelta, runDelta, 0.1) = lerp(0.1, 0.3, 0.1) = 0.12,
        // the same weight the pose crossfade uses
        ref readonly var afterBlend = ref world.Get<Transform3D>(root);
        afterBlend.Position.Z.ShouldBe(0.22f, 0.001f);
    }

    #endregion

    #region Disabled / Opt-Out Tests

    [Fact]
    public void Update_DisabledComponent_DoesNotMoveEntityOrSuppressBone()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 2f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .With(RootMotion.ForBone("Root") with { Enabled = false })
            .Build();

        var bone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 5; i++)
        {
            playerSystem.Update(0.1f);
            poseSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        // Entity untouched, bone keeps its animated translation
        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.Length().IsApproximatelyZero().ShouldBeTrue();

        ref readonly var boneTransform = ref world.Get<Transform3D>(bone);
        boneTransform.Position.Z.ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void Update_WithoutRootMotionComponent_LeavesEntityAndBoneUntouched()
    {
        world = CreateWorldWithRootMotion();
        var manager = world.GetExtension<AnimationManager>();
        var clipId = manager.RegisterClip(
            CreateLinearPositionClip("Walk", 1f, new Vector3(0f, 0f, 2f), WrapMode.Once));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var bone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("Root", root.Id))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var poseSystem = new SkeletonPoseSystem();
        var rootMotionSystem = new RootMotionSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(poseSystem);
        world.AddSystem(rootMotionSystem);

        for (var i = 0; i < 5; i++)
        {
            playerSystem.Update(0.1f);
            poseSystem.Update(0.1f);
            rootMotionSystem.Update(0.1f);
        }

        ref readonly var transform = ref world.Get<Transform3D>(root);
        transform.Position.Length().IsApproximatelyZero().ShouldBeTrue();

        ref readonly var boneTransform = ref world.Get<Transform3D>(bone);
        boneTransform.Position.Z.ShouldBe(1f, 0.001f);
    }

    [Fact]
    public void Update_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new RootMotionSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimationConfig_Default_RootMotionDisabled()
    {
        var config = AnimationConfig.Default;

        config.EnableRootMotion.ShouldBeFalse();
    }

    #endregion
}
