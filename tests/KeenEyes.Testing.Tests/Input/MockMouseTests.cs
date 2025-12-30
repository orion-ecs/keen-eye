using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.Testing.Tests.Input;

public class MockMouseTests
{
    #region SetPosition

    [Fact]
    public void SetPosition_WithFloats_SetsPosition()
    {
        var mouse = new MockMouse();

        mouse.SetPosition(100, 200);

        Assert.Equal(new Vector2(100, 200), mouse.Position);
    }

    [Fact]
    public void SetPosition_WithVector2_SetsPosition()
    {
        var mouse = new MockMouse();

        mouse.SetPosition(new Vector2(150, 250));

        Assert.Equal(new Vector2(150, 250), mouse.Position);
    }

    #endregion

    #region SetButtonDown/SetButtonUp

    [Fact]
    public void SetButtonDown_SetsButtonPressed()
    {
        var mouse = new MockMouse();

        mouse.SetButtonDown(MouseButton.Left);

        Assert.True(mouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void SetButtonUp_ReleasesButton()
    {
        var mouse = new MockMouse();
        mouse.SetButtonDown(MouseButton.Left);

        mouse.SetButtonUp(MouseButton.Left);

        Assert.False(mouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void IsButtonUp_WhenNotPressed_ReturnsTrue()
    {
        var mouse = new MockMouse();

        Assert.True(mouse.IsButtonUp(MouseButton.Left));
    }

    [Fact]
    public void IsButtonUp_WhenPressed_ReturnsFalse()
    {
        var mouse = new MockMouse();
        mouse.SetButtonDown(MouseButton.Left);

        Assert.False(mouse.IsButtonUp(MouseButton.Left));
    }

    #endregion

    #region SetScrollDelta

    [Fact]
    public void SetScrollDelta_SetsScrollDelta()
    {
        var mouse = new MockMouse();
        mouse.SetScrollDelta(0, 120);

        var state = mouse.GetState();

        Assert.Equal(new Vector2(0, 120), state.ScrollDelta);
    }

    [Fact]
    public void GetState_ClearsScrollDeltaAfterRead()
    {
        var mouse = new MockMouse();
        mouse.SetScrollDelta(0, 120);
        _ = mouse.GetState();

        var state = mouse.GetState();

        Assert.Equal(Vector2.Zero, state.ScrollDelta);
    }

    #endregion

    #region ClearAllButtons

    [Fact]
    public void ClearAllButtons_ReleasesAllButtons()
    {
        var mouse = new MockMouse();
        mouse.SetButtonDown(MouseButton.Left);
        mouse.SetButtonDown(MouseButton.Right);
        mouse.SetButtonDown(MouseButton.Middle);

        mouse.ClearAllButtons();

        Assert.False(mouse.IsButtonDown(MouseButton.Left));
        Assert.False(mouse.IsButtonDown(MouseButton.Right));
        Assert.False(mouse.IsButtonDown(MouseButton.Middle));
    }

    #endregion

    #region Reset

    [Fact]
    public void Reset_ClearsAllState()
    {
        var mouse = new MockMouse();
        mouse.SetPosition(100, 200);
        mouse.SetButtonDown(MouseButton.Left);
        mouse.SetScrollDelta(0, 120);
        mouse.IsCursorVisible = false;
        mouse.IsCursorCaptured = true;

        mouse.Reset();

        Assert.Equal(Vector2.Zero, mouse.Position);
        Assert.False(mouse.IsButtonDown(MouseButton.Left));
        Assert.True(mouse.IsCursorVisible);
        Assert.False(mouse.IsCursorCaptured);
    }

    #endregion

    #region SimulateMove

    [Fact]
    public void SimulateMove_UpdatesPosition()
    {
        var mouse = new MockMouse();

        mouse.SimulateMove(new Vector2(100, 200));

        Assert.Equal(new Vector2(100, 200), mouse.Position);
    }

    [Fact]
    public void SimulateMove_FiresOnMoveEvent()
    {
        var mouse = new MockMouse();
        MouseMoveEventArgs? receivedArgs = null;
        mouse.OnMove += args => receivedArgs = args;

        mouse.SimulateMove(new Vector2(100, 200));

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(100, 200), receivedArgs.Value.Position);
    }

    [Fact]
    public void SimulateMove_CalculatesDelta()
    {
        var mouse = new MockMouse();
        mouse.SetPosition(50, 50);
        MouseMoveEventArgs? receivedArgs = null;
        mouse.OnMove += args => receivedArgs = args;

        mouse.SimulateMove(new Vector2(100, 200));

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(50, 150), receivedArgs.Value.Delta);
    }

    #endregion

    #region SimulateMoveBy

    [Fact]
    public void SimulateMoveBy_UpdatesPositionByDelta()
    {
        var mouse = new MockMouse();
        mouse.SetPosition(100, 100);

        mouse.SimulateMoveBy(new Vector2(50, 50));

        Assert.Equal(new Vector2(150, 150), mouse.Position);
    }

    [Fact]
    public void SimulateMoveBy_FiresOnMoveEvent()
    {
        var mouse = new MockMouse();
        MouseMoveEventArgs? receivedArgs = null;
        mouse.OnMove += args => receivedArgs = args;

        mouse.SimulateMoveBy(new Vector2(25, 75));

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(25, 75), receivedArgs.Value.Delta);
    }

    #endregion

    #region SimulateButtonDown/SimulateButtonUp

    [Fact]
    public void SimulateButtonDown_SetsButtonPressed()
    {
        var mouse = new MockMouse();

        mouse.SimulateButtonDown(MouseButton.Left);

        Assert.True(mouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void SimulateButtonDown_FiresOnButtonDownEvent()
    {
        var mouse = new MockMouse();
        MouseButtonEventArgs? receivedArgs = null;
        mouse.OnButtonDown += args => receivedArgs = args;

        mouse.SimulateButtonDown(MouseButton.Left, KeyModifiers.Shift);

        Assert.NotNull(receivedArgs);
        Assert.Equal(MouseButton.Left, receivedArgs.Value.Button);
        Assert.Equal(KeyModifiers.Shift, receivedArgs.Value.Modifiers);
    }

    [Fact]
    public void SimulateButtonUp_ReleasesButton()
    {
        var mouse = new MockMouse();
        mouse.SimulateButtonDown(MouseButton.Left);

        mouse.SimulateButtonUp(MouseButton.Left);

        Assert.False(mouse.IsButtonDown(MouseButton.Left));
    }

    [Fact]
    public void SimulateButtonUp_FiresOnButtonUpEvent()
    {
        var mouse = new MockMouse();
        MouseButtonEventArgs? receivedArgs = null;
        mouse.OnButtonUp += args => receivedArgs = args;

        mouse.SimulateButtonUp(MouseButton.Left, KeyModifiers.Control);

        Assert.NotNull(receivedArgs);
        Assert.Equal(MouseButton.Left, receivedArgs.Value.Button);
        Assert.Equal(KeyModifiers.Control, receivedArgs.Value.Modifiers);
    }

    #endregion

    #region SimulateScroll

    [Fact]
    public void SimulateScroll_FiresOnScrollEvent()
    {
        var mouse = new MockMouse();
        mouse.SetPosition(100, 100);
        MouseScrollEventArgs? receivedArgs = null;
        mouse.OnScroll += args => receivedArgs = args;

        mouse.SimulateScroll(0, 120);

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(0, 120), receivedArgs.Value.Delta);
        Assert.Equal(new Vector2(100, 100), receivedArgs.Value.Position);
    }

    [Fact]
    public void SimulateScrollVertical_FiresOnScrollEvent()
    {
        var mouse = new MockMouse();
        MouseScrollEventArgs? receivedArgs = null;
        mouse.OnScroll += args => receivedArgs = args;

        mouse.SimulateScrollVertical(120);

        Assert.NotNull(receivedArgs);
        Assert.Equal(new Vector2(0, 120), receivedArgs.Value.Delta);
    }

    #endregion

    #region SimulateEnter/SimulateLeave

    [Fact]
    public void SimulateEnter_FiresOnEnterEvent()
    {
        var mouse = new MockMouse();
        var fired = false;
        mouse.OnEnter += () => fired = true;

        mouse.SimulateEnter();

        Assert.True(fired);
    }

    [Fact]
    public void SimulateLeave_FiresOnLeaveEvent()
    {
        var mouse = new MockMouse();
        var fired = false;
        mouse.OnLeave += () => fired = true;

        mouse.SimulateLeave();

        Assert.True(fired);
    }

    #endregion

    #region SimulateClick

    [Fact]
    public void SimulateClick_FiresDownThenUp()
    {
        var mouse = new MockMouse();
        var events = new List<string>();
        mouse.OnButtonDown += _ => events.Add("Down");
        mouse.OnButtonUp += _ => events.Add("Up");

        mouse.SimulateClick(MouseButton.Left);

        Assert.Equal(["Down", "Up"], events);
    }

    #endregion

    #region SimulateDoubleClick

    [Fact]
    public void SimulateDoubleClick_FiresTwoClicks()
    {
        var mouse = new MockMouse();
        var downCount = 0;
        var upCount = 0;
        mouse.OnButtonDown += _ => downCount++;
        mouse.OnButtonUp += _ => upCount++;

        mouse.SimulateDoubleClick(MouseButton.Left);

        Assert.Equal(2, downCount);
        Assert.Equal(2, upCount);
    }

    #endregion

    #region SimulateDrag

    [Fact]
    public void SimulateDrag_WithTwoPoints_PerformsDragSequence()
    {
        var mouse = new MockMouse();
        var events = new List<string>();
        mouse.OnMove += _ => events.Add("Move");
        mouse.OnButtonDown += _ => events.Add("Down");
        mouse.OnButtonUp += _ => events.Add("Up");

        mouse.SimulateDrag(new Vector2(0, 0), new Vector2(100, 100));

        Assert.Equal(["Move", "Down", "Move", "Up"], events);
    }

    [Fact]
    public void SimulateDrag_WithPointsList_PerformsDragSequence()
    {
        var mouse = new MockMouse();
        var events = new List<string>();
        mouse.OnMove += _ => events.Add("Move");
        mouse.OnButtonDown += _ => events.Add("Down");
        mouse.OnButtonUp += _ => events.Add("Up");

        var points = new List<Vector2>
        {
            new(0, 0),
            new(50, 50),
            new(100, 100)
        };
        mouse.SimulateDrag(points);

        Assert.Equal(["Move", "Down", "Move", "Move", "Up"], events);
    }

    [Fact]
    public void SimulateDrag_WithFewerThan2Points_ThrowsArgumentException()
    {
        var mouse = new MockMouse();
        var points = new List<Vector2> { new(0, 0) };

        var ex = Assert.Throws<ArgumentException>(() => mouse.SimulateDrag(points));
        Assert.Contains("2 points", ex.Message);
    }

    #endregion

    #region GetState

    [Fact]
    public void GetState_ReturnsCurrentState()
    {
        var mouse = new MockMouse();
        mouse.SetPosition(100, 200);
        mouse.SetButtonDown(MouseButton.Left);
        mouse.SetButtonDown(MouseButton.Right);
        mouse.SetScrollDelta(0, 120);

        var state = mouse.GetState();

        Assert.Equal(new Vector2(100, 200), state.Position);
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Left));
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Right));
        Assert.Equal(new Vector2(0, 120), state.ScrollDelta);
    }

    [Fact]
    public void GetState_IncludesAllButtonFlags()
    {
        var mouse = new MockMouse();
        mouse.SetButtonDown(MouseButton.Left);
        mouse.SetButtonDown(MouseButton.Right);
        mouse.SetButtonDown(MouseButton.Middle);
        mouse.SetButtonDown(MouseButton.Button4);
        mouse.SetButtonDown(MouseButton.Button5);

        var state = mouse.GetState();

        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Left));
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Right));
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Middle));
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Button4));
        Assert.True(state.PressedButtons.HasFlag(MouseButtons.Button5));
    }

    #endregion

    #region IMouse.SetPosition

    [Fact]
    public void IMouse_SetPosition_SetsPosition()
    {
        IMouse mouse = new MockMouse();

        mouse.SetPosition(new Vector2(100, 200));

        Assert.Equal(new Vector2(100, 200), mouse.Position);
    }

    #endregion

    #region IsCursorVisible/IsCursorCaptured

    [Fact]
    public void IsCursorVisible_DefaultTrue()
    {
        var mouse = new MockMouse();

        Assert.True(mouse.IsCursorVisible);
    }

    [Fact]
    public void IsCursorCaptured_DefaultFalse()
    {
        var mouse = new MockMouse();

        Assert.False(mouse.IsCursorCaptured);
    }

    [Fact]
    public void IsCursorVisible_CanBeSet()
    {
        var mouse = new MockMouse { IsCursorVisible = false };

        Assert.False(mouse.IsCursorVisible);
    }

    [Fact]
    public void IsCursorCaptured_CanBeSet()
    {
        var mouse = new MockMouse { IsCursorCaptured = true };

        Assert.True(mouse.IsCursorCaptured);
    }

    #endregion
}
