using System;
using Godot;

namespace Knight.Prologue;

internal readonly record struct LegacyPrologueCheckpointSnapshot(
    PrologueRoomId Room,
    StringName Entrance);

/// <summary>
/// Isolated checkpoint state for retained old-room test fixtures. The continuous canyon never
/// constructs or reads this authority because its level branch is selected by SeamlessWorldPath.
/// </summary>
internal sealed class LegacyPrologueCheckpointState
{
    private PrologueRoomId _room = PrologueRoomId.AwakeningCourtyard;
    private StringName _entrance = "start";

    public void Reset()
    {
        _room = PrologueRoomId.AwakeningCourtyard;
        _entrance = "start";
    }

    public bool Activate(PrologueRoomId room, StringName entrance)
    {
        if (!Enum.IsDefined(room) || entrance == null || entrance.IsEmpty) return false;
        if (_room == room && _entrance == entrance) return false;
        _room = room;
        _entrance = entrance;
        return true;
    }

    public LegacyPrologueCheckpointSnapshot Restore() => new(_room, _entrance);
}
