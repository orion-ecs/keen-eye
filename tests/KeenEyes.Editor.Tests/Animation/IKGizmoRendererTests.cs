// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;

using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;
using KeenEyes.Common;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Animation;

namespace KeenEyes.Editor.Tests.Animation;

public class IKGizmoRendererTests
{
    #region Property Tests

    [Fact]
    public void Id_ReturnsExpectedValue()
    {
        var renderer = new IKGizmoRenderer();

        Assert.Equal("ik-gizmo", renderer.Id);
    }

    [Fact]
    public void DisplayName_ReturnsIKChains()
    {
        var renderer = new IKGizmoRenderer();

        Assert.Equal("IK Chains", renderer.DisplayName);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        var renderer = new IKGizmoRenderer();

        Assert.True(renderer.IsEnabled);
    }

    [Fact]
    public void IsEnabled_CanBeSet()
    {
        var renderer = new IKGizmoRenderer { IsEnabled = false };

        Assert.False(renderer.IsEnabled);
    }

    [Fact]
    public void Order_RendersAfterSkeletonGizmo()
    {
        var renderer = new IKGizmoRenderer();

        Assert.Equal(101, renderer.Order);
    }

    #endregion

    #region ShouldRender Tests

    [Fact]
    public void ShouldRender_WithChainReference_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().With(IKChainReference.Default).Build();
        var renderer = new IKGizmoRenderer();

        Assert.True(renderer.ShouldRender(entity, world));
    }

    [Fact]
    public void ShouldRender_WithRig_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().With(IKRig.Default).Build();
        var renderer = new IKGizmoRenderer();

        Assert.True(renderer.ShouldRender(entity, world));
    }

    [Fact]
    public void ShouldRender_WithTarget_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn().With(IKTarget.Default).Build();
        var renderer = new IKGizmoRenderer();

        Assert.True(renderer.ShouldRender(entity, world));
    }

    [Fact]
    public void ShouldRender_WithUnrelatedEntity_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().With(Transform3D.Identity).Build();
        var renderer = new IKGizmoRenderer();

        Assert.False(renderer.ShouldRender(entity, world));
    }

    #endregion

    #region ComputeEffectiveWeight Tests

    [Fact]
    public void ComputeEffectiveWeight_MultipliesWeights()
    {
        var weight = IKGizmoRenderer.ComputeEffectiveWeight(0.5f, 0.5f, 1f);

        Assert.Equal(0.25f, weight, 5);
    }

    [Fact]
    public void ComputeEffectiveWeight_ClampsAboveOne()
    {
        var weight = IKGizmoRenderer.ComputeEffectiveWeight(2f, 1f, 1f);

        Assert.Equal(1f, weight, 5);
    }

    [Fact]
    public void ComputeEffectiveWeight_ClampsBelowZero()
    {
        var weight = IKGizmoRenderer.ComputeEffectiveWeight(-1f, 1f, 1f);

        Assert.Equal(0f, weight, 5);
    }

    #endregion

    #region ComputeChainLength Tests

    [Fact]
    public void ComputeChainLength_SumsSegmentLengths()
    {
        var joints = new[]
        {
            Vector3.Zero,
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 2f, 0f),
        };

        var length = IKGizmoRenderer.ComputeChainLength(joints);

        Assert.Equal(3f, length, 5);
    }

    [Fact]
    public void ComputeChainLength_WithSingleJoint_ReturnsZero()
    {
        var joints = new[] { Vector3.Zero };

        var length = IKGizmoRenderer.ComputeChainLength(joints);

        Assert.Equal(0f, length, 5);
    }

    #endregion

    #region IsUnreachable Tests

    [Fact]
    public void IsUnreachable_WhenTargetBeyondReach_ReturnsTrue()
    {
        Assert.True(IKGizmoRenderer.IsUnreachable(chainLength: 2f, targetDistance: 5f));
    }

    [Fact]
    public void IsUnreachable_WhenTargetWithinReach_ReturnsFalse()
    {
        Assert.False(IKGizmoRenderer.IsUnreachable(chainLength: 2f, targetDistance: 1.5f));
    }

    #endregion

    #region GetWorldPosition Tests

    [Fact]
    public void GetWorldPosition_ComposesHierarchy()
    {
        using var world = new World();
        var root = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .Build();
        var child = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .Build();
        world.SetParent(child, root);

        var position = IKGizmoRenderer.GetWorldPosition(world, child);

        Assert.Equal(2f, position.X, 5);
        Assert.Equal(0f, position.Y, 5);
    }

    [Fact]
    public void GetWorldPosition_WithDeadEntity_ReturnsZero()
    {
        using var world = new World();
        var entity = world.Spawn().With(Transform3D.Identity).Build();
        world.Despawn(entity);

        var position = IKGizmoRenderer.GetWorldPosition(world, entity);

        Assert.Equal(Vector3.Zero, position);
    }

    #endregion

    #region TryCollectChainBones Tests

    [Fact]
    public void TryCollectChainBones_WithMatchingHierarchy_CollectsRootToTip()
    {
        using var world = new World();
        var (_, upper, lower, hand) = CreateArmSkeleton(world);
        var chain = IKChainDefinition.TwoBone("Arm", "Upper", "Lower", "Hand", Vector3.UnitZ);

        var collected = IKGizmoRenderer.TryCollectChainBones(world, hand, chain, out var bones);

        Assert.True(collected);
        Assert.Equal(3, bones.Length);
        Assert.Equal(upper.Id, bones[0].Id);
        Assert.Equal(lower.Id, bones[1].Id);
        Assert.Equal(hand.Id, bones[2].Id);
    }

    [Fact]
    public void TryCollectChainBones_WithBoneNameMismatch_ReturnsFalse()
    {
        using var world = new World();
        var (_, _, _, hand) = CreateArmSkeleton(world);
        var chain = IKChainDefinition.TwoBone("Arm", "Upper", "Forearm", "Hand", Vector3.UnitZ);

        var collected = IKGizmoRenderer.TryCollectChainBones(world, hand, chain, out var bones);

        Assert.False(collected);
        Assert.Empty(bones);
    }

    [Fact]
    public void TryCollectChainBones_WithSingleBoneChain_ReturnsFalse()
    {
        using var world = new World();
        var (_, _, _, hand) = CreateArmSkeleton(world);
        var chain = new IKChainDefinition { Name = "Tip", BoneNames = ["Hand"] };

        var collected = IKGizmoRenderer.TryCollectChainBones(world, hand, chain, out var bones);

        Assert.False(collected);
        Assert.Empty(bones);
    }

    #endregion

    #region Render Tests

    [Fact]
    public void Render_WithNoIKManager_DrawsNothing()
    {
        using var world = new World();
        world.Spawn().With(IKChainReference.Default).With(Transform3D.Identity).Build();
        var drawer = new RecordingGizmoDrawer();
        var renderer = new IKGizmoRenderer();

        renderer.Render(CreateContext(world, drawer));

        Assert.Empty(drawer.Spheres);
        Assert.Empty(drawer.Lines);
    }

    [Fact]
    public void Render_WhenDisabled_DrawsNothing()
    {
        using var world = CreateIKWorld(out var hand, out _, new Vector3(1.5f, 0f, 0f));
        var drawer = new RecordingGizmoDrawer();
        var renderer = new IKGizmoRenderer { IsEnabled = false };

        renderer.Render(CreateContext(world, drawer));

        Assert.Empty(drawer.Spheres);
        Assert.Empty(drawer.Lines);
        Assert.NotEqual(0, hand.Id); // guard: hand was created
    }

    [Fact]
    public void Render_WithReachableChain_DrawsChainLinesAndGreenTarget()
    {
        using var world = CreateIKWorld(out _, out var target, new Vector3(1.5f, 0f, 0f));
        var drawer = new RecordingGizmoDrawer();
        var renderer = new IKGizmoRenderer();

        renderer.Render(CreateContext(world, drawer));

        // Two bone segments across three joints produce two chain lines at minimum.
        Assert.True(drawer.Lines.Count >= 2);

        // The target sphere is drawn green (dominant green channel) at the target position.
        Assert.Contains(drawer.Spheres, s =>
            Vector3.Distance(s.Center, target) < 1e-4f &&
            s.Color.Y > 0.5f &&
            s.Color.X < 0.5f);
    }

    [Fact]
    public void Render_WithUnreachableTarget_DrawsRedTarget()
    {
        using var world = CreateIKWorld(out _, out var target, new Vector3(10f, 0f, 0f));
        var drawer = new RecordingGizmoDrawer();
        var renderer = new IKGizmoRenderer();

        renderer.Render(CreateContext(world, drawer));

        // The unreachable target sphere is drawn red (dominant red channel).
        Assert.Contains(drawer.Spheres, s =>
            Vector3.Distance(s.Center, target) < 1e-4f &&
            s.Color.X > 0.5f &&
            s.Color.Y < 0.5f);
    }

    #endregion

    #region Test Helpers

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

    /// <summary>
    /// Builds a world with the AnimationPlugin (IK enabled), a three-bone arm chain whose end
    /// effector references a target placed at <paramref name="targetPosition"/>.
    /// </summary>
    private static World CreateIKWorld(out Entity hand, out Vector3 targetPosition, Vector3 target)
    {
        var world = new World();
        world.InstallPlugin(new AnimationPlugin(new AnimationConfig { EnableIK = true }));

        var (_, _, _, effector) = CreateArmSkeleton(world);

        var targetEntity = world.Spawn()
            .With(IKTarget.AtPosition(target))
            .With(new Transform3D(target, Quaternion.Identity, Vector3.One))
            .Build();

        var manager = world.GetExtension<IKManager>();
        var chainId = manager.RegisterChain(
            IKChainDefinition.TwoBone("Arm", "Upper", "Lower", "Hand", Vector3.UnitZ));

        world.Add(effector, IKChainReference.ForChain(chainId, targetEntity.Id));

        hand = effector;
        targetPosition = target;
        return world;
    }

    private static GizmoRenderContext CreateContext(World world, IGizmoDrawer drawer) => new()
    {
        SceneWorld = world,
        SelectedEntities = [],
        ViewMatrix = Matrix4x4.Identity,
        ProjectionMatrix = Matrix4x4.Identity,
        CameraPosition = Vector3.Zero,
        Bounds = new ViewportBounds(0f, 0f, 800f, 600f),
        Drawer = drawer,
    };

    private sealed class RecordingGizmoDrawer : IGizmoDrawer
    {
        public List<(Vector3 Center, float Radius, Vector4 Color)> Spheres { get; } = [];

        public List<(Vector3 Start, Vector3 End, Vector4 Color)> Lines { get; } = [];

        public List<(Vector3 Position, Vector4 Color)> Points { get; } = [];

        public void DrawTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector4 color)
        {
        }

        public void DrawLine(Vector3 start, Vector3 end, Vector4 color, float width = 1f)
            => Lines.Add((start, end, color));

        public void DrawPoint(Vector3 position, Vector4 color, float size = 4f)
            => Points.Add((position, color));

        public void DrawWireBox(Vector3 min, Vector3 max, Vector4 color, float lineWidth = 1f)
        {
        }

        public void DrawWireSphere(Vector3 center, float radius, Vector4 color, int segments = 16)
            => Spheres.Add((center, radius, color));

        public void DrawText(Vector3 position, string text, Vector4 color)
        {
        }
    }

    #endregion
}
