# Audio

The `KeenEyes.Audio` library provides ECS components and systems for sound playback, while `KeenEyes.Audio.Silk` supplies a Silk.NET/OpenAL backend that you install into a `World` as a plugin.

## Overview

Audio in KeenEyes is split across three packages:

- **`KeenEyes.Audio.Abstractions`** - Backend-agnostic contracts: `IAudioContext` (high-level playback API), `IAudioDevice` (low-level backend operations), handles (`AudioClipHandle`, `SoundHandle`), and value types (`PlaybackOptions`, `AudioClipInfo`, `AudioConfig`).
- **`KeenEyes.Audio`** - ECS integration: the `AudioSource` and `AudioListener` components, plus `AudioSourceSystem`, `AudioListenerSystem`, and `AudioUpdateSystem` that synchronize those components with whichever `IAudioContext` is installed.
- **`KeenEyes.Audio.Silk`** - A concrete backend, `SilkAudioPlugin`, that implements `IAudioContext` on top of OpenAL via Silk.NET and wires up the ECS systems automatically.

Two usage styles are supported and can be mixed freely:

1. **Fire-and-forget** - call `IAudioContext.Play`/`PlayAt` directly for one-shot sounds (UI clicks, hit reactions).
2. **ECS-driven** - attach an `AudioSource` component to an entity for sounds whose playback state and 3D position should track that entity across frames.

## Quick Start

### Installation

`SilkAudioPlugin` requires a Silk window plugin to already be installed, since it shares the window's OpenAL lifecycle:

```csharp
using KeenEyes.Audio.Abstractions;
using KeenEyes.Audio.Silk;
using KeenEyes.Platform.Silk;

using var world = new World();

// Window plugin must be installed first
world.InstallPlugin(new SilkWindowPlugin(new WindowConfig
{
    Title = "My Game",
    Width = 1920,
    Height = 1080
}));

// Then install audio
world.InstallPlugin(new SilkAudioPlugin(new SilkAudioConfig
{
    MaxOneShotSources = 64,
    InitialMasterVolume = 0.8f
}));
```

Installing `SilkAudioPlugin` registers:

- The `IAudioContext` extension (backed by `SilkAudioContext`), retrievable via `world.GetExtension<IAudioContext>()`.
- The `AudioSource` and `AudioListener` components.
- Three systems in the `SystemPhase.Update` phase: `AudioUpdateSystem` (order 0, recycles pooled sources), `AudioListenerSystem` (order 5, syncs the active listener), and `AudioSourceSystem` (order 10, syncs sources and playback state).

If the window plugin was not installed first, `Install` throws an `InvalidOperationException`.

### Playing a One-Shot Sound

```csharp
var audio = world.GetExtension<IAudioContext>();

var explosion = audio.LoadClip("assets/sounds/explosion.wav");
audio.Play(explosion, volume: 0.8f);
```

`Play` and `PlayAt` also accept a `PlaybackOptions` value for control over pitch, looping, and mixer channel:

```csharp
audio.Play(explosion, new PlaybackOptions
{
    Volume = 0.5f,
    Pitch = 1f,
    Loop = false,
    Channel = AudioChannel.SFX
});
```

Both `Play` overloads return a `SoundHandle` that can be used with `Stop`, `Pause`, `Resume`, `SetVolume`, `SetPitch`, `SetPosition`, and `IsPlaying`.

## Core Concepts

### `AudioClipHandle` and `SoundHandle`

`IAudioContext` deals in two opaque handle types instead of exposing backend resource types directly:

- `AudioClipHandle` identifies loaded audio data (returned by `LoadClip`/`CreateClip`).
- `SoundHandle` identifies a specific playing instance. A single clip can have many concurrent sound handles.

Both are `readonly record struct` types with a static `Invalid` value and an `IsValid` property.

### `IAudioContext`

`IAudioContext` is the application-facing API. Beyond clip loading and playback, it exposes:

- `MasterVolume` (get/set, 0.0-1.0) for global volume.
- `GetChannelVolume(AudioChannel)` / `SetChannelVolume(AudioChannel, float)` for per-category mixing across `AudioChannel.Master`, `Music`, `SFX`, `Voice`, and `Ambient`.
- `SetListenerPosition`, `SetListenerOrientation`, and `SetListenerVelocity` as convenience wrappers around the device - for ECS-driven games, prefer the `AudioListener` component instead.
- `StopAll`, `PauseAll`, and `ResumeAll` for bulk control.
- `Update()`, which the `AudioUpdateSystem` calls once per frame to recycle finished sources back into the pool.

### `AudioSource` Component

Attach `AudioSource` to an entity to have `AudioSourceSystem` manage a backend source on its behalf:

```csharp
using System.Numerics;
using KeenEyes.Audio;
using KeenEyes.Common;

var enemy = world.Spawn()
    .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
    .With(AudioSource.Default with
    {
        Clip = explosion,
        Spatial = true,
        PlayOnAwake = true,
        MinDistance = 2f,
        MaxDistance = 50f
    })
    .Build();
```

Key fields on `AudioSource`:

- `Clip` (`AudioClipHandle`) - the clip to play.
- `Volume`, `Pitch`, `Loop`, `Channel` - playback parameters, mirroring `PlaybackOptions`.
- `PlayOnAwake` - starts playback the first time the system sees the component; the flag is consumed (set back to `false`) afterward.
- `Spatial` - when `true`, `AudioSourceSystem` reads the entity's `Transform3D` (and `Velocity3D`, if present, for Doppler) each frame to update the backend source's position/velocity. When `false`, the sound plays without distance attenuation.
- `MinDistance`, `MaxDistance`, `RolloffMode` - 3D distance attenuation settings, used only when `Spatial` is `true`.
- `State` (`AudioPlayState`) - set to `AudioPlayState.Playing` or `AudioPlayState.Stopped` to request playback changes; the system updates it to reflect what actually happened (e.g. a non-looping sound naturally finishing sets it back to `Stopped`).
- `CurrentSound` (`SoundHandle`) - the handle for the actively playing sound, so you can still call `IAudioContext` methods like `SetVolume` on it directly.

`AudioSourceSystem` creates the backend source lazily (only once `State` becomes `Playing`), and cleans it up when the component is removed or the entity is destroyed - `SilkAudioPlugin` wires both of those lifecycle events automatically.

### `AudioListener` Component

`AudioListener` marks the entity that acts as the "ears" for 3D spatial audio - typically the player or camera:

```csharp
var player = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(AudioListener.Active)
    .Build();
```

`AudioListenerSystem` reads the listener entity's `Transform3D` each frame to update the backend listener's position and orientation, derives velocity from the position delta for Doppler calculations, and applies `AudioListener.VolumeMultiplier`, `DopplerFactor`, and `SpeedOfSound`. Only one active listener is honored; if several entities have `IsActive = true`, the first one encountered by the query wins.

Use `AudioListener.Active` or `AudioListener.Inactive` to get an instance with sensible defaults (`DopplerFactor = 1`, `VolumeMultiplier = 1`, `SpeedOfSound = 343`).

### Channels and Playback Options

`AudioChannel` provides five mixer groups - `Master`, `Music`, `SFX`, `Voice`, `Ambient` - so a UI can, for example, mute music while keeping sound effects audible via `SetChannelVolume`. `PlaybackOptions` (a `readonly record struct`) bundles `Volume`, `Pitch`, `Loop`, and `Channel` for a single `Play`/`PlayAt` call; use `PlaybackOptions.Default` or `with` expressions to tweak individual fields.

### Distance Attenuation

`AudioRolloffMode` controls how spatial sound volume falls off with distance: `Linear`, `Logarithmic` (the default used by both `AudioConfig` and `AudioSource.Default`), `Exponential`, or `Custom` (where the application computes gain manually - the backend applies no automatic attenuation in that mode).

## Performance

- One-shot sounds played through `IAudioContext.Play`/`PlayAt` draw from an internal source pool sized by `SilkAudioConfig.MaxOneShotSources` (default 32); `AudioUpdateSystem` must run every frame to recycle finished sources back into that pool, or it will eventually be exhausted.
- `AudioSourceSystem` only allocates a backend source for an `AudioSource` once its `State` becomes `Playing`, so idle sources with no clip assigned cost nothing.
- Spatial position/velocity updates are only issued for entities with `Spatial = true`, avoiding unnecessary device calls for 2D/UI sounds.

## Next Steps

- [Plugins Guide](plugins.md) - How plugins like `SilkAudioPlugin` install extensions and systems
- [Systems Guide](systems.md) - System phases, ordering, and lifecycle
- [Audio System Design](research/audio-system.md) - Original design document
