using System.Globalization;
using System.Numerics;
using KeenEyes.Animation;
using KeenEyes.Animation.Components;
using KeenEyes.Animation.Tweening;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Sample.NovaFall;

/// <summary>
/// Builds and updates the UI-kit HUD: the heat bar with its four tier notches,
/// the combo toast ladder (NICE → BLAZING → INCANDESCENT → SUPERNOVA), and the
/// death score card. The big outlined score readout is drawn by the render
/// system with <c>ITextRenderer.DrawTextOutlined</c>; everything else here is
/// <c>WidgetFactory</c> widgets whose components are mutated in place.
/// </summary>
/// <remarks>
/// <para>
/// The HUD is retained-mode: widgets are created once (lazily, when the UI
/// context and font are ready) and then driven each frame by writing their
/// <c>UIRect</c>/<c>UIText</c>/<c>UIStyle</c> components — the ECS way to do UI:
/// widgets are entities, updates are component writes.
/// </para>
/// <para>
/// All accent colors come from the <see cref="Palette"/> singleton, so the HUD
/// re-tints itself through tier changes along with the rest of the world.
/// </para>
/// </remarks>
/// <param name="fontPath">Path to a TTF font, or null to skip HUD creation.</param>
public sealed class HudSystem(string? fontPath) : SystemBase
{
    private const float BarX = 16f;
    private const float BarY = 100f;
    private const float BarWidth = 260f;
    private const float BarHeight = 16f;

    private bool built;
    private bool buildFailed;
    private FontHandle font;

    private Entity heatBarFill;
    private Entity tierLabel;
    private Entity savePipLabel;
    private Entity toastLabel;
    private Entity deathCard;
    private Entity deathScoreLabel;
    private Entity deathBestLabel;
    private Entity deathDepthLabel;

    private float toastSecondsRemaining;
    private int lastToastCombo;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        var juice = World.GetSingleton<JuiceConfig>();
        if (!juice.PresentationAvailable
            || !World.TryGetExtension<UIContext>(out var ui) || ui is null)
        {
            return;
        }

        if (!built && !TryBuild(ui))
        {
            return;
        }

        var palette = World.GetSingleton<Palette>();
        UpdateHeatBar(in palette);
        UpdateToast(deltaTime, in palette, juice.Enabled);
        UpdateDeathCard();
    }

    private bool TryBuild(UIContext ui)
    {
        if (buildFailed)
        {
            return false;
        }

        if (fontPath is null
            || !World.TryGetExtension<IFontManagerProvider>(out var fontManagerProvider)
            || fontManagerProvider?.GetFontManager() is not { } fontManager)
        {
            return false;
        }

        try
        {
            font = fontManager.LoadFont(fontPath, 20f);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            Console.WriteLine($"HUD disabled - failed to load font '{fontPath}': {ex.Message}");
            buildFailed = true;
            return false;
        }

        var canvas = ui.CreateCanvas("NovaFallHud");

        BuildHeatBar(canvas);
        BuildToast(canvas);
        BuildDeathCard(canvas);

        built = true;
        return true;
    }

    #region Heat bar

    private void BuildHeatBar(Entity canvas)
    {
        // Background trough.
        var barBg = WidgetFactory.CreatePanel(World, canvas, "HeatBar.Bg", new PanelConfig(
            Width: BarWidth, Height: BarHeight,
            BackgroundColor: new Vector4(0f, 0f, 0f, 0.55f),
            CornerRadius: 4));
        PlaceFixed(barBg, BarX, BarY, BarWidth, BarHeight, zIndex: 0);

        // Fill, resized every frame from the heat fraction.
        heatBarFill = WidgetFactory.CreatePanel(World, canvas, "HeatBar.Fill", new PanelConfig(
            Width: 1, Height: BarHeight - 4,
            BackgroundColor: Vector4.One,
            CornerRadius: 3));
        PlaceFixed(heatBarFill, BarX + 2, BarY + 2, 1, BarHeight - 4, zIndex: 1);

        // Four tier notches: where Ember, Flame, Plasma, and Nova begin.
        for (var tier = 0; tier < 4; tier++)
        {
            var fraction = HeatSystem.ThresholdForTier(tier) / Tuning.MaxHeat;
            var notch = WidgetFactory.CreatePanel(World, canvas, $"HeatBar.Notch{tier}", new PanelConfig(
                Width: 2, Height: BarHeight + 6,
                BackgroundColor: new Vector4(1f, 1f, 1f, 0.65f)));
            PlaceFixed(notch, BarX + fraction * BarWidth - 1, BarY - 3, 2, BarHeight + 6, zIndex: 2);
        }

        tierLabel = WidgetFactory.CreateLabel(World, canvas, "HeatBar.Tier", "EMBER x1", font, new LabelConfig(
            Width: BarWidth, Height: 20, FontSize: 15));
        PlaceFixed(tierLabel, BarX, BarY + BarHeight + 6, BarWidth, 20, zIndex: 0);

        // The small SAVE pip: visible while the run's one Adrenaline Save is
        // still unspent, gone the moment it fires.
        savePipLabel = WidgetFactory.CreateLabel(World, canvas, "HeatBar.SavePip", "SAVE", font, new LabelConfig(
            Width: 60, Height: 20, FontSize: 13));
        PlaceFixed(savePipLabel, BarX + BarWidth + 14, BarY - 1, 60, 20, zIndex: 0);
    }

    private void UpdateHeatBar(in Palette palette)
    {
        var heat = World.GetSingleton<HeatState>();
        var fraction = Math.Clamp(heat.Heat / Tuning.MaxHeat, 0f, 1f);

        ref var fillRect = ref World.Get<UIRect>(heatBarFill);
        fillRect.Size = new Vector2(Math.Max(1f, fraction * (BarWidth - 4)), BarHeight - 4);

        ref var fillStyle = ref World.Get<UIStyle>(heatBarFill);
        fillStyle.BackgroundColor = palette.UiAccent;

        ref var text = ref World.Get<UIText>(tierLabel);
        text.Content = string.Create(
            CultureInfo.InvariantCulture,
            $"{HeatSystem.NameForTier(heat.Tier)} x{HeatSystem.MultiplierForTier(heat.Tier)}");
        text.Color = palette.UiAccent;

        var settings = World.GetSingleton<RunConfig>().Settings;
        var showPip = World.GetSingleton<GameState>().Phase == GamePhase.Playing
            && settings.AdrenalineEnabled
            && World.GetSingleton<AdrenalineState>().Available;
        World.Get<UIElement>(savePipLabel).Visible = showPip;
        if (showPip)
        {
            World.Get<UIText>(savePipLabel).Color = palette.UiAccent with { W = 0.85f };
        }
    }

    #endregion

    #region Combo toasts

    private void BuildToast(Entity canvas)
    {
        toastLabel = WidgetFactory.CreateLabel(World, canvas, "ComboToast", string.Empty, font, new LabelConfig(
            Width: 600, Height: 64, FontSize: 30, HorizontalAlign: TextAlignH.Center));
        PlaceFixed(toastLabel, Tuning.ShaftWidth / 2f - 300f, 300f, 600, 64, zIndex: 5);
        World.Get<UIElement>(toastLabel).Visible = false;
    }

    private void UpdateToast(float deltaTime, in Palette palette, bool juiceEnabled)
    {
        var combo = World.GetSingleton<ComboState>().Combo;

        // Fire when the combo lands exactly on a ladder rung.
        if (juiceEnabled && combo != lastToastCombo)
        {
            var rung = Array.IndexOf(Tuning.ComboToastThresholds, combo);
            if (rung >= 0)
            {
                ShowToast(Tuning.ComboToastTexts[rung], in palette);
            }

            lastToastCombo = combo;
        }

        // Flashover moments outrank the combo ladder for the toast slot.
        if (juiceEnabled)
        {
            ref readonly var events = ref World.GetSingleton<FrameEvents>();
            if (events.SurgeStarted)
            {
                ShowToast("FLASHOVER", in palette);
            }

            if (events.SurgeSweepAwarded)
            {
                ShowToast("+1000 SURGE SWEEP", in palette);
            }
        }

        if (toastSecondsRemaining <= 0f)
        {
            return;
        }

        toastSecondsRemaining -= deltaTime;

        ref var text = ref World.Get<UIText>(toastLabel);

        // Text punch: the font size rides a back-ease tween while it is alive.
        if (World.Has<TweenFloat>(toastLabel))
        {
            text.FontSize = World.Get<TweenFloat>(toastLabel).CurrentValue;
        }

        // Fade out over the last third of the toast's life.
        var fade = Math.Clamp(toastSecondsRemaining / (Tuning.ToastSeconds / 3f), 0f, 1f);
        text.Color = text.Color with { W = fade };

        if (toastSecondsRemaining <= 0f)
        {
            World.Get<UIElement>(toastLabel).Visible = false;
        }
    }

    private void ShowToast(string message, in Palette palette)
    {
        ref var text = ref World.Get<UIText>(toastLabel);
        text.Content = message;
        text.Color = palette.UiAccent;
        World.Get<UIElement>(toastLabel).Visible = true;
        toastSecondsRemaining = Tuning.ToastSeconds;

        // The punch: overshoot from small to full size with a back ease. Needs
        // the animation plugin's TweenSystem; without it the toast still shows
        // at full size.
        if (World.TryGetExtension<AnimationManager>(out _))
        {
            var punch = TweenFloat.Create(12f, 34f, duration: 0.35f, EaseType.BackOut);
            if (World.Has<TweenFloat>(toastLabel))
            {
                World.Get<TweenFloat>(toastLabel) = punch;
            }
            else
            {
                World.Add(toastLabel, punch);
            }
        }
    }

    #endregion

    #region Death score card

    private void BuildDeathCard(Entity canvas)
    {
        deathCard = WidgetFactory.CreatePanel(World, canvas, "DeathCard", new PanelConfig(
            Width: 420, Height: 250,
            Direction: LayoutDirection.Vertical,
            MainAxisAlign: LayoutAlign.Center,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 10,
            Padding: UIEdges.All(18),
            BackgroundColor: new Vector4(0.02f, 0.03f, 0.08f, 0.92f),
            CornerRadius: 10));

        ref var rect = ref World.Get<UIRect>(deathCard);
        rect.AnchorMin = new Vector2(0.5f, 0.5f);
        rect.AnchorMax = new Vector2(0.5f, 0.5f);
        rect.Pivot = new Vector2(0.5f, 0.5f);
        rect.LocalZIndex = 10;

        WidgetFactory.CreateLabel(World, deathCard, "DeathCard.Title", "THE FURNACE CLAIMS YOU", font,
            new LabelConfig(Width: 380, Height: 34, FontSize: 22, HorizontalAlign: TextAlignH.Center));
        deathScoreLabel = WidgetFactory.CreateLabel(World, deathCard, "DeathCard.Score", string.Empty, font,
            new LabelConfig(Width: 380, Height: 30, FontSize: 20, HorizontalAlign: TextAlignH.Center));
        deathBestLabel = WidgetFactory.CreateLabel(World, deathCard, "DeathCard.Best", string.Empty, font,
            new LabelConfig(Width: 380, Height: 26, FontSize: 16, HorizontalAlign: TextAlignH.Center));
        deathDepthLabel = WidgetFactory.CreateLabel(World, deathCard, "DeathCard.Depth", string.Empty, font,
            new LabelConfig(Width: 380, Height: 26, FontSize: 16, HorizontalAlign: TextAlignH.Center));
        WidgetFactory.CreateLabel(World, deathCard, "DeathCard.Prompt", "press A / D for the menu", font,
            new LabelConfig(Width: 380, Height: 26, FontSize: 14, HorizontalAlign: TextAlignH.Center,
                TextColor: new Vector4(0.7f, 0.75f, 0.85f, 1f)));

        World.Get<UIElement>(deathCard).Visible = false;
    }

    private void UpdateDeathCard()
    {
        var death = World.GetSingleton<DeathSequenceState>();
        var visible = World.GetSingleton<GameState>().Phase == GamePhase.Dead && death.ScoreCardVisible;

        ref var element = ref World.Get<UIElement>(deathCard);
        if (element.Visible == visible)
        {
            return;
        }

        element.Visible = visible;
        if (!visible)
        {
            return;
        }

        var score = World.GetSingleton<ScoreState>();
        var depth = World.GetSingleton<ScrollState>().Depth;

        World.Get<UIText>(deathScoreLabel).Content = string.Create(
            CultureInfo.InvariantCulture, $"SCORE  {score.Score:F0}");
        World.Get<UIText>(deathBestLabel).Content = string.Create(
            CultureInfo.InvariantCulture, $"BEST  {score.Best}");
        World.Get<UIText>(deathDepthLabel).Content = string.Create(
            CultureInfo.InvariantCulture, $"DEPTH  {depth:F0}m");
    }

    #endregion

    /// <summary>
    /// Positions a widget at fixed pixel coordinates relative to the top-left
    /// of the screen (the canvas stretches to the full window).
    /// </summary>
    private void PlaceFixed(Entity entity, float x, float y, float width, float height, short zIndex)
    {
        ref var rect = ref World.Get<UIRect>(entity);
        rect.AnchorMin = Vector2.Zero;
        rect.AnchorMax = Vector2.Zero;
        rect.Pivot = Vector2.Zero;
        rect.Offset = new UIEdges(x, y, 0, 0);
        rect.Size = new Vector2(width, height);
        rect.WidthMode = UISizeMode.Fixed;
        rect.HeightMode = UISizeMode.Fixed;
        rect.LocalZIndex = zIndex;
    }
}
