using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for modal dialog widgets: Modal, Alert, Confirm, Prompt.
/// </summary>
public static partial class WidgetFactory
{
    #region Modal

    /// <summary>
    /// Creates a modal dialog widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to (typically a root canvas).</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="config">Optional modal configuration.</param>
    /// <param name="buttons">Optional action buttons to create.</param>
    /// <returns>A tuple containing the modal entity, backdrop entity, and content panel entity.</returns>
    /// <remarks>
    /// <para>
    /// The modal is composed of:
    /// - A backdrop overlay that covers the entire screen
    /// - A centered dialog container with title bar
    /// - A content panel for custom content
    /// - Optional action buttons in a footer
    /// </para>
    /// <para>
    /// The modal starts hidden. Use <see cref="UIModalSystem.OpenModal"/> to show it.
    /// Add children to the returned content panel entity.
    /// </para>
    /// </remarks>
    public static (Entity Modal, Entity Backdrop, Entity ContentPanel) CreateModal(
        IWorld world,
        Entity parent,
        FontHandle font,
        ModalConfig? config = null,
        IEnumerable<ModalButtonDef>? buttons = null)
    {
        config ??= ModalConfig.Default;

        // Create backdrop (full screen overlay)
        var backdrop = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(UIRect.Stretch())
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackdropColor()
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(backdrop, parent);
        }

        // Create modal container (centered)
        var modal = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height ?? 200),
                WidthMode = UISizeMode.Fixed,
                HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.FitContent,
                LocalZIndex = 1000
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor(),
                CornerRadius = config.CornerRadius
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIModal(config.Title, config.CloseOnBackdropClick, config.CloseOnEscape)
            {
                Backdrop = backdrop
            })
            .Build();

        world.Add(modal, new UIHiddenTag());

        if (parent.IsValid)
        {
            world.SetParent(modal, parent);
        }

        // Update backdrop with modal reference
        world.Add(backdrop, new UIModalBackdrop(modal));
        world.Add(backdrop, new UIHiddenTag());

        // Create title bar
        var titleBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTitleBarColor(),
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(12f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .Build();

        world.SetParent(titleBar, modal);

        // Create title label
        var titleLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = config.Title,
                Font = font,
                Color = config.GetTitleTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleLabel, titleBar);

        // Create close button if enabled
        if (config.ShowCloseButton)
        {
            var closeButton = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(config.TitleBarHeight - 12, config.TitleBarHeight - 12),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.5f, 0.2f, 0.2f, 0f),
                    CornerRadius = 3f
                })
                .With(new UIText
                {
                    Content = "X",
                    Font = font,
                    Color = config.GetTitleTextColor(),
                    FontSize = config.FontSize * 0.8f,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .With(new UIModalCloseButton(modal))
                .Build();

            world.SetParent(closeButton, titleBar);
        }

        // Create content panel
        var contentPadding = config.GetContentPadding();
        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                Padding = contentPadding
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, modal);

        // Update modal with content container reference
        ref var modalComponent = ref world.Get<UIModal>(modal);
        modalComponent.ContentContainer = contentPanel;

        // Create button footer if buttons are provided
        var buttonList = buttons?.ToList();
        if (buttonList != null && buttonList.Count > 0)
        {
            var buttonFooter = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, 48),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    Padding = new UIEdges(16, 8, 16, 8)
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.End,
                    CrossAxisAlign = LayoutAlign.Center,
                    Spacing = config.ButtonSpacing
                })
                .Build();

            world.SetParent(buttonFooter, modal);

            // Create action buttons
            foreach (var buttonDef in buttonList)
            {
                var buttonWidth = buttonDef.Width ?? 80;
                var buttonColor = buttonDef.IsPrimary
                    ? new Vector4(0.3f, 0.5f, 0.8f, 1f)
                    : new Vector4(0.25f, 0.25f, 0.3f, 1f);

                var actionButton = world.Spawn()
                    .With(new UIElement { Visible = true, RaycastTarget = true })
                    .With(new UIRect
                    {
                        AnchorMin = Vector2.Zero,
                        AnchorMax = Vector2.One,
                        Pivot = new Vector2(0.5f, 0.5f),
                        Size = new Vector2(buttonWidth, 32),
                        WidthMode = UISizeMode.Fixed,
                        HeightMode = UISizeMode.Fixed
                    })
                    .With(new UIStyle
                    {
                        BackgroundColor = buttonColor,
                        CornerRadius = 4f
                    })
                    .With(new UIText
                    {
                        Content = buttonDef.Text,
                        Font = font,
                        Color = new Vector4(1f, 1f, 1f, 1f),
                        FontSize = 14,
                        HorizontalAlign = TextAlignH.Center,
                        VerticalAlign = TextAlignV.Middle
                    })
                    .With(new UIInteractable
                    {
                        CanClick = true
                    })
                    .With(new UIModalButton(modal, buttonDef.Result))
                    .Build();

                world.SetParent(actionButton, buttonFooter);
            }
        }

        return (modal, backdrop, contentPanel);
    }

    /// <summary>
    /// Creates a modal dialog widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to (typically a root canvas).</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="config">Optional modal configuration.</param>
    /// <param name="buttons">Optional action buttons to create.</param>
    /// <returns>A tuple containing the modal entity, backdrop entity, and content panel entity.</returns>
    public static (Entity Modal, Entity Backdrop, Entity ContentPanel) CreateModal(
        IWorld world,
        Entity parent,
        string name,
        FontHandle font,
        ModalConfig? config = null,
        IEnumerable<ModalButtonDef>? buttons = null)
    {
        config ??= ModalConfig.Default;

        var backdrop = world.Spawn($"{name}_Backdrop")
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(UIRect.Stretch())
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackdropColor()
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(backdrop, parent);
        }

        var modal = world.Spawn(name)
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0.5f, 0.5f),
                AnchorMax = new Vector2(0.5f, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height ?? 200),
                WidthMode = UISizeMode.Fixed,
                HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.FitContent,
                LocalZIndex = 1000
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor(),
                CornerRadius = config.CornerRadius
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UIModal(config.Title, config.CloseOnBackdropClick, config.CloseOnEscape)
            {
                Backdrop = backdrop
            })
            .Build();

        world.Add(modal, new UIHiddenTag());

        if (parent.IsValid)
        {
            world.SetParent(modal, parent);
        }

        world.Add(backdrop, new UIModalBackdrop(modal));
        world.Add(backdrop, new UIHiddenTag());

        var titleBar = world.Spawn($"{name}_TitleBar")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTitleBarColor(),
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(12f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .Build();

        world.SetParent(titleBar, modal);

        var titleLabel = world.Spawn($"{name}_Title")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, config.TitleBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = config.Title,
                Font = font,
                Color = config.GetTitleTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleLabel, titleBar);

        if (config.ShowCloseButton)
        {
            var closeButton = world.Spawn($"{name}_CloseButton")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(config.TitleBarHeight - 12, config.TitleBarHeight - 12),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.5f, 0.2f, 0.2f, 0f),
                    CornerRadius = 3f
                })
                .With(new UIText
                {
                    Content = "X",
                    Font = font,
                    Color = config.GetTitleTextColor(),
                    FontSize = config.FontSize * 0.8f,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .With(new UIModalCloseButton(modal))
                .Build();

            world.SetParent(closeButton, titleBar);
        }

        var contentPadding = config.GetContentPadding();
        var contentPanel = world.Spawn($"{name}_Content")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                Padding = contentPadding
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, modal);

        ref var modalComponent = ref world.Get<UIModal>(modal);
        modalComponent.ContentContainer = contentPanel;

        var buttonList = buttons?.ToList();
        if (buttonList != null && buttonList.Count > 0)
        {
            var buttonFooter = world.Spawn($"{name}_Footer")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, 48),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    Padding = new UIEdges(16, 8, 16, 8)
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.End,
                    CrossAxisAlign = LayoutAlign.Center,
                    Spacing = config.ButtonSpacing
                })
                .Build();

            world.SetParent(buttonFooter, modal);

            var buttonIndex = 0;
            foreach (var buttonDef in buttonList)
            {
                var buttonWidth = buttonDef.Width ?? 80;
                var buttonColor = buttonDef.IsPrimary
                    ? new Vector4(0.3f, 0.5f, 0.8f, 1f)
                    : new Vector4(0.25f, 0.25f, 0.3f, 1f);

                var actionButton = world.Spawn($"{name}_Button{buttonIndex}")
                    .With(new UIElement { Visible = true, RaycastTarget = true })
                    .With(new UIRect
                    {
                        AnchorMin = Vector2.Zero,
                        AnchorMax = Vector2.One,
                        Pivot = new Vector2(0.5f, 0.5f),
                        Size = new Vector2(buttonWidth, 32),
                        WidthMode = UISizeMode.Fixed,
                        HeightMode = UISizeMode.Fixed
                    })
                    .With(new UIStyle
                    {
                        BackgroundColor = buttonColor,
                        CornerRadius = 4f
                    })
                    .With(new UIText
                    {
                        Content = buttonDef.Text,
                        Font = font,
                        Color = new Vector4(1f, 1f, 1f, 1f),
                        FontSize = 14,
                        HorizontalAlign = TextAlignH.Center,
                        VerticalAlign = TextAlignV.Middle
                    })
                    .With(new UIInteractable
                    {
                        CanClick = true
                    })
                    .With(new UIModalButton(modal, buttonDef.Result))
                    .Build();

                world.SetParent(actionButton, buttonFooter);
                buttonIndex++;
            }
        }

        return (modal, backdrop, contentPanel);
    }

    #endregion

    #region Alert

    /// <summary>
    /// Creates an alert dialog with a message and OK button.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="message">The alert message text.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="config">Optional alert configuration.</param>
    /// <returns>A tuple containing the modal entity, backdrop entity, and content panel entity.</returns>
    public static (Entity Modal, Entity Backdrop, Entity ContentPanel) CreateAlert(
        IWorld world,
        Entity parent,
        string message,
        FontHandle font,
        AlertConfig? config = null)
    {
        config ??= new AlertConfig();

        var modalConfig = new ModalConfig(
            Width: config.Width,
            Title: config.Title,
            CloseOnBackdropClick: config.CloseOnBackdropClick,
            CloseOnEscape: config.CloseOnEscape
        );

        var buttons = new[]
        {
            new ModalButtonDef(config.OkButtonText, ModalResult.OK, IsPrimary: true)
        };

        var result = CreateModal(world, parent, font, modalConfig, buttons);

        // Add message label to content panel
        var messageLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIText
            {
                Content = message,
                Font = font,
                Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
                FontSize = 14,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Top
            })
            .Build();

        world.SetParent(messageLabel, result.ContentPanel);

        return result;
    }

    #endregion

    #region Confirm

    /// <summary>
    /// Creates a confirm dialog with a message and OK/Cancel buttons.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="message">The confirm message text.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="config">Optional confirm configuration.</param>
    /// <returns>A tuple containing the modal entity, backdrop entity, and content panel entity.</returns>
    public static (Entity Modal, Entity Backdrop, Entity ContentPanel) CreateConfirm(
        IWorld world,
        Entity parent,
        string message,
        FontHandle font,
        ConfirmConfig? config = null)
    {
        config ??= new ConfirmConfig();

        var modalConfig = new ModalConfig(
            Width: config.Width,
            Title: config.Title,
            CloseOnBackdropClick: config.CloseOnBackdropClick,
            CloseOnEscape: config.CloseOnEscape
        );

        var buttons = new[]
        {
            new ModalButtonDef(config.CancelButtonText, ModalResult.Cancel, IsPrimary: false),
            new ModalButtonDef(config.OkButtonText, ModalResult.OK, IsPrimary: true)
        };

        var result = CreateModal(world, parent, font, modalConfig, buttons);

        // Add message label to content panel
        var messageLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIText
            {
                Content = message,
                Font = font,
                Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
                FontSize = 14,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Top
            })
            .Build();

        world.SetParent(messageLabel, result.ContentPanel);

        return result;
    }

    #endregion

    #region Prompt

    /// <summary>
    /// Creates a prompt dialog with a text input and OK/Cancel buttons.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="message">The prompt message text.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="config">Optional prompt configuration.</param>
    /// <returns>A tuple containing the modal entity, backdrop entity, content panel entity, and text input entity.</returns>
    public static (Entity Modal, Entity Backdrop, Entity ContentPanel, Entity TextInput) CreatePrompt(
        IWorld world,
        Entity parent,
        string message,
        FontHandle font,
        PromptConfig? config = null)
    {
        config ??= new PromptConfig();

        var modalConfig = new ModalConfig(
            Width: config.Width,
            Title: config.Title,
            CloseOnBackdropClick: config.CloseOnBackdropClick,
            CloseOnEscape: config.CloseOnEscape
        );

        var buttons = new[]
        {
            new ModalButtonDef(config.CancelButtonText, ModalResult.Cancel, IsPrimary: false),
            new ModalButtonDef(config.OkButtonText, ModalResult.OK, IsPrimary: true)
        };

        var result = CreateModal(world, parent, font, modalConfig, buttons);

        // Add message label to content panel
        var messageLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIText
            {
                Content = message,
                Font = font,
                Color = new Vector4(0.9f, 0.9f, 0.9f, 1f),
                FontSize = 14,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Top
            })
            .Build();

        world.SetParent(messageLabel, result.ContentPanel);

        // Add text input
        var textInput = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, 32),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.1f, 0.1f, 0.15f, 1f),
                CornerRadius = 4f,
                Padding = UIEdges.Symmetric(8f, 4f)
            })
            .With(new UIText
            {
                Content = config.InitialValue,
                Font = font,
                Color = new Vector4(1f, 1f, 1f, 1f),
                FontSize = 14,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true
            })
            .With(new UITextInput
            {
                PlaceholderText = config.Placeholder,
                CursorPosition = config.InitialValue.Length,
                ShowingPlaceholder = string.IsNullOrEmpty(config.InitialValue)
            })
            .Build();

        world.SetParent(textInput, result.ContentPanel);

        return (result.Modal, result.Backdrop, result.ContentPanel, textInput);
    }

    #endregion
}
