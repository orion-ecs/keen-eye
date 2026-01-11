using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the LodGroup component and related types.
/// </summary>
public class LodGroupTests
{
    #region LodLevel Tests

    [Fact]
    public void LodLevel_Constructor_SetsMeshId()
    {
        var level = new LodLevel(42, 10f);

        Assert.Equal(42, level.MeshId);
    }

    [Fact]
    public void LodLevel_Constructor_SetsThreshold()
    {
        var level = new LodLevel(42, 25.5f);

        Assert.Equal(25.5f, level.Threshold);
    }

    [Fact]
    public void LodLevel_RecordEquality_SameMeshIdAndThreshold_AreEqual()
    {
        var level1 = new LodLevel(42, 10f);
        var level2 = new LodLevel(42, 10f);

        Assert.Equal(level1, level2);
    }

    [Fact]
    public void LodLevel_RecordEquality_DifferentMeshId_AreNotEqual()
    {
        var level1 = new LodLevel(42, 10f);
        var level2 = new LodLevel(43, 10f);

        Assert.NotEqual(level1, level2);
    }

    [Fact]
    public void LodLevel_RecordEquality_DifferentThreshold_AreNotEqual()
    {
        var level1 = new LodLevel(42, 10f);
        var level2 = new LodLevel(42, 20f);

        Assert.NotEqual(level1, level2);
    }

    #endregion

    #region LodSelectionMode Tests

    [Fact]
    public void LodSelectionMode_HasDistanceValue()
    {
        Assert.Equal(0, (int)LodSelectionMode.Distance);
    }

    [Fact]
    public void LodSelectionMode_HasScreenSizeValue()
    {
        Assert.Equal(1, (int)LodSelectionMode.ScreenSize);
    }

    #endregion

    #region LodGroup Factory Method Tests

    [Fact]
    public void Create_WithOneLevel_SetsLevelCount()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Equal(1, lodGroup.LevelCount);
    }

    [Fact]
    public void Create_WithOneLevel_SetsLevel0()
    {
        var level = new LodLevel(42, 0f);
        var lodGroup = LodGroup.Create(level);

        Assert.Equal(level, lodGroup.Level0);
    }

    [Fact]
    public void Create_WithTwoLevels_SetsLevelCount()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f));

        Assert.Equal(2, lodGroup.LevelCount);
    }

    [Fact]
    public void Create_WithTwoLevels_SetsBothLevels()
    {
        var level0 = new LodLevel(1, 0f);
        var level1 = new LodLevel(2, 10f);
        var lodGroup = LodGroup.Create(level0, level1);

        Assert.Equal(level0, lodGroup.Level0);
        Assert.Equal(level1, lodGroup.Level1);
    }

    [Fact]
    public void Create_WithThreeLevels_SetsLevelCount()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            new LodLevel(3, 20f));

        Assert.Equal(3, lodGroup.LevelCount);
    }

    [Fact]
    public void Create_WithThreeLevels_SetsAllLevels()
    {
        var level0 = new LodLevel(1, 0f);
        var level1 = new LodLevel(2, 10f);
        var level2 = new LodLevel(3, 20f);
        var lodGroup = LodGroup.Create(level0, level1, level2);

        Assert.Equal(level0, lodGroup.Level0);
        Assert.Equal(level1, lodGroup.Level1);
        Assert.Equal(level2, lodGroup.Level2);
    }

    [Fact]
    public void Create_WithFourLevels_SetsLevelCount()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            new LodLevel(3, 20f),
            new LodLevel(4, 50f));

        Assert.Equal(4, lodGroup.LevelCount);
    }

    [Fact]
    public void Create_WithFourLevels_SetsAllLevels()
    {
        var level0 = new LodLevel(1, 0f);
        var level1 = new LodLevel(2, 10f);
        var level2 = new LodLevel(3, 20f);
        var level3 = new LodLevel(4, 50f);
        var lodGroup = LodGroup.Create(level0, level1, level2, level3);

        Assert.Equal(level0, lodGroup.Level0);
        Assert.Equal(level1, lodGroup.Level1);
        Assert.Equal(level2, lodGroup.Level2);
        Assert.Equal(level3, lodGroup.Level3);
    }

    [Fact]
    public void Create_DefaultSelectionMode_IsDistance()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Equal(LodSelectionMode.Distance, lodGroup.SelectionMode);
    }

    [Fact]
    public void Create_DefaultBoundingSphereRadius_IsZero()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Equal(0f, lodGroup.BoundingSphereRadius);
    }

    [Fact]
    public void Create_DefaultBias_IsZero()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Equal(0f, lodGroup.Bias);
    }

    [Fact]
    public void Create_DefaultCurrentLevel_IsZero()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Equal(0, lodGroup.CurrentLevel);
    }

    #endregion

    #region GetLevel Tests

    [Fact]
    public void GetLevel_Index0_ReturnsLevel0()
    {
        var level = new LodLevel(42, 0f);
        var lodGroup = LodGroup.Create(level);

        Assert.Equal(level, lodGroup.GetLevel(0));
    }

    [Fact]
    public void GetLevel_Index1_ReturnsLevel1()
    {
        var level0 = new LodLevel(1, 0f);
        var level1 = new LodLevel(42, 10f);
        var lodGroup = LodGroup.Create(level0, level1);

        Assert.Equal(level1, lodGroup.GetLevel(1));
    }

    [Fact]
    public void GetLevel_Index2_ReturnsLevel2()
    {
        var level2 = new LodLevel(42, 20f);
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            level2);

        Assert.Equal(level2, lodGroup.GetLevel(2));
    }

    [Fact]
    public void GetLevel_Index3_ReturnsLevel3()
    {
        var level3 = new LodLevel(42, 50f);
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            new LodLevel(3, 20f),
            level3);

        Assert.Equal(level3, lodGroup.GetLevel(3));
    }

    [Fact]
    public void GetLevel_NegativeIndex_ThrowsArgumentOutOfRange()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Throws<ArgumentOutOfRangeException>(() => lodGroup.GetLevel(-1));
    }

    [Fact]
    public void GetLevel_IndexTooLarge_ThrowsArgumentOutOfRange()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Throws<ArgumentOutOfRangeException>(() => lodGroup.GetLevel(4));
    }

    #endregion

    #region SetLevel Tests

    [Fact]
    public void SetLevel_Index0_UpdatesLevel0()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));
        var newLevel = new LodLevel(99, 5f);

        lodGroup.SetLevel(0, newLevel);

        Assert.Equal(newLevel, lodGroup.Level0);
    }

    [Fact]
    public void SetLevel_Index1_UpdatesLevel1()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f));
        var newLevel = new LodLevel(99, 15f);

        lodGroup.SetLevel(1, newLevel);

        Assert.Equal(newLevel, lodGroup.Level1);
    }

    [Fact]
    public void SetLevel_Index2_UpdatesLevel2()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            new LodLevel(3, 20f));
        var newLevel = new LodLevel(99, 25f);

        lodGroup.SetLevel(2, newLevel);

        Assert.Equal(newLevel, lodGroup.Level2);
    }

    [Fact]
    public void SetLevel_Index3_UpdatesLevel3()
    {
        var lodGroup = LodGroup.Create(
            new LodLevel(1, 0f),
            new LodLevel(2, 10f),
            new LodLevel(3, 20f),
            new LodLevel(4, 50f));
        var newLevel = new LodLevel(99, 75f);

        lodGroup.SetLevel(3, newLevel);

        Assert.Equal(newLevel, lodGroup.Level3);
    }

    [Fact]
    public void SetLevel_NegativeIndex_ThrowsArgumentOutOfRange()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Throws<ArgumentOutOfRangeException>(() => lodGroup.SetLevel(-1, new LodLevel(1, 0f)));
    }

    [Fact]
    public void SetLevel_IndexTooLarge_ThrowsArgumentOutOfRange()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));

        Assert.Throws<ArgumentOutOfRangeException>(() => lodGroup.SetLevel(4, new LodLevel(1, 0f)));
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void LodGroup_IsValueType()
    {
        var lodGroup1 = LodGroup.Create(new LodLevel(1, 0f));
        var lodGroup2 = lodGroup1;

        lodGroup2.Bias = 5f;

        // Changes to lodGroup2 should not affect lodGroup1
        Assert.Equal(0f, lodGroup1.Bias);
        Assert.Equal(5f, lodGroup2.Bias);
    }

    [Fact]
    public void LodGroup_FieldsCanBeModified()
    {
        var lodGroup = LodGroup.Create(new LodLevel(1, 0f));
        lodGroup.SelectionMode = LodSelectionMode.ScreenSize;
        lodGroup.BoundingSphereRadius = 2.5f;
        lodGroup.Bias = 1.5f;
        lodGroup.CurrentLevel = 2;

        Assert.Equal(LodSelectionMode.ScreenSize, lodGroup.SelectionMode);
        Assert.Equal(2.5f, lodGroup.BoundingSphereRadius);
        Assert.Equal(1.5f, lodGroup.Bias);
        Assert.Equal(2, lodGroup.CurrentLevel);
    }

    #endregion
}
