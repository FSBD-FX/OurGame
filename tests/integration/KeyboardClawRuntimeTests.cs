using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Moth;
using Knight.Player;
using Knight.Shared;
using Knight.Tests.Presentation;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class KeyboardClawRuntimeTests
{
    public static async Task Run(SceneTree tree, bool presentation = false)
    {
        RepeatRustObservationControls();
        var root = new Node2D { Name = "KeyboardClawRuntime" };
        var floor = new StaticBody2D { Position = new Vector2(500,446), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1000,20) } });
        if (presentation) floor.AddChild(new Polygon2D { Color = new Color("3b5963"),
            Polygon = [new(-500,-10),new(500,-10),new(500,10),new(-500,10)] });
        root.AddChild(floor);
        var target = new GrappleSurface2D { Name = "UpperAnchor", Position = new Vector2(500,180) };
        if (presentation)
        {
            var marker = new Node2D();
            marker.Draw += () => { marker.DrawCircle(Vector2.Zero,10,new Color("72d9d1"),false,2);
                marker.DrawLine(new(-16,0),new(16,0),new Color("72d9d1"),2); };
            target.AddChild(marker);
        }
        root.AddChild(target);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(500,400); player.Name = "Player"; root.AddChild(player);
        var canvas = new CanvasLayer { Name = "TestCanvas" }; root.AddChild(canvas);
        var formal = new ObservedKeyboardClawHud { Name = "Hud", InputPath = "../../Player/Input",
            MechanicalClawPath = "../../Player/MechanicalClaw", Size = new Vector2(1280,720) };
        var reticle = new PrologueAimReticle { Name = "AimReticle" }; formal.AddChild(reticle); canvas.AddChild(formal);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var motor = player.GetNode<PlayerMotor>("Motor");
        int boosts=0, attacks=0;
        player.GetNode<PlayerCombat>("Combat").AttackStarted += (_,_) => attacks++;
        bool turnDuringAim = true;
        bool castPreviewDropped = false;
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (frame.State == MechanicalClawState.Boost) boosts++;
            if (frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup &&
                claw.KeyboardPreview.Surface != target) castPreviewDropped = true;
            if (turnDuringAim && frame.State == MechanicalClawState.Aim)
            {
                turnDuringAim = false;
                KeyboardActionRuntimeTests.KeyEvent(Key.W,false);
                KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            }
        };
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = new Vector2(5000,5000) });
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree,input,3);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await formal.WaitForConsumedInput(input.CurrentFrameSequence);
            GD.Print($"KEYBOARD_PREVIEW_DIAGNOSTIC input={input.CurrentFrameSequence} keyboard={input.Current.KeyboardActions} " +
                $"aim={input.Current.Aim} preview={claw.KeyboardPreview.Surface?.GetInstanceId() ?? 0UL} " +
                $"exists={claw.KeyboardPreview.Snapshot.Exists} available={claw.KeyboardPreview.Surface?.IsAvailable} " +
                $"player={player.GlobalPosition} velocity={player.Velocity} floor={player.IsOnFloor()} " +
                $"hudProcesses={formal.CompletedProcessCount} consumed={formal.LastConsumedInputSequence} " +
                $"consumedPreview={formal.LastConsumedPreviewId} visible={reticle.Visible} " +
                $"frameVisible={reticle.CurrentFrame.Visible} state={reticle.CurrentFrame.State}");
            TestAssert.Equal(target,claw.KeyboardPreview.Surface,"W selects upper anchor despite remote mouse");
            TestAssert.True(reticle.Visible && reticle.CurrentFrame.State == PrologueAimReticleState.Grippable,
                "visible keyboard preview is grippable without E mode or pointer aiming");
            Vector2 expectedScreen = tree.Root.GetCanvasTransform() * target.GlobalPosition;
            TestAssert.True(reticle.CurrentFrame.ScreenPosition.DistanceTo(expectedScreen) < 1f,
                "preview marker shows the authoritative target rather than the mouse cursor");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree,input,60);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            for (int tick=0; tick<30 && claw.CurrentFrame.LockedTargetRuntimeId == 0; tick++)
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
            TestAssert.Equal(target.GetInstanceId(),claw.CurrentFrame.LockedTargetRuntimeId,
                "K locks the shown anchor even after turning during aim/windup");
            TestAssert.True(!turnDuringAim,"test actually turns after cast begins");
            TestAssert.True(!castPreviewDropped,"authoritative preview remains continuous through aim and windup");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false); KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            for (int tick=0; tick<60 && claw.CurrentFrame.State is not (MechanicalClawState.Latched or
                MechanicalClawState.Reel or MechanicalClawState.Swing); tick++)
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
            TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing,"real K attaches and auto-pulls without W hold");
            if (presentation) await KeyboardActionRuntimeTests.Frames(tree,input,35);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,true); KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            KeyboardActionRuntimeTests.KeyEvent(Key.J,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.Equal(1,boosts,"attached Space+J+K accepts exactly one boost");
            TestAssert.Equal(0,attacks,"attached boost consumes competing J");
            TestAssert.Equal(0,motor.AcceptedJumpCount,"attached Space is not an ordinary extra jump");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false); KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            await KeyboardActionRuntimeTests.Frames(tree,input,130);
            TestAssert.True(player.IsOnFloor(),"boost release returns to real floor");
            TestAssert.True(!motor.HasGrappleReleaseMomentum,
                "real floor ends the old grapple flight environment permission");
            TestAssert.Equal(1,boosts,"held/released Space never duplicates boost");
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);
            await Attach(tree,input,claw);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Released or MechanicalClawState.Retract or
                MechanicalClawState.Idle,"a fresh second K cancels attachment without boost");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            await KeyboardActionRuntimeTests.Frames(tree,input,110);
            TestAssert.True(player.IsOnFloor(),"ordinary K cancellation lands on real support");
            await Attach(tree,input,claw);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.L,true); KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.Equal(1,boosts,"L+attached Space cancels without boosting");
            TestAssert.Equal(0,motor.AcceptedJumpCount,"raw guard prevents an ordinary jump on attached cancellation");
            TestAssert.True(player.GetNode<PlayerShieldController>("Shield").GuardDirection.IsEqualApprox(Vector2.Up),
                "L guard uses keyboard W, not the remote mouse");
            KeyboardActionRuntimeTests.KeyEvent(Key.L,false); KeyboardActionRuntimeTests.KeyEvent(Key.Space,false);
            await KeyboardActionRuntimeTests.Frames(tree,input,110);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true); KeyboardActionRuntimeTests.KeyEvent(Key.Space,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,4);
            TestAssert.Equal(1,motor.AcceptedJumpCount,"ordinary grounded Space combines with K cast");
            TestAssert.Equal(target.GetInstanceId(),claw.CurrentFrame.LockedTargetRuntimeId,
                "jump+K locks the valid upper target");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space,false); KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            await KeyboardActionRuntimeTests.Frames(tree,input,15);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true); await KeyboardActionRuntimeTests.Frames(tree,input,4);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
            await KeyboardActionRuntimeTests.Frames(tree,input,110);
            if (presentation)
            {
                KeyboardActionRuntimeTests.KeyEvent(Key.W,false);
                KeyboardActionRuntimeTests.KeyEvent(Key.J,true);
                await KeyboardActionRuntimeTests.Frames(tree,input,3);
                KeyboardActionRuntimeTests.KeyEvent(Key.J,false);
                await KeyboardActionRuntimeTests.Frames(tree,input,40);
                KeyboardActionRuntimeTests.KeyEvent(Key.L,true);
                await KeyboardActionRuntimeTests.Frames(tree,input,50);
                KeyboardActionRuntimeTests.KeyEvent(Key.L,false);
                await KeyboardActionRuntimeTests.Frames(tree,input,30);
            }
            GD.Print("KEYBOARD_CLAW_RUNTIME_OK");
        }
        finally
        {
            foreach (Key key in new[] { Key.W, Key.A, Key.K, Key.J, Key.Space, Key.L })
                KeyboardActionRuntimeTests.KeyEvent(key,false);
            root.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
        }
        await NearEnemyKeyboardCast(tree);
        await RepeatRustKeyboardCast(tree);
    }

    // Characterizes ordinary production behavior, not a Main-route strategy.
    // No model stepping, state/pose reset, frozen AI or direct receiver calls.
    private static async Task RepeatRustKeyboardCast(SceneTree tree)
    {
        var root = new Node2D { Name = "RepeatRustKeyboardRuntime" };
        var floor = new StaticBody2D { Position = new Vector2(640,410),
            CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1600,20) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(400,364);
        player.AddToGroup("player"); root.AddChild(player);
        var rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
        rust.Position = new Vector2(580,368); root.AddChild(rust);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        var playerHealth = player.GetNode<HealthComponent>("Health");
        var rustHealth = rust.GetNode<HealthComponent>("Health");
        var target = rust.GetNode<ThreadTarget2D>("ThreadTarget");
        var adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
        var driver = KeyboardRouteInputDriver.Begin(player);
        ulong playerId = player.GetInstanceId(), rustId = rust.GetInstanceId(), rootId = root.GetInstanceId();
        ulong targetId = target.GetInstanceId(), adapterId = adapter.GetInstanceId();
        var receipts = new RustKeyboardCastReceipt[2];
        int round = -1, latched = 0, released = 0, sampledPresses = 0, stunnedEntries = 0;
        int applicationsBefore = 0, edgesBefore = 0, damage = 0, healed = 0, deaths = 0, connections = 0;
        ulong castId = 0, pressSequence = 0, lastSequence = 0;
        ulong stunExitPhysics = 0, warningPhysics = 0, warningAttackId = 0;
        bool effect = false, complete = false, fault = false;
        bool enteredAttack = false;
        var experiment = new RustKeyboardExperimentObservation();
        void ObserveClaw(MechanicalClawFrame frame, GrappleSurface2D selected)
        {
            if (round < 0) return;
            experiment.Observe(frame,input.CurrentFrame,input.CurrentFrameSequence);
            if (complete) return;
            if (lastSequence != input.CurrentFrameSequence)
            {
                lastSequence = input.CurrentFrameSequence;
                if (input.CurrentFrame.ClawPressed)
                {
                    sampledPresses++; pressSequence = lastSequence;
                    fault |= !input.CurrentFrame.KeyboardActions || input.CurrentFrame.AttackPressed ||
                        input.CurrentFrame.ShieldHeld || input.CurrentFrame.PrimaryPressed;
                }
            }
            fault |= frame.Failure != MechanicalClawFailure.None;
            if (frame.LockedTargetRuntimeId != 0)
                fault |= frame.LockedTargetRuntimeId != targetId || frame.CastId != castId;
            if (frame.State == MechanicalClawState.Latched)
            {
                latched++;
                fault |= selected != target || frame.CastId != castId || frame.LockedTargetRuntimeId != targetId;
            }
            if (frame.State == MechanicalClawState.Released) released++;
            if (sampledPresses == 0 || frame.State != MechanicalClawState.Idle) return;
            fault |= frame.CastId != 0 || frame.NextCastId != castId + 1 ||
                claw.CurrentTarget != null || claw.ResidencyTarget != null;
            receipts[round] = new RustKeyboardCastReceipt(castId, targetId, pressSequence,
                input.CurrentFrameSequence, Engine.GetPhysicsFrames(), applicationsBefore, adapter.ApplicationCount,
                edgesBefore, driver.ClawEdges, sampledPresses, latched, released, effect, fault);
            complete = true;
            GD.Print($"KEYBOARD_REPEAT_RUST_RECEIPT round={round + 1} player={playerId} rust={rustId} " +
                $"root={rootId} adapter={adapterId} value={receipts[round]}");
        }
        void ObserveRust(RustShellFrame frame, int facing)
        {
            if (frame.State == RustShellState.Stunned && frame.EnteredState)
            {
                stunnedEntries++;
                if (round >= 0 && !complete)
                    effect |= latched == 1 && adapter.ApplicationCount == applicationsBefore + 1;
            }
            if (stunnedEntries == 1 && frame.State != RustShellState.Stunned && stunExitPhysics == 0)
                stunExitPhysics = Engine.GetPhysicsFrames();
            if (stunExitPhysics != 0 && warningPhysics == 0 &&
                frame.State == RustShellState.Telegraph && frame.EnteredState)
            {
                warningPhysics = Engine.GetPhysicsFrames(); warningAttackId = frame.AttackTimeline.AttackId;
            }
            enteredAttack |= frame.AttackActive;
        }
        claw.ClawFrameChanged += ObserveClaw; rust.FrameChanged += ObserveRust;
        playerHealth.Damaged += OnDamage; rustHealth.Damaged += OnDamage;
        playerHealth.Healed += OnHeal; rustHealth.Healed += OnHeal;
        playerHealth.Died += OnDeath; rustHealth.Died += OnDeath;
        rust.GetNode<Hitbox>("AttackHitbox").Connected += OnConnected;
        void OnDamage(int remaining, Vector2 impulse) => damage++;
        void OnHeal(int restored, int remaining) => healed++;
        void OnDeath() => deaths++;
        void OnConnected(Hurtbox hurtbox) => connections++;
        async Task Tick()
        {
            await KeyboardActionRuntimeTests.Frames(tree,input,1);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            driver.ObserveFrame();
            TestAssert.True(player.GetInstanceId() == playerId && rust.GetInstanceId() == rustId &&
                player.GetParent() == root && rust.GetParent() == root && root.GetInstanceId() == rootId &&
                !player.IsQueuedForDeletion() && !rust.IsQueuedForDeletion() &&
                target.GetParent() == rust && target.GetInstanceId() == targetId && adapter.GetInstanceId() == adapterId &&
                rust.IsPhysicsProcessing() && playerHealth.CurrentHealth == 5 && rustHealth.CurrentHealth == 3 &&
                damage == 0 && healed == 0 && deaths == 0 && connections == 0 && !enteredAttack &&
                motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0 &&
                Mathf.IsEqualApprox(energy.EnergyRatio,1f) && energy.HeatRatio == 0f && !experiment.Fault,
                $"REPEAT_RUST_INVARIANT_RED: hp={playerHealth.CurrentHealth}/{rustHealth.CurrentHealth} " +
                $"damage={damage} healed={healed} deaths={deaths} active={enteredAttack} connections={connections}");
        }
        try
        {
            for (int tick = 0; tick < 8; tick++) await Tick();
            TestAssert.True(tree.GetFirstNodeInGroup("player") == player && player.IsOnFloor() && rust.IsOnFloor(),
                "ordinary saved Rust sees the same living saved Player on real support");
            RustKeyboardCastReceipt first = default;
            for (round = 0; round < 2; round++)
            {
                if (round == 1)
                {
                    for (int tick = 0; tick < 180 && warningPhysics == 0; tick++) await Tick();
                    TestAssert.True(stunExitPhysics > first.IdlePhysics && warningPhysics > stunExitPhysics &&
                        warningAttackId > 0 && rust.CurrentState == RustShellState.Telegraph &&
                        rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds > .3f && stunnedEntries == 1,
                        $"REPEAT_RUST_NEW_WARNING_RED: exit={stunExitPhysics} idle={first.IdlePhysics} " +
                        $"warning={warningPhysics} state={rust.CurrentState} stuns={stunnedEntries}");
                    TestAssert.Equal(first,receipts[0],"first immutable receipt survives natural stun exit and new warning");
                }
                complete = false; effect = false; fault = false;
                latched = released = sampledPresses = 0; pressSequence = 0;
                applicationsBefore = adapter.ApplicationCount; edgesBefore = driver.ClawEdges;
                castId = claw.CurrentFrame.NextCastId; lastSequence = input.CurrentFrameSequence;
                var preview = claw.KeyboardPreview;
                TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle &&
                    input.KeyboardAimDirection.Dot(Vector2.Right) > .99f && preview.Surface == target &&
                    preview.Snapshot.Exists && preview.Snapshot.HasLineOfSight && preview.Snapshot.IsLegacyInteraction,
                    $"REPEAT_RUST_PREVIEW_RED: round={round + 1} state={claw.CurrentFrame.State} " +
                    $"preview={preview.Surface?.GetInstanceId() ?? 0} player={player.GlobalPosition} rust={rust.GlobalPosition}");
                GD.Print($"KEYBOARD_REPEAT_RUST_BEGIN round={round + 1} cast={castId} apps={applicationsBefore} " +
                    $"physics={Engine.GetPhysicsFrames()} sequence={input.CurrentFrameSequence} " +
                    $"warning={warningPhysics} remaining={rust.CurrentFrame.AttackTimeline.PhaseRemainingSeconds}");
                driver.Command(InputActions.Claw,true);
                await Tick(); driver.Command(InputActions.Claw,false);
                for (int tick = 0; tick < 90 && !complete; tick++) await Tick();
                var receipt = receipts[round];
                TestAssert.True(complete && !receipt.Fault && receipt.TargetId == targetId &&
                    receipt.PressSequence > 0 && receipt.IdleSequence > receipt.PressSequence &&
                    receipt.ApplicationsBefore == round && receipt.ApplicationsAfter == round + 1 &&
                    receipt.EdgesBefore == round && receipt.EdgesAfter == round + 1 && receipt.PressSamples == 1 &&
                    receipt.Latched == 1 && receipt.Released == 1 && receipt.Effect && stunnedEntries == round + 1,
                    $"REPEAT_RUST_COMPLETE_RED: round={round + 1} complete={complete} receipt={receipt} stuns={stunnedEntries}");
                if (round == 0) first = receipt;
                else TestAssert.True(receipts[0] == first && receipt.CastId == first.CastId + 1 &&
                    receipt.PressSequence > first.IdleSequence && receipt.IdlePhysics > warningPhysics,
                    "second distinct cast preserves first receipt and completes after its new warning");
            }
            TestAssert.True(!experiment.Fault && experiment.ClawPresses == 2,
                "continuous experiment observation retains all failures and exactly two keyboard samples");
            GD.Print("KEYBOARD_REPEAT_RUST_OK casts=2 applications=2 stuns=2 idle_handoffs=2 " +
                "same_saved_actors=true normal_ai=true new_warning=true hp=5/3 energy_full=true " +
                "damage=0 healed=0 jumps=0 dashes=0 main_route=false human=false");
        }
        finally
        {
            driver.Dispose();
            claw.ClawFrameChanged -= ObserveClaw; rust.FrameChanged -= ObserveRust;
            playerHealth.Damaged -= OnDamage; rustHealth.Damaged -= OnDamage;
            playerHealth.Healed -= OnHeal; rustHealth.Healed -= OnHeal;
            playerHealth.Died -= OnDeath; rustHealth.Died -= OnDeath;
            rust.GetNode<Hitbox>("AttackHitbox").Connected -= OnConnected;
            root.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
        }
    }

    private readonly record struct RustKeyboardCastReceipt(ulong CastId, ulong TargetId, ulong PressSequence,
        ulong IdleSequence, ulong IdlePhysics, int ApplicationsBefore, int ApplicationsAfter,
        int EdgesBefore, int EdgesAfter, int PressSamples, int Latched, int Released, bool Effect, bool Fault);

    private static void RepeatRustObservationControls()
    {
        var neutral = default(PlayerInputFrame) with { KeyboardActions = true };
        var pressed = neutral with { ClawPressed = true };
        var idle = MechanicalClawFrame.Idle();
        RustKeyboardExperimentObservation Stream(bool failed, bool forbidden)
        {
            var observer = new RustKeyboardExperimentObservation();
            observer.Observe(idle with { State = MechanicalClawState.Aim, CastId = 1 },pressed,1);
            observer.Observe(idle,neutral,2); // first full receipt collected
            observer.Observe(idle with { Failure = failed ? MechanicalClawFailure.TargetLost : MechanicalClawFailure.None },
                neutral with { AttackPressed = forbidden },3); // no K edge during handoff
            observer.Observe(idle with { State = MechanicalClawState.Aim, CastId = 2 },pressed,4);
            observer.Observe(idle,neutral,5); // otherwise-valid second cast
            return observer;
        }
        var failure = Stream(true,false);
        var forbidden = Stream(false,true);
        var clean = Stream(false,false);
        GD.Print($"KEYBOARD_REPEAT_RUST_OBSERVER_CONTROL failure_retained={failure.Fault} " +
            $"non_claw_forbidden_retained={forbidden.Fault} clean={!clean.Fault} clean_presses={clean.ClawPresses}");
        TestAssert.True(failure.Fault && forbidden.Fault && !clean.Fault && clean.ClawPresses == 2,
            "REPEAT_RUST_OBSERVER_GAP_RED: first Idle then failure/forbidden input cannot be erased by a clean second cast");
    }

    // This is the same observer used by the actual native fixture above. The
    // controlled value streams exercise fault retention, not fake native input.
    private sealed class RustKeyboardExperimentObservation
    {
        private ulong _sequence;
        public bool Fault { get; private set; }
        public int ClawPresses { get; private set; }
        public void Observe(MechanicalClawFrame frame, PlayerInputFrame sample, ulong sequence)
        {
            Fault |= frame.Failure != MechanicalClawFailure.None;
            if (_sequence == sequence) return;
            _sequence = sequence;
            Fault |= !sample.KeyboardActions || sample.AttackPressed || sample.ShieldHeld ||
                sample.PrimaryPressed || sample.PrimaryHeld || sample.PrimaryReleased || sample.LegacyShieldHeld ||
                sample.KeyboardShieldHeld || sample.JumpPressed || sample.JumpHeld || sample.DashPressed ||
                sample.InteractPressed || sample.AttackModePressed || sample.GrappleModePressed ||
                sample.RequestedSlot != 0 || sample.ThreadPressed || sample.ThreadHeld || sample.ThreadReleased;
            if (sample.ClawPressed) ClawPresses++;
        }
    }

    private static async Task NearEnemyKeyboardCast(SceneTree tree)
    {
        var root = new Node2D { Name = "NearEnemyKeyboardRuntime" };
        var floor = new StaticBody2D { Position = new Vector2(500,446), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1000,20) } });
        root.AddChild(floor);
        var wall = new StaticBody2D { Name = "FarAxisTerrain", Position = new Vector2(500,100),
            CollisionLayer = CombatCollisionLayers.World };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(200,20) } });
        wall.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile", UseCollisionHitPoint = true,
            Material = GrappleSurfaceMaterial.TempleMetal });
        root.AddChild(wall);
        var moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate<GraveLampMoth>();
        moth.Name = "NearEnemy"; moth.Position = new Vector2(540,200); root.AddChild(moth);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(500,400); root.AddChild(player);
        var canvas = new CanvasLayer(); root.AddChild(canvas);
        var hud = new ObservedKeyboardClawHud { Name = "Hud", InputPath = "../../Player/Input",
            MechanicalClawPath = "../../Player/MechanicalClaw", Size = new Vector2(1280,720) };
        var reticle = new PrologueAimReticle { Name = "AimReticle" }; hud.AddChild(reticle); canvas.AddChild(hud);
        tree.Root.AddChild(root);
        // Selection and dispatch use the real saved receiver/colliders. Only AI
        // motion is held still so the intended keyboard geometry stays fixed.
        moth.SetPhysicsProcess(false);
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var target = moth.GetNode<ThreadTarget2D>("ThreadTarget");
        var adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
        ulong previewId = 0, lockedId = 0;
        int latched = 0, released = 0;
        bool turned = false, previewChanged = false;
        void Observe(MechanicalClawFrame frame, GrappleSurface2D selected)
        {
            if (frame.LockedTargetRuntimeId != 0) lockedId = frame.LockedTargetRuntimeId;
            if (frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup)
                previewChanged |= claw.KeyboardPreview.Surface != target;
            if (frame.State == MechanicalClawState.Latched) latched++;
            if (frame.State == MechanicalClawState.Released) released++;
            if (!turned && frame.State == MechanicalClawState.Aim)
            {
                turned = true;
                KeyboardActionRuntimeTests.KeyEvent(Key.W,false);
                KeyboardActionRuntimeTests.KeyEvent(Key.A,true);
            }
        }
        claw.ClawFrameChanged += Observe;
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree,input,8);
            KeyboardActionRuntimeTests.KeyEvent(Key.W,true);
            await KeyboardActionRuntimeTests.Frames(tree,input,3);
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await hud.WaitForConsumedInput(input.CurrentFrameSequence);
            previewId = hud.LastConsumedPreviewId;
            TestAssert.Equal(target.GetInstanceId(),previewId,"actual HUD consumes the promoted saved enemy");
            TestAssert.True(reticle.Visible && reticle.CurrentFrame.State == PrologueAimReticleState.Grippable,
                "the enemy priority preview is visibly grippable in the real HUD consumer");
            TestAssert.Equal(0,adapter.ApplicationCount,"preview never applies the real enemy interaction");
            KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
            for (int tick=0; tick<90 && adapter.ApplicationCount == 0; tick++)
                await KeyboardActionRuntimeTests.Frames(tree,input,1);
            KeyboardActionRuntimeTests.KeyEvent(Key.K,false); KeyboardActionRuntimeTests.KeyEvent(Key.A,false);
            TestAssert.True(turned && !previewChanged,"turning after K preserves the shown enemy through windup");
            TestAssert.Equal(previewId,lockedId,"actual K locks the same enemy shown by the HUD");
            TestAssert.Equal(1,adapter.ApplicationCount,"actual K dispatches one saved enemy interaction");
            TestAssert.True(moth.Velocity.Dot(moth.GlobalPosition.DirectionTo(player.GlobalPosition)) > 0f,
                "the real Moth receives pull velocity toward the actual Player");
            TestAssert.Equal(1,latched,"accepted enemy pull publishes exactly one Latched edge");
            TestAssert.Equal(1,released,"accepted enemy pull immediately publishes one Released edge");
            TestAssert.True(claw.CurrentTarget == null && claw.CurrentFrame.State == MechanicalClawState.Released,
                "enemy interaction does not retain a movement rope or switch to the far terrain");
            GD.Print("KEYBOARD_NEAR_ENEMY_CAST_OK saved_player=true saved_enemy=true hud_preview=true " +
                "same_locked_target=true adapter_applications=1 turn_preserved=true human=false");
        }
        finally
        {
            claw.ClawFrameChanged -= Observe;
            foreach (Key key in new[] { Key.W, Key.A, Key.K }) KeyboardActionRuntimeTests.KeyEvent(key,false);
            root.QueueFree(); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task Attach(SceneTree tree,PlayerInputReader input,MechanicalClawController claw)
    {
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(Key.K,true);
        await KeyboardActionRuntimeTests.Frames(tree,input,4);
        KeyboardActionRuntimeTests.KeyEvent(Key.K,false);
        for(int tick=0; tick<60 && claw.CurrentFrame.State is not (MechanicalClawState.Latched or
            MechanicalClawState.Reel or MechanicalClawState.Swing); tick++)
            await KeyboardActionRuntimeTests.Frames(tree,input,1);
        TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
            MechanicalClawState.Swing,"real recast attaches before cancellation test");
    }
}

// Observe the real inherited HUD callback, without presenting a frame from the test
// or changing process order. ProcessFrame itself precedes node _Process callbacks.
public partial class ObservedKeyboardClawHud : PrologueFormalHud
{
    public ulong CompletedProcessCount { get; private set; }
    public ulong LastConsumedInputSequence { get; private set; }
    public ulong LastConsumedPreviewId { get; private set; }
    private ulong _awaitedInputSequence;
    private TaskCompletionSource<bool> _completedSample;

    public Task WaitForConsumedInput(ulong sequence)
    {
        if (LastConsumedInputSequence >= sequence) return Task.CompletedTask;
        _awaitedInputSequence = sequence;
        _completedSample = new TaskCompletionSource<bool>();
        return _completedSample.Task;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        LastConsumedInputSequence = GetNode<PlayerInputReader>(InputPath).CurrentFrameSequence;
        LastConsumedPreviewId = GetNode<MechanicalClawController>(MechanicalClawPath)
            .KeyboardPreview.Surface?.GetInstanceId() ?? 0UL;
        CompletedProcessCount++;
        if (LastConsumedInputSequence >= _awaitedInputSequence)
            _completedSample?.TrySetResult(true);
    }
}
