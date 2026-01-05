namespace KeenEyes.Localization;

/// <summary>
/// Tag component that marks an entity for localized text updates.
/// </summary>
/// <remarks>
/// <para>
/// This tag can be used in queries to efficiently find all entities
/// that need text updates when the locale changes.
/// </para>
/// <para>
/// The <see cref="LocalizedTextSystem"/> automatically adds this tag
/// when a <see cref="LocalizedText"/> component is added, and removes
/// it when the component is removed.
/// </para>
/// <para>
/// Users typically don't need to add this tag manually - the system
/// handles it automatically. However, it can be useful for custom
/// queries and filtering.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query all entities with localized text
/// foreach (var entity in world.Query&lt;LocalizedTextTag&gt;())
/// {
///     // Process localized text entities
/// }
/// </code>
/// </example>
[TagComponent]
public partial struct LocalizedTextTag : ITagComponent;
