using Godot;
using Knight.Settings;

namespace Knight.Ui;

public readonly record struct PrologueEnergyHeatFrame(
    float EnergyRatio,
    float HeatRatio,
    bool IsOverheated)
{
    public static PrologueEnergyHeatFrame Create(float energyRatio, float heatRatio, bool overheated) =>
        new(Mathf.Clamp(energyRatio, 0f, 1f), Mathf.Clamp(heatRatio, 0f, 1f), overheated);
}

public partial class PrologueEnergyHeatVisual : Control
{
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private StyleBoxFlat _trackStyle;
    private StyleBoxFlat _energyStyle;
    private StyleBoxFlat _heatStyle;
    private StyleBoxFlat _overheatedStyle;
    private ProgressBar _styledEnergy;
    private ProgressBar _styledHeat;
    private bool _heatWasOverheated;
    public PrologueEnergyHeatFrame CurrentFrame { get; private set; } =
        PrologueEnergyHeatFrame.Create(1f, 0f, false);

    public override void _Ready() => Present(CurrentFrame);

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        _trackStyle?.Dispose();
        _energyStyle?.Dispose();
        _heatStyle?.Dispose();
        _overheatedStyle?.Dispose();
    }

    public void Present(PrologueEnergyHeatFrame frame)
    {
        frame = PrologueEnergyHeatFrame.Create(frame.EnergyRatio, frame.HeatRatio, frame.IsOverheated);
        CurrentFrame = frame;
        ProgressBar energy = GetNodeOrNull<ProgressBar>("Energy");
        ProgressBar heat = GetNodeOrNull<ProgressBar>("Heat");
        ApplyMeterStyles(energy, heat, frame.IsOverheated);
        if (energy != null) energy.Value = frame.EnergyRatio;
        if (heat != null) heat.Value = frame.HeatRatio;
        if (GetNodeOrNull<Control>("Heat/OverheatMarker") is { } marker) marker.Visible = frame.IsOverheated;
        QueueRedraw();
    }

    private void ApplyMeterStyles(ProgressBar energy, ProgressBar heat, bool overheated)
    {
        if (energy == null && heat == null) return;
        if (_trackStyle == null)
        {
            // Instance-owned resources are created once, not for each HUD frame or heat edge.
            _trackStyle = MeterStyle(new Color("081118"));
            _energyStyle = MeterStyle(PrologueUiPalette.Thread);
            _heatStyle = MeterStyle(PrologueUiPalette.Brass);
            _overheatedStyle = MeterStyle(new Color("ef7b68"));
        }
        if (energy != null && energy != _styledEnergy)
        {
            energy.AddThemeStyleboxOverride("background", _trackStyle);
            energy.AddThemeStyleboxOverride("fill", _energyStyle);
            _styledEnergy = energy;
        }
        if (heat != null && (heat != _styledHeat || overheated != _heatWasOverheated))
        {
            if (heat != _styledHeat) heat.AddThemeStyleboxOverride("background", _trackStyle);
            heat.AddThemeStyleboxOverride("fill", overheated ? _overheatedStyle : _heatStyle);
            _styledHeat = heat;
            _heatWasOverheated = overheated;
        }
    }

    private static StyleBoxFlat MeterStyle(Color fill) => new()
    {
        BgColor = fill,
        BorderWidthLeft = 0, BorderWidthTop = 0, BorderWidthRight = 0, BorderWidthBottom = 0,
        CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
        CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2,
        ContentMarginLeft = 0, ContentMarginTop = 0, ContentMarginRight = 0, ContentMarginBottom = 0
    };

    public void SetLanguage(GameLanguage language)
    {
        _language = language;
        if (GetNodeOrNull<Label>("EnergyLabel") is { } energy)
            energy.Text = language == GameLanguage.English ? "ENERGY" : "能量";
        if (GetNodeOrNull<Label>("HeatLabel") is { } heat)
            heat.Text = language == GameLanguage.English ? "HEAT" : "热量";
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0f,
            new Vector2(Size.X / 194f, Size.Y / 72f));
        if (CurrentFrame.IsOverheated)
        {
            DrawLine(new Vector2(54, 58), new Vector2(186, 58), new Color("ef7b68"), 2f);
            DrawString(ThemeDB.FallbackFont, new Vector2(118, 33),
                _language == GameLanguage.English ? "VENT" : "散热", HorizontalAlignment.Left,
                60, 10, new Color("ef7b68"));
        }
    }
}
