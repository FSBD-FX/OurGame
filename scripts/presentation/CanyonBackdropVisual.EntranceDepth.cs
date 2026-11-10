using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Presentation;

/// <summary>Static replaceable scenery: local vertices and ordinary per-vertex color, no gameplay authority.</summary>
public sealed class CanyonEntranceDepthPaintCommand
{
    private readonly Vector2[] _points;
    private readonly Color[] _colors;
    public string Role { get; }
    public IReadOnlyList<Vector2> Points { get; }
    public IReadOnlyList<Color> Colors { get; }

    internal CanyonEntranceDepthPaintCommand(string role, Vector2[] points, Color[] colors)
    {
        Role = role;
        _points = (Vector2[])points.Clone();
        _colors = colors.Length == 1 ? Enumerable.Repeat(colors[0],points.Length).ToArray() : (Color[])colors.Clone();
        Points = Array.AsReadOnly(_points);
        Colors = Array.AsReadOnly(_colors);
    }

    internal void Submit(CanvasItem target) => target.DrawPolygon(_points,_colors);
}

public partial class CanyonBackdropVisual
{
    /// <summary>
    /// Sparse recessed entrance only. The enclosing rock solids/ReadableEdge own the
    /// traversable silhouette; these muted, non-emissive forms never advertise support.
    /// CanvasItem retains the submitted commands: no per-frame processing or particles.
    /// </summary>
    public IReadOnlyList<CanyonEntranceDepthPaintCommand> BuildEntranceDepthPaintPlan()
    {
        Vector2 size = SectorBounds.Size;
        if (!UseCanyonDepthStaging || SectorId != PrologueSectorId.CanyonEntrance ||
            DistantLayer || CoverageLayer || VisualRole != "sector" ||
            !float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0)
            return Array.Empty<CanyonEntranceDepthPaintCommand>();

        Vector2 scale = size / new Vector2(1280,720);
        var paint = new List<CanyonEntranceDepthPaintCommand>();
        void Layer(string role, Vector2[] points, Color[] colors) => paint.Add(
            new CanyonEntranceDepthPaintCommand(role,points.Select(p => p*scale).ToArray(),colors));

        // Exact four existing staged contours remain the bed of the new composition.
        Layer("recess-base",[new(48,80),new(600,80),new(600,400),new(1280,400),new(1280,636),new(48,636)],
            [new("5a6253",.55f),new("596052",.46f),new("334447",.5f),new("172c34",.65f),new("132831",.8f),new("283b3b",.5f)]);
        Layer("ambient-wash",[new(85,100),new(540,80),new(470,375),new(342,560),new(98,603)], [new("7e8167",.12f)]);
        Layer("rear-rock",[new(110,332),new(226,260),new(356,286),new(444,377),new(417,530),new(115,601)], [new("405253",.25f)]);
        Layer("far-bank-recess",[new(616,456),new(792,418),new(970,452),new(1190,431),new(1280,470),new(1280,636),new(600,636)], [new("0f242d",.25f)]);

        // Broad uneven shale, then a vertical fault: no repetitive horizontal stair sockets.
        Layer("shale-recession",[new(104,160),new(283,121),new(530,151),new(551,270),new(470,438),new(288,575),new(108,593),new(138,374)],
            [new("21373d",.08f),new("35494b",.30f),new("1d333b",.25f),new("142e39",.38f),
                new("112c37",.34f),new("1b3337",.13f),new("354642",.08f),new("3a4b46",.19f)]);
        Layer("fault-shadow",[new(298,214),new(335,182),new(309,369),new(284,496),new(265,595),new(243,590),new(264,486),new(284,356)],
            [new("0e252f",0f),new("0e252f",.13f),new("0e252f",.31f),new("0e252f",.27f),
                new("0e252f",.05f),new("0e252f",0f),new("0e252f",.15f),new("0e252f",.25f)]);

        // Buried, bent structural rib. Its lit cut is muted bronze, not a grappling glint.
        Layer("buried-rib",[new(399,164),new(419,173),new(392,360),new(381,477),new(346,574),new(326,585),new(353,466),new(366,352)],
            [new("41483e",.12f),new("5c5a48",.30f),new("4d5144",.34f),new("373f39",.32f),
                new("303d38",.09f),new("303d38",0f),new("263a37",.23f),new("3d473d",.28f)]);
        Layer("buried-rib-bevel",[new(411,177),new(419,173),new(392,360),new(381,477),new(346,574),new(341,568),new(372,472),new(383,354)],
            [new("79765a",.06f),new("79765a",.19f),new("79765a",.26f),new("79765a",.20f),
                new("79765a",0f),new("79765a",0f),new("79765a",.13f),new("79765a",.21f)]);

        // A broken angled conduit is half swallowed by the far-bank strata. It has
        // no horizontal rim/door rectangle, no emitter core and no collision node.
        Layer("broken-conduit",[new(778,448),new(812,445),new(838,493),new(948,517),new(1048,492),new(1138,512),new(1160,542),new(1052,524),new(950,550),new(816,521)],
            [new("3a4740",.08f),new("555646",.27f),new("4e5143",.36f),new("303d3a",.34f),new("3e4840",.30f),
                new("474d42",.23f),new("243b3d",.08f),new("22383b",.14f),new("1d3438",.19f),new("263a38",.12f)]);
        Layer("broken-conduit-bevel",[new(786,451),new(812,445),new(838,493),new(948,517),new(1048,492),new(1138,512),new(1143,519),new(1046,503),new(948,529),new(829,505)],
            [new("73745a",0f),new("73745a",.18f),new("73745a",.24f),new("73745a",.21f),new("73745a",.19f),
                new("73745a",.08f),new("73745a",0f),new("73745a",.09f),new("73745a",.13f),new("73745a",.12f)]);
        Layer("earth-mass",[new(96,435),new(177,392),new(232,411),new(273,487),new(240,562),new(139,598),new(91,558)],
            [new("4f5546",.08f),new("5e614d",.24f),new("535b4a",.22f),new("34483f",.16f),new("263c39",.11f),new("263c39",0f),new("34473d",.05f)]);
        Layer("light-wash",[new(137,104),new(332,91),new(524,134),new(451,300),new(288,478),new(151,565),new(188,361)],
            [new("83856c",.06f),new("83856c",.10f),new("83856c",.10f),new("83856c",.05f),new("83856c",0f),new("83856c",0f),new("83856c",.02f)]);
        return paint.AsReadOnly();
    }
}
