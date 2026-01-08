using System.Collections.Immutable;
using KeenEyes.Input.Abstractions;
using KeenEyes.Testing.Input;

namespace KeenEyes.TestBridge.Input;

/// <summary>
/// An input context that merges real hardware input with virtual TestBridge input.
/// </summary>
/// <remarks>
/// <para>
/// CompositeInputContext enables hybrid testing scenarios where both real user input
/// and TestBridge-injected input can work together. State queries return true if
/// EITHER source has the input active. Events are forwarded from BOTH sources.
/// </para>
/// <para>
/// This allows automated tests to inject input while still allowing manual interaction,
/// which is useful for debugging and interactive testing scenarios.
/// </para>
/// <para>
/// Device wrappers are created lazily to support input contexts that aren't
/// initialized until a window loads (like SilkInputContext).
/// </para>
/// </remarks>
public sealed class CompositeInputContext : IInputContext
{
    private readonly IInputContext real;
    private readonly MockInputContext virtual_;
    private CompositeKeyboard? keyboard;
    private CompositeMouse? mouse;
    private ImmutableArray<CompositeGamepad>? gamepads;
    private bool eventsWired;
    private bool disposed;

    /// <summary>
    /// Creates a new composite input context merging real and virtual input.
    /// </summary>
    /// <param name="real">The real hardware input context.</param>
    /// <param name="virtual_">The virtual (mock) input context for TestBridge injection.</param>
    public CompositeInputContext(IInputContext real, MockInputContext virtual_)
    {
        this.real = real;
        this.virtual_ = virtual_;

        // Wire virtual events immediately (mock is always ready)
        WireVirtualEvents();
    }

    /// <summary>
    /// Gets the underlying mock input context for direct TestBridge access.
    /// </summary>
    /// <remarks>
    /// Use this to inject virtual input via the TestBridge.
    /// </remarks>
    public MockInputContext VirtualInput => virtual_;

    #region Lazy Initialization

    private void EnsureInitialized()
    {
        if (keyboard is not null)
        {
            return;
        }

        // Create composite devices
        keyboard = new CompositeKeyboard(real.Keyboard, virtual_.Keyboard);
        mouse = new CompositeMouse(real.Mouse, virtual_.Mouse);

        // Create composite gamepads (match the count from virtual context)
        var gamepadBuilder = ImmutableArray.CreateBuilder<CompositeGamepad>();
        var virtualGamepads = virtual_.Gamepads;
        var realGamepads = real.Gamepads;

        for (int i = 0; i < virtualGamepads.Length; i++)
        {
            var realPad = i < realGamepads.Length ? realGamepads[i] : virtualGamepads[i];
            gamepadBuilder.Add(new CompositeGamepad(realPad, virtualGamepads[i]));
        }

        gamepads = gamepadBuilder.ToImmutable();

        // Wire up real events now that devices are available
        if (!eventsWired)
        {
            eventsWired = true;
            WireRealEvents();
        }
    }

    #endregion

    #region IInputContext Implementation

    /// <inheritdoc />
    public IKeyboard Keyboard
    {
        get
        {
            EnsureInitialized();
            return keyboard!;
        }
    }

    /// <inheritdoc />
    public IMouse Mouse
    {
        get
        {
            EnsureInitialized();
            return mouse!;
        }
    }

    /// <inheritdoc />
    public IGamepad Gamepad
    {
        get
        {
            EnsureInitialized();
            return gamepads!.Value.Length > 0
                ? gamepads!.Value[0]
                : throw new InvalidOperationException("No gamepads available.");
        }
    }

    /// <inheritdoc />
    public ImmutableArray<IKeyboard> Keyboards
    {
        get
        {
            EnsureInitialized();
            return [keyboard!];
        }
    }

    /// <inheritdoc />
    public ImmutableArray<IMouse> Mice
    {
        get
        {
            EnsureInitialized();
            return [mouse!];
        }
    }

    /// <inheritdoc />
    public ImmutableArray<IGamepad> Gamepads
    {
        get
        {
            EnsureInitialized();
            return gamepads!.Value.CastArray<IGamepad>();
        }
    }

    /// <inheritdoc />
    public int ConnectedGamepadCount
    {
        get
        {
            EnsureInitialized();
            return gamepads!.Value.Count(g => g.IsConnected);
        }
    }

    /// <inheritdoc />
    public void Update()
    {
        // Update both underlying contexts
        real.Update();
        virtual_.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Note: We don't dispose the underlying contexts here
        // as they may be owned by other systems (plugins, etc.)
        // The caller is responsible for disposing them appropriately.
    }

    #endregion

    #region Global Events

    /// <inheritdoc />
    public event Action<IKeyboard, KeyEventArgs>? OnKeyDown;

    /// <inheritdoc />
    public event Action<IKeyboard, KeyEventArgs>? OnKeyUp;

    /// <inheritdoc />
    public event Action<IKeyboard, char>? OnTextInput;

    /// <inheritdoc />
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonDown;

    /// <inheritdoc />
    public event Action<IMouse, MouseButtonEventArgs>? OnMouseButtonUp;

    /// <inheritdoc />
    public event Action<IMouse, MouseMoveEventArgs>? OnMouseMove;

    /// <inheritdoc />
    public event Action<IMouse, MouseScrollEventArgs>? OnMouseScroll;

    /// <inheritdoc />
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonDown;

    /// <inheritdoc />
    public event Action<IGamepad, GamepadButtonEventArgs>? OnGamepadButtonUp;

    /// <inheritdoc />
    public event Action<IGamepad>? OnGamepadConnected;

    /// <inheritdoc />
    public event Action<IGamepad>? OnGamepadDisconnected;

    #endregion

    #region Event Wiring

    private void WireRealEvents()
    {
        real.OnKeyDown += (kb, args) => OnKeyDown?.Invoke(keyboard!, args);
        real.OnKeyUp += (kb, args) => OnKeyUp?.Invoke(keyboard!, args);
        real.OnTextInput += (kb, c) => OnTextInput?.Invoke(keyboard!, c);

        real.OnMouseButtonDown += (m, args) => OnMouseButtonDown?.Invoke(mouse!, args);
        real.OnMouseButtonUp += (m, args) => OnMouseButtonUp?.Invoke(mouse!, args);
        real.OnMouseMove += (m, args) => OnMouseMove?.Invoke(mouse!, args);
        real.OnMouseScroll += (m, args) => OnMouseScroll?.Invoke(mouse!, args);

        real.OnGamepadButtonDown += (g, args) => OnGamepadButtonDown?.Invoke(GetCompositeGamepad(g.Index), args);
        real.OnGamepadButtonUp += (g, args) => OnGamepadButtonUp?.Invoke(GetCompositeGamepad(g.Index), args);
        real.OnGamepadConnected += g => OnGamepadConnected?.Invoke(GetCompositeGamepad(g.Index));
        real.OnGamepadDisconnected += g => OnGamepadDisconnected?.Invoke(GetCompositeGamepad(g.Index));
    }

    private void WireVirtualEvents()
    {
        // Wire virtual events with null checks for lazy keyboard/mouse
        virtual_.OnKeyDown += (kb, args) =>
        {
            if (keyboard is not null)
            {
                OnKeyDown?.Invoke(keyboard, args);
            }
            else
            {
                // Forward with virtual keyboard if composite not yet initialized
                OnKeyDown?.Invoke(kb, args);
            }
        };

        virtual_.OnKeyUp += (kb, args) =>
        {
            if (keyboard is not null)
            {
                OnKeyUp?.Invoke(keyboard, args);
            }
            else
            {
                OnKeyUp?.Invoke(kb, args);
            }
        };

        virtual_.OnTextInput += (kb, c) =>
        {
            if (keyboard is not null)
            {
                OnTextInput?.Invoke(keyboard, c);
            }
            else
            {
                OnTextInput?.Invoke(kb, c);
            }
        };

        virtual_.OnMouseButtonDown += (m, args) =>
        {
            if (mouse is not null)
            {
                OnMouseButtonDown?.Invoke(mouse, args);
            }
            else
            {
                OnMouseButtonDown?.Invoke(m, args);
            }
        };

        virtual_.OnMouseButtonUp += (m, args) =>
        {
            if (mouse is not null)
            {
                OnMouseButtonUp?.Invoke(mouse, args);
            }
            else
            {
                OnMouseButtonUp?.Invoke(m, args);
            }
        };

        virtual_.OnMouseMove += (m, args) =>
        {
            if (mouse is not null)
            {
                OnMouseMove?.Invoke(mouse, args);
            }
            else
            {
                OnMouseMove?.Invoke(m, args);
            }
        };

        virtual_.OnMouseScroll += (m, args) =>
        {
            if (mouse is not null)
            {
                OnMouseScroll?.Invoke(mouse, args);
            }
            else
            {
                OnMouseScroll?.Invoke(m, args);
            }
        };

        virtual_.OnGamepadButtonDown += (g, args) =>
        {
            var composite = gamepads.HasValue && g.Index < gamepads.Value.Length
                ? gamepads.Value[g.Index]
                : g;
            OnGamepadButtonDown?.Invoke(composite, args);
        };

        virtual_.OnGamepadButtonUp += (g, args) =>
        {
            var composite = gamepads.HasValue && g.Index < gamepads.Value.Length
                ? gamepads.Value[g.Index]
                : g;
            OnGamepadButtonUp?.Invoke(composite, args);
        };

        virtual_.OnGamepadConnected += g =>
        {
            var composite = gamepads.HasValue && g.Index < gamepads.Value.Length
                ? gamepads.Value[g.Index]
                : g;
            OnGamepadConnected?.Invoke(composite);
        };

        virtual_.OnGamepadDisconnected += g =>
        {
            var composite = gamepads.HasValue && g.Index < gamepads.Value.Length
                ? gamepads.Value[g.Index]
                : g;
            OnGamepadDisconnected?.Invoke(composite);
        };
    }

    private IGamepad GetCompositeGamepad(int index)
    {
        EnsureInitialized();
        return index >= 0 && index < gamepads!.Value.Length ? gamepads!.Value[index] : Gamepad;
    }

    #endregion
}
