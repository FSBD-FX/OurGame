using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public partial class BuildLostTemple : SceneTree
{
    public override void _Initialize()
    {
        PrologueSector sector = PrologueCanyonBuildParts.Build(PrologueSectorId.LostTemple);
        Error error = PrologueCanyonBuildParts.SaveAndValidate(sector,
            PrologueCanyonBuildParts.ScenePath(PrologueSectorId.LostTemple),
            out int before, out int after, out string failure);
        int microScenes = CanyonContentCatalog.ForSector(PrologueSectorId.LostTemple).Count;
        sector.Free();
        if (error != Error.Ok) { GD.PushError($"BuildLostTemple failed: {failure}"); Quit(1); return; }
        GD.Print($"BuildLostTemple: packed and validated nodes_before={before} " +
            $"nodes_after={after} micro_scenes={microScenes}.");
        Quit(0);
    }
}
