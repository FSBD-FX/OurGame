using System;
using Godot;
using Knight.Abilities;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Presentation;

public static class RoomVisualCatalog
{
    public static readonly string[] StableSectorVisualRoots =
    [
        "VisualRoot/Background", "VisualRoot/Midground", "VisualRoot/GameplayReadable",
        "VisualRoot/Foreground", "VisualRoot/Effects"
    ];

    public static Color CanyonSkirtBase => new("09151b");

    public static Rect2 ContinuousCanyonCoverageBounds()
    {
        // Largest supported normal window, inverted through the smallest supported camera zoom.
        // Add the largest authored focus plus abyss velocity look-ahead before expanding the
        // immutable design envelope. This is world-space coverage, not a guessed pixel margin.
        Vector2 viewport = new(1920f, 1080f);
        Vector2 minimumZoom = Vector2.One;
        Vector2 focusAndLookAhead = new(140f, 400f);
        Vector2 halfWorldFootprint = viewport / minimumZoom.Abs() * .5f + focusAndLookAhead;
        Rect2 design = CanyonWorldLayout.DesignBounds;
        return new Rect2(design.Position - halfWorldFootprint,
            design.Size + halfWorldFootprint * 2f);
    }

    public static string CanyonCompositionId(CanyonChallengeKind kind) => kind switch
    {
        CanyonChallengeKind.LowCeilingBridge => "broken-entrance-bell",
        CanyonChallengeKind.StaggeredRift => "staggered-belfry-ribs",
        CanyonChallengeKind.ControlledAbyss => "abyss-salvage-crane",
        CanyonChallengeKind.ArchiveSideCave => "sealed-archive-vault",
        CanyonChallengeKind.FrozenDetour => "frozen-bell-choir",
        CanyonChallengeKind.GearPistons => "buried-gear-crown",
        CanyonChallengeKind.SteamValves => "steam-pump-trench",
        CanyonChallengeKind.CounterweightLift => "moving-counterweight-lift",
        CanyonChallengeKind.PendulumGallery => "pendulum-procession",
        CanyonChallengeKind.SplitLevelEncounter => "upper-gallery-bell-frame",
        CanyonChallengeKind.SealedCourtyard => "sealed-temple-forecourt",
        CanyonChallengeKind.BossHall => "fallen-bell-temple-facade",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static string CanyonMaterialFamily(GrappleSurfaceMaterial material) => material switch
    {
        GrappleSurfaceMaterial.TempleMetal => "riveted-temple-brass",
        GrappleSurfaceMaterial.Ice => "rejected-frost-glass",
        GrappleSurfaceMaterial.Forbidden => "sealed-iron",
        GrappleSurfaceMaterial.DedicatedAnchor => "thread-anchor-cyan",
        _ => "grip-stone-masonry"
    };

    public static Color CanyonReadableEdge(PrologueSectorId sector,
        GrappleSurfaceMaterial material) => material switch
    {
        GrappleSurfaceMaterial.Ice => new Color("a8d8e8"),
        GrappleSurfaceMaterial.TempleMetal => new Color("b9a77b"),
        GrappleSurfaceMaterial.Forbidden => new Color("938590"),
        _ => sector switch
        {
            PrologueSectorId.CanyonEntrance => new Color("9dbb95"),
            PrologueSectorId.CanyonDescent => new Color("92adaf"),
            PrologueSectorId.CanyonBasin => new Color("b5aa7b"),
            PrologueSectorId.CanyonAscent => new Color("8fa8b5"),
            PrologueSectorId.LostTemple => new Color("b7a17f"),
            _ => new Color("a9c79d")
        }
    };

    public static RoomVisualDescriptor For(PrologueRoomId room) => room switch
    {
        PrologueRoomId.AwakeningCourtyard => new(room, "broken-clock",
            new Color("08131a"), new Color("3f5b52"), new Color("9a7445"),
            .08f, .15f, .27f, RoomAtmosphereKind.ClockDust),
        PrologueRoomId.BrokenToothCorridor => new(room, "severed-gear-train",
            new Color("0a1016"), new Color("46574f"), new Color("a26d3d"),
            .07f, .14f, .25f, RoomAtmosphereKind.GearSparks),
        PrologueRoomId.UpperPendulum => new(room, "pendulum-chains",
            new Color("07141c"), new Color("365d5b"), new Color("8c7247"),
            .1f, .18f, .31f, RoomAtmosphereKind.HighDraft),
        PrologueRoomId.LowerBuriedMachinery => new(room, "copper-pipes-fungus",
            new Color("15110d"), new Color("5b5039"), new Color("b17743"),
            .05f, .12f, .23f, RoomAtmosphereKind.FungalSpores),
        PrologueRoomId.BellChainMechanism => new(room, "vertical-bell-chain",
            new Color("100d18"), new Color("5a4960"), new Color("b88c4c"),
            .09f, .17f, .3f, RoomAtmosphereKind.ChainMotes),
        PrologueRoomId.FallenBellkeeperArena => new(room, "backlit-broken-bell",
            new Color("0b0a11"), new Color("59485d"), new Color("c39a55"),
            .06f, .13f, .24f, RoomAtmosphereKind.BellAsh),
        _ => throw new ArgumentOutOfRangeException(nameof(room), room, "Unknown prologue room.")
    };
}
