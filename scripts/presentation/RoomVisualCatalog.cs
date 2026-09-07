using System;
using Godot;
using Knight.Prologue;

namespace Knight.Presentation;

public static class RoomVisualCatalog
{
    public static readonly string[] StableSectorVisualRoots =
    [
        "VisualRoot/Background", "VisualRoot/Midground", "VisualRoot/GameplayReadable",
        "VisualRoot/Foreground", "VisualRoot/Effects"
    ];

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
