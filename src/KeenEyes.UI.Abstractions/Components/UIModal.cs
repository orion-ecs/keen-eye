namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that identifies a modal dialog container.
/// </summary>
/// <remarks>
/// <para>
/// Modals are dialog windows that block interaction with the rest of the UI
/// until dismissed. They typically appear centered on screen with a semi-transparent
/// backdrop behind them.
/// </para>
/// <para>
/// The modal system handles backdrop clicks (to close if allowed), Escape key
/// (to close if allowed), and button clicks to close with a result.
/// </para>
/// </remarks>
/// <param name="title">The modal title displayed in the header.</param>
/// <param name="closeOnBackdropClick">Whether clicking the backdrop closes the modal.</param>
/// <param name="closeOnEscape">Whether pressing Escape closes the modal.</param>
public struct UIModal(string title, bool closeOnBackdropClick = true, bool closeOnEscape = true) : IComponent
{
    /// <summary>
    /// The modal title displayed in the header.
    /// </summary>
    public string Title = title;

    /// <summary>
    /// Whether clicking the backdrop closes the modal.
    /// </summary>
    public bool CloseOnBackdropClick = closeOnBackdropClick;

    /// <summary>
    /// Whether pressing Escape closes the modal.
    /// </summary>
    public bool CloseOnEscape = closeOnEscape;

    /// <summary>
    /// Whether the modal is currently open/visible.
    /// </summary>
    public bool IsOpen = false;

    /// <summary>
    /// Reference to the backdrop entity.
    /// </summary>
    public Entity Backdrop = Entity.Null;

    /// <summary>
    /// Reference to the content container entity.
    /// </summary>
    public Entity ContentContainer = Entity.Null;
}

/// <summary>
/// Component that identifies a modal's backdrop for click-to-close handling.
/// </summary>
/// <param name="modal">The modal entity this backdrop belongs to.</param>
public struct UIModalBackdrop(Entity modal) : IComponent
{
    /// <summary>
    /// Reference to the modal entity.
    /// </summary>
    public Entity Modal = modal;
}

/// <summary>
/// Component that identifies a modal's close button.
/// </summary>
/// <param name="modal">The modal entity this close button belongs to.</param>
public struct UIModalCloseButton(Entity modal) : IComponent
{
    /// <summary>
    /// Reference to the modal entity.
    /// </summary>
    public Entity Modal = modal;
}

/// <summary>
/// Component that identifies a modal action button (OK, Cancel, etc.).
/// </summary>
/// <param name="modal">The modal entity this button belongs to.</param>
/// <param name="result">The result value when this button is clicked.</param>
public struct UIModalButton(Entity modal, ModalResult result) : IComponent
{
    /// <summary>
    /// Reference to the modal entity.
    /// </summary>
    public Entity Modal = modal;

    /// <summary>
    /// The result value returned when this button is clicked.
    /// </summary>
    public ModalResult Result = result;
}

/// <summary>
/// Predefined modal result values.
/// </summary>
public enum ModalResult
{
    /// <summary>No result (modal was closed without action).</summary>
    None = 0,

    /// <summary>OK/Confirm was clicked.</summary>
    OK = 1,

    /// <summary>Cancel was clicked.</summary>
    Cancel = 2,

    /// <summary>Yes was clicked.</summary>
    Yes = 3,

    /// <summary>No was clicked.</summary>
    No = 4,

    /// <summary>Custom result 1.</summary>
    Custom1 = 100,

    /// <summary>Custom result 2.</summary>
    Custom2 = 101,

    /// <summary>Custom result 3.</summary>
    Custom3 = 102
}
