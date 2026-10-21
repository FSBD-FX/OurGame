using Godot;
using Knight.Ui;

namespace Knight.Tests.Unit;

public static class PrologueEnergyHeatStyleTests
{
    public static void Run()
    {
        ResourceBarsHaveDistinctReadableStyles();
        OverheatSwitchesAndRestoresCachedHeatStyle();
        PresentationPreservesValuesAndLayout();
        CachedStylesAreReleasedWithTheVisual();
    }

    private static void ResourceBarsHaveDistinctReadableStyles()
    {
        PrologueEnergyHeatVisual visual = CreateVisual();
        try
        {
            visual.Present(PrologueEnergyHeatFrame.Create(.6f, .4f, false));
            ProgressBar energy = visual.GetNode<ProgressBar>("Energy");
            ProgressBar heat = visual.GetNode<ProgressBar>("Heat");
            StyleBoxFlat energyFill = Style(energy, "fill");
            StyleBoxFlat heatFill = Style(heat, "fill");
            TestAssert.True(energyFill.BgColor.G > energyFill.BgColor.R &&
                            energyFill.BgColor.B > energyFill.BgColor.R,
                "energy fill is visibly teal rather than the default gray bar");
            TestAssert.True(heatFill.BgColor.R > heatFill.BgColor.G &&
                            heatFill.BgColor.G > heatFill.BgColor.B,
                "ordinary heat has an amber fill distinct from energy");
            foreach (ProgressBar bar in new[] { energy, heat })
            {
                StyleBoxFlat track = Style(bar, "background");
                TestAssert.True(track.BgColor.R < .12f && track.BgColor.G < .12f &&
                                track.BgColor.B < .12f && track.BgColor.A > .95f,
                    "both meters retain a dark opaque track at low resource values");
                TestAssert.True(track.BorderWidthLeft == 1 && track.BorderWidthTop == 1 &&
                                track.BorderWidthRight == 1 && track.BorderWidthBottom == 1 &&
                                track.BorderColor.G > track.BgColor.G + .15f,
                    "a visible one-pixel edge separates each dark track from the scene");
            }
        }
        finally { visual.Free(); }
    }

    private static void OverheatSwitchesAndRestoresCachedHeatStyle()
    {
        PrologueEnergyHeatVisual visual = CreateVisual();
        try
        {
            visual.Present(PrologueEnergyHeatFrame.Create(.7f, .3f, false));
            ProgressBar energy = visual.GetNode<ProgressBar>("Energy");
            ProgressBar heat = visual.GetNode<ProgressBar>("Heat");
            ulong energyStyleId = Style(energy, "fill").GetInstanceId();
            ulong normalHeatStyleId = Style(heat, "fill").GetInstanceId();
            ulong trackStyleId = Style(heat, "background").GetInstanceId();

            visual.Present(PrologueEnergyHeatFrame.Create(.7f, 1f, true));
            StyleBoxFlat overheated = Style(heat, "fill");
            ulong overheatedStyleId = overheated.GetInstanceId();
            TestAssert.True(overheatedStyleId != normalHeatStyleId &&
                            overheated.BgColor.R > overheated.BgColor.G * 1.5f &&
                            overheated.BgColor.G > overheated.BgColor.B,
                "overheated heat visibly changes from amber to orange-red");
            TestAssert.True(visual.GetNode<Control>("Heat/OverheatMarker").Visible,
                "the existing non-color overheat marker remains visible");

            for (int index = 0; index < 120; index++)
            {
                bool hot = index % 2 == 0;
                visual.Present(PrologueEnergyHeatFrame.Create(.7f, .3f, hot));
                TestAssert.Equal(hot ? overheatedStyleId : normalHeatStyleId,
                    Style(heat, "fill").GetInstanceId(),
                    "heat reuses its cached state styles and restores amber after cooling");
                TestAssert.Equal(energyStyleId, Style(energy, "fill").GetInstanceId(),
                    "heat transitions do not replace or recolor the energy style");
                TestAssert.Equal(trackStyleId, Style(heat, "background").GetInstanceId(),
                    "repeated presentation does not allocate replacement track styles");
            }
            TestAssert.True(!visual.GetNode<Control>("Heat/OverheatMarker").Visible,
                "cooling also restores the existing non-color overheat marker state");
        }
        finally { visual.Free(); }
    }

    private static void PresentationPreservesValuesAndLayout()
    {
        PrologueEnergyHeatVisual visual = CreateVisual();
        try
        {
            ProgressBar energy = visual.GetNode<ProgressBar>("Energy");
            ProgressBar heat = visual.GetNode<ProgressBar>("Heat");
            // Godot can clamp Size to the default theme's minimum during initialization.
            // This contract preserves the actual layout, not the requested setter values.
            Vector2 energyPosition = energy.Position;
            Vector2 heatPosition = heat.Position;
            Vector2 energySize = energy.Size;
            Vector2 heatSize = heat.Size;
            visual.Present(PrologueEnergyHeatFrame.Create(.42f, .86f, false));
            TestAssert.Near(.42f, (float)energy.Value, .0001f,
                "meter styling preserves the authoritative energy value");
            TestAssert.Near(.86f, (float)heat.Value, .0001f,
                "meter styling preserves the authoritative heat value");
            TestAssert.Equal(energyPosition, energy.Position,
                "energy styling does not move the existing meter");
            TestAssert.Equal(heatPosition, heat.Position,
                "heat styling does not move the existing meter");
            TestAssert.Equal(energySize, energy.Size,
                "energy styling retains the existing compact meter dimensions");
            TestAssert.Equal(heatSize, heat.Size,
                "heat styling retains the existing compact meter dimensions");
        }
        finally { visual.Free(); }
    }

    private static StyleBoxFlat Style(ProgressBar bar, string name)
    {
        TestAssert.True(bar.HasThemeStyleboxOverride(name),
            $"{bar.Name} owns its readable {name} theme instead of the default style");
        StyleBoxFlat style = bar.GetThemeStylebox(name) as StyleBoxFlat;
        TestAssert.True(style != null, $"{bar.Name} uses a flat readable {name} style");
        return style;
    }

    private static void CachedStylesAreReleasedWithTheVisual()
    {
        PrologueEnergyHeatVisual visual = CreateVisual();
        StyleBoxFlat energy = null;
        StyleBoxFlat heat = null;
        StyleBoxFlat overheated = null;
        StyleBoxFlat track = null;
        try
        {
            visual.Present(PrologueEnergyHeatFrame.Create(.6f, .4f, false));
            energy = Style(visual.GetNode<ProgressBar>("Energy"), "fill");
            heat = Style(visual.GetNode<ProgressBar>("Heat"), "fill");
            track = Style(visual.GetNode<ProgressBar>("Heat"), "background");
            visual.Present(PrologueEnergyHeatFrame.Create(.6f, 1f, true));
            overheated = Style(visual.GetNode<ProgressBar>("Heat"), "fill");
        }
        finally { visual.Free(); }
        foreach (StyleBoxFlat style in new[] { energy, heat, overheated, track })
            TestAssert.True(!GodotObject.IsInstanceValid(style),
                "node deletion releases its cached style wrappers without a global resource cache");
    }

    private static PrologueEnergyHeatVisual CreateVisual()
    {
        var visual = new PrologueEnergyHeatVisual { Size = new Vector2(194, 72) };
        visual.AddChild(new ProgressBar
        {
            Name = "Energy", Position = new Vector2(50, 6), Size = new Vector2(136, 14),
            MinValue = 0, MaxValue = 1, Step = .001, ShowPercentage = false
        });
        var heat = new ProgressBar
        {
            Name = "Heat", Position = new Vector2(50, 42), Size = new Vector2(136, 14),
            MinValue = 0, MaxValue = 1, Step = .001, ShowPercentage = false
        };
        heat.AddChild(new Control { Name = "OverheatMarker", Visible = false });
        visual.AddChild(heat);
        return visual;
    }
}
