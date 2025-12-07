# Cross-Platform Build and Deployment - Research Report

**Date:** December 2024
**Purpose:** Evaluate build configurations, native dependencies, and deployment strategies for shipping a C# OpenGL game on Windows, Linux, and macOS

## Executive Summary

This report evaluates cross-platform deployment strategies for a C# game using Silk.NET and OpenGL. **Self-contained deployment with platform-specific native libraries** is the recommended baseline approach, with **Native AOT** as an excellent option for games due to Silk.NET's full AOT compatibility (since v2.18.0). For macOS, notarization and hardened runtime are mandatory requirements.

---

## Target Platforms

| Platform | Runtime Identifier | Architecture |
|----------|-------------------|--------------|
| Windows x64 | `win-x64` | Intel/AMD 64-bit |
| Windows ARM64 | `win-arm64` | ARM64 (Snapdragon, etc.) |
| Linux x64 | `linux-x64` | Intel/AMD 64-bit |
| Linux ARM64 | `linux-arm64` | ARM64 (Raspberry Pi 4+, etc.) |
| macOS Intel | `osx-x64` | Intel 64-bit |
| macOS Apple Silicon | `osx-arm64` | M1/M2/M3/M4 |

---

## .NET Publishing Options

### Comparison Matrix

| Mode | Runtime Required | Package Size | Startup Time | Reflection | Complexity |
|------|-----------------|--------------|--------------|------------|------------|
| **Framework-Dependent** | Yes | ~1-5 MB | Slow (~80ms) | Full | Low |
| **Self-Contained** | No | ~60-80 MB | Slow (~80ms) | Full | Low |
| **ReadyToRun (R2R)** | No | ~80-100 MB | Medium (~50ms) | Full | Low |
| **Native AOT** | No | ~10-15 MB | Fast (~15ms) | Limited | Medium |
| **Single-File + AOT** | No | ~10-15 MB | Fast (~15ms) | Limited | Medium |

### Framework-Dependent Deployment

```bash
dotnet publish -c Release
```

**Pros:**
- Smallest package size
- Automatic runtime updates via system .NET

**Cons:**
- Requires users to install .NET runtime
- Version compatibility issues possible

**Use Case:** Internal tools, developer-focused applications

---

### Self-Contained Deployment

```bash
dotnet publish -c Release --self-contained -r win-x64
```

**Pros:**
- No external dependencies
- Full reflection support
- Predictable runtime version

**Cons:**
- Larger package size (~60-80 MB)
- Slower startup (JIT compilation)

**Use Case:** General distribution, compatibility-focused releases

---

### ReadyToRun (R2R) Compilation

```bash
dotnet publish -c Release --self-contained -r win-x64 -p:PublishReadyToRun=true
```

**Pros:**
- Faster startup than pure self-contained
- Full reflection support
- JIT fallback for dynamic code

**Cons:**
- Larger than self-contained
- Still slower than Native AOT

**Use Case:** Balance between compatibility and startup time

---

### Native AOT Deployment

```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true
```

**Pros:**
- Fastest startup (~15ms vs ~80ms)
- Smallest runtime footprint (~10-15 MB)
- No JIT overhead during gameplay
- Reduced memory usage (up to 40% lower)

**Cons:**
- Limited reflection support
- Cross-OS compilation not supported (must build on target OS)
- All libraries must be AOT-compatible
- Longer build times

**Use Case:** Games, performance-critical applications

---

### Single-File Deployment

```bash
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true
```

Can be combined with Native AOT:

```bash
dotnet publish -c Release -r win-x64 -p:PublishAot=true -p:PublishSingleFile=true
```

**Pros:**
- Clean single executable distribution
- Simpler deployment

**Cons:**
- Extraction overhead on first run (non-AOT)
- Harder to debug deployed applications

---

## Native AOT and Silk.NET Compatibility

### Current Status: Fully Compatible

As of **Silk.NET v2.18.0** (October 2023), the library is fully trimming and AOT compatible.

| Silk.NET Component | AOT Compatible |
|-------------------|----------------|
| OpenGL bindings | Yes |
| Vulkan bindings | Yes |
| GLFW windowing | Yes |
| SDL windowing | Yes |
| Input handling | Yes |
| OpenAL audio | Yes |

### Configuration for AOT

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RuntimeIdentifiers>win-x64;linux-x64;osx-x64;osx-arm64</RuntimeIdentifiers>
    <PublishAot>true</PublishAot>
    <TrimMode>link</TrimMode>
  </PropertyGroup>
</Project>
```

### AOT Considerations for Game Development

1. **No runtime code generation** - Shaders must be pre-compiled or loaded from files
2. **Reflection limitations** - Avoid `Type.GetType()` for loading; use explicit references
3. **Serialization** - Use source-generated serializers (System.Text.Json source gen)
4. **Plugin systems** - Not supported; use compile-time composition instead

---

## Startup Time Benchmarks

Based on 2024 benchmark data:

| Platform | Native AOT | Self-Contained | Improvement |
|----------|-----------|----------------|-------------|
| Windows x64 | ~17 ms | ~80 ms | **4.7x faster** |
| Linux x64 | ~14 ms | ~70 ms | **5x faster** |
| macOS x64 | ~16 ms | ~75 ms | **4.7x faster** |
| Raspberry Pi 4 | ~90 ms | ~530 ms | **5.9x faster** |

**Key Insight:** Native AOT provides consistent ~5x startup improvement across platforms. For games, this translates to near-instant launch times.

---

## Native Dependency Bundling

### Directory Structure

```
MyGame/
├── MyGame.exe (or MyGame on Unix)
├── runtimes/
│   ├── win-x64/
│   │   └── native/
│   │       ├── glfw3.dll
│   │       ├── SDL2.dll
│   │       └── openal32.dll
│   ├── linux-x64/
│   │   └── native/
│   │       ├── libglfw.so.3
│   │       ├── libSDL2-2.0.so.0
│   │       └── libopenal.so.1
│   ├── osx-x64/
│   │   └── native/
│   │       ├── libglfw.3.dylib
│   │       ├── libSDL2-2.0.0.dylib
│   │       └── libopenal.1.dylib
│   └── osx-arm64/
│       └── native/
│           ├── libglfw.3.dylib
│           ├── libSDL2-2.0.0.dylib
│           └── libopenal.1.dylib
```

### NuGet Package References

Silk.NET automatically handles native library bundling via NuGet:

```xml
<ItemGroup>
  <PackageReference Include="Silk.NET.OpenGL" Version="2.22.0" />
  <PackageReference Include="Silk.NET.Windowing" Version="2.22.0" />
  <PackageReference Include="Silk.NET.Input" Version="2.22.0" />
</ItemGroup>
```

### Manual Native Library Inclusion

For custom native libraries:

```xml
<ItemGroup>
  <!-- Windows -->
  <None Include="libs/win-x64/mylib.dll"
        CopyToOutputDirectory="PreserveNewest"
        Link="runtimes/win-x64/native/mylib.dll" />

  <!-- Linux -->
  <None Include="libs/linux-x64/libmylib.so"
        CopyToOutputDirectory="PreserveNewest"
        Link="runtimes/linux-x64/native/libmylib.so" />

  <!-- macOS -->
  <None Include="libs/osx-x64/libmylib.dylib"
        CopyToOutputDirectory="PreserveNewest"
        Link="runtimes/osx-x64/native/libmylib.dylib" />
</ItemGroup>
```

### Known Limitation

Architecture-specific folders (`runtimes/<rid>/native/`) work correctly with NuGet package references but have issues with direct project references. Native libraries are best distributed via NuGet packages for reliable cross-platform support.

---

## Platform-Specific Deployment

### Windows

#### Native Dependencies
- **OpenGL**: Built-in (`opengl32.dll`)
- **GLFW**: Bundle `glfw3.dll` or use Silk.NET NuGet
- **SDL2**: Bundle `SDL2.dll` or use Silk.NET NuGet

#### Distribution Options

| Method | Signing Required | Store Distribution | Complexity |
|--------|-----------------|-------------------|------------|
| **ZIP archive** | No | No | Lowest |
| **MSIX** | Recommended | Windows Store | Medium |
| **MSI** | Recommended | No | Medium |
| **Steam** | No (Steam handles) | Steam | Low |

#### MSIX Packaging

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

MSIX benefits:
- Differential updates (only changed files downloaded)
- Single disk instance (shared files between versions)
- Clean uninstall (no registry residue)

---

### Linux

#### Native Dependencies
- **OpenGL**: Mesa/vendor drivers (system-provided)
- **GLFW**: System package (`libglfw3`) or bundle `libglfw.so.3`
- **SDL2**: System package (`libsdl2-2.0-0`) or bundle `libSDL2-2.0.so.0`

#### Distribution Options

| Method | Sandboxed | Universal | Recommended For |
|--------|-----------|-----------|-----------------|
| **Tarball** | No | Yes* | Simple, Steam |
| **AppImage** | Optional | Yes | Standalone games |
| **Flatpak** | Yes | Yes | Desktop integration |
| **Steam Runtime** | Yes | Steam users | Steam distribution |

*Requires linking against older glibc for compatibility

#### AppImage Creation

```bash
# Install appimagetool
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage

# Create AppDir structure
mkdir -p MyGame.AppDir/usr/bin
mkdir -p MyGame.AppDir/usr/lib
cp -r publish/* MyGame.AppDir/usr/bin/
cp native-libs/*.so MyGame.AppDir/usr/lib/

# Create .desktop file and AppRun script
# ... (see AppImage documentation)

# Build AppImage
./appimagetool-x86_64.AppImage MyGame.AppDir MyGame-x86_64.AppImage
```

#### Steam Runtime Compatibility

For Steam distribution, target **Steam Linux Runtime 3.0 (Sniper)** based on Debian 11:

- Build against older glibc (2.31 or earlier)
- Test with `steam-runtime-sniper` container
- Native .NET runtime works well with Steam Runtime

---

### macOS

#### Native Dependencies
- **OpenGL**: System framework (deprecated but functional)
- **GLFW**: Bundle `libglfw.3.dylib`
- **SDL2**: Bundle `libSDL2-2.0.0.dylib`

**Important:** OpenGL is deprecated on macOS. Consider MoltenVK for Vulkan compatibility in future releases.

#### App Bundle Structure

```
MyGame.app/
├── Contents/
│   ├── Info.plist
│   ├── MacOS/
│   │   └── MyGame (executable)
│   ├── Resources/
│   │   └── MyGame.icns
│   └── Frameworks/
│       ├── libglfw.3.dylib
│       └── libSDL2-2.0.0.dylib
```

#### Code Signing and Notarization (Required)

Since macOS Catalina (10.15), all distributed software must be notarized.

**Requirements:**
1. Apple Developer ID certificate ($99/year)
2. Hardened Runtime enabled
3. Secure timestamp
4. Notarization submission

**Code Signing Command:**

```bash
# Sign all binaries with hardened runtime
codesign --force --options runtime --timestamp \
    --sign "Developer ID Application: Your Name (TEAMID)" \
    --entitlements entitlements.plist \
    MyGame.app

# Sign native libraries
codesign --force --timestamp \
    --sign "Developer ID Application: Your Name (TEAMID)" \
    MyGame.app/Contents/Frameworks/*.dylib
```

**Required Entitlements (entitlements.plist):**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
    "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Required for .NET runtime -->
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <!-- Required for loading unsigned native libraries -->
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <!-- Required for dylib loading -->
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
</dict>
</plist>
```

**Note:** Native AOT applications may not require `allow-jit` since there's no JIT compilation.

**Notarization:**

```bash
# Create ZIP for notarization
ditto -c -k --keepParent MyGame.app MyGame.zip

# Submit for notarization (using notarytool, altool is deprecated)
xcrun notarytool submit MyGame.zip \
    --apple-id "your@email.com" \
    --team-id "TEAMID" \
    --password "@keychain:AC_PASSWORD" \
    --wait

# Staple the ticket
xcrun stapler staple MyGame.app
```

#### Universal Binary (Intel + Apple Silicon)

For a single binary supporting both architectures:

```bash
# Build for both architectures
dotnet publish -c Release -r osx-x64 -o publish/x64
dotnet publish -c Release -r osx-arm64 -o publish/arm64

# Merge executables with lipo
lipo -create \
    publish/x64/MyGame \
    publish/arm64/MyGame \
    -output publish/universal/MyGame

# Verify
lipo -info publish/universal/MyGame
# Output: Architectures in the fat file: MyGame are: x86_64 arm64
```

**Note:** .NET managed DLLs are architecture-independent (IL), but native libraries need both architectures bundled or merged.

---

## CI/CD Configuration

### GitHub Actions Matrix Build

```yaml
name: Cross-Platform Build

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    strategy:
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            artifact: MyGame-win-x64
          - os: ubuntu-latest
            rid: linux-x64
            artifact: MyGame-linux-x64
          - os: macos-latest
            rid: osx-x64
            artifact: MyGame-osx-x64
          - os: macos-latest
            rid: osx-arm64
            artifact: MyGame-osx-arm64

    runs-on: ${{ matrix.os }}

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish
        run: |
          dotnet publish src/MyGame/MyGame.csproj \
            -c Release \
            -r ${{ matrix.rid }} \
            -p:PublishAot=true \
            -p:PublishSingleFile=true \
            -o publish/${{ matrix.rid }}

      - name: Upload Artifact
        uses: actions/upload-artifact@v4
        with:
          name: ${{ matrix.artifact }}
          path: publish/${{ matrix.rid }}

  # macOS signing job (runs on macOS runner)
  sign-macos:
    needs: build
    runs-on: macos-latest
    steps:
      - name: Download macOS builds
        uses: actions/download-artifact@v4
        with:
          pattern: MyGame-osx-*

      - name: Import certificate
        env:
          MACOS_CERTIFICATE: ${{ secrets.MACOS_CERTIFICATE }}
          MACOS_CERTIFICATE_PWD: ${{ secrets.MACOS_CERTIFICATE_PWD }}
        run: |
          echo $MACOS_CERTIFICATE | base64 --decode > certificate.p12
          security create-keychain -p "" build.keychain
          security import certificate.p12 -k build.keychain \
            -P $MACOS_CERTIFICATE_PWD -T /usr/bin/codesign
          security set-key-partition-list -S apple-tool:,apple: \
            -s -k "" build.keychain

      - name: Sign and notarize
        env:
          APPLE_ID: ${{ secrets.APPLE_ID }}
          APPLE_TEAM_ID: ${{ secrets.APPLE_TEAM_ID }}
          APPLE_PASSWORD: ${{ secrets.APPLE_PASSWORD }}
        run: |
          # Sign, create app bundle, notarize, staple
          # (detailed script implementation)
```

### Build Time Considerations

| Platform | Self-Contained | Native AOT |
|----------|---------------|------------|
| Windows | ~30s | ~2-3 min |
| Linux | ~25s | ~2-3 min |
| macOS | ~35s | ~3-4 min |

Native AOT builds are significantly slower but produce better runtime performance.

### Cost Considerations (GitHub Actions)

| Runner | Rate Multiplier |
|--------|-----------------|
| Linux | 1x |
| Windows | 2x |
| macOS | 10x |

**Optimization:** Use Linux for non-platform-specific builds (tests, analysis) and reserve macOS runners for platform-specific builds only.

---

## Avalonia for Launcher/Installer UI

### Suitability Assessment

| Criteria | Score | Notes |
|----------|-------|-------|
| **Cross-platform** | 10/10 | Windows, Linux, macOS native look |
| **Native AOT** | 9/10 | Fully supported since v11 |
| **Complexity** | 7/10 | XAML knowledge required |
| **Community** | 9/10 | Active development, good docs |
| **File size** | 6/10 | Adds ~15-25 MB to distribution |

### Native AOT Configuration for Avalonia

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.0" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.0" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.0" />
  </ItemGroup>

  <!-- AOT trimming roots for Avalonia -->
  <ItemGroup>
    <TrimmerRootAssembly Include="Avalonia.Themes.Fluent" />
  </ItemGroup>
</Project>
```

### Recommendation

**Use Avalonia if:**
- Complex launcher UI required (settings, mod management, news feed)
- Consistent cross-platform look is important
- Team has XAML experience

**Skip Avalonia if:**
- Simple "click to launch" functionality only
- Minimal file size is critical
- Already using Silk.NET windowing (use it for simple launcher too)

---

## OpenGL Version Strategy

### Recommended: OpenGL 4.1

| Version | macOS Support | Feature Level | Coverage |
|---------|--------------|---------------|----------|
| OpenGL 3.3 | Yes | Basic | 99% GPUs |
| **OpenGL 4.1** | **Yes (max)** | **Good** | **98% GPUs** |
| OpenGL 4.5 | No | Modern | 90% GPUs |
| OpenGL 4.6 | No | Latest | 85% GPUs |

**Why 4.1:**
- Maximum version supported on macOS
- Good feature set (tessellation, compute shaders via extensions)
- Wide hardware compatibility
- Single codebase for all platforms

### Future Consideration: Vulkan via MoltenVK

For macOS without OpenGL deprecation concerns:
- MoltenVK translates Vulkan to Metal
- Silk.NET supports Vulkan bindings
- More modern API design

---

## Recommendations Summary

### Publishing Mode

| Use Case | Recommended Mode |
|----------|-----------------|
| **General release** | Native AOT (Silk.NET compatible) |
| **Maximum compatibility** | Self-contained + ReadyToRun |
| **Steam/platform stores** | Native AOT or Self-contained |
| **Debug/development** | Framework-dependent |

### Distribution Format

| Platform | Primary | Secondary |
|----------|---------|-----------|
| **Windows** | ZIP / MSIX | Steam |
| **Linux** | AppImage / Tarball | Flatpak / Steam |
| **macOS** | Notarized .app in DMG | Steam |

### Native AOT Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RuntimeIdentifiers>win-x64;linux-x64;osx-x64;osx-arm64</RuntimeIdentifiers>

    <!-- AOT Configuration -->
    <PublishAot>true</PublishAot>
    <PublishSingleFile>true</PublishSingleFile>
    <TrimMode>link</TrimMode>

    <!-- Optimize for size -->
    <OptimizationPreference>Size</OptimizationPreference>
    <StackTraceSupport>false</StackTraceSupport>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Silk.NET.OpenGL" Version="2.22.0" />
    <PackageReference Include="Silk.NET.Windowing" Version="2.22.0" />
    <PackageReference Include="Silk.NET.Input" Version="2.22.0" />
  </ItemGroup>
</Project>
```

---

## Research Task Checklist

### Completed
- [x] Evaluate .NET publishing options (Framework-Dependent, Self-Contained, Native AOT, Single-File)
- [x] Verify Native AOT compatibility with Silk.NET
- [x] Document startup time benchmarks across publish modes
- [x] Document native dependency bundling per platform
- [x] Research Apple Silicon builds and universal binaries
- [x] Research code signing and notarization (macOS)
- [x] Evaluate Avalonia for launcher/installer UI
- [x] Document CI/CD strategies for cross-platform builds
- [x] Research Linux distribution formats (AppImage, Flatpak, Steam Runtime)
- [x] Research Windows distribution formats (MSIX, MSI)

### For Future Investigation
- [ ] Benchmark actual game startup times with Silk.NET + Native AOT
- [ ] Test Steam Runtime compatibility with .NET Native AOT
- [ ] Evaluate MoltenVK performance vs OpenGL on macOS
- [ ] Measure memory usage differences between publish modes
- [ ] Test ARM64 performance on Windows (Snapdragon laptops)

---

## Sources

### Microsoft Documentation
- [.NET Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
- [.NET Application Publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [Native Files in .NET Packages](https://learn.microsoft.com/en-us/nuget/create-packages/native-files-in-net-packages)
- [.NET 10 SDK New Features](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk)
- [macOS Notarization Issues](https://learn.microsoft.com/en-us/dotnet/core/install/macos-notarization-issues)
- [MSIX Deployment](https://learn.microsoft.com/en-us/windows/msix/desktop/managing-your-msix-deployment-enterprise)

### Silk.NET
- [Silk.NET GitHub](https://github.com/dotnet/Silk.NET)
- [Silk.NET Documentation](https://dotnet.github.io/Silk.NET/)
- [Silk.NET NuGet](https://www.nuget.org/packages/Silk.NET/)

### Avalonia
- [Avalonia Native AOT Deployment](https://docs.avaloniaui.net/docs/deployment/native-aot)
- [Avalonia Documentation](https://docs.avaloniaui.net/)

### Apple Developer
- [Building Universal Binaries](https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary)
- [Notarizing macOS Software](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)

### Linux Distribution
- [AppImage Documentation](https://docs.appimage.org/)
- [Flatpak](https://flatpak.org/)
- [Steam Linux Runtime](https://gitlab.steamos.cloud/steamrt/steam-runtime-tools)
- [Steam Runtime Guide for Developers](https://gitlab.steamos.cloud/steamrt/steam-runtime-tools/-/blob/main/docs/slr-for-game-developers.md)

### Community Resources
- [State of Native AOT in .NET 10](https://code.soundaranbu.com/state-of-nativeaot-net10)
- [.NET Native AOT Explained](https://blog.ndepend.com/net-native-aot-explained/)
- [Avalonia Native AOT on Windows](https://dev.to/chuongmep/avaloniaui-native-aot-deployment-on-windows-2jg1)
- [GitHub Actions Matrix Builds](https://ncorti.com/blog/howto-github-actions-build-matrix)
