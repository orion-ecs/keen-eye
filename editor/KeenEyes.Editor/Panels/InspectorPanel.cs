using System.Numerics;
using System.Reflection;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Common.Inspector;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The inspector panel displays the components of the currently selected entity.
/// </summary>
public static class InspectorPanel
{
    /// <summary>
    /// Creates the inspector panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="worldManager">The world manager for scene access.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        EditorWorldManager worldManager)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "InspectorPanel", new PanelConfig(
            Width: 320,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        // Create header
        CreateHeader(editorWorld, panel, font);

        // Create content area (scrollable)
        var contentArea = WidgetFactory.CreatePanel(editorWorld, panel, "InspectorContent", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.10f, 0.10f, 0.13f, 1f),
            Spacing: 2
        ));

        ref var contentRect = ref editorWorld.Get<UIRect>(contentArea);
        contentRect.WidthMode = UISizeMode.Fill;
        contentRect.HeightMode = UISizeMode.Fill;

        // Create empty state label
        var emptyLabel = WidgetFactory.CreateLabel(editorWorld, contentArea, "InspectorEmpty",
            "No entity selected", font, new LabelConfig(
                FontSize: 13,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Center,
                VerticalAlign: TextAlignV.Middle
            ));

        ref var emptyRect = ref editorWorld.Get<UIRect>(emptyLabel);
        emptyRect.WidthMode = UISizeMode.Fill;
        emptyRect.HeightMode = UISizeMode.Fill;

        // Store state
        editorWorld.Add(panel, new InspectorPanelState
        {
            ContentArea = contentArea,
            EmptyLabel = emptyLabel,
            WorldManager = worldManager,
            Font = font
        });

        // Subscribe to selection events
        worldManager.EntitySelected += entity => RefreshInspector(editorWorld, panel, entity);
        worldManager.SelectionCleared += () => ClearInspector(editorWorld, panel);

        return panel;
    }

    private static void CreateHeader(IWorld world, Entity panel, FontHandle font)
    {
        var header = WidgetFactory.CreatePanel(world, panel, "InspectorHeader", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, header, "InspectorTitle", "Inspector", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));
    }

    /// <summary>
    /// Refreshes the inspector to show the selected entity's components.
    /// </summary>
    private static void RefreshInspector(IWorld editorWorld, Entity panel, Entity selectedEntity)
    {
        if (!editorWorld.Has<InspectorPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<InspectorPanelState>(panel);
        var worldManager = state.WorldManager;
        var sceneWorld = worldManager.CurrentSceneWorld;

        if (sceneWorld is null || !selectedEntity.IsValid)
        {
            ClearInspector(editorWorld, panel);
            return;
        }

        // Hide empty label
        if (editorWorld.Has<UIElement>(state.EmptyLabel))
        {
            ref var emptyElement = ref editorWorld.Get<UIElement>(state.EmptyLabel);
            emptyElement.Visible = false;
        }

        // Clear existing component displays
        ClearComponentDisplays(editorWorld, state.ContentArea, state.EmptyLabel);

        // Create entity header with name
        CreateEntityHeader(editorWorld, state.ContentArea, state.Font, worldManager, selectedEntity);

        // Get and display components for this entity
        DisplayComponents(editorWorld, state.ContentArea, state.Font, sceneWorld, selectedEntity);
    }

    private static void DisplayComponents(
        IWorld editorWorld,
        Entity contentArea,
        FontHandle font,
        IWorld sceneWorld,
        Entity entity)
    {
        var components = sceneWorld.GetComponents(entity);

        foreach (var (componentType, componentValue) in components)
        {
            CreateComponentSection(editorWorld, contentArea, font, componentType, componentValue);
        }
    }

    private static void CreateComponentSection(
        IWorld editorWorld,
        Entity contentArea,
        FontHandle font,
        Type componentType,
        object componentValue)
    {
        // Create component header
        var componentPanel = WidgetFactory.CreatePanel(editorWorld, contentArea, $"Component_{componentType.Name}", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.12f, 0.12f, 0.15f, 1f),
            Spacing: 4
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(componentPanel);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.FitContent;

        editorWorld.Add(componentPanel, new InspectorComponentTag());

        // Component header
        var headerPanel = WidgetFactory.CreatePanel(editorWorld, componentPanel, $"Header_{componentType.Name}", new PanelConfig(
            Height: 24,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Start,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new Vector4(0.18f, 0.18f, 0.22f, 1f),
            Padding: UIEdges.Symmetric(8, 0)
        ));

        ref var headerRect = ref editorWorld.Get<UIRect>(headerPanel);
        headerRect.WidthMode = UISizeMode.Fill;

        // Component type name
        WidgetFactory.CreateLabel(editorWorld, headerPanel, $"Label_{componentType.Name}",
            componentType.Name, font, new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Left
            ));

        // Component fields
        var fieldsPanel = WidgetFactory.CreatePanel(editorWorld, componentPanel, $"Fields_{componentType.Name}", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            Padding: UIEdges.All(8),
            Spacing: 4
        ));

        ref var fieldsRect = ref editorWorld.Get<UIRect>(fieldsPanel);
        fieldsRect.WidthMode = UISizeMode.Fill;
        fieldsRect.HeightMode = UISizeMode.FitContent;

        // Get editable fields using ComponentIntrospector
        var fields = ComponentIntrospector.GetEditableFields(componentType);

        foreach (var field in fields)
        {
            CreateFieldRow(editorWorld, fieldsPanel, font, field, componentValue);
        }
    }

    private static void CreateFieldRow(
        IWorld editorWorld,
        Entity fieldsPanel,
        FontHandle font,
        FieldInfo field,
        object componentValue)
    {
        var metadata = ComponentIntrospector.GetFieldMetadata(field);
        var value = ComponentIntrospector.GetFieldValue(componentValue, field);

        // Create field row
        var fieldRow = WidgetFactory.CreatePanel(editorWorld, fieldsPanel, $"Field_{field.Name}", new PanelConfig(
            Height: 20,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center
        ));

        ref var rowRect = ref editorWorld.Get<UIRect>(fieldRow);
        rowRect.WidthMode = UISizeMode.Fill;

        // Field name label
        WidgetFactory.CreateLabel(editorWorld, fieldRow, $"FieldName_{field.Name}",
            metadata.DisplayName, font, new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));

        // Field value label (read-only display for now)
        var valueText = FormatFieldValue(value, field.FieldType);
        var valueColor = metadata.IsReadOnly
            ? EditorColors.TextMuted
            : EditorColors.TextWhite;

        WidgetFactory.CreateLabel(editorWorld, fieldRow, $"FieldValue_{field.Name}",
            valueText, font, new LabelConfig(
                FontSize: 11,
                TextColor: valueColor,
                HorizontalAlign: TextAlignH.Right
            ));
    }

    private static string FormatFieldValue(object? value, Type fieldType)
    {
        if (value is null)
        {
            return "null";
        }

        // Format vectors nicely
        if (value is Vector2 v2)
        {
            return $"({v2.X:F2}, {v2.Y:F2})";
        }

        if (value is Vector3 v3)
        {
            return $"({v3.X:F2}, {v3.Y:F2}, {v3.Z:F2})";
        }

        if (value is Vector4 v4)
        {
            return $"({v4.X:F2}, {v4.Y:F2}, {v4.Z:F2}, {v4.W:F2})";
        }

        if (value is Quaternion q)
        {
            // Convert to euler angles for display
            return $"({q.X:F2}, {q.Y:F2}, {q.Z:F2}, {q.W:F2})";
        }

        // Format floats with precision
        if (value is float f)
        {
            return f.ToString("F2");
        }

        if (value is double d)
        {
            return d.ToString("F2");
        }

        // Entity reference
        if (value is Entity entity)
        {
            return entity.IsValid ? $"Entity({entity.Id})" : "None";
        }

        // Arrays and lists
        if (ComponentIntrospector.IsCollectionType(fieldType))
        {
            var enumerable = value as System.Collections.IEnumerable;
            var count = 0;
            if (enumerable is not null)
            {
                foreach (var _ in enumerable)
                {
                    count++;
                }
            }
            return $"[{count} items]";
        }

        return value.ToString() ?? string.Empty;
    }

    private static void CreateEntityHeader(
        IWorld editorWorld,
        Entity contentArea,
        FontHandle font,
        EditorWorldManager worldManager,
        Entity entity)
    {
        var entityName = worldManager.GetEntityName(entity);

        var headerPanel = WidgetFactory.CreatePanel(editorWorld, contentArea, "EntityHeader", new PanelConfig(
            Height: 32,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Start,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new Vector4(0.15f, 0.15f, 0.18f, 1f),
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref editorWorld.Get<UIRect>(headerPanel);
        headerRect.WidthMode = UISizeMode.Fill;

        // Entity icon (placeholder)
        _ = WidgetFactory.CreatePanel(editorWorld, headerPanel, "EntityIcon", new PanelConfig(
            Width: 16,
            Height: 16,
            BackgroundColor: new Vector4(0.3f, 0.5f, 0.8f, 1f)
        ));

        // Entity name label
        WidgetFactory.CreateLabel(editorWorld, headerPanel, "EntityName", entityName, font, new LabelConfig(
            FontSize: 14,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        editorWorld.Add(headerPanel, new InspectorComponentTag());
    }

    private static void ClearInspector(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<InspectorPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<InspectorPanelState>(panel);

        // Show empty label
        if (editorWorld.Has<UIElement>(state.EmptyLabel))
        {
            ref var emptyElement = ref editorWorld.Get<UIElement>(state.EmptyLabel);
            emptyElement.Visible = true;
        }

        // Clear component displays
        ClearComponentDisplays(editorWorld, state.ContentArea, state.EmptyLabel);
    }

    private static void ClearComponentDisplays(IWorld editorWorld, Entity contentArea, Entity preserveEntity)
    {
        var children = editorWorld.GetChildren(contentArea).ToList();
        foreach (var child in children)
        {
            // Don't remove the empty label
            if (child == preserveEntity)
            {
                continue;
            }

            // Remove component display panels
            if (editorWorld.Has<InspectorComponentTag>(child))
            {
                DespawnRecursive(editorWorld, child);
            }
        }
    }

    private static void DespawnRecursive(IWorld world, Entity entity)
    {
        var children = world.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(world, child);
        }
        world.Despawn(entity);
    }
}

/// <summary>
/// Component storing the state of the inspector panel.
/// </summary>
internal struct InspectorPanelState : IComponent
{
    public Entity ContentArea;
    public Entity EmptyLabel;
    public EditorWorldManager WorldManager;
    public FontHandle Font;
}

/// <summary>
/// Tag component to identify inspector component display panels.
/// </summary>
internal struct InspectorComponentTag : IComponent;
