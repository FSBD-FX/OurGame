using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Guidance;
using Knight.Prologue.Residents;
using Knight.Settings;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

public static partial class CanyonMechanismRuntimeTests
{
    public static async Task RunRecyclingFogCommands(SceneTree tree)
    {
        Type type=typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Presentation.Fog.FogRecyclingReadabilityOverlay");
        TestAssert.True(type!=null,
            "RECYCLING_FOG_MISSING: saved hazards require a real protected native-footprint consumer, not high ZIndex alone");
        var world=new Node2D { Name="RecyclingFogCommandFixture" };
        var sector=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn").Instantiate<PrologueSector>();
        var overlay=(Node2D)Activator.CreateInstance(type);
        var layer=new CanvasLayer { Name="ProtectedCues", Layer=2, FollowViewportEnabled=true };
        var ownedWall=new RectangleShape2D { Size=new(8,236) };
        HeadlampOcclusionSampler sampler=null;
        try
        {
            world.AddChild(sector); layer.AddChild(overlay); world.AddChild(layer);
            tree.Root.AddChild(world); await Frames(tree,3);
            var pad=sector.GetNode<CanyonSpringboard>("Gameplay/Mechanisms/CombinationSpringboard");
            var gear=sector.GetNode<CanyonFixedGear>("Gameplay/Mechanisms/CombinationFixedGear");
            sampler=new HeadlampOcclusionSampler(world);
            HeadlampOcclusionFrame frame=sampler.Capture();
            TestAssert.True(frame.IsComplete,"command fixture uses a complete current native terrain snapshot");
            var sources=new CollisionObject2D[] {pad,gear,gear};
            void Present(Vector2? actor=null, HeadlampOcclusionFrame snapshot=null) =>
                type.GetMethod("PresentSources").Invoke(overlay,new object[]{sources,actor??new Vector2(160,600),snapshot??frame});
            int Count(string property)=>(int)type.GetProperty(property).GetValue(overlay);
            void Positive()
            {
                Present();
                TestAssert.True(Count("PresentedGearCount")==1 && Count("PresentedPadCount")==1 &&
                    Count("PresentedSegmentCount")>=5 && ReferenceEquals(type.GetProperty("LastOcclusionFrame").GetValue(overlay),frame),
                    "both actual nearby sources submit one deduplicated protected command set from the same frame");
            }
            Positive();
            gear.Visible=false; Present(); TestAssert.Equal(0,Count("PresentedGearCount"),"hidden native gear withdraws commands");
            gear.Visible=true; var collider=gear.GetNode<CollisionShape2D>("Collision");
            collider.Disabled=true; await Frames(tree,2); frame=sampler.Capture(); Present();
            TestAssert.Equal(0,Count("PresentedGearCount"),"disabled native hazard cannot retain a danger cue");
            collider.Disabled=false; await Frames(tree,2); frame=sampler.Capture(); Positive();
            var wall=new StaticBody2D { Position=new(152,518), CollisionLayer=CombatCollisionLayers.World,
                CollisionMask=CombatCollisionLayers.None };
            wall.AddChild(new CollisionShape2D { Shape=ownedWall }); world.AddChild(wall);
            await Frames(tree,2); frame=sampler.Capture(); Present();
            TestAssert.True(Count("PresentedGearCount")==0 && Count("PresentedPadCount")==1,
                "actual intervening wall hides every gear edge without hiding the clear-side pad or revealing a room");
            wall.QueueFree(); await Frames(tree,2); frame=sampler.Capture(); Positive();
            Present(new Vector2(2000,600)); TestAssert.Equal(0,Count("PresentedSegmentCount"),"local cue range does not reveal distant devices");
            Present(snapshot:new HeadlampOcclusionFrame(Array.Empty<HeadlampOccluder>(),false,"negative control"));
            TestAssert.Equal(0,Count("PresentedSegmentCount"),"incomplete snapshots never authorize retained commands");
            sector.SetMeta("_seamless_staged",true); Present();
            TestAssert.Equal(0,Count("PresentedSegmentCount"),"staged owner withdraws both current source identities");
            sector.RemoveMeta("_seamless_staged"); Positive();
            tree.Paused=true; Present();
            TestAssert.True(Count("PresentedSegmentCount")==0 && type.GetProperty("LastOcclusionFrame").GetValue(overlay)==null,
                "pause withdraws geometry and provenance, not a cached last picture");
            tree.Paused=false; Positive(); world.QueueFree(); Present();
            TestAssert.Equal(0,Count("PresentedSegmentCount"),"queued consumer ancestor clears without repainting stale devices");
            GD.Print("RECYCLING_FOG_COMMANDS_OK native_saved_sources=true same_snapshot=true deduplicated=true " +
                "wall=true hidden_disabled=true staged_pause_queued=true local_range=true pixels=false human=false");
        }
        finally
        {
            tree.Paused=false; sampler?.Dispose();
            if(GodotObject.IsInstanceValid(world) && !world.IsQueuedForDeletion()) world.QueueFree();
            await Frames(tree,2); ownedWall.Dispose();
        }
    }

    public static async Task RunGeneratedRecyclingPassage(SceneTree tree)
    {
        bool sawActualLaunchCard=false;
        int sourceCardCases=0;
        for (int variant=0; variant<2; variant++)
        {
            var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            KeyboardRouteInputDriver driver=null;
            try
            {
                IsolatedPrologueFixture.Configure(level,"recycling-entrance-"+variant);
                var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
                tree.Root.AddChild(level);
                for(int i=0;i<3;i++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                TestAssert.True(flow.State==PrologueGameFlowState.Title &&
                    level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").HasFocus(),
                    "generated recycling fixture starts on the real focused Main title");
                KeyboardActionRuntimeTests.KeyEvent(Key.Enter,true);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                KeyboardActionRuntimeTests.KeyEvent(Key.Enter,false);
                for(int i=0;i<30 && flow.State!=PrologueGameFlowState.Playing;i++)
                    await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                TestAssert.Equal(PrologueGameFlowState.Playing,flow.State,"actual Enter starts the saved normal Main");
                CharacterBody2D player=level.Player;
                var input=player.GetNode<PlayerInputReader>("Input");
                var health=player.GetNode<HealthComponent>("Health");
                var motor=player.GetNode<PlayerMotor>("Motor");
                await ThrusterPlayerRuntimeTests.Frames(tree,input,8);
                TestAssert.True(player.IsOnFloor() && player.Position.DistanceTo(new(160,600))<2 && health.CurrentHealth==5,
                    "normal unchanged start has real support and full original Health");
                PrologueSector sector=level.CurrentSectorRoot;
                var pad=sector.GetNode<CanyonSpringboard>("Gameplay/Mechanisms/CombinationSpringboard");
                var gear=sector.GetNode<CanyonFixedGear>("Gameplay/Mechanisms/CombinationFixedGear");
                var guidance=level.GetNode<PrologueGuidanceController>("Guidance");
                guidance.SetLanguage(GameLanguage.ChineseSimplified); guidance.RefreshNow();
                TestAssert.True(guidance.CurrentInstruction.Visible &&
                    guidance.CurrentInstruction.StepText.Contains("弹板") && guidance.CurrentInstruction.StepText.Contains("齿轮") &&
                    guidance.CurrentInstruction.StepText.Contains("SPACE") && guidance.CurrentInstruction.StepText.Contains("反推"),
                    "RECYCLING_GUIDANCE_MISSING: the actual visible saved combination must announce pressure, danger, jump and reverse-brake controls in the existing fixed reading area");
                TestAssert.True(guidance.CurrentInstruction.Targets.Count==0,
                    "mechanism ECHO uses the existing fixed view without world target boxes");
                gear.Visible=false; guidance.RefreshNow();
                TestAssert.True(!guidance.CurrentInstruction.StepText.Contains("弹板"),
                    "a withdrawn actual paired gear cannot authorize the combination card");
                gear.Visible=true; guidance.SetGuidanceEnabled(false);
                TestAssert.True(!guidance.CurrentInstruction.Visible,"the actual guidance option clears the device card");
                guidance.SetGuidanceEnabled(true); guidance.RefreshNow();
                TestAssert.True(guidance.CurrentInstruction.StepText.Contains("弹板"),"current restored sources restore their card");
                InputEvent[] oldJump=InputMap.ActionGetEvents(InputActions.Jump).ToArray();
                InputEvent[] oldRight=InputMap.ActionGetEvents(InputActions.MoveRight).ToArray();
                try
                {
                    InputMap.ActionEraseEvents(InputActions.Jump);
                    InputMap.ActionAddEvent(InputActions.Jump,new InputEventKey { PhysicalKeycode=Key.H });
                    InputMap.ActionEraseEvents(InputActions.MoveRight);
                    InputMap.ActionAddEvent(InputActions.MoveRight,new InputEventKey { PhysicalKeycode=Key.N });
                    guidance.RefreshNow();
                    TestAssert.True(guidance.CurrentInstruction.StepText.Contains("H：跳过弹板") &&
                        guidance.CurrentInstruction.StepText.Contains("N：空中反推制动") &&
                        !guidance.CurrentInstruction.StepText.Contains("SPACE"),
                        "normal source-bound combination card resolves the actual live jump/brake bindings");
                }
                finally
                {
                    InputMap.ActionEraseEvents(InputActions.Jump);foreach(InputEvent binding in oldJump) InputMap.ActionAddEvent(InputActions.Jump,binding);
                    InputMap.ActionEraseEvents(InputActions.MoveRight);foreach(InputEvent binding in oldRight) InputMap.ActionAddEvent(InputActions.MoveRight,binding);
                    guidance.RefreshNow();
                }
                TestAssert.True(guidance.CurrentInstruction.StepText.Contains("SPACE") && guidance.CurrentInstruction.StepText.Contains("D：空中反推制动"),
                    "real combination controls restore defaults before ordinary route input starts");
                sourceCardCases++;
                int deaths=0,recoveries=0; ulong launchFrame=0,deathFrame=0,gearAtDeath=0;
                int jumpsAtLaunch=-1;
                string routeStage=variant==0?"unreacted_walk":"outbound_first";
                pad.Launched+=(_,_,_)=>{launchFrame=Engine.GetPhysicsFrames();jumpsAtLaunch=motor.AcceptedJumpCount;};
                health.Died+=()=>{deaths++;deathFrame=Engine.GetPhysicsFrames();gearAtDeath=gear.ContactSequence;};
                health.Damaged+=(remaining,_)=>GD.Print($"RECYCLING_GENERATED_DAMAGE_OBSERVATION variant={variant} " +
                    $"stage={routeStage} hp={remaining} position={player.GlobalPosition} velocity={player.Velocity} " +
                    $"physics={Engine.GetPhysicsFrames()} pad={pad.LaunchSequence} gear={gear.ContactSequence} jumps={motor.AcceptedJumpCount}");
                level.RecoveryRequested+=_=>recoveries++;
                driver=KeyboardRouteInputDriver.Begin(player);
                driver.Command(InputActions.MoveRight,true);
                ulong playerId=player.GetInstanceId();
                async Task Tick()
                {
                    await ThrusterPlayerRuntimeTests.Frames(tree,input,1);
                    TestAssert.True(level.Player==player && player.GetInstanceId()==playerId,
                        "the real mounted player identity is never substituted");
                    if(variant==0 && !sawActualLaunchCard && launchFrame>0 && health.CurrentHealth>0)
                    {
                        guidance.RefreshNow();
                        TestAssert.True(guidance.CurrentInstruction.StepText.Contains("弹板已弹射") &&
                            guidance.CurrentInstruction.Targets.Count==0,
                            "the actual captured source launch, not a button or guessed position, selects the launched card");
                        sawActualLaunchCard=true;
                    }
                    if(variant==1) TestAssert.True(deaths==0 && recoveries==0 && health.CurrentHealth==5,
                        $"generated bypass uses no death/recovery/HP write: stage={routeStage} pos={player.GlobalPosition} " +
                        $"hp={health.CurrentHealth} deaths={deaths} recoveries={recoveries} pad={pad.LaunchSequence}");
                }
                if(variant==0)
                {
                    for(int i=0;i<160 && deaths==0;i++) await Tick();
                    TestAssert.True(deaths==1 && launchFrame>0 && deathFrame>launchFrame && gearAtDeath>0 &&
                        jumpsAtLaunch==0,
                        $"RECYCLING_GENERATED_COMBO_RED: real saved placement must produce the claimed chain, " +
                        $"not a fixture-only trajectory; deaths={deaths} hp={health.CurrentHealth} " +
                        $"launch={launchFrame} death={deathFrame} gear_at_death={gearAtDeath} " +
                        $"position={player.GlobalPosition} velocity={player.Velocity}");
                    GD.Print("RECYCLING_GENERATED_COMBO_OK saved_main=true actual_enter=true actual_pad_then_gear=true " +
                        "no_space=true initial_pose_writes=0 human=false whole_route=false");
                }
                else
                {
                    async Task JumpPlateToFragment()
                    {
                        for(int i=0;i<30 && player.Position.X<176;i++) await Tick();
                        TestAssert.True(player.IsOnFloor() && player.Position.X<195,"first jump begins before the actual plate");
                        driver.Command(InputActions.Jump,true);
                        for(int i=0;i<7;i++) await Tick();
                        driver.Command(InputActions.Jump,false);
                        for(int i=0;i<50 && player.Position.X<280;i++) await Tick();
                        driver.Command(InputActions.MoveRight,false);
                        for(int i=0;i<40 && !player.IsOnFloor();i++) await Tick();
                        TestAssert.True(player.IsOnFloor() && player.Position.X is >=270 and <=310 && pad.LaunchSequence==0,
                            $"RECYCLING_GENERATED_BYPASS_RED: ordinary short jump lands beyond the plate, " +
                            $"before erosion; pos={player.Position} velocity={player.Velocity} pad={pad.LaunchSequence}");
                    }
                    await JumpPlateToFragment();
                    TestAssert.True(level.Progress.CollectedFragmentIds.Contains("canyon.v2.fragment.entrance"),
                        "the unchanged first fragment is actually collected on the live passage");
                    routeStage="reverse_runup";
                    driver.Command(InputActions.MoveLeft,true);
                    for(int i=0;i<30 && (player.Position.X>260 || player.Velocity.X>-240);i++) await Tick();
                    TestAssert.True(player.IsOnFloor() && player.Position.X>252 && pad.LaunchSequence==0,
                        "reverse run-up remains on the far side of the live pressure plate");
                    routeStage="return_jump"; driver.Command(InputActions.Jump,true);
                    for(int i=0;i<7;i++) await Tick();
                    driver.Command(InputActions.Jump,false);
                    int returnFrames=0;
                    for(;returnFrames<100 && player.Position.X>196;returnFrames++) await Tick();
                    // The real leftward return clears the plate first. Brake in
                    // the air on its safe left side, land, then walk under the
                    // overhead gear. All stages share the previous 50+50 cap.
                    routeStage="return_air_brake"; driver.Command(InputActions.MoveLeft,false);
                    driver.Command(InputActions.MoveRight,true);
                    for(;returnFrames<100 && player.Velocity.X<0;returnFrames++) await Tick();
                    driver.Command(InputActions.MoveRight,false); routeStage="return_settle";
                    for(;returnFrames<100 && !player.IsOnFloor();returnFrames++) await Tick();
                    TestAssert.True(returnFrames<100 && player.IsOnFloor() && player.Position.X<204 && pad.LaunchSequence==0,
                        "opposite-key air braking yields a real safe-side landing before walking beneath the overhead gear");
                    routeStage="return_ground_walk"; driver.Command(InputActions.MoveLeft,true);
                    for(;returnFrames<100 && player.Position.X>160;returnFrames++) await Tick();
                    driver.Command(InputActions.MoveLeft,false);
                    TestAssert.True(player.IsOnFloor() && player.Position.X<176 &&
                        motor.AcceptedJumpCount==2 && motor.AcceptedDashCount==0 && pad.LaunchSequence==0,
                        $"RECYCLING_GENERATED_RETURN_RED: original low remnant must permit a genuine return; " +
                        $"pos={player.Position} jumps={motor.AcceptedJumpCount} pad={pad.LaunchSequence}");
                    routeStage="outbound_again"; driver.Command(InputActions.MoveRight,true); await JumpPlateToFragment();
                    driver.Command(InputActions.MoveRight,true);
                    for(int i=0;i<30 && player.Position.X<300;i++) await Tick();
                    TestAssert.True(player.IsOnFloor() && player.Position.X is >=295 and <=310,
                        "unchanged near erosion bank is still a genuinely grounded launch");
                    routeStage="erosion_gap"; driver.Command(InputActions.Jump,true);
                    bool farBank=false;
                    for(int i=0;i<100;i++)
                    {
                        await Tick();
                        if(player.Position.X>=440) driver.Command(InputActions.MoveRight,false);
                        for(int j=0;j<player.GetSlideCollisionCount();j++)
                        {
                            using KinematicCollision2D hit=player.GetSlideCollision(j);
                            farBank|=hit.GetCollider() is StaticBody2D { Name: var name } &&
                                name=="BridgeFarBank" && hit.GetNormal().Y<-.98f;
                        }
                        if(farBank && player.IsOnFloor() && player.Position.X>=440) break;
                    }
                    driver.Command(InputActions.Jump,false); driver.Command(InputActions.MoveRight,false);
                    TestAssert.True(farBank && player.IsOnFloor() && motor.AcceptedJumpCount==4 &&
                        motor.AcceptedDashCount==0 && pad.LaunchSequence==0,
                        "new plate bypass plus unchanged erosion gap use four distinct grounded jumps across the complete out/back/out route");
                    guidance.RefreshNow();
                    TestAssert.True(!guidance.CurrentInstruction.StepText.Contains("弹板"),"leaving the actual combination region withdraws its card");
                    var environment=ResidentEnvironmentInventory.Capture(level.GetNode<Node2D>("WorldRoot"));
                    TestAssert.Equal(0,environment.Issues.Count,"mounted environment reservations remain structurally valid");
                    foreach((string prefix,string name,Rect2 bounds) in new[]{
                        ("fixed-gear","CombinationFixedGear",new Rect2(86,476,60,60)),
                        ("fixed-gear","PracticeFixedGear",new Rect2(870,400,60,60)),
                        ("springboard","CombinationSpringboard",new Rect2(204,624,48,12)),
                        ("springboard","PracticeSpringboard",new Rect2(756,624,48,12))})
                    {
                        string id=prefix+":Sectors/CanyonEntrance/Gameplay/Mechanisms/"+name+"/Collision";
                        ResidentKeepOut[] found=environment.Regions.Where(region=>region.SourceId==id).ToArray();
                        TestAssert.True(found.Length==1 && found[0].Kind==ResidentKeepOutKind.Hazard && found[0].WorldBounds==bounds,
                            "new hazard reservation has a unique literal identity and actual saved footprint: "+id);
                    }
                    TestAssert.Equal(158,level.GetNode<Knight.Presentation.Minimap.PrologueMinimapController>("MinimapController").Catalog.SolidCount,
                        "new Areas do not masquerade as terrain; current minimap includes the structural annex shapes");
                    GD.Print("RECYCLING_INVENTORY_OK identities=4 native_scene_bounds=true hazard_kind=true solids=158 whole_map_census=false");
                    GD.Print("RECYCLING_GENERATED_PASSAGE_OK saved_main=true actual_enter=true out_back_out=true " +
                        "first_fragment=true erosion_gap=true jumps=4 dash=0 health=5->5 recovery=0 pose_writes=0 " +
                        "real_air_brake=true human=false whole_route=false");
                }
            }
            finally
            {
                driver?.Dispose(); KeyboardActionRuntimeTests.KeyEvent(Key.Enter,false);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
            }
        }
        TestAssert.True(sourceCardCases==2 && sawActualLaunchCard,"both real saved sessions and the actual launch event exercised ECHO");
        GD.Print("RECYCLING_GUIDANCE_RUNTIME_OK actual_sources=true hidden_source_veto=true setting_clear=true " +
            "launch_event=true fixed_existing_view=true no_world_targets=true pixels=false");
    }

    public static void RunSavedRecyclingContract()
    {
        // Off-tree artifact contract only; ordinary route and visible fog are
        // separate gates, not inferred from four nodes in a PackedScene.
        PrologueSector child = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn")
            .Instantiate<PrologueSector>();
        PrologueLevelController parent = null;
        PrologueSector referenced = null;
        try
        {
            AssertSaved(child, "saved child");
            parent = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            PrologueSectorDefinition definition = parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors")
                .Definitions.Single(item => item.Id == PrologueSectorId.CanyonEntrance);
            referenced = definition.Scene.Instantiate<PrologueSector>();
            AssertSaved(referenced, "parent-referenced child");
            TestAssert.True(parent.GetNodeOrNull<Node2D>("ProtectedCues/Recycling")?.GetType().FullName==
                "Knight.Presentation.Fog.FogRecyclingReadabilityOverlay",
                "saved parent contains the new protected consumer rather than relying on unprotected world Draw");
            QuestFragmentPlacementTests.Run();
            GD.Print("RECYCLING_SAVED_CONTRACT_OK child=true parent_reference=true pads=2 gears=2 " +
                "native_shapes_authored=true fragment_contract=9 structural_only=true passage=false pixels=false");
        }
        finally
        {
            referenced?.Free(); parent?.Free(); child.Free();
        }

        static void AssertSaved(PrologueSector sector, string context)
        {
            Node mechanisms = sector.GetNode("Gameplay/Mechanisms");
            var combo = mechanisms.GetNodeOrNull<CanyonSpringboard>("CombinationSpringboard");
            var gear = mechanisms.GetNodeOrNull<CanyonFixedGear>("CombinationFixedGear");
            var practice = mechanisms.GetNodeOrNull<CanyonSpringboard>("PracticeSpringboard");
            var practiceGear = mechanisms.GetNodeOrNull<CanyonFixedGear>("PracticeFixedGear");
            TestAssert.True(combo != null && gear != null && practice != null && practiceGear != null,
                $"RECYCLING_SAVED_MISSING: {context} must actually contain both new mechanism families, not only fixtures");
            TestAssert.True(mechanisms.GetChildren().OfType<CanyonSpringboard>().Count() == 2 &&
                mechanisms.GetChildren().OfType<CanyonFixedGear>().Count() == 2,
                "saved entrance has exactly the four requested combo/practice devices");
            TestAssert.True(combo.Position == new Vector2(228, 636) && combo.Width == 48f &&
                combo.LaunchVelocity == new Vector2(-620, -480) &&
                combo.SupportBodyPath.ToString() == "../../Collision/StartFloor" &&
                gear.Position == new Vector2(116, 506) && gear.Radius == 30f && gear.Damage == 5 &&
                practice.Position == new Vector2(780, 636) && practice.Width == 48f &&
                practice.LaunchVelocity == new Vector2(0, -420) &&
                practice.SupportBodyPath.ToString() == "../../Collision/BridgeFarBank" &&
                practiceGear.Position == new Vector2(900, 430) && practiceGear.Radius == 30f,
                "saved literal placement/force/support preserve the candidate's actual meaning");
            foreach (CanyonSpringboard pad in new[] { combo, practice })
                TestAssert.True(pad.GetNode<CollisionShape2D>("Collision") is
                    { Position: { X: 0, Y: -6 }, Shape: RectangleShape2D { Size: { X: 48, Y: 12 } } },
                    "saved plate sensor fits the authored top width exactly");
            foreach (CanyonFixedGear source in new[] { gear, practiceGear })
                TestAssert.True(source.GetNode<CollisionShape2D>("Collision") is
                    { Position: { X: 0, Y: 0 }, Shape: CircleShape2D { Radius: 30 } },
                    "saved native gear circle matches its damaging radius");
        }
    }

    public static async Task RunDirectionalSpringboard(SceneTree tree)
    {
        // Controlled native fixture, not whole-map or normal-display proof.
        var root = new Node2D { Name = "DirectionalSpringboardFixture" };
        var owned = new List<Shape2D>();
        KeyboardRouteInputDriver driver = null;
        try
        {
            var floor = new StaticBody2D { Name = "Floor", CollisionLayer = CombatCollisionLayers.World,
                CollisionMask = CombatCollisionLayers.None };
            var floorShape = new RectangleShape2D { Size = new Vector2(800, 40) }; owned.Add(floorShape);
            floor.AddChild(new CollisionShape2D { Position = new Vector2(400, 520), Shape = floorShape });
            root.AddChild(floor);
            CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.Name = "Player"; player.Position = new Vector2(180, 464);
            // PrologueLevelController registers its saved Player this way.
            player.AddToGroup("player");
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            root.AddChild(player); tree.Root.AddChild(root); await Frames(tree, 8);
            var health = player.GetNode<HealthComponent>("Health");
            var motor = player.GetNode<PlayerMotor>("Motor");
            int hp = health.CurrentHealth, jumps = motor.AcceptedJumpCount;
            TestAssert.True(player.IsOnFloor() && hp == 5 && !motor.IsDashing && player.IsInGroup("player"),
                "spring fixture starts with saved live Player on real support and unchanged mobility/resources");
            Type boardType = typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Mechanisms.CanyonSpringboard");
            TestAssert.True(boardType != null,
                "DIRECTIONAL_SPRINGBOARD_MISSING: real support has no directional springboard mechanism yet");
            var board = (Area2D)Activator.CreateInstance(boardType);
            board.Name = "Pad"; board.Position = new Vector2(300, 500);
            boardType.GetProperty("SupportBodyPath").SetValue(board, new NodePath("../Floor"));
            var padShape = new RectangleShape2D { Size = new Vector2(64, 12) }; owned.Add(padShape);
            board.AddChild(new CollisionShape2D { Name = "Collision", Position = new Vector2(0, -6), Shape = padShape });
            root.AddChild(board); await Frames(tree, 2);
            long Sequence() => Convert.ToInt64(boardType.GetProperty("LaunchSequence").GetValue(board));
            driver = KeyboardRouteInputDriver.Begin(player);
            driver.Command(InputActions.MoveRight, true);
            int frames = 0;
            bool sampledRight = false;
            for (; frames < 80 && Sequence() == 0; frames++)
            {
                sampledRight |= Input.IsPhysicalKeyPressed(Key.D) &&
                    player.GetNode<PlayerInputReader>("Input").Current.KeyboardActions;
                await Frames(tree, 1);
                if (player.GlobalPosition.X is < 270f or > 330f || frames % 3 != 0) continue;
                Godot.Collections.Array<Node2D> overlaps = board.GetOverlappingBodies();
                using Godot.Collections.Array backing = (Godot.Collections.Array)overlaps;
                GD.Print($"SPRING_CONTACT_OBSERVATION frame={frames} player={player.GlobalPosition} velocity={player.Velocity} " +
                    $"floor={player.IsOnFloor()} contacts={player.GetSlideCollisionCount()} player_layer={player.CollisionLayer} " +
                    $"sensor={board.Monitoring}/{board.CollisionMask} overlap={overlaps.Count} support={floor.GetRid().Id}");
                for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                {
                    KinematicCollision2D hit = player.GetSlideCollision(index);
                    GD.Print($"SPRING_CONTACT_POINT collider={hit.GetColliderRid().Id} point={hit.GetPosition()} " +
                        $"normal={hit.GetNormal()} local={board.ToLocal(hit.GetPosition())}");
                }
            }
            Vector2 launched = player.Velocity;
            driver.Command(InputActions.MoveRight, false);
            TestAssert.True(Sequence() == 1 && frames < 80 && launched.X < -400f && launched.Y < -300f &&
                health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps &&
                sampledRight && driver.KeyDownCount > 0,
                $"DIRECTIONAL_SPRINGBOARD_LAUNCH_RED: real D walk must become one physical left/up launch, " +
                $"sequence={Sequence()} frames={frames} velocity={launched} hp={hp}->{health.CurrentHealth} " +
                $"jumps={jumps}->{motor.AcceptedJumpCount} sampled_real_D={sampledRight}");
            driver.Command(InputActions.MoveRight, true);
            float firstUpward = 0f;
            for (int i = 0; i < 8; i++)
            {
                await ThrusterPlayerRuntimeTests.Frames(tree, player.GetNode<PlayerInputReader>("Input"), 1);
                if (i == 0) firstUpward = player.Velocity.Y;
            }
            TestAssert.True(firstUpward < -400f,
                $"ENVIRONMENT_LAUNCH_JUMP_CUT_RED: unheld Space must not turn a real -480 external launch " +
                $"into an ordinary short jump; first_observed_vy={firstUpward:R} " +
                $"hp={health.CurrentHealth} ordinary_jumps={motor.AcceptedJumpCount - jumps}");
            TestAssert.True(player.Velocity.X > launched.X && player.Velocity.X < 0f && !player.IsOnFloor() &&
                health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps && Sequence() == 1,
                "opposite D genuinely brakes leftward airborne inertia without instant reversal, extra jump or repeated pad launch");
            GD.Print("CANYON_SPRINGBOARD_RUNTIME_OK real_keyboard=true real_floor_contact=true launch=left_up " +
                "reverse_brake=true launch_count=1 extra_jump=0 pose_writes=0 whole_route=false human=false");
        }
        finally
        {
            driver?.Dispose();
            root.QueueFree(); await Frames(tree, 2);
            foreach (Shape2D shape in owned) shape.Dispose();
        }
    }

    public static async Task RunSpringboardControls(SceneTree tree)
    {
        int cases = 0;
        async Task Check(string name, Action<SpringboardFixture> setup, Func<SpringboardFixture, Task> body)
        {
            var f = new SpringboardFixture(tree);
            try
            {
                setup(f); await f.Mount(); await body(f); cases++;
                GD.Print("SPRINGBOARD_CONTROL_OK " + name);
            }
            finally { await f.DisposeAsync(); }
        }
        await Check("ordinary_space_bypasses_plate", f => { }, async f =>
        {
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight, true);
            for (int i = 0; i < 50 && f.Player.Position.X < 240f; i++) await Frames(tree, 1);
            TestAssert.True(f.Player.IsOnFloor() && f.Board.LaunchSequence == 0,
                "Space starts before actual pad rather than after a launch");
            int jumps = f.Motor.AcceptedJumpCount;
            f.Driver.Command(InputActions.Jump, true); await Frames(tree, 7);
            f.Driver.Command(InputActions.Jump, false);
            for (int i = 0; i < 60 && f.Player.Position.X < 380f; i++) await Frames(tree, 1);
            f.Driver.Command(InputActions.MoveRight, false);
            for (int i = 0; i < 80 && !f.Player.IsOnFloor(); i++) await Frames(tree, 1);
            TestAssert.True(f.Player.Position.X > 360f && f.Player.IsOnFloor() &&
                f.Board.LaunchSequence == 0 && f.Motor.AcceptedJumpCount == jumps + 1 && f.Health.CurrentHealth == 5,
                "one real ordinary Space clears the pressure plate and naturally lands on unchanged floor");
        });
        await Check("unregistered_body_rejected", f =>
        {
            f.Player.Position = new Vector2(300, 464); f.RegisterPlayer = false;
        }, async f =>
        {
            await Frames(tree, 20);
            TestAssert.True(f.Player.IsOnFloor() && !f.Player.IsInGroup("player") &&
                f.Board.LaunchSequence == 0 && f.Player.Velocity == Vector2.Zero,
                "saved healthy body without production player registration never launches");
        });
        await Check("below_one_way_support_rejected", f =>
        {
            f.FloorCollider.OneWayCollision = true; f.FloorCollider.OneWayCollisionMargin = 1f;
            f.Player.Position = new Vector2(300, 514);
            f.AddFloor("LowerFloor", new Rect2(0, 550, 800, 40));
        }, async f =>
        {
            await Frames(tree, 20);
            Godot.Collections.Array<Node2D> overlaps = f.Board.GetOverlappingBodies();
            using Godot.Collections.Array backing = (Godot.Collections.Array)overlaps;
            bool lower = false;
            for (int i = 0; i < f.Player.GetSlideCollisionCount(); i++)
                lower |= f.Player.GetSlideCollision(i).GetColliderRid() == f.LowerFloor.GetRid();
            TestAssert.True(f.Player.IsOnFloor() && lower && overlaps.Contains(f.Player) &&
                f.Board.LaunchSequence == 0 && f.Health.CurrentHealth == 5,
                "actual below-sensor overlap and another real floor are not top contact on the configured one-way support");
        });
        await Check("occupancy_pause_checkpoint_sequence", f =>
        {
            f.Player.Position = new Vector2(300, 464); f.Board.LaunchVelocity = new(-160, 0);
            f.Board.RechargeSeconds = .2f;
        }, async f =>
        {
            await Frames(tree, 40);
            TestAssert.True(f.Board.LaunchSequence == 1 && f.Player.IsOnFloor() && f.Player.Velocity.IsZeroApprox(),
                "after recharge expires a resting body still occupying the plate is not repeatedly launched");
            tree.Paused = true;
            try
            {
                f.Board.ResetForCheckpoint(); f.Board._PhysicsProcess(2d);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.Equal(1UL, f.Board.LaunchSequence, "paused direct stale callback cannot launch even after reset");
            }
            finally { tree.Paused = false; }
            await Frames(tree, 4);
            TestAssert.Equal(2UL, f.Board.LaunchSequence,
                "normal unpaused fresh reset permits one new launch without replaying the old sequence");
        });
        await Check("snapshot_and_natural_reentry", f =>
        {
            f.Player.Position = new Vector2(300, 464); f.Board.LaunchVelocity = new(-160, 0);
            f.Board.RechargeSeconds = .2f;
        }, async f =>
        {
            await Frames(tree, 12);
            bool invalidRejected = false; f.Board.Width = float.NaN;
            try { f.Board._Ready(); }
            catch (InvalidOperationException) { invalidRejected = true; }
            finally { f.Board.Width = 64f; }
            TestAssert.True(invalidRejected, "invalid reinitialization is rejected before partially replacing the live snapshot");
            f.Board.LaunchVelocity = new(600, 0);
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight, true);
            for (int i = 0; i < 80 && f.Player.Position.X < 380f; i++) await Frames(tree, 1);
            f.Driver.Command(InputActions.MoveRight, false);
            f.Driver.Command(InputActions.MoveLeft, true);
            for (int i = 0; i < 80 && f.Board.LaunchSequence < 2; i++) await Frames(tree, 1);
            TestAssert.True(f.Board.LaunchSequence == 2 && f.Player.Velocity.X < -100f &&
                f.Health.CurrentHealth == 5 && f.Motor.AcceptedJumpCount == 0,
                "leaving and naturally walking back rearms with the original immutable left launch, not a hot-edit right velocity");
        });
        await Check("dead_player_rejected", f =>
        {
            f.Player.Position = new Vector2(300, 464); f.Board.LaunchVelocity = new(-160, 0);
        }, async f =>
        {
            await Frames(tree, 12);
            f.Health.ReceiveDamage(new DamageRequest(5, Vector2.Zero, 0, 99101, f.Board.GetInstanceId(), DamageGuardClass.Unblockable));
            ulong sequence = f.Board.LaunchSequence;
            f.Board.ResetForCheckpoint(); f.Board._PhysicsProcess(2d);
            TestAssert.True(f.Health.CurrentHealth == 0 && f.Board.LaunchSequence == sequence,
                "dead saved player is not revived or launched by reset/late sensor delivery");
        });
        await Check("actual_shift_burn_replaced", f => { }, async f =>
        {
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight, true);
            for (int i = 0; i < 50 && f.Player.Position.X < 220f; i++) await Frames(tree, 1);
            int dashes = f.Motor.AcceptedDashCount, jumps = f.Motor.AcceptedJumpCount;
            f.Driver.Command(InputActions.Dash, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            TestAssert.True(f.Motor.IsDashing && f.Motor.ThrusterState.BurnRemaining > .1f &&
                f.Motor.AcceptedDashCount == dashes + 1 && f.Board.LaunchSequence == 0,
                $"actual fresh D+Shift burns before native pad contact, not a pre-accept abort; " +
                $"burning={f.Motor.IsDashing} remaining={f.Motor.ThrusterState.BurnRemaining:R} " +
                $"dashes={dashes}->{f.Motor.AcceptedDashCount} pad={f.Board.LaunchSequence} " +
                $"player={f.Player.Position} velocity={f.Player.Velocity} raw_shift={Input.IsPhysicalKeyPressed(Key.Shift)} " +
                $"sampled_dash={f.Player.GetNode<PlayerInputReader>("Input").Current.DashPressed}");
            for (int i = 0; i < 8 && f.Board.LaunchSequence == 0; i++)
                await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            TestAssert.True(f.Board.LaunchSequence == 1 && !f.Motor.IsDashing &&
                f.Motor.ThrusterState.BurnRemaining == 0 && f.Player.Velocity.X < -400f &&
                f.Player.Velocity.Y < -300f && f.Motor.AcceptedDashCount == dashes + 1,
                "real plate contact interrupts the still-live burn and owns physical left/up launch");
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 5);
            TestAssert.True(!f.Motor.IsDashing && f.Motor.AcceptedDashCount == dashes + 1 &&
                f.Motor.AcceptedJumpCount == jumps && f.Health.CurrentHealth == 5,
                "held Shift is not replayed by claw cleanup or pad takeover");
        });
        await Check("actual_grounded_reel_replaced", f =>
        {
            var wall = new StaticBody2D { Name = "GripWall", Position = new(540, 450),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None };
            wall.AddChild(new CollisionShape2D { Shape = f.Own(new RectangleShape2D { Size = new(36, 100) }) });
            wall.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
                Material = GrappleSurfaceMaterial.GripStone, UseCollisionHitPoint = true });
            f.Root.AddChild(wall);
        }, async f =>
        {
            var claw = f.Player.GetNode<MechanicalClawController>("MechanicalClaw");
            GrappleSurface2D anchor = f.Root.GetNode<GrappleSurface2D>("GripWall/GrappleSurfaceProfile");
            bool floorReel = false; ulong attachedAuthority = 0;
            var probe = new SpringboardPhysicsProbe { ProcessPhysicsPriority = 50, Observe = () =>
            {
                if (claw.CurrentFrame.State != MechanicalClawState.Reel || claw.CurrentTarget != anchor ||
                    !f.Player.IsOnFloor()) return;
                for (int i = 0; i < f.Player.GetSlideCollisionCount(); i++)
                    if (f.Player.GetSlideCollision(i).GetColliderRid() == f.Floor.GetRid() &&
                        f.Player.GetSlideCollision(i).GetNormal().Dot(Vector2.Up) > .98f)
                    { floorReel = true; attachedAuthority = claw.AuthorityRevision; }
            }};
            f.Root.AddChild(probe);
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            TestAssert.True(claw.KeyboardPreview.Surface == anchor && claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "real D preview selects the actual visible grippable solid, not a synthetic target state");
            f.Driver.Command(InputActions.Claw, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            f.Driver.Command(InputActions.Claw, false); f.Driver.Command(InputActions.MoveRight, false);
            for (int i = 0; i < 80 && f.Board.LaunchSequence == 0; i++)
                await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            TestAssert.True(floorReel && f.Board.LaunchSequence == 1 &&
                claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentTarget == null &&
                claw.ResidencyTarget == null && claw.AuthorityRevision > attachedAuthority &&
                f.Player.Velocity.X < -400f && f.Player.Velocity.Y < -300f && f.Motor.GrappleMovementScale == 1f,
                "SPRING_REAL_REEL_HANDOFF_RED: native floor-contact Reel is cancelled before the pad owns launch");
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 5);
            TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentTarget == null &&
                f.Motor.AcceptedJumpCount == 0 && f.Health.CurrentHealth == 5 && f.Player.Velocity.Y < 0,
                "old real cast cannot reattach or overwrite the continuing physical pad flight");
        });
        foreach(bool reset in new[]{true,false})
        await Check(reset?"real_reel_callback_reset_wins":"real_reel_callback_replacement_wins", f=>
        {
            var wall=new StaticBody2D { Name="GripWall", Position=new(540,450),
                CollisionLayer=CombatCollisionLayers.World, CollisionMask=CombatCollisionLayers.None };
            wall.AddChild(new CollisionShape2D { Shape=f.Own(new RectangleShape2D { Size=new(36,100) }) });
            wall.AddChild(new GrappleSurface2D { Name="GrappleSurfaceProfile",
                Material=GrappleSurfaceMaterial.GripStone, UseCollisionHitPoint=true });
            f.Root.AddChild(wall);
        }, async f=>
        {
            var claw=f.Player.GetNode<MechanicalClawController>("MechanicalClaw");
            var anchor=f.Root.GetNode<GrappleSurface2D>("GripWall/GrappleSurfaceProfile");
            bool armed=false,replaced=false,captured=false,floorReel=false;
            ulong callbackPhysics=0,committedRevision=0,observedRevision=0,observedLaunches=0;
            Vector2 expected=reset?Vector2.Zero:new(310,-350), observed=default;
            void Replace(MechanicalClawFrame frame,GrappleSurface2D target)
            {
                if(!armed || replaced || frame.State!=MechanicalClawState.Idle) return;
                replaced=true; callbackPhysics=Engine.GetPhysicsFrames();
                if(reset) f.Motor.ResetTransientState(); else f.Motor.ExternalImpulse(expected);
                committedRevision=f.Motor.MotionAuthorityRevision;
            }
            claw.ClawFrameChanged+=Replace;
            try
            {
                f.Root.AddChild(new SpringboardPhysicsProbe { ProcessPhysicsPriority=50, Observe=()=>
                {
                    if(claw.CurrentFrame.State!=MechanicalClawState.Reel || claw.CurrentTarget!=anchor || !f.Player.IsOnFloor()) return;
                    for(int i=0;i<f.Player.GetSlideCollisionCount();i++)
                        if(f.Player.GetSlideCollision(i).GetColliderRid()==f.Floor.GetRid() &&
                            f.Player.GetSlideCollision(i).GetNormal().Dot(Vector2.Up)>.98f)
                        { floorReel=true;armed=true; }
                }});
                f.Root.AddChild(new SpringboardPhysicsProbe { ProcessPhysicsPriority=150, Observe=()=>
                {
                    if(!replaced || captured || Engine.GetPhysicsFrames()!=callbackPhysics) return;
                    captured=true;observed=f.Player.Velocity;
                    observedRevision=f.Motor.MotionAuthorityRevision;observedLaunches=f.Board.LaunchSequence;
                }});
                f.Driver=KeyboardRouteInputDriver.Begin(f.Player);
                f.Driver.Command(InputActions.MoveRight,true);
                await ThrusterPlayerRuntimeTests.Frames(tree,f.Player.GetNode<PlayerInputReader>("Input"),1);
                TestAssert.True(claw.KeyboardPreview.Surface==anchor,"callback control begins with the actual visible solid grip");
                f.Driver.Command(InputActions.Claw,true);
                await ThrusterPlayerRuntimeTests.Frames(tree,f.Player.GetNode<PlayerInputReader>("Input"),1);
                f.Driver.Command(InputActions.Claw,false);f.Driver.Command(InputActions.MoveRight,false);
                for(int i=0;i<80 && !captured;i++)
                    await ThrusterPlayerRuntimeTests.Frames(tree,f.Player.GetNode<PlayerInputReader>("Input"),1);
                TestAssert.True(floorReel && replaced && captured && observedLaunches==0 &&
                    observed==expected && observedRevision==committedRevision &&
                    f.Player.GetNode<PlayerMotor>("Motor")==f.Motor && f.Health.CurrentHealth==5,
                    $"SPRING_MOTOR_AUTHORITY_RED: real floor-contact Reel cleanup callback wins; reset={reset} " +
                    $"floor_reel={floorReel} replaced={replaced} captured={captured} pad={observedLaunches} " +
                    $"velocity={observed}/{expected} revision={observedRevision}/{committedRevision} physics={callbackPhysics}");
            }
            finally { claw.ClawFrameChanged-=Replace; }
        });
        await Check("hidden_disabled_sensor_rejects_contact", f =>
        {
            f.Player.Position = new(300,464); f.Board.Visible = false;
        }, async f =>
        {
            await Frames(tree,4);
            TestAssert.True(f.Board.LaunchSequence == 0 && !f.Board.IsDangerTelegraphActive,
                "SPRING_HIDDEN_SENSOR_RED: a hidden plate cannot launch a player without a visible cue");
            f.Board.Visible = true; f.Board.Monitoring = false; await Frames(tree,3);
            TestAssert.True(f.Board.LaunchSequence == 0 && !f.Board.IsDangerTelegraphActive,
                "monitoring disabled also withdraws the plate's activation authority");
            var collider = f.Board.GetNode<CollisionShape2D>("Collision");
            collider.Disabled = true; f.Board.Monitoring = true; await Frames(tree,4);
            TestAssert.True(f.Board.LaunchSequence == 0 && !f.Board.IsDangerTelegraphActive,
                "disabled sensor cannot authorize a stale overlap launch");
            collider.Disabled = false; await Frames(tree,4);
            TestAssert.True(f.Board.LaunchSequence == 1 && f.Player.Velocity.Y < -350f,
                "restored visible native plate launches normally without resetting Health or pose");
        });
        await Check("native_sensor_shape_is_authoritative", f => { }, async f =>
        {
            var collider = f.Board.GetNode<CollisionShape2D>("Collision");
            var shape = (RectangleShape2D)collider.Shape; // This fixture owns it.
            shape.Size = new(96,12);
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight,true);
            for(int i=0;i<60 && f.Player.Position.X<300;i++)
                await ThrusterPlayerRuntimeTests.Frames(tree,f.Player.GetNode<PlayerInputReader>("Input"),1);
            f.Driver.Command(InputActions.MoveRight,false); await Frames(tree,3);
            TestAssert.True(f.Board.LaunchSequence == 0 && f.Health.CurrentHealth == 5 &&
                !f.Board.IsDangerTelegraphActive && f.Player.IsOnFloor(),
                "mutated larger native sensor cannot make the unchanged smaller visible plate launch");
            shape.Size = new(64,12); await Frames(tree,4);
            TestAssert.True(f.Board.LaunchSequence == 1,
                "restoring the original authored native footprint restores ordinary activation");
        });
        await Check("queued_ancestor_revokes_contact", f =>
        {
            f.Player.Position = new Vector2(300, 464); f.Board.LaunchVelocity = new(-160, 0);
        }, async f =>
        {
            await Frames(tree, 12);
            f.Board.ResetForCheckpoint(); ulong sequence = f.Board.LaunchSequence;
            f.Root.QueueFree();
            TestAssert.True(f.Root.IsQueuedForDeletion() && !f.Board.IsQueuedForDeletion() &&
                !f.Player.IsQueuedForDeletion(), "test exercises ancestor-only queued retirement before native deletion");
            f.Board._PhysicsProcess(2d);
            TestAssert.Equal(sequence, f.Board.LaunchSequence,
                "SPRING_QUEUED_OWNER_RED: a queued room already revokes contact before child ExitTree");
        });
        GD.Print($"CANYON_SPRINGBOARD_CONTROLS_OK cases={cases} native_player=true " +
            "natural_jump_and_reentry=true lifecycle=true human=false whole_route=false");
    }

    public static async Task RunFixedGearContact(SceneTree tree)
    {
        var f = new SpringboardFixture(tree);
        int deaths = 0; bool guardAtDamage = false; ulong guardPhysics = 0;
        void OnDied() => deaths++;
        void OnDamaged(int remaining, Vector2 impulse) => guardAtDamage |=
            remaining == 0 && guardPhysics == Engine.GetPhysicsFrames();
        try
        {
            f.Board.Position = new Vector2(700, 500); // Separate the standalone gear from the pad fixture.
            await f.Mount();
            TestAssert.True(f.Player.IsOnFloor() && f.Health.CurrentHealth == 5,
                "fixed-gear test starts with real saved player and floor");
            Type gearType = typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Mechanisms.CanyonFixedGear");
            TestAssert.True(gearType != null,
                "FIXED_GEAR_MISSING: existing pistons are not the requested continuous fixed-tooth hazard");
            var gear = (Area2D)Activator.CreateInstance(gearType);
            gear.Name = "Gear"; gear.Position = new(420, 464);
            gear.AddChild(new CollisionShape2D { Name = "Collision",
                Shape = f.Own(new CircleShape2D { Radius = 30f }) });
            f.Root.AddChild(gear);
            f.Root.AddChild(new SpringboardPhysicsProbe { ProcessPhysicsPriority = 50, Observe = () =>
            {
                if (f.Health.CurrentHealth > 0 && f.Player.Position.DistanceTo(gear.Position) < 85f &&
                    f.Player.GetNode<PlayerShieldController>("Shield").IsActive &&
                    f.Player.GetNode<PlayerInputReader>("Input").Current.KeyboardShieldHeld)
                    guardPhysics = Engine.GetPhysicsFrames();
            }});
            f.Health.Died += OnDied; f.Health.Damaged += OnDamaged;
            f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
            f.Driver.Command(InputActions.MoveRight, true);
            f.Driver.PointerButton(MouseButton.Right, true, f.Player, gear.GlobalPosition);
            for (int i = 0; i < 160 && f.Health.CurrentHealth > 0; i++)
                await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
            TestAssert.True(f.Health.CurrentHealth == 0 && deaths == 1 && guardAtDamage &&
                f.Board.LaunchSequence == 0 && f.Motor.AcceptedJumpCount == 0,
                $"FIXED_GEAR_NATIVE_DAMAGE_RED: real guarded walking contact must deliver lethal gear damage once; " +
                $"hp={f.Health.CurrentHealth} deaths={deaths} actual_guard_at_damage={guardAtDamage} position={f.Player.Position}");
            await Frames(tree, 10);
            TestAssert.True(deaths == 1 && f.Health.CurrentHealth == 0,
                "continued overlap cannot repeat player death or reset Health");
            GD.Print("CANYON_FIXED_GEAR_RUNTIME_OK native_contact=true real_guard=true unblockable=true " +
                "death_events=1 native_player=true human=false whole_route=false");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(f.Player))
            { f.Health.Died -= OnDied; f.Health.Damaged -= OnDamaged; }
            await f.DisposeAsync();
        }
    }

    public static async Task RunRecyclingCombo(SceneTree tree)
    {
        for (int variant = 0; variant < 2; variant++)
        {
            var f = new SpringboardFixture(tree);
            int deaths = 0; ulong launchPhysics = 0, deathPhysics = 0;
            void OnDied() { deaths++; deathPhysics = Engine.GetPhysicsFrames(); }
            try
            {
                (CanyonFixedGear comboGear, _) = AddFixedGearFixture(f, new Vector2(180, 370));
                f.Board.Launched += (_, _, _) => launchPhysics = Engine.GetPhysicsFrames();
                await f.Mount(); f.Health.Died += OnDied;
                f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
                f.Driver.Command(InputActions.MoveRight, true);
                Vector2 previous = f.Player.Position;
                float maximumStep = 0f;
                int flightRows = 0;
                async Task Tick()
                {
                    await ThrusterPlayerRuntimeTests.Frames(tree, f.Player.GetNode<PlayerInputReader>("Input"), 1);
                    maximumStep = Mathf.Max(maximumStep, previous.DistanceTo(f.Player.Position));
                    previous = f.Player.Position;
                    if (variant == 0 && launchPhysics > 0 && flightRows < 18)
                    {
                        flightRows++;
                        GD.Print($"RECYCLING_FLIGHT_OBSERVATION row={flightRows}/18 physics={Engine.GetPhysicsFrames()} " +
                            $"player={f.Player.Position} velocity={f.Player.Velocity} floor={f.Player.IsOnFloor()} " +
                            $"gear={comboGear.GlobalPosition} active={comboGear.IsDangerTelegraphActive} " +
                            $"contact_sequence={comboGear.ContactSequence} gravity={f.Motor.MotionSettings.Gravity:R} " +
                            $"health={f.Health.CurrentHealth}");
                    }
                }
                if (variant == 0)
                {
                    for (int i = 0; i < 160 && f.Health.CurrentHealth > 0; i++) await Tick();
                    TestAssert.True(f.Board.LaunchSequence == 1 && launchPhysics > 0 && deathPhysics > launchPhysics &&
                        f.Health.CurrentHealth == 0 && deaths == 1 && f.Motor.AcceptedJumpCount == 0 &&
                        f.Motor.AcceptedDashCount == 0 && maximumStep < 24f,
                        $"RECYCLING_COMBO_FLIGHT_RED: holding real D without Space must fly from pad into gear, " +
                        $"not teleport or scripted damage; hp={f.Health.CurrentHealth} deaths={deaths} " +
                        $"launch={f.Board.LaunchSequence}/{launchPhysics} death={deathPhysics} " +
                        $"position={f.Player.Position} max_step={maximumStep:R}");
                    GD.Print("RECYCLING_COMBO_FLIGHT_OK actual_pad=true later_native_gear_contact=true held_D=true " +
                        "ordinary_jumps=0 dash=0 one_death=true pose_writes=0");
                }
                else
                {
                    for (int i = 0; i < 50 && f.Player.Position.X < 240f; i++) await Tick();
                    f.Driver.Command(InputActions.Jump, true);
                    for (int i = 0; i < 7; i++) await Tick();
                    f.Driver.Command(InputActions.Jump, false);
                    for (int i = 0; i < 60 && f.Player.Position.X < 380f; i++) await Tick();
                    f.Driver.Command(InputActions.MoveRight, false);
                    for (int i = 0; i < 80 && !f.Player.IsOnFloor(); i++) await Tick();
                    TestAssert.True(f.Player.IsOnFloor() && f.Player.Position.X > 360f &&
                        f.Board.LaunchSequence == 0 && f.Health.CurrentHealth == 5 && f.Motor.AcceptedJumpCount == 1,
                        "one actual ordinary Space avoids both mechanisms and lands beyond the combination");
                    GD.Print($"RECYCLING_RETURN_OBSERVATION stage=before_reverse_jump player={f.Player.Position} " +
                        $"velocity={f.Player.Velocity} floor={f.Player.IsOnFloor()} jumps={f.Motor.AcceptedJumpCount} " +
                        $"space={Input.IsPhysicalKeyPressed(Key.Space)} physics={Engine.GetPhysicsFrames()}");
                    f.Driver.Command(InputActions.MoveLeft, true);
                    int returnFrames = 0;
                    // Landing still carries +260 rightward momentum. Brake on the
                    // real floor and take a short leftward run-up before jumping;
                    // all of this consumes the original 80-frame return budget.
                    for (; returnFrames < 80 && (f.Player.Position.X > 360f || f.Player.Velocity.X > -240f);
                        returnFrames++) await Tick();
                    TestAssert.True(returnFrames < 80 && f.Player.IsOnFloor() &&
                        f.Health.CurrentHealth == 5 && f.Board.LaunchSequence == 0,
                        "return run-up uses real grounded reverse braking before the same ordinary Space");
                    f.Driver.Command(InputActions.Jump, true);
                    for (int i = 0; i < 7; i++, returnFrames++) await Tick();
                    f.Driver.Command(InputActions.Jump, false);
                    for (; returnFrames < 80 && f.Player.Position.X > 230f; returnFrames++) await Tick();
                    f.Driver.Command(InputActions.MoveLeft, false);
                    for (int i = 0; i < 80 && !f.Player.IsOnFloor(); i++) await Tick();
                    TestAssert.True(f.Player.IsOnFloor() && f.Player.Position.X < 248f &&
                        f.Board.LaunchSequence == 0 && f.Health.CurrentHealth == 5 && deaths == 0 &&
                        f.Motor.AcceptedJumpCount == 2 && f.Motor.AcceptedDashCount == 0 && maximumStep < 24f,
                        $"RECYCLING_COMBO_RETURN_RED: ordinary Space route must also return safely; " +
                        $"hp={f.Health.CurrentHealth} pad={f.Board.LaunchSequence} jumps={f.Motor.AcceptedJumpCount} " +
                        $"position={f.Player.Position} max_step={maximumStep:R}");
                    GD.Print("RECYCLING_COMBO_BYPASS_OK natural_space_out_and_back=true ordinary_jumps=2 " +
                        "dash=0 health=5->5 launches=0 pose_writes=0");
                }
            }
            finally
            {
                if (GodotObject.IsInstanceValid(f.Player)) f.Health.Died -= OnDied;
                await f.DisposeAsync();
            }
        }
        var control = new SpringboardFixture(tree);
        try
        {
            control.Player.Position = new(300, 464); control.Board.Position = new(700, 500);
            (CanyonFixedGear gear, CircleShape2D shape) = AddFixedGearFixture(control, new(300, 464), 1);
            control.Root.SetMeta("_seamless_staged", true);
            await control.Mount();
            TestAssert.True(control.Health.CurrentHealth == 5 && gear.ContactSequence == 0,
                "staged room cannot deliver even a real native gear overlap");
            control.Root.RemoveMeta("_seamless_staged"); shape.Radius = 45f;
            await Frames(tree, 6);
            TestAssert.True(control.Health.CurrentHealth == 5 && !gear.IsDangerTelegraphActive,
                "mutated native footprint cannot create an invisible larger damaging circle");
            shape.Radius = 30f; gear.Visible = false; await Frames(tree, 6);
            TestAssert.True(control.Health.CurrentHealth == 5 && gear.ContactSequence == 0,
                "hidden gear is not an invisible damage source");
            gear.Visible = true; gear.Monitoring = false; await Frames(tree, 2);
            TestAssert.True(control.Health.CurrentHealth == 5, "monitoring disabled never polls an invalid overlap list");
            gear.Monitoring = true; CollisionShape2D collider = gear.GetNode<CollisionShape2D>("Collision");
            collider.Disabled = true; await Frames(tree, 4);
            TestAssert.True(control.Health.CurrentHealth == 5 && !gear.IsDangerTelegraphActive,
                "disabled native circle vetoes stale overlap delivery");
            collider.Disabled = false; await Frames(tree, 4);
            TestAssert.True(control.Health.CurrentHealth == 4 && gear.ContactSequence == 1,
                "restored exact visible native geometry genuinely damages through configured one-point contact");
            ulong sequence = gear.ContactSequence;
            gear.ResetForCheckpoint(); control.Root.QueueFree(); gear._PhysicsProcess(2d);
            TestAssert.True(control.Health.CurrentHealth == 4 && gear.ContactSequence == sequence,
                "queued ancestor synchronously revokes even reset gear before child exit");
            GD.Print("RECYCLING_GEAR_CONTROLS_OK staged=true geometry=true hidden=true disabled=true " +
                "restored_real_damage=1 queued_ancestor=true native_player=true");
        }
        finally { await control.DisposeAsync(); }
    }

    private static (CanyonFixedGear Gear, CircleShape2D Shape) AddFixedGearFixture(
        SpringboardFixture fixture, Vector2 position, int damage = 5)
    {
        var gear = new CanyonFixedGear { Name = "Gear", Position = position, Damage = damage };
        var shape = fixture.Own(new CircleShape2D { Radius = 30f });
        gear.AddChild(new CollisionShape2D { Name = "Collision", Shape = shape });
        fixture.Root.AddChild(gear); return (gear, shape);
    }

    public static async Task Run(SceneTree tree)
    {
        await CounterweightMovesARealPassengerAndStopsAtEndpoints(tree);
        await PistonSteamAndPendulumKeepCollisionAndTelegraphsInSync(tree);
        await MovingPistonOccluderReleasesAStaticGrip(tree);
        await ValveSealAndShortcutUsePersistentRunState(tree);
        await RunRequiredLifecycleCases(tree);
        ShortcutSignalConsumerPersistsOnlyNewStableIds();
        await GeneratedSectorsContainSixDistinctFamilies(tree);
        await RunGeneratedSteamVisibility(tree);
        await RunGeneratedSealGate(tree);
        await RunGeneratedCounterweight(tree);
    }

    public static async Task RunGeneratedCounterweight(SceneTree tree)
    {
        await GeneratedCounterweightConnectsBothPhysicalApproaches(tree);
        await GeneratedLowerBoardingToleratesApproachOffsets(tree);
    }

    public static async Task RunGeneratedShortcutRoute(SceneTree tree, bool toleranceOnly = false,
        bool firstTransferOnly = false)
    {
        if (!toleranceOnly) await GeneratedShortcutIsLockedFromBothSides(tree);
        Task route = toleranceOnly
            ? PrologueCanyonTraversalTests.RunTask4ShortcutTolerance(tree)
            : PrologueCanyonTraversalTests.RunTask4Shortcut(tree, firstTransferOnly);
        int openedFixtures = 0;
        while (!route.IsCompleted)
        {
            foreach (Node node in tree.Root.FindChildren("UpperBasinShortcutWinch", "", true, false))
            {
                if (node is not CanyonShortcutWinch winch || winch.IsOpen) continue;
                TestAssert.True(winch.TryReceiveThread(default),
                    "generated shortcut opens only through its real static winch control path");
                openedFixtures++;
            }
            await Frames(tree, 1);
        }
        await route;
        TestAssert.True(openedFixtures >= (toleranceOnly ? 2 : 1),
            "every generated full-route fixture was opened through its own mounted winch");
        GD.Print($"TASK5_GENERATED_SHORTCUT_ROUTE_OK tolerance={toleranceOnly} opened={openedFixtures}");
    }

    public static Task RunGeneratedShortcutLock(SceneTree tree) =>
        GeneratedShortcutIsLockedFromBothSides(tree);

    public static async Task RunGeneratedMainWithSealPrerequisites(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = "Task5GeneratedMainWithSealPrerequisites";
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        string fixture = $"user://task5-generated-main/{Guid.NewGuid():N}";
        flow.SavePath = fixture + "/save.json";
        flow.SettingsPath = fixture + "/settings.json";
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        TestAssert.Equal(new Vector2(160, 600), player.Position,
            "Task5 prerequisite wrapper retains the generated main spawn");
        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        camera.Enabled = true;
        tree.Root.AddChild(level);
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        ulong playerId = player.GetInstanceId();
        Vector2 previous = player.GlobalPosition;
        float maxStep = 0f;
        int recoveryRequested = 0;
        int recoveryCompleted = 0;
        var visited = new HashSet<PrologueSectorId> { world.CurrentSector };
        level.RecoveryRequested += _ => recoveryRequested++;
        level.RecoveryCompleted += _ => recoveryCompleted++;
        try
        {
            await Frames(tree, 8);
            level.Progress.ActivateMechanism("canyon.v2.courtyard-crank");
            level.Progress.ActivateMechanism("canyon.v2.encounter.split-level");
            TestAssert.True(!BossHallReachable(level.Progress),
                "fixture prerequisites alone do not bypass the seal activation contract");
            void Observe()
            {
                TestAssert.Equal(playerId, player.GetInstanceId(),
                    "Task5 prerequisite wrapper keeps one persistent production player");
                float step = player.GlobalPosition.DistanceTo(previous);
                maxStep = Mathf.Max(maxStep, step);
                TestAssert.True(step <= 72f,
                    $"Task5 prerequisite wrapper permits no teleport; step={step:0.00} " +
                    $"from={previous} to={player.GlobalPosition}");
                previous = player.GlobalPosition;
                visited.Add(world.CurrentSector);
                TestAssert.True(world.MountedSectorIds.Contains(world.CurrentSector),
                    $"current sector {world.CurrentSector} stays mounted");
                Vector2 topLeft = player.GetCanvasTransform() *
                                  (player.GlobalPosition - new Vector2(20, 36));
                Vector2 bottomRight = player.GetCanvasTransform() *
                                      (player.GlobalPosition + new Vector2(20, 36));
                TestAssert.True(player.GetViewportRect().Encloses(
                        new Rect2(topLeft, bottomRight - topLeft)),
                    $"full real capsule stays camera-visible; player={player.GlobalPosition}");
            }
            await PrologueCanyonTraversalTests.DriveTask4GeneratedMain(tree, world, player,
                Observe, name =>
                {
                    GD.Print($"TASK5_PREREQUISITE_MAIN_MILESTONE name={name} " +
                             $"sector={world.CurrentSector} position={player.GlobalPosition}");
                    return Task.CompletedTask;
                });
            TestAssert.True(level.Progress.ActivatedMechanisms.Contains(
                                CanyonWorldLayout.CourtyardSealMechanismId) &&
                            BossHallReachable(level.Progress),
                "mounted generated seal consumes the explicit fixture prerequisites and opens graph access");
            TestAssert.Equal(0, recoveryRequested,
                "Task5 prerequisite route requests no production recovery");
            TestAssert.Equal(0, recoveryCompleted,
                "Task5 prerequisite route completes no production recovery");
            TestAssert.Equal(string.Join("|", Enum.GetValues<PrologueSectorId>()),
                string.Join("|", Enum.GetValues<PrologueSectorId>().Where(visited.Contains)),
                "Task5 prerequisite wrapper crosses every generated sector in route order");
            TestAssert.True(level.Progress.RestoreCheckpoint() is
                    { HasActivatedCheckpoint: true, SectorId: PrologueSectorId.LostTemple },
                "Task5 prerequisite route still activates the real pre-boss checkpoint");
            GD.Print($"TASK5_PREREQUISITE_MAIN_ROUTE_OK fixture_prerequisites=True " +
                     $"player_input=True max_step={maxStep:0.00} recovery=0");
        }
        finally
        {
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Frames(tree, 4);
        }
    }

    public static async Task RunGeneratedSteamVisibility(SceneTree tree, string output = null)
    {
        var root = new Node2D { Name = "Task5GeneratedSteamVisibility" };
        PrologueSector basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        basin.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin].Position;
        root.AddChild(basin);
        tree.Root.AddChild(root);
        try
        {
            CanyonSteamJet jet = basin.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            CanyonSteamValve valve = basin.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            CanvasItem warning = basin.GetNode<CanvasItem>(
                "VisualRoot/GameplayReadable/MechanismTelegraphs/SteamJetTelegraph");
            CanvasItem active = basin.GetNodeOrNull<CanvasItem>(
                "VisualRoot/GameplayReadable/MechanismTelegraphs/SteamJetActive");
            TestAssert.True(active != null,
                "generated damaging steam owns a distinct active-phase visual");
            if (active == null) throw new InvalidOperationException(
                "generated SteamJetActive visual is missing");
            if (output != null)
            {
                if (!Path.IsPathFullyQualified(output))
                    throw new ArgumentException("--output must be an absolute external directory");
                string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
                string normalized = Path.GetFullPath(output);
                if ((normalized.TrimEnd('/', '\\') + Path.DirectorySeparatorChar).StartsWith(
                    project.TrimEnd('/', '\\') + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("steam visual proof output must stay outside the project");
                output = normalized;
                Directory.CreateDirectory(output);
                tree.Root.Size = new Vector2I(1280, 720);
                tree.Root.ContentScaleSize = new Vector2I(1280, 720);
                var camera = new Camera2D
                {
                    Name = "SteamProofCamera", GlobalPosition = jet.GlobalPosition,
                    Enabled = true, Zoom = new Vector2(2.5f, 2.5f)
                };
                root.AddChild(camera);
                await Frames(tree, 2);
            }
            jet.SetPhysicsProcess(false);
            jet.ResetForCheckpoint();
            await Frames(tree, 1);
            TestAssert.True(!warning.Visible && !active.Visible && jet.Collision.Disabled,
                "generated steam safe phase is quiet, visible-free, and non-damaging");
            await CaptureSteamFrame(tree, output, "safe");

            jet.AdvanceForTest(1f);
            TestAssert.True(warning.Visible && !active.Visible && jet.Collision.Disabled &&
                            jet.IsDangerTelegraphActive,
                "generated steam warning is visible before collision activates");
            await CaptureSteamFrame(tree, output, "warning");

            jet.AdvanceForTest(.8f);
            TestAssert.True(!warning.Visible && active.Visible && !jet.Collision.Disabled &&
                            jet.Frame.HazardActive,
                "generated active steam stays visibly present while its damage collision is active");
            await CaptureSteamFrame(tree, output, "active");

            var progress = new PrologueProgress();
            valve.ApplyProgress(progress);
            TestAssert.True(valve.TryReceiveThread(default),
                "generated valve accepts its scoped receiver interaction");
            TestAssert.True(jet.DisabledForRun && !warning.Visible && !active.Visible &&
                            jet.Collision.Disabled,
                "disabled generated steam removes damage, warning, and active visuals together");
            await CaptureSteamFrame(tree, output, "disabled");
        }
        finally
        {
            await Dispose(tree, root);
        }
    }

    public static async Task RunGeneratedSealGate(SceneTree tree)
    {
        ReleasePlayerInput();
        var root = new Node2D { Name = "Task5GeneratedSealGate" };
        PrologueSector temple = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn")
            .Instantiate<PrologueSector>();
        temple.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.LostTemple].Position;
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "SealGatePlayer";
        player.Position = new Vector2(5430, 1184);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(temple);
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 5);
            CanyonSealGate seal = temple.GetNode<CanyonSealGate>(
                "Gameplay/Mechanisms/CourtyardSeal");
            GrappleSurface2D farFloor = temple.GetNode<GrappleSurface2D>(
                "Gameplay/Collision/CheckpointFloor/GrappleSurfaceProfile");
            PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
            MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            float closedLimitX = seal.GlobalPosition.X - 18f - 20f;

            var mechanismOnly = new PrologueProgress();
            mechanismOnly.ActivateMechanism(seal.RequiredMechanismId);
            seal.ApplyProgress(mechanismOnly);
            TestAssert.True(!seal.IsOpen && !seal.Collision.Disabled &&
                            !BossHallReachable(mechanismOnly),
                "mechanism-only progress leaves physical and graph courtyard routes sealed");
            await SettlePlayerAt(tree, player, new Vector2(5430, 1184));
            Input.ActionPress(InputActions.MoveRight);
            await Frames(tree, 60);
            Input.ActionRelease(InputActions.MoveRight);
            TestAssert.True(player.GlobalPosition.X <= closedLimitX + 1f,
                $"mechanism-only generated seal blocks real walking; position={player.GlobalPosition} " +
                $"limit={closedLimitX}");

            var encounterOnly = new PrologueProgress();
            encounterOnly.ActivateMechanism(seal.RequiredEncounterId);
            seal.ApplyProgress(encounterOnly);
            TestAssert.True(!seal.IsOpen && !seal.Collision.Disabled &&
                            !BossHallReachable(encounterOnly),
                "encounter-only progress leaves physical and graph courtyard routes sealed");
            await SettlePlayerAt(tree, player, new Vector2(5430, 1184));
            Input.ActionPress(InputActions.MoveRight);
            Input.ActionPress(InputActions.Jump);
            await Frames(tree, 20);
            Input.ActionRelease(InputActions.Jump);
            await Frames(tree, 70);
            Input.ActionRelease(InputActions.MoveRight);
            TestAssert.True(player.GlobalPosition.X <= closedLimitX + 1f,
                $"encounter-only generated seal blocks an ordinary held jump; " +
                $"position={player.GlobalPosition} limit={closedLimitX}");

            await SettlePlayerAt(tree, player, new Vector2(5430, 1184));
            claw.ResetTransientState();
            bool latchedBeyondSeal = false;
            void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D surface)
            {
                if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing)
                    latchedBeyondSeal |= surface == farFloor;
            }
            claw.ClawFrameChanged += ObserveClaw;
            try
            {
                await AimAt(tree, player, farFloor.WorldAnchor);
                await TapAction(tree, InputActions.SelectGrappleMode);
                await TapAction(tree, InputActions.PrimaryAction);
                await Frames(tree, 90);
            }
            finally
            {
                claw.ClawFrameChanged -= ObserveClaw;
                ReleasePlayerInput();
            }
            TestAssert.True(!latchedBeyondSeal && player.GlobalPosition.X <= closedLimitX + 1f,
                $"closed generated seal blocks a real claw cast toward far-side terrain; " +
                $"latched={latchedBeyondSeal} position={player.GlobalPosition}");

            var completed = new PrologueProgress();
            completed.ActivateMechanism(seal.RequiredMechanismId);
            completed.ActivateMechanism(seal.RequiredEncounterId);
            seal.ApplyProgress(completed);
            seal.RefreshState();
            TestAssert.True(seal.IsOpen && seal.Collision.Disabled &&
                            completed.ActivatedMechanisms.Contains(seal.ActivationId) &&
                            BossHallReachable(completed),
                "both named prerequisites latch one physical and graph-visible completed seal state");
            await SettlePlayerAt(tree, player, new Vector2(5430, 1184));
            Input.ActionPress(InputActions.MoveRight);
            Input.ActionPress(InputActions.Jump);
            await Frames(tree, 20);
            Input.ActionRelease(InputActions.Jump);
            await Frames(tree, 70);
            Input.ActionRelease(InputActions.MoveRight);
            TestAssert.True(player.GlobalPosition.X > seal.GlobalPosition.X + 70f,
                $"opened generated seal admits a normal held jump onto the checkpoint side; " +
                $"position={player.GlobalPosition} seal={seal.GlobalPosition}");

            await AssertCompletedSealRemountWithoutPrerequisiteReplay(tree, root, seal.ActivationId);
            GD.Print($"TASK5_GENERATED_SEAL_PHYSICS_OK closed_limit={closedLimitX} " +
                     $"opened_position={player.GlobalPosition}");
        }
        finally
        {
            ReleasePlayerInput();
            await Dispose(tree, root);
            await Frames(tree, 2);
        }
    }

    public static async Task RunRequiredLifecycleCases(SceneTree tree)
    {
        var root = new Node2D { Name = "Task5RequiredLifecycle" };
        CanyonCounterweightLift lift = MechanismFixture.Lift();
        CanyonGearPiston piston = MechanismFixture.Piston();
        CanyonSteamJet jet = MechanismFixture.Jet();
        CanyonPendulumHazard pendulum = MechanismFixture.Pendulum();
        root.AddChild(lift);
        root.AddChild(piston);
        root.AddChild(jet);
        root.AddChild(pendulum);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            lift.Activate();
            lift.AdvanceForTest(.2f);
            piston.AdvanceForTest(1.2f);
            jet.AdvanceForTest(1.5f);
            pendulum.AdvanceForTest(.5f);
            float liftTime = lift.ElapsedSeconds;
            Vector2 liftPosition = lift.Position;
            float pistonTime = piston.ElapsedSeconds;
            float jetTime = jet.ElapsedSeconds;
            float pendulumTime = pendulum.ElapsedSeconds;
            tree.Paused = true;
            lift.AdvanceForTest(.4f);
            piston.AdvanceForTest(.4f);
            jet.AdvanceForTest(.4f);
            pendulum.AdvanceForTest(.4f);
            TestAssert.True(Mathf.IsEqualApprox(lift.ElapsedSeconds, liftTime) &&
                            lift.Position.IsEqualApprox(liftPosition),
                "pause freezes the activated lift timer and moving-body position");
            TestAssert.True(Mathf.IsEqualApprox(piston.ElapsedSeconds, pistonTime) &&
                            Mathf.IsEqualApprox(jet.ElapsedSeconds, jetTime) &&
                            Mathf.IsEqualApprox(pendulum.ElapsedSeconds, pendulumTime),
                "pause freezes every timed piston, steam, and pendulum phase");
            tree.Paused = false;

            lift.ResetForCheckpoint();
            piston.ResetForCheckpoint();
            jet.ResetForCheckpoint();
            pendulum.ResetForCheckpoint();
            TestAssert.True(!lift.IsActivated && lift.ElapsedSeconds == 0f &&
                            lift.Position.IsEqualApprox(lift.LowerPosition) &&
                            piston.ElapsedSeconds == 0f && piston.Collision.Disabled &&
                            jet.ElapsedSeconds == 0f && jet.Collision.Disabled &&
                            pendulum.ElapsedSeconds == 0f,
                "checkpoint recovery resets every nonpersistent timer, phase, and lift endpoint");

            var progress = new PrologueProgress();
            var persistentRoot = new Node2D { Name = "PersistentValveRemount" };
            CanyonSteamJet persistentJet = MechanismFixture.Jet();
            persistentJet.Name = "Jet";
            CanyonSteamValve valve = MechanismFixture.Valve();
            valve.Name = "Valve";
            valve.PairedJetPath = new NodePath("../Jet");
            persistentRoot.AddChild(persistentJet);
            persistentRoot.AddChild(valve);
            root.AddChild(persistentRoot);
            await Frames(tree, 1);
            valve.ApplyProgress(progress);
            valve.TryReceiveThread(default);
            valve.ResetForCheckpoint();
            TestAssert.True(valve.IsActivated && persistentJet.DisabledForRun &&
                            persistentJet.Collision.Disabled,
                "checkpoint recovery preserves the activated valve and disabled jet for this run");

            var remountRoot = new Node2D { Name = "PersistentValveSecondMount" };
            CanyonSteamJet remountedJet = MechanismFixture.Jet();
            remountedJet.Name = "Jet";
            CanyonSteamValve remountedValve = MechanismFixture.Valve();
            remountedValve.Name = "Valve";
            remountedValve.PairedJetPath = new NodePath("../Jet");
            remountRoot.AddChild(remountedJet);
            remountRoot.AddChild(remountedValve);
            root.AddChild(remountRoot);
            await Frames(tree, 1);
            remountedValve.ApplyProgress(progress);
            TestAssert.True(remountedValve.IsActivated && remountedJet.DisabledForRun,
                "same-run remount restores the activated valve and disabled paired jet");

            progress.Reset();
            remountedValve.ApplyProgress(progress);
            TestAssert.True(!remountedValve.IsActivated && !remountedJet.DisabledForRun &&
                            remountedJet.ElapsedSeconds == 0f && remountedJet.Collision.Disabled,
                "new run clears valve persistence and restarts its paired jet in the safe phase");
        }
        finally
        {
            tree.Paused = false;
            await Dispose(tree, root);
        }
    }

    private static async Task GeneratedShortcutIsLockedFromBothSides(SceneTree tree)
    {
        ReleasePlayerInput();
        var root = new Node2D { Name = "Task5GeneratedLockedShortcut" };
        PrologueSector basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        basin.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin].Position;
        PrologueSector ascent = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
            .Instantiate<PrologueSector>();
        ascent.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "LockedShortcutPlayer"; player.Position = new Vector2(4175, 3600);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(basin); root.AddChild(ascent); root.AddChild(player); tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 5);
            CanyonShortcutWinch winch = ascent.GetNode<CanyonShortcutWinch>(
                "Gameplay/Mechanisms/UpperBasinShortcutWinch");
            TestAssert.True(!winch.IsOpen && !winch.BarrierCollision.Disabled,
                "generated upper-Basin shortcut begins physically locked");
            StaticBody2D barrier = winch.GetNode<StaticBody2D>("Barrier");
            Vector2 barrierSize = ((RectangleShape2D)winch.BarrierCollision.Shape).Size;
            Rect2 barrierBounds = BodyBounds(barrier.GlobalPosition, barrierSize);
            Rect2 corridorBounds = new(ascent.GlobalPosition + winch.CorridorBounds.Position,
                winch.CorridorBounds.Size);
            var closedVisual = ascent.GetNode<Polygon2D>(
                "VisualRoot/GameplayReadable/MechanismTelegraphs/UpperBasinShortcutClosed");
            Rect2 visualBounds = PolygonBounds(closedVisual);
            TestAssert.Equal(new Rect2(3840, 1520, 410, 1930), barrierBounds,
                "generated locked shutter fills the hand-checked non-public shortcut shaft");
            TestAssert.True(corridorBounds.Encloses(barrierBounds) &&
                            visualBounds.IsEqualApprox(barrierBounds) &&
                            barrier.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material ==
                            GrappleSurfaceMaterial.Forbidden,
                "closed visual, Forbidden collision, and declared corridor bounds agree");
            foreach (StaticBody2D publicSupport in new[]
                     {
                         basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3636"),
                         ascent.GetNode<StaticBody2D>("Gameplay/Collision/LiftEntryFloor"),
                         ascent.GetNode<StaticBody2D>("Gameplay/Collision/LiftAccessStepOne"),
                         ascent.GetNode<StaticBody2D>("Gameplay/Collision/AscentGrip1")
                     })
            {
                Vector2 supportSize = ((RectangleShape2D)publicSupport.GetNode<CollisionShape2D>(
                    "Collision").Shape).Size;
                TestAssert.True(!barrierBounds.Intersects(
                        BodyBounds(publicSupport.GlobalPosition, supportSize)),
                    $"closed shortcut shutter does not intersect public/main support " +
                    $"{publicSupport.Name}");
            }

            StaticBody2D publicJunction = basin.GetNode<StaticBody2D>(
                "Gameplay/Collision/ShortcutStep3636");
            StaticBody2D lowerShortcutStep = ascent.GetNode<StaticBody2D>(
                "Gameplay/Collision/ShortcutStep3476");
            GrappleSurface2D lowerShortcutGrip = ascent.GetNode<GrappleSurface2D>(
                "Gameplay/Collision/ShortcutStep3476Grip/GrappleSurfaceProfile");
            StaticBody2D upperGallery = ascent.GetNode<StaticBody2D>(
                "Gameplay/Collision/SplitUpperGallery");
            StaticBody2D firstUpperStep = ascent.GetNode<StaticBody2D>(
                "Gameplay/Collision/ShortcutStep1556");

            await SettlePlayerAt(tree, player, new Vector2(4175, 3600));
            TestAssert.True(HasFloorContact(player, publicJunction),
                "closed lower-end probe begins fully supported on the public 3636 junction");
            Input.ActionPress(InputActions.MoveLeft);
            Input.ActionPress(InputActions.Jump);
            bool landedLowerStepWhileClosed = false;
            for (int frame = 0; frame < 120; frame++)
            {
                await Frames(tree, 1);
                landedLowerStepWhileClosed |= HasFloorContact(player, lowerShortcutStep);
                if (frame == 19) Input.ActionRelease(InputActions.Jump);
            }
            Input.ActionRelease(InputActions.MoveLeft);
            TestAssert.True(!landedLowerStepWhileClosed,
                $"closed lower endpoint rejects a normal held jump from public 3636 onto the " +
                $"first non-public shortcut support; position={player.GlobalPosition}");

            await SettlePlayerAt(tree, player, new Vector2(4175, 3600));
            bool closedLowerLatch = await CastAtGripAndObserveLatch(tree, player,
                lowerShortcutGrip);
            TestAssert.True(!closedLowerLatch,
                $"closed lower endpoint occludes the real public-junction claw cast; " +
                $"latched={closedLowerLatch} position={player.GlobalPosition}");

            await SettlePlayerAt(tree, player, new Vector2(4200, 1444));
            TestAssert.True(HasFloorContact(player, upperGallery),
                "closed upper-end probe begins fully supported on SplitUpperGallery");
            bool closedUpperEntry = await WalkDropToSupport(tree, player, firstUpperStep);
            TestAssert.True(!closedUpperEntry,
                $"closed upper endpoint blocks the normal gallery walk/drop onto its first " +
                $"non-public support; entered={closedUpperEntry} position={player.GlobalPosition}");

            await SettlePlayerAt(tree, player, new Vector2(4175, 2640));
            bool landed2996 = await DropAlongClosedShortcutRightSide(tree, player,
                ascent.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep2996"));
            bool landed3316 = landed2996 && await DropAlongClosedShortcutRightSide(tree, player,
                ascent.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3316"));
            bool reachedPublic3636 = landed3316 && await DropAlongClosedShortcutRightSide(tree, player,
                basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3636"));
            TestAssert.True(!reachedPublic3636,
                $"closed shortcut blocks the full same-side 2676->2996->3316->3636 descent " +
                $"using only normal A/D and gravity; landed2996={landed2996} " +
                $"landed3316={landed3316} reached3636={reachedPublic3636} " +
                $"position={player.GlobalPosition}");

            TestAssert.True(winch.TryReceiveThread(default) && winch.IsOpen &&
                            winch.BarrierCollision.Disabled,
                "scoped receiver opens the same generated shutter for the A/B entry probes");
            await SettlePlayerAt(tree, player, new Vector2(4200, 1444));
            bool openUpperEntry = await WalkDropToSupport(tree, player, firstUpperStep);
            TestAssert.True(openUpperEntry,
                $"the same normal upper-gallery walk/drop reaches Step1556 after opening; " +
                $"position={player.GlobalPosition}");
            await SettlePlayerAt(tree, player, new Vector2(4175, 3600));
            bool openLowerLatch = await CastAtGripAndObserveLatch(tree, player,
                lowerShortcutGrip);
            TestAssert.True(openLowerLatch,
                $"the same real public-junction claw cast latches after opening; " +
                $"position={player.GlobalPosition}");
            GD.Print($"TASK5_GENERATED_SHORTCUT_LOCKED_BOTH_SIDES_OK " +
                     $"closed_upper={closedUpperEntry} closed_lower_latch={closedLowerLatch} " +
                     $"open_upper={openUpperEntry} open_lower_latch={openLowerLatch}");
        }
        finally
        {
            ReleasePlayerInput();
            await Dispose(tree, root);
            await Frames(tree, 2);
        }
    }

    private static async Task CounterweightMovesARealPassengerAndStopsAtEndpoints(SceneTree tree)
    {
        ReleasePlayerInput();
        var root = new Node2D { Name = "CounterweightRuntime" };
        var lift = MechanismFixture.Lift();
        CharacterBody2D rider = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        rider.Name = "Rider";
        rider.Position = new Vector2(0, -47);
        rider.GetNode<Camera2D>("Camera2D").Enabled = false;
        var crushBoundary = new StaticBody2D { Name = "CrushCeiling", Position = new Vector2(0, -220),
            CollisionLayer = CombatCollisionLayers.World };
        crushBoundary.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(180, 20) } });
        root.AddChild(lift); root.AddChild(crushBoundary); root.AddChild(rider); tree.Root.AddChild(root);
        await Frames(tree, 5);
        PlayerMotor motor = rider.GetNode<PlayerMotor>("Motor");
        TestAssert.True(rider.IsOnFloor(), "real Player settles on the counterweight carriage");

        Vector2 idle = lift.Position;
        await Frames(tree, 5);
        TestAssert.Near(idle.Y, lift.Position.Y, .01f, "lift waits for activation");
        lift.Activate();
        await WaitFor(tree, () => lift.ElapsedSeconds > lift.TravelSeconds + .01f &&
                                  lift.Frame.Phase01 > .999f,
            "counterweight upper dwell");
        TestAssert.Near(lift.UpperPosition.Y, lift.Position.Y, 1.5f,
            "real AnimatableBody lift stops at upper endpoint");
        TestAssert.True(rider.Position.Y < -150f && rider.IsOnFloor(),
            "real PlayerMotor passenger remains supported through the upward carriage leg");
        float upperPassengerY = rider.Position.Y;
        TestAssert.True(!rider.TestMove(rider.GlobalTransform, Vector2.Up * 2f) &&
                        rider.TestMove(rider.GlobalTransform, Vector2.Up * 20f),
            "upper endpoint leaves real Player clearance while detecting the authored crushing boundary");

        await WaitFor(tree, () => lift.ElapsedSeconds > lift.TravelSeconds * 2f +
                                  lift.EndpointDwellSeconds + .01f && lift.Frame.Phase01 < .001f,
            "counterweight lower dwell after reversal");
        TestAssert.Near(lift.LowerPosition.Y, lift.Position.Y, 1.5f,
            "lift reverses and stops at lower endpoint");
        TestAssert.True(rider.Position.Y > upperPassengerY + 100f && rider.IsOnFloor(),
            "real PlayerMotor passenger remains supported through the full downward carriage leg");

        lift.ResetForCheckpoint();
        rider.Position = new Vector2(0, -47); motor.ResetTransientState();
        await Frames(tree, 4);
        int jumpsBefore = motor.AcceptedJumpCount;
        float jumpStartY = rider.Position.Y;
        Input.ActionPress(InputActions.Jump);
        await WaitFor(tree, () => motor.AcceptedJumpCount == jumpsBefore + 1 && !rider.IsOnFloor(),
            "real carriage jump departure");
        Input.ActionRelease(InputActions.Jump);
        await Frames(tree, 2);
        TestAssert.True(rider.Position.Y < jumpStartY - 2f,
            "normal Player input visibly jumps away from the carriage");

        lift.ResetForCheckpoint();
        rider.Position = new Vector2(0, -47); motor.ResetTransientState();
        await Frames(tree, 4);
        int dashesBefore = motor.AcceptedDashCount;
        Input.ActionPress(InputActions.MoveRight);
        await Frames(tree, 1);
        Input.ActionPress(InputActions.Dash);
        await WaitFor(tree, () => motor.AcceptedDashCount == dashesBefore + 1,
            "real carriage dash acceptance");
        Input.ActionRelease(InputActions.Dash);
        await Frames(tree, 10);
        Input.ActionRelease(InputActions.MoveRight);
        TestAssert.True(rider.Position.X > 70f && !rider.IsOnFloor(),
            "normal Player dash leaves the carriage collision footprint");
        await Dispose(tree, root);
    }

    private static async Task MovingPistonOccluderReleasesAStaticGrip(SceneTree tree)
    {
        var root = new Node2D { Name = "MovingOccluderRuntime", Position = new Vector2(400, 300) };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player"; player.Position = new Vector2(0, 14); player.AddToGroup("player");
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(0, 52),
            CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(800, 24) } });
        var grip = new StaticBody2D { Name = "StaticGrip", Position = new Vector2(260, -20),
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget };
        grip.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(36, 160) } });
        grip.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.TempleMetal, UseCollisionHitPoint = true });
        CanyonGearPiston piston = MechanismFixture.Piston();
        piston.Name = "MovingOccluder"; piston.SafeSeconds = .1f; piston.TelegraphSeconds = .1f;
        piston.ActiveSeconds = 1f; piston.RecoverySeconds = .2f;
        piston.RetractedPosition = new Vector2(130, -220);
        piston.ExtendedPosition = new Vector2(130, -20);
        piston.RouteBounds = new Rect2(90, -260, 80, 360);
        root.AddChild(floor); root.AddChild(grip); root.AddChild(piston); root.AddChild(player);
        tree.Root.AddChild(root); await Frames(tree, 3);

        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        piston.SetPhysicsProcess(false);
        ReleasePlayerInput();
        try
        {
            await AimAt(tree, player, grip.GlobalPosition);
            await TapAction(tree, InputActions.SelectGrappleMode);
            TestAssert.Equal(PlayerActionMode.Grapple, modes.CurrentMode,
                "real E input selects Grapple mode before the occlusion probe");
            await TapAction(tree, InputActions.PrimaryAction);
            await WaitFor(tree, () => claw.CurrentFrame.State is MechanicalClawState.Latched or
                                     MechanicalClawState.Reel or MechanicalClawState.Swing,
                "unobstructed permanent grip attachment");
            TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Latched or
                            MechanicalClawState.Reel or MechanicalClawState.Swing,
                "claw first acquires the unobstructed permanent static grip");

            piston.AdvanceForTest(.25f);
            await Frames(tree, 1);
            await WaitFor(tree, () => claw.CurrentFrame.State is MechanicalClawState.Released or
                                     MechanicalClawState.Failed,
                "moving-piston line-of-sight release");
            TestAssert.True(piston.Frame.HazardActive &&
                            claw.CurrentFrame.State is MechanicalClawState.Released or MechanicalClawState.Failed,
                $"moving forbidden piston entering LOS releases the claw from its separate static grip; " +
                $"piston_active={piston.Frame.HazardActive} collision_disabled={piston.Collision.Disabled} " +
                $"piston={piston.GlobalPosition} claw={claw.CurrentFrame.State} player={player.GlobalPosition} " +
                $"grip={grip.GlobalPosition}");
        }
        finally
        {
            ReleasePlayerInput();
            await Dispose(tree, root);
        }
    }

    private static async Task PistonSteamAndPendulumKeepCollisionAndTelegraphsInSync(SceneTree tree)
    {
        var root = new Node2D { Name = "TimedMechanismRuntime" };
        CanyonGearPiston piston = MechanismFixture.Piston();
        CanyonSteamJet jet = MechanismFixture.Jet();
        CanyonPendulumHazard pendulum = MechanismFixture.Pendulum();
        root.AddChild(piston); root.AddChild(jet); root.AddChild(pendulum); tree.Root.AddChild(root);
        await Frames(tree, 2);

        piston.AdvanceForTest(1.1f);
        TestAssert.True(piston.IsDangerTelegraphActive && piston.Collision.Disabled,
            "piston warning is visible while collision remains disabled");
        piston.AdvanceForTest(.5f);
        TestAssert.True(piston.Frame.HazardActive && !piston.Collision.Disabled,
            "piston collision exists only during active extension");

        jet.AdvanceForTest(1.05f);
        TestAssert.True(jet.IsDangerTelegraphActive && jet.Collision.Disabled,
            "steam telegraph precedes active hazard overlap");
        jet.AdvanceForTest(.4f);
        TestAssert.True(jet.Frame.HazardActive && !jet.Collision.Disabled,
            "steam collision follows its cycle rather than remaining decorative");

        pendulum.AdvanceForTest(.5f);
        await Frames(tree, 1);
        TestAssert.Near(pendulum.Rotation, pendulum.VisibleAngleRadians, .001f,
            "pendulum body collision transform and visible angle are identical");
        TestAssert.Equal(GrappleSurfaceMaterial.Forbidden,
            pendulum.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material,
            "moving pendulum is permanently forbidden to the claw");

        float pistonTime = piston.ElapsedSeconds;
        float jetTime = jet.ElapsedSeconds;
        float pendulumTime = pendulum.ElapsedSeconds;
        tree.Paused = true;
        piston.AdvanceForTest(.5f); jet.AdvanceForTest(.5f); pendulum.AdvanceForTest(.5f);
        TestAssert.Near(pistonTime, piston.ElapsedSeconds, .001f, "pause freezes piston timer");
        TestAssert.Near(jetTime, jet.ElapsedSeconds, .001f, "pause freezes steam timer");
        TestAssert.Near(pendulumTime, pendulum.ElapsedSeconds, .001f, "pause freezes pendulum timer");
        tree.Paused = false;
        await Dispose(tree, root);
    }

    private static async Task ValveSealAndShortcutUsePersistentRunState(SceneTree tree)
    {
        ReleasePlayerInput();
        var progress = new PrologueProgress();
        var root = new Node2D { Name = "PersistentMechanismRuntime" };
        CanyonSteamJet jet = MechanismFixture.Jet(); jet.Name = "Jet";
        CanyonSteamValve valve = MechanismFixture.Valve(); valve.PairedJetPath = new NodePath("../Jet");
        CanyonSealGate seal = MechanismFixture.Seal();
        CanyonShortcutWinch winch = MechanismFixture.Winch(); winch.Position = new Vector2(500, 0);
        var floor = new StaticBody2D { Name = "ShortcutFloor", Position = new Vector2(500, 100),
            CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(420, 24) } });
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "ShortcutPlayer"; player.Position = new Vector2(410, 52);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        root.AddChild(jet); root.AddChild(valve); root.AddChild(seal); root.AddChild(winch);
        root.AddChild(floor); root.AddChild(player);
        tree.Root.AddChild(root); await Frames(tree, 2);
        valve.ApplyProgress(progress); seal.ApplyProgress(progress); winch.ApplyProgress(progress);
        winch.Opened += id => progress.OpenShortcut(id.ToString());

        TestAssert.True(valve.TryReceiveThread(default), "valve accepts one-shot control input");
        TestAssert.True(jet.DisabledForRun && progress.ActivatedMechanisms.Contains(valve.ActivationId),
            "valve permanently disables its paired jet for this run");

        progress.ActivateMechanism(seal.RequiredMechanismId);
        seal.RefreshState();
        TestAssert.True(!seal.IsOpen && !seal.Collision.Disabled,
            "mechanism prerequisite alone leaves the courtyard visibly sealed");
        progress.ActivateMechanism(seal.RequiredEncounterId);
        seal.RefreshState();
        TestAssert.True(seal.IsOpen && seal.Collision.Disabled &&
                        progress.ActivatedMechanisms.Contains(seal.ActivationId),
            "both named prerequisites latch the completed seal collision state");

        Input.ActionPress(InputActions.MoveRight);
        await Frames(tree, 35);
        Input.ActionRelease(InputActions.MoveRight);
        TestAssert.True(player.Position.X < 470f,
            "closed shortcut barrier physically stops the real Player approaching from Basin");
        TestAssert.True(winch.TryReceiveThread(default) && winch.IsOpen && winch.BarrierCollision.Disabled,
            "winch changes a static blocked corridor into open geometry");
        await Frames(tree, 2);
        Input.ActionPress(InputActions.MoveRight);
        await WaitFor(tree, () => player.Position.X > 535f,
            "opened shortcut Basin-to-upper traversal");
        Input.ActionRelease(InputActions.MoveRight);
        player.Position = new Vector2(590, 52); motor.ResetTransientState();
        await Frames(tree, 3);
        Input.ActionPress(InputActions.MoveLeft);
        await WaitFor(tree, () => player.Position.X < 465f,
            "opened shortcut upper-to-Basin traversal");
        Input.ActionRelease(InputActions.MoveLeft);
        TestAssert.True(player.Position.X < 465f,
            "opened shortcut admits real Player traversal from both physical approaches");
        TestAssert.True(winch.GetNodeOrNull<AnimatableBody2D>("PassengerCar") == null,
            "shortcut winch contains no passenger-moving body");
        CanyonShortcutWinch remounted = MechanismFixture.Winch();
        try
        {
            remounted.ApplyProgress(progress);
            TestAssert.True(remounted.IsOpen && remounted.BarrierCollision.Disabled,
                "shortcut survives sector retirement/remount through same-run progress");
            TestAssert.True(remounted.CorridorBounds.HasPoint(new Vector2(-30, 0)) &&
                            remounted.CorridorBounds.HasPoint(new Vector2(30, 0)),
                "opened corridor declares bidirectional traversal space");
        }
        finally
        {
            remounted.Free();
            ReleasePlayerInput();
            await Dispose(tree, root);
        }
    }

    private static async Task GeneratedSectorsContainSixDistinctFamilies(SceneTree tree)
    {
        string[] paths = ["res://scenes/prologue/sectors/CanyonBasin.tscn",
            "res://scenes/prologue/sectors/CanyonAscent.tscn", "res://scenes/prologue/sectors/LostTemple.tscn"];
        var root = new Node2D { Name = "GeneratedMechanismSectors" };
        foreach (string path in paths)
        {
            PrologueSector sector = GD.Load<PackedScene>(path).Instantiate<PrologueSector>();
            sector.Position = CanyonWorldLayout.SectorBounds[sector.SectorId].Position;
            root.AddChild(sector);
        }
        tree.Root.AddChild(root); await Frames(tree, 2);
        TestAssert.True(root.GetNode("CanyonBasin/Gameplay/Mechanisms/GearPiston") is CanyonGearPiston &&
                        root.GetNode("CanyonBasin/Gameplay/Mechanisms/SteamValve") is CanyonSteamValve &&
                        root.GetNode("CanyonBasin/Gameplay/Mechanisms/SteamJet") is CanyonSteamJet &&
                        root.GetNode("CanyonAscent/Gameplay/Mechanisms/CounterweightLift") is CanyonCounterweightLift &&
                        root.GetNode("CanyonAscent/Gameplay/Mechanisms/PendulumHazard") is CanyonPendulumHazard &&
                        root.GetNode("LostTemple/Gameplay/Mechanisms/CourtyardSeal") is CanyonSealGate &&
                        root.GetNode("CanyonAscent/Gameplay/Mechanisms/UpperBasinShortcutWinch") is CanyonShortcutWinch,
            "generated Basin/Ascent/Temple mount all six distinct functional families and shortcut gate");
        var pendulum = root.GetNode<CanyonPendulumHazard>(
            "CanyonAscent/Gameplay/Mechanisms/PendulumHazard");
        var pendulumCollision = pendulum.GetNode<CollisionShape2D>("Collision");
        var pendulumShape = (RectangleShape2D)pendulumCollision.Shape;
        Rect2 pendulumNodeBounds = CanyonWorldLayout.Nodes.First(node =>
            node.Id == CanyonRouteNodeId.PendulumGallery).WorldBounds;
        Rect2 pendulumSweep = RotatingRectangleSweep(pendulum.GlobalPosition,
            pendulumCollision.Position, pendulumShape.Size, pendulum.MaxAngleDegrees);
        StaticBody2D pendulumCeiling = root.GetNode<StaticBody2D>(
            "CanyonAscent/Gameplay/Collision/PendulumCeiling");
        Rect2 ceilingBounds = BodyBounds(pendulumCeiling.GlobalPosition,
            ((RectangleShape2D)pendulumCeiling.GetNode<CollisionShape2D>("Collision").Shape).Size);
        TestAssert.True(pendulumNodeBounds.Encloses(pendulumSweep) &&
                        ceilingBounds.HasPoint(pendulum.GlobalPosition),
            $"generated pendulum full visible/collision sweep and pivot support stay inside the " +
            $"PendulumGallery node; node={pendulumNodeBounds} sweep={pendulumSweep} " +
            $"support={ceilingBounds}");
        GD.Print($"TASK5_GENERATED_PENDULUM_BOUNDS_OK node={pendulumNodeBounds} " +
                 $"sweep={pendulumSweep} support={ceilingBounds}");
        await Dispose(tree, root);
    }

    private static async Task GeneratedCounterweightConnectsBothPhysicalApproaches(SceneTree tree)
    {
        ReleasePlayerInput();
        var root = new Node2D { Name = "Task5GeneratedCounterweight" };
        PrologueSector basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        PrologueSector ascent = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
            .Instantiate<PrologueSector>();
        basin.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin].Position;
        ascent.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "GeneratedLiftPlayer";
        player.Position = new Vector2(5050, 3564);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(basin); root.AddChild(ascent); root.AddChild(player); tree.Root.AddChild(root);
        try
        {
            CanyonCounterweightLift lift = ascent.GetNode<CanyonCounterweightLift>(
                "Gameplay/Mechanisms/CounterweightLift");
            StaticBody2D steamLanding = basin.GetNode<StaticBody2D>(
                "Gameplay/Collision/SteamRejoinTop");
            StaticBody2D steamStep = basin.GetNode<StaticBody2D>(
                "Gameplay/Collision/SteamRejoinStep8");
            StaticBody2D backLanding = ascent.GetNode<StaticBody2D>(
                "Gameplay/Collision/LiftBackLanding");
            StaticBody2D upperLanding = ascent.GetNode<StaticBody2D>(
                "Gameplay/Collision/AscentGrip2");
            await Frames(tree, 5);
            TestAssert.True(HasFloorContact(player, steamLanding) || HasFloorContact(player, steamStep),
                "lower Steam branch begins on its real generated landing");

            var carriage = (RectangleShape2D)lift.GetNode<CollisionShape2D>("Collision").Shape;
            Rect2 nodeBounds = CanyonWorldLayout.Nodes.First(node =>
                node.Id == CanyonRouteNodeId.CounterweightLift).WorldBounds;
            Rect2 lowerBounds = BodyBounds(ascent.Position + lift.LowerPosition, carriage.Size);
            Rect2 upperBounds = BodyBounds(ascent.Position + lift.UpperPosition, carriage.Size);
            Rect2 sweepBounds = lowerBounds.Merge(upperBounds);
            Rect2 backBounds = BodyBounds(backLanding.GlobalPosition,
                ((RectangleShape2D)backLanding.GetNode<CollisionShape2D>("Collision").Shape).Size);
            string[] staticIntrusions = new[] { basin, ascent }
                .SelectMany(sector => sector.GetNode("Gameplay/Collision").GetChildren())
                .OfType<StaticBody2D>()
                .Where(body => body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D shape &&
                               sweepBounds.Intersects(BodyBounds(body.GlobalPosition, shape.Size)))
                .Select(body => body.Name.ToString()).ToArray();
            TestAssert.True(nodeBounds.Encloses(lowerBounds) && nodeBounds.Encloses(upperBounds) &&
                            !sweepBounds.Intersects(backBounds) && staticIntrusions.Length == 0,
                $"generated carriage sweep stays inside its route node and clear of every static approach; " +
                $"node={nodeBounds} sweep={sweepBounds} back={backBounds} " +
                $"intrusions={string.Join(",", staticIntrusions)}");

            await WalkOnEitherSupportToX(tree, player, steamLanding, steamStep, 4980f);
            await JumpTowardSupport(tree, player, lift, moveRight: false,
                "lower Steam landing to generated carriage");
            TestAssert.True(player.GlobalPosition.Y < 3600f && HasFloorContact(player, lift),
                $"real Player boards inside the Ascent boundary; position={player.GlobalPosition}");
            AssertCapsuleFullyOnCarriage(player, lift, carriage.Size,
                "lower Steam approach boarding");
            lift.Activate();
            await RideGeneratedLift(tree, player, lift, nodeBounds, upper: true,
                "generated carriage upper endpoint with its real passenger");
            TestAssert.Near(ascent.Position.Y + lift.UpperPosition.Y, lift.GlobalPosition.Y, 1.5f,
                "generated carriage stops at the authored upper endpoint");

            await WalkTowardSupport(tree, player, upperLanding, moveRight: false,
                "upper carriage to Gear/main landing");
            TestAssert.True(HasFloorContact(player, upperLanding),
                "upper endpoint has a real physical exit toward the Gear/main approach");

            await WalkOnSupportToX(tree, player, upperLanding, 4760f,
                moveRight: player.GlobalPosition.X < 4760f);
            await WaitFor(tree, () =>
            {
                float cycle = (lift.TravelSeconds + lift.EndpointDwellSeconds) * 2f;
                float cycleTime = Mathf.PosMod(lift.ElapsedSeconds, cycle);
                return cycleTime < lift.TravelSeconds && lift.Frame.Phase01 >= .72f &&
                       lift.Frame.Phase01 <= .78f;
            },
                "generated upward carriage approach for level boarding");
            int upperBoardJumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
            await WalkTowardSupport(tree, player, lift, moveRight: true,
                "upper Gear/main landing onto the approaching generated carriage");
            await NudgeFullyOntoCarriage(tree, player, lift, carriage.Size);
            TestAssert.Equal(upperBoardJumps,
                player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount,
                "upper approach boards with ordinary D movement and no hidden jump or boost");
            AssertCapsuleFullyOnCarriage(player, lift, carriage.Size,
                "upper Gear/main approach boarding");
            await RideGeneratedLift(tree, player, lift, nodeBounds, upper: false,
                "generated carriage lower endpoint with a real downward passenger");
            TestAssert.Near(ascent.Position.Y + lift.LowerPosition.Y, lift.GlobalPosition.Y, 1.5f,
                "generated carriage reverses and stops at the authored lower endpoint");

            await JumpTowardEitherSupport(tree, player, steamLanding, steamStep, moveRight: true,
                "lower carriage back to Steam landing");
            TestAssert.True(HasFloorContact(player, steamLanding) || HasFloorContact(player, steamStep),
                "lower endpoint has a real physical exit back to the Steam approach");
            GD.Print($"TASK5_GENERATED_LIFT_BOTH_APPROACHES_OK lower={lowerBounds} upper={upperBounds} " +
                     $"passenger={player.GlobalPosition} within_node=True");
        }
        finally
        {
            ReleasePlayerInput();
            await Dispose(tree, root);
            await Frames(tree, 2);
        }
    }

    private static async Task GeneratedLowerBoardingToleratesApproachOffsets(SceneTree tree)
    {
        foreach (float launchX in new[] { 4960f, 4980f, 5000f })
        {
            ReleasePlayerInput();
            var root = new Node2D { Name = $"Task5GeneratedLiftTolerance{launchX}" };
            PrologueSector basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
                .Instantiate<PrologueSector>();
            PrologueSector ascent = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
                .Instantiate<PrologueSector>();
            basin.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin].Position;
            ascent.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonAscent].Position;
            CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            player.Name = "GeneratedLiftTolerancePlayer";
            player.Position = new Vector2(launchX, 3564);
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            root.AddChild(basin); root.AddChild(ascent); root.AddChild(player); tree.Root.AddChild(root);
            try
            {
                await Frames(tree, 5);
                StaticBody2D steamLanding = basin.GetNode<StaticBody2D>(
                    "Gameplay/Collision/SteamRejoinTop");
                StaticBody2D steamStep = basin.GetNode<StaticBody2D>(
                    "Gameplay/Collision/SteamRejoinStep8");
                TestAssert.True(HasFloorContact(player, steamLanding) || HasFloorContact(player, steamStep),
                    $"tolerance launch x={launchX} is fully supported on the generated Steam approach");
                CanyonCounterweightLift lift = ascent.GetNode<CanyonCounterweightLift>(
                    "Gameplay/Mechanisms/CounterweightLift");
                var carriage = (RectangleShape2D)lift.GetNode<CollisionShape2D>("Collision").Shape;
                if (player.GlobalPosition.X > 4982f)
                    await WalkOnEitherSupportToX(tree, player, steamLanding, steamStep, 4980f);
                float actualLaunchX = player.GlobalPosition.X;
                await JumpTowardSupport(tree, player, lift, moveRight: false,
                    $"Steam launch x={launchX} to generated carriage");
                AssertCapsuleFullyOnCarriage(player, lift, carriage.Size,
                    $"Steam launch x={launchX}");
                GD.Print($"TASK5_GENERATED_LIFT_TOLERANCE_OK start_x={launchX} " +
                         $"actual_launch_x={actualLaunchX} " +
                         $"landing={player.GlobalPosition} hold_frames=20");
            }
            finally
            {
                ReleasePlayerInput();
                await Dispose(tree, root);
                await Frames(tree, 2);
            }
        }
    }

    private static void ShortcutSignalConsumerPersistsOnlyNewStableIds()
    {
        var level = new PrologueLevelController();
        int persistRequests = 0;
        level.ProgressPersistRequested += reason =>
        {
            if (reason == "shortcut") persistRequests++;
        };
        level.RecordShortcutOpened("canyon.v2.upper-basin-shortcut");
        level.RecordShortcutOpened("canyon.v2.upper-basin-shortcut");
        level.RecordShortcutOpened();
        TestAssert.True(level.Progress.IsShortcutOpen("canyon.v2.upper-basin-shortcut") &&
                        level.Progress.BellChainShortcutOpen && persistRequests == 2,
            "stable-id consumer keeps gates independent and duplicate delivery requests no extra persist");
        level.Free();
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Dispose(SceneTree tree, Node root)
    {
        root.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> predicate, string description)
    {
        for (int frame = 0; frame < 360 && !predicate(); frame++) await Frames(tree, 1);
        TestAssert.True(predicate(), $"bounded real physics reaches {description}");
    }

    private static bool BossHallReachable(PrologueProgress progress) =>
        CanyonRouteGraph.Reachable(CanyonRouteNodeId.SealedCourtyard,
                progress.OpenShortcuts, progress.ActivatedMechanisms, progress.BossDefeated)
            .Contains(CanyonRouteNodeId.BossHall);

    private static async Task SettlePlayerAt(SceneTree tree, CharacterBody2D player,
        Vector2 position)
    {
        ReleasePlayerInput();
        player.Position = position;
        player.GetNode<PlayerMotor>("Motor").ResetTransientState();
        player.GetNode<MechanicalClawController>("MechanicalClaw").ResetTransientState();
        await Frames(tree, 5);
    }

    private static async Task<bool> DropAlongClosedShortcutRightSide(SceneTree tree,
        CharacterBody2D player, StaticBody2D target)
    {
        ReleasePlayerInput();
        Input.ActionPress(InputActions.MoveLeft);
        for (int frame = 0; frame < 45 && player.IsOnFloor(); frame++) await Frames(tree, 1);
        Input.ActionRelease(InputActions.MoveLeft);
        for (int frame = 0; frame < 150 && !HasFloorContact(player, target); frame++)
        {
            if (player.GlobalPosition.X < 4130f)
            {
                Input.ActionRelease(InputActions.MoveLeft);
                Input.ActionPress(InputActions.MoveRight);
            }
            else if (player.GlobalPosition.X > 4210f)
            {
                Input.ActionRelease(InputActions.MoveRight);
                Input.ActionPress(InputActions.MoveLeft);
            }
            else
            {
                Input.ActionRelease(InputActions.MoveLeft);
                Input.ActionRelease(InputActions.MoveRight);
            }
            await Frames(tree, 1);
        }
        ReleasePlayerInput();
        bool landed = HasFloorContact(player, target);
        GD.Print($"TASK5_LOCKED_SHORTCUT_SAME_SIDE_DROP target={target.Name} " +
                 $"landed={landed} position={player.GlobalPosition}");
        return landed;
    }

    private static async Task<bool> WalkDropToSupport(SceneTree tree, CharacterBody2D player,
        StaticBody2D target)
    {
        ReleasePlayerInput();
        int direction = target.GlobalPosition.X < player.GlobalPosition.X ? -1 : 1;
        Input.ActionPress(direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight);
        for (int frame = 0; frame < 300 && !HasFloorContact(player, target); frame++)
        {
            if (Mathf.Abs(player.GlobalPosition.X - target.GlobalPosition.X) < 8f)
            {
                Input.ActionRelease(InputActions.MoveLeft);
                Input.ActionRelease(InputActions.MoveRight);
            }
            await Frames(tree, 1);
        }
        ReleasePlayerInput();
        bool landed = HasFloorContact(player, target);
        GD.Print($"TASK5_SHORTCUT_UPPER_ENTRY target={target.Name} landed={landed} " +
                 $"position={player.GlobalPosition}");
        return landed;
    }

    private static async Task<bool> CastAtGripAndObserveLatch(SceneTree tree,
        CharacterBody2D player, GrappleSurface2D target)
    {
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        claw.ResetTransientState();
        bool latched = false;
        void Observe(MechanicalClawFrame frame, GrappleSurface2D surface)
        {
            if (surface == target && frame.State is (MechanicalClawState.Latched or
                    MechanicalClawState.Reel or MechanicalClawState.Swing))
                latched = true;
        }
        claw.ClawFrameChanged += Observe;
        try
        {
            await AimAt(tree, player, target.WorldAnchor);
            await TapAction(tree, InputActions.SelectGrappleMode);
            await TapAction(tree, InputActions.PrimaryAction);
            for (int frame = 0; frame < 120 && !latched; frame++) await Frames(tree, 1);
            return latched;
        }
        finally
        {
            claw.ClawFrameChanged -= Observe;
            ReleasePlayerInput();
        }
    }

    private static async Task AssertCompletedSealRemountWithoutPrerequisiteReplay(SceneTree tree,
        Node parent, string activationId)
    {
        var completedOnly = new PrologueProgress();
        completedOnly.ActivateMechanism(activationId);
        PrologueSector remount = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn")
            .Instantiate<PrologueSector>();
        remount.Name = "LostTempleSealRemount";
        remount.Position = CanyonWorldLayout.SectorBounds[PrologueSectorId.LostTemple].Position;
        parent.AddChild(remount);
        await Frames(tree, 2);
        CanyonSealGate seal = remount.GetNode<CanyonSealGate>(
            "Gameplay/Mechanisms/CourtyardSeal");
        var encounter = new CanyonEncounterGroup
        {
            Name = "RemountEventSentinel",
            EncounterId = "canyon.v2.encounter.split-level"
        };
        parent.AddChild(encounter);
        int completionEvents = 0;
        encounter.Completed += _ => completionEvents++;
        seal.ApplyProgress(completedOnly);
        encounter.ApplyProgress(completedOnly);
        CanvasItem closed = seal.GetNode<CanvasItem>(seal.ClosedVisualPath);
        CanvasItem open = seal.GetNode<CanvasItem>(seal.OpenVisualPath);
        TestAssert.True(seal.IsOpen && seal.Collision.Disabled && !closed.Visible && open.Visible &&
                        !encounter.IsComplete && completionEvents == 0 &&
                        BossHallReachable(completedOnly),
            "completed-seal remount restores collision, visuals, and graph access without " +
            "replaying either prerequisite or synthesizing an encounter-completion event");
        remount.QueueFree();
        encounter.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task CaptureSteamFrame(SceneTree tree, string output, string name)
    {
        if (output == null) return;
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = tree.Root.GetTexture().GetImage();
        TestAssert.Equal(1280, image.GetWidth(), $"{name} steam proof width");
        TestAssert.Equal(720, image.GetHeight(), $"{name} steam proof height");
        TestAssert.Equal(Error.Ok, image.SavePng(Path.Combine(output, name + ".png")),
            $"{name} steam proof saves outside the project");
    }

    private static Rect2 BodyBounds(Vector2 center, Vector2 size) => new(center - size / 2f, size);

    private static Rect2 PolygonBounds(Polygon2D polygon)
    {
        Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector2 local in polygon.Polygon)
        {
            Vector2 point = polygon.ToGlobal(local);
            minimum = new(Mathf.Min(minimum.X, point.X), Mathf.Min(minimum.Y, point.Y));
            maximum = new(Mathf.Max(maximum.X, point.X), Mathf.Max(maximum.Y, point.Y));
        }
        return new Rect2(minimum, maximum - minimum);
    }

    private static Rect2 RotatingRectangleSweep(Vector2 pivot, Vector2 localCenter,
        Vector2 size, float maxAngleDegrees)
    {
        Vector2 minimum = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new(float.NegativeInfinity, float.NegativeInfinity);
        Vector2 half = size / 2f;
        Vector2[] corners = [new(-half.X, -half.Y), new(half.X, -half.Y),
            new(half.X, half.Y), new(-half.X, half.Y)];
        for (float degrees = -maxAngleDegrees; degrees <= maxAngleDegrees + .001f; degrees += .25f)
        {
            float radians = Mathf.DegToRad(degrees);
            foreach (Vector2 corner in corners)
            {
                Vector2 point = pivot + (localCenter + corner).Rotated(radians);
                minimum = new Vector2(Mathf.Min(minimum.X, point.X), Mathf.Min(minimum.Y, point.Y));
                maximum = new Vector2(Mathf.Max(maximum.X, point.X), Mathf.Max(maximum.Y, point.Y));
            }
        }
        return new Rect2(minimum, maximum - minimum);
    }

    private static bool HasFloorContact(CharacterBody2D player, CollisionObject2D support)
    {
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            if (collision.GetCollider() == support && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static async Task RideGeneratedLift(SceneTree tree, CharacterBody2D player,
        CanyonCounterweightLift lift, Rect2 routeBounds, bool upper, string description)
    {
        float startY = player.GlobalPosition.Y;
        for (int frame = 0; frame < 300; frame++)
        {
            await Frames(tree, 1);
            Rect2 capsule = BodyBounds(player.GlobalPosition, new Vector2(40, 72));
            TestAssert.True(routeBounds.Encloses(capsule),
                $"full Player capsule stays inside CounterweightLift bounds during {description}; " +
                $"frame={frame} capsule={capsule} route={routeBounds}");
            TestAssert.True(HasFloorContact(player, lift),
                $"real passenger remains supported without crushing during {description}; " +
                $"frame={frame} position={player.GlobalPosition} collisions={CollisionSummary(player)}");
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
                TestAssert.True(player.GetSlideCollision(index).GetCollider() == lift,
                    $"generated shaft has no foreign-body squeeze during {description}; " +
                    $"frame={frame} collisions={CollisionSummary(player)}");
            bool reached = upper
                ? lift.Frame.Phase01 > .999f && player.GlobalPosition.Y < startY - 300f
                : lift.Frame.Phase01 < .001f && player.GlobalPosition.Y > startY + 300f;
            if (reached) return;
        }
        TestAssert.True(false, $"bounded real physics reaches {description}");
    }

    private static void AssertCapsuleFullyOnCarriage(CharacterBody2D player,
        CanyonCounterweightLift lift, Vector2 carriageSize, string description)
    {
        float left = lift.GlobalPosition.X - carriageSize.X / 2f + 20f;
        float right = lift.GlobalPosition.X + carriageSize.X / 2f - 20f;
        TestAssert.True(HasFloorContact(player, lift) && player.GlobalPosition.X >= left - .1f &&
                        player.GlobalPosition.X <= right + .1f,
            $"full 40px Player capsule lands inside the 80px carriage after {description}; " +
            $"position={player.GlobalPosition} safe_x={left}..{right}");
    }

    private static async Task NudgeFullyOntoCarriage(SceneTree tree, CharacterBody2D player,
        CanyonCounterweightLift lift, Vector2 carriageSize)
    {
        float safeLeft = lift.GlobalPosition.X - carriageSize.X / 2f + 20f;
        Input.ActionPress(InputActions.MoveRight);
        for (int frame = 0; frame < 30 && player.GlobalPosition.X < safeLeft + 2f; frame++)
            await Frames(tree, 1);
        Input.ActionRelease(InputActions.MoveRight);
        await Frames(tree, 1);
        TestAssert.True(HasFloorContact(player, lift),
            $"continued ordinary D movement settles fully onto the approaching carriage; " +
            $"position={player.GlobalPosition} support={lift.GlobalPosition}");
    }

    private static async Task WalkOnEitherSupportToX(SceneTree tree, CharacterBody2D player,
        CollisionObject2D first, CollisionObject2D second, float targetX)
    {
        Input.ActionPress(InputActions.MoveLeft);
        for (int frame = 0; frame < 60 && player.GlobalPosition.X > targetX + 2f; frame++)
            await Frames(tree, 1);
        Input.ActionRelease(InputActions.MoveLeft);
        TestAssert.True(player.GlobalPosition.X <= targetX + 2f &&
                        (HasFloorContact(player, first) || HasFloorContact(player, second)),
            $"normal A input walks to a supported lower-lift launch; target={targetX} " +
            $"position={player.GlobalPosition}");
        await Frames(tree, 2);
    }

    private static async Task WalkOnSupportToX(SceneTree tree, CharacterBody2D player,
        CollisionObject2D support, float targetX, bool moveRight)
    {
        GD.Print($"TASK5_LIFT_LAUNCH_ENTRY target={targetX} move_right={moveRight} " +
                 $"position={player.GlobalPosition} velocity={player.Velocity} " +
                 $"supported={HasFloorContact(player, support)}");
        StringName horizontal = moveRight ? InputActions.MoveRight : InputActions.MoveLeft;
        Input.ActionPress(horizontal);
        int walkedFrames = 0;
        for (int frame = 0; frame < 60 &&
             (moveRight ? player.GlobalPosition.X < targetX - 2f : player.GlobalPosition.X > targetX + 2f);
             frame++)
        {
            await Frames(tree, 1);
            walkedFrames++;
        }
        Input.ActionRelease(horizontal);
        GD.Print($"TASK5_LIFT_LAUNCH_RESULT target={targetX} walked_frames={walkedFrames} " +
                 $"position={player.GlobalPosition} velocity={player.Velocity} " +
                 $"supported={HasFloorContact(player, support)}");
        TestAssert.True(Mathf.Abs(player.GlobalPosition.X - targetX) <= 8f &&
                        HasFloorContact(player, support),
            $"normal directional input reaches supported lift launch x={targetX}; " +
            $"position={player.GlobalPosition}");
        await Frames(tree, 2);
    }

    private static async Task JumpTowardSupport(SceneTree tree, CharacterBody2D player,
        CollisionObject2D support, bool moveRight, string description)
    {
        const int normalJumpHoldFrames = 20;
        StringName horizontal = moveRight ? InputActions.MoveRight : InputActions.MoveLeft;
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        int jumpsBefore = motor.AcceptedJumpCount;
        Input.ActionPress(horizontal);
        Input.ActionPress(InputActions.Jump);
        bool releasedHorizontal = false;
        for (int frame = 0; frame < normalJumpHoldFrames; frame++)
        {
            await Frames(tree, 1);
            if (!releasedHorizontal &&
                (moveRight ? player.GlobalPosition.X >= support.GlobalPosition.X
                    : player.GlobalPosition.X <= support.GlobalPosition.X))
            {
                Input.ActionRelease(horizontal);
                releasedHorizontal = true;
            }
        }
        Input.ActionRelease(InputActions.Jump);
        TestAssert.Equal(jumpsBefore + 1, motor.AcceptedJumpCount,
            $"normal Player input accepts the jump for {description}");
        for (int frame = 0; frame < 150 && !HasFloorContact(player, support); frame++)
        {
            await Frames(tree, 1);
            if (!releasedHorizontal &&
                (moveRight ? player.GlobalPosition.X >= support.GlobalPosition.X
                    : player.GlobalPosition.X <= support.GlobalPosition.X))
            {
                Input.ActionRelease(horizontal);
                releasedHorizontal = true;
            }
            if (frame % 15 == 0)
                GD.Print($"TASK5_LIFT_JUMP_TRACE target={description} frame={frame} " +
                         $"position={player.GlobalPosition} velocity={player.Velocity} " +
                         $"floor={player.IsOnFloor()} support={support.GlobalPosition} " +
                         $"phase={(support as CanyonCounterweightLift)?.Frame.Phase01} " +
                         $"elapsed={(support as CanyonCounterweightLift)?.ElapsedSeconds} " +
                         $"contacts={CollisionSummary(player)}");
        }
        Input.ActionRelease(horizontal);
        TestAssert.True(HasFloorContact(player, support),
            $"normal Player jump reaches {description}; position={player.GlobalPosition} " +
            $"hold_frames={normalJumpHoldFrames}");
        await Frames(tree, 2);
    }

    private static async Task WalkTowardSupport(SceneTree tree, CharacterBody2D player,
        CollisionObject2D support, bool moveRight, string description)
    {
        StringName horizontal = moveRight ? InputActions.MoveRight : InputActions.MoveLeft;
        Input.ActionPress(horizontal);
        for (int frame = 0; frame < 150 && !HasFloorContact(player, support); frame++)
            await Frames(tree, 1);
        Input.ActionRelease(horizontal);
        TestAssert.True(HasFloorContact(player, support),
            $"normal Player movement reaches {description}; position={player.GlobalPosition}");
        await Frames(tree, 2);
    }

    private static async Task JumpTowardEitherSupport(SceneTree tree, CharacterBody2D player,
        CollisionObject2D first, CollisionObject2D second, bool moveRight, string description)
    {
        const int normalJumpHoldFrames = 20;
        StringName horizontal = moveRight ? InputActions.MoveRight : InputActions.MoveLeft;
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        int jumpsBefore = motor.AcceptedJumpCount;
        Input.ActionPress(horizontal);
        Input.ActionPress(InputActions.Jump);
        bool releasedHorizontal = false;
        for (int frame = 0; frame < normalJumpHoldFrames; frame++)
        {
            await Frames(tree, 1);
            if (!releasedHorizontal &&
                (moveRight ? player.GlobalPosition.X >= first.GlobalPosition.X
                    : player.GlobalPosition.X <= first.GlobalPosition.X))
            {
                Input.ActionRelease(horizontal);
                releasedHorizontal = true;
            }
        }
        Input.ActionRelease(InputActions.Jump);
        TestAssert.Equal(jumpsBefore + 1, motor.AcceptedJumpCount,
            $"normal Player input accepts the jump for {description}");
        for (int frame = 0; frame < 150 && !HasFloorContact(player, first) &&
             !HasFloorContact(player, second); frame++)
        {
            await Frames(tree, 1);
            if (!releasedHorizontal &&
                (moveRight ? player.GlobalPosition.X >= first.GlobalPosition.X
                    : player.GlobalPosition.X <= first.GlobalPosition.X))
            {
                Input.ActionRelease(horizontal);
                releasedHorizontal = true;
            }
        }
        Input.ActionRelease(horizontal);
        TestAssert.True(HasFloorContact(player, first) || HasFloorContact(player, second),
            $"normal Player jump reaches {description}; position={player.GlobalPosition} " +
            $"hold_frames={normalJumpHoldFrames}");
        await Frames(tree, 2);
    }

    private static string CollisionSummary(CharacterBody2D player)
    {
        string result = "";
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            result += $"{(collision.GetCollider() as Node)?.Name}:{collision.GetNormal()};";
        }
        return result;
    }

    private static async Task TapAction(SceneTree tree, StringName action)
    {
        Input.ActionPress(action);
        await Frames(tree, 1);
        Input.ActionRelease(action);
        await Frames(tree, 1);
    }

    private static async Task AimAt(SceneTree tree, CharacterBody2D player, Vector2 worldPoint)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Vector2 viewportPoint = player.GetViewport().GetCanvasTransform() * worldPoint;
        Vector2 windowPoint = player.GetViewport().GetFinalTransform() * viewportPoint;
        var motion = new InputEventMouseMotion { Position = windowPoint, GlobalPosition = windowPoint };
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        for (int frame = 0; frame < 4 && input.Current.MouseWorld.DistanceTo(worldPoint) >= .1f; frame++)
            await Frames(tree, 1);
        TestAssert.True(input.HasObservedMouseMotion && input.Current.MouseWorld.DistanceTo(worldPoint) < .1f,
            $"real mouse motion aims at the static grip; sampled={input.Current.MouseWorld} target={worldPoint} " +
            $"window={windowPoint} viewport={viewportPoint} observed={input.LastObservedMouseViewportPosition}");
    }

    private static void ReleasePlayerInput()
    {
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.Dash);
    }

    private sealed class SpringboardFixture
    {
        private readonly SceneTree _tree;
        private readonly List<Shape2D> _owned = new();
        public Node2D Root { get; } = new() { Name = "SpringboardControlFixture" };
        public StaticBody2D Floor { get; }
        public StaticBody2D LowerFloor { get; private set; }
        public CollisionShape2D FloorCollider { get; }
        public CharacterBody2D Player { get; }
        public CanyonSpringboard Board { get; }
        public bool RegisterPlayer { get; set; } = true;
        public KeyboardRouteInputDriver Driver;
        public PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        public HealthComponent Health => Player.GetNode<HealthComponent>("Health");
        public SpringboardFixture(SceneTree tree)
        {
            _tree = tree;
            Floor = AddFloor("Floor", new Rect2(0, 500, 800, 40));
            FloorCollider = Floor.GetNode<CollisionShape2D>("Collision");
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(180, 464); Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
            Board = new CanyonSpringboard { Name = "Pad", Position = new(300, 500), SupportBodyPath = new("../Floor") };
            var sensor = new RectangleShape2D { Size = new(64, 12) }; _owned.Add(sensor);
            Board.AddChild(new CollisionShape2D { Name = "Collision", Position = new(0, -6), Shape = sensor });
            Root.AddChild(Board);
        }
        public StaticBody2D AddFloor(string name, Rect2 bounds)
        {
            var floor = new StaticBody2D { Name = name, CollisionLayer = CombatCollisionLayers.World,
                CollisionMask = CombatCollisionLayers.None };
            var shape = new RectangleShape2D { Size = bounds.Size }; _owned.Add(shape);
            floor.AddChild(new CollisionShape2D { Name = "Collision", Position = bounds.GetCenter(), Shape = shape });
            Root.AddChild(floor); if (name == "LowerFloor") LowerFloor = floor; return floor;
        }
        public T Own<T>(T shape) where T : Shape2D { _owned.Add(shape); return shape; }
        public async Task Mount()
        {
            if (RegisterPlayer) Player.AddToGroup("player");
            _tree.Root.AddChild(Root); await Frames(_tree, 8);
        }
        public async Task DisposeAsync()
        {
            Driver?.Dispose();
            if (GodotObject.IsInstanceValid(Root) && !Root.IsQueuedForDeletion()) Root.QueueFree();
            await Frames(_tree, 2);
            foreach (Shape2D shape in _owned) shape.Dispose();
            _owned.Clear();
        }
    }

    private partial class SpringboardPhysicsProbe : Node
    {
        public Action Observe;
        public override void _PhysicsProcess(double delta) => Observe?.Invoke();
    }

    private static class MechanismFixture
    {
        public static CanyonCounterweightLift Lift()
        {
            var node = new CanyonCounterweightLift { Name = "Lift", LowerPosition = Vector2.Zero,
                UpperPosition = new Vector2(0, -120), TravelSeconds = .5f, EndpointDwellSeconds = .35f };
            AddMovingBody(node, new Vector2(120, 20)); return node;
        }

        public static CanyonGearPiston Piston()
        {
            var node = new CanyonGearPiston { Name = "Piston", SafeSeconds = 1f,
                TelegraphSeconds = .5f, ActiveSeconds = .5f, RecoverySeconds = .5f };
            AddMovingBody(node, new Vector2(80, 28)); AddTelegraph(node); return node;
        }

        public static CanyonSteamJet Jet()
        {
            var node = new CanyonSteamJet { Name = "SteamJet", SafeSeconds = 1f,
                TelegraphSeconds = .4f, ActiveSeconds = .6f };
            node.AddChild(new CollisionShape2D { Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(40, 120) } });
            AddTelegraph(node); return node;
        }

        public static CanyonSteamValve Valve() => new() { Name = "Valve",
            ActivationId = "canyon.v2.steam-valve-lower" };

        public static CanyonPendulumHazard Pendulum()
        {
            var node = new CanyonPendulumHazard { Name = "Pendulum", PeriodSeconds = 2f,
                MaxAngleDegrees = 55f };
            AddMovingBody(node, new Vector2(24, 150)); AddTelegraph(node); return node;
        }

        public static CanyonSealGate Seal()
        {
            var node = new CanyonSealGate { Name = "Seal",
                RequiredMechanismId = "canyon.v2.seal-crank",
                RequiredEncounterId = "canyon.v2.encounter.split-level" };
            node.AddChild(new CollisionShape2D { Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(36, 220) } });
            node.AddChild(new Polygon2D { Name = "ClosedVisual" });
            node.AddChild(new Polygon2D { Name = "OpenVisual" });
            node.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
                Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
            return node;
        }

        public static CanyonShortcutWinch Winch()
        {
            var node = new CanyonShortcutWinch { Name = "Winch",
                ShortcutId = "canyon.v2.upper-basin-shortcut", CorridorBounds = new Rect2(-60, -160, 120, 320) };
            var barrier = new StaticBody2D { Name = "Barrier", CollisionLayer = CombatCollisionLayers.World };
            barrier.AddChild(new CollisionShape2D { Name = "Collision",
                Shape = new RectangleShape2D { Size = new Vector2(36, 220) } });
            barrier.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
                Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
            node.AddChild(barrier); node.AddChild(new Polygon2D { Name = "ClosedVisual" });
            node.AddChild(new Polygon2D { Name = "OpenVisual" }); return node;
        }

        private static void AddMovingBody(Node node, Vector2 size)
        {
            node.AddChild(new CollisionShape2D { Name = "Collision",
                Shape = new RectangleShape2D { Size = size } });
            node.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
                Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        }

        private static void AddTelegraph(Node node) =>
            node.AddChild(new Polygon2D { Name = "Telegraph" });
    }
}
