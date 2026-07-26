# ADR-006: Custom MSBuild SDK for KeenEyes Projects

**Status:** Accepted
**Revision:** v2
**Implementation:** Shipped
**First accepted:** 2025-12-18 · **Last amended:** 2026-07-26

## Context

External consumers of KeenEyes must currently configure their projects with significant boilerplate:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsAotCompatible>true</IsAotCompatible>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="KeenEyes.Core" Version="0.1.0" />
    <PackageReference Include="KeenEyes.Generators" Version="0.1.0"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

This creates several problems:

1. **Error-prone setup** - Forgetting `OutputItemType="Analyzer"` on generators causes silent failures
2. **Version coupling** - Core and Generators versions must match but are specified separately
3. **Convention drift** - Users may not enable nullable, AOT compatibility, or C# 13 features
4. **No tooling hooks** - No standard way to detect KeenEyes projects or their configuration
5. **Editor integration blocked** - A future visual editor needs to identify and introspect KeenEyes projects

## Decision

Create custom MSBuild SDK packages that encapsulate KeenEyes conventions and dependencies:

### SDK Flavors

| Package | Target Use Case | Default Dependencies |
|---------|-----------------|---------------------|
| `KeenEyes.Sdk` | Games and applications | Core + Generators + Shaders.Generator |
| `KeenEyes.Sdk.Plugin` | Plugin libraries | Abstractions + Generators (opt-in Common) |
| `KeenEyes.Sdk.Library` | Reusable ECS libraries | Core + Generators (private) |

### Minimal Project File

With the SDK, a game project becomes:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
</Project>
```

The SDK automatically:
- Imports `Microsoft.NET.Sdk` as the base
- Sets `TargetFramework=net10.0`, `LangVersion=preview`, `Nullable=enable`
- Sets `OutputType=Exe` (for main SDK) or `Library` (for Plugin/Library SDKs)
- Enables `IsAotCompatible=true`
- References appropriate KeenEyes packages with correct attributes
- Defines custom ItemGroup types and auto-detects KeenEyes file types for build-time processing

Package references per flavor:

- `KeenEyes.Sdk` references `KeenEyes.Core`, `KeenEyes.Generators` (as analyzer), and
  `KeenEyes.Shaders.Generator` (as analyzer, for KESL shader compilation). Each is individually
  opt-out via `IncludeKeenEyesCore`, `IncludeKeenEyesGenerators`, and `IncludeKeenEyesShaders`.
- `KeenEyes.Sdk.Plugin` references `KeenEyes.Abstractions` (opt-out via
  `IncludeKeenEyesAbstractions`) and `KeenEyes.Generators`, with opt-in `KeenEyes.Common`
  (`IncludeKeenEyesCommon=true`).
- `KeenEyes.Sdk.Library` references `KeenEyes.Core` and `KeenEyes.Generators` with
  `PrivateAssets=all` on Generators, so the analyzer does not flow to library consumers.

### Custom ItemGroups

The SDK defines six item types for editor and build pipeline integration:

| Item Type | File Extension | Handling |
|-----------|----------------|----------|
| `KeenEyesScene` | `.kescene` | Auto-detected, fed to source generators |
| `KeenEyesPrefab` | `.keprefab` | Auto-detected, fed to source generators |
| `KeenEyesWorld` | `.keworld` | Auto-detected, fed to source generators |
| `KeenEyesShader` | `.kesl` | Auto-detected, fed to the KESL shader generator |
| `KeenEyesAsset` | any | Copied to the output directory by the `CopyKeenEyesAssets` target |
| `KeenEyesSystem` | — | ItemDefinitionGroup metadata for system declarations |

Scene, prefab, world, and shader files are auto-detected by glob in `Sdk.targets` and forwarded
as `AdditionalFiles` to the KeenEyes source generators, so consumers do not declare these
ItemGroups manually — dropping a `.kescene` or `.kesl` file anywhere in the project is enough.
Explicit declarations remain possible for `KeenEyesAsset` items:

```xml
<!-- Game assets (auto-copied to output) -->
<ItemGroup>
  <KeenEyesAsset Include="Assets/**/*" />
</ItemGroup>
```

### Project Metadata

The SDK generates `keeneyes.project.json` in the output directory:

```json
{
  "projectName": "MyGame",
  "projectType": "Game",
  "sdkVersion": "0.1.0",
  "coreVersion": "0.1.0",
  "targetFramework": "net10.0",
  "outputType": "Exe",
  "isAotCompatible": true,
  "features": ["ECS", "SourceGenerators", "AOT"]
}
```

The Library SDK variant additionally records `isPackable`. Every build also writes
`keeneyes.version.json` (SDK, Core, and minimum-Core versions) to the intermediate output path
for version-compatibility checking.

This enables:
- Editor project detection without loading the full MSBuild graph
- Version compatibility checking
- Upgrade path recommendations
- Feature flag queries

Editor-side consumption of these files is not yet implemented (see Future Considerations).

### Version Properties

The SDK exposes version metadata for tooling:

| Property | Description |
|----------|-------------|
| `$(KeenEyesSdkVersion)` | SDK package version |
| `$(KeenEyesCoreVersion)` | KeenEyes.Core version included |
| `$(KeenEyesMinimumCoreVersion)` | Minimum compatible Core version |
| `$(KeenEyesProjectType)` | Project type (Game, Plugin, Library) |
| `$(IsKeenEyesProject)` | Always `true` for SDK projects |

## Alternatives Considered

### Option 1: NuGet Meta-Package Only

Create a single `KeenEyes` package that references Core and Generators:

```xml
<PackageReference Include="KeenEyes" Version="0.1.0" />
```

**Rejected because:**
- Cannot set project properties (TargetFramework, LangVersion, etc.)
- Cannot define custom ItemGroup types
- No project metadata generation
- Generators still need special attributes that meta-packages can't enforce

### Option 2: dotnet new Templates Only

Rely solely on `dotnet new keeneyes-game` templates:

**Rejected because:**
- Templates are point-in-time snapshots; don't receive updates
- No version tracking or upgrade tooling
- Custom ItemGroups would need manual documentation
- Editor integration still needs project detection

### Option 3: MSBuild Props/Targets Package

Create a package with `.props` and `.targets` files instead of an SDK:

```xml
<PackageReference Include="KeenEyes.Build" Version="0.1.0" />
```

**Rejected because:**
- Users still need to reference Core, Generators separately
- Props/targets are added to existing SDK, not replacements
- Cannot override TFM or LangVersion defaults cleanly
- Less elegant than `Sdk="..."` attribute

## Consequences

### Positive

- **Minimal boilerplate** - New projects need only the SDK reference
- **Convention enforcement** - C# 13, nullable, AOT enabled by default
- **Version coupling** - SDK version implies compatible package versions
- **Editor foundation** - Project detection, metadata, custom item types ready
- **Upgrade tooling** - Version properties enable compatibility checking
- **Asset pipeline active** - Scene, prefab, world, and shader files auto-flow to source generators at build time; assets auto-copy to output

### Negative

- **SDK resolution complexity** - Requires NuGet SDK resolution (not just packages)
- **Version lock-in** - SDK version determines all package versions
- **Additional packages** - Three more packages to publish and maintain
- **Learning curve** - Users must understand SDK vs regular packages

### Neutral

- **Opt-in architecture** - Users can still use regular `Microsoft.NET.Sdk` with explicit references
- **Internal usage optional** - Monorepo samples continue using ProjectReferences

## Implementation Notes

### Package Structure

```
KeenEyes.Sdk/
├── Sdk/
│   ├── Sdk.props    # Imported first (sets defaults)
│   └── Sdk.targets  # Imported last (adds references, targets)
├── README.md
└── KeenEyes.Sdk.csproj
```

### SDK Props/Targets Flow

1. **Sdk.props** runs before user's PropertyGroups
   - Sets conditional defaults (only if not already set)
   - Imports `Microsoft.NET.Sdk.props`
   - Defines custom ItemDefinitionGroups

2. **User's .csproj content** runs
   - Can override any SDK defaults
   - Can add custom ItemGroups

3. **Sdk.targets** runs after user's content
   - Imports `Microsoft.NET.Sdk.targets`
   - Adds PackageReferences based on Include properties
   - Defines build targets for asset processing
   - Generates project metadata JSON

### Opting Out

Users can disable automatic references:

```xml
<PropertyGroup>
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
  <IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>
  <IncludeKeenEyesShaders>false</IncludeKeenEyesShaders>
</PropertyGroup>
```

The Plugin SDK additionally offers `IncludeKeenEyesAbstractions` (opt-out) and
`IncludeKeenEyesCommon` (opt-in, default `false`).

## Future Considerations

1. **Editor integration** - Read `keeneyes.project.json` for project discovery. Not yet implemented: the file is generated on every build, but no editor code consumes it.
2. **Asset processing** - Build targets for scene/prefab compilation. Partially shipped: scene, prefab, world, and shader files are auto-detected and fed to source generators (see Custom ItemGroups); dedicated compilation targets beyond that remain future work.
3. **Analyzers** - Include KeenEyes-specific Roslyn analyzers. Not yet implemented.
4. **Upgrade CLI** - `dotnet keeneyes upgrade` command using version metadata. Not yet implemented: `keeneyes.version.json` carries the data, but no CLI command reads it.

## Related

- [ADR-009](009-kesl-shader-language.md) (KESL shader language) — the game SDK ships the KESL shader generator wiring (`KeenEyes.Shaders.Generator` auto-reference and `.kesl` auto-detection).

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** No status change (Accepted since 2025-12-18); Implementation: Shipped — all three SDK packages, convention defaults, opt-outs, version properties, and metadata generation are in place; templates and docs consume them. Decision amended to as-built reality: item types grew from four to six (`KeenEyesShader`, `KeenEyesSystem`) and are auto-detected by glob and fed to source generators as `AdditionalFiles` rather than declared manually; the game SDK also auto-references `KeenEyes.Shaders.Generator` (opt-out `IncludeKeenEyesShaders`); `keeneyes.project.json` gained `outputType` (and `isPackable` for Library) and a second `keeneyes.version.json` is written to the intermediate output path. Noted that no editor code consumes the metadata files yet; added Related link to ADR-009.
- **v1 — 2025-12-18 (3b6053a1):** Accepted — Introduce KeenEyes.Sdk / KeenEyes.Sdk.Plugin / KeenEyes.Sdk.Library MSBuild SDKs to replace consumer boilerplate with convention defaults, automatic package references, custom item types, and keeneyes.project.json metadata; implemented in the same commit.
