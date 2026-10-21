using Godot;
using Knight.Boss;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Guidance;

public static class PrologueGuidanceTargetResolver
{
    public static bool TryGetStaticOwningSector(PrologueGuidanceTargetKey key, out PrologueSectorId owner)
    {
        PrologueSectorId? authored = key switch
        {
            PrologueGuidanceTargetKey.EntranceCombat or PrologueGuidanceTargetKey.EntranceExit => PrologueSectorId.CanyonEntrance,
            PrologueGuidanceTargetKey.DescentGripStone or PrologueGuidanceTargetKey.DescentIce => PrologueSectorId.CanyonDescent,
            PrologueGuidanceTargetKey.BasinShieldLane or PrologueGuidanceTargetKey.BasinCheckpoint => PrologueSectorId.CanyonBasin,
            PrologueGuidanceTargetKey.AscentAnchorOne => PrologueSectorId.CanyonAscent,
            PrologueGuidanceTargetKey.TempleEntrance or PrologueGuidanceTargetKey.TempleMechanism or
                PrologueGuidanceTargetKey.TempleCheckpoint or PrologueGuidanceTargetKey.BossActivation => PrologueSectorId.LostTemple,
            _ => null // Dynamic and unknown keys must not be classified as an absent static marker.
        };
        owner = authored.GetValueOrDefault();
        return authored.HasValue;
    }

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

        int layout = sector.HasMeta("canyon_layout_version") ? sector.GetMeta("canyon_layout_version").AsInt32() : 1;
        if (layout == 2) return TryResolveV2(sector, key, out target);
        if (layout != 1) return false;
        string path = (sectorId, key) switch
        {
            (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceCombat) =>
                "Gameplay/TraversalContracts/entrance_move_attack",
            (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceExit) =>
                "Gameplay/RouteMarkers/NextSeam",
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

    private static bool TryResolveV2(PrologueSector sector, PrologueGuidanceTargetKey key, out Node2D target)
    {
        string path = (sector.SectorId, key) switch
        {
            (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceCombat) => "Gameplay/Actors/RustShellSquire",
            (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceExit) => "Gameplay/RouteMarkers/NextSeam",
            (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentGripStone) => "Gameplay/Collision/AbyssBrakeRight1/GrappleSurfaceProfile",
            (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentIce) => "Gameplay/Collision/FrozenCeiling/GrappleSurfaceProfile",
            (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinShieldLane) => "Gameplay/ShieldLane",
            (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinCheckpoint) => "Gameplay/BasinCheckpoint",
            (PrologueSectorId.CanyonAscent, PrologueGuidanceTargetKey.AscentAnchorOne) => "Gameplay/Collision/AscentGrip1/GrappleSurfaceProfile",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleEntrance) => "Gameplay/Collision/CourtyardFloor/GrappleSurfaceProfile",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleMechanism) => "Gameplay/Mechanisms/BellChain",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleCheckpoint) => "Gameplay/TempleCheckpoint",
            (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.BossActivation) => "Gameplay/ActivationTrigger",
            _ => null
        };
        target = path == null ? null : sector.GetNodeOrNull<Node2D>(path);
        if (!GodotObject.IsInstanceValid(target)) return false;
        if (key is not (PrologueGuidanceTargetKey.DescentGripStone or PrologueGuidanceTargetKey.DescentIce or
            PrologueGuidanceTargetKey.AscentAnchorOne or PrologueGuidanceTargetKey.TempleEntrance)) return true;
        GrappleSurfaceMaterial expected = key == PrologueGuidanceTargetKey.DescentIce ? GrappleSurfaceMaterial.Ice :
            key == PrologueGuidanceTargetKey.DescentGripStone ? GrappleSurfaceMaterial.GripStone : GrappleSurfaceMaterial.TempleMetal;
        uint layers = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
        bool valid = target is GrappleSurface2D { UseCollisionHitPoint: true } profile && profile.Material == expected &&
            profile.GetParent() is StaticBody2D body && (body.CollisionLayer & layers) == layers &&
            body.GetNodeOrNull<CollisionShape2D>("Collision") is { Disabled: false, Shape: RectangleShape2D shape } &&
            float.IsFinite(shape.Size.X) && float.IsFinite(shape.Size.Y) && shape.Size.X > 0 && shape.Size.Y > 0;
        if (!valid) target = null;
        return valid;
    }
}
