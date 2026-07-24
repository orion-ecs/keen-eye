using System.Numerics;
using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Tests;

/// <summary>
/// Tests for <see cref="AudioSourceSystem"/> playback state synchronization.
/// </summary>
public sealed class AudioSourceSystemTests : IDisposable
{
    private readonly World world = new();
    private readonly FakeAudioDevice device = new();
    private readonly AudioSourceSystem system = new();

    public AudioSourceSystemTests()
    {
        world.SetExtension<IAudioContext>(new FakeAudioContext(device), owned: false);
        system.Initialize(world);
    }

    public void Dispose()
    {
        system.Dispose();
        world.Dispose();
    }

    private static AudioSource PlayingSource() => AudioSource.Default with
    {
        Clip = new AudioClipHandle(1),
        State = AudioPlayState.Playing,
    };

    [Fact]
    public void Update_FirstPlay_StartsBackendPlaybackOnce()
    {
        var entity = world.Spawn().With(PlayingSource()).Build();

        system.Update(1f / 60f);

        ref readonly var source = ref world.Get<AudioSource>(entity);
        Assert.Equal(1, device.TotalPlayCount);
        Assert.Equal(AudioPlayState.Playing, source.State);
        Assert.True(source.CurrentSound.IsValid);
    }

    [Fact]
    public void Update_WhenNonLoopingSoundFinishes_MarksStoppedAndClearsHandle()
    {
        var entity = world.Spawn().With(PlayingSource()).Build();
        system.Update(1f / 60f);

        // The backend source reaches the end of the clip.
        device.SimulateFinished();
        system.Update(1f / 60f);

        ref readonly var source = ref world.Get<AudioSource>(entity);
        Assert.Equal(AudioPlayState.Stopped, source.State);
        Assert.False(source.CurrentSound.IsValid);
        // No additional PlaySource for a finished non-looping sound.
        Assert.Equal(1, device.TotalPlayCount);
    }

    [Fact]
    public void Update_SettingStateToPlayingAfterFinish_RestartsPlayback()
    {
        var entity = world.Spawn().With(PlayingSource()).Build();

        // Frame 1: initial playback.
        system.Update(1f / 60f);
        Assert.Equal(1, device.TotalPlayCount);

        // Frame 2: the non-looping sound finishes naturally.
        device.SimulateFinished();
        system.Update(1f / 60f);
        Assert.Equal(AudioPlayState.Stopped, world.Get<AudioSource>(entity).State);

        // The user requests a replay per the AudioSource.State contract
        // ("Set this to Playing to start playback.").
        world.Get<AudioSource>(entity).State = AudioPlayState.Playing;

        // Frame 3: the stopped source must be restarted.
        system.Update(1f / 60f);

        ref readonly var source = ref world.Get<AudioSource>(entity);
        Assert.Equal(2, device.TotalPlayCount);
        Assert.Equal(AudioPlayState.Playing, source.State);
        Assert.True(source.CurrentSound.IsValid);
    }

    /// <summary>
    /// Minimal <see cref="IAudioDevice"/> test double that tracks per-source
    /// playback state and counts <see cref="PlaySource"/> invocations.
    /// </summary>
    private sealed class FakeAudioDevice : IAudioDevice
    {
        private readonly Dictionary<uint, AudioPlayState> states = [];
        private uint nextSource = 1;

        public int TotalPlayCount { get; private set; }

        public bool IsInitialized => true;

        public string DeviceName => "Fake";

        public uint CreateSource()
        {
            uint id = nextSource++;
            states[id] = AudioPlayState.Stopped;
            return id;
        }

        public void PlaySource(uint sourceId)
        {
            states[sourceId] = AudioPlayState.Playing;
            TotalPlayCount++;
        }

        public void StopSource(uint sourceId) => states[sourceId] = AudioPlayState.Stopped;

        public void PauseSource(uint sourceId) => states[sourceId] = AudioPlayState.Paused;

        public AudioPlayState GetSourceState(uint sourceId) =>
            states.TryGetValue(sourceId, out var state) ? state : AudioPlayState.Stopped;

        /// <summary>
        /// Simulates every playing source reaching the end of its clip.
        /// </summary>
        public void SimulateFinished()
        {
            foreach (var id in states.Keys.ToArray())
            {
                if (states[id] == AudioPlayState.Playing)
                {
                    states[id] = AudioPlayState.Stopped;
                }
            }
        }

        public uint CreateBuffer() => 1;

        public void DeleteBuffer(uint bufferId)
        {
        }

        public void BufferData(uint bufferId, AudioFormat format, ReadOnlySpan<byte> data, int sampleRate)
        {
        }

        public void DeleteSource(uint sourceId) => states.Remove(sourceId);

        public void SetSourceBuffer(uint sourceId, uint bufferId)
        {
        }

        public void SetSourceGain(uint sourceId, float gain)
        {
        }

        public void SetListenerGain(float gain)
        {
        }

        public void SetSourcePosition(uint sourceId, Vector3 position)
        {
        }

        public void SetSourceVelocity(uint sourceId, Vector3 velocity)
        {
        }

        public void SetSourcePitch(uint sourceId, float pitch)
        {
        }

        public void SetSourceLooping(uint sourceId, bool loop)
        {
        }

        public void SetSourceMinDistance(uint sourceId, float distance)
        {
        }

        public void SetSourceMaxDistance(uint sourceId, float distance)
        {
        }

        public void SetSourceRolloff(uint sourceId, float rolloff)
        {
        }

        public void SetListenerPosition(Vector3 position)
        {
        }

        public void SetListenerVelocity(Vector3 velocity)
        {
        }

        public void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
        }

        public void SetDistanceModel(AudioRolloffMode mode)
        {
        }

        public void SetSpeedOfSound(float speed)
        {
        }

        public void SetDopplerFactor(float factor)
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Minimal <see cref="IAudioContext"/> test double exposing only the members
    /// <see cref="AudioSourceSystem"/> depends on (the device and buffer lookup).
    /// </summary>
    private sealed class FakeAudioContext(IAudioDevice device) : IAudioContext
    {
        public IAudioDevice? Device => device;

        public bool IsInitialized => true;

        public float MasterVolume { get; set; } = 1f;

        public uint GetBufferId(AudioClipHandle handle) => handle.IsValid ? 1u : 0u;

        public AudioClipHandle LoadClip(string path) => AudioClipHandle.Invalid;

        public AudioClipHandle CreateClip(ReadOnlySpan<byte> data, AudioFormat format, int sampleRate) =>
            AudioClipHandle.Invalid;

        public AudioClipInfo? GetClipInfo(AudioClipHandle handle) => null;

        public void UnloadClip(AudioClipHandle handle)
        {
        }

        public SoundHandle Play(AudioClipHandle clip, float volume = 1f) => SoundHandle.Invalid;

        public SoundHandle Play(AudioClipHandle clip, PlaybackOptions options) => SoundHandle.Invalid;

        public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, float volume = 1f) => SoundHandle.Invalid;

        public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, PlaybackOptions options) => SoundHandle.Invalid;

        public void Stop(SoundHandle sound)
        {
        }

        public void Pause(SoundHandle sound)
        {
        }

        public void Resume(SoundHandle sound)
        {
        }

        public void SetVolume(SoundHandle sound, float volume)
        {
        }

        public void SetPitch(SoundHandle sound, float pitch)
        {
        }

        public void SetPosition(SoundHandle sound, Vector3 position)
        {
        }

        public bool IsPlaying(SoundHandle sound) => false;

        public void StopAll()
        {
        }

        public void PauseAll()
        {
        }

        public void ResumeAll()
        {
        }

        public float GetChannelVolume(AudioChannel channel) => 1f;

        public void SetChannelVolume(AudioChannel channel, float volume)
        {
        }

        public void SetListenerPosition(Vector3 position)
        {
        }

        public void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
        }

        public void SetListenerVelocity(Vector3 velocity)
        {
        }

        public void Update()
        {
        }

        public void Dispose()
        {
        }
    }
}
