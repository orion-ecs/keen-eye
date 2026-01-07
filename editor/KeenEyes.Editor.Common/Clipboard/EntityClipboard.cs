using KeenEyes.Editor.Common.Serialization;

namespace KeenEyes.Editor.Common.Clipboard;

/// <summary>
/// Manages entity clipboard operations for cut, copy, and paste functionality.
/// </summary>
/// <remarks>
/// <para>
/// The clipboard maintains snapshots of entities that have been cut or copied.
/// These snapshots can be pasted to create new entities with the same component
/// data and hierarchy structure.
/// </para>
/// <para>
/// The clipboard is designed for single-user editor scenarios and maintains
/// a single clip at a time. Cutting or copying new entities replaces any
/// previous clipboard contents.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var clipboard = new EntityClipboard();
///
/// // Copy selected entities
/// clipboard.Copy(world, selectedEntities);
///
/// // Paste to create duplicates
/// var pasted = clipboard.Paste(world);
/// </code>
/// </example>
public sealed class EntityClipboard
{
    private IReadOnlyList<EntitySnapshot>? clipboardContent;
    private ClipboardOperation lastOperation;

    /// <summary>
    /// Gets whether the clipboard has content that can be pasted.
    /// </summary>
    public bool HasContent => clipboardContent is { Count: > 0 };

    /// <summary>
    /// Gets the number of entities currently in the clipboard.
    /// </summary>
    public int Count => clipboardContent?.Count ?? 0;

    /// <summary>
    /// Gets the operation that placed content in the clipboard.
    /// </summary>
    public ClipboardOperation LastOperation => lastOperation;

    /// <summary>
    /// Gets the timestamp when the clipboard content was captured.
    /// </summary>
    public DateTimeOffset? ContentTimestamp => clipboardContent?.FirstOrDefault()?.Timestamp;

    /// <summary>
    /// Raised when clipboard content changes.
    /// </summary>
    public event Action<ClipboardChangedEventArgs>? ClipboardChanged;

    /// <summary>
    /// Copies the specified entities to the clipboard.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">The entities to copy.</param>
    /// <param name="includeChildren">Whether to include child entities.</param>
    /// <returns>The number of entities copied.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="entities"/> is null.
    /// </exception>
    /// <remarks>
    /// Dead entities in the input are silently skipped.
    /// </remarks>
    public int Copy(IWorld world, IEnumerable<Entity> entities, bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entities);

        var snapshots = EntitySerializer.CaptureEntities(world, entities, includeChildren);
        SetContent(snapshots, ClipboardOperation.Copy);
        return snapshots.Count;
    }

    /// <summary>
    /// Copies a single entity to the clipboard.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to copy.</param>
    /// <param name="includeChildren">Whether to include child entities.</param>
    /// <returns>True if the entity was copied; false if it was not alive.</returns>
    public bool Copy(IWorld world, Entity entity, bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity))
        {
            return false;
        }

        var snapshot = EntitySerializer.CaptureEntity(world, entity, includeChildren);
        SetContent([snapshot], ClipboardOperation.Copy);
        return true;
    }

    /// <summary>
    /// Cuts the specified entities to the clipboard (copy then delete).
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">The entities to cut.</param>
    /// <param name="includeChildren">Whether to include child entities.</param>
    /// <returns>The number of entities cut.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="entities"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Entities are first captured to the clipboard, then deleted from the world.
    /// If an entity has children and <paramref name="includeChildren"/> is true,
    /// the entire subtree is captured and deleted.
    /// </para>
    /// <para>
    /// Dead entities in the input are silently skipped.
    /// </para>
    /// </remarks>
    public int Cut(IWorld world, IEnumerable<Entity> entities, bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(entities);

        // Capture entities first
        var entityList = entities.Where(e => world.IsAlive(e)).ToList();
        var snapshots = EntitySerializer.CaptureEntities(world, entityList, includeChildren);

        // Delete the original entities
        foreach (var entity in entityList)
        {
            if (world.IsAlive(entity))
            {
                // Use recursive despawn if available and children were included
                if (includeChildren && world is KeenEyes.Capabilities.IHierarchyCapability hierarchy)
                {
                    hierarchy.DespawnRecursive(entity);
                }
                else
                {
                    world.Despawn(entity);
                }
            }
        }

        SetContent(snapshots, ClipboardOperation.Cut);
        return snapshots.Count;
    }

    /// <summary>
    /// Cuts a single entity to the clipboard (copy then delete).
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to cut.</param>
    /// <param name="includeChildren">Whether to include child entities.</param>
    /// <returns>True if the entity was cut; false if it was not alive.</returns>
    public bool Cut(IWorld world, Entity entity, bool includeChildren = true)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity))
        {
            return false;
        }

        var snapshot = EntitySerializer.CaptureEntity(world, entity, includeChildren);

        // Delete the original
        if (includeChildren && world is KeenEyes.Capabilities.IHierarchyCapability hierarchy)
        {
            hierarchy.DespawnRecursive(entity);
        }
        else
        {
            world.Despawn(entity);
        }

        SetContent([snapshot], ClipboardOperation.Cut);
        return true;
    }

    /// <summary>
    /// Pastes the clipboard content into the world.
    /// </summary>
    /// <param name="world">The world to paste into.</param>
    /// <param name="parent">Optional parent for the pasted entities.</param>
    /// <returns>The newly created entities, or an empty list if the clipboard is empty.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Pasting does not clear the clipboard, allowing multiple paste operations.
    /// Each paste creates new entities with new IDs.
    /// </para>
    /// <para>
    /// If entity names were captured, unique names are generated to avoid conflicts.
    /// </para>
    /// </remarks>
    public IReadOnlyList<Entity> Paste(IWorld world, Entity? parent = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (clipboardContent is null || clipboardContent.Count == 0)
        {
            return [];
        }

        return EntitySerializer.RestoreEntities(world, clipboardContent, parent);
    }

    /// <summary>
    /// Clears the clipboard content.
    /// </summary>
    public void Clear()
    {
        if (clipboardContent is not null)
        {
            var previousCount = clipboardContent.Count;
            clipboardContent = null;
            lastOperation = ClipboardOperation.None;

            ClipboardChanged?.Invoke(new ClipboardChangedEventArgs(
                ClipboardOperation.None,
                0,
                previousCount));
        }
    }

    /// <summary>
    /// Gets a preview of the clipboard content without restoring it.
    /// </summary>
    /// <returns>
    /// The current clipboard snapshots, or null if the clipboard is empty.
    /// </returns>
    /// <remarks>
    /// Use this for UI preview purposes. The returned snapshots should not be modified.
    /// </remarks>
    public IReadOnlyList<EntitySnapshot>? GetContent()
    {
        return clipboardContent;
    }

    private void SetContent(IReadOnlyList<EntitySnapshot> snapshots, ClipboardOperation operation)
    {
        var previousCount = clipboardContent?.Count ?? 0;
        clipboardContent = snapshots;
        lastOperation = operation;

        ClipboardChanged?.Invoke(new ClipboardChangedEventArgs(
            operation,
            snapshots.Count,
            previousCount));
    }
}

/// <summary>
/// Specifies the type of clipboard operation.
/// </summary>
public enum ClipboardOperation
{
    /// <summary>
    /// No operation or clipboard cleared.
    /// </summary>
    None,

    /// <summary>
    /// Entities were copied to the clipboard.
    /// </summary>
    Copy,

    /// <summary>
    /// Entities were cut to the clipboard.
    /// </summary>
    Cut
}

/// <summary>
/// Event arguments for clipboard content changes.
/// </summary>
/// <param name="Operation">The operation that changed the clipboard.</param>
/// <param name="CurrentCount">The number of entities currently in the clipboard.</param>
/// <param name="PreviousCount">The number of entities previously in the clipboard.</param>
public readonly record struct ClipboardChangedEventArgs(
    ClipboardOperation Operation,
    int CurrentCount,
    int PreviousCount);
