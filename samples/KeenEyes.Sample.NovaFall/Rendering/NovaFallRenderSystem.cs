using System.Globalization;
using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Renders the shaft with flat SDF shapes via <see cref="I2DRenderer"/>: a faked
/// background gradient, the Furnace ceiling, floors as outlined rounded rects, and
/// the ball as an ellipse subtly squashed by its velocity.
/// </summary>
/// <remarks>
/// <para>
/// The renderer is resolved lazily through <c>World.TryGetExtension</c> (it only
/// exists once the window has loaded), following the same pattern as
/// <c>KeenEyes.UI.UIRenderSystem</c>. In headless mode no renderer ever appears and
/// the system is a no-op.
/// </para>
/// <para>
/// The simulation runs in a fixed design space (see <see cref="Tuning"/>); this
/// system scales design units to the current window size, so resizing the window
/// never affects gameplay.
/// </para>
/// </remarks>
/// <param name="fontPath">
/// Path to a TTF font for the HUD, or null to fall back to showing the score in
/// the window title.
/// </param>
public sealed class NovaFallRenderSystem(string? fontPath) : SystemBase
{
    private static readonly Vector4[] tierColors =
    [
        new(0.95f, 0.55f, 0.20f, 1f),  // Ember — warm orange
        new(1.00f, 0.35f, 0.10f, 1f),  // Flame — hot orange-red
        new(0.75f, 0.40f, 1.00f, 1f),  // Plasma — violet
        new(0.85f, 0.95f, 1.00f, 1f),  // Nova — white-blue
    ];

    private static readonly Vector4 floorFill = new(0.16f, 0.20f, 0.32f, 1f);
    private static readonly Vector4 floorOutline = new(0.45f, 0.56f, 0.80f, 1f);
    private static readonly Vector4 hudColor = new(0.92f, 0.94f, 1.00f, 1f);

    private I2DRenderer? renderer;
    private ITextRenderer? textRenderer;
    private FontHandle font;
    private bool fontLoaded;
    private bool fontLoadAttempted;
    private int framesSinceTitleUpdate;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy init: the renderer only exists after the window has loaded, and it
        // never exists in headless simulation mode.
        if (renderer is null && !TryInitializeRenderers())
        {
            return;
        }

        var scaleX = 1f;
        var scaleY = 1f;
        if (World.TryGetExtension<IGraphicsContext>(out var graphics) && graphics.Width > 0 && graphics.Height > 0)
        {
            scaleX = graphics.Width / Tuning.ShaftWidth;
            scaleY = graphics.Height / Tuning.ShaftHeight;
        }

        renderer!.Begin();
        try
        {
            DrawBackground(scaleX, scaleY);
            DrawFloors(scaleX, scaleY);
            DrawBall(scaleX, scaleY);
        }
        finally
        {
            renderer.End();
        }

        DrawHud(scaleX, scaleY);
    }

    private bool TryInitializeRenderers()
    {
        if (!World.TryGetExtension<I2DRenderer>(out renderer)
            && World.TryGetExtension<I2DRendererProvider>(out var provider))
        {
            renderer = provider.Get2DRenderer();
        }

        if (renderer is null)
        {
            return false;
        }

        if (!World.TryGetExtension<ITextRenderer>(out textRenderer)
            && World.TryGetExtension<ITextRendererProvider>(out var textProvider))
        {
            textRenderer = textProvider.GetTextRenderer();
        }

        return true;
    }

    private void DrawBackground(float scaleX, float scaleY)
    {
        var width = Tuning.ShaftWidth * scaleX;
        var height = Tuning.ShaftHeight * scaleY;

        // Faked vertical gradient: a few stacked translucent rects over the clear
        // color — warm near the Furnace above, darkening toward the depths below.
        renderer!.FillRect(0f, 0f, width, height * 0.30f, new Vector4(0.55f, 0.16f, 0.05f, 0.10f));
        renderer.FillRect(0f, 0f, width, height * 0.12f, new Vector4(0.90f, 0.30f, 0.08f, 0.16f));
        renderer.FillRect(0f, height * 0.62f, width, height * 0.38f, new Vector4(0.00f, 0.01f, 0.06f, 0.28f));

        // The Furnace ceiling: a glowing band with a hard edge. Touch it and die.
        var ceilingY = Tuning.CeilingY * scaleY;
        renderer.FillRect(0f, 0f, width, ceilingY, new Vector4(0.95f, 0.42f, 0.10f, 0.35f));
        renderer.DrawLine(0f, ceilingY, width, ceilingY, new Vector4(1.00f, 0.55f, 0.15f, 1f), 3f);
    }

    private void DrawFloors(float scaleX, float scaleY)
    {
        const float cornerRadius = 6f;
        const float outlineThickness = 2f;

        foreach (var entity in World.Query<Floor, Position2D>())
        {
            ref readonly var floor = ref World.Get<Floor>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            var y = position.Y * scaleY;
            var thickness = floor.Thickness * scaleY;
            var gapLeft = (floor.GapCenterX - floor.GapWidth / 2f) * scaleX;
            var gapRight = (floor.GapCenterX + floor.GapWidth / 2f) * scaleX;
            var shaftRight = Tuning.ShaftWidth * scaleX;

            // Each floor is two slabs: wall → gap-left and gap-right → wall.
            DrawSlab(0f, y, gapLeft, thickness, cornerRadius, outlineThickness);
            DrawSlab(gapRight, y, shaftRight - gapRight, thickness, cornerRadius, outlineThickness);
        }
    }

    private void DrawSlab(float x, float y, float width, float height, float cornerRadius, float outlineThickness)
    {
        if (width < 1f)
        {
            return;
        }

        renderer!.FillRoundedRect(x, y, width, height, cornerRadius, floorFill);
        renderer.DrawRoundedRect(x, y, width, height, cornerRadius, floorOutline, outlineThickness);
    }

    private void DrawBall(float scaleX, float scaleY)
    {
        var tier = World.GetSingleton<HeatState>().Tier;
        var color = tierColors[Math.Clamp(tier, 0, tierColors.Length - 1)];

        foreach (var entity in World.Query<Ball, Position2D, Velocity2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);
            ref readonly var velocity = ref World.Get<Velocity2D>(entity);

            // Velocity-driven aspect: elongate along the dominant motion axis.
            // (The full squash/stretch tween is a later phase; this is the cheap
            // teaser that already makes the ball feel alive.)
            var verticalStretch = MathF.Min(MathF.Abs(velocity.Y) / 2800f, 0.35f);
            var horizontalStretch = MathF.Min(MathF.Abs(velocity.X) / 2400f, 0.25f);
            var radiusX = ball.Radius * (1f + horizontalStretch - verticalStretch) * scaleX;
            var radiusY = ball.Radius * (1f + verticalStretch - horizontalStretch) * scaleY;

            renderer!.FillEllipse(position.X * scaleX, position.Y * scaleY, radiusX, radiusY, color);
            renderer.DrawEllipse(
                position.X * scaleX, position.Y * scaleY, radiusX, radiusY,
                new Vector4(1f, 1f, 1f, 0.35f), 2f);
        }
    }

    private void DrawHud(float scaleX, float scaleY)
    {
        var score = World.GetSingleton<ScoreState>();
        var heat = World.GetSingleton<HeatState>();
        var depth = World.GetSingleton<ScrollState>().Depth;
        var phase = World.GetSingleton<GameState>().Phase;

        var hudLine = string.Create(
            CultureInfo.InvariantCulture,
            $"SCORE {score.Score:F0}  x{HeatSystem.MultiplierForTier(heat.Tier)} {HeatSystem.NameForTier(heat.Tier)}  {depth:F0}m");

        EnsureFontLoaded();
        if (fontLoaded && textRenderer is not null)
        {
            textRenderer.Begin();
            try
            {
                textRenderer.DrawText(
                    font, hudLine.AsSpan(),
                    16f * scaleX, (Tuning.CeilingY + 14f) * scaleY,
                    hudColor);

                var prompt = phase switch
                {
                    GamePhase.Ready => "Press A/D or LEFT/RIGHT to dive",
                    GamePhase.Dead => $"BEST {score.Best} — press A/D to dive again",
                    _ => null,
                };

                if (prompt is not null)
                {
                    textRenderer.DrawText(
                        font, prompt.AsSpan(),
                        (Tuning.ShaftWidth / 2f) * scaleX, (Tuning.ShaftHeight / 2f) * scaleY,
                        hudColor, TextAlignH.Center, TextAlignV.Middle);
                }
            }
            finally
            {
                textRenderer.End();
            }
        }
        else
        {
            // Graceful degradation: no usable font, so surface the score through
            // the window title instead (throttled — title updates are not free).
            framesSinceTitleUpdate++;
            if (framesSinceTitleUpdate >= 30 && phase == GamePhase.Playing
                && World.TryGetExtension<ISilkWindowProvider>(out var windowProvider))
            {
                windowProvider.Window.Title = $"NOVAFALL — {hudLine}";
                framesSinceTitleUpdate = 0;
            }
        }
    }

    private void EnsureFontLoaded()
    {
        if (fontLoadAttempted)
        {
            return;
        }

        fontLoadAttempted = true;

        if (fontPath is null
            || !World.TryGetExtension<IFontManagerProvider>(out var fontManagerProvider))
        {
            return;
        }

        var fontManager = fontManagerProvider.GetFontManager();
        if (fontManager is null)
        {
            return;
        }

        try
        {
            font = fontManager.LoadFont(fontPath, 20f);
            fontLoaded = true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Console.WriteLine($"Failed to load font '{fontPath}': {ex.Message}");
        }
    }
}
