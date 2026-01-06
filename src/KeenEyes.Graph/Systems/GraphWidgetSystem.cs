using KeenEyes.Graph.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that handles input for focused widgets in graph nodes.
/// </summary>
/// <remarks>
/// <para>
/// Runs before <see cref="GraphInputSystem"/> to intercept keyboard and mouse input
/// for widgets that have focus. This prevents node dragging and other interactions
/// when editing widget values.
/// </para>
/// <para>
/// Handles text editing (backspace, typing), slider dragging, dropdown selection,
/// and focus management (clicking outside to clear focus, ESC to cancel).
/// </para>
/// </remarks>
public sealed class GraphWidgetSystem : SystemBase
{
    private IInputContext? inputContext;
    private GraphContext? graphContext;

    // Key state for debouncing
    private readonly HashSet<Key> keysDownLastFrame = [];

    // Mouse state for drag detection
    private bool isDraggingSlider;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (inputContext is null && !World.TryGetExtension(out inputContext))
        {
            return;
        }

        if (graphContext is null && !World.TryGetExtension(out graphContext))
        {
            return;
        }

        var mouse = inputContext!.Mouse;
        var keyboard = inputContext.Keyboard;

        // Process each canvas that has a focused widget
        foreach (var canvas in World.Query<GraphCanvas, WidgetFocus, GraphCanvasTag>())
        {
            ProcessWidgetInput(canvas, mouse, keyboard, deltaTime);
        }

        // Update state for next frame
        UpdateKeyState(keyboard);
    }

    private void ProcessWidgetInput(Entity canvas, IMouse mouse, IKeyboard keyboard, float deltaTime)
    {
        ref var focus = ref World.Get<WidgetFocus>(canvas);

        // Handle ESC to cancel editing
        if (WasKeyJustPressed(keyboard, Key.Escape))
        {
            ClearFocus(canvas);
            return;
        }

        // Handle Enter to commit and clear focus
        if (WasKeyJustPressed(keyboard, Key.Enter))
        {
            CommitAndClearFocus(canvas, ref focus);
            return;
        }

        // Process input based on widget type
        switch (focus.Type)
        {
            case WidgetType.FloatField:
            case WidgetType.IntField:
                ProcessTextInput(ref focus, keyboard);
                break;

            case WidgetType.TextArea:
                ProcessTextAreaInput(ref focus, keyboard);
                break;

            case WidgetType.Slider:
                ProcessSliderInput(ref focus, mouse, deltaTime);
                break;

            case WidgetType.Dropdown:
                ProcessDropdownInput(ref focus, keyboard);
                break;

            case WidgetType.ColorPicker:
                ProcessColorPickerInput(ref focus, mouse, keyboard);
                break;
        }
    }

    private void ProcessTextInput(ref WidgetFocus focus, IKeyboard keyboard)
    {
        // Handle backspace
        if (WasKeyJustPressed(keyboard, Key.Backspace) && focus.EditBuffer.Length > 0)
        {
            if (focus.CursorPosition > 0)
            {
                focus.EditBuffer = focus.EditBuffer.Remove(focus.CursorPosition - 1, 1);
                focus.CursorPosition--;
            }
            return;
        }

        // Handle delete
        if (WasKeyJustPressed(keyboard, Key.Delete) && focus.CursorPosition < focus.EditBuffer.Length)
        {
            focus.EditBuffer = focus.EditBuffer.Remove(focus.CursorPosition, 1);
            return;
        }

        // Handle cursor movement
        if (WasKeyJustPressed(keyboard, Key.Left) && focus.CursorPosition > 0)
        {
            focus.CursorPosition--;
            return;
        }

        if (WasKeyJustPressed(keyboard, Key.Right) && focus.CursorPosition < focus.EditBuffer.Length)
        {
            focus.CursorPosition++;
            return;
        }

        // Handle Home/End
        if (WasKeyJustPressed(keyboard, Key.Home))
        {
            focus.CursorPosition = 0;
            return;
        }

        if (WasKeyJustPressed(keyboard, Key.End))
        {
            focus.CursorPosition = focus.EditBuffer.Length;
            return;
        }

        // Handle character input for number fields
        ProcessNumberKeys(ref focus, keyboard);
    }

    private void ProcessNumberKeys(ref WidgetFocus focus, IKeyboard keyboard)
    {
        // Number keys
        for (var key = Key.Number0; key <= Key.Number9; key++)
        {
            if (WasKeyJustPressed(keyboard, key))
            {
                var digit = (char)('0' + (key - Key.Number0));
                InsertChar(ref focus, digit);
                return;
            }
        }

        // Numpad keys
        for (var key = Key.Keypad0; key <= Key.Keypad9; key++)
        {
            if (WasKeyJustPressed(keyboard, key))
            {
                var digit = (char)('0' + (key - Key.Keypad0));
                InsertChar(ref focus, digit);
                return;
            }
        }

        // Minus/negative
        if (WasKeyJustPressed(keyboard, Key.Minus) || WasKeyJustPressed(keyboard, Key.KeypadSubtract))
        {
            // Only allow minus at the beginning
            if (focus.CursorPosition == 0 && !focus.EditBuffer.Contains('-'))
            {
                InsertChar(ref focus, '-');
            }
            return;
        }

        // Decimal point (for float fields only)
        if (focus.Type == WidgetType.FloatField)
        {
            if (WasKeyJustPressed(keyboard, Key.Period) || WasKeyJustPressed(keyboard, Key.KeypadDecimal))
            {
                if (!focus.EditBuffer.Contains('.'))
                {
                    InsertChar(ref focus, '.');
                }
            }
        }
    }

    private void ProcessTextAreaInput(ref WidgetFocus focus, IKeyboard keyboard)
    {
        // Handle backspace
        if (WasKeyJustPressed(keyboard, Key.Backspace) && focus.EditBuffer.Length > 0)
        {
            if (focus.CursorPosition > 0)
            {
                focus.EditBuffer = focus.EditBuffer.Remove(focus.CursorPosition - 1, 1);
                focus.CursorPosition--;
            }
            return;
        }

        // Handle alphanumeric keys (simplified)
        for (var key = Key.A; key <= Key.Z; key++)
        {
            if (WasKeyJustPressed(keyboard, key))
            {
                var shift = (keyboard.Modifiers & KeyModifiers.Shift) != 0;
                var ch = shift ? (char)key : char.ToLowerInvariant((char)key);
                InsertChar(ref focus, ch);
                return;
            }
        }

        // Handle number keys
        ProcessNumberKeys(ref focus, keyboard);

        // Handle space
        if (WasKeyJustPressed(keyboard, Key.Space))
        {
            InsertChar(ref focus, ' ');
        }
    }

    private void ProcessSliderInput(ref WidgetFocus focus, IMouse mouse, float deltaTime)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            if (!isDraggingSlider)
            {
                // Start drag
                isDraggingSlider = true;
                focus.DragStartValue = float.TryParse(focus.EditBuffer, out var v) ? v : 0f;
            }

            // Continue drag - actual value update happens in NodeWidgets based on position
        }
        else if (isDraggingSlider)
        {
            // End drag
            isDraggingSlider = false;
        }
    }

    private void ProcessDropdownInput(ref WidgetFocus focus, IKeyboard keyboard)
    {
        // Handle arrow keys for selection
        if (WasKeyJustPressed(keyboard, Key.Down))
        {
            // Increment selection (actual bounds checking in NodeWidgets)
            var current = int.TryParse(focus.EditBuffer, out var v) ? v : 0;
            focus.EditBuffer = (current + 1).ToString();
        }
        else if (WasKeyJustPressed(keyboard, Key.Up))
        {
            var current = int.TryParse(focus.EditBuffer, out var v) ? v : 0;
            if (current > 0)
            {
                focus.EditBuffer = (current - 1).ToString();
            }
        }

        // Space toggles expanded state
        if (WasKeyJustPressed(keyboard, Key.Space))
        {
            focus.IsExpanded = !focus.IsExpanded;
        }
    }

    private void ProcessColorPickerInput(ref WidgetFocus focus, IMouse mouse, IKeyboard keyboard)
    {
        // Color picker input is handled primarily through mouse clicks in the popup
        // Space toggles the picker popup
        if (WasKeyJustPressed(keyboard, Key.Space))
        {
            focus.IsExpanded = !focus.IsExpanded;
        }
    }

    private static void InsertChar(ref WidgetFocus focus, char c)
    {
        focus.EditBuffer = focus.EditBuffer.Insert(focus.CursorPosition, c.ToString());
        focus.CursorPosition++;
    }

    private void ClearFocus(Entity canvas)
    {
        World.Remove<WidgetFocus>(canvas);
    }

    private void CommitAndClearFocus(Entity canvas, ref WidgetFocus focus)
    {
        // Value has been edited in EditBuffer - the node's RenderBody will read it on next render
        // Just clear the focus component
        World.Remove<WidgetFocus>(canvas);
    }

    private bool WasKeyJustPressed(IKeyboard keyboard, Key key)
    {
        var isDownNow = keyboard.IsKeyDown(key);
        var wasDownLastFrame = keysDownLastFrame.Contains(key);
        return isDownNow && !wasDownLastFrame;
    }

    private void UpdateKeyState(IKeyboard keyboard)
    {
        keysDownLastFrame.Clear();

        // Track common editing keys
        Key[] keysToTrack =
        [
            Key.Escape, Key.Enter, Key.Backspace, Key.Delete,
            Key.Left, Key.Right, Key.Up, Key.Down,
            Key.Home, Key.End, Key.Space,
            Key.Minus, Key.Period,
            Key.KeypadSubtract, Key.KeypadDecimal
        ];

        foreach (var key in keysToTrack)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }

        // Track letter keys
        for (var key = Key.A; key <= Key.Z; key++)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }

        // Track number keys
        for (var key = Key.Number0; key <= Key.Number9; key++)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }

        // Track numpad keys
        for (var key = Key.Keypad0; key <= Key.Keypad9; key++)
        {
            if (keyboard.IsKeyDown(key))
            {
                keysDownLastFrame.Add(key);
            }
        }
    }
}
