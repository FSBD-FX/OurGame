using Godot;

namespace Knight.Prologue;

[GlobalClass]
public partial class PrologueRoomLinkResource : Resource
{
    [Export] public PrologueRoomId SourceRoom { get; set; }
    [Export] public StringName ExitId { get; set; }
    [Export] public PrologueRoomId DestinationRoom { get; set; }
    [Export] public StringName EntranceId { get; set; }

    public PrologueRoomLink ToLink() => new(SourceRoom, ExitId, DestinationRoom, EntranceId);
}
