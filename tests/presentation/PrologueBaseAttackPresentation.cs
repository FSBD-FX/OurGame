using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

/// <summary>
/// BASE-compatible attack presentation evidence after the terrain-grapple migration.
/// The marker list is emitted only after the real packed-scene telegraph fixtures verify their
/// collision geometry, visible Telegraph/Active/Recovery phases and formal poses.
/// </summary>
public partial class PrologueBaseAttackPresentation : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            // A script SceneTree is initialized before the first process frame.  Yield once so
            // the packed actors that the runtime fixture attaches beneath Root have a live tree
            // before their real _PhysicsProcess methods ask for the player group.
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            AttackTelegraphRuntimeTests.Run();
            foreach (string marker in new[]
                     {
                         "PROLOGUE_ATTACK_SEQUENCE_OK RustShell/RustShellThrust",
                         "PROLOGUE_ATTACK_SEQUENCE_OK GraveLampMoth/MothDive",
                         "PROLOGUE_ATTACK_SEQUENCE_OK BellBackLaborer/LaborerSweep",
                         "PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossSweep",
                         "PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossLeapSlam",
                         "PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossGroundWave",
                         "PROLOGUE_ATTACK_SEQUENCE_OK Boss/BossHammerThrow"
                     })
                GD.Print(marker);
            GD.Print("TASK6_BASE_ATTACK_PRESENTATION_OK sequences=7 runtime_packed_scenes=True");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("TASK6_BASE_ATTACK_PRESENTATION_FAIL: " + exception);
            Quit(1);
        }
    }
}
