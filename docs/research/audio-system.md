# Audio System Architecture

This document outlines the architecture for the KeenEyes audio system, providing sound effect playback, music streaming, and 3D positional audio.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Library Selection](#library-selection)
3. [Architecture Overview](#architecture-overview)
4. [Core Components](#core-components)
5. [Audio Context API](#audio-context-api)
6. [3D Positional Audio](#3d-positional-audio)
7. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes Audio will use **OpenAL via Silk.NET** as the audio backend. This provides:
- Cross-platform support (Windows, macOS, Linux)
- 3D positional audio out of the box
- Streaming support for music
- Native AOT compatibility
- No additional licensing concerns

**Key Decision:** Use OpenAL Soft (open-source OpenAL implementation) through Silk.NET.OpenAL bindings.

---

## Library Selection

### Evaluation Summary

| Library | License | Platforms | 3D Audio | Streaming | AOT | Decision |
|---------|---------|-----------|----------|-----------|-----|----------|
| **OpenAL (Silk.NET)** | LGPL | All | Yes | Yes | Yes | **Chosen** |
| FMOD | Commercial | All | Yes | Yes | Yes | License cost |
| Wwise | Commercial | All | Yes | Yes | Yes | Complex licensing |
| NAudio | MIT | Windows | No | Yes | Yes | Windows only |
| miniaudio | MIT | All | Yes | Yes | Partial | Low-level |

### Why OpenAL?

1. **Silk.NET Integration** - Already using Silk.NET for graphics/input
2. **Battle-tested** - Used in countless games
3. **3D Audio** - Built-in spatialization
4. **Open Source** - OpenAL Soft is actively maintained
5. **Simple API** - Easy to wrap and expose

---

## Architecture Overview

### Project Structure

```
KeenEyes.Audio/
├── KeenEyes.Audio.csproj
├── AudioPlugin.cs                 # IWorldPlugin entry point
│
├── Core/
│   ├── IAudioContext.cs          # Main audio API
│   ├── IAudioSource.cs           # Playing sound instance
│   ├── IAudioClip.cs             # Loaded audio data
│   ├── IAudioListener.cs         # 3D listener position
│   └── AudioConfig.cs            # Configuration options
│
├── Components/
│   ├── AudioSource.cs            # Entity sound emitter
│   ├── AudioListener.cs          # Entity listener (usually camera/player)
│   └── AudioTrigger.cs           # Event-based sound triggers
│
├── Systems/
│   ├── AudioUpdateSystem.cs      # Updates 3D positions
│   ├── AudioTriggerSystem.cs     # Handles trigger events
│   └── MusicSystem.cs            # Background music management
│
├── Backend/
│   ├── OpenALBackend.cs          # OpenAL implementation
│   ├── OpenALSource.cs           # OpenAL source wrapper
│   ├── OpenALClip.cs             # OpenAL buffer wrapper
│   └── AudioDecoder.cs           # WAV, OGG, MP3 decoding
│
└── Utilities/
    ├── AudioPool.cs              # Source pooling for effects
    └── AudioMixer.cs             # Volume groups and mixing
```

### Dependency Graph

```
KeenEyes.Abstractions
         ↑
    KeenEyes.Audio
         ↑
    Silk.NET.OpenAL
```

---

## Core Components

### AudioSource - Entity Sound Emitter

```csharp
[Component]
public partial struct AudioSource
{
    // Audio clip reference
    public IAudioClip? Clip;

    // Playback settings
    public float Volume;           // 0.0 - 1.0
    public float Pitch;            // 0.5 - 2.0 typical
    public bool Loop;
    public bool PlayOnAwake;       // Auto-play when spawned

    // 3D settings
    public bool Spatial;           // Enable 3D positioning
    public float MinDistance;      // Full volume distance
    public float MaxDistance;      // Inaudible distance
    public AudioRolloff Rolloff;   // Distance attenuation curve

    // Mixer routing
    public string MixerGroup;      // "SFX", "Music", "Voice", etc.

    // Runtime state (managed by system)
    public AudioPlayState State;
    public float PlaybackPosition;
    internal nint SourceHandle;    // Backend handle
}

public enum AudioPlayState
{
    Stopped,
    Playing,
    Paused
}

public enum AudioRolloff
{
    Linear,
    Logarithmic,
    Custom
}
```

### AudioListener - 3D Listener

```csharp
[Component]
public partial struct AudioListener
{
    public bool Active;            // Only one listener active at a time
    public float GlobalVolume;     // Master volume multiplier

    // Orientation (derived from entity transform if not set)
    public Vector3? ForwardOverride;
    public Vector3? UpOverride;
}
```

### AudioTrigger - Event-Based Sounds

```csharp
[Component]
public partial struct AudioTrigger
{
    public IAudioClip? Clip;
    public AudioTriggerEvent TriggerOn;
    public float Volume;
    public float PitchVariation;   // Random pitch range
    public float Cooldown;         // Minimum time between triggers

    // Runtime
    public float LastTriggerTime;
}

[Flags]
public enum AudioTriggerEvent
{
    None = 0,
    OnSpawn = 1 << 0,
    OnDespawn = 1 << 1,
    OnCollision = 1 << 2,
    OnDamage = 1 << 3,
    OnDeath = 1 << 4,
    Custom = 1 << 5
}
```

---

## Audio Context API

### IAudioContext - Main API

```csharp
public interface IAudioContext
{
    // Clip loading
    IAudioClip LoadClip(string path);
    IAudioClip LoadClip(ReadOnlySpan<byte> data, AudioFormat format);
    Task<IAudioClip> LoadClipAsync(string path);

    // One-shot playback (fire and forget)
    void PlayOneShot(IAudioClip clip, float volume = 1f);
    void PlayOneShot(IAudioClip clip, Vector3 position, float volume = 1f);

    // Music (streaming)
    void PlayMusic(IAudioClip clip, bool loop = true, float fadeIn = 0f);
    void StopMusic(float fadeOut = 0f);
    void PauseMusic();
    void ResumeMusic();
    void SetMusicVolume(float volume);

    // Mixer control
    void SetGroupVolume(string group, float volume);
    float GetGroupVolume(string group);
    void MuteGroup(string group, bool muted);

    // Global settings
    float MasterVolume { get; set; }
    bool Muted { get; set; }

    // Listener (for non-ECS usage)
    void SetListenerPosition(Vector3 position);
    void SetListenerOrientation(Vector3 forward, Vector3 up);
}
```

### IAudioClip - Loaded Audio

```csharp
public interface IAudioClip : IDisposable
{
    string Name { get; }
    float Duration { get; }        // In seconds
    int SampleRate { get; }
    int Channels { get; }          // 1 = mono, 2 = stereo
    bool IsStreaming { get; }      // Large files stream from disk

    nint Handle { get; }           // Backend-specific handle
}
```

### World Extension

```csharp
public static class WorldAudioExtensions
{
    extension(IWorld world)
    {
        public IAudioContext Audio => world.GetExtension<IAudioContext>();
    }
}

// Usage
world.Audio.PlayOneShot(explosionClip, entity.Position);
```

---

## 3D Positional Audio

### How It Works

1. **AudioListener** component marks the "ear" position (usually player/camera)
2. **AudioSource** components with `Spatial = true` emit from entity positions
3. **AudioUpdateSystem** syncs entity transforms to OpenAL sources each frame
4. OpenAL handles spatialization (panning, distance attenuation, doppler)

### AudioUpdateSystem

```csharp
public class AudioUpdateSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var audio = World.GetExtension<OpenALBackend>();

        // Update listener position
        foreach (var entity in World.Query<AudioListener, Transform3D>())
        {
            ref readonly var listener = ref World.Get<AudioListener>(entity);
            if (!listener.Active) continue;

            ref readonly var transform = ref World.Get<Transform3D>(entity);

            audio.SetListenerPosition(transform.Position);
            audio.SetListenerOrientation(
                listener.ForwardOverride ?? transform.Forward,
                listener.UpOverride ?? transform.Up
            );

            break; // Only one active listener
        }

        // Update source positions
        foreach (var entity in World.Query<AudioSource, Transform3D>())
        {
            ref var source = ref World.Get<AudioSource>(entity);
            if (!source.Spatial || source.State != AudioPlayState.Playing)
                continue;

            ref readonly var transform = ref World.Get<Transform3D>(entity);
            audio.SetSourcePosition(source.SourceHandle, transform.Position);
        }
    }
}
```

### Distance Attenuation

```csharp
// OpenAL distance models
public enum AudioRolloff
{
    // Volume = 1 - (distance - minDist) / (maxDist - minDist)
    Linear,

    // Volume = minDist / (minDist + rolloff * (distance - minDist))
    Logarithmic,

    // User-defined curve
    Custom
}
```

---

## Audio Pooling

### Why Pool?

Creating/destroying OpenAL sources is expensive. Pool them for one-shot effects.

```csharp
public class AudioPool
{
    private readonly Stack<nint> availableSources = new();
    private readonly Dictionary<nint, float> activeSources = new(); // source -> end time
    private readonly OpenALBackend backend;
    private readonly int maxSources;

    public AudioPool(OpenALBackend backend, int maxSources = 32)
    {
        this.backend = backend;
        this.maxSources = maxSources;

        // Pre-allocate sources
        for (int i = 0; i < maxSources; i++)
        {
            availableSources.Push(backend.CreateSource());
        }
    }

    public void PlayOneShot(IAudioClip clip, Vector3 position, float volume)
    {
        if (availableSources.Count == 0)
        {
            // Steal oldest active source
            RecycleOldestSource();
        }

        var source = availableSources.Pop();

        backend.SetSourceClip(source, clip);
        backend.SetSourcePosition(source, position);
        backend.SetSourceVolume(source, volume);
        backend.PlaySource(source);

        activeSources[source] = Time.Current + clip.Duration;
    }

    public void Update()
    {
        var now = Time.Current;
        var toRecycle = new List<nint>();

        foreach (var (source, endTime) in activeSources)
        {
            if (now >= endTime || !backend.IsPlaying(source))
            {
                toRecycle.Add(source);
            }
        }

        foreach (var source in toRecycle)
        {
            backend.StopSource(source);
            activeSources.Remove(source);
            availableSources.Push(source);
        }
    }
}
```

---

## Music System

### Streaming Large Files

Music files are too large to load entirely. Stream from disk.

```csharp
public class MusicSystem : SystemBase
{
    private IAudioClip? currentMusic;
    private nint musicSource;
    private float targetVolume = 1f;
    private float currentVolume = 1f;
    private float fadeSpeed = 0f;

    public void PlayMusic(IAudioClip clip, bool loop, float fadeIn)
    {
        var audio = World.GetExtension<OpenALBackend>();

        if (currentMusic != null)
        {
            // Crossfade
            fadeSpeed = -1f / fadeIn;
        }

        currentMusic = clip;
        audio.SetSourceClip(musicSource, clip);
        audio.SetSourceLoop(musicSource, loop);
        audio.PlaySource(musicSource);

        if (fadeIn > 0)
        {
            currentVolume = 0;
            fadeSpeed = 1f / fadeIn;
        }
    }

    public override void Update(float deltaTime)
    {
        if (fadeSpeed != 0)
        {
            currentVolume += fadeSpeed * deltaTime;
            currentVolume = Math.Clamp(currentVolume, 0, targetVolume);

            if (currentVolume <= 0 || currentVolume >= targetVolume)
            {
                fadeSpeed = 0;
            }

            var audio = World.GetExtension<OpenALBackend>();
            audio.SetSourceVolume(musicSource, currentVolume);
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `KeenEyes.Audio` project
2. Implement OpenAL backend wrapper
3. Implement IAudioContext interface
4. Basic WAV loading
5. One-shot playback

**Milestone:** Play sound effects

### Phase 2: ECS Integration

1. Implement AudioSource component
2. Implement AudioListener component
3. Create AudioUpdateSystem
4. 3D positional audio working

**Milestone:** Spatial audio with entities

### Phase 3: Music & Streaming

1. Implement audio streaming
2. OGG/Vorbis decoder integration
3. Music playback with fading
4. MusicSystem implementation

**Milestone:** Background music support

### Phase 4: Advanced Features

1. Audio pooling for one-shots
2. Mixer groups and volume control
3. AudioTrigger system
4. Pitch variation and effects

**Milestone:** Production-ready audio

### Phase 5: Polish

1. MP3 support (optional)
2. Audio compression options
3. Performance profiling
4. Documentation

---

## File Format Support

| Format | Support | Notes |
|--------|---------|-------|
| WAV | Full | Uncompressed, simple |
| OGG | Full | Recommended for music |
| MP3 | Optional | Patent concerns, larger decoder |
| FLAC | Optional | Lossless, large files |

### Decoder Strategy

```csharp
public interface IAudioDecoder
{
    bool CanDecode(string extension);
    AudioData Decode(Stream stream);
    IAudioStreamReader CreateStreamReader(Stream stream); // For streaming
}

public class AudioDecoderRegistry
{
    private readonly List<IAudioDecoder> decoders = new();

    public AudioDecoderRegistry()
    {
        Register(new WavDecoder());
        Register(new OggDecoder());
    }

    public void Register(IAudioDecoder decoder) => decoders.Add(decoder);

    public IAudioDecoder GetDecoder(string path)
    {
        var ext = Path.GetExtension(path);
        return decoders.FirstOrDefault(d => d.CanDecode(ext))
            ?? throw new NotSupportedException($"No decoder for {ext}");
    }
}
```

---

## Open Questions

1. **Reverb/Effects** - OpenAL EFX extension support?
2. **Audio Occlusion** - Raycast-based obstruction?
3. **Voice Chat** - Scope for networking integration?
4. **Recording** - Microphone input support?
5. **Compression** - Runtime audio compression for memory?

---

## Related Issues

- Milestone #16: Audio System
- Issue #419: Create KeenEyes.Audio project with OpenAL backend
- Issue #420: Implement audio components and systems
- Issue #421: Add music streaming and mixer support
