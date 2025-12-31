# KeenEyes.Sdk.Library

MSBuild SDK for KeenEyes library development.

## Quick Start

```xml
<Project Sdk="KeenEyes.Sdk.Library/0.1.0">

  <PropertyGroup>
    <PackageId>MyCompany.KeenEyes.Physics</PackageId>
    <Description>Custom physics components for KeenEyes</Description>
  </PropertyGroup>

</Project>
```

## What's Included

The Library SDK is designed for creating reusable NuGet packages:
- References `KeenEyes.Core` by default
- Sets `IsPackable=true`
- Enables XML documentation generation
- Configures generators as `PrivateAssets="all"`

| Feature | Default | Override |
|---------|---------|----------|
| Target Framework | `net10.0` | Set `<TargetFramework>` |
| Output Type | `Library` | Set `<OutputType>` |
| IsPackable | `true` | Set `<IsPackable>` |
| GenerateDocumentationFile | `true` | Set `<GenerateDocumentationFile>` |
| KeenEyes.Core | Included | Set `<IncludeKeenEyesCore>false</IncludeKeenEyesCore>` |
| KeenEyes.Generators | Included (private) | Set `<IncludeKeenEyesGenerators>false</IncludeKeenEyesGenerators>` |

## Generator Configuration

The generators are included with `PrivateAssets="all"`, meaning:
- Your library uses the generators during build
- Consumers of your library don't transitively get the generators
- Each project controls its own generator version

## Abstraction-Only Libraries

For libraries that only define interfaces/abstractions without implementations:

```xml
<PropertyGroup>
  <IncludeKeenEyesCore>false</IncludeKeenEyesCore>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="KeenEyes.Abstractions" Version="0.1.0" />
</ItemGroup>
```

## SDK Comparison

| Aspect | KeenEyes.Sdk | KeenEyes.Sdk.Library |
|--------|--------------|----------------------|
| Default OutputType | Exe | Library |
| IsPackable | false | true |
| GenerateDocumentationFile | false | true |
| Generators PrivateAssets | none | all |
| Use Case | Games | Reusable libraries |
