namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Defines the visual type of a toast notification.
/// </summary>
public enum ToastType
{
    /// <summary>Informational toast (default).</summary>
    Info,

    /// <summary>Success/confirmation toast.</summary>
    Success,

    /// <summary>Warning toast.</summary>
    Warning,

    /// <summary>Error/failure toast.</summary>
    Error
}

/// <summary>
/// Defines the position where toasts appear.
/// </summary>
public enum ToastPosition
{
    /// <summary>Top-left corner of the container.</summary>
    TopLeft,

    /// <summary>Top-center of the container.</summary>
    TopCenter,

    /// <summary>Top-right corner of the container.</summary>
    TopRight,

    /// <summary>Bottom-left corner of the container.</summary>
    BottomLeft,

    /// <summary>Bottom-center of the container.</summary>
    BottomCenter,

    /// <summary>Bottom-right corner of the container.</summary>
    BottomRight
}

/// <summary>
/// Component that identifies a toast notification.
/// </summary>
/// <remarks>
/// <para>
/// Toasts are temporary notification messages that appear and automatically
/// disappear after a duration. They can also be manually dismissed.
/// </para>
/// <para>
/// Toasts are typically managed by a <see cref="UIToastContainer"/> which
/// handles positioning and stacking of multiple toasts.
/// </para>
/// </remarks>
/// <param name="message">The toast message text.</param>
/// <param name="duration">How long the toast displays in seconds (0 = indefinite).</param>
public struct UIToast(string message, float duration = 3f) : IComponent
{
    /// <summary>
    /// The toast message text.
    /// </summary>
    public string Message = message;

    /// <summary>
    /// The toast display duration in seconds. 0 means indefinite (manual dismiss only).
    /// </summary>
    public float Duration = duration;

    /// <summary>
    /// The type of toast (affects visual styling).
    /// </summary>
    public ToastType Type = ToastType.Info;

    /// <summary>
    /// Whether the toast can be dismissed by clicking on it.
    /// </summary>
    public bool CanDismiss = true;

    /// <summary>
    /// The title of the toast (optional).
    /// </summary>
    public string? Title = null;

    /// <summary>
    /// Reference to the container entity (set by the system).
    /// </summary>
    public Entity Container = Entity.Null;

    /// <summary>
    /// Time remaining before auto-dismiss (set and updated by the system).
    /// </summary>
    public float TimeRemaining = duration;

    /// <summary>
    /// Whether the toast has been marked for removal.
    /// </summary>
    public bool IsClosing = false;
}

/// <summary>
/// Component that identifies a toast container which manages toast stacking and positioning.
/// </summary>
/// <remarks>
/// <para>
/// The toast container manages the layout and lifecycle of toast notifications.
/// Multiple toasts stack vertically based on the container's position.
/// </para>
/// </remarks>
public struct UIToastContainer : IComponent
{
    /// <summary>
    /// The position where toasts appear.
    /// </summary>
    public ToastPosition Position = ToastPosition.TopRight;

    /// <summary>
    /// Maximum number of toasts to display simultaneously.
    /// Additional toasts are queued until space is available.
    /// </summary>
    public int MaxVisible = 5;

    /// <summary>
    /// Spacing between stacked toasts in pixels.
    /// </summary>
    public float Spacing = 10f;

    /// <summary>
    /// Margin from the edge of the container in pixels.
    /// </summary>
    public float Margin = 20f;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public UIToastContainer()
    {
    }
}

/// <summary>
/// Tag component marking an entity as a toast close button.
/// </summary>
/// <param name="toast">The toast entity this close button belongs to.</param>
public struct UIToastCloseButton(Entity toast) : IComponent
{
    /// <summary>
    /// Reference to the toast entity.
    /// </summary>
    public Entity Toast = toast;
}
