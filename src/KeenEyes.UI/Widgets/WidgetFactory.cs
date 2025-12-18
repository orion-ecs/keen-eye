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
/// <para>
/// This class is split into partial files by widget category:
/// <list type="bullet">
/// <item><description>WidgetFactory.Basic.cs - Button, Panel, Label, Divider</description></item>
/// <item><description>WidgetFactory.Input.cs - TextField, Checkbox, Slider, Toggle, Dropdown</description></item>
/// <item><description>WidgetFactory.Display.cs - ProgressBar, TabView, ScrollView</description></item>
/// <item><description>WidgetFactory.Window.cs - Window, Splitter</description></item>
/// <item><description>WidgetFactory.Overlay.cs - Tooltip, Popover</description></item>
/// <item><description>WidgetFactory.Menu.cs - Menu, MenuBar, ContextMenu, RadialMenu</description></item>
/// <item><description>WidgetFactory.Dock.cs - DockContainer, DockPanel</description></item>
/// </list>
/// </para>
/// </remarks>
public static partial class WidgetFactory
{
}
