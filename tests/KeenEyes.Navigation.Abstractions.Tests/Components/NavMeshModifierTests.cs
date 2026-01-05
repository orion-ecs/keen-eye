// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Abstractions.Tests.Components;

public class NavMeshModifierTests
{
    #region Create Tests

    [Fact]
    public void Create_ReturnsDefaultModifier()
    {
        var modifier = NavMeshModifier.Create();

        Assert.False(modifier.IgnoreFromBuild);
        Assert.False(modifier.OverrideAreaType);
        Assert.True(modifier.AffectChildren);
        Assert.Equal(-1, modifier.AgentTypeMask);
    }

    [Fact]
    public void Create_DefaultAreaTypeIsWalkable()
    {
        var modifier = NavMeshModifier.Create();

        Assert.Equal(NavAreaType.Walkable, modifier.AreaType);
    }

    #endregion

    #region CreateExclude Tests

    [Fact]
    public void CreateExclude_SetsIgnoreFromBuild()
    {
        var modifier = NavMeshModifier.CreateExclude();

        Assert.True(modifier.IgnoreFromBuild);
    }

    [Fact]
    public void CreateExclude_AffectsChildrenByDefault()
    {
        var modifier = NavMeshModifier.CreateExclude();

        Assert.True(modifier.AffectChildren);
    }

    [Fact]
    public void CreateExclude_WithFalse_DoesNotAffectChildren()
    {
        var modifier = NavMeshModifier.CreateExclude(affectChildren: false);

        Assert.False(modifier.AffectChildren);
    }

    [Fact]
    public void CreateExclude_AllAgentTypesByDefault()
    {
        var modifier = NavMeshModifier.CreateExclude();

        Assert.Equal(-1, modifier.AgentTypeMask);
    }

    #endregion

    #region CreateAreaOverride Tests

    [Fact]
    public void CreateAreaOverride_SetsOverrideAreaType()
    {
        var modifier = NavMeshModifier.CreateAreaOverride(NavAreaType.Water);

        Assert.True(modifier.OverrideAreaType);
        Assert.Equal(NavAreaType.Water, modifier.AreaType);
    }

    [Fact]
    public void CreateAreaOverride_DoesNotIgnoreFromBuild()
    {
        var modifier = NavMeshModifier.CreateAreaOverride(NavAreaType.Road);

        Assert.False(modifier.IgnoreFromBuild);
    }

    [Fact]
    public void CreateAreaOverride_AffectsChildrenByDefault()
    {
        var modifier = NavMeshModifier.CreateAreaOverride(NavAreaType.Grass);

        Assert.True(modifier.AffectChildren);
    }

    [Fact]
    public void CreateAreaOverride_WithFalse_DoesNotAffectChildren()
    {
        var modifier = NavMeshModifier.CreateAreaOverride(NavAreaType.Sand, affectChildren: false);

        Assert.False(modifier.AffectChildren);
    }

    [Fact]
    public void CreateAreaOverride_AllAgentTypesByDefault()
    {
        var modifier = NavMeshModifier.CreateAreaOverride(NavAreaType.Mud);

        Assert.Equal(-1, modifier.AgentTypeMask);
    }

    #endregion

    #region All Area Types Tests

    [Theory]
    [InlineData(NavAreaType.Walkable)]
    [InlineData(NavAreaType.Water)]
    [InlineData(NavAreaType.Road)]
    [InlineData(NavAreaType.Grass)]
    [InlineData(NavAreaType.Door)]
    [InlineData(NavAreaType.Sand)]
    [InlineData(NavAreaType.Mud)]
    [InlineData(NavAreaType.Ice)]
    [InlineData(NavAreaType.Hazard)]
    [InlineData(NavAreaType.Jump)]
    [InlineData(NavAreaType.Climb)]
    [InlineData(NavAreaType.OffMeshLink)]
    [InlineData(NavAreaType.NotWalkable)]
    public void CreateAreaOverride_SupportsAllAreaTypes(NavAreaType areaType)
    {
        var modifier = NavMeshModifier.CreateAreaOverride(areaType);

        Assert.Equal(areaType, modifier.AreaType);
        Assert.True(modifier.OverrideAreaType);
    }

    #endregion
}
