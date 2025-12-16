using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;
using SilkInput = Silk.NET.Input;

namespace KeenEyes.Input.Silk;

/// <summary>
/// Silk.NET implementation of <see cref="IKeyboard"/>.
/// </summary>
internal sealed class SilkKeyboard : IKeyboard
{
    private readonly SilkInput.IKeyboard keyboard;

    /// <inheritdoc />
    public KeyModifiers Modifiers => GetCurrentModifiers();

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyDown;

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyUp;

    /// <inheritdoc />
    public event Action<char>? OnTextInput;

    internal SilkKeyboard(SilkInput.IKeyboard keyboard)
    {
        this.keyboard = keyboard;

        keyboard.KeyDown += HandleKeyDown;
        keyboard.KeyUp += HandleKeyUp;
        keyboard.KeyChar += HandleKeyChar;
    }

    /// <inheritdoc />
    public bool IsKeyDown(Key key)
    {
        var silkKey = KeyMapper.ToSilkKey(key);
        return keyboard.IsKeyPressed(silkKey);
    }

    /// <inheritdoc />
    public bool IsKeyUp(Key key)
    {
        return !IsKeyDown(key);
    }

    /// <inheritdoc />
    public KeyboardState GetState()
    {
        // Build state from currently pressed keys
        var pressedKeysBuilder = ImmutableHashSet.CreateBuilder<Key>();

        foreach (var silkKey in keyboard.SupportedKeys)
        {
            if (keyboard.IsKeyPressed(silkKey))
            {
                var key = KeyMapper.FromSilkKey(silkKey);
                if (key.HasValue)
                {
                    pressedKeysBuilder.Add(key.Value);
                }
            }
        }

        return new KeyboardState(pressedKeysBuilder.ToImmutable(), GetCurrentModifiers());
    }

    private void HandleKeyDown(SilkInput.IKeyboard _, SilkInput.Key silkKey, int scanCode)
    {
        var key = KeyMapper.FromSilkKey(silkKey);
        if (key.HasValue)
        {
            var modifiers = GetCurrentModifiers();
            OnKeyDown?.Invoke(new KeyEventArgs(key.Value, modifiers, false));
        }
    }

    private void HandleKeyUp(SilkInput.IKeyboard _, SilkInput.Key silkKey, int scanCode)
    {
        var key = KeyMapper.FromSilkKey(silkKey);
        if (key.HasValue)
        {
            var modifiers = GetCurrentModifiers();
            OnKeyUp?.Invoke(new KeyEventArgs(key.Value, modifiers, false));
        }
    }

    private void HandleKeyChar(SilkInput.IKeyboard _, char character)
    {
        OnTextInput?.Invoke(character);
    }

    private KeyModifiers GetCurrentModifiers()
    {
        var modifiers = KeyModifiers.None;

        if (keyboard.IsKeyPressed(SilkInput.Key.ShiftLeft) || keyboard.IsKeyPressed(SilkInput.Key.ShiftRight))
        {
            modifiers |= KeyModifiers.Shift;
        }

        if (keyboard.IsKeyPressed(SilkInput.Key.ControlLeft) || keyboard.IsKeyPressed(SilkInput.Key.ControlRight))
        {
            modifiers |= KeyModifiers.Control;
        }

        if (keyboard.IsKeyPressed(SilkInput.Key.AltLeft) || keyboard.IsKeyPressed(SilkInput.Key.AltRight))
        {
            modifiers |= KeyModifiers.Alt;
        }

        if (keyboard.IsKeyPressed(SilkInput.Key.SuperLeft) || keyboard.IsKeyPressed(SilkInput.Key.SuperRight))
        {
            modifiers |= KeyModifiers.Super;
        }

        return modifiers;
    }
}
