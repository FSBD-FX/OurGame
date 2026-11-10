using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Tools;

namespace Knight.Tests.Unit;

public static class FirstGateAbyssTerrainTests
{
    // Catches a bounds-only expansion, sealed04 handoff, missing30 real placements,
    // non-native scene loss, or fog/minimap-budget-breaking collider proliferation.
    public static string Run(string storage)
    {
        var root=FirstGateAbyssTerrain.Build();
        TestAssert.True(root!=null,"ABYSS_TERRAIN_RED: actual continuous first-half terrain must exist, not allocation metadata");
        try
        {
            var bodies=root.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>().ToArray();
            TestAssert.True(bodies.Length>15 && bodies.Length<=128,"larger connected geometry preserves structural/future fog capacity");
            foreach(var (point,occupied,label) in new[]{
                (new Vector2(2944,1600),false,"04 opens into05"),
                (new Vector2(600,900),true,"03 old roof"),
                (new Vector2(1700,1750),false,"old128 gap"),
                (new Vector2(1808,1712),true,"old far bank"),
                (new Vector2(4080,1992),true,"05 mid-level landing"),
                (new Vector2(3000,2100),false,"06 westward passage"),
                (new Vector2(1600,3900),false,"07 continuous deep shaft"),
                (new Vector2(3500,4400),false,"08 high corridor"),
                (new Vector2(2000,4960),false,"09 low return corridor"),
                (new Vector2(3070,6600),true,"11 native low pillar"),
                (new Vector2(5500,7300),false,"12 actual final corridor"),
                (new Vector2(5500,7520),true,"12 solid floor"),
                (new Vector2(5880,7300),true,"temporary end remains physically closed")})
                TestAssert.Equal(occupied,bodies.Any(b=>SolidRect(b).HasPoint(point)),"ABYSS_GEOMETRY_LITERAL: "+label);
            string[] expected=Enumerable.Range(1,12).Select(n=>$"knight.first-gate.main.{n:00}.resource")
                .Concat("abcdef".SelectMany(c=>new[]{"chest","resource.01","resource.02"}.Select(s=>$"knight.first-gate.branch.{c}.{s}"))).Order().ToArray();
            var caches=root.GetNode("Gameplay/ResourceCaches").GetChildren().OfType<PrologueResourceCache>().ToArray();
            TestAssert.True(caches.Length==30 && caches.Select(c=>c.RewardId).Order().SequenceEqual(expected),"thirty unique physical main/branch rewards, not marker slots");
            foreach(var (resource,total) in new[]{("mineral",28),("fiber",9),("salvage",6),("data",3),("echo-memory",2)})
                TestAssert.Equal(total,ResourceRewardCatalog.Total(caches.Select(c=>c.RewardId),resource),"literal fixed grant totals "+resource);
            TestAssert.True(All(root).OfType<CanyonUpdraft>().Count()==2,"two actual readable descent fields, not damage exemptions");
            string path=Path.Combine(storage,"FirstGateAbyss01To12-v341.tscn");int count=Count(root);
            TestAssert.True(!File.Exists(path),"never overwrite earlier terrain evidence");
            TestAssert.Equal(Error.Ok,SceneBuildUtil.PackAndSave(root,path),"actual expanded leaf saved");
            using var packed=GD.Load<PackedScene>(path);var copy=packed.Instantiate();
            try {TestAssert.Equal(count,Count(copy),"expanded scene survives native pack/reload");}
            finally{copy.Free();}
            using var def=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,packed,Vector2.Zero,new(0,0,6144,8192),null,null);
            using var map=MinimapTerrainCatalogBuilder.Build(new[]{def});
            TestAssert.Equal(786432,map.CellCount,"actual expanded bounds remain within existing map capacity");
            TestAssert.Equal(bodies.Length,map.SolidCount,"all structural geometry reaches actual catalog");
            GD.Print($"ABYSS_TERRAIN_AUTHORING_OK nodes={count} solids={bodies.Length} rewards=30 cells=786432 actual_traversal=false prototype=true human=false");
            return path;
        }
        finally{FreeOwned(root);}
    }
    private static Rect2 SolidRect(StaticBody2D body)
    {var shape=(RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape;return new(body.Position-shape.Size/2,shape.Size);}
    private static IEnumerable<Node> All(Node root)
    {yield return root;foreach(Node child in root.GetChildren())foreach(Node item in All(child))yield return item;}
    private static int Count(Node root)=>1+root.GetChildren().Sum(Count);
    private static void FreeOwned(Node root)
    {var shapes=All(root).OfType<CollisionShape2D>().Select(n=>n.Shape).Distinct().ToArray();root.Free();foreach(var shape in shapes)shape.Dispose();}
}
