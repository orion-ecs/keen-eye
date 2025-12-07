# Asset Loading and Management - Research Report

**Date:** December 2024
**Purpose:** Evaluate approaches to loading and managing game assets (textures, models, audio, fonts) in a cross-platform C# engine

## Executive Summary

For a cross-platform C# game engine, **pure managed libraries without native dependencies** should be preferred for portability and simplicity. The recommended stack is:

- **Textures:** StbImageSharp (runtime) + BCnEncoder.NET (asset pipeline)
- **Models:** SharpGLTF (glTF as primary runtime format)
- **Fonts:** FontStashSharp (built on StbTrueTypeSharp)
- **Audio:** NVorbis (Ogg Vorbis) + platform-specific playback

This combination provides full cross-platform support, no native dependencies, and active maintenance.

---

## Image Loading Libraries

### StbImageSharp

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/StbSharp/StbImageSharp](https://github.com/StbSharp/StbImageSharp) |
| **NuGet** | [StbImageSharp](https://www.nuget.org/packages/StbImageSharp/) |
| **Latest Version** | 2.30.15 (September 2024) |
| **License** | MIT/Public Domain |

**Strengths:**
- **Pure C#** - No native dependencies, port of stb_image.h
- **Lightweight** - Minimal overhead, focused on loading only
- **Format support** - PNG, JPG, BMP, TGA, GIF, PSD, HDR
- **Safe version available** - SafeStbImageSharp for no-unsafe contexts
- **Game engine proven** - Used by MonoGame, Unity extensions

**Weaknesses:**
- No DDS/KTX support (GPU-compressed formats)
- No image manipulation features
- No encoding support (loading only)

**API Example:**
```csharp
using StbImageSharp;

using var stream = File.OpenRead("texture.png");
var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

// image.Data contains raw RGBA bytes
// image.Width, image.Height for dimensions
```

---

### SixLabors.ImageSharp

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/SixLabors/ImageSharp](https://github.com/SixLabors/ImageSharp) |
| **NuGet** | [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) |
| **Latest Version** | 3.x (active development) |
| **License** | Apache 2.0 (Six Labors Split License for commercial) |

**Strengths:**
- **Full-featured** - Loading, saving, processing, manipulation
- **Pure managed** - No native dependencies
- **SIMD optimized** - Near-native performance with hardware acceleration
- **Extensive format support** - PNG, JPEG, GIF, BMP, TIFF, WebP, and more
- **Image processing** - Resize, crop, filters, transformations

**Weaknesses:**
- **Heavier** - More overhead than StbImageSharp for simple loading
- **Licensing concerns** - Commercial license required for revenue > $1M
- **Overkill for runtime** - Better suited for asset pipeline tools

**Best Use Case:** Asset pipeline tooling, thumbnail generation, image processing

---

### BCnEncoder.NET

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/Nominom/BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) |
| **NuGet** | [BCnEncoder.Net](https://www.nuget.org/packages/BCnEncoder.Net/) |
| **Latest Version** | 2.2.1 |
| **License** | MIT / Unlicense (dual) |

**Strengths:**
- **GPU texture compression** - BC1-3 (DXT), BC4-5 (RGTC), BC6-7 (BPTC)
- **Pure C#** - .NET Standard 2.1 compatible
- **Output formats** - KTX and DDS file formats
- **Quality settings** - Fast, Balanced, BestQuality compression modes
- **ImageSharp integration** - Works with SixLabors.ImageSharp

**Weaknesses:**
- Requires ImageSharp as dependency
- Compression is CPU-intensive (batch offline, not runtime)

**Best Use Case:** Asset pipeline for cooking textures to GPU-compressed formats

**API Example:**
```csharp
using BCnEncoder.Encoder;
using BCnEncoder.Shared;

using var image = Image.Load<Rgba32>("texture.png");
var encoder = new BcEncoder
{
    OutputOptions =
    {
        GenerateMipMaps = true,
        Quality = CompressionQuality.Balanced,
        Format = CompressionFormat.Bc1,
        FileFormat = OutputFileFormat.Ktx
    }
};

using var output = File.Create("texture.ktx");
encoder.EncodeToStream(image, output);
```

---

## Model Loading Libraries

### SharpGLTF

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/vpenades/SharpGLTF](https://github.com/vpenades/SharpGLTF) |
| **NuGet** | [SharpGLTF.Core](https://www.nuget.org/packages/SharpGLTF.Core) |
| **Stars** | 538 |
| **Latest Version** | 1.0.5 (October 2024) |
| **License** | MIT |

**Strengths:**
- **Pure C#** - No native dependencies
- **glTF 2.0 complete** - Full spec support including extensions
- **Read and write** - Both loading and saving models
- **Runtime helpers** - SharpGLTF.Runtime namespace for GPU upload
- **Active development** - Regular updates through 2024
- **Toolkit available** - SharpGLTF.Toolkit for model manipulation

**Weaknesses:**
- glTF format only (but glTF is the recommended runtime format)
- More complex API than simple OBJ loaders

**Why glTF?**
- Open standard maintained by Khronos Group
- Stores mesh data in GPU-ready binary format
- 5x smaller and 10x faster to load than text formats
- Supports PBR materials, animations, skinning
- Called the "JPEG of 3D"

**API Example:**
```csharp
using SharpGLTF.Schema2;

var model = ModelRoot.Load("model.gltf");

foreach (var mesh in model.LogicalMeshes)
{
    foreach (var primitive in mesh.Primitives)
    {
        var positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
        var indices = primitive.GetIndexAccessor().AsIndicesArray();
        // Upload to GPU...
    }
}
```

---

### AssimpNet

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/assimp/assimp-net](https://github.com/assimp/assimp-net) |
| **NuGet** | [AssimpNet](https://www.nuget.org/packages/AssimpNet) |
| **Latest Version** | 4.1.0 (stable), 5.0.0-beta1 (preview) |
| **License** | BSD 3-Clause |

**Strengths:**
- **40+ formats** - FBX, OBJ, glTF, 3DS, Collada, and many more
- **Industry standard** - Used widely in game development
- **Post-processing** - Mesh optimization, tangent generation, triangulation
- **High-level API** - Clean C# API wrapping native library

**Weaknesses:**
- **Native dependency** - Requires Assimp native DLL per platform
- **Large binary** - Native library adds significant size
- **Distribution complexity** - Must bundle platform-specific binaries
- **Not mobile-friendly** - Native compilation challenges

**Best Use Case:** Asset pipeline/import tool, not runtime loading

**Recommendation:** Use AssimpNet in asset pipeline to convert formats → glTF, then use SharpGLTF at runtime

---

### Format Comparison: glTF vs FBX

| Aspect | glTF | FBX |
|--------|------|-----|
| **Licensing** | Open (Khronos) | Proprietary (Autodesk) |
| **Runtime Loading** | Excellent (GPU-ready) | Requires processing |
| **File Size** | Smaller (binary buffers) | Larger |
| **Animation** | Good | Excellent (complex rigs) |
| **PBR Materials** | Native support | Historic lighting model |
| **C# Libraries** | Pure managed available | Requires native SDK |
| **Recommendation** | Runtime format | Authoring exchange format |

---

## Font Loading Libraries

### FontStashSharp

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/FontStashSharp/FontStashSharp](https://github.com/FontStashSharp/FontStashSharp) |
| **NuGet** | [FontStashSharp](https://www.nuget.org/packages/FontStashSharp) |
| **License** | MIT |

**Strengths:**
- **Pure C#** - Built on StbTrueTypeSharp (no native deps)
- **Dynamic atlas** - Glyphs rendered on demand, no character range needed
- **Multiple fonts** - One FontSystem can hold Latin + Japanese + Emoji
- **Rich text** - Colored text, blur, stroke effects
- **Game engine integration** - MonoGame, FNA, Stride backends
- **Custom renderers** - Can implement for any graphics API

**Weaknesses:**
- Designed around SpriteBatch-style rendering
- May need custom renderer for raw OpenGL/Vulkan

**API Example:**
```csharp
using FontStashSharp;

var fontSystem = new FontSystem();
fontSystem.AddFont(File.ReadAllBytes("arial.ttf"));

var font = fontSystem.GetFont(24); // 24pt size
font.DrawText(spriteBatch, "Hello World", new Vector2(100, 100), Color.White);
```

---

### StbTrueTypeSharp

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/StbSharp/StbTrueTypeSharp](https://github.com/StbSharp/StbTrueTypeSharp) |
| **License** | MIT/Public Domain |

**Strengths:**
- **Pure C#** - Port of stb_truetype.h
- **Minimal** - Just font loading and glyph rasterization
- **Foundation for FontStashSharp** - Proven by higher-level libraries

**Weaknesses:**
- Low-level API - Need to build atlas management yourself

**Recommendation:** Use FontStashSharp which wraps this with a nice API

---

### SharpFont (FreeType Wrapper)

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/Robmaister/SharpFont](https://github.com/Robmaister/SharpFont) |
| **Status** | **Unmaintained - Seeking maintainer** |
| **Last Release** | 4.0.1 (2016) |

**Issues:**
- No releases in 8+ years
- Native FreeType dependency
- Unclear maintenance future

**Recommendation:** **Avoid** - Use FontStashSharp instead

---

## Audio Loading Libraries

### NVorbis

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/NVorbis/NVorbis](https://github.com/NVorbis/NVorbis) |
| **NuGet** | [NVorbis](https://www.nuget.org/packages/NVorbis/) |
| **Latest Version** | 0.10.5 |
| **License** | MIT |

**Strengths:**
- **Pure C#** - Fully managed Ogg Vorbis decoder
- **No P/Invoke** - Runs in partial trust environments
- **Streaming support** - Efficient for large audio files
- **.NET Standard 2.0** - Cross-platform compatible
- **NAudio integration** - NAudio.Vorbis package available

**Weaknesses:**
- Ogg Vorbis only (no MP3, no WAV decoding)
- Decoding only (no encoding)

**API Example:**
```csharp
using NVorbis;

using var vorbis = new VorbisReader("music.ogg");
var buffer = new float[vorbis.Channels * vorbis.SampleRate]; // 1 second buffer
int samplesRead = vorbis.ReadSamples(buffer, 0, buffer.Length);
// Submit to audio API...
```

---

### NAudio

| Attribute | Value |
|-----------|-------|
| **Repository** | [github.com/naudio/NAudio](https://github.com/naudio/NAudio) |
| **NuGet** | [NAudio](https://www.nuget.org/packages/NAudio/) |
| **Latest Version** | 2.2.1 |
| **License** | MIT |

**Strengths:**
- **Comprehensive** - MP3, WAV, AIFF, WMA decoding
- **Windows audio APIs** - WASAPI, WaveIn/Out, ASIO, MediaFoundation
- **Recording** - Audio capture support
- **Effects** - DSP, mixing, sample rate conversion

**Weaknesses:**
- **Windows only** - P/Invoke to Windows audio APIs
- **Not cross-platform** - Linux/macOS not supported
- Large dependency for just file loading

**Recommendation:** Use only for Windows-specific audio features or asset pipeline tools on Windows

---

### Cross-Platform Audio Alternatives

| Library | Description | Native Deps | Notes |
|---------|-------------|-------------|-------|
| **OpenAL (Silk.NET)** | 3D audio API | Yes | Cross-platform playback |
| **SDL2 Audio** | Simple audio | Yes | Via Silk.NET.SDL |
| **ManagedBass** | BASS wrapper | Yes | Feature-rich, unclear docs |
| **libsoundio-sharp** | Low-level I/O | Yes | Cross-platform, MIT license |

**Strategy:** Use NVorbis for decoding Ogg files, platform-specific APIs for playback (OpenAL via Silk.NET is a good choice for cross-platform 3D audio).

---

## Asset Pipeline Architecture

### Development vs Release Workflow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           DEVELOPMENT                                    │
├─────────────────────────────────────────────────────────────────────────┤
│  Source Assets          →    Asset Pipeline    →    Development Build   │
│  ─────────────              ──────────────         ─────────────────    │
│  • textures/*.png           • Validate            • Load source formats │
│  • models/*.fbx             • (Optional cook)     • Hot reload support  │
│  • audio/*.wav              • Watch for changes   • Debug-friendly      │
│  • fonts/*.ttf                                                          │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                             RELEASE                                      │
├─────────────────────────────────────────────────────────────────────────┤
│  Source Assets          →    Asset Pipeline    →    Release Build       │
│  ─────────────              ──────────────         ─────────────────    │
│  • textures/*.png           • Compress (BC1-7)    • GPU-compressed tex  │
│  • models/*.fbx             • Convert (→ glTF)    • Binary model data   │
│  • audio/*.wav              • Transcode (→ Ogg)   • Compressed audio    │
│  • fonts/*.ttf              • Generate mipmaps    • PAK archives        │
│                             • Build manifests     • Fast loading        │
└─────────────────────────────────────────────────────────────────────────┘
```

### Asset Cooking Recommendations

| Asset Type | Source Format | Cooked Format | Tool |
|------------|---------------|---------------|------|
| Textures | PNG, TGA, PSD | KTX (BC1-BC7) | BCnEncoder.NET |
| Models | FBX, OBJ | glTF Binary (.glb) | AssimpNet → SharpGLTF |
| Audio | WAV | Ogg Vorbis | External (oggenc) |
| Fonts | TTF/OTF | TTF (unchanged) | — |
| Data | JSON, YAML | Binary | MessagePack, Protobuf |

---

### Virtual File System (VFS)

A VFS provides abstraction over file access, enabling:
- **Mounting** - Map directories or archives to virtual paths
- **Layering** - Mod files override base game files
- **Portability** - Same API for files, archives, network resources

**Recommended C# Library:** [Lurler's VirtualFileSystem](https://github.com/Lurler/VirtualFileSystem)

**VFS Architecture:**
```csharp
public interface IFileSystem
{
    Stream Open(string path);
    bool Exists(string path);
    IEnumerable<string> List(string directory);
}

public class VirtualFileSystem : IFileSystem
{
    private readonly List<IMount> mounts = new();

    public void Mount(string virtualPath, IMount mount) { ... }

    // Later mounts override earlier ones
    // mount("/", baseGame)
    // mount("/", modPack1)  // overrides baseGame files
}
```

**Mount Types:**
- `DirectoryMount` - Physical filesystem directory
- `ZipMount` - ZIP archive (PAK file with ZIP format)
- `MemoryMount` - In-memory files (testing, generated content)

---

### Async Loading Pattern

**Goal:** Load assets without blocking the main thread, upload to GPU when ready.

```
┌──────────┐     ┌────────────────┐     ┌─────────────┐     ┌───────────┐
│  Request │ ──▶ │ Loading Queue  │ ──▶ │ Background  │ ──▶ │  GPU      │
│          │     │                │     │   Thread    │     │  Upload   │
└──────────┘     └────────────────┘     └─────────────┘     └───────────┘
                                              │                    │
                                              │ Decode/Process     │ Main Thread
                                              │                    │ Time-sliced
                                              ▼                    │
                                        ┌───────────┐              ▼
                                        │ CPU Data  │ ──────▶  Ready!
                                        │  Buffer   │
                                        └───────────┘
```

**Key Principles:**

1. **Disk I/O on background threads** - Never block main thread on file reads
2. **Processing on background threads** - Decode images, parse models off main
3. **GPU upload on main thread** - Most APIs require main thread for GL calls
4. **Time-sliced uploads** - Budget upload time per frame (e.g., 2-10ms)
5. **Ring buffer for staging** - Reuse memory for pending uploads

**API Design:**
```csharp
public interface IAssetLoader
{
    // Fire and forget - asset will be ready eventually
    AssetHandle<T> LoadAsync<T>(string path) where T : IAsset;

    // Check if ready
    bool IsLoaded(AssetHandle handle);

    // Get loaded asset (throws if not ready)
    T Get<T>(AssetHandle<T> handle) where T : IAsset;

    // Pump GPU uploads - call once per frame
    void ProcessPendingUploads(TimeSpan budget);
}
```

---

## Decision Matrix

### Image Loading

| Criteria | Weight | StbImageSharp | ImageSharp | BCnEncoder |
|----------|--------|---------------|------------|------------|
| **Pure C#** | High | 10 | 10 | 10 |
| **Performance** | High | 9 | 8 | 7 |
| **Simplicity** | Medium | 9 | 6 | 7 |
| **Format Support** | Medium | 7 | 9 | 6 |
| **Maintenance** | Medium | 8 | 9 | 7 |
| **Use Case** | - | Runtime | Pipeline | Pipeline |

### Model Loading

| Criteria | Weight | SharpGLTF | AssimpNet |
|----------|--------|-----------|-----------|
| **Pure C#** | High | 10 | 3 |
| **Format Support** | Medium | 5 | 10 |
| **Maintenance** | High | 9 | 7 |
| **Runtime Suitable** | High | 10 | 4 |
| **Recommendation** | - | **Runtime** | **Pipeline** |

### Font Loading

| Criteria | Weight | FontStashSharp | SharpFont (FreeType) |
|----------|--------|----------------|----------------------|
| **Pure C#** | High | 10 | 3 |
| **Maintenance** | High | 9 | 1 |
| **Features** | Medium | 8 | 9 |
| **Recommendation** | - | **Use this** | **Avoid** |

### Audio Loading

| Criteria | Weight | NVorbis | NAudio |
|----------|--------|---------|--------|
| **Pure C#** | High | 10 | 5 |
| **Cross-Platform** | High | 10 | 2 |
| **Format Support** | Medium | 4 | 9 |
| **Recommendation** | - | **Decoding** | **Windows pipeline** |

---

## Recommendations Summary

### Runtime Asset Loading Stack

| Asset Type | Library | Why |
|------------|---------|-----|
| **Textures** | StbImageSharp | Pure C#, fast, proven |
| **GPU Textures** | Custom KTX/DDS loader | Load pre-compressed textures |
| **Models** | SharpGLTF | Pure C#, glTF is ideal runtime format |
| **Fonts** | FontStashSharp | Pure C#, dynamic atlas, rich features |
| **Audio** | NVorbis | Pure C#, streaming Ogg Vorbis |

### Asset Pipeline Stack

| Task | Library | Why |
|------|---------|-----|
| **Image Processing** | ImageSharp | Full manipulation API |
| **Texture Compression** | BCnEncoder.NET | GPU formats (BC1-7) |
| **Model Import** | AssimpNet | 40+ formats support |
| **Model Export** | SharpGLTF | Write optimized glTF |
| **Audio Conversion** | External (oggenc) | Quality Vorbis encoding |

### No Native Dependencies (Recommended)

All recommended runtime libraries are pure C#:
- ✅ StbImageSharp
- ✅ SharpGLTF
- ✅ FontStashSharp
- ✅ NVorbis

This ensures:
- Single assembly deployment
- Mobile platform compatibility
- No platform-specific DLLs to manage
- Easier debugging and testing

---

## Integration Notes for KeenEyes

### Asset Components

```csharp
[Component]
public partial struct TextureRef
{
    public AssetHandle<Texture2D> Handle;
}

[Component]
public partial struct MeshRef
{
    public AssetHandle<Mesh> Handle;
}

[Component]
public partial struct AudioSource
{
    public AssetHandle<AudioClip> Clip;
    public float Volume;
    public bool Loop;
}
```

### Asset Loading System

```csharp
public class AssetUploadSystem : SystemBase
{
    private readonly IAssetLoader loader;
    private readonly TimeSpan uploadBudget = TimeSpan.FromMilliseconds(4);

    public override void Update(float deltaTime)
    {
        // Time-slice GPU uploads to avoid frame drops
        loader.ProcessPendingUploads(uploadBudget);
    }
}
```

### Query for Renderables

```csharp
public class RenderSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform, MeshRef, TextureRef>())
        {
            ref readonly var transform = ref World.Get<Transform>(entity);
            ref readonly var mesh = ref World.Get<MeshRef>(entity);
            ref readonly var texture = ref World.Get<TextureRef>(entity);

            if (!AssetLoader.IsLoaded(mesh.Handle)) continue;
            if (!AssetLoader.IsLoaded(texture.Handle)) continue;

            // Submit draw call with loaded assets
            Renderer.Draw(
                AssetLoader.Get(mesh.Handle),
                AssetLoader.Get(texture.Handle),
                transform
            );
        }
    }
}
```

---

## Next Steps

- [ ] Benchmark StbImageSharp vs ImageSharp load times
- [ ] Prototype async asset loader with GPU upload queue
- [ ] Create KTX/DDS loader for GPU-compressed textures
- [ ] Design AssetHandle<T> with staleness detection
- [ ] Evaluate VFS libraries for mod support
- [ ] Create asset pipeline CLI tool

---

## Sources

### Image Loading
- [StbImageSharp GitHub](https://github.com/StbSharp/StbImageSharp)
- [StbImageSharp NuGet](https://www.nuget.org/packages/StbImageSharp/)
- [SixLabors ImageSharp](https://sixlabors.com/products/imagesharp/)
- [BCnEncoder.NET GitHub](https://github.com/Nominom/BCnEncoder.NET)

### Model Loading
- [SharpGLTF GitHub](https://github.com/vpenades/SharpGLTF)
- [SharpGLTF NuGet](https://www.nuget.org/packages/SharpGLTF.Core)
- [AssimpNet GitHub](https://github.com/assimp/assimp-net)
- [glTF Overview (Khronos)](https://www.khronos.org/gltf/)
- [Why glTF (Godot)](https://godotengine.org/article/we-should-all-use-gltf-20-export-3d-assets-game-engines/)

### Font Loading
- [FontStashSharp GitHub](https://github.com/FontStashSharp/FontStashSharp)
- [StbTrueTypeSharp GitHub](https://github.com/StbSharp/StbTrueTypeSharp)
- [SharpFont GitHub](https://github.com/Robmaister/SharpFont)

### Audio Loading
- [NVorbis GitHub](https://github.com/NVorbis/NVorbis)
- [NVorbis NuGet](https://www.nuget.org/packages/NVorbis/)
- [NAudio GitHub](https://github.com/naudio/NAudio)
- [libsoundio-sharp GitHub](https://github.com/atsushieno/libsoundio-sharp)

### Asset Pipeline
- [Asset Pipelines (CMU)](https://graphics.cs.cmu.edu/courses/15-466-f19/notes/asset-pipelines.html)
- [GPU Texture Compression (NVIDIA)](https://developer.nvidia.com/astc-texture-compression-for-game-assets)
- [VirtualFileSystem (C#)](https://github.com/Lurler/VirtualFileSystem)
- [Unity Async Upload Pipeline](https://blog.unity.com/technology/optimizing-loading-performance-understanding-the-async-upload-pipeline)
- [PAK Files VFS](https://simoncoenen.com/blog/programming/PakFiles)
