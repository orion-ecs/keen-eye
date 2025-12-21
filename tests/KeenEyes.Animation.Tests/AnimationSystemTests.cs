using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Animation.Events;
using KeenEyes.Animation.Systems;
using KeenEyes.Animation.Tweening;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Tests for animation systems.
/// </summary>
public class AnimationSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region AnimationPlayerSystem Tests

    [Fact]
    public void AnimationPlayerSystem_Initialize_FindsAnimationManager()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        // Should not throw during update
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimationPlayerSystem_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimationPlayerSystem_AdvancesPlaybackTime()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0.5f, 0.001f);
    }

    [Fact]
    public void AnimationPlayerSystem_AppliesSpeedMultiplier()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId) with { Speed = 2f })
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(1f, 0.001f); // 0.5f * 2f = 1f
    }

    [Fact]
    public void AnimationPlayerSystem_AppliesClipSpeed()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f, Speed = 0.5f };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(1f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0.5f, 0.001f); // 1f * 1f * 0.5f = 0.5f
    }

    [Fact]
    public void AnimationPlayerSystem_WrapModeOnce_StopsAtEnd()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 1f, WrapMode = WrapMode.Once };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(2f); // Past the clip duration

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(1f, 0.001f);
        player.IsComplete.ShouldBeTrue();
        player.IsPlaying.ShouldBeFalse();
    }

    [Fact]
    public void AnimationPlayerSystem_WrapModeLoop_Wraps()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 1f, WrapMode = WrapMode.Loop };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(1.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0.5f, 0.001f); // Wrapped
        player.IsComplete.ShouldBeFalse();
        player.IsPlaying.ShouldBeTrue();
    }

    [Fact]
    public void AnimationPlayerSystem_SkipsNotPlayingEntities()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId, autoPlay: false))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0f);
        player.IsPlaying.ShouldBeFalse();
    }

    [Fact]
    public void AnimationPlayerSystem_SkipsInvalidClipId()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(new AnimationPlayer { ClipId = 999, IsPlaying = true, Speed = 1f })
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(0.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0f);
    }

    [Fact]
    public void AnimationPlayerSystem_SavesPreviousTime()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(0.25f);
        system.Update(0.25f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.PreviousTime.ShouldBe(0.25f, 0.001f);
        player.Time.ShouldBe(0.5f, 0.001f);
    }

    [Fact]
    public void AnimationPlayerSystem_WrapModeOverride_TakesPrecedence()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 1f, WrapMode = WrapMode.Once };
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId) with { WrapModeOverride = WrapMode.Loop })
            .Build();

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        system.Update(1.5f);

        ref readonly var player = ref world.Get<AnimationPlayer>(entity);
        player.Time.ShouldBe(0.5f, 0.001f); // Wrapped because override is Loop
        player.IsComplete.ShouldBeFalse();
        player.IsPlaying.ShouldBeTrue();
    }

    #endregion

    #region AnimationEventSystem Tests

    [Fact]
    public void AnimationEventSystem_Initialize_FindsAnimationManager()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var system = new AnimationEventSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimationEventSystem_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new AnimationEventSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimationEventSystem_FiresEventsInTimeRange()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        clip.Events.AddEvent(0.5f, "test_event", "param1");
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var eventSystem = new AnimationEventSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(eventSystem);

        var receivedEvents = new List<AnimationEventTriggeredEvent>();
        using var subscription = world.Subscribe<AnimationEventTriggeredEvent>(e => receivedEvents.Add(e));

        // First frame - advance to 0.25
        playerSystem.Update(0.25f);
        eventSystem.Update(0.25f);

        receivedEvents.Count.ShouldBe(0); // Event at 0.5f not reached

        // Second frame - advance to 0.75 (crosses 0.5f)
        playerSystem.Update(0.5f);
        eventSystem.Update(0.5f);

        receivedEvents.Count.ShouldBe(1);
        receivedEvents[0].Entity.ShouldBe(entity);
        receivedEvents[0].EventName.ShouldBe("test_event");
        receivedEvents[0].Parameter.ShouldBe("param1");
        receivedEvents[0].Time.ShouldBe(0.5f);
        receivedEvents[0].ClipId.ShouldBe(clipId);
    }

    [Fact]
    public void AnimationEventSystem_FiresMultipleEventsInRange()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        clip.Events.AddEvent(0.2f, "event1");
        clip.Events.AddEvent(0.4f, "event2");
        clip.Events.AddEvent(0.6f, "event3");
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        var playerSystem = new AnimationPlayerSystem();
        var eventSystem = new AnimationEventSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(eventSystem);

        var receivedEvents = new List<AnimationEventTriggeredEvent>();
        using var subscription = world.Subscribe<AnimationEventTriggeredEvent>(e => receivedEvents.Add(e));

        // Advance from 0 to 0.5 (crosses 0.2 and 0.4, not 0.6)
        playerSystem.Update(0.5f);
        eventSystem.Update(0.5f);

        receivedEvents.Count.ShouldBe(2);
        receivedEvents[0].EventName.ShouldBe("event1");
        receivedEvents[1].EventName.ShouldBe("event2");
    }

    [Fact]
    public void AnimationEventSystem_SkipsNotPlayingEntities()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        clip.Events.AddEvent(0.5f, "test_event");
        var clipId = manager.RegisterClip(clip);

        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId, autoPlay: false) with { Time = 0.25f, PreviousTime = 0f })
            .Build();

        var eventSystem = new AnimationEventSystem();
        world.AddSystem(eventSystem);

        var receivedEvents = new List<AnimationEventTriggeredEvent>();
        using var subscription = world.Subscribe<AnimationEventTriggeredEvent>(e => receivedEvents.Add(e));

        // Manually set time past event (but not playing)
        ref var player = ref world.Get<AnimationPlayer>(entity);
        player.Time = 0.75f;

        eventSystem.Update(0.5f);

        receivedEvents.Count.ShouldBe(0); // Not playing, so no events
    }

    #endregion

    #region SpriteAnimationSystem Tests

    [Fact]
    public void SpriteAnimationSystem_Initialize_FindsAnimationManager()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var system = new SpriteAnimationSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void SpriteAnimationSystem_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new SpriteAnimationSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void SpriteAnimationSystem_UpdatesFrameIndex()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var sheet = new SpriteSheet { Name = "TestSheet" };
        sheet.AddFrame(new Rectangle(0, 0, 0.25f, 1), 0.1f);
        sheet.AddFrame(new Rectangle(0.25f, 0, 0.25f, 1), 0.1f);
        sheet.AddFrame(new Rectangle(0.5f, 0, 0.25f, 1), 0.1f);
        var sheetId = manager.RegisterSpriteSheet(sheet);

        var entity = world.Spawn()
            .With(SpriteAnimator.ForSheet(sheetId))
            .Build();

        var system = new SpriteAnimationSystem();
        world.AddSystem(system);

        ref readonly var animator = ref world.Get<SpriteAnimator>(entity);
        animator.CurrentFrame.ShouldBe(0);

        // Advance past first frame
        system.Update(0.15f);

        ref readonly var animator2 = ref world.Get<SpriteAnimator>(entity);
        animator2.CurrentFrame.ShouldBe(1);
    }

    [Fact]
    public void SpriteAnimationSystem_SkipsNotPlayingEntities()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var sheet = new SpriteSheet { Name = "TestSheet" };
        sheet.AddFrame(new Rectangle(0, 0, 0.5f, 1), 0.1f);
        sheet.AddFrame(new Rectangle(0.5f, 0, 0.5f, 1), 0.1f);
        var sheetId = manager.RegisterSpriteSheet(sheet);

        var entity = world.Spawn()
            .With(SpriteAnimator.ForSheet(sheetId, autoPlay: false))
            .Build();

        var system = new SpriteAnimationSystem();
        world.AddSystem(system);

        system.Update(0.2f);

        ref readonly var animator = ref world.Get<SpriteAnimator>(entity);
        animator.CurrentFrame.ShouldBe(0);
        animator.Time.ShouldBe(0f);
    }

    #endregion

    #region TweenSystem Tests

    [Fact]
    public void TweenSystem_UpdatesTweenFloat()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenFloat.Create(0f, 10f, 1f, EaseType.Linear))
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var tween = ref world.Get<TweenFloat>(entity);
        tween.CurrentValue.ShouldBe(5f, 0.001f);
        tween.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void TweenSystem_CompletesAtDuration()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenFloat.Create(0f, 10f, 1f, EaseType.Linear))
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(1f);

        ref readonly var tween = ref world.Get<TweenFloat>(entity);
        tween.CurrentValue.ShouldBe(10f, 0.001f);
        tween.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public void TweenSystem_LoopMode_WrapsTime()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenFloat.Create(0f, 10f, 1f, EaseType.Linear) with { Loop = true })
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(1.5f);

        ref readonly var tween = ref world.Get<TweenFloat>(entity);
        tween.CurrentValue.ShouldBe(5f, 0.001f); // Wrapped to 0.5 progress
        tween.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void TweenSystem_PingPongMode_Reverses()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenFloat.Create(0f, 10f, 1f, EaseType.Linear) with { Loop = true, PingPong = true })
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(1.5f);

        ref readonly var tween = ref world.Get<TweenFloat>(entity);
        tween.CurrentValue.ShouldBe(5f, 0.001f); // Reversed, now at 0.5 going back
        tween.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void TweenSystem_UpdatesTweenVector3()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenVector3.Create(Vector3.Zero, new Vector3(10f, 20f, 30f), 1f, EaseType.Linear))
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var tween = ref world.Get<TweenVector3>(entity);
        tween.CurrentValue.X.ShouldBe(5f, 0.001f);
        tween.CurrentValue.Y.ShouldBe(10f, 0.001f);
        tween.CurrentValue.Z.ShouldBe(15f, 0.001f);
    }

    [Fact]
    public void TweenSystem_SkipsCompletedTweens()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var entity = world.Spawn()
            .With(TweenFloat.Create(0f, 10f, 1f, EaseType.Linear) with { IsComplete = true })
            .Build();

        var system = new TweenSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var tween = ref world.Get<TweenFloat>(entity);
        tween.ElapsedTime.ShouldBe(0f); // Not updated because already complete
    }

    #endregion

    #region AnimatorSystem Tests

    [Fact]
    public void AnimatorSystem_Initialize_FindsAnimationManager()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var system = new AnimatorSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimatorSystem_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new AnimatorSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void AnimatorSystem_UpdatesCurrentStateTime()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "IdleClip", Duration = 2f };
        var clipId = manager.RegisterClip(clip);

        var controller = new AnimatorController { Name = "TestController" };
        var idleState = new AnimatorState { Name = "Idle", ClipId = clipId };
        controller.AddState(idleState, isDefault: true);
        var controllerId = manager.RegisterController(controller);

        var entity = world.Spawn()
            .With(Animator.ForController(controllerId))
            .Build();

        var system = new AnimatorSystem();
        world.AddSystem(system);

        system.Update(0.5f);

        ref readonly var animator = ref world.Get<Animator>(entity);
        animator.StateTime.ShouldBe(0.5f, 0.001f);
    }

    [Fact]
    public void AnimatorSystem_HandlesStateWithNoClip()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        var controller = new AnimatorController { Name = "TestController" };
        var idleState = new AnimatorState { Name = "Idle" }; // No clip
        controller.AddState(idleState, isDefault: true);
        var controllerId = manager.RegisterController(controller);

        world.Spawn()
            .With(Animator.ForController(controllerId))
            .Build();

        var system = new AnimatorSystem();
        world.AddSystem(system);

        // Should not crash
        system.Update(0.5f);
    }

    #endregion

    #region SkeletonPoseSystem Tests

    [Fact]
    public void SkeletonPoseSystem_Initialize_FindsAnimationManager()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void SkeletonPoseSystem_WithNoManager_DoesNotCrash()
    {
        world = new World();
        // Don't install animation plugin

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        // Should not throw
        system.Update(1f / 60f);
    }

    [Fact]
    public void SkeletonPoseSystem_AppliesBonePose()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        // Create a clip with a bone track
        var positionCurve = new Vector3Curve();
        positionCurve.AddKeyframe(0f, Vector3.Zero);
        positionCurve.AddKeyframe(1f, new Vector3(10f, 0f, 0f));

        var boneTrack = new BoneTrack
        {
            BoneName = "bone1",
            PositionCurve = positionCurve
        };

        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        clip.AddBoneTrack(boneTrack);
        var clipId = manager.RegisterClip(clip);

        // Create skeleton root entity with AnimationPlayer
        var skeletonRoot = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId) with { Time = 0.5f })
            .Build();

        // Create bone entity linked to skeleton
        var boneEntity = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("bone1", skeletonRoot.Id))
            .Build();

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        ref readonly var boneTransform = ref world.Get<Transform3D>(boneEntity);
        boneTransform.Position.X.ShouldBe(5f, 0.1f); // Halfway between 0 and 10
    }

    [Fact]
    public void SkeletonPoseSystem_SkipsBonesWithNoMatchingTrack()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        // Create a clip with a bone track for "bone1"
        var positionCurve = new Vector3Curve();
        positionCurve.AddKeyframe(0f, new Vector3(10f, 10f, 10f));

        var boneTrack = new BoneTrack
        {
            BoneName = "bone1",
            PositionCurve = positionCurve
        };

        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        clip.AddBoneTrack(boneTrack);
        var clipId = manager.RegisterClip(clip);

        // Create skeleton root entity with AnimationPlayer
        var skeletonRoot = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        // Create bone entity for "bone2" (not in clip)
        var originalPos = new Vector3(5f, 5f, 5f);
        var boneEntity = world.Spawn()
            .With(new Transform3D(originalPos, Quaternion.Identity, Vector3.One))
            .With(BoneReference.Create("bone2", skeletonRoot.Id))
            .Build();

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        ref readonly var boneTransform = ref world.Get<Transform3D>(boneEntity);
        boneTransform.Position.ShouldBe(originalPos); // Unchanged
    }

    [Fact]
    public void SkeletonPoseSystem_AppliesRotation()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        // Create a clip with rotation keys
        var rotationCurve = new QuaternionCurve();
        rotationCurve.AddKeyframe(0f, Quaternion.Identity);
        rotationCurve.AddKeyframe(1f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2));

        var boneTrack = new BoneTrack
        {
            BoneName = "bone1",
            RotationCurve = rotationCurve
        };

        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        clip.AddBoneTrack(boneTrack);
        var clipId = manager.RegisterClip(clip);

        // Create skeleton root entity with AnimationPlayer at midpoint
        var skeletonRoot = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId) with { Time = 0.5f })
            .Build();

        // Create bone entity
        var boneEntity = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("bone1", skeletonRoot.Id))
            .Build();

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        ref readonly var boneTransform = ref world.Get<Transform3D>(boneEntity);
        // Should be roughly 45 degrees (halfway to 90)
        var expectedAngle = MathF.PI / 4f;
        var actualAngle = 2f * MathF.Acos(boneTransform.Rotation.W);
        actualAngle.ShouldBe(expectedAngle, 0.1f);
    }

    [Fact]
    public void SkeletonPoseSystem_AppliesScale()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        // Create a clip with scale keys
        var scaleCurve = new Vector3Curve();
        scaleCurve.AddKeyframe(0f, Vector3.One);
        scaleCurve.AddKeyframe(1f, new Vector3(2f, 2f, 2f));

        var boneTrack = new BoneTrack
        {
            BoneName = "bone1",
            ScaleCurve = scaleCurve
        };

        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        clip.AddBoneTrack(boneTrack);
        var clipId = manager.RegisterClip(clip);

        // Create skeleton root entity with AnimationPlayer at midpoint
        var skeletonRoot = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId) with { Time = 0.5f })
            .Build();

        // Create bone entity
        var boneEntity = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("bone1", skeletonRoot.Id))
            .Build();

        var system = new SkeletonPoseSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        ref readonly var boneTransform = ref world.Get<Transform3D>(boneEntity);
        boneTransform.Scale.X.ShouldBe(1.5f, 0.1f);
        boneTransform.Scale.Y.ShouldBe(1.5f, 0.1f);
        boneTransform.Scale.Z.ShouldBe(1.5f, 0.1f);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AnimationSystems_WorkTogether()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();

        // Create clip with events and bone animation
        var positionCurve = new Vector3Curve();
        positionCurve.AddKeyframe(0f, Vector3.Zero);
        positionCurve.AddKeyframe(0.5f, new Vector3(0f, 0.2f, 0f));
        positionCurve.AddKeyframe(1f, Vector3.Zero);

        var boneTrack = new BoneTrack
        {
            BoneName = "leg",
            PositionCurve = positionCurve
        };

        var clip = new AnimationClip { Name = "WalkClip", Duration = 1f };
        clip.Events.AddEvent(0.25f, "footstep_left");
        clip.Events.AddEvent(0.75f, "footstep_right");
        clip.AddBoneTrack(boneTrack);
        var clipId = manager.RegisterClip(clip);

        // Create skeleton root
        var skeletonRoot = world.Spawn()
            .With(Transform3D.Identity)
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        // Create bone
        var legBone = world.Spawn()
            .With(Transform3D.Identity)
            .With(BoneReference.Create("leg", skeletonRoot.Id))
            .Build();

        // Create all systems
        var playerSystem = new AnimationPlayerSystem();
        var eventSystem = new AnimationEventSystem();
        var poseSystem = new SkeletonPoseSystem();
        world.AddSystem(playerSystem);
        world.AddSystem(eventSystem);
        world.AddSystem(poseSystem);

        var receivedEvents = new List<AnimationEventTriggeredEvent>();
        using var subscription = world.Subscribe<AnimationEventTriggeredEvent>(e => receivedEvents.Add(e));

        // Simulate several frames
        for (int i = 0; i < 20; i++)
        {
            playerSystem.Update(0.05f);
            eventSystem.Update(0.05f);
            poseSystem.Update(0.05f);
        }

        // Should have received both footstep events
        receivedEvents.Count.ShouldBe(2);
        receivedEvents[0].EventName.ShouldBe("footstep_left");
        receivedEvents[1].EventName.ShouldBe("footstep_right");

        // Bone should have returned to original position (time wrapped around)
        ref readonly var legTransform = ref world.Get<Transform3D>(legBone);
        legTransform.Position.Y.ShouldBeInRange(-0.1f, 0.3f); // Some tolerance for timing
    }

    [Fact]
    public void AnimationSystems_HandleEntityLifecycle()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 1f };
        var clipId = manager.RegisterClip(clip);

        var playerSystem = new AnimationPlayerSystem();
        world.AddSystem(playerSystem);

        // Create entity
        var entity = world.Spawn()
            .With(AnimationPlayer.ForClip(clipId))
            .Build();

        playerSystem.Update(1f / 60f);

        // Despawn entity
        world.Despawn(entity);

        // Should handle gracefully
        playerSystem.Update(1f / 60f);

        // No assertions needed - just verify no crash
    }

    [Fact]
    public void AnimationSystems_HandleMultipleEntities()
    {
        world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var manager = world.GetExtension<AnimationManager>();
        var clip = new AnimationClip { Name = "TestClip", Duration = 2f };
        var clipId = manager.RegisterClip(clip);

        // Create multiple entities
        for (int i = 0; i < 10; i++)
        {
            world.Spawn()
                .With(AnimationPlayer.ForClip(clipId) with { Speed = 1f + i * 0.1f })
                .Build();
        }

        var system = new AnimationPlayerSystem();
        world.AddSystem(system);

        // Should handle all entities
        system.Update(0.5f);

        // Verify all entities were updated with their respective speeds
        int entityIndex = 0;
        foreach (var entity in world.Query<AnimationPlayer>())
        {
            ref readonly var player = ref world.Get<AnimationPlayer>(entity);
            var expectedTime = 0.5f * (1f + entityIndex * 0.1f);
            player.Time.ShouldBe(expectedTime, 0.01f);
            entityIndex++;
        }
    }

    #endregion
}
