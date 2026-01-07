using System.Numerics;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that processes input events and updates UI interaction states.
/// </summary>
/// <remarks>
/// <para>
/// The input system performs hit testing to determine which UI element is under the cursor,
/// updates hover/pressed states on <see cref="UIInteractable"/> components, and fires
/// appropriate events.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase to process
/// input before other systems.
/// </para>
/// </remarks>
public sealed class UIInputSystem : SystemBase
{
    private UIHitTester? hitTester;
    private UIContext? uiContext;
    private IInputContext? inputContext;
    private Entity hoveredEntity = Entity.Null;
    private Entity pressedEntity = Entity.Null;
    private Vector2 dragStartPosition;
    private Vector2 lastDragPosition;
    private bool isDragging;
    private double lastClickTime;
    private const double DoubleClickTime = 0.3; // seconds

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        hitTester = new UIHitTester(World);
    }

    /// <inheritdoc />
    protected override void OnBeforeUpdate(float deltaTime)
    {
        // Clear pending events from previous frame
        foreach (var entity in World.Query<UIInteractable>())
        {
            ref var interactable = ref World.Get<UIInteractable>(entity);
            interactable.PendingEvents = UIEventType.None;
        }
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (inputContext is null && !World.TryGetExtension(out inputContext))
        {
            return;
        }

        if (uiContext is null && !World.TryGetExtension(out uiContext))
        {
            return;
        }

        if (hitTester is null)
        {
            return;
        }

        // Local copies for null safety - fields are verified non-null above
        var input = inputContext!;
        var mouse = input.Mouse;
        var mousePos = mouse.Position;

        // Hit test
        var hitEntity = hitTester.HitTest(mousePos);

        // Handle hover changes
        ProcessHover(hitEntity, mousePos);

        // Handle mouse button input
        ProcessMouseInput(mouse, mousePos, hitEntity);
    }

    private void ProcessHover(Entity hitEntity, Vector2 mousePos)
    {
        // Hover exit
        if (hoveredEntity.IsValid &&
            hoveredEntity != hitEntity &&
            World.IsAlive(hoveredEntity) &&
            World.Has<UIInteractable>(hoveredEntity))
        {
            ref var interactable = ref World.Get<UIInteractable>(hoveredEntity);
            interactable.State &= ~UIInteractionState.Hovered;
            interactable.PendingEvents |= UIEventType.PointerExit;

            World.Send(new UIPointerExitEvent(hoveredEntity));
        }

        // Hover enter
        if (hitEntity.IsValid &&
            hitEntity != hoveredEntity &&
            World.Has<UIInteractable>(hitEntity))
        {
            ref var interactable = ref World.Get<UIInteractable>(hitEntity);
            interactable.State |= UIInteractionState.Hovered;
            interactable.PendingEvents |= UIEventType.PointerEnter;

            World.Send(new UIPointerEnterEvent(hitEntity, mousePos));
        }

        hoveredEntity = hitEntity;
    }

    private void ProcessMouseInput(IMouse mouse, Vector2 mousePos, Entity hitEntity)
    {
        // Check for mouse button down
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            if (!pressedEntity.IsValid && hitEntity.IsValid && World.Has<UIInteractable>(hitEntity))
            {
                ref var interactable = ref World.Get<UIInteractable>(hitEntity);

                if (interactable.CanClick || interactable.CanDrag)
                {
                    pressedEntity = hitEntity;
                    interactable.State |= UIInteractionState.Pressed;
                    interactable.PendingEvents |= UIEventType.PointerDown;
                    dragStartPosition = mousePos;
                    lastDragPosition = mousePos;

                    // Request focus if element is focusable
                    if (interactable.CanFocus && uiContext is not null)
                    {
                        uiContext.RequestFocus(hitEntity);
                    }
                }
            }

            // Handle dragging
            if (pressedEntity.IsValid && World.Has<UIInteractable>(pressedEntity))
            {
                ref var interactable = ref World.Get<UIInteractable>(pressedEntity);

                if (interactable.CanDrag && !isDragging)
                {
                    // Start drag if moved enough
                    var delta = mousePos - dragStartPosition;
                    if (delta.LengthSquared() > 25) // 5 pixel threshold squared
                    {
                        isDragging = true;
                        interactable.State |= UIInteractionState.Dragging;
                        interactable.PendingEvents |= UIEventType.DragStart;

                        World.Send(new UIDragStartEvent(pressedEntity, dragStartPosition));
                    }
                }

                if (isDragging)
                {
                    var delta = mousePos - lastDragPosition;
                    lastDragPosition = mousePos;
                    World.Send(new UIDragEvent(pressedEntity, mousePos, delta));
                }
            }
        }
        else
        {
            // Mouse button released
            if (pressedEntity.IsValid && World.IsAlive(pressedEntity) && World.Has<UIInteractable>(pressedEntity))
            {
                ref var interactable = ref World.Get<UIInteractable>(pressedEntity);
                interactable.State &= ~UIInteractionState.Pressed;
                interactable.PendingEvents |= UIEventType.PointerUp;

                if (isDragging)
                {
                    // End drag
                    interactable.State &= ~UIInteractionState.Dragging;
                    interactable.PendingEvents |= UIEventType.DragEnd;
                    isDragging = false;

                    World.Send(new UIDragEndEvent(pressedEntity, mousePos));
                }
                else if (interactable.CanClick && pressedEntity == hitEntity)
                {
                    // Click occurred
                    interactable.PendingEvents |= UIEventType.Click;

                    // Check for double-click
                    var currentTime = Environment.TickCount / 1000.0;
                    if (currentTime - lastClickTime < DoubleClickTime)
                    {
                        interactable.PendingEvents |= UIEventType.DoubleClick;
                    }
                    lastClickTime = currentTime;

                    World.Send(new UIClickEvent(pressedEntity, mousePos, MouseButton.Left));
                }
            }

            pressedEntity = Entity.Null;
        }
    }
}
