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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
