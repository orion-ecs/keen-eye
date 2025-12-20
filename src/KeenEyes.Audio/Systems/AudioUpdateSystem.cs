using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio;

/// <summary>
/// System that updates the audio context each frame.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the Update phase and calls <see cref="IAudioContext.Update"/>
/// to maintain the audio backend. The primary purpose is to recycle audio sources
/// that have finished playing back to the source pool.
/// </para>
/// <para>
/// Without this system, the source pool would eventually be exhausted as finished
/// sources would not be returned to the pool for reuse.
/// </para>
/// <para>
/// This system is backend-agnostic and works with any <see cref="IAudioContext"/>
/// implementation registered as a world extension.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typically added by the audio plugin, but can be added manually:
/// world.AddSystem&lt;AudioUpdateSystem&gt;(SystemPhase.Update);
/// </code>
/// </example>
public sealed class AudioUpdateSystem : ISystem
{
    private IWorld? world;
    private IAudioContext? audioContext;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Lazy initialization - get audio context on first update
        audioContext ??= world?.TryGetExtension<IAudioContext>(out var ctx) == true ? ctx : null;

        // Call update to recycle finished sources
        audioContext?.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
