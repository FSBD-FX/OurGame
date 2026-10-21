using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Focused saved-scene proof, not a human or title-to-exit certificate.
public static class CanyonUpdraftRuntimeTests
{
    public static Task RunPresentation(SceneTree tree) => SavedDescent(tree,presentation:true);

    internal static async Task CaptureWorldProbe(SceneTree tree,CharacterBody2D player,string stage)
    {
        if(DisplayServer.GetName()=="headless") return;
        await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        string folder=System.IO.Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(),"updraft-world-proof");
        System.IO.Directory.CreateDirectory(folder);
        string path=System.IO.Path.Combine(folder,stage+".png");
        TestAssert.True(!System.IO.File.Exists(path),"normal-world updraft proof never overwrites previous media");
        using Image frame=tree.Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok,frame.SavePng(path),"save normal game viewport, including actual fog/HUD");
        GD.Print($"ABYSS_UPDRAFT_NORMAL_VIEWPORT stage={stage} player={player.GlobalPosition} path={path}");
    }
    public static async Task Run(SceneTree tree)
    {
        ModelControls();
        await LocalControls(tree);
        await JumpAndDashOwnership(tree);
        await ClawAndReleaseOwnership(tree);
        await OrdinaryFloorKeepsHeightDamage(tree);
        await SavedDescent(tree);
    }

    private static async Task JumpAndDashOwnership(SceneTree tree)
    {
        var root=new Node2D { Name="UpdraftJumpDashFixture",Position=new(400,300) };
        root.AddChild(new CanyonUpdraft { Bounds=new(-500,-500,1600,1200),DescentSpeed=100 });
        var floor=new StaticBody2D { Position=new(0,60),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new(1000,20) } });
        root.AddChild(floor);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=new(0,14); player.GetNode<Camera2D>("Camera2D").Enabled=false;
        root.AddChild(player); tree.Root.AddChild(root);
        var input=player.GetNode<PlayerInputReader>("Input"); var motor=player.GetNode<PlayerMotor>("Motor");
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            TestAssert.True(FloorIs(player,floor),"in-wind jump starts on native floor");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            TestAssert.True(motor.AcceptedJumpCount==1 && player.Velocity.Y<0 && motor.UpdraftVelocityDelta==Vector2.Zero,
                "real ordinary jump owns upward motion inside an eligible wind");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            KeyboardActionRuntimeTests.KeyEvent(Key.S,true);
            KeyboardActionRuntimeTests.KeyEvent(Key.Shift,true);
            bool sawDownDash=false;
            for(int tick=0; tick<12; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                if(motor.IsDashing && player.Velocity.Y>500)
                {
                    sawDownDash=true;
                    TestAssert.Near(-50f, motor.UpdraftVelocityDelta.Y, .004f,
                        "actual downward short jet receives one full tick of 3000/60 wind braking");
                    TestAssert.Near(motor.UpdraftCandidateVelocity.Y - 50f, player.Velocity.Y, .004f,
                        "free short-jet velocity includes wind instead of retaining legacy dash immunity");
                }
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.Shift,false); KeyboardActionRuntimeTests.KeyEvent(Key.S,false);
            TestAssert.True(sawDownDash && motor.AcceptedDashCount==1,"counterexample actually accepts a high-speed downward dash");
            for(int tick=0; tick<100 && !FloorIs(player,floor); tick++) await KeyboardActionRuntimeTests.Frames(tree,input,1);
            await KeyboardActionRuntimeTests.Frames(tree,input,5);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            bool sawOrdinaryBraking=false;
            for(int tick=0; tick<100; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                if(player.Velocity.Y<0) TestAssert.Equal(Vector2.Zero,motor.UpdraftVelocityDelta,"wind leaves subsequent upward arc unchanged");
                sawOrdinaryBraking |= motor.UpdraftVelocityDelta.Y<0 && !motor.IsDashing;
                if(sawOrdinaryBraking) break;
            }
            TestAssert.True(motor.AcceptedJumpCount==2 && sawOrdinaryBraking,"ordinary falling regains local wind after dash lifecycle ends");
            GD.Print("ABYSS_UPDRAFT_JUMP_DASH_OK real_jumps=2 real_down_dash=1 brake_resumed=true");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false); KeyboardActionRuntimeTests.KeyEvent(Key.Shift,false);
            KeyboardActionRuntimeTests.KeyEvent(Key.S,false); await Free(tree,root);
        }
    }

    private static async Task ClawAndReleaseOwnership(SceneTree tree)
    {
        var root=new Node2D { Name="UpdraftActualClawFixture",Position=new(400,250) };
        var wind=new CanyonUpdraft { Bounds=new(-500,-1500,1800,3000),DescentSpeed=100,
            ProcessMode=Node.ProcessModeEnum.Disabled };
        root.AddChild(wind);
        var anchor=new GrappleSurface2D { Position=new(220,220),Material=GrappleSurfaceMaterial.DedicatedAnchor };
        var area=new Area2D { CollisionLayer=CombatCollisionLayers.ThreadTarget };
        area.AddChild(new CollisionShape2D { Shape=new CircleShape2D { Radius=12 } });
        anchor.AddChild(area); root.AddChild(anchor);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=new(0,14); player.GetNode<Camera2D>("Camera2D").Enabled=false;
        root.AddChild(player); tree.Root.AddChild(root);
        var input=player.GetNode<PlayerInputReader>("Input"); var motor=player.GetNode<PlayerMotor>("Motor");
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        int boosts=0,releases=0;
        void Observe(MechanicalClawFrame frame,GrappleSurface2D _)
        {
            if(frame.State==MechanicalClawState.Boost) boosts++;
            if(frame.State==MechanicalClawState.Released && frame.ReleaseKind==MechanicalClawReleaseKind.Boost) releases++;
        }
        claw.ClawFrameChanged+=Observe;
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree,input,2);
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true); KeyboardActionRuntimeTests.KeyEvent(Key.S,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,2);
            TestAssert.True(claw.KeyboardPreview.Surface==anchor,"actual diagonal keyboard preview resolves the below-right anchor");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            bool sawDownAttached=false;
            for(int tick=0; tick<90; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                if(claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                {
                    TestAssert.Equal(Vector2.Zero,motor.UpdraftVelocityDelta,"real attached claw owns motion inside wind");
                    if(player.Velocity.Y>100 && motor.UpdraftCandidateVelocity.Y>100)
                    { sawDownAttached=true; break; }
                }
            }
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false); KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
            KeyboardActionRuntimeTests.KeyEvent(Key.S,false);
            TestAssert.True(sawDownAttached && claw.CurrentFrame.LockedTargetRuntimeId==anchor.GetInstanceId(),
                $"actual attached descent must exceed wind limit, not pass on negativeY; state={claw.CurrentFrame.State} velocity={player.Velocity}");
            wind.ProcessMode=Node.ProcessModeEnum.Inherit;
            for(int tick=0; tick<3; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                    "actual claw still owns each eligible-wind counterexample sample");
                TestAssert.True(CanyonUpdraft.ApplyAtPlayer(player,motor.UpdraftCandidateVelocity,1d/60).Y < motor.UpdraftCandidateVelocity.Y,
                    $"wind would brake the exact free candidate without attachment, not merely servo body speed; candidate={motor.UpdraftCandidateVelocity}");
                TestAssert.Equal(Vector2.Zero,motor.UpdraftVelocityDelta,"attachment excludes otherwise-effective wind on exact free velocity");
            }
            int jumps=motor.AcceptedJumpCount;
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,2);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            TestAssert.True(boosts==1 && releases==1 && motor.AcceptedJumpCount==jumps && motor.HasGrappleReleaseMomentum,
                "actual attached Space boosts/releases once without an ordinary airborne jump");
            int fallingReleaseSamples=0;
            for(int tick=0; tick<120; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                TestAssert.True(motor.HasGrappleReleaseMomentum && motor.UpdraftVelocityDelta==Vector2.Zero,
                    "wind never steals actual preserved release momentum");
                if(player.Velocity.Y>500)
                {
                    TestAssert.True(CanyonUpdraft.ApplyAtPlayer(player,motor.UpdraftCandidateVelocity,1d/60).Y < motor.UpdraftCandidateVelocity.Y,
                        "release sample is inside otherwise-effective wind");
                    if(++fallingReleaseSamples==3) break;
                }
                else fallingReleaseSamples=0;
            }
            TestAssert.True(fallingReleaseSamples==3,"release exclusion has3 consecutive actual >500 downward samples inside wind");
            motor.ResetTransientState(); // Explicit reset-boundary control, not a natural-route action.
            bool resumed=false;
            for(int tick=0; tick<20; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                resumed |= motor.UpdraftVelocityDelta.Y<0;
                if(resumed) break;
            }
            TestAssert.True(resumed && !motor.HasGrappleReleaseMomentum,"reset inside wind clears release ownership and resumes ordinary braking");
            GD.Print($"ABYSS_UPDRAFT_CLAW_OK actual_down_attach=true boost={boosts} release={releases} falling_release=true reset_brake=true");
        }
        finally
        {
            claw.ClawFrameChanged-=Observe;
            foreach(Key key in new[]{Key.D,Key.S,Key.K,Key.Space}) KeyboardActionRuntimeTests.KeyEvent(key,false);
            await Free(tree,root);
        }
    }

    private static void ModelControls()
    {
        TestAssert.Equal(new Vector2(260, 730), CanyonUpdraft.LimitDescent(new(260,780),1f/60,500,3000),
            "wind brakes smoothly by50 per tick, preserving horizontal velocity");
        TestAssert.Equal(new Vector2(-260,-500), CanyonUpdraft.LimitDescent(new(-260,-500),1,500,3000),
            "wind never changes an upward jump");
        TestAssert.Equal(new Vector2(17,300), CanyonUpdraft.LimitDescent(new(17,300),1,500,3000),
            "wind never accelerates a slow fall toward its terminal limit");
        TestAssert.Equal(new Vector2(17,500), CanyonUpdraft.LimitDescent(new(17,780),1,500,3000),
            "wind does not reverse falling velocity");
        TestAssert.Equal(new Vector2(17,780), CanyonUpdraft.LimitDescent(new(17,780),0,500,3000),
            "zero time does not change velocity");
    }

    private static async Task LocalControls(SceneTree tree)
    {
        var root = new Node2D { Name = "UpdraftEligibilityFixture" };
        var wind = new CanyonUpdraft { Bounds = new Rect2(0,0,640,1000) };
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new(320,100); // Synthetic setup, before the tree, not route motion.
        root.AddChild(wind); root.AddChild(player); tree.Root.AddChild(root);
        try
        {
            var motor = player.GetNode<PlayerMotor>("Motor");
            Vector2 source = new(123,780);
            TestAssert.Equal(new Vector2(123,730), CanyonUpdraft.ApplyAtPlayer(player,source,1d/60),
                "live same-world field affects an actor inside its bounds");
            motor.ResetTransientState();
            TestAssert.Equal(new Vector2(123,730), CanyonUpdraft.ApplyAtPlayer(player,source,1d/60),
                "reset inside a field does not lose eligibility");
            wind.Position = new(700,0);
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60),
                "leaving the field has no persistent registration or force");
            wind.Position = Vector2.Zero;
            root.SetMeta("_seamless_staged", true);
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "staged wind is inert");
            root.RemoveMeta("_seamless_staged");
            wind.ProcessMode = Node.ProcessModeEnum.Disabled;
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "disabled wind is inert");
            wind.ProcessMode = Node.ProcessModeEnum.Inherit;
            tree.Paused = true;
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "paused field cannot apply force");
            tree.Paused = false;
            wind.QueueFree();
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "queued wind is inert immediately");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "freed wind cannot leave a stale effect");
            var viewport = new SubViewport { World2D = new World2D() };
            var foreign = new CanyonUpdraft { Bounds = new(0,0,640,1000) };
            viewport.AddChild(foreign); root.AddChild(viewport);
            TestAssert.True(foreign.GetWorld2D() != player.GetWorld2D(), "counterexample has a distinct native World2D");
            TestAssert.Equal(source, CanyonUpdraft.ApplyAtPlayer(player,source,1d/60), "different-world wind is inert");
            GD.Print("ABYSS_UPDRAFT_SCOPE_OK reset exit staged disabled paused queued freed different_world=true");
        }
        finally { tree.Paused = false; await Free(tree,root); }
    }

    private static async Task OrdinaryFloorKeepsHeightDamage(SceneTree tree)
    {
        var root = new Node2D { Name = "UpdraftOrdinaryLandingFixture" };
        root.AddChild(new CanyonUpdraft { Bounds = new(0,0,640,1000) });
        var floor = new StaticBody2D { Position = new(320,920), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new(640,40) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new(320,100); root.AddChild(player); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        try
        {
            bool sawBraking = false;
            for(int tick=0; tick<240 && !player.IsOnFloor(); tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                sawBraking |= motor.UpdraftVelocityDelta.Y < 0;
                if(tick == 30)
                {
                    Vector2 position = player.GlobalPosition;
                    tree.Paused = true;
                    for(int frame=0; frame<5; frame++) await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                    TestAssert.Equal(position,player.GlobalPosition,"pause freezes actual descent");
                    tree.Paused = false;
                }
            }
            TestAssert.True(player.IsOnFloor() && sawBraking,"ordinary landing actually traversed active updraft");
            TestAssert.True(motor.LastFallHeight > 760 && motor.LastFallDamage == 1 && health.CurrentHealth == 4,
                $"wind never clears height damage: height={motor.LastFallHeight} damage={motor.LastFallDamage} hp={health.CurrentHealth}");
            await KeyboardActionRuntimeTests.Frames(tree,input,10);
            TestAssert.True(health.CurrentHealth == 4 && motor.UpdraftVelocityDelta == Vector2.Zero,
                "standing in wind causes neither repeated injury nor force");
            TestAssert.True(motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0,
                "wind never manufactures a jump or dash");
            GD.Print($"ABYSS_UPDRAFT_DAMAGE_OK height={motor.LastFallHeight} damage={motor.LastFallDamage} hp={health.CurrentHealth}");
        }
        finally { tree.Paused = false; await Free(tree,root); }
    }

    private static async Task SavedDescent(SceneTree tree,bool presentation=false)
    {
        if(presentation)
            TestAssert.True(DisplayServer.GetName()!="headless" && RenderingServer.GetCurrentRenderingMethod()=="mobile",
                "presentation requires a real display and the admitted Mobile renderer");
        var root = new Node2D { Name = "SavedUpdraftDescentFixture" };
        var sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn")
            .Instantiate<PrologueSector>();
        sector.Position = new(1280,0);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new(2140,1260); // Actual commitment, pre-tree synthetic setup only.
        var camera=player.GetNode<Camera2D>("Camera2D");
        camera.LimitLeft=1280; camera.LimitRight=2560; camera.LimitTop=0; camera.LimitBottom=3600;
        camera.PositionSmoothingEnabled=false;
        root.AddChild(sector); root.AddChild(player); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        async Task Capture(string name)
        {
            if(!presentation) return;
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            string folder=System.IO.Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(),"updraft-proof");
            System.IO.Directory.CreateDirectory(folder);
            string path=System.IO.Path.Combine(folder,name+".png");
            TestAssert.True(!System.IO.File.Exists(path),"updraft proof never overwrites prior media");
            using Image image=tree.Root.GetTexture().GetImage();
            TestAssert.Equal(Error.Ok,image.SavePng(path),"save actual viewport evidence");
            GD.Print($"ABYSS_UPDRAFT_VIEWPORT name={name} player={player.GlobalPosition} renderer={RenderingServer.GetCurrentRenderingMethod()} gpu={RenderingServer.GetVideoAdapterName()} path={path}");
        }
        try
        {
            var wind = sector.GetNodeOrNull<CanyonUpdraft>("Gameplay/Mechanisms/AbyssUpdraft");
            TestAssert.True(wind != null,
                "ABYSS_UPDRAFT_PRESENT_RED: saved well has the explicitly chosen environmental updraft");
            TestAssert.Equal(new Rect2(1956,1360,534,2120),new Rect2(wind.GlobalPosition+wind.Bounds.Position,wind.Bounds.Size),
                "force and visual bounds match the approved well-only rectangle");
            TestAssert.True(wind.GetNodeOrNull<Label>("EntryCue")?.Text.Contains("上升气流") == true,
                "saved entry has a fixed explicit pre-fall cue");
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            var commitment = sector.GetNode<StaticBody2D>("Gameplay/Collision/AbyssCommitment");
            var bottom = sector.GetNode<StaticBody2D>("Gameplay/Collision/AbyssSafeBottom");
            TestAssert.True(FloorIs(player,commitment),"saved actor starts on actual commitment support");
            await Capture("01-entry-cue");
            ulong actor = player.GetInstanceId();
            var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
            ulong castId=claw.CurrentFrame.NextCastId;
            int onset=-1,landed=-1,windFrames=0;
            ulong onsetPhysics=0,landedPhysics=0;
            bool capturedMid=false, brakingEntry=false;
            int brakeFrames=0;
            KeyboardActionRuntimeTests.KeyEvent(Key.D,true);
            for(int tick=0; tick<420; tick++)
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
                if(!player.IsOnFloor() && onset<0)
                {
                    onset=tick; onsetPhysics=Engine.GetPhysicsFrames();
                    KeyboardActionRuntimeTests.KeyEvent(Key.D,false);
                    KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
                    brakingEntry=true;
                }
                else if(brakingEntry)
                {
                    brakeFrames++;
                    if(player.Velocity.X<=0f)
                    { KeyboardActionRuntimeTests.KeyEvent(Key.A,false); brakingEntry=false; }
                }
                if(motor.UpdraftVelocityDelta.Y<0) windFrames++;
                if(!capturedMid && player.GlobalPosition.Y>2060)
                { capturedMid=true; await Capture("02-updraft-descent"); }
                TestAssert.True(player.GetParent()==root && player.GetInstanceId()==actor &&
                    player.GetNode<HealthComponent>("Health").CurrentHealth>0 &&
                    claw.CurrentFrame.NextCastId==castId && claw.CurrentFrame.VelocityCorrection==Vector2.Zero,
                    "saved descent preserves living actor ownership and never casts/borrows claw force");
                if(onset>=0 && FloorIs(player,bottom))
                { landed=tick; landedPhysics=Engine.GetPhysicsFrames(); break; }
            }
            double seconds=landedPhysics>onsetPhysics ? (landedPhysics-onsetPhysics)/(double)Engine.PhysicsTicksPerSecond : -1;
            TestAssert.True(onset>=0 && landed>onset && seconds>=4 && seconds<6 && windFrames>100,
                $"actual entry-braked saved descent retains4–6seconds: onset={onset} landed={landed} seconds={seconds:0.000} wind={windFrames} brakes={brakeFrames} position={player.GlobalPosition}");
            TestAssert.True(actor==player.GetInstanceId() && motor.AcceptedJumpCount==0 && motor.AcceptedDashCount==0 &&
                player.GetNode<HealthComponent>("Health").CurrentHealth==5,
                "same actor, no jumps/dashes or injury on existing explicit soft bottom");
            TestAssert.True(brakeFrames>0 && brakeFrames<12 && !brakingEntry,
                "real reverse entry braking stops the inherited horizontal drift within .2s, without jet or teleport");
            GD.Print($"ABYSS_UPDRAFT_SAVED_OK seconds={seconds:0.000} physics={onsetPhysics}->{landedPhysics} wind_samples={windFrames} entry_brake_frames={brakeFrames} actor={actor} hp=5 position={player.GlobalPosition}");
            await Capture("03-supported-bottom");
        }
        finally { KeyboardActionRuntimeTests.KeyEvent(Key.D,false); KeyboardActionRuntimeTests.KeyEvent(Key.A,false); await Free(tree,root); }
    }

    private static bool FloorIs(CharacterBody2D player,StaticBody2D body)
    {
        for(int i=0; i<player.GetSlideCollisionCount(); i++)
            if(player.GetSlideCollision(i).GetNormal().Y<-.7f && player.GetSlideCollision(i).GetCollider()==body) return true;
        return false;
    }

    private static async Task Free(SceneTree tree,Node root)
    {
        root.QueueFree();
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
    }
}
