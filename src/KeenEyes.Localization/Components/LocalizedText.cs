namespace KeenEyes.Localization;

/// <summary>
/// Component that marks an entity as having localized text content.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to UI entities that display text which should be
/// automatically updated when the locale changes.
/// </para>
/// <para>
/// The <see cref="LocalizedTextSystem"/> watches for locale changes and updates
/// any associated <c>UIText</c> components with the new translations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a UI element with localized text
/// var button = world.Spawn()
///     .With(UIElement.Create())
///     .With(UIText.Create(""))
///     .With(new LocalizedText { Key = "menu.start" })
///     .Build();
///
/// // The LocalizedTextSystem will automatically set UIText.Content
/// // to the translated value of "menu.start"
/// </code>
/// </example>
[Component]
public partial struct LocalizedText : IComponent
{
    /// <summary>
    /// The localization key to look up.
    /// </summary>
    /// <remarks>
    /// This should match a key in one of the registered string sources.
    /// Example: "menu.start", "dialog.greeting", "item.sword.name"
    /// </remarks>
    public string Key;

    /// <summary>
    /// Optional format arguments to substitute into the localized string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the localized string contains format placeholders like {0}, {1}, etc.,
    /// these arguments will be substituted in order.
    /// </para>
    /// <para>
    /// This is useful for dynamic content like player names, scores, or quantities.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // If "score.display" = "Score: {0} / {1}"
    /// new LocalizedText
    /// {
    ///     Key = "score.display",
    ///     FormatArgs = [currentScore, maxScore]
    /// }
    /// // Displays: "Score: 150 / 1000"
    /// </code>
    /// </example>
    public object?[]? FormatArgs;

    /// <summary>
    /// Creates a LocalizedText component with the specified key.
    /// </summary>
    /// <param name="key">The localization key.</param>
    public static LocalizedText Create(string key) => new()
    {
        Key = key,
        FormatArgs = null
    };

    /// <summary>
    /// Creates a LocalizedText component with the specified key and format arguments.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="args">Format arguments to substitute into the localized string.</param>
    public static LocalizedText Create(string key, params object?[] args) => new()
    {
        Key = key,
        FormatArgs = args
    };
}
