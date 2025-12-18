# KeenEyes SDK

The KeenEyes SDK packages simplify project setup by providing sensible defaults, automatic package references, and custom item types for editor integration.

## Quick Start

Replace the standard SDK with `KeenEyes.Sdk`:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
</Project>
```

That's it! Your project is now configured with:
- .NET 10 targeting
- C# 13 features enabled
- Nullable reference types
- AOT compatibility
- KeenEyes.Core and Generators referenced

## SDK Packages

| Package | Use Case | Output Type | Dependencies |
|---------|----------|-------------|--------------|
| `KeenEyes.Sdk` | Games and applications | Exe | Core + Generators |
| `KeenEyes.Sdk.Plugin` | Plugin libraries | Library | Abstractions + Generators |
| `KeenEyes.Sdk.Library` | Reusable ECS libraries | Library | Core + Generators |

### Game SDK

For standalone games and applications:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <!-- Everything configured automatically -->
</Project>
```

### Plugin SDK

For plugins that extend KeenEyes without depending on Core:

```xml
<Project Sdk="KeenEyes.Sdk.Plugin/0.1.0">
  <!-- References Abstractions, not Core -->
</Project>
```

To include common components:

```xml
<Project Sdk="KeenEyes.Sdk.Plugin/0.1.0">
  <PropertyGroup>
    <IncludeKeenEyesCommon>true</IncludeKeenEyesCommon>
  </PropertyGroup>
</Project>
```

### Library SDK

For creating reusable NuGet packages:

```xml
<Project Sdk="KeenEyes.Sdk.Library/0.1.0">
  <PropertyGroup>
    <PackageId>MyCompany.KeenEyes.Physics</PackageId>
    <Description>Custom physics components</Description>
  </PropertyGroup>
</Project>
```

The Library SDK enables `IsPackable=true` and `GenerateDocumentationFile=true` by default.

## Default Properties

The SDK sets these defaults (all can be overridden):

| Property | Default | Description |
|----------|---------|-------------|
| `TargetFramework` | `net10.0` | .NET version |
| `LangVersion` | `preview` | C# 13 features |
| `Nullable` | `enable` | Nullable reference types |
| `ImplicitUsings` | `enable` | Implicit global usings |
| `IsAotCompatible` | `true` | Native AOT ready |
| `TreatWarningsAsErrors` | `true` | Strict compilation |
| `EnableNETAnalyzers` | `true` | Code analysis |

### Overriding Defaults

Set properties explicitly to override:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## Automatic Package References

### Opting Out

Disable automatic package references:

```xml
<PropertyGroup>
  <!-- Don't include KeenEyes.Core -->
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>

  <!-- Don't include KeenEyes.Generators -->
  <IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>

  <!-- Plugin SDK only: Include KeenEyes.Common -->
  <IncludeKeenEyesCommon>true</IncludeKeenEyesCommon>
</PropertyGroup>
```

### Adding Feature Packages

Add additional KeenEyes packages as needed:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <ItemGroup>
    <PackageReference Include="KeenEyes.Physics" Version="0.1.0" />
    <PackageReference Include="KeenEyes.Graphics.Silk" Version="0.1.0" />
  </ItemGroup>
</Project>
```

## Custom Item Types

The SDK defines item types for game assets and editor integration.

### Scenes

Scene files define world configuration and entity setup:

```xml
<ItemGroup>
  <KeenEyesScene Include="Scenes/**/*.kescene" />
</ItemGroup>
```

### Prefabs

Prefab files define reusable entity templates:

```xml
<ItemGroup>
  <KeenEyesPrefab Include="Prefabs/**/*.keprefab" />
</ItemGroup>
```

### Assets

Game assets (textures, audio, data) are automatically copied to output:

```xml
<ItemGroup>
  <KeenEyesAsset Include="Assets/**/*" />
</ItemGroup>
```

### World Configuration

World files define initial world setup:

```xml
<ItemGroup>
  <KeenEyesWorld Include="Worlds/**/*.keworld" />
</ItemGroup>
```

## Version Information

The SDK exposes version metadata through MSBuild properties:

| Property | Description |
|----------|-------------|
| `$(KeenEyesSdkVersion)` | SDK package version |
| `$(KeenEyesCoreVersion)` | KeenEyes.Core version |
| `$(KeenEyesMinimumCoreVersion)` | Minimum compatible version |
| `$(KeenEyesProjectType)` | `Game`, `Plugin`, or `Library` |
| `$(IsKeenEyesProject)` | Always `true` for SDK projects |

### Generated Metadata

The SDK generates `keeneyes.project.json` in the output directory:

```json
{
  "projectName": "MyGame",
  "projectType": "Game",
  "sdkVersion": "0.1.0",
  "coreVersion": "0.1.0",
  "targetFramework": "net10.0",
  "isAotCompatible": true
}
```

This file enables tooling to detect and introspect KeenEyes projects.

## Comparison: SDK vs Manual Setup

### With SDK

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
</Project>
```

### Without SDK (Manual)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsAotCompatible>true</IsAotCompatible>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="KeenEyes.Core" Version="0.1.0" />
    <PackageReference Include="KeenEyes.Generators" Version="0.1.0"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

## Troubleshooting

### SDK Not Found

If you see "SDK not found" errors:

1. Ensure NuGet can resolve the SDK package
2. Check your `nuget.config` includes the KeenEyes package source
3. Try `dotnet restore --force`

### Version Mismatch

The SDK version determines the Core/Generators versions. If you need specific versions:

```xml
<PropertyGroup>
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="KeenEyes.Core" Version="0.2.0" />
</ItemGroup>
```

### Disabling SDK Features

To use the SDK for conventions but manage dependencies manually:

```xml
<Project Sdk="KeenEyes.Sdk/0.1.0">
  <PropertyGroup>
    <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
    <IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>
  </PropertyGroup>

  <!-- Manual references -->
  <ItemGroup>
    <ProjectReference Include="../KeenEyes.Core/KeenEyes.Core.csproj" />
  </ItemGroup>
</Project>
```

## See Also

- [ADR-006: Custom MSBuild SDK](adr/006-custom-msbuild-sdk.md) - Design decision record
- [Getting Started](getting-started.md) - New to KeenEyes?
- [AOT Deployment](aot-deployment.md) - Native AOT compilation guide
