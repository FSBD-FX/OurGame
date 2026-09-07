using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static class CanyonContentCatalog
{
    private static readonly CanyonMicroSceneDefinition[] Entries =
    [
        new("BrokenBridgeLookout", PrologueSectorId.CanyonEntrance,
            new Rect2(0, 0, 360, 720), false),
        new("SuspendedYard", PrologueSectorId.CanyonEntrance,
            new Rect2(360, 0, 430, 720), false),
        new("CollapsedGate", PrologueSectorId.CanyonEntrance,
            new Rect2(790, 0, 490, 720), false),
        new("CraneShaft", PrologueSectorId.CanyonDescent,
            new Rect2(0, 0, 640, 500), false),
        new("FrostVent", PrologueSectorId.CanyonDescent,
            new Rect2(520, 360, 760, 440), false),
        new("BrokenLift", PrologueSectorId.CanyonDescent,
            new Rect2(300, 760, 980, 680), false),
        new("ArchiveAlcove", PrologueSectorId.CanyonDescent,
            new Rect2(0, 980, 360, 300), true),
        new("GearGraveyard", PrologueSectorId.CanyonBasin,
            new Rect2(0, 0, 520, 720), false),
        new("SteamPumpTrench", PrologueSectorId.CanyonBasin,
            new Rect2(520, 0, 580, 720), false),
        new("CheckpointCampSalvage", PrologueSectorId.CanyonBasin,
            new Rect2(1100, 0, 500, 720), false),
        new("CounterweightWell", PrologueSectorId.CanyonAscent,
            new Rect2(0, 900, 480, 540), false),
        new("ScaffoldPendulumGallery", PrologueSectorId.CanyonAscent,
            new Rect2(320, 360, 700, 900), false),
        new("CeilingRailHighRuin", PrologueSectorId.CanyonAscent,
            new Rect2(760, 0, 520, 560), false),
        new("OuterSealGuardWorkshop", PrologueSectorId.LostTemple,
            new Rect2(0, 0, 760, 720), false),
        new("BellAntechamberBossHall", PrologueSectorId.LostTemple,
            new Rect2(760, 0, 1160, 720), false)
    ];

    public static IReadOnlyList<CanyonMicroSceneDefinition> All => Entries;

    public static IReadOnlyList<CanyonMicroSceneDefinition> ForSector(PrologueSectorId sectorId) =>
        Entries.Where(item => item.SectorId == sectorId).ToArray();
}
