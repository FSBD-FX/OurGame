using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonMechanismRuntimeTests
{
    public static async Task RunMovingGear(SceneTree tree)
    {
        Type type=typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Mechanisms.CanyonMovingGear");
        TestAssert.True(type!=null && typeof(CanyonFixedGear).IsAssignableFrom(type),
            "MOVING_GEAR_RUNTIME_RED: a real moving native circle must retain the fixed gear damage contract");
        int cases=0;
        for(int variant=0;variant<8;variant++)
        {
            var f=new SpringboardFixture(tree);
            try
            {
                f.Board.Position=new(700,500);
                if(variant==1) f.Player.Position=new(60,400); // Controlled empty capsule-corner fixture.
                await f.Mount();
                if(variant==5)
                {
                    f.Driver=KeyboardRouteInputDriver.Begin(f.Player);
                    f.Driver.Command(InputActions.KeyboardShield,true);
                    await ThrusterPlayerRuntimeTests.Frames(tree,f.Player.GetNode<PlayerInputReader>("Input"),3);
                    TestAssert.True(f.Player.GetNode<PlayerShieldController>("Shield").IsActive,
                        "moving gear death control genuinely holds the keyboard shield before the new source exists");
                }
                if(variant==4) f.Root.SetMeta("_seamless_staged",true);
                var gear=(CanyonFixedGear)Activator.CreateInstance(type);
                gear.Name="MovingGear"; gear.Position=new(100,464); gear.Radius=24;
                gear.Damage=variant==5?5:1;
                SetMovingProperty(gear,"TravelOffset",new Vector2(160,0));
                SetMovingProperty(gear,"PeriodSeconds",1f);
                var shape=f.Own(new CircleShape2D { Radius=24 });
                var collider=new CollisionShape2D { Name="Collision",Shape=shape,Disabled=variant==4 };
                gear.AddChild(collider); f.Root.AddChild(gear);
                var body=f.Player.GetNode<CollisionShape2D>("BodyCollider");
                Rect2 actorBounds=body.GlobalTransform*body.Shape.GetRect();
                Rect2 startCircleBounds=new(76,440,48,48);
                if(variant==0)
                {
                    TestAssert.True(!actorBounds.Intersects(startCircleBounds),"sweep control starts outside the native circle");
                    gear._PhysicsProcess(1d); // One complete period: endpoints coincide, visited path does not.
                    TestAssert.True(gear.Position.DistanceTo(new(100,464))<.0001f &&
                        !actorBounds.Intersects(new Rect2(gear.GlobalPosition-new Vector2(24,24),new(48,48))) &&
                        f.Health.CurrentHealth==4 && gear.ContactSequence==1,
                        "MOVING_GEAR_SWEEP_RED: both endpoints miss but the actual native swept capsule must damage the saved body once");
                }
                else if(variant==1)
                {
                    TestAssert.True(actorBounds.Intersects(new Rect2(76,440,208,48)),
                        "empty rounded-corner control really intersects the sweep AABB");
                    gear._PhysicsProcess(1d);
                    TestAssert.True(f.Health.CurrentHealth==5 && gear.ContactSequence==0,
                        "MOVING_GEAR_CORNER_RED: AABB empty corners cannot manufacture native circle-path contact");
                }
                else if(variant==2)
                {
                    int callbacks=0;
                    void OnDamage(int _,Vector2 __)
                    {
                        callbacks++;
                        object before=MovingProperty<object>(gear,"CurrentMotion");
                        gear._PhysicsProcess(.01d);
                        TestAssert.Equal(before,MovingProperty<object>(gear,"CurrentMotion"),
                            "damage callback reentry cannot advance the same gear motion a second time");
                    }
                    f.Health.Damaged+=OnDamage;
                    try
                    {
                        gear._PhysicsProcess(.25d);
                        GD.Print($"MOVING_GEAR_CONTACT_OBS stage=after_quarter hp={f.Health.CurrentHealth} sequence={gear.ContactSequence} callbacks={callbacks} node={gear.GlobalPosition} native={PhysicsServer2D.AreaGetTransform(gear.GetRid()).Origin} valid={gear.IsDangerTelegraphActive}");
                        gear._PhysicsProcess(.01d);
                        TestAssert.True(f.Health.CurrentHealth==4 && gear.ContactSequence==1 && callbacks==1,
                            $"endpoint and swept overlap share one cadence/identity, with normal health invulnerability; hp={f.Health.CurrentHealth} sequence={gear.ContactSequence} callbacks={callbacks} node={gear.GlobalPosition} native={PhysicsServer2D.AreaGetTransform(gear.GetRid()).Origin} valid={gear.IsDangerTelegraphActive}");
                    }
                    finally { f.Health.Damaged-=OnDamage; }
                }
                else if(variant==3)
                {
                    int resets=0;
                    void OnDamage(int _,Vector2 __) { resets++; ((ICheckpointResettable)gear).ResetForCheckpoint(); }
                    f.Health.Damaged+=OnDamage;
                    try { gear._PhysicsProcess(.25d); }
                    finally { f.Health.Damaged-=OnDamage; }
                    TestAssert.True(resets==1 && gear.Position==new Vector2(100,464) &&
                        MovingOffset(gear)==Vector2.Zero && gear.ContactSequence==1,
                        "interface reset during real damage returns to the authored origin and revokes the old segment");
                    gear._PhysicsProcess(.01d);
                    TestAssert.True(f.Health.CurrentHealth==4 && gear.ContactSequence==1,
                        "reset teleport is not a swept attack through the old path");
                }
                else if(variant==4)
                {
                    void Rejected(string why)
                    {
                        object before=MovingProperty<object>(gear,"CurrentMotion");
                        gear._PhysicsProcess(1d);
                        TestAssert.Equal(before,MovingProperty<object>(gear,"CurrentMotion"),why+" cannot advance motion");
                        TestAssert.True(f.Health.CurrentHealth==5 && gear.ContactSequence==0,why+" cannot deliver damage");
                    }
                    Rejected("staged disabled-before-Ready");
                    var reservation=ResidentEnvironmentInventory.Capture(f.Root).Regions
                        .Single(x=>x.SourceId.StartsWith("moving-gear:",StringComparison.Ordinal));
                    TestAssert.True(reservation.Kind==ResidentKeepOutKind.Hazard &&
                        reservation.WorldBounds==new Rect2(76,440,208,48),
                        "moving reservation retains the entire future path even while staged and disabled");
                    f.Root.RemoveMeta("_seamless_staged"); collider.Disabled=false;
                    tree.Paused=true; Rejected("paused"); tree.Paused=false;
                    gear.Visible=false; Rejected("hidden"); gear.Visible=true;
                    gear.Monitoring=false; Rejected("monitoring off"); gear.Monitoring=true;
                    collider.Disabled=true; Rejected("native circle disabled"); collider.Disabled=false;
                    gear.SetPhysicsProcess(false); Rejected("physics processing off"); gear.SetPhysicsProcess(true);
                    gear.Scale=new(2,1); Rejected("non-unit world basis");
                    TestAssert.True(!gear.IsDangerTelegraphActive,"base-typed danger getter respects moving geometry authorization");
                    gear.Scale=Vector2.One;
                    f.Root.Position=new(20,0); Rejected("moved mounted parent anchor"); f.Root.Position=Vector2.Zero;
                    shape.Radius=40; Rejected("changed borrowed native radius"); shape.Radius=24;
                    SetMovingProperty(gear,"TravelOffset",new Vector2(9999,0));
                    SetMovingProperty(gear,"PeriodSeconds",.1f); gear.Radius=99;
                    gear._PhysicsProcess(1d);
                    TestAssert.True(f.Health.CurrentHealth==4 && gear.DangerRadius==24 && gear.ContactSequence==1,
                        "post-Ready export edits do not partially reconfigure the validated motion/radius snapshots");
                    object beforeQueue=MovingProperty<object>(gear,"CurrentMotion");
                    f.Root.QueueFree(); gear._PhysicsProcess(1d);
                    TestAssert.Equal(beforeQueue,MovingProperty<object>(gear,"CurrentMotion"),"queued owner revokes motion immediately");
                    TestAssert.True(f.Health.CurrentHealth==4 && gear.ContactSequence==1,"queued source cannot replay the old sweep");
                }
                else if(variant==5)
                {
                    int deaths=0;
                    void OnDeath()=>deaths++;
                    f.Health.Died+=OnDeath;
                    try
                    {
                        gear._PhysicsProcess(1d); gear._PhysicsProcess(1d);
                        TestAssert.True(f.Health.CurrentHealth==0 && deaths==1 && gear.ContactSequence==1,
                            "native moving contact remains unblockable and emits one death, not direct HP assignment");
                    }
                    finally { f.Health.Died-=OnDeath; }
                }
                else if(variant==6)
                {
                    await Frames(tree,18);
                    TestAssert.True(gear.Position.X>100 && gear.Position.X<260 && gear.ContactSequence==1 &&
                        f.Health.CurrentHealth==4 && MovingOffset(gear).X>0,
                        "ordinary engine physics really advances the new circle and reaches the saved player");
                }
                else
                {
                    gear._PhysicsProcess(.25d);
                    ulong previous=gear.ContactSequence;
                    f.Root.RemoveChild(gear);
                    TestAssert.True(GodotObject.IsInstanceValid(shape),"exit never disposes the borrowed authored collider shape");
                    f.Root.AddChild(gear);
                    TestAssert.True(gear.Position==new Vector2(100,464) && MovingOffset(gear)==Vector2.Zero &&
                        gear.IsDangerTelegraphActive,"same-instance reentry reinitializes owned queries without retaining an old segment");
                    gear._PhysicsProcess(1d);
                    TestAssert.Equal(previous+1,gear.ContactSequence,"same-instance reentry preserves monotonic attack identity");
                }
                cases++;
            }
            finally { tree.Paused=false; await f.DisposeAsync(); }
        }
        TestAssert.Equal(8,cases,"all native moving-gear controls execute");
        await MovingGearMultiShapeContact(tree);
        await MovingGearDamageCallbackRevocation(tree);
        GD.Print("MOVING_GEAR_RUNTIME_OK cases=8 swept_native=true full_cycle=true empty_corner=true single_consumer=true reset_pause_queue=true reentry=true saved_player=true automatic_physics=true normal_route=false human=false");
    }

    private static CanyonMovingGear AddMovingContactGear(SpringboardFixture fixture)
    {
        var gear=new CanyonMovingGear { Name="CallbackMovingGear",Position=new(100,464),Radius=24,
            TravelOffset=new(160,0),PeriodSeconds=1,Damage=1 };
        gear.AddChild(new CollisionShape2D { Name="Collision",Shape=fixture.Own(new CircleShape2D { Radius=24 }) });
        fixture.Root.AddChild(gear);
        return gear;
    }

    private static Dictionary<ulong,HashSet<int>> ObserveMovingContactShapes(CharacterBody2D player,bool fullPeriod)
    {
        // Independent literals: the r24 circle visits x=100..180 for .25s,
        // x=100..260 for 1s. Capsule height is segment length plus diameter.
        using var capsule=new CapsuleShape2D { Radius=24,Height=fullPeriod?208:128 };
        using var query=new PhysicsShapeQueryParameters2D
        {
            Shape=capsule,Transform=new Transform2D(-Mathf.Pi/2,new Vector2(fullPeriod?180:140,464)),
            CollisionMask=CombatCollisionLayers.Player,CollideWithBodies=true,CollideWithAreas=false,Margin=0
        };
        using Godot.Collections.Array hits=(Godot.Collections.Array)player.GetWorld2D().DirectSpaceState.IntersectShape(query,32);
        TestAssert.True(hits.Count<32,"moving contact preflight must be complete, not a truncated hit list");
        var shapes=new Dictionary<ulong,HashSet<int>>();
        for(int index=0;index<hits.Count;index++)
        {
            using var hit=hits[index].AsGodotDictionary();
            if(hit["collider"].AsGodotObject() is not CharacterBody2D body) continue;
            ulong id=body.GetInstanceId();
            if(!shapes.TryGetValue(id,out var indices)) shapes.Add(id,indices=new HashSet<int>());
            indices.Add(hit["shape"].AsInt32());
        }
        return shapes;
    }

    private static async Task MovingGearMultiShapeContact(SceneTree tree)
    {
        var f=new SpringboardFixture(tree);
        try
        {
            f.Board.Position=new(700,500); f.Health.InvulnerabilitySeconds=0;
            foreach(int direction in new[]{-1,1})
                f.Player.AddChild(new CollisionShape2D { Name="ExtraBody"+direction,Position=new(direction*5,0),
                    Shape=f.Own(new RectangleShape2D { Size=new(8,16) }) });
            await f.Mount();
            TestAssert.True(f.Health.CurrentHealth==5 && f.Player.IsOnFloor(),"multi-shape uses the healthy supported saved player");
            var hits=ObserveMovingContactShapes(f.Player,false);
            TestAssert.True(hits.Count==1 && hits.TryGetValue(f.Player.GetInstanceId(),out var indices) && indices.Count>=3,
                "native quarter-path query really contains three distinct shape indices for one saved Player ID");
            var gear=AddMovingContactGear(f); int damaged=0,contacted=0;
            f.Health.Damaged+=(_,_)=>damaged++; gear.Contacted+=(_,_)=>contacted++;
            gear._PhysicsProcess(.25d); gear._PhysicsProcess(.01d);
            TestAssert.True(f.Health.CurrentHealth==4 && damaged==1 && contacted==1 && gear.ContactSequence==1,
                "MOVING_GEAR_MULTI_SHAPE_RED: three actual body shapes must spend one cadence and deliver one real damage without iframe masking");
            GD.Print("MOVING_GEAR_MULTI_SHAPE_OK native_shape_indices=3 minimum=true hp=4 damage_events=1 contact_events=1 sequence=1 invulnerability=0 saved_player=true human=false");
        }
        finally { await f.DisposeAsync(); }
    }

    private static async Task MovingGearDamageCallbackRevocation(SceneTree tree)
    {
        string[] modes={"live","queue","remove","hide","disable-collider","stop-physics","pause","monitoring-off","pause-contact"};
        var failures=new List<string>();
        foreach(string mode in modes)
        {
            var f=new SpringboardFixture(tree); CanyonMovingGear gear=null;
            try
            {
                f.Board.Position=new(700,500); f.Health.InvulnerabilitySeconds=0;
                var second=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
                second.Name="SecondSavedPlayer"; second.Position=new(230,464);
                second.GetNode<Camera2D>("Camera2D").Enabled=false;
                var otherHealth=second.GetNode<HealthComponent>("Health"); otherHealth.InvulnerabilitySeconds=0;
                bool pauseMode=mode is "pause" or "pause-contact";
                if(pauseMode) { f.Player.ProcessMode=Node.ProcessModeEnum.Always; second.ProcessMode=Node.ProcessModeEnum.Always; }
                second.AddToGroup("player"); f.Root.AddChild(second);
                await f.Mount();
                TestAssert.True(f.Health.CurrentHealth==5 && otherHealth.CurrentHealth==5 &&
                    f.Player.IsOnFloor() && second.IsOnFloor(),"callback fixture has two healthy supported real saved players");
                var hits=ObserveMovingContactShapes(f.Player,true);
                TestAssert.True(hits.Count==2 && hits.ContainsKey(f.Player.GetInstanceId()) && hits.ContainsKey(second.GetInstanceId()),
                    "native full-period query pre-proves both different saved Player IDs, without assuming hit order");
                gear=AddMovingContactGear(f); int damaged=0,contacted=0;
                if(pauseMode) gear.ProcessMode=Node.ProcessModeEnum.Always;
                void OnDamage(int _,Vector2 __)
                {
                    damaged++;
                    if(damaged!=1) return;
                    switch(mode)
                    {
                        case "queue": gear.QueueFree(); break;
                        case "remove": f.Root.RemoveChild(gear); break;
                        case "hide": gear.Visible=false; break;
                        case "disable-collider": gear.GetNode<CollisionShape2D>("Collision").Disabled=true; break;
                        case "stop-physics": gear.SetPhysicsProcess(false); break;
                        case "pause":
                            tree.Paused=true;
                            TestAssert.True(f.Player.CanProcess() && second.CanProcess() && gear.CanProcess(),
                                "Always controls must not mask the explicit source pause gate");
                            break;
                        case "monitoring-off": gear.Monitoring=false; break;
                    }
                }
                f.Health.Damaged+=OnDamage; otherHealth.Damaged+=OnDamage;
                gear.Contacted+=(_,_)=>
                {
                    contacted++;
                    if(mode!="pause-contact" || contacted!=1) return;
                    tree.Paused=true;
                    TestAssert.True(f.Player.CanProcess() && second.CanProcess() && gear.CanProcess(),
                        "pause from Contacted must not rely on paused body filtering");
                };
                try
                {
                    gear._PhysicsProcess(1d);
                    bool live=mode=="live";
                    int total=f.Health.CurrentHealth+otherHealth.CurrentHealth;
                    GD.Print($"MOVING_GEAR_CALLBACK_CASE mode={mode} first_hp={f.Health.CurrentHealth} second_hp={otherHealth.CurrentHealth} total={total} damage_events={damaged} contact_events={contacted} sequence={gear.ContactSequence} actual_two_bodies=true human=false");
                    int expectedContacts=live?2:mode=="pause-contact"?1:0;
                    bool correct=live
                        ? f.Health.CurrentHealth==4 && otherHealth.CurrentHealth==4 && damaged==2 && contacted==2 && gear.ContactSequence==2
                        : total==9 && Math.Min(f.Health.CurrentHealth,otherHealth.CurrentHealth)==4 && damaged==1 && contacted==expectedContacts && gear.ContactSequence==1;
                    if(!correct) failures.Add(mode+": callback batch/contact publication");
                    if(!live)
                    {
                        int beforeTotal=f.Health.CurrentHealth+otherHealth.CurrentHealth;
                        int beforeDamaged=damaged,beforeContacted=contacted;
                        ulong beforeSequence=gear.ContactSequence;
                        gear._PhysicsProcess(1d);
                        if(f.Health.CurrentHealth+otherHealth.CurrentHealth!=beforeTotal || damaged!=beforeDamaged ||
                            contacted!=beforeContacted || gear.ContactSequence!=beforeSequence)
                            failures.Add(mode+": revoked full-period replay");
                    }
                    if(mode=="queue") TestAssert.True(gear.IsQueuedForDeletion(),"queue revokes synchronously before deferred free");
                    if(mode=="remove") TestAssert.True(!gear.IsInsideTree(),"remove revokes while the orphan remains valid");
                }
                finally { f.Health.Damaged-=OnDamage; otherHealth.Damaged-=OnDamage; }
            }
            finally
            {
                tree.Paused=false;
                // Removed gear is not owned by the fixture tree any more. Free it
                // before f releases the authored shape; queued gear stays deferred.
                if(GodotObject.IsInstanceValid(gear) && !gear.IsInsideTree() && !gear.IsQueuedForDeletion()) gear.Free();
                await f.DisposeAsync();
            }
        }
        TestAssert.True(failures.Count==0,"MOVING_GEAR_CALLBACK_REVOKE_RED: "+string.Join("; ",failures));
        GD.Print("MOVING_GEAR_CALLBACK_REVOKE_OK cases=9 native_two_players=true order_independent=true invulnerability=0 real_damage=true replay_rejected=true always_pause_control=true human=false");
    }

    public static async Task RunMovingGearFog(SceneTree tree)
    {
        var world=new Node2D { Name="MovingGearFogFixture" };
        var sector=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn").Instantiate<PrologueSector>();
        var type=typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Mechanisms.CanyonMovingGear");
        var gear=(CanyonFixedGear)Activator.CreateInstance(type);
        gear.Name="MovingGear"; gear.Position=new(180,500); gear.Radius=24; gear.Damage=1;
        SetMovingProperty(gear,"TravelOffset",new Vector2(80,0)); SetMovingProperty(gear,"PeriodSeconds",1f);
        using var shape=new CircleShape2D { Radius=24 };
        using var wallShape=new RectangleShape2D { Size=new(300,8) };
        var collision=new CollisionShape2D { Name="Collision",Shape=shape };
        gear.AddChild(collision); sector.GetNode("Gameplay/Mechanisms").AddChild(gear);
        var overlay=new FogRecyclingReadabilityOverlay();
        var layer=new CanvasLayer { Layer=2,FollowViewportEnabled=true };
        HeadlampOcclusionSampler sampler=null;
        HeadlampOcclusionFrame lastComplete=null;
        try
        {
            world.AddChild(sector); layer.AddChild(overlay); world.AddChild(layer); tree.Root.AddChild(world);
            await Frames(tree,2); sampler=new HeadlampOcclusionSampler(world);
            var sources=new CollisionObject2D[]{gear,gear};
            void Present(Vector2? actor=null)
            {
                var frame=sampler.Capture(); TestAssert.True(frame.IsComplete,"moving Area does not turn the World rectangle snapshot incomplete");
                lastComplete=frame;
                overlay.PresentSources(sources,actor??new Vector2(200,580),frame);
            }
            void Positive() { Present(); TestAssert.True(overlay.PresentedGearCount==1 && overlay.PresentedSegmentCount>0,
                "canonical same-layer moving subtype submits deduplicated current-circle cues"); }
            Positive(); Vector2 before=gear.GlobalPosition; await Frames(tree,8); Positive();
            TestAssert.True(gear.GlobalPosition.DistanceTo(before)>1,"fog control observes an actually moving native Area");
            gear.Visible=false; Present(); TestAssert.Equal(0,overlay.PresentedGearCount,"hidden moving cue clears"); gear.Visible=true;
            collision.Disabled=true; await Frames(tree,2); Present(); TestAssert.Equal(0,overlay.PresentedGearCount,"disabled moving cue clears");
            collision.Disabled=false; await Frames(tree,2); Positive();
            var wall=new StaticBody2D { Position=new(220,548),CollisionLayer=CombatCollisionLayers.World };
            wall.AddChild(new CollisionShape2D { Shape=wallShape }); world.AddChild(wall);
            await Frames(tree,2); Present(); TestAssert.Equal(0,overlay.PresentedGearCount,"real wall hides the entire current circle, not just its original position");
            wall.QueueFree(); await Frames(tree,2); Positive();
            Present(new Vector2(2000,580)); TestAssert.Equal(0,overlay.PresentedSegmentCount,"distant track cannot reveal a room");
            sector.SetMeta("_seamless_staged",true); Present(); TestAssert.Equal(0,overlay.PresentedSegmentCount,"staged cue clears");
            sector.RemoveMeta("_seamless_staged"); Positive();
            tree.Paused=true; Present(); TestAssert.Equal(0,overlay.PresentedSegmentCount,"pause removes every moving cue");
            tree.Paused=false; Positive(); world.QueueFree();
            TestAssert.True(!sampler.Capture().IsComplete,"a queued world correctly revokes new snapshot authorization");
            overlay.PresentSources(sources,new Vector2(200,580),lastComplete);
            TestAssert.Equal(0,overlay.PresentedSegmentCount,"queued ancestors withdraw all source commands");
            GD.Print("MOVING_GEAR_FOG_OK canonical_subtype=true actual_motion=true deduplicated=true wall_range=true hidden_disabled=true pause_staged_queue=true pixels=false controller_saved_map=false human=false");
        }
        finally
        {
            tree.Paused=false; sampler?.Dispose();
            if(GodotObject.IsInstanceValid(world) && !world.IsQueuedForDeletion()) world.QueueFree();
            await Frames(tree,2);
        }
    }

    private static T MovingProperty<T>(CanyonFixedGear gear,string name)
    {
        var property=gear.GetType().GetProperty(name);
        TestAssert.True(property!=null,"MOVING_GEAR_API_RED: missing scalar property "+name);
        return (T)property.GetValue(gear);
    }
    private static Vector2 MovingOffset(CanyonFixedGear gear)
    {
        object frame=MovingProperty<object>(gear,"CurrentMotion");
        return (Vector2)frame.GetType().GetProperty("Offset").GetValue(frame);
    }
    private static void SetMovingProperty(CanyonFixedGear gear,string name,object value)
    {
        var property=gear.GetType().GetProperty(name);
        TestAssert.True(property!=null,"MOVING_GEAR_API_RED: missing authoring property "+name);
        property.SetValue(gear,value);
    }
}
