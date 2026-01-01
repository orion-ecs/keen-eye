using System.Numerics;

using KeenEyes.Editor.Viewport;

namespace KeenEyes.Editor.Tests.Viewport;

public class EditorCameraControllerTests
{
    #region Initial State Tests

    [Fact]
    public void InitialState_HasDefaultValues()
    {
        var controller = new EditorCameraController();

        Assert.Equal(Vector3.Zero, controller.Target);
        Assert.Equal(EditorCameraMode.Orbit, controller.Mode);
    }

    [Fact]
    public void Reset_RestoresToDefaults()
    {
        var controller = new EditorCameraController
        {
            Target = new Vector3(10, 20, 30),
            Distance = 50f,
            Yaw = 90f,
            Pitch = 45f,
            Mode = EditorCameraMode.Fly
        };

        controller.Reset();

        Assert.Equal(Vector3.Zero, controller.Target);
        Assert.Equal(0f, controller.Yaw);
        Assert.Equal(-30f, controller.Pitch);
        Assert.Equal(EditorCameraMode.Orbit, controller.Mode);
    }

    #endregion

    #region Distance Tests

    [Fact]
    public void Distance_ClampsToMinimum()
    {
        var controller = new EditorCameraController { Distance = 0.01f };

        Assert.Equal(0.1f, controller.Distance);
    }

    [Fact]
    public void Distance_ClampsToMaximum()
    {
        var controller = new EditorCameraController { Distance = 2000f };

        Assert.Equal(1000f, controller.Distance);
    }

    [Fact]
    public void Distance_AcceptsValidValue()
    {
        var controller = new EditorCameraController { Distance = 50f };

        Assert.Equal(50f, controller.Distance);
    }

    #endregion

    #region Pitch Tests

    [Fact]
    public void Pitch_ClampsToMinimum()
    {
        var controller = new EditorCameraController { Pitch = -100f };

        Assert.Equal(-89f, controller.Pitch);
    }

    [Fact]
    public void Pitch_ClampsToMaximum()
    {
        var controller = new EditorCameraController { Pitch = 100f };

        Assert.Equal(89f, controller.Pitch);
    }

    [Fact]
    public void Pitch_AcceptsValidValue()
    {
        var controller = new EditorCameraController { Pitch = 45f };

        Assert.Equal(45f, controller.Pitch);
    }

    #endregion

    #region Yaw Tests

    [Fact]
    public void Yaw_AcceptsAnyValue()
    {
        var controller = new EditorCameraController { Yaw = 720f };

        Assert.Equal(720f, controller.Yaw);
    }

    #endregion

    #region Position Tests

    [Fact]
    public void Position_CalculatesCorrectlyAtOrigin()
    {
        var controller = new EditorCameraController
        {
            Target = Vector3.Zero,
            Distance = 10f,
            Yaw = 0f,
            Pitch = 0f
        };

        var position = controller.Position;

        // At yaw=0, pitch=0, camera is at (0, 0, distance) from target
        Assert.True(Math.Abs(position.X) < 0.001f);
        Assert.True(Math.Abs(position.Y) < 0.001f);
        Assert.True(Math.Abs(position.Z - 10f) < 0.001f);
    }

    [Fact]
    public void Position_OffsetsByTarget()
    {
        var controller = new EditorCameraController
        {
            Target = new Vector3(5f, 5f, 5f),
            Distance = 10f,
            Yaw = 0f,
            Pitch = 0f
        };

        var position = controller.Position;

        Assert.True(Math.Abs(position.X - 5f) < 0.001f);
        Assert.True(Math.Abs(position.Y - 5f) < 0.001f);
        Assert.True(Math.Abs(position.Z - 15f) < 0.001f);
    }

    [Fact]
    public void Position_RespondsToYaw()
    {
        var controller = new EditorCameraController
        {
            Target = Vector3.Zero,
            Distance = 10f,
            Yaw = 90f, // 90 degrees around Y axis
            Pitch = 0f
        };

        var position = controller.Position;

        // At yaw=90, camera moves along +X axis
        Assert.True(Math.Abs(position.X - 10f) < 0.001f);
        Assert.True(Math.Abs(position.Y) < 0.001f);
        Assert.True(Math.Abs(position.Z) < 0.001f);
    }

    [Fact]
    public void Position_RespondsToPitch()
    {
        var controller = new EditorCameraController
        {
            Target = Vector3.Zero,
            Distance = 10f,
            Yaw = 0f,
            Pitch = 89f // Maximum allowed
        };

        var position = controller.Position;

        // At high pitch, camera is mostly above target
        Assert.True(position.Y > 9f); // Most of distance is in Y
    }

    #endregion

    #region Direction Tests

    [Fact]
    public void Forward_PointsTowardsTarget()
    {
        var controller = new EditorCameraController
        {
            Target = Vector3.Zero,
            Distance = 10f,
            Yaw = 0f,
            Pitch = 0f
        };

        var forward = controller.Forward;
        var expected = Vector3.Normalize(controller.Target - controller.Position);

        Assert.True(Math.Abs(forward.X - expected.X) < 0.001f);
        Assert.True(Math.Abs(forward.Y - expected.Y) < 0.001f);
        Assert.True(Math.Abs(forward.Z - expected.Z) < 0.001f);
    }

    [Fact]
    public void Right_IsPerpendicularToForwardAndUp()
    {
        var controller = new EditorCameraController
        {
            Distance = 10f,
            Yaw = 45f,
            Pitch = -30f
        };

        var forward = controller.Forward;
        var right = controller.Right;

        // Right should be perpendicular to forward
        var dot = Vector3.Dot(forward, right);
        Assert.True(Math.Abs(dot) < 0.001f);
    }

    [Fact]
    public void Up_IsPerpendicularToForwardAndRight()
    {
        var controller = new EditorCameraController
        {
            Distance = 10f,
            Yaw = 45f,
            Pitch = -30f
        };

        var forward = controller.Forward;
        var up = controller.Up;

        // Up should be perpendicular to forward
        var dot = Vector3.Dot(forward, up);
        Assert.True(Math.Abs(dot) < 0.001f);
    }

    #endregion

    #region ViewMatrix Tests

    [Fact]
    public void GetViewMatrix_ReturnsValidMatrix()
    {
        var controller = new EditorCameraController
        {
            Target = new Vector3(0, 0, 0),
            Distance = 10f
        };

        var viewMatrix = controller.GetViewMatrix();

        // View matrix should be invertible
        Assert.True(Matrix4x4.Invert(viewMatrix, out _));
    }

    [Fact]
    public void GetViewMatrix_ChangesWithCameraMovement()
    {
        var controller = new EditorCameraController();
        controller.Reset();
        var matrix1 = controller.GetViewMatrix();

        controller.Yaw = 45f;
        var matrix2 = controller.GetViewMatrix();

        Assert.NotEqual(matrix1, matrix2);
    }

    #endregion

    #region Transform Tests

    [Fact]
    public void GetTransform_ReturnsCorrectPosition()
    {
        var controller = new EditorCameraController
        {
            Target = Vector3.Zero,
            Distance = 10f,
            Yaw = 0f,
            Pitch = 0f
        };

        var transform = controller.GetTransform();

        var expectedPosition = controller.Position;
        Assert.Equal(expectedPosition, transform.Position);
    }

    [Fact]
    public void GetTransform_HasUnitScale()
    {
        var controller = new EditorCameraController();

        var transform = controller.GetTransform();

        Assert.Equal(Vector3.One, transform.Scale);
    }

    #endregion

    #region FocusOn Tests

    [Fact]
    public void FocusOn_UpdatesTarget()
    {
        var controller = new EditorCameraController();
        var focusPoint = new Vector3(10f, 20f, 30f);

        controller.FocusOn(focusPoint);

        Assert.Equal(focusPoint, controller.Target);
    }

    [Fact]
    public void FocusOn_WithDistance_UpdatesDistance()
    {
        var controller = new EditorCameraController();

        controller.FocusOn(Vector3.Zero, 25f);

        Assert.Equal(25f, controller.Distance);
    }

    [Fact]
    public void FocusOn_WithDistance_ClampsDistance()
    {
        var controller = new EditorCameraController();

        controller.FocusOn(Vector3.Zero, 0.001f);

        Assert.Equal(0.1f, controller.Distance); // Minimum distance
    }

    #endregion

    #region Preset View Tests

    [Fact]
    public void SetPresetView_Front_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Front);

        Assert.Equal(0f, controller.Yaw);
        Assert.Equal(0f, controller.Pitch);
    }

    [Fact]
    public void SetPresetView_Back_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Back);

        Assert.Equal(180f, controller.Yaw);
        Assert.Equal(0f, controller.Pitch);
    }

    [Fact]
    public void SetPresetView_Left_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Left);

        Assert.Equal(-90f, controller.Yaw);
        Assert.Equal(0f, controller.Pitch);
    }

    [Fact]
    public void SetPresetView_Right_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Right);

        Assert.Equal(90f, controller.Yaw);
        Assert.Equal(0f, controller.Pitch);
    }

    [Fact]
    public void SetPresetView_Top_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Top);

        Assert.Equal(0f, controller.Yaw);
        Assert.Equal(-89f, controller.Pitch);
    }

    [Fact]
    public void SetPresetView_Bottom_SetsCorrectAngles()
    {
        var controller = new EditorCameraController();

        controller.SetPresetView(ViewPreset.Bottom);

        Assert.Equal(0f, controller.Yaw);
        Assert.Equal(89f, controller.Pitch);
    }

    #endregion

    #region Mode Tests

    [Fact]
    public void Mode_CanBeChanged()
    {
        var controller = new EditorCameraController { Mode = EditorCameraMode.Fly };

        Assert.Equal(EditorCameraMode.Fly, controller.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToTopDown()
    {
        var controller = new EditorCameraController { Mode = EditorCameraMode.TopDown };

        Assert.Equal(EditorCameraMode.TopDown, controller.Mode);
    }

    #endregion
}
