# Localization Plugin Research

This document provides research findings and recommendations for implementing a localization system in KeenEyes, addressing the questions raised in [issue #432](https://github.com/orion-ecs/keen-eye/issues/432).

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Format Complexity Analysis](#format-complexity-analysis)
3. [Asset Localization](#asset-localization)
4. [Hot-Reload Support](#hot-reload-support)
5. [Fallback Chain](#fallback-chain)
6. [Translator Tooling](#translator-tooling)
7. [Font Support](#font-support)
8. [Integration Design](#integration-design)
9. [Implementation Recommendation](#implementation-recommendation)

---

## Executive Summary

**Recommendation**: Implement a `LocalizationPlugin` with a layered architecture:

1. **Core**: Simple key-value JSON with string interpolation (covers 90% of use cases)
2. **Optional**: ICU MessageFormat for pluralization/gender (via `MessageFormat.NET` NuGet package)
3. **Future**: Fluent support if demand arises

**Key architectural decision**: Use `LocalizationKey` in `UIText` components instead of raw strings. This enables hot-reload, asset localization, and clean separation between code and content.

---

## Format Complexity Analysis

### Option A: Simple Key-Value JSON

```json
{
  "menu.start": "Start Game",
  "menu.quit": "Quit",
  "player.health": "Health: {0}",
  "item.gold": "Gold: {amount}"
}
```

**Pros**:
- Simple to understand and edit
- No external dependencies
- Fast parsing with `System.Text.Json`
- AOT-compatible
- Translators can work with standard tools

**Cons**:
- No built-in pluralization ("1 item" vs "5 items")
- No gender agreement support
- Manual parameter formatting

**Best for**: Games with simple text (most indie games, action games, puzzlers)

### Option B: ICU MessageFormat

```json
{
  "items.count": "{count, plural, =0 {No items} =1 {One item} other {# items}}",
  "player.greeting": "{gender, select, male {He} female {She} other {They}} found treasure!"
}
```

**Pros**:
- Industry standard (used by Android, iOS, many web frameworks)
- Handles pluralization correctly for all languages (including complex rules for Russian, Arabic, etc.)
- Gender and select expressions
- Available via [MessageFormat.NET](https://github.com/jeffijoe/messageformat.net) (MIT license, targets .NET 6+)

**Cons**:
- Complex syntax intimidates translators
- Additional NuGet dependency
- Parsing overhead (though cacheable)

**Best for**: RPGs, games with inventory systems, narrative-heavy games

### Option C: Mozilla Fluent

```ftl
hello = Hello, { $name }!
items = { $count ->
    [0] No items
    [one] One item
   *[other] { $count } items
}
```

**Pros**:
- Designed for asymmetric localization (different languages need different logic)
- Excellent error recovery (degrades gracefully)
- Compound messages (multiple related strings)
- [Active standardization work](https://blog.mozilla.org/l10n/2024/01/18/advancing-mozillas-mission-through-our-work-on-localization-standards/) with Unicode MessageFormat 2

**Cons**:
- Custom file format (not JSON)
- No mature .NET implementation (would need to port or wrap)
- Learning curve for developers and translators

**Best for**: Complex UIs with many grammatical variations, large teams

### Comparison Matrix

| Feature | Key-Value | ICU MessageFormat | Fluent |
|---------|-----------|-------------------|--------|
| Simplicity | Excellent | Moderate | Moderate |
| Pluralization | Manual | Built-in | Built-in |
| Gender/Select | No | Yes | Yes |
| .NET Support | Native | MessageFormat.NET | None |
| AOT Compatible | Yes | Yes | Unknown |
| Translator-Friendly | Yes | Learning curve | Learning curve |
| Error Recovery | Poor | Poor | Excellent |

### Recommendation

**Start with Key-Value + optional ICU**:
1. Default: Simple key-value JSON with `{name}` parameter substitution
2. Opt-in: ICU MessageFormat for messages requiring pluralization
3. The `IMessageFormatter` interface allows future Fluent support

```csharp
// Simple (default)
loc.Get("menu.start");  // "Start Game"
loc.Format("player.health", health);  // "Health: 100"

// ICU (opt-in per message)
loc.FormatIcu("items.count", new { count = 5 });  // "5 items"
```

---

## Asset Localization

### Problem

Games need more than text localization:
- **Textures**: Different box art, culturally-appropriate icons
- **Audio**: Voice acting in different languages
- **Videos**: Localized cutscenes
- **Fonts**: CJK fonts only needed for Asian locales

### Design

Integrate with the existing Asset Management system:

```csharp
[Component]
public partial struct LocalizedAsset
{
    /// <summary>Base asset key (e.g., "textures/logo").</summary>
    public string AssetKey;

    /// <summary>Current resolved handle based on active locale.</summary>
    public AssetHandle ResolvedHandle;
}
```

Asset resolution follows the fallback chain:
1. `textures/logo.en-US.png` (exact locale)
2. `textures/logo.en.png` (language only)
3. `textures/logo.png` (default)

### Implementation Approach

```csharp
public interface ILocalizedAssetResolver
{
    /// <summary>Resolves an asset key to the best match for current locale.</summary>
    AssetHandle Resolve(string assetKey, Locale locale);

    /// <summary>Preloads assets for a locale (for seamless language switching).</summary>
    Task PreloadLocaleAssetsAsync(Locale locale);
}
```

The `LocalizedAssetSystem` watches for locale changes and updates `ResolvedHandle`:

```csharp
public class LocalizedAssetSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var loc = World.GetExtension<ILocalization>();
        if (!loc.LocaleChanged) return;

        foreach (var entity in World.Query<LocalizedAsset>())
        {
            ref var asset = ref World.Get<LocalizedAsset>(entity);
            asset.ResolvedHandle = resolver.Resolve(asset.AssetKey, loc.CurrentLocale);
        }
    }
}
```

---

## Hot-Reload Support

### Requirements

1. Change language without restarting the game
2. UI updates immediately when locale changes
3. Asset references update seamlessly

### Design

**Event-driven locale changes**:

```csharp
public interface ILocalization
{
    Locale CurrentLocale { get; }
    IReadOnlyList<Locale> AvailableLocales { get; }

    void SetLocale(Locale locale);

    /// <summary>Raised when locale changes. UI systems subscribe to refresh.</summary>
    event Action<Locale>? LocaleChanged;

    // String access
    string Get(string key);
    string Format(string key, params object[] args);
}
```

**UI Integration via tag component**:

```csharp
/// <summary>Marks a UIText as needing localization refresh.</summary>
[TagComponent]
public partial struct LocalizedTextTag { }

/// <summary>Stores the localization key for a text element.</summary>
[Component]
public partial struct LocalizedText
{
    public string Key;
    public string[]? Parameters;  // Optional parameter keys for dynamic values
}
```

**Refresh system**:

```csharp
public class LocalizedTextSystem : SystemBase
{
    private Locale lastLocale;

    public override void Update(float deltaTime)
    {
        var loc = World.GetExtension<ILocalization>();

        // Only refresh when locale changes
        if (loc.CurrentLocale == lastLocale) return;
        lastLocale = loc.CurrentLocale;

        foreach (var entity in World.Query<LocalizedText, UIText>())
        {
            ref readonly var locText = ref World.Get<LocalizedText>(entity);
            ref var uiText = ref World.Get<UIText>(entity);

            uiText.Content = locText.Parameters is { Length: > 0 }
                ? loc.Format(locText.Key, locText.Parameters)
                : loc.Get(locText.Key);
        }
    }
}
```

### Performance Considerations

- **String caching**: Parsed strings cached per locale (cleared on locale change)
- **Lazy loading**: Only load strings when accessed, not all at startup
- **Asset preloading**: Optional `PreloadLocaleAsync()` for seamless transitions

---

## Fallback Chain

### Standard Fallback Pattern

```
en-US → en → default (first loaded locale)
zh-Hans-CN → zh-Hans → zh → default
```

### Configuration

```csharp
public sealed class LocalizationConfig
{
    /// <summary>Locale to use on first launch (before user selection).</summary>
    public Locale DefaultLocale { get; init; } = Locale.Parse("en");

    /// <summary>Custom fallback chain (overrides default language-based fallback).</summary>
    public IReadOnlyDictionary<Locale, Locale>? FallbackOverrides { get; init; }

    /// <summary>What to do when a key is missing.</summary>
    public MissingKeyBehavior MissingKeyBehavior { get; init; } = MissingKeyBehavior.ReturnKey;
}

public enum MissingKeyBehavior
{
    /// <summary>Return the key itself (e.g., "menu.start").</summary>
    ReturnKey,

    /// <summary>Return empty string.</summary>
    ReturnEmpty,

    /// <summary>Throw exception (useful during development).</summary>
    ThrowException,

    /// <summary>Return placeholder showing the key (e.g., "[MISSING: menu.start]").</summary>
    ReturnPlaceholder
}
```

### Implementation

```csharp
internal sealed class LocalizationManager : ILocalization
{
    private readonly Dictionary<Locale, Dictionary<string, string>> stringTables = new();

    public string Get(string key)
    {
        foreach (var locale in GetFallbackChain(CurrentLocale))
        {
            if (stringTables.TryGetValue(locale, out var table) &&
                table.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return config.MissingKeyBehavior switch
        {
            MissingKeyBehavior.ReturnKey => key,
            MissingKeyBehavior.ReturnEmpty => string.Empty,
            MissingKeyBehavior.ReturnPlaceholder => $"[MISSING: {key}]",
            MissingKeyBehavior.ThrowException => throw new KeyNotFoundException($"Localization key not found: {key}"),
            _ => key
        };
    }

    private IEnumerable<Locale> GetFallbackChain(Locale locale)
    {
        // 1. Exact match (en-US)
        yield return locale;

        // 2. Language only (en)
        if (locale.Region is not null)
        {
            yield return new Locale(locale.Language);
        }

        // 3. Custom fallback override
        if (config.FallbackOverrides?.TryGetValue(locale, out var fallback) == true)
        {
            yield return fallback;
        }

        // 4. Default locale
        if (locale != config.DefaultLocale)
        {
            yield return config.DefaultLocale;
        }
    }
}
```

---

## Translator Tooling

### File Format Recommendations

**Primary format: JSON** (for key-value and ICU)

```json
{
  "_meta": {
    "locale": "en-US",
    "version": "1.0.0",
    "lastModified": "2025-12-15"
  },
  "menu": {
    "start": "Start Game",
    "options": "Options",
    "quit": "Quit"
  },
  "gameplay": {
    "items.count": "{count, plural, =0 {No items} =1 {One item} other {# items}}"
  }
}
```

**Alternative: CSV** (for spreadsheet workflows)

```csv
key,en,es,fr,de
menu.start,Start Game,Iniciar Juego,Commencer,Spiel starten
menu.quit,Quit,Salir,Quitter,Beenden
```

### Tool Integration

| Tool | Format | Notes |
|------|--------|-------|
| Lokalise | JSON, CSV | Professional TMS with ICU support |
| Crowdin | JSON, CSV | Community translation platform |
| POEditor | JSON | Simple and affordable |
| Transifex | JSON | Open-source friendly |
| Phrase | JSON, ICU | Full ICU MessageFormat support |
| Excel/Sheets | CSV | Simple, accessible to all |

### Export/Import Pipeline

```csharp
public interface IStringSource
{
    /// <summary>Loads strings for a locale.</summary>
    Task<IReadOnlyDictionary<string, string>> LoadAsync(Locale locale);

    /// <summary>Supported file extensions.</summary>
    IReadOnlyList<string> SupportedExtensions { get; }
}

public sealed class JsonStringSource : IStringSource
{
    public IReadOnlyList<string> SupportedExtensions => [".json"];

    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(Locale locale)
    {
        var path = $"localization/{locale.Code}.json";
        // Load and flatten nested JSON to dot-notation keys
    }
}

public sealed class CsvStringSource : IStringSource
{
    public IReadOnlyList<string> SupportedExtensions => [".csv"];
    // CSV with all languages in columns
}
```

### Development Workflow Recommendations

1. **Source of truth**: Keep English strings in code/JSON, export to translation platform
2. **Key naming**: Use hierarchical keys (`menu.options.audio.volume`)
3. **Context comments**: Include translator notes in a `_context` key
4. **Screenshots**: Provide visual context for UI strings
5. **Character limits**: Define max lengths for UI-constrained strings

---

## Font Support

### Challenges

1. **CJK Characters**: Chinese, Japanese, Korean require large font files (10MB+)
2. **Arabic/Hebrew**: Right-to-left text, contextual shaping
3. **Thai/Hindi**: Complex script rendering, stacking diacritics
4. **Emoji**: Color emoji support

### Design

**Locale-specific font stacks**:

```csharp
public sealed class LocalizedFontConfig
{
    /// <summary>Font to use for this locale.</summary>
    public FontHandle PrimaryFont { get; init; }

    /// <summary>Fallback fonts for missing glyphs.</summary>
    public IReadOnlyList<FontHandle> FallbackFonts { get; init; } = [];

    /// <summary>Text direction.</summary>
    public TextDirection Direction { get; init; } = TextDirection.LeftToRight;
}

public enum TextDirection
{
    LeftToRight,
    RightToLeft
}
```

**Font configuration per locale**:

```csharp
var config = new LocalizationConfig
{
    FontConfigs = new Dictionary<Locale, LocalizedFontConfig>
    {
        [Locale.Parse("en")] = new()
        {
            PrimaryFont = fonts.Load("Roboto-Regular.ttf")
        },
        [Locale.Parse("ja")] = new()
        {
            PrimaryFont = fonts.Load("NotoSansJP-Regular.otf"),
            FallbackFonts = [fonts.Load("Roboto-Regular.ttf")]
        },
        [Locale.Parse("ar")] = new()
        {
            PrimaryFont = fonts.Load("NotoSansArabic-Regular.ttf"),
            Direction = TextDirection.RightToLeft
        }
    }
};
```

### RTL Support Considerations

RTL requires coordination with the UI system:

1. **Text rendering**: Graphics system handles text direction
2. **Layout mirroring**: UI layout flips for RTL locales
3. **Bidirectional text**: Mixed LTR/RTL in same string (numbers in Arabic text)

```csharp
[Component]
public partial struct UIRect
{
    // Existing fields...

    /// <summary>If true, mirrors layout for RTL locales.</summary>
    public bool MirrorForRtl;
}
```

### Lazy Font Loading

Load large font files only when needed:

```csharp
public class FontManager
{
    public async Task<FontHandle> GetFontForLocaleAsync(Locale locale)
    {
        if (!loadedFonts.TryGetValue(locale, out var font))
        {
            var config = fontConfigs[locale];
            font = await assets.LoadAsync<Font>(config.PrimaryFont);
            loadedFonts[locale] = font;
        }
        return font;
    }
}
```

---

## Integration Design

### Plugin Architecture

```
KeenEyes.Localization/
├── KeenEyes.Localization.Abstractions/
│   ├── ILocalization.cs              # Main API interface
│   ├── Locale.cs                     # Language + region
│   ├── LocalizedText.cs              # Component for localized UI text
│   ├── LocalizedAsset.cs             # Component for localized assets
│   └── IMessageFormatter.cs          # Formatting interface
│
├── KeenEyes.Localization/
│   ├── LocalizationPlugin.cs         # Plugin entry point
│   ├── LocalizationManager.cs        # Core implementation
│   ├── Sources/
│   │   ├── JsonStringSource.cs       # JSON file loader
│   │   └── CsvStringSource.cs        # CSV file loader
│   ├── Formatters/
│   │   ├── SimpleFormatter.cs        # Basic {0} substitution
│   │   └── IcuFormatter.cs           # ICU MessageFormat (optional)
│   └── Systems/
│       ├── LocalizedTextSystem.cs    # Updates UIText on locale change
│       └── LocalizedAssetSystem.cs   # Updates asset refs on locale change
```

### LocalizationPlugin Implementation

```csharp
public sealed class LocalizationPlugin : IWorldPlugin
{
    private readonly LocalizationConfig config;
    private LocalizationManager? manager;

    public string Name => "Localization";

    public LocalizationPlugin(LocalizationConfig? config = null)
    {
        this.config = config ?? LocalizationConfig.Default;
    }

    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<LocalizedText>();
        context.RegisterComponent<LocalizedAsset>();
        context.RegisterComponent<LocalizedTextTag>(isTag: true);

        // Create and expose API
        manager = new LocalizationManager(config);
        context.SetExtension<ILocalization>(manager);

        // Register systems
        context.AddSystem<LocalizedTextSystem>(SystemPhase.EarlyUpdate, order: 50);
        context.AddSystem<LocalizedAssetSystem>(SystemPhase.EarlyUpdate, order: 51);
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<ILocalization>();
        manager?.Dispose();
    }
}
```

### Usage Example

```csharp
// Setup
using var world = new World();
world.InstallPlugin(new LocalizationPlugin(new LocalizationConfig
{
    DefaultLocale = Locale.Parse("en-US"),
    StringSources = [new JsonStringSource("localization/")]
}));

var loc = world.GetExtension<ILocalization>();
await loc.LoadLocaleAsync(Locale.Parse("en-US"));

// Create localized UI
var startButton = world.Spawn()
    .With(UIElement.Default)
    .With(UIRect.Centered(200, 50))
    .With(UIText.Centered("", 24))  // Content set by system
    .With(new LocalizedText { Key = "menu.start" })
    .WithTag<LocalizedTextTag>()
    .Build();

// Change language at runtime
loc.SetLocale(Locale.Parse("es"));  // UI updates automatically
```

### Extension Member for Ergonomic Access

```csharp
[PluginExtension("Localization")]
public interface ILocalization
{
    // ... interface members
}

// Generated extension enables:
// world.Localization.Get("menu.start")
// world.Localization.SetLocale(Locale.Parse("es"))
```

---

## Implementation Recommendation

### Phase 1: Core (MVP)

**Scope**: Simple key-value JSON, hot-reload, UI integration

**Deliverables**:
- `Locale` value type
- `ILocalization` interface
- `LocalizationPlugin` with `JsonStringSource`
- `LocalizedText` component
- `LocalizedTextSystem`
- Fallback chain

**Estimated complexity**: Low

### Phase 2: Formatting

**Scope**: ICU MessageFormat support

**Deliverables**:
- Add `MessageFormat.NET` NuGet dependency
- `IcuFormatter` implementation
- `Format()` and `FormatIcu()` methods
- Pluralization, gender, select support

**Estimated complexity**: Low-Medium

### Phase 3: Asset Localization

**Scope**: Localized textures, audio, fonts

**Deliverables**:
- `LocalizedAsset` component
- `ILocalizedAssetResolver` interface
- `LocalizedAssetSystem`
- Asset preloading for locale changes

**Estimated complexity**: Medium

### Phase 4: Advanced Features

**Scope**: RTL, complex scripts, tooling integration

**Deliverables**:
- RTL layout mirroring
- Font stack configuration
- CSV import/export
- Editor integration (if applicable)

**Estimated complexity**: Medium-High

### Decision Summary

| Question | Decision | Rationale |
|----------|----------|-----------|
| Format complexity | Key-value JSON + optional ICU | Covers 90% of cases, ICU available when needed |
| Asset localization | Suffix-based resolution | Simple, works with existing asset system |
| Hot-reload | Event-driven refresh | Clean, efficient, works with ECS |
| Fallback chain | en-US → en → default | Industry standard, configurable |
| Tooling | JSON primary, CSV supported | Works with all major TMS platforms |
| Font support | Per-locale font config | Handles CJK, RTL, lazy loading |

---

## References

- [Mozilla Fluent](https://projectfluent.org/) - Asymmetric localization system
- [ICU MessageFormat](https://unicode-org.github.io/icu/userguide/format_parse/messages/) - Unicode standard
- [MessageFormat.NET](https://github.com/jeffijoe/messageformat.net) - .NET ICU implementation
- [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.2/manual/) - Unity's approach
- [Unreal Localization](https://docs.unrealengine.com/en-US/ProductionPipelines/Localization/) - Unreal's approach
- [CLDR Plural Rules](https://cldr.unicode.org/index/cldr-spec/plural-rules) - Language-specific pluralization
