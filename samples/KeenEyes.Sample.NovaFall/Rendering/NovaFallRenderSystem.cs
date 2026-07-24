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
    private FontHandle menuFont;
    private bool fontLoaded;
    private bool fontLoadAttempted;
    private int framesSinceTitleUpdate;
    private float pulseClock;
    private Vector2[] trailScratch = [];
    private readonly Vector2[] crackScratch = new Vector2[CrackPoints];

    private const int CrackPoints = 7;

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
            DrawAdrenalineVignette(viewMin, viewSize);
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
        if (World.GetSingleton<AdrenalineState>().Active)
        {
            // Slow motion thickens the comet: the one steer that matters
            // deserves the heaviest ink in the game.
            width *= 1.6f;
        }

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

    /// <summary>
    /// Draws every floor with its personality on its face: Brittle floors are
    /// ash-gray with hairline fractures (and a growing white crack once landed
    /// on), Bumpers carry an accent-bright coil line and wobble after a launch,
    /// and Pulse floors physically breathe — their slabs are drawn at the same
    /// EFFECTIVE gap the collision system uses, so the shrinking-edges close
    /// telegraph is the hitbox, not a decoration over it.
    /// </summary>
    private void DrawFloors(in Palette palette)
    {
        const float cornerRadius = 6f;
        const float outlineThickness = 2f;
        var musicSeconds = World.GetSingleton<MusicClock>().Seconds;

        foreach (var entity in World.Query<Floor, Position2D>())
        {
            ref readonly var floor = ref World.Get<Floor>(entity);
            ref readonly var position = ref World.Get<Position2D>(entity);

            var gapWidth = FloorLayout.EffectiveGapWidth(in floor, musicSeconds);
            var gapLeft = floor.GapCenterX - gapWidth / 2f;
            var gapRight = floor.GapCenterX + gapWidth / 2f;

            var fill = palette.FloorFill;
            var outline = palette.FloorOutline;
            var y = position.Y;
            var thickness = floor.Thickness;

            switch (floor.Kind)
            {
                case FloorKind.Brittle:
                    // Ash-gray fill: visibly not load-bearing.
                    fill = Vector4.Lerp(fill, new Vector4(0.42f, 0.42f, 0.46f, 1f), 0.45f);
                    break;

                case FloorKind.Bumper:
                    // Accent outline: reads as "springy", not "solid".
                    outline = palette.UiAccent;
                    if (floor.WobbleSeconds > 0f)
                    {
                        // Bounce-ease wobble: a decaying sine on the slab height.
                        var energy = floor.WobbleSeconds / Tuning.BumperWobbleSeconds;
                        var wobble = MathF.Sin((1f - energy) * MathF.PI * 3f) * energy;
                        var stretch = 1f + 0.30f * wobble;
                        thickness = floor.Thickness * stretch;
                        y -= thickness - floor.Thickness;
                    }

                    break;

                case FloorKind.Pulse:
                case FloorKind.Standard:
                default:
                    break;
            }

            // Each floor is two slabs: wall → gap-left and gap-right → wall.
            // For a fully closed Pulse gap the two slabs meet in the middle.
            DrawSlab(0f, y, gapLeft, thickness, cornerRadius, outlineThickness, fill, outline);
            DrawSlab(gapRight, y, Tuning.ShaftWidth - gapRight, thickness,
                cornerRadius, outlineThickness, fill, outline);

            if (floor.Kind == FloorKind.Brittle)
            {
                DrawBrittleCracks(in floor, y);
            }
            else if (floor.Kind == FloorKind.Pulse)
            {
                DrawPulseEdges(in floor, y, gapLeft, gapRight, musicSeconds, in palette);
            }
        }
    }

    private void DrawSlab(
        float x, float y, float width, float height,
        float cornerRadius, float outlineThickness, Vector4 fill, Vector4 outline)
    {
        if (width < 1f)
        {
            return;
        }

        renderer!.FillRoundedRect(x, y, width, height, cornerRadius, fill);
        renderer.DrawRoundedRect(x, y, width, height, cornerRadius, outline, outlineThickness);
    }

    /// <summary>
    /// Brittle floor fractures: faint hairlines always (the "don't trust me"
    /// tell), plus a jagged line-strip crack that grows across the slab over
    /// the crumble delay once the floor has been landed on — the visible half
    /// of the telegraph contract.
    /// </summary>
    private void DrawBrittleCracks(in Floor floor, float y)
    {
        // Hairlines: two short deterministic fissures per floor, derived from
        // the floor index so they never flicker frame to frame.
        var rng = SeededGenerator.ForFloor((ulong)floor.Index, floor.Index);
        var hairColor = new Vector4(0.15f, 0.15f, 0.18f, 0.8f);
        for (var i = 0; i < 2; i++)
        {
            var x = rng.NextRange(30f, Tuning.ShaftWidth - 30f);
            var drift = rng.NextRange(-10f, 10f);
            renderer!.DrawLine(x, y + 3f, x + drift, y + floor.Thickness - 3f, hairColor, 1.5f);
        }

        if (!floor.Cracking)
        {
            return;
        }

        // The growing crack: a jagged strip spreading from the gap's left edge
        // across the left slab, whitening as the crumble approaches.
        var progress = Math.Clamp(floor.CrackSeconds / Tuning.BrittleCrumbleDelaySeconds, 0f, 1f);
        var gapLeft = floor.GapCenterX - floor.GapWidth / 2f;
        var reach = Math.Max(gapLeft, Tuning.ShaftWidth - (floor.GapCenterX + floor.GapWidth / 2f));
        var length = reach * progress;

        var crackRng = SeededGenerator.ForFloor((ulong)floor.Index * 31UL, floor.Index);
        for (var i = 0; i < CrackPoints; i++)
        {
            var t = i / (float)(CrackPoints - 1);
            crackScratch[i] = new Vector2(
                gapLeft - length * t,
                y + floor.Thickness * (0.5f + (crackRng.NextFloat() - 0.5f) * 0.7f));
        }

        var heat = 0.4f + 0.6f * progress;
        renderer!.DrawLineStrip(
            crackScratch.AsSpan(0, CrackPoints),
            new Vector4(1f, 0.95f + 0.05f * heat, 0.9f, heat),
            1.5f + 2f * progress);
    }

    /// <summary>
    /// Pulse floor gap edges: bright ticks marking the moving edge of the
    /// breathing gap. While the gap shrinks toward a close the ticks brighten —
    /// the shrinking edges themselves ARE the &gt;= 0.6 s telegraph, because the
    /// drawn slabs already track the effective gap.
    /// </summary>
    private void DrawPulseEdges(
        in Floor floor, float y, float gapLeft, float gapRight, float musicSeconds, in Palette palette)
    {
        var openness = FloorLayout.PulseOpenness(musicSeconds);
        var closing = openness < 1f;
        var alpha = closing ? 1f : 0.6f;
        var color = palette.UiAccent with { W = alpha };

        if (openness > 0.01f)
        {
            renderer!.DrawLine(gapLeft, y - 3f, gapLeft, y + floor.Thickness + 3f, color, 3f);
            renderer.DrawLine(gapRight, y - 3f, gapRight, y + floor.Thickness + 3f, color, 3f);
        }
        else
        {
            // Fully closed: a seam glows where the gap will reopen.
            renderer!.DrawLine(
                floor.GapCenterX - 8f, y + floor.Thickness / 2f,
                floor.GapCenterX + 8f, y + floor.Thickness / 2f,
                color, 2f);
        }
    }

    /// <summary>
    /// Adrenaline Save vignette: translucent edge bands darken the frame while
    /// the world runs at 20% — tunnel vision for the one steer that matters.
    /// </summary>
    private void DrawAdrenalineVignette(Vector2 viewMin, Vector2 viewSize)
    {
        if (!World.GetSingleton<AdrenalineState>().Active)
        {
            return;
        }

        var shade = new Vector4(0f, 0f, 0.02f, 0.45f);
        var bandX = viewSize.X * 0.16f;
        var bandY = viewSize.Y * 0.12f;

        renderer!.FillRect(viewMin.X, viewMin.Y, viewSize.X, bandY, shade);
        renderer.FillRect(viewMin.X, viewMin.Y + viewSize.Y - bandY, viewSize.X, bandY, shade);
        renderer.FillRect(viewMin.X, viewMin.Y + bandY, bandX, viewSize.Y - 2f * bandY, shade);
        renderer.FillRect(viewMin.X + viewSize.X - bandX, viewMin.Y + bandY, bandX, viewSize.Y - 2f * bandY, shade);
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
                    DrawReadyMenu(scaleX, scaleY);
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

    /// <summary>
    /// The Ready-screen menu: mode row (name, description, and — for Daily
    /// Inferno — today's medal and remaining attempts), cosmetic style row, and
    /// the control hints. The active row wears the angle brackets, keeping the
    /// one-axis input grammar legible even in a menu.
    /// </summary>
    private void DrawReadyMenu(float scaleX, float scaleY)
    {
        var menu = World.GetSingleton<MenuState>();
        var profileState = World.GetSingleton<ProfileState>();
        var mode = menu.SelectedMode;
        var centerX = (Tuning.ShaftWidth / 2f) * scaleX;
        var lineY = Tuning.ShaftHeight * 0.40f;
        const float lineSpacing = 34f;

        var modeLine = menu.Row == MenuRow.Mode
            ? $"< {ModeCatalog.NameOf(mode)} >"
            : ModeCatalog.NameOf(mode);
        textRenderer!.DrawTextOutlined(
            font, modeLine.AsSpan(), centerX, lineY * scaleY,
            hudColor, hudOutline, 2f, TextAlignH.Center, TextAlignV.Middle);
        lineY += lineSpacing * 1.4f;

        textRenderer.DrawTextOutlined(
            menuFont, ModeCatalog.DescriptionOf(mode).AsSpan(), centerX, lineY * scaleY,
            hudColor with { W = 0.85f }, hudOutline, 1.5f, TextAlignH.Center, TextAlignV.Middle);
        lineY += lineSpacing;

        if (profileState.Profile is { } profile)
        {
            var best = profile.ModeBests[(int)mode];
            var infoLine = string.Create(
                CultureInfo.InvariantCulture, $"best {best.BestScore:F0} pts - {best.BestDepth:F0}m");

            if (mode == GameMode.DailyInferno)
            {
                var index = profile.DailyRecordIndexFor(profileState.TodayKey);
                var record = profile.DailyHistory[index];
                var attemptsLeft = Math.Max(Tuning.DailyAttemptsPerDay - record.AttemptsUsed, 0);
                infoLine = string.Create(
                    CultureInfo.InvariantCulture,
                    $"today: {DailySchedule.MedalName(record.Medal)} - {attemptsLeft} attempts left");
            }

            textRenderer.DrawTextOutlined(
                menuFont, infoLine.AsSpan(), centerX, lineY * scaleY,
                hudColor with { W = 0.85f }, hudOutline, 1.5f, TextAlignH.Center, TextAlignV.Middle);
            lineY += lineSpacing;

            var style = CosmeticStyles.All[Math.Clamp(profile.SelectedStyle, 0, CosmeticStyles.All.Length - 1)];
            var styleLine = menu.Row == MenuRow.Cosmetics
                ? $"STYLE  < {style.Name} >"
                : $"STYLE  {style.Name}";
            textRenderer.DrawTextOutlined(
                menuFont, styleLine.AsSpan(), centerX, lineY * scaleY,
                hudColor with { W = 0.85f }, hudOutline, 1.5f, TextAlignH.Center, TextAlignV.Middle);
            lineY += lineSpacing;
        }

        textRenderer.DrawTextOutlined(
            menuFont, "A/D cycle - TAB row - SPACE dive".AsSpan(), centerX, (lineY + 10f) * scaleY,
            hudColor with { W = 0.6f }, hudOutline, 1.5f, TextAlignH.Center, TextAlignV.Middle);
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
            menuFont = fontManager.LoadFont(fontPath, 20f);
            fontLoaded = true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Console.WriteLine($"Failed to load font '{fontPath}': {ex.Message}");
        }
    }
}
