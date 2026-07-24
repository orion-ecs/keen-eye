using System.Globalization;
using System.Numerics;
using KeenEyes.Animation.Components;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles;
using KeenEyes.Particles.Components;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Renders the whole shaft in one camera pass: palette-driven background, the
/// Furnace ceiling, the additive comet trail and glow stack, the particle pools
/// (drawn here, not by the stock particle render system), the floors, the
/// squash-and-stretch ball, and the death flash. The HUD score is drawn on top
/// in screen space with outlined text.
/// </summary>
/// <remarks>
/// <para>
/// HOW CAMERA + BLEND + PALETTE COMPOSE — the three Phase B render ideas meet
/// here, and each has one owner:
/// <list type="bullet">
///   <item><description><b>Camera:</b> <see cref="CameraSystem"/> writes
///   <see cref="CameraState"/> (shake, kick, zoom); this system folds it into an
///   orthographic projection over DESIGN space and hands it to
///   <c>I2DRenderer.Begin(matrix)</c>. Every world-space draw in the pass —
///   including particles — shakes and zooms together, and window size stops
///   mattering entirely (the matrix, not per-draw scaling, maps design units to
///   the screen).</description></item>
///   <item><description><b>Blend:</b> layers switch with
///   <c>SetBlendMode</c>, which flushes the batch at exactly that boundary:
///   additive for trail/glow/sparks (light adds up), alpha for debris, floors,
///   and the ball (solids occlude).</description></item>
///   <item><description><b>Palette:</b> every color below reads the
///   <see cref="Palette"/> singleton, which <see cref="PaletteSystem"/> tweens
///   through tier changes — the render system never knows what tier it is.</description></item>
/// </list>
/// </para>
/// <para>
/// WHY PARTICLES ARE DRAWN HERE — the stock <c>ParticleRenderSystem</c> opens
/// its own default-projection batch, which would ignore the camera and draw
/// either before or after ALL of this system's output. NOVAFALL's readability
/// contract requires background → trail/glow → particles → floors → ball, in
/// one camera. So the bootstrap disables the stock pass and this system reads
/// the same <c>ParticleManager</c> pools and draws them at exactly the right
/// layer — including rotated rect fragments (a thick line segment IS a rotated
/// rectangle), which the circle-fallback stock path cannot do.
/// </para>
/// <para>
/// The renderer is resolved lazily through <c>World.TryGetExtension</c> (it only
/// exists once the window has loaded). In headless mode no renderer ever appears
/// and the system is a no-op.
/// </para>
/// </remarks>
/// <param name="fontPath">
/// Path to a TTF font for the score readout, or null to fall back to showing the
/// score in the window title.
/// </param>
public sealed class NovaFallRenderSystem(string? fontPath) : SystemBase
{
    private static readonly Vector4 furnaceBand = new(0.95f, 0.42f, 0.10f, 0.35f);
    private static readonly Vector4 furnaceEdge = new(1.00f, 0.55f, 0.15f, 1f);
    private static readonly Vector4 hudColor = new(0.92f, 0.94f, 1.00f, 1f);
    private static readonly Vector4 hudOutline = new(0f, 0f, 0.05f, 0.9f);

    private I2DRenderer? renderer;
    private ITextRenderer? textRenderer;
    private FontHandle font;
    private bool fontLoaded;
    private bool fontLoadAttempted;
    private int framesSinceTitleUpdate;
    private float pulseClock;
    private Vector2[] trailScratch = [];

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy init: the renderer only exists after the window has loaded, and it
        // never exists in headless simulation mode.
        if (renderer is null && !TryInitializeRenderers())
        {
            return;
        }

        pulseClock += deltaTime;

        var palette = World.GetSingleton<Palette>();
        var juiceOn = World.GetSingleton<JuiceConfig>().Enabled;
        var projection = ComposeCameraProjection(out var viewMin, out var viewSize);

        renderer!.Begin(in projection);
        try
        {
            DrawBackground(in palette, viewMin, viewSize);

            if (juiceOn)
            {
                DrawTrail(in palette);
                DrawGlow(in palette);
                DrawParticles(BlendMode.Alpha);
                DrawParticles(BlendMode.Additive);
                renderer.SetBlendMode(BlendMode.Alpha);
            }

            // Readability contract: floors and ball are drawn AFTER every
            // particle and glow, at full opacity, with a contrast outline.
            DrawFloors(in palette);
            DrawBall(in palette);
            DrawDeathFlash(viewMin, viewSize);
        }
        finally
        {
            renderer.End();
        }

        DrawHud();
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

    /// <summary>
    /// Builds the orthographic projection from <see cref="CameraState"/>: the
    /// design-space view rectangle, scaled by zoom and displaced by shake and
    /// kick, mapped Y-down exactly like the renderer's own screen projection.
    /// </summary>
    private Matrix4x4 ComposeCameraProjection(out Vector2 viewMin, out Vector2 viewSize)
    {
        var camera = World.GetSingleton<CameraState>();
        var zoom = camera.Zoom > 0.01f ? camera.Zoom : 1f;

        var width = Tuning.ShaftWidth / zoom;
        var height = Tuning.ShaftHeight / zoom;
        var centerX = Tuning.ShaftWidth / 2f + camera.ShakeOffset.X;
        var centerY = Tuning.ShaftHeight / 2f + camera.ShakeOffset.Y + camera.KickY;

        viewMin = new Vector2(centerX - width / 2f, centerY - height / 2f);
        viewSize = new Vector2(width, height);

        // Y-down ortho: top maps to the smaller Y, matching screen convention.
        return Matrix4x4.CreateOrthographicOffCenter(
            viewMin.X, viewMin.X + width,
            viewMin.Y + height, viewMin.Y,
            -1f, 1f);
    }

    private void DrawBackground(in Palette palette, Vector2 viewMin, Vector2 viewSize)
    {
        // Cover the whole (shaken, zoomed) view, with a margin so shake never
        // exposes the clear color at the edges.
        renderer!.FillRect(
            viewMin.X - 16f, viewMin.Y - 16f,
            viewSize.X + 32f, viewSize.Y + 32f,
            palette.Background);

        // Depth cue: warm near the Furnace above, darkening toward the depths.
        renderer.FillRect(0f, 0f, Tuning.ShaftWidth, Tuning.ShaftHeight * 0.30f,
            new Vector4(0.55f, 0.16f, 0.05f, 0.10f));
        renderer.FillRect(0f, Tuning.ShaftHeight * 0.62f, Tuning.ShaftWidth, Tuning.ShaftHeight * 0.38f + 32f,
            new Vector4(0.00f, 0.01f, 0.06f, 0.28f));

        // The Furnace ceiling: a glowing band with a hard edge. Touch it and die.
        renderer.FillRect(0f, viewMin.Y - 16f, Tuning.ShaftWidth, Tuning.CeilingY - viewMin.Y + 16f, furnaceBand);
        renderer.DrawLine(0f, Tuning.CeilingY, Tuning.ShaftWidth, Tuning.CeilingY, furnaceEdge, 3f);
    }

    /// <summary>
    /// The comet trail: three <c>DrawLineStrip</c> ribbons over the same ring
    /// buffer — long+thin+faint, half+medium, quarter+thick+bright. Additive
    /// blending stacks them into a core-hot, tail-faded ribbon whose length and
    /// width grow with heat tier.
    /// </summary>
    private void DrawTrail(in Palette palette)
    {
        var trail = World.GetSingleton<TrailState>();
        if (trail.Points is null || trail.Count < 2)
        {
            return;
        }

        var tier = World.GetSingleton<HeatState>().Tier;
        var points = Math.Min(trail.Count, Tuning.TrailBasePoints + tier * Tuning.TrailPointsPerTier);
        if (points < 2)
        {
            return;
        }

        if (trailScratch.Length < points)
        {
            trailScratch = new Vector2[Tuning.TrailCapacity];
        }

        // Unroll the ring buffer newest-first into the scratch span.
        for (var i = 0; i < points; i++)
        {
            var index = (trail.Head - i + trail.Points.Length * 2) % trail.Points.Length;
            trailScratch[i] = trail.Points[index];
        }

        var width = Tuning.TrailBaseWidth + tier * Tuning.TrailWidthPerTier;
        var color = palette.Trail;

        renderer!.SetBlendMode(BlendMode.Additive);
        renderer.DrawLineStrip(trailScratch.AsSpan(0, points), color with { W = 0.16f }, width * 1.6f);
        renderer.DrawLineStrip(trailScratch.AsSpan(0, Math.Max(2, points / 2)), color with { W = 0.30f }, width);
        renderer.DrawLineStrip(trailScratch.AsSpan(0, Math.Max(2, points / 4)), color with { W = 0.55f }, width * 0.6f);
    }

    /// <summary>
    /// Four concentric translucent circles under the ball, scale and alpha keyed
    /// to heat and sine-pulsed. Additive blending is what makes the layers read
    /// as one glow instead of four discs.
    /// </summary>
    private void DrawGlow(in Palette palette)
    {
        var heat = World.GetSingleton<HeatState>();
        var heatFraction = Math.Clamp(heat.Heat / Tuning.MaxHeat, 0f, 1f);
        var pulse = 1f + 0.08f * MathF.Sin(pulseClock * MathF.Tau * Tuning.GlowPulseHz);

        ReadOnlySpan<float> radiusScale = [1.6f, 2.6f, 3.8f, 5.2f];
        ReadOnlySpan<float> baseAlpha = [0.30f, 0.17f, 0.10f, 0.05f];

        renderer!.SetBlendMode(BlendMode.Additive);

        foreach (var entity in World.Query<Ball, Position2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            var scale = (0.55f + 0.9f * heatFraction) * pulse;
            var alphaScale = 0.35f + 0.65f * heatFraction;

            for (var layer = 0; layer < radiusScale.Length; layer++)
            {
                renderer.FillCircle(
                    position.X, position.Y,
                    ball.Radius * radiusScale[layer] * scale,
                    palette.Trail with { W = baseAlpha[layer] * alphaScale });
            }
        }
    }

    /// <summary>
    /// Draws every live particle pool for one blend mode: circles for sparks and
    /// embers, thick rotated line segments (= rotated rectangles) for floor
    /// fragments. Pool data comes straight from <c>ParticleManager</c>; the
    /// spawn/update systems still own the simulation of every particle.
    /// </summary>
    private void DrawParticles(BlendMode blendMode)
    {
        if (!World.TryGetExtension<ParticleManager>(out var particles) || particles is null)
        {
            return;
        }

        var blendSet = false;

        foreach (var entity in World.Query<ParticleEmitter>())
        {
            ref readonly var emitter = ref World.Get<ParticleEmitter>(entity);
            if (emitter.BlendMode != blendMode)
            {
                continue;
            }

            var pool = particles.GetPool(entity);
            if (pool is null || pool.ActiveCount == 0)
            {
                continue;
            }

            if (!blendSet)
            {
                renderer!.SetBlendMode(blendMode);
                blendSet = true;
            }

            var rectFragments = World.Has<RectFragments>(entity);

            for (var i = 0; i < pool.Capacity; i++)
            {
                if (!pool.Alive[i])
                {
                    continue;
                }

                var color = new Vector4(pool.ColorsR[i], pool.ColorsG[i], pool.ColorsB[i], pool.ColorsA[i]);
                var size = pool.Sizes[i];
                var x = pool.PositionsX[i];
                var y = pool.PositionsY[i];

                if (rectFragments)
                {
                    // A thick line segment IS a rotated rectangle: length from
                    // the particle size, thickness a slab-ish ratio, direction
                    // from the particle's spinning rotation.
                    var rotation = pool.Rotations[i];
                    var half = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * (size * 0.9f);
                    renderer!.DrawLine(
                        new Vector2(x, y) - half, new Vector2(x, y) + half,
                        color, MathF.Max(1.5f, size * 0.55f));
                }
                else
                {
                    renderer!.FillCircle(x, y, size / 2f, color, segments: 12);
                }
            }
        }
    }

    private void DrawFloors(in Palette palette)
    {
        const float cornerRadius = 6f;
        const float outlineThickness = 2f;

        foreach (var entity in World.Query<Floor, Position2D>())
        {
            ref readonly var floor = ref World.Get<Floor>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            var gapLeft = floor.GapCenterX - floor.GapWidth / 2f;
            var gapRight = floor.GapCenterX + floor.GapWidth / 2f;

            // Each floor is two slabs: wall → gap-left and gap-right → wall.
            DrawSlab(0f, position.Y, gapLeft, floor.Thickness, cornerRadius, outlineThickness, in palette);
            DrawSlab(gapRight, position.Y, Tuning.ShaftWidth - gapRight, floor.Thickness,
                cornerRadius, outlineThickness, in palette);
        }
    }

    private void DrawSlab(
        float x, float y, float width, float height,
        float cornerRadius, float outlineThickness, in Palette palette)
    {
        if (width < 1f)
        {
            return;
        }

        renderer!.FillRoundedRect(x, y, width, height, cornerRadius, palette.FloorFill);
        renderer.DrawRoundedRect(x, y, width, height, cornerRadius, palette.FloorOutline, outlineThickness);
    }

    private void DrawBall(in Palette palette)
    {
        foreach (var entity in World.Query<Ball, Position2D, Velocity2D>())
        {
            ref readonly var ball = ref World.Get<Ball>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);
            ref readonly var velocity = ref World.Get<Velocity2D>(entity);

            // Continuous teardrop stretch from velocity...
            var verticalStretch = MathF.Min(MathF.Abs(velocity.Y) / 2800f, 0.35f);
            var horizontalStretch = MathF.Min(MathF.Abs(velocity.X) / 2400f, 0.25f);
            var scaleX = 1f + horizontalStretch - verticalStretch;
            var scaleY = 1f + verticalStretch - horizontalStretch;

            // ...multiplied by the tween-driven impact recovery (pancake on
            // landing, elastic back to round — see SquashStretchSystem).
            if (World.Has<TweenVector2>(entity))
            {
                ref readonly var recovery = ref World.Get<TweenVector2>(entity);
                if (!recovery.IsComplete)
                {
                    scaleX *= recovery.CurrentValue.X;
                    scaleY *= recovery.CurrentValue.Y;
                }
            }

            var radiusX = ball.Radius * scaleX;
            var radiusY = ball.Radius * scaleY;

            renderer!.FillEllipse(position.X, position.Y, radiusX, radiusY, palette.Ball);

            // The SDF ellipse is axis-aligned, so the drift tilt is faked: the
            // rim highlight slides opposite the horizontal motion, which reads
            // as the ball leaning into its drift.
            var lean = Math.Clamp(velocity.X / Tuning.MaxHorizontalSpeed, -1f, 1f);
            renderer.DrawEllipse(
                position.X - lean * radiusX * 0.18f, position.Y - radiusY * 0.08f,
                radiusX * 0.92f, radiusY * 0.92f,
                new Vector4(1f, 1f, 1f, 0.35f), 2f);
        }
    }

    private void DrawDeathFlash(Vector2 viewMin, Vector2 viewSize)
    {
        var death = World.GetSingleton<DeathSequenceState>();
        if (death.FlashAlpha <= 0.001f)
        {
            return;
        }

        renderer!.FillRect(
            viewMin.X - 16f, viewMin.Y - 16f,
            viewSize.X + 32f, viewSize.Y + 32f,
            new Vector4(1f, 1f, 1f, death.FlashAlpha));
    }

    private void DrawHud()
    {
        var score = World.GetSingleton<ScoreState>();
        var heat = World.GetSingleton<HeatState>();
        var depth = World.GetSingleton<ScrollState>().Depth;
        var phase = World.GetSingleton<GameState>().Phase;

        var scaleX = 1f;
        var scaleY = 1f;
        if (World.TryGetExtension<IGraphicsContext>(out var graphics) && graphics.Width > 0 && graphics.Height > 0)
        {
            scaleX = graphics.Width / Tuning.ShaftWidth;
            scaleY = graphics.Height / Tuning.ShaftHeight;
        }

        EnsureFontLoaded();
        if (fontLoaded && textRenderer is not null)
        {
            textRenderer.Begin();
            try
            {
                // The big outlined score, top center — readable over anything.
                var scoreLine = string.Create(CultureInfo.InvariantCulture, $"{score.Score:F0}");
                textRenderer.DrawTextOutlined(
                    font, scoreLine.AsSpan(),
                    (Tuning.ShaftWidth / 2f) * scaleX, 24f * scaleY,
                    hudColor, hudOutline, 2f,
                    TextAlignH.Center, TextAlignV.Top);

                var depthLine = string.Create(CultureInfo.InvariantCulture, $"{depth:F0}m");
                textRenderer.DrawTextOutlined(
                    font, depthLine.AsSpan(),
                    (Tuning.ShaftWidth - 16f) * scaleX, (Tuning.CeilingY + 20f) * scaleY,
                    hudColor, hudOutline, 1.5f,
                    TextAlignH.Right, TextAlignV.Top);

                if (phase == GamePhase.Ready)
                {
                    textRenderer.DrawTextOutlined(
                        font, "Press A/D or LEFT/RIGHT to dive".AsSpan(),
                        (Tuning.ShaftWidth / 2f) * scaleX, (Tuning.ShaftHeight / 2f) * scaleY,
                        hudColor, hudOutline, 1.5f,
                        TextAlignH.Center, TextAlignV.Middle);
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
                var hudLine = string.Create(
                    CultureInfo.InvariantCulture,
                    $"SCORE {score.Score:F0}  x{HeatSystem.MultiplierForTier(heat.Tier)} {HeatSystem.NameForTier(heat.Tier)}  {depth:F0}m");
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
            font = fontManager.LoadFont(fontPath, 34f);
            fontLoaded = true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Console.WriteLine($"Failed to load font '{fontPath}': {ex.Message}");
        }
    }
}
