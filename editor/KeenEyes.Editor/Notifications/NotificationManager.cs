using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Notifications;

/// <summary>
/// Manages toast notifications in the editor using the UI toast system.
/// </summary>
/// <remarks>
/// <para>
/// This manager bridges the <see cref="INotificationCapability"/> interface to the
/// underlying UI toast infrastructure provided by <see cref="WidgetFactory"/>.
/// </para>
/// <para>
/// Notifications are displayed as toast popups in the specified container, typically
/// positioned in a corner of the editor window. They automatically dismiss after
/// a configurable duration or can be dismissed manually by the user.
/// </para>
/// </remarks>
public sealed class NotificationManager : INotificationCapability
{
    private readonly IWorld world;
    private readonly Entity container;
    private readonly Dictionary<int, NotificationHandle> activeNotifications = [];
    private readonly Lock syncLock = new();

    /// <summary>
    /// Creates a new notification manager.
    /// </summary>
    /// <param name="world">The editor UI world.</param>
    /// <param name="container">The toast container entity created by <see cref="WidgetFactory.CreateToastContainer"/>.</param>
    public NotificationManager(IWorld world, Entity container)
    {
        ArgumentNullException.ThrowIfNull(world);
        this.world = world;
        this.container = container;
    }

    /// <inheritdoc />
    public int ActiveCount
    {
        get
        {
            lock (syncLock)
            {
                return activeNotifications.Count;
            }
        }
    }

    /// <inheritdoc />
    public event Action<NotificationHandle>? NotificationShown;

    /// <inheritdoc />
    public event Action<NotificationHandle>? NotificationDismissed;

    /// <inheritdoc />
    public NotificationHandle Show(NotificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var handle = new NotificationHandle(options);

        var toastConfig = new ToastConfig(
            Message: options.Message,
            Title: options.Title,
            Type: MapSeverityToToastType(options.Severity),
            Duration: (float)options.Duration.TotalSeconds,
            CanDismiss: options.Dismissible,
            ShowCloseButton: options.Dismissible);

        var toastEntity = WidgetFactory.ShowToast(world, container, toastConfig);
        handle.Entity = toastEntity;

        lock (syncLock)
        {
            activeNotifications[handle.Id] = handle;
        }

        // Subscribe to toast events to track dismissal
        SubscribeToToastEvents(handle, toastEntity);

        NotificationShown?.Invoke(handle);

        return handle;
    }

    /// <inheritdoc />
    public NotificationHandle ShowSuccess(string message, string? title = null)
    {
        return Show(new NotificationOptions
        {
            Message = message,
            Title = title,
            Severity = NotificationSeverity.Success,
            Duration = TimeSpan.FromSeconds(3)
        });
    }

    /// <inheritdoc />
    public NotificationHandle ShowError(string message, string? title = null)
    {
        return Show(new NotificationOptions
        {
            Message = message,
            Title = title,
            Severity = NotificationSeverity.Error,
            Duration = TimeSpan.Zero // Errors stay until dismissed
        });
    }

    /// <inheritdoc />
    public NotificationHandle ShowWarning(string message, string? title = null)
    {
        return Show(new NotificationOptions
        {
            Message = message,
            Title = title,
            Severity = NotificationSeverity.Warning,
            Duration = TimeSpan.FromSeconds(5)
        });
    }

    /// <inheritdoc />
    public NotificationHandle ShowInfo(string message, string? title = null)
    {
        return Show(new NotificationOptions
        {
            Message = message,
            Title = title,
            Severity = NotificationSeverity.Info,
            Duration = TimeSpan.FromSeconds(4)
        });
    }

    /// <inheritdoc />
    public void Dismiss(NotificationHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);

        if (!handle.IsActive)
        {
            return;
        }

        // Despawn the toast entity
        if (handle.Entity.IsValid && world.IsAlive(handle.Entity))
        {
            DespawnRecursive(handle.Entity);
        }

        MarkDismissed(handle);
    }

    /// <inheritdoc />
    public void DismissAll()
    {
        List<NotificationHandle> toRemove;

        lock (syncLock)
        {
            toRemove = activeNotifications.Values.ToList();
        }

        foreach (var handle in toRemove)
        {
            Dismiss(handle);
        }
    }

    private void SubscribeToToastEvents(NotificationHandle handle, Entity toastEntity)
    {
        // The UIToast component tracks its own dismissal via timer
        // We need to detect when the toast entity is despawned
        // For now, we rely on the caller to properly dismiss through us
        // or we can periodically check if the entity is still alive

        // If the toast has a duration, it will auto-dismiss
        if (handle.Options.Duration > TimeSpan.Zero)
        {
            // Subscribe to the toast dismissed event
            world.Subscribe<UIToastDismissedEvent>(e =>
            {
                if (e.Toast == toastEntity)
                {
                    MarkDismissed(handle);
                }
            });
        }
    }

    private void MarkDismissed(NotificationHandle handle)
    {
        if (!handle.IsActive)
        {
            return;
        }

        handle.IsActive = false;

        lock (syncLock)
        {
            activeNotifications.Remove(handle.Id);
        }

        NotificationDismissed?.Invoke(handle);
    }

    private void DespawnRecursive(Entity entity)
    {
        var children = world.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(child);
        }

        if (world.IsAlive(entity))
        {
            world.Despawn(entity);
        }
    }

    private static ToastType MapSeverityToToastType(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Info => ToastType.Info,
            NotificationSeverity.Success => ToastType.Success,
            NotificationSeverity.Warning => ToastType.Warning,
            NotificationSeverity.Error => ToastType.Error,
            _ => ToastType.Info
        };
    }
}
