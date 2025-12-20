using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Silk.Backend;

/// <summary>
/// Pool of OpenAL sources for efficient one-shot playback.
/// </summary>
/// <remarks>
/// <para>
/// Creating and destroying OpenAL sources is expensive. This pool pre-allocates
/// sources and recycles them as sounds finish playing, improving performance
/// for games with many simultaneous sound effects.
/// </para>
/// </remarks>
internal sealed class SourcePool : IDisposable
{
    private readonly OpenALDevice device;
    private readonly List<uint> availableSources = [];
    private readonly List<uint> playingSources = [];
    private bool disposed;

    internal SourcePool(OpenALDevice device, int maxSources)
    {
        this.device = device;

        // Pre-allocate sources
        for (int i = 0; i < maxSources; i++)
        {
            availableSources.Add(device.CreateSource());
        }
    }

    /// <summary>
    /// Gets a source for one-shot playback, or null if pool is exhausted.
    /// </summary>
    /// <returns>A source ID, or null if no sources are available.</returns>
    public uint? AcquireSource()
    {
        if (availableSources.Count == 0)
        {
            return null;
        }

        int lastIndex = availableSources.Count - 1;
        uint source = availableSources[lastIndex];
        availableSources.RemoveAt(lastIndex);
        playingSources.Add(source);
        return source;
    }

    /// <summary>
    /// Updates the pool, recycling sources that have finished playing.
    /// </summary>
    public void Update()
    {
        for (int i = playingSources.Count - 1; i >= 0; i--)
        {
            uint source = playingSources[i];
            if (device.GetSourceState(source) != AudioPlayState.Playing)
            {
                // Reset source state
                device.StopSource(source);
                device.SetSourceBuffer(source, 0);

                playingSources.RemoveAt(i);
                availableSources.Add(source);
            }
        }
    }

    /// <summary>
    /// Stops all playing sources and returns them to the pool.
    /// </summary>
    public void StopAll()
    {
        foreach (var source in playingSources)
        {
            device.StopSource(source);
            device.SetSourceBuffer(source, 0);
            availableSources.Add(source);
        }
        playingSources.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var source in availableSources)
        {
            device.DeleteSource(source);
        }
        foreach (var source in playingSources)
        {
            device.DeleteSource(source);
        }
    }
}
