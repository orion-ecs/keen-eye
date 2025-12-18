using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for creating common UI widgets as ECS entities.
/// </summary>
/// <remarks>
/// <para>
/// Widgets are pure ECS entity builders, not wrapper classes. Each factory method
/// creates an entity with the appropriate components for that widget type.
/// </para>
/// <para>
/// After creation, you can further customize the widget by modifying its components
/// directly using the world's Get methods.
/// </para>
/// </remarks>
public static class WidgetFactory
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

    #region TextField

    /// <summary>
    /// Creates a text field widget (text input).
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional text field configuration.</param>
    /// <returns>The created text field entity.</returns>
    /// <remarks>
    /// The text field entity includes a <see cref="UIText"/> component for input text.
    /// Use <c>world.Get&lt;UIText&gt;(entity).Content</c> to get/set the current value.
    /// </remarks>
    public static Entity CreateTextField(
        IWorld world,
        Entity parent,
        FontHandle font,
        TextFieldConfig? config = null)
    {
        config ??= TextFieldConfig.Default;

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
                CornerRadius = 4f,
                Padding = UIEdges.Symmetric(8f, 4f)
            })
            .With(new UIText
            {
                Content = config.PlaceholderText,
                Font = font,
                Color = config.GetPlaceholderColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
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
    /// Creates a text field widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional text field configuration.</param>
    /// <returns>The created text field entity.</returns>
    public static Entity CreateTextField(
        IWorld world,
        Entity parent,
        string name,
        FontHandle font,
        TextFieldConfig? config = null)
    {
        config ??= TextFieldConfig.Default;

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
                CornerRadius = 4f,
                Padding = UIEdges.Symmetric(8f, 4f)
            })
            .With(new UIText
            {
                Content = config.PlaceholderText,
                Font = font,
                Color = config.GetPlaceholderColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
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

    #region Checkbox

    /// <summary>
    /// Creates a checkbox widget with a label.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="label">The checkbox label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional checkbox configuration.</param>
    /// <returns>The created checkbox container entity.</returns>
    /// <remarks>
    /// <para>
    /// The checkbox is composed of a container entity with horizontal layout,
    /// containing a box entity and a label entity.
    /// </para>
    /// <para>
    /// To check/uncheck programmatically, modify the <see cref="UIInteractable.State"/>
    /// on the container entity using the <see cref="UIInteractionState.Pressed"/> flag
    /// as a toggle indicator.
    /// </para>
    /// </remarks>
    public static Entity CreateCheckbox(
        IWorld world,
        Entity parent,
        string label,
        FontHandle font,
        CheckboxConfig? config = null)
    {
        config ??= CheckboxConfig.Default;

        // Create container with horizontal layout
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(200, config.Size),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
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
            world.SetParent(container, parent);
        }

        // Create the checkbox box
        var box = world.Spawn()
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
                BackgroundColor = config.IsChecked ? config.GetCheckColor() : config.GetBackgroundColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = 3f
            })
            .Build();

        world.SetParent(box, container);

        // Create the label
        var labelEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(150, config.Size),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = label,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, container);

        return container;
    }

    /// <summary>
    /// Creates a checkbox widget with a name and label.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="label">The checkbox label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional checkbox configuration.</param>
    /// <returns>The created checkbox container entity.</returns>
    public static Entity CreateCheckbox(
        IWorld world,
        Entity parent,
        string name,
        string label,
        FontHandle font,
        CheckboxConfig? config = null)
    {
        config ??= CheckboxConfig.Default;

        // Create container with horizontal layout
        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(200, config.Size),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
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
            world.SetParent(container, parent);
        }

        // Create the checkbox box
        var box = world.Spawn($"{name}_Box")
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
                BackgroundColor = config.IsChecked ? config.GetCheckColor() : config.GetBackgroundColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = 3f
            })
            .Build();

        world.SetParent(box, container);

        // Create the label
        var labelEntity = world.Spawn($"{name}_Label")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(150, config.Size),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = label,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, container);

        return container;
    }

    #endregion

    #region Slider

    /// <summary>
    /// Creates a slider widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional slider configuration.</param>
    /// <returns>The created slider container entity.</returns>
    /// <remarks>
    /// <para>
    /// The slider is composed of a container with track, fill, and thumb entities.
    /// </para>
    /// <para>
    /// The slider value is stored in the container's <see cref="UIScrollable.ScrollPosition"/>.X
    /// as a normalized value (0-1). To get the actual value, multiply by (MaxValue - MinValue) and add MinValue.
    /// </para>
    /// </remarks>
    public static Entity CreateSlider(
        IWorld world,
        Entity parent,
        SliderConfig? config = null)
    {
        config ??= SliderConfig.Default;

        // Calculate initial normalized value
        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        // Create container
        var container = world.Spawn()
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
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .With(new UIScrollable
            {
                ScrollPosition = new Vector2(normalizedValue, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create track
        var trackHeight = 4f;
        var track = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(1, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, trackHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = trackHeight / 2
            })
            .Build();

        world.SetParent(track, container);

        // Create fill
        var fill = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(normalizedValue, 0.5f),
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, trackHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = trackHeight / 2
            })
            .Build();

        world.SetParent(fill, container);

        // Create thumb
        var thumb = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(normalizedValue, 0.5f),
                AnchorMax = new Vector2(normalizedValue, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.ThumbSize, config.ThumbSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetThumbColor(),
                CornerRadius = config.ThumbSize / 2
            })
            .Build();

        world.SetParent(thumb, container);

        return container;
    }

    /// <summary>
    /// Creates a slider widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional slider configuration.</param>
    /// <returns>The created slider container entity.</returns>
    public static Entity CreateSlider(
        IWorld world,
        Entity parent,
        string name,
        SliderConfig? config = null)
    {
        config ??= SliderConfig.Default;

        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        var container = world.Spawn(name)
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
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .With(new UIScrollable
            {
                ScrollPosition = new Vector2(normalizedValue, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var trackHeight = 4f;
        var track = world.Spawn($"{name}_Track")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(1, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, trackHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = trackHeight / 2
            })
            .Build();

        world.SetParent(track, container);

        var fill = world.Spawn($"{name}_Fill")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(normalizedValue, 0.5f),
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(0, trackHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = trackHeight / 2
            })
            .Build();

        world.SetParent(fill, container);

        var thumb = world.Spawn($"{name}_Thumb")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = new Vector2(normalizedValue, 0.5f),
                AnchorMax = new Vector2(normalizedValue, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(config.ThumbSize, config.ThumbSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetThumbColor(),
                CornerRadius = config.ThumbSize / 2
            })
            .Build();

        world.SetParent(thumb, container);

        return container;
    }

    #endregion

    #region ProgressBar

    /// <summary>
    /// Creates a progress bar widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for the label (if shown).</param>
    /// <param name="config">Optional progress bar configuration.</param>
    /// <returns>The created progress bar container entity.</returns>
    /// <remarks>
    /// <para>
    /// The progress bar is composed of a track and a fill entity.
    /// </para>
    /// <para>
    /// To update the progress, modify the fill entity's <see cref="UIRect.AnchorMax"/>.X value
    /// with the normalized progress (0-1).
    /// </para>
    /// </remarks>
    public static Entity CreateProgressBar(
        IWorld world,
        Entity parent,
        FontHandle font,
        ProgressBarConfig? config = null)
    {
        config ??= ProgressBarConfig.Default;

        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        // Create container (track)
        var container = world.Spawn()
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
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create fill
        var fill = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(normalizedValue, 1),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        world.SetParent(fill, container);

        // Create label if requested
        if (config.ShowLabel)
        {
            var percentage = (int)(normalizedValue * 100);
            var labelEntity = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UIText
                {
                    Content = $"{percentage}%",
                    Font = font,
                    Color = config.GetLabelColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(labelEntity, container);
        }

        return container;
    }

    /// <summary>
    /// Creates a progress bar widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="font">The font to use for the label (if shown).</param>
    /// <param name="config">Optional progress bar configuration.</param>
    /// <returns>The created progress bar container entity.</returns>
    public static Entity CreateProgressBar(
        IWorld world,
        Entity parent,
        string name,
        FontHandle font,
        ProgressBarConfig? config = null)
    {
        config ??= ProgressBarConfig.Default;

        var normalizedValue = (config.Value - config.MinValue) / (config.MaxValue - config.MinValue);
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        var container = world.Spawn(name)
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
            .With(new UIStyle
            {
                BackgroundColor = config.GetTrackColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var fill = world.Spawn($"{name}_Fill")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(normalizedValue, 1),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetFillColor(),
                CornerRadius = config.CornerRadius
            })
            .Build();

        world.SetParent(fill, container);

        if (config.ShowLabel)
        {
            var percentage = (int)(normalizedValue * 100);
            var labelEntity = world.Spawn($"{name}_Label")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UIText
                {
                    Content = $"{percentage}%",
                    Font = font,
                    Color = config.GetLabelColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(labelEntity, container);
        }

        return container;
    }

    #endregion

    #region Toggle

    /// <summary>
    /// Creates a toggle/switch widget with a label.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="label">The toggle label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional toggle configuration.</param>
    /// <returns>The created toggle container entity.</returns>
    /// <remarks>
    /// <para>
    /// The toggle is composed of a container with a track, thumb, and label.
    /// </para>
    /// <para>
    /// The toggle state is stored via <see cref="UIInteractionState.Pressed"/> flag on the container.
    /// </para>
    /// </remarks>
    public static Entity CreateToggle(
        IWorld world,
        Entity parent,
        string label,
        FontHandle font,
        ToggleConfig? config = null)
    {
        config ??= ToggleConfig.Default;

        // Create container with horizontal layout
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(200, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex,
                State = config.IsOn ? UIInteractionState.Pressed : UIInteractionState.Normal
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create track
        var trackColor = config.IsOn ? config.GetTrackOnColor() : config.GetTrackOffColor();
        var track = world.Spawn()
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
            .With(new UIStyle
            {
                BackgroundColor = trackColor,
                CornerRadius = config.Height / 2
            })
            .Build();

        world.SetParent(track, container);

        // Create thumb
        var thumbPadding = 2f;
        var thumbSize = config.Height - thumbPadding * 2;
        var thumbOffset = config.IsOn ? config.Width - thumbSize - thumbPadding : thumbPadding;

        var thumb = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0, 0.5f),
                Offset = new UIEdges(thumbOffset, 0, 0, 0),
                Size = new Vector2(thumbSize, thumbSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetThumbColor(),
                CornerRadius = thumbSize / 2
            })
            .Build();

        world.SetParent(thumb, track);

        // Create label
        var labelEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(150, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = label,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, container);

        return container;
    }

    /// <summary>
    /// Creates a toggle/switch widget with a name and label.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="label">The toggle label text.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional toggle configuration.</param>
    /// <returns>The created toggle container entity.</returns>
    public static Entity CreateToggle(
        IWorld world,
        Entity parent,
        string name,
        string label,
        FontHandle font,
        ToggleConfig? config = null)
    {
        config ??= ToggleConfig.Default;

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(200, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = config.Spacing
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex,
                State = config.IsOn ? UIInteractionState.Pressed : UIInteractionState.Normal
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var trackColor = config.IsOn ? config.GetTrackOnColor() : config.GetTrackOffColor();
        var track = world.Spawn($"{name}_Track")
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
            .With(new UIStyle
            {
                BackgroundColor = trackColor,
                CornerRadius = config.Height / 2
            })
            .Build();

        world.SetParent(track, container);

        var thumbPadding = 2f;
        var thumbSize = config.Height - thumbPadding * 2;
        var thumbOffset = config.IsOn ? config.Width - thumbSize - thumbPadding : thumbPadding;

        var thumb = world.Spawn($"{name}_Thumb")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0, 0.5f),
                Offset = new UIEdges(thumbOffset, 0, 0, 0),
                Size = new Vector2(thumbSize, thumbSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetThumbColor(),
                CornerRadius = thumbSize / 2
            })
            .Build();

        world.SetParent(thumb, track);

        var labelEntity = world.Spawn($"{name}_Label")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0f, 0.5f),
                Size = new Vector2(150, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = label,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, container);

        return container;
    }

    #endregion

    #region Dropdown

    /// <summary>
    /// Creates a dropdown widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="items">The dropdown items to display.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional dropdown configuration.</param>
    /// <returns>The created dropdown header entity.</returns>
    /// <remarks>
    /// <para>
    /// The dropdown is composed of a header entity and a list entity that is shown/hidden on click.
    /// </para>
    /// <para>
    /// The selected index is stored in the header's UIScrollable.ScrollPosition.X as an integer.
    /// </para>
    /// </remarks>
    public static Entity CreateDropdown(
        IWorld world,
        Entity parent,
        string[] items,
        FontHandle font,
        DropdownConfig? config = null)
    {
        config ??= DropdownConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, items.Length - 1));
        var selectedText = items.Length > 0 ? items[selectedIndex] : "";

        // Create header
        var header = world.Spawn()
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
                CornerRadius = 4f,
                Padding = UIEdges.Symmetric(8f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .With(new UIScrollable
            {
                ScrollPosition = new Vector2(selectedIndex, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(header, parent);
        }

        // Create selected text label
        var selectedLabel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(config.Width - 30, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = selectedText,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(selectedLabel, header);

        // Create arrow indicator
        var arrow = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(20, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = "▼",
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize * 0.7f,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(arrow, header);

        // Create dropdown list (initially hidden)
        var dropdownHeight = Math.Min(items.Length * config.Height, config.MaxDropdownHeight);
        var dropdownList = world.Spawn()
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 1),
                AnchorMax = new Vector2(1, 1),
                Pivot = new Vector2(0, 0),
                Size = new Vector2(0, dropdownHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetDropdownColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = 4f
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .Build();

        world.SetParent(dropdownList, header);

        // Create list items
        for (var i = 0; i < items.Length; i++)
        {
            var isSelected = i == selectedIndex;
            var item = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.Height),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isSelected ? config.GetSelectedColor() : Vector4.Zero,
                    Padding = UIEdges.Symmetric(8f, 0)
                })
                .With(new UIText
                {
                    Content = items[i],
                    Font = font,
                    Color = config.GetTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Left,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .Build();

            world.SetParent(item, dropdownList);
        }

        return header;
    }

    /// <summary>
    /// Creates a dropdown widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="items">The dropdown items to display.</param>
    /// <param name="font">The font to use for the text.</param>
    /// <param name="config">Optional dropdown configuration.</param>
    /// <returns>The created dropdown header entity.</returns>
    public static Entity CreateDropdown(
        IWorld world,
        Entity parent,
        string name,
        string[] items,
        FontHandle font,
        DropdownConfig? config = null)
    {
        config ??= DropdownConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, items.Length - 1));
        var selectedText = items.Length > 0 ? items[selectedIndex] : "";

        var header = world.Spawn(name)
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
                CornerRadius = 4f,
                Padding = UIEdges.Symmetric(8f, 0)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanFocus = true,
                TabIndex = config.TabIndex
            })
            .With(new UIScrollable
            {
                ScrollPosition = new Vector2(selectedIndex, 0)
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(header, parent);
        }

        var selectedLabel = world.Spawn($"{name}_SelectedLabel")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0, 0.5f),
                Size = new Vector2(config.Width - 30, config.Height),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = selectedText,
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize,
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(selectedLabel, header);

        var arrow = world.Spawn($"{name}_Arrow")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(20, config.Height),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = "▼",
                Font = font,
                Color = config.GetTextColor(),
                FontSize = config.FontSize * 0.7f,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(arrow, header);

        var dropdownHeight = Math.Min(items.Length * config.Height, config.MaxDropdownHeight);
        var dropdownList = world.Spawn($"{name}_List")
            .With(new UIElement { Visible = false, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(0, 1),
                AnchorMax = new Vector2(1, 1),
                Pivot = new Vector2(0, 0),
                Size = new Vector2(0, dropdownHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetDropdownColor(),
                BorderColor = config.GetBorderColor(),
                BorderWidth = config.BorderWidth,
                CornerRadius = 4f
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .Build();

        world.SetParent(dropdownList, header);

        for (var i = 0; i < items.Length; i++)
        {
            var isSelected = i == selectedIndex;
            var item = world.Spawn($"{name}_Item_{i}")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.Height),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isSelected ? config.GetSelectedColor() : Vector4.Zero,
                    Padding = UIEdges.Symmetric(8f, 0)
                })
                .With(new UIText
                {
                    Content = items[i],
                    Font = font,
                    Color = config.GetTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Left,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true
                })
                .Build();

            world.SetParent(item, dropdownList);
        }

        return header;
    }

    #endregion

    #region TabView

    /// <summary>
    /// Creates a tab view widget with a tab bar and content area.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="tabs">The tab configurations.</param>
    /// <param name="font">The font to use for tab labels.</param>
    /// <param name="config">Optional tab view configuration.</param>
    /// <returns>A tuple containing the tab view container and an array of content panel entities.</returns>
    /// <remarks>
    /// <para>
    /// The tab view is composed of:
    /// - A container with vertical layout
    /// - A tab bar with horizontal layout containing tab buttons
    /// - Content panels for each tab (only the selected one is visible)
    /// </para>
    /// <para>
    /// The selected tab index is stored in the container's UIScrollable.ScrollPosition.X as an integer.
    /// </para>
    /// </remarks>
    public static (Entity TabView, Entity[] ContentPanels) CreateTabView(
        IWorld world,
        Entity parent,
        TabConfig[] tabs,
        FontHandle font,
        TabViewConfig? config = null)
    {
        config ??= TabViewConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, tabs.Length - 1));

        // Create container with tab view state
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateTabViewRect(config))
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UITabViewState(selectedIndex))
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create tab bar
        var tabBar = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TabBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTabBarColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = config.TabSpacing
            })
            .Build();

        world.SetParent(tabBar, container);

        // Create tabs with UITabButton component for tab switching
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;
            var tab = tabs[i];

            var tabButton = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0, 1),
                    Size = new Vector2(tab.MinWidth, config.TabBarHeight - 4),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isActive ? config.GetActiveTabColor() : config.GetTabColor(),
                    CornerRadius = 4f,
                    Padding = UIEdges.Symmetric(tab.Padding, 0)
                })
                .With(new UIText
                {
                    Content = tab.Label,
                    Font = font,
                    Color = isActive ? config.GetActiveTabTextColor() : config.GetTabTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true
                })
                .With(new UITabButton(i, container))
                .Build();

            world.SetParent(tabButton, tabBar);
        }

        // Create content area
        var contentArea = world.Spawn()
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
                BackgroundColor = config.GetContentColor()
            })
            .Build();

        world.SetParent(contentArea, container);

        // Create content panels with UITabPanel component for visibility toggling
        var contentPanels = new Entity[tabs.Length];
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;

            var panelBuilder = world.Spawn()
                .With(new UIElement { Visible = isActive, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UILayout
                {
                    Direction = LayoutDirection.Vertical,
                    MainAxisAlign = LayoutAlign.Start,
                    CrossAxisAlign = LayoutAlign.Start,
                    Spacing = 8
                })
                .With(new UITabPanel(i, container));

            // Add UIHiddenTag to non-selected panels so layout system skips them
            if (!isActive)
            {
                panelBuilder = panelBuilder.WithTag<UIHiddenTag>();
            }

            var panel = panelBuilder.Build();

            world.SetParent(panel, contentArea);
            contentPanels[i] = panel;
        }

        return (container, contentPanels);
    }

    /// <summary>
    /// Creates a tab view widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="tabs">The tab configurations.</param>
    /// <param name="font">The font to use for tab labels.</param>
    /// <param name="config">Optional tab view configuration.</param>
    /// <returns>A tuple containing the tab view container and an array of content panel entities.</returns>
    public static (Entity TabView, Entity[] ContentPanels) CreateTabView(
        IWorld world,
        Entity parent,
        string name,
        TabConfig[] tabs,
        FontHandle font,
        TabViewConfig? config = null)
    {
        config ??= TabViewConfig.Default;

        var selectedIndex = Math.Clamp(config.SelectedIndex, 0, Math.Max(0, tabs.Length - 1));

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(CreateTabViewRect(config))
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start
            })
            .With(new UITabViewState(selectedIndex))
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var tabBar = world.Spawn($"{name}_TabBar")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.TabBarHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetTabBarColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.End,
                Spacing = config.TabSpacing
            })
            .Build();

        world.SetParent(tabBar, container);

        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;
            var tab = tabs[i];

            var tabButton = world.Spawn($"{name}_Tab_{i}")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0, 1),
                    Size = new Vector2(tab.MinWidth, config.TabBarHeight - 4),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = isActive ? config.GetActiveTabColor() : config.GetTabColor(),
                    CornerRadius = 4f,
                    Padding = UIEdges.Symmetric(tab.Padding, 0)
                })
                .With(new UIText
                {
                    Content = tab.Label,
                    Font = font,
                    Color = isActive ? config.GetActiveTabTextColor() : config.GetTabTextColor(),
                    FontSize = config.FontSize,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true
                })
                .With(new UITabButton(i, container))
                .Build();

            world.SetParent(tabButton, tabBar);
        }

        var contentArea = world.Spawn($"{name}_Content")
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
                BackgroundColor = config.GetContentColor()
            })
            .Build();

        world.SetParent(contentArea, container);

        var contentPanels = new Entity[tabs.Length];
        for (var i = 0; i < tabs.Length; i++)
        {
            var isActive = i == selectedIndex;

            var panelBuilder = world.Spawn($"{name}_Panel_{i}")
                .With(new UIElement { Visible = isActive, RaycastTarget = false })
                .With(UIRect.Stretch())
                .With(new UILayout
                {
                    Direction = LayoutDirection.Vertical,
                    MainAxisAlign = LayoutAlign.Start,
                    CrossAxisAlign = LayoutAlign.Start,
                    Spacing = 8
                })
                .With(new UITabPanel(i, container));

            // Add UIHiddenTag to non-selected panels so layout system skips them
            if (!isActive)
            {
                panelBuilder = panelBuilder.WithTag<UIHiddenTag>();
            }

            var panel = panelBuilder.Build();

            world.SetParent(panel, contentArea);
            contentPanels[i] = panel;
        }

        return (container, contentPanels);
    }

    private static UIRect CreateTabViewRect(TabViewConfig config)
    {
        if (config.Width.HasValue && config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
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
                Pivot = Vector2.Zero,
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
                Pivot = Vector2.Zero,
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

    #region ScrollView

    /// <summary>
    /// Creates a scroll view widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional scroll view configuration.</param>
    /// <returns>A tuple containing the scroll view container and the content panel entity.</returns>
    /// <remarks>
    /// <para>
    /// The scroll view is composed of:
    /// - A viewport that clips content
    /// - A content panel that can be larger than the viewport
    /// - Optional scrollbars
    /// </para>
    /// <para>
    /// Add children to the returned content panel entity.
    /// The scroll position is stored in the container's <see cref="UIScrollable"/> component.
    /// </para>
    /// </remarks>
    public static (Entity ScrollView, Entity ContentPanel) CreateScrollView(
        IWorld world,
        Entity parent,
        ScrollViewConfig? config = null)
    {
        config ??= ScrollViewConfig.Default;

        // Create container
        var container = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(CreateScrollViewRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UIScrollable
            {
                ScrollPosition = Vector2.Zero,
                ContentSize = new Vector2(
                    config.ContentWidth ?? 0,
                    config.ContentHeight ?? 0),
                HorizontalScroll = config.ShowHorizontalScrollbar,
                VerticalScroll = config.ShowVerticalScrollbar
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Create content panel
        var contentWidth = config.ContentWidth ?? 0;
        var contentHeight = config.ContentHeight ?? 0;

        var contentPanel = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(contentWidth, contentHeight),
                WidthMode = config.ContentWidth.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.ContentHeight.HasValue ? UISizeMode.Fixed : UISizeMode.Fill
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, container);

        // Create vertical scrollbar if enabled
        if (config.ShowVerticalScrollbar)
        {
            var vScrollbar = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = new Vector2(1, 0),
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0),
                    Size = new Vector2(config.ScrollbarWidth, 0),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(vScrollbar, container);

            // Create thumb
            var vThumb = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0.3f), // Initial size, will be updated by system
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .Build();

            world.SetParent(vThumb, vScrollbar);
        }

        // Create horizontal scrollbar if enabled
        if (config.ShowHorizontalScrollbar)
        {
            var hScrollbar = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0),
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.ScrollbarWidth),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(hScrollbar, container);

            var hThumb = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(0.3f, 1), // Initial size, will be updated by system
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .Build();

            world.SetParent(hThumb, hScrollbar);
        }

        return (container, contentPanel);
    }

    /// <summary>
    /// Creates a scroll view widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional scroll view configuration.</param>
    /// <returns>A tuple containing the scroll view container and the content panel entity.</returns>
    public static (Entity ScrollView, Entity ContentPanel) CreateScrollView(
        IWorld world,
        Entity parent,
        string name,
        ScrollViewConfig? config = null)
    {
        config ??= ScrollViewConfig.Default;

        var container = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(CreateScrollViewRect(config))
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UIScrollable
            {
                ScrollPosition = Vector2.Zero,
                ContentSize = new Vector2(
                    config.ContentWidth ?? 0,
                    config.ContentHeight ?? 0),
                HorizontalScroll = config.ShowHorizontalScrollbar,
                VerticalScroll = config.ShowVerticalScrollbar
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        var contentWidth = config.ContentWidth ?? 0;
        var contentHeight = config.ContentHeight ?? 0;

        var contentPanel = world.Spawn($"{name}_Content")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(contentWidth, contentHeight),
                WidthMode = config.ContentWidth.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.ContentHeight.HasValue ? UISizeMode.Fixed : UISizeMode.Fill
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8
            })
            .Build();

        world.SetParent(contentPanel, container);

        if (config.ShowVerticalScrollbar)
        {
            var vScrollbar = world.Spawn($"{name}_VScrollbar")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = new Vector2(1, 0),
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(1, 0),
                    Size = new Vector2(config.ScrollbarWidth, 0),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(vScrollbar, container);

            var vThumb = world.Spawn($"{name}_VThumb")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0.3f),
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .Build();

            world.SetParent(vThumb, vScrollbar);
        }

        if (config.ShowHorizontalScrollbar)
        {
            var hScrollbar = world.Spawn($"{name}_HScrollbar")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(1, 0),
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.ScrollbarWidth),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarTrackColor()
                })
                .Build();

            world.SetParent(hScrollbar, container);

            var hThumb = world.Spawn($"{name}_HThumb")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = new Vector2(0.3f, 1),
                    Pivot = Vector2.Zero,
                    Size = Vector2.Zero,
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fill
                })
                .With(new UIStyle
                {
                    BackgroundColor = config.GetScrollbarThumbColor(),
                    CornerRadius = config.ScrollbarWidth / 2
                })
                .With(new UIInteractable
                {
                    CanDrag = true
                })
                .Build();

            world.SetParent(hThumb, hScrollbar);
        }

        return (container, contentPanel);
    }

    private static UIRect CreateScrollViewRect(ScrollViewConfig config)
    {
        if (config.Width.HasValue && config.Height.HasValue)
        {
            return new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
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
                Pivot = Vector2.Zero,
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
                Pivot = Vector2.Zero,
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
}
