using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public partial class BuildCanyonBasin : SceneTree
{
    public override void _Initialize()
    {
        PrologueSector sector = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        Error error = PrologueCanyonBuildParts.SaveAndValidate(sector,
            PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonBasin),
            out int before, out int after, out string failure);
        int microScenes = CanyonContentCatalog.ForSector(PrologueSectorId.CanyonBasin).Count;
        sector.Free();
        if (error != Error.Ok) { GD.PushError($"BuildCanyonBasin failed: {failure}"); Quit(1); return; }
        GD.Print($"BuildCanyonBasin: packed and validated nodes_before={before} " +
            $"nodes_after={after} micro_scenes={microScenes}.");
        Quit(0);
    }
}
