# Mobile Platform Support (iOS & Android) - Research Report

**Date:** July 2026
**Purpose:** Evaluate graphics, audio, windowing/input, and packaging options for shipping KeenEyes games on iOS and Android under Native AOT

## Executive Summary

KeenEyes can reach iOS and Android without abandoning its current architecture. All three subsystems sit behind engine-owned abstractions (`KeenEyes.Graphics.Abstractions`, `KeenEyes.Audio.Abstractions`, `KeenEyes.Input.Abstractions`), so mobile support means **adding backends, not migrating**. The recommended stack:

| Layer | Recommendation | Why |
|-------|---------------|-----|
| **App container / windowing / input** | **SDL3** (vendored AOT-safe P/Invoke bindings) | Only 2026-production-grade mobile container reachable from C# with zero reflection; first-class lifecycle, touch, IME, sensors, haptics. Both Silk.NET 3.0 and FNA converged on it. |
| **Graphics** | **OpenGL ES 3.0** via `Silk.NET.OpenGLES` — system GLES on Android, **ANGLE-on-Metal** on iOS; **SDL3 GPU** as the strategic second backend | Smallest delta from the existing GL 3.3 renderer; ANGLE removes Apple's GLES-deprecation risk entirely; SDL_GPU is the modern successor path (FNA3D committed to it). |
| **Audio** | **miniaudio** via a small engine-owned `[LibraryImport]` binding | Public domain (kills the OpenAL Soft LGPL problem on iOS), native AAudio on Android, built-in decode/streaming, ~1:1 mapping onto our OpenAL-shaped abstraction. |
| **Packaging** | iOS: `net10.0-ios` + `PublishAot`. Android: `net10.0-android` + MonoAOT now, flip to Native AOT when it exits experimental; `linux-bionic-arm64` as escape hatch | iOS Native AOT is officially supported and proven (FNA ships this way). Android workload Native AOT is still experimental in .NET 10. |

Key negative findings: **GLFW will never support mobile** (a second windowing backend is mandatory), **Silk.NET 3.0 has not shipped** (preview feed only — do not plan around it), and **`Silk.NET.OpenAL.Soft.Native` ships no mobile binaries** (verified by unpacking the package), so the current audio path does not carry over.

---

## Constraints

- .NET 10, C# 13, zero reflection. Native AOT is **mandatory** on iOS (Apple forbids JIT) and strongly preferred on Android.
- All native bindings must be AOT/trim-safe (`DllImport`/`[LibraryImport]` P/Invoke).
- Current desktop stack: Silk.NET 2.23 — GLFW windowing, `Silk.NET.Input`, OpenGL 3.3 core, OpenAL Soft.
- KESL shader compiler (`editor/KeenEyes.Shaders.Compiler`) emits GLSL (`#version 450`) and HLSL behind an `IShaderGenerator` seam — new shader targets are contained changes.
- The input abstraction models keyboard/mouse/gamepad only; touch is not yet modeled.
- The game loop is engine-owned; mobile lifecycle (suspend/resume, surface loss) must integrate with it.

## Platform Ground Truths (2026)

- **Apple:** OpenGL ES is deprecated since iOS 12 but still present and still App Store-accepted; frozen at ES 3.0, could be removed in any future release. Metal is the only first-class API. Building the iOS GLES backend on **ANGLE's Metal backend** (upstream since 2021; the older MetalANGLE fork is retired in its favor) removes the deprecation risk entirely — the shipped app is a Metal app.
- **Google:** Vulkan was declared **the official Android graphics API** in 2025. GLES remains supported but feature-frozen; Android 15+ bundles **ANGLE-on-Vulkan** as the long-term GLES compatibility layer. ~85% of active devices support Vulkan; all 64-bit devices on Android 10+ have Vulkan 1.1.
- **GLFW is desktop-only, permanently** (FAQ lists Windows/macOS/X11/Wayland; mobile has never been on the roadmap).
- **SDL3 is stable and mature** (3.2.0 stable Jan 2025; 3.4.x through mid-2026), with iOS and Android as first-class platforms. SDL2 is in maintenance mode behind sdl2-compat.
- **Silk.NET 3.0 has not shipped** (milestone ~49% complete, experimental builds on the GitHub feed only, no date; its windowing is itself moving to SDL3). Silk.NET 2.x remains maintained (2.23, Jan 2026) and is trim/AOT-compatible since 2.18.
- **.NET mobile packaging:** `net10.0-ios` + `PublishAot` is officially documented and supported. `net10.0-android` + `PublishAot` is **experimental** in .NET 10 (big startup wins measured, but no built-in Java interop and active publish bugs). `linux-bionic-arm64` Native AOT with custom Gradle packaging is proven (Android-NativeAOT demo: 4.3 MB APK, ~120 ms startup) but you own the entire packaging/JNI story.

---

## Graphics

### Comparison

| Candidate | iOS | Android | C# bindings / AOT | Shader input | Effort from GL 3.3 | Verdict |
|---|---|---|---|---|---|---|
| **GLES 3.0 (system / ANGLE)** | ANGLE-on-Metal: store-safe, no deprecation risk | Fully supported (~100% devices); ANGLE-on-Vulkan is Android's own future | `Silk.NET.OpenGLES` 2.23 — pure P/Invoke, AOT-safe, near-identical to current `Silk.NET.OpenGL` | GLSL ES 300 | **Lowest** (port, not rewrite) | **Primary** |
| **SDL3 GPU** | First-class Metal, no translation layer | Vulkan only (~85% devices, rising; feature-toggle props widen coverage) | SDL3-CS variants — raw P/Invoke, AOT-safe | SPIR-V / MSL / DXIL via SDL_shadercross | Medium | **Strategic second backend** |
| Vulkan + MoltenVK | MoltenVK 1.4 "nearly conformant"; framework packaging solved | Native 1.1+ | `Silk.NET.Vulkan`, AOT-safe | SPIR-V | **Highest** (full explicit-API renderer) | Not now — SDL_GPU gives the same targets for far less |
| WebGPU (wgpu/Dawn) | Metal backend works | Vulkan backend, Android artifacts published | `Silk.NET.WebGPU` pinned to an older `webgpu.h`; header churn is chronic | WGSL / SPIR-V | High | Revisit 2027 |
| bgfx | Metal, iOS 16+ | GLES/Vulkan | C# bindings stale/unpackaged — we'd own them | **Own shaderc dialect** — fights KESL | Medium-high | Rejected |
| Veldrid | ppy fork ships osu! on iOS | ppy fork: GLES/Vulkan | Original abandoned (2023); forks serve one project each | SPIR-V | Medium | Rejected (stewardship); useful as reference code |

### Recommendation

**Primary: a GLES 3.0 backend on `Silk.NET.OpenGLES`** — system GLES/EGL on Android, ANGLE-on-Metal on iOS.

- Smallest delta from the existing GL 3.3 renderer: GLES 3.0 ≈ GL 3.3 core minus a few features (no geometry shaders, UBO alignment care, multisample-texture nuances).
- KESL needs only a **GLSL ES 300 emitter** (`#version 300 es`, precision qualifiers, in/out renames) — a dialect tweak on the existing `GlslGenerator`, no SPIR-V toolchain required to ship.
- Reaches ~100% of Android devices (no Vulkan floor); Google itself blessed the ANGLE architecture by bundling it in Android 15+.
- **Open verification item:** confirm an `ios-arm64` static ANGLE build. `Silk.NET.OpenGLES.ANGLE.Native` (2025.9.12) exists but its RID list must be checked; fall back to building ANGLE ourselves or nutiteq-style prebuilts.

**Strategic second backend: SDL3 GPU.** First-class Metal on iOS with zero translation layers, Vulkan on Android, superbly maintained, and FNA3D has committed to it as its sole future graphics path — the strongest endorsement available from the most AOT-disciplined C# game stack. Shader pipeline: KESL → GLSL → glslang → SPIR-V → SDL_shadercross → MSL/DXIL, all offline at build time. Since SDL3 is already the recommended windowing layer, this backend adds no new runtime native dependency.

Proof-of-path evidence: MonoGame and KNI ship GLES on both stores today; osu! ships on iOS/Android via ppy.Veldrid (Metal + GLES/Vulkan); FNA ships iOS under Native AOT.

## Audio

### Comparison

| | OpenAL Soft (current) | **miniaudio** | FAudio | SDL3_mixer | SoLoud |
|---|---|---|---|---|---|
| Maintenance | Active (1.25.2) | Active (0.11.25, Mar 2026) | Very active (monthly) | Active, young (first stable Mar 2026) | **Dormant since Aug 2024** |
| License | **LGPL-2.0+** | **Public domain / MIT-0** | zlib | zlib | zlib |
| iOS story | Static link legally gray; clean path = embedded dynamic framework + custom AOT publish plumbing | **Static link freely, zero obligations** | Clean, FNA-proven under AOT | Clean | — |
| Android backend | Oboe/OpenSL — build your own `.so` | **Native AAudio** + OpenSL fallback | Via SDL3 audio | Via SDL3 audio | OpenSL (old) |
| Mobile natives in C# pkg | **None** (`Silk.NET.OpenAL.Soft.Native` = desktop RIDs only, verified) | Build-your-own (tiny) or MiniAudioEx | FNA tooling | SDL3-CS ecosystem | None |
| Spatial audio | Full OpenAL (+HRTF, EFX) | ≈ OpenAL parity (cones, doppler, attenuation; no HRTF/EFX) | X3DAudio calculator model | Positional (new, less proven) | — |
| Decode/streaming | None (BYO — status quo) | **WAV/FLAC/MP3/OGG built-in + streaming** | PCM/ADPCM/WMA only | OGG/MP3/WAV/FLAC | — |
| Migration effort | Zero abstraction change, heavy packaging/licensing cost | Low-moderate (near-1:1 concept map) | Moderate-high (XAudio2 shape) | Moderate; couples to SDL3 | n/a |

### Recommendation

**Switch mobile to miniaudio; keep OpenAL Soft on desktop initially.** The license alone decides iOS (static link with zero obligations vs the LGPL framework-embedding dance), and miniaudio's native AAudio backend is the best Android latency path of any candidate. Its `ma_engine`/`ma_sound` model (source, listener, cones, doppler, attenuation) maps nearly 1:1 onto the current OpenAL-shaped `KeenEyes.Audio.Abstractions` — exactly the swap the abstraction exists for.

Prefer an **engine-owned thin `[LibraryImport]` + `[UnmanagedCallersOnly]` binding over vanilla miniaudio** rather than depending on MiniAudioEx (active but single-maintainer, wraps a fork, no formal releases) — the needed surface (engine init, load/play/stream, 3D params, groups) is small. Use MiniAudioEx's repo as a reference for iOS/Android build recipes. On iOS: static `.a` + `DirectPInvoke`; on Android: per-ABI `.so`.

**Follow-up worth considering:** once the miniaudio backend is proven, converge desktop onto it too — one backend everywhere, no LGPL anywhere, built-in decoders replace managed ones. The loss would be OpenAL Soft's HRTF/EFX, which KeenEyes doesn't currently expose. **SDL3_mixer** is the re-evaluation candidate if we want maximal stack coherence after SDL3 adoption, but at ~4 months post-1.0 it's too young to displace miniaudio.

## Windowing, Input, and App Lifecycle

### Recommendation: SDL3 direct

Build a new **`KeenEyes.Platform.Sdl3`** backend on SDL3 with vendored, auto-generated pure-P/Invoke bindings (flibitijibibo/SDL3-CS consumed as source, or ppy.SDL3-CS from NuGet — battle-tested in osu!framework's shipping iOS/Android packages). GLFW remains the desktop backend short-term.

Why not the alternatives:

- **Silk.NET 2.x SDL mobile:** exists (2.22 added iOS support via `SilkMobile.RunApp`) but is SDL2-based, has a known-unfixed iOS run-loop defect (blocking `Run()` instead of the animation callback — fix deferred to 3.0), models no touch devices, and has near-zero real-world shipping record. Prototype-grade only.
- **Silk.NET 3.0:** preview-only builds on a GitHub feed; timeline unbounded. Its windowing is itself SDL3-backed, so a KeenEyes SDL3 backend built now shares concepts with any eventual migration.
- **SDL2/FNA-legacy stack:** superseded; FNA itself defaulted to SDL3.

What SDL3 provides that the engine needs:

- **Lifecycle:** `SDL_EVENT_WILL/DID_ENTER_BACKGROUND/FOREGROUND`, `SDL_EVENT_TERMINATING`, `SDL_EVENT_LOW_MEMORY`. (Caveat: Android task-kill may skip TERMINATING — treat `DID_ENTER_BACKGROUND` as the save point.)
- **Touch:** finger down/motion/up/**canceled** events with device ID, finger ID, normalized coords, deltas, pressure. Touch-synthesized mouse events are tagged (`SDL_TOUCH_MOUSEID`) and filterable. Gesture recognition is **not** in SDL3 core — the engine owns tap/long-press/pinch/pan and virtual-gamepad overlays.
- **Text input:** IME sessions (`SDL_StartTextInput`, composition events, on-screen keyboard management), plus sensors, haptics, pen.
- **Main callbacks** (`SDL_AppInit`/`SDL_AppIterate`/`SDL_AppEvent`/`SDL_AppQuit`): exist precisely because iOS/Android forbid an app-owned blocking loop.

### Required engine changes (independent of library choice)

1. **Game loop inversion of control** — the single largest engineering item. Mobile requires the OS to drive frames: the engine loop must support a per-frame `Tick(dt)` mode mapped onto SDL3 main callbacks. Desktop keeps the blocking loop.
2. **Touch input model** in `KeenEyes.Input.Abstractions`: `TouchDevice` with per-finger `TouchPoint { DeviceId, FingerId, NormalizedX/Y, DeltaX/Y, Pressure, Phase }` including a **Canceled** phase (OS-stolen touches); a policy suppressing touch→mouse double-delivery; engine-side gesture recognizers.
3. **Text-input session API** (begin/end IME, composition, keyboard-occlusion rect) — mobile keyboards are request-driven.
4. **Lifecycle surface** to the engine: background/foreground, GL surface lost/recreated (Android context loss), low-memory.
5. **Known .NET seam:** naive `SDL_Init` from P/Invoke fails on mobile ("did you include SDL_main.h") — solved by exporting an entry point via `[UnmanagedCallersOnly]` or `SDL_RunApp`; a one-time integration cost with osu!framework and FNA as working references.

## Packaging Pipelines

**iOS (one real path):** `net10.0-ios` + `PublishAot=true` (use `dotnet publish`), statically linked SDL3/ANGLE/miniaudio (`NativeReference` / `-force_load`), standard Xcode signing. FNA ships App Store titles this way; documented limitations are no on-device managed debugging and no DXT textures.

**Android (sequenced):**
1. **Now:** `net10.0-android` + MonoAOT — mature, store-ready, standard AAB flow, `SDLActivity` Java glue (osu!framework's approach). Acceptable interim: the zero-reflection codebase runs identically on Mono.
2. **When stable:** flip to `PublishAot` on the same TFM (experimental in .NET 10; measured startup ~1.3 s → ~300 ms).
3. **Escape hatch:** `linux-bionic-arm64` Native AOT + custom Gradle packaging with embedded `SDLActivity` glue — full control, zero Mono, but we own Gradle/signing/ABI splits/JNI for platform services.

## Proposed Roadmap

1. **`KeenEyes.Platform.Sdl3`** — SDL3 windowing/lifecycle backend + game-loop `Tick(dt)` inversion (desktop-testable: SDL3 runs everywhere, which also derisks it before any mobile toolchain work).
2. **Touch + lifecycle + IME additions** to `KeenEyes.Input.Abstractions` and the platform abstraction.
3. **`KeenEyes.Graphics.Gles`** backend (`Silk.NET.OpenGLES`) + KESL GLSL ES 300 emitter. Verify the `ios-arm64` ANGLE static build early (go/no-go for the iOS graphics plan).
4. **`KeenEyes.Audio.MiniAudio`** backend with engine-owned bindings + native build recipes (iOS static lib, Android per-ABI `.so`).
5. **Packaging proof:** NOVAFALL (or a smaller sample) on `net10.0-ios PublishAot` and `net10.0-android` MonoAOT.
6. **Later:** KESL SPIR-V emitter (independent work item) → **SDL3 GPU backend**, retiring GLES on iOS first; re-evaluate desktop audio convergence on miniaudio; watch Android workload Native AOT and Silk.NET 3.0.

## Rejected Options (summary)

- **Vulkan + MoltenVK now:** correct long-term API, worst effort/reward for a GL 3.3-shaped engine; SDL_GPU reaches the same native APIs for far less.
- **WebGPU native:** healthy upstream but `webgpu.h` churn and thin mobile production record in C#; revisit 2027.
- **bgfx:** stale/unpackaged C# bindings and a proprietary shader dialect that fights the KESL compiler.
- **Veldrid:** original abandoned; maintained forks each serve one project. KeenEyes already owns its abstraction layer — adopting someone else's adds stewardship risk without capability.
- **SoLoud:** dormant since mid-2024.
- **FAudio:** superbly maintained but XAudio2-shaped (fights the OpenAL-style abstraction), no OGG/MP3 decode, Android not first-class.
- **Keeping OpenAL Soft on mobile:** viable but inherits a cross-compile CI pipeline (no upstream mobile artifacts in the Silk native package) plus the iOS LGPL framework-embedding dance.

## Key Sources

Graphics: [Android: Vulkan official](https://developer.android.com/games/develop/vulkan/overview) · [LunarG: Vulkan on Apple, Jan 2026](https://www.lunarg.com/the-state-of-vulkan-on-apple-jan-2026/) · [ANGLE Metal backend](https://groups.google.com/g/angleproject/c/DkD0rdMQCbM) · [Silk.NET.OpenGLES.ANGLE.Native](https://www.nuget.org/packages/Silk.NET.OpenGLES.ANGLE.Native) · [SDL3 GPU](https://wiki.libsdl.org/SDL3/CategoryGPU) · [SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross) · [FNA3D → SDL_GPU commitment](https://github.com/FNA-XNA/FNA3D/issues/230) · [Silk.NET 3.0 milestone](https://github.com/dotnet/Silk.NET/milestone/9) · [Apple GLES store acceptance](https://developer.apple.com/forums/thread/735391)

Audio: [openal-soft](https://github.com/kcat/openal-soft) · [Silk.NET.OpenAL.Soft.Native (desktop RIDs only)](https://www.nuget.org/packages/Silk.NET.OpenAL.Soft.Native/) · [miniaudio](https://miniaud.io/) · [miniaudio spatialization](https://github.com/mackron/miniaudio/discussions/523) · [MiniAudioExNET](https://github.com/japajoe/MiniAudioExNET) · [FAudio](https://github.com/FNA-XNA/FAudio) · [SDL_mixer](https://github.com/libsdl-org/SDL_mixer) · [LGPL static-linking discussion](https://developer.apple.com/forums/thread/702873)

Platform: [GLFW FAQ (desktop-only)](https://www.glfw.org/faq) · [SDL3 README-android](https://wiki.libsdl.org/SDL3/README-android) / [README-ios](https://wiki.libsdl.org/SDL3/README-ios) / [README-touch](https://wiki.libsdl.org/SDL3/README-touch) · [SDL #13201 (C# SDL_main)](https://github.com/libsdl-org/SDL/issues/13201) · [flibitijibibo/SDL3-CS](https://github.com/flibitijibibo/SDL3-CS) · [ppy.SDL3-CS](https://www.nuget.org/packages/ppy.SDL3-CS/) · [FNA on Apple platforms](https://fna-xna.github.io/docs/appendix/Appendix-C:-FNA-on-Apple-Platforms/) · [Native AOT for iOS-like platforms](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/ios-like-platforms/) · [Android Native AOT status](https://github.com/dotnet/runtime/issues/106748) · [Android-NativeAOT demo](https://github.com/jonathanpeppers/Android-NativeAOT)
