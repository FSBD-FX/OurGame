using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Tests.Integration;
using System;
using System.Threading.Tasks;

namespace Knight.Tests.Presentation;

// Component only. Disclosed initial central Player placement and pre-mount
// zero invulnerability; thereafter actual E/LMB/A/D, no gameplay API steering.
// Movie writing belongs to the guarded launcher. No save/recorder/audio/director.
public static class CanyonSteamDiverterComponentProbe
{
    public static async Task Run(SceneTree tree)
    {
        tree.Root.ContentScaleSize = new Vector2I(1280, 720);
        tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        tree.Root.Size = new Vector2I(1280, 720);
        DisplayServer.WindowSetSize(new Vector2I(1280, 720));
        GD.Print("TASK17A_COMPONENT_PROOF limits=component-only;initial-placement;zero-invulnerability;scripted-physical-input;silent-fixed60fps;not-human-realtime-installed-full-game");
        await CanyonSteamDiverterPhysicalTests.WithFixture(tree, "component-visible-proof", 0f, async f =>
        {
            CanyonSteamDiverterVisual visual = CanyonSteamDiverterVisualTests.AddVisual(f);
            f.Root.AddChild(new Camera2D { Name = "ComponentCamera", Enabled = true });
            var backdrop = new CanyonSteamDiverterProofBackdrop {
                FloorCollision = f.Root.GetNode<CollisionShape2D>("Floor/Collision") };
            f.Root.AddChild(backdrop);
            var hud = new CanvasLayer { Name = "ComponentProofDisclosure", Layer = 50 };
            var title = NewLabel("COMPONENT ONLY · SCRIPTED INPUT · INITIAL PLACEMENT / ZERO INVUL · SILENT FIXED 60 FPS", 18f, 12f, 17);
            var stage = NewLabel("Actual E/LMB: selected arrow means STEAM FLOW, not a safe walking direction", 18f, 40f, 18);
            var state = NewLabel("", 18f, 68f, 17);
            hud.AddChild(new ColorRect { Size = new Vector2(1280, 108), Color = new Color("11171a"), MouseFilter = Control.MouseFilterEnum.Ignore });
            hud.AddChild(title); hud.AddChild(stage); hud.AddChild(state); f.Root.AddChild(hud);
            f.Root.AddChild(new CanyonSteamDiverterProofHud { Visual = visual, Fixture = f, StateLabel = state, ProcessPriority = 10 });
            await f.Step(); await f.Step();
            TestAssert.True(visual.Presentation.IsBound, "visible component uses bound removable feedback, not a fake indicator");

            stage.Text = "Fresh E + LMB → left steam; a second fresh cast during .60 s busy must reject";
            f.SampleObserved = () => AssertHarmless(f);
            var first = await f.Cast(CanyonSteamDiverterSide.Left);
            await BusyFreshCast(f, first);
            await f.FinishHandoff(first);
            TestAssert.True(Engine.GetPhysicsFrames() - first.PhysicsFrame <= 38UL,
                "actual busy cast cannot restart original .60s handoff (two sampled-frame observation allowance)");
            f.Record("busy-fresh-cast-rejected-original-timer-complete");

            stage.Text = "Immediately after each handoff: accepted right, then accepted left — observe harmless-loop pattern";
            var second = await f.Cast(CanyonSteamDiverterSide.Right); await f.FinishHandoff(second);
            var third = await f.Cast(CanyonSteamDiverterSide.Left); await f.FinishHandoff(third);
            TestAssert.Equal(3, f.Adapter.ApplicationCount, "three genuine accepted mapped casts plus one genuinely rejected busy cast");
            f.Record("two-immediate-post-handoff-accepted-switches-remain-harmless");
            f.SampleObserved = null;

            stage.Text = "Watch two automatic left cycles: outlined warning → full warm danger column; central capsule stays safe";
            await f.ObserveTwoCycles(CanyonSteamDiverterSide.Left, 100, mustDamage: false, pocket: true);
            stage.Text = "D: walk through harmless RIGHT side, then E/LMB reverse, A return during full safe/warning";
            await Walk(f, Key.D, () => f.Player.Position.X >= 170f, 90);
            TestAssert.True(!f.FootprintContains(f.Left) && !f.FootprintContains(f.Right), "actual motor reaches outside both footprints without a teleport");
            var right = await f.Cast(CanyonSteamDiverterSide.Right); await f.FinishHandoff(right);
            await Walk(f, Key.A, () => f.Player.Position.X <= 0f, 90);
            TestAssert.True(!f.FootprintContains(f.Left) && !f.FootprintContains(f.Right), "actual reverse traversal returns capsule to real central safe pocket");
            stage.Text = "E/LMB restore LEFT steam; D return through harmless right side — no reset, healing or position override";
            var left = await f.Cast(CanyonSteamDiverterSide.Left); await f.FinishHandoff(left);
            await Walk(f, Key.D, () => f.Player.Position.X >= 170f, 90);
            TestAssert.Equal(100, f.Health.CurrentHealth, "actual keyboard traversal/reverse/return is healthy with disclosed zero invulnerability");
            f.Record("actual-keyboard-cross-reverse-return-complete");
            stage.Text = "Component proof complete · placement, actors, full-level and native shutdown acceptance remain OPEN";
            for (int index = 0; index < 45; index++) await f.Step();
        });
        GD.Print("TASK17A_COMPONENT_PROOF_OK");
    }

    private static async Task BusyFreshCast(CanyonSteamDiverterPhysicalTests.Fixture f,
        CanyonSteamDiverterPhysicalTests.Latch original)
    {
        int failed = f.FailedCasts, accepted = f.Adapter.ApplicationCount;
        int grapple = f.GrapplePressSamples, primary = f.PrimaryPressSamples;
        await f.Wait(() => f.Claw.CurrentFrame.State is MechanicalClawState.Idle or MechanicalClawState.Released,
            20, "busy attempt starts from a fresh actual claw state");
        await f.Step(() => { f.Aim(); f.SendE(true); });
        await f.Wait(() => f.GrapplePressSamples > grapple,
            8, "busy attempt's literal E is sampled");
        await f.Step(() => f.SendE(false)); await f.Step();
        await f.Step(() => { f.Aim(); f.SendLeft(true); });
        await f.Wait(() => f.FailedCasts > failed, 24, "fresh actual busy LMB reaches rejection callback");
        await f.Step(() => f.SendLeft(false));
        await f.Wait(() => !f.InputReader.CurrentFrame.PrimaryHeld, 8, "busy primary release reaches a fresh actual sample");
        TestAssert.True(f.LastFailedCast.Failure == MechanicalClawFailure.TargetLost &&
            f.LastFailedCast.LockedTargetRuntimeId == f.Target.GetInstanceId(), "busy rejection is the actual bound legacy target, not a missed cast");
        TestAssert.Equal(accepted, f.Adapter.ApplicationCount, "busy actual input cannot apply controller twice");
        TestAssert.True(f.PrimaryPressSamples > primary, "busy fresh LMB press was observed by the real input reader");
        TestAssert.True(f.Pair.IsHandingOff && f.Pair.SelectedSide == original.Side, "busy actual input leaves original transition and selection intact");
        f.Record("actual-busy-target-rejection", f.LastFailedCast.CastId, f.LastFailedCast.LockedTargetRuntimeId);
    }
    private static async Task Walk(CanyonSteamDiverterPhysicalTests.Fixture f, Key key, Func<bool> reached, int cap)
    {
        await f.Step(() => SendKey(f, key, true));
        await f.Wait(reached, cap, "actual " + key + " motor traversal reaches disclosed coordinate");
        await f.Step(() => SendKey(f, key, false));
        await f.Wait(() => Mathf.Abs(f.Player.Velocity.X) < 1f && f.InputReader.CurrentFrame.MoveAxis == 0f,
            45, "actual movement release and normal motor braking settle without velocity override");
        TestAssert.Equal(100, f.Health.CurrentHealth, "actual movement samples preserve real Health");
        f.Record("actual-walk-" + key + "-settled");
    }
    private static void SendKey(CanyonSteamDiverterPhysicalTests.Fixture f, Key key, bool pressed)
    {
        using var input = new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed, Echo = false };
        Input.ParseInputEvent(input); Input.FlushBufferedEvents(); f.Record("actual-key-" + key + "-" + pressed);
    }
    private static void AssertHarmless(CanyonSteamDiverterPhysicalTests.Fixture f) =>
        TestAssert.True(!f.Left.Frame.HazardActive && !f.Right.Frame.HazardActive &&
            f.Left.Collision.Disabled && f.Right.Collision.Disabled && !f.Left.Monitoring && !f.Right.Monitoring &&
            f.Health.CurrentHealth == 100, "rapid fresh casts are observed harmless on every sampled postphysics step");
    private static Label NewLabel(string text, float x, float y, int size)
    {
        var label = new Label { Text = text, Position = new Vector2(x, y), MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color("f0d481")); return label;
    }
}

// Test-only visible context: uses the actual floor collision, no extra collider.
public partial class CanyonSteamDiverterProofBackdrop : Node2D
{
    internal CollisionShape2D FloorCollision;
    public override void _Draw()
    {
        Rect2 rect = FloorCollision.Shape.GetRect();
        Vector2[] local = { rect.Position, new(rect.End.X, rect.Position.Y), rect.End, new(rect.Position.X, rect.End.Y) };
        Vector2[] polygon = new Vector2[4];
        for (int index = 0; index < 4; index++) polygon[index] = ToLocal(FloorCollision.GlobalTransform * local[index]);
        DrawColoredPolygon(polygon, new Color("30251c"));
        DrawPolyline(new[] { polygon[0], polygon[1], polygon[2], polygon[3], polygon[0] }, new Color("d4aa58"), 3f);
    }
}

public partial class CanyonSteamDiverterProofHud : Node
{
    internal CanyonSteamDiverterVisual Visual;
    internal CanyonSteamDiverterPhysicalTests.Fixture Fixture;
    internal Label StateLabel;
    public override void _Process(double delta)
    {
        CanyonSteamDiverterPresentation frame = Visual.Presentation;
        StateLabel.Text = $"STEAM → {frame.SelectedSide} | {(frame.HandoffVapor ? "BUSY / HARMLESS VAPOR" : "cycling")} | " +
            $"L warning/danger {frame.Left.Warning}/{frame.Left.Danger} · R {frame.Right.Warning}/{frame.Right.Danger} | " +
            $"HP {Fixture.Health.CurrentHealth} · move {Fixture.InputReader.CurrentFrame.MoveAxis} · physics {Engine.GetPhysicsFrames()}";
    }
}
