# ADR-016: Mobile Platform Support (iOS & Android)

**Status:** Proposed
**Revision:** v2
**Implementation:** Not started
**First accepted:** — (proposed 2026-07-27)
**Relates to:** [ADR-004](004-reflection-elimination.md) (AOT compatibility) · [ADR-005](005-graphics-input-abstraction-layers.md) (graphics/input abstraction layers) · [ADR-009](009-kesl-shader-language.md) (KESL) · [Mobile Platform Support research](../research/mobile-platform-support.md) · tracking [#1344](https://github.com/orion-ecs/keen-eye/issues/1344) [#1345](https://github.com/orion-ecs/keen-eye/issues/1345) [#1346](https://github.com/orion-ecs/keen-eye/issues/1346) [#1347](https://github.com/orion-ecs/keen-eye/issues/1347) [#1348](https://github.com/orion-ecs/keen-eye/issues/1348) [#1349](https://github.com/orion-ecs/keen-eye/issues/1349) [#1350](https://github.com/orion-ecs/keen-eye/issues/1350)

## Context

As of mid-2026 KeenEyes ran only on desktop: Silk.NET 2.23 with GLFW windowing, an OpenGL 3.3 core renderer, `Silk.NET.Input` (keyboard/mouse/gamepad), and OpenAL Soft audio. The engine's hard constraints — Native AOT everywhere, zero reflection, engine-owned abstractions (`KeenEyes.Graphics.Abstractions`, `KeenEyes.Audio.Abstractions`, `KeenEyes.Input.Abstractions`) — were chosen partly with mobile in mind (Apple forbids JIT), but no mobile backend existed.

Research in July 2026 ([research report](../research/mobile-platform-support.md)) established the forces:

- **GLFW is desktop-only, permanently** — mobile requires a second platform backend regardless of any other choice.
- **Silk.NET 3.0 had not shipped** (preview-only builds, ~49% milestone, no date); its own windowing rewrite targets SDL3.
- **Apple** deprecated OpenGL ES (frozen at ES 3.0, still store-accepted) with Metal as the only first-class API; **Google** declared Vulkan the official Android API (~85% device coverage) while bundling ANGLE-on-Vulkan in Android 15+ as the long-term GLES story.
- **The audio path did not carry over**: `Silk.NET.OpenAL.Soft.Native` ships desktop RIDs only, and OpenAL Soft's LGPL makes static linking into an iOS binary legally gray.
- **.NET packaging**: `net10.0-ios` + `PublishAot` was officially supported and proven (FNA ships App Store titles this way); Android workload Native AOT was still experimental in .NET 10.
- Mobile OSes forbid an app-owned blocking game loop; the OS drives frames.
- The input abstraction had no touch model, and KESL emitted only desktop GLSL 450 and HLSL.

## Decision

KeenEyes adds mobile support as **new backends behind the existing abstractions** — no migration of the desktop stack. The mobile stack is:

### Platform container: SDL3

A new `KeenEyes.Platform.Sdl3` backend owns windowing, lifecycle, and input event delivery on mobile (and is desktop-capable, which is where it is built and tested first). Bindings are pure P/Invoke, AOT/trim-safe (vendored flibitijibibo-style SDL3-CS or ppy.SDL3-CS). GLFW remains the desktop default until SDL3 proves out.

This entails engine changes that hold for any container choice:

1. **Game-loop inversion of control** — the engine loop gains a per-frame `Tick(dt)` mode driven by SDL3 main callbacks (`SDL_AppInit`/`SDL_AppIterate`/`SDL_AppEvent`/`SDL_AppQuit`); desktop keeps the blocking loop.
2. **Touch input model** in `KeenEyes.Input.Abstractions`: touch devices with per-finger points (device ID, finger ID, normalized position, deltas, pressure, phase including **Canceled**), suppression of touch→mouse double-delivery, engine-side gesture recognition (SDL3 core has none).
3. **Text-input session API** (IME begin/end, composition events, keyboard-occlusion rect).
4. **Lifecycle surface**: background/foreground, GL surface loss/recreation, low-memory; on Android, `DID_ENTER_BACKGROUND` is the save point (TERMINATING is not guaranteed).

### Graphics: GLES 3.0 first, SDL3 GPU second

- **Primary backend**: `KeenEyes.Graphics.Gles` on `Silk.NET.OpenGLES` — system GLES/EGL on Android (~100% device reach), **ANGLE-on-Metal on iOS** (the shipped app is a Metal app; Apple's GLES deprecation is irrelevant). The KESL compiler gains a **GLSL ES 300 emitter** (dialect variant of the existing GLSL generator). This is a port of the GL 3.3 renderer, not a rewrite.
- **Strategic second backend**: **SDL3 GPU** (first-class Metal on iOS, Vulkan on Android), gated on a KESL **SPIR-V emitter** (offline pipeline: KESL → SPIR-V → SDL_shadercross → MSL/DXIL). Adopted when GLES 3.0's feature ceiling binds or the ANGLE-on-iOS path fails verification; iOS retires GLES first.
- **Go/no-go verification item**: an `ios-arm64` static ANGLE build (check `Silk.NET.OpenGLES.ANGLE.Native` RID coverage; fall back to building ANGLE).

### Audio: miniaudio on mobile

A new `KeenEyes.Audio.MiniAudio` backend wraps vanilla miniaudio via a small **engine-owned `[LibraryImport]`/`[UnmanagedCallersOnly]` binding** (no third-party wrapper dependency). Public domain license (static-link freely on iOS), native AAudio on Android, CoreAudio on iOS, built-in WAV/FLAC/MP3/OGG decode and streaming; its source/listener spatial model maps ~1:1 onto the OpenAL-shaped abstraction. Desktop stays on OpenAL Soft for now; converging desktop onto miniaudio (one backend, no LGPL anywhere) is a candidate follow-up once the mobile backend is proven.

### Packaging

- **iOS:** `net10.0-ios` + `PublishAot=true`, statically linked natives (SDL3, ANGLE, miniaudio), standard Xcode signing.
- **Android:** `net10.0-android` + MonoAOT now (store-ready; zero-reflection code runs identically on Mono), flipping to `PublishAot` on the same TFM when it exits experimental; `linux-bionic-arm64` + custom Gradle packaging is the escape hatch.

### macOS: build host and beneficiary

macOS appears in this plan in two distinct roles. Both are recorded here because neither is optional and neither was previously stated.

**1. macOS is a hard prerequisite for the iOS phases.** `net10.0-ios` builds, `PublishAot` for iOS, and the simulator work in the ANGLE spike all require a Mac running Xcode — Apple's toolchain is not available on other hosts, and "standard Xcode signing" above presumes it. Concretely:

- Phases 3 (iOS half) and 5 cannot be executed, and the #1346 go/no-go cannot be answered, without Mac hardware or a macOS CI runner.
- The minimum macOS and Xcode versions are those the .NET 10 iOS workload requires; pin the exact pair in this ADR when the workload is first installed rather than guessing here.
- CI has no macOS runner for iOS packaging today. The existing macOS jobs are the Native AOT publish in `aot-compatibility.yml` (`osx-arm64`) and the full test-suite run added by #1356 — neither installs the iOS workload. Phase 5 needs that gap closed or an explicit "local Mac only" decision.

**2. macOS gains a Metal-backed renderer as a consequence of the iOS work.** Apple deprecated OpenGL; the desktop backend is GL 3.3 core, which macOS still honours only up to its 4.1 ceiling. `KeenEyes.Graphics.Gles` on ANGLE (phase 3) is Metal-backed, so it is also a macOS-viable renderer — the iOS investment hands desktop macOS a path off deprecated GL for free.

*Open decision:* whether `Graphics.Gles` becomes the macOS **default** over GL 3.3, and when. The recommendation is to keep GL 3.3 as the macOS default until the GLES backend has shipped on mobile and reached visual parity, then re-evaluate — treating a macOS switch as its own migration with its own evidence, not as an implicit side effect of the iOS phases. This needs confirming before phase 3 concludes.

**Distribution prerequisites (not required for local testing, blocking for shipping).** Signing a macOS build requires an Apple Developer account, and *notarization plus the hardened runtime are mandatory* for distribution — see [cross-platform deployment research](../research/cross-platform-deployment.md), which covers macOS RIDs (`osx-x64`, `osx-arm64`), per-platform Native AOT startup measurements, and the notarization requirement in detail. That report predates this ADR and remains the reference; it is not restated here.

**Current macOS status (measured, 2026-07-28).** The desktop engine already substantially runs on Apple Silicon, which lowers the risk of both roles above:

- 14,892 tests executed on `macos-latest`; 14,811 passed. All 21 failures shared one root cause — the Unix domain-socket path limit on pipe names (#1359).
- macOS natives already ship in build output (`runtimes/osx-arm64`, `runtimes/osx-x64`: `libglfw.3.dylib`, `libopenal.dylib`).
- The GL context request is already macOS-correct: `WindowOptions.Default` resolves to OpenGL / Core / ForwardCompatible / 3.3, which is what macOS requires above 2.1, and sits inside its 4.1 ceiling.
- Two macOS-specific defects were found and fixed while establishing this: HiDPI viewport sizing (#1355) and the pipe path limit (#1359).

### Sequencing

1. `KeenEyes.Platform.Sdl3` + loop inversion (desktop-testable).
2. Touch/lifecycle/IME abstraction additions.
3. GLES backend + KESL GLSL ES emitter; verify iOS ANGLE early.
4. miniaudio backend + native build recipes (iOS static `.a`, Android per-ABI `.so`).
5. Packaging proof with a sample on both platforms.
6. Later: KESL SPIR-V emitter → SDL3 GPU backend; re-evaluate desktop audio convergence; track Android Native AOT and Silk.NET 3.0.

## Consequences

### Positive

- Mobile arrives without touching shipped desktop code paths — every piece is a new backend behind an existing seam, validating the ADR-005 abstraction investment.
- The zero-reflection/AOT discipline (ADR-004) pays off directly: every chosen dependency is pure P/Invoke and the iOS Native AOT pipeline needs no engine changes.
- SDL3 alignment matches where the C# ecosystem converged (FNA, osu!framework, Silk.NET 3.0's own direction), so the backend stays forward-compatible with future stacks.
- The GLES-first path defers the SPIR-V toolchain; shipping mobile does not block on new shader infrastructure.
- Tick-based loop inversion also benefits the editor and headless/CI hosting (an externally driven frame step is broadly useful).
- Desktop macOS gains a Metal-backed renderer for free via the iOS GLES/ANGLE work, giving the engine an exit from deprecated OpenGL on Apple hardware without a macOS-specific project.

### Negative

- Three new native artifacts to build and ship per mobile ABI (SDL3, ANGLE on iOS, miniaudio) — a cross-compile/CI pipeline the repo does not have today.
- GLES 3.0 caps the mobile renderer below desktop GL 3.3 in places (no geometry shaders, UBO alignment care) until the SDL3 GPU backend lands; SDL_GPU on Android then imposes a Vulkan device floor (~85%, rising).
- Two audio backends (OpenAL desktop, miniaudio mobile) until/unless desktop converges; miniaudio lacks OpenAL Soft's HRTF/EFX (currently unexposed by the engine).
- Android runs MonoAOT interim — two runtime configurations to support until workload Native AOT stabilizes.
- The engine owns gesture recognition and virtual-gamepad UI; SDL3 provides only raw fingers.
- Betting against Silk.NET 3.0's windowing means a possible future migration if it ships and wins — mitigated by it also being SDL3-based.
- The iOS phases are gated on Mac hardware or a macOS CI runner with the iOS workload, neither of which the project has today; that is a scheduling dependency, not just a tooling detail.

## Alternatives Considered

- **Silk.NET 2.x SDL mobile windowing** — exists (2.22+ `SilkMobile.RunApp`) but SDL2-based, with a known-unfixed iOS blocking-run-loop defect (deferred to 3.0), no touch modeling, and near-zero shipping record. Prototype-grade only.
- **Wait for Silk.NET 3.0** — unbounded timeline (preview feed only as of July 2026); its windowing is itself SDL3-backed, so building on SDL3 now shares the concepts anyway.
- **Vulkan + MoltenVK as the primary graphics backend** — the "correct" explicit API, but a full renderer rewrite (memory, sync, pipelines) with per-device Android driver quirks; SDL3 GPU reaches the same native APIs (Metal/Vulkan) for a fraction of the effort.
- **WebGPU native (wgpu/Dawn)** — healthy upstream but chronic `webgpu.h` churn (Silk.NET.WebGPU pinned to an older header) and a thin C#-on-mobile production record. Revisit ~2027.
- **bgfx** — capable native library, but C# bindings are stale/unpackaged and its mandatory bgfx-dialect shader compiler fights KESL.
- **Veldrid (ppy or other forks)** — proves the architecture (osu! ships on both stores) but the original is abandoned and each fork serves one project; KeenEyes already owns an equivalent abstraction layer.
- **OpenAL Soft on mobile** — viable technically (Oboe/CoreAudio backends), but requires building all mobile natives in-house (upstream C# native package is desktop-only) plus LGPL compliance via embedded dynamic frameworks on iOS. Chosen only if HRTF/EFX or backend uniformity outweighs that cost.
- **FAudio** — superbly maintained and AOT-proven, but XAudio2-shaped (impedance mismatch with the OpenAL-style abstraction), no OGG/MP3 decode, Android not a first-class target.
- **SDL3_mixer** — coherent with an SDL3 stack but only stable since March 2026 with a less proven spatial model; re-evaluate after SDL3 adoption.
- **SoLoud** — dormant since mid-2024.

---

## Changelog

- **v2 — 2026-07-28 (#1354):** Added the macOS section covering both roles the plan depends on and neither of which was stated: macOS as the hard prerequisite for the iOS phases (Xcode requirement, version pinning deferred to workload install, no macOS CI runner for iOS packaging today), and macOS as beneficiary of the GLES/ANGLE work — recording as an open decision whether `Graphics.Gles` supersedes GL 3.3 as the macOS default, with a recommendation to defer until mobile parity is proven. Named notarization / hardened runtime / Apple Developer account as distribution prerequisites, referencing the existing cross-platform deployment research. Recorded measured macOS status (14,811 of 14,892 tests passing on Apple Silicon; natives and GL context request already correct; #1355 and #1359 fixed en route). Consequences updated for the free Metal path and the Mac-hardware scheduling dependency.
- **v1 — 2026-07-27 (#1344–#1350):** Proposed — SDL3 platform container, GLES 3.0-first graphics (ANGLE-on-Metal on iOS) with SDL3 GPU as the strategic successor, miniaudio mobile audio, and `net10.0-ios PublishAot` / `net10.0-android` packaging, based on the July 2026 mobile platform research.
