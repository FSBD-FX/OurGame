using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Unit;

public static class FirstGateEntryTerrainTests
{
    // Catches an empty/background-only map, serialization loss, or placements detached from registered grants.
    public static string Run(string storage)
    {
        var root=FirstGateEntryTerrain.Build();
        try
        {
            var collision=root.GetNodeOrNull<Node2D>("Gameplay/Collision");
            TestAssert.True(collision!=null && collision.GetChildCount()>12,"ENTRY_TERRAIN_RED: actual enclosed rock masses must exist");
            TestAssert.True(collision.GetChildCount()<=256,"entry native rectangle budget, not unlimited tiled colliders");
            foreach(var body in collision.GetChildren().OfType<StaticBody2D>())
            {
                TestAssert.True(body.GetScript().VariantType==Variant.Type.Nil,"terrain collision body remains script-free");
                TestAssert.True(body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile") is {UseCollisionHitPoint:true},"real terrain grip profile");
                var shape=body.GetNode<CollisionShape2D>("Collision");
                TestAssert.True(shape.Shape is RectangleShape2D && !shape.Disabled,"native rectangle enabled");
                var skin=root.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/"+body.Name);
                TestAssert.True(skin.Position==body.Position && skin.SkinSize==((RectangleShape2D)shape.Shape).Size,"skin matches actual body");
            }
            var caches=root.GetNode<Node2D>("Gameplay/ResourceCaches").GetChildren().OfType<PrologueResourceCache>().ToArray();
            string[] expected={"knight.first-gate.main.01.resource","knight.first-gate.main.02.resource","knight.first-gate.main.03.resource","knight.first-gate.main.04.resource","knight.first-gate.branch.a.chest","knight.first-gate.branch.a.resource.01","knight.first-gate.branch.a.resource.02"};
            TestAssert.True(caches.Select(x=>x.RewardId).Order().SequenceEqual(expected.Order()),"seven independently listed main/A grants physically placed");
            foreach(var cache in caches)
                TestAssert.True(ResourceRewardCatalog.TryGet(cache.RewardId,out _) && !cache.IsClaimed,"saved unbound placements are not awarded progress");
            var refugeVisual=root.GetNodeOrNull<PrologueMechanismVisual>("Gameplay/ARefuge/Visual");
            TestAssert.True(refugeVisual is {Visible:true,Kind:MechanismVisualKind.GraveLamp,InitialState:MechanismVisualState.Idle} &&
                refugeVisual.Position==new Vector2(-52,-6),"PROFILE_REFUGE_VISUAL_RED: persistent recognizable grave lamp stands beside, not behind, the chest");
            string path=Path.Combine(storage,"FirstGateEntry01To04-v339.tscn");
            int nodes=Count(root);
            TestAssert.True(!File.Exists(path),"never overwrite earlier candidate evidence");
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"physical candidate saved externally");
            using var packed=GD.Load<PackedScene>(path);
            var copy=packed.Instantiate();
            try
            {
                TestAssert.Equal(nodes,Count(copy),"saved candidate loses no nodes");
                TestAssert.True(copy.GetNodeOrNull<PrologueMechanismVisual>("Gameplay/ARefuge/Visual") is
                    {Visible:true,Kind:MechanismVisualKind.GraveLamp,InitialState:MechanismVisualState.Idle},"saved leaf retains replaceable checkpoint presentation");
            }
            finally {copy.Free();}
            using var definition=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,Vector2.Zero,FirstGateEntryTerrain.Bounds,null,null);
            using var catalog=MinimapTerrainCatalogBuilder.Build(new[]{definition});
            TestAssert.Equal(98304,catalog.CellCount,"independent new bounds produce384x256 eight-pixel minimap cells");
            TestAssert.Equal(collision.GetChildCount(),catalog.SolidCount,"saved catalog consumes all actual structural bodies");
            foreach(var (point,occupied,label) in new[]{
                (new Vector2(600,1100),false,"03 enclosed passage"),
                (new Vector2(600,900),true,"03 actual roof"),
                (new Vector2(100,1450),true,"left turn outer wall"),
                (new Vector2(1700,1750),false,"beam128px gap"),
                (new Vector2(1808,1712),true,"beam far bank begins1760")})
            {
                bool actual=Enumerable.Range(0,catalog.SolidCount).Select(catalog.Solid)
                    .Any(s=>s.LocalBounds.HasPoint(s.LocalToWorld.AffineInverse()*point));
                TestAssert.Equal(occupied,actual,"ENTRY_GEOMETRY_LITERAL: "+label);
            }
            GD.Print($"ENTRY_TERRAIN_AUTHORING_OK nodes={nodes} solids={collision.GetChildCount()} rewards=7 bounds=3072x2048 prototype=true campaign_integrated=false");
            return path;
        }
        finally
        {
            // These are newly constructed authoring resources, never cached Player or shared legacy assets.
            // Keep their managed owners alive through Free, then deterministically release our references.
            var shapes=All(root).OfType<CollisionShape2D>().Select(x=>x.Shape).Distinct().ToArray();
            root.Free();
            foreach(var shape in shapes)shape.Dispose();
        }
    }
    private static int Count(Node node)=>1+node.GetChildren().Sum(Count);
    private static IEnumerable<Node> All(Node root)
    {yield return root;foreach(Node child in root.GetChildren())foreach(Node n in All(child))yield return n;}
}
