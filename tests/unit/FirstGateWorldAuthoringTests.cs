using System;
using System.IO;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class FirstGateWorldAuthoringTests
{
    public static string Run(string storage)
    {
        string leaf=FirstGateEntryTerrainTests.Run(storage);
        using var leafScene=GD.Load<PackedScene>(leaf);
        TestAssert.True(PrologueWorldProfiles.TryResolve("knight.first-gate.development.v1",out var profile),"selected profile available");
        using var definition=new PrologueSectorDefinition(PrologueSectorId.CanyonEntrance,leafScene,Vector2.Zero,new(0,0,3072,2048),null,null);
        var definitions=new[]{definition};
        var level=BuildPrologueLevel.BuildCandidate(profile,definitions);
        TestAssert.True(level!=null,"PROFILE_PARENT_RED: actual common game graph must build with one saved new leaf");
        var ownedCatalog=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        var borrowedPlayer=GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        ulong borrowedShape=level.GetNode<CollisionShape2D>("PlayerRoot/Player/BodyCollider").Shape.GetInstanceId();
        try
        {
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(!ReferenceEquals(world.Definitions[0],definition),"PROFILE_AUTHORING_OWNERSHIP_RED: candidate owns its new definition, not the caller's borrowed definition");
            TestAssert.Equal("user://knight/knight.first-gate.development.v1.json",level.GetNode<PrologueGameFlowController>("GameFlow").SavePath,
                "PROFILE_SLOT_RED: saved candidate default must not target legacy layout2 slot even without fixture injection");
            TestAssert.True(world.CandidateProfileId==profile.Id && world.LayoutVersion==3 && world.Definitions.Count==1,
                "serialized candidate identity and actual single container");
            TestAssert.True(level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position==new Vector2(192,472),"parent authors canonical spawn before Ready");
            bool oldRejected=false;try{_ = new PrologueSectorStreamModel(definitions);}catch(ArgumentException){oldRejected=true;}
            TestAssert.True(oldRejected,"default five-sector validation not relaxed");
            var map=level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            TestAssert.Equal(98304,map.CellCount,"candidate parent uses actual larger new bounds");
            var markers=level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
            TestAssert.True(markers.Count==2 && markers["knight.first-gate.start"]==new Vector2(192,472) && markers["knight.first-gate.a-refuge"]==new Vector2(1760,856),"saved minimap checkpoint table contains only actual new marker/trigger");
            string path=Path.Combine(storage,"FirstGateEntryCampaign-v340.tscn");
            TestAssert.Equal(Error.Ok,BuildPrologueLevel.SaveCandidate(level,path),"candidate writes new external file only");
            TestAssert.True(ReferenceEquals(definition.Scene,leafScene),"saving candidate never replaces caller's borrowed leaf reference");
            using var saved=ResourceLoader.Load<PackedScene>(path,cacheMode:ResourceLoader.CacheMode.Ignore);
            var copy=saved.Instantiate<PrologueLevelController>();
            try
            {
                TestAssert.Equal(Count(level),Count(copy),"fresh reloaded parent node count");
                var copyWorld=copy.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                TestAssert.True(copyWorld.CandidateProfileId==profile.Id && copyWorld.Definitions.Count==1,"plain profile reconstructed from saved exported key");
                TestAssert.True(copy.GetNode<PrologueMinimapController>("MinimapController").Catalog.SerializedValues.AsSpan().SequenceEqual(map.SerializedValues),"fresh saved map exact geometry bytes");
            }
            finally
            {
                copy.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Clear();
                copy.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;
                copy.Free();
            }
            GD.Print("WORLD_PROFILE_PARENT_OK actual_sectors=1 saved_key=true checkpoints=2 old_five_preserved=true campaign_ready=false");
            return path;
        }
        finally
        {
            var ownedDefinitions=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            var ownedSnapshots=ownedDefinitions.Select(d=>d.Scene).Where(s=>s!=null && !ReferenceEquals(s,leafScene)).Distinct().ToArray();
            level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.Clear();
            level.GetNode<PrologueMinimapController>("MinimapController").Catalog=null;
            foreach(var d in ownedDefinitions)d.Scene=null;
            level.Free();
            foreach(var d in ownedDefinitions)d.Dispose();
            foreach(var snapshot in ownedSnapshots)snapshot.Dispose();
            ownedCatalog.Dispose();
            TestAssert.True(GodotObject.IsInstanceValid(leafScene) && GodotObject.IsInstanceValid(borrowedPlayer) && GodotObject.IsInstanceIdValid(borrowedShape),
                "owned authoring cleanup never disposes shared leaf/Player resources");
        }
    }
    private static int Count(Node root)=>1+root.GetChildren().Sum(Count);
}
