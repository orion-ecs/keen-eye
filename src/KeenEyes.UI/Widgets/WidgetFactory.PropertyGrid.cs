using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for creating property grid widgets.
/// </summary>
public static partial class WidgetFactory
{
    /// <summary>
    /// Creates a property grid widget.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for labels and values.</param>
    /// <param name="config">Optional configuration.</param>
    /// <returns>The property grid entity.</returns>
    public static Entity CreatePropertyGrid(
        IWorld world,
        Entity parent,
        FontHandle font,
        PropertyGridConfig? config = null)
    {
        config ??= PropertyGridConfig.Default;

        // Create the main container
        var gridEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width ?? 0, config.Height ?? 0),
                WidthMode = config.Width.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .With(new UIPropertyGrid(config.LabelWidthRatio)
            {
                ContentContainer = Entity.Null,
                CategoryCount = 0,
                RowCount = 0
            })
            .Build();

        world.SetParent(gridEntity, parent);

        // Create content container
        var contentContainer = world.Spawn()
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
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        world.SetParent(contentContainer, gridEntity);

        // Update grid reference
        ref var grid = ref world.Get<UIPropertyGrid>(gridEntity);
        grid.ContentContainer = contentContainer;

        return gridEntity;
    }

    /// <summary>
    /// Creates a property category within a property grid.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="propertyGrid">The parent property grid entity.</param>
    /// <param name="name">The category display name.</param>
    /// <param name="font">The font for the category header.</param>
    /// <param name="config">Optional configuration.</param>
    /// <param name="isExpanded">Initial expanded state.</param>
    /// <returns>The category entity.</returns>
    public static Entity CreatePropertyCategory(
        IWorld world,
        Entity propertyGrid,
        string name,
        FontHandle font,
        PropertyGridConfig? config = null,
        bool isExpanded = true)
    {
        config ??= PropertyGridConfig.Default;

        ref var grid = ref world.Get<UIPropertyGrid>(propertyGrid);
        var categoryIndex = grid.CategoryCount;
        grid.CategoryCount++;

        // Create category container
        var categoryEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .With(new UIPropertyCategory(propertyGrid, name)
            {
                IsExpanded = isExpanded,
                ContentContainer = Entity.Null,
                Index = categoryIndex
            })
            .Build();

        world.SetParent(categoryEntity, grid.ContentContainer);

        // Create header row
        var headerEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.CategoryHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetCategoryColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 4
            })
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(headerEntity, categoryEntity);
        world.Add(headerEntity, new UIPropertyCategoryHeaderTag());

        // Create expand arrow
        var arrowEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(16, 16),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = isExpanded
                    ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
                    : new Vector4(0.7f, 0.7f, 0.7f, 1f)
            })
            .Build();

        world.SetParent(arrowEntity, headerEntity);
        world.Add(arrowEntity, new UIPropertyCategoryArrowTag());

        // Create category label
        var labelEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UIText
            {
                Content = name,
                Font = font,
                FontSize = config.CategoryFontSize,
                Color = config.GetLabelColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, headerEntity);

        // Create content container for properties
        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = isExpanded, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        world.SetParent(contentContainer, categoryEntity);

        // Update category reference
        ref var category = ref world.Get<UIPropertyCategory>(categoryEntity);
        category.ContentContainer = contentContainer;

        return categoryEntity;
    }

    /// <summary>
    /// Creates a property row within a property grid or category.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="propertyGrid">The property grid entity.</param>
    /// <param name="parentContainer">The parent container (category content or grid content).</param>
    /// <param name="propertyName">The unique property identifier.</param>
    /// <param name="label">The display label.</param>
    /// <param name="propertyType">The type of property editor.</param>
    /// <param name="font">The font for labels.</param>
    /// <param name="config">Optional configuration.</param>
    /// <param name="category">Optional category entity.</param>
    /// <param name="isReadOnly">Whether the property is read-only.</param>
    /// <returns>The property row entity.</returns>
    public static Entity CreatePropertyRow(
        IWorld world,
        Entity propertyGrid,
        Entity parentContainer,
        string propertyName,
        string label,
        PropertyType propertyType,
        FontHandle font,
        PropertyGridConfig? config = null,
        Entity category = default,
        bool isReadOnly = false)
    {
        config ??= PropertyGridConfig.Default;

        ref var grid = ref world.Get<UIPropertyGrid>(propertyGrid);
        var rowIndex = grid.RowCount;
        grid.RowCount++;

        // Alternate row colors
        var rowColor = rowIndex % 2 == 1
            ? config.GetRowAlternateColor()
            : Vector4.Zero;

        // Create row container
        var rowEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.RowHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = rowColor
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 0
            })
            .With(new UIPropertyRow(propertyGrid, propertyName)
            {
                Label = label,
                Type = propertyType,
                Category = category,
                EditorEntity = Entity.Null,
                Index = rowIndex,
                IsReadOnly = isReadOnly
            })
            .Build();

        world.SetParent(rowEntity, parentContainer);

        // Create label column (fixed width based on ratio)
        // For a 300px wide grid with 0.4 ratio, label would be 120px
        float labelWidth = 120f; // reasonable default for label column

        var labelEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(labelWidth, config.RowHeight),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle())
            .With(new UIText
            {
                Content = label,
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetLabelColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(labelEntity, rowEntity);

        // Create value column (fills remaining space)
        var valueEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.RowHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 2
            })
            .Build();

        world.SetParent(valueEntity, rowEntity);

        // Create appropriate editor based on property type
        var editorEntity = CreatePropertyEditor(world, valueEntity, propertyType, font, config, isReadOnly);

        // Update row reference
        ref var row = ref world.Get<UIPropertyRow>(rowEntity);
        row.EditorEntity = editorEntity;

        return rowEntity;
    }

    private static Entity CreatePropertyEditor(
        IWorld world,
        Entity parent,
        PropertyType propertyType,
        FontHandle font,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        return propertyType switch
        {
            PropertyType.String or PropertyType.MultiLineString =>
                CreateStringEditor(world, parent, font, config, isReadOnly),
            PropertyType.Int or PropertyType.Float =>
                CreateNumericEditor(world, parent, font, config, isReadOnly),
            PropertyType.Bool =>
                CreateBoolEditor(world, parent, config, isReadOnly),
            PropertyType.Color =>
                CreateColorEditor(world, parent, config, isReadOnly),
            PropertyType.Vector2 =>
                CreateVectorEditor(world, parent, font, config, 2, isReadOnly),
            PropertyType.Vector3 =>
                CreateVectorEditor(world, parent, font, config, 3, isReadOnly),
            PropertyType.Vector4 =>
                CreateVectorEditor(world, parent, font, config, 4, isReadOnly),
            PropertyType.Enum =>
                CreateEnumEditor(world, parent, font, config, isReadOnly),
            _ => CreateStringEditor(world, parent, font, config, isReadOnly)
        };
    }

    private static Entity CreateStringEditor(
        IWorld world,
        Entity parent,
        FontHandle font,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        var editorBuilder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.RowHeight - 4),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.1f, 0.1f, 0.12f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.35f, 1f),
                BorderWidth = 1
            })
            .With(new UIText
            {
                Content = "",
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetValueColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            });

        if (!isReadOnly)
        {
            editorBuilder = editorBuilder
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true,
                    CanDrag = false,
                    TabIndex = 0
                })
                .With(UITextInput.SingleLine());
        }

        var editorEntity = editorBuilder.Build();
        world.SetParent(editorEntity, parent);
        return editorEntity;
    }

    private static Entity CreateNumericEditor(
        IWorld world,
        Entity parent,
        FontHandle font,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        var editorBuilder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.RowHeight - 4),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.1f, 0.1f, 0.12f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.35f, 1f),
                BorderWidth = 1
            })
            .With(new UIText
            {
                Content = "0",
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetValueColor(),
                HorizontalAlign = TextAlignH.Right,
                VerticalAlign = TextAlignV.Middle
            });

        if (!isReadOnly)
        {
            editorBuilder = editorBuilder
                .With(new UIInteractable
                {
                    CanClick = true,
                    CanFocus = true,
                    CanDrag = true, // Keep drag-to-change behavior
                    TabIndex = 0
                })
                .With(UITextInput.SingleLine());
        }

        var editorEntity = editorBuilder.Build();
        world.SetParent(editorEntity, parent);
        return editorEntity;
    }

    private static Entity CreateBoolEditor(
        IWorld world,
        Entity parent,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        var editorBuilder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(18, 18),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.18f, 1f),
                BorderColor = new Vector4(0.4f, 0.4f, 0.45f, 1f),
                BorderWidth = 1
            });

        if (!isReadOnly)
        {
            editorBuilder = editorBuilder.With(UIInteractable.Clickable());
        }

        var editorEntity = editorBuilder.Build();
        world.SetParent(editorEntity, parent);
        return editorEntity;
    }

    private static Entity CreateColorEditor(
        IWorld world,
        Entity parent,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        var editorBuilder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.RowHeight - 4, config.RowHeight - 4),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(1f, 1f, 1f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.35f, 1f),
                BorderWidth = 1
            });

        if (!isReadOnly)
        {
            editorBuilder = editorBuilder.With(UIInteractable.Clickable());
        }

        var editorEntity = editorBuilder.Build();
        world.SetParent(editorEntity, parent);
        return editorEntity;
    }

    private static Entity CreateVectorEditor(
        IWorld world,
        Entity parent,
        FontHandle font,
        PropertyGridConfig config,
        int componentCount,
        bool isReadOnly)
    {
        var containerEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 4
            })
            .Build();

        world.SetParent(containerEntity, parent);

        string[] labels = componentCount switch
        {
            2 => ["X", "Y"],
            3 => ["X", "Y", "Z"],
            4 => ["X", "Y", "Z", "W"],
            _ => ["X", "Y"]
        };

        Vector4[] colors =
        [
            new Vector4(0.8f, 0.3f, 0.3f, 1f), // Red for X
            new Vector4(0.3f, 0.8f, 0.3f, 1f), // Green for Y
            new Vector4(0.3f, 0.5f, 0.8f, 1f), // Blue for Z
            new Vector4(0.7f, 0.7f, 0.7f, 1f)  // Gray for W
        ];

        for (int i = 0; i < componentCount; i++)
        {
            // Label
            var labelEntity = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = false })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.Zero,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(12, config.RowHeight),
                    WidthMode = UISizeMode.Fixed,
                    HeightMode = UISizeMode.FitContent
                })
                .With(new UIStyle())
                .With(new UIText
                {
                    Content = labels[i],
                    Font = font,
                    FontSize = config.FontSize - 1,
                    Color = colors[i],
                    HorizontalAlign = TextAlignH.Center,
                    VerticalAlign = TextAlignV.Middle
                })
                .Build();

            world.SetParent(labelEntity, containerEntity);

            // Value field
            var fieldBuilder = world.Spawn()
                .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
                .With(new UIRect
                {
                    AnchorMin = Vector2.Zero,
                    AnchorMax = Vector2.Zero,
                    Pivot = Vector2.Zero,
                    Size = new Vector2(0, config.RowHeight - 4),
                    WidthMode = UISizeMode.Fill,
                    HeightMode = UISizeMode.Fixed
                })
                .With(new UIStyle
                {
                    BackgroundColor = new Vector4(0.1f, 0.1f, 0.12f, 1f),
                    BorderColor = new Vector4(0.3f, 0.3f, 0.35f, 1f),
                    BorderWidth = 1
                })
                .With(new UIText
                {
                    Content = "0",
                    Font = font,
                    FontSize = config.FontSize,
                    Color = config.GetValueColor(),
                    HorizontalAlign = TextAlignH.Right,
                    VerticalAlign = TextAlignV.Middle
                });

            if (!isReadOnly)
            {
                fieldBuilder = fieldBuilder
                    .With(new UIInteractable
                    {
                        CanClick = true,
                        CanFocus = true,
                        CanDrag = true,
                        TabIndex = 0
                    })
                    .With(UITextInput.SingleLine());
            }

            var fieldEntity = fieldBuilder.Build();
            world.SetParent(fieldEntity, containerEntity);
        }

        return containerEntity;
    }

    private static Entity CreateEnumEditor(
        IWorld world,
        Entity parent,
        FontHandle font,
        PropertyGridConfig config,
        bool isReadOnly)
    {
        var editorBuilder = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = !isReadOnly })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.RowHeight - 4),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.15f, 0.15f, 0.18f, 1f),
                BorderColor = new Vector4(0.3f, 0.3f, 0.35f, 1f),
                BorderWidth = 1
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.SpaceBetween,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 0
            });

        if (!isReadOnly)
        {
            editorBuilder = editorBuilder.With(UIInteractable.Clickable());
        }

        var editorEntity = editorBuilder.Build();
        world.SetParent(editorEntity, parent);

        // Value text
        var valueText = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UIText
            {
                Content = "",
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetValueColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(valueText, editorEntity);

        // Dropdown arrow
        var arrowEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = new Vector2(12, 12),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1f)
            })
            .Build();

        world.SetParent(arrowEntity, editorEntity);

        return editorEntity;
    }

    /// <summary>
    /// Creates a property grid with categorized properties from definitions.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for text.</param>
    /// <param name="categories">The property category definitions.</param>
    /// <param name="config">Optional configuration.</param>
    /// <returns>The property grid entity.</returns>
    public static Entity CreatePropertyGridWithCategories(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<PropertyCategoryDef> categories,
        PropertyGridConfig? config = null)
    {
        var gridEntity = CreatePropertyGrid(world, parent, font, config);

        foreach (var categoryDef in categories)
        {
            var categoryEntity = CreatePropertyCategory(
                world, gridEntity, categoryDef.Name, font, config, categoryDef.IsExpanded);

            ref var category = ref world.Get<UIPropertyCategory>(categoryEntity);

            if (categoryDef.Properties != null)
            {
                foreach (var propDef in categoryDef.Properties)
                {
                    CreatePropertyRow(
                        world,
                        gridEntity,
                        category.ContentContainer,
                        propDef.Name,
                        propDef.Label ?? propDef.Name,
                        propDef.Type,
                        font,
                        config,
                        categoryEntity,
                        propDef.IsReadOnly);
                }
            }
        }

        return gridEntity;
    }

    /// <summary>
    /// Creates a property grid with flat (uncategorized) properties.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for text.</param>
    /// <param name="properties">The property definitions.</param>
    /// <param name="config">Optional configuration.</param>
    /// <returns>The property grid entity.</returns>
    public static Entity CreatePropertyGridFlat(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<PropertyDef> properties,
        PropertyGridConfig? config = null)
    {
        var gridEntity = CreatePropertyGrid(world, parent, font, config);
        ref var grid = ref world.Get<UIPropertyGrid>(gridEntity);

        foreach (var propDef in properties)
        {
            CreatePropertyRow(
                world,
                gridEntity,
                grid.ContentContainer,
                propDef.Name,
                propDef.Label ?? propDef.Name,
                propDef.Type,
                font,
                config,
                Entity.Null,
                propDef.IsReadOnly);
        }

        return gridEntity;
    }

    /// <summary>
    /// Sets the value of a property row.
    /// </summary>
    /// <param name="world">The world containing the property.</param>
    /// <param name="rowEntity">The property row entity.</param>
    /// <param name="value">The new value to set.</param>
    public static void SetPropertyValue(IWorld world, Entity rowEntity, object? value)
    {
        ref var row = ref world.Get<UIPropertyRow>(rowEntity);

        if (!row.EditorEntity.IsValid)
        {
            return;
        }

        // Update visual based on property type
        switch (row.Type)
        {
            case PropertyType.String:
            case PropertyType.MultiLineString:
                if (world.Has<UIText>(row.EditorEntity))
                {
                    ref var text = ref world.Get<UIText>(row.EditorEntity);
                    text.Content = value?.ToString() ?? "";
                }
                break;

            case PropertyType.Int:
            case PropertyType.Float:
                if (world.Has<UIText>(row.EditorEntity))
                {
                    ref var text = ref world.Get<UIText>(row.EditorEntity);
                    text.Content = value?.ToString() ?? "0";
                }
                break;

            case PropertyType.Bool:
                if (world.Has<UIStyle>(row.EditorEntity))
                {
                    ref var style = ref world.Get<UIStyle>(row.EditorEntity);
                    var isChecked = value is bool b && b;
                    style.BackgroundColor = isChecked
                        ? new Vector4(0.3f, 0.6f, 0.9f, 1f)
                        : new Vector4(0.15f, 0.15f, 0.18f, 1f);
                }
                break;

            case PropertyType.Color:
                if (world.Has<UIStyle>(row.EditorEntity) && value is Vector4 color)
                {
                    ref var style = ref world.Get<UIStyle>(row.EditorEntity);
                    style.BackgroundColor = color;
                }
                break;

            case PropertyType.Enum:
                // Find the text child and update it
                foreach (var child in world.GetChildren(row.EditorEntity))
                {
                    if (world.Has<UIText>(child))
                    {
                        ref var text = ref world.Get<UIText>(child);
                        text.Content = value?.ToString() ?? "";
                        break;
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Expands or collapses a property category.
    /// </summary>
    /// <param name="world">The world containing the category.</param>
    /// <param name="categoryEntity">The category entity.</param>
    /// <param name="isExpanded">The new expanded state.</param>
    public static void SetPropertyCategoryExpanded(IWorld world, Entity categoryEntity, bool isExpanded)
    {
        ref var category = ref world.Get<UIPropertyCategory>(categoryEntity);
        category.IsExpanded = isExpanded;

        // Update content visibility
        if (category.ContentContainer.IsValid && world.Has<UIElement>(category.ContentContainer))
        {
            ref var contentElement = ref world.Get<UIElement>(category.ContentContainer);
            contentElement.Visible = isExpanded;
        }

        // Update arrow visual
        foreach (var child in world.GetChildren(categoryEntity))
        {
            if (world.Has<UIPropertyCategoryHeaderTag>(child))
            {
                foreach (var headerChild in world.GetChildren(child))
                {
                    if (world.Has<UIPropertyCategoryArrowTag>(headerChild) && world.Has<UIStyle>(headerChild))
                    {
                        ref var style = ref world.Get<UIStyle>(headerChild);
                        style.BackgroundColor = isExpanded
                            ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
                            : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                        break;
                    }
                }
                break;
            }
        }
    }
}
