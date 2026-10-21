using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class KeyboardClawRuntimeTests
{
    public static async Task Run(SceneTree tree, bool presentation = false)
    {
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
