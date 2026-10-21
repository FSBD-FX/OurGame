using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Ui;
using Knight.Tests.Presentation;
using Knight.Shared;
using Knight.Enemies.Laborer;

namespace Knight.Tests.Integration;

/// <summary>Saved Basin outcomes. No fixture activation, clock advancing, healing, or post-mount placement.</summary>
public static class BasinCounterweightRuntimeTests
{
    public static async Task Run(SceneTree tree, BasinCounterweightPhase phase)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (phase == BasinCounterweightPhase.Contract) await Contract(tree);
        else if (phase == BasinCounterweightPhase.RouteContract)
        {
            var f=new Fixture(tree,new Vector2(2492,3880),full:true);
            try { await f.Start(); PrologueCanyonGeneratedMainRouteTests.AssertTask10GearPistonOneContract(f.Sector); }
            finally { await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.RoutePhysicsCadence)
        {
            var f=new Fixture(tree,new Vector2(2492,3880),full:true);
            try
            {
                await f.Start();
                using var driver=KeyboardRouteInputDriver.Begin(f.Player);
                PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,f.Player,driver.ObserveFrame);
                try
                {
                    GD.Print("BASIN_ROUTE_PHYSICS_CADENCE diagnostic_only=true cadence=PhysicsFrame_then_Observe controlled_initial_spawn=true full_main=false human=false");
                    await TraverseForFullRoute(f.Player,f.Sector,async count =>
                    {
                        for(int i=0;i<count;i++)
                        {
                            // Match Main's PhysicsFrames -> Observe boundary.
                            // Do not add the old local fixture's ProcessFrame wait.
                            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                            driver.ObserveFrame();
                        }
                    });
                    TestAssert.True(f.Lift.IsActivated,"BasinFullRouteRequiresPassengerLift: route cannot skip the approved lift");
                }
                finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
            }
            finally { await f.Dispose(); }
        }
        else if (phase is BasinCounterweightPhase.RouteTransfer or BasinCounterweightPhase.RouteEncounter)
        {
            bool encounter=phase==BasinCounterweightPhase.RouteEncounter;
            var f = new Fixture(tree,new Vector2(encounter?2460:2492,3880),full:true);
            try
            {
                await f.Start();
                using var driver=KeyboardRouteInputDriver.Begin(f.Player);
                PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,f.Player,driver.ObserveFrame);
                try
                {
                    var group=f.Sector.FindChildren("*","",true,false).OfType<CanyonEncounterGroup>()
                        .Single(g=>g.EncounterId.ToString()=="canyon.v2.encounter.gear-pistons");
                    if(encounter) await f.Until(()=>group.IsActive && !group.IsArrivalWarningActive,60,"actual location activates real gear enemies");
                    ulong[] actorIds=group.GetNode(group.ActorRootPath).GetChildren().OfType<CharacterBody2D>()
                        .Select(actor=>actor.GetInstanceId()).OrderBy(id=>id).ToArray();
                    if(encounter) TestAssert.True(actorIds.Length>0,"gear retention starts with actual spawned actors, not fixture-injected actors");
                    await TraverseForFullRoute(f.Player,f.Sector,async count =>
                    {
                        for(int i=0;i<count;i++)
                        {
                            await f.Frames(1); driver.ObserveFrame();
                            if(encounter)
                            {
                                TestAssert.True(group.IsActive && group.CurrentBatch==0 &&
                                    actorIds.SequenceEqual(group.GetNode(group.ActorRootPath).GetChildren().OfType<CharacterBody2D>()
                                        .Select(actor=>actor.GetInstanceId()).OrderBy(id=>id)),
                                    $"BasinGalleryEncounterRetention: real lift/gallery path must not retire/recreate active enemies; player={f.Player.GlobalPosition} active={group.IsActive} batch={group.CurrentBatch}");
                            }
                        }
                    });
                    TestAssert.True(f.Lift.IsActivated,"BasinFullRouteRequiresPassengerLift: route cannot skip the approved lift");
                    if(encounter) await LeaveAndReenterGear(f,driver,group);
                }
                finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
            }
            finally { await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.Ride)
        {
            foreach (float landingX in new[] { 2724f, 2760f, 2796f })
                await Ride(tree, landingX);
        }
        else if (phase == BasinCounterweightPhase.Transfer)
        {
            foreach(float landingX in new[]{2724f,2760f,2796f}) await Transfer(tree,landingX);
        }
        else if (phase == BasinCounterweightPhase.Exits) await Exits(tree);
        else if (phase == BasinCounterweightPhase.Lifecycle) await Lifecycle(tree);
        else if (phase == BasinCounterweightPhase.Neighbors)
        {
            await CanyonMechanismRuntimeTests.RunGeneratedCounterweight(tree);
            await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(tree);
            await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(tree);
        }
        else if (phase == BasinCounterweightPhase.NeighborLifecycle) await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(tree);
        else if (phase == BasinCounterweightPhase.Steam) await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(tree);
        else if (phase == BasinCounterweightPhase.Fog)
        {
            await FogMovingGeometryReadabilityRuntimeTests.Run(tree);
            await FogMovingGeometrySavedControllerRuntimeTests.Run(tree);
            await FogFragmentReadabilityRuntimeTests.Run(tree);
        }
        else if (phase == BasinCounterweightPhase.AttachedExits) await AttachedExits(tree);
        else if (phase is BasinCounterweightPhase.Presentation or BasinCounterweightPhase.Stills)
            await Transfer(tree,2760,capture:true);
        else if (phase is BasinCounterweightPhase.BoardingTrace or BasinCounterweightPhase.BoardingCadence)
        {
            int originalMaxFps=Engine.MaxFps;
            if(phase==BasinCounterweightPhase.BoardingCadence) Engine.MaxFps=12;
            GD.Print($"BASIN_BOARDING_CADENCE forced_render_cap={phase==BasinCounterweightPhase.BoardingCadence} max_fps={Engine.MaxFps} physics_ticks={Engine.PhysicsTicksPerSecond} performance=false");
            var f = new Fixture(tree, new Vector2(2492,3880),full:true);
            try { await f.Start(); await Activate(f); await BoardReturn(f,2760); }
            finally { Engine.MaxFps=originalMaxFps; await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.Regressions)
        {
            QuestFragmentPlacementTests.Run();
            await QuestFragmentCollectionTests.Run(tree);
            await MinimapCatalogTests.Run(tree);
            await KeyboardClawTargetTests.Run(tree);
        }
        else throw new InvalidOperationException($"Phase not implemented yet: {phase}");
    }

    // Negative companion to the real airborne retention test. Drop left off
    // PistonTwo to the lower passage (the gallery wall blocks a direct right
    // walk), leave below the gallery, then return with one ordinary jump.
    private static async Task LeaveAndReenterGear(Fixture f,KeyboardRouteInputDriver driver,
        CanyonEncounterGroup group)
    {
        int completed=0,defeated=0,hp=f.Health.CurrentHealth;
        bool requireInactive=false;
        ulong playerId=f.Player.GetInstanceId();
        ulong[] oldActors=group.GetNode(group.ActorRootPath).GetChildren()
            .OfType<CharacterBody2D>().Select(a=>a.GetInstanceId()).ToArray();
        CanyonEncounterGroup.CompletedEventHandler onComplete=_=>completed++;
        CanyonEncounterGroup.GroupDefeatedEventHandler onDefeat=_=>defeated++;
        group.Completed+=onComplete; group.GroupDefeated+=onDefeat;
        async Task Tick()
        {
            await f.Frames(1); driver.ObserveFrame();
            TestAssert.True(f.Player.GetInstanceId()==playerId && f.Health.CurrentHealth==hp,
                "gear negative exit/return keeps same player and original health");
            TestAssert.True(!group.IsComplete && completed==0 && defeated==0,
                "gear departure and re-entry cannot fabricate a defeat or completion");
            if(requireInactive) TestAssert.True(!group.IsActive && group.CurrentBatch==-1 && group.LiveActorCount==0,
                "gallery continuation must not expand encounter activation along the lower return passage");
        }
        async Task Until(Func<bool> condition,int cap,string message)
        {
            for(int i=0;i<cap && !condition();i++) await Tick();
            TestAssert.True(condition(),$"{message}; position={f.Player.GlobalPosition} active={group.IsActive} batch={group.CurrentBatch}");
        }
        try
        {
            var walkway=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway");
            driver.Command(InputActions.MoveLeft,true);
            // The moving lift catches a fall immediately left of PistonTwo.
            // Walk across it to the authored48px missed-lift return gap; allow
            // the unchanged ~12px braking distance before its center2676.
            await Until(()=>f.Player.GlobalPosition.X<=2690,110,"physical A reaches missed-lift return gap");
            driver.Command(InputActions.MoveLeft,false);
            await Until(()=>Floor(f.Player,walkway),140,"ordinary fall reaches original lower return walkway");
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>f.Player.GlobalPosition.X>=3074,100,"physical D leaves below gallery continuation");
            driver.Command(InputActions.MoveRight,false);
            await Tick();
            TestAssert.True(f.Player.GlobalPosition.Y>=3796 && !group.IsActive &&
                group.CurrentBatch==-1 && group.LiveActorCount==0,
                "BasinGearOutsideContinuationRetires: lower Steam-side departure must still retire living enemies");
            GD.Print($"BASIN_GEAR_REAL_EXIT_OK player={f.Player.GlobalPosition} live={group.LiveActorCount} completed={completed}");
            requireInactive=true;
            driver.Command(InputActions.MoveLeft,true);
            await Until(()=>f.Player.GlobalPosition.X<=2488,180,"physical A follows original lower return passage");
            driver.Command(InputActions.MoveLeft,false);
            await Tick();
            requireInactive=false;
            driver.Command(InputActions.Jump,true); driver.Command(InputActions.MoveLeft,true);
            await Tick();
            driver.Command(InputActions.Jump,false);
            await Until(()=>group.IsActive,70,"ordinary Space+A re-enters original activation bounds");
            driver.Command(InputActions.MoveLeft,false);
            TestAssert.True(group.Definition.ActivationBounds.HasPoint(f.Player.GlobalPosition),
                "first reactivation occurs inside original authored activation bounds, not the gallery continuation");
            ulong[] newActors=group.GetNode(group.ActorRootPath).GetChildren()
                .OfType<CharacterBody2D>().Select(a=>a.GetInstanceId()).ToArray();
            TestAssert.True(group.CurrentBatch==0 && group.LiveActorCount>0 &&
                newActors.Length>0 && !newActors.Intersect(oldActors).Any(),
                "genuine re-entry restarts original batch zero with new actors, not a fake defeat or continuation teleport");
            GD.Print($"BASIN_GEAR_REAL_REENTRY_OK player={f.Player.GlobalPosition} batch={group.CurrentBatch} health={hp} completed={completed}");
        }
        finally { driver.ReleaseAll(); group.Completed-=onComplete; group.GroupDefeated-=onDefeat; }
    }

    // Shared by local route characterization and the actual Title-to-exit route.
    // Receives an already live player: never spawns, repositions, heals, resets
    // mechanism clocks, changes saves or invents an attachment.
    internal static async Task TraverseForFullRoute(CharacterBody2D player,
        PrologueSector sector,Func<int,Task> frames)
    {
        var driver=KeyboardRouteInputDriver.Current ??
            throw new InvalidOperationException("Basin route requires the active keyboard driver");
        var lift=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
        var first=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
        var second=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonTwo");
        var gallery=sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
        var control=sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl");
        var anchor=sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        var motor=player.GetNode<PlayerMotor>("Motor");
        var health=player.GetNode<HealthComponent>("Health");
        int initialHealth=health.CurrentHealth,initialJumps=motor.AcceptedJumpCount;
        ulong identity=player.GetInstanceId();
        var bell=sector.GetNode("Gameplay/Actors").GetChildren().OfType<BellBackLaborer>()
            .SingleOrDefault(actor=>actor.Name.ToString().StartsWith("canyon_v2_encounter_gear-pistons_",StringComparison.Ordinal));
        var bellHitbox=bell?.GetNode<Hitbox>(bell.AttackHitboxPath);
        var shield=player.GetNode<PlayerShieldController>("Shield");
        int bellContacts=0,guardedContacts=0;
        bool waitShieldHeld=false;
        BasinGalleryCastTrace galleryTrace=null;
        Hitbox.ConnectedEventHandler contact=target=>
        {
            if(target==player.GetNode<Hurtbox>("Hurtbox"))
            {
                bellContacts++;
                if(shield.IsActive && health.CurrentHealth==initialHealth) guardedContacts++;
            }
            GD.Print($"TASK10_BASIN_BELL_CONTACT source={bell.GetInstanceId()} target={target.GetPath()} physics={Engine.GetPhysicsFrames()} bell={bell.GlobalPosition} state={bell.CurrentState} player={player.GlobalPosition} shield={shield.IsActive} direction={shield.GuardDirection}");
        };
        if(bellHitbox!=null) bellHitbox.Connected+=contact;
        string stage="entry";
        HealthComponent.DamagedEventHandler damage=(remaining,impulse)=>
        {
            string actors=string.Join(';',sector.GetNode("Gameplay/Actors").GetChildren().OfType<Node2D>()
                .Select(actor=>$"{actor.Name}@{actor.GlobalPosition}"));
            GD.Print($"TASK10_BASIN_ROUTE_DAMAGE stage={stage} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} velocity={player.Velocity} health={initialHealth}->{remaining} impulse={impulse} floor={player.IsOnFloor()} last_fall={motor.LastFallHeight}/{motor.LastFallDamage} bell_state={bell?.CurrentState} lift_time={lift.ElapsedSeconds} actors={actors}");
        };
        health.Damaged+=damage;
        driver.ReleaseAll();
        async Task Step(int count=1)
        {
            for(int i=0;i<count;i++)
            {
                // Respond to the real visible sweep tell, not elapsed lift time.
                // A permanent guard overheats before the platform returns.
                bool defend=(stage is "lower window wait" or "boarding") &&
                    GodotObject.IsInstanceValid(bell) && bell.GlobalPosition.X>player.GlobalPosition.X &&
                    (bell.CurrentState==BellBackLaborerState.Attack ||
                     bell.CurrentState==BellBackLaborerState.Telegraph &&
                     bell.CurrentFrame.AttackTimeline.PhaseRemainingSeconds<=.35f);
                if(defend!=waitShieldHeld)
                {
                    driver.Command(InputActions.Shield,defend);
                    waitShieldHeld=defend;
                }
                await frames(1);
                TestAssert.True(GodotObject.IsInstanceValid(player) && player.IsInsideTree() &&
                    player.GetInstanceId()==identity && health.CurrentHealth==initialHealth && initialHealth>0,
                    $"Basin route keeps the same live player without damage or healing; stage={stage} position={player.GlobalPosition} health={initialHealth}->{health.CurrentHealth}");
            }
        }
        async Task Until(Func<bool> condition,int cap,string stage)
        {
            for(int i=0;i<cap && !condition();i++) await Step();
            TestAssert.True(condition(),$"BasinRoute {stage}: player={player.GlobalPosition} velocity={player.Velocity} lift={lift.ElapsedSeconds} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
        }
        async Task Walk(float target,int cap,bool stopOnFirst=false)
        {
            if(stopOnFirst)
            {
                var input=player.GetNode<PlayerInputReader>("Input");
                int used=0;
                float predictedStop=float.NaN;
                for(;used<cap;used++)
                {
                    TestAssert.True(Floor(player,first),
                        "Basin control approach retains actual PistonOne support");
                    var stop=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,
                        motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,cap-used,.5f);
                    TestAssert.True(stop.SpeedThresholdReached,
                        "Basin control neutral forecast fits original approach budget");
                    predictedStop=player.GlobalPosition.X+stop.NetDistance;
                    bool stopped=Mathf.Abs(player.GlobalPosition.X-target)<=1.5f &&
                        Mathf.Abs(player.Velocity.X)<=.5f && Mathf.Abs(input.Current.MoveAxis)<=.001f &&
                        !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D);
                    if(stopped) break;
                    int axis=Mathf.Abs(target-predictedStop)<=1.5f ? 0 : (int)Mathf.Sign(target-predictedStop);
                    driver.Command(InputActions.MoveLeft,axis<0);
                    driver.Command(InputActions.MoveRight,axis>0);
                    await Step();
                }
                driver.Command(InputActions.MoveLeft,false);
                driver.Command(InputActions.MoveRight,false);
                TestAssert.True(used<cap && Floor(player,first) &&
                    Mathf.Abs(player.GlobalPosition.X-target)<=1.5f && Mathf.Abs(player.Velocity.X)<=.5f &&
                    Mathf.Abs(input.Current.MoveAxis)<=.001f,
                    $"Basin control station stops with original{cap} budget; player={player.GlobalPosition} velocity={player.Velocity}");
                GD.Print($"TASK10_BASIN_CONTROL_STOP_OK frames={used}/{cap} player={player.GlobalPosition} " +
                    $"velocity={player.Velocity} predicted_stop_x={predictedStop:0.000} " +
                    $"sample={input.CurrentFrameSequence} axis={input.Current.MoveAxis} " +
                    $"health={initialHealth}->{health.CurrentHealth} " +
                    "observation=physics_signal_before_node_process no_pose_or_velocity_write=true");
                await Step(4);
                return;
            }
            float direction=Mathf.Sign(target-player.GlobalPosition.X);
            if(Mathf.Abs(target-player.GlobalPosition.X)<12) return;
            StringName action=direction>0?InputActions.MoveRight:InputActions.MoveLeft;
            Key physical=direction>0?Key.D:Key.A;
            bool released=false;
            Exception failure=null;
            int boardingSamples=0;
            ulong lastBoardingSample=0;
            // Input release is a physics decision, independent of render cadence
            // or diagnostic buffers. Never re-press a withdrawn native key.
            var observer=new BasinBoardingTickObserver { ProcessPhysicsPriority=100 };
            observer.Sample=_=>
            {
                if(released || failure!=null) return;
                try
                {
                    TestAssert.True(Godot.Input.IsPhysicalKeyPressed(physical),
                        $"BasinRoute owned {physical} withdrawn before threshold release");
                    if(stage=="boarding" && boardingSamples<12 && Engine.GetPhysicsFrames()>=lastBoardingSample+10)
                    {
                        lastBoardingSample=Engine.GetPhysicsFrames(); boardingSamples++;
                        string collisions=string.Join(';',Enumerable.Range(0,player.GetSlideCollisionCount()).Select(index=>
                        {
                            var collision=player.GetSlideCollision(index);
                            return $"{(collision.GetCollider() as Node)?.Name}:{collision.GetNormal()}";
                        }));
                        GD.Print($"TASK10_BASIN_BOARDING_SAMPLE physics={lastBoardingSample} player={player.GlobalPosition} velocity={player.Velocity} move={player.GetNode<PlayerInputReader>("Input").CurrentFrame.MoveAxis} scale={motor.EffectiveMovementScale} claw={claw.CurrentFrame.State} bell={bell?.GlobalPosition} bell_state={bell?.CurrentState} shield={shield.IsActive} collisions={collisions}");
                    }
                    if(direction*(player.GlobalPosition.X-target)>=-12)
                    { driver.Command(action,false); released=true; }
                }
                catch(Exception error) { failure=error; }
            };
            driver.Command(action,true);
            player.AddChild(observer);
            try
            {
                for(int i=0;i<cap && !released && failure==null;i++) await Step();
                if(failure!=null) throw new InvalidOperationException("Basin route walk input failed",failure);
                TestAssert.True(released,$"BasinRoute walk to{target} reaches threshold; actual={player.GlobalPosition}");
                await Step(4);
            }
            finally { observer.Sample=null; driver.Command(action,false); observer.Free(); }
        }
        try
        {
            stage="control approach";
            await Until(()=>claw.CurrentFrame.State==MechanicalClawState.Idle,60,"natural claw readiness");
            TestAssert.True(Floor(player,first),"Basin route starts on PistonOne reached by the live route");
            await Walk(2492,80,stopOnFirst:true);
            await Step(4);
            TestAssert.True(Floor(player,first) && Mathf.Abs(player.GlobalPosition.X-2492)<8,
                $"Basin route walks to visible control station; actual={player.GlobalPosition}");
            driver.Command(InputActions.MoveUp,true); await Step(3);
            TestAssert.True(claw.KeyboardPreview.Surface==control && claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "Basin route W previews static lift control");
            Vector2 caster=player.GlobalPosition;
            driver.Command(InputActions.Claw,true); await Step(3); driver.Command(InputActions.Claw,false);
            await Until(()=>lift.IsActivated,40,"native K activates lift");
            driver.Command(InputActions.MoveUp,false);
            TestAssert.True(player.GlobalPosition.DistanceTo(caster)<1,"Basin mechanism cast does not pull caster");
            // Join the next complete lower dwell, even if an earlier route visit
            // already activated the lift. Never restart its production phase.
            float cycle=2*(lift.TravelSeconds+lift.EndpointDwellSeconds);
            float nextWindow=Mathf.Floor(lift.ElapsedSeconds/cycle)*cycle+2*lift.TravelSeconds+lift.EndpointDwellSeconds+.45f;
            if(nextWindow<lift.ElapsedSeconds) nextWindow+=cycle;
            stage="lower window wait";
            // W aimed at the mechanism; return to horizontal facing with an
            // actual short D tap before defending the enemy to our right.
            driver.Command(InputActions.MoveRight,true); await Step();
            driver.Command(InputActions.MoveRight,false);
            await Until(()=>lift.ElapsedSeconds>=nextWindow,510,"next lower boarding window");
            TestAssert.True(lift.Frame.Phase01<.001f && Floor(player,first),"Basin route waits on original support for returning lift");
            GD.Print($"TASK10_BASIN_WAIT_DEFENSE contacts={bellContacts} guarded={guardedContacts} health={health.CurrentHealth} player={player.GlobalPosition}");
            driver.Command(InputActions.Shield,false); waitShieldHeld=false;
            stage="boarding";
            // The real GearPiston can physically stop this walk for its entire
            // authored active interval. Include that bounded obstruction time;
            // do not alter its phase/collider, skip the lift, or widen the full
            // route deadline. Empty-arena timing alone is not a route budget.
            var piston=sector.GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
            int obstructionFrames=Mathf.CeilToInt(piston.ActiveSeconds*Engine.PhysicsTicksPerSecond);
            TestAssert.True(obstructionFrames>0 && obstructionFrames<=60,
                "authored piston obstruction is bounded to at most one second");
            await Walk(2760,110+obstructionFrames);
            await Until(()=>Floor(player,lift),30,"walk/fall boards lift");
            GD.Print($"TASK10_BASIN_LIFT_BOARDED player={player.GlobalPosition} elapsed={lift.ElapsedSeconds} actor={identity}");
            stage="ride";
            for(int i=0;i<200 && lift.Frame.Phase01<=.999f;i++)
            {
                await Step();
                TestAssert.True(Floor(player,lift) && player.GlobalPosition.X>=2720 && player.GlobalPosition.X<=2800,
                    "Basin route full capsule rides actual deck without foreign support");
            }
            TestAssert.True(lift.Frame.Phase01>.999f && Floor(player,lift),"Basin route reaches upper endpoint supported");
            stage="gallery pull";
            galleryTrace=new BasinGalleryCastTrace(player,claw,anchor);
            galleryTrace.Start();
            driver.Command(InputActions.MoveUp,true); driver.Command(InputActions.MoveRight,true); await Step();
            galleryTrace.Capture("before_preview_assert");
            TestAssert.True(claw.KeyboardPreview.Surface==anchor && claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "Basin route W+D previews gallery point, not lift or unrelated wall");
            driver.Command(InputActions.Claw,true); galleryTrace.Capture("k_issued");
            // PhysicsFrame is emitted before the reader/controller. An injected K
            // can become just-pressed on the following input tick: releasing W/D
            // after one signal would make that cast use horizontal last-facing.
            // Wait for real cast consumption, not a render-dependent frame count.
            await Until(()=>claw.CurrentFrame.State is MechanicalClawState.Aim or
                MechanicalClawState.Windup or MechanicalClawState.Travel or
                MechanicalClawState.Latched or MechanicalClawState.Reel,4,
                "native K is consumed before direction release");
            driver.Command(InputActions.Claw,false); galleryTrace.Capture("k_released");
            driver.Command(InputActions.MoveUp,false); driver.Command(InputActions.MoveRight,false);
            galleryTrace.Capture("direction_keys_released");
            await Until(()=>claw.CurrentFrame.LockedTargetRuntimeId!=0,30,"gallery anchor locked");
            galleryTrace.Capture("before_lock_assert");
            TestAssert.Equal(anchor.GetInstanceId(),claw.CurrentFrame.LockedTargetRuntimeId,"Basin route actual K locks intended gallery");
            galleryTrace.Freeze(lockVerified:true);
            bool sawReel=claw.CurrentFrame.State==MechanicalClawState.Reel;
            for(int i=0;i<150 && !claw.CurrentFrame.Arrived;i++)
            { await Step(); sawReel|=claw.CurrentFrame.State==MechanicalClawState.Reel; }
            TestAssert.True(sawReel && claw.CurrentFrame.Arrived && claw.CurrentFrame.State==MechanicalClawState.Swing,
                $"BasinRoute real Reel must arrive as Swing; actual={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
            driver.Command(InputActions.Claw,true); await Step(3); driver.Command(InputActions.Claw,false);
            await Until(()=>Floor(player,gallery),45,"K release onto gallery");
            await Step(15);
            TestAssert.True(Floor(player,gallery) && player.GlobalPosition.X>=3060 && player.GlobalPosition.X<=3260,
                "Basin route lands and remains fully supported without corrective D input");
            GD.Print($"TASK10_BASIN_GALLERY_ARRIVAL player={player.GlobalPosition} actor={identity} health={health.CurrentHealth}");
            // Rejoin live Bell encounter on the lower PistonTwo. Stop before the
            // gallery fragment so natural omission/recovery tests retain meaning.
            stage="return to Bell";
            await Walk(2975,60);
            await Until(()=>Floor(player,second),45,"ordinary left drop returns to PistonTwo");
            TestAssert.Equal(initialJumps,motor.AcceptedJumpCount,"Basin transfer is walking/riding/claw, not extra jumps");
            TestAssert.True(Floor(player,second) && player.GlobalPosition.X>=2864 && player.GlobalPosition.X<=3020,
                "Basin route returns full capsule to supported Bell approach");
            GD.Print($"TASK10_BASIN_LIFT_ROUTE_OK player={player.GlobalPosition} actor={identity} health={health.CurrentHealth} keyboard_injected=true human=false");
        }
        finally
        {
            galleryTrace?.Freeze();
            galleryTrace?.Dump();
            health.Damaged-=damage; driver.ReleaseAll();
            if(GodotObject.IsInstanceValid(bellHitbox)) bellHitbox.Connected-=contact;
        }
    }

    // Passive, short-window diagnostic. No resolver calls, gameplay writes or
    // per-publication formatting/IO. Native reads are sequential, not atomic;
    // the event frame is retained separately from the then-current controller.
    private sealed class BasinGalleryCastTrace
    {
        private const int Capacity=32, PublicationCapacity=12, PreClawCapacity=12, TargetCapacity=16;
        private readonly GalleryCastRow[] _rows=new GalleryCastRow[Capacity];
        private readonly ulong[] _targetIds=new ulong[TargetCapacity];
        private readonly string[] _targetPaths=new string[TargetCapacity];
        private readonly CharacterBody2D _player;
        private readonly MechanicalClawController _claw;
        private readonly GrappleSurface2D _expected;
        private PlayerInputReader _input;
        private BasinBoardingTickObserver _preClaw;
        private int _count,_targetCount,_publications,_preClawSamples,_omittedRows,_omittedPublications,_omittedPreClaw,_unmappedTargets,_failures;
        private int _inputPriority,_preClawPriority,_clawPriority;
        private bool _subscribed,_frozen,_dumped,_firstLock,_lockVerified;
        private string _firstError;
        private ulong _expectedId;
        private Vector2 _expectedAnchor;

        internal BasinGalleryCastTrace(CharacterBody2D player,MechanicalClawController claw,GrappleSurface2D expected)
        { _player=player; _claw=claw; _expected=expected; }

        internal void Start()
        {
            try
            {
                _input=_player.GetNode<PlayerInputReader>("Input");
                _expectedId=Remember(_expected);
                _expectedAnchor=_expected.WorldAnchor;
                _inputPriority=_input.ProcessPhysicsPriority;
                _clawPriority=_claw.ProcessPhysicsPriority;
                _preClawPriority=checked(_clawPriority-1);
                if(_inputPriority>=_preClawPriority)
                    throw new InvalidOperationException("Gallery trace requires input < observer < claw physics priority");
                _preClaw=new BasinBoardingTickObserver
                {
                    Name="BasinGalleryPreClawTrace", ProcessPhysicsPriority=_preClawPriority,
                    Sample=OnBeforeClaw
                };
                _player.AddChild(_preClaw);
                _claw.ClawFrameChanged+=OnFrame; _subscribed=true;
                Capture("before_direction_keys");
            }
            catch(Exception error) { Fault(error); }
        }

        private void OnBeforeClaw(double delta)
        {
            if(_frozen || _firstError!=null || _firstLock) return;
            if(_preClawSamples==PreClawCapacity) { _omittedPreClaw++; return; }
            _preClawSamples++;
            // After the reader, before the controller (and ordinary motor):
            // current/preview frames are explicitly the previous publication.
            Capture("before_claw_physics");
        }

        internal void Capture(string boundary)
        {
            if(_frozen || _firstError!=null) return;
            try { Add(boundary,_claw.CurrentFrame,_claw.CurrentTarget); }
            catch(Exception error) { Fault(error); }
        }

        private void OnFrame(MechanicalClawFrame frame,GrappleSurface2D target)
        {
            if(_frozen || _firstError!=null || _firstLock) return;
            try
            {
                bool firstLock=frame.LockedTargetRuntimeId!=0;
                // Reserve room for the first lock and explicit input boundaries,
                // even if a long wait exhausts ordinary publication retention.
                if(firstLock || _publications<PublicationCapacity)
                {
                    Add(firstLock ? "first_lock_publication" : "claw_publication",frame,target);
                    _publications++;
                }
                else _omittedPublications++;
                _firstLock=firstLock;
            }
            catch(Exception error) { Fault(error); }
        }

        private void Add(string boundary,MechanicalClawFrame published,GrappleSurface2D publishedTarget)
        {
            if(_count==Capacity) { _omittedRows++; return; }
            MechanicalClawTargetResolution preview=_claw.KeyboardPreview;
            GrappleSurface2D currentTarget=_claw.CurrentTarget;
            ulong previewId=Remember(preview.Surface),currentId=Remember(currentTarget),publishedId=Remember(publishedTarget);
            PlayerInputFrame sampled=_input.CurrentFrame;
            var row=new GalleryCastRow(boundary,Engine.GetPhysicsFrames(),Engine.GetProcessFrames(),
                _input.CurrentFrameSequence,Godot.Input.IsPhysicalKeyPressed(Key.W),
                Godot.Input.IsPhysicalKeyPressed(Key.D),Godot.Input.IsPhysicalKeyPressed(Key.K),
                sampled,_input.KeyboardAimDirection,_player.GlobalPosition,_player.Velocity,
                previewId,preview.Snapshot,currentId,
                GodotObject.IsInstanceValid(currentTarget) ? currentTarget.WorldAnchor : Vector2.Zero,
                publishedId,published,_claw.CurrentFrame);
            _rows[_count++]=row;
        }

        private ulong Remember(GrappleSurface2D target)
        {
            if(!GodotObject.IsInstanceValid(target)) return 0;
            ulong id=target.GetInstanceId();
            for(int i=0;i<_targetCount;i++) if(_targetIds[i]==id) return id;
            if(_targetCount==TargetCapacity) { _unmappedTargets++; return id; }
            string path=target.GetPath().ToString();
            _targetIds[_targetCount]=id; _targetPaths[_targetCount++]=path;
            return id;
        }

        private string PathFor(ulong id)
        {
            if(id==0) return "none";
            for(int i=0;i<_targetCount;i++) if(_targetIds[i]==id) return _targetPaths[i];
            return "unmapped-runtime-id";
        }

        private void Fault(Exception error)
        { _failures++; _firstError??=error.GetType().Name+": "+error.Message; }

        internal void Freeze(bool lockVerified=false)
        {
            if(_frozen) return;
            _frozen=true; _lockVerified=lockVerified;
            try { if(_subscribed) { _claw.ClawFrameChanged-=OnFrame; _subscribed=false; } }
            catch(Exception error) { Fault(error); }
            try
            {
                if(GodotObject.IsInstanceValid(_preClaw))
                {
                    _preClaw.Sample=null;
                    _preClaw.SetPhysicsProcess(false);
                    _preClaw.QueueFree();
                }
            }
            catch(Exception error) { Fault(error); }
        }

        internal void Dump()
        {
            if(_dumped) return;
            _dumped=true;
            // Formatting reads only copied managed rows/paths. Never let ancillary
            // diagnostics replace the route exception or prevent its cleanup.
            try
            {
                GD.Print($"TASK10_BASIN_GALLERY_TRACE_BEGIN diagnostic_only=true lock_assertion_passed={_lockVerified} expected={_expectedId} expected_path={PathFor(_expectedId)} expected_anchor={_expectedAnchor} rows={_count}/{Capacity} omitted_rows={_omittedRows} omitted_publications={_omittedPublications} omitted_pre_claw={_omittedPreClaw} input_priority={_inputPriority} observer_priority={_preClawPriority} claw_priority={_clawPriority} unmapped_target_observations={_unmappedTargets} observer_failures={_failures} first_error={_firstError ?? "none"} sampled_WD=derived_from_net_aim_axes sampled_K=accepted_ClawPressed_edge_no_held_field order=pre_claw_or_publication_not_OS_or_atomic_snapshot pre_claw_frames=previous_publication");
                for(int i=0;i<_count;i++)
                {
                    GalleryCastRow row=_rows[i];
                    GD.Print($"TASK10_BASIN_GALLERY_TRACE row={i} boundary={row.Boundary} physics={row.Physics} process={row.Process} reader={row.Reader} raw_WDK={row.RawW}/{row.RawD}/{row.RawK} sampled_WD={row.Sampled.Aim.Y<0}/{row.Sampled.Aim.X>0} sampled_K_pressed={row.Sampled.ClawPressed} keyboard={row.Sampled.KeyboardActions} sampled_aim={row.Sampled.Aim} move={row.Sampled.MoveAxis} resolved_aim={row.Aim} position={row.Position} velocity={row.Velocity} preview={row.PreviewId}:{PathFor(row.PreviewId)} preview_snapshot_id={row.Preview.RuntimeId} preview_anchor={row.Preview.WorldAnchor} preview_normal={row.Preview.SurfaceNormal} preview_los={row.Preview.HasLineOfSight} current={row.CurrentId}:{PathFor(row.CurrentId)} current_world_anchor={row.CurrentAnchor} published_target={row.PublishedId}:{PathFor(row.PublishedId)} published_state={row.Published.State} published_cast={row.Published.CastId} published_lock={row.Published.LockedTargetRuntimeId}:{PathFor(row.Published.LockedTargetRuntimeId)} published_anchor={row.Published.AnchorPoint} published_normal={row.Published.SurfaceNormal} published_caster={row.Published.CasterWorldPosition} controller_state={row.Current.State} controller_cast={row.Current.CastId} controller_lock={row.Current.LockedTargetRuntimeId}:{PathFor(row.Current.LockedTargetRuntimeId)} controller_anchor={row.Current.AnchorPoint} controller_normal={row.Current.SurfaceNormal}");
                }
                GD.Print("TASK10_BASIN_GALLERY_TRACE_END");
            }
            catch(Exception) { /* Route result and original cleanup stay authoritative. */ }
        }

        private readonly record struct GalleryCastRow(string Boundary,ulong Physics,ulong Process,ulong Reader,
            bool RawW,bool RawD,bool RawK,PlayerInputFrame Sampled,Vector2 Aim,Vector2 Position,Vector2 Velocity,
            ulong PreviewId,MechanicalClawTargetSnapshot Preview,ulong CurrentId,Vector2 CurrentAnchor,
            ulong PublishedId,MechanicalClawFrame Published,MechanicalClawFrame Current);
    }

    // Catches replacement by a decorative one-shot, missing receiver, caster pull,
    // and phase-restarting repeated K casts in the actual serialized level leaf.
    private static async Task Contract(SceneTree tree)
    {
        var f = new Fixture(tree, new Vector2(2492, 3880));
        try
        {
            await f.Start();
            await Activate(f);
            float elapsed = f.Lift.ElapsedSeconds;
            await f.Frames(45);
            TestAssert.True(f.Lift.ElapsedSeconds > elapsed + .6f && f.Lift.GlobalPosition.Y < 3968,
                "native K starts immediate real ascent, not an endpoint dwell or one-shot decoration");
            await f.Until(() => f.Claw.CurrentFrame.State == MechanicalClawState.Idle, 90, "mechanism cast retracts");
            float before = f.Lift.ElapsedSeconds;
            f.Key(Key.W, true);
            await f.Frames(2);
            TestAssert.True(f.Claw.KeyboardPreview.Surface == f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl"),
                "repeat activation must again aim at static control, not unrelated terrain");
            await f.Press(Key.K, 3);
            await f.Frames(25);
            f.Key(Key.W, false);
            TestAssert.True(f.Lift.ElapsedSeconds > before + .3f && f.Lift.ElapsedSeconds < before + .6f,
                "repeated K neither restarts nor accelerates lift phase");
            TestAssert.True(f.Player.GlobalPosition.DistanceTo(new Vector2(2492,3880)) < 1f,
                $"static mechanism activation never drags its caster; actual={f.Player.GlobalPosition} target={f.Claw.CurrentTarget?.GetPath()}");
            GD.Print($"BASIN_NATIVE_K_OK receipt={f.KeyReceipts} samples={f.Input.CurrentFrameSequence} elapsed={f.Lift.ElapsedSeconds}");
            AssertSweep(f);
        }
        finally { await f.Dispose(); }
    }

    private static async Task Activate(Fixture f)
    {
        TestAssert.True(f.Player.IsOnFloor(), "mechanism casting station stands on authored PistonOne");
        f.Key(Key.W, true);
        await f.Frames(3);
        var control = f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl");
        TestAssert.True(f.Claw.KeyboardPreview.Surface == control &&
            f.Claw.KeyboardPreview.Snapshot.IsLegacyInteraction &&
            f.Claw.KeyboardPreview.Snapshot.HasLineOfSight,
            $"W preview selects visible static mechanism; actual={f.Claw.KeyboardPreview.Surface?.GetPath()}");
        Vector2 caster = f.Player.GlobalPosition;
        await f.Press(Key.K, 3);
        await f.Until(() => f.Lift.IsActivated, 40, "real K reaches static receiver");
        f.Key(Key.W, false);
        TestAssert.True(f.SampledClawEdges>0,"real PlayerInputReader sampled physical K edge before receiver activation");
        TestAssert.True(f.Player.GlobalPosition.DistanceTo(caster) < 1f, "activation does not move caster toward control");
        TestAssert.True(f.Lift.ElapsedSeconds < .3f, "first departure has no hidden startup wait");
        TestAssert.Equal(GrappleSurfaceMaterial.Forbidden,
            f.Lift.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material,
            "moving passenger deck remains claw Forbidden");
    }

    private static async Task Ride(SceneTree tree, float landingX)
    {
        var f = new Fixture(tree, new Vector2(2492, 3880));
        try
        {
            await f.Start(); await Activate(f); AssertSweep(f);
            await BoardReturn(f, landingX);
            int health = f.Health.CurrentHealth;
            float start = f.Lift.ElapsedSeconds;
            bool up=false, down=false, upperDwell=false, lowerDwell=false;
            bool safe=false, telegraph=false, active=false, recovery=false;
            while (f.Lift.ElapsedSeconds < start + 8.1f)
            {
                float before = f.Lift.GlobalPosition.Y;
                await f.Frames(1);
                AssertRider(f, health);
                float moved = f.Lift.GlobalPosition.Y - before;
                up |= moved < -.1f; down |= moved > .1f;
                upperDwell |= !f.Lift.Frame.HazardActive && f.Lift.Frame.Phase01 > .999f;
                lowerDwell |= !f.Lift.Frame.HazardActive && f.Lift.Frame.Phase01 < .001f;
                var p = f.Piston.Frame;
                safe |= p.Phase01 == 0 && !p.Telegraphing;
                telegraph |= p.Telegraphing; active |= p.HazardActive;
                recovery |= !p.HazardActive && !p.Telegraphing && p.Phase01 > 0;
            }
            TestAssert.True(up && down && upperDwell && lowerDwell,
                "passenger remains supported through both travel directions and endpoint dwells");
            TestAssert.True(safe && telegraph && active && recovery, "ride observed all actual piston phases");
            GD.Print($"BASIN_RIDE_OK landing_x={landingX} passenger={f.Player.GlobalPosition} health={health} up={up} down={down} dwell={upperDwell && lowerDwell} piston_all=true frames={f.Input.CurrentFrameSequence}");
        }
        finally { await f.Dispose(); }
    }

    private static async Task BoardReturn(Fixture f, float landingX)
    {
        GD.Print($"BASIN_PREWAIT physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} lift_time={f.Lift.ElapsedSeconds} raw_d={Godot.Input.IsPhysicalKeyPressed(Key.D)} axis={f.Input.Current.MoveAxis}");
        await f.Until(() => f.Lift.ElapsedSeconds >= 6.45f, 430, "missed departure genuinely returns");
        TestAssert.True(f.Lift.Frame.Phase01 < .001f && f.Player.IsOnFloor(),
            "first missed cycle returns to lower boarding window while caster waits on original support");
        GD.Print($"BASIN_BOARDING_BEGIN physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} lift_time={f.Lift.ElapsedSeconds} raw_d={Godot.Input.IsPhysicalKeyPressed(Key.D)} axis={f.Input.Current.MoveAxis}");
        TestAssert.True(f.Player.GlobalPosition.DistanceTo(new Vector2(2492,3880))<1f &&
            f.Player.Velocity.Length()<1f && !Godot.Input.IsPhysicalKeyPressed(Key.D),
            "BasinBoardingInitialState: untouched caster waits at original station before owned D walk");
        int jumps = f.Motor.AcceptedJumpCount;
        f.BeginBoardingTrace(landingX - 12f);
        f.Key(Key.D, true);
        // Release12px before desired center to allow the unchanged motor's
        // ordinary braking distance. Waiting for floor is part of falling80px,
        // not a second boarding attempt or a new timing window.
        await f.Until(() => f.BoardingReleaseCompleted, 110,
            "walk/fall boarding from original PistonOne reaches deck");
        f.AssertBoardingReleaseTimely();
        await f.Until(() => Floor(f.Player, f.Lift), 30, "ordinary fall completes on returned carriage");
        await f.Frames(4);
        TestAssert.True(Floor(f.Player, f.Lift), $"walk/fall boards returned lift, position={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition}");
        TestAssert.Equal(jumps, f.Motor.AcceptedJumpCount, "boarding uses real walking/falling, not hidden jump/teleport");
        f.CompleteBoardingTrace();
    }

    // Missing point anchor, wrong native ranking, non-radial docking, or an
    // edge-only landing must fail this real saved-scene transfer contract.
    private static async Task Transfer(SceneTree tree,float landingX,bool capture=false)
    {
        var f = new Fixture(tree, new Vector2(2492,3880),full:capture);
        try
        {
            if(capture) f.EnableCapture();
            await f.Start(); await Activate(f); await BoardReturn(f,landingX);
            f.CaptureStage("02-boarded");
            int health=f.Health.CurrentHealth;
            await f.Until(() => f.Lift.Frame.Phase01 > .999f, 200, "upper passenger endpoint reached");
            f.CaptureStage("03-upper-deck");
            AssertRider(f,health);
            var gallery=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
            var surface=f.Sector.GetNodeOrNull<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
            TestAssert.True(surface!=null,"SavedBasinGalleryPointAnchorMissing");
            f.Key(Key.W,true); f.Key(Key.D,true); await f.Frames(1);
            MechanicalClawTargetSnapshot preview=f.Claw.KeyboardPreview.Snapshot;
            Vector2 radial=(f.Player.GlobalPosition-new Vector2(3112,3624)).Normalized();
            GD.Print($"BASIN_UPPER_PREVIEW station={landingX} player={f.Player.GlobalPosition} surface={f.Claw.KeyboardPreview.Surface?.GetPath()} anchor={preview.WorldAnchor} normal={preview.SurfaceNormal} los={preview.HasLineOfSight}");
            TestAssert.True(f.Claw.KeyboardPreview.Surface == surface && preview.HasLineOfSight &&
                !preview.IsLegacyInteraction && preview.Material==GrappleSurfaceMaterial.DedicatedAnchor &&
                preview.WorldAnchor.DistanceTo(new Vector2(3112,3624))<.1f && preview.SurfaceNormal.DistanceTo(radial)<.01f,
                "native W+D preview selects visible dedicated point anchor with independent radial normal");
            AssertRider(f,health);
            f.Key(Key.K,true); await f.Frames(1); f.Key(Key.K,false); f.Key(Key.W,false); f.Key(Key.D,false); await f.Frames(2);
            await f.Until(() => f.Claw.CurrentFrame.LockedTargetRuntimeId != 0, 30, "upper K locks previewed gallery");
            TestAssert.Equal(surface.GetInstanceId(),f.Claw.CurrentFrame.LockedTargetRuntimeId,"actual K locks exact previewed gallery, not a competing side");
            TestAssert.True(f.Claw.CurrentFrame.SurfaceNormal.DistanceTo(radial)<.02f && f.Claw.CurrentFrame.EffectiveVisibility &&
                f.Claw.CurrentFrame.DockPoint.X>=3068 && f.Claw.CurrentFrame.DockPoint.X<=3072 &&
                f.Claw.CurrentFrame.DockPoint.Y>=3643 && f.Claw.CurrentFrame.DockPoint.Y<=3650,
                "locked point target retains radial dock fully above and inside GalleryOne");
            MechanicalClawState previous=f.Claw.CurrentFrame.State;
            for(int i=0;i<150 && !f.Claw.CurrentFrame.Arrived;i++)
            {
                await f.Frames(1);
                var frame=f.Claw.CurrentFrame;
                if(frame.State==MechanicalClawState.Reel) f.CaptureStage("04-automatic-pull");
                if(frame.State!=previous || i%10==0)
                    GD.Print($"BASIN_PULL_TRACE tick={i} state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} arrived={frame.Arrived} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} dock={frame.DockPoint} collision={string.Join(',',Enumerable.Range(0,f.Player.GetSlideCollisionCount()).Select(n=>f.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
                previous=frame.State;
                if(frame.State==MechanicalClawState.Released && frame.ReleaseKind==MechanicalClawReleaseKind.Obstructed) break;
            }
            // Physical supported landing is the outcome. Preserve early release
            // reason; never relabel Obstructed as an Arrived event.
            bool arrived=f.Claw.CurrentFrame.Arrived;
            string release=f.Claw.CurrentFrame.ReleaseKind.ToString();
            Vector2 dock=f.Player.GlobalPosition;
            TestAssert.True(arrived && f.Claw.CurrentFrame.State==MechanicalClawState.Swing,
                "BasinTransferArrival: automatic pull must genuinely arrive before a supported gallery release");
            f.CaptureStage("05-arrived-swing");
            if(arrived) await f.Press(Key.K,3);
            await f.Until(() => Floor(f.Player,gallery),45,"normal release settles onto actual gallery top");
            TestAssert.True(f.Player.GlobalPosition.X>=3060 && f.Player.GlobalPosition.X<=3260 &&
                MathF.Abs(f.Player.GlobalPosition.Y-3720f)<1.5f,
                "release lands full capsule on supported gallery, not side cling");
            TestAssert.Equal(health,f.Health.CurrentHealth,"claw transfer landing neither damages nor heals");
            f.CaptureStage("06-supported-gallery");
            await f.Frames(15);
            TestAssert.True(Floor(f.Player,gallery) && f.Player.GlobalPosition.X>=3060 && f.Player.GlobalPosition.X<=3260,
                "natural landing stays fully supported without D walk-on repair");
            GD.Print($"BASIN_TRANSFER_OK station={landingX} preview={preview.WorldAnchor} dock={dock} supported={f.Player.GlobalPosition} health={health} receipt={f.KeyReceipts} arrived={arrived} release={release}");
            if(capture) f.AssertCaptureComplete();
        }
        finally { await f.Dispose(); }
    }

    private static void AssertRider(Fixture f, int health)
    {
        TestAssert.True(Floor(f.Player, f.Lift), $"every-frame floor remains deck; player={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds}");
        TestAssert.Near(f.Lift.GlobalPosition.Y - 48f, f.Player.GlobalPosition.Y, 1.5f,
            "real 72px capsule bottom tracks real 24px deck top");
        TestAssert.True(f.Player.GlobalPosition.X >= 2720f - .1f && f.Player.GlobalPosition.X <= 2800f + .1f,
            "complete unchanged40px capsule remains fully on120px deck");
        TestAssert.Equal(health, f.Health.CurrentHealth, "ride never damages or heals passenger");
        for (int i=0;i<f.Player.GetSlideCollisionCount();i++)
            TestAssert.True(f.Player.GetSlideCollision(i).GetCollider() == f.Lift, "rider has no foreign static/piston contacts");
    }

    private static async Task AttachedExits(SceneTree tree)
    {
        foreach(Key exit in new[]{Key.K,Key.Space,Key.Shift})
        {
            var f=new Fixture(tree,new Vector2(2492,3880));
            try
            {
                await f.Start();await Activate(f);await BoardReturn(f,2760);
                await f.Until(()=>f.Lift.Frame.Phase01>.999f,200,"attached exit begins at actual upperdeck");
                var target=f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
                f.Key(Key.W,true);f.Key(Key.D,true);await f.Frames(1);
                TestAssert.True(f.Claw.KeyboardPreview.Surface==target && f.Claw.KeyboardPreview.Snapshot.HasLineOfSight,
                    "attached exit W+D previews the same saved gallery anchor as transfer");
                await f.Press(Key.K,3);f.Key(Key.W,false);f.Key(Key.D,false);
                await f.Until(()=>f.Claw.CurrentFrame.State==MechanicalClawState.Reel && f.Player.GlobalPosition.X>2810,90,"actual saved gallery pull is underway before exit");
                TestAssert.Equal(target.GetInstanceId(),f.Claw.CurrentFrame.LockedTargetRuntimeId,
                    "attached exit cancels the intended gallery anchor, not another surface");
                int jumps=f.Motor.AcceptedJumpCount,dashes=f.Motor.AcceptedDashCount,health=f.Health.CurrentHealth;
                int boosts=0,cancels=0;
                void Released(MechanicalClawFrame frame,GrappleSurface2D _) {
                    if(frame.State==MechanicalClawState.Boost) boosts++;
                    if(frame.State==MechanicalClawState.Released && frame.ReleaseKind==MechanicalClawReleaseKind.Cancel) cancels++;
                }
                f.Claw.ClawFrameChanged+=Released;
                try {await f.Press(exit,3);await f.Frames(3);}
                finally {f.Claw.ClawFrameChanged-=Released;}
                TestAssert.Equal(exit==Key.Space?1:0,boosts,"attached Space is exactly one original boost; K/Shift cannot boost");
                TestAssert.Equal(exit==Key.Space?0:1,cancels,"fresh K and Shift each release actual attachment once");
                TestAssert.Equal(jumps,f.Motor.AcceptedJumpCount,"attached Space is not an extra normaljump");
                TestAssert.Equal(dashes+(exit==Key.Shift?1:0),f.Motor.AcceptedDashCount,"attached Shift retains original dash");
                await f.Until(()=>f.Player.IsOnFloor(),180,"attached release reaches real original support");
                TestAssert.Equal(health,f.Health.CurrentHealth,"bounded attachment exit has no hidden healing or damage suppression");
                GD.Print($"BASIN_ATTACHED_EXIT_OK key={exit} boost={boosts} cancel={cancels} player={f.Player.GlobalPosition} height={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage} Ksamples={f.SampledClawEdges} Spacesamples={f.SampledJumpEdges} Shiftsamples={f.SampledDashEdges}");
            }
            finally {await f.Dispose();}
        }
    }

    private static async Task Exits(SceneTree tree)
    {
        foreach(Key exit in new[]{Key.Space,Key.Shift})
        {
            var f=new Fixture(tree,new Vector2(2492,3880));
            try
            {
                await f.Start();await Activate(f);await BoardReturn(f,2760);
                int jumps=f.Motor.AcceptedJumpCount,dashes=f.Motor.AcceptedDashCount,health=f.Health.CurrentHealth;
                // The unchanged variable-height jump needs a held Space to clear
                // the80px boarding ledge. A three-frame tap intentionally cannot.
                GD.Print($"BASIN_NORMAL_EXIT_BEGIN key={exit} player={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds}");
                f.Key(Key.A,true);await f.Press(exit,exit==Key.Space?20:3);
                await f.Until(()=>!Floor(f.Player,f.Lift),15,"normal exit leaves carriage");
                for(int i=0;i<90 && f.Player.GlobalPosition.X>=2648;i++)
                {
                    await f.Frames(1);
                    if(i%10==0) GD.Print($"BASIN_NORMAL_EXIT_TRACE key={exit} tick={i} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} onfloor={f.Player.IsOnFloor()} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds} piston={f.Piston.Frame} piston_collision={!f.Piston.Collision.Disabled} contacts={string.Join(',',Enumerable.Range(0,f.Player.GetSlideCollisionCount()).Select(n=>f.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
                }
                TestAssert.True(f.Player.GlobalPosition.X<2648,"normal exit moves clear of shaft");
                f.Key(Key.A,false);
                await f.Until(()=>f.Player.IsOnFloor(),150,"normal carriage exit reaches existing support");
                TestAssert.Equal(jumps+(exit==Key.Space?1:0),f.Motor.AcceptedJumpCount,"Space exit is original jump, Shift is not hidden jump");
                TestAssert.Equal(dashes+(exit==Key.Shift?1:0),f.Motor.AcceptedDashCount,"Shift exit is original dash");
                TestAssert.Equal(health,f.Health.CurrentHealth,"normal lower exit preserves original health");
                GD.Print($"BASIN_NORMAL_EXIT_OK key={exit} player={f.Player.GlobalPosition} fall={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage}");
            }
            finally {await f.Dispose();}
        }
        var missed=new Fixture(tree,new Vector2(2492,3880));
        try
        {
            await missed.Start();await Activate(missed);
            int health=missed.Health.CurrentHealth;
            // Miss the immediate first departure by walking into the empty shaft
            // after activation. Do not wait for the returning carriage this time.
            await missed.Until(()=>missed.Lift.Frame.Phase01>.999f,150,"first departure clears boarding station before deliberately late approach");
            missed.Key(Key.D,true);
            for(int i=0;i<110 && missed.Player.GlobalPosition.X<2748;i++)
            {
                await missed.Frames(1);
                if(i%10==0) GD.Print($"BASIN_MISSED_TRACE tick={i} player={missed.Player.GlobalPosition} velocity={missed.Player.Velocity} onfloor={missed.Player.IsOnFloor()} elapsed={missed.Lift.ElapsedSeconds} piston={missed.Piston.Frame} piston_collision={!missed.Piston.Collision.Disabled} contacts={string.Join(',',Enumerable.Range(0,missed.Player.GetSlideCollisionCount()).Select(n=>missed.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
            }
            TestAssert.True(missed.Player.GlobalPosition.X>=2748,"real missed boarding enters empty shaft");
            missed.Key(Key.D,false);
            var walkway=missed.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway");
            await missed.Until(()=>Floor(missed.Player,walkway),180,"missed departure falls to original lower return walkway");
            TestAssert.Equal(0,missed.Motor.LastFallDamage,"bounded failed jump retains original below600px safe fall rule");
            TestAssert.Equal(health,missed.Health.CurrentHealth,"failed jump needs no fixture healing/invulnerability");
            GD.Print($"BASIN_MISSED_BOARDING_LANDING player={missed.Player.GlobalPosition} height={missed.Motor.LastFallHeight} damage={missed.Motor.LastFallDamage} health={missed.Health.CurrentHealth}");
            await WalkBack(missed);
        }
        finally {await missed.Dispose();}
    }

    private static async Task WalkBack(Fixture f)
    {
        f.Key(Key.A,true);
        await f.Until(()=>f.Player.GlobalPosition.X<=2488,180,"actual A walks under shaft/piston body to original return step");
        await f.Press(Key.Space,3);
        var step=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutCampReturnStep");
        await f.Until(()=>Floor(f.Player,step),100,"original Space climbs40px camp return step");
        f.Key(Key.A,false);await f.Frames(4);
        f.Key(Key.A,true);
        await f.Until(()=>f.Player.GlobalPosition.X<=2412 && Floor(f.Player,step),30,"supported step approach clears original low ceiling");
        // Original variable-height jump requires holding Space for its full rise.
        await f.Press(Key.Space,20);
        var entry=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearEntry");
        await f.Until(()=>Floor(f.Player,entry),120,"original Space climbs80px back to GearEntry");
        f.Key(Key.A,false);await f.Frames(4);
        TestAssert.True(f.Player.GlobalPosition.X<2380 && Floor(f.Player,entry),"missed jump returns by real supported walking/jumps, without reposition/recovery");
        GD.Print($"BASIN_WALK_BACK_OK player={f.Player.GlobalPosition} jumpcount={f.Motor.AcceptedJumpCount} fall={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage}");
    }

    private static async Task Lifecycle(SceneTree tree)
    {
        var f=new Fixture(tree,new Vector2(2492,3880),full:true);
        string save=null;
        try
        {
            await f.Start();await Activate(f);await BoardReturn(f,2760);
            var level=f.Level;
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu");
            await f.Until(()=>f.Lift.Frame.Phase01>.2f && f.Lift.Frame.Phase01<.8f,120,"riding pause occurs during motion");
            pause.Open();float time=f.Lift.ElapsedSeconds;Vector2 carriage=f.Lift.GlobalPosition,passenger=f.Player.GlobalPosition;
            await f.Frames(12);
            TestAssert.True(tree.Paused && f.Lift.ElapsedSeconds==time && f.Lift.GlobalPosition==carriage && f.Player.GlobalPosition==passenger,
                "real pause freezes actual riding passenger and lift phase");
            pause.Execute(ProloguePauseCommand.Resume);await f.Frames(3);
            TestAssert.True(f.Lift.ElapsedSeconds>time,"resume continues original phase, no restart");
            TestAssert.True(world.TryActivateCheckpoint("canyon.v2.basin-camp"),"legal Basin checkpoint activated");
            TestAssert.Equal(FragmentCollectionResult.Collected,flow.TryCollectFragment("canyon.v2.fragment.entrance"),"isolated metadata fixture commits first distinct known fragment");
            TestAssert.Equal(FragmentCollectionResult.Collected,flow.TryCollectFragment("canyon.v2.fragment.rift"),"isolated metadata fixture commits second distinct known fragment");
            level.Progress.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId);
            flow.PersistProgress("basin-lifecycle-fixture");
            int recoveries=0;level.RecoveryCompleted+=_=>recoveries++;
            level.RequestRespawn("basin-counterweight-checkpoint-reset");
            await f.Until(()=>recoveries==1 && flow.State==PrologueGameFlowState.Playing,180,"production checkpoint recovery completes once");
            GD.Print($"BASIN_RECOVERY_BOUNDARY activated={f.Lift.IsActivated} elapsed={f.Lift.ElapsedSeconds} position={f.Lift.GlobalPosition}");
            await f.Until(()=>!f.Lift.IsActivated && f.Lift.ElapsedSeconds==0 &&
                f.Lift.GlobalPosition==new Vector2(2760,4008),6,"existing deferred checkpoint mechanism reset and SyncToPhysics transform complete");
            TestAssert.True(!f.Lift.IsActivated && f.Lift.ElapsedSeconds==0 && f.Lift.GlobalPosition==new Vector2(2760,4008),
                "checkpoint recovery resets lift to inactive lower endpoint");
            AssertProgress(level);
            ulong old=f.Lift.GetInstanceId();
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,new Vector2(5800,1040)),"legal actual Temple stream request");
            await f.Frames(12);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonBasin,out _),"actual remote stream unloads Basin, not visibility-only");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(2492,3880)),"legal Basin remount request");
            await f.Frames(12);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin,out var sector),"actual Basin remounted");
            var fresh=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            TestAssert.True(fresh.GetInstanceId()!=old && !fresh.IsActivated && fresh.ElapsedSeconds==0,"new streamed identity cannot retain lift transient phase");
            AssertProgress(level);
            TestAssert.Equal(1,recoveries,"streaming remount is not extra checkpoint recovery");
            TestAssert.True(flow.TrySaveBeforeQuit(out _),"isolated durable save succeeds before title");
            save=f.Paths.SavePath;
            flow.ReturnToTitle();await f.Frames(3);
            TestAssert.Equal(PrologueGameFlowState.Title,flow.State,"real ReturnToTitle completed");
            GD.Print($"BASIN_LIFECYCLE_OK pause=true recovery={recoveries} unloaded=true fresh_identity=true isolated=true");
        }
        finally {await f.Dispose();}
        var continued=new Fixture(tree,new Vector2(2492,3880),full:true,existingSave:save);
        try
        {
            await continued.Start();
            var flow=continued.Level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave,"ConfigureExisting admitted same isolated saved slot");
            flow.Continue();await continued.Frames(12);
            var sector=continued.Level.CurrentSectorRoot;
            var lift=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            AssertProgress(continued.Level);
            TestAssert.True(!lift.IsActivated && lift.ElapsedSeconds==0,"Continue preserves progress but never transient lift phase");
            GD.Print("BASIN_CONTINUE_OK fragments=2 shortcut=true checkpoint=basin-camp transient_lift=false real_save_untouched=true");
        }
        finally {await continued.Dispose();}
    }

    private static void AssertProgress(PrologueLevelController level)
    {
        TestAssert.True(level.Progress.CollectedFragmentIds.SetEquals(new[]{"canyon.v2.fragment.entrance","canyon.v2.fragment.rift"}) &&
            level.Progress.IsShortcutOpen(CanyonWorldLayout.UpperBasinShortcutId) &&
            level.Progress.RestoreCheckpoint().CheckpointId=="canyon.v2.basin-camp",
            "Basin ride lifecycle retains exact fragment IDs, shortcut and original checkpoint");
    }

    private static void AssertSweep(Fixture f)
    {
        // Independent hand-derived conservative envelope: deck center x2760 +/-60
        // plus full capsule radius20 even for edge-centered riders. Upper deck3844
        // minus full capsule72 =3772; lower deck bottom4020. Not production helpers.
        Rect2 fullSweep = new(2680,3772,160,248);
        var ledge=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
        var ledgeCollision=ledge.GetNode<CollisionShape2D>("Collision");
        float ledgeRight=ledgeCollision.GlobalPosition.X+((RectangleShape2D)ledgeCollision.Shape).Size.X/2;
        float missedBoardingGap=2700f-ledgeRight; // Actual120px deck's left edge; x is fixed.
        TestAssert.True(missedBoardingGap>=44f,
            $"SavedBasinMissedBoardingClearance:40px capsule plus positive margin must fit below upper carriage; actual={missedBoardingGap}");
        foreach (StaticBody2D body in f.Sector.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>())
        {
            var shape = body.GetNodeOrNull<CollisionShape2D>("Collision");
            if (shape?.Shape is not RectangleShape2D rect) continue;
            Rect2 bounds = new(shape.GlobalPosition - rect.Size / 2, rect.Size);
            TestAssert.True(!fullSweep.Intersects(bounds), $"conservative deck/edge-rider sweep clears actual saved static {body.Name}, bounds={bounds}");
        }
        var pistonShape = (RectangleShape2D)f.Piston.Collision.Shape;
        Rect2 pistonSweep = new(2534,3844,132,132);
        TestAssert.True(pistonShape.Size == new Vector2(132,32) && !fullSweep.Intersects(pistonSweep),
            "actual live piston extends/retracts outside passenger sweep");
        GD.Print($"BASIN_SWEEP_OK rider={fullSweep} piston={pistonSweep} minimum_static_margin=4 clearance_piston=14 missed_boarding_gap={missedBoardingGap}");
    }

    private static bool Floor(CharacterBody2D player, Node floor)
    {
        if (!player.IsOnFloor()) return false;
        for (int i=0;i<player.GetSlideCollisionCount();i++)
            if (player.GetSlideCollision(i).GetCollider() == floor && player.GetSlideCollision(i).GetNormal().Y < -.7f) return true;
        return false;
    }

    private sealed class Fixture
    {
        internal readonly SceneTree Tree;
        internal readonly Node2D Root;
        internal PrologueSector Sector;
        internal readonly CharacterBody2D Player;
        internal readonly PrologueLevelController Level;
        internal readonly IsolatedPrologueFixturePaths Paths;
        internal PlayerInputReader Input => Player.GetNode<PlayerInputReader>("Input");
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal MechanicalClawController Claw => Player.GetNode<MechanicalClawController>("MechanicalClaw");
        internal HealthComponent Health => Player.GetNode<HealthComponent>("Health");
        internal CanyonCounterweightLift Lift;
        internal CanyonGearPiston Piston => Sector.GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
        internal int KeyReceipts;
        internal int SampledClawEdges,SampledJumpEdges,SampledDashEdges;
        private ulong _sampleSequence;
        private readonly FormalInputSampleObserver _inputObserver=new() { ProcessPhysicsPriority=-250 };
        private readonly System.Collections.Generic.HashSet<Key> _held = new();
        private readonly System.Collections.Generic.HashSet<string> _captured = new();
        private readonly System.Collections.Generic.HashSet<string> _captureRequests = new();
        private string _captureFolder;
        private Exception _captureFailure;
        private readonly BasinBoardingTickObserver _boardingObserver=new() { ProcessPhysicsPriority=100 };
        private readonly BoardingSample[] _boardingSamples=new BoardingSample[240];
        private int _boardingCount;
        private bool _boardingTrace;
        private float _boardingThreshold;
        private ulong _boardingReleaseTick;
        private Vector2 _boardingReleasePosition;
        private bool _boardingReleaseArmed,_boardingReleased;
        private Exception _boardingFailure;
        internal bool BoardingReleaseCompleted => _boardingFailure==null ? _boardingReleased :
            throw new InvalidOperationException("boarding physics input release failed",_boardingFailure);
        private readonly record struct BoardingSample(ulong Physics,ulong Process,ulong Sequence,
            Vector2 Position,Vector2 Velocity,float Axis,bool RawD,bool OwnedD,bool Floor,float LiftTime,double Delta);
        internal void BeginBoardingTrace(float threshold)
        {
            _boardingThreshold=threshold; _boardingCount=0; _boardingTrace=true;
            _boardingReleaseArmed=true; _boardingReleased=false; _boardingFailure=null;
        }
        private void RecordBoardingRelease()
        {
            _boardingReleaseTick=Engine.GetPhysicsFrames(); _boardingReleasePosition=Player.GlobalPosition;
        }
        internal void AssertBoardingReleaseTimely()
        {
            BoardingSample crossed=default;
            for(int i=0;i<_boardingCount;i++)
                if(_boardingSamples[i].Position.X>=_boardingThreshold) { crossed=_boardingSamples[i];break; }
            TestAssert.True(crossed.Physics!=0 && _boardingReleaseTick>=crossed.Physics &&
                _boardingReleaseTick-crossed.Physics<=1,
                $"BasinBoardingDriverLateRelease: intended threshold must release by next physics tick; crossed={crossed.Physics} release={_boardingReleaseTick} delta={_boardingReleaseTick-crossed.Physics} position={_boardingReleasePosition}");
        }
        private void SampleBoarding(double delta)
        {
            if(!_boardingTrace || _boardingCount>=_boardingSamples.Length) return;
            _boardingSamples[_boardingCount++]=new(Engine.GetPhysicsFrames(),Engine.GetProcessFrames(),
                Input.CurrentFrameSequence,Player.GlobalPosition,Player.Velocity,Input.Current.MoveAxis,
                Godot.Input.IsPhysicalKeyPressed(Godot.Key.D),_held.Contains(Godot.Key.D),
                Floor(Player,Lift),Lift.ElapsedSeconds,delta);
            // The driver must see every physics tick, even when a drawn frame
            // batches several ticks. Release only its own D at the same authored
            // threshold; ordinary production braking/collision remains in charge.
            if(_boardingReleaseArmed && Player.GlobalPosition.X>=_boardingThreshold)
            {
                _boardingReleaseArmed=false;
                try { Key(Godot.Key.D,false); RecordBoardingRelease(); _boardingReleased=true; }
                catch(Exception error) { _boardingFailure=error; }
            }
        }
        internal void CompleteBoardingTrace() => WriteBoardingTrace();
        private void WriteBoardingTrace()
        {
            if(!_boardingTrace) return;
            _boardingTrace=false;
            _boardingReleaseArmed=false;
            GD.Print($"BASIN_BOARDING_TRACE threshold={_boardingThreshold} release_tick={_boardingReleaseTick} release_position={_boardingReleasePosition} count={_boardingCount} overflow={_boardingCount==_boardingSamples.Length}");
            for(int i=0;i<_boardingCount;i++)
            {
                var s=_boardingSamples[i];
                GD.Print($"BASIN_BOARDING_TICK i={i} physics={s.Physics} process={s.Process} sequence={s.Sequence} position={s.Position} velocity={s.Velocity} axis={s.Axis} raw_d={s.RawD} owned_d={s.OwnedD} lift_floor={s.Floor} lift_time={s.LiftTime} delta={s.Delta}");
            }
        }
        internal void EnableCapture()
        {
            _captureFolder=System.IO.Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(),"proof");
            TestAssert.True(!System.IO.Directory.Exists(_captureFolder),"capture output must be a fresh external directory");
            System.IO.Directory.CreateDirectory(_captureFolder);
            RenderingServer.FramePostDraw+=OnCaptureFrame;
            CaptureStage("01-control-station");
        }
        internal void CaptureStage(string stage)
        {
            if(_captureFolder!=null && !_captured.Contains(stage)) _captureRequests.Add(stage);
        }
        private void OnCaptureFrame()
        {
            if(_captureFailure!=null || !Root.IsInsideTree()) return;
            try
            {
                foreach(string stage in _captureRequests.ToArray())
                {
                    using Image pixels=Tree.Root.GetTexture().GetImage();
                    TestAssert.True(pixels.GetWidth()==1280 && pixels.GetHeight()==720,"proof is actual1280x720 viewport");
                    TestAssert.Equal(Error.Ok,pixels.SavePng(System.IO.Path.Combine(_captureFolder,stage+".png")),"save rendered proof PNG");
                    GD.Print($"BASIN_CAPTURE stage={stage} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} actor={Player.GlobalPosition} claw={Claw.CurrentFrame.State} health={Health.CurrentHealth} full_scene={Level!=null} human=false performance=false");
                    _captured.Add(stage);_captureRequests.Remove(stage);
                }
            }
            catch(Exception error) { _captureFailure=error; }
        }
        internal void AssertCaptureComplete()
        {
            if(_captureFailure!=null) throw new InvalidOperationException("rendered proof failed",_captureFailure);
            TestAssert.True(_captured.Count==6 && _captureRequests.Count==0,"all six physical transfer stages were actually rendered");
        }
        internal Fixture(SceneTree tree, Vector2 spawn,bool full=false,string existingSave=null)
        {
            Tree=tree;
            if(full)
            {
                Level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
                Paths=existingSave==null?IsolatedPrologueFixture.Configure(Level,"basin-counterweight-lifecycle"):
                    IsolatedPrologueFixture.ConfigureExisting(Level,existingSave);
                var flow=Level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart=false;flow.AutomationAutoStart=existingSave==null;
                Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector=PrologueSectorId.CanyonBasin;
                Player=Level.GetNode<CharacterBody2D>("PlayerRoot/Player");Player.Position=spawn;
                Root=Level;return;
            }
            Root=new Node2D {Name="BasinCounterweightFixture"};
            Sector=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
            Sector.Position=new Vector2(1280,3600);
            Player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position=spawn;
            Root.AddChild(Sector); Root.AddChild(Player);
        }
        internal async Task Start()
        {
            Root.AddChild(_inputObserver);
            _boardingObserver.Sample=SampleBoarding;
            Root.AddChild(_boardingObserver);
            Tree.Root.AddChild(Root);
            await Frames(8);
            if(Level!=null) Sector=Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").TryGetMountedSector(PrologueSectorId.CanyonBasin,out var mounted)?mounted:null;
            TestAssert.True(Sector!=null,"saved Basin fixture actually mounted");
            Lift=Sector.GetNodeOrNull<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            TestAssert.True(Lift != null, "SavedBasinPassengerLiftMissing: saved Basin must provide a real looping passenger-bearing lift");
            GD.Print($"BASIN_FIXTURE_READY physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={Player.GlobalPosition} velocity={Player.Velocity} lift_time={Lift.ElapsedSeconds} full={Level!=null}");
        }
        internal void Key(Key key, bool down)
        {
            if (_held.Contains(key)==down) return;
            if (down) _held.Add(key); else _held.Remove(key);
            if(down && key is Godot.Key.K or Godot.Key.Space or Godot.Key.Shift) _inputObserver.Arm(Input);
            var inputEvent=new InputEventKey { PhysicalKeycode=key, Keycode=key, Pressed=down };
            Godot.Input.ParseInputEvent(inputEvent); Godot.Input.FlushBufferedEvents();
            TestAssert.True(Godot.Input.IsPhysicalKeyPressed(key)==down, $"native physical key receipt {key}={down}");
            KeyReceipts++;
        }
        internal async Task Press(Key key, int frames) { Key(key,true); await Frames(frames); Key(key,false); }
        internal async Task Frames(int count)
        {
            for (int i=0;i<count;i++)
            {
                await Tree.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
                await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
                // Observe the actual input tick, before action owners consume
                // edges. Slow rendered frames can contain several physics ticks.
                if(_inputObserver.Captured && _inputObserver.FirstSequence!=_sampleSequence)
                {
                    _sampleSequence=_inputObserver.FirstSequence;
                    var sampled=_inputObserver.FirstFrame;
                    if(sampled.ClawPressed) SampledClawEdges++;
                    if(sampled.JumpPressed) SampledJumpEdges++;
                    if(sampled.DashPressed) SampledDashEdges++;
                }
            }
        }
        internal async Task Until(Func<bool> condition,int cap,string description)
        {
            for(int i=0;i<cap && !condition();i++) await Frames(1);
            TestAssert.True(condition(), $"{description}; player={Player.GlobalPosition} lift={Lift?.GlobalPosition} elapsed={Lift?.ElapsedSeconds} claw={Claw.CurrentFrame.State}");
        }
        internal async Task Dispose()
        {
            WriteBoardingTrace();
            _boardingObserver.Sample=null;
            if(_captureFolder!=null) RenderingServer.FramePostDraw-=OnCaptureFrame;
            foreach (Key key in _held.ToArray()) Key(key,false);
            Tree.Paused=false;
            if(Root.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(Tree,Root); else Root.Free();
        }
    }
}

// Test-only post-Motor callback; the fixture may release its owned D here.
internal partial class BasinBoardingTickObserver : Node
{
    internal Action<double> Sample;
    public override void _PhysicsProcess(double delta) => Sample?.Invoke(delta);
}
