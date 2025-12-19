using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for basic UI widgets: Button, Panel, Label, Divider.
/// </summary>
public static partial class WidgetFactory
{
    #region Button

    /// <summary>
    /// Creates a button widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="text">The button label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional button configuration.</param>
    /// <returns>The created button entity.</returns>
    public static Entity CreateButton(
        IWorld world,
        Entity parent,
        string text,
        FontHandle font,
        ButtonConfig? config = null)
    {
        config ??= ButtonConfig.Default;

        var entity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(10f, 5f)
            })
            .With(new UIText
            {
                Content = text,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    /// <summary>
    /// Creates a button widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="text">The button label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional button configuration.</param>
    /// <returns>The created button entity.</returns>
    public static Entity CreateButton(
        IWorld world,
        Entity parent,
        string name,
        string text,
        FontHandle font,
        ButtonConfig? config = null)
    {
        config ??= ButtonConfig.Default;

        var entity = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.Symmetric(10f, 5f)
            })
            .With(new UIText
            {
                Content = text,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    #endregion

    #region Panel

    /// <summary>
    /// Creates a panel widget (container for other elements).
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional panel configuration.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity CreatePanel(
        IWorld world,
        Entity parent,
        PanelConfig? config = null)
    {
        config ??= PanelConfig.Default;

        var builder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreatePanelRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = config.GetPadding()
            })
            .With(new UILayout
            {
                Direction = config.Direction,
                MainAxisAlign = config.MainAxisAlign,
                CrossAxisAlign = config.CrossAxisAlign,
                Spacing = config.Spacing
            });

        var entity = builder.Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    /// <summary>
    /// Creates a panel widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional panel configuration.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity CreatePanel(
        IWorld world,
        Entity parent,
        string name,
        PanelConfig? config = null)
    {
        config ??= PanelConfig.Default;

        var entity = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreatePanelRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.CornerRadius,
                Padding = config.GetPadding()
            })
            .With(new UILayout
            {
                Direction = config.Direction,
                MainAxisAlign = config.MainAxisAlign,
                CrossAxisAlign = config.CrossAxisAlign,
                Spacing = config.Spacing
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    private static UIRect CreatePanelRect(PanelConfig config)
    {
        if (config.Width.HasValue && config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width.Value, config.Height.Value),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            };
        }
        else if (config.Width.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width.Value, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            };
        }
        else if (config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, config.Height.Value),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            };
        }
        else
        {
            return UIRect.Stretch();
        }
    }

    #endregion

    #region Label

    /// <summary>
    /// Creates a label widget (non-interactive text display).
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="text">The label text content.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional label configuration.</param>
    /// <returns>The created label entity.</returns>
    public static Entity CreateLabel(
        IWorld world,
        Entity parent,
        string text,
        FontHandle font,
        LabelConfig? config = null)
    {
        config ??= LabelConfig.Default;

        var entity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateLabelRect(config))
            .With(new UIText
            {
                Content = text,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = config.HorizontalAlign,
                VerticalAlign = config.VerticalAlign
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    /// <summary>
    /// Creates a label widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="text">The label text content.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional label configuration.</param>
    /// <returns>The created label entity.</returns>
    public static Entity CreateLabel(
        IWorld world,
        Entity parent,
        string name,
        string text,
        FontHandle font,
        LabelConfig? config = null)
    {
        config ??= LabelConfig.Default;

        var entity = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateLabelRect(config))
            .With(new UIText
            {
                Content = text,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = config.HorizontalAlign,
                VerticalAlign = config.VerticalAlign
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    private static UIRect CreateLabelRect(LabelConfig config)
    {
        return new UIRect
        {
            AnchorMin = Vector2.Zero,
            AnchorMax = Vector2.One,
            Pivot = new Vector2(0.5f, 0.5f),
            Size = new Vector2(config.Width ?? 200, config.Height ?? 30),
            WidthMode = config.Width.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
            HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.Fill
        };
    }

    #endregion

    #region Divider

    /// <summary>
    /// Creates a divider/separator widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional divider configuration.</param>
    /// <returns>The created divider entity.</returns>
    public static Entity CreateDivider(
        IWorld world,
        Entity parent,
        DividerConfig? config = null)
    {
        config ??= DividerConfig.Horizontal;

        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;
        var width = isHorizontal ? (config.Length ?? 0) : config.Thickness;
        var height = isHorizontal ? config.Thickness : (config.Length ?? 0);
        var widthMode = isHorizontal && !config.Length.HasValue ? UISizeMode.Fill : UISizeMode.Fixed;
        var heightMode = !isHorizontal && !config.Length.HasValue ? UISizeMode.Fill : UISizeMode.Fixed;

        var divider = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(width, height),
                WidthMode = widthMode,
                HeightMode = heightMode
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetColor(),
                Padding = isHorizontal
                    ? new UIEdges(0, config.Margin, 0, config.Margin)
                    : new UIEdges(config.Margin, 0, config.Margin, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(divider, parent);
        }

        return divider;
    }

    /// <summary>
    /// Creates a divider/separator widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional divider configuration.</param>
    /// <returns>The created divider entity.</returns>
    public static Entity CreateDivider(
        IWorld world,
        Entity parent,
        string name,
        DividerConfig? config = null)
    {
        config ??= DividerConfig.Horizontal;

        var isHorizontal = config.Orientation == LayoutDirection.Horizontal;
        var width = isHorizontal ? (config.Length ?? 0) : config.Thickness;
        var height = isHorizontal ? config.Thickness : (config.Length ?? 0);
        var widthMode = isHorizontal && !config.Length.HasValue ? UISizeMode.Fill : UISizeMode.Fixed;
        var heightMode = !isHorizontal && !config.Length.HasValue ? UISizeMode.Fill : UISizeMode.Fixed;

        var divider = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(width, height),
                WidthMode = widthMode,
                HeightMode = heightMode
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetColor(),
                Padding = isHorizontal
                    ? new UIEdges(0, config.Margin, 0, config.Margin)
                    : new UIEdges(config.Margin, 0, config.Margin, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(divider, parent);
        }

        return divider;
    }

    #endregion

    #region Image

    /// <summary>
    /// Creates an image widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="texture">The texture to display.</param>
    /// <param name="config">Optional image configuration.</param>
    /// <returns>The created image entity.</returns>
    public static Entity CreateImage(
        IWorld world,
        Entity parent,
        TextureHandle texture,
        ImageConfig? config = null)
    {
        config ??= ImageConfig.Default;

        var entity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIImage
            {
                Texture = texture,
                Tint = config.GetTint(),
                ScaleMode = config.ScaleMode,
                SourceRect = config.GetSourceRect(),
                PreserveAspect = config.PreserveAspect
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    /// <summary>
    /// Creates an image widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="texture">The texture to display.</param>
    /// <param name="config">Optional image configuration.</param>
    /// <returns>The created image entity.</returns>
    public static Entity CreateImage(
        IWorld world,
        Entity parent,
        string name,
        TextureHandle texture,
        ImageConfig? config = null)
    {
        config ??= ImageConfig.Default;

        var entity = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIImage
            {
                Texture = texture,
                Tint = config.GetTint(),
                ScaleMode = config.ScaleMode,
                SourceRect = config.GetSourceRect(),
                PreserveAspect = config.PreserveAspect
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    #endregion

    #region Card

    /// <summary>
    /// Creates a card widget with a title bar and content area.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="title">The card title text.</param>
    /// <param name="font">The font to use for the title.</param>
    /// <param name="config">Optional card configuration.</param>
    /// <returns>A tuple containing the card entity and content area entity.</returns>
    public static (Entity Card, Entity Content) CreateCard(
        IWorld world,
        Entity parent,
        string title,
        FontHandle font,
        CardConfig? config = null)
    {
        config ??= CardConfig.Default;

        // Create card container
        var card = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Width, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = config.CornerRadius
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(card, parent);
        }

        // Create title bar
        var titleBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, config.TitleHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTitleBarColor(),
                Padding = UIEdges.Symmetric(12, 8)
            })
            .With(new UIText
            {
                Content = title,
                Font = font,
                Color = new Vector4(1f, 1f, 1f, 1f),
                FontSize = 16,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleBar, card);

        // Create content area
        var content = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                Padding = UIEdges.All(12)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(content, card);

        return (card, content);
    }

    #endregion

    #region Badge

    /// <summary>
    /// Creates a badge widget (notification indicator).
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="value">The badge value to display.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional badge configuration.</param>
    /// <returns>The created badge entity.</returns>
    public static Entity CreateBadge(
        IWorld world,
        Entity parent,
        int value,
        FontHandle font,
        BadgeConfig? config = null)
    {
        config ??= BadgeConfig.Default;

        // Format the display text
        var displayText = value > config.MaxValue ? $"{config.MaxValue}+" : value.ToString();

        var entity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Size, config.Size),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor(),
                CornerRadius = config.Size / 2 // Make it circular
            })
            .With(new UIText
            {
                Content = displayText,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    #endregion

    #region Avatar

    /// <summary>
    /// Creates an avatar widget (user profile image with fallback).
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for fallback text.</param>
    /// <param name="config">Optional avatar configuration.</param>
    /// <returns>The created avatar entity.</returns>
    public static Entity CreateAvatar(
        IWorld world,
        Entity parent,
        FontHandle font,
        AvatarConfig? config = null)
    {
        config ??= AvatarConfig.Default;

        var builder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.Size, config.Size),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFallbackBackgroundColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = config.CornerRadius
            });

        // If image is provided, add UIImage component
        // Note: Check against Invalid rather than IsValid since default(TextureHandle) has Id=0 which IsValid considers valid
        if (config.Image != TextureHandle.Invalid && config.Image.Id != 0)
        {
            builder = builder.With(new UIImage
            {
                Texture = config.Image,
                Tint = Vector4.One,
                ScaleMode = ImageScaleMode.ScaleToFill,
                SourceRect = Rectangle.Empty,
                PreserveAspect = true
            });
        }
        // Otherwise, add text fallback
        else if (!string.IsNullOrEmpty(config.FallbackText))
        {
            builder = builder.With(new UIText
            {
                Content = config.FallbackText,
                Font = font,
                Color = config.GetFallbackTextColor(),
                FontSize = config.FallbackFontSize,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            });
        }

        var entity = builder.Build();

        if (parent.IsValid)
        {
            world.SetParent(entity, parent);
        }

        return entity;
    }

    #endregion
}
