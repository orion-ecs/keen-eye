using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.Data;
using KeenEyes.Animation.IK;
using KeenEyes.Animation.Systems;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Full-pipeline integration tests for <see cref="IKSolverSystem"/> running inside
/// a world with <see cref="AnimationPlugin"/> installed with IK enabled.
/// </summary>
public class IKSolverSystemTests
{
    private const float DeltaTime = 1f / 60f;
    private const float ReachTolerance = 0.01f;

    private static AnimationConfig IKEnabledConfig => new() { EnableIK = true };

    /// <summary>
    /// Creates a three-bone arm skeleton (Upper at origin, Lower at +1X, Hand at +2X world)
    /// parented under a skeleton root that carries an <see cref="IKRig"/>.
    /// </summary>
    private static (Entity Root, Entity Upper, Entity Lower, Entity Hand) CreateArmSkeleton(World world)
    {
        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(IKRig.Default)
            .Build();

        var upper = CreateBone(world, "Upper", root, Vector3.Zero, root.Id);
        var lower = CreateBone(world, "Lower", upper, Vector3.UnitX, root.Id);
        var hand = CreateBone(world, "Hand", lower, Vector3.UnitX, root.Id);

        return (root, upper, lower, hand);
    }

    private static Entity CreateBone(World world, string name, Entity parent, Vector3 localPosition, int skeletonRootId)
    {
        var bone = world.Spawn()
            .With(new Transform3D(localPosition, Quaternion.Identity, Vector3.One))
            .With(BoneReference.Create(name, skeletonRootId))
            .Build();

        world.SetParent(bone, parent);
        return bone;
    }

    private static int RegisterArmChain(World world)
    {
        var ikManager = world.GetExtension<IKManager>();
        return ikManager.RegisterChain(
            IKChainDefinition.TwoBone("Arm", "Upper", "Lower", "Hand", Vector3.UnitZ));
    }

    private static Transform3D[] SnapshotLocalTransforms(World world, params Entity[] entities)
    {
        var snapshot = new Transform3D[entities.Length];
        for (var i = 0; i < entities.Length; i++)
        {
            snapshot[i] = world.Get<Transform3D>(entities[i]);
        }

        return snapshot;
    }

    private static void AssertLocalTransformsUnchanged(World world, Transform3D[] snapshot, params Entity[] entities)
    {
        for (var i = 0; i < entities.Length; i++)
        {
            ref readonly var current = ref world.Get<Transform3D>(entities[i]);

            Vector3.Distance(current.Position, snapshot[i].Position).ShouldBeLessThan(1e-6f);
            Vector3.Distance(current.Scale, snapshot[i].Scale).ShouldBeLessThan(1e-6f);
            MathF.Abs(Quaternion.Dot(current.Rotation, snapshot[i].Rotation)).ShouldBeGreaterThan(1f - 1e-6f);
        }
    }

    #region Plugin Wiring Tests

    [Fact]
    public void Install_WithDefaultConfig_DoesNotRegisterIKSolverSystem()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin());

        world.GetSystem<IKSolverSystem>().ShouldBeNull();
        world.HasExtension<IKManager>().ShouldBeFalse();
    }

    [Fact]
    public void Install_WithEnableIK_RegistersIKSolverSystemAndManager()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        world.GetSystem<IKSolverSystem>().ShouldNotBeNull();
        world.HasExtension<IKManager>().ShouldBeTrue();

        var ikManager = world.GetExtension<IKManager>();
        ikManager.HasSolver(IKSolverType.TwoBone).ShouldBeTrue();
        ikManager.HasSolver(IKSolverType.FABRIK).ShouldBeTrue();
    }

    [Fact]
    public void Install_WithDefaultConfig_DoesNotRegisterSkinnedMeshBoneSystem()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin());

        world.GetSystem<SkinnedMeshBoneSystem>().ShouldBeNull();
    }

    [Fact]
    public void Install_WithEnableGpuSkinning_RegistersSkinnedMeshBoneSystem()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableGpuSkinning = true }));

        world.GetSystem<SkinnedMeshBoneSystem>().ShouldNotBeNull();
    }

    [Fact]
    public void Uninstall_WithEnableIK_RemovesIKManagerExtension()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        world.UninstallPlugin<AnimationPlugin>().ShouldBeTrue();

        world.HasExtension<IKManager>().ShouldBeFalse();
    }

    #endregion

    #region Solving Tests

    [Fact]
    public void Update_WithFullWeight_ChainTipReachesTarget()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, _, _, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);
        var targetPosition = new Vector3(1f, 1f, 0f);

        var target = world.Spawn()
            .With(IKTarget.AtPosition(targetPosition))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        world.Update(DeltaTime);

        var tip = IKSolverTestHelpers.GetWorldPosition(world, hand);
        Vector3.Distance(tip, targetPosition).ShouldBeLessThan(ReachTolerance);
    }

    [Fact]
    public void Update_WithFabrikChain_ChainTipReachesTarget()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var root = world.Spawn()
            .With(Transform3D.Identity)
            .With(IKRig.Default)
            .Build();

        var boneA = CreateBone(world, "A", root, Vector3.Zero, root.Id);
        var boneB = CreateBone(world, "B", boneA, Vector3.UnitX, root.Id);
        var boneC = CreateBone(world, "C", boneB, Vector3.UnitX, root.Id);
        var boneD = CreateBone(world, "D", boneC, Vector3.UnitX, root.Id);

        var ikManager = world.GetExtension<IKManager>();
        var chainId = ikManager.RegisterChain(
            IKChainDefinition.MultiBone("Tail", ["A", "B", "C", "D"]));

        var targetPosition = new Vector3(1.5f, 1.5f, 0f);
        var target = world.Spawn()
            .With(IKTarget.AtPosition(targetPosition))
            .Build();
        world.Add(boneD, IKChainReference.ForChain(chainId, target.Id));

        world.Update(DeltaTime);

        var tip = IKSolverTestHelpers.GetWorldPosition(world, boneD);
        Vector3.Distance(tip, targetPosition).ShouldBeLessThan(0.02f);
    }

    [Fact]
    public void Update_WithPoleTargetEntity_BendsMidJointTowardPole()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, _, lower, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);

        // Pole entity placed on the -Z side; the default chain pole vector is +Z.
        var pole = world.Spawn()
            .With(new Transform3D(new Vector3(0.5f, 0f, -5f), Quaternion.Identity, Vector3.One))
            .Build();

        var target = world.Spawn()
            .With(IKTarget.WithPole(new Vector3(1f, 1f, 0f), pole.Id))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        world.Update(DeltaTime);

        var midJoint = IKSolverTestHelpers.GetWorldPosition(world, lower);
        midJoint.Z.ShouldBeLessThan(0f);
    }

    #endregion

    #region Blending Tests

    [Fact]
    public void Update_WithZeroGlobalWeight_LeavesFkPoseUntouched()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (root, upper, lower, hand) = CreateArmSkeleton(world);
        world.Get<IKRig>(root).GlobalWeight = 0f;

        var chainId = RegisterArmChain(world);
        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithZeroTargetWeight_LeavesFkPoseUntouched()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, upper, lower, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);

        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)) with { Weight = 0f })
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithHalfWeight_BlendsBetweenFkAndIkPoses()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (root, _, _, hand) = CreateArmSkeleton(world);
        world.Get<IKRig>(root).GlobalWeight = 0.5f;

        var chainId = RegisterArmChain(world);
        var targetPosition = new Vector3(1f, 1f, 0f);
        var target = world.Spawn()
            .With(IKTarget.AtPosition(targetPosition))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        var fkTip = IKSolverTestHelpers.GetWorldPosition(world, hand);

        world.Update(DeltaTime);

        var blendedTip = IKSolverTestHelpers.GetWorldPosition(world, hand);

        // The blended pose must have moved away from FK but not fully reached the target.
        Vector3.Distance(blendedTip, fkTip).ShouldBeGreaterThan(0.05f);
        Vector3.Distance(blendedTip, targetPosition).ShouldBeGreaterThan(ReachTolerance);
        Vector3.Distance(blendedTip, targetPosition).ShouldBeLessThan(Vector3.Distance(fkTip, targetPosition));
    }

    #endregion

    #region Graceful Degradation Tests

    [Fact]
    public void Update_WithMissingTargetEntity_PreservesFkPoseWithoutThrowing()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, upper, lower, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);

        // Reference an entity ID that does not exist.
        world.Add(hand, IKChainReference.ForChain(chainId, 99999));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithDespawnedTargetEntity_PreservesFkPoseWithoutThrowing()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, upper, lower, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);

        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));
        world.Despawn(target);

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithUnregisteredChainId_PreservesFkPose()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, upper, lower, hand) = CreateArmSkeleton(world);

        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(9999, target.Id));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithDisabledRig_PreservesFkPose()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (root, upper, lower, hand) = CreateArmSkeleton(world);
        world.Get<IKRig>(root).Enabled = false;

        var chainId = RegisterArmChain(world);
        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithDisabledChainReference_PreservesFkPose()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, upper, lower, hand) = CreateArmSkeleton(world);
        var chainId = RegisterArmChain(world);

        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id) with { Enabled = false });

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    [Fact]
    public void Update_WithEnableIKFalse_LeavesPoseUntouched()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin());

        var (_, upper, lower, hand) = CreateArmSkeleton(world);

        // Without EnableIK there is no IKManager to register chains with; the IK
        // components can still be attached but must have no effect.
        var target = world.Spawn()
            .With(IKTarget.AtPosition(new Vector3(1f, 1f, 0f)))
            .Build();
        world.Add(hand, IKChainReference.ForChain(1, target.Id));

        var snapshot = SnapshotLocalTransforms(world, upper, lower, hand);

        world.Update(DeltaTime);

        world.GetSystem<IKSolverSystem>().ShouldBeNull();
        AssertLocalTransformsUnchanged(world, snapshot, upper, lower, hand);
    }

    #endregion

    #region Pipeline Integration Tests

    [Fact]
    public void Update_WithFkAnimationPlaying_AppliesIkAfterFkPose()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (root, _, _, hand) = CreateArmSkeleton(world);

        // FK clip rotates the Upper bone 90 degrees about Z, which alone would
        // swing the arm so the hand ends up at (0, 2, 0).
        var rotationCurve = new QuaternionCurve();
        rotationCurve.AddKeyframe(0f, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f));

        var clip = new AnimationClip { Name = "Raise", Duration = 1f, WrapMode = WrapMode.Loop };
        clip.AddBoneTrack(new BoneTrack { BoneName = "Upper", RotationCurve = rotationCurve });

        var animations = world.GetExtension<AnimationManager>();
        var clipId = animations.RegisterClip(clip);
        world.Add(root, AnimationPlayer.ForClip(clipId));

        var chainId = RegisterArmChain(world);
        var targetPosition = new Vector3(1f, 1f, 0f);
        var target = world.Spawn()
            .With(IKTarget.AtPosition(targetPosition))
            .Build();
        world.Add(hand, IKChainReference.ForChain(chainId, target.Id));

        world.Update(DeltaTime);

        // If IK ran before (or instead of) the FK pose, the hand would sit at the
        // FK result (0, 2, 0); reaching the target proves IK solved after FK.
        var tip = IKSolverTestHelpers.GetWorldPosition(world, hand);
        Vector3.Distance(tip, targetPosition).ShouldBeLessThan(ReachTolerance);
    }

    [Fact]
    public void Update_WithFkAnimationAndNoChain_AppliesFkPoseOnly()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (root, _, _, hand) = CreateArmSkeleton(world);

        var rotationCurve = new QuaternionCurve();
        rotationCurve.AddKeyframe(0f, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f));

        var clip = new AnimationClip { Name = "Raise", Duration = 1f, WrapMode = WrapMode.Loop };
        clip.AddBoneTrack(new BoneTrack { BoneName = "Upper", RotationCurve = rotationCurve });

        var animations = world.GetExtension<AnimationManager>();
        var clipId = animations.RegisterClip(clip);
        world.Add(root, AnimationPlayer.ForClip(clipId));

        world.Update(DeltaTime);

        // FK alone swings the whole arm up: hand lands at (0, 2, 0).
        var tip = IKSolverTestHelpers.GetWorldPosition(world, hand);
        Vector3.Distance(tip, new Vector3(0f, 2f, 0f)).ShouldBeLessThan(ReachTolerance);
    }

    #endregion
}
