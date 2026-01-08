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
            .With(new UITextInput
            {
                CursorPosition = 0,
                SelectionStart = 0,
                SelectionEnd = 0,
                IsEditing = false,
                MaxLength = config.MaxLength,
                Multiline = false,
                PlaceholderText = config.PlaceholderText,
                ShowingPlaceholder = true
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
            .With(new UITextInput
            {
                CursorPosition = 0,
                SelectionStart = 0,
                SelectionEnd = 0,
                IsEditing = false,
                MaxLength = config.MaxLength,
                Multiline = false,
                PlaceholderText = config.PlaceholderText,
                ShowingPlaceholder = true
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
                State = config.IsOn ? UIInteractionState.Pressed : UIInteractionState.None
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
                State = config.IsOn ? UIInteractionState.Pressed : UIInteractionState.None
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

    #region ColorPicker

    /// <summary>
    /// Creates a color picker widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="config">Optional color picker configuration.</param>
    /// <returns>The created color picker container entity.</returns>
    /// <remarks>
    /// <para>
    /// The color picker is composed of a saturation-value area, hue slider,
    /// alpha slider (optional), and color preview panel.
    /// </para>
    /// <para>
    /// Subscribe to <see cref="UIColorChangedEvent"/> to receive color change notifications.
    /// </para>
    /// </remarks>
    public static Entity CreateColorPicker(
        IWorld world,
        Entity parent,
        ColorPickerConfig? config = null)
    {
        config ??= ColorPickerConfig.Default;
        var initialColor = config.GetInitialColor();

        // Create container
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
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.All(8f)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8f
            })
            .With(new UIColorPicker(initialColor)
            {
                Mode = config.Mode,
                ShowAlpha = config.ShowAlpha,
                ShowHexInput = config.ShowHexInput
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        // Calculate dimensions
        float sliderHeight = 20f;
        float satValHeight = config.Height - 80f - (config.ShowAlpha ? sliderHeight + 8f : 0);
        float satValWidth = config.Width - 16f;

        // Create saturation-value area (for HSV mode)
        var satValArea = world.Spawn()
            .With(new UIElement { Visible = config.Mode != ColorPickerMode.RGB, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(satValWidth, satValHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = UIColorPickerSystem.HsvToRgb(
                    UIColorPickerSystem.RgbToHue(initialColor), 1f, 1f),
                CornerRadius = 4f
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true
            })
            .With(new UIColorSatValArea(container))
            .Build();

        world.SetParent(satValArea, container);

        // Create hue slider
        var hueSlider = world.Spawn()
            .With(new UIElement { Visible = config.Mode != ColorPickerMode.RGB, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, sliderHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
                CornerRadius = sliderHeight / 2
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true
            })
            .With(new UIColorSlider(container, ColorChannel.Hue))
            .Build();

        world.SetParent(hueSlider, container);

        // Create alpha slider (if enabled)
        var alphaSlider = Entity.Null;
        if (config.ShowAlpha)
        {
            alphaSlider = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(0, sliderHeight),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
                    CornerRadius = sliderHeight / 2
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanDrag = true
                })
                .With(new UIColorSlider(container, ColorChannel.Alpha))
                .Build();

            world.SetParent(alphaSlider, container);
        }

        // Create color preview
        Entity previewEntity = Entity.Null;
        if (config.ShowPreview)
        {
            previewEntity = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(0, 30f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = initialColor,
                    CornerRadius = 4f
                })
                .Build();

            world.SetParent(previewEntity, container);
        }

        // Update the picker component with entity references
        ref var picker = ref world.Get<UIColorPicker>(container);
        picker.PreviewEntity = previewEntity;
        picker.HueSliderEntity = hueSlider;
        picker.SatValAreaEntity = satValArea;
        picker.AlphaSliderEntity = alphaSlider;

        return container;
    }

    /// <summary>
    /// Creates a color picker widget with a name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="name">The entity name for identification.</param>
    /// <param name="config">Optional color picker configuration.</param>
    /// <returns>The created color picker container entity.</returns>
    public static Entity CreateColorPicker(
        IWorld world,
        Entity parent,
        string name,
        ColorPickerConfig? config = null)
    {
        config ??= ColorPickerConfig.Default;
        var initialColor = config.GetInitialColor();

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
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
                CornerRadius = config.CornerRadius,
                Padding = UIEdges.All(8f)
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8f
            })
            .With(new UIColorPicker(initialColor)
            {
                Mode = config.Mode,
                ShowAlpha = config.ShowAlpha,
                ShowHexInput = config.ShowHexInput
            })
            .Build();

        if (parent.IsValid)
        {
            world.SetParent(container, parent);
        }

        float sliderHeight = 20f;
        float satValHeight = config.Height - 80f - (config.ShowAlpha ? sliderHeight + 8f : 0);
        float satValWidth = config.Width - 16f;

        var satValArea = world.Spawn($"{name}_SatVal")
            .With(new UIElement { Visible = config.Mode != ColorPickerMode.RGB, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(satValWidth, satValHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = UIColorPickerSystem.HsvToRgb(
                    UIColorPickerSystem.RgbToHue(initialColor), 1f, 1f),
                CornerRadius = 4f
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true
            })
            .With(new UIColorSatValArea(container))
            .Build();

        world.SetParent(satValArea, container);

        var hueSlider = world.Spawn($"{name}_Hue")
            .With(new UIElement { Visible = config.Mode != ColorPickerMode.RGB, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0, sliderHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
                CornerRadius = sliderHeight / 2
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true
            })
            .With(new UIColorSlider(container, ColorChannel.Hue))
            .Build();

        world.SetParent(hueSlider, container);

        var alphaSlider = Entity.Null;
        if (config.ShowAlpha)
        {
            alphaSlider = world.Spawn($"{name}_Alpha")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(0, sliderHeight),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.2f, 0.2f, 0.2f, 1f),
                    CornerRadius = sliderHeight / 2
                })
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanDrag = true
                })
                .With(new UIColorSlider(container, ColorChannel.Alpha))
                .Build();

            world.SetParent(alphaSlider, container);
        }

        Entity previewEntity = Entity.Null;
        if (config.ShowPreview)
        {
            previewEntity = world.Spawn($"{name}_Preview")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Pivot = new Vector2(0.5f, 0.5f),
                    Size = new Vector2(0, 30f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = initialColor,
                    CornerRadius = 4f
                })
                .Build();

            world.SetParent(previewEntity, container);
        }

        ref var picker = ref world.Get<UIColorPicker>(container);
        picker.PreviewEntity = previewEntity;
        picker.HueSliderEntity = hueSlider;
        picker.SatValAreaEntity = satValArea;
        picker.AlphaSliderEntity = alphaSlider;

        return container;
    }

    #endregion

    #region DatePicker

    /// <summary>
    /// Creates a date picker widget.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="config">Optional date picker configuration.</param>
    /// <returns>The created date picker container entity.</returns>
    /// <remarks>
    /// <para>
    /// The date picker is composed of a calendar grid with navigation buttons
    /// and optional time spinners.
    /// </para>
    /// </remarks>
    public static Entity CreateDatePicker(
        IWorld world,
        Entity parent,
        FontHandle font,
        DatePickerConfig? config = null)
    {
        return CreateDatePicker(world, parent, font, "DatePicker", config);
    }

    /// <summary>
    /// Creates a date picker widget with a custom name.
    /// </summary>
    /// <param name="world">The world to create the entity in.</param>
    /// <param name="parent">The parent entity to attach to.</param>
    /// <param name="font">The font to use for text rendering.</param>
    /// <param name="name">The name for the date picker entity.</param>
    /// <param name="config">Optional date picker configuration.</param>
    /// <returns>The created date picker container entity.</returns>
    public static Entity CreateDatePicker(
        IWorld world,
        Entity parent,
        FontHandle font,
        string name,
        DatePickerConfig? config = null)
    {
        config ??= DatePickerConfig.Default;
        var initialValue = config.GetInitialValue();

        // Create main container
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
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 8f
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.15f, 1f),
                CornerRadius = config.CornerRadius
            })
            .With(new UIDatePicker(initialValue)
            {
                Mode = config.Mode,
                TimeFormat = config.TimeFormat,
                ShowSeconds = config.ShowSeconds,
                MinDate = config.MinDate,
                MaxDate = config.MaxDate,
                FirstDayOfWeek = config.FirstDayOfWeek
            })
            .Build();

        if (parent != Entity.Null)
        {
            world.SetParent(container, parent);
        }

        Entity headerEntity = Entity.Null;
        Entity prevButton = Entity.Null;
        Entity nextButton = Entity.Null;
        Entity calendarGrid = Entity.Null;

        // Create calendar components for Date and DateTime modes
        if (config.Mode != DatePickerMode.Time)
        {
            // Create header with navigation
            var headerRow = world.Spawn($"{name}_Header")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Size = new Vector2(0, 32f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.SpaceBetween,
                    CrossAxisAlign = LayoutAlign.Center
                })
                .Build();

            world.SetParent(headerRow, container);

            // Previous month button
            prevButton = world.Spawn($"{name}_Prev")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    Size = new Vector2(32f, 32f),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                    CornerRadius = 4f
                })
                .With(new UIText
                {
                    Content = "<",  // Left arrow (ASCII-safe)
                    Font = font,
                    FontSize = 16f,
                    Color = Vector4.One,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(UIInteractable.Clickable())
                .Build();

            world.SetParent(prevButton, headerRow);

            // Month/year text
            headerEntity = world.Spawn($"{name}_MonthYear")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    Size = new Vector2(0, 32f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIText
                {
                    Content = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(initialValue.Month)} {initialValue.Year}",
                    Font = font,
                    FontSize = 16f,
                    Color = Vector4.One,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(headerEntity, headerRow);

            // Next month button
            nextButton = world.Spawn($"{name}_Next")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIRect
                {
                    Size = new Vector2(32f, 32f),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.3f, 0.3f, 0.3f, 1f),
                    CornerRadius = 4f
                })
                .With(new UIText
                {
                    Content = ">",  // Right arrow (ASCII-safe)
                    Font = font,
                    FontSize = 16f,
                    Color = Vector4.One,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .With(UIInteractable.Clickable())
                .Build();

            world.SetParent(nextButton, headerRow);

            // Create weekday headers (use Start alignment with spacing to match grid)
            var dayHeaderRow = world.Spawn($"{name}_DayHeaders")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Size = new Vector2(0, 24f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.Start,
                    CrossAxisAlign = LayoutAlign.Center,
                    Spacing = 2f  // Match grid spacing
                })
                .Build();

            world.SetParent(dayHeaderRow, container);

            var dayNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            int startDay = (int)config.FirstDayOfWeek;
            for (int i = 0; i < 7; i++)
            {
                int dayIndex = (startDay + i) % 7;
                var dayHeader = world.Spawn($"{name}_DayHeader_{i}")
                    .With(new UIElement { Visible = true, RaycastTarget = false })
                    .With(new UIRect
                    {
                        Size = new Vector2(32f, 24f),
                        WidthMode = UISizeMode.Fixed,
                        HeightMode = UISizeMode.Fixed
                    })
                    .With(new UIText
                    {
                        Content = dayNames[dayIndex][..Math.Min(2, dayNames[dayIndex].Length)],
                        Font = font,
                        FontSize = 12f,
                        Color = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                        HorizontalAlign = TextAlignH.Center,
                        VerticalAlign = TextAlignV.Middle
                    })
                    .Build();

                world.SetParent(dayHeader, dayHeaderRow);
            }

            // Create calendar grid (use Start alignment for consistent cell spacing)
            calendarGrid = world.Spawn($"{name}_Grid")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Size = new Vector2(0, 0),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.FitContent  // Fit to content for proper layout
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.Start,
                    CrossAxisAlign = LayoutAlign.Start,
                    Wrap = true,
                    Spacing = 2f
                })
                .Build();

            world.SetParent(calendarGrid, container);

            // Create day cells (6 rows x 7 days = 42 cells)
            CreateCalendarDays(world, container, calendarGrid, font, name, initialValue, config);
        }

        Entity hourEntity = Entity.Null;
        Entity minuteEntity = Entity.Null;
        Entity secondEntity = Entity.Null;
        Entity ampmEntity = Entity.Null;

        // Create time components for Time and DateTime modes
        if (config.Mode != DatePickerMode.Date)
        {
            var timeRow = world.Spawn($"{name}_TimeRow")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.One,
                    Size = new Vector2(0, 40f),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UILayout
                {
                    Direction = LayoutDirection.Horizontal,
                    MainAxisAlign = LayoutAlign.Center,
                    CrossAxisAlign = LayoutAlign.Center,
                    Spacing = 8f
                })
                .Build();

            world.SetParent(timeRow, container);

            // Hour spinner
            int displayHour = initialValue.Hour;
            if (config.TimeFormat == TimeFormat.Hour12)
            {
                displayHour = displayHour % 12;
                if (displayHour == 0)
                {
                    displayHour = 12;
                }
            }

            hourEntity = CreateTimeSpinner(world, container, timeRow, font, $"{name}_Hour", displayHour.ToString("D2"), TimeField.Hour);

            // Separator
            var separator1 = world.Spawn($"{name}_Sep1")
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    Size = new Vector2(8f, 32f),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIText
                {
                    Content = ":",
                    Font = font,
                    FontSize = 20f,
                    Color = Vector4.One,
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(separator1, timeRow);

            // Minute spinner
            minuteEntity = CreateTimeSpinner(world, container, timeRow, font, $"{name}_Minute", initialValue.Minute.ToString("D2"), TimeField.Minute);

            // Second spinner (optional)
            if (config.ShowSeconds)
            {
                var separator2 = world.Spawn($"{name}_Sep2")
                    .With(new UIElement { Visible = true, RaycastTarget = false })
                    .With(new UIRect
                    {
                        Size = new Vector2(8f, 32f),
                        WidthMode = UISizeMode.Fixed,
                        HeightMode = UISizeMode.Fixed
                    })
                    .With(new UIText
                    {
                        Content = ":",
                        Font = font,
                        FontSize = 20f,
                        Color = Vector4.One,
                        HorizontalAlign = TextAlignH.Center,
                        VerticalAlign = TextAlignV.Middle
                    })
                    .Build();

                world.SetParent(separator2, timeRow);

                secondEntity = CreateTimeSpinner(world, container, timeRow, font, $"{name}_Second", initialValue.Second.ToString("D2"), TimeField.Second);
            }

            // AM/PM toggle (12-hour mode only)
            if (config.TimeFormat == TimeFormat.Hour12)
            {
                ampmEntity = CreateTimeSpinner(world, container, timeRow, font, $"{name}_AmPm", initialValue.Hour >= 12 ? "PM" : "AM", TimeField.AmPm);
            }
        }

        // Update picker with entity references
        ref var datePicker = ref world.Get<UIDatePicker>(container);
        datePicker.HeaderEntity = headerEntity;
        datePicker.PrevMonthButton = prevButton;
        datePicker.NextMonthButton = nextButton;
        datePicker.CalendarGridEntity = calendarGrid;
        datePicker.HourEntity = hourEntity;
        datePicker.MinuteEntity = minuteEntity;
        datePicker.SecondEntity = secondEntity;
        datePicker.AmPmEntity = ampmEntity;

        return container;
    }

    private static void CreateCalendarDays(
        IWorld world,
        Entity pickerEntity,
        Entity gridEntity,
        FontHandle font,
        string name,
        DateTime initialValue,
        DatePickerConfig config)
    {
        var today = DateTime.Today;
        int year = initialValue.Year;
        int month = initialValue.Month;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int firstDayOfMonth = (int)new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified).DayOfWeek;
        int firstDayOffset = (int)config.FirstDayOfWeek;

        // Calculate the offset for the first day
        int startOffset = (firstDayOfMonth - firstDayOffset + 7) % 7;

        // Previous month info
        int prevMonth = month - 1;
        int prevYear = year;
        if (prevMonth < 1)
        {
            prevMonth = 12;
            prevYear--;
        }
        int daysInPrevMonth = DateTime.DaysInMonth(prevYear, prevMonth);

        int cellIndex = 0;
        float cellSize = 32f;

        // Previous month overflow days
        for (int i = startOffset - 1; i >= 0; i--)
        {
            int day = daysInPrevMonth - i;
            CreateDayCell(world, pickerEntity, gridEntity, font, name, cellIndex++, day, prevMonth, prevYear,
                false, false, false, IsDateDisabled(prevYear, prevMonth, day, config), cellSize);
        }

        // Current month days
        for (int day = 1; day <= daysInMonth; day++)
        {
            bool isToday = year == today.Year && month == today.Month && day == today.Day;
            bool isSelected = year == initialValue.Year && month == initialValue.Month && day == initialValue.Day;
            bool isDisabled = IsDateDisabled(year, month, day, config);

            CreateDayCell(world, pickerEntity, gridEntity, font, name, cellIndex++, day, month, year,
                true, isToday, isSelected, isDisabled, cellSize);
        }

        // Next month overflow days (fill remaining cells to 42)
        int nextMonth = month + 1;
        int nextYear = year;
        if (nextMonth > 12)
        {
            nextMonth = 1;
            nextYear++;
        }

        int nextDay = 1;
        while (cellIndex < 42)
        {
            CreateDayCell(world, pickerEntity, gridEntity, font, name, cellIndex++, nextDay++, nextMonth, nextYear,
                false, false, false, IsDateDisabled(nextYear, nextMonth, nextDay - 1, config), cellSize);
        }
    }

    private static bool IsDateDisabled(int year, int month, int day, DatePickerConfig config)
    {
        var date = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        return (config.MinDate.HasValue && date < config.MinDate.Value.Date) ||
               (config.MaxDate.HasValue && date > config.MaxDate.Value.Date);
    }

    private static void CreateDayCell(
        IWorld world,
        Entity pickerEntity,
        Entity gridEntity,
        FontHandle font,
        string name,
        int index,
        int day,
        int month,
        int year,
        bool isCurrentMonth,
        bool isToday,
        bool isSelected,
        bool isDisabled,
        float size)
    {
        var bgColor = GetDayCellColor(isCurrentMonth, isToday, isSelected, isDisabled);
        var textColor = isDisabled || !isCurrentMonth
            ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
            : Vector4.One;

        var cell = world.Spawn($"{name}_Day{index}")
            .With(new UIElement { Visible = true, RaycastTarget = !isDisabled })
            .With(new UIRect
            {
                Size = new Vector2(size, size),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = bgColor,
                CornerRadius = 4f
            })
            .With(new UIText
            {
                Content = day.ToString(),
                Font = font,
                FontSize = 14f,
                Color = textColor,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UICalendarDay(pickerEntity, day, month, year)
            {
                IsCurrentMonth = isCurrentMonth,
                IsToday = isToday,
                IsSelected = isSelected,
                IsDisabled = isDisabled
            })
            .Build();

        if (!isDisabled)
        {
            world.Add(cell, UIInteractable.Clickable());
        }

        world.SetParent(cell, gridEntity);
    }

    private static Vector4 GetDayCellColor(bool isCurrentMonth, bool isToday, bool isSelected, bool isDisabled)
    {
        if (isDisabled)
        {
            return new Vector4(0.15f, 0.15f, 0.15f, 1f);
        }

        if (isSelected)
        {
            return new Vector4(0.3f, 0.5f, 0.9f, 1f);
        }

        if (isToday)
        {
            return new Vector4(0.4f, 0.4f, 0.4f, 1f);
        }

        if (!isCurrentMonth)
        {
            return new Vector4(0.2f, 0.2f, 0.2f, 0.5f);
        }

        return new Vector4(0.25f, 0.25f, 0.25f, 1f);
    }

    private static Entity CreateTimeSpinner(
        IWorld world,
        Entity pickerEntity,
        Entity parentRow,
        FontHandle font,
        string name,
        string value,
        TimeField field)
    {
        var spinner = world.Spawn(name)
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                Size = new Vector2(40f, 32f),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.25f, 0.25f, 0.25f, 1f),
                CornerRadius = 4f
            })
            .With(new UIText
            {
                Content = value,
                Font = font,
                FontSize = 18f,
                Color = Vector4.One,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UITimeSpinner(pickerEntity, field))
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(spinner, parentRow);

        return spinner;
    }

    #endregion
}
