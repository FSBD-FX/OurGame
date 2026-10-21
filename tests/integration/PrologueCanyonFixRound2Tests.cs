using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class PrologueCanyonFixRound2Tests
{
    private static readonly IReadOnlyDictionary<PrologueSectorId, (float Start, float End)[]>
        AuthoredBottomRecoveryOpenings = new Dictionary<PrologueSectorId, (float Start, float End)[]>
        {
            [PrologueSectorId.CanyonEntrance] = Array.Empty<(float, float)>(),
            [PrologueSectorId.CanyonDescent] = new[] { (1004f, 1196f) },
            [PrologueSectorId.CanyonBasin] = Array.Empty<(float, float)>(),
            [PrologueSectorId.CanyonAscent] = Array.Empty<(float, float)>(),
            [PrologueSectorId.LostTemple] = Array.Empty<(float, float)>()
        };

    public static async Task Run(SceneTree tree)
    {
        AscentLeftSeamDoesNotCarveBottomRecovery();
        AssertGeneratedRecoveryGeometryMatchesIndependentAuthoredOpenings();
        await AscentBottomFallUsesRealRecoveryArea(tree);
        GD.Print("TASK8_FIX2_STRUCTURE_OK ascent_bottom_recovery=covered " +
            "physical_complement=all_five_sectors");
    }

    // Break caught: treating a left-edge seam as a bottom seam leaves this real fall path without
    // KillZone authority.
    private static void AscentLeftSeamDoesNotCarveBottomRecovery()
    {
        PrologueSector ascent = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonAscent.tscn").Instantiate<PrologueSector>();
        try
        {
            bool covered = ascent.GetNode("Gameplay/FallRecovery").GetChildren().OfType<Area2D>()
                .Any(area => Contains(area, new Vector2(50f, 1432f)));
            TestAssert.True(covered,
                "Ascent local bottom x=50 remains physically covered by fall recovery; " +
                "PreviousSeam=(2,1314) is a left edge, not a bottom seam");
        }
        finally { ascent.Free(); }
    }

    private static bool Contains(Area2D area, Vector2 point)
    {
        if (area.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is not RectangleShape2D shape)
            throw new InvalidOperationException($"{area.Name} needs rectangular physical recovery");
        return new Rect2(area.Position - shape.Size / 2f, shape.Size).HasPoint(point);
    }

    // This oracle is deliberately authored as fixed sector intervals, not derived from the
    // recovery classifier or its in-memory carve plan. It reads only packed Area2D geometry.
    private static void AssertGeneratedRecoveryGeometryMatchesIndependentAuthoredOpenings()
    {
        foreach ((PrologueSectorId id, (float Start, float End)[] openings) in
                 AuthoredBottomRecoveryOpenings)
        {
            PrologueSector sector = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id))
                .Instantiate<PrologueSector>();
            try
            {
                Rect2 bounds = PrologueCanyonBuildParts.Bounds(id);
                Area2D[] areas = sector.GetNode("Gameplay/FallRecovery").GetChildren()
                    .OfType<Area2D>().ToArray();
                for (float x = bounds.Position.X + .5f; x < bounds.End.X; x += 1f)
                {
                    bool expectedOpen = openings.Any(opening =>
                        x >= opening.Start && x <= opening.End);
                    bool physicallyCovered = areas.Any(area =>
                        Contains(area, new Vector2(x, bounds.End.Y - 8f)));
                    TestAssert.True(physicallyCovered != expectedOpen,
                        $"{id} packed RecoveryVolume geometry has the independent authored " +
                        $"bottom complement at x={x:0.0}; expected_open={expectedOpen} " +
                        $"covered={physicallyCovered}");
                }
            }
            finally { sector.Free(); }
        }
        GD.Print("TASK8_FIX3_PHYSICAL_COMPLEMENT_OK sectors=5 descent_opening=1004..1196");
    }

    // Break caught: a fixture can make recovery appear to work by directly enabling gameplay
    // while the normal flow remains Title, which produces rejected Title->Recovering transitions.
    // The sole position assignment is this fixture's pre-tree spawn; normal headless autoplay
    // enters Playing before production gravity reaches the real recovery Area.
    private static async Task AscentBottomFallUsesRealRecoveryArea(SceneTree tree)
    {
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        Vector2 localFallStart = new(50f, 1360f);
        Vector2 expectedLiveRoundStart = PrologueCanyonBuildParts.WorldOffsets[
            PrologueSectorId.CanyonAscent] + localFallStart;
        player.GlobalPosition = expectedLiveRoundStart;
        world.InitialSector = PrologueSectorId.CanyonAscent;
        bool bodyEntered = false;
        bool recoveryRequested = false;
        bool recoveryCompleted = false;
        bool playingBeforeRecovery = false;
        bool recoveringAfterRequest = false;
        bool playingAfterRecovery = false;
        PrologueSectorId sectorAtRequest = PrologueSectorId.CanyonEntrance;
        PrologueSectorId sectorAtCompletion = PrologueSectorId.CanyonEntrance;
        Vector2 bodyEnteredPosition = Vector2.Zero;
        Vector2 completionPosition = Vector2.Zero;
        bool completionGameplayActive = false;
        level.RecoveryRequested += OnRecovery;
        level.RecoveryCompleted += OnRecoveryCompleted;
        tree.Root.AddChild(level);
        try
        {
            for (int frame = 0; frame < 60 &&
                 !(flow.State == PrologueGameFlowState.Playing && player.IsPhysicsProcessing()); frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && player.IsPhysicsProcessing(),
                "normal headless autoplay enters Playing and activates the player before recovery");
            TestAssert.Equal(PrologueSectorId.CanyonAscent, world.CurrentSector,
                "normal new-run flow mounts the fixture's authored Ascent initial sector");
            playingBeforeRecovery = true;
            PrologueSector ascent = null;
            for (int frame = 0; frame < 60 && ascent == null; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                ascent = world.GetChildren().OfType<PrologueSector>().FirstOrDefault(sector =>
                    sector.SectorId == PrologueSectorId.CanyonAscent);
            }
            TestAssert.True(ascent != null, "physical Ascent fall probe mounts its authored sector");
            Area2D recovery = ascent.GetNode<Area2D>("Gameplay/FallRecovery/RecoveryVolume");
            recovery.BodyEntered += OnBodyEntered;
            for (int frame = 0; frame < 300 &&
                 !(bodyEntered && recoveryRequested && recoveryCompleted); frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                recoveringAfterRequest |= recoveryRequested &&
                    flow.State == PrologueGameFlowState.Recovering;
            }
            // RecoveryCompleted is emitted before GameFlow's own subscriber promotes Recovering
            // back to Playing, so observe the next bounded physics ticks rather than sampling
            // inside the signal's subscription order.
            for (int frame = 0; frame < 30 && recoveryCompleted && !playingAfterRecovery; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                playingAfterRecovery = flow.State == PrologueGameFlowState.Playing &&
                    player.IsPhysicsProcessing();
            }
            TestAssert.True(bodyEntered && recoveryRequested && recoveryCompleted,
                "Ascent local x=50 physical fall enters RecoveryVolume and completes production respawn; " +
                $"player={player.GlobalPosition} velocity={player.Velocity} recovery={recovery.GlobalPosition} " +
                $"entered={bodyEntered} requested={recoveryRequested} completed={recoveryCompleted} " +
                $"request_sector={sectorAtRequest} completion_sector={sectorAtCompletion}");
            Vector2 localFall = bodyEnteredPosition - PrologueCanyonBuildParts.WorldOffsets[
                PrologueSectorId.CanyonAscent];
            TestAssert.True(Mathf.Abs(localFall.X - 50f) <= 16f,
                $"real input fall crosses the Ascent x≈50 recovery probe, actual_local={localFall}");
            TestAssert.Equal(PrologueSectorId.CanyonAscent, sectorAtRequest,
                "real recovery request originates in the live Ascent rather than an early sector reset");
            TestAssert.Equal(PrologueSectorId.CanyonAscent, sectorAtCompletion,
                "completed recovery restores the live Ascent checkpoint rather than the wrong sector");
            TestAssert.True(playingBeforeRecovery && recoveringAfterRequest && playingAfterRecovery &&
                flow.LastRejectedTransition == string.Empty,
                "normal recovery performs Playing->Recovering->Playing without a rejected Title transition; " +
                $"before={playingBeforeRecovery} recovering={recoveringAfterRequest} " +
                $"after={playingAfterRecovery} rejected={flow.LastRejectedTransition}");
            TestAssert.True(completionPosition.DistanceTo(expectedLiveRoundStart) <= 8f &&
                completionGameplayActive,
                $"completed recovery restores the expected live round start and gameplay processing; " +
                $"completion={completionPosition} expected={expectedLiveRoundStart} " +
                $"processing={completionGameplayActive}");
            GD.Print($"TASK8_FIX4_RECOVERY_FLOW_OK states=Playing>Recovering>Playing " +
                $"request={sectorAtRequest} completion={sectorAtCompletion} " +
                $"fall_local={localFall} player={completionPosition} rejected={flow.LastRejectedTransition}");
            recovery.BodyEntered -= OnBodyEntered;
        }
        finally
        {
            level.RecoveryRequested -= OnRecovery;
            level.RecoveryCompleted -= OnRecoveryCompleted;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }

        void OnBodyEntered(Node2D body)
        {
            if (body != player) return;
            bodyEntered = true;
            bodyEnteredPosition = player.GlobalPosition;
        }
        void OnRecovery(string _)
        {
            recoveryRequested = true;
            sectorAtRequest = world.CurrentSector;
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "RecoveryRequested originates from the normal Playing flow state");
        }
        void OnRecoveryCompleted(string _)
        {
            recoveryCompleted = true;
            sectorAtCompletion = world.CurrentSector;
            completionPosition = player.GlobalPosition;
            completionGameplayActive = player.IsPhysicsProcessing();
        }
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
