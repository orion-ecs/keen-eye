using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Interface for graph-specific rendering operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides specialized rendering methods for graph editor elements including
/// connections, ports, and selection visualization.
/// </para>
/// </remarks>
public interface IGraphRenderer
{
    /// <summary>
    /// Draws a connection line between two ports.
    /// </summary>
    /// <param name="start">The start position in screen coordinates.</param>
    /// <param name="end">The end position in screen coordinates.</param>
    /// <param name="type">The port type for color determination.</param>
    /// <param name="style">The connection rendering style.</param>
    /// <param name="isSelected">Whether the connection is selected.</param>
    /// <param name="requiresConversion">Whether implicit type conversion is needed.</param>
    void DrawConnection(
        Vector2 start,
        Vector2 end,
        PortTypeId type,
        ConnectionStyle style,
        bool isSelected,
        bool requiresConversion);

    /// <summary>
    /// Draws the background grid for the canvas.
    /// </summary>
    /// <param name="visibleArea">The visible area in canvas coordinates.</param>
    /// <param name="gridSize">The grid spacing in pixels.</param>
    /// <param name="zoom">The current zoom level.</param>
    void DrawGrid(Rectangle visibleArea, float gridSize, float zoom);

    /// <summary>
    /// Draws the selection box during box-select interaction.
    /// </summary>
    /// <param name="bounds">The selection rectangle in screen coordinates.</param>
    void DrawSelectionBox(Rectangle bounds);

    /// <summary>
    /// Draws a highlight around a port.
    /// </summary>
    /// <param name="position">The port center in screen coordinates.</param>
    /// <param name="type">The port type for color determination.</param>
    /// <param name="isValidTarget">Whether the port is a valid connection target.</param>
    void DrawPortHighlight(Vector2 position, PortTypeId type, bool isValidTarget);

    /// <summary>
    /// Draws a preview connection line during drag-to-connect.
    /// </summary>
    /// <param name="start">The start position in screen coordinates.</param>
    /// <param name="end">The end position in screen coordinates.</param>
    /// <param name="sourceType">The source port type.</param>
    /// <param name="targetType">The target port type, or null if not over a valid port.</param>
    /// <param name="style">The connection rendering style.</param>
    void DrawConnectionPreview(
        Vector2 start,
        Vector2 end,
        PortTypeId sourceType,
        PortTypeId? targetType,
        ConnectionStyle style);
}
