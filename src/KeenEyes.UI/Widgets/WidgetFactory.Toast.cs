using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for creating toast notifications and containers.
/// </summary>
public static partial class WidgetFactory
{
    /// <summary>
    /// Creates a toast container for managing toast notifications.
    /// </summary>
    /// <param name="world">The world to create the container in.</param>
    /// <param name="config">The container configuration.</param>
    /// <returns>The container entity.</returns>
    public static Entity CreateToastContainer(IWorld world, ToastContainerConfig? config = null)
    {
        config ??= ToastContainerConfig.Default;

        // Create the container with appropriate anchoring based on position
        var rect = config.Position switch
        {
            ToastPosition.TopLeft => new UIRect
            {
                AnchorMin = new Vector2(0, 0),
                AnchorMax = new Vector2(0, 1),
                Pivot = new Vector2(0, 0),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            },
            ToastPosition.TopCenter => new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0),
                AnchorMax = new Vector2(0.5f, 1),
                Pivot = new Vector2(0.5f, 0),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            },
            ToastPosition.TopRight => new UIRect
            {
                AnchorMin = new Vector2(1, 0),
                AnchorMax = new Vector2(1, 1),
                Pivot = new Vector2(1, 0),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            },
            ToastPosition.BottomLeft => new UIRect
            {
                AnchorMin = new Vector2(0, 0),
                AnchorMax = new Vector2(0, 1),
                Pivot = new Vector2(0, 1),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            },
            ToastPosition.BottomCenter => new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0),
                AnchorMax = new Vector2(0.5f, 1),
                Pivot = new Vector2(0.5f, 1),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            },
            ToastPosition.BottomRight or _ => new UIRect
            {
                AnchorMin = new Vector2(1, 0),
                AnchorMax = new Vector2(1, 1),
                Pivot = new Vector2(1, 1),
                Size = new Vector2(350, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            }
        };

        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(rect)
            .With(new UIStyle
            {
                Padding = new UIEdges(config.Margin, config.Margin, config.Margin, config.Margin)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = IsBottomPosition(config.Position) ? LayoutAlign.End : LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
            })
            .With(new UIToastContainer
            {
                Position = config.Position,
                MaxVisible = config.MaxVisible,
                Spacing = config.Spacing,
                Margin = config.Margin
            })
            .Build();

        return container;
    }

    /// <summary>
    /// Creates a toast notification.
    /// </summary>
    /// <param name="world">The world to create the toast in.</param>
    /// <param name="container">The toast container entity.</param>
    /// <param name="config">The toast configuration.</param>
    /// <returns>The toast entity.</returns>
    public static Entity CreateToast(IWorld world, Entity container, ToastConfig config)
    {
        var padding = config.GetPadding();
        var backgroundColor = config.GetBackgroundColor();
        var textColor = config.GetTextColor();

        // Calculate height based on whether title is present
        float baseHeight = config.Title != null ? 70 : 50;

        // Create main toast entity
        var toast = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = config.CanDismiss })
            .With(new UIRect
            {
                Size = new Vector2(config.Width, baseHeight),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = backgroundColor,
                CornerRadius = config.CornerRadius,
                Padding = padding
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanClick = config.CanDismiss,
                CanFocus = false
            })
            .With(new UIToast(config.Message, config.Duration)
            {
                Type = config.Type,
                Title = config.Title,
                CanDismiss = config.CanDismiss,
                Container = container
            })
            .Build();

        world.SetParent(toast, container);

        // Create content panel for text
        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect
            {
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Center,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 4
            })
            .Build();

        world.SetParent(contentPanel, toast);

        // Create title text (if provided)
        if (config.Title != null)
        {
            var titleText = world.Spawn()
                .With(new UIElement { Visible = true })
                .With(new UIRect
                {
                    WidthMode = UISizeMode.FitContent,
                    HeightMode = UISizeMode.FitContent
                })
                .With(new UIText
                {
                    Content = config.Title,
                    Color = textColor,
                    FontSize = config.TitleFontSize,
                    HorizontalAlign = TextAlignH.Left
                })
                .Build();

            world.SetParent(titleText, contentPanel);
        }

        // Create message text
        var messageText = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect
            {
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIText
            {
                Content = config.Message,
                Color = new Vector4(textColor.X * 0.9f, textColor.Y * 0.9f, textColor.Z * 0.9f, textColor.W),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left
            })
            .Build();

        world.SetParent(messageText, contentPanel);

        // Create close button (if enabled)
        if (config.ShowCloseButton)
        {
            var closeButton = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    Size = new Vector2(24, 24),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0, 0, 0, 0),
                    CornerRadius = 4
                })
                .With(new UIText
                {
                    Content = "\u00D7",  // Unicode multiplication sign (×)
                    Color = textColor,
                    FontSize = 18,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable { CanClick = true })
                .With(new UIToastCloseButton(toast))
                .Build();

            world.SetParent(closeButton, toast);
        }

        return toast;
    }

    /// <summary>
    /// Creates and shows a toast notification using the toast system.
    /// </summary>
    /// <param name="world">The world to create the toast in.</param>
    /// <param name="container">The toast container entity.</param>
    /// <param name="config">The toast configuration.</param>
    /// <returns>The toast entity.</returns>
    public static Entity ShowToast(IWorld world, Entity container, ToastConfig config)
    {
        var toast = CreateToast(world, container, config);

        // Find and use the toast system to show the toast
        // This fires the shown event
        foreach (var entity in world.Query<UIToast>())
        {
            if (entity == toast && world.HasExtension<UIContext>())
            {
                world.Send(new UIToastShownEvent(toast));
                break;
            }
        }

        return toast;
    }

    /// <summary>
    /// Creates an info toast.
    /// </summary>
    public static Entity ShowInfoToast(IWorld world, Entity container, string message, string? title = null, float duration = 3f) =>
        ShowToast(world, container, ToastConfig.Info(message, title, duration));

    /// <summary>
    /// Creates a success toast.
    /// </summary>
    public static Entity ShowSuccessToast(IWorld world, Entity container, string message, string? title = null, float duration = 3f) =>
        ShowToast(world, container, ToastConfig.Success(message, title, duration));

    /// <summary>
    /// Creates a warning toast.
    /// </summary>
    public static Entity ShowWarningToast(IWorld world, Entity container, string message, string? title = null, float duration = 5f) =>
        ShowToast(world, container, ToastConfig.Warning(message, title, duration));

    /// <summary>
    /// Creates an error toast.
    /// </summary>
    public static Entity ShowErrorToast(IWorld world, Entity container, string message, string? title = null, float duration = 0f) =>
        ShowToast(world, container, ToastConfig.Error(message, title, duration));

    private static bool IsBottomPosition(ToastPosition position) =>
        position is ToastPosition.BottomLeft or ToastPosition.BottomCenter or ToastPosition.BottomRight;
}
