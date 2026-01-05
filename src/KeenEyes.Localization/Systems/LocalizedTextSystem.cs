using KeenEyes.UI.Abstractions;

namespace KeenEyes.Localization;

/// <summary>
/// System that updates UI text components when the locale changes.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for <see cref="LocaleChangedEvent"/> and automatically
/// updates the <see cref="UIText.Content"/> for all entities that have both
/// <see cref="LocalizedText"/> and <see cref="UIText"/> components.
/// </para>
/// <para>
/// The system runs in the <see cref="SystemPhase.Update"/> phase to ensure
/// text updates happen before rendering.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a localized UI button
/// var startButton = world.Spawn()
///     .With(UIElement.Create())
///     .With(UIText.Create(""))
///     .With(LocalizedText.Create("menu.start"))
///     .Build();
///
/// // When locale changes, the system automatically updates UIText.Content
/// localization.SetLocale(Locale.JapaneseJP);
/// // startButton's UIText.Content is now the Japanese translation of "menu.start"
/// </code>
/// </example>
public sealed class LocalizedTextSystem : SystemBase
{
    private LocalizationManager? localization;
    private EventSubscription? localeChangedSubscription;
    private bool needsFullUpdate;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        World.TryGetExtension(out localization);

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

        if (needsFullUpdate)
        {
            UpdateAllLocalizedText();
            needsFullUpdate = false;
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        localeChangedSubscription?.Dispose();
        localeChangedSubscription = null;
        base.Dispose();
    }

    private void OnLocaleChanged(LocaleChangedEvent evt)
    {
        // Mark for full update on next Update() call
        needsFullUpdate = true;
    }

    private void UpdateAllLocalizedText()
    {
        if (localization == null)
        {
            return;
        }

        // Query all entities with both LocalizedText and UIText components
        foreach (var entity in World.Query<LocalizedText, UIText>())
        {
            ref readonly var localizedText = ref World.Get<LocalizedText>(entity);
            ref var uiText = ref World.Get<UIText>(entity);

            // Get the localized string
            string content;
            if (localizedText.FormatArgs is { Length: > 0 })
            {
                content = localization.Format(localizedText.Key, localizedText.FormatArgs);
            }
            else
            {
                content = localization.Get(localizedText.Key);
            }

            // Update the UI text content
            uiText.Content = content;
        }
    }

    /// <summary>
    /// Forces an update of all localized text entities.
    /// </summary>
    /// <remarks>
    /// Call this if you've modified <see cref="LocalizedText.FormatArgs"/> on entities
    /// and want to see the changes reflected immediately without changing the locale.
    /// </remarks>
    public void RefreshAllText()
    {
        needsFullUpdate = true;
    }
}
