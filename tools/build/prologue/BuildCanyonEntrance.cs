using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public partial class BuildCanyonEntrance : SceneTree
{
    public override void _Initialize() => Build(PrologueSectorId.CanyonEntrance);

    private void Build(PrologueSectorId id)
    {
        PrologueSector sector = PrologueCanyonBuildParts.Build(id);
        Error error = PrologueCanyonBuildParts.SaveAndValidate(sector,
            PrologueCanyonBuildParts.ScenePath(id), out int before, out int after, out string failure);
        int microScenes = CanyonContentCatalog.ForSector(id).Count;
        sector.Free();
        if (error != Error.Ok) { GD.PushError($"BuildCanyonEntrance failed: {failure}"); Quit(1); return; }
        GD.Print($"BuildCanyonEntrance: packed and validated nodes_before={before} " +
            $"nodes_after={after} micro_scenes={microScenes}.");
        Quit(0);
    }
}
