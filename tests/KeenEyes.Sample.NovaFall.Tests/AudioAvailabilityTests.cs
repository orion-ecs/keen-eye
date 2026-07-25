using System.Numerics;
using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Sample.NovaFall.Tests;

/// <summary>
/// Pins down that the game plays without sound: no audio plugin at all, and an audio backend
/// that is installed but could not open a device.
/// </summary>
/// <remarks>
/// Regression coverage for #1256, where a machine with no native OpenAL runtime could not run
/// the sample at all. Audio is optional hardware, so <see cref="NovaFallAudioSystem"/> must
/// no-op in both cases rather than throwing into the frame loop.
/// </remarks>
public class AudioAvailabilityTests
{
    [Fact]
    public void Update_WithNoAudioExtension_DoesNothing()
    {
        using var world = CreatePresentationWorld();
        var system = new NovaFallAudioSystem();
        system.Initialize(world);

        var exception = Record.Exception(() => system.Update(1f / 60f));

        Assert.Null(exception);
    }

    [Fact]
    public void Update_WithUninitializedAudioContext_DoesNotTouchTheBackend()
    {
        using var world = CreatePresentationWorld();
        var audio = new UnavailableAudioContext();
        world.SetExtension<IAudioContext>(audio, owned: false);

        var system = new NovaFallAudioSystem();
        system.Initialize(world);

        // Several frames, including a frame of play: the system must never reach the backend.
        var exception = Record.Exception(() =>
        {
            system.Update(1f / 60f);
            world.GetSingleton<GameState>().Phase = GamePhase.Playing;
            system.Update(1f / 60f);
            system.Update(1f / 60f);
        });

        Assert.Null(exception);
        Assert.Equal(0, audio.CallCount);
    }

    private static World CreatePresentationWorld()
    {
        var world = new World();

        // presentation: true is the windowed path - the juice systems believe they may render
        // and play sound, which is exactly when a missing audio device has to be survivable.
        GameSetup.InitializeSingletons(world, seed: 1234, pinSeed: true, presentation: true);
        GameSetup.StartRun(world, seed: 1234);

        return world;
    }

    /// <summary>
    /// An <see cref="IAudioContext"/> whose device never opened, matching what the Silk backend
    /// reports on a machine with no OpenAL runtime: uninitialized, with a typed reason, and
    /// throwing on any playback call.
    /// </summary>
    private sealed class UnavailableAudioContext : IAudioContext
    {
        public int CallCount { get; private set; }

        public IAudioDevice? Device => null;

        public bool IsInitialized => false;

        public AudioException? InitializationError { get; } =
            new AudioInitializationException("The native OpenAL runtime could not be loaded.");

        public float MasterVolume
        {
            get => Fail<float>();
            set => Fail<float>();
        }

        public AudioClipHandle LoadClip(string path) => Fail<AudioClipHandle>();

        public AudioClipHandle CreateClip(ReadOnlySpan<byte> data, AudioFormat format, int sampleRate) =>
            Fail<AudioClipHandle>();

        public AudioClipInfo? GetClipInfo(AudioClipHandle handle) => Fail<AudioClipInfo?>();

        public void UnloadClip(AudioClipHandle handle) => Fail<bool>();

        public uint GetBufferId(AudioClipHandle handle) => Fail<uint>();

        public SoundHandle Play(AudioClipHandle clip, float volume = 1f) => Fail<SoundHandle>();

        public SoundHandle Play(AudioClipHandle clip, PlaybackOptions options) => Fail<SoundHandle>();

        public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, float volume = 1f) =>
            Fail<SoundHandle>();

        public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, PlaybackOptions options) =>
            Fail<SoundHandle>();

        public void Stop(SoundHandle sound) => Fail<bool>();

        public void Pause(SoundHandle sound) => Fail<bool>();

        public void Resume(SoundHandle sound) => Fail<bool>();

        public void SetVolume(SoundHandle sound, float volume) => Fail<bool>();

        public void SetPitch(SoundHandle sound, float pitch) => Fail<bool>();

        public void SetPosition(SoundHandle sound, Vector3 position) => Fail<bool>();

        public bool IsPlaying(SoundHandle sound) => Fail<bool>();

        public void StopAll() => Fail<bool>();

        public void PauseAll() => Fail<bool>();

        public void ResumeAll() => Fail<bool>();

        public float GetChannelVolume(AudioChannel channel) => Fail<float>();

        public void SetChannelVolume(AudioChannel channel, float volume) => Fail<bool>();

        public void SetListenerPosition(Vector3 position) => Fail<bool>();

        public void SetListenerOrientation(Vector3 forward, Vector3 up) => Fail<bool>();

        public void SetListenerVelocity(Vector3 velocity) => Fail<bool>();

        public void Update() => Fail<bool>();

        public void Dispose()
        {
        }

        private T Fail<T>()
        {
            CallCount++;
            throw new InvalidOperationException(
                "Audio is unavailable: the caller must check IAudioContext.IsInitialized first.");
        }
    }
}
