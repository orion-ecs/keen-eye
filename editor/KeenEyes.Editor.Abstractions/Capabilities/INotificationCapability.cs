namespace KeenEyes.Editor.Abstractions.Capabilities;

/// <summary>
/// Capability for displaying toast notifications and alerts in the editor.
/// </summary>
/// <remarks>
/// <para>
/// Toast notifications provide non-blocking feedback to users for operations
/// like successful saves, errors, warnings, or informational messages.
/// </para>
/// <para>
/// Notifications automatically dismiss after a configurable duration or can
/// be dismissed manually by the user.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Show a success notification
/// notifications.ShowSuccess("Scene saved successfully");
///
/// // Show an error with details
/// notifications.ShowError("Failed to install plugin", "Network connection failed");
///
/// // Show a notification with custom options
/// notifications.Show(new NotificationOptions
/// {
///     Title = "Build Complete",
///     Message = "Project built in 2.3 seconds",
///     Severity = NotificationSeverity.Info,
///     Duration = TimeSpan.FromSeconds(5),
///     Icon = "build"
/// });
/// </code>
/// </example>
public interface INotificationCapability : IEditorCapability
{
    /// <summary>
    /// Shows a notification with the specified options.
    /// </summary>
    /// <param name="options">The notification options.</param>
    /// <returns>
    /// A handle that can be used to update or dismiss the notification.
    /// </returns>
    NotificationHandle Show(NotificationOptions options);

    /// <summary>
    /// Shows a success notification.
    /// </summary>
    /// <param name="message">The notification message.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <returns>A handle to the notification.</returns>
    NotificationHandle ShowSuccess(string message, string? title = null);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <returns>A handle to the notification.</returns>
    NotificationHandle ShowError(string message, string? title = null);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    /// <param name="message">The warning message.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <returns>A handle to the notification.</returns>
    NotificationHandle ShowWarning(string message, string? title = null);

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    /// <param name="message">The info message.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <returns>A handle to the notification.</returns>
    NotificationHandle ShowInfo(string message, string? title = null);

    /// <summary>
    /// Dismisses a specific notification.
    /// </summary>
    /// <param name="handle">The notification handle.</param>
    void Dismiss(NotificationHandle handle);

    /// <summary>
    /// Dismisses all active notifications.
    /// </summary>
    void DismissAll();

    /// <summary>
    /// Gets the number of active notifications.
    /// </summary>
    int ActiveCount { get; }

    /// <summary>
    /// Event raised when a notification is shown.
    /// </summary>
    event Action<NotificationHandle>? NotificationShown;

    /// <summary>
    /// Event raised when a notification is dismissed.
    /// </summary>
    event Action<NotificationHandle>? NotificationDismissed;
}

/// <summary>
/// Options for creating a notification.
/// </summary>
public sealed record NotificationOptions
{
    /// <summary>
    /// Gets the notification message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the optional notification title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets the notification severity level.
    /// </summary>
    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Info;

    /// <summary>
    /// Gets how long the notification should be displayed before auto-dismissing.
    /// </summary>
    /// <remarks>
    /// Set to <see cref="TimeSpan.Zero"/> or negative to keep the notification
    /// visible until manually dismissed.
    /// </remarks>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Gets the optional icon identifier.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets whether the notification can be dismissed by clicking it.
    /// </summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>
    /// Gets an optional action to execute when the notification is clicked.
    /// </summary>
    public Action? OnClick { get; init; }

    /// <summary>
    /// Gets optional action buttons to display on the notification.
    /// </summary>
    public IReadOnlyList<NotificationAction>? Actions { get; init; }
}

/// <summary>
/// Severity levels for notifications.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Successful operation.
    /// </summary>
    Success,

    /// <summary>
    /// Warning that requires attention.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevented an operation.
    /// </summary>
    Error
}

/// <summary>
/// An action button that can be added to a notification.
/// </summary>
/// <param name="Label">The button label.</param>
/// <param name="Execute">The action to execute when clicked.</param>
/// <param name="DismissOnClick">Whether to dismiss the notification when clicked.</param>
public readonly record struct NotificationAction(
    string Label,
    Action Execute,
    bool DismissOnClick = true);

/// <summary>
/// A handle to an active notification.
/// </summary>
/// <remarks>
/// The handle can be used to update or dismiss a notification after it's shown.
/// The handle remains valid even after the notification is dismissed.
/// </remarks>
public sealed class NotificationHandle
{
    private static int nextId;

    /// <summary>
    /// Gets the unique identifier for this notification.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the notification options.
    /// </summary>
    public NotificationOptions Options { get; }

    /// <summary>
    /// Gets when the notification was shown.
    /// </summary>
    public DateTimeOffset ShownAt { get; }

    /// <summary>
    /// Gets or sets whether this notification is still active.
    /// </summary>
    public bool IsActive { get; internal set; } = true;

    /// <summary>
    /// Gets the entity representing this notification in the UI (if any).
    /// </summary>
    public Entity Entity { get; internal set; }

    /// <summary>
    /// Creates a new notification handle.
    /// </summary>
    /// <param name="options">The notification options.</param>
    public NotificationHandle(NotificationOptions options)
    {
        Id = Interlocked.Increment(ref nextId);
        Options = options;
        ShownAt = DateTimeOffset.UtcNow;
    }
}
