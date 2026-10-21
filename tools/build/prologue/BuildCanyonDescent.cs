using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public partial class BuildCanyonDescent : SceneTree
{
    public override void _Initialize()
    {
        PrologueSector sector = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonDescent);
        Error error = PrologueCanyonBuildParts.SaveAndValidate(sector,
            PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonDescent),
            out int before, out int after, out string failure);
        int microScenes = CanyonContentCatalog.ForSector(PrologueSectorId.CanyonDescent).Count;
        sector.Free();
        if (error != Error.Ok) { GD.PushError($"BuildCanyonDescent failed: {failure}"); Quit(1); return; }
        GD.Print($"BuildCanyonDescent: packed and validated nodes_before={before} " +
            $"nodes_after={after} micro_scenes={microScenes}.");
        Quit(0);
    }
}
