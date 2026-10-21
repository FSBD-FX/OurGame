using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Enemies.Residents;
using Knight.Prologue.Seamless;
using Knight.World;

namespace Knight.Tests.Integration;

// Real saved definitions, staging, registry and retirement. Fixed native query
// probes are not AI motion, legal residence placements or player route proof.
public static class ResidentVoidStreamRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var f=new Fixture(tree);var failures=new List<string>();int cases=0;
        try
        {
            f.Initialize();SeamlessPrologueWorld world=f.AddWorld();await f.Frames(3);f.AssertHealthy();
            TestAssert.True(world.MountedSectorIds.SequenceEqual(new[]{PrologueSectorId.CanyonDescent,PrologueSectorId.CanyonBasin,PrologueSectorId.CanyonAscent}),
                "actual saved Basin definition mounts Descent/Basin/Ascent, not a synthetic registry");
            PrologueSector oldDescent=f.Sector(world,PrologueSectorId.CanyonDescent);
            ulong oldId=oldDescent.GetInstanceId();
            Probe survivor=f.Probe(f.Sector(world,PrologueSectorId.CanyonAscent),new(1450,3640));
            Probe retiring=f.Probe(oldDescent,new(1450,3640));
            CollisionShape2D source=oldDescent.GetNode<CollisionShape2D>("Gameplay/FallRecovery/RecoveryVolume/Collision");
            Rect2 sourceBounds=source.GlobalTransform*source.Shape.GetRect();
            TestAssert.True(sourceBounds==new Rect2(1280,3584,336,16),"actual saved16px source bounds after real staging");
            TestAssert.True(!source.Disabled&&source.GetParent() is KillZone {Monitoring:true},"source active after real world restoration");
            TestAssert.True(!survivor.Shape.Shape.Collide(survivor.Before,source.Shape,source.GlobalTransform)&&
                !survivor.Shape.Shape.Collide(survivor.Shape.GlobalTransform,source.Shape,source.GlobalTransform)&&
                survivor.Shape.Shape.CollideWithMotion(survivor.Before,new(0,100),source.Shape,source.GlobalTransform,Vector2.Zero),
                "independent native geometry precondition: clear endpoints, actual source swept between them");
            Check("registered_cross_sector_source_has_authority",()=>TestAssert.True(Cross(survivor)&&Cross(retiring),
                "same real world source can affect a probe whose owning sector differs"));
            Check("staged_source_has_no_authority",()=>
            {
                oldDescent.SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true);
                try {TestAssert.True(!Cross(survivor),"staged source rejected while native geometry and registry stay identical");}
                finally {oldDescent.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);}
                TestAssert.True(Cross(survivor),"removing fault restores same source authority");
            });
            Check("staged_actor_owner_has_no_authority",()=>
            {
                PrologueSector ascent=f.Sector(world,PrologueSectorId.CanyonAscent);
                ascent.SetMeta(SeamlessPrologueWorld.StagedSectorMeta,true);
                try {TestAssert.True(!Cross(survivor),"staged actor owner cannot consume another registered source");}
                finally {ascent.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta);}
                TestAssert.True(Cross(survivor),"actor owner reactivation restores authority");
            });
            PrologueSector copy=f.AddUnregisteredDescent(world,new(41280,0));
            Probe nearCopy=f.Probe(f.Sector(world,PrologueSectorId.CanyonAscent),new(41450,3640));
            Probe ownedByCopy=f.Probe(copy,new(1450,3640));
            CollisionShape2D copyShape=copy.GetNode<CollisionShape2D>("Gameplay/FallRecovery/RecoveryVolume/Collision");
            TestAssert.True(copyShape.Shape.CollideWithMotion(copyShape.GlobalTransform,Vector2.Zero,nearCopy.Shape.Shape,nearCopy.Before,new(0,100)),
                "unregistered copy really geometrically intersects separate native probe");
            Check("unregistered_same_id_source_has_no_authority",()=>TestAssert.True(!Cross(nearCopy),
                "same SectorId/parent/world but different registry instance cannot supply fatal source"));
            Check("unregistered_same_id_actor_has_no_authority",()=>TestAssert.True(!Cross(ownedByCopy),
                "unregistered actor ancestor cannot consume registered original geometry"));
            copy.QueueFree();await f.Frames(2);

            await f.Go(world,PrologueSectorId.LostTemple,new(5920,1080));
            Check("source_unload_with_actor_owner_kept_withdraws_authority",()=>
            {
                TestAssert.True(GodotObject.IsInstanceValid(survivor.Body)&&survivor.Body.IsInsideTree()&&
                    ReferenceEquals(survivor.Body.GetParent().GetParent().GetParent(),f.Sector(world,PrologueSectorId.CanyonAscent)),
                    "probe's original Ascent actor owner remains actually mounted");
                TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonDescent,out _)&&!Cross(survivor),
                    "actual Descent unload withdraws source, not just actor deletion");
            });
            Check("unloaded_actor_query_is_safe_and_rejected",()=>TestAssert.True(!Cross(retiring),
                "old actor/body references have no authority after real retirement"));
            await f.Go(world,PrologueSectorId.CanyonBasin,new(1680,3960));
            Check("same_sector_id_replacement_is_not_old_instance",()=>
            {
                PrologueSector replacement=f.Sector(world,PrologueSectorId.CanyonDescent);
                TestAssert.True(replacement.GetInstanceId()!=oldId&&Cross(survivor)&&!Cross(retiring),
                    "real new Descent restores source but never revives old probe reference");
            });
            await f.Go(world,PrologueSectorId.LostTemple,new(5920,1080));
            SeamlessPrologueWorld other=f.AddWorld();await f.Frames(3);f.AssertHealthy();
            Probe foreign=f.Probe(f.Sector(other,PrologueSectorId.CanyonAscent),new(1450,3640));
            Check("same_physical_world_different_stream_world_isolated",()=>TestAssert.True(
                world.GetWorld2D()==other.GetWorld2D()&&!ReferenceEquals(world,other)&&Cross(foreign)&&!Cross(survivor),
                "foreign registered source is geometrically live but cannot cross logical streamed-world identity"));
            var isolated=new Fixture(tree);
            try
            {
                isolated.Initialize();SeamlessPrologueWorld far=isolated.AddWorld();await isolated.Frames(3);isolated.AssertHealthy();
                Probe physical=isolated.Probe(isolated.Sector(far,PrologueSectorId.CanyonAscent),new(1450,3640));
                Check("foreign_physical_world_does_not_restore_missing_source",()=>TestAssert.True(
                    far.GetWorld2D()!=world.GetWorld2D()&&Cross(physical)&&!Cross(survivor),
                    "foreign physical sources also cannot replace an unloaded own source"));
            }
            finally {await isolated.DisposeAsync();}
            f.AssertHealthy();f.AssertBorrowedUnchanged();
            GD.Print($"RESIDENT_VOID_STREAM_RESULT cases={cases} failures={failures.Count} real_registry=true saved_definitions=5 npc_motion=false formal_spawned=0");
            TestAssert.True(failures.Count==0,string.Join(" | ",failures));
        }
        catch(Exception error){GD.Print("RESIDENT_VOID_STREAM_OPERATION_FAILURE "+error);throw;}
        finally {await f.DisposeAsync();}

        void Check(string name,Action body)
        {
            cases++;try {body();GD.Print("RESIDENT_VOID_STREAM_CASE_OK "+name);}
            catch(Exception error){failures.Add(name+": "+error.Message);GD.Print("RESIDENT_VOID_STREAM_CASE_FAIL "+name+": "+error.Message);}
        }
    }
    private static bool Cross(Probe probe)=>ResidentVoidContact.Crossed(probe.Body,probe.Shape,probe.Before);
    private sealed record Probe(CharacterBody2D Body,CollisionShape2D Shape,Transform2D Before);

    private sealed class Fixture
    {
        private readonly SceneTree _tree;
        private World2D _physics;
        private SubViewport _viewport;
        private Node2D _scope;
        private readonly List<SeamlessPrologueWorld> _worlds=new();
        private readonly List<PrologueSectorDefinition> _definitions=new();
        private readonly List<Godot.Collections.Array> _arrays=new();
        private readonly List<Shape2D> _ownedShapes=new();
        private readonly List<(Shape2D shape,Rect2 bounds)> _borrowed=new();
        private readonly HashSet<ulong> _captured=new();
        private readonly List<Node> _detached=new();
        private readonly List<string> _observerErrors=new();
        private bool _subscribed;
        private int _beforeReady,_mounted;
        public Fixture(SceneTree tree){_tree=tree;}
        public void Initialize()
        {
            _physics=new World2D();
            _viewport=new SubViewport{Name="ResidentLiveStreamWorld",Size=new(32,32),World2D=_physics};
            _scope=new Node2D();_viewport.AddChild(_scope);
            _tree.NodeAdded+=OnNodeAdded;_subscribed=true;_tree.Root.AddChild(_viewport);
        }
        public SeamlessPrologueWorld AddWorld()
        {
            var root=new Node2D{Name="MetadataOnlyRoot"+_worlds.Count};_detached.Add(root);
            var playerRoot=new Node2D{Name="PlayerRoot"};
            var anchor=new CharacterBody2D{Name="Player",Position=new(1680,3960),CollisionLayer=0,CollisionMask=0};
            playerRoot.AddChild(anchor);root.AddChild(playerRoot);
            var worldRoot=new Node2D{Name="WorldRoot"};
            var world=new SeamlessPrologueWorld{Name="Sectors",InitialSector=PrologueSectorId.CanyonBasin,LayoutVersion=2,
                PlayerPath=new NodePath("../../PlayerRoot/Player")};
            // Register the entire detached ownership graph before any fallible
            // metadata read; a failure must not strand an unparented world.
            worldRoot.AddChild(world);root.AddChild(worldRoot);
            _worlds.Add(world);_arrays.Add((Godot.Collections.Array)world.Definitions);
            world.SectorMounted+=OnMounted;
            // Read the saved export itself. No disposable whole-level donor is
            // instantiated merely to obtain metadata; sector Ready stays real.
            using SceneState state=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").GetState();
            var sources=new List<PrologueSectorDefinition>();
            for(int node=0;node<state.GetNodeCount();node++)
            {
                using NodePath path=state.GetNodePath(node);
                if(path.ToString() is not ("WorldRoot/Sectors" or "./WorldRoot/Sectors"))continue;
                for(int property=0;property<state.GetNodePropertyCount(node);property++)
                {
                    using StringName name=state.GetNodePropertyName(node,property);
                    if(name.ToString()!="Definitions")continue;
                    using Variant value=state.GetNodePropertyValue(node,property);
                    using Godot.Collections.Array values=value.AsGodotArray();
                    for(int i=0;i<values.Count;i++)sources.Add(values[i].AsGodotObject() as PrologueSectorDefinition);
                }
            }
            TestAssert.True(sources.Count==5&&sources.All(x=>x!=null)&&sources.Select(x=>x.Id).Distinct().Count()==5,
                "actual saved five-definition export read without whole-level instantiation/save flow");
            foreach(PrologueSectorDefinition source in sources)
            {
                var copy=new PrologueSectorDefinition{Id=source.Id,Scene=source.Scene,WorldOffset=source.WorldOffset,
                    Bounds=source.Bounds,PreviousValue=source.PreviousValue,NextValue=source.NextValue};
                _definitions.Add(copy);_arrays.Add((Godot.Collections.Array)copy.NeighborValues);_arrays.Add((Godot.Collections.Array)copy.KeyInteractionBounds);
                foreach(int neighbor in source.NeighborValues)copy.NeighborValues.Add(neighbor);
                foreach(Rect2 area in source.KeyInteractionBounds)copy.KeyInteractionBounds.Add(area);
                TestAssert.True(copy.NeighborValues.SequenceEqual(source.NeighborValues)&&copy.KeyInteractionBounds.SequenceEqual(source.KeyInteractionBounds),
                    "copy preserves explicit nonlinear neighbors and interaction ordering");
                world.Definitions.Add(copy);
            }
            _scope.AddChild(root);_detached.Remove(root);AssertHealthy();
            return world;
        }
        public PrologueSector Sector(SeamlessPrologueWorld world,PrologueSectorId id)
        {
            TestAssert.True(world.TryGetMountedSector(id,out PrologueSector sector),"actual registry contains "+id);return sector;
        }
        public Probe Probe(PrologueSector owner,Vector2 end)
        {
            var shape=new CapsuleShape2D{Radius=18,Height=64};_ownedShapes.Add(shape);
            var actor=new CharacterBody2D{Name="AuthorityProbe",Position=end-owner.GlobalPosition,CollisionLayer=0,CollisionMask=0};
            _detached.Add(actor);var collider=new CollisionShape2D{Name="BodyCollider",Shape=shape};actor.AddChild(collider);
            owner.GetNode("Gameplay/Actors").AddChild(actor);_detached.Remove(actor);
            TestAssert.True(actor.GlobalPosition.DistanceTo(end)<.001f,"native fixed probe has literal world endpoint before querying");
            return new Probe(actor,collider,new Transform2D(0,end-new Vector2(0,100)));
        }
        public PrologueSector AddUnregisteredDescent(SeamlessPrologueWorld world,Vector2 position)
        {
            var copy=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
            _detached.Add(copy);copy.Position=position;CloneShapes(copy);_captured.Add(copy.GetInstanceId());
            world.AddChild(copy);_detached.Remove(copy);Freeze(copy);AssertHealthy();return copy;
        }
        public async Task Go(SeamlessPrologueWorld world,PrologueSectorId id,Vector2 position)
        {
            TestAssert.True(world.RestoreCheckpoint(id,position),"actual public transition accepted");
            await Frames(4);AssertHealthy();
            TestAssert.True(world.CurrentSector==id&&world.RetiredSectorCount==0,"real transition/retirement completed, not mocked registry");
        }
        public async Task Frames(int count)
        {
            for(int i=0;i<count;i++){await _tree.ToSignal(_tree,SceneTree.SignalName.PhysicsFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);}
        }
        private void OnNodeAdded(Node node)
        {
            if(node is not PrologueSector sector||!_worlds.Any(x=>ReferenceEquals(sector.GetParent(),x))||_captured.Contains(sector.GetInstanceId()))return;
            try
            {
                TestAssert.True(sector.IsInsideTree()&&!sector.IsNodeReady()&&sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta),
                    "actual staged root captured before Ready, no guessed event timing");
                CloneShapes(sector);_captured.Add(sector.GetInstanceId());_beforeReady++;
            }
            catch(Exception error){_observerErrors.Add("NodeAdded: "+error.Message);}
        }
        private void OnMounted(PrologueSector sector)
        {
            try
            {
                var world=(SeamlessPrologueWorld)sector.GetParent();
                TestAssert.True(_captured.Contains(sector.GetInstanceId())&&sector.IsNodeReady()&&
                    !sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)&&world.TryGetMountedSector(sector.SectorId,out PrologueSector actual)&&ReferenceEquals(actual,sector),
                    "SectorMounted after real Ready/stage withdrawal/registry identity");
                Freeze(sector);_mounted++;
            }
            catch(Exception error){_observerErrors.Add("SectorMounted: "+error.Message);}
        }
        private void CloneShapes(Node root)
        {
            foreach(CollisionShape2D collider in Walk(root).OfType<CollisionShape2D>().Where(x=>x.Shape!=null))
            {
                _borrowed.Add((collider.Shape,collider.Shape.GetRect()));
                Shape2D copy=(Shape2D)collider.Shape.Duplicate();_ownedShapes.Add(copy);
                TestAssert.True(copy.GetRect()==collider.Shape.GetRect(),"owned shape duplicate preserves source geometry");
                collider.Shape=copy;
            }
        }
        private static void Freeze(Node root){foreach(Node child in Walk(root)){child.SetProcess(false);child.SetPhysicsProcess(false);}}
        private static IEnumerable<Node> Walk(Node root)
        {
            yield return root;using Godot.Collections.Array children=(Godot.Collections.Array)root.GetChildren();
            for(int i=0;i<children.Count;i++)foreach(Node child in Walk(children[i].AsGodotObject() as Node))yield return child;
        }
        public void AssertHealthy()=>TestAssert.True(_observerErrors.Count==0,string.Join(" | ",_observerErrors));
        public void AssertBorrowedUnchanged()
        {
            TestAssert.True(_borrowed.All(x=>x.shape.GetRect()==x.bounds),"Ready/remount never mutates borrowed saved shapes");
            TestAssert.True(_beforeReady>=3&&_mounted==_beforeReady,"every real streamed instance captured before Ready and registered exactly once");
            GD.Print($"RESIDENT_VOID_STREAM_FIXTURE staged_captures={_beforeReady} mounts={_mounted} borrowed_unchanged=true no_player_group=true no_save_flow=true");
        }
        public async Task DisposeAsync()
        {
            if(_subscribed){_tree.NodeAdded-=OnNodeAdded;_subscribed=false;}
            foreach(SeamlessPrologueWorld world in _worlds)if(GodotObject.IsInstanceValid(world))world.SectorMounted-=OnMounted;
            foreach(Node detached in _detached)if(GodotObject.IsInstanceValid(detached))detached.Free();_detached.Clear();
            if(GodotObject.IsInstanceValid(_viewport)){if(_viewport.IsInsideTree())_viewport.QueueFree();else _viewport.Free();}
            await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);await _tree.ToSignal(_tree,SceneTree.SignalName.ProcessFrame);
            foreach(SeamlessPrologueWorld world in _worlds)TestAssert.True(!GodotObject.IsInstanceValid(world),"owned world/retired graph actually freed before resources");
            foreach(Godot.Collections.Array array in _arrays)array.Clear();
            foreach(PrologueSectorDefinition definition in _definitions){definition.Scene=null;definition.Dispose();}
            foreach(Godot.Collections.Array array in _arrays)array.Dispose();
            int ownedCount=_ownedShapes.Count,borrowedUnique=_borrowed.Select(x=>x.shape.GetInstanceId()).Distinct().Count();
            foreach(Shape2D shape in _ownedShapes)shape.Dispose();
            TestAssert.True(_ownedShapes.All(x=>!GodotObject.IsInstanceValid(x)),"every explicitly owned shape wrapper disposed");
            GD.Print($"RESIDENT_VOID_STREAM_OWNERSHIP owned_disposed={ownedCount} borrowed_unique={borrowedUnique} borrowed_disposed=0");
            _arrays.Clear();_definitions.Clear();_ownedShapes.Clear();_borrowed.Clear();_worlds.Clear();_physics?.Dispose();
        }
    }
}
