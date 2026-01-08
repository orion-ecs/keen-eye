using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// A keyboard implementation that merges input from real and virtual sources.
/// </summary>
/// <remarks>
/// <para>
/// CompositeKeyboard allows both real hardware input and virtual (TestBridge-injected)
/// input to work together. State queries return true if EITHER source has the key pressed.
/// Events are forwarded from BOTH sources.
/// </para>
/// <para>
/// This enables hybrid testing scenarios where real user input and automated test
/// input can coexist.
/// </para>
/// </remarks>
internal sealed class CompositeKeyboard : IKeyboard
{
    private readonly IKeyboard real;
    private readonly IKeyboard virtual_;

    /// <summary>
    /// Creates a new composite keyboard merging real and virtual input.
    /// </summary>
    /// <param name="real">The real hardware keyboard.</param>
    /// <param name="virtual_">The virtual (mock) keyboard for TestBridge injection.</param>
    public CompositeKeyboard(IKeyboard real, IKeyboard virtual_)
    {
        this.real = real;
        this.virtual_ = virtual_;

        // Forward events from both sources
        real.OnKeyDown += args => OnKeyDown?.Invoke(args);
        real.OnKeyUp += args => OnKeyUp?.Invoke(args);
        real.OnTextInput += c => OnTextInput?.Invoke(c);

        virtual_.OnKeyDown += args => OnKeyDown?.Invoke(args);
        virtual_.OnKeyUp += args => OnKeyUp?.Invoke(args);
        virtual_.OnTextInput += c => OnTextInput?.Invoke(c);
    }

    /// <inheritdoc />
    public KeyboardState GetState()
    {
        var realState = real.GetState();
        var virtualState = virtual_.GetState();

        // Merge pressed keys from both sources
        var mergedKeys = realState.PressedKeys.Union(virtualState.PressedKeys);

        // Merge modifiers (OR them together)
        var mergedModifiers = realState.Modifiers | virtualState.Modifiers;

        return new KeyboardState(mergedKeys, mergedModifiers);
    }

    /// <inheritdoc />
    public bool IsKeyDown(Key key) => real.IsKeyDown(key) || virtual_.IsKeyDown(key);

    /// <inheritdoc />
    public bool IsKeyUp(Key key) => real.IsKeyUp(key) && virtual_.IsKeyUp(key);

    /// <inheritdoc />
    public KeyModifiers Modifiers => real.Modifiers | virtual_.Modifiers;

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyDown;

    /// <inheritdoc />
    public event Action<KeyEventArgs>? OnKeyUp;

    /// <inheritdoc />
    public event Action<char>? OnTextInput;
}
