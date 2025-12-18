namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component for an accordion container with expandable/collapsible sections.
/// </summary>
/// <remarks>
/// <para>
/// Accordions display stacked panels where each section can be expanded or collapsed.
/// In single-expand mode, expanding one section collapses all others.
/// </para>
/// </remarks>
/// <param name="allowMultipleExpanded">Whether multiple sections can be expanded simultaneously.</param>
public struct UIAccordion(bool allowMultipleExpanded = false) : IComponent
{
    /// <summary>
    /// Whether multiple sections can be expanded at the same time.
    /// When false, expanding a section automatically collapses others.
    /// </summary>
    public bool AllowMultipleExpanded = allowMultipleExpanded;

    /// <summary>
    /// Container entity for accordion sections.
    /// </summary>
    public Entity ContentContainer = Entity.Null;

    /// <summary>
    /// Number of sections in this accordion.
    /// </summary>
    public int SectionCount = 0;
}

/// <summary>
/// Component for a section within an accordion.
/// </summary>
/// <remarks>
/// <para>
/// Each section has a header that can be clicked to expand/collapse,
/// and a content area that shows when expanded.
/// </para>
/// </remarks>
/// <param name="accordion">The owning accordion entity.</param>
/// <param name="title">The section title displayed in the header.</param>
public struct UIAccordionSection(Entity accordion, string title) : IComponent
{
    /// <summary>
    /// The accordion this section belongs to.
    /// </summary>
    public Entity Accordion = accordion;

    /// <summary>
    /// The section title displayed in the header.
    /// </summary>
    public string Title = title;

    /// <summary>
    /// Whether this section is currently expanded.
    /// </summary>
    public bool IsExpanded = false;

    /// <summary>
    /// Index of this section among siblings.
    /// </summary>
    public int Index = 0;

    /// <summary>
    /// The header entity for this section.
    /// </summary>
    public Entity Header = Entity.Null;

    /// <summary>
    /// The content container entity for this section.
    /// </summary>
    public Entity ContentContainer = Entity.Null;
}

/// <summary>
/// Tag for the clickable header of an accordion section.
/// </summary>
public struct UIAccordionHeaderTag : ITagComponent;

/// <summary>
/// Tag for the expand/collapse arrow in an accordion section header.
/// </summary>
public struct UIAccordionArrowTag : ITagComponent;

/// <summary>
/// Tag for the content container of an accordion section.
/// </summary>
public struct UIAccordionContentTag : ITagComponent;
