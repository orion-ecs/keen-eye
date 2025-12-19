using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles keyboard input for text fields.
/// </summary>
/// <remarks>
/// <para>
/// This system processes text input events when a text field is focused:
/// <list type="bullet">
/// <item>Character input - inserts typed characters at cursor</item>
/// <item>Backspace/Delete - removes characters</item>
/// <item>Arrow keys - moves cursor</item>
/// <item>Home/End - jumps to start/end of text</item>
/// <item>Shift+Arrow - text selection</item>
/// <item>Ctrl+A - select all</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UITextInputSystem : SystemBase
{
    private UIContext? uiContext;
    private IInputContext? inputContext;

    private EventSubscription? focusGainedSubscription;
    private EventSubscription? focusLostSubscription;

    private Action<IKeyboard, char>? textInputHandler;
    private Action<IKeyboard, KeyEventArgs>? keyDownHandler;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        focusGainedSubscription = World.Subscribe<UIFocusGainedEvent>(OnFocusGained);
        focusLostSubscription = World.Subscribe<UIFocusLostEvent>(OnFocusLost);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        focusGainedSubscription?.Dispose();
        focusLostSubscription?.Dispose();
        UnsubscribeFromInput();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization of input context
        if (inputContext is null && World.TryGetExtension(out inputContext))
        {
            SubscribeToInput();
        }

        if (uiContext is null)
        {
            World.TryGetExtension(out uiContext);
        }
    }

    private void SubscribeToInput()
    {
        if (inputContext is null)
        {
            return;
        }

        textInputHandler = OnTextInput;
        keyDownHandler = OnKeyDown;

        inputContext.OnTextInput += textInputHandler;
        inputContext.OnKeyDown += keyDownHandler;
    }

    private void UnsubscribeFromInput()
    {
        if (inputContext is null)
        {
            return;
        }

        if (textInputHandler is not null)
        {
            inputContext.OnTextInput -= textInputHandler;
        }

        if (keyDownHandler is not null)
        {
            inputContext.OnKeyDown -= keyDownHandler;
        }
    }

    private void OnFocusGained(UIFocusGainedEvent e)
    {
        if (!World.IsAlive(e.Element) || !World.Has<UITextInput>(e.Element))
        {
            return;
        }

        ref var textInput = ref World.Get<UITextInput>(e.Element);
        textInput.IsEditing = true;

        // If showing placeholder, clear it and prepare for input
        if (textInput.ShowingPlaceholder && World.Has<UIText>(e.Element))
        {
            ref var uiText = ref World.Get<UIText>(e.Element);
            uiText.Content = "";
            textInput.ShowingPlaceholder = false;
            textInput.CursorPosition = 0;
            textInput.SelectionStart = 0;
            textInput.SelectionEnd = 0;
        }
        else if (World.Has<UIText>(e.Element))
        {
            // Select all text on focus
            ref readonly var uiText = ref World.Get<UIText>(e.Element);
            var textLength = uiText.Content?.Length ?? 0;
            textInput.CursorPosition = textLength;
            textInput.SelectionStart = 0;
            textInput.SelectionEnd = textLength;
        }
    }

    private void OnFocusLost(UIFocusLostEvent e)
    {
        if (!World.IsAlive(e.Element) || !World.Has<UITextInput>(e.Element))
        {
            return;
        }

        ref var textInput = ref World.Get<UITextInput>(e.Element);
        textInput.IsEditing = false;
        textInput.ClearSelection();

        // If text is empty, show placeholder
        if (World.Has<UIText>(e.Element))
        {
            ref var uiText = ref World.Get<UIText>(e.Element);
            if (string.IsNullOrEmpty(uiText.Content) && !string.IsNullOrEmpty(textInput.PlaceholderText))
            {
                uiText.Content = textInput.PlaceholderText;
                textInput.ShowingPlaceholder = true;
            }
        }
    }

    private void OnTextInput(IKeyboard keyboard, char character)
    {
        if (uiContext is null || !uiContext.HasFocus)
        {
            return;
        }

        var focused = uiContext.FocusedEntity;
        if (!World.IsAlive(focused) || !World.Has<UITextInput>(focused) || !World.Has<UIText>(focused))
        {
            return;
        }

        ref var textInput = ref World.Get<UITextInput>(focused);
        if (!textInput.IsEditing)
        {
            return;
        }

        // Ignore control characters except newline in multiline fields
        if (char.IsControl(character))
        {
            if (character == '\n' || character == '\r')
            {
                if (!textInput.Multiline)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        ref var uiText = ref World.Get<UIText>(focused);
        var oldText = uiText.Content ?? "";

        // Delete selection if any
        if (textInput.HasSelection)
        {
            var (start, end) = textInput.GetSelectionRange();
            oldText = oldText.Remove(start, end - start);
            textInput.CursorPosition = start;
            textInput.ClearSelection();
        }

        // Check max length
        if (textInput.MaxLength > 0 && oldText.Length >= textInput.MaxLength)
        {
            return;
        }

        // Insert character
        var newText = oldText.Insert(textInput.CursorPosition, character.ToString());
        uiText.Content = newText;
        textInput.CursorPosition++;
        textInput.ClearSelection();

        World.Send(new UITextChangedEvent(focused, oldText, newText));
    }

    private void OnKeyDown(IKeyboard keyboard, KeyEventArgs args)
    {
        if (uiContext is null || !uiContext.HasFocus)
        {
            return;
        }

        var focused = uiContext.FocusedEntity;
        if (!World.IsAlive(focused) || !World.Has<UITextInput>(focused) || !World.Has<UIText>(focused))
        {
            return;
        }

        ref var textInput = ref World.Get<UITextInput>(focused);
        if (!textInput.IsEditing)
        {
            return;
        }

        ref var uiText = ref World.Get<UIText>(focused);
        var text = uiText.Content ?? "";
        bool shiftHeld = (args.Modifiers & KeyModifiers.Shift) != 0;
        bool ctrlHeld = (args.Modifiers & KeyModifiers.Control) != 0;

        switch (args.Key)
        {
            case Key.Backspace:
                HandleBackspace(ref textInput, ref uiText, text, focused);
                break;

            case Key.Delete:
                HandleDelete(ref textInput, ref uiText, text, focused);
                break;

            case Key.Left:
                HandleLeftArrow(ref textInput, text, shiftHeld, ctrlHeld);
                break;

            case Key.Right:
                HandleRightArrow(ref textInput, text, shiftHeld, ctrlHeld);
                break;

            case Key.Home:
                HandleHome(ref textInput, shiftHeld);
                break;

            case Key.End:
                HandleEnd(ref textInput, text, shiftHeld);
                break;

            case Key.A when ctrlHeld:
                // Select all
                textInput.SelectionStart = 0;
                textInput.SelectionEnd = text.Length;
                textInput.CursorPosition = text.Length;
                break;
        }
    }

    private void HandleBackspace(ref UITextInput textInput, ref UIText uiText, string text, Entity focused)
    {
        if (textInput.HasSelection)
        {
            var (start, end) = textInput.GetSelectionRange();
            var newText = text.Remove(start, end - start);
            uiText.Content = newText;
            textInput.CursorPosition = start;
            textInput.ClearSelection();
            World.Send(new UITextChangedEvent(focused, text, newText));
        }
        else if (textInput.CursorPosition > 0)
        {
            var newText = text.Remove(textInput.CursorPosition - 1, 1);
            uiText.Content = newText;
            textInput.CursorPosition--;
            World.Send(new UITextChangedEvent(focused, text, newText));
        }
    }

    private void HandleDelete(ref UITextInput textInput, ref UIText uiText, string text, Entity focused)
    {
        if (textInput.HasSelection)
        {
            var (start, end) = textInput.GetSelectionRange();
            var newText = text.Remove(start, end - start);
            uiText.Content = newText;
            textInput.CursorPosition = start;
            textInput.ClearSelection();
            World.Send(new UITextChangedEvent(focused, text, newText));
        }
        else if (textInput.CursorPosition < text.Length)
        {
            var newText = text.Remove(textInput.CursorPosition, 1);
            uiText.Content = newText;
            World.Send(new UITextChangedEvent(focused, text, newText));
        }
    }

    private static void HandleLeftArrow(ref UITextInput textInput, string text, bool shiftHeld, bool ctrlHeld)
    {
        if (textInput.CursorPosition > 0)
        {
            int newPos;
            if (ctrlHeld)
            {
                // Move to previous word boundary
                newPos = FindPreviousWordBoundary(text, textInput.CursorPosition);
            }
            else
            {
                newPos = textInput.CursorPosition - 1;
            }

            if (shiftHeld)
            {
                // Extend selection
                if (!textInput.HasSelection)
                {
                    textInput.SelectionStart = textInput.CursorPosition;
                }

                textInput.CursorPosition = newPos;
                textInput.SelectionEnd = newPos;
            }
            else
            {
                if (textInput.HasSelection)
                {
                    // Collapse selection to start
                    var (start, _) = textInput.GetSelectionRange();
                    textInput.CursorPosition = start;
                    textInput.ClearSelection();
                }
                else
                {
                    textInput.CursorPosition = newPos;
                }
            }
        }
        else if (!shiftHeld && textInput.HasSelection)
        {
            textInput.CursorPosition = 0;
            textInput.ClearSelection();
        }
    }

    private static void HandleRightArrow(ref UITextInput textInput, string text, bool shiftHeld, bool ctrlHeld)
    {
        if (textInput.CursorPosition < text.Length)
        {
            int newPos;
            if (ctrlHeld)
            {
                // Move to next word boundary
                newPos = FindNextWordBoundary(text, textInput.CursorPosition);
            }
            else
            {
                newPos = textInput.CursorPosition + 1;
            }

            if (shiftHeld)
            {
                // Extend selection
                if (!textInput.HasSelection)
                {
                    textInput.SelectionStart = textInput.CursorPosition;
                }

                textInput.CursorPosition = newPos;
                textInput.SelectionEnd = newPos;
            }
            else
            {
                if (textInput.HasSelection)
                {
                    // Collapse selection to end
                    var (_, end) = textInput.GetSelectionRange();
                    textInput.CursorPosition = end;
                    textInput.ClearSelection();
                }
                else
                {
                    textInput.CursorPosition = newPos;
                }
            }
        }
        else if (!shiftHeld && textInput.HasSelection)
        {
            textInput.CursorPosition = text.Length;
            textInput.ClearSelection();
        }
    }

    private static void HandleHome(ref UITextInput textInput, bool shiftHeld)
    {
        if (shiftHeld)
        {
            if (!textInput.HasSelection)
            {
                textInput.SelectionStart = textInput.CursorPosition;
            }

            textInput.SelectionEnd = 0;
            textInput.CursorPosition = 0;
        }
        else
        {
            textInput.CursorPosition = 0;
            textInput.ClearSelection();
        }
    }

    private static void HandleEnd(ref UITextInput textInput, string text, bool shiftHeld)
    {
        if (shiftHeld)
        {
            if (!textInput.HasSelection)
            {
                textInput.SelectionStart = textInput.CursorPosition;
            }

            textInput.SelectionEnd = text.Length;
            textInput.CursorPosition = text.Length;
        }
        else
        {
            textInput.CursorPosition = text.Length;
            textInput.ClearSelection();
        }
    }

    private static int FindPreviousWordBoundary(string text, int position)
    {
        if (position <= 0)
        {
            return 0;
        }

        int pos = position - 1;

        // Skip whitespace
        while (pos > 0 && char.IsWhiteSpace(text[pos]))
        {
            pos--;
        }

        // Skip word characters
        while (pos > 0 && !char.IsWhiteSpace(text[pos - 1]))
        {
            pos--;
        }

        return pos;
    }

    private static int FindNextWordBoundary(string text, int position)
    {
        if (position >= text.Length)
        {
            return text.Length;
        }

        int pos = position;

        // Skip current word
        while (pos < text.Length && !char.IsWhiteSpace(text[pos]))
        {
            pos++;
        }

        // Skip whitespace
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
        {
            pos++;
        }

        return pos;
    }
}
