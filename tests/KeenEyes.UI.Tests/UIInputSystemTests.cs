using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIInputSystem hit testing and interaction handling.
/// </summary>
public class UIInputSystemTests
{
    #region Hit Testing Tests

    [Fact]
    public void HitTest_WithMouseOverElement_SetsHoveredState()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(150, 150); // Inside button

        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.True(interactable.IsHovered);
        Assert.True(interactable.HasEvent(UIEventType.PointerEnter));
    }

    [Fact]
    public void HitTest_WithMouseOutsideElement_DoesNotSetHovered()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(50, 50); // Outside button

        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.False(interactable.IsHovered);
        Assert.False(interactable.HasEvent(UIEventType.PointerEnter));
    }

    [Fact]
    public void HitTest_MouseExitsElement_ClearsHoveredState()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        // First frame: hover
        input.SetMousePosition(150, 150);
        system.Update(0);

        // Second frame: exit
        input.SetMousePosition(50, 50);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.False(interactable.IsHovered);
        Assert.True(interactable.HasEvent(UIEventType.PointerExit));
    }

    [Fact]
    public void HitTest_MouseEnterEvent_Fired()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        UIPointerEnterEvent? receivedEvent = null;
        world.Subscribe<UIPointerEnterEvent>(e => receivedEvent = e);

        input.SetMousePosition(150, 150);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
    }

    [Fact]
    public void HitTest_MouseExitEvent_Fired()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        // Hover first
        input.SetMousePosition(150, 150);
        system.Update(0);

        UIPointerExitEvent? receivedEvent = null;
        world.Subscribe<UIPointerExitEvent>(e => receivedEvent = e);

        // Exit
        input.SetMousePosition(50, 50);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
    }

    #endregion

    #region Click Tests

    [Fact]
    public void Click_MouseDownOnElement_SetsPressedState()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);

        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.True(interactable.IsPressed);
        Assert.True(interactable.HasEvent(UIEventType.PointerDown));
    }

    [Fact]
    public void Click_MouseUpAfterDown_FiresClickEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(150, 150);

        UIClickEvent? receivedEvent = null;
        world.Subscribe<UIClickEvent>(e => receivedEvent = e);

        // Press
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Release
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
        Assert.Equal(MouseButton.Left, receivedEvent.Value.Button);
    }

    [Fact]
    public void Click_MouseUpAfterDown_ClearsPressedState()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(150, 150);

        // Press
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Release
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.False(interactable.IsPressed);
        Assert.True(interactable.HasEvent(UIEventType.Click));
        Assert.True(interactable.HasEvent(UIEventType.PointerUp));
    }

    [Fact]
    public void Click_MouseUpOutsideElement_DoesNotFireClick()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        UIClickEvent? receivedEvent = null;
        world.Subscribe<UIClickEvent>(e => receivedEvent = e);

        // Press inside
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Release outside
        input.SetMousePosition(50, 50);
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        Assert.Null(receivedEvent);
    }

    [Fact]
    public void Click_OnFocusableElement_RequestsFocus()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);

        system.Update(0);

        Assert.Equal(button, uiContext.FocusedEntity);
    }

    #endregion

    #region Drag Tests

    [Fact]
    public void Drag_MouseMoveWhilePressed_StartsDrag()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        // Press
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Drag far enough to trigger
        input.SetMousePosition(160, 150);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(draggable);
        Assert.True(interactable.IsDragging);
        Assert.True(interactable.HasEvent(UIEventType.DragStart));
    }

    [Fact]
    public void Drag_SmallMovement_DoesNotStartDrag()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        // Press
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Small movement (< 5 pixel threshold)
        input.SetMousePosition(152, 150);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(draggable);
        Assert.False(interactable.IsDragging);
    }

    [Fact]
    public void Drag_WhileDragging_FiresDragEvents()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        UIDragEvent? receivedEvent = null;
        world.Subscribe<UIDragEvent>(e => receivedEvent = e);

        // Press and start drag
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);
        input.SetMousePosition(160, 150);
        system.Update(0);

        // Continue dragging
        input.SetMousePosition(170, 150);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(draggable, receivedEvent.Value.Element);
        Assert.True(receivedEvent.Value.Delta.X.ApproximatelyEquals(10f));
    }

    [Fact]
    public void Drag_MouseRelease_EndsDrag()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0); // Compute bounds after creating elements

        UIDragEndEvent? receivedEvent = null;
        world.Subscribe<UIDragEndEvent>(e => receivedEvent = e);

        // Press and start drag
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);
        input.SetMousePosition(160, 150);
        system.Update(0);

        // Release
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(draggable, receivedEvent.Value.Element);

        ref readonly var interactable = ref world.Get<UIInteractable>(draggable);
        Assert.False(interactable.IsDragging);
        Assert.True(interactable.HasEvent(UIEventType.DragEnd));
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Update_WithNoInputContext_DoesNothing()
    {
        using var world = new World();
        var system = new UIInputSystem();
        world.AddSystem(system);

        // No InputContext extension set - should not crash
        system.Update(0);
    }

    [Fact]
    public void Update_WithNoUIContext_DoesNothing()
    {
        using var world = new World();
        var input = new MockInputContext();
        world.SetExtension<IInputContext>(input);
        // No UIContext set

        var system = new UIInputSystem();
        world.AddSystem(system);

        // Should not crash
        system.Update(0);
    }

    [Fact]
    public void ProcessHover_WithDeadHoveredEntity_HandlesGracefully()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 100, 100, 200, 100);
        var button2 = CreateButton(world, canvas, 100, 250, 200, 100);
        layoutSystem.Update(0);

        // Hover first button
        input.SetMousePosition(150, 150);
        system.Update(0);

        // Despawn the hovered button
        world.Despawn(button);

        // Move to second button - should handle dead first button gracefully
        input.SetMousePosition(150, 300);
        system.Update(0);

        ref readonly var interactable2 = ref world.Get<UIInteractable>(button2);
        Assert.True(interactable2.IsHovered);
    }

    [Fact]
    public void DoubleClick_WithinThreshold_FiresDoubleClickEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0);
        input.SetMousePosition(150, 150);

        // First click
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        // Second click immediately
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(button);
        Assert.True(interactable.HasEvent(UIEventType.DoubleClick));
    }

    [Fact]
    public void DragStart_FiresDragStartEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0);

        UIDragStartEvent? receivedEvent = null;
        world.Subscribe<UIDragStartEvent>(e => receivedEvent = e);

        // Press
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Move enough to trigger drag start
        input.SetMousePosition(160, 150);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(draggable, receivedEvent.Value.Element);
    }

    [Fact]
    public void Press_OnNonClickableNonDraggable_DoesNotSetPressed()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        // Create element with interactable but no click/drag
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 100))
            .With(new UIInteractable { CanClick = false, CanDrag = false, CanFocus = false })
            .Build();
        world.SetParent(element, uiContext.CreateCanvas());
        layoutSystem.Update(0);

        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(element);
        Assert.False(interactable.IsPressed);
    }

    [Fact]
    public void Press_OnDraggableOnly_SetsPressedState()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        // Create element that is draggable but not clickable
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 100))
            .With(new UIInteractable { CanClick = false, CanDrag = true, CanFocus = false })
            .Build();
        world.SetParent(element, uiContext.CreateCanvas());
        layoutSystem.Update(0);

        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        ref readonly var interactable = ref world.Get<UIInteractable>(element);
        Assert.True(interactable.IsPressed);
    }

    [Fact]
    public void Release_OnDeadPressedEntity_DoesNotCrash()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var button = CreateButton(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0);

        // Press
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        // Despawn the pressed entity
        world.Despawn(button);

        // Release should not crash
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);
    }

    [Fact]
    public void Release_WhileDragging_EndsWithDragEndEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var draggable = CreateDraggable(world, uiContext.CreateCanvas(), 100, 100, 200, 100);
        layoutSystem.Update(0);

        // Start drag
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);
        input.SetMousePosition(160, 150);
        system.Update(0);

        ref var interactable = ref world.Get<UIInteractable>(draggable);
        Assert.True(interactable.IsDragging);

        // Release
        input.SetMouseButton(MouseButton.Left, false);
        system.Update(0);

        ref readonly var interactable2 = ref world.Get<UIInteractable>(draggable);
        Assert.False(interactable2.IsDragging);
        Assert.True(interactable2.HasEvent(UIEventType.DragEnd));
    }

    [Fact]
    public void Hover_OnNonInteractableElement_DoesNotSetHovered()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        // Create element without UIInteractable
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 100))
            .Build();
        world.SetParent(element, uiContext.CreateCanvas());
        layoutSystem.Update(0);

        input.SetMousePosition(150, 150);
        system.Update(0);

        Assert.False(world.Has<UIInteractable>(element));
    }

    [Fact]
    public void HoverExit_ToNonInteractableElement_StillClearsHover()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 100, 100, 200, 100);

        // Create non-interactable element
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 250, 200, 100))
            .Build();
        world.SetParent(element, canvas);
        layoutSystem.Update(0);

        // Hover button
        input.SetMousePosition(150, 150);
        system.Update(0);

        ref var buttonInteractable = ref world.Get<UIInteractable>(button);
        Assert.True(buttonInteractable.IsHovered);

        // Move to non-interactable element
        input.SetMousePosition(150, 300);
        system.Update(0);

        ref readonly var buttonInteractable2 = ref world.Get<UIInteractable>(button);
        Assert.False(buttonInteractable2.IsHovered);
    }

    [Fact]
    public void Press_WithAlreadyPressedEntity_DoesNotPressTwice()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 100, 100, 200, 100);
        var button2 = CreateButton(world, canvas, 100, 250, 200, 100);
        layoutSystem.Update(0);

        // Press button1
        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        ref var button1Interactable = ref world.Get<UIInteractable>(button1);
        Assert.True(button1Interactable.IsPressed);

        // Move to button2 while still pressed - should not press button2
        input.SetMousePosition(150, 300);
        system.Update(0);

        ref readonly var button2Interactable = ref world.Get<UIInteractable>(button2);
        Assert.False(button2Interactable.IsPressed);
    }

    [Fact]
    public void Click_OnNonFocusable_DoesNotRequestFocus()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext, out var layoutSystem);
        var system = new UIInputSystem();
        world.AddSystem(system);

        // Create clickable but non-focusable button
        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(100, 100, 200, 100))
            .With(new UIInteractable { CanClick = true, CanDrag = false, CanFocus = false })
            .Build();
        world.SetParent(button, uiContext.CreateCanvas());
        layoutSystem.Update(0);

        input.SetMousePosition(150, 150);
        input.SetMouseButton(MouseButton.Left, true);
        system.Update(0);

        Assert.Equal(Entity.Null, uiContext.FocusedEntity);
    }

    #endregion

    #region Helper Methods

    private static World CreateWorldWithInput(out MockInputContext input, out UIContext uiContext, out UILayoutSystem layoutSystem)
    {
        var world = new World();
        input = new MockInputContext();
        uiContext = new UIContext(world);

        world.SetExtension<IInputContext>(input);
        world.SetExtension(uiContext);

        // Add layout system to compute bounds (caller must call Update after creating elements)
        layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);
        layoutSystem.Initialize(world);
        layoutSystem.SetScreenSize(800, 600);

        return world;
    }

    private static Entity CreateButton(World world, Entity parent, float x, float y, float width, float height)
    {
        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(x, y, width, height))
            .With(UIInteractable.Button())
            .Build();
        world.SetParent(button, parent);
        return button;
    }

    private static Entity CreateDraggable(World world, Entity parent, float x, float y, float width, float height)
    {
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(x, y, width, height))
            .With(UIInteractable.Draggable())
            .Build();
        world.SetParent(element, parent);
        return element;
    }

    #endregion

    #region Mock Input Context

    private sealed class MockInputContext : IInputContext
    {
        private readonly MockMouse mouse = new();
        private readonly MockKeyboard keyboard = new();

        public IMouse Mouse => mouse;
        public IKeyboard Keyboard => keyboard;
        public IGamepad Gamepad => null!;
        public System.Collections.Immutable.ImmutableArray<IKeyboard> Keyboards => [keyboard];
        public System.Collections.Immutable.ImmutableArray<IMouse> Mice => [mouse];
        public System.Collections.Immutable.ImmutableArray<IGamepad> Gamepads => [];
        public int ConnectedGamepadCount => 0;

        public void Update() { }
        public void Dispose() { }

        public event Action<IKeyboard, KeyEventArgs>? OnKeyDown;
        public event Action<IKeyboard, KeyEventArgs>? OnKeyUp;
        public event Action<IKeyboard, char>? OnTextInput;
        public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonDown;
        public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonUp;
        public event Action<IMouse, MouseMoveEventArgs>? OnMouseMove;
        public event Action<IMouse, MouseScrollEventArgs>? OnMouseScroll;
        public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonDown;
        public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonUp;
        public event Action<IGamepad>? OnGamepadConnected;
        public event Action<IGamepad>? OnGamepadDisconnected;

        public void SetMousePosition(float x, float y) => mouse.SetPosition(x, y);
        public void SetMouseButton(MouseButton button, bool isDown) => mouse.SetButton(button, isDown);

        // Suppress unused event warnings
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        private void SuppressWarnings()
        {
            _ = OnKeyDown;
            _ = OnKeyUp;
            _ = OnTextInput;
            _ = OnMouseButtonDown;
            _ = OnMouseButtonUp;
            _ = OnMouseMove;
            _ = OnMouseScroll;
            _ = OnGamepadButtonDown;
            _ = OnGamepadButtonUp;
            _ = OnGamepadConnected;
            _ = OnGamepadDisconnected;
        }
    }

    private sealed class MockMouse : IMouse
    {
        private Vector2 position;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Collection initialization can be simplified")]
        private readonly Dictionary<MouseButton, bool> buttons = new();

        public Vector2 Position => position;

        public bool IsCursorVisible { get; set; } = true;
        public bool IsCursorCaptured { get; set; } = false;

        public MouseState GetState() => new(position, MouseButtons.None, Vector2.Zero);
        public bool IsButtonDown(MouseButton button) => buttons.GetValueOrDefault(button, false);
        public bool IsButtonUp(MouseButton button) => !buttons.GetValueOrDefault(button, false);
        void IMouse.SetPosition(Vector2 position) => this.position = position;

        public void SetPosition(float x, float y) => position = new Vector2(x, y);
        public void SetButton(MouseButton button, bool isDown) => buttons[button] = isDown;

        public event Action<MouseButtonEventArgs>? OnButtonDown;
        public event Action<MouseButtonEventArgs>? OnButtonUp;
        public event Action<MouseMoveEventArgs>? OnMove;
        public event Action<MouseScrollEventArgs>? OnScroll;
        public event Action? OnEnter;
        public event Action? OnLeave;

        // Suppress unused event warnings
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        private void SuppressWarnings()
        {
            _ = OnButtonDown;
            _ = OnButtonUp;
            _ = OnMove;
            _ = OnScroll;
            _ = OnEnter;
            _ = OnLeave;
        }
    }

    private sealed class MockKeyboard : IKeyboard
    {
        private readonly HashSet<Key> keysDown = [];

        public KeyModifiers Modifiers => KeyModifiers.None;

        public KeyboardState GetState() => new([.. keysDown], Modifiers);
        public bool IsKeyDown(Key key) => keysDown.Contains(key);
        public bool IsKeyUp(Key key) => !keysDown.Contains(key);

        public event Action<KeyEventArgs>? OnKeyDown;
        public event Action<KeyEventArgs>? OnKeyUp;
        public event Action<char>? OnTextInput;

        public void SetKey(Key key, bool isDown)
        {
            if (isDown)
            {
                keysDown.Add(key);
            }
            else
            {
                keysDown.Remove(key);
            }
        }

        // Suppress unused event warnings
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        private void SuppressWarnings()
        {
            _ = OnKeyDown;
            _ = OnKeyUp;
            _ = OnTextInput;
        }
    }

    #endregion
}
