using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class BasinLaborerClearanceRuntimeTests
{
    // Broad-pre and temp-storage callers retain their entry point while testing
    // the current saved Main, not the retired layout1 SalvageFloor paths.
    public static Task Run(SceneTree tree) => CurrentBasinLaborerClearanceRuntimeTests.Run(tree);

    internal static PrologueLevelController CreateLevelFixture()
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        // Controlled pre-tree target placement, not a claim of walking here.
        // Only the input adapter is disabled; all motor/health/AI physics run.
        var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = new Vector2(3300, 4200);
        player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
        level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = PrologueSectorId.CanyonBasin;
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "basin-clearance");
        return level;
    }

    private static bool SupportedBy(CharacterBody2D body, StaticBody2D floor)
    {
        if (!body.IsOnFloor()) return false;
        for (int index = 0; index < body.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = body.GetSlideCollision(index);
            if (collision.GetCollider() == floor && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
