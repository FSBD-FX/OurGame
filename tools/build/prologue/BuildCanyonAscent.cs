using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public partial class BuildCanyonAscent : SceneTree
{
    public override void _Initialize()
    {
        PrologueSector sector = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonAscent);
        Error error = PrologueCanyonBuildParts.SaveAndValidate(sector,
            PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonAscent),
            out int before, out int after, out string failure);
        int microScenes = CanyonContentCatalog.ForSector(PrologueSectorId.CanyonAscent).Count;
        sector.Free();
        if (error != Error.Ok) { GD.PushError($"BuildCanyonAscent failed: {failure}"); Quit(1); return; }
        GD.Print($"BuildCanyonAscent: packed and validated nodes_before={before} " +
            $"nodes_after={after} micro_scenes={microScenes}.");
        Quit(0);
    }
}
