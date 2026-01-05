namespace KeenEyes.Localization;

/// <summary>
/// Event raised when the active locale changes.
/// </summary>
/// <remarks>
/// <para>
/// Subscribe to this event via the world's event bus to react to locale changes.
/// This is typically used to update UI text when the player changes language settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// world.Events.Subscribe&lt;LocaleChangedEvent&gt;(e =>
/// {
///     Console.WriteLine($"Locale changed from {e.PreviousLocale} to {e.NewLocale}");
/// });
/// </code>
/// </example>
/// <param name="PreviousLocale">The locale that was active before the change.</param>
/// <param name="NewLocale">The newly active locale.</param>
public readonly record struct LocaleChangedEvent(Locale PreviousLocale, Locale NewLocale);
