using System.Numerics;
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
    private readonly Dictionary<int, SoundInstance> activeSounds = [];
    private readonly Dictionary<AudioChannel, float> channelVolumes = new()
    {
        [AudioChannel.Master] = 1f,
        [AudioChannel.Music] = 1f,
        [AudioChannel.SFX] = 1f,
        [AudioChannel.Voice] = 1f,
        [AudioChannel.Ambient] = 1f
    };
    private readonly HashSet<int> pausedByPauseAll = [];

    private int nextBufferId = 1;
    private int nextSoundId = 1;

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
            device?.SetListenerGain(masterVolume * channelVolumes[AudioChannel.Master]);
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

    #region Clip Operations

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
    public uint GetBufferId(AudioClipHandle handle)
    {
        return buffers.TryGetValue(handle.Id, out var buffer) ? buffer.BufferId : 0;
    }

    #endregion

    #region Playback

    /// <inheritdoc />
    public SoundHandle Play(AudioClipHandle clip, float volume = 1f)
    {
        return Play(clip, new PlaybackOptions
        {
            Volume = volume,
            Pitch = 1f,
            Loop = false,
            Channel = AudioChannel.SFX
        });
    }

    /// <inheritdoc />
    public SoundHandle Play(AudioClipHandle clip, PlaybackOptions options)
    {
        ThrowIfNotInitialized();

        if (!buffers.TryGetValue(clip.Id, out var buffer))
        {
            return SoundHandle.Invalid;
        }

        var source = sourcePool!.AcquireSource();
        if (source == null)
        {
            return SoundHandle.Invalid;
        }

        var effectiveVolume = ComputeEffectiveVolume(options.Volume, options.Channel);

        device!.SetSourceBuffer(source.Value, buffer.BufferId);
        device.SetSourceGain(source.Value, effectiveVolume);
        device.SetSourcePitch(source.Value, options.Pitch);
        device.SetSourceLooping(source.Value, options.Loop);
        device.PlaySource(source.Value);

        int soundId = nextSoundId++;
        activeSounds[soundId] = new SoundInstance(source.Value, clip, options.Channel, options.Volume, true);

        return new SoundHandle(soundId);
    }

    /// <inheritdoc />
    public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, float volume = 1f)
    {
        return PlayAt(clip, position, new PlaybackOptions
        {
            Volume = volume,
            Pitch = 1f,
            Loop = false,
            Channel = AudioChannel.SFX
        });
    }

    /// <inheritdoc />
    public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, PlaybackOptions options)
    {
        ThrowIfNotInitialized();

        if (!buffers.TryGetValue(clip.Id, out var buffer))
        {
            return SoundHandle.Invalid;
        }

        var source = sourcePool!.AcquireSource();
        if (source == null)
        {
            return SoundHandle.Invalid;
        }

        var effectiveVolume = ComputeEffectiveVolume(options.Volume, options.Channel);

        device!.SetSourceBuffer(source.Value, buffer.BufferId);
        device.SetSourceGain(source.Value, effectiveVolume);
        device.SetSourcePitch(source.Value, options.Pitch);
        device.SetSourceLooping(source.Value, options.Loop);
        device.SetSourcePosition(source.Value, position);
        device.PlaySource(source.Value);

        int soundId = nextSoundId++;
        activeSounds[soundId] = new SoundInstance(source.Value, clip, options.Channel, options.Volume, true);

        return new SoundHandle(soundId);
    }

    /// <inheritdoc />
    public void Stop(SoundHandle sound)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        device?.StopSource(instance.SourceId);
        activeSounds.Remove(sound.Id);
        pausedByPauseAll.Remove(sound.Id);
    }

    /// <inheritdoc />
    public void Pause(SoundHandle sound)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        device?.PauseSource(instance.SourceId);
    }

    /// <inheritdoc />
    public void Resume(SoundHandle sound)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        device?.PlaySource(instance.SourceId);
        pausedByPauseAll.Remove(sound.Id);
    }

    /// <inheritdoc />
    public void SetVolume(SoundHandle sound, float volume)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        var effectiveVolume = ComputeEffectiveVolume(volume, instance.Channel);
        device?.SetSourceGain(instance.SourceId, effectiveVolume);

        // Update stored base volume
        activeSounds[sound.Id] = instance with { BaseVolume = volume };
    }

    /// <inheritdoc />
    public void SetPitch(SoundHandle sound, float pitch)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        device?.SetSourcePitch(instance.SourceId, pitch);
    }

    /// <inheritdoc />
    public void SetPosition(SoundHandle sound, Vector3 position)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return;
        }

        device?.SetSourcePosition(instance.SourceId, position);
    }

    /// <inheritdoc />
    public bool IsPlaying(SoundHandle sound)
    {
        if (!activeSounds.TryGetValue(sound.Id, out var instance))
        {
            return false;
        }

        var state = device?.GetSourceState(instance.SourceId);
        return state == AudioPlayState.Playing;
    }

    /// <inheritdoc />
    public void StopAll()
    {
        sourcePool?.StopAll();
        activeSounds.Clear();
        pausedByPauseAll.Clear();
    }

    /// <inheritdoc />
    public void PauseAll()
    {
        foreach (var (soundId, instance) in activeSounds)
        {
            var state = device?.GetSourceState(instance.SourceId);
            if (state == AudioPlayState.Playing)
            {
                device?.PauseSource(instance.SourceId);
                pausedByPauseAll.Add(soundId);
            }
        }
    }

    /// <inheritdoc />
    public void ResumeAll()
    {
        foreach (var soundId in pausedByPauseAll)
        {
            if (activeSounds.TryGetValue(soundId, out var instance))
            {
                device?.PlaySource(instance.SourceId);
            }
        }
        pausedByPauseAll.Clear();
    }

    #endregion

    #region Channel Volume

    /// <inheritdoc />
    public float GetChannelVolume(AudioChannel channel)
    {
        return channelVolumes.TryGetValue(channel, out var volume) ? volume : 1f;
    }

    /// <inheritdoc />
    public void SetChannelVolume(AudioChannel channel, float volume)
    {
        volume = Math.Clamp(volume, 0f, 2f);
        channelVolumes[channel] = volume;

        // Update master volume through the listener if Master channel changed
        if (channel == AudioChannel.Master)
        {
            device?.SetListenerGain(masterVolume * volume);
        }

        // Update all active sounds on this channel
        foreach (var (_, instance) in activeSounds)
        {
            if (instance.Channel == channel || channel == AudioChannel.Master)
            {
                var effectiveVolume = ComputeEffectiveVolume(instance.BaseVolume, instance.Channel);
                device?.SetSourceGain(instance.SourceId, effectiveVolume);
            }
        }
    }

    private float ComputeEffectiveVolume(float baseVolume, AudioChannel channel)
    {
        var masterChannelVol = channelVolumes[AudioChannel.Master];
        var channelVol = channelVolumes.TryGetValue(channel, out var vol) ? vol : 1f;
        return baseVolume * masterChannelVol * channelVol * masterVolume;
    }

    #endregion

    #region Listener

    /// <inheritdoc />
    public void SetListenerPosition(Vector3 position)
    {
        device?.SetListenerPosition(position);
    }

    /// <inheritdoc />
    public void SetListenerOrientation(Vector3 forward, Vector3 up)
    {
        device?.SetListenerOrientation(forward, up);
    }

    /// <inheritdoc />
    public void SetListenerVelocity(Vector3 velocity)
    {
        device?.SetListenerVelocity(velocity);
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    public void Update()
    {
        sourcePool?.Update();

        // Clean up finished sounds from tracking
        List<int>? toRemove = null;
        foreach (var (soundId, instance) in activeSounds)
        {
            var state = device?.GetSourceState(instance.SourceId);
            if (state == AudioPlayState.Stopped)
            {
                toRemove ??= [];
                toRemove.Add(soundId);
            }
        }

        if (toRemove != null)
        {
            foreach (var soundId in toRemove)
            {
                activeSounds.Remove(soundId);
                pausedByPauseAll.Remove(soundId);
            }
        }
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
        activeSounds.Clear();
        pausedByPauseAll.Clear();

        device?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        windowProvider.Window.Load -= HandleWindowLoad;
        windowProvider.Window.Closing -= HandleWindowClosing;
        DisposeAudioResources();
    }

    #endregion

    /// <summary>
    /// Internal record for tracking active sound instances.
    /// </summary>
    private sealed record SoundInstance(
        uint SourceId,
        AudioClipHandle Clip,
        AudioChannel Channel,
        float BaseVolume,
        bool IsPooled);
}
