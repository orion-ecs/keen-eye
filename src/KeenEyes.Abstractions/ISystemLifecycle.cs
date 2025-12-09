namespace KeenEyes;

/// <summary>
/// Optional interface for systems that need before/after update lifecycle hooks.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on systems that need to perform setup before processing
/// or cleanup after processing. The <see cref="SystemGroup"/> will automatically
/// call these methods around <see cref="ISystem.Update"/> for systems that implement
/// this interface.
/// </para>
/// <para>
/// The <c>SystemBase</c> class in KeenEyes.Core implements this interface and provides
/// virtual methods that can be overridden for custom lifecycle behavior.
/// </para>
/// </remarks>
public interface ISystemLifecycle
{
    /// <summary>
    /// Called before <see cref="ISystem.Update"/> each frame.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    void OnBeforeUpdate(float deltaTime);

    /// <summary>
    /// Called after <see cref="ISystem.Update"/> each frame.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    void OnAfterUpdate(float deltaTime);
}
