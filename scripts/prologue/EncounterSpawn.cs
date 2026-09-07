using Godot;

namespace Knight.Prologue;

[GlobalClass]
public partial class EncounterSpawn : Resource
{
    [Export] public PackedScene Scene { get; set; }
    [Export] public NodePath MarkerPath { get; set; }
    [Export] public StringName SpawnId { get; set; }
}
