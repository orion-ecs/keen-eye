using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="ITextRenderer"/> for testing text rendering
/// operations without a real GPU.
/// </summary>
/// <remarks>
/// <para>
/// MockTextRenderer records all text draw commands, enabling verification of UI and
/// text rendering code without actual GPU calls. All text operations are captured in
/// the <see cref="Commands"/> collection.
/// </para>
/// <para>
/// Use this mock to verify that your UI code is drawing the expected text with correct
/// parameters, alignment, and effects.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fontManager = new MockFontManager();
/// var renderer = new MockTextRenderer();
/// var font = fontManager.LoadFont("test.ttf", 16);
///
/// renderer.Begin();
/// renderer.DrawText(font, "Hello World", 100, 100, Colors.White);
/// renderer.DrawTextOutlined(font, "Title", 200, 50, Colors.White, Colors.Black, 2);
/// renderer.End();
///
/// renderer.Commands.Should().HaveCount(2);
/// renderer.Commands[0].Should().BeOfType&lt;DrawTextCommand&gt;();
/// </code>
/// </example>
public sealed class MockTextRenderer : ITextRenderer
{
    private bool isInBatch;
    private Matrix4x4 currentProjection = Matrix4x4.Identity;
    private bool disposed;

    /// <summary>
    /// Gets the list of all recorded text commands.
    /// </summary>
    public List<TextDrawCommand> Commands { get; } = [];

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

    #region Text Rendering

    /// <inheritdoc />
    public void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextCommand(
            font,
            text.ToString(),
            new Vector2(x, y),
            color,
            1f,
            alignH,
            alignV));
    }

    /// <inheritdoc />
    public void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float scale,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextCommand(
            font,
            text.ToString(),
            new Vector2(x, y),
            color,
            scale,
            alignH,
            alignV));
    }

    /// <inheritdoc />
    public void DrawTextRotated(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float rotation,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextRotatedCommand(
            font,
            text.ToString(),
            new Vector2(x, y),
            color,
            rotation,
            alignH,
            alignV));
    }

    /// <inheritdoc />
    public void DrawTextWrapped(
        FontHandle font,
        ReadOnlySpan<char> text,
        in Rectangle bounds,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextWrappedCommand(
            font,
            text.ToString(),
            bounds,
            color,
            alignH,
            alignV));
    }

    /// <inheritdoc />
    public void DrawTextOutlined(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 outlineColor,
        float outlineWidth = 1f,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextOutlinedCommand(
            font,
            text.ToString(),
            new Vector2(x, y),
            textColor,
            outlineColor,
            outlineWidth,
            alignH,
            alignV));
    }

    /// <inheritdoc />
    public void DrawTextShadowed(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 shadowColor,
        Vector2 shadowOffset,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        RecordCommand(new DrawTextShadowedCommand(
            font,
            text.ToString(),
            new Vector2(x, y),
            textColor,
            shadowColor,
            shadowOffset,
            alignH,
            alignV));
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking state.
    /// </summary>
    public void Reset()
    {
        Commands.Clear();
        isInBatch = false;
        currentProjection = Matrix4x4.Identity;
        BeginCount = 0;
        EndCount = 0;
        FlushCount = 0;
        CurrentBatchSize = 0;
    }

    /// <summary>
    /// Clears only the commands list.
    /// </summary>
    public void ClearCommands()
    {
        Commands.Clear();
        CurrentBatchSize = 0;
    }

    private void RecordCommand(TextDrawCommand command)
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
/// Base class for all text draw commands.
/// </summary>
public abstract record TextDrawCommand
{
    /// <summary>
    /// Gets the font handle used for this command.
    /// </summary>
    public abstract FontHandle Font { get; }

    /// <summary>
    /// Gets the text that was drawn.
    /// </summary>
    public abstract string Text { get; }
}

/// <summary>
/// A basic text draw command.
/// </summary>
/// <param name="Font">The font handle.</param>
/// <param name="Text">The text to draw.</param>
/// <param name="Position">The draw position.</param>
/// <param name="Color">The text color.</param>
/// <param name="Scale">The scale factor.</param>
/// <param name="AlignH">The horizontal alignment.</param>
/// <param name="AlignV">The vertical alignment.</param>
public sealed record DrawTextCommand(
    FontHandle Font,
    string Text,
    Vector2 Position,
    Vector4 Color,
    float Scale,
    TextAlignH AlignH,
    TextAlignV AlignV) : TextDrawCommand
{
    /// <inheritdoc />
    public override FontHandle Font { get; } = Font;

    /// <inheritdoc />
    public override string Text { get; } = Text;
}

/// <summary>
/// A rotated text draw command.
/// </summary>
/// <param name="Font">The font handle.</param>
/// <param name="Text">The text to draw.</param>
/// <param name="Position">The rotation origin position.</param>
/// <param name="Color">The text color.</param>
/// <param name="Rotation">The rotation angle in radians.</param>
/// <param name="AlignH">The horizontal alignment.</param>
/// <param name="AlignV">The vertical alignment.</param>
public sealed record DrawTextRotatedCommand(
    FontHandle Font,
    string Text,
    Vector2 Position,
    Vector4 Color,
    float Rotation,
    TextAlignH AlignH,
    TextAlignV AlignV) : TextDrawCommand
{
    /// <inheritdoc />
    public override FontHandle Font { get; } = Font;

    /// <inheritdoc />
    public override string Text { get; } = Text;
}

/// <summary>
/// A wrapped text draw command.
/// </summary>
/// <param name="Font">The font handle.</param>
/// <param name="Text">The text to draw.</param>
/// <param name="Bounds">The bounding rectangle.</param>
/// <param name="Color">The text color.</param>
/// <param name="AlignH">The horizontal alignment.</param>
/// <param name="AlignV">The vertical alignment.</param>
public sealed record DrawTextWrappedCommand(
    FontHandle Font,
    string Text,
    Rectangle Bounds,
    Vector4 Color,
    TextAlignH AlignH,
    TextAlignV AlignV) : TextDrawCommand
{
    /// <inheritdoc />
    public override FontHandle Font { get; } = Font;

    /// <inheritdoc />
    public override string Text { get; } = Text;
}

/// <summary>
/// An outlined text draw command.
/// </summary>
/// <param name="Font">The font handle.</param>
/// <param name="Text">The text to draw.</param>
/// <param name="Position">The draw position.</param>
/// <param name="TextColor">The main text color.</param>
/// <param name="OutlineColor">The outline color.</param>
/// <param name="OutlineWidth">The outline width in pixels.</param>
/// <param name="AlignH">The horizontal alignment.</param>
/// <param name="AlignV">The vertical alignment.</param>
public sealed record DrawTextOutlinedCommand(
    FontHandle Font,
    string Text,
    Vector2 Position,
    Vector4 TextColor,
    Vector4 OutlineColor,
    float OutlineWidth,
    TextAlignH AlignH,
    TextAlignV AlignV) : TextDrawCommand
{
    /// <inheritdoc />
    public override FontHandle Font { get; } = Font;

    /// <inheritdoc />
    public override string Text { get; } = Text;
}

/// <summary>
/// A shadowed text draw command.
/// </summary>
/// <param name="Font">The font handle.</param>
/// <param name="Text">The text to draw.</param>
/// <param name="Position">The draw position.</param>
/// <param name="TextColor">The main text color.</param>
/// <param name="ShadowColor">The shadow color.</param>
/// <param name="ShadowOffset">The shadow offset in pixels.</param>
/// <param name="AlignH">The horizontal alignment.</param>
/// <param name="AlignV">The vertical alignment.</param>
public sealed record DrawTextShadowedCommand(
    FontHandle Font,
    string Text,
    Vector2 Position,
    Vector4 TextColor,
    Vector4 ShadowColor,
    Vector2 ShadowOffset,
    TextAlignH AlignH,
    TextAlignV AlignV) : TextDrawCommand
{
    /// <inheritdoc />
    public override FontHandle Font { get; } = Font;

    /// <inheritdoc />
    public override string Text { get; } = Text;
}

#endregion
