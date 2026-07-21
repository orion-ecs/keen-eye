# Localization

The `KeenEyes.Localization` library adds multi-language text and locale-aware asset support to a `World`. It manages locale switching and fallback, loads translations from JSON/CSV/in-memory sources, keeps UI text and localized assets in sync automatically, and resolves per-locale fonts.

## Overview

`KeenEyes.Localization` provides:

- **Locale management** - switch the active `Locale` at runtime with automatic fallback (custom overrides → language-only → default locale)
- **String sources** - load translations from JSON files (`JsonStringSource`), spreadsheet-friendly CSV files (`CsvStringSource`), or in-memory dictionaries (`DictionaryStringSource`)
- **Automatic UI updates** - entities with `LocalizedText` and a UI text component are updated whenever the locale changes, no manual wiring required
- **Localized assets** - entities with `LocalizedAsset` get their resolved path recomputed automatically, following an exact-locale → language-only → default fallback chain
- **Formatting** - simple `{0}`/`{name}` substitution via `Format`, or full ICU MessageFormat (pluralization, gender, select) via `FormatIcu`
- **Locale-specific fonts** - configure primary/fallback fonts and size/line-height multipliers per locale

## Quick Start

### Installation

```csharp
using KeenEyes.Localization;

using var world = new World();

world.InstallPlugin(new LocalizationPlugin(new LocalizationConfig
{
    DefaultLocale = Locale.EnglishUS,
    MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder,
    AssetRootPath = "Assets",
}));
```

`LocalizationPlugin.Install` registers:

- Components: `LocalizedText`, `LocalizedTextTag`, `LocalizedAsset`
- Extensions: `world.Localization` (an `ILocalization` backed by `LocalizationManager`) and an `ILocalizedAssetResolver`
- Systems: `LocalizedAssetSystem` (`SystemPhase.EarlyUpdate`, order `-50`) and `LocalizedTextSystem` (`SystemPhase.Update`, order `100`)

`LocalizationManager` is annotated with `[PluginExtension("Localization")]`, which is what generates the `world.Localization` accessor property.

### Loading Translations

```csharp
var source = JsonStringSource.FromFile("locales/en-US.json", Locale.EnglishUS);
world.Localization.AddSource(source);

string title = world.Localization.Get("game.title");
```

A matching JSON file is a flat or nested key-value document; nested objects are flattened with dot notation:

```json
{
    "menu.start": "Start Game",
    "dialog": {
        "greeting": "Hello, {0}!"
    }
}
```

### Localized UI Text

```csharp
var startButton = world.Spawn()
    .With(UIElement.Default)
    .With(UIText.Create(""))
    .With(LocalizedText.Create("menu.start"))
    .Build();

// LocalizedTextSystem fills in UIText.Content automatically, both on
// creation and whenever the locale changes.
world.Localization.SetLocale(Locale.JapaneseJP);
```

## Core Concepts

### Locale

`Locale` is a `readonly record struct` wrapping an IETF BCP 47 language tag (e.g. `"en-US"`, `"ja-JP"`):

```csharp
var locale = new Locale("en-US");
locale.Language;      // "en"
locale.Region;         // "US"
locale.HasRegion;      // true
locale.LanguageOnly;   // Locale("en")
locale.IsRightToLeft;  // false
locale.TextDirection;  // TextDirection.LeftToRight
```

Predefined statics cover common locales, including several right-to-left languages: `Locale.EnglishUS`, `Locale.EnglishGB`, `Locale.JapaneseJP`, `Locale.ChineseSimplified`, `Locale.ChineseTraditional`, `Locale.KoreanKR`, `Locale.GermanDE`, `Locale.FrenchFR`, `Locale.SpanishES`, `Locale.PortugueseBR`, `Locale.ItalianIT`, `Locale.RussianRU`, `Locale.Arabic`, `Locale.ArabicSA`, `Locale.ArabicEG`, `Locale.HebrewIL`, `Locale.PersianIR`, `Locale.UrduPK`, `Locale.ThaiTH`, and `Locale.HindiIN`. A `Locale` also converts implicitly from `string`.

### LocalizationConfig

```csharp
var config = new LocalizationConfig
{
    DefaultLocale = Locale.EnglishUS,
    MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder,
    EnableFallback = true,
    AssetRootPath = "Assets",
    FallbackOverrides =
    {
        [new Locale("en-GB")] = new Locale("en-US"),
        [new Locale("pt-PT")] = new Locale("pt-BR"),
    },
    FontConfigs =
    {
        [Locale.EnglishUS] = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" },
        [Locale.JapaneseJP] = new LocalizedFontConfig
        {
            PrimaryFont = "fonts/NotoSansJP.ttf",
            FallbackFonts = ["fonts/Roboto.ttf"],
        },
    },
};
```

`LocalizationPlugin`'s constructor calls `config.Validate()` and throws `ArgumentException` if `DefaultLocale.Code` is null/whitespace. `LocalizationConfig.Default` returns a config with sensible development defaults (`MissingKeyBehavior.ReturnKey`, fallback enabled).

### String Sources

All sources implement `IStringSource` (`SupportedLocales`, `TryGetString`, `GetKeys`, `HasLocale`). `world.Localization.AddSource(source)` registers one; later-added sources take priority over earlier ones for the same key.

```csharp
// In-memory, useful for tests and small string sets
var dict = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
{
    ["menu.start"] = "Start Game",
    ["menu.quit"] = "Quit",
});

// JSON file (flat or nested keys)
var json = JsonStringSource.FromFile("locales/ja-JP.json", Locale.JapaneseJP);

// CSV file with one "key" column and one column per locale
var csv = CsvStringSource.FromFile("translations.csv");

world.Localization.AddSource(dict);
world.Localization.AddSource(json);
world.Localization.AddSource(csv);
```

`JsonStringSource` and `CsvStringSource` both offer `FromFile`, `FromFileAsync`, `FromString`, and `FromStream`/`FromStreamAsync` factories. `CsvStringSource` additionally exposes `ToCsv`/`Export`/`ExportAsync` for round-tripping translations through spreadsheet tools, and `MergeFromString` on both types to merge in additional translations.

### Retrieving and Formatting Strings

`world.Localization` implements `ILocalization`:

```csharp
string title = world.Localization.Get("game.title");

// .NET-style positional/composite formatting
string greeting = world.Localization.Format("greeting", player.Name);
// "greeting" = "Hello, {0}!" -> "Hello, Alex!"

// ICU MessageFormat for pluralization/gender/select
string count = world.Localization.FormatIcu("items.count", new { count = 5 });
// "items.count" = "{count, plural, =0 {No items} =1 {One item} other {# items}}" -> "5 items"

bool exists = world.Localization.HasKey("menu.start");
bool found = world.Localization.TryGet("menu.start", out var value);
```

`Get` and `Format` fall back through `LocalizationConfig.FallbackOverrides`, then the language-only locale, then `DefaultLocale`, before invoking `MissingKeyBehavior` (`ReturnKey`, `ReturnEmpty`, `ReturnPlaceholder`, or `ThrowException`). `TryGet` does not apply fallback or missing-key handling.

`FormatIcu` is powered by `IMessageFormatter` — `SimpleFormatter` (positional `{0}` and named `{name}` placeholders) or `IcuFormatter` (full ICU MessageFormat via the `MessageFormat` NuGet package), which `LocalizationManager` uses by default. Both formatters are marked `[RequiresUnreferencedCode]` when passed an anonymous object, because reading its properties uses reflection; pass a `Dictionary<string, object?>` instead for full AOT compatibility.

### Locale Changes

```csharp
world.Localization.SetLocale(Locale.JapaneseJP);
```

`SetLocale` is a no-op if the locale is unchanged; otherwise it publishes a `LocaleChangedEvent(PreviousLocale, NewLocale)` via `world.Send`:

```csharp
world.Subscribe<LocaleChangedEvent>(e =>
{
    Console.WriteLine($"Locale changed from {e.PreviousLocale} to {e.NewLocale}");
});
```

`ILocalization.CurrentTextDirection` and `ILocalization.IsRightToLeft` are convenience accessors over `CurrentLocale`, useful for deciding whether to mirror UI layouts.

### Localized Text Components

`LocalizedText` marks an entity's text as translatable:

```csharp
[Component]
public partial struct LocalizedText : IComponent
{
    public string Key;
    public object?[]? FormatArgs;
}
```

`LocalizedText.Create(key)` and `LocalizedText.Create(key, args)` build the component. `LocalizedTextSystem` adds/removes the `LocalizedTextTag` tag component alongside it, subscribes to `LocaleChangedEvent`, and on the next `Update` writes the resolved string into any entity's `UIText.Content` — using `Format` when `FormatArgs` is non-empty, otherwise `Get`. Call `LocalizedTextSystem.RefreshAllText()` to force a refresh without changing the locale (e.g. after mutating `FormatArgs` in place).

### Localized Asset Components

`LocalizedAsset` marks an entity's asset reference as locale-dependent:

```csharp
[Component]
public partial struct LocalizedAsset : IComponent
{
    public string AssetKey;
    public string? ResolvedPath;
    public readonly bool IsResolved => !string.IsNullOrEmpty(ResolvedPath);
}
```

```csharp
var logo = world.Spawn()
    .With(new LocalizedAsset { AssetKey = "textures/logo" })
    .Build();
```

`LocalizedAssetSystem` subscribes to `LocaleChangedEvent` and calls `ILocalizedAssetResolver.Resolve(AssetKey, locale)` to fill in `ResolvedPath`, trying (in order) the exact locale (`textures/logo.en-US.png`), the language only (`textures/logo.en.png`), then the unlocalized default (`textures/logo.png`). The default resolver, `LocalizedAssetResolver`, checks the file system under `LocalizationConfig.AssetRootPath` for these variants. Call `asset.Invalidate()` to clear `ResolvedPath` and force re-resolution, or `LocalizedAssetSystem.RefreshAllAssets()` to re-resolve every entity.

### Fonts and Preloading

```csharp
LocalizedFontConfig? fontConfig = world.Localization.GetCurrentFontConfig();
```

`LocalizedFontConfig` holds `PrimaryFont`, `FallbackFonts`, `SizeMultiplier`, and `LineHeightMultiplier` for a locale, and exposes `GetAllFontPaths()` for preloading. `ILocalization.GetFontConfig(locale)` falls back from the exact locale to its language-only form to the default locale's configuration.

Before switching locales, preload all of the target locale's assets (and fonts) to avoid stutter:

```csharp
await world.Localization.PreloadLocaleAssetsAsync(
    Locale.JapaneseJP,
    ["textures/logo", "textures/menu_background", "audio/intro_voice"]);

world.Localization.SetLocale(Locale.JapaneseJP);
```

`PreloadLocaleAssetsAsync` requires the `KeenEyes.Assets` plugin to be installed; it resolves each key/font via the asset resolver and loads them as `RawAsset` with `LoadPriority.Low`, silently skipping assets that fail to preload.

## Performance

- String lookups walk registered sources in reverse-registration order and stop at the first hit — keep the number of sources reasonable, or consolidate frequently-queried locales into a single source.
- `LocalizedTextSystem` only re-evaluates entities when a `LocaleChangedEvent` fires (or on its first `Update`) and is otherwise idle. `LocalizedAssetSystem` does the same full sweep on locale change, but every other frame it still iterates entities with `LocalizedAsset` to resolve any that are newly added and not yet `IsResolved`.
- `LocalizedAssetResolver.Resolve` performs file-system existence checks per fallback step; call `PreloadLocaleAssetsAsync` ahead of a locale switch to move that I/O off the critical path.
- `FormatIcu`/`IcuFormatter` cache compiled `MessageFormatter` instances (`useCache: true`) but still use reflection for anonymous-object arguments — prefer `Dictionary<string, object?>` args in hot paths and for AOT builds.

## Next Steps

- [Plugins Guide](plugins.md) - How plugins work
- [Systems Guide](systems.md) - System design patterns
- [UI System](ui.md) - `UIText` and other UI components updated by `LocalizedTextSystem`
- [Localization Plugin Design](research/localization-plugin.md) - Original design document
