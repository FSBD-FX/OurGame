using Godot;

namespace Knight.Prologue;

public readonly record struct PrologueRoomLink(
    PrologueRoomId SourceRoom,
    StringName ExitId,
    PrologueRoomId DestinationRoom,
    StringName EntranceId);
