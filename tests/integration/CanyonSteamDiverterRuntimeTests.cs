using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Abilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Knight.Tests.Integration;

public static class CanyonSteamDiverterRuntimeTests
{
    // Break caught: mounted controller leaves its default harmless jet free to
    // cycle. This is only initial binding, not real mapped claw-input proof.
    public static async Task RunMountedDefault(SceneTree tree)
    {
        var root = new Node2D { Name = "Task17AZeroInvulnerabilityContractFixture", Position = new Vector2(400, 300) };
        var pair = new CanyonSteamDiverter { Name = "Diverter" };
        // Initial-state observation is deliberately clock-controlled. Live
        // automatic advancement has its own contract below, not frozen jets.
        pair.Ready += () => pair.SetPhysicsProcess(false);
        CanyonSteamJet left = Jet("LeftJet", new Vector2(-100, -15));
        CanyonSteamJet right = Jet("RightJet", new Vector2(100, -15));
        pair.AddChild(left); pair.AddChild(right); root.AddChild(pair);
        var floor = new StaticBody2D
        {
            Name = "Floor", Position = new Vector2(0, 60),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1000, 20) } });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "Player"; player.Position = new Vector2(0, 14);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        health.MaxHealth = 100; health.InvulnerabilitySeconds = 0f;
        root.AddChild(player);
        ReleaseInput();
        tree.Root.AddChild(root);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(root.IsNodeReady() && pair.IsNodeReady() && left.IsNodeReady() &&
                right.IsNodeReady() && player.IsNodeReady() && health.IsNodeReady(),
                "isolated runtime fixture and actual Player Health are Ready before contract observation");
            TestAssert.Equal(100, health.CurrentHealth, "actual player health initialized before mounted exclusivity observation");
            TestAssert.Equal(0, tree.GetNodesInGroup("prologue_audio").Count,
                "component fixture mounts no prologue audio director");
            TestAssert.True(!left.IsPhysicsProcessing() && !right.IsPhysicsProcessing(),
                "mounted pair owns both jet clocks rather than leaving autonomous physics enabled");
            TestAssert.Equal(0f, left.ElapsedSeconds, "mounted harmless left clock remains at its reset boundary");
            TestAssert.Equal(0f, right.ElapsedSeconds, "mounted default right clock starts at its reset boundary");
            TestAssert.Equal(new CanyonMechanismFrame(false, false, true, 0f), left.Frame,
                "mounted left publishes the complete literal harmless frame");
            TestAssert.Equal(new CanyonMechanismFrame(false, false, true, 0f), right.Frame,
                "mounted right starts at the complete literal ordinary safe frame");
            TestAssert.True(left.DisabledForRun,
                "mounted pair logically disarms its default harmless left jet");
            TestAssert.True(left.Collision.Disabled && !left.Monitoring && !left.Frame.HazardActive,
                "default harmless jet physical state matches logical disarm");
            TestAssert.True(!right.DisabledForRun && !right.Frame.HazardActive && right.Collision.Disabled && !right.Monitoring,
                "mounted default right starts in its ordinary harmless safe phase");
            TestAssert.Equal(100, health.CurrentHealth, "fresh real Player/Hurtbox remains healthy in central pocket at mount");
        }
        finally
        {
            tree.Paused = false;
            ReleaseInput();
            root.QueueFree();
            for (int index = 0; index < 3; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    public static async Task RunFunctionalContracts(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (Func<SceneTree, Task> contract in new Func<SceneTree, Task>[] {
            ReceiverDisarmsAndBusyHandoffDoesNotRestart, OwnedClockPreservesWarningAndResetOrders,
            AutomaticPhysicsCompletesARealCycle, InvalidMountCannotCommitDamage,
            PartialChildLossDisarmsSurvivors })
        {
            try { await contract(tree); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(contract.Method.Name + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: paired diverter functional runtime contracts are not satisfied\n" + string.Join("\n", failures));
    }

    // Break caught: receiver remains inert; a busy latch flips/restarts; a
    // newly latched handoff consumes this same physics frame's delta.
    // This intentionally tests DIRECT receiver calls, not E/LMB or claw latch.
    private static Task ReceiverDisarmsAndBusyHandoffDoesNotRestart(SceneTree tree) =>
        WithFixture(tree, automatic: false, async (root, pair, left, right, player, health) =>
        {
            var interaction = new ThreadInteraction(player, player.GlobalPosition, pair.GlobalPosition, new ThreadAbilitySettings());
            TestAssert.True(pair.TryReceiveThread(interaction), "live paired receiver accepts an idle interaction");
            TestAssert.Equal(CanyonSteamDiverterSide.Left, pair.SelectedSide, "accepted receiver flips desired side exactly once");
            AssertBothDisarmed(left, right, "receiver immediately disarms both real jets");
            TestAssert.True(pair.IsHandingOff && pair.IsDangerTelegraphActive, "handoff is harmless but distinct local telegraph");
            pair.Advance(10d);
            TestAssert.True(pair.IsHandingOff, "same latch frame cannot consume newly started handoff");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            pair.Advance(.30d);
            tree.Paused = true;
            pair.Advance(10d);
            TestAssert.True(pair.IsHandingOff, "pause cannot consume remaining real receiver handoff time");
            AssertBothDisarmed(left, right, "paused handoff keeps both real jets disarmed");
            tree.Paused = false;
            TestAssert.True(!pair.TryReceiveThread(interaction), "busy receiver rejects rather than flips or refreshes handoff");
            TestAssert.Equal(CanyonSteamDiverterSide.Left, pair.SelectedSide, "busy receiver leaves desired side unchanged");
            pair.Advance(.299d); AssertBothDisarmed(left, right, "full handoff remains disarmed before .60 seconds");
            pair.Advance(.001d);
            TestAssert.True(!pair.IsHandingOff && !pair.IsDangerTelegraphActive && !left.DisabledForRun && right.DisabledForRun,
                "complete handoff permits only selected left clock");
            AssertJet(left, false, false, "handoff starts selected jet from ordinary safe phase");
            TestAssert.Equal(0f, left.ElapsedSeconds, "handoff completion discards excess callback time");
            TestAssert.True(pair.TryReceiveThread(interaction), "fresh receiver may reverse after completed handoff");
            AssertBothDisarmed(left, right, "reverse handoff also immediately disarms both");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            pair.Advance(10d);
            TestAssert.True(!pair.IsHandingOff && left.DisabledForRun && !right.DisabledForRun,
                "large handoff step completes once without instantly advancing selected jet");
            AssertJet(right, false, false, "reverse starts right safe despite large handoff delta");
            TestAssert.Equal(100, health.CurrentHealth, "direct receiver fixture central pocket remains healthy");
        });

    // Break caught: unbounded jet advancement skips warning; pause consumes
    // time; either reset traversal order re-arms the nonselected jet; removing
    // optional visuals changes clocks. Actual overlap acceptance is separate.
    private static Task OwnedClockPreservesWarningAndResetOrders(SceneTree tree) =>
        WithFixture(tree, automatic: false, async (root, pair, left, right, player, health) =>
        {
            pair.Advance(.749999d); AssertJet(right, false, false, "ordinary safe interval before early warning");
            pair.Advance(.000001d); AssertJet(right, true, false, "owned clock presents safe final-quarter warning");
            tree.Paused = true;
            float pausedElapsed = right.ElapsedSeconds;
            pair.Advance(10d);
            TestAssert.Equal(pausedElapsed, right.ElapsedSeconds, "paused real controller clock consumes no physics time");
            tree.Paused = false;
            foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1d, 0d }) pair.Advance(invalid);
            TestAssert.Equal(pausedElapsed, right.ElapsedSeconds, "invalid controller delta cannot poison or advance jet clock");
            pair.Advance(.25d); AssertJet(right, true, false, "ordinary warning starts after complete early warning");
            TestAssert.Equal(0f, right.Frame.Phase01, "ordinary warning progress restart is not mistaken for safe phase");
            pair.Advance(.399999d); AssertJet(right, true, false, "real jet stays harmless throughout full ordinary .4 warning");
            pair.Advance(.000002d); AssertJet(right, false, true, "owned clock eventually arms actual selected jet");
            TestAssert.True(left.DisabledForRun, "unselected jet cannot cycle while right is active");
            var interaction = new ThreadInteraction(player, player.GlobalPosition, pair.GlobalPosition, new ThreadAbilitySettings());
            TestAssert.True(pair.TryReceiveThread(interaction), "reset-order fixture first selects formerly dormant left");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            pair.Advance(10d); // Handoff completes, no cycle remainder.
            pair.Advance(10d); pair.Advance(10d); pair.Advance(10d);
            AssertJet(left, false, true, "reset-order fixture genuinely advances left through complete warning to active");
            TestAssert.True(left.ElapsedSeconds > 1.4f && right.DisabledForRun,
                "left nonzero clock is not a vacuous dormant reset observation");
            pair.ResetForCheckpoint();
            AssertDefaultReset(pair, left, right, "controller reset clears both clocks before neighbor reset calls");
            left.ResetForCheckpoint(); right.ResetForCheckpoint();
            AssertDefaultReset(pair, left, right, "parent-first reset order");
            pair.Advance(10d); AssertJet(right, true, false, "one huge callback cannot skip early warning");
            pair.Advance(10d); AssertJet(right, true, false, "second huge callback cannot skip ordinary warning");
            pair.Advance(10d); AssertJet(right, false, true, "third huge callback reaches actual active phase");
            left.ResetForCheckpoint(); right.ResetForCheckpoint(); pair.ResetForCheckpoint();
            AssertDefaultReset(pair, left, right, "jet-first reset order");
            right.GetNode<Node2D>("Telegraph").Free(); right.GetNode<Node2D>("ActiveVisual").Free();
            pair.Advance(10d); pair.Advance(10d); pair.Advance(10d);
            AssertJet(right, false, true, "removing optional jet visuals does not change active collision/timing");
            pair.Advance(10d); AssertJet(right, false, false, "cycle explicitly restarts safe even without visuals");
            TestAssert.Equal(0f, right.ElapsedSeconds, "explicit cycle reset has no modulo edge remainder");
            TestAssert.Equal(100, health.CurrentHealth, "controlled cycle leaves central real Player pocket healthy");
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        });

    // Break caught: only manual Advance works, or production deliberately
    // freezes the default jet to preserve the old initial-state test.
    private static Task AutomaticPhysicsCompletesARealCycle(SceneTree tree) =>
        WithFixture(tree, automatic: true, async (root, pair, left, right, player, health) =>
        {
            bool early = false, warning = false, active = false, restarted = false;
            for (int index = 0; index < 180 && !restarted; index++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                AssertJet(left, false, false, "automatic cycle keeps nonselected jet physically harmless");
                TestAssert.True(left.DisabledForRun && !left.IsPhysicsProcessing() && !right.IsPhysicsProcessing(),
                    "automatic controller retains exclusive ownership of both child clocks");
                if (right.Frame.Telegraphing && right.ElapsedSeconds < 1f) early = true;
                if (right.Frame.Telegraphing && right.ElapsedSeconds >= 1f) warning = true;
                if (right.Frame.HazardActive) active = true;
                if (active && !right.Frame.HazardActive && !right.Frame.Telegraphing) restarted = true;
                TestAssert.Equal(100, health.CurrentHealth, "real central pocket remains healthy during automatic complete cycle");
            }
            TestAssert.True(early && warning && active && restarted,
                "automatic controller physics traverses early warning ordinary warning active and safe restart");
        });

    // Break caught: losing the dormant dependency strands its active sibling
    // indefinitely armed while the controller and both owned clocks stop.
    // Left selection below is a labelled DIRECT receiver call; reaching active
    // and the loss boundary use automatic physics, not manual clock stepping.
    private static async Task PartialChildLossDisarmsSurvivors(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (bool selectLeft in new[] { false, true })
        {
            string scenario = selectLeft ? "selected-left-remove-dormant-right" : "selected-right-free-dormant-left";
            GD.Print("TASK17A_PARTIAL_LOSS_BEGIN " + scenario);
            try
            {
                await WithFixture(tree, automatic: true, async (root, pair, left, right, player, health) =>
                {
                    CanyonSteamJet survivor = selectLeft ? left : right;
                    CanyonSteamJet missing = selectLeft ? right : left;
                    var interaction = new ThreadInteraction(player, player.GlobalPosition, pair.GlobalPosition, new ThreadAbilitySettings());
                    if (selectLeft)
                    {
                        if (!pair.TryReceiveThread(interaction))
                            throw new InvalidOperationException("Partial-loss prerequisite: DIRECT receiver must accept left selection.");
                        GD.Print("TASK17A_PARTIAL_LOSS_DIRECT_RECEIVER " + scenario);
                    }
                    await WaitForCommittedActive(tree, survivor, 240, scenario);
                    if (!root.IsInsideTree() || !pair.IsInsideTree() || survivor.GetParent() != pair ||
                        !pair.IsPhysicsProcessing() || left.IsPhysicsProcessing() || right.IsPhysicsProcessing() ||
                        !missing.DisabledForRun || missing.Frame.HazardActive || missing.Monitoring || !missing.Collision.Disabled ||
                        pair.SelectedSide != (selectLeft ? CanyonSteamDiverterSide.Left : CanyonSteamDiverterSide.Right))
                        throw new InvalidOperationException("Partial-loss prerequisite: live selected active sibling and owned clocks required.");
                    PrintPartialLoss(scenario, "before-loss", pair, survivor, health);
                    try
                    {
                        if (selectLeft) pair.RemoveChild(missing);
                        else missing.Free();
                        if ((selectLeft && missing.IsInsideTree()) || (!selectLeft && GodotObject.IsInstanceValid(missing)))
                            throw new InvalidOperationException("Partial-loss prerequisite: actual RemoveChild/Free boundary not reached.");
                        PrintPartialLoss(scenario, "after-loss-before-commit", pair, survivor, health);
                        await CommittedFrame(tree);
                        if (!pair.IsInsideTree() || !root.IsInsideTree() || survivor.GetParent() != pair || !survivor.IsInsideTree())
                            throw new InvalidOperationException("Partial-loss prerequisite: original parent and survivor must remain mounted.");
                        PrintPartialLoss(scenario, "next-committed-physics", pair, survivor, health);
                        AssertSurvivorDisarmed(survivor, scenario + " partial child loss disarms mounted survivor by next committed physics boundary");
                        int postDisarmHealth = health.CurrentHealth; // No healing or position/velocity reset.
                        TestAssert.True(!pair.TryReceiveThread(interaction), scenario + " incomplete original controller rejects receiver");
                        pair.Advance(10d);
                        pair.ResetForCheckpoint(); survivor.ResetForCheckpoint();
                        survivor.ResetForCheckpoint(); pair.ResetForCheckpoint();
                        pair.Advance(10d);
                        TestAssert.True(!pair.TryReceiveThread(interaction) && !pair.IsHandingOff,
                            scenario + " old advance/reset/receiver cannot resume incomplete pair");
                        for (int index = 0; index < 140; index++)
                        {
                            await CommittedFrame(tree);
                            AssertSurvivorDisarmed(survivor, scenario + " surviving jet stays disarmed beyond a whole cycle");
                            TestAssert.Equal(postDisarmHealth, health.CurrentHealth, scenario + " post-disarm health baseline never falls");
                        }
                        PrintPartialLoss(scenario, "old-callbacks-and-later-commits-harmless", pair, survivor, health);

                        // Retire the incomplete original, not an automatic replacement
                        // lookup/rebind. A wholly fresh instance owns fresh children.
                        root.RemoveChild(pair); pair.Free();
                        var fresh = new CanyonSteamDiverter { Name = "Diverter" };
                        CanyonSteamJet freshLeft = Jet("LeftJet", new Vector2(-100, -15));
                        CanyonSteamJet freshRight = Jet("RightJet", new Vector2(100, -15));
                        fresh.AddChild(freshLeft); fresh.AddChild(freshRight); root.AddChild(fresh);
                        if (!fresh.IsNodeReady() || !freshLeft.IsNodeReady() || !freshRight.IsNodeReady() ||
                            !fresh.IsPhysicsProcessing() || freshLeft.IsPhysicsProcessing() || freshRight.IsPhysicsProcessing())
                            throw new InvalidOperationException("Partial-loss prerequisite: fresh remount Ready/owned automatic clocks required.");
                        AssertDefaultReset(fresh, freshLeft, freshRight, scenario + " fresh remount independently starts default right safe");
                        await WaitForCommittedActive(tree, freshRight, 180, scenario + " fresh remount");
                        AssertJet(freshLeft, false, false, scenario + " fresh remount keeps left harmless");
                        TestAssert.True(freshLeft.DisabledForRun, scenario + " fresh remount never revives old bindings");
                        TestAssert.Equal(postDisarmHealth, health.CurrentHealth, scenario + " central real Player stays healthy after fresh automatic remount");
                        PrintPartialLoss(scenario, "fresh-remount-active", fresh, freshRight, health);
                    }
                    finally
                    {
                        // RemoveChild leaves a live off-tree dormant object. No old
                        // jet AdvanceRuntime/GetTree call or disposed-node observer.
                        if (selectLeft && GodotObject.IsInstanceValid(missing) && !missing.IsInsideTree()) missing.Free();
                    }
                });
                GD.Print("TASK17A_PARTIAL_LOSS_OK " + scenario);
            }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(scenario + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: partial child loss must fail closed\n" + string.Join("\n", failures));
    }

    private static async Task CommittedFrame(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task WaitForCommittedActive(SceneTree tree, CanyonSteamJet jet, int cap, string scenario)
    {
        for (int index = 0; index < cap && !jet.Frame.HazardActive; index++) await CommittedFrame(tree);
        if (!jet.Frame.HazardActive || jet.DisabledForRun || !jet.Monitoring || jet.Collision.Disabled)
            throw new InvalidOperationException("Partial-loss prerequisite: automatic selected active phase not reached: " + scenario);
    }

    private static void AssertSurvivorDisarmed(CanyonSteamJet survivor, string reason)
    {
        TestAssert.True(survivor.DisabledForRun, reason);
        AssertJet(survivor, false, false, reason);
    }

    private static void PrintPartialLoss(string scenario, string stage, CanyonSteamDiverter pair,
        CanyonSteamJet survivor, HealthComponent health) =>
        GD.Print($"TASK17A_PARTIAL_LOSS_STATE {scenario} {stage} physics={Engine.GetPhysicsFrames()} " +
            $"selected={pair.SelectedSide} busy={pair.IsHandingOff} parentMounted={pair.IsInsideTree()} " +
            $"survivorMounted={survivor.IsInsideTree()} disabled={survivor.DisabledForRun} " +
            $"frame={survivor.Frame} monitoring={survivor.Monitoring} shapeDisabled={survivor.Collision.Disabled} hp={health.CurrentHealth}");

    // Break caught: finite individual values whose effective cycle overflows
    // can escape child Ready/controller binding and commit a live hazard.
    // Player is already Ready inside right's future footprint before mounting
    // the pair. This checks committed state/damage, not private transient Frame.
    private static async Task InvalidMountCannotCommitDamage(SceneTree tree)
    {
        var root = new Node2D { Name = "Task17AInvalidMountZeroInvulnerabilityFixture", Position = new Vector2(400, 300) };
        CharacterBody2D player = Player(new Vector2(100, 14));
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        root.AddChild(Floor()); root.AddChild(player); ReleaseInput(); tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            if (!player.IsNodeReady() || !health.IsNodeReady() || health.CurrentHealth != 100)
                throw new InvalidOperationException("Runtime fixture prerequisite: real Player must be Ready before invalid pair mount.");
            var pair = new CanyonSteamDiverter { Name = "Diverter" };
            CanyonSteamJet left = Jet("LeftJet", new Vector2(-100, -15));
            CanyonSteamJet right = Jet("RightJet", new Vector2(100, -15));
            right.SafeSeconds = float.MaxValue; right.TelegraphSeconds = float.MaxValue; right.ActiveSeconds = float.MaxValue;
            pair.AddChild(left); pair.AddChild(right); root.AddChild(pair);
            if (!pair.IsNodeReady() || !left.IsNodeReady() || !right.IsNodeReady())
                throw new InvalidOperationException("Runtime fixture prerequisite: invalid pair must finish Ready synchronously.");
            AssertBothDisarmed(left, right, "invalid effective tuning disarms both jets before first physics commit");
            await Frames(tree, 70);
            AssertBothDisarmed(left, right, "invalid effective tuning remains harmless over real physics commits");
            TestAssert.Equal(100, health.CurrentHealth, "already-overlapping real Player takes no invalid-initialization damage");
        }
        finally { tree.Paused = false; ReleaseInput(); root.QueueFree(); await Frames(tree, 3); }
    }

    // Narrow direct-player fixture; no full level/save/settings/audio/recorder.
    // Prerequisites are NOT assertion-prefixed so harness faults cannot be RED.
    private static async Task WithFixture(SceneTree tree, bool automatic,
        Func<Node2D, CanyonSteamDiverter, CanyonSteamJet, CanyonSteamJet, CharacterBody2D, HealthComponent, Task> contract)
    {
        var root = new Node2D { Name = "Task17AFunctionalZeroInvulnerabilityFixture", Position = new Vector2(400, 300) };
        var pair = new CanyonSteamDiverter { Name = "Diverter" };
        // Godot auto-enables an overridden callback before Ready. Apply the
        // fixture's clock control only after real controller Ready completes,
        // before first physics; never reset HP/position/clocks to force a pass.
        if (!automatic) pair.Ready += () => pair.SetPhysicsProcess(false);
        CanyonSteamJet left = Jet("LeftJet", new Vector2(-100, -15));
        CanyonSteamJet right = Jet("RightJet", new Vector2(100, -15));
        pair.AddChild(left); pair.AddChild(right); root.AddChild(pair); root.AddChild(Floor());
        CharacterBody2D player = Player(new Vector2(0, 14));
        HealthComponent health = player.GetNode<HealthComponent>("Health"); root.AddChild(player);
        ReleaseInput(); tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            if (!root.IsNodeReady() || !pair.IsNodeReady() || !left.IsNodeReady() || !right.IsNodeReady() ||
                !player.IsNodeReady() || !health.IsNodeReady() || health.CurrentHealth != 100 ||
                tree.GetNodesInGroup("prologue_audio").Count != 0 || pair.IsPhysicsProcessing() != automatic)
                throw new InvalidOperationException("Runtime fixture prerequisite: Ready/Health/no-audio/clock-control boundary failed.");
            await contract(root, pair, left, right, player, health);
        }
        finally { tree.Paused = false; ReleaseInput(); root.QueueFree(); await Frames(tree, 3); }
    }

    private static CharacterBody2D Player(Vector2 position)
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "Player"; player.Position = position; player.GetNode<Camera2D>("Camera2D").Enabled = false;
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        health.MaxHealth = 100; health.InvulnerabilitySeconds = 0f;
        return player;
    }
    private static StaticBody2D Floor()
    {
        var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(0, 60),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(1000, 20) } });
        return floor;
    }
    private static async Task Frames(SceneTree tree, int count)
    { for (int index = 0; index < count; index++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static void AssertBothDisarmed(CanyonSteamJet left, CanyonSteamJet right, string reason)
    {
        TestAssert.True(left.DisabledForRun && right.DisabledForRun, reason);
        AssertJet(left, false, false, reason); AssertJet(right, false, false, reason);
    }
    private static void AssertDefaultReset(CanyonSteamDiverter pair, CanyonSteamJet left, CanyonSteamJet right, string reason)
    {
        TestAssert.True(pair.SelectedSide == CanyonSteamDiverterSide.Right && !pair.IsHandingOff &&
            left.DisabledForRun && !right.DisabledForRun, reason);
        TestAssert.Equal(0f, left.ElapsedSeconds, reason); TestAssert.Equal(0f, right.ElapsedSeconds, reason);
        AssertJet(left, false, false, reason); AssertJet(right, false, false, reason);
    }
    private static void AssertJet(CanyonSteamJet jet, bool warning, bool active, string reason)
    {
        TestAssert.True(jet.Frame.Telegraphing == warning && jet.Frame.HazardActive == active && jet.Frame.Traversable == !active,
            reason + " logical frame");
        TestAssert.True(jet.Collision.Disabled == !active && jet.Monitoring == active, reason + " actual collision/monitoring");
    }

    private static CanyonSteamJet Jet(string name, Vector2 position)
    {
        var jet = new CanyonSteamJet { Name = name, Position = position, Monitoring = false,
            SafeSeconds = 1f, TelegraphSeconds = .4f, ActiveSeconds = .6f };
        jet.AddChild(new CollisionShape2D { Name = "Collision", Disabled = true,
            Shape = new RectangleShape2D { Size = new Vector2(56, 150) } });
        jet.AddChild(new Node2D { Name = "Telegraph", Visible = false });
        jet.AddChild(new Node2D { Name = "ActiveVisual", Visible = false });
        return jet;
    }

    private static void ReleaseInput()
    {
        foreach (Key key in new[] { Key.E, Key.Q, Key.A, Key.D, Key.Space })
        {
            using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false, Echo = false };
            Input.ParseInputEvent(input);
        }
        foreach (MouseButton button in new[] { MouseButton.Left, MouseButton.Right })
        {
            using var input = new InputEventMouseButton { ButtonIndex = button, Pressed = false };
            Input.ParseInputEvent(input);
        }
        foreach (string action in new[] { "move_left", "move_right", "move_up", "move_down", "jump", "dash", "select_attack_mode", "select_grapple_mode", "primary_action", "shield" })
            if (Input.IsActionPressed(action)) Input.ActionRelease(action);
        Input.FlushBufferedEvents();
    }
}
