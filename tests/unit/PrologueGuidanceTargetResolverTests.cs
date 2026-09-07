using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueGuidanceTargetResolverTests
{
    private static readonly (PrologueSectorId Sector, PrologueGuidanceTargetKey Key, string Path)[] Rows =
    [
        (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceCombat,
            "Gameplay/TraversalContracts/entrance_move_attack"),
        (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentGripStone,
            "Gameplay/Collision/CraneShaftWall/GrappleSurfaceProfile"),
        (PrologueSectorId.CanyonDescent, PrologueGuidanceTargetKey.DescentIce,
            "Gameplay/Collision/VisibleIceWall/GrappleSurfaceProfile"),
        (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinShieldLane,
            "Gameplay/ShieldLane"),
        (PrologueSectorId.CanyonBasin, PrologueGuidanceTargetKey.BasinCheckpoint,
            "Gameplay/Checkpoint"),
        (PrologueSectorId.CanyonAscent, PrologueGuidanceTargetKey.AscentAnchorOne,
            "Gameplay/GrappleSurfaces/AscentAnchorOne"),
        (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleEntrance,
            "Gameplay/TraversalContracts/temple_mechanism"),
        (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleMechanism,
            "Gameplay/Mechanisms/BellChain"),
        (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.TempleCheckpoint,
            "Gameplay/TempleCheckpoint"),
        (PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.BossActivation,
            "Gameplay/ActivationTrigger")
    ];

    public static async Task Run(SceneTree tree)
    {
        foreach (var row in Rows)
        {
            PrologueSector sector = GD.Load<PackedScene>(
                    $"res://scenes/prologue/sectors/{row.Sector}.tscn")
                .Instantiate<PrologueSector>();
            tree.Root.AddChild(sector);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(sector,
                row.Sector, row.Key, out Node2D target),
                $"current mounted {row.Sector}/{row.Key} resolves its declared live target");
            TestAssert.Equal(sector.GetNode<Node2D>(row.Path), target,
                $"{row.Sector}/{row.Key} uses exact stable sector path {row.Path}");
            if (row.Key is PrologueGuidanceTargetKey.DescentGripStone or
                PrologueGuidanceTargetKey.DescentIce)
            {
                GrappleSurfaceMaterial expected = row.Key == PrologueGuidanceTargetKey.DescentGripStone
                    ? GrappleSurfaceMaterial.GripStone
                    : GrappleSurfaceMaterial.Ice;
                TestAssert.True(target is GrappleSurface2D profile && profile.UseCollisionHitPoint &&
                                profile.Material == expected && profile.GetParent() is StaticBody2D,
                    $"{row.Key} resolves the generated terrain profile with explicit {expected} material");
            }

            PrologueSectorId wrong = row.Sector == PrologueSectorId.CanyonEntrance
                ? PrologueSectorId.CanyonDescent
                : PrologueSectorId.CanyonEntrance;
            TestAssert.True(!PrologueGuidanceTargetResolver.TryResolve(sector,
                wrong, row.Key, out _),
                "a mounted sector can never satisfy guidance for a different current sector ID");
            sector.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        await BossTargetsFollowTheLiveTempleBoss(tree);
    }

    private static async Task BossTargetsFollowTheLiveTempleBoss(SceneTree tree)
    {
        PrologueSector temple = GD.Load<PackedScene>(
                "res://scenes/prologue/sectors/LostTemple.tscn")
            .Instantiate<PrologueSector>();
        tree.Root.AddChild(temple);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        BossArenaController arena = temple.GetNode<BossArenaController>(
            "Gameplay/BossArenaController");
        arena.BeginEncounter();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(temple,
            PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.BossBody, out Node2D body),
            "BossBody resolves from the live boss in the current mounted Temple sector");
        TestAssert.Equal(arena.CurrentBoss, body,
            "BossBody follows the arena's live CurrentBoss instance");
        TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(temple,
            PrologueSectorId.LostTemple, PrologueGuidanceTargetKey.BossChain, out Node2D chain),
            "BossChain resolves from the live boss in the current mounted Temple sector");
        TestAssert.Equal(arena.CurrentBoss.GetNode<Node2D>("ChainTarget"), chain,
            "BossChain follows the live boss ChainTarget");

        temple.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
