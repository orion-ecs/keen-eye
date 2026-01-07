using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// Provides UI system state and helper methods for managing UI elements.
/// </summary>
/// <remarks>
/// <para>
/// UIContext is registered as a world extension by the UIPlugin. It tracks the currently
/// focused element and provides methods for creating UI canvases.
/// </para>
/// <para>
/// Access the context via <c>world.GetExtension&lt;UIContext&gt;()</c> after installing
/// the UI plugin.
/// </para>
/// </remarks>
[PluginExtension("UI")]
public sealed class UIContext
{
    private readonly IWorld world;
    private Entity focusedEntity;

    /// <summary>
    /// Gets the currently focused UI element, or <see cref="Entity.Null"/> if none.
    /// </summary>
    public Entity FocusedEntity => focusedEntity;

    /// <summary>
    /// Gets whether any UI element is currently focused.
    /// </summary>
    public bool HasFocus => focusedEntity.IsValid;

    internal UIContext(IWorld world)
    {
        this.world = world;
        focusedEntity = Entity.Null;
    }

    /// <summary>
    /// Requests focus for a UI element.
    /// </summary>
    /// <param name="entity">The entity to focus.</param>
    /// <remarks>
    /// <para>
    /// The entity must have a <see cref="UIInteractable"/> component with
    /// <see cref="UIInteractable.CanFocus"/> set to true.
    /// </para>
    /// <para>
    /// This method updates the focus state and fires appropriate events via
    /// the messaging system.
    /// </para>
    /// </remarks>
    public void RequestFocus(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            return;
        }

        // Entity must be interactable and focusable
        if (!world.Has<UIInteractable>(entity))
        {
            return;
        }

        ref var interactable = ref world.Get<UIInteractable>(entity);
        if (!interactable.CanFocus)
        {
            return;
        }

        // Don't refocus the same entity
        if (focusedEntity == entity)
        {
            return;
        }

        var previousFocused = focusedEntity;

        // Clear previous focus
        if (previousFocused.IsValid && world.IsAlive(previousFocused))
        {
            ClearFocusInternal(previousFocused, entity);
        }

        // Set new focus
        focusedEntity = entity;
        interactable.State |= UIInteractionState.Focused;
        interactable.PendingEvents |= UIEventType.FocusGained;
        world.Add(entity, new UIFocusedTag());

        // Send focus event
        world.Send(new UIFocusGainedEvent(entity, previousFocused.IsValid ? previousFocused : null));
    }

    /// <summary>
    /// Clears focus from the currently focused element.
    /// </summary>
    public void ClearFocus()
    {
        if (!focusedEntity.IsValid)
        {
            return;
        }

        if (world.IsAlive(focusedEntity))
        {
            ClearFocusInternal(focusedEntity, Entity.Null);
        }

        focusedEntity = Entity.Null;
    }

    private void ClearFocusInternal(Entity entity, Entity nextFocused)
    {
        if (world.Has<UIInteractable>(entity))
        {
            ref var interactable = ref world.Get<UIInteractable>(entity);
            interactable.State &= ~UIInteractionState.Focused;
            interactable.PendingEvents |= UIEventType.FocusLost;
        }

        if (world.Has<UIFocusedTag>(entity))
        {
            world.Remove<UIFocusedTag>(entity);
        }

        // Send focus lost event
        world.Send(new UIFocusLostEvent(entity, nextFocused.IsValid ? nextFocused : null));
    }

    /// <summary>
    /// Marks a UI element's layout as dirty, triggering recalculation.
    /// </summary>
    /// <param name="entity">The entity whose layout needs recalculation.</param>
    public void SetLayoutDirty(Entity entity)
    {
        if (!world.IsAlive(entity))
        {
            return;
        }

        if (!world.Has<UILayoutDirtyTag>(entity))
        {
            world.Add(entity, new UILayoutDirtyTag());
        }
    }

    /// <summary>
    /// Creates a new UI canvas (root element).
    /// </summary>
    /// <param name="name">Optional name for the canvas entity.</param>
    /// <returns>The canvas entity.</returns>
    /// <remarks>
    /// <para>
    /// A canvas serves as the root of a UI hierarchy. All UI elements should be
    /// descendants of a canvas entity.
    /// </para>
    /// <para>
    /// The canvas is created with full-screen stretch bounds by default.
    /// </para>
    /// </remarks>
    public Entity CreateCanvas(string? name = null)
    {
        var builder = name is not null ? world.Spawn(name) : world.Spawn();

        return builder
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();
    }

    /// <summary>
    /// Internal: Called by UIInputSystem when focus changes due to user interaction.
    /// </summary>
    internal void SetFocusedEntityDirect(Entity entity)
    {
        focusedEntity = entity;
    }
}
