using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Settings;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public partial class PrologueVfxRuntimeTests : SceneTree
{
    public static async Task Task10ReplaceableEffectRootsExist(SceneTree tree)
    {
        PrologueVfxDirector director = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn")
            .Instantiate<PrologueVfxDirector>();
        tree.Root.AddChild(director);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        try
        {
            string[] paths =
            [
                "World/ShieldFan", "World/ShieldBlockFlash", "World/ShieldHeavyBreak",
                "World/ShieldOverheatVent", "World/ClawMaterialHit", "World/RopeTension"
            ];
            foreach (string path in paths)
                TestAssert.True(director.GetNodeOrNull(path) != null,
                    $"Task10 VFX scene exposes replaceable effect root {path}");
        }
        finally
        {
            director.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    public static async Task Task10TransitionsAreEntryOnlyAndClean(SceneTree tree)
    {
        PrologueVfxDirector director = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn")
            .Instantiate<PrologueVfxDirector>();
        tree.Root.AddChild(director);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        MechanicalClawFrame travel = MechanicalClawFrame.Idle() with
        {
            CastId = 610, State = MechanicalClawState.Travel,
            AnchorPoint = new Vector2(320, 180), EffectiveVisibility = true
        };
        director.PresentClawFrame(travel, new Vector2(100, 180));
        director.PresentClawFrame(travel, new Vector2(100, 180));
        TestAssert.Equal(1, director.ActiveTask10EffectCount,
            "Task10 repeated runtime claw frame creates one entry effect and never polling spam");
        TestAssert.Equal(1, director.GetNode<Node2D>("World/ClawMaterialHit").GetChildCount(),
            "Task10 claw travel is routed beneath the replaceable claw-material root");
        director.PresentClawFrame(travel with { State = MechanicalClawState.Reel }, new Vector2(100, 180));
        TestAssert.Equal(1, director.GetNode<Node2D>("World/RopeTension").GetChildCount(),
            "Task10 real Reel entry creates the rope-tension placeholder effect");
        director.PresentClawFrame(travel with
        {
            State = MechanicalClawState.Boost,
            BoostConsumed = true
        }, new Vector2(100, 180));
        TestAssert.Equal(2, director.GetNode<Node2D>("World/RopeTension").GetChildCount(),
            "Task10 accepted Boost gets one distinct tension burst after Reel for the same cast");
        director.PresentClawFrame(travel with
        {
            State = MechanicalClawState.Boost,
            BoostConsumed = true
        }, new Vector2(100, 180));
        TestAssert.Equal(2, director.GetNode<Node2D>("World/RopeTension").GetChildCount(),
            "Task10 duplicate Boost for the same cast cannot create a second burst");
        director.ClearTransientEffects();
        TestAssert.Equal(0, director.ActiveTask10EffectCount,
            "Task10 death/pause/unload/completion cleanup drops all transition identities");
        foreach (string rootName in PrologueVfxDirector.Task10EffectRootNames)
            TestAssert.True(!director.GetNode<Node2D>($"World/{rootName}").IsQueuedForDeletion(),
                $"Task10 cleanup preserves stable replacement root {rootName}");
        director.QueueFree();
        for (int frame = 0; frame < 5; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
    public override async void _Initialize()
    {
        try
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            await Run(this);
            GD.Print("THREAD_CAST_RUNTIME_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    public static async Task Run(SceneTree tree)
    {
        await ThreadCastRuntimeTests.Run(tree);
        await Task7CaseRegistry.Execute(Task7Cases(tree));
    }

    internal static System.Collections.Generic.IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Task7CaseRegistry.Case(nameof(PrologueVfxRuntimeTests),
            nameof(BasicTypedVfxRuntimeContract), () => BasicTypedVfxRuntimeContract(tree)),
        Task7CaseRegistry.Case(nameof(PrologueVfxRuntimeTests),
            nameof(VerifyRuntimeFrameBinding), () => VerifyRuntimeFrameBinding(tree))
    ];

    private static async Task BasicTypedVfxRuntimeContract(SceneTree tree)
    {
        TestAssert.True(ResourceLoader.Exists("res://scenes/vfx/PrologueVfx.tscn"),
            "formal prologue VFX leaf scene exists");
        PrologueVfxDirector director = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn")
            .Instantiate<PrologueVfxDirector>();
        var fixture = new Node2D { Name = "VfxRuntimeFixture" };
        var body = new CharacterBody2D { Name = "GameplayBody", Position = new Vector2(100, 200) };
        var health = new HealthComponent { Name = "Health" };
        body.AddChild(health);
        fixture.AddChild(body);
        fixture.AddChild(director);
        tree.Root.AddChild(fixture);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        Vector2 positionBefore = body.Position;
        int healthBefore = health.CurrentHealth;
        director.Play(PresentationCue.AttackHit, new Vector2(120, 180), Vector2.Right);
        director.Play(PresentationCue.BossTransition, new Vector2(200, 100), Vector2.Up);
        TestAssert.Equal(2, director.CueCount, "typed presentation cues reach the runtime VFX director");
        TestAssert.Equal(positionBefore, body.Position, "VFX playback cannot move gameplay bodies");
        TestAssert.Equal(healthBefore, health.CurrentHealth, "VFX playback cannot mutate gameplay health");
        TestAssert.True(director.ActiveBurstCount > 0, "runtime cue playback creates visible burst nodes");

        director.Configure(new GameSettingsData { ScreenShake = 1f, ReduceFlashes = true });
        director.Play(PresentationCue.BossTransition, Vector2.Zero, Vector2.Right);
        TestAssert.Near(0f, director.LastScreenFlashOpacity, .001f,
            "reduced-flash runtime playback never raises the full-screen flash");

        AttackPresentationFrame telegraph = Attack(901, AttackPhase.Telegraph,
            new AttackShapeFrame(AttackShapeKind.GroundBand, new Vector2(220, 240), new Vector2(160, 28), 0f,
                System.Array.Empty<Vector2>()));
        int beforeTyped = director.TypedEffectCount;
        director.PresentAttackFrame(body, telegraph);
        director.PresentAttackFrame(body, telegraph);
        TestAssert.Equal(beforeTyped + 1, director.TypedEffectCount,
            "repeated AttackId and Phase never duplicate typed VFX");
        TestAssert.Equal(new Color("d7b85b"), director.LastTypedColor,
            "Telegraph uses gold readiness rather than danger red");
        TestAssert.Equal("countdown-outline", director.LastNonColorChannel,
            "Telegraph retains a countdown shape channel independent of color");
        TestAssert.True(director.ActiveDangerCount == 0,
            "Telegraph never marks a non-active attack as true red danger");

        director.PresentAttackFrame(body, Attack(901, AttackPhase.Active, telegraph.Shape));
        TestAssert.Equal(new Color("b75a4b"), director.LastTypedColor,
            "Active uses red only on the authoritative danger shape");
        TestAssert.Equal(AttackShapeKind.GroundBand, director.LastTypedAttackShape,
            "typed VFX preserves the exact gameplay attack geometry kind");
        TestAssert.True(director.ActiveDangerCount == 1,
            "Active publishes one true danger shape");

        director.PresentAttackFrame(body, Attack(901, AttackPhase.Recovery, telegraph.Shape));
        TestAssert.Equal(new Color("f2e6c9"), director.LastTypedColor,
            "Recovery uses white impact confirmation rather than lingering danger red");
        TestAssert.True(director.ActiveDangerCount == 0,
            "Recovery clears active danger immediately");
        TestAssert.True(director.LastTypedAudit.Contains("AttackId=901") &&
                        director.LastTypedAudit.Contains("Phase=Recovery") &&
                        director.LastTypedAudit.Contains("danger=False"),
            "typed VFX proof records attack ID, phase, and danger semantics");

        ThreadCastFrame apply = Thread(404, ThreadCastState.Apply, ThreadFailureReason.None);
        director.PresentThreadFrame(apply, new Vector2(100, 200), new Vector2(320, 150));
        director.PresentThreadFrame(apply, new Vector2(100, 200), new Vector2(320, 150));
        TestAssert.Equal(new Color("8de4d8"), director.LastTypedColor,
            "mechanical-thread staged VFX owns the cyan language");
        TestAssert.Equal("triple-tension", director.LastNonColorChannel,
            "thread Apply retains a three-strand tension channel independent of color");
        TestAssert.True(director.ActiveThreadEffectCount == 1,
            "repeated CastId and State never duplicate thread VFX");
        director.PresentThreadFrame(Thread(404, ThreadCastState.Failed, ThreadFailureReason.Cancelled),
            new Vector2(100, 200), new Vector2(320, 150));
        TestAssert.True(director.ActiveThreadEffectCount == 0,
            "cancelled or released targets clear thread transients immediately");

        director.PresentAttackFrame(body, Attack(902, AttackPhase.Active, telegraph.Shape));
        PrologueTypedVfxEffect queuedForSafeRelease = null;
        foreach (Node child in director.GetNode<Node2D>("World").GetChildren())
            if (child is PrologueTypedVfxEffect typed && typed.IsDanger) queuedForSafeRelease = typed;
        director.ClearTransientEffects();
        TestAssert.True(director.TypedEffectCount == 0 && director.ActiveDangerCount == 0,
            "room/death cleanup removes all typed cross-room transients");
        TestAssert.True(queuedForSafeRelease != null && queuedForSafeRelease.IsQueuedForDeletion() &&
                        queuedForSafeRelease.GetParent() != null,
            "cleanup keeps managed VFX parented until the deferred free prevents a GCHandle teardown race");

        var lineHost = new Node2D { Name = "LineHost", Position = new Vector2(40, 30) };
        var line = new ThreadLineVisual { Name = "FormalThreadLine" };
        lineHost.AddChild(line);
        fixture.AddChild(lineHost);
        Vector2 start = new(55, 44);
        Vector2 end = new(312, 121);
        line.Present(start, end, .75f, ThreadLineState.Connected);
        TestAssert.Equal(start, line.LastWorldStart, "formal thread line preserves its runtime start endpoint");
        TestAssert.Equal(end, line.LastWorldEnd, "formal thread line preserves its runtime target endpoint");
        TestAssert.Equal(4, line.Points.Length, "formal thread strand includes two controlled tension bends");
        TestAssert.Equal(3, line.ActiveStrandCount, "formal thread line is three real procedural strands");

        PackedScene levelScene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = levelScene.Instantiate<PrologueLevelController>();
        TestAssert.True(level.GetNodeOrNull<PrologueVfxDirector>("Vfx") != null,
            "generated prologue mounts the formal VFX director");
        TestAssert.True(level.GetNodeOrNull<CameraFeedbackController>("Vfx/CameraFeedback") != null,
            "generated prologue mounts bounded camera feedback");
        TestAssert.True(level.GetNodeOrNull<ThreadLineVisual>(
                "PlayerRoot/Player/VisualRoot/MechanicalClaw/Line") != null,
            "generated player exposes the formal line under the stable mechanical-claw visual root");
        level.Free();

        fixture.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static AttackPresentationFrame Attack(ulong id, AttackPhase phase, AttackShapeFrame shape) =>
        new AttackPresentationFrame(
            new AttackTimelineFrame(id, EnemyAttackKind.BossGroundWave, phase, .5f, .4f,
                shape.Origin, true), shape).Validate();

    private static ThreadCastFrame Thread(ulong id, ThreadCastState state, ThreadFailureReason failure) => new(
        id, state, ThreadAimMode.Hold, Vector2.Right, 8, failure == ThreadFailureReason.None, failure,
        .2f, .1f);

    private static async Task VerifyRuntimeFrameBinding(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, nameof(VerifyRuntimeFrameBinding));
        tree.Root.AddChild(level);
        for (int frame = 0; frame < 5; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        PrologueVfxDirector levelVfx = level.GetNode<PrologueVfxDirector>("Vfx");
        Knight.Audio.PrologueAudioDirector levelAudio = level.GetNode<Knight.Audio.PrologueAudioDirector>("Audio");
        int audioBefore = levelAudio.FramePlaybackCount;
        RustShellSquire actor = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        var runtimeRoom = new PrologueRoom { Name = "TypedAttackFrameFixture" };
        level.GetNode<Node2D>("WorldRoot").AddChild(runtimeRoom);
        typeof(PrologueLevelController).GetProperty(nameof(PrologueLevelController.CurrentRoom))!
            .SetValue(level, runtimeRoom);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        level.Player.ProcessMode = Node.ProcessModeEnum.Disabled;
        level.Player.Velocity = Vector2.Zero;
        actor.Gravity = 0f;
        runtimeRoom.AddChild(actor);
        actor.GlobalPosition = level.Player.GlobalPosition + new Vector2(48, -16);

        for (int frame = 0; frame < 240 && levelAudio.FramePlaybackCount == audioBefore; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        GD.Print($"TASK4_BINDING_DIAGNOSTIC before={audioBefore} after={levelAudio.FramePlaybackCount} " +
                 $"cue={levelAudio.LastCue} audio={levelAudio.LastFrameAudit} vfx={levelVfx.LastTypedAudit} " +
                 $"actor_state={actor.CurrentState} actor={actor.GlobalPosition} player={level.Player.GlobalPosition} " +
                 $"los={actor.HasLineOfSightTo(level.Player)} process={actor.IsPhysicsProcessing()}");
        TestAssert.True(levelAudio.FramePlaybackCount > audioBefore &&
                        levelAudio.LastCue == Knight.Audio.PrologueAudioCue.RustShellTelegraph,
            "real RustShell AttackFrameChanged drives typed telegraph audio without a compatibility fallback");
        TestAssert.True(levelVfx.LastTypedAudit.Contains("Phase=Telegraph"),
            "real RustShell AttackFrameChanged drives typed phase VFX");

        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
    }
}
