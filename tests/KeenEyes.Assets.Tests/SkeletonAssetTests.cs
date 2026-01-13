using System.Numerics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for SkeletonAsset bone hierarchy and inverse bind matrices.
/// </summary>
public class SkeletonAssetTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidData_CreatesSkeleton()
    {
        var bones = CreateSimpleSpineSkeleton();
        var matrices = CreateIdentityMatrices(bones.Length);

        using var skeleton = new SkeletonAsset("TestSkeleton", bones, matrices, 0);

        Assert.Equal("TestSkeleton", skeleton.Name);
        Assert.Equal(3, skeleton.BoneCount);
        Assert.Equal(0, skeleton.RootBoneIndex);
    }

    [Fact]
    public void Constructor_WithNullBones_ThrowsArgumentNullException()
    {
        var matrices = CreateIdentityMatrices(3);

        Assert.Throws<ArgumentNullException>(() =>
            new SkeletonAsset("Test", null!, matrices));
    }

    [Fact]
    public void Constructor_WithNullMatrices_ThrowsArgumentNullException()
    {
        var bones = CreateSimpleSpineSkeleton();

        Assert.Throws<ArgumentNullException>(() =>
            new SkeletonAsset("Test", bones, null!));
    }

    [Fact]
    public void Constructor_WithMismatchedArrayLengths_ThrowsArgumentException()
    {
        var bones = CreateSimpleSpineSkeleton(); // 3 bones
        var matrices = CreateIdentityMatrices(5); // 5 matrices

        Assert.Throws<ArgumentException>(() =>
            new SkeletonAsset("Test", bones, matrices));
    }

    [Fact]
    public void Constructor_WithNullName_UsesDefaultName()
    {
        var bones = CreateSimpleSpineSkeleton();
        var matrices = CreateIdentityMatrices(bones.Length);

        using var skeleton = new SkeletonAsset(null!, bones, matrices);

        Assert.Equal("Skeleton", skeleton.Name);
    }

    #endregion

    #region FindBone Tests

    [Fact]
    public void FindBone_WithExistingBone_ReturnsIndex()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Equal(0, skeleton.FindBone("Root"));
        Assert.Equal(1, skeleton.FindBone("Spine"));
        Assert.Equal(2, skeleton.FindBone("Head"));
    }

    [Fact]
    public void FindBone_WithNonExistentBone_ReturnsNegativeOne()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Equal(-1, skeleton.FindBone("LeftArm"));
        Assert.Equal(-1, skeleton.FindBone("NonExistent"));
    }

    [Fact]
    public void FindBone_IsCaseSensitive()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Equal(0, skeleton.FindBone("Root"));
        Assert.Equal(-1, skeleton.FindBone("root"));
        Assert.Equal(-1, skeleton.FindBone("ROOT"));
    }

    #endregion

    #region GetBone Tests

    [Fact]
    public void GetBone_WithValidIndex_ReturnsBoneData()
    {
        using var skeleton = CreateTestSkeleton();

        var root = skeleton.GetBone(0);
        var spine = skeleton.GetBone(1);
        var head = skeleton.GetBone(2);

        Assert.Equal("Root", root.Name);
        Assert.Equal(-1, root.ParentIndex);

        Assert.Equal("Spine", spine.Name);
        Assert.Equal(0, spine.ParentIndex);

        Assert.Equal("Head", head.Name);
        Assert.Equal(1, head.ParentIndex);
    }

    [Fact]
    public void GetBone_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Throws<ArgumentOutOfRangeException>(() => skeleton.GetBone(-1));
    }

    [Fact]
    public void GetBone_WithIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Throws<ArgumentOutOfRangeException>(() => skeleton.GetBone(100));
    }

    #endregion

    #region GetInverseBindMatrix Tests

    [Fact]
    public void GetInverseBindMatrix_WithValidIndex_ReturnsMatrix()
    {
        using var skeleton = CreateTestSkeleton();

        var matrix = skeleton.GetInverseBindMatrix(0);

        Assert.Equal(Matrix4x4.Identity, matrix);
    }

    [Fact]
    public void GetInverseBindMatrix_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Throws<ArgumentOutOfRangeException>(() => skeleton.GetInverseBindMatrix(-1));
    }

    [Fact]
    public void GetInverseBindMatrix_WithIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var skeleton = CreateTestSkeleton();

        Assert.Throws<ArgumentOutOfRangeException>(() => skeleton.GetInverseBindMatrix(100));
    }

    #endregion

    #region GetChildBones Tests

    [Fact]
    public void GetChildBones_ForRootBone_ReturnsDirectChildren()
    {
        using var skeleton = CreateBranchingSkeleton();

        var children = skeleton.GetChildBones(0); // Root's children

        Assert.Equal(3, children.Length); // Spine, LeftArm, and RightArm
        Assert.Contains(1, children); // Spine
        Assert.Contains(2, children); // LeftArm
        Assert.Contains(3, children); // RightArm
    }

    [Fact]
    public void GetChildBones_ForLeafBone_ReturnsEmptyArray()
    {
        using var skeleton = CreateTestSkeleton();

        var children = skeleton.GetChildBones(2); // Head has no children

        Assert.Empty(children);
    }

    [Fact]
    public void GetChildBones_ForMidBone_ReturnsDirectChildren()
    {
        using var skeleton = CreateTestSkeleton();

        var children = skeleton.GetChildBones(1); // Spine's children

        Assert.Single(children);
        Assert.Equal(2, children[0]); // Head
    }

    #endregion

    #region IsCompatibleWith Tests

    [Fact]
    public void IsCompatibleWith_AllBonesPresent_ReturnsTrue()
    {
        using var skeleton = CreateTestSkeleton();

        var result = skeleton.IsCompatibleWith(["Root", "Spine", "Head"]);

        Assert.True(result);
    }

    [Fact]
    public void IsCompatibleWith_SubsetOfBones_ReturnsTrue()
    {
        using var skeleton = CreateTestSkeleton();

        var result = skeleton.IsCompatibleWith(["Root", "Spine"]);

        Assert.True(result);
    }

    [Fact]
    public void IsCompatibleWith_MissingBone_ReturnsFalse()
    {
        using var skeleton = CreateTestSkeleton();

        var result = skeleton.IsCompatibleWith(["Root", "Spine", "LeftArm"]);

        Assert.False(result);
    }

    [Fact]
    public void IsCompatibleWith_EmptyList_ReturnsTrue()
    {
        using var skeleton = CreateTestSkeleton();

        var result = skeleton.IsCompatibleWith([]);

        Assert.True(result);
    }

    #endregion

    #region SizeBytes Tests

    [Fact]
    public void SizeBytes_ReturnsEstimatedSize()
    {
        using var skeleton = CreateTestSkeleton();

        // 3 bones * 100 bytes + 3 matrices * 64 bytes = 492 bytes
        var expectedMinSize = (3 * 100) + (3 * 64);

        Assert.Equal(expectedMinSize, skeleton.SizeBytes);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var skeleton = CreateTestSkeleton();
        skeleton.Dispose();
        skeleton.Dispose(); // Should not throw
    }

    #endregion

    #region Helper Methods

    private static BoneData[] CreateSimpleSpineSkeleton()
    {
        return
        [
            new BoneData("Root", -1, Matrix4x4.Identity),
            new BoneData("Spine", 0, Matrix4x4.CreateTranslation(0, 1, 0)),
            new BoneData("Head", 1, Matrix4x4.CreateTranslation(0, 0.5f, 0))
        ];
    }

    private static BoneData[] CreateBranchingSkeletonBones()
    {
        return
        [
            new BoneData("Root", -1, Matrix4x4.Identity),
            new BoneData("Spine", 0, Matrix4x4.CreateTranslation(0, 1, 0)),
            new BoneData("LeftArm", 0, Matrix4x4.CreateTranslation(-1, 0.8f, 0)),
            new BoneData("RightArm", 0, Matrix4x4.CreateTranslation(1, 0.8f, 0)),
            new BoneData("Head", 1, Matrix4x4.CreateTranslation(0, 0.5f, 0))
        ];
    }

    private static Matrix4x4[] CreateIdentityMatrices(int count)
    {
        var matrices = new Matrix4x4[count];
        for (var i = 0; i < count; i++)
        {
            matrices[i] = Matrix4x4.Identity;
        }

        return matrices;
    }

    private static SkeletonAsset CreateTestSkeleton()
    {
        var bones = CreateSimpleSpineSkeleton();
        var matrices = CreateIdentityMatrices(bones.Length);
        return new SkeletonAsset("TestSkeleton", bones, matrices, 0);
    }

    private static SkeletonAsset CreateBranchingSkeleton()
    {
        var bones = CreateBranchingSkeletonBones();
        var matrices = CreateIdentityMatrices(bones.Length);
        return new SkeletonAsset("BranchingSkeleton", bones, matrices, 0);
    }

    #endregion
}
