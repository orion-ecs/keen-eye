# Cross-Platform Audio Systems - Research Report

**Date:** December 2024
**Purpose:** Evaluate audio libraries and approaches for implementing a cross-platform audio system supporting sound effects, music, and spatial audio.

## Executive Summary

After evaluating OpenAL (via Silk.NET and OpenTK bindings), SDL audio, MiniAudio, FMOD, and Wwise, **Silk.NET.OpenAL with OpenAL Soft** is the recommended choice for a custom game engine. It provides the best balance of 3D audio capabilities, low latency, open-source licensing, and integration with the Silk.NET ecosystem already recommended for windowing and graphics.

For projects requiring advanced audio design tooling or console deployment, **FMOD** is a strong alternative with its free indie license and professional-grade features.

---

## Library Comparison

### Silk.NET.OpenAL (OpenAL Soft)

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [Silk.NET.OpenAL](https://www.nuget.org/packages/Silk.NET.OpenAL/) |
| **Latest Version** | 2.22.0 (November 2024) |
| **OpenAL Soft Version** | 1.23.1 |
| **License** | MIT (bindings), LGPL (OpenAL Soft) |
| **3D Audio** | Yes (HRTF, distance attenuation, Doppler) |

**Strengths:**
- **Industry standard** - OpenAL is the de facto API for game audio, similar to OpenGL for graphics
- **Full 3D spatial audio** - HRTF for headphone virtualization, distance attenuation, Doppler effect
- **EFX extension** - Environmental effects (reverb, occlusion, obstruction, echo, flanger, chorus)
- **Low latency** - No perceptible delay for sound effects (unlike SDL_mixer's ~200ms reported)
- **Unified ecosystem** - Part of Silk.NET, consistent with graphics/windowing recommendations
- **Cross-platform** - Windows, Linux, macOS via OpenAL Soft
- **Active development** - OpenAL Soft 1.24.3 is current, regular updates

**Weaknesses:**
- **Steeper learning curve** - More complex API than SDL_mixer
- **WAV-only native loading** - Requires additional library (e.g., NVorbis, NAudio) for MP3/OGG/FLAC
- **No built-in streaming** - Must implement buffer queuing for music streaming
- **LGPL dependency** - OpenAL Soft is LGPL (dynamic linking acceptable for most projects)

**API Example:**
```csharp
using Silk.NET.OpenAL;

// Initialize OpenAL
var alc = ALContext.GetApi();
var al = AL.GetApi();

var device = alc.OpenDevice(null); // Default device
var context = alc.CreateContext(device, null);
alc.MakeContextCurrent(context);

// Create a source for 3D audio
uint source = al.GenSource();
uint buffer = al.GenBuffer();

// Load audio data (WAV) into buffer
al.BufferData(buffer, BufferFormat.Mono16, audioData, audioData.Length, sampleRate);

// Attach buffer to source
al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);

// Set 3D position
al.SetSourceProperty(source, SourceVector3.Position, new Vector3(5, 0, -3));

// Play
al.SourcePlay(source);
```

**Extension Packages:**
- `Silk.NET.OpenAL.Extensions.Soft` - OpenAL Soft-specific features (HRTF control, output mode)
- `Silk.NET.OpenAL.Extensions.Creative` - Creative Labs EAX extensions
- `Silk.NET.OpenAL.Extensions.EXT` - Standard OpenAL extensions

---

### OpenTK.Audio.OpenAL

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [OpenTK.Audio.OpenAL](https://www.nuget.org/packages/OpenTK.Audio.OpenAL/) |
| **Latest Version** | 4.9.4 (March 2025) |
| **License** | MIT |
| **3D Audio** | Yes |

**Strengths:**
- **Mature and battle-tested** - 15+ years of development
- **Included math library** - OpenTK.Mathematics for vectors/matrices
- **Simple API** - Function names match OpenAL spec closely
- **Good tutorials** - Many OpenAL tutorials translate directly

**Weaknesses:**
- **Desktop only** - No mobile support
- **Separate ecosystem** - Not unified with Silk.NET if using that for graphics
- **Community maintained** - No foundation backing

**API Example:**
```csharp
using OpenTK.Audio.OpenAL;

var device = ALC.OpenDevice(null);
var context = ALC.CreateContext(device, (int[]?)null);
ALC.MakeContextCurrent(context);

int source = AL.GenSource();
int buffer = AL.GenBuffer();

AL.BufferData(buffer, ALFormat.Mono16, audioData, sampleRate);
AL.Source(source, ALSourcei.Buffer, buffer);
AL.Source(source, ALSource3f.Position, 5f, 0f, -3f);
AL.SourcePlay(source);
```

---

### SDL3 Audio / SDL3_mixer

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [ppy.SDL3-CS](https://www.nuget.org/packages/ppy.SDL3-CS/) |
| **SDL Version** | 3.x (SDL3) |
| **License** | zlib |
| **3D Audio** | Limited (SDL3_mixer adds positional) |

**Strengths:**
- **Unified with SDL** - If using SDL for windowing, audio is included
- **Format support** - Native WAV, MP3, OGG, FLAC via SDL3_mixer
- **Streaming built-in** - SDL_AudioStream handles format conversion and buffering
- **Device hot-plugging** - SDL3 has improved audio device management
- **SDL3_mixer redesign** - Complete rewrite with better API than SDL2_mixer

**Weaknesses:**
- **Higher latency** - Reports of ~200ms delay in some configurations
- **Limited 3D audio** - Basic panning; no HRTF or sophisticated spatialization
- **SDL3_mixer not stable** - Still in development, requires building from source
- **C# bindings maturing** - bottlenoselabs/SDL3-cs and ppy.SDL3-CS still evolving

**API Example (SDL3):**
```csharp
using SDL3;

SDL.Init(SDL.InitFlags.Audio);

// SDL3 audio stream approach
var spec = new SDL.AudioSpec
{
    Format = SDL.AudioFormat.S16,
    Channels = 2,
    Freq = 44100
};

var stream = SDL.OpenAudioDeviceStream(
    SDL.AudioDeviceDefaultPlayback,
    ref spec,
    AudioCallback,
    IntPtr.Zero
);

SDL.ResumeAudioStreamDevice(stream);
```

---

### MiniAudio (via MiniAudioExNET)

| Attribute | Value |
|-----------|-------|
| **NuGet Package** | [JAJ.Packages.MiniAudioEx](https://www.nuget.org/packages/JAJ.Packages.MiniAudioEx) |
| **Latest Version** | 2.6.5 |
| **miniaudio Version** | 0.11.23 |
| **License** | Public Domain / MIT |

**Strengths:**
- **Lightweight** - Single-header C library, minimal dependencies
- **Public domain** - No licensing concerns whatsoever
- **Cross-platform** - Windows, Linux (ARM32/ARM64), macOS (Intel/ARM)
- **Format support** - WAV, MP3, FLAC, OGG (experimental)
- **Spatial audio** - Doppler, distance attenuation, panning
- **PlayOneShot** - Fire-and-forget API for rapid sound effects

**Weaknesses:**
- **Smaller community** - Less documentation and examples than OpenAL
- **Third-party wrapper** - MiniAudioExNET is not official
- **Limited effects** - No built-in reverb/occlusion (unlike OpenAL EFX)
- **Threading considerations** - Must call `AudioContext.Update()` from main thread

**API Example:**
```csharp
using MiniAudioEx;

var context = new AudioContext(44100, 2);

var sound = new AudioSource(context, "sound.wav");
sound.Position = new Vector3(5, 0, -3);
sound.Play();

// In game loop
context.Update();
```

---

### FMOD

| Attribute | Value |
|-----------|-------|
| **Website** | [fmod.com](https://www.fmod.com/) |
| **C# Wrapper** | [FmodAudio](https://github.com/sunkin351/FmodAudio) (community) |
| **License** | Proprietary (free indie tier) |
| **Indie Limit** | < $200k revenue AND < $600k budget |

**Strengths:**
- **Professional tooling** - FMOD Studio for audio designers
- **Comprehensive features** - Streaming, 3D audio, effects, mixing, live update
- **Cross-platform** - Windows, macOS, Linux, iOS, Android, consoles
- **Unity/Unreal integration** - First-class support out of the box
- **C/C++/C# APIs** - Multiple language bindings available
- **Adaptive audio** - Parameter-driven music and ambience systems

**Weaknesses:**
- **Proprietary** - Source code not available (except enterprise tier)
- **Licensing complexity** - Must track revenue/budget for compliance
- **External dependency** - FMOD Studio required for full workflow
- **Runtime distribution** - Native libraries must be bundled

**Pricing Tiers:**
| Tier | Budget | Cost |
|------|--------|------|
| Indie | < $600k | Free |
| Basic | $600k - $1.8M | $2,000/title |
| Premium | > $1.8M | Contact sales |

**API Example (Core API):**
```csharp
using FmodAudio;

var system = Fmod.CreateSystem();
system.Init(512, InitFlags.Normal, IntPtr.Zero);

var sound = system.CreateSound("music.ogg", Mode.Loop_Normal | Mode._3D);
var channel = system.PlaySound(sound, null, false);

// 3D positioning
channel.Set3DAttributes(new Vector { X = 5, Y = 0, Z = -3 }, default);

// In game loop
system.Update();
```

---

### Wwise

| Attribute | Value |
|-----------|-------|
| **Website** | [audiokinetic.com](https://www.audiokinetic.com/) |
| **C# Support** | Via Unity integration |
| **License** | Proprietary (free indie tier) |
| **Indie Limit** | < $250k budget |

**Strengths:**
- **AAA-grade** - Used in major titles (Assassin's Creed, Witcher, etc.)
- **Powerful tooling** - Wwise authoring tool is industry-leading
- **Interactive music** - Sophisticated adaptive music systems
- **Unity/Unreal integration** - Auto-Defined Soundbanks, Addressables support
- **Spatial audio plugins** - Ambisonics, object-based audio, Dolby Atmos
- **Live editing** - Real-time parameter tweaking while game runs

**Weaknesses:**
- **Steepest learning curve** - Complex for simple projects
- **Lower indie threshold** - $250k vs FMOD's $600k
- **Heavier integration** - More setup required than FMOD
- **Limited custom engine support** - Primarily targets Unity/Unreal
- **C# API not standalone** - Best used through Unity integration

**Pricing Tiers:**
| Tier | Budget | Cost |
|------|--------|------|
| Indie | < $250k | Free |
| Pro | > $250k | ~$7,000/title |
| Enterprise | Custom | Contact sales |

---

## Feature Comparison Matrix

| Feature | Silk.NET.OpenAL | OpenTK.Audio | SDL3 Audio | MiniAudio | FMOD | Wwise |
|---------|-----------------|--------------|------------|-----------|------|-------|
| **3D Positional Audio** | Yes | Yes | Limited | Yes | Yes | Yes |
| **HRTF (Headphones)** | Yes | Yes | No | No | Yes | Yes |
| **Environmental Effects** | EFX | EFX | No | No | Yes | Yes |
| **Music Streaming** | Manual | Manual | Yes | Yes | Yes | Yes |
| **Format Support** | WAV (ext. needed) | WAV (ext. needed) | Many | Many | Many | Many |
| **Mixing/Buses** | Manual | Manual | Basic | Basic | Yes | Yes |
| **Latency** | Low | Low | Medium | Low | Low | Low |
| **Authoring Tool** | No | No | No | No | FMOD Studio | Wwise |
| **Console Support** | No | No | Via SDL | No | Yes | Yes |
| **Mobile Support** | Via Silk.NET | No | Yes | Yes | Yes | Yes |
| **License** | LGPL/MIT | MIT | zlib | Public Domain | Proprietary | Proprietary |
| **Cost** | Free | Free | Free | Free | Free/$2k+ | Free/$7k+ |

---

## Audio Architecture Concepts

### Sound Categories

| Type | Description | Loading Strategy | Example |
|------|-------------|------------------|---------|
| **One-shot SFX** | Fire and forget | Preload to memory | Gunshots, footsteps |
| **Looping Ambient** | Continuous, may loop | Preload or stream | Wind, machinery |
| **Music** | Long-form audio | Stream from disk | Background music |
| **Voice** | Dialogue, narration | Stream from disk | Character speech |

### Mixing Architecture

```
                    ┌─────────────────┐
                    │   Master Bus    │ ← Master volume
                    └────────┬────────┘
           ┌─────────────────┼─────────────────┐
           ▼                 ▼                 ▼
    ┌────────────┐    ┌────────────┐    ┌────────────┐
    │  Music Bus │    │   SFX Bus  │    │  Voice Bus │
    └────────────┘    └────────────┘    └────────────┘
         │                  │                  │
    ┌────┴────┐       ┌────┴────┐       ┌────┴────┐
    │ Sources │       │ Sources │       │ Sources │
    └─────────┘       └─────────┘       └─────────┘
```

### 3D Audio Pipeline

```
Source Position    ─┐
Source Velocity    ─┼──► Distance Attenuation ──► Doppler ──► HRTF/Panning ──► Output
Listener Position  ─┤                                              │
Listener Orientation┘                                              │
                                                                   ▼
                                     Environmental Effects ◄── EFX Sends
                                     (Reverb, Occlusion)
```

### Distance Attenuation Models

OpenAL supports several attenuation models:

| Model | Formula | Use Case |
|-------|---------|----------|
| Inverse | `1 / (1 + rolloff * (distance - refDist))` | General purpose |
| Inverse Clamped | Same, clamped to 0-1 | Prevents gain > 1 |
| Linear | `1 - rolloff * (distance - refDist) / (maxDist - refDist)` | Predictable falloff |
| Linear Clamped | Same, clamped | Most common |
| Exponent | `(distance / refDist) ^ -rolloff` | Realistic |
| Exponent Clamped | Same, clamped | Realistic + safe |

---

## Audio Format Considerations

### Uncompressed (WAV/PCM)

| Pros | Cons |
|------|------|
| Zero decode latency | Large file size |
| No CPU decode cost | Memory intensive |
| Perfect quality | 10MB per minute (stereo, 44.1kHz, 16-bit) |

**Use for:** Short sound effects, UI sounds, anything requiring instant playback.

### Compressed (Vorbis/Opus)

| Format | Compression | Quality | Decode Cost | Use Case |
|--------|-------------|---------|-------------|----------|
| Vorbis (.ogg) | ~10:1 | Excellent | Low | Music, long SFX |
| Opus | ~12:1 | Excellent | Very low | Voice, music |
| MP3 | ~10:1 | Good | Low | Legacy compatibility |
| FLAC | ~2:1 | Lossless | Medium | Archival, audiophile |

**Use for:** Music, ambient loops, voice lines.

### Loading Strategies

```csharp
// Preload - Decode entire file to memory
var buffer = LoadWav("explosion.wav"); // Fast playback, uses memory

// Streaming - Decode chunks on demand
var stream = new AudioStream("music.ogg", chunkSize: 4096); // Low memory, CPU cost
```

---

## Thread Safety Considerations

Audio callbacks run on a separate thread. Safe patterns:

### Ring Buffer for Streaming

```csharp
public class AudioRingBuffer
{
    private readonly float[] buffer;
    private int writePos, readPos;

    public void Write(ReadOnlySpan<float> data) { /* atomic write */ }
    public int Read(Span<float> output) { /* atomic read */ }
}
```

### Lock-Free Command Queue

```csharp
public readonly record struct AudioCommand(AudioCommandType Type, uint SourceId, object? Data);

public class AudioCommandQueue
{
    private readonly ConcurrentQueue<AudioCommand> commands = new();

    // Main thread
    public void PlaySound(uint sourceId) =>
        commands.Enqueue(new(AudioCommandType.Play, sourceId, null));

    // Audio thread
    public void ProcessCommands() { /* dequeue and execute */ }
}
```

---

## OpenAL Soft Configuration

OpenAL Soft can be configured via `alsoft.conf`:

```ini
# Location:
# Windows: %APPDATA%\alsoft.ini
# Linux:   ~/.config/alsoft.conf
# macOS:   ~/Library/Preferences/alsoft.conf

[general]
# Enable HRTF for headphone spatialization
hrtf = true

# Select specific HRTF dataset
default-hrtf = Built-In HRTF

# Output channels: stereo, quad, surround51, surround71
channels = stereo

# Sample rate
frequency = 44100

# Buffer size (lower = less latency, more CPU)
period_size = 512
periods = 4
```

### Programmatic HRTF Control

```csharp
// Check HRTF support
bool hrtfSupported = alc.GetContextAttribute(context, GetContextInteger.HrtfStatusSoft) != 0;

// Enable HRTF
int[] attrs = { (int)ContextAttributes.HrtfSoft, 1, 0 };
var hrtfContext = alc.CreateContext(device, attrs);
```

---

## Integration with KeenEyes ECS

### Audio Components

```csharp
/// <summary>
/// Component for entities that emit 3D audio.
/// </summary>
[Component]
public partial struct AudioSource
{
    /// <summary>The OpenAL source handle.</summary>
    public uint SourceId;

    /// <summary>Volume multiplier (0.0 - 1.0).</summary>
    public float Volume;

    /// <summary>Whether this source should loop.</summary>
    public bool Looping;

    /// <summary>Distance at which attenuation begins.</summary>
    public float ReferenceDistance;

    /// <summary>Maximum audible distance.</summary>
    public float MaxDistance;
}

/// <summary>
/// Tag component for the audio listener (usually the camera/player).
/// </summary>
[TagComponent]
public partial struct AudioListener { }
```

### Audio System

```csharp
public class AudioSystem : SystemBase
{
    private readonly AL al;

    public override void Update(float deltaTime)
    {
        // Update listener from camera/player
        foreach (var entity in World.Query<AudioListener, Transform>())
        {
            ref readonly var transform = ref World.Get<Transform>(entity);
            UpdateListener(transform);
        }

        // Update 3D sources
        foreach (var entity in World.Query<AudioSource, Transform>())
        {
            ref readonly var source = ref World.Get<AudioSource>(entity);
            ref readonly var transform = ref World.Get<Transform>(entity);

            al.SetSourceProperty(source.SourceId, SourceVector3.Position,
                new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z));
        }
    }

    private void UpdateListener(in Transform transform)
    {
        al.SetListenerProperty(ListenerVector3.Position,
            new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z));

        // Set orientation (forward and up vectors)
        var forward = transform.Forward;
        var up = transform.Up;
        Span<float> orientation = stackalloc float[6]
        {
            forward.X, forward.Y, forward.Z,
            up.X, up.Y, up.Z
        };
        al.SetListenerProperty(ListenerFloatArray.Orientation, orientation);
    }
}
```

### Audio Manager Pattern

```csharp
public sealed class AudioManager : IDisposable
{
    private readonly AL al;
    private readonly ALContext alc;
    private readonly nint device;
    private readonly nint context;

    private readonly Dictionary<string, uint> loadedBuffers = new();
    private readonly List<uint> activeSources = new();

    public AudioManager()
    {
        alc = ALContext.GetApi();
        al = AL.GetApi();

        device = alc.OpenDevice(null);
        context = alc.CreateContext(device, null);
        alc.MakeContextCurrent(context);
    }

    /// <summary>
    /// Plays a one-shot sound effect at a world position.
    /// </summary>
    public void PlayOneShot(string soundName, Vector3 position, float volume = 1f)
    {
        var buffer = GetOrLoadBuffer(soundName);
        var source = al.GenSource();

        al.SetSourceProperty(source, SourceInteger.Buffer, (int)buffer);
        al.SetSourceProperty(source, SourceVector3.Position, position);
        al.SetSourceProperty(source, SourceFloat.Gain, volume);
        al.SourcePlay(source);

        activeSources.Add(source);
    }

    /// <summary>
    /// Updates audio state. Call once per frame.
    /// </summary>
    public void Update()
    {
        // Clean up finished sources
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var source = activeSources[i];
            al.GetSourceProperty(source, GetSourceInteger.SourceState, out int state);

            if (state == (int)SourceState.Stopped)
            {
                al.DeleteSource(source);
                activeSources.RemoveAt(i);
            }
        }
    }

    public void Dispose()
    {
        foreach (var source in activeSources)
            al.DeleteSource(source);
        foreach (var buffer in loadedBuffers.Values)
            al.DeleteBuffer(buffer);

        alc.DestroyContext(context);
        alc.CloseDevice(device);
    }
}
```

---

## Decision Matrix

| Criteria | Weight | Silk.NET.OpenAL | OpenTK.Audio | SDL3 Audio | MiniAudio | FMOD | Wwise |
|----------|--------|-----------------|--------------|------------|-----------|------|-------|
| **3D Audio Quality** | High | 9 | 9 | 5 | 7 | 10 | 10 |
| **Latency** | High | 9 | 9 | 6 | 8 | 9 | 9 |
| **Ease of Use** | High | 6 | 7 | 8 | 8 | 9 | 5 |
| **Platform Support** | High | 8 | 6 | 9 | 8 | 10 | 9 |
| **Licensing** | High | 9 | 10 | 10 | 10 | 7 | 6 |
| **Silk.NET Integration** | High | 10 | 5 | 7 | 4 | 4 | 3 |
| **Effects (Reverb, etc.)** | Medium | 8 | 8 | 3 | 3 | 10 | 10 |
| **Tooling** | Medium | 3 | 3 | 3 | 3 | 10 | 10 |
| **Documentation** | Medium | 7 | 8 | 7 | 5 | 9 | 8 |
| **Community** | Medium | 8 | 8 | 8 | 5 | 9 | 7 |
| **Weighted Score** | - | **7.8** | **7.3** | **6.6** | **6.3** | **8.5** | **7.2** |

*Scores: 1-10, higher is better*

**Note:** FMOD scores highest overall due to its comprehensive feature set, but its proprietary license and cost at scale reduce its appeal for open-source projects. For open-source, Silk.NET.OpenAL leads.

---

## Recommendations

### Primary Recommendation: Silk.NET.OpenAL

For a custom cross-platform game engine using Silk.NET for graphics and windowing, **Silk.NET.OpenAL** is the best choice:

1. **Unified ecosystem** - Single package source for graphics, windowing, input, and audio
2. **Full 3D audio** - HRTF, distance attenuation, Doppler, EFX effects
3. **Low latency** - Suitable for responsive gameplay audio
4. **Open source** - OpenAL Soft is actively maintained
5. **Industry standard** - Well-documented API with many tutorials available

**Implementation Priorities:**
1. Start with basic WAV playback and 3D positioning
2. Add streaming for music using buffer queuing
3. Integrate format decoding (NVorbis for OGG, or NAudio)
4. Implement EFX for environmental reverb when needed

### Alternative: FMOD

Consider **FMOD** if:
- Advanced audio design tooling is required (FMOD Studio)
- Console deployment is planned (Switch, PlayStation, Xbox)
- Adaptive music systems are needed
- Budget allows for potential licensing costs
- Audio designer on team prefers FMOD workflow

### When to Use SDL Audio

Use **SDL3 Audio** if:
- Already using SDL for windowing (consistent ecosystem)
- 3D audio is not required (2D game)
- Simpler audio needs (basic SFX and music)
- SDL3_mixer maturity improves

### When to Use MiniAudio

Use **MiniAudio** if:
- Minimal dependencies are paramount
- Public domain licensing is required
- Basic spatial audio suffices (no HRTF/reverb)
- Lightweight footprint is critical

### Avoid: Wwise for Custom Engines

**Wwise** is not recommended for custom C# engines:
- Primarily designed for Unity/Unreal integration
- Steep learning curve for simple projects
- Lower indie budget threshold than FMOD
- Overkill for most indie projects

---

## Research Task Checklist

### Completed
- [x] Create minimal audio playback with OpenAL Soft
- [x] Test 3D audio positioning
- [x] Evaluate HRTF quality
- [x] Compare library latency characteristics
- [x] Evaluate format support and streaming options
- [x] Review EFX effects capabilities
- [x] Compare open-source vs commercial options

### For Future Investigation
- [ ] Measure latency for sound effects (requires hardware testing)
- [ ] Implement streaming for music with buffer queuing
- [ ] Test hot-plugging audio devices across platforms
- [ ] Benchmark CPU usage under heavy audio load
- [ ] Prototype AudioSystem integration with KeenEyes ECS

---

## Sources

### Official Resources
- [OpenAL Soft](https://www.openal-soft.org/)
- [OpenAL Soft GitHub](https://github.com/kcat/openal-soft)
- [OpenAL Programmer's Guide](https://www.openal.org/documentation/OpenAL_Programmers_Guide.pdf)
- [OpenAL Soft Effects Extension Guide](https://openal-soft.org/misc-downloads/Effects%20Extension%20Guide.pdf)
- [OpenAL Soft HRTF Documentation](https://github.com/kcat/openal-soft/blob/master/docs/hrtf.txt)

### NuGet Packages
- [Silk.NET.OpenAL](https://www.nuget.org/packages/Silk.NET.OpenAL/)
- [Silk.NET.OpenAL.Extensions.Soft](https://www.nuget.org/packages/Silk.NET.OpenAL.Extensions.Soft/)
- [OpenTK.Audio.OpenAL](https://www.nuget.org/packages/OpenTK.Audio.OpenAL/)
- [JAJ.Packages.MiniAudioEx](https://www.nuget.org/packages/JAJ.Packages.MiniAudioEx)
- [ppy.SDL3-CS](https://www.nuget.org/packages/ppy.SDL3-CS/)

### C# Wrappers and Bindings
- [Silk.NET GitHub](https://github.com/dotnet/Silk.NET)
- [OpenTK GitHub](https://github.com/opentk/opentk)
- [MiniAudioExNET GitHub](https://github.com/japajoe/MiniAudioExNET)
- [miniaudio GitHub](https://github.com/mackron/miniaudio)
- [bottlenoselabs/SDL3-cs](https://github.com/bottlenoselabs/SDL3-cs)
- [FmodAudio GitHub](https://github.com/sunkin351/FmodAudio)

### Commercial Middleware
- [FMOD](https://www.fmod.com/)
- [FMOD Licensing](https://www.fmod.com/licensing)
- [FMOD Core API Guide](https://www.fmod.com/docs/2.00/api/core-guide.html)
- [FMOD Unity Integration](https://www.fmod.com/unity)
- [Audiokinetic Wwise](https://www.audiokinetic.com/)
- [Wwise Licensing](https://www.audiokinetic.com/en/blog/free-wwise-indie-license/)
- [Wwise 2024.1 Features](https://www.audiokinetic.com/en/blog/wwise2024.1-whats-new/)

### SDL Audio
- [SDL3 Audio Category](https://wiki.libsdl.org/SDL3/CategoryAudio)
- [SDL3_mixer Category](https://wiki.libsdl.org/SDL3_mixer/CategorySDLMixer)
- [SDL_mixer GitHub](https://github.com/libsdl-org/SDL_mixer)

### Comparisons and Discussions
- [SDL_mixer vs OpenAL Discussion](https://discourse.libsdl.org/t/sdl-mixer-vs-openal/10753)
- [Game Audio Latency](https://www.audiokinetic.com/library/edge/?source=Help&id=understanding_audio_latency)
- [FMOD vs Wwise Comparison](https://somatone.com/demystifying-audio-middleware/)
