using Godot;
using Knight.Boss;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Guidance;

public static class PrologueGuidanceTargetResolver
{
    public static bool TryResolve(PrologueSector sector, PrologueSectorId sectorId,
        PrologueGuidanceTargetKey key, out Node2D target)
    {
        target = null;
        if (!GodotObject.IsInstanceValid(sector) || sector.SectorId != sectorId) return false;
        if (key is PrologueGuidanceTargetKey.BossBody or PrologueGuidanceTargetKey.BossChain)
        {
            BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
                "Gameplay/BossArenaController");
            if (!GodotObject.IsInstanceValid(arena?.CurrentBoss)) return false;
            target = key == PrologueGuidanceTargetKey.BossBody
                ? arena.CurrentBoss
                : arena.CurrentBoss.GetNodeOrNull<Node2D>("ChainTarget");
            return GodotObject.IsInstanceValid(target);
        }

        string path = (sectorId, key) switch
        {
            (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceCombat) =>
                "Gameplay/TraversalContracts/entrance_move_attack",
            (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentGripStone) =>
                "Gameplay/Collision/CraneShaftWall/GrappleSurfaceProfile",
            (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentIce) =>
                "Gameplay/Collision/VisibleIceWall/GrappleSurfaceProfile",
            (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinShieldLane) =>
                "Gameplay/ShieldLane",
            (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinCheckpoint) =>
                "Gameplay/Checkpoint",
            (PrologueSectorId.CanyonAscent, PrologueGuidanceTargetKey.AscentAnchorOne) =>
                "Gameplay/GrappleSurfaces/AscentAnchorOne",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleEntrance) =>
                "Gameplay/TraversalContracts/temple_mechanism",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleMechanism) =>
                "Gameplay/Mechanisms/BellChain",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleCheckpoint) =>
                "Gameplay/TempleCheckpoint",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.BossActivation) =>
                "Gameplay/ActivationTrigger",
            _ => null
        };
        target = path == null ? null : sector.GetNodeOrNull<Node2D>(path);
        return GodotObject.IsInstanceValid(target);
    }
}
