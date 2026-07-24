using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Systems;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Regression tests for <see cref="SkinnedMeshBoneSystem"/> covering GPU-skinning bone
/// matrix computation (issue #1115).
/// </summary>
public class SkinnedMeshBoneSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    [Fact]
    public void Update_BoneEntityWithWorldOffset_ResolvesLiveHandleInsteadOfIdentity()
    {
        world = new World();

        // Bone entity translated 5 units along X.
        var bone = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .Build();

        // Skinned mesh referencing that bone with an identity inverse bind matrix.
        var mesh = world.Spawn()
            .With(Transform3D.Identity)
            .With(SkinnedMesh.Create(1, [bone.Id], [Matrix4x4.Identity]))
            .Build();

        var system = new SkinnedMeshBoneSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        var buffer = system.GetBoneMatrixBuffer(mesh.Id);
        buffer.ShouldNotBeNull();

        // Before the fix the bone handle was fabricated with Version 0, IsAlive rejected it,
        // and the matrix stayed identity (translation == 0).
        var boneMatrix = buffer.GetBoneMatrix(0);
        boneMatrix.M41.ApproximatelyEquals(5f).ShouldBeTrue();
    }

    [Fact]
    public void Update_NonCommutingTransforms_UsesRowVectorMultiplicationOrder()
    {
        world = new World();

        // Bone animated to a rotated + translated pose (rotation makes the multiply order matter).
        var bone = world.Spawn()
            .With(new Transform3D(
                new Vector3(0f, 3f, 0f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f),
                Vector3.One))
            .Build();

        // Inverse bind matrix that pulls the bind-pose vertex back to the bone origin.
        var inverseBind = Matrix4x4.CreateTranslation(-2f, 0f, 0f);

        var mesh = world.Spawn()
            .With(Transform3D.Identity)
            .With(SkinnedMesh.Create(1, [bone.Id], [inverseBind]))
            .Build();

        var system = new SkinnedMeshBoneSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        var buffer = system.GetBoneMatrixBuffer(mesh.Id);
        buffer.ShouldNotBeNull();

        // A bind-pose vertex at (2,0,0) should map to the animated bone origin (0,3,0).
        // Correct row-vector order (inverseBind * boneWorld) yields (0,3,0);
        // the reversed order (boneWorld * inverseBind) would yield (-2,5,0).
        var skinned = Vector3.Transform(new Vector3(2f, 0f, 0f), buffer.GetBoneMatrix(0));
        skinned.X.ApproximatelyEquals(0f).ShouldBeTrue();
        skinned.Y.ApproximatelyEquals(3f).ShouldBeTrue();
        skinned.Z.ApproximatelyEquals(0f).ShouldBeTrue();
    }

    [Fact]
    public void Update_ParentedBone_ComposesParentWorldMatrixThroughLiveHandles()
    {
        world = new World();

        // Parent bone translated 10 along X.
        var parentBone = world.Spawn()
            .With(new Transform3D(new Vector3(10f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .Build();

        // Child bone offset 1 along X in local space, parented to the parent bone.
        var childBone = world.Spawn()
            .With(new Transform3D(new Vector3(1f, 0f, 0f), Quaternion.Identity, Vector3.One))
            .Build();

        world.SetParent(childBone, parentBone);

        var mesh = world.Spawn()
            .With(Transform3D.Identity)
            .With(SkinnedMesh.Create(1, [childBone.Id], [Matrix4x4.Identity]))
            .Build();

        var system = new SkinnedMeshBoneSystem();
        world.AddSystem(system);

        system.Update(1f / 60f);

        var buffer = system.GetBoneMatrixBuffer(mesh.Id);
        buffer.ShouldNotBeNull();

        // Child world X = local 1 + parent 10 = 11. The parent recursion must resolve a live
        // handle too; the old code round-tripped through a Version-0 id and lost it.
        var boneMatrix = buffer.GetBoneMatrix(0);
        boneMatrix.M41.ApproximatelyEquals(11f).ShouldBeTrue();
    }
}
