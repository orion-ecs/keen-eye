namespace KeenEyes.Localization;

/// <summary>
/// System that resolves localized asset paths when the locale changes.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for <see cref="LocaleChangedEvent"/> and automatically
/// updates the <see cref="LocalizedAsset.ResolvedPath"/> for all entities that
/// have the <see cref="LocalizedAsset"/> component.
/// </para>
/// <para>
/// The system runs in the <see cref="SystemPhase.EarlyUpdate"/> phase to ensure
/// asset paths are resolved before rendering systems run.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an entity with a localized texture
/// var logo = world.Spawn()
///     .With(new LocalizedAsset { AssetKey = "textures/logo" })
///     .Build();
///
/// // When locale changes, ResolvedPath is automatically updated
/// localization.SetLocale(Locale.JapaneseJP);
/// // logo's LocalizedAsset.ResolvedPath is now "textures/logo.ja-JP.png" (or fallback)
/// </code>
/// </example>
public sealed class LocalizedAssetSystem : SystemBase
{
    private LocalizationManager? localization;
    private ILocalizedAssetResolver? resolver;
    private EventSubscription? localeChangedSubscription;
    private bool needsFullUpdate;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension(out localization);
        World.TryGetExtension(out resolver);

        // Subscribe to locale changes
        localeChangedSubscription = World.Subscribe<LocaleChangedEvent>(OnLocaleChanged);

        // Do an initial update for any existing entities
        needsFullUpdate = true;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (localization == null && !World.TryGetExtension(out localization))
        {
            return;
        }

        if (resolver == null && !World.TryGetExtension(out resolver))
        {
            return;
        }

        if (needsFullUpdate)
        {
            UpdateAllLocalizedAssets();
            needsFullUpdate = false;
        }
        else
        {
            // Check for newly added or invalidated assets
            UpdateUnresolvedAssets();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            localeChangedSubscription?.Dispose();
            localeChangedSubscription = null;
        }
        base.Dispose(disposing);
    }

    private void OnLocaleChanged(LocaleChangedEvent evt)
    {
        // Mark for full update on next Update() call
        needsFullUpdate = true;
    }

    private void UpdateAllLocalizedAssets()
    {
        if (localization == null || resolver == null)
        {
            return;
        }

        var currentLocale = localization.CurrentLocale;

        foreach (var entity in World.Query<LocalizedAsset>())
        {
            ref var localizedAsset = ref World.Get<LocalizedAsset>(entity);
            ResolveAsset(ref localizedAsset, currentLocale);
        }
    }

    private void UpdateUnresolvedAssets()
    {
        if (localization == null || resolver == null)
        {
            return;
        }

        var currentLocale = localization.CurrentLocale;

        foreach (var entity in World.Query<LocalizedAsset>())
        {
            ref var localizedAsset = ref World.Get<LocalizedAsset>(entity);

            // Only resolve if not already resolved
            if (!localizedAsset.IsResolved && !string.IsNullOrEmpty(localizedAsset.AssetKey))
            {
                ResolveAsset(ref localizedAsset, currentLocale);
            }
        }
    }

    private void ResolveAsset(ref LocalizedAsset asset, Locale locale)
    {
        if (string.IsNullOrEmpty(asset.AssetKey))
        {
            asset.ResolvedPath = null;
            return;
        }

        asset.ResolvedPath = resolver!.Resolve(asset.AssetKey, locale);
    }

    /// <summary>
    /// Forces a re-resolution of all localized assets.
    /// </summary>
    /// <remarks>
    /// Call this if asset files have been added or removed and you want to
    /// refresh the resolved paths without changing the locale.
    /// </remarks>
    public void RefreshAllAssets()
    {
        needsFullUpdate = true;
    }
}
