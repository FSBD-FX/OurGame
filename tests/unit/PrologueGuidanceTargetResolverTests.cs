using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueGuidanceTargetResolverTests
{
    // Explicit negative case: its one expected warning is kept out of clean route logs.
    public static async Task RunTask4MissingOwnerWarning(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
        string fixture=$"user://task4-guidance-negative/{Guid.NewGuid():N}";
        flow.SavePath=fixture+"/save.json"; flow.SettingsPath=fixture+"/settings.json";
        tree.Root.AddChild(level);
        try
        {
            for(int i=0;i<4;i++) await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
            var controller=level.GetNode<PrologueGuidanceController>("Guidance");
            var warnings=(HashSet<string>)typeof(PrologueGuidanceController).GetField("_missingWarnings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(controller);
            TestAssert.Equal(0,warnings.Count,"valid authored entrance target starts without missing warnings");
            level.CurrentSectorRoot.GetNode("Gameplay/Actors/RustShellSquire").Free();
            controller.RefreshNow();
            TestAssert.True(warnings.Contains("CanyonEntrance/EntranceCombat") && warnings.Count==1,
                "same-owner missing actor still emits the existing WarnMissing diagnostic");
            controller.RefreshNow();
            TestAssert.Equal(1,warnings.Count,"same-owner warning remains deduplicated, not globally suppressed");
            GD.Print("TASK4_GUIDANCE_SAME_OWNER_WARNING_OK expected_warnings=1");
        }
        finally { level.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            for(int i=0;i<3;i++) await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame); }
    }

    public static void RunTask4()
    {
        var sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        try
        {
            sector.SetMeta("canyon_layout_version", 2);
            TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(sector, sector.SectorId,
                PrologueGuidanceTargetKey.DescentGripStone, out Node2D target),
                "V2GuidanceResolvesRealBrakeTerrain: explicit layout2 resolves actual thick brake wall instead of removed legacy path");
            TestAssert.Equal(sector.GetNode<Node2D>("Gameplay/Collision/AbyssBrakeRight1/GrappleSurfaceProfile"), target,
                "layout2 points to actual first readable brake face");
            var profile = (GrappleSurface2D)target;
            profile.Material = GrappleSurfaceMaterial.Ice;
            TestAssert.True(!PrologueGuidanceTargetResolver.TryResolve(sector, sector.SectorId,
                PrologueGuidanceTargetKey.DescentGripStone, out _), "layout2 cannot label ice as a valid brake");
            profile.Material = GrappleSurfaceMaterial.GripStone;
            var collision = profile.GetParent().GetNode<CollisionShape2D>("Collision");
            collision.Disabled = true;
            TestAssert.True(!PrologueGuidanceTargetResolver.TryResolve(sector, sector.SectorId,
                PrologueGuidanceTargetKey.DescentGripStone, out _), "layout2 does not guide to disabled physical terrain");
            collision.Disabled = false;
            sector.SetMeta("canyon_layout_version", 99);
            TestAssert.True(!PrologueGuidanceTargetResolver.TryResolve(sector, sector.SectorId,
                PrologueGuidanceTargetKey.DescentGripStone, out _), "unknown explicit layout does not guess geometry");
        }
        finally { sector.Free(); }
        foreach(var row in Rows)
        {
            TestAssert.True(PrologueGuidanceTargetResolver.TryGetStaticOwningSector(row.Key,out var owner) && owner==row.Sector,
                "static guidance retains its explicit authored owning sector: "+row.Key);
            var generated=GD.Load<PackedScene>($"res://scenes/prologue/sectors/{row.Sector}.tscn").Instantiate<PrologueSector>();
            try
            {
                TestAssert.Equal(2,generated.GetMeta("canyon_layout_version",1).AsInt32(),"generated sectors explicitly declare layout2, without dimension/path inference");
                string path=row.Key switch
                {
                    PrologueGuidanceTargetKey.EntranceCombat=>"Gameplay/Actors/RustShellSquire",
                    PrologueGuidanceTargetKey.DescentGripStone=>"Gameplay/Collision/AbyssBrakeRight1/GrappleSurfaceProfile",
                    PrologueGuidanceTargetKey.DescentIce=>"Gameplay/Collision/FrozenCeiling/GrappleSurfaceProfile",
                    PrologueGuidanceTargetKey.BasinCheckpoint=>"Gameplay/BasinCheckpoint",
                    PrologueGuidanceTargetKey.AscentAnchorOne=>"Gameplay/Collision/AscentGrip1/GrappleSurfaceProfile",
                    PrologueGuidanceTargetKey.TempleEntrance=>"Gameplay/Collision/CourtyardFloor/GrappleSurfaceProfile",
                    _=>row.Path
                };
                TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(generated,row.Sector,row.Key,out var mapped),"explicit v2 semantic guidance target resolves: "+row.Key);
                TestAssert.Equal(generated.GetNode<Node2D>(path),mapped,"v2 guidance points to actual semantic terrain/actor/interactive: "+row.Key);
            }
            finally { generated.Free(); }
        }
        foreach(var key in new[]{PrologueGuidanceTargetKey.BossBody,PrologueGuidanceTargetKey.BossChain,
            PrologueGuidanceTargetKey.ClawAttachment,PrologueGuidanceTargetKey.DescentClawOpportunity,(PrologueGuidanceTargetKey)999})
            TestAssert.True(!PrologueGuidanceTargetResolver.TryGetStaticOwningSector(key,out _),
                "dynamic and unknown guidance keys cannot be silently treated as off-owner static targets");
        GD.Print("TASK4_GUIDANCE_TERRAIN_OK");
    }

    private static readonly (PrologueSectorId Sector, PrologueGuidanceTargetKey Key, string Path)[] Rows =
    [
        (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceCombat,
            "Gameplay/TraversalContracts/entrance_move_attack"),
        (PrologueSectorId.CanyonEntrance, PrologueGuidanceTargetKey.EntranceExit,
            "Gameplay/RouteMarkers/NextSeam"),
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
                    $"res://scenes/prologue/sample/legacy/{row.Sector}.tscn")
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
        // Resolver identity needs the live dormant Boss, not an unrelated
        // quota-bypassing activation of an unbound current-layout arena.
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
