using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIFocusSystem keyboard navigation and focus management.
/// </summary>
public class UIFocusSystemTests
{
    #region Tab Navigation Tests

    [Fact]
    public void TabKey_WithFocusableElements_NavigatesToNext()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Tab
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button2, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_AtLastElement_WrapsToFirst()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Focus last button
        uiContext.RequestFocus(button2);

        // Press Tab
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button1, uiContext.FocusedEntity);
    }

    [Fact]
    public void ShiftTab_WithFocusableElements_NavigatesToPrevious()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Focus second button
        uiContext.RequestFocus(button2);

        // Press Shift+Tab
        input.PressKey(Key.Tab, KeyModifiers.Shift);
        system.Update(0);

        Assert.Equal(button1, uiContext.FocusedEntity);
    }

    [Fact]
    public void ShiftTab_AtFirstElement_WrapsToLast()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Shift+Tab
        input.PressKey(Key.Tab, KeyModifiers.Shift);
        system.Update(0);

        Assert.Equal(button2, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_WithNoFocus_FocusesFirstElement()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Press Tab with no focus
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button1, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_RespectsTabIndex()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 10); // Higher tab index
        var button2 = CreateButton(world, canvas, 5);  // Lower tab index

        // Press Tab (should focus lower tab index first)
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button2, uiContext.FocusedEntity);

        // Press Tab again
        input.ReleaseKey(Key.Tab);
        system.Update(0);
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button1, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_SkipsHiddenElements()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);
        var button3 = CreateButton(world, canvas, 2);

        // Hide middle button
        world.Add(button2, new UIHiddenTag());

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Tab (should skip hidden button2)
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button3, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_SkipsDisabledElements()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);
        var button3 = CreateButton(world, canvas, 2);

        // Disable middle button
        world.Add(button2, new UIDisabledTag());

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Tab (should skip disabled button2)
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button3, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_WithNoFocusableElements_DoesNothing()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        // No focusable elements
        var canvas = uiContext.CreateCanvas();

        // Press Tab
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.False(uiContext.HasFocus);
    }

    #endregion

    #region Escape Key Tests

    [Fact]
    public void EscapeKey_WithFocus_ClearsFocus()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        // Focus button
        uiContext.RequestFocus(button);
        Assert.True(uiContext.HasFocus);

        // Press Escape
        input.PressKey(Key.Escape);
        system.Update(0);

        Assert.False(uiContext.HasFocus);
        Assert.Equal(Entity.Null, uiContext.FocusedEntity);
    }

    [Fact]
    public void EscapeKey_WithNoFocus_DoesNothing()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        // Press Escape with no focus
        input.PressKey(Key.Escape);
        system.Update(0);

        Assert.False(uiContext.HasFocus);
    }

    #endregion

    #region Enter/Space Key Tests

    [Fact]
    public void EnterKey_OnFocusedElement_FiresSubmitEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        UISubmitEvent? receivedEvent = null;
        world.Subscribe<UISubmitEvent>(e => receivedEvent = e);

        // Focus and press Enter
        uiContext.RequestFocus(button);
        input.PressKey(Key.Enter);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
    }

    [Fact]
    public void SpaceKey_OnFocusedClickable_FiresClickEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        UIClickEvent? receivedEvent = null;
        world.Subscribe<UIClickEvent>(e => receivedEvent = e);

        // Focus and press Space
        uiContext.RequestFocus(button);
        input.PressKey(Key.Space);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
    }

    [Fact]
    public void KeypadEnter_OnFocusedElement_FiresSubmitEvent()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        UISubmitEvent? receivedEvent = null;
        world.Subscribe<UISubmitEvent>(e => receivedEvent = e);

        // Focus and press Keypad Enter
        uiContext.RequestFocus(button);
        input.PressKey(Key.KeypadEnter);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(button, receivedEvent.Value.Element);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Update_WithNoInputContext_DoesNothing()
    {
        using var world = new World();
        var system = new UIFocusSystem();
        world.AddSystem(system);

        // No InputContext - should not crash
        system.Update(0);
    }

    [Fact]
    public void Update_WithNoUIContext_DoesNothing()
    {
        using var world = new World();
        var input = new MockInputContext();
        world.SetExtension<IInputContext>(input);
        // No UIContext

        var system = new UIFocusSystem();
        world.AddSystem(system);

        // Should not crash
        system.Update(0);
    }

    [Fact]
    public void TabKey_SkipsInvisibleElements()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);
        var button3 = CreateButton(world, canvas, 2);

        // Make middle button invisible
        ref var element = ref world.Get<UIElement>(button2);
        element.Visible = false;

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Tab (should skip invisible button2)
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button3, uiContext.FocusedEntity);
    }

    [Fact]
    public void SpaceKey_OnNonClickable_DoesNotFireClick()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();

        // Create non-clickable focusable element
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 50))
            .With(new UIInteractable { CanFocus = true, CanClick = false, TabIndex = 0 })
            .Build();
        world.SetParent(element, canvas);

        UIClickEvent? receivedEvent = null;
        world.Subscribe<UIClickEvent>(e => receivedEvent = e);

        // Focus and press Space
        uiContext.RequestFocus(element);
        input.PressKey(Key.Space);
        system.Update(0);

        Assert.Null(receivedEvent);
    }

    [Fact]
    public void EnterAndSpace_SimultaneousPress_FiresBothEvents()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        UISubmitEvent? submitEvent = null;
        UIClickEvent? clickEvent = null;
        world.Subscribe<UISubmitEvent>(e => submitEvent = e);
        world.Subscribe<UIClickEvent>(e => clickEvent = e);

        // Focus and press both Enter and Space
        uiContext.RequestFocus(button);
        input.PressKey(Key.Enter);
        input.PressKeyOnly(Key.Space); // Press Space without resetting modifiers
        system.Update(0);

        Assert.NotNull(submitEvent);
        Assert.NotNull(clickEvent);
    }

    [Fact]
    public void FocusedEntity_BecomesDeadDuringUpdate_DoesNotCrash()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        // Focus button then despawn it
        uiContext.RequestFocus(button);
        world.Despawn(button);

        // Press Enter - should not crash
        input.PressKey(Key.Enter);
        system.Update(0);
    }

    [Fact]
    public void FocusedEntity_LosesInteractableComponent_DoesNotCrash()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        // Focus button then remove interactable
        uiContext.RequestFocus(button);
        world.Remove<UIInteractable>(button);

        // Press Enter - should not crash
        input.PressKey(Key.Enter);
        system.Update(0);
    }

    [Fact]
    public void TabKey_SkipsNonFocusableElements()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);

        // Create non-focusable element
        var nonFocusable = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 50))
            .With(new UIInteractable { CanFocus = false, CanClick = true, TabIndex = 1 })
            .Build();
        world.SetParent(nonFocusable, canvas);

        var button3 = CreateButton(world, canvas, 2);

        // Focus first button
        uiContext.RequestFocus(button1);

        // Press Tab (should skip non-focusable element)
        input.PressKey(Key.Tab);
        system.Update(0);

        Assert.Equal(button3, uiContext.FocusedEntity);
    }

    [Fact]
    public void ShiftTab_WithNoFocus_FocusesLastElement()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);

        // Press Shift+Tab with no focus (should focus last element)
        input.PressKey(Key.Tab, KeyModifiers.Shift);
        system.Update(0);

        Assert.Equal(button2, uiContext.FocusedEntity);
    }

    [Fact]
    public void TabKey_SameTabIndex_NavigatesConsistently()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        // All buttons have same tab index
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 0);
        var button3 = CreateButton(world, canvas, 0);

        // Press Tab - should focus some button
        input.PressKey(Key.Tab);
        system.Update(0);

        var firstFocused = uiContext.FocusedEntity;
        Assert.True(firstFocused == button1 || firstFocused == button2 || firstFocused == button3);

        // Release and press again - should move to different button
        input.ReleaseKey(Key.Tab);
        system.Update(0);
        input.PressKey(Key.Tab);
        system.Update(0);

        var secondFocused = uiContext.FocusedEntity;
        Assert.NotEqual(firstFocused, secondFocused);
    }

    [Fact]
    public void TabKey_HeldDown_OnlyNavigatesOnce()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button1 = CreateButton(world, canvas, 0);
        var button2 = CreateButton(world, canvas, 1);
        var button3 = CreateButton(world, canvas, 2);

        // Press Tab
        input.PressKey(Key.Tab);
        system.Update(0);
        Assert.Equal(button1, uiContext.FocusedEntity);

        // Keep Tab pressed - should not navigate again
        system.Update(0);
        Assert.Equal(button1, uiContext.FocusedEntity);

        // Release and press again
        input.ReleaseKey(Key.Tab);
        system.Update(0);
        input.PressKey(Key.Tab);
        system.Update(0);
        Assert.Equal(button2, uiContext.FocusedEntity);
    }

    [Fact]
    public void EscapeKey_HeldDown_OnlyClearsFocusOnce()
    {
        using var world = CreateWorldWithInput(out var input, out var uiContext);
        var system = new UIFocusSystem();
        world.AddSystem(system);

        var canvas = uiContext.CreateCanvas();
        var button = CreateButton(world, canvas, 0);

        // Focus button
        uiContext.RequestFocus(button);

        // Press Escape
        input.PressKey(Key.Escape);
        system.Update(0);
        Assert.False(uiContext.HasFocus);

        // Focus again
        uiContext.RequestFocus(button);
        Assert.True(uiContext.HasFocus);

        // Escape still held - should not clear focus again
        system.Update(0);
        Assert.True(uiContext.HasFocus);
    }

    #endregion

    #region Helper Methods

    private static World CreateWorldWithInput(out MockInputContext input, out UIContext uiContext)
    {
        var world = new World();
        input = new MockInputContext();
        uiContext = new UIContext(world);

        world.SetExtension<IInputContext>(input);
        world.SetExtension(uiContext);

        return world;
    }

    private static Entity CreateButton(World world, Entity parent, int tabIndex)
    {
        var button = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 50))
            .With(new UIInteractable
            {
                CanFocus = true,
                CanClick = true,
                TabIndex = tabIndex
            })
            .Build();
        world.SetParent(button, parent);
        return button;
    }

    #endregion

    #region Mock Input Context

    private sealed class MockInputContext : IInputContext
    {
        private readonly MockKeyboard keyboard = new();
        private readonly MockMouse mouse = new();

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

        public void PressKey(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            keyboard.SetKey(key, true);
            keyboard.SetModifiers(modifiers);
        }

        public void PressKeyOnly(Key key)
        {
            // Press key without changing modifiers
            keyboard.SetKey(key, true);
        }

        public void ReleaseKey(Key key)
        {
            keyboard.SetKey(key, false);
            keyboard.SetModifiers(KeyModifiers.None);
        }

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
        public System.Numerics.Vector2 Position => System.Numerics.Vector2.Zero;

        public bool IsCursorVisible { get; set; } = true;
        public bool IsCursorCaptured { get; set; } = false;

        public MouseState GetState() => new(System.Numerics.Vector2.Zero, MouseButtons.None, System.Numerics.Vector2.Zero);
        public bool IsButtonDown(MouseButton button) => false;
        public bool IsButtonUp(MouseButton button) => true;
        public void SetPosition(System.Numerics.Vector2 position) { }

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
        private KeyModifiers modifiers;

        public KeyModifiers Modifiers => modifiers;

        public KeyboardState GetState() => new([.. keysDown], modifiers);
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

        public void SetModifiers(KeyModifiers value) => modifiers = value;

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
