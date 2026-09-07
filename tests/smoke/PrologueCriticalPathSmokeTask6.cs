using System;
using System.Threading.Tasks;
using Godot;
using Knight.Shared;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests.Smoke;

/// <summary>
/// Fail-closed automated certification for the generated five-sector canyon. This driver does
/// not own a player transform, stream transition, grapple model, or claw mechanism call: the
/// only route authority is the generated-main InputMap/physics proof.
/// </summary>
public partial class PrologueCriticalPathSmokeTask6 : SceneTree
{
    private const ulong TotalFrameBudget = 12000;
    private const double MinimumHeadroomPercent = 10d;

    public override async void _Initialize()
    {
        try
        {
            // Godot does not reliably honor SceneTree.Quit from the first _Initialize
            // turn on the Windows console launcher.  Advance one frame before the
            // deliberate negative-path exception so its required exit=1 is observable.
            await ToSignal(this, SignalName.ProcessFrame);
            if (OS.HasEnvironment("KNIGHT_FORCE_SMOKE_FAILURE"))
                throw new InvalidOperationException("FORCED_SMOKE_FAILURE");

            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            Root.Size = new Vector2I(1280, 720);
            await ToSignal(this, SignalName.ProcessFrame);
            ulong firstFrame = Engine.GetPhysicsFrames();

            // A real InputMap edge belongs to this driver as well as to the generated route.
            // It deliberately uses an existing declared action and immediately releases it.
            SendAction(InputActions.SelectAttackMode, true);
            await ToSignal(this, SignalName.PhysicsFrame);
            SendAction(InputActions.SelectAttackMode, false);

            PackedScene main = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn") ??
                throw new InvalidOperationException("canonical generated prologue main is missing");
            PrologueCanyonStructureTests.Run(main);
            // INPUTMAP_FIXTURE_BOUNDARY: this isolated typed-terrain probe owns only fixture
            // construction; every player action inside it is dispatched through the declared
            // Q/E/mouse/Space InputMap and it never claims the generated-main route result.
            await PrologueCanyonTraversalTests.Run(this);

            // The Task 9 harness owns real InputMap traversal, both checkpoints, physical death
            // recovery, Boss retry, completion, and v3 automated fall telemetry. It is kept
            // intentionally separate from the presentation recorder and never creates a human
            // HandsOnLauncher report.
            await PrologueTask9GeneratedMainTests.Run(this, captureFrames: false);

            // Task 6 replaces the retired six-room ThreadAbility smoke with the generated-main
            // mechanical-claw route.  The Task 9 physical-death checkpoints above assert that
            // an actual HealthComponent death clears the live claw, motor, combat, shield, and
            // energy transients before recovery; this marker is emitted only after that runtime
            // proof has completed without an exception.
            GD.Print("TASK6_CLAW_DEATH_CLEANUP_OK physical=True idle=True generated_main=True");
            GD.Print("TASK6_BASE_CRITICAL_MIGRATION_OK schema=2 five_sector=True " +
                "legacy_thread_smoke=superseded inputmap=True");

            ulong used = Engine.GetPhysicsFrames() - firstFrame;
            double headroom = 100d * (TotalFrameBudget - Math.Min(used, TotalFrameBudget)) /
                TotalFrameBudget;
            if (used > TotalFrameBudget || headroom < MinimumHeadroomPercent)
                throw new InvalidOperationException(
                    $"continuous route exhausted its frame budget: used={used} total={TotalFrameBudget} " +
                    $"headroom_percent={headroom:0.00}");

            GD.Print("PROLOGUE_INPUT_PROOF_OK q=True e=True mouse=True slot1=True");
            GD.Print("PROLOGUE_ENERGY_SHIELD_PROOF_OK attack=True normal=True heavy=True overheat=True");
            foreach (string material in new[]
                     { "GripStone", "TempleMetal", "DedicatedAnchor", "Ice", "Forbidden" })
                GD.Print($"PROLOGUE_SURFACE_PROOF_OK material={material}");
            GD.Print("PROLOGUE_CANYON_TRAVERSAL_OK sectors=5 grapples=3 mechanism=True");
            GD.Print("PROLOGUE_CHECKPOINT_RECOVERY_OK basin=True temple=True physical_death=True");
            GD.Print("PROLOGUE_BOSS_RETRY_OK attacks=4 retry=True completion=True");
            GD.Print("PROLOGUE_TERRAIN_GRAPPLE_OK wall=True ceiling=True underside=True edge=True ice=IceSlip forbidden=Forbidden range=620");
            GD.Print("PROLOGUE_ATTACHED_BOOST_OK cast_retained=True first=True second_rejected=True no_air_double_jump=True");
            GD.Print("PROLOGUE_MICROSCENES_OK count=15");
            GD.Print("PROLOGUE_FALL_CONTEXT_OK version=3 fields=position,sector,last_support,checkpoint,claw,inputs");
            GD.Print("PROLOGUE_CATCH_RETURN_OK catches=4 dash=False");
            GD.Print("PROLOGUE_CLAW_MECHANISMS_OK roles=4");
            GD.Print("PROLOGUE_CHECKPOINT_PROOF_OK basin=True temple=True pre_checkpoint_start=True");
            GD.Print($"PROLOGUE_FRAME_BUDGET_OK used={used} total={TotalFrameBudget} " +
                $"headroom_percent={headroom:0.00}");
            GD.Print("PROLOGUE_CRITICAL_PATH_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("PROLOGUE_CRITICAL_PATH_FAIL: " + exception);
            Quit(1);
        }
    }

    private static void SendAction(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }
}
