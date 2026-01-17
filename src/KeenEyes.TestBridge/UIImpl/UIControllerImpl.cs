using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.UI;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.TestBridge.UIImpl;

/// <summary>
/// In-process implementation of <see cref="IUIController"/>.
/// </summary>
internal sealed class UIControllerImpl(World world) : IUIController
{
    #region Statistics

    /// <inheritdoc />
    public Task<UIStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<UIContext>(out var ui))
        {
            return Task.FromResult(new UIStatisticsSnapshot
            {
                TotalElementCount = 0,
                VisibleElementCount = 0,
                InteractableCount = 0,
                FocusedElementId = null
            });
        }

        var totalCount = 0;
        var visibleCount = 0;
        var interactableCount = 0;

        foreach (var entity in world.Query<UIElement>())
        {
            totalCount++;

            ref readonly var element = ref world.Get<UIElement>(entity);
            if (element.Visible && !world.Has<UIHiddenTag>(entity))
            {
                visibleCount++;
            }

            if (world.Has<UIInteractable>(entity))
            {
                interactableCount++;
            }
        }

        return Task.FromResult(new UIStatisticsSnapshot
        {
            TotalElementCount = totalCount,
            VisibleElementCount = visibleCount,
            InteractableCount = interactableCount,
            FocusedElementId = ui.HasFocus ? ui.FocusedEntity.Id : null
        });
    }

    #endregion

    #region Focus Management

    /// <inheritdoc />
    public Task<int?> GetFocusedElementAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<UIContext>(out var ui))
        {
            return Task.FromResult<int?>(null);
        }

        return Task.FromResult<int?>(ui.HasFocus ? ui.FocusedEntity.Id : null);
    }

    /// <inheritdoc />
    public Task<bool> SetFocusAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<UIContext>(out var ui))
        {
            return Task.FromResult(false);
        }

        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity))
        {
            return Task.FromResult(false);
        }

        if (!world.Has<UIInteractable>(entity))
        {
            return Task.FromResult(false);
        }

        ui.RequestFocus(entity);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> ClearFocusAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<UIContext>(out var ui))
        {
            return Task.FromResult(false);
        }

        if (!ui.HasFocus)
        {
            return Task.FromResult(false);
        }

        ui.ClearFocus();
        return Task.FromResult(true);
    }

    #endregion

    #region Element Inspection

    /// <inheritdoc />
    public Task<UIElementSnapshot?> GetElementAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UIElement>(entity))
        {
            return Task.FromResult<UIElementSnapshot?>(null);
        }

        ref readonly var element = ref world.Get<UIElement>(entity);

        var name = world.GetName(entity);
        var parent = world.GetParent(entity);
        var children = world.GetChildren(entity).ToList();

        return Task.FromResult<UIElementSnapshot?>(new UIElementSnapshot
        {
            EntityId = entityId,
            Name = name,
            IsVisible = element.Visible && !world.Has<UIHiddenTag>(entity),
            IsRaycastTarget = element.RaycastTarget,
            ParentId = parent.IsValid ? parent.Id : null,
            ChildIds = children.Select(c => c.Id).ToList(),
            HasInteractable = world.Has<UIInteractable>(entity),
            HasText = world.Has<UIText>(entity),
            HasStyle = world.Has<UIStyle>(entity)
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UIElementSnapshot>> GetElementTreeAsync(int? rootEntityId = null, CancellationToken cancellationToken = default)
    {
        var elements = new List<UIElementSnapshot>();

        if (rootEntityId.HasValue)
        {
            // Get tree starting from specific root
            var rootEntity = new Entity(rootEntityId.Value, 0);
            if (world.IsAlive(rootEntity) && world.Has<UIElement>(rootEntity))
            {
                CollectElementTree(rootEntity, elements);
            }
        }
        else
        {
            // Get all root canvases
            foreach (var rootEntity in world.Query<UIElement, UIRootTag>())
            {
                CollectElementTree(rootEntity, elements);
            }
        }

        return Task.FromResult<IReadOnlyList<UIElementSnapshot>>(elements);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetRootElementsAsync(CancellationToken cancellationToken = default)
    {
        var roots = new List<int>();

        foreach (var rootEntity in world.Query<UIElement, UIRootTag>())
        {
            roots.Add(rootEntity.Id);
        }

        return Task.FromResult<IReadOnlyList<int>>(roots);
    }

    /// <inheritdoc />
    public Task<UIBoundsSnapshot?> GetElementBoundsAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UIRect>(entity))
        {
            return Task.FromResult<UIBoundsSnapshot?>(null);
        }

        ref readonly var rect = ref world.Get<UIRect>(entity);

        return Task.FromResult<UIBoundsSnapshot?>(new UIBoundsSnapshot
        {
            X = rect.ComputedBounds.X,
            Y = rect.ComputedBounds.Y,
            Width = rect.ComputedBounds.Width,
            Height = rect.ComputedBounds.Height,
            LocalZIndex = rect.LocalZIndex
        });
    }

    /// <inheritdoc />
    public Task<UIStyleSnapshot?> GetElementStyleAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UIStyle>(entity))
        {
            return Task.FromResult<UIStyleSnapshot?>(null);
        }

        ref readonly var style = ref world.Get<UIStyle>(entity);

        return Task.FromResult<UIStyleSnapshot?>(new UIStyleSnapshot
        {
            BackgroundColorR = style.BackgroundColor.X,
            BackgroundColorG = style.BackgroundColor.Y,
            BackgroundColorB = style.BackgroundColor.Z,
            BackgroundColorA = style.BackgroundColor.W,
            BorderWidth = style.BorderWidth,
            CornerRadius = style.CornerRadius,
            PaddingLeft = style.Padding.Left,
            PaddingRight = style.Padding.Right,
            PaddingTop = style.Padding.Top,
            PaddingBottom = style.Padding.Bottom
        });
    }

    /// <inheritdoc />
    public Task<UIInteractionSnapshot?> GetInteractionStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UIInteractable>(entity))
        {
            return Task.FromResult<UIInteractionSnapshot?>(null);
        }

        ref readonly var interactable = ref world.Get<UIInteractable>(entity);

        return Task.FromResult<UIInteractionSnapshot?>(new UIInteractionSnapshot
        {
            EntityId = entityId,
            CanFocus = interactable.CanFocus,
            CanClick = interactable.CanClick,
            CanDrag = interactable.CanDrag,
            IsHovered = interactable.IsHovered,
            IsPressed = interactable.IsPressed,
            IsFocused = interactable.IsFocused,
            IsDragging = interactable.IsDragging
        });
    }

    #endregion

    #region Hit Testing

    /// <inheritdoc />
    public Task<int?> HitTestAsync(float x, float y, CancellationToken cancellationToken = default)
    {
        var hitTester = new UIHitTester(world);
        var hit = hitTester.HitTest(new Vector2(x, y));

        return Task.FromResult<int?>(hit.IsValid ? hit.Id : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> HitTestAllAsync(float x, float y, CancellationToken cancellationToken = default)
    {
        var hitTester = new UIHitTester(world);
        var hits = hitTester.HitTestAll(new Vector2(x, y));

        return Task.FromResult<IReadOnlyList<int>>(hits.Select(e => e.Id).ToList());
    }

    #endregion

    #region Element Search

    /// <inheritdoc />
    public Task<int?> FindElementByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        foreach (var entity in world.Query<UIElement>())
        {
            var entityName = world.GetName(entity);
            if (string.Equals(entityName, name, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<int?>(entity.Id);
            }
        }

        return Task.FromResult<int?>(null);
    }

    #endregion

    #region Interaction Simulation

    /// <inheritdoc />
    public Task<bool> SimulateClickAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<UIInteractable>(entity))
        {
            return Task.FromResult(false);
        }

        ref var interactable = ref world.Get<UIInteractable>(entity);

        if (!interactable.CanClick)
        {
            return Task.FromResult(false);
        }

        // Simulate click by setting the pending event
        interactable.PendingEvents |= UIEventType.Click;

        // Get the element center position for the click event
        var clickPosition = Vector2.Zero;
        if (world.Has<UIRect>(entity))
        {
            ref readonly var rect = ref world.Get<UIRect>(entity);
            clickPosition = rect.ComputedBounds.Center;
        }

        // Send click event
        world.Send(new UIClickEvent(entity, clickPosition, MouseButton.Left));

        return Task.FromResult(true);
    }

    #endregion

    #region Helper Methods

    private void CollectElementTree(Entity entity, List<UIElementSnapshot> elements)
    {
        if (!world.Has<UIElement>(entity))
        {
            return;
        }

        ref readonly var element = ref world.Get<UIElement>(entity);

        var name = world.GetName(entity);
        var parent = world.GetParent(entity);
        var children = world.GetChildren(entity).ToList();

        elements.Add(new UIElementSnapshot
        {
            EntityId = entity.Id,
            Name = name,
            IsVisible = element.Visible && !world.Has<UIHiddenTag>(entity),
            IsRaycastTarget = element.RaycastTarget,
            ParentId = parent.IsValid ? parent.Id : null,
            ChildIds = children.Select(c => c.Id).ToList(),
            HasInteractable = world.Has<UIInteractable>(entity),
            HasText = world.Has<UIText>(entity),
            HasStyle = world.Has<UIStyle>(entity)
        });

        // Recursively collect children
        foreach (var child in children)
        {
            if (world.Has<UIElement>(child))
            {
                CollectElementTree(child, elements);
            }
        }
    }

    #endregion
}
