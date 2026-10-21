using Godot;
using Knight.Prologue;

namespace Knight.Presentation;

public enum RoomAtmosphereKind
{
    ClockDust,
    GearSparks,
    HighDraft,
    FungalSpores,
    ChainMotes,
    BellAsh
}

public sealed record RoomVisualDescriptor(
    PrologueRoomId Room,
    string Landmark,
    Color Shadow,
    Color Stone,
    Color Copper,
    float FarParallax,
    float MidParallax,
    float FrontParallax,
    RoomAtmosphereKind Atmosphere);
