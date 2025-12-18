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
}
