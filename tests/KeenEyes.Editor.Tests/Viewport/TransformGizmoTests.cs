using KeenEyes.Editor.Viewport;

namespace KeenEyes.Editor.Tests.Viewport;

public class TransformGizmoTests
{
    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaultMode()
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoMode.Translate, gizmo.Mode);
    }

    [Fact]
    public void InitialState_HasWorldSpace()
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoSpace.World, gizmo.Space);
    }

    [Fact]
    public void InitialState_IsNotDragging()
    {
        var gizmo = new TransformGizmo();

        Assert.False(gizmo.IsDragging);
    }

    [Fact]
    public void InitialState_HasNoHoveredAxis()
    {
        var gizmo = new TransformGizmo();

        Assert.Equal(GizmoAxis.None, gizmo.HoveredAxis);
    }

    #endregion

    #region Mode Tests

    [Fact]
    public void Mode_CanBeSetToTranslate()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Translate };

        Assert.Equal(GizmoMode.Translate, gizmo.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToRotate()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };

        Assert.Equal(GizmoMode.Rotate, gizmo.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToScale()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };

        Assert.Equal(GizmoMode.Scale, gizmo.Mode);
    }

    #endregion

    #region CycleMode Tests

    [Fact]
    public void CycleMode_FromTranslate_GoesToRotate()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Translate };

        gizmo.CycleMode();

        Assert.Equal(GizmoMode.Rotate, gizmo.Mode);
    }

    [Fact]
    public void CycleMode_FromRotate_GoesToScale()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Rotate };

        gizmo.CycleMode();

        Assert.Equal(GizmoMode.Scale, gizmo.Mode);
    }

    [Fact]
    public void CycleMode_FromScale_GoesToTranslate()
    {
        var gizmo = new TransformGizmo { Mode = GizmoMode.Scale };

        gizmo.CycleMode();

        Assert.Equal(GizmoMode.Translate, gizmo.Mode);
    }

    [Fact]
    public void CycleMode_ThreeTimes_ReturnsToOriginal()
    {
        var gizmo = new TransformGizmo();
        var original = gizmo.Mode;

        gizmo.CycleMode();
        gizmo.CycleMode();
        gizmo.CycleMode();

        Assert.Equal(original, gizmo.Mode);
    }

    #endregion

    #region Space Tests

    [Fact]
    public void Space_CanBeSetToLocal()
    {
        var gizmo = new TransformGizmo { Space = GizmoSpace.Local };

        Assert.Equal(GizmoSpace.Local, gizmo.Space);
    }

    [Fact]
    public void Space_CanBeSetToWorld()
    {
        var gizmo = new TransformGizmo { Space = GizmoSpace.Local };

        gizmo.Space = GizmoSpace.World;

        Assert.Equal(GizmoSpace.World, gizmo.Space);
    }

    #endregion

    #region ToggleSpace Tests

    [Fact]
    public void ToggleSpace_FromWorld_GoesToLocal()
    {
        var gizmo = new TransformGizmo { Space = GizmoSpace.World };

        gizmo.ToggleSpace();

        Assert.Equal(GizmoSpace.Local, gizmo.Space);
    }

    [Fact]
    public void ToggleSpace_FromLocal_GoesToWorld()
    {
        var gizmo = new TransformGizmo { Space = GizmoSpace.Local };

        gizmo.ToggleSpace();

        Assert.Equal(GizmoSpace.World, gizmo.Space);
    }

    [Fact]
    public void ToggleSpace_TwiceReturnsToOriginal()
    {
        var gizmo = new TransformGizmo();
        var original = gizmo.Space;

        gizmo.ToggleSpace();
        gizmo.ToggleSpace();

        Assert.Equal(original, gizmo.Space);
    }

    #endregion

    #region GizmoMode Enum Tests

    [Fact]
    public void GizmoMode_HasThreeValues()
    {
        var values = Enum.GetValues<GizmoMode>();

        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void GizmoMode_ContainsTranslate()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoMode), GizmoMode.Translate));
    }

    [Fact]
    public void GizmoMode_ContainsRotate()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoMode), GizmoMode.Rotate));
    }

    [Fact]
    public void GizmoMode_ContainsScale()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoMode), GizmoMode.Scale));
    }

    #endregion

    #region GizmoSpace Enum Tests

    [Fact]
    public void GizmoSpace_HasTwoValues()
    {
        var values = Enum.GetValues<GizmoSpace>();

        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void GizmoSpace_ContainsWorld()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoSpace), GizmoSpace.World));
    }

    [Fact]
    public void GizmoSpace_ContainsLocal()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoSpace), GizmoSpace.Local));
    }

    #endregion

    #region GizmoAxis Enum Tests

    [Fact]
    public void GizmoAxis_ContainsAllExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.None));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.X));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.Y));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.Z));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.XY));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.XZ));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.YZ));
        Assert.True(Enum.IsDefined(typeof(GizmoAxis), GizmoAxis.All));
    }

    [Fact]
    public void GizmoAxis_HasEightValues()
    {
        var values = Enum.GetValues<GizmoAxis>();

        Assert.Equal(8, values.Length);
    }

    #endregion
}
