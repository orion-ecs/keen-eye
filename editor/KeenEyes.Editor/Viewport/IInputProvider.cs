using System.Numerics;

using KeenEyes.Input.Abstractions;

namespace KeenEyes.Editor.Viewport;

/// <summary>
/// Provides polling-based input state for the editor viewport.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts input access for viewport camera control and gizmo interaction.
/// It provides per-frame deltas for mouse movement and scroll wheel, as well as
/// button and key state queries.
/// </para>
/// <para>
/// Call <see cref="Update"/> at the start of each frame to refresh delta values.
/// </para>
/// </remarks>
public interface IInputProvider
{
    /// <summary>
    /// Gets the current mouse position in window coordinates.
    /// </summary>
    Vector2 MousePosition { get; }

    /// <summary>
    /// Gets the mouse movement delta since the last frame.
    /// </summary>
    Vector2 MouseDelta { get; }

    /// <summary>
    /// Gets the scroll wheel delta since the last frame.
    /// </summary>
    Vector2 MouseScrollDelta { get; }

    /// <summary>
    /// Checks if a mouse button is currently pressed.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns>True if the button is pressed.</returns>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Checks if a key is currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is pressed.</returns>
    bool IsKeyDown(Key key);

    /// <summary>
    /// Updates the input state. Call at the start of each frame.
    /// </summary>
    void Update();
}
