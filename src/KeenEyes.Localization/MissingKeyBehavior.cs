namespace KeenEyes.Localization;

/// <summary>
/// Defines how the localization system handles requests for missing translation keys.
/// </summary>
public enum MissingKeyBehavior
{
    /// <summary>
    /// Returns the key itself when no translation is found.
    /// </summary>
    /// <remarks>
    /// This is useful during development to easily spot missing translations.
    /// Example: Requesting "menu.start" returns "menu.start" if not found.
    /// </remarks>
    ReturnKey,

    /// <summary>
    /// Returns an empty string when no translation is found.
    /// </summary>
    ReturnEmpty,

    /// <summary>
    /// Returns a formatted placeholder indicating the missing key.
    /// </summary>
    /// <remarks>
    /// Example: Requesting "menu.start" returns "[MISSING: menu.start]".
    /// </remarks>
    ReturnPlaceholder,

    /// <summary>
    /// Throws a <see cref="KeyNotFoundException"/> when no translation is found.
    /// </summary>
    /// <remarks>
    /// Useful for catching missing translations during testing.
    /// </remarks>
    ThrowException
}
