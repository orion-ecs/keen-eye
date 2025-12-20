using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="I2DRenderer"/> for testing 2D rendering
/// operations without a real GPU.
/// </summary>
/// <remarks>
/// <para>
/// Mock2DRenderer records all draw commands, enabling verification of 2D rendering
/// code without actual GPU calls. All drawing operations are captured in the
/// <see cref="Commands"/> collection.
/// </para>
/// <para>
/// Use this mock to verify that your UI or 2D game code is drawing the expected
/// primitives with correct parameters.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var renderer = new Mock2DRenderer();
///
/// renderer.Begin();
/// renderer.FillRect(10, 10, 100, 50, new Vector4(1, 0, 0, 1));
/// renderer.DrawLine(0, 0, 100, 100, new Vector4(0, 1, 0, 1));
/// renderer.End();
///
/// renderer.Commands.Should().HaveCount(2);
/// renderer.Commands[0].Should().BeOfType&lt;FillRectCommand&gt;();
/// </code>
/// </example>
public sealed class Mock2DRenderer : I2DRenderer
{
    private readonly Stack<Rectangle> clipStack = new();
    private bool isInBatch;
    private Matrix4x4 currentProjection = Matrix4x4.Identity;
    private bool disposed;

    /// <summary>
    /// Gets the list of all recorded draw commands.
    /// </summary>
    public List<Draw2DCommand> Commands { get; } = [];

    #region Batch Tracking

    /// <summary>
    /// Gets the number of times <see cref="Begin()"/> has been called.
    /// </summary>
    public int BeginCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="End"/> has been called.
    /// </summary>
    public int EndCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="Flush"/> has been called.
    /// </summary>
    public int FlushCount { get; private set; }

    /// <summary>
    /// Gets whether currently inside a Begin/End batch.
    /// </summary>
    public bool IsInBatch => isInBatch;

    /// <summary>
    /// Gets the current batch size (number of commands since last Begin).
    /// </summary>
    public int CurrentBatchSize { get; private set; }

    /// <summary>
    /// Gets the current projection matrix.
    /// </summary>
    public Matrix4x4 CurrentProjection => currentProjection;

    /// <summary>
    /// Gets the batch hint if set.
    /// </summary>
    public int? BatchHint { get; private set; }

    #endregion

    #region Clipping

    /// <summary>
    /// Gets the current clip rectangle, or null if no clip is active.
    /// </summary>
    public Rectangle? CurrentClip => clipStack.Count > 0 ? clipStack.Peek() : null;

    /// <summary>
    /// Gets the current clip stack depth.
    /// </summary>
    public int ClipStackDepth => clipStack.Count;

    #endregion

    #region Batch Control

    /// <inheritdoc />
    public void Begin()
    {
        Begin(Matrix4x4.Identity);
    }

    /// <inheritdoc />
    public void Begin(in Matrix4x4 projection)
    {
        if (isInBatch)
        {
            throw new InvalidOperationException("Already in a batch. Call End() first.");
        }

        isInBatch = true;
        currentProjection = projection;
        CurrentBatchSize = 0;
        BeginCount++;
    }

    /// <inheritdoc />
    public void End()
    {
        if (!isInBatch)
        {
            throw new InvalidOperationException("Not in a batch. Call Begin() first.");
        }

        isInBatch = false;
        EndCount++;
    }

    /// <inheritdoc />
    public void Flush()
    {
        FlushCount++;
    }

    #endregion

    #region Rectangles

    /// <inheritdoc />
    public void FillRect(float x, float y, float width, float height, Vector4 color)
    {
        RecordCommand(new FillRectCommand(new Rectangle(x, y, width, height), color));
    }

    /// <inheritdoc />
    public void FillRect(in Rectangle rect, Vector4 color)
    {
        RecordCommand(new FillRectCommand(rect, color));
    }

    /// <inheritdoc />
    public void DrawRect(float x, float y, float width, float height, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawRectCommand(new Rectangle(x, y, width, height), color, thickness));
    }

    /// <inheritdoc />
    public void DrawRect(in Rectangle rect, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawRectCommand(rect, color, thickness));
    }

    /// <inheritdoc />
    public void FillRoundedRect(float x, float y, float width, float height, float radius, Vector4 color)
    {
        RecordCommand(new FillRoundedRectCommand(new Rectangle(x, y, width, height), radius, color));
    }

    /// <inheritdoc />
    public void DrawRoundedRect(float x, float y, float width, float height, float radius, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawRoundedRectCommand(new Rectangle(x, y, width, height), radius, color, thickness));
    }

    #endregion

    #region Lines

    /// <inheritdoc />
    public void DrawLine(float x1, float y1, float x2, float y2, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawLineCommand(new Vector2(x1, y1), new Vector2(x2, y2), color, thickness));
    }

    /// <inheritdoc />
    public void DrawLine(Vector2 start, Vector2 end, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawLineCommand(start, end, color, thickness));
    }

    /// <inheritdoc />
    public void DrawLineStrip(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawLineStripCommand(points.ToArray(), color, thickness));
    }

    /// <inheritdoc />
    public void DrawPolygon(ReadOnlySpan<Vector2> points, Vector4 color, float thickness = 1f)
    {
        RecordCommand(new DrawPolygonCommand(points.ToArray(), color, thickness));
    }

    #endregion

    #region Circles and Ellipses

    /// <inheritdoc />
    public void FillCircle(float centerX, float centerY, float radius, Vector4 color, int segments = 32)
    {
        RecordCommand(new FillCircleCommand(new Vector2(centerX, centerY), radius, color, segments));
    }

    /// <inheritdoc />
    public void DrawCircle(float centerX, float centerY, float radius, Vector4 color, float thickness = 1f, int segments = 32)
    {
        RecordCommand(new DrawCircleCommand(new Vector2(centerX, centerY), radius, color, thickness, segments));
    }

    /// <inheritdoc />
    public void FillEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, int segments = 32)
    {
        RecordCommand(new FillEllipseCommand(new Vector2(centerX, centerY), radiusX, radiusY, color, segments));
    }

    /// <inheritdoc />
    public void DrawEllipse(float centerX, float centerY, float radiusX, float radiusY, Vector4 color, float thickness = 1f, int segments = 32)
    {
        RecordCommand(new DrawEllipseCommand(new Vector2(centerX, centerY), radiusX, radiusY, color, thickness, segments));
    }

    #endregion

    #region Textured Quads

    /// <inheritdoc />
    public void DrawTexture(TextureHandle texture, float x, float y, Vector4? tint = null)
    {
        RecordCommand(new DrawTextureCommand(texture, new Rectangle(x, y, 0, 0), null, tint ?? Vector4.One));
    }

    /// <inheritdoc />
    public void DrawTexture(TextureHandle texture, float x, float y, float width, float height, Vector4? tint = null)
    {
        RecordCommand(new DrawTextureCommand(texture, new Rectangle(x, y, width, height), null, tint ?? Vector4.One));
    }

    /// <inheritdoc />
    public void DrawTextureRegion(TextureHandle texture, in Rectangle destRect, in Rectangle sourceRect, Vector4? tint = null)
    {
        RecordCommand(new DrawTextureRegionCommand(texture, destRect, sourceRect, tint ?? Vector4.One));
    }

    /// <inheritdoc />
    public void DrawTextureRotated(TextureHandle texture, in Rectangle destRect, float rotation, Vector2 origin, Vector4? tint = null)
    {
        RecordCommand(new DrawTextureRotatedCommand(texture, destRect, rotation, origin, tint ?? Vector4.One));
    }

    #endregion

    #region Clipping

    /// <inheritdoc />
    public void PushClip(in Rectangle rect)
    {
        var effectiveClip = clipStack.Count > 0 ? clipStack.Peek().Intersection(rect) : rect;
        clipStack.Push(effectiveClip);
        RecordCommand(new PushClipCommand(rect, effectiveClip));
    }

    /// <inheritdoc />
    public void PopClip()
    {
        if (clipStack.Count == 0)
        {
            throw new InvalidOperationException("No clip to pop.");
        }

        clipStack.Pop();
        RecordCommand(new PopClipCommand());
    }

    /// <inheritdoc />
    public void ClearClip()
    {
        clipStack.Clear();
        RecordCommand(new ClearClipCommand());
    }

    #endregion

    #region Batching Hints

    /// <inheritdoc />
    public void SetBatchHint(int count)
    {
        BatchHint = count;
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking state.
    /// </summary>
    public void Reset()
    {
        Commands.Clear();
        clipStack.Clear();
        isInBatch = false;
        currentProjection = Matrix4x4.Identity;
        BeginCount = 0;
        EndCount = 0;
        FlushCount = 0;
        CurrentBatchSize = 0;
        BatchHint = null;
    }

    /// <summary>
    /// Clears only the commands list.
    /// </summary>
    public void ClearCommands()
    {
        Commands.Clear();
        CurrentBatchSize = 0;
    }

    private void RecordCommand(Draw2DCommand command)
    {
        Commands.Add(command);
        CurrentBatchSize++;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Reset();
        }
    }
}

#region Command Types

/// <summary>
/// Base class for all 2D draw commands.
/// </summary>
public abstract record Draw2DCommand;

/// <summary>
/// A filled rectangle command.
/// </summary>
/// <param name="Rect">The rectangle bounds.</param>
/// <param name="Color">The fill color.</param>
public sealed record FillRectCommand(Rectangle Rect, Vector4 Color) : Draw2DCommand;

/// <summary>
/// An outlined rectangle command.
/// </summary>
/// <param name="Rect">The rectangle bounds.</param>
/// <param name="Color">The outline color.</param>
/// <param name="Thickness">The line thickness.</param>
public sealed record DrawRectCommand(Rectangle Rect, Vector4 Color, float Thickness) : Draw2DCommand;

/// <summary>
/// A filled rounded rectangle command.
/// </summary>
/// <param name="Rect">The rectangle bounds.</param>
/// <param name="Radius">The corner radius.</param>
/// <param name="Color">The fill color.</param>
public sealed record FillRoundedRectCommand(Rectangle Rect, float Radius, Vector4 Color) : Draw2DCommand;

/// <summary>
/// An outlined rounded rectangle command.
/// </summary>
/// <param name="Rect">The rectangle bounds.</param>
/// <param name="Radius">The corner radius.</param>
/// <param name="Color">The outline color.</param>
/// <param name="Thickness">The line thickness.</param>
public sealed record DrawRoundedRectCommand(Rectangle Rect, float Radius, Vector4 Color, float Thickness) : Draw2DCommand;

/// <summary>
/// A line command.
/// </summary>
/// <param name="Start">The start point.</param>
/// <param name="End">The end point.</param>
/// <param name="Color">The line color.</param>
/// <param name="Thickness">The line thickness.</param>
public sealed record DrawLineCommand(Vector2 Start, Vector2 End, Vector4 Color, float Thickness) : Draw2DCommand;

/// <summary>
/// A line strip command.
/// </summary>
/// <param name="Points">The points defining the line strip.</param>
/// <param name="Color">The line color.</param>
/// <param name="Thickness">The line thickness.</param>
public sealed record DrawLineStripCommand(Vector2[] Points, Vector4 Color, float Thickness) : Draw2DCommand;

/// <summary>
/// A polygon outline command.
/// </summary>
/// <param name="Points">The polygon vertices.</param>
/// <param name="Color">The outline color.</param>
/// <param name="Thickness">The line thickness.</param>
public sealed record DrawPolygonCommand(Vector2[] Points, Vector4 Color, float Thickness) : Draw2DCommand;

/// <summary>
/// A filled circle command.
/// </summary>
/// <param name="Center">The circle center.</param>
/// <param name="Radius">The circle radius.</param>
/// <param name="Color">The fill color.</param>
/// <param name="Segments">The number of segments.</param>
public sealed record FillCircleCommand(Vector2 Center, float Radius, Vector4 Color, int Segments) : Draw2DCommand;

/// <summary>
/// An outlined circle command.
/// </summary>
/// <param name="Center">The circle center.</param>
/// <param name="Radius">The circle radius.</param>
/// <param name="Color">The outline color.</param>
/// <param name="Thickness">The line thickness.</param>
/// <param name="Segments">The number of segments.</param>
public sealed record DrawCircleCommand(Vector2 Center, float Radius, Vector4 Color, float Thickness, int Segments) : Draw2DCommand;

/// <summary>
/// A filled ellipse command.
/// </summary>
/// <param name="Center">The ellipse center.</param>
/// <param name="RadiusX">The horizontal radius.</param>
/// <param name="RadiusY">The vertical radius.</param>
/// <param name="Color">The fill color.</param>
/// <param name="Segments">The number of segments.</param>
public sealed record FillEllipseCommand(Vector2 Center, float RadiusX, float RadiusY, Vector4 Color, int Segments) : Draw2DCommand;

/// <summary>
/// An outlined ellipse command.
/// </summary>
/// <param name="Center">The ellipse center.</param>
/// <param name="RadiusX">The horizontal radius.</param>
/// <param name="RadiusY">The vertical radius.</param>
/// <param name="Color">The outline color.</param>
/// <param name="Thickness">The line thickness.</param>
/// <param name="Segments">The number of segments.</param>
public sealed record DrawEllipseCommand(Vector2 Center, float RadiusX, float RadiusY, Vector4 Color, float Thickness, int Segments) : Draw2DCommand;

/// <summary>
/// A texture draw command.
/// </summary>
/// <param name="Texture">The texture handle.</param>
/// <param name="DestRect">The destination rectangle.</param>
/// <param name="SourceRect">The source rectangle (null for full texture).</param>
/// <param name="Tint">The color tint.</param>
public sealed record DrawTextureCommand(TextureHandle Texture, Rectangle DestRect, Rectangle? SourceRect, Vector4 Tint) : Draw2DCommand;

/// <summary>
/// A texture region draw command.
/// </summary>
/// <param name="Texture">The texture handle.</param>
/// <param name="DestRect">The destination rectangle.</param>
/// <param name="SourceRect">The source rectangle in texture coordinates.</param>
/// <param name="Tint">The color tint.</param>
public sealed record DrawTextureRegionCommand(TextureHandle Texture, Rectangle DestRect, Rectangle SourceRect, Vector4 Tint) : Draw2DCommand;

/// <summary>
/// A rotated texture draw command.
/// </summary>
/// <param name="Texture">The texture handle.</param>
/// <param name="DestRect">The destination rectangle.</param>
/// <param name="Rotation">The rotation angle in radians.</param>
/// <param name="Origin">The rotation origin.</param>
/// <param name="Tint">The color tint.</param>
public sealed record DrawTextureRotatedCommand(TextureHandle Texture, Rectangle DestRect, float Rotation, Vector2 Origin, Vector4 Tint) : Draw2DCommand;

/// <summary>
/// A push clip command.
/// </summary>
/// <param name="RequestedClip">The requested clip rectangle.</param>
/// <param name="EffectiveClip">The effective clip after intersection.</param>
public sealed record PushClipCommand(Rectangle RequestedClip, Rectangle EffectiveClip) : Draw2DCommand;

/// <summary>
/// A pop clip command.
/// </summary>
public sealed record PopClipCommand : Draw2DCommand;

/// <summary>
/// A clear clip command.
/// </summary>
public sealed record ClearClipCommand : Draw2DCommand;

#endregion
