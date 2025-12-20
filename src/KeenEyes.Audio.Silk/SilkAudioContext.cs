using KeenEyes.Audio.Abstractions;
using KeenEyes.Audio.Silk.Backend;
using KeenEyes.Audio.Silk.Decoders;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Audio.Silk;

/// <summary>
/// Silk.NET OpenAL implementation of <see cref="IAudioContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This context wraps OpenAL via Silk.NET and provides audio clip loading
/// and one-shot playback functionality.
/// </para>
/// <para>
/// The audio context uses a shared <see cref="ISilkWindowProvider"/> to coordinate
/// lifecycle with the window, initializing OpenAL when the window loads and
/// cleaning up when it closes.
/// </para>
/// </remarks>
[PluginExtension("SilkAudio")]
public sealed class SilkAudioContext : IAudioContext
{
    private readonly SilkAudioConfig config;
    private readonly ISilkWindowProvider windowProvider;
    private readonly Dictionary<int, AudioBuffer> buffers = [];
    private int nextBufferId = 1;

    private OpenALDevice? device;
    private SourcePool? sourcePool;
    private float masterVolume = 1f;
    private bool initialized;
    private bool disposed;

    /// <inheritdoc />
    public IAudioDevice? Device => device;

    /// <inheritdoc />
    public bool IsInitialized => initialized;

    /// <inheritdoc />
    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Math.Clamp(value, 0f, 2f);
            device?.SetListenerGain(masterVolume);
        }
    }

    internal SilkAudioContext(ISilkWindowProvider windowProvider, SilkAudioConfig? config = null)
    {
        this.windowProvider = windowProvider;
        this.config = config ?? new SilkAudioConfig();

        // Hook into window lifecycle
        windowProvider.Window.Load += HandleWindowLoad;
        windowProvider.Window.Closing += HandleWindowClosing;
    }

    private void HandleWindowLoad()
    {
        device = new OpenALDevice(config.DeviceName);
        sourcePool = new SourcePool(device, config.MaxOneShotSources);
        device.SetListenerGain(config.InitialMasterVolume);
        masterVolume = config.InitialMasterVolume;
        initialized = true;
    }

    private void HandleWindowClosing()
    {
        DisposeAudioResources();
    }

    /// <inheritdoc />
    public AudioClipHandle LoadClip(string path)
    {
        ThrowIfNotInitialized();

        try
        {
            var fileData = File.ReadAllBytes(path);
            var wavData = WavDecoder.Decode(fileData);

            return CreateClipInternal(wavData.Data, wavData.Format, wavData.SampleRate,
                wavData.Channels, wavData.BitsPerSample, wavData.Duration);
        }
        catch (IOException ex)
        {
            throw new AudioLoadException($"Failed to load audio file: {path}", ex);
        }
    }

    /// <inheritdoc />
    public AudioClipHandle CreateClip(ReadOnlySpan<byte> data, AudioFormat format, int sampleRate)
    {
        ThrowIfNotInitialized();

        int channels = format is AudioFormat.Stereo8 or AudioFormat.Stereo16 ? 2 : 1;
        int bitsPerSample = format is AudioFormat.Mono16 or AudioFormat.Stereo16 ? 16 : 8;
        float duration = (float)data.Length / (sampleRate * channels * (bitsPerSample / 8));

        return CreateClipInternal(data.ToArray(), format, sampleRate, channels, bitsPerSample, duration);
    }

    private AudioClipHandle CreateClipInternal(byte[] data, AudioFormat format, int sampleRate,
        int channels, int bitsPerSample, float duration)
    {
        uint bufferId = device!.CreateBuffer();
        device.BufferData(bufferId, format, data, sampleRate);

        var buffer = new AudioBuffer
        {
            BufferId = bufferId,
            Format = format,
            SampleRate = sampleRate,
            Channels = channels,
            BitsPerSample = bitsPerSample,
            Duration = duration
        };

        int id = nextBufferId++;
        buffers[id] = buffer;
        return new AudioClipHandle(id);
    }

    /// <inheritdoc />
    public AudioClipInfo? GetClipInfo(AudioClipHandle handle)
    {
        if (!buffers.TryGetValue(handle.Id, out var buffer))
        {
            return null;
        }

        return new AudioClipInfo(
            handle,
            buffer.Format,
            buffer.SampleRate,
            buffer.Channels,
            buffer.BitsPerSample,
            buffer.Duration);
    }

    /// <inheritdoc />
    public void UnloadClip(AudioClipHandle handle)
    {
        if (buffers.Remove(handle.Id, out var buffer))
        {
            device?.DeleteBuffer(buffer.BufferId);
        }
    }

    /// <inheritdoc />
    public void Play(AudioClipHandle handle, float volume = 1f)
    {
        ThrowIfNotInitialized();

        if (!buffers.TryGetValue(handle.Id, out var buffer))
        {
            return; // Invalid handle, silently ignore
        }

        var source = sourcePool!.AcquireSource();
        if (source == null)
        {
            return; // Pool exhausted, silently ignore
        }

        device!.SetSourceBuffer(source.Value, buffer.BufferId);
        device.SetSourceGain(source.Value, volume);
        device.PlaySource(source.Value);
    }

    /// <inheritdoc />
    public void StopAll()
    {
        sourcePool?.StopAll();
    }

    /// <inheritdoc />
    public void Update()
    {
        sourcePool?.Update();
    }

    private void ThrowIfNotInitialized()
    {
        if (!initialized)
        {
            throw new InvalidOperationException(
                "Audio not initialized. Wait for window to load.");
        }
    }

    private void DisposeAudioResources()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        sourcePool?.Dispose();

        foreach (var buffer in buffers.Values)
        {
            device?.DeleteBuffer(buffer.BufferId);
        }
        buffers.Clear();

        device?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        windowProvider.Window.Load -= HandleWindowLoad;
        windowProvider.Window.Closing -= HandleWindowClosing;
        DisposeAudioResources();
    }
}
