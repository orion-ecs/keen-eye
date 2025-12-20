using KeenEyes.Audio.Abstractions;
using Silk.NET.OpenAL;
using AudioExceptionType = KeenEyes.Audio.Abstractions.AudioException;

namespace KeenEyes.Audio.Silk.Backend;

/// <summary>
/// OpenAL implementation of <see cref="IAudioDevice"/>.
/// </summary>
internal sealed unsafe class OpenALDevice : IAudioDevice
{
    private readonly AL al;
    private readonly ALContext alc;
    private readonly Device* device;
    private readonly Context* context;
    private bool disposed;

    /// <inheritdoc />
    public bool IsInitialized => device != null && context != null;

    /// <inheritdoc />
    public string DeviceName { get; }

    internal OpenALDevice(string? deviceName = null)
    {
        alc = ALContext.GetApi(soft: true);
        al = AL.GetApi(soft: true);

        device = alc.OpenDevice(deviceName);
        if (device == null)
        {
            throw new AudioInitializationException("Failed to open OpenAL device");
        }

        context = alc.CreateContext(device, null);
        if (context == null)
        {
            alc.CloseDevice(device);
            throw new AudioInitializationException("Failed to create OpenAL context");
        }

        alc.MakeContextCurrent(context);

        DeviceName = deviceName ?? alc.GetContextProperty(device, GetContextString.DeviceSpecifier) ?? "Unknown";
    }

    /// <inheritdoc />
    public uint CreateBuffer()
    {
        uint buffer = al.GenBuffer();
        CheckError("GenBuffer");
        return buffer;
    }

    /// <inheritdoc />
    public void DeleteBuffer(uint bufferId)
    {
        al.DeleteBuffer(bufferId);
    }

    /// <inheritdoc />
    public void BufferData(uint bufferId, AudioFormat format, ReadOnlySpan<byte> data, int sampleRate)
    {
        var alFormat = ToOpenALFormat(format);
        fixed (byte* ptr = data)
        {
            al.BufferData(bufferId, alFormat, ptr, data.Length, sampleRate);
        }
        CheckError("BufferData");
    }

    /// <inheritdoc />
    public uint CreateSource()
    {
        uint source = al.GenSource();
        CheckError("GenSource");
        return source;
    }

    /// <inheritdoc />
    public void DeleteSource(uint sourceId)
    {
        al.DeleteSource(sourceId);
    }

    /// <inheritdoc />
    public void SetSourceBuffer(uint sourceId, uint bufferId)
    {
        al.SetSourceProperty(sourceId, SourceInteger.Buffer, (int)bufferId);
    }

    /// <inheritdoc />
    public void SetSourceGain(uint sourceId, float gain)
    {
        al.SetSourceProperty(sourceId, SourceFloat.Gain, gain);
    }

    /// <inheritdoc />
    public void PlaySource(uint sourceId)
    {
        al.SourcePlay(sourceId);
    }

    /// <inheritdoc />
    public void StopSource(uint sourceId)
    {
        al.SourceStop(sourceId);
    }

    /// <inheritdoc />
    public AudioPlayState GetSourceState(uint sourceId)
    {
        al.GetSourceProperty(sourceId, GetSourceInteger.SourceState, out int state);
        return state switch
        {
            (int)SourceState.Playing => AudioPlayState.Playing,
            (int)SourceState.Paused => AudioPlayState.Paused,
            _ => AudioPlayState.Stopped
        };
    }

    /// <inheritdoc />
    public void SetListenerGain(float gain)
    {
        al.SetListenerProperty(ListenerFloat.Gain, gain);
    }

    private static BufferFormat ToOpenALFormat(AudioFormat format) => format switch
    {
        AudioFormat.Mono8 => BufferFormat.Mono8,
        AudioFormat.Mono16 => BufferFormat.Mono16,
        AudioFormat.Stereo8 => BufferFormat.Stereo8,
        AudioFormat.Stereo16 => BufferFormat.Stereo16,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    private void CheckError(string operation)
    {
        var error = al.GetError();
        if (error != AudioError.NoError)
        {
            throw new AudioExceptionType($"OpenAL error during {operation}: {error}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        alc.MakeContextCurrent(null);
        alc.DestroyContext(context);
        alc.CloseDevice(device);

        al.Dispose();
        alc.Dispose();
    }
}
