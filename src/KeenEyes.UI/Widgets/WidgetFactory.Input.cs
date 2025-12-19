using System.Numerics;

using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for input UI widgets: TextField, Checkbox, Slider, Toggle, Dropdown.
/// </summary>
public static partial class WidgetFactory
{
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

        // Add UICheckbox component for behavior
        world.Add(container, new UICheckbox(config.IsChecked) { BoxEntity = box });

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

        // Add UICheckbox component for behavior
        world.Add(container, new UICheckbox(config.IsChecked) { BoxEntity = box });

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

        // Add UISlider component for behavior
        world.Add(container, new UISlider(config.MinValue, config.MaxValue, config.Value)
        {
            FillEntity = fill,
            ThumbEntity = thumb
        });

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

        // Add UISlider component for behavior
        world.Add(container, new UISlider(config.MinValue, config.MaxValue, config.Value)
        {
            FillEntity = fill,
            ThumbEntity = thumb
        });

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
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(0, 0.5f),
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

        // Add UIToggle component for behavior
        world.Add(container, new UIToggle(config.IsOn)
        {
            TrackEntity = track,
            ThumbEntity = thumb
        });

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
                AnchorMin = new Vector2(0, 0.5f),
                AnchorMax = new Vector2(0, 0.5f),
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

        // Add UIToggle component for behavior
        world.Add(container, new UIToggle(config.IsOn)
        {
            TrackEntity = track,
            ThumbEntity = thumb
        });

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
}
