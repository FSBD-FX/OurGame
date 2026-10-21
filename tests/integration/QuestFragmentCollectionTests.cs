using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class QuestFragmentCollectionTests
{
    public static async Task Run(SceneTree tree)
    {
        PrologueLevelController level = null;
        try
        {
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            var paths = IsolatedPrologueFixture.Configure(level, "fragment-contact");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            flow.StartNew();
            await Frames(tree, 12);
            var health = level.Player.GetNode<HealthComponent>("Health");
            var hud = level.GetNode<PrologueHud>("Hud");
            var formal = hud.GetNode<PrologueFormalHud>("FormalHud");
            TestAssert.True(level.Player.IsOnFloor() && health.CurrentHealth == health.MaxHealth,
                "contact fixture begins with supported, full-health actual player");

            QuestFragment Add(string name, string id, Vector2 point, bool automaticSectorBinding = false)
            {
                Node2D parent = automaticSectorBinding
                    ? level.CurrentSectorRoot.GetNode<Node2D>("Gameplay") : level;
                var fragment = new QuestFragment { Name = name, StableId = id,
                    CollisionLayer = 0, CollisionMask = 2, Position = parent.ToLocal(point) };
                fragment.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 18 } });
                fragment.AddChild(new Node2D { Name = "AvailableVisual" });
                fragment.AddChild(new Label { Name = "Status" });
                if (!automaticSectorBinding) fragment.BindRuntime(level);
                parent.AddChild(fragment);
                return fragment;
            }
            void AssertCount(int count, string context)
            {
                var disk = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
                TestAssert.True(File.Exists(paths.SavePath) && !disk.WasRecovered &&
                    disk.Version == PrologueSaveData.CurrentVersion && disk.LayoutVersion == 2 &&
                    disk.CollectionRuleVersion == 1 && !disk.LegacyCollectionQuotaExempt &&
                    disk.CollectedFragmentIds.Count == count &&
                    level.Progress.CollectedFragmentIds.Count == count && flow.Save.CollectedFragmentIds.Count == count &&
                    level.Progress.CollectedFragmentIds.SetEquals(disk.CollectedFragmentIds) &&
                    level.Progress.CollectedFragmentIds.SetEquals(flow.Save.CollectedFragmentIds) &&
                    formal.CurrentModel.FragmentCount == count && formal.CurrentModel.FragmentRequired == 6,
                    context + " keeps valid disk bytes, live, Save and HUD in agreement");
            }

            var first = Add("FullHealthFragment", "canyon.v2.fragment.entrance", level.Player.GlobalPosition,
                automaticSectorBinding: true);
            int firstEvents = 0;
            int firstPlayerEntries = 0;
            bool firstObserverSawCommit = false;
            first.Collected += _ =>
            {
                firstEvents++;
                var diskAtEvent = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
                firstObserverSawCommit = !diskAtEvent.WasRecovered && diskAtEvent.CollectedFragmentIds.Contains(first.StableId) &&
                    level.Progress.CollectedFragmentIds.Contains(first.StableId) && flow.Save.CollectedFragmentIds.Contains(first.StableId) &&
                    first.IsCollected && !first.GetNode<Node2D>("AvailableVisual").Visible && formal.CurrentModel.FragmentCount == 1;
            };
            first.BodyEntered += body => { if (body == level.Player) firstPlayerEntries++; };
            await Frames(tree, 8);
            TestAssert.True(firstPlayerEntries == 1 && first.IsCollected && first.FeedbackCount == 1 && firstEvents == 1 && firstObserverSawCommit,
                "FullHealthStillCollects: actual physics BodyEntered collects once despite full health");
            AssertCount(1, "full-health contact");
            string firstHash = Hash(paths.SavePath);
            // Explicit duplicate signal negative-control, in addition to real first contact.
            first.EmitSignal(Area2D.SignalName.BodyEntered, level.Player);
            first.EmitSignal(Area2D.SignalName.BodyEntered, level.Player);
            TestAssert.True(firstEvents == 1 && first.FeedbackCount == 1 && Hash(paths.SavePath) == firstHash,
                "DuplicateBodyCallbacksOnce: repeated body callback cannot change saved bytes or repeat feedback");
            var duplicate = Add("DuplicateStableId", first.StableId, level.Player.GlobalPosition);
            await Frames(tree, 6);
            TestAssert.True(duplicate.IsCollected && duplicate.FeedbackCount == 0,
                "distinct node carrying already collected ID restores spent without another reward");
            var invalid = Add("InvalidFragment", "supply_cave", level.Player.GlobalPosition);
            await Frames(tree, 6);
            TestAssert.True(!invalid.IsCollected && invalid.FeedbackCount == 0 && Hash(paths.SavePath) == firstHash,
                "discovery IDs cannot become quest fragments through contact");
            var nonPlayer = new StaticBody2D { Name = "NonPlayerFixture", CollisionLayer = 2,
                CollisionMask = 0, Position = level.Player.GlobalPosition + new Vector2(220, 0) };
            nonPlayer.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10 } });
            level.AddChild(nonPlayer);
            var guarded = Add("NonPlayerGuard", "canyon.v2.fragment.abyss-floor", nonPlayer.GlobalPosition);
            int otherEntries = 0;
            guarded.BodyEntered += body => { if (body == nonPlayer) otherEntries++; };
            await Frames(tree, 6);
            TestAssert.True(otherEntries == 1 && !guarded.IsCollected && guarded.FeedbackCount == 0 &&
                Hash(paths.SavePath) == firstHash, "actual non-player body contact cannot claim a valid fragment");

            var second = Add("RetryFragment", "canyon.v2.fragment.rift", level.Player.GlobalPosition + new Vector2(120, 0));
            int secondEvents = 0;
            second.Collected += _ => secondEvents++;
            await Frames(tree, 3);
            PrologueSaveData oldSave = flow.Save;
            string oldHash = Hash(paths.SavePath);
            using (var writeDenied = new FileStream(paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                TestAssert.True(!PrologueSaveStore.LoadOrNew(paths.SavePath, 2).WasRecovered,
                    "failure fixture permits the production preflight read before denying replacement");
                second.GlobalPosition = level.Player.GlobalPosition;
                await Frames(tree, 8);
                TestAssert.True(second.OverlapsBody(level.Player) && second.LastResult == FragmentCollectionResult.SaveFailed &&
                    !second.IsCollected && second.GetNode<Node2D>("AvailableVisual").Visible &&
                    second.FeedbackCount == 0 && secondEvents == 0 && ReferenceEquals(oldSave, flow.Save) &&
                    Hash(paths.SavePath) == oldHash &&
                    second.GetNode<Label>("Status").Text.Contains("保存失败", StringComparison.Ordinal),
                    "FailSaveKeepsFragmentRetryable: actual atomic replacement failure leaves old Save, bytes, node and feedback intact with visible failure");
                AssertCount(1, "failed contact");
                TestAssert.True(Directory.GetFiles(paths.CaseRoot, ".save.json.*.tmp").Length == 0,
                    "failed atomic write removes its own temporary file");
            }
            second.GlobalPosition += new Vector2(120, 0);
            await Frames(tree, 4);
            second.GlobalPosition = level.Player.GlobalPosition;
            await Frames(tree, 8);
            TestAssert.True(second.IsCollected && secondEvents == 1 && second.FeedbackCount == 1,
                "unlock and real re-entry retries the preserved pickup exactly once");
            AssertCount(2, "retry success");

            hud.SetHealth(health.CurrentHealth, health.MaxHealth);
            hud.SetObjective("REACH THE CANYON BASIN");
            hud.ShowPrompt("K", "fixture prompt");
            formal.SetLanguage(GameLanguage.English);
            var counter = formal.GetNodeOrNull<Label>("Health/FragmentCounter");
            TestAssert.True(counter != null && counter.IsVisibleInTree() && counter.Text.Contains("2/6", StringComparison.Ordinal) &&
                counter.Text.Contains("NEED 4", StringComparison.Ordinal),
                "HUD real count survives wrapper health/objective/prompt updates and language switching");
            formal.SetLanguage(GameLanguage.ChineseSimplified);
            TestAssert.True(counter.Text.Contains("尚差 4", StringComparison.Ordinal), "Chinese quota deficit is explicit");
            var registryModel = PrologueHudViewModel.Gameplay(5, 5, 1f, "", "", "")
                .WithFragments(new[] { first.StableId, first.StableId, "supply_cave" }, true);
            formal.Present(registryModel);
            TestAssert.True(formal.CurrentModel.FragmentCount == 1 && formal.CurrentModel.CollectionQuotaExempt &&
                counter.Text.Contains("1/6", StringComparison.Ordinal) && counter.Text.Contains("旧进度", StringComparison.Ordinal),
                "HudUsesRegistryCount: duplicates/discoveries excluded and legacy entitlement never fakes 6/6");
            hud.SetFragments(level.Progress.CollectedFragmentIds, false);

            var third = Add("StateGuardFragment", "canyon.v2.fragment.abyss-floor", level.Player.GlobalPosition + new Vector2(120, 0));
            tree.Paused = true;
            third.EmitSignal(Area2D.SignalName.BodyEntered, level.Player);
            TestAssert.True(flow.TryCollectFragment(third.StableId) == FragmentCollectionResult.Unavailable,
                "paused session refuses direct and body collection without misreporting a disk failure");
            tree.Paused = false;
            int recoveries = 0;
            level.RecoveryCompleted += _ => recoveries++;
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f,
                650002UL, level.Player.GetInstanceId(), DamageGuardClass.Unblockable)) &&
                health.CurrentHealth == 0 && flow.State == PrologueGameFlowState.Recovering,
                "same-frame refusal fixture actually entered lethal recovery before the callback");
            third.EmitSignal(Area2D.SignalName.BodyEntered, level.Player);
            TestAssert.True(flow.TryCollectFragment(third.StableId) == FragmentCollectionResult.Unavailable,
                "lethal/deferred recovery same-frame contact cannot add collection");
            for (int frame = 0; frame < 120 && recoveries == 0; frame++) await Frames(tree, 1);
            await Frames(tree, 4);
            TestAssert.True(recoveries == 1 && !third.IsCollected, "death keeps successful two fragments without claiming refused third");
            AssertCount(2, "post-contact death");

            flow.ReturnToTitle();
            flow.StartNew();
            // Move retained instances away before another physical entry in the new run.
            first.GlobalPosition += new Vector2(400, 0);
            duplicate.GlobalPosition += new Vector2(400, 0);
            second.GlobalPosition += new Vector2(400, 0);
            await Frames(tree, 4);
            AssertCount(0, "explicit new game");
            TestAssert.True(!first.IsCollected && first.GetNode<Node2D>("AvailableVisual").Visible,
                "same mounted pickup becomes available after a real new-game reset");
            first.GlobalPosition = level.Player.GlobalPosition;
            await Frames(tree, 6);
            AssertCount(1, "recollect after new game");
            TestAssert.True(firstEvents == 2 && first.FeedbackCount == 2, "feedback repeats only for a genuinely new run");
            first.GlobalPosition += new Vector2(400, 0);
            var reentrant = Add("ReentrantFeedback", "canyon.v2.fragment.courtyard", level.Player.GlobalPosition);
            int reentrantEvents = 0;
            reentrant.Collected += _ =>
            {
                reentrantEvents++;
                flow.ReturnToTitle();
                flow.StartNew();
            };
            await Frames(tree, 8);
            AssertCount(0, "new game during collection feedback");
            TestAssert.True(reentrantEvents == 1 && reentrant.FeedbackCount == 1 && !reentrant.IsCollected,
                "public feedback may reset the session without later old-session writes or freeing the reusable pickup");
            // Keep known audio teardown independent of the queued-unload negative control.
            await TestSceneTeardown.DrainAudioOwner(tree, level);
            var unloading = Add("UnloadFeedback", "canyon.v2.fragment.gear-gallery", level.Player.GlobalPosition);
            bool unloadObserved = false;
            bool laterClaimRefused = false;
            unloading.Collected += _ =>
            {
                level.QueueFree();
                unloadObserved = level.IsQueuedForDeletion();
                string committedHash = Hash(paths.SavePath);
                PrologueSaveData committedSave = flow.Save;
                laterClaimRefused = flow.TryCollectFragment("canyon.v2.fragment.ascent-mid") == FragmentCollectionResult.Unavailable &&
                    ReferenceEquals(committedSave, flow.Save) && Hash(paths.SavePath) == committedHash;
            };
            await Frames(tree, 8);
            level = null;
            TestAssert.True(unloadObserved && laterClaimRefused,
                "successful feedback requesting unload prevents any later same-frame claim/write into the queued session");
            string beforeRemount = Hash(paths.SavePath);
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.Configure(level, "fragment-contact-remount");
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.SavePath = paths.SavePath;
            flow.HeadlessAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            TestAssert.True(flow.HasSave, "fresh scene discovers the actual contact-written slot after queued unload");
            flow.Continue();
            await Frames(tree, 5);
            hud = level.GetNode<PrologueHud>("Hud");
            formal = hud.GetNode<PrologueFormalHud>("FormalHud");
            var restored = Add("RestoredContactFragment", "canyon.v2.fragment.gear-gallery",
                level.Player.GlobalPosition, automaticSectorBinding: true);
            await Frames(tree, 6);
            AssertCount(1, "real contact slot after unload/remount Continue");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && restored.IsCollected &&
                !restored.GetNode<Node2D>("AvailableVisual").Visible && restored.FeedbackCount == 0 &&
                beforeRemount == Hash(paths.SavePath),
                "automatic sector binding restores the spent pickup and HUD without duplicate feedback or rewriting contact save");
            QuestFragmentCrossProcessTests.PreserveFlowWrittenSlot(paths.SavePath, "contact");
            GD.Print("QUEST_FRAGMENT_CONTACT_OK controlled_contact_fixture=true normal_route=false");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }
    private static async Task Frames(SceneTree tree, int count)
    { for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
